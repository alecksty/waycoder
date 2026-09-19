// =====================================================================
// world.js — 3D 战场世界：天空、光照、地形高度场、海洋、体积云、地面基地
// 地形高度用解析函数 heightAt(x,z) 计算，逻辑与网格顶点完全一致，
// 因此碰撞检测无需射线求交，直接用函数采样即可。
// =====================================================================
import * as THREE from 'three';
import { ValueNoise2D, RNG, clamp, lerp, smoothstep, computeFormationOffset } from './utils.js';
import { buildJetModel, PALETTE_ALLY, PALETTE_ENEMY } from './aircraft.js';

export const WORLD = {
  half: 3200,          // 半边长（米）
  seaLevel: 0,
  ceiling: 4200,       // 升限
  cloudBase: 900,
  cloudTop: 2100,
  plateau: 22,         // 中央机场高原标高
  plateauR: 900,       // 高原半径（此半径内完全平坦）
  plateauFade: 1500,   // 高原到野外的过渡宽度
  fieldMax: 300,       // 野外丘陵最大标高
};

// 复用的临时向量（高度场采样/设施命中判定都在热路径上，避免每帧 new）
const _tmpVec = new THREE.Vector3();
const _zeroVec = new THREE.Vector3();

export class World {
  constructor(mount, quality = 'mid') {
    this.quality = quality;
    this.noise = new ValueNoise2D(20240913);
    this.rng = new RNG(4242);

    // ---------------- 渲染器 ----------------
    this.renderer = new THREE.WebGLRenderer({ antialias: quality !== 'low', powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, quality === 'high' ? 2 : 1.5));
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.05;
    mount.appendChild(this.renderer.domElement);

    // ---------------- 场景与相机 ----------------
    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.FogExp2(0x9fc6e8, 0.00019);
    this.camera = new THREE.PerspectiveCamera(62, window.innerWidth / window.innerHeight, 0.6, 14000);

    // 外部可注册需要随相机移动的远景元素（地平线山脉、海面等）
    this.followers = [];

    this._buildSky();
    this._buildLights();
    this._buildOcean();
    this._buildTerrain();
    this._buildHorizon();
    this._buildAirbase();
    this._buildClouds();

    this._onResize = () => {
      this.camera.aspect = window.innerWidth / window.innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(window.innerWidth, window.innerHeight);
    };
    window.addEventListener('resize', this._onResize);
  }

  // ===================================================================
  // 天空穹顶：顶点着色器的渐变 + 太阳辉光
  // ===================================================================
  _buildSky() {
    const geo = new THREE.SphereGeometry(9000, 32, 20);
    const mat = new THREE.ShaderMaterial({
      side: THREE.BackSide,
      depthWrite: false,
      fog: false,
      uniforms: {
        topColor: { value: new THREE.Color(0x0a2a5e) },
        midColor: { value: new THREE.Color(0x63a6e0) },
        bottomColor: { value: new THREE.Color(0xd8ecff) },
        sunDir: { value: new THREE.Vector3(0.5, 0.42, -0.75).normalize() },
        sunColor: { value: new THREE.Color(0xfff3d0) },
      },
      vertexShader: /* glsl */`
        varying vec3 vDir;
        void main() {
          vDir = normalize(position);
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: /* glsl */`
        uniform vec3 topColor, midColor, bottomColor, sunColor, sunDir;
        varying vec3 vDir;
        void main() {
          float h = clamp(vDir.y * 0.5 + 0.5, 0.0, 1.0);
          vec3 col = mix(bottomColor, midColor, smoothstep(0.42, 0.58, h));
          col = mix(col, topColor, smoothstep(0.55, 1.0, h));
          float sun = max(dot(normalize(vDir), normalize(sunDir)), 0.0);
          col += sunColor * pow(sun, 220.0) * 1.6;
          col += sunColor * pow(sun, 8.0) * 0.16;
          gl_FragColor = vec4(col, 1.0);
        }
      `,
    });
    this.sky = new THREE.Mesh(geo, mat);
    this.sky.frustumCulled = false;
    this.scene.add(this.sky);
  }

  _buildLights() {
    this.sun = new THREE.DirectionalLight(0xfff2d8, 2.1);
    this.sun.position.set(900, 760, -1350);
    this.scene.add(this.sun);
    this.scene.add(this.sun.target);

    this.hemi = new THREE.HemisphereLight(0xbfe0ff, 0x415a3a, 0.85);
    this.scene.add(this.hemi);

    this.ambient = new THREE.AmbientLight(0xffffff, 0.22);
    this.scene.add(this.ambient);
  }

  // ===================================================================
  // 地形高度场：解析噪声，被网格生成与碰撞检测共用
  //   中央 900m 为完全平坦的机场高原（标高 22m，高于海面），
  //   向外 900→2400m 平滑过渡到野外丘陵/山脉，最大约 300m。
  // ===================================================================
  fieldHeight(x, z) {
    const n1 = this.noise.fbm(x / 2100, z / 2100, 5);
    const n2 = this.noise.fbm(x / 480 + 31.7, z / 480 - 12.3, 3);
    return (n1 - 0.5) * 1180 + (n2 - 0.5) * 190;
  }

  heightAt(x, z) {
    const d = Math.hypot(x, z);
    const fade = smoothstep(clamp((d - WORLD.plateauR) / WORLD.plateauFade, 0, 1));
    const k = 0.985 * fade;                      // 高原内 k=0：绝对平坦
    const field = clamp(this.fieldHeight(x, z), -220, WORLD.fieldMax);
    return field * k + WORLD.plateau * (1 - fade);
  }

  /** 地形是否为机场高原（用于跑道/机库等地物的硬碰撞检测） */
  onPlateau(x, z, r = 700) {
    return Math.hypot(x, z) < WORLD.plateauR + r &&
           Math.abs(x) < 190 &&
           Math.abs(z) < r;
  }

  heightNormal(x, z, eps = 12) {
    const hL = this.heightAt(x - eps, z), hR = this.heightAt(x + eps, z);
    const hD = this.heightAt(x, z - eps), hU = this.heightAt(x, z + eps);
    return new THREE.Vector3(hL - hR, 2 * eps, hD - hU).normalize();
  }

  _buildTerrain() {
    const seg = { low: 128, mid: 220, high: 320 }[this.quality] || 220;
    const size = WORLD.half * 2;
    const geo = new THREE.PlaneGeometry(size, size, seg, seg);
    geo.rotateX(-Math.PI / 2);
    const pos = geo.attributes.position;
    const colors = new Float32Array(pos.count * 3);
    const cLow = new THREE.Color(0x466e39);
    const cMid = new THREE.Color(0x6f7f45);
    const cHigh = new THREE.Color(0x8b8f97);
    const cSnow = new THREE.Color(0xf2f6fa);
    const cSand = new THREE.Color(0xc9b280);
    const tmp = new THREE.Color();

    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i), z = pos.getZ(i);
      const h = this.heightAt(x, z);
      pos.setY(i, h);
      if (h < 3) tmp.copy(cSand);
      else if (h < 120) tmp.copy(cSand).lerp(cLow, clamp(h / 120, 0, 1));
      else if (h < 230) tmp.copy(cLow).lerp(cMid, clamp((h - 120) / 110, 0, 1));
      else if (h < 300) tmp.copy(cMid).lerp(cHigh, clamp((h - 230) / 70, 0, 1));
      else tmp.copy(cHigh).lerp(cSnow, clamp((h - 300) / 90, 0, 1));
      // 轻微色噪，避免大片纯色
      const j = 0.92 + this.noise.sample(x * 0.01, z * 0.01) * 0.16;
      colors[i * 3] = tmp.r * j; colors[i * 3 + 1] = tmp.g * j; colors[i * 3 + 2] = tmp.b * j;
    }
    geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));
    geo.computeVertexNormals();

    const mat = new THREE.MeshLambertMaterial({ vertexColors: true });
    this.terrain = new THREE.Mesh(geo, mat);
    this.terrain.frustumCulled = false;
    this.scene.add(this.terrain);
  }

  // ===================================================================
  // 远景地平线山脉：环形网格，随相机同步平移，制造「无限地平线」
  // ===================================================================
  _horizonHeight(angle, r) {
    const x = Math.cos(angle) * r, z = Math.sin(angle) * r;
    const n = this.noise.fbm(x / 2600 + 7.3, z / 2600 - 3.1, 4);
    const ridge = Math.pow(Math.max(0, n), 1.35);
    // 内缘贴海面，外缘隆起成山脊
    const band = smoothstep(clamp((r - 3400) / 2600, 0, 1));
    return ridge * 1750 * band;
  }

  _buildHorizon() {
    const thetaSeg = 128, ringSeg = 14;
    const r0 = 3300, r1 = 9000;
    const verts = [], cols = [], idx = [];
    const cNear = new THREE.Color(0x54606b), cFar = new THREE.Color(0x9fb6c9), cSnow = new THREE.Color(0xe8f2fb);
    const tmp = new THREE.Color();
    for (let j = 0; j <= ringSeg; j++) {
      const r = lerp(r0, r1, j / ringSeg);
      for (let i = 0; i <= thetaSeg; i++) {
        const a = (i / thetaSeg) * Math.PI * 2;
        const h = this._horizonHeight(a, r);
        verts.push(Math.cos(a) * r, h, Math.sin(a) * r);
        tmp.copy(cNear).lerp(cFar, clamp((h - 200) / 1200, 0, 1));
        if (h > 1250) tmp.lerp(cSnow, clamp((h - 1250) / 500, 0, 1));
        cols.push(tmp.r, tmp.g, tmp.b);
      }
    }
    const stride = thetaSeg + 1;
    for (let j = 0; j < ringSeg; j++) {
      for (let i = 0; i < thetaSeg; i++) {
        const a = j * stride + i, b = a + 1, c = a + stride, d = c + 1;
        idx.push(a, c, b, b, c, d);
      }
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3));
    geo.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3));
    geo.setIndex(idx);
    geo.computeVertexNormals();
    const mat = new THREE.MeshLambertMaterial({ vertexColors: true, fog: true });
    this.horizon = new THREE.Mesh(geo, mat);
    this.horizon.frustumCulled = false;
    this.scene.add(this.horizon);
    // 随相机平移（略慢一点，制造视差）
    this.followers.push({ obj: this.horizon, factor: 0.92 });
  }

  _buildOcean() {
    const geo = new THREE.PlaneGeometry(24000, 24000, 1, 1);
    geo.rotateX(-Math.PI / 2);
    const mat = new THREE.MeshStandardMaterial({
      color: 0x123a63, roughness: 0.18, metalness: 0.55,
      transparent: true, opacity: 0.94,
    });
    this.ocean = new THREE.Mesh(geo, mat);
    this.ocean.position.y = WORLD.seaLevel;
    this.ocean.frustumCulled = false;
    this.scene.add(this.ocean);
    // 海面随相机按网格步长吸附——移动的是整格，视觉上完全无缝
    this._oceanStep = 1000;
    this.followers.push({ obj: this.ocean, snap: this._oceanStep });
  }

  // ===================================================================
  // 地面基地：跑道 + 滑行道 + 机库 + 塔台 + 防空阵地 + 导航灯
  // 中央 900m 是平坦高原，无需再做贴地拟合。
  // ===================================================================
  _buildAirbase() {
    const grp = new THREE.Group();
    grp.position.set(0, WORLD.plateau, 0);

    const concrete = new THREE.MeshStandardMaterial({ color: 0x4a4f55, roughness: 0.95 });
    const asphalt = new THREE.MeshStandardMaterial({ color: 0x3a4046, roughness: 0.98 });
    const dark = new THREE.MeshStandardMaterial({ color: 0x2b3037, roughness: 0.9 });
    const metal = new THREE.MeshStandardMaterial({ color: 0x8d969f, roughness: 0.5, metalness: 0.6 });
    const stripe = new THREE.MeshBasicMaterial({ color: 0xe8eef5 });

    // 主跑道：长 1900m、宽 70m
    const runway = new THREE.Mesh(new THREE.BoxGeometry(70, 1.2, 1900), concrete);
    runway.position.y = 0.6;
    grp.add(runway);
    // 中线虚线
    for (let i = -14; i <= 14; i++) {
      const mark = new THREE.Mesh(new THREE.BoxGeometry(3.2, 0.42, 44), stripe);
      mark.position.set(0, 1.28, i * 64);
      grp.add(mark);
    }
    // 两端跑道头（横排白条）
    for (const zEnd of [-930, 930]) {
      for (let i = -4; i <= 4; i++) {
        const t = new THREE.Mesh(new THREE.BoxGeometry(5, 0.42, 60), stripe);
        t.position.set(i * 8, 1.28, zEnd);
        grp.add(t);
      }
    }
    // 跑道边灯（夜间/低能见度引导）
    const edgeLightMat = new THREE.MeshStandardMaterial({ color: 0x1b2a38, emissive: 0x2ad0ff, emissiveIntensity: 1.1 });
    for (let i = -15; i <= 15; i++) {
      for (const s of [-1, 1]) {
        const l = new THREE.Mesh(new THREE.SphereGeometry(0.75, 8, 6), edgeLightMat);
        l.position.set(37 * s, 1.4, i * 62);
        grp.add(l);
      }
    }

    // 滑行道 + 停机坪
    const taxi = new THREE.Mesh(new THREE.BoxGeometry(150, 1.0, 26), asphalt);
    taxi.position.set(-110, 0.5, -300);
    grp.add(taxi);
    const apron = new THREE.Mesh(new THREE.BoxGeometry(220, 1.0, 240), asphalt);
    apron.position.set(-190, 0.5, -300);
    grp.add(apron);

    // 机库（拱顶）
    for (let i = 0; i < 4; i++) {
      const hz = -420 + i * 130;
      const hangar = new THREE.Mesh(new THREE.BoxGeometry(46, 17, 34), metal);
      hangar.position.set(-190, 8.5, hz);
      grp.add(hangar);
      const roof = new THREE.Mesh(new THREE.CylinderGeometry(17.4, 17.4, 46, 14, 1, false, 0, Math.PI), metal);
      roof.rotation.z = Math.PI / 2;
      roof.position.set(hangar.position.x, 17, hz);
      grp.add(roof);
      const door = new THREE.Mesh(new THREE.BoxGeometry(0.6, 12, 26), dark);
      door.position.set(-166, 6.5, hz);
      grp.add(door);
    }

    // 塔台
    const tower = new THREE.Mesh(new THREE.BoxGeometry(16, 42, 16), dark);
    tower.position.set(72, 21, 180);
    grp.add(tower);
    const towerTop = new THREE.Mesh(new THREE.BoxGeometry(24, 9, 24), new THREE.MeshStandardMaterial({
      color: 0x1b2a38, emissive: 0x2ad0ff, emissiveIntensity: 0.55,
    }));
    towerTop.position.set(72, 46, 180);
    grp.add(towerTop);
    const towerBeacon = new THREE.Mesh(new THREE.SphereGeometry(2, 10, 8), new THREE.MeshBasicMaterial({ color: 0xff3355 }));
    towerBeacon.position.set(72, 53, 180);
    grp.add(towerBeacon);

    // 跑道头灯标
    const beaconMat = new THREE.MeshBasicMaterial({ color: 0xff3355 });
    for (let i = -1; i <= 1; i += 2) {
      const b = new THREE.Mesh(new THREE.SphereGeometry(2.4, 10, 8), beaconMat);
      b.position.set(i * 40, 3, 940);
      grp.add(b);
    }

    // 停机坪上的候机（僚机地面停放；玩家着陆加油时也在同一处）
    this.parkedJets = [];
    const parkedSlots = [
      { side: -1, pal: PALETTE_ALLY },   // 僚机 1
      { side: 1, pal: PALETTE_ALLY },    // 僚机 2
      { side: -1, pal: PALETTE_ENEMY, enemy: true },
      { side: 1, pal: PALETTE_ENEMY, enemy: true },
    ];
    for (const slot of parkedSlots) {
      const off = computeFormationOffset(slot.side);
      const mesh = buildJetModel(slot.pal, 1);
      // 机头朝跑道方向（-Z），与起飞方向一致
      mesh.position.set(-190 + off.x * 0.5, 1.4, -300 + off.z * 0.5);
      mesh.rotation.y = 0;
      grp.add(mesh);
      this.parkedJets.push({ mesh, side: slot.side, enemy: !!slot.enemy, alive: true, home: mesh.position.clone() });
    }

    // 可摧毁地面设施：油罐 / 弹药库 / 机库（对地突击的备选目标）
    this.groundFacilities = [];
    const mkFacility = (kind, x, z, hp, radius) => {
      const f = new THREE.Group();
      f.position.set(x, this.heightAt(x, z) - WORLD.plateau, z);
      if (kind === 'fuel') {
        for (let i = 0; i < 3; i++) {
          const tank = new THREE.Mesh(new THREE.CylinderGeometry(6.5, 6.5, 14, 12),
            new THREE.MeshStandardMaterial({ color: 0x9aa79b, roughness: 0.6, metalness: 0.35 }));
          tank.position.set(i * 15 - 15, 7, 0);
          f.add(tank);
        }
      } else if (kind === 'ammo') {
        for (let i = 0; i < 4; i++) {
          const box = new THREE.Mesh(new THREE.BoxGeometry(14, 7, 11),
            new THREE.MeshStandardMaterial({ color: 0x5c6348, roughness: 0.9 }));
          box.position.set((i % 2) * 18 - 9, 3.5, Math.floor(i / 2) * 15 - 7);
          box.rotation.y = i * 0.15;
          f.add(box);
        }
      } else {
        for (let i = 0; i < 2; i++) {
          const roof = new THREE.Mesh(new THREE.CylinderGeometry(17.4, 17.4, 40, 14, 1, false, 0, Math.PI),
            new THREE.MeshStandardMaterial({ color: 0x8d969f, roughness: 0.55, metalness: 0.5 }));
          roof.rotation.z = Math.PI / 2;
          roof.position.set(i * 46 - 23, 17, 0);
          f.add(roof);
        }
      }
      grp.add(f);
      const item = { kind, group: f, alive: true, hp, radius };
      this.groundFacilities.push(item);
      return item;
    };
    mkFacility('fuel', -190, -110, 2, 90);
    mkFacility('ammo', -320, -430, 2, 80);
    mkFacility('hangar', -190, 260, 3, 110);

    // 可重复利用的坠机残骸（坠海/坠地的飞机拖出黑烟柱，作视觉地标）
    this.crashSites = [];
    const crashMat = new THREE.MeshStandardMaterial({ color: 0x1a1c20, roughness: 1 });
    for (let i = 0; i < 6; i++) {
      const g = new THREE.Group();
      const hull = new THREE.Mesh(new THREE.BoxGeometry(16, 5, 26), crashMat);
      hull.rotation.z = this.rng.range(-0.3, 0.3);
      hull.rotation.y = this.rng.range(0, Math.PI);
      g.add(hull);
      g.visible = false;
      this.scene.add(g);
      this.crashSites.push({ group: g, life: 0 });
    }

    // 敌方高炮阵地带：环绕战场外圈，会向低空目标射击（对地突击波的目标）
    this.flakSites = [];
    const flakMat = new THREE.MeshStandardMaterial({ color: 0x6d6a5c, roughness: 0.85 });
    const crateMat = new THREE.MeshStandardMaterial({ color: 0x55603f, roughness: 0.95 });
    const radarMat = new THREE.MeshStandardMaterial({ color: 0x8d969f, roughness: 0.55, metalness: 0.5 });
    for (let i = 0; i < 6; i++) {
      const a = (i / 6) * Math.PI * 2 + 0.5;
      const r = 2800;
      const site = new THREE.Group();
      // 落位到实际地形高度，避免阵地下陷/悬空
      const sx = Math.cos(a) * r, sz = Math.sin(a) * r;
      site.position.set(sx, this.heightAt(sx, sz) - WORLD.plateau, sz);
      for (let j = 0; j < 3; j++) {
        const gun = new THREE.Mesh(new THREE.CylinderGeometry(1.6, 2.2, 6 + j, 8), flakMat);
        gun.position.set(Math.cos(j * 2.1) * 9, 3 + j * 0.6, Math.sin(j * 2.1) * 9);
        gun.rotation.z = 0.6;
        site.add(gun);
      }
      const sandbag = new THREE.Mesh(new THREE.TorusGeometry(15, 3, 6, 16), flakMat);
      sandbag.rotation.x = Math.PI / 2;
      sandbag.position.y = 2;
      site.add(sandbag);
      // 弹药箱
      for (let j = 0; j < 3; j++) {
        const crate = new THREE.Mesh(new THREE.BoxGeometry(3.4, 2.6, 3.0), crateMat);
        crate.position.set(-16 - j * 4, 1.3, 12 - j * 5);
        crate.rotation.y = j * 0.4;
        site.add(crate);
      }
      // 搜索雷达（旋转天线，视觉上表明这是「活」的威胁）
      const dish = new THREE.Mesh(new THREE.BoxGeometry(9, 0.5, 3), radarMat);
      const mast = new THREE.Mesh(new THREE.CylinderGeometry(0.7, 0.9, 11, 8), radarMat);
      mast.position.y = 5.5;
      dish.position.y = 11;
      dish.name = 'dish';
      const radar = new THREE.Group();
      radar.position.set(14, 0, -12);
      radar.add(mast, dish);
      site.add(radar);
      site.userData.radar = radar;
      site.userData.alive = true;
      site.userData.hp = 2;
      grp.add(site);
      this.flakSites.push(site);
    }

    this.airbase = grp;
    this.scene.add(grp);
  }

  /**
   * 取得地面设施的世界坐标。
   * 阵地是基地组（airbase）的子节点，position 是组内局部坐标，
   * 计算射击诸元/雷达标绘时必须叠加基地偏移，否则会打偏一整座高原。
   */
  siteWorldPos(site, out = new THREE.Vector3()) {
    const b = this.airbase ? this.airbase.position : { x: 0, y: 0, z: 0 };
    return out.set(site.position.x + b.x, site.position.y + b.y, site.position.z + b.z);
  }

  /** 按序号取高炮阵地（跨波次索引稳定，便于自测与任务指定目标） */
  getFlakSite(i) {
    const n = this.flakSites.length;
    if (!n) return null;
    return this.flakSites[((i % n) + n) % n];
  }

  /** 战场几何中心（0,0）的世界坐标，轰炸机航线终点 */
  baseWorldPos(out = new THREE.Vector3()) {
    return out.set(0, WORLD.plateau + 900, 0);
  }

  /**
   * 机场飞行区判定。
   * 着陆加油要求「在跑道正上方 + 贴地 + 大致对正跑道」。跑道沿 Z 轴布置，
   * 所以横向 |x| 必须很小，纵向放宽到 1000m。
   */
  overAirfield(pos, margin = 120) {
    if (Math.abs(pos.x) > 90) return false;
    if (Math.abs(pos.z) > 1000) return false;
    const alt = pos.y - this.heightAt(pos.x, pos.z);
    return alt >= 0 && alt < margin;
  }

  /** 按停机位索引取世界坐标（僚机地面停放 / 玩家着陆位共用） */
  parkSlotWorldPos(side, index = 0, out = new THREE.Vector3()) {
    const off = computeFormationOffset(side);
    const b = this.airbase ? this.airbase.position : { x: 0, y: 0, z: 0 };
    return out.set(-190 + off.x * 0.5 + b.x, 1.4 + b.y, -300 + off.z * 0.5 + index * 14 + b.z);
  }

  /** 找一个空闲的坠机残骸位，把黑烟柱拖到指定位置（坠机点地标） */
  showCrash(pos) {
    const list = this.crashSites;
    if (!list || !list.length) return null;
    let slot = list.find((c) => c.life <= 0) || list[0];
    slot.life = 26;
    slot.group.position.set(pos.x, Math.max(pos.y, this.heightAt(pos.x, pos.z)) + 1, pos.z);
    slot.group.visible = true;
    return slot;
  }

  /** 残骸烟雾寿命递减（由主循环调用） */
  updateCrashSites(dt) {
    for (const c of this.crashSites || []) {
      if (c.life <= 0) continue;
      c.life -= dt;
      if (c.life <= 0) c.group.visible = false;
    }
  }

  /**
   * 对地面可摧毁设施施加伤害（油罐/弹药库/机库）。
   * 返回本次被摧毁的设施数组，交给主程序做连锁爆炸与计分。
   */
  damageFacilities(pos, radius, amount = 1) {
    const killed = [];
    for (const f of this.groundFacilities) {
      if (!f.alive) continue;
      const wp = _tmpVec.set(f.group.position.x, f.group.position.y, f.group.position.z);
      wp.add(this.airbase ? this.airbase.position : _zeroVec);
      wp.y = this.heightAt(wp.x, wp.z) + 6;
      if (wp.distanceTo(pos) > Math.max(radius, f.radius)) continue;
      f.hp -= amount;
      if (f.hp <= 0) {
        f.alive = false;
        f.group.visible = false;
        killed.push(f);
      }
    }
    return killed;
  }

  // ===================================================================
  // 体积云：用带云纹理的 Sprite 组成云层，禁用纹理下载，程序化生成
  // ===================================================================
  _cloudTexture() {
    const s = 128;
    const cv = document.createElement('canvas');
    cv.width = cv.height = s;
    const ctx = cv.getContext('2d');
    const img = ctx.createImageData(s, s);
    for (let y = 0; y < s; y++) {
      for (let x = 0; x < s; x++) {
        const dx = (x / s - 0.5) * 2, dy = (y / s - 0.5) * 2;
        const r = Math.hypot(dx, dy);
        let a = clamp(1 - r, 0, 1);
        a *= a;
        const n = this.noise.fbm(x / 16, y / 16, 4);
        const i = (y * s + x) * 4;
        const v = 235 + n * 20;
        img.data[i] = v; img.data[i + 1] = v; img.data[i + 2] = 255;
        img.data[i + 3] = Math.floor(clamp(a * (0.45 + n * 0.85), 0, 1) * 235);
      }
    }
    ctx.putImageData(img, 0, 0);
    const tex = new THREE.CanvasTexture(cv);
    tex.colorSpace = THREE.SRGBColorSpace;
    return tex;
  }

  _buildClouds() {
    const count = { low: 90, mid: 180, high: 260 }[this.quality] || 180;
    const tex = this._cloudTexture();
    const mat = new THREE.SpriteMaterial({
      map: tex, transparent: true, opacity: 0.5, depthWrite: false, fog: true,
    });
    this.clouds = new THREE.Group();
    for (let i = 0; i < count; i++) {
      const sp = new THREE.Sprite(mat.clone());
      const a = this.rng.range(0, Math.PI * 2);
      const r = this.rng.range(200, WORLD.half * 0.95);
      sp.position.set(Math.cos(a) * r, this.rng.range(WORLD.cloudBase, WORLD.cloudTop), Math.sin(a) * r);
      const w = this.rng.range(320, 900);
      sp.scale.set(w, w * this.rng.range(0.35, 0.6), 1);
      sp.material.opacity = this.rng.range(0.28, 0.62);
      sp.userData.speed = this.rng.range(3, 9);
      this.clouds.add(sp);
    }
    this.scene.add(this.clouds);
  }

  // ===================================================================
  // 每帧更新：云层漂移、天空/太阳跟随相机、远景元素跟随、海面波纹
  // ===================================================================
  update(dt, camera) {
    // 自测/离屏调用时常不传相机：回落到本世界自带相机，避免 undefined.camera 崩溃
    if (!camera) camera = this.camera;
    this.sky.position.copy(camera.position);
    this.sun.target.position.set(camera.position.x, 0, camera.position.z);
    this.sun.position.set(camera.position.x + 900, 760, camera.position.z - 1350);

    // 地面设施的雷达天线持续转动（视觉活性；被摧毁的阵地停转）
    for (const site of this.flakSites) {
      const radar = site.userData.radar;
      if (radar && site.userData.alive !== false) radar.rotation.y += dt * 1.6;
    }

    // 远景跟随：地平线山脉整体平移；海面按网格吸附平移（无缝接续）
    const followers = this.followers;
    for (let i = 0; i < followers.length; i++) {
      const f = followers[i];
      if (f.snap) {
        f.obj.position.x = Math.round(camera.position.x / f.snap) * f.snap;
        f.obj.position.z = Math.round(camera.position.z / f.snap) * f.snap;
      } else {
        f.obj.position.x = camera.position.x * (f.factor ?? 1);
        f.obj.position.z = camera.position.z * (f.factor ?? 1);
      }
    }

    // 云层漂移（相对世界，按相机为基准循环，避免飞远后无云）
    const arr = this.clouds.children;
    for (let i = 0; i < arr.length; i++) {
      const c = arr[i];
      c.position.x += c.userData.speed * dt;
      const dx = c.position.x - camera.position.x;
      if (dx > WORLD.half) c.position.x -= WORLD.half * 2;
      else if (dx < -WORLD.half) c.position.x += WORLD.half * 2;
    }

    // 海面轻微起伏（用整片位移模拟波峰，成本为零）
    const t = (this._time = (this._time || 0) + dt);
    this.ocean.position.y = WORLD.seaLevel + Math.sin(t * 0.6) * 0.35;
  }

  /** 地形碰撞检测：返回 true 表示撞地 */
  hitsGround(pos, margin = 1.5) {
    if (pos.y - margin < WORLD.seaLevel) return true;
    return pos.y - margin < this.heightAt(pos.x, pos.z);
  }

  /** 位置钳制在战场范围内 */
  clampToArena(pos, out = { hit: false }) {
    const lim = WORLD.half - 60;
    out.hit = false;
    if (pos.x > lim) { pos.x = lim; out.hit = true; }
    if (pos.x < -lim) { pos.x = -lim; out.hit = true; }
    if (pos.z > lim) { pos.z = lim; out.hit = true; }
    if (pos.z < -lim) { pos.z = -lim; out.hit = true; }
    if (pos.y > WORLD.ceiling) { pos.y = WORLD.ceiling; out.hit = true; }
    if (pos.y < 12) { pos.y = 12; out.hit = true; }
    return out;
  }

  render() { this.renderer.render(this.scene, this.camera); }

  dispose() {
    window.removeEventListener('resize', this._onResize);
    this.renderer.dispose();
    this.renderer.domElement.remove();
  }
}

export { lerp };
