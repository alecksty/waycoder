// =====================================================================
// weapons.js — 武器与特效系统
//   · 机炮：瞬时射线（hitscan）判定，绘制曳光线
//   · 导弹：实体弹道 + 比例导引 + 干扰弹欺骗
//   · 炸弹：重力弹道 + 地面爆炸范围伤害
//   · 爆炸/火花/烟雾：程序化贴图 Sprite + 粒子池
// 敌我双方共用本系统，通过 owner 阵营区分命中目标
// =====================================================================
import * as THREE from 'three';
import { clamp, lerp, RNG, predictImpact } from './utils.js';

const _v1 = new THREE.Vector3();
const _v2 = new THREE.Vector3();
const _v3 = new THREE.Vector3();
const _rs = new THREE.Vector3();   // raySphere 专用，避免与其他临时向量冲突
const _fwd = new THREE.Vector3();  // 武器系统专用零时向量
const _right = new THREE.Vector3();
const _up = new THREE.Vector3();
const _axisZ = new THREE.Vector3(0, 0, -1);
// 高炮解算专用临时量：不能用 _v1/_v2/_v3，那几个会被 siteWorldPos 等复用而互相覆盖
const UD_AIM = new THREE.Vector3();
const UD_DIR = new THREE.Vector3();
const UD_SPREAD = new THREE.Vector3();
const UD_PREV = new THREE.Vector3();
const UD_TMP1 = new THREE.Vector3();
const UD_TMP2 = new THREE.Vector3();
const UD_TMP3 = new THREE.Vector3();
const UD_TMP4 = new THREE.Vector3();
// 高炮误差正交基的参考轴（只用 .set，绝不被写入/读回，避免临时向量互相污染）
const _flakRefX = new THREE.Vector3(1, 0, 0);
const _flakRefY = new THREE.Vector3(0, 1, 0);

export const GUN = {
  range: 2400,
  speed: 1050,
  damage: 7,
  spread: 0.0042,
  rate: 0.075,
  heat: 0.08,     // 每次开火升温（约 13 发过热）
};

const MISSILE = {
  speed: 480,
  accel: 340,
  maxSpeed: 900,
  life: 12,
  turnRate: 2.0,
  damage: 115,
  proximity: 26,
  splash: 70,
};

const BOMB = { gravity: 9.81 * 2.2, damage: 220, splash: 120 };

// 地面高炮（AA gun）：射速慢、弹道可见、低空威胁大，高空可安全掠过
const FLAK = {
  range: 2800,          // 开火距离
  ceiling: 2100,        // 射高上限（高于此高度不构成威胁）
  speed: 620,           // 炮弹速度
  spread: 0.02,         // 散布（弧度）
  damage: 6,            // 单发伤害
  cooldown: 0.34,       // 射击间隔
  tracersPerVolley: 2,  // 一个阵地一次齐射的弹数
  // 跟踪精度：解算出的提前量按「角误差」打偏，误差随距离与目标机动放大。
  // 没有这一项时高炮是「完美解算」——只要进了射界就必然命中，
  // 实测评飞越一个阵地 10 秒必被击落，对地突击波根本无法完成。
  baseError: 0.014,     // 基础角误差（弧度）：光学/雷达的固有测角测距误差
  rangeError: 0.030,    // 距离带来的额外误差（2800m 满射程时 +0.030 rad）
  gLoadError: 0.008,    // 目标机动（g 载荷）带来的额外角误差
};

/**
 * 高炮单发命中率估算（0..1）：按「角误差在目标截面上的落点分布」算，
 * 目标当作半径 12m 的球（机体 9m + 近炸余量）。
 * 只用于自测/难度评估，不参与实际判定（实际判定是弹丸与机体的线段求交）。
 */
export function flakHitProbability(dist, gLoad = 1, radius = 12) {
  const err = FLAK.baseError + FLAK.rangeError * clamp(dist / FLAK.range, 0, 1)
            + clamp(gLoad - 1, 0, 6) * FLAK.gLoadError;
  const miss = err * dist;                      // 角误差在目标处的横向偏差（米）
  if (miss <= 1e-6) return 1;
  // 期望值 E[1(miss≤r)] ≈ (r/miss)²，超过 1 时按 1 计
  return clamp((radius / miss) ** 2, 0, 1);
}

// 殉爆连锁：爆心附近 N 米内的航空弹药会被引燃（半径 = clamp(爆炸尺寸 × 4, 90, 240)）。
// 链条天然会收敛（每级半径固定、弹药数量有限），但「一环套一环」的等距弹链
// 在极端情况下能串很多级，所以还是给个深度上限兜住爆炸雪崩。
// 12 级足以覆盖 90m 间距上近 1km 的弹链；超过就停下，剩余的弹药自然留在地上等下次爆炸。
const CHAIN_MAX_DEPTH = 12;

// ---------------------------------------------------------------------
// 程序化贴图
// ---------------------------------------------------------------------
function radialTexture(size, stops) {
  const cv = document.createElement('canvas');
  cv.width = cv.height = size;
  const ctx = cv.getContext('2d');
  const g = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  for (const [p, c] of stops) g.addColorStop(p, c);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, size, size);
  const tex = new THREE.CanvasTexture(cv);
  tex.colorSpace = THREE.SRGBColorSpace;
  return tex;
}

export class WeaponSystem {
  constructor(scene, world, particles, audio) {
    this.scene = scene;
    this.world = world;
    this.particles = particles;
    this.audio = audio;
    this.rng = new RNG(9911);

    this.player = null;
    this.enemies = [];
    this.allies = [];       // 友军僚机（被敌方武器攻击 / 可发射导弹）
    this.onAlliedKill = null;  // (victim) => void  僚机击落敌机
    this.onAllyDown = null;    // (ally) => void    僚机被击落

    this.tracers = [];
    this.missiles = [];
    this.bombs = [];
    this.flares = [];
    this.explosions = [];
    // 地面高炮弹丸（独立于机炮曳光：存活 3 秒、可被地形遮挡）
    this.shells = [];
    // 弹药落地事件队列（主程序结算地面设施/停机坪损失用；有上限，不无限增长）
    this.groundImpacts = [];
    this.onGroundImpact = null;   // (pos, owner, radius) => void

    this.stats = { shots: 0, hits: 0, fired: 0, enemyShots: 0, missilesFired: 0, missileHits: 0, flakFired: 0 };
    this.shake = 0;
    this._chainDepth = 0;        // 连锁引爆深度（防无限递归）
    this.onKill = null;          // (victim, credit) => void
    this.onPlayerHit = null;     // (damage) => void
    this.onExplosion = null;     // (pos, credit, size) => void 用于地面目标判定
    this.onOverheat = null;      // () => void 机炮过热
    this.onFlakHit = null;       // (site, damage) => void 被高炮命中
    // 高炮伤害倍率（按难度调整，由主程序写入；1 = 普通难度）
    this.flakDamageScale = 1;

    // ---- 曳光弹线段缓冲（一次分配，循环复用）----
    this.maxTracers = 900;
    this.tracerGeo = new THREE.BufferGeometry();
    this.tracerPos = new Float32Array(this.maxTracers * 6);
    this.tracerCol = new Float32Array(this.maxTracers * 6);
    this.tracerGeo.setAttribute('position', new THREE.BufferAttribute(this.tracerPos, 3));
    this.tracerGeo.setAttribute('color', new THREE.BufferAttribute(this.tracerCol, 3));
    this.tracerGeo.setDrawRange(0, 0);
    this.tracerLines = new THREE.LineSegments(
      this.tracerGeo,
      new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, opacity: 0.95, depthWrite: false })
    );
    this.tracerLines.frustumCulled = false;
    scene.add(this.tracerLines);

    // ---- 特效贴图 ----
    this.texBoom = radialTexture(128, [
      [0, 'rgba(255,255,240,1)'], [0.25, 'rgba(255,210,110,0.95)'],
      [0.55, 'rgba(255,120,40,0.55)'], [1, 'rgba(120,30,0,0)'],
    ]);
    this.texSpark = radialTexture(64, [
      [0, 'rgba(255,255,220,1)'], [0.4, 'rgba(255,170,60,0.8)'], [1, 'rgba(255,90,0,0)'],
    ]);
    this.texSmoke = radialTexture(64, [
      [0, 'rgba(210,210,215,0.85)'], [0.6, 'rgba(140,140,150,0.35)'], [1, 'rgba(90,90,100,0)'],
    ]);
    this.texFlare = radialTexture(64, [
      [0, 'rgba(255,255,255,1)'], [0.35, 'rgba(255,240,180,0.9)'], [1, 'rgba(255,160,40,0)'],
    ]);

    // 共享 sprite 材质模板（每个实例 clone，便于单独控制透明度）
    this.matBoom = new THREE.SpriteMaterial({ map: this.texBoom, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });
    this.matSpark = new THREE.SpriteMaterial({ map: this.texSpark, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });
    this.matSmoke = new THREE.SpriteMaterial({ map: this.texSmoke, transparent: true, depthWrite: false });
    this.matFlare = new THREE.SpriteMaterial({ map: this.texFlare, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });
    this.matHitMark = new THREE.SpriteMaterial({ map: this.texSpark, color: 0xff4d4d, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });

    // 导弹与炸弹网格模板
    this.missileGeo = new THREE.CylinderGeometry(0.16, 0.16, 3.0, 8);
    this.missileGeo.rotateX(Math.PI / 2);
    this.missileMatPlayer = new THREE.MeshStandardMaterial({ color: 0xdfe8f0, emissive: 0x2ce0ff, emissiveIntensity: 0.4, roughness: 0.4, metalness: 0.5 });
    this.missileMatEnemy = new THREE.MeshStandardMaterial({ color: 0x2a2a2a, emissive: 0xff5a2a, emissiveIntensity: 0.5, roughness: 0.5, metalness: 0.4 });
    this.missileMatAlly = new THREE.MeshStandardMaterial({ color: 0xcfe0d6, emissive: 0x4dffa1, emissiveIntensity: 0.45, roughness: 0.4, metalness: 0.5 });
    this.bombGeo = new THREE.CapsuleGeometry(0.6, 2.6, 4, 8);
    this.bombMat = new THREE.MeshStandardMaterial({ color: 0x40484f, roughness: 0.6, metalness: 0.5 });

    // ---- 粒子池渲染（Points），与 ParticlePool 容量一一对应 ----
    this._initParticleRenderer();
  }

  /** 把 ParticlePool 的数据同步到 GPU 点精灵缓冲 */
  _initParticleRenderer() {
    const cap = this.particles.capacity;
    this._pGeo = new THREE.BufferGeometry();
    this._pPos = new Float32Array(cap * 3);
    this._pCol = new Float32Array(cap * 3);
    this._pSize = new Float32Array(cap);
    this._pGeo.setAttribute('position', new THREE.BufferAttribute(this._pPos, 3));
    this._pGeo.setAttribute('color', new THREE.BufferAttribute(this._pCol, 3));
    this._pGeo.setAttribute('psize', new THREE.BufferAttribute(this._pSize, 1));
    this._pGeo.setDrawRange(0, 0);

    const mat = new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
      vertexShader: /* glsl */`
        attribute float psize;
        varying vec3 vColor;
        varying float vAlpha;
        void main() {
          vColor = color;
          vAlpha = 1.0;
          vec4 mv = modelViewMatrix * vec4(position, 1.0);
          gl_PointSize = psize * 90.0 / max(1.0, -mv.z);
          gl_Position = projectionMatrix * mv;
        }
      `,
      fragmentShader: /* glsl */`
        varying vec3 vColor;
        varying float vAlpha;
        void main() {
          float d = length(gl_PointCoord - vec2(0.5));
          if (d > 0.5) discard;
          float a = smoothstep(0.5, 0.06, d);
          gl_FragColor = vec4(vColor, a * vAlpha);
        }
      `,
      vertexColors: true,
    });
    this.particlePoints = new THREE.Points(this._pGeo, mat);
    this.particlePoints.frustumCulled = false;
    this.scene.add(this.particlePoints);
  }

  /** 每帧把粒子池内容写入点精灵缓冲 */
  _renderParticles() {
    const pool = this.particles;
    const pos = this._pPos, col = this._pCol, siz = this._pSize;
    let n = 0;
    for (let i = 0; i < pool.capacity; i++) {
      if (!pool.alive[i]) continue;
      const k = i * 3, j = n * 3;
      pos[j] = pool.pos[k]; pos[j + 1] = pool.pos[k + 1]; pos[j + 2] = pool.pos[k + 2];
      const t = clamp(pool.life[i] / pool.maxLife[i], 0, 1);
      if (pool.kind[i] === 0) {
        // 火花：白→橙→暗红
        col[j] = 1.0;
        col[j + 1] = 0.55 + t * 0.4;
        col[j + 2] = 0.15 + t * 0.65;
      } else if (pool.kind[i] === 2) {
        // 碎片：白热→暗金属
        const g = 0.35 + t * 0.6;
        col[j] = g * 0.95;
        col[j + 1] = g * 0.9;
        col[j + 2] = g * 0.75;
      } else {
        // 烟雾/爆炸：亮橙→灰
        const g = 0.28 + t * 0.65;
        col[j] = g + 0.25 * t;
        col[j + 1] = g * 0.9;
        col[j + 2] = g * 0.85;
      }
      siz[n] = pool.size[i] * (0.35 + t * 0.65);
      n++;
      if (n >= pool.capacity) break;
    }
    this._pGeo.attributes.position.needsUpdate = true;
    this._pGeo.attributes.color.needsUpdate = true;
    this._pGeo.attributes.psize.needsUpdate = true;
    this._pGeo.setDrawRange(0, n);
  }

  attach(player, enemies, allies = []) {
    this.player = player;
    this.enemies = enemies;
    this.allies = allies;
  }

  // ===================================================================
  // 玩家武器
  // ===================================================================
  firePlayerGun() {
    const p = this.player;
    if (!p || !p.alive || p.weapons.gunCooldown > 0 || p.gunOverheat) return false;
    p.weapons.gunCooldown = GUN.rate;
    p.weapons.gunHeat = clamp(p.weapons.gunHeat + GUN.heat, 0, 1.5);
    if (p.weapons.gunHeat > 1.0 && !p.gunOverheat) {
      p.gunOverheat = true;
      this.audio && this.audio.overheatWarn();
      if (this.onOverheat) this.onOverheat();
    }

    const fwd = p.getForward(_fwd);
    const right = p.getRight(_right);
    const up = p.getUp(_up);
    // 两门机炮交替，从左右翼根射出
    // 统计口径：一次开火 = 两门炮共 2 发出膛（shots / fired 同源），命中数按实际命中弹数累计
    this._fireSalvo = { hits: 0 };
    this._muzzle((origin, dir, side) => this._gunRay(origin, dir, side), p, fwd, right, up);
    this.stats.shots += 2;
    this.stats.fired += 2;
    this.stats.hits += this._fireSalvo.hits;
    this.audio && this.audio.gun();
    return true;
  }

  _muzzle(fn, p, fwd, right, up) {
    for (const side of [-1, 1]) {
      const origin = p.position.clone()
        .addScaledVector(right, 2.6 * side)
        .addScaledVector(up, -0.2)
        .addScaledVector(fwd, -4.0);
      const dir = fwd.clone()
        .addScaledVector(right, this.rng.range(-GUN.spread, GUN.spread) + 0.004 * side)
        .addScaledVector(up, this.rng.range(-GUN.spread, GUN.spread))
        .normalize();
      fn(origin, dir, side);
    }
  }

  _gunRay(origin, dir, side) {
    // 射线与所有敌机做球体求交，取最近命中
    let bestT = Infinity, bestVictim = null;
    for (const e of this.enemies) {
      if (!e.alive || e.spawnGrace > 0.25) continue;
      const t = raySphere(origin, dir, e.position, e.radius * 1.9);
      if (t >= 0 && t < bestT && t < GUN.range) { bestT = t; bestVictim = e; }
    }
    // 地形遮挡：射线与地形/海面相交则子弹被挡住
    const groundT = this._rayGround(origin, dir, Math.min(bestT === Infinity ? GUN.range : bestT, GUN.range));
    if (groundT >= 0 && groundT < bestT) {
      bestT = groundT;
      bestVictim = null;
      this._impactEffect(origin.clone().addScaledVector(dir, groundT), 0.9);
    }

    const endT = bestVictim ? bestT : Math.min(bestT, GUN.range);
    const end = origin.clone().addScaledVector(dir, endT);
    this._addTracer(origin, end, 'player');

    if (bestVictim) {
      if (this._fireSalvo) this._fireSalvo.hits++;
      this._sparkAt(end, 0xffd070);
      this._hitMarker(end);
      this.audio && this.audio.ping();
      const killed = bestVictim.damage(GUN.damage, this.player);
      if (killed) this._kill(bestVictim, 'player');
    }
  }

  firePlayerMissile(target) {
    const p = this.player;
    if (!p || !p.alive || p.weapons.missiles <= 0 || p.weapons.missileCooldown > 0) return false;
    p.weapons.missiles--;
    p.weapons.missileCooldown = 0.65;
    this._spawnMissile(p, target, 'player');
    this.stats.missilesFired++;
    this.audio && this.audio.missile();
    return true;
  }

  dropPlayerBomb() {
    const p = this.player;
    if (!p || !p.alive || p.weapons.bombs <= 0 || p.weapons.bombCooldown > 0) return false;
    p.weapons.bombs--;
    p.weapons.bombCooldown = 0.8;
    const mesh = new THREE.Mesh(this.bombGeo, this.bombMat);
    const pos = p.position.clone().addScaledVector(p.getUp(_v1), -1.5);
    mesh.position.copy(pos);
    this.scene.add(mesh);
    this.bombs.push({
      mesh, pos, vel: p.velocity.clone().addScaledVector(p.getForward(_v2), 40),
      owner: 'player', life: 30, spin: new THREE.Vector3(this.rng.range(-3, 3), this.rng.range(-3, 3), this.rng.range(-3, 3)),
    });
    this.audio && this.audio.bombRelease();
    return true;
  }

  releasePlayerFlare() {
    const p = this.player;
    if (!p || !p.alive || p.weapons.flares <= 0 || p.weapons.flareCooldown > 0) return false;
    p.weapons.flares--;
    p.weapons.flareCooldown = 1.1;
    for (let i = 0; i < 6; i++) {
      const pos = p.position.clone().addScaledVector(p.getUp(_v1), -2.5);
      const sprite = new THREE.Sprite(this.matFlare.clone());
      sprite.position.copy(pos);
      sprite.scale.set(6, 6, 1);
      this.scene.add(sprite);
      this.flares.push({
        sprite, pos: pos.clone(),
        vel: p.velocity.clone().addScaledVector(p.getForward(_v2), -60)
          .add(new THREE.Vector3(this.rng.range(-22, 22), this.rng.range(-26, 6), this.rng.range(-22, 22))),
        life: 4.2, maxLife: 4.2, owner: 'player',
      });
    }
    this.audio && this.audio.flare();
    return true;
  }

  // ===================================================================
  // 敌方武器
  // ===================================================================
  fireEnemyGun(enemy, burst = 5) {
    const p = this.player;
    // 注意：这里不能用 `!p` 作为提前返回条件——纯 AI 对 AI 的场景（如自测里敌机打僚机）
    // 玩家为 null 时仍必须能命中友军
    if (!enemy.alive) return;
    const fwd = enemy.getForward(_fwd);
    for (let i = 0; i < burst; i++) {
      const spread = 0.011 + (1 - enemy.skill) * 0.016;
      const origin = enemy.position.clone().addScaledVector(fwd, enemy.radius + 3);
      const dir = fwd.clone()
        .add(new THREE.Vector3(this.rng.range(-spread, spread), this.rng.range(-spread, spread), this.rng.range(-spread, spread)))
        .normalize();
      this.stats.enemyShots++;

      let bestVictim = null;
      let bestT = -1;
      if (p && p.alive) {
        const t = raySphere(origin, dir, p.position, p.radius * 1.7);
        if (t >= 0) { bestT = t; bestVictim = p; }
      }
      for (const ally of this.allies) {
        if (!ally.alive || ally.spawnGrace > 0.25) continue;
        const t = raySphere(origin, dir, ally.position, ally.radius * 1.7);
        if (t >= 0 && (bestT < 0 || t < bestT)) { bestT = t; bestVictim = ally; }
      }
      let hitT = bestT;
      const groundT = this._rayGround(origin, dir, GUN.range);
      if (groundT >= 0 && (hitT < 0 || groundT < hitT)) { hitT = groundT; bestVictim = null; }

      const endT = hitT >= 0 ? hitT : GUN.range * 0.6;
      const end = origin.clone().addScaledVector(dir, endT);
      this._addTracer(origin, end, 'enemy');

      if (bestVictim && bestT >= 0) {
        this._sparkAt(end, 0xff8a4a);
        const dmg = 4 + enemy.skill * 5;
        if (bestVictim === p) this._damagePlayer(dmg, enemy);
        else if (bestVictim.damage(dmg, enemy)) this._killAlly(bestVictim);
      }
    }
    this.audio && this.audio.enemyGun();
  }

  fireEnemyMissile(enemy, target) {
    if (enemy.missiles <= 0 || !target) return;
    this._spawnMissile(enemy, target, 'enemy');
    this.stats.enemyShots++;
    this.audio && this.audio.enemyMissile();
  }

  // ===================================================================
  // 僚机武器（玩家方 AI）：与敌方共用弹道，阵营标记为 friendly
  // ===================================================================
  fireAllyGun(ally, burst = 4) {
    if (!ally || !ally.alive || !this.enemies) return;
    const fwd = ally.getForward(_fwd);
    const right = ally.getRight(_right);
    const up = ally.getUp(_up);
    for (let i = 0; i < burst; i++) {
      const side = i % 2 ? 1 : -1;
      const origin = ally.position.clone()
        .addScaledVector(right, 2.4 * side)
        .addScaledVector(up, -0.2)
        .addScaledVector(fwd, -3.6);
      const dir = fwd.clone()
        .addScaledVector(right, this.rng.range(-GUN.spread, GUN.spread))
        .addScaledVector(up, this.rng.range(-GUN.spread, GUN.spread))
        .normalize();

      let bestT = Infinity, bestVictim = null;
      for (const e of this.enemies) {
        if (!e.alive || e.spawnGrace > 0.25) continue;
        const t = raySphere(origin, dir, e.position, e.radius * 1.9);
        if (t >= 0 && t < bestT && t < GUN.range) { bestT = t; bestVictim = e; }
      }
      const groundT = this._rayGround(origin, dir, Math.min(bestT === Infinity ? GUN.range : bestT, GUN.range));
      if (groundT >= 0 && groundT < bestT) { bestT = groundT; bestVictim = null; }

      const end = origin.clone().addScaledVector(dir, bestVictim ? bestT : Math.min(bestT, GUN.range));
      this._addTracer(origin, end, 'ally');
      if (bestVictim) {
        this._sparkAt(end, 0xffd070);
        if (bestVictim.damage(GUN.damage * 0.9, ally)) this._alliedKill(bestVictim, ally);
      }
    }
    this.audio && this.audio.enemyGun();
  }

  fireAllyMissile(ally, target) {
    if (!ally || !ally.alive || !target || !target.alive) return;
    this._spawnMissile(ally, target, 'friendly');
    this.stats.missilesFired++;
    this.audio && this.audio.missile();
  }

  _alliedKill(victim, ally) {
    victim._deathHandled = true;
    this.explode(victim.position, victim.radius * 9, victim.variant === 'bomber' ? 3.4 : 2.2);
    if (this.onAlliedKill) this.onAlliedKill(victim, ally);
  }

  _killAlly(ally) {
    if (ally._deathHandled) return;
    ally._deathHandled = true;
    this.explode(ally.position, ally.radius * 8, 2.0);
    if (this.onAllyDown) this.onAllyDown(ally);
  }

  dropEnemyBomb(enemy) {
    const pos = enemy.position.clone();
    pos.y -= 6;
    const mesh = new THREE.Mesh(this.bombGeo, this.bombMat);
    mesh.scale.setScalar(1.7);
    mesh.position.copy(pos);
    this.scene.add(mesh);
    this.bombs.push({
      mesh, pos, vel: enemy.velocity.clone().add(new THREE.Vector3(0, -20, 0)),
      owner: 'enemy', life: 30, spin: new THREE.Vector3(1, 0.5, 0.2),
      damage: 160, splash: 100,
    });
  }

  releaseEnemyFlare(enemy) {
    const m = this._nearestMissileThreat(enemy);
    if (!m || this.rng.next() > 0.35 * enemy.skill) return;
    for (let i = 0; i < 4; i++) {
      const pos = enemy.position.clone();
      const sprite = new THREE.Sprite(this.matFlare.clone());
      sprite.position.copy(pos);
      sprite.scale.set(5, 5, 1);
      this.scene.add(sprite);
      this.flares.push({
        sprite, pos: pos.clone(),
        vel: enemy.velocity.clone().multiplyScalar(0.6)
          .add(new THREE.Vector3(this.rng.range(-30, 30), this.rng.range(-30, 10), this.rng.range(-30, 30))),
        life: 3.4, maxLife: 3.4, owner: 'enemy',
      });
    }
  }

  // ===================================================================
  // 地面高炮：低空威胁，压制玩家贴地突防
  //   ① 目标选取：低于射高的空中目标（优先玩家，其次僚机）
  //   ② 提前量：按弹速做二次方程解（中段重力下坠计入）
  //   ③ 弹丸实体存在 3 秒，可被地形挡住，命中判定用线段最近的球体求交
  // ===================================================================
  /** @param {THREE.Camera} [cam] 听音位置（取不到时回落到世界相机），用于判断是否播放炮声 */
  updateFlak(dt, cam) {
    const sites = this.world.flakSites;
    if (!sites || !sites.length) return;
    const listener = cam || (this.world && this.world.camera) || null;
    for (const site of sites) {
      this._updateFlakSite(site, dt, listener);
    }
  }

  /** 单个高炮阵地：搜索目标 → 解算提前量 → 齐射 */
  _updateFlakSite(site, dt, listener = null) {
    const ud = site.userData;
    if (ud.alive === false) return;
    // 注：雷达天线旋转由 world.update 统一驱动（对空/对地两种模式都要转）

    const origin = this.world.siteWorldPos(site, _v1);
    origin.y = this.world.heightAt(origin.x, origin.z) + 8;

    const target = this._flakTarget(origin);
    if (!target) return;

    const v = target.velocity;
    const toT = _v2.copy(target.position).sub(origin);
    const dist = toT.length();
    const flight = dist / FLAK.speed;
    // 二次方程求精确提前量：|p_t + v_t·t − p_gun| = c·t
    const ax = v.x, ay = v.y, az = v.z;
    const bx = toT.x, by = toT.y, bz = toT.z;
    const c = FLAK.speed;
    const A = ax * ax + ay * ay + az * az - c * c;
    const B = 2 * (ax * bx + ay * by + az * bz);
    const C = bx * bx + by * by + bz * bz;
    let t;
    if (Math.abs(A) < 1e-6) {
      t = Math.abs(B) > 1e-6 ? -C / B : flight;
    } else {
      const disc = B * B - 4 * A * C;
      if (disc < 0) { ud.cool = 0.25; return; }
      const sq = Math.sqrt(disc);
      const t1 = (-B + sq) / (2 * A), t2 = (-B - sq) / (2 * A);
      const cand = [t1, t2].filter((x) => x > 0.05 && x < 8);
      t = cand.length ? Math.min(...cand) : flight;
    }
    UD_AIM.copy(target.position).addScaledVector(v, t);
    // 重力下坠补偿（炮弹与炸弹同用一套重力常数）
    UD_AIM.y += 0.5 * BOMB.gravity * t * t;
    const dir = UD_DIR.copy(UD_AIM).sub(origin).normalize();

    // 跟踪误差：真实高炮靠光学/雷达测距测角，有固定误差；目标机动时还要重新解算。
    // 误差在「垂直于弹道的平面」上施加，这才是真正的角偏差——直接给方向加随机向量
    // 在近距离时会变成几乎打不中，反而失真。
    const errScale = (FLAK.baseError + FLAK.rangeError * (dist / FLAK.range)) * this.rng.range(0.5, 1.5);
    const gErr = clamp((target.gLoad || 1) - 1, 0, 6) * FLAK.gLoadError;
    const totalErr = errScale + gErr;
    if (totalErr > 0) {
      // 构造正交基：dir 已是单位向量，取一个不平行的参考轴叉乘即可
      const ref = Math.abs(dir.y) > 0.9 ? _flakRefX.set(1, 0, 0) : _flakRefY.set(0, 1, 0);
      UD_TMP3.copy(dir).cross(ref).normalize();
      UD_TMP4.copy(dir).cross(UD_TMP3).normalize();
      const a = this.rng.range(0, Math.PI * 2);
      dir.addScaledVector(UD_TMP3, Math.cos(a) * totalErr)
         .addScaledVector(UD_TMP4, Math.sin(a) * totalErr)
         .normalize();
    }

    ud.cool = (ud.cool || 0) - dt;
    if (ud.cool > 0) return;
    ud.cool = FLAK.cooldown * this.rng.range(0.85, 1.25);

    for (let i = 0; i < FLAK.tracersPerVolley; i++) {
      const d = UD_SPREAD.copy(dir)
        .add(UD_TMP1.set(
          this.rng.range(-FLAK.spread, FLAK.spread),
          this.rng.range(-FLAK.spread, FLAK.spread),
          this.rng.range(-FLAK.spread, FLAK.spread)
        ))
        .normalize();
      this.shells.push({
        pos: origin.clone(),
        vel: d.clone().multiplyScalar(FLAK.speed),
        life: 3.2, owner: 'flak', damage: FLAK.damage, site,
      });
    }
    this.stats.flakFired += FLAK.tracersPerVolley;
    // 炮声只在「炮位靠近听者」时播放；自测里没有相机也不能崩
    const near = listener ? origin.distanceTo(listener.position) < 1400 : false;
    if (near && this.audio) this.audio.enemyGun();
  }

  /** 挑一个在射高内、距离够近的空中目标 */
  _flakTarget(origin) {
    let best = null, bestD = FLAK.range;
    const consider = (t) => {
      if (!t || !t.alive) return;
      const d = t.position.distanceTo(origin);
      if (d > bestD) return;
      if (t.position.y > FLAK.ceiling || t.position.y < 60) return;
      bestD = d; best = t;
    };
    consider(this.player);
    for (const a of this.allies) consider(a);
    return best;
  }

  _updateShells(dt) {
    // 高炮解算专用临时量：不能复用 _v1/_v2/_v3 —— siteWorldPos 内部会覆盖它们
    const list = this.shells;
    if (!list.length) return;
    const prev = UD_PREV;
    for (let i = list.length - 1; i >= 0; i--) {
      const s = list[i];
      // 身份校验：命中会引发爆炸，爆炸又可能连锁移除本数组里的其它弹丸
      if (!s || list[i] !== s) continue;
      s.life -= dt;
      if (s.life <= 0) { list.splice(i, 1); continue; }
      prev.copy(s.pos);
      s.vel.y -= BOMB.gravity * dt;
      s.pos.addScaledVector(s.vel, dt);

      // 弹道可见（每发每帧一条短线，做出「弹幕」观感）
      this._addTracer(UD_TMP1.copy(prev), UD_TMP2.copy(s.pos), 'flak');

      // 命中判定：取线段上最近的目标
      let hit = null, hitT = Infinity;
      const seg = UD_TMP1.copy(s.pos).sub(prev);
      const segLen = seg.length();
      if (segLen > 1e-4) {
        const sd = seg.clone().multiplyScalar(1 / segLen);
        const test = (t) => {
          if (!t || !t.alive) return;
          const r = t.radius + 3;
          const to = UD_TMP2.copy(t.position).sub(prev);
          const proj = to.dot(sd);
          if (proj < 0 || proj > segLen) return;
          const closest = UD_TMP3.copy(prev).addScaledVector(sd, proj);
          if (closest.distanceTo(t.position) > r) return;
          if (proj < hitT) { hitT = proj; hit = t; }
        };
        test(this.player);
        for (const a of this.allies) test(a);
      }
      if (hit) {
        this._sparkAt(s.pos, 0xffb060);
        // 通报「是哪一处阵地开的火、附近还有几处活跃阵地」，供 HUD 提示玩家脱离子弹幕
        if (this.onFlakHit && s.site) {
          const wp = UD_TMP1.copy(this.world.siteWorldPos(s.site, UD_TMP4));
          let near = 0;
          for (const o of this.world.flakSites) {
            if (o.userData.alive === false) continue;
            if (this.world.siteWorldPos(o, UD_TMP3).distanceTo(wp) < 2600) near++;
          }
          this.onFlakHit(s.site, s.damage, near);
        }
        if (hit === this.player) this._damagePlayer(s.damage * this.flakDamageScale, s.site);
        else if (hit.damage(s.damage * this.flakDamageScale, null)) this._killAlly(hit);
        list.splice(i, 1);
        continue;
      }
      // 入地：爆出一小团黑烟（表示高炮弹着）
      if (s.pos.y < this.world.heightAt(s.pos.x, s.pos.z) || s.pos.y <= 0) {
        this._impactEffect(s.pos, 0.7);
        list.splice(i, 1);
        continue;
      }
      if (s.pos.y > 4600) { list.splice(i, 1); }
    }
  }

  _nearestMissileThreat(enemy) {
    let best = null, bestD = Infinity;
    for (const m of this.missiles) {
      if (m.owner === 'enemy' || !m.target) continue;
      if (m.target !== enemy) continue;
      const d = m.pos.distanceTo(enemy.position);
      if (d < bestD) { bestD = d; best = m; }
    }
    return bestD < 900 ? best : null;
  }

  // ===================================================================
  // 内部：实体生成与命中
  // ===================================================================
  _spawnMissile(owner, target, faction) {
    const from = owner.position.clone().addScaledVector(owner.getUp(_v1), -1.2);
    const mat = faction === 'enemy' ? this.missileMatEnemy : faction === 'friendly' ? this.missileMatAlly : this.missileMatPlayer;
    const mesh = new THREE.Mesh(this.missileGeo, mat);
    mesh.position.copy(from);
    this.scene.add(mesh);
    const dir = owner.getForward(_v2).clone();
    this.missiles.push({
      mesh, pos: from,
      vel: owner.velocity.clone().addScaledVector(dir, 120),
      dir: dir.clone(),
      owner: faction,
      target,
      life: MISSILE.life,
      launchedFrom: owner,
      smokeT: 0,
    });
  }

  _damagePlayer(amount, source) {
    const p = this.player;
    if (!p || !p.alive) return;
    p.damage(amount, source);
    this.shake = Math.min(1.4, this.shake + amount * 0.012);
    if (this.onPlayerHit) this.onPlayerHit(amount);
    if (!p.alive) this._kill(p, 'enemy');
  }

  _kill(victim, credit) {
    if (victim._deathHandled) return;
    victim._deathHandled = true;
    this.explode(victim.position, victim.radius * 9, victim.variant === 'bomber' ? 3.4 : 2.2);
    if (this.onKill) this.onKill(victim, credit);
  }

  // ===================================================================
  // 特效
  // ===================================================================
  _addTracer(from, to, faction) {
    this.tracers.push({
      head: to.clone(), tail: from.clone(),
      vel: to.clone().sub(from).normalize().multiplyScalar(GUN.speed),
      life: 0.075, faction,
    });
  }

  _sparkAt(pos, color) {
    const p = this.particles;
    for (let i = 0; i < 5; i++) {
      p.spawn(pos.x, pos.y, pos.z,
        this.rng.range(-14, 14), this.rng.range(-14, 14), this.rng.range(-14, 14),
        this.rng.range(0.18, 0.5), this.rng.range(1.2, 2.6), 0, 2.6, -4);
    }
  }

  /** 命中反馈：红色命中标记（随机抖动模拟碎裂） */
  _hitMarker(pos) {
    if (!this.matHitMark) return;
    const s = new THREE.Sprite(this.matHitMark.clone());
    s.position.copy(pos);
    s.scale.setScalar(6.5);
    this.scene.add(s);
    this.explosions.push({ sprite: s, life: 0.16, maxLife: 0.16, grow: 22, type: 'spark', drift: null });
  }

  _impactEffect(pos, scale = 1) {
    const p = this.particles;
    for (let i = 0; i < 8; i++) {
      p.spawn(pos.x, pos.y, pos.z,
        this.rng.range(-22, 22) * scale, this.rng.range(4, 30) * scale, this.rng.range(-22, 22) * scale,
        this.rng.range(0.4, 0.9), this.rng.range(2, 5) * scale, 1, 1.8, -12);
    }
    const sp = new THREE.Sprite(this.matSmoke.clone());
    sp.position.copy(pos);
    sp.material.opacity = 0.5 * scale;
    sp.scale.setScalar(10 * scale);
    this.scene.add(sp);
    this.explosions.push({ sprite: sp, life: 1.1, maxLife: 1.1, grow: 22 * scale, type: 'smoke' });
  }

  /** 大型爆炸（飞机/炸弹） */
  explode(pos, size = 30, power = 2, credit = null, splashDamage = 0, splashRadius = 0) {
    const flash = new THREE.Sprite(this.matBoom.clone());
    flash.position.copy(pos);
    flash.scale.setScalar(size);
    this.scene.add(flash);
    this.explosions.push({ sprite: flash, life: 0.85, maxLife: 0.85, grow: size * 2.2, type: 'boom' });

    const ring = new THREE.Sprite(this.matSpark.clone());
    ring.position.copy(pos);
    ring.scale.setScalar(size * 0.7);
    this.scene.add(ring);
    this.explosions.push({ sprite: ring, life: 0.45, maxLife: 0.45, grow: size * 4.5, type: 'spark' });

    for (let i = 0; i < Math.floor(14 * power); i++) {
      const dirX = this.rng.range(-1, 1), dirY = this.rng.range(-1, 1), dirZ = this.rng.range(-1, 1);
      const sp = 30 * power;
      this.particles.spawn(pos.x, pos.y, pos.z,
        dirX * sp, dirY * sp + 10, dirZ * sp,
        this.rng.range(0.5, 1.6), this.rng.range(2, 6) * power, 0, 1.4, -10);
    }
    // 碎片：小尺寸、带重力、颜色偏白（kind=2）
    for (let i = 0; i < Math.floor(10 * power); i++) {
      this.particles.spawn(pos.x, pos.y, pos.z,
        this.rng.range(-46, 46) * power, this.rng.range(4, 52) * power, this.rng.range(-46, 46) * power,
        this.rng.range(0.8, 2.2), this.rng.range(0.9, 2.2), 2, 0.35, -26);
    }
    // 黑烟
    for (let i = 0; i < Math.floor(8 * power); i++) {
      const s = new THREE.Sprite(this.matSmoke.clone());
      s.position.copy(pos).add(new THREE.Vector3(this.rng.range(-6, 6), this.rng.range(-4, 8), this.rng.range(-6, 6)));
      s.scale.setScalar(size * this.rng.range(0.8, 1.6));
      s.material.opacity = 0.55;
      this.scene.add(s);
      this.explosions.push({
        sprite: s, life: this.rng.range(1.6, 3.2), maxLife: 3.2, grow: size * 0.9, type: 'smoke',
        drift: new THREE.Vector3(this.rng.range(-4, 4), this.rng.range(3, 9), this.rng.range(-4, 4)),
      });
    }

    const distToCam = this.player ? pos.distanceTo(this.player.position) : 500;
    this.shake = Math.min(2.2, this.shake + clamp(size / Math.max(40, distToCam), 0, 0.9));
    this.audio && this.audio.explosion(distToCam);
    if (this.onExplosion) this.onExplosion(pos, credit, size);

    if (splashDamage > 0) {
      for (const e of this.enemies) {
        if (!e.alive) continue;
        const d = e.position.distanceTo(pos);
        if (d < splashRadius) {
          const dmg = splashDamage * (1 - d / splashRadius);
          if (e.damage(dmg, null)) this._kill(e, credit || 'player');
        }
      }
      // 敌方爆炸同样会波及玩家
      if (credit !== 'player' && this.player && this.player.alive) {
        const d = this.player.position.distanceTo(pos);
        if (d < splashRadius) this._damagePlayer(splashDamage * (1 - d / splashRadius), null);
      }
      // 友军僚机也会被敌方爆炸波及（否则僚机可以贴脸蹭爆炸）
      if (credit !== 'player' && credit !== 'friendly') {
        for (const a of this.allies) {
          if (!a.alive) continue;
          const d = a.position.distanceTo(pos);
          if (d < splashRadius && a.damage(splashDamage * (1 - d / splashRadius), null)) this._killAlly(a);
        }
      }
    }
    // 连锁引爆：航空炸弹/导弹在爆心附近会被殉爆
    // 深度计数挂在实例上（_chainDetonate 里自增），保证多层连锁时不会回到 0 而无限递归
    this._chainDetonate(pos, size, (this._chainDepth || 0) + 1);
  }

  /**
   * 链式引爆：爆心附近的在飞炸弹、导弹会被引燃（航空弹药殉爆）。
   * 只对「实体弹药」生效，曳光/粒子不做连锁。
   *
   * 三条防炸线，缺一条就会出问题：
   *   ① 深度上限 —— 没有它，等距弹链能一级级串到栈溢出；
   *   ② 先快照后结算 —— 嵌套爆炸会改动数组，边遍历边改会漏判/误判；
   *   ③ 先摘除再引爆 —— 否则下一轮扫描会把「自己」当成殉爆目标，连环自引爆。
   */
  _chainDetonate(pos, size = 30, depth = 1) {
    if (depth > CHAIN_MAX_DEPTH) return;    // 连锁层数上限，防止雪崩式爆炸
    const savedDepth = this._chainDepth;
    this._chainDepth = depth;
    try {
      const radius = clamp(size * 4, 90, 240);
      // 先快照受害者：嵌套爆炸会改动数组，按对象引用回查索引才安全
      const victims = [];
      for (const b of this.bombs) if (b.pos.distanceTo(pos) <= radius) victims.push({ bomb: true, obj: b });
      for (const m of this.missiles) if (m.pos.distanceTo(pos) <= radius) victims.push({ bomb: false, obj: m });

      for (const v of victims) {
        const list = v.bomb ? this.bombs : this.missiles;
        const idx = list.indexOf(v.obj);
        if (idx < 0) continue;             // 已被上一轮嵌套爆炸引爆
        const p = v.obj.pos.clone();
        const owner = v.obj.owner || null;
        if (v.bomb) {
          this.scene.remove(v.obj.mesh);
          list.splice(idx, 1);
          this.explode(p, 50, 3.0, owner, v.obj.damage || BOMB.damage, v.obj.splash || BOMB.splash);
        } else {
          this._removeMissile(idx, v.obj);
          this.explode(p, 30, 2.2, owner, MISSILE.damage, MISSILE.splash);
        }
      }
    } finally {
      this._chainDepth = savedDepth;
    }
  }

  /** 射线与地形求交（步进采样，代价可控） */
  _rayGround(origin, dir, maxDist) {
    const step = 22;
    let t = 0;
    let prevAbove = origin.y - this.world.heightAt(origin.x, origin.z) > 0;
    while (t < maxDist) {
      t = Math.min(t + step, maxDist);
      const x = origin.x + dir.x * t, y = origin.y + dir.y * t, z = origin.z + dir.z * t;
      if (y < 0 && dir.y < 0) return t;
      const above = y - this.world.heightAt(x, z) > 0;
      if (prevAbove && !above) return t;
      prevAbove = above;
      if (t >= maxDist) break;
    }
    return -1;
  }

  // ===================================================================
  // 每帧更新
  // ===================================================================
  update(dt, ctx) {
    this._updateTracers(dt);
    this._updateMissiles(dt, ctx);
    this._updateBombs(dt, ctx);
    // 高炮「听音位置」优先用主程序传进来的相机（座舱/电影视角下位置差别很大）
    this.updateFlak(dt, ctx && ctx.camera);
    this._updateShells(dt);
    this._updateFlares(dt);
    this._updateExplosions(dt);
    this._renderParticles();
    this.shake = Math.max(0, this.shake - dt * 2.4);
  }

  _updateTracers(dt) {
    const list = this.tracers;
    for (let i = list.length - 1; i >= 0; i--) {
      const t = list[i];
      t.life -= dt;
      if (t.life <= 0) { list.splice(i, 1); continue; }
      t.tail.copy(t.head);
      t.head.addScaledVector(t.vel, dt);
    }
    // 写入缓冲
    const n = Math.min(list.length, this.maxTracers);
    const pos = this.tracerPos, col = this.tracerCol;
    for (let i = 0; i < n; i++) {
      const t = list[i];
      const k = i * 6;
      pos[k] = t.tail.x; pos[k + 1] = t.tail.y; pos[k + 2] = t.tail.z;
      pos[k + 3] = t.head.x; pos[k + 4] = t.head.y; pos[k + 5] = t.head.z;
      if (t.faction === 'player') { col[k] = 0.7; col[k + 1] = 1.0; col[k + 2] = 1.0; col[k + 3] = 1; col[k + 4] = 1; col[k + 5] = 0.85; }
      else if (t.faction === 'ally') { col[k] = 0.5; col[k + 1] = 1.0; col[k + 2] = 0.6; col[k + 3] = 0.9; col[k + 4] = 1; col[k + 5] = 0.75; }
      else if (t.faction === 'flak') { col[k] = 1.0; col[k + 1] = 0.95; col[k + 2] = 0.55; col[k + 3] = 1; col[k + 4] = 0.5; col[k + 5] = 0.15; }
      else { col[k] = 1.0; col[k + 1] = 0.55; col[k + 2] = 0.25; col[k + 3] = 1; col[k + 4] = 0.9; col[k + 5] = 0.4; }
    }
    this.tracerGeo.attributes.position.needsUpdate = true;
    this.tracerGeo.attributes.color.needsUpdate = true;
    this.tracerGeo.setDrawRange(0, n * 2);
  }

  _updateMissiles(dt, ctx) {
    const list = this.missiles;
    for (let i = list.length - 1; i >= 0; i--) {
      const m = list[i];
      // 身份校验：本循环内的 explode() 会连锁引爆并移除其它导弹（splice 后下标会位移）
      if (!m || list[i] !== m) continue;
      m.life -= dt;

      // 干扰弹欺骗：目标附近有敌对干扰弹时有一定概率丢失目标
      if (m.target && m.target.alive !== false) {
        for (const f of this.flares) {
          if (f.owner === m.owner) continue;
          const d = f.pos.distanceTo(m.pos);
          if (d < 420) {
            const toF = f.pos.clone().sub(m.pos).normalize();
            const fwdM = m.vel.clone().normalize();
            if (fwdM.dot(toF) > 0.86 && this.rng.next() < dt * 1.6) {
              m.target = null;
              m.dir.copy(fwdM);
            }
            break;
          }
        }
      }

      // 比例导引
      const speed = m.vel.length();
      const target = m.target;
      if (target && target.alive !== false) {
        const toT = target.position.clone().sub(m.pos);
        const dist = toT.length();
        // 命中判定
        if (dist < MISSILE.proximity) {
          this.explode(m.pos, 26, 1.8, m.owner, MISSILE.damage, MISSILE.splash);
          if (m.owner === 'player' || m.owner === 'friendly') this.stats.missileHits++;
          this._removeMissile(i, m);
          continue;
        }
        const los = toT.normalize();
        const lead = target.velocity.clone().multiplyScalar(dist / Math.max(200, speed));
        const aim = target.position.clone().add(lead).sub(m.pos).normalize();
        const blend = clamp(MISSILE.turnRate * dt * (1 + (1 - clamp(dist / 1200, 0, 1)) * 0.8), 0, 1);
        m.dir.lerp(aim, blend).normalize();
      } else {
        // 失去目标：直线飞行，逐渐掉落
        m.dir.y -= dt * 0.35;
        m.dir.normalize();
      }

      const newSpeed = Math.min(MISSILE.maxSpeed, speed + MISSILE.accel * dt);
      m.vel.copy(m.dir).multiplyScalar(newSpeed);
      m.pos.addScaledVector(m.vel, dt);
      m.mesh.position.copy(m.pos);
      m.mesh.quaternion.setFromUnitVectors(_axisZ, m.dir);

      // 尾烟：浓烟拖尾 + 点火亮点（按时间节流，避免每帧生成）
      if (this.particles) {
        m.smokeT -= dt;
        if (m.smokeT <= 0) {
          m.smokeT = 0.018;
          const back = _v3.copy(m.pos).addScaledVector(m.dir, -3.2);
          this.particles.spawn(back.x, back.y, back.z,
            this.rng.range(-4, 4), this.rng.range(-4, 4), this.rng.range(-4, 4),
            this.rng.range(0.8, 1.8), this.rng.range(2.6, 4.6), 1, 1.0, 0);
          this.particles.spawn(m.pos.x, m.pos.y, m.pos.z,
            0, 0, 0, 0.06, 2.2, 0, 0, 0);
        }
      }

      // 撞地 / 撞目标 / 寿命
      if (this.world.hitsGround(m.pos, 0.5)) {
        this.explode(m.pos, 24, 1.6, m.owner, MISSILE.damage * 0.5, MISSILE.splash);
        this._removeMissile(i, m);
        continue;
      }
      // 误伤判定：不同阵营之间才会命中
      const hitTarget = this._missileHitCheck(m);
      if (hitTarget) {
        this.explode(m.pos, 26, 1.8, m.owner, MISSILE.damage, MISSILE.splash);
        if (hitTarget === this.player) this._damagePlayer(MISSILE.damage, m.launchedFrom);
        else if (hitTarget.faction === 'ally') {
          if (hitTarget.damage(MISSILE.damage, null)) this._killAlly(hitTarget);
        } else if (hitTarget.damage(MISSILE.damage * 0.5, null)) {
          if (m.owner === 'friendly') this._alliedKill(hitTarget, m.launchedFrom);
          else this._kill(hitTarget, m.owner);
        }
        this._removeMissile(i, m);
        continue;
      }
      if (m.life <= 0) this._removeMissile(i, m);
    }
    this._resolveMissileChains();
  }

  /**
   * 同帧多枚导弹同时命中同一目标时，第一枚就把目标打爆，
   * 后续导弹的目标已死却还留在数组里继续飞。这里把它们改成「已失去目标」
   * （直线飞行 + 缓慢下坠），并连带到附近已被打爆的目标上殉爆。
   */
  _resolveMissileChains() {
    for (const m of this.missiles) {
      if (!m.target || m.target.alive !== false) continue;
      m.target = null;
    }
  }

  _missileHitCheck(m) {
    const enemyFaction = m.owner === 'enemy';
    if (enemyFaction) {
      if (this.player && this.player.alive &&
          this.player.position.distanceTo(m.pos) < MISSILE.proximity + this.player.radius) return this.player;
      for (const a of this.allies) {
        if (!a.alive) continue;
        if (a.position.distanceTo(m.pos) < MISSILE.proximity + a.radius) return a;
      }
      return null;
    }
    // 玩家/僚机导弹只打敌机
    for (const e of this.enemies) {
      if (!e.alive || e.spawnGrace > 0.25) continue;
      if (e.position.distanceTo(m.pos) < MISSILE.proximity + e.radius) return e;
    }
    return null;
  }

  _removeMissile(index, m) {
    this.scene.remove(m.mesh);
    this.missiles.splice(index, 1);
  }

  _updateBombs(dt, ctx) {
    const list = this.bombs;
    for (let i = list.length - 1; i >= 0; i--) {
      const b = list[i];
      // 身份校验：explode()/连锁引爆会在本循环内 splice 本数组（比如别的炸弹在附近殉爆），
      // 那样 list[i] 会指向 undefined 或位移后的新元素。只处理「仍在这个下标上」的那颗。
      if (!b || list[i] !== b) continue;
      b.life -= dt;
      b.vel.y -= BOMB.gravity * dt;
      b.pos.addScaledVector(b.vel, dt);
      b.mesh.position.copy(b.pos);
      b.mesh.rotation.x += b.spin.x * dt;
      b.mesh.rotation.y += b.spin.y * dt;
      b.mesh.rotation.z += b.spin.z * dt;

      const dmg = b.damage || BOMB.damage;
      const splash = b.splash || BOMB.splash;
      if (this.world.hitsGround(b.pos, 1)) {
        const hitPos = b.pos.clone().setY(Math.max(b.pos.y, this.world.heightAt(b.pos.x, b.pos.z)));
        // 记录落地事件，主程序据此结算机场设施/停机坪损失（不在这里判游戏规则）
        b.grounded = true;
        this.groundImpacts.push({ pos: hitPos, owner: b.owner, radius: splash });
        if (this.groundImpacts.length > 24) this.groundImpacts.shift();
        this.explode(hitPos, 55, 3.2, b.owner, dmg, splash);
        if (this.onGroundImpact) this.onGroundImpact(hitPos, b.owner, splash);
        this.scene.remove(b.mesh);
        list.splice(i, 1);
        continue;
      }
      if (b.owner === 'player') {
        for (const e of this.enemies) {
          if (!e.alive) continue;
          if (e.position.distanceTo(b.pos) < e.radius + 4) {
            this.explode(b.pos, 55, 3.2, 'player', dmg, splash);
            this.scene.remove(b.mesh);
            list.splice(i, 1);
            break;
          }
        }
      }
      if (b.life <= 0) { this.scene.remove(b.mesh); list.splice(i, 1); }
    }
  }

  _updateFlares(dt) {
    const list = this.flares;
    for (let i = list.length - 1; i >= 0; i--) {
      const f = list[i];
      if (!f || list[i] !== f) continue;
      f.life -= dt;
      if (f.life <= 0) { this.scene.remove(f.sprite); f.sprite.material.dispose(); list.splice(i, 1); continue; }
      f.vel.y -= 22 * dt;
      const damp = Math.max(0, 1 - 1.4 * dt);
      f.vel.multiplyScalar(damp);
      f.pos.addScaledVector(f.vel, dt);
      f.sprite.position.copy(f.pos);
      const k = f.life / f.maxLife;
      f.sprite.material.opacity = k;
      const s = 6 * (1.6 - k) + 3;
      f.sprite.scale.set(s, s, 1);
      // 拖尾火花
      if (this.particles && this.rng.next() < 0.6) {
        this.particles.spawn(f.pos.x, f.pos.y, f.pos.z,
          this.rng.range(-8, 8), this.rng.range(-8, 8), this.rng.range(-8, 8),
          0.5, 1.6, 1, 2, -8);
      }
    }
  }

  _updateExplosions(dt) {
    const list = this.explosions;
    for (let i = list.length - 1; i >= 0; i--) {
      const e = list[i];
      if (!e || list[i] !== e) continue;
      e.life -= dt;
      if (e.life <= 0) { this.scene.remove(e.sprite); e.sprite.material.dispose(); list.splice(i, 1); continue; }
      const k = 1 - e.life / e.maxLife;
      const s = e.sprite.scale.x + e.grow * dt;
      e.sprite.scale.set(s, s, 1);
      if (e.type === 'boom') e.sprite.material.opacity = Math.max(0, 1 - k * 1.2);
      else if (e.type === 'spark') e.sprite.material.opacity = Math.max(0, 1 - k * 1.6);
      else e.sprite.material.opacity = Math.max(0, 0.5 * (1 - k));
      if (e.drift) e.sprite.position.addScaledVector(e.drift, dt);
    }
  }

  dispose() {
    this.clear();
    this.tracerGeo.dispose();
    this._pGeo.dispose();
    for (const tex of [this.texBoom, this.texSpark, this.texSmoke, this.texFlare]) tex.dispose();
    for (const m of [this.matBoom, this.matSpark, this.matSmoke, this.matFlare, this.matHitMark,
                     this.missileMatPlayer, this.missileMatEnemy, this.missileMatAlly, this.bombMat]) m.dispose();
    this.missileGeo.dispose();
    this.bombGeo.dispose();
  }

  clear() {
    this.tracers.length = 0;
    for (const m of this.missiles) this.scene.remove(m.mesh);
    this.missiles.length = 0;
    for (const b of this.bombs) this.scene.remove(b.mesh);
    this.bombs.length = 0;
    this.shells.length = 0;
    for (const f of this.flares) { this.scene.remove(f.sprite); f.sprite.material.dispose(); }
    this.flares.length = 0;
    for (const e of this.explosions) { this.scene.remove(e.sprite); e.sprite.material.dispose(); }
    this.explosions.length = 0;
    this.groundImpacts.length = 0;
    this.tracerGeo.setDrawRange(0, 0);
    this.stats.shots = 0; this.stats.hits = 0; this.stats.fired = 0; this.stats.enemyShots = 0;
    this.stats.missilesFired = 0; this.stats.missileHits = 0; this.stats.flakFired = 0;
    this.shake = 0;
  }
}

/** 射线与球体求交：返回最近正距离，未命中返回 -1 */
export function raySphere(origin, dir, center, radius) {
  const oc = _rs.copy(origin).sub(center);
  const b = oc.dot(dir);
  const c = oc.lengthSq() - radius * radius;
  const disc = b * b - c;
  if (disc < 0) return -1;
  const sq = Math.sqrt(disc);
  const t1 = -b - sq;
  if (t1 >= 0) return t1;
  const t2 = -b + sq;
  return t2 >= 0 ? t2 : -1;
}

export { BOMB, MISSILE, FLAK };
