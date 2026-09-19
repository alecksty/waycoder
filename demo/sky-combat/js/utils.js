// =====================================================================
// utils.js — 通用工具：随机数、数学、噪声、帧计时、对象池
// 全部为零依赖纯函数/类，供世界、战机、武器、主程序复用
// =====================================================================

/** 可复现的伪随机数发生器（Mulberry32） */
export class RNG {
  constructor(seed = 1337) { this.s = seed >>> 0; }
  next() {
    this.s = (this.s + 0x6d2b79f5) >>> 0;
    let t = this.s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }
  range(a, b) { return a + (b - a) * this.next(); }
  int(a, b) { return Math.floor(this.range(a, b + 1)); }
  pick(arr) { return arr[Math.floor(this.next() * arr.length)]; }
  sign() { return this.next() < 0.5 ? -1 : 1; }
}

export const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
export const lerp = (a, b, t) => a + (b - a) * t;
export const smoothstep = (t) => t * t * (3 - 2 * t);
export const rad = (d) => (d * Math.PI) / 180;
export const deg = (r) => (r * 180) / Math.PI;

/** 以固定步长把当前值逼近目标值（用于角度/速度平滑） */
export function approach(cur, target, maxDelta) {
  const d = target - cur;
  if (Math.abs(d) <= maxDelta) return target;
  return cur + Math.sign(d) * maxDelta;
}

/** 角度差（结果落在 -PI..PI） */
export function angleDelta(a, b) {
  let d = (b - a) % (Math.PI * 2);
  if (d > Math.PI) d -= Math.PI * 2;
  if (d < -Math.PI) d += Math.PI * 2;
  return d;
}

/** 二维值噪声（用于地形高度） */
export class ValueNoise2D {
  constructor(seed = 7) {
    const rng = new RNG(seed);
    this.size = 256;
    this.table = new Float32Array(this.size * this.size);
    for (let i = 0; i < this.table.length; i++) this.table[i] = rng.next();
  }
  _at(x, y) {
    const s = this.size;
    const xi = ((x % s) + s) % s | 0;
    const yi = ((y % s) + s) % s | 0;
    return this.table[yi * s + xi];
  }
  sample(x, y) {
    const x0 = Math.floor(x), y0 = Math.floor(y);
    const fx = smoothstep(x - x0), fy = smoothstep(y - y0);
    const v00 = this._at(x0, y0), v10 = this._at(x0 + 1, y0);
    const v01 = this._at(x0, y0 + 1), v11 = this._at(x0 + 1, y0 + 1);
    return lerp(lerp(v00, v10, fx), lerp(v01, v11, fx), fy);
  }
  /** 多倍频分形噪声 */
  fbm(x, y, octaves = 4, lacunarity = 2, gain = 0.5) {
    let amp = 1, freq = 1, sum = 0, norm = 0;
    for (let i = 0; i < octaves; i++) {
      sum += amp * this.sample(x * freq, y * freq);
      norm += amp;
      amp *= gain; freq *= lacunarity;
    }
    return sum / norm;
  }
}

/** 帧计时器：提供 dt（秒，已钳制）与累计时间 */
export class FrameTimer {
  constructor(maxDt = 1 / 20) {
    this.maxDt = maxDt;
    this.last = 0;
    this.elapsed = 0;
    this.dt = 0;
    this.scale = 1;          // 时间缩放（子弹时间等）
    this.frames = 0;
  }
  tick(nowMs) {
    if (!this.last) this.last = nowMs;
    let dt = (nowMs - this.last) / 1000;
    this.last = nowMs;
    if (dt > this.maxDt) dt = this.maxDt;
    if (dt < 0) dt = 0;
    this.dt = dt * this.scale;
    this.elapsed += this.dt;
    this.frames++;
    return this.dt;
  }
  reset() { this.last = 0; this.elapsed = 0; this.dt = 0; }
}

/**
 * 轻量粒子池：预分配数组，避免运行时 GC 抖动。
 * 每个粒子包含位置/速度/寿命/尺寸/颜色，渲染由外部读取。
 */
export class ParticlePool {
  constructor(capacity) {
    this.capacity = capacity;
    this.pos = new Float32Array(capacity * 3);
    this.vel = new Float32Array(capacity * 3);
    this.life = new Float32Array(capacity);
    this.maxLife = new Float32Array(capacity);
    this.size = new Float32Array(capacity);
    this.drag = new Float32Array(capacity);
    this.gravity = new Float32Array(capacity);
    this.kind = new Uint8Array(capacity);
    this.alive = new Uint8Array(capacity);
    this.count = 0;
    this.cursor = 0;
  }
  spawn(x, y, z, vx, vy, vz, life, size, kind = 0, drag = 1.6, gravity = 0) {
    const i = this.cursor;
    this.cursor = (this.cursor + 1) % this.capacity;
    if (!this.alive[i]) this.count++;
    this.alive[i] = 1;
    this.pos[i * 3] = x; this.pos[i * 3 + 1] = y; this.pos[i * 3 + 2] = z;
    this.vel[i * 3] = vx; this.vel[i * 3 + 1] = vy; this.vel[i * 3 + 2] = vz;
    this.life[i] = life; this.maxLife[i] = life; this.size[i] = size;
    this.kind[i] = kind; this.drag[i] = drag; this.gravity[i] = gravity;
    return i;
  }
  update(dt) {
    let live = 0;
    for (let i = 0; i < this.capacity; i++) {
      if (!this.alive[i]) continue;
      this.life[i] -= dt;
      if (this.life[i] <= 0) { this.alive[i] = 0; continue; }
      const k = i * 3;
      const d = Math.max(0, 1 - this.drag[i] * dt);
      this.vel[k] *= d; this.vel[k + 1] *= d; this.vel[k + 2] *= d;
      this.vel[k + 1] += this.gravity[i] * dt;
      this.pos[k] += this.vel[k] * dt;
      this.pos[k + 1] += this.vel[k + 1] * dt;
      this.pos[k + 2] += this.vel[k + 2] * dt;
      live++;
    }
    this.count = live;
  }
  clear() { this.alive.fill(0); this.count = 0; }
}

/** 数字格式化：12345 -> 12,345 */
export function fmtNum(n) {
  return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

/** 秒 -> mm:ss */
export function fmtTime(sec) {
  const s = Math.max(0, Math.floor(sec));
  const m = Math.floor(s / 60);
  return `${String(m).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;
}

/** 本地存储读写（带异常保护，隐私模式/禁用存储时不会崩） */
export const Store = {
  get(key, def) {
    try {
      const raw = localStorage.getItem(key);
      if (raw == null) return def;
      const v = JSON.parse(raw);
      return v == null ? def : v;
    } catch { return def; }
  },
  set(key, val) {
    try { localStorage.setItem(key, JSON.stringify(val)); return true; }
    catch { return false; }
  },
};

// =====================================================================
// 附加工具：持久化记录、方向转换、任务文本（供主程序/IDE 风格模块复用）
// =====================================================================

/**
 * 当前波次任务的完成进度（0..1），用于 HUD 进度条。
 *
 * 抽成纯函数是为了可自测：不同任务类型的「进度」语义并不一样，
 * 曾经统一按「剩余敌机比例」算，导致王牌波 / 对地突击波进度条长期显示 0%。
 *
 * @param {{type:string, flakTargets?:number, maxBaseLoss?:number, commander?:object}} m 任务对象
 * @param {{enemies:Array, base?:number, flakTargetsDown?:number}} st 战场快照
 */
export function missionProgress(m, st) {
  const enemies = st.enemies || [];
  const alive = enemies.filter((e) => e.alive).length;
  const total = Math.max(1, enemies.length);
  const killedRatio = clamp(1 - alive / total, 0, 1);
  if (!m) return 1;
  if (m.type === 'strike') {
    return clamp((st.flakTargetsDown || 0) / Math.max(1, m.flakTargets || 1), 0, 1);
  }
  if (m.type === 'ace') {
    const ace = m.commander;
    if (ace && ace.alive) {
      // 目标只有长机（护航机不强制清空），所以进度看长机血量；
      // 长机还活着就绝不显示 100%，避免玩家以为可以过波了。
      const hurt = 1 - clamp(ace.hp / Math.max(1, ace.maxHp), 0, 1);
      return clamp(Math.max(hurt, killedRatio) * 0.85, 0, 0.99);
    }
    return killedRatio;
  }
  if (m.type === 'escort' || m.type === 'defend') {
    const bombers = enemies.filter((e) => e.role === 'bomber');
    const aliveBombers = bombers.filter((e) => e.alive).length;
    const baseOk = clamp(1 - (100 - (st.base == null ? 100 : st.base)) / (m.maxBaseLoss || 34), 0, 1);
    if (m.type === 'defend') return killedRatio * 0.7 + baseOk * 0.3;
    return clamp(1 - aliveBombers / Math.max(1, bombers.length), 0, 1) * 0.6 + baseOk * 0.4;
  }
  return killedRatio;
}

/** 战斗记录存储（带容量上限与严重度排序） */
export class ScoreBoard {
  constructor(max = 20) { this.max = max; this.entries = []; }
  add(entry) {
    this.entries.push(entry);
    this.entries.sort((a, b) => b.score - a.score);
    if (this.entries.length > this.max) this.entries.length = this.max;
    return this.entries;
  }
  best() { return this.entries.length ? this.entries[0].score : 0; }
  avgAccuracy() {
    if (!this.entries.length) return 0;
    return this.entries.reduce((s, e) => s + (e.acc || 0), 0) / this.entries.length;
  }
  winRate() {
    if (!this.entries.length) return 0;
    return this.entries.filter((e) => e.win).length / this.entries.length;
  }
  /** 最高连杀记录 */
  bestCombo() {
    return this.entries.reduce((m, e) => Math.max(m, e.combo || 0), 0);
  }
}

/** 罗盘方位名（0°=北，顺时针 45° 一格） */
export const HEADING_LABELS = ['北', '东北', '东', '东南', '南', '西南', '西', '西北'];

/** 角度归一到 [0,360) */
export function normDeg(d) {
  const v = d % 360;
  return v < 0 ? v + 360 : v;
}

/** 航向角 -> 中文方位名（与 headingOf 共用同一张表，避免两处漂移） */
export function headingLabel(deg) {
  return HEADING_LABELS[Math.round(normDeg(deg) / 45) % 8];
}

/** 把机头朝向转换为罗盘航向角（0=北/-Z，顺时针） */
export function headingOf(fwd, out = { deg: 0, label: 'N' }) {
  const d = (Math.atan2(fwd.x, -fwd.z) * 180 / Math.PI + 360) % 360;
  out.deg = d;
  out.label = headingLabel(d);
  return out;
}

/** 米/秒 -> 公里/小时（HUD、结算、锁定面板统一口径） */
/**
 * HUD 显示模式归一化：auto（紧张时自动简化）/ full（始终完整）/ minimal（始终简化）。
 * 兼容旧存档里的布尔语义 'on' / 'off'，未知值一律回落到 auto。
 */
export function hudModeOf(v) {
  if (v === 'full' || v === 'minimal') return v;
  if (v === 'off') return 'minimal';
  if (v === 'on') return 'auto';
  return 'auto';
}

/** 按 H 键在三种 HUD 模式间循环 */
export function nextHudMode(v) {
  const m = hudModeOf(v);
  return m === 'auto' ? 'full' : m === 'full' ? 'minimal' : 'auto';
}

export const MPS_TO_KMH = 3.6;
export function mpsToKmh(v) { return v * MPS_TO_KMH; }

/**
 * 僚机相对长机的编队偏移（机体坐标系，-Z 为机头方向）。
 * 左侧僚机 side=-1，右侧僚机 side=+1；world.js 的候机机位也复用它，
 * 保证「地面停放的飞机」和「空中编队位置」不会各写一套魔数。
 */
export function computeFormationOffset(side, out = { x: 0, y: 0, z: 0 }) {
  out.x = side * 62;
  out.y = side * 12;
  out.z = 66;
  return out;
}

/** 任务简报文本（供任务栏与结算界面复用） */
export function missionBrief(mission, stats = {}) {
  if (!mission) return '—';
  switch (mission.type) {
    case 'strike': return `对地突击 · 摧毁 ${stats.flakDown ?? 0}/${mission.flakTargets ?? 1} 处阵地`;
    case 'escort': return `拦截轰炸机 · 基地完好度 ≥ ${100 - (mission.maxBaseLoss ?? 30)}%`;
    case 'ace': return `王牌对决 · ${mission.commander && mission.commander.alive ? '击落敌方长机' : '长机已消灭'}`;
    default: return `空域清扫 · 剩余 ${stats.alive ?? 0} 架敌机`;
  }
}

/** 把秒数转换为「X 分 Y 秒」战报文本 */
export function battleTime(sec) {
  const s = Math.max(0, Math.floor(sec));
  if (s < 60) return `${s} 秒`;
  return `${Math.floor(s / 60)} 分 ${s % 60} 秒`;
}

/** 命中率百分比（0 分母安全） */
export function accuracy(hits, fired) {
  if (!fired || fired <= 0) return 0;
  return clamp(Math.round((hits / fired) * 100), 0, 100);
}

/**
 * 预测投弹落点（HUD 的 CCIP 准星与地面高炮的提前量共用同一套弹道模型）。
 * 弹体出舱后：水平方向保持初速（无阻力），垂直方向仅受重力。
 * 与 weapons.js 中 _updateBombs 的积分方式一致，因此准星与实弹落点吻合。
 * @param {{x:number,y:number,z:number}} origin 投放点
 * @param {{x:number,y:number,z:number}} vel 初速度
 * @param {(x:number,z:number)=>number} heightAt 地形高度采样函数
 * @returns {{x:number,y:number,z:number,time:number,hit:boolean}} 落点（hit=false 表示在 maxTime 内未落地）
 */
export function predictImpact(origin, vel, heightAt, gravity = 21.582, maxTime = 45, step = 0.12) {
  const p = { x: origin.x, y: origin.y, z: origin.z };
  let vx = vel.x, vyy = vel.y, vz = vel.z;
  let t = 0;
  let prevAbove = p.y - heightAt(p.x, p.z) > 0;
  while (t < maxTime) {
    const h = Math.min(step, maxTime - t);
    const px = p.x, py = p.y, pz = p.z, pyv = vyy;
    t += h;
    // 恒重力下的精确位移：p += v·h − ½g·h²（与 weapons.js 的炸弹积分保持一致）
    p.x += vx * h;
    p.y += vyy * h - 0.5 * gravity * h * h;
    p.z += vz * h;
    vyy -= gravity * h;
    const above = p.y - heightAt(p.x, p.z) > 0;
    if (prevAbove && !above) {
      // 二分细化：抛体在一步内水平匀速、竖直匀加速，用解析式回退求交点
      let lo = t - h, hi = t;
      let bx = p.x, by = p.y, bz = p.z;
      for (let i = 0; i < 10; i++) {
        const tm = (lo + hi) / 2;
        const dt = tm - (t - h);
        const mx = px + vx * dt;
        const my = py + pyv * dt - 0.5 * gravity * dt * dt;
        const mz = pz + vz * dt;
        if (my - heightAt(mx, mz) > 0) { lo = tm; bx = mx; by = my; bz = mz; }
        else hi = tm;
      }
      return { x: bx, y: Math.max(by, 0), z: bz, time: lo, hit: true };
    }
    prevAbove = above;
    if (p.y <= 0) return { x: p.x, y: 0, z: p.z, time: t, hit: true };
  }
  return { x: p.x, y: p.y, z: p.z, time: t, hit: false };
}

/** 由载机位置/速度推算投弹初速（与 WeaponSystem.dropPlayerBomb 保持一致） */
export function bombLaunchVelocity(forward, velocity, out = { x: 0, y: 0, z: 0 }) {
  out.x = velocity.x + forward.x * 40;
  out.y = velocity.y + forward.y * 40;
  out.z = velocity.z + forward.z * 40;
  return out;
}

// =====================================================================
// 手柄（Gamepad API）映射
//   把「手握手柄」翻译成与键盘同一套语义：三轴操纵量 + 离散动作。
//   刻意写成纯函数：不碰 navigator，方便在自测里喂假手柄对象验证映射。
// =====================================================================

/** 手柄按钮编号（标准映射 Standard Gamepad） */
export const PAD_BUTTON = {
  A: 0, B: 1, X: 2, Y: 3,
  LB: 4, RB: 5, LT: 6, RT: 7,
  SELECT: 8, START: 9,
  LSTICK: 10, RSTICK: 11,
  DPAD_UP: 12, DPAD_DOWN: 13, DPAD_LEFT: 14, DPAD_RIGHT: 15,
};

/** 死区处理：小抖动归零，并把剩余行程重新拉伸到 0..1，避免手感发木 */
export function applyDeadzone(v, deadzone = 0.14) {
  const a = Math.abs(v);
  if (a <= deadzone) return 0;
  return Math.sign(v) * ((a - deadzone) / (1 - deadzone));
}

/**
 * 手柄 → 操纵量。与键盘口径一致：
 *   左摇杆 X 右为正 → 右滚；左摇杆 Y 下为正 → 推杆低头（与 ArrowDown 一致）
 *   右摇杆 X → 偏航（方向舵），LT/RT → 减速/加速
 * @param {{axes:number[], buttons:Array}} gp 标准映射手柄
 * @param {{deadzone?:number, invertPitch?:boolean}} opts
 * @returns {{pitch:number, roll:number, yaw:number, throttle:number|null}}
 */
export function gamepadAxes(gp, opts = {}) {
  const dz = opts.deadzone == null ? 0.14 : opts.deadzone;
  const ax = (gp && gp.axes) || [];
  const out = { pitch: 0, roll: 0, yaw: 0, throttle: null };
  if (!gp) return out;
  // 标准映射：0=左摇杆X 1=左摇杆Y 2=右摇杆X 3=右摇杆Y
  let roll = applyDeadzone(ax[0] || 0, dz);
  let pitch = applyDeadzone(ax[1] || 0, dz);
  let yaw = applyDeadzone(ax[2] || 0, dz);
  if (opts.invertPitch) pitch = -pitch;
  out.roll = clamp(roll, -1, 1);
  out.pitch = clamp(pitch, -1, 1);
  out.yaw = clamp(-yaw, -1, 1);   // 摇杆右拨 = 右偏航 = yaw 负（与 Q/E 一致）
  const btns = (gp.buttons) || [];
  const val = (i) => {
    const b = btns[i];
    if (b == null) return 0;
    return typeof b === 'number' ? b : (b.value || 0);
  };
  // 扳机做油门：LT 收油、RT 加油。两者都松时交回键盘控制（返回 null）
  const lt = val(PAD_BUTTON.LT), rt = val(PAD_BUTTON.RT);
  if (lt > 0.05 || rt > 0.05) out.throttle = clamp(1 - lt * 0.75 + rt * 0.4, 0.05, 1.15);
  return out;
}

/**
 * 手柄离散动作的「边沿检测」：按住只触发一次，松开后可再次触发。
 * @param {Array} buttons 手柄按钮数组
 * @param {Set<number>|number[]} held 上一帧按下的按钮集合（会被本函数原地更新）
 * @returns {{gun:boolean, missile:boolean, bomb:boolean, flare:boolean,
 *            cycleTarget:boolean, camera:boolean, pause:boolean}}
 */
export function gamepadButtons(buttons, held) {
  const set = held instanceof Set ? held : new Set(held || []);
  const btns = buttons || [];
  const now = (i) => {
    const b = btns[i];
    if (b == null) return false;
    return typeof b === 'number' ? b > 0.5 : (b.pressed || (b.value || 0) > 0.5);
  };
  const edge = (i) => {
    const on = now(i);
    const was = set.has(i);
    if (on && !was) set.add(i);
    else if (!on && was) set.delete(i);
    return on && !was;
  };
  const r = {
    gun: edge(PAD_BUTTON.RT) || edge(PAD_BUTTON.A),
    missile: edge(PAD_BUTTON.B),
    bomb: edge(PAD_BUTTON.X),
    flare: edge(PAD_BUTTON.Y),
    cycleTarget: edge(PAD_BUTTON.LB) || edge(PAD_BUTTON.DPAD_LEFT) || edge(PAD_BUTTON.DPAD_RIGHT),
    camera: edge(PAD_BUTTON.RB) || edge(PAD_BUTTON.RSTICK),
    pause: edge(PAD_BUTTON.START),
  };
  // 十字键上下：直接当油门微调（一次 10%），避免再引入一套状态
  r.throttleUp = edge(PAD_BUTTON.DPAD_UP);
  r.throttleDown = edge(PAD_BUTTON.DPAD_DOWN);
  return r;
}
