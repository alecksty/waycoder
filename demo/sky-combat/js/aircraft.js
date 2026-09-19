// =====================================================================
// aircraft.js — 飞行器模型与飞行力学
//   · buildJetModel() 程序化生成战机网格（无机型资源文件）
//   · Aircraft 基类：四元数姿态、推力/阻力/升力、失速、损伤
//   · PlayerAircraft：玩家操控 + 武器状态
//   · EnemyAircraft：分层状态机 AI（追击/规避/反制/轰炸机）
// =====================================================================
import * as THREE from 'three';
import { clamp, lerp, approach, angleDelta, RNG } from './utils.js';

const _euler = new THREE.Euler();

/** 把角度归一化到 -PI..PI（航向/坡度是周期量，不能让数值无限增长） */
function wrapPi(a) {
  a = (a + Math.PI) % (Math.PI * 2);
  if (a < 0) a += Math.PI * 2;
  return a - Math.PI;
}

const _q = new THREE.Quaternion();
const _q2 = new THREE.Quaternion();
const _worldUp = new THREE.Vector3(0, 1, 0);
const _ref = new THREE.Vector3(0, 1, 0);
const _rh = new THREE.Vector3();
const _dir = new THREE.Vector3();
const _upTmp = new THREE.Vector3(0, 1, 0);
const _v1 = new THREE.Vector3();
const _v2 = new THREE.Vector3();
const _v3 = new THREE.Vector3();

// ---------------------------------------------------------------------
// 程序化战机模型
// ---------------------------------------------------------------------
export function buildJetModel(palette, scale = 1) {
  const g = new THREE.Group();
  const body = new THREE.MeshStandardMaterial({ color: palette.body, roughness: 0.42, metalness: 0.62 });
  const dark = new THREE.MeshStandardMaterial({ color: palette.dark, roughness: 0.35, metalness: 0.8 });
  const glass = new THREE.MeshStandardMaterial({
    color: 0x101c26, roughness: 0.05, metalness: 1, transparent: true, opacity: 0.72,
  });
  const accent = new THREE.MeshStandardMaterial({
    color: palette.accent, emissive: palette.accent, emissiveIntensity: 0.85, roughness: 0.4,
  });

  // 机身：用缩放的长方体组合出三角翼战机轮廓
  const fus = new THREE.Mesh(new THREE.BoxGeometry(1.5, 1.15, 9.6), body);
  fus.position.z = 0;
  g.add(fus);

  const nose = new THREE.Mesh(new THREE.ConeGeometry(0.72, 3.2, 12), body);
  nose.rotation.x = -Math.PI / 2;
  nose.position.z = -6.2;
  g.add(nose);

  const canopy = new THREE.Mesh(new THREE.SphereGeometry(0.62, 12, 10, 0, Math.PI * 2, 0, Math.PI * 0.55), glass);
  canopy.scale.set(1, 0.85, 2.3);
  canopy.position.set(0, 0.62, -1.7);
  g.add(canopy);

  // 主翼（后掠三角翼）
  const wingShape = new THREE.Shape();
  wingShape.moveTo(0, 0);
  wingShape.lineTo(3.9, 2.9);
  wingShape.lineTo(4.1, 3.7);
  wingShape.lineTo(0.6, 3.5);
  wingShape.lineTo(0, 1.4);
  wingShape.lineTo(0, 0);
  const wingGeo = new THREE.ExtrudeGeometry(wingShape, { depth: 0.26, bevelEnabled: false });
  wingGeo.rotateX(Math.PI / 2);
  for (const s of [1, -1]) {
    const w = new THREE.Mesh(wingGeo, body);
    w.scale.x = s;
    w.position.set(0.55 * s, -0.1, 1.1);
    g.add(w);
    // 翼尖导弹挂架
    const pylon = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.3, 1.8), dark);
    pylon.position.set(3.9 * s, -0.42, 2.1);
    g.add(pylon);
    const tipLight = new THREE.Mesh(new THREE.SphereGeometry(0.13, 8, 6), accent);
    tipLight.position.set(4.0 * s, -0.15, 2.9);
    g.add(tipLight);
  }

  // 水平尾翼
  const tailGeo = new THREE.BoxGeometry(3.4, 0.18, 1.7);
  const tail = new THREE.Mesh(tailGeo, body);
  tail.position.set(0, 0.1, 4.2);
  g.add(tail);

  // 垂直双垂尾
  for (const s of [1, -1]) {
    const fin = new THREE.Mesh(new THREE.BoxGeometry(0.16, 2.1, 2.0), dark);
    fin.position.set(0.85 * s, 1.15, 4.1);
    fin.rotation.z = -0.22 * s;
    g.add(fin);
  }

  // 发动机喷口 + 尾焰
  const eng = new THREE.Mesh(new THREE.CylinderGeometry(0.78, 0.92, 1.5, 14), dark);
  eng.rotation.x = Math.PI / 2;
  eng.position.z = 4.9;
  g.add(eng);

  const flame = new THREE.Mesh(
    new THREE.ConeGeometry(0.68, 3.0, 14, 1, true),
    new THREE.MeshBasicMaterial({ color: palette.flame, transparent: true, opacity: 0.85, side: THREE.DoubleSide })
  );
  flame.rotation.x = Math.PI / 2;
  flame.position.z = 6.4;
  g.add(flame);

  const afterburner = new THREE.Mesh(
    new THREE.ConeGeometry(0.92, 7.0, 14, 1, true),
    new THREE.MeshBasicMaterial({ color: 0x9fd8ff, transparent: true, opacity: 0.0, side: THREE.DoubleSide })
  );
  afterburner.rotation.x = Math.PI / 2;
  afterburner.position.z = 8.4;
  g.add(afterburner);

  g.scale.setScalar(scale);
  g.userData.parts = { flame, afterburner, body, dark, accent };
  return g;
}

export const PALETTE_PLAYER = { body: 0x8fa4b8, dark: 0x2b3742, accent: 0x2ce0ff, flame: 0xffa53d };
export const PALETTE_ENEMY = { body: 0x6b6f74, dark: 0x22262b, accent: 0xff3b3b, flame: 0xff6a2a };
export const PALETTE_ENEMY_ACE = { body: 0x7d2f3a, dark: 0x2b1216, accent: 0xffc93b, flame: 0xff8a2a };
export const PALETTE_BOMBER = { body: 0x4d5a4a, dark: 0x232a22, accent: 0x9dff5e, flame: 0xff8a2a };
export const PALETTE_ALLY = { body: 0x9aa7b4, dark: 0x2f3a44, accent: 0x4dffa1, flame: 0xffc25a };

// ---------------------------------------------------------------------
// 飞机基类
// ---------------------------------------------------------------------
export class Aircraft {
  constructor(scene, opts = {}) {
    this.scene = scene;
    this.faction = opts.faction || 'enemy';
    this.type = opts.type || 'fighter';
    this.palette = opts.palette || PALETTE_ENEMY;
    this.scale = opts.scale || 1;

    this.mesh = buildJetModel(this.palette, this.scale);
    if (this.faction === 'enemy') {
      // 敌机朝向：模型机头朝 -Z，与玩家一致，无需翻转
      this.mesh.userData.parts.accent.emissiveIntensity = 1.2;
    }
    scene.add(this.mesh);

    this.position = new THREE.Vector3();
    this.quaternion = new THREE.Quaternion();
    this.velocity = new THREE.Vector3();
    this.speed = 0;

    // 性能参数（可由 opts 覆盖）
    this.maxSpeed = opts.maxSpeed ?? 330;       // m/s ≈ 1188 km/h
    this.minSpeed = opts.minSpeed ?? 78;        // 失速速度
    this.thrustAccel = opts.thrustAccel ?? 62;  // m/s^2
    this.abMultiplier = opts.abMultiplier ?? 1.7;
    this.dragK = opts.dragK ?? 0.00075;
    this.pitchRate = opts.pitchRate ?? 1.5;
    this.rollRate = opts.rollRate ?? 3.1;
    this.yawRate = opts.yawRate ?? 0.62;
    this.gLimit = opts.gLimit ?? 9.5;

    // 状态
    this.throttle = 0.75;
    this.afterburner = false;
    this.airbrake = false;
    this.deathTime = 0;
    this.hp = opts.hp ?? 100;
    this.maxHp = this.hp;
    this.alive = true;
    this.stalling = false;
    this.gLoad = 1;
    this.radius = (opts.radius ?? 6) * this.scale;
    this.contrailOn = false;
    this.smokeLevel = 0;
    this.sysDamage = 0;      // 0..1 系统损伤（液压/发动机受损 → 推力与操控下降）
    this.hitFlash = 0;
    this.trailT = 0;         // 翼尖尾迹发射计时
    this.smokeT = 0;         // 受损黑烟发射计时

    // 气动耦合参数（主程序按操纵模式覆写）
    this.bankTurnGain = 0.98;   // 倾斜转弯强度（1.0 ≈ 真实协调转弯 g·tan(bank)/V）
    this.bankDropGain = 0.18;   // 倾斜时低头趋势
    this.levelAssist = 0.5;     // 坡度阻尼强度（0 = 完全不回正）
    this.trimAssist = 0;        // 俯仰自动配平强度（0 = 保留俯仰自由度）

    // 平滑控制量
    this.inputs = { pitch: 0, roll: 0, yaw: 0 };
    this.angular = { pitch: 0, roll: 0, yaw: 0 };
    // 姿态角（世界参考）：航向 0 = 北(-Z)，顺时针为正；俯仰 > 0 抬头；坡度 > 0 左倾
    this.heading = 0;
    this.pitch = 0;
    this.bank = 0;
    this.bankVis = 0;           // 视觉坡度（侧立时更稳定，仅供显示）
    this.pitchLimit = opts.pitchLimit ?? 1.42;   // 约 ±81°，防止越顶失控
    this.turnBias = 0;          // 转弯偏置（侧立改平用）

    this.wingtipLeft = new THREE.Vector3();
    this.wingtipRight = new THREE.Vector3();
    this.forward = new THREE.Vector3(0, 0, -1);
  }

  /** 应用位置与姿态到网格 */
  syncMesh() {
    this.mesh.position.copy(this.position);
    this.mesh.quaternion.copy(this.quaternion);
  }

  getForward(out = _v1) { return out.set(0, 0, -1).applyQuaternion(this.quaternion); }
  getUp(out = _v1) { return out.set(0, 1, 0).applyQuaternion(this.quaternion); }
  getRight(out = _v1) { return out.set(1, 0, 0).applyQuaternion(this.quaternion); }

  updateWingtips() {
    this.wingtipLeft.set(-4.0 * this.scale, -0.15 * this.scale, 2.6 * this.scale).applyQuaternion(this.quaternion).add(this.position);
    this.wingtipRight.set(4.0 * this.scale, -0.15 * this.scale, 2.6 * this.scale).applyQuaternion(this.quaternion).add(this.position);
  }

  /** 油门 / 加力等对引擎外观的反馈 */
  updateEngineVisual(dt) {
    const parts = this.mesh.userData.parts;
    const burn = this.alive ? (0.35 + this.throttle * 0.65) : 0;
    const want = this.afterburner ? 0.75 : 0;
    parts.afterburner.material.opacity = approach(parts.afterburner.material.opacity, want, dt * 3.2);
    const fl = parts.flame.scale;
    const target = this.alive ? (0.6 + this.throttle * 0.9 + (this.afterburner ? 0.5 : 0)) : 0.1;
    fl.set(approach(fl.x, target, dt * 2), approach(fl.y, target, dt * 2), approach(fl.z, target, dt * 2));
    parts.flame.material.opacity = approach(parts.flame.material.opacity, burn, dt * 2.5);
    if (this.hitFlash > 0) {
      this.hitFlash -= dt * 4;
      const f = clamp(this.hitFlash, 0, 1);
      parts.body.emissive && parts.body.emissive.setRGB(f, f * 0.15, f * 0.1);
    }
  }

  // -------------------------------------------------------------------
  // 姿态控制：内部用「航向 / 俯仰 / 坡度」三个世界参考角驱动，再组装回四元数。
  // 这样滚转是纯粹绕机头轴的滚转、转弯是绕世界竖直轴的圆弧，绝不会退化成
  // 「绕机体 Y 轴偏航」那种会导致俯仰振荡、螺旋失控的模型。
  // -------------------------------------------------------------------

  /**
   * 从当前四元数反推姿态角（航向/俯仰/坡度）。
   * 构造函数注入过 quaternion、或测试直接设过姿态后，都要先调它。
   * 约定：航向 0 = 北(-Z)、顺时针（向东）为正；俯仰 > 0 抬头；坡度 > 0 左倾（左翼下沉）
   */
  syncAttitude() {
    const fwd = this.getForward(_v1);
    const up = this.getUp(_v2);
    const horiz = Math.max(1e-6, Math.hypot(fwd.x, fwd.z));
    this.heading = Math.atan2(fwd.x, -fwd.z);
    this.pitch = Math.atan2(fwd.y, horiz);
    this.bank = this._bankOf(fwd, up);
    this.bankVis = this.bank;
    this._attitudeReady = true;
    return this;
  }

  /** 坡度角：绕机头轴转过的角度（左倾为正）。垂直爬升/俯冲时坡度无意义，返回 0 */
  _bankOf(fwd, up) {
    const ref = _ref.set(0, 1, 0).addScaledVector(fwd, -fwd.y);   // 不滚转时应有的天向
    if (ref.lengthSq() < 1e-8) return 0;
    ref.normalize();
    const rh = _rh.crossVectors(fwd, ref).normalize();            // 机体右侧水平方向
    return -Math.atan2(rh.dot(up), ref.dot(up));
  }

  /**
   * 由姿态角组装四元数。Euler 顺序 YXZ = 先滚转（绕机头轴）→ 再俯仰（绕机体右轴）
   * → 最后航向（绕世界竖直轴），这正是飞机的标准欧拉角约定，且与上面的
   * syncAttitude 反推完全一致（单轴输入可无损往返）。
   */
  _applyAttitude() {
    // Euler 的 x=俯仰、y=航向（取负号，使 0 = 北 = -Z、向东为正）、z=坡度（左倾为正）
    _euler.set(this.pitch, -this.heading, this.bank, 'YXZ');
    this.quaternion.setFromEuler(_euler);
    this.rollCos = this.getUp(_upTmp).y;      // >0 正飞，<0 倒扣
  }

  /** 核心飞行积分：输入已写入 this.inputs（-1..1） */
  integrate(dt) {
    // 构造函数注入过姿态（例如测试直接设 quaternion）时必须先反推姿态角，
    // 否则 heading/pitch/bank 三个内部量会和实际姿态不一致
    if (this._attitudeReady !== true) this.syncAttitude();
    const fwd = this.getForward(_v1.clone());
    this.forward.copy(fwd);

    const speedFactor = clamp(this.speed / this.minSpeed, 0, 1);
    const authority = this.speed > this.minSpeed ? 1 : lerp(0.18, 1, speedFactor);
    // 高速时舵面效率下降（模拟真实气动）
    const highSpeedPenalty = clamp(1 - (this.speed - 280) / 900, 0.55, 1);
    // 战伤：液压受损后舵面效率下降最多 35%
    const dmgPenalty = 1 - this.sysDamage * 0.35;
    const auth = authority * highSpeedPenalty * dmgPenalty;

    // 失速时舵面几乎失效
    const stallK = this.stalling ? 0.35 : 1;

    // ---- 滚转：绕机头轴（坡度）----
    // 横向稳定性：无滚转输入时坡度缓慢回正（真实飞机的上反角效应）
    const levelGain = (this.levelAssist ?? 0.5) * (1 - Math.min(1, Math.abs(this.inputs.roll)));
    let wantRoll = -this.inputs.roll * this.rollRate;
    // 坡度阻尼必须与坡度反向（bank 越大越往回压），否则会变成正反馈自旋
    if (levelGain > 0) wantRoll += clamp(-this.bank * 1.15, -0.95, 0.95) * levelGain;
    this.angular.roll = lerp(this.angular.roll, wantRoll * auth * stallK, clamp(dt * 9, 0, 1));
    this.bank = wrapPi(this.bank + this.angular.roll * dt);

    // ---- 俯仰（爬升角）----
    // 自动配平：无俯仰输入时缓慢回到近水平姿态
    const trimGain = (this.trimAssist ?? 0) * (1 - Math.min(1, Math.abs(this.inputs.pitch)));
    let wantPitch = this.inputs.pitch * this.pitchRate;
    if (trimGain > 0) wantPitch += clamp(-this.pitch * 0.9, -1.2, 1.2) * trimGain;
    this.angular.pitch = lerp(this.angular.pitch, wantPitch * auth * stallK, clamp(dt * 7, 0, 1));

    // G 载荷限制：过载时抑制俯仰率
    const gPotential = Math.abs(this.angular.pitch) * this.speed / 9.81;
    if (gPotential > this.gLimit) {
      this.angular.pitch *= this.gLimit / gPotential;
      this.gLoad = this.gLimit;
    } else {
      this.gLoad = lerp(this.gLoad, gPotential, clamp(dt * 4, 0, 1));
    }
    this.pitch = clamp(this.pitch + this.angular.pitch * dt, -this.pitchLimit, this.pitchLimit);
    // 失速：机头自然下垂
    if (this.stalling) this.pitch = clamp(this.pitch - 0.55 * dt * (1 - this.speed / this.minSpeed), -this.pitchLimit, this.pitchLimit);

    // ---- 偏航（方向舵）----
    this.angular.yaw = lerp(this.angular.yaw, this.inputs.yaw * this.yawRate * auth * stallK, clamp(dt * 5, 0, 1));
    this.heading = wrapPi(this.heading - this.angular.yaw * dt);

    // ---- 协调转弯 ----
    // 坡度产生的转弯必须绕「世界竖直轴」旋转：机头沿水平圆弧移动，坡度保持不变。
    // 若绕机体 Y 轴偏航，机头会在机体系里画圆锥 → 俯仰振荡 → 螺旋失控。
    const turnRate = Math.tan(clamp(this.bank, -1.45, 1.45)) * 9.81 * this.bankTurnGain / Math.max(70, this.speed);
    this.heading = wrapPi(this.heading - turnRate * dt);

    // ---- 组装姿态（航向 + 俯仰 + 坡度）----
    this._applyAttitude();

    // ---- 速度积分 ----
    // 机头相对水平面的爬升分量：fwd.y > 0 表示正在爬升（用刚组装好的新姿态）
    const climbSin = this.getForward(_v1).y;
    let thrust = this.thrustAccel * this.throttle * (this.afterburner ? this.abMultiplier : 1);
    thrust *= 1 - this.sysDamage * 0.3;            // 发动机受损推力下降
    if (this.airbrake) thrust -= this.thrustAccel * 0.55;

    const drag = this.dragK * this.speed * this.speed + (this.airbrake ? 18 : 0);
    let accel = thrust - drag;

    // 重力分量：爬升减速、俯冲加速
    accel -= climbSin * 9.81 * clamp(this.speed / 140, 0.25, 1.15);

    this.speed = clamp(this.speed + accel * dt, 0, this.maxSpeed * (this.afterburner ? 1.28 : 1));

    // 失速判定
    this.stalling = this.speed < this.minSpeed;

    // 升力/侧滑：速度向量向机头方向靠拢，制造协调转弯手感
    const desiredVel = this.getForward(_v2).multiplyScalar(this.speed);
    const sla = this.stalling ? 0.55 : 2.6 * clamp(this.speed / 190, 0.4, 1.6);
    this.velocity.lerp(desiredVel, clamp(dt * sla, 0, 1));

    this.position.addScaledVector(this.velocity, dt);
    this.updateWingtips();
  }

  /** 受到伤害 */
  damage(amount, source) {
    if (!this.alive) return false;
    this.hp -= amount;
    this.hitFlash = 1;
    if (this.hp <= 0) {
      this.hp = 0;
      this.alive = false;
      this.smokeLevel = 1;
      this.sysDamage = 1;
      return true; // 被击落
    }
    this.smokeLevel = clamp(1 - this.hp / this.maxHp, 0, 1);
    this.sysDamage = clamp(this.smokeLevel * 1.15, 0, 1);
    return false;
  }

  /** 受损黑烟（浓烟 + 火花），由拥有者每帧调用 */
  emitDamageSmoke(dt, particles, rng) {
    const level = this.alive ? this.smokeLevel : 1;
    if (!particles || level < 0.28) return;
    this.smokeT -= dt;
    if (this.smokeT > 0) return;
    this.smokeT = 0.05 + (1 - level) * 0.14;
    const p = _v1.set(0, 0.4, 5.2).applyQuaternion(this.quaternion).add(this.position);
    const r = this.radius * 0.8;
    particles.spawn(
      p.x + (rng.next() - 0.5) * r, p.y + (rng.next() - 0.5) * r, p.z + (rng.next() - 0.5) * r,
      this.velocity.x * 0.35 + (rng.next() - 0.5) * 7,
      this.velocity.y * 0.35 + 3 + rng.next() * 7,
      this.velocity.z * 0.35 + (rng.next() - 0.5) * 7,
      0.7 + level * 1.6, 1.6 + level * 6.5, 1, 0.55, 0.4);
    if (rng.next() < level * 0.7) {
      particles.spawn(p.x, p.y, p.z,
        (rng.next() - 0.5) * 12, (rng.next() - 0.5) * 12, (rng.next() - 0.5) * 12,
        0.25, 1.4, 0, 3.2, -6);
    }
  }

  /** 翼尖拉烟 / 高 G 涡流，由拥有者每帧调用 */
  emitWingtips(dt, particles) {
    if (!particles || !this.contrailOn || !this.alive) return;
    this.trailT -= dt;
    if (this.trailT > 0) return;
    const amp = this.gLoad > 5 || this.speed > 250 ? 1 : 0.62;
    this.trailT = 0.032 / amp;
    for (const tip of [this.wingtipLeft, this.wingtipRight]) {
      particles.spawn(tip.x, tip.y, tip.z, 0, 0.4, 0, 0.7, 3.4 * amp, 1, 0.35, 0.15);
    }
  }

  /**
   * 自动驾驶：把机头指向世界坐标中的某点（敌机 AI / 僚机共用）。
   * 基于「目标航向 + 目标爬升角」求解三个舵量，天然不会滚成倒扣。
   *   ① 水平方位误差 → 目标坡度（左转倾左、右转倾右，最多 70°）
   *   ② 垂直误差   → 目标爬升角（按剩余水平距离缩放，远处更早配平）
   *   ③ 坡度误差   → 滚转输入（纯比例）
   * @returns {{behind:boolean, localX:number, localY:number, localZ:number, headingErr:number, pitchErr:number, distance:number}} 供 AI 判断
   */
  steerTowards(point, dt, aggressive = 1) {
    const fwd = this.getForward(_v1.clone());
    const up = this.getUp(_v2.clone());
    const right = this.getRight(_v3.clone());

    const to = _dir.copy(point).sub(this.position);
    const distH = Math.hypot(to.x, to.z);
    const localX = to.dot(right) / Math.max(1, to.length());
    const localY = to.dot(up) / Math.max(1, to.length());
    const localZ = to.dot(fwd) / Math.max(1, to.length());
    const behind = localZ < 0;

    // ① 水平方位：目标航向 vs 当前航向（最短角差，正 = 需右转）
    const targetHeading = distH > 1e-3 ? Math.atan2(to.x, -to.z) : this.heading;
    const headingErr = angleDelta(this.heading, targetHeading);
    // ② 垂直：目标爬升角，远处只做温和修正，近处才全量跟随
    const targetPitch = Math.atan2(to.y, Math.max(60, distH));
    const pitchErr = targetPitch - this.pitch;

    // 目标坡度：右转 → 右倾（bank < 0）。
    // 注意：本模型转弯率 = g·tan(bank)/V，300 m/s 时 60° 坡度也只有 3°/s，
    // 所以急转必须允许大坡度（最大约 79°），否则 AI 掉头要花半分钟。
    const maxBank = clamp(1.38 * aggressive, 0.32, 1.38);
    const wantBank = clamp(-headingErr * 1.35, -maxBank, maxBank);
    const bankErr = wantBank - this.bank;

    // 坡度回路是「速率指令」：inputs.roll 与 d(bank)/dt 反向，所以取 -bankErr
    const rollCmd = clamp((-bankErr * 1.9 - this.angular.roll * 0.35) * aggressive, -1, 1);
    this.inputs.roll = approach(this.inputs.roll, rollCmd, dt * 5.5);
    // 坡度不够时先滚转、少拉杆，避免低速大迎角失速
    const bankReady = 1 - Math.min(1, Math.abs(bankErr) / 0.7) * 0.55;
    const pitchCmd = clamp((pitchErr * (distH > 1500 ? 1.1 : 2.4) - this.angular.pitch * 0.5) * aggressive, -1, 1);
    this.inputs.pitch = approach(this.inputs.pitch, pitchCmd * bankReady, dt * 3.6);
    this.inputs.yaw = 0;
    return { behind, localX, localY, localZ, headingErr, pitchErr, distance: to.length() };
  }

  dispose() {
    this.scene.remove(this.mesh);
    this.mesh.traverse((o) => {
      if (o.geometry) o.geometry.dispose();
      if (o.material) {
        if (Array.isArray(o.material)) o.material.forEach((m) => m.dispose());
        else o.material.dispose();
      }
    });
  }
}

// ---------------------------------------------------------------------
// 玩家战机
// ---------------------------------------------------------------------
export class PlayerAircraft extends Aircraft {
  constructor(scene, opts = {}) {
    super(scene, {
      faction: 'player', type: 'f32', palette: PALETTE_PLAYER, scale: 1.0,
      maxSpeed: 342, minSpeed: 74, thrustAccel: 68, pitchRate: 1.55, rollRate: 3.3, yawRate: 0.6,
      gLimit: 9.8, hp: 100, radius: 6.2, ...opts,
    });
    this.fuel = 100;
    this.weapons = {
      mode: 0,                                  // 0 机炮 1 导弹 2 炸弹 3 干扰弹
      missiles: 8, bombs: 4, flares: 6,
      gunHeat: 0, gunCooldown: 0, missileCooldown: 0, bombCooldown: 0, flareCooldown: 0,
    };
    this.cameraMode = 0;                         // 0 追尾 1 座舱 2 电影
    this.lastFiredGun = 0;
    this.gunOverheat = false;
    this.kills = 0;
    this.deaths = 0;
  }

  update(dt) {
    if (this.alive) {
      this.integrate(dt);
      const w = this.weapons;
      // 过热封锁：升温快、降温慢，超过 1.0 后必须冷却到 0.62 以下才解除
      const heating = w.gunCooldown > 0;
      w.gunHeat = clamp(w.gunHeat + (heating ? 0 : -dt * (w.gunHeat > 1 ? 0.3 : 0.62)), 0, 1.5);
      if (w.gunHeat > 1.0) this.gunOverheat = true;
      else if (w.gunHeat < 0.62) this.gunOverheat = false;
      w.gunCooldown = Math.max(0, w.gunCooldown - dt);
      w.missileCooldown = Math.max(0, w.missileCooldown - dt);
      w.bombCooldown = Math.max(0, w.bombCooldown - dt);
      w.flareCooldown = Math.max(0, w.flareCooldown - dt);
      this.fuel = clamp(this.fuel - dt * (this.afterburner ? 1.35 : 0.28), 0, 100);
      if (this.fuel <= 0) this.afterburner = false;
      this.throttle = clamp(this.throttle, 0.2, 1);
      this.contrailOn = this.speed > 150 || this.gLoad > 4.5 || this.position.y < 700;
    } else {
      // 坠机：自由落体并旋转
      this.velocity.y -= 24 * dt;
      this.position.addScaledVector(this.velocity, dt);
      this.quaternion.multiply(_q.setFromEuler(new THREE.Euler(dt * 1.4, dt * 0.8, dt * 2.2)));
      this.updateWingtips();
    }
    this.syncMesh();
    this.updateEngineVisual(dt);
  }

  /** 相对机体朝向的瞄准方向（机炮轴线） */
  aimPoint(distance = 900, out = new THREE.Vector3()) {
    return out.copy(this.position).addScaledVector(this.getForward(_v3.clone()), distance);
  }
}

// ---------------------------------------------------------------------
// 敌方战机：分层状态机
//   cruise → engage → attack → evasive → reposition
//   另有 kamikaze / bomber 变体行为
// ---------------------------------------------------------------------
export class EnemyAircraft extends Aircraft {
  constructor(scene, opts = {}) {
    const isAce = opts.variant === 'ace';
    const isBomber = opts.variant === 'bomber';
    const palette = isBomber ? PALETTE_BOMBER : isAce ? PALETTE_ENEMY_ACE : PALETTE_ENEMY;
    super(scene, {
      faction: 'enemy',
      type: isBomber ? 'bomber' : isAce ? 'ace' : 'fighter',
      palette,
      scale: isBomber ? 1.9 : isAce ? 1.05 : 0.95,
      maxSpeed: isBomber ? 190 : isAce ? 320 : 295,
      minSpeed: 62,
      thrustAccel: isBomber ? 26 : isAce ? 56 : 48,
      pitchRate: isBomber ? 0.55 : isAce ? 1.42 : 1.18,
      rollRate: isBomber ? 0.7 : isAce ? 2.4 : 1.9,
      yawRate: 0.45,
      gLimit: isBomber ? 3.2 : 7.4,
      hp: isBomber ? 240 : isAce ? 190 : 110,
      radius: isBomber ? 20 : isAce ? 6.4 : 5.8,
      ...opts,
    });
    this.variant = opts.variant || 'fighter';
    this.rng = new RNG(opts.seed || Math.floor(Math.random() * 1e9));
    this.state = 'cruise';
    this.stateTime = 0;
    this.target = null;
    this.skill = opts.skill ?? (isAce ? 0.92 : isBomber ? 0.35 : 0.65);
    this.fireCooldown = this.rng.range(0.6, 2.4);
    this.missileCooldown = this.rng.range(4, 12);
    this.missiles = isBomber ? 0 : isAce ? 4 : 3;
    this.avoidGround = 0;
    this.wanderPhase = this.rng.range(0, 6.28);
    this.cruiseAlt = opts.altitude ?? this.rng.range(900, 2600);
    this.bombCooldown = this.rng.range(2, 6);
    this.aggression = opts.aggression ?? this.rng.range(0.7, 1.25);
    // 对地攻击属性（机场防卫波的敌机在 _spawnWave 里打开）
    this.targetBase = !!opts.targetBase;
    this.throttle = 0.85;
    this.homeBase = new THREE.Vector3();
    this.evadeSign = this.rng.sign();
    this.spawnGrace = 1.0;
  }

  update(dt, ctx) {
    if (!this.alive) {
      this.deathTime += dt;
      this.velocity.y -= 26 * dt;
      this.position.addScaledVector(this.velocity, dt);
      this.quaternion.multiply(_q.setFromEuler(_euler.set(dt * 0.9, dt * 1.7, dt * 1.3)));
      this.syncMesh();
      this.updateEngineVisual(dt);
      return;
    }

    this.stateTime += dt;
    this.spawnGrace = Math.max(0, this.spawnGrace - dt);
    const player = ctx.player;
    this.target = player;

    const distToPlayer = player ? this.position.distanceTo(player.position) : Infinity;
    const alt = this.position.y;
    const ground = ctx.world.heightAt(this.position.x, this.position.z);

    // ---------- 地形规避（最高优先级）----------
    const pullUp = alt - ground < 260 || alt < 240;
    if (pullUp) {
      this.avoidGround = 1.1;
    }
    this.avoidGround = Math.max(0, this.avoidGround - dt);

    // ---------- 状态迁移 ----------
    if (this.variant === 'bomber') {
      this._updateBomber(dt, ctx, distToPlayer);
    } else if (this.role === 'strike' && this.targetBase) {
      // 对地攻击机：地形规避仍优先（低空进场最怕撞山）
      if (pullUp && this.avoidGround > 0) {
        this.state = 'evade-ground';
        this._flyToAltitude(ground + 900, dt, 1.3);
      } else {
        this._updateStrike(dt, ctx, distToPlayer);
      }
    } else if (pullUp && this.avoidGround > 0) {
      this.state = 'evade-ground';
      this._flyToAltitude(ground + 900, dt, 1.3);
    } else {
      switch (this.state) {
        case 'cruise':
          if (distToPlayer < 4200 || ctx.aggressive) this._enter('engage');
          else this._patrol(dt);
          break;
        case 'engage':
          this._engage(dt, ctx, distToPlayer);
          break;
        case 'attack':
          this._attack(dt, ctx, distToPlayer);
          break;
        case 'evasive':
          this._evasive(dt, ctx, distToPlayer);
          break;
        case 'reposition':
          this._reposition(dt, ctx, distToPlayer);
          break;
      }
    }

    this.integrate(dt);
    this.contrailOn = this.smokeLevel > 0.25 || this.speed > 170 || alt < 600;
    this.syncMesh();
    this.updateEngineVisual(dt);
  }

  _enter(state) { this.state = state; this.stateTime = 0; }

  /** 外部强制触发规避机动（被咬住时由主程序调用） */
  forceEvade() { this._enter('evasive'); this.afterburner = true; }

  _patrol(dt) {
    this.throttle = 0.7;
    this.wanderPhase += dt * 0.35;
    const p = this.position.clone();
    p.x += Math.cos(this.wanderPhase) * 1600;
    p.z += Math.sin(this.wanderPhase * 0.8) * 1600;
    p.y = this.cruiseAlt;
    this.steerTowards(p, dt, 0.7);
  }

  _flyToAltitude(y, dt, gain = 1) {
    this.throttle = 1;
    const fwd = this.getForward(_v1.clone());
    const desired = this.position.clone().addScaledVector(fwd, 700);
    desired.y = y;
    this.steerTowards(desired, dt, gain);
  }

  _engage(dt, ctx, dist) {
    if (dist < 900 / Math.max(0.4, this.skill) && this.stateTime > 1.1) { this._enter('attack'); return; }
    this.throttle = 1;
    // 追向玩家的前置点（提前量随距离增加）
    const aim = this._leadPoint(ctx.player, this.speed);
    const r = this.steerTowards(aim, dt, 1.0 * this.skill);
    if (dist > 5200) this._enter('cruise');
  }

  _attack(dt, ctx, dist) {
    const p = ctx.player;
    this.throttle = 0.95;
    const aim = this._leadPoint(p, this.speed);
    const r = this.steerTowards(aim, dt, 1.05 * this.skill);

    // 机炮射击条件：视线夹角小 + 距离合适
    const fwd = this.getForward(_v1.clone());
    const toP = p.position.clone().sub(this.position).normalize();
    const cos = fwd.dot(toP);
    this.fireCooldown -= dt;
    if (this.fireCooldown <= 0 && dist < 1500 && cos > 0.965 - (1 - this.skill) * 0.02 && p.alive) {
      const burst = this.rng.int(4, 9);
      ctx.fireEnemyGun(this, burst);
      this.fireCooldown = this.rng.range(1.1, 2.6) * (2 - this.skill);
    }

    // 导弹发射
    this.missileCooldown -= dt;
    if (this.missiles > 0 && this.missileCooldown <= 0 && dist > 700 && dist < 3200 && cos > 0.93 && this.spawnGrace <= 0) {
      this.missiles--;
      ctx.fireEnemyMissile(this, p);
      this.missileCooldown = this.rng.range(9, 18) / this.aggression;
    }

    if (dist > 1900 || r.behind || cos < 0.5) { this._enter('reposition'); }
  }

  _evasive(dt, ctx, dist) {
    this.throttle = 1;
    this.afterburner = true;
    this.evadeSign = this.stateTime % 4 < 2 ? 1 : -1;
    const up = this.getUp(_v2.clone());
    const dir = up.multiplyScalar(this.evadeSign).add(
      this.getForward(_v1.clone()).multiplyScalar(-0.25)
    ).normalize();
    const p = this.position.clone().addScaledVector(dir, 2400);
    p.y = clamp(p.y, ctx.world.heightAt(p.x, p.z) + 500, 3400);
    this.steerTowards(p, dt, 1.4);
    ctx.releaseEnemyFlare(this);
    if (this.stateTime > this.rng.range(3.5, 6)) { this.afterburner = false; this._enter('reposition'); }
  }

  _reposition(dt, ctx, dist) {
    this.throttle = 1;
    const p = ctx.player;
    if (!p) { this._enter('cruise'); return; }
    const back = this.rng.next() < 0.5 ? 1 : -1;
    const offset = p.position.clone()
      .addScaledVector(p.getForward(_v3.clone()), -2200 * back)
      .add(new THREE.Vector3(this.rng.range(-1500, 1500), this.rng.range(400, 1400), this.rng.range(-1500, 1500)));
    offset.y = clamp(offset.y, 500, 3200);
    this.steerTowards(offset, dt, 0.95);
    if (this.stateTime > this.rng.range(4, 8) || this.position.distanceTo(offset) < 600) { this._enter('engage'); }
  }

  _updateBomber(dt, ctx, dist) {
    // 轰炸机：飞向地面基地投弹，遭遇玩家时用尾炮自卫
    this.throttle = 0.9;
    const target = ctx.bomberTarget || new THREE.Vector3(0, 900, 0);
    const p = target.clone();
    p.y = this.cruiseAlt;
    const horizontal = Math.hypot(this.position.x - p.x, this.position.z - p.z);
    if (horizontal > 380) {
      this.steerTowards(p, dt, 0.55);
    } else {
      this.inputs.roll = approach(this.inputs.roll, Math.sin(this.stateTime * 0.6) * 0.25, dt);
      this.inputs.pitch = approach(this.inputs.pitch, 0.05, dt);
      this.bombCooldown -= dt;
      if (this.bombCooldown <= 0) {
        this.bombCooldown = this.rng.range(1.2, 2.2);
        ctx.dropEnemyBomb(this);
      }
    }
    this._tailGun(dt, ctx, dist);
  }

  /**
   * 对地攻击机（机场防卫波的敌机）：低空直扑机场，抛投炸弹后拉起重整。
   * 与轰炸机的区别是「会俯冲、会反复进场」，而不是在高空平飞投弹。
   */
  _updateStrike(dt, ctx, dist) {
    const base = ctx.baseTarget || new THREE.Vector3(0, 600, 0);
    const horizontal = Math.hypot(this.position.x - base.x, this.position.z - base.z);
    const ground = ctx.world.heightAt(this.position.x, this.position.z);
    this.throttle = 1;

    if (this.state === 'attack') {
      // 俯冲投弹路线：对准机场，降到约 420m 投弹，然后退出
      const p = base.clone();
      p.y = Math.max(ground + 380, 420);
      this.steerTowards(p, dt, 0.9);
      this.bombCooldown -= dt;
      if (this.bombCooldown <= 0 && horizontal < 1400 && this.position.y < 1200) {
        this.bombCooldown = this.rng.range(1.4, 2.6);
        ctx.dropEnemyBomb(this);
      }
      if (horizontal < 260 || this.position.y < 340) this._enter('reposition');
    } else {
      // 重整队形后再进场（保持对玩家有威胁但不会一直贴脸）
      const back = this.position.clone().addScaledVector(
        this.getForward(_v3.clone()).setY(0).normalize(), -2600);
      back.y = clamp(ground + 1500, 1500, 3000);
      this.steerTowards(back, dt, 0.8);
      if (this.stateTime > this.rng.range(3.5, 6)) this._enter('attack');
    }
    this._tailGun(dt, ctx, dist);
  }

  /** 后方自卫机炮（轰炸机/攻击机共用） */
  _tailGun(dt, ctx, dist) {
    if (!ctx.player || dist >= 1400) return;
    const back = this.getForward(_v1.clone()).multiplyScalar(-1);
    const toP = ctx.player.position.clone().sub(this.position).normalize();
    this.fireCooldown -= dt;
    if (this.fireCooldown <= 0 && back.dot(toP) > 0.95) {
      ctx.fireEnemyGun(this, 3);
      this.fireCooldown = this.rng.range(1.6, 3.2);
    }
  }

  /** 计算对目标的前置拦截点 */
  _leadPoint(target, projectileSpeed) {
    const rel = target.position.clone().sub(this.position);
    const dist = rel.length();
    const closure = target.velocity.clone().sub(this.velocity).length() * 0.4;
    const t = clamp(dist / Math.max(120, projectileSpeed + closure), 0, 1.6);
    return target.position.clone().addScaledVector(target.velocity, t);
  }

  /** 判断玩家是否在自己尾后（用于触发规避） */
  isThreatenedBy(player, hardLock) {
    if (!player || !player.alive) return false;
    const toSelf = this.position.clone().sub(player.position);
    const dist = toSelf.length();
    if (dist > 1600) return false;
    const align = player.getForward(_v1.clone()).dot(toSelf.normalize());
    return hardLock || align > 0.975;
  }
}

// ---------------------------------------------------------------------
// 僚机：玩家方 AI 战机，跟随长机并在敌机进入射程时自动开火。
// 状态机：formation（编队跟随） → engage（拦截） → return（归队）
// ---------------------------------------------------------------------
export class WingmanAircraft extends Aircraft {
  constructor(scene, opts = {}) {
    super(scene, {
      faction: 'ally', type: 'f32', palette: PALETTE_ALLY, scale: 0.96,
      maxSpeed: 340, minSpeed: 70, thrustAccel: 62, pitchRate: 1.35, rollRate: 2.6, yawRate: 0.5,
      gLimit: 8.2, hp: 90, radius: 5.6, ...opts,
    });
    this.state = 'formation';
    this.stateTime = 0;
    this.formationOffset = opts.formationOffset ? opts.formationOffset.clone()
      : new THREE.Vector3(opts.side === -1 ? -70 : 70, opts.side === -1 ? 16 : -16, 60);
    this.engageRange = opts.engageRange ?? 2600;
    this.fireCooldown = 0.5;
    this.target = null;
    this.wander = (opts.seed ?? 1) * 0.37;
  }

  _enter(s) { if (this.state !== s) { this.state = s; this.stateTime = 0; } }

  /**
   * @param {object} ctx { player, enemies, fireGun(enemy, burst), world }
   */
  update(dt, ctx = {}) {
    if (!this.alive) {
      this.deathTime += dt;
      this.velocity.y -= 24 * dt;
      this.position.addScaledVector(this.velocity, dt);
      this.quaternion.multiply(_q.setFromEuler(_euler.set(dt * 0.9, dt * 1.5, dt * 1.1)));
      this.syncMesh();
      this.updateEngineVisual(dt);
      return;
    }
    this.stateTime += dt;
    const player = ctx.player;

    // 选目标：最近的存活敌机（且在自己前方 150° 内）
    this.target = null;
    let best = Infinity;
    for (const e of ctx.enemies || []) {
      if (!e.alive) continue;
      const d = this.position.distanceTo(e.position);
      if (d < best && d < this.engageRange + 1400) { best = d; this.target = e; }
    }

    const ground = ctx.world ? ctx.world.heightAt(this.position.x, this.position.z) : 0;
    if (this.position.y - ground < 260) {
      this._flyTo(this.position.x, ground + 900, this.position.z, dt, 1.3);
    } else if (this.target) {
      this._enter('engage');
      this._engage(dt, ctx, best);
    } else if (player && player.alive) {
      this._enter('formation');
      this._follow(dt, player);
    } else {
      this._patrol(dt);
    }

    this.contrailOn = this.speed > 170 || this.gLoad > 4.5;
    this.integrate(dt);
    this.syncMesh();
    this.updateEngineVisual(dt);
  }

  /** 编队跟随：保持在长机侧后方，带一点松弛与高度差 */
  _follow(dt, player) {
    const dist = this.position.distanceTo(player.position);
    this.throttle = 1;
    // 掉队越远越用力追（并开启加力），贴近后收油门稳定编队
    this.afterburner = dist > 250;
    const off = _v1.copy(this.formationOffset)
      .applyQuaternion(player.quaternion)
      .add(_v2.set(Math.sin(this.wander + this.stateTime * 0.7) * 8, 0, 0));
    // 只做一点点提前量：纯尾追时若按「追上时间」外推目标点，追赶目标会持续后退，
    // 反而永远追不上；僚机本身速度更高，直接飞向编队位置即可收敛
    const lead = 0.35;
    const aim = _v3.copy(player.position)
      .addScaledVector(player.velocity, lead)
      .add(off);
    // aggressive 同时限制最大坡度：远距离全力追、贴近后柔和修正
    const st = this.steerTowards(aim, dt, dist > 420 ? 1.05 : 0.6);
    // 能量管理：方位误差大时先转弯别加速，否则速度太快只能绕大圈追不上
    if (Math.abs(st.headingErr) > 0.5) {
      this.throttle = 0.6;
    } else if (dist > 700) {
      this.throttle = 1;
    } else if (dist < 120) {
      this.throttle = 0.72;
    } else {
      this.throttle = 0.85;
    }
  }

  _patrol(dt) {
    this.throttle = 0.8;
    const p = _v1.copy(this.position);
    p.x += Math.cos((this.wander += dt * 0.22)) * 1200;
    p.z += Math.sin(this.wander * 0.9) * 1200;
    this.steerTowards(p, dt, 0.6);
  }

  _flyTo(x, y, z, dt, gain) {
    this.throttle = 1;
    const aim = _v1.set(x, y, z);
    this.steerTowards(aim, dt, gain);
  }

  /** 拦截：追前置点 + 视线角满足时开火（由主程序转发到武器系统） */
  _engage(dt, ctx, dist) {
    const t = this.target;
    this.throttle = 1;
    this.afterburner = dist > 1800;
    const lead = this._leadPoint(t, 900);
    const r = this.steerTowards(lead, dt, 1.0);

    const fwd = this.getForward(_v1.clone());
    const toT = _v2.copy(t.position).sub(this.position).normalize();
    const cos = fwd.dot(toT);
    this.fireCooldown -= dt;
    if (this.fireCooldown <= 0 && dist < 1500 && cos > 0.96 && ctx.fireGun) {
      ctx.fireGun(this, 4);
      this.fireCooldown = 0.9 + Math.random() * 0.8;
    }
    if (ctx.fireMissile && dist > 900 && dist < 3000 && cos > 0.94 && (this._mslCd ?? 0) <= 0) {
      this._mslCd = 12 + Math.random() * 8;
      ctx.fireMissile(this, t);
    }
    this._mslCd = (this._mslCd ?? 0) - dt;
    if (!t.alive || dist > this.engageRange + 1800 || r.behind) this._enter('formation');
  }

  _leadPoint(target, projectileSpeed) {
    const rel = _v1.copy(target.position).sub(this.position);
    const dist = rel.length();
    const closure = _v2.copy(target.velocity).sub(this.velocity).length() * 0.4;
    const t = clamp(dist / Math.max(120, projectileSpeed + closure), 0, 1.6);
    return _v3.copy(target.position).addScaledVector(target.velocity, t);
  }
}
