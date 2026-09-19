// =====================================================================
// main.js — 游戏主程序：状态机、输入、相机、锁定、HUD、雷达、任务流程
// =====================================================================
import * as THREE from 'three';
import { World, WORLD } from './world.js';
import { PlayerAircraft, EnemyAircraft, WingmanAircraft } from './aircraft.js';
import { WeaponSystem, GUN } from './weapons.js';
import { AudioSystem } from './audio.js';
import { ParticlePool, RNG, clamp, lerp, fmtNum, fmtTime, Store, approach,
         predictImpact, bombLaunchVelocity, mpsToKmh, headingOf, computeFormationOffset,
         hudModeOf, nextHudMode, missionProgress, gamepadAxes, gamepadButtons } from './utils.js';

const _v1 = new THREE.Vector3();
const _v2 = new THREE.Vector3();
const _v3 = new THREE.Vector3();

const STORE_KEY = 'skystrike.v1.save';
const MAX_WAVES = 8;

// 后勤：跑道着陆加油/补弹。燃油每秒消耗见 aircraft.js（普通 0.28 / 加力 1.35）
const LOGISTICS = {
  rearmAlt: 150,        // 触发高度（相对地面，米）
  rearmZoneX: 90,       // 跑道横向半宽
  rearmZoneZ: 1000,     // 跑道纵向半长
  rearmAlignDeg: 60,    // 与跑道轴向（±Z）的最大夹角
  fuelPerSec: 42,       // 地面补油速率（% / 秒）
};

// 任务类型（每一波一个目标）——不同的玩法摘要
const MISSION_TYPES = [
  { id: 'sweep', name: '空域清扫', brief: '清除空域内全部敌机' },
  { id: 'escort', name: '拦截轰炸机', brief: '阻止轰炸机抵近我方基地' },
  { id: 'ace', name: '王牌对决', brief: '击落敌方王牌机' },
  { id: 'strike', name: '对地突击', brief: '投弹摧毁敌防空阵地' },
  { id: 'defend', name: '机场防卫', brief: '阻止敌机攻击跑道与机库' },
];

// 难度参数：血量/携弹/敌方技能倍率
const DIFFICULTY = {
  easy: { name: '简单', hp: 150, skill: 0.78, enemyFire: 0.85, missiles: 10, bombs: 5, flares: 8 },
  normal: { name: '普通', hp: 100, skill: 1.0, enemyFire: 1.0, missiles: 8, bombs: 4, flares: 6 },
  hard: { name: '困难', hp: 78, skill: 1.18, enemyFire: 1.2, missiles: 6, bombs: 3, flares: 4 },
};

// =====================================================================
// 输入管理
// =====================================================================
class InputManager {
  constructor(lockTarget = document.body) {
    this.lockTarget = lockTarget;
    this.keys = new Set();
    this.justPressed = new Set();
    this.mouse = { x: 0, y: 0, dx: 0, dy: 0, left: false, right: false, wheel: 0 };
    this.locked = false;
    this.wantLock = false;
    this.axes = { pitch: 0, roll: 0, yaw: 0 };
    this.gamepad = null;        // 当前帧检测到的手柄（无则 null）
    this.padThrottle = null;    // 手柄扳机给出的油门（null = 交给键盘）
    this.invertPadPitch = false;
    this._padHeld = null;       // 上一帧按下的手柄按钮（边沿检测用）

    const down = (e) => {
      if (e.repeat) return;
      const k = e.key;
      this.keys.add(k);
      this.justPressed.add(k);
      if (['Tab', ' ', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(k)) e.preventDefault();
    };
    const up = (e) => { this.keys.delete(e.key); };
    window.addEventListener('keydown', down);
    window.addEventListener('keyup', up);
    window.addEventListener('blur', () => { this.keys.clear(); this.mouse.left = false; this.mouse.right = false; });

    // 鼠标：始终监听 window，指针锁定时用 movement 增量做自由视角
    window.addEventListener('mousemove', (e) => {
      this.mouse.x = e.clientX; this.mouse.y = e.clientY;
      if (this.locked) {
        this.mouse.dx += e.movementX || 0;
        this.mouse.dy += e.movementY || 0;
      }
    });
    window.addEventListener('mousedown', (e) => {
      if (e.button === 0) this.mouse.left = true;
      if (e.button === 2) this.mouse.right = true;
      // 仅当没点到 UI 按钮时才尝试锁定指针
      const onUI = e.target && e.target.closest && e.target.closest('button, a, input, .dialog, .overlay');
      if (e.button === 0 && !this.locked && this.wantLock && !onUI) this.requestLock();
    });
    window.addEventListener('mouseup', (e) => {
      if (e.button === 0) this.mouse.left = false;
      if (e.button === 2) this.mouse.right = false;
    });
    window.addEventListener('contextmenu', (e) => e.preventDefault());
    document.addEventListener('pointerlockchange', () => {
      this.locked = document.pointerLockElement === this.lockTarget;
    });
  }

  requestLock() {
    const t = this.lockTarget;
    if (t && t.requestPointerLock) { try { t.requestPointerLock(); } catch { /* 忽略重复请求 */ } }
  }

  /** 轮询手柄（Gamepad API）。没有手柄时返回 null，调用方零成本跳过。 */
  pollGamepad() {
    if (!navigator.getGamepads) return null;
    const list = navigator.getGamepads();
    for (const gp of list) if (gp && gp.connected) return gp;
    return null;
  }

  down(k) { return this.keys.has(k); }
  pressed(k) { return this.justPressed.has(k); }
  endFrame() { this.justPressed.clear(); this.mouse.dx = 0; this.mouse.dy = 0; this.mouse.wheel = 0; }
  exitLock() { if (this.locked && document.exitPointerLock) document.exitPointerLock(); }

  /** 轮询：把按键映射为三轴操纵量 */
  readAxes() {
    const k = this.keys;
    let pitch = 0, roll = 0, yaw = 0;
    if (k.has('w') || k.has('W') || k.has('ArrowUp')) pitch -= 1;
    if (k.has('s') || k.has('S') || k.has('ArrowDown')) pitch += 1;
    if (k.has('a') || k.has('A') || k.has('ArrowLeft')) roll -= 1;
    if (k.has('d') || k.has('D') || k.has('ArrowRight')) roll += 1;
    if (k.has('q') || k.has('Q')) yaw += 1;
    if (k.has('e') || k.has('E')) yaw -= 1;
    // 手柄：与键盘叠加（谁先动谁生效，两者同时给输入时取和并钳制）
    const gp = this.pollGamepad();
    this.gamepad = gp;
    if (gp) {
      const ga = gamepadAxes(gp, { invertPitch: this.invertPadPitch });
      pitch = clamp(pitch + ga.pitch, -1, 1);
      roll = clamp(roll + ga.roll, -1, 1);
      yaw = clamp(yaw + ga.yaw, -1, 1);
      if (ga.throttle != null) this.padThrottle = ga.throttle;
    } else {
      this.padThrottle = null;
    }
    this.axes.pitch = pitch; this.axes.roll = roll; this.axes.yaw = yaw;
    return this.axes;
  }

  /** 手柄离散动作（按下沿）。每帧调用一次，内部记录上一帧状态。 */
  readGamepadActions() {
    if (!this.gamepad) {
      if (this._padHeld) this._padHeld.clear();
      return null;
    }
    return gamepadButtons(this.gamepad.buttons, this._padHeld || (this._padHeld = new Set()));
  }
}

// =====================================================================
// 主游戏
// =====================================================================
class SkyStrike {
  constructor() {
    this.el = {
      mount: document.getElementById('mount'),
      loading: document.getElementById('loading'),
      loadFill: document.getElementById('loadFill'),
      loadTip: document.getElementById('loadTip'),
      menu: document.getElementById('menu'),
      hud: document.getElementById('hud'),
      hmd: document.getElementById('hmd'),
      radar: document.getElementById('radar'),
      pause: document.getElementById('pause'),
      help: document.getElementById('help'),
      hangar: document.getElementById('hangar'),
      board: document.getElementById('board'),
      boardList: document.getElementById('boardList'),
      result: document.getElementById('result'),
      resultTitle: document.getElementById('resultTitle'),
      resultSub: document.getElementById('resultSub'),
      toasts: document.getElementById('toasts'),
      killFeed: document.getElementById('killFeed'),
      centerMsg: document.getElementById('centerMsg'),
      combo: document.getElementById('combo'),
      warn: document.getElementById('warn'),
      damageFlash: document.getElementById('damageFlash'),
      // 状态
      hpBar: document.getElementById('hpBar'), hpTxt: document.getElementById('hpTxt'),
      thrBar: document.getElementById('thrBar'), thrTxt: document.getElementById('thrTxt'),
      altTxt: document.getElementById('altTxt'), spdTxt: document.getElementById('spdTxt'),
      missionTxt: document.getElementById('missionTxt'), enemyTxt: document.getElementById('enemyTxt'),
      timeTxt: document.getElementById('timeTxt'), scoreTxt: document.getElementById('scoreTxt'),
      gunTxt: document.getElementById('gunTxt'), missileTxt: document.getElementById('missileTxt'),
      bombTxt: document.getElementById('bombTxt'), flareTxt: document.getElementById('flareTxt'),
      weaponTxt: document.getElementById('weaponTxt'),
      tgtType: document.getElementById('tgtType'), tgtDist: document.getElementById('tgtDist'),
      tgtClosure: document.getElementById('tgtClosure'), tgtHp: document.getElementById('tgtHp'),
      bestScore: document.getElementById('bestScore'), totalKills: document.getElementById('totalKills'),
      totalSorties: document.getElementById('totalSorties'),
      resScore: document.getElementById('resScore'), resKills: document.getElementById('resKills'),
      resAcc: document.getElementById('resAcc'), resTime: document.getElementById('resTime'),
      resCombo: document.getElementById('resCombo'), resHp: document.getElementById('resHp'),
      resMsl: document.getElementById('resMsl'), resBase: document.getElementById('resBase'),
      resAlly: document.getElementById('resAlly'), resFacility: document.getElementById('resFacility'),
      fuelBar: document.getElementById('fuelBar'), fuelTxt: document.getElementById('fuelTxt'),
      baseBar: document.getElementById('baseBar'), baseTxt: document.getElementById('baseTxt'),
      rearmTip: document.getElementById('rearmTip'),
      objTxt: document.getElementById('objTxt'), objBar: document.getElementById('objBar'),
      objHint: document.getElementById('objHint'),
      gunHeatBar: document.getElementById('gunHeatBar'),
      allyTxt: document.getElementById('allyTxt'),
      allyOrderTxt: document.getElementById('allyOrderTxt'),
      bossBarWrap: document.getElementById('bossBarWrap'), bossName: document.getElementById('bossName'),
      bossBar: document.getElementById('bossBar'),
      warnPanel: document.getElementById('warnPanel'), warnRwr: document.getElementById('warnRwr'),
      warnStall: document.getElementById('warnStall'), warnGear: document.getElementById('warnGear'),
      warnFlak: document.getElementById('warnFlak'), warnFuel: document.getElementById('warnFuel'),
      warnBase: document.getElementById('warnBase'),
    };

    this.save = Store.get(STORE_KEY, { best: 0, totalKills: 0, sorties: 0, records: [], settings: {} });
    this.settings = Object.assign({ quality: 'mid', audio: 'on', control: 'normal', difficulty: 'normal', wingman: 'on', units: 'metric', hud: 'auto' }, this.save.settings || {});
    // 旧存档用的是 on/off 布尔语义，这里迁移到 auto/full/minimal 三档
    if (this.settings.hud === 'on') this.settings.hud = 'auto';
    else if (this.settings.hud === 'off') this.settings.hud = 'minimal';

    this.state = 'loading';
    this.input = new InputManager(document.body);
    this.audio = new AudioSystem();
    this.rng = new RNG(24680);
    this.particles = null;
    this.world = null;
    this.combat = null;
    this.player = null;
    this.enemies = [];
    this.allies = [];                 // 僚机
    this.mission = null;              // 当前波次任务
    this.waveIntro = 0;               // 波次开场提示计时
    this.lockedTarget = null;
    this.lockProgress = 0;
    this.cam = { pos: new THREE.Vector3(), quat: new THREE.Quaternion(), lookOff: new THREE.Vector2(), fov: 62 };
    this.hud2d = { ctx: null, w: 0, h: 0, dpr: 1 };
    this.stats = {
      score: 0, kills: 0, shots: 0, hits: 0, combo: 0, maxCombo: 0, comboTimer: 0,
      time: 0, wave: 0, base: 100, alliesLost: 0, flakDown: 0,
      facilitiesDown: 0,     // 本方地面设施（油罐/弹药库/机库）被摧毁数量
      rearmCount: 0,         // 着陆补弹次数
      fuelUsed: 0,           // 累计燃油消耗（%）
    };
    this.rearm = { active: false, done: false, progress: 0 };   // 着陆补弹状态
    this.toastTimers = [];
    this.killFeedTimers = [];

    this._bindUI();
    this._refreshSaveUI();
    this._syncHudSegment();
    requestAnimationFrame((t) => this._boot(t));
  }

  // ===================================================================
  // 启动 / 世界初始化
  // ===================================================================
  async _boot(t) {
    const steps = [
      ['正在初始化渲染器…', () => { this.world = new World(this.el.mount, this.settings.quality); }],
      ['正在生成地形与云层…', () => new Promise((r) => setTimeout(r, 30))],
      ['正在装载粒子与特效系统…', () => { this.particles = new ParticlePool(this.settings.quality === 'low' ? 900 : 2200); }],
      ['正在通电武器系统…', () => { this.combat = new WeaponSystem(this.world.scene, this.world, this.particles, this.audio); }],
      ['正在校准 HUD…', () => { this._resizeHUD(); }],
      ['准备完毕，等待起飞指令', () => new Promise((r) => setTimeout(r, 120))],
    ];
    for (let i = 0; i < steps.length; i++) {
      this.el.loadTip.textContent = steps[i][0];
      this.el.loadFill.style.width = `${Math.round(((i + 0.6) / steps.length) * 100)}%`;
      try { await steps[i][1](); }
      catch (err) {
        this.el.loadTip.innerHTML = `⚠ 初始化失败：${err && err.message ? err.message : err}`;
        console.error(err);
        return;
      }
      await new Promise((r) => setTimeout(r, 20));
    }
    this.el.loadFill.style.width = '100%';
    setTimeout(() => this._setState('menu'), 220);

    this.lastTime = 0;
    this.loop = (now) => {
      requestAnimationFrame(this.loop);
      this._frame(now);
    };
    requestAnimationFrame(this.loop);
  }

  _setState(s, data) {
    this.state = s;
    const show = (el, on) => el && el.classList.toggle('hidden', !on);
    show(this.el.loading, s === 'loading');
    show(this.el.menu, s === 'menu');
    show(this.el.hud, s === 'playing' || s === 'paused');
    show(this.el.pause, s === 'paused');
    show(this.el.result, s === 'result');
    if (s === 'menu') { this.input.wantLock = false; this.input.exitLock(); this._refreshSaveUI(); }
    if (s === 'playing') this.input.wantLock = true;
    if (data && data.title) {
      this.el.resultTitle.textContent = data.title;
      this.el.resultSub.textContent = data.sub;
    }
  }

  // ===================================================================
  // UI 绑定
  // ===================================================================
  _bindUI() {
    const on = (id, fn, needAudio = true) => {
      const b = document.getElementById(id);
      if (!b) return;
      b.onclick = () => {
        if (needAudio) this.audio.init();
        this.audio.uiClick();
        fn();
      };
    };
    on('btnStart', () => this.startMission());
    on('btnHelp', () => this.el.help.classList.remove('hidden'), false);
    on('btnHelpClose', () => this.el.help.classList.add('hidden'));
    on('btnHangar', () => this.el.hangar.classList.remove('hidden'), false);
    on('btnHangarClose', () => this.el.hangar.classList.add('hidden'));
    on('btnBoard', () => { this._renderBoard(); this.el.board.classList.remove('hidden'); }, false);
    on('btnBoardClose', () => this.el.board.classList.add('hidden'));
    on('btnResume', () => this._setState('playing'));
    on('btnRestartPause', () => this.startMission());
    on('btnQuit', () => this._setState('menu'));
    on('btnAgain', () => this.startMission());
    on('btnResultMenu', () => this._setState('menu'));

    // 分段选择器
    const seg = (id, key, apply) => {
      const box = document.getElementById(id);
      if (!box) return;
      box.querySelectorAll('button').forEach((b) => {
        b.onclick = () => {
          box.querySelectorAll('button').forEach((x) => x.classList.remove('on'));
          b.classList.add('on');
          this.settings[key] = b.dataset.v;
          this._persistSettings();
          if (apply) apply(b.dataset.v);
          this.audio.uiClick();
        };
        if (b.dataset.v === this.settings[key]) {
          box.querySelectorAll('button').forEach((x) => x.classList.remove('on'));
          b.classList.add('on');
        }
      });
    };
    seg('segQuality', 'quality');
    seg('segAudio', 'audio', (v) => this.audio.setEnabled(v === 'on'));
    seg('segCtrl', 'control');
    seg('segDiff', 'difficulty');
    seg('segWing', 'wingman');
    seg('segUnits', 'units');
    seg('segHud', 'hud', () => this._syncHudSegment());

    // F3 调试面板（默认关闭，不占用布局）
    const dbg = document.createElement('pre');
    dbg.id = 'debugPanel';
    dbg.className = 'hidden';
    document.body.appendChild(dbg);
    this.el.debug = dbg;

    this.audio.setEnabled(this.settings.audio === 'on');
  }

  /** F3：开关调试面板（简单移动平均帧率 + 实体计数） */
  _toggleDebug() {
    this.debug = !this.debug;
    if (this.el.debug) this.el.debug.classList.toggle('hidden', !this.debug);
    this._toast(this.debug ? '调试信息：开（F3 关闭）' : '调试信息：关', 1000);
  }

  _updateDebug() {
    const el = this.el.debug;
    if (!this.debug || !el) return;
    const inst = this.lastDt > 0 ? 1 / this.lastDt : 0;
    this._fpsAvg = this._fpsAvg === undefined ? inst : this._fpsAvg * 0.92 + inst * 0.08;
    let nodes = 0;
    if (this.world) this.world.scene.traverse(() => { nodes++; });
    const c = this.combat;
    el.textContent = [
      `FPS ${this._fpsAvg.toFixed(0)} (瞬时 ${inst.toFixed(0)})  dt ${(this.lastDt * 1000).toFixed(1)}ms`,
      `状态 ${this.state} 波次 ${this.stats.wave}/${MAX_WAVES} 任务 ${this.mission ? this.mission.type : '-'}`,
      `敌机 ${c ? c.enemies.filter((e) => e.alive).length : 0}/${c ? c.enemies.length : 0}` +
        ` 僚机 ${c ? c.allies.filter((a) => a.alive).length : 0}/${c ? c.allies.length : 0}`,
      `弹药 导弹${c ? c.missiles.length : 0} 炸弹${c ? c.bombs.length : 0} 高炮${c ? c.shells.length : 0}` +
        ` 曳光${c ? c.tracers.length : 0} 爆炸${c ? c.explosions.length : 0}`,
      `粒子 ${this.particles ? this.particles.count + '/' + this.particles.max : 0} 场景节点 ${nodes}`,
      `得分 ${this.stats.score} 时间 ${fmtTime(this.stats.time)} 机场 ${this.stats.base}%`,
      this.player ? `机体 ${Math.round(this.player.hp)}/${Math.round(this.player.maxHp)} 油 ${Math.round(this.player.fuel)}%` +
        ` 速度 ${Math.round(this.player.speed)} m/s` : '无玩家实体',
    ].join('\n');
  }

  /** 归一化 HUD 模式：auto / full / minimal（纯函数在 utils 里，便于自测） */
  _hudMode() { return hudModeOf(this.settings.hud); }

  /** 机库「HUD」分段按钮与当前设置同步（快捷键改设置时也要同步 UI） */
  _syncHudSegment() {
    const box = document.getElementById('segHud');
    if (!box) return;
    box.querySelectorAll('button').forEach((b) => b.classList.toggle('on', b.dataset.v === this._hudMode()));
  }

  _persistSettings() {
    this.save.settings = this.settings;
    Store.set(STORE_KEY, this.save);
  }

  // ===================================================================
  // 单位显示：公制（米/公里）与航空制（英尺/节/海里）
  // 内部物理量一律用 SI，只在渲染边界换算，避免两套数值互相污染
  // ===================================================================
  get imperial() { return this.settings.units === 'imperial'; }

  /** 高度：m / ft */
  fmtAlt(meters) {
    return this.imperial
      ? `${fmtNum(meters * 3.28084)} ft`
      : `${Math.round(meters)} m`;
  }

  /** 距离：m→km / m→nm */
  fmtDist(meters) {
    if (this.imperial) {
      const nm = meters / 1852;
      return nm >= 1 ? `${nm.toFixed(2)} nm` : `${fmtNum(meters * 3.28084)} ft`;
    }
    return meters >= 1000 ? `${(meters / 1000).toFixed(2)} km` : `${Math.round(meters)} m`;
  }

  /** 空速：km/h / kt */
  fmtSpeed(mps) {
    return this.imperial
      ? `${Math.round(mps * 1.94384)} kt`
      : `${Math.round(mpsToKmh(mps))} km/h`;
  }

  /** 接近率：带正负号的 km/h / kt（+ 表示接近） */
  fmtClosure(mps) {
    const sign = mps > 0 ? '+' : '';
    return this.imperial
      ? `${sign}${Math.round(mps * 1.94384)} kt`
      : `${sign}${Math.round(mpsToKmh(mps))} km/h`;
  }

  _refreshSaveUI() {
    this.el.bestScore.textContent = fmtNum(this.save.best || 0);
    this.el.totalKills.textContent = fmtNum(this.save.totalKills || 0);
    this.el.totalSorties.textContent = fmtNum(this.save.sorties || 0);
  }

  _renderBoard() {
    const list = this.el.boardList;
    const recs = (this.save.records || []).slice(0, 12);
    if (!recs.length) { list.innerHTML = '<div class="empty">暂无战绩，出击一次试试</div>'; return; }
    list.innerHTML = recs.map((r) => {
      const cls = r.win ? 'win' : 'lose';
      return `<div class="brow ${cls}">
        <i>${new Date(r.date).toLocaleString('zh-CN', { hour12: false })}</i>
        <span>击落 ${r.kills} · 命中 ${r.acc}%</span>
        <b>${fmtNum(r.score)} 分</b>
      </div>`;
    }).join('');
  }

  // ===================================================================
  // 任务流程
  // ===================================================================
  startMission() {
    this.audio.init();
    this.audio.startEngine();
    this._cleanupMission();

    // 重置统计
    Object.assign(this.stats, {
      score: 0, kills: 0, shots: 0, hits: 0, combo: 0, maxCombo: 0, comboTimer: 0,
      time: 0, wave: 0, base: 100, alliesLost: 0, flakDown: 0,
      facilitiesDown: 0, rearmCount: 0, fuelUsed: 0,
    });
    this.rearm = { active: false, done: false, progress: 0 };
    this.combat.clear();
    this.el.killFeed.innerHTML = '';
    this.el.centerMsg.classList.add('hidden');
    this.el.combo.classList.add('hidden');
    this.el.bossBarWrap.classList.add('hidden');

    const diff = DIFFICULTY[this.settings.difficulty] || DIFFICULTY.normal;

    // 玩家从基地跑道起飞
    const startAlt = this.world.heightAt(0, 700) + 260;
    this.player = new PlayerAircraft(this.world.scene, { hp: diff.hp });
    this.player.position.set(0, startAlt, 700);
    this.player.velocity.set(0, 0, -170);
    this.player.speed = 170;
    this.player.quaternion.setFromEuler(new THREE.Euler(0, 0, 0));   // 机头朝 -Z 出发
    this.player.syncAttitude();
    this.player.weapons.missiles = diff.missiles;
    this.player.weapons.bombs = diff.bombs;
    this.player.weapons.flares = diff.flares;
    this.player.syncMesh();
    this.enemies = [];
    this.allies = [];

    // 僚机编队（可在机库关闭）
    if (this.settings.wingman === 'on') {
      for (const side of [-1, 1]) {
        const off = computeFormationOffset(side);
        const w = new WingmanAircraft(this.world.scene, {
          side, seed: side + 2,
          formationOffset: new THREE.Vector3(off.x, off.y, off.z),
        });
        w.position.copy(this.player.position).add(new THREE.Vector3(off.x, off.y, off.z));
        w.velocity.copy(this.player.velocity);
        w.speed = this.player.speed;
        w.syncAttitude();
        w.callsign = side < 0 ? '僚机 1' : '僚机 2';
        w.order = this.allyOrder || 'follow';
        w.syncMesh();
        this.allies.push(w);
      }
    }

    this.combat.attach(this.player, this.enemies, this.allies);
    // 高炮伤害随难度缩放（简单 0.75 / 普通 1.0 / 困难 1.2）
    this.combat.flakDamageScale = diff.enemyFire;
    this.combat.onKill = (victim, credit) => this._onKill(victim, credit);
    this.combat.onPlayerHit = (dmg) => this._flashDamage();
    this.combat.onAlliedKill = (victim, ally) => this._onAlliedKill(victim, ally);
    this.combat.onAllyDown = (ally) => this._onAllyDown(ally);
    this.combat.onExplosion = (pos, credit) => {
      if (credit === 'player' || credit === 'friendly') this._damageFlak(pos, 130);
    };
    this.combat.onOverheat = () => this._toast('机炮过热！松开扳机等待冷却', 1400);
    // 弹药落地：只结算「敌方弹药砸本方机场」的损失（玩家炸弹已由 onExplosion 走 _damageFlak）
    this.combat.onGroundImpact = (pos, owner, radius) => {
      if (owner !== 'enemy') return;
      this._landSuppliesNear(pos, radius);
    };
    this.combat.onFlakHit = (site, dmg, nearby) => {
      this._radioDamage += dmg;
      if (this._radioDamage >= 12) {
        this._radioDamage = 0;
        this._toast(`⚠ 高炮命中！附近 ${nearby} 处阵地 · 爬升到 2100m 以上脱离`, 1700);
      }
    };

    this.lockedTarget = null;
    this.lockProgress = 0;
    this.cam.lookOff.set(0, 0);
    this._flakBusy = false;
    this._radioDamage = 0;
    this._flakHi = false;
    // 高炮阵地复位（上一局被打掉的重新上线）+ 地面设施修复
    for (const s of this.world.flakSites || []) {
      s.visible = true;
      s.userData.alive = true;
      s.userData.hp = 2;
      s.userData.cool = this.rng.range(0.4, 1.6);
    }
    for (const f of this.world.groundFacilities || []) {
      f.alive = true;
      f.hp = f.kind === 'hangar' ? 3 : 2;
      f.group.visible = true;
    }
    for (const c of this.world.crashSites || []) { c.life = 0; c.group.visible = false; }
    // 停机坪候机复位（上一局被炸掉的重新出现）
    for (const j of this.world.parkedJets || []) { j.alive = true; j.mesh.visible = true; }

    this._setState('playing');
    this._spawnWave(1);
    this.audio.lock();
  }

  _cleanupMission() {
    for (const e of this.enemies) e.dispose();
    this.enemies.length = 0;
    for (const a of this.allies) a.dispose();
    this.allies.length = 0;
    if (this.player) { this.player.dispose(); this.player = null; }
    this.particles.clear();
    this.combat.clear();
    this.mission = null;
  }

  // ===================================================================
  // 后勤：跑道着陆加油 / 补弹
  // ===================================================================
  /** 机库「单位」分段按钮与当前设置同步（快捷键改设置时也要同步 UI） */
  _syncUnitSegment() {
    const box = document.getElementById('segUnits');
    if (!box) return;
    box.querySelectorAll('button').forEach((b) => {
      b.classList.toggle('on', b.dataset.v === this.settings.units);
    });
  }

  /** 玩家当前是否处于「可补给」状态（跑道上方低空 + 大致对正跑道） */
  rearmState(p = this.player) {
    const st = { inZone: false, aligned: false, ready: false, alt: 0 };
    if (!p || !p.alive) return st;
    const alt = p.position.y - this.world.heightAt(p.position.x, p.position.z);
    st.alt = alt;
    st.inZone = Math.abs(p.position.x) <= LOGISTICS.rearmZoneX &&
                Math.abs(p.position.z) <= LOGISTICS.rearmZoneZ &&
                alt >= 0 && alt <= LOGISTICS.rearmAlt && p.velocity.y < 12;
    const fwd = p.getForward(_v1);
    // 跑道沿 Z 轴，机头对准 ±Z 即可（无论朝北还是朝南进场）
    const alignDeg = Math.acos(clamp(Math.abs(fwd.z), 0, 1)) * 180 / Math.PI;
    st.aligned = alignDeg <= LOGISTICS.rearmAlignDeg;
    st.ready = st.inZone && st.aligned;
    return st;
  }

  /** 每帧推进补给（由 _simulate 调用，dt 已钳制） */
  _updateRearm(dt) {
    const p = this.player;
    const st = this.rearmState(p);
    const diff = DIFFICULTY[this.settings.difficulty] || DIFFICULTY.normal;
    const needFuel = p.fuel < 99.5;
    const needAmmo = p.weapons.missiles < diff.missiles ||
                     p.weapons.bombs < diff.bombs ||
                     p.weapons.flares < diff.flares;
    const canUse = st.ready && (needFuel || needAmmo);
    if (!canUse) {
      if (this.rearm.active) {
        this.rearm.active = false;
        this._toast('已离开跑道 · 补给中断', 1200);
      }
      this.rearm.progress = 0;
      return;
    }
    if (!this.rearm.active) {
      this.rearm.active = true;
      this._toast('跑道补给中 · 保持贴地对正', 1600);
      this.audio.ping();
    }
    const before = p.fuel;
    p.fuel = clamp(p.fuel + LOGISTICS.fuelPerSec * dt, 0, 100);
    this.stats.fuelUsed += Math.max(0, p.fuel - before) * 0;
    this.rearm.progress = clamp(1 - p.fuel / 100, 0, 1);
    if (needAmmo) {
      p.weapons.missiles = Math.min(diff.missiles, p.weapons.missiles + 1);
      p.weapons.bombs = Math.min(diff.bombs, p.weapons.bombs + 1);
      p.weapons.flares = Math.min(diff.flares, p.weapons.flares + 1);
    }
    if (p.fuel >= 99.5 && !needAmmo) {
      this.rearm.done = true;
      this.stats.rearmCount++;
      this._killFeed('跑道补给完成 · 油弹已满');
      this.audio.radio();
    }
  }

  /** 按住 R 且满足条件时立即补一次（键按住会逐帧推进，这里只做提示） */
  _tryRearm() {
    const st = this.rearmState();
    if (st.ready) { this._updateRearm(this.lastDt || 0); return; }
    if (this.rearm._hintCool > 0) { this.rearm._hintCool -= this.lastDt || 0; return; }
    this.rearm._hintCool = 4;
    const p = this.player;
    if (!p || !p.alive) return;
    if (Math.abs(p.position.x) > LOGISTICS.rearmZoneX || Math.abs(p.position.z) > LOGISTICS.rearmZoneZ) {
      this._toast('补给需返回机场跑道上方（跑道沿南北向）', 1500);
    } else {
      this._toast(`对正跑道并降低高度（当前 ${Math.round(st.alt)}m）`, 1500);
    }
  }

  // ===================================================================
  // 波次任务生成：每一波有明确目标（清空 / 拦截 / 王牌 / 对地）
  // ===================================================================
  _makeMission(n) {
    const type = MISSION_TYPES[(n - 1) % MISSION_TYPES.length];
    const diff = DIFFICULTY[this.settings.difficulty] || DIFFICULTY.normal;
    const skill = clamp((0.5 + n * 0.055) * diff.skill, 0.45, 0.98);
    const m = { type: type.id, name: type.name, brief: type.brief, skill, wave: n, spawned: 0, waveTag: n };
    if (type.id === 'escort') m.maxBaseLoss = 34;
    if (type.id === 'strike') m.flakTargets = clamp(1 + Math.floor(n / 3), 1, 3);
    if (type.id === 'defend') m.maxBaseLoss = 30;    // 机场防卫：与拦截同口径评估完好度
    return m;
  }

  _spawnWave(n) {
    this.stats.wave = n;
    this.mission = this._makeMission(n);
    const m = this.mission;
    const count = clamp(3 + Math.floor(n * 0.8), 3, 8);
    const spawnAlt = () => this.rng.range(700, 2600);

    const spawnFighter = (opts = {}) => {
      const skill = clamp(m.skill + (opts.skillBias || 0), 0.45, 0.99);
      const e = new EnemyAircraft(this.world.scene, {
        variant: opts.variant || 'fighter', skill,
        seed: Math.floor(this.rng.next() * 1e9),
        targetBase: !!opts.targetBase, ...opts.extra,
      });
      const a = opts.angle !== undefined ? opts.angle : this.rng.range(0, Math.PI * 2);
      const r = opts.r !== undefined ? opts.r : this.rng.range(1500, 2600);
      e.position.set(
        this.player.position.x + Math.cos(a) * r,
        opts.alt !== undefined ? opts.alt : spawnAlt(),
        this.player.position.z + Math.sin(a) * r
      );
      if (opts.velDir) {
        e.velocity.copy(opts.velDir).multiplyScalar(opts.speed || 220);
        e.quaternion.setFromUnitVectors(new THREE.Vector3(0, 0, -1), opts.velDir.clone().normalize());
      } else {
        const toPlayer = this.player.position.clone().sub(e.position).normalize();
        e.velocity.copy(toPlayer).multiplyScalar(opts.speed || 220);
        e.quaternion.setFromUnitVectors(new THREE.Vector3(0, 0, -1), toPlayer);
      }
      e.speed = opts.speed || 220;
      e.syncAttitude();
      e.syncMesh();
      e.homeBase.copy(e.position);
      e.mission = m;
      e.role = opts.role || 'escort';
      this.enemies.push(e);
      m.spawned++;
      return e;
    };

    // 基础战斗机群（数量随波次与难度增长）
    for (let i = 0; i < count; i++) spawnFighter({});

    if (m.type === 'ace') {
      // 王牌波：一架高血量指挥官 + 少量护航
      const ace = new EnemyAircraft(this.world.scene, {
        variant: 'ace', skill: clamp(m.skill + 0.08, 0.6, 0.99),
        seed: Math.floor(this.rng.next() * 1e9),
      });
      const hpBoost = 1 + (n - 1) * 0.12;
      ace.hp = Math.round(ace.hp * hpBoost * 1.25);
      ace.maxHp = ace.hp;
      const a = this.rng.range(0, Math.PI * 2);
      ace.position.set(this.player.position.x + Math.cos(a) * 2400, spawnAlt(), this.player.position.z + Math.sin(a) * 2400);
      const toPlayer = this.player.position.clone().sub(ace.position).normalize();
      ace.velocity.copy(toPlayer).multiplyScalar(280);
      ace.quaternion.setFromUnitVectors(new THREE.Vector3(0, 0, -1), toPlayer);
      ace.speed = 280;
      ace.syncAttitude();
      ace.syncMesh();
      ace.mission = m;
      ace.role = 'commander';
      ace.callsign = `「赤隼」${n}-01`;
      this.enemies.push(ace);
      m.commander = ace;
      m.spawned++;
      this.el.bossBarWrap.classList.remove('hidden');
      this.el.bossName.textContent = ace.callsign;
      this._toast('⚠ 敌方王牌机长机出现');
    } else if (m.type === 'escort') {
      // 拦截波：轰炸机编队奔赴基地
      const bn = clamp(1 + Math.floor(n / 3), 1, 3);
      for (let i = 0; i < bn; i++) {
        const e = new EnemyAircraft(this.world.scene, { variant: 'bomber', seed: Math.floor(this.rng.next() * 1e9) });
        const a = this.rng.range(0, Math.PI * 2);
        const r = 2200;
        e.position.set(Math.cos(a) * r, this.rng.range(1200, 1900), Math.sin(a) * r);
        const dirToBase = new THREE.Vector3(0, 0, 0).sub(e.position).setY(0).normalize();
        e.velocity.copy(dirToBase).multiplyScalar(175);
        e.quaternion.setFromUnitVectors(new THREE.Vector3(0, 0, -1), dirToBase);
        e.speed = 175;
        e.cruiseAlt = e.position.y;
        e.syncAttitude();
        e.syncMesh();
        e.mission = m;
        e.role = 'bomber';
        this.enemies.push(e);
        m.spawned++;
      }
      this._toast(`⚠ 敌方轰炸机编队 ${bn} 架正在逼近基地`);
    } else if (m.type === 'defend') {
      // 机场防卫：敌机从各个方向直扑机场，优先打跑道/机库
      const dn = clamp(2 + Math.floor(n / 2), 2, 6);
      for (let i = 0; i < dn; i++) {
        const a = (i / dn) * Math.PI * 2 + 0.4;
        spawnFighter({
          angle: a, r: this.rng.range(1800, 2600), speed: 230,
          role: 'strike', targetBase: true,
        });
      }
      this.el.objHint.textContent = '目标：击落全部来袭敌机（保护机场完好度）';
      this._toast(`⚠ 机场防卫 · ${dn} 架敌机扑向机场`, 2200);
    } else if (m.type === 'strike') {
      // 对地突击：指定若干处高炮阵地为摧毁目标，其余仍有威胁但不计入任务进度
      const sites = this.world.flakSites || [];
      const pick = this.rng.int(0, Math.max(0, sites.length - 1));
      for (let i = 0; i < sites.length; i++) {
        const s = sites[i];
        s.userData.target = ((i - pick + sites.length) % sites.length) < m.flakTargets;
        s.userData.alive = true;
        s.userData.hp = 2;
        s.visible = true;
      }
      // 目标阵地亮红色标记环，方便 HUD 分辨「要炸哪一个」
      m.flakList = sites.filter((s) => s.userData.target);
      this.el.objHint.textContent = `目标：投弹摧毁 ${m.flakTargets} 处高炮阵地（B 投弹 · HUD ⊕ 为准星）`;
      this._toast(`⚠ 对地突击 · 摧毁 ${m.flakTargets} 处高炮阵地`, 2200);
    }

    // 后续波次偶尔追加王牌机压场
    if (m.type !== 'ace' && (n % 3 === 0)) {
      const e = spawnFighter({ variant: 'ace', skillBias: 0.12, r: 2600, speed: 280, role: 'ace' });
      this._toast('⚠ 敌方王牌机出现');
      m.extraAce = e;
    }

    if (m.type === 'sweep') this._centerMsg(`第 ${n}/${MAX_WAVES} 波 · ${m.name}\n${m.brief}`, 2200);
    else this._centerMsg(`第 ${n}/${MAX_WAVES} 波 · ${m.name}\n${m.brief}`, 2400);
    this.waveIntro = 2.4;
  }

  /** 当前波次任务是否完成 */
  _missionComplete() {
    const m = this.mission;
    if (!m) return this.enemies.every((e) => !e.alive);
    if (m.type === 'strike' && this._flakTargetsDown() < m.flakTargets) return false;
    if ((m.type === 'escort' || m.type === 'defend') && this.stats.base < 100 - m.maxBaseLoss) return false;
    if (m.type === 'ace' && m.commander && m.commander.alive) return false;
    return this.enemies.every((e) => !e.alive);
  }

  /** 已摧毁的「任务指定」高炮阵地数（非指定阵地被打掉不计入进度） */
  _flakTargetsDown() {
    return (this.world.flakSites || []).filter((s) => s.userData.target && s.userData.alive === false).length;
  }

  /** 本方地面设施完好度（100 = 全部完好；机场防卫波用） */
  baseIntegrity() {
    const list = this.world.groundFacilities || [];
    if (!list.length) return 100;
    const total = list.reduce((s, f) => s + (f.kind === 'hangar' ? 3 : 2), 0);
    const left = list.reduce((s, f) => s + (f.alive ? (f.kind === 'hangar' ? 3 : 2) : 0), 0);
    return Math.round((left / Math.max(1, total)) * 100);
  }

  _missionProgress() {
    return missionProgress(this.mission, {
      enemies: this.enemies,
      base: this.stats.base,
      flakTargetsDown: this._flakTargetsDown(),
    });
  }

  _onAlliedKill(victim, ally) {
    const label = victim.variant === 'bomber' ? '轰炸机' : victim.variant === 'ace' ? '王牌机' : '战斗机';
    this.stats.score += 60;
    this._killFeed(`僚机击落 ${label} +60`);
    this._toast(`僚机 ${ally.callsign || ''} 击落一架${label}`, 1200);
    this.audio.radio();
  }

  _onAllyDown(ally) {
    this.stats.alliesLost++;
    this._killFeed(`僚机 ${ally.callsign || ''} 被击落`);
    this._toast('⚠ 僚机被击落！', 1600);
    this.audio.damage();
  }

  _onKill(victim, credit) {
    const isPlayerPlane = victim === this.player;
    // 僚机
    if (this.allies.indexOf(victim) >= 0) {
      if (!victim._deathHandled) { victim._deathHandled = true; this.combat._killAlly(victim); }
      return;
    }
    if (credit === 'player' && !isPlayerPlane) {
      const base = victim.variant === 'bomber' ? 400 : victim.variant === 'ace' ? 300 : 120;
      this.stats.comboTimer = 6;
      this.stats.combo++;
      this.stats.maxCombo = Math.max(this.stats.maxCombo, this.stats.combo);
      const mult = 1 + Math.min(this.stats.combo - 1, 9) * 0.15;
      const gain = Math.round(base * mult);
      this.stats.score += gain;
      this.stats.kills++;
      this.save.totalKills = (this.save.totalKills || 0) + 1;
      const label = victim.variant === 'bomber' ? '轰炸机' : victim.variant === 'ace' ? '王牌机' : '战斗机';
      this._killFeed(`击落 ${label} +${gain}${this.stats.combo > 1 ? ` ×${this.stats.combo}` : ''}`);
      if (this.stats.combo > 1) {
        this.el.combo.textContent = `${this.stats.combo} 连杀  ×${mult.toFixed(2)}`;
        this.el.combo.classList.remove('hidden');
      }
      this.audio.hit();
    } else if (isPlayerPlane) {
      this._toast('机体损毁！');
      this.audio.missionFailed();
      setTimeout(() => this.finishMission(false, '战机被击落'), 1600);
    } else {
      this.stats.score += 40;
      this._killFeed(`${victim.variant === 'bomber' ? '轰炸机' : '敌机'} 撞地自毁 +40`);
    }
    this._refreshSaveUI();
  }

  /** 炸弹波及地面防空阵地 */
  // ===================================================================
  // 僚机无线电指令（X 键循环）
  // ===================================================================
  _radioCommand() {
    if (!this.allies.length) { this._toast('无僚机在线', 1000); return; }
    const orders = ['follow', 'engage', 'defend'];
    this.allyOrder = orders[(orders.indexOf(this.allyOrder || 'follow') + 1) % orders.length];
    const names = { follow: '编队跟随', engage: '自由攻击', defend: '保卫基地' };
    if (this.el.allyOrderTxt) this.el.allyOrderTxt.textContent = names[this.allyOrder];
    for (const a of this.allies) a.order = this.allyOrder;
    this.audio.radio();
    this._toast(`僚机指令：${names[this.allyOrder]}`, 1500);
  }

  _damageFlak(pos, radius) {
    // 爆炸回调会再次触发本函数（连锁），用重入锁避免同一发炸弹被重复计伤
    if (this._flakBusy) return;
    this._flakBusy = true;
    try {
      const sites = this.world.flakSites || [];
      for (const s of sites) {
        if (s.userData.alive === false) continue;
        // 阵地是基地组的子节点，必须换算到世界坐标后再算距离，否则会判偏一整座高原
        const wp = this.world.siteWorldPos(s, _v1);
        wp.y = this.world.heightAt(wp.x, wp.z) + 10;
        if (wp.distanceTo(pos) > radius) continue;
        // 一个阵地需要两次命中摧毁
        s.userData.hp = (s.userData.hp ?? 2) - 1;
        this.audio.ping();
        if (s.userData.hp <= 0) {
          s.userData.alive = false;
          this.stats.flakDown++;
          this.stats.score += 500;
          s.visible = false;
          this.combat.explode(wp, 60, 3.4, null);
          this._killFeed(`摧毁高炮阵地 +500 (${this.stats.flakDown})`);
          this.audio.explosion(500);
        } else {
          this._toast(`命中高炮阵地！剩余耐久 ${s.userData.hp}`, 1200);
        }
      }
    } finally {
      this._flakBusy = false;
    }
  }

  /**
   * 炸弹/导弹爆炸波及本方地面设施（油罐、弹药库、机库）。
   * 被摧毁的设施会连锁爆炸：弹药库殉爆会再炸一遍附近设施与阵地。
   */
  _damageFacilities(pos, radius) {
    const killed = this.world.damageFacilities(pos, radius, 1);
    for (const f of killed) {
      f.group.visible = false;
      this.stats.facilitiesDown++;
      this.stats.base = clamp(this.stats.base - 8, 0, 100);
      const wp = this.world.siteWorldPos(f.group, _v1);
      this.combat.explode(_v2.set(wp.x, this.world.heightAt(wp.x, wp.z) + 8, wp.z), 60, 3.4, null);
      this._killFeed(`⚠ ${f.kind === 'fuel' ? '油库' : f.kind === 'ammo' ? '弹药库' : '机库'}被摧毁`);
      this.audio.explosion(600);
      // 弹药库殉爆：波及更大范围（连锁到相邻设施与地面阵地）
      if (f.kind === 'ammo') {
        this._damageFacilities(_v3.set(wp.x, this.world.heightAt(wp.x, wp.z) + 8, wp.z), 120);
      }
      if (this.stats.base <= 0) this.finishMission(false, '我方机场被摧毁');
    }
    return killed;
  }

  /**
   * 坠落弹药（敌方炸弹/导弹）落在地面后的结算入口：
   * 扣机场完好度 + 砸设施 + 砸停机坪候机。coord 用碰撞点，radius 用溅射半径。
   */
  _landSuppliesNear(pos, radius) {
    const onAirfield = Math.abs(pos.x) <= LOGISTICS.rearmZoneX + 120 && Math.abs(pos.z) <= LOGISTICS.rearmZoneZ;
    this._damageFacilities(pos, Math.max(radius, 80));
    if (!onAirfield) return;
    this.stats.base = clamp(this.stats.base - 5, 0, 100);
    // 停机坪候机被炸毁（真实损失：影响结算与视觉）
    for (const j of this.world.parkedJets || []) {
      if (!j.alive) continue;
      const wp = _v1.copy(j.mesh.position);
      if (this.world.airbase) wp.add(this.world.airbase.position);
      if (wp.distanceTo(pos) > 70) continue;
      j.alive = false;
      j.mesh.visible = false;
      this.stats.base = clamp(this.stats.base - 3, 0, 100);
      this.combat.explode(wp.setY(this.world.heightAt(wp.x, wp.z) + 6), 40, 2.6, null);
      this._killFeed('⚠ 停机坪上的战机被炸毁');
    }
    if (this.stats.base <= 0) this.finishMission(false, '我方机场被摧毁');
  }

  _flashDamage() {
    const f = this.el.damageFlash;
    f.style.opacity = '1';
    clearTimeout(this._dmgT);
    this._dmgT = setTimeout(() => { f.style.opacity = '0'; }, 130);
    this.audio.damage();
  }

  finishMission(win, reason) {
    if (this.state === 'result') return;
    const st = this.combat.stats;
    const fired = st.fired > 0 ? st.fired : this.stats.shots;
    const acc = fired > 0 ? Math.round((st.hits / fired) * 100) : 0;
    const hpPct = Math.round((this.player ? this.player.hp / this.player.maxHp : 0) * 100);
    const integrity = this.baseIntegrity();
    let score = this.stats.score;
    if (win) {
      score += Math.round(this.stats.base * 6)
        + Math.round(this.stats.time < 300 ? 600 : 250)
        + this.stats.alliesLost === 0 && this.allies.length ? 400 : 0
        + Math.round(integrity * 2);          // 机场保全加成
    }
    this.stats.score = score;

    this.save.sorties = (this.save.sorties || 0) + 1;
    this.save.best = Math.max(this.save.best || 0, score);
    this.save.records = this.save.records || [];
    this.save.records.unshift({
      date: Date.now(), score, kills: this.stats.kills, acc, win,
      missiles: st.missilesFired, mslHits: st.missileHits,
    });
    this.save.records = this.save.records.slice(0, 20);
    Store.set(STORE_KEY, this.save);

    this.el.resScore.textContent = fmtNum(score);
    this.el.resKills.textContent = String(this.stats.kills);
    this.el.resAcc.textContent = `${acc}%`;
    this.el.resTime.textContent = `${Math.round(this.stats.time)}s`;
    this.el.resCombo.textContent = String(this.stats.maxCombo);
    this.el.resHp.textContent = `${hpPct}%`;
    if (this.el.resMsl) {
      this.el.resMsl.textContent = `${st.missileHits}/${st.missilesFired}`;
      this.el.resBase.textContent = `${Math.round(this.stats.base)}%`;
      this.el.resAlly.textContent = String(this.allies.filter((a) => a.alive).length);
    }
    if (this.el.resFacility) {
      this.el.resFacility.textContent = this.stats.facilitiesDown > 0
        ? `${this.stats.facilitiesDown} 处 / 剩 ${integrity}%`
        : `完好 ${integrity}%`;
    }
    this.el.bossBarWrap.classList.add('hidden');
    this._setState('result', {
      title: win ? '任务完成' : '任务失败',
      sub: win ? `第 ${MAX_WAVES} 波敌机全部清除，机场完好度 ${this.stats.base}% · 补弹 ${this.stats.rearmCount} 次` : reason,
    });
    this.audio.stopEngine();
    this.audio.setEnabled(this.settings.audio === 'on');
    if (win) this.audio.missionComplete(); else this.audio.missionFailed();
    this.input.exitLock();
  }

  // ===================================================================
  // 输入意图 → 游戏动作
  // ===================================================================
  _handleActions() {
    const i = this.input, p = this.player, c = this.combat;

    const pad = i.readGamepadActions();     // 手柄动作（无手柄时为 null）
    if (i.pressed('Escape') || i.pressed('p') || i.pressed('P') || (pad && pad.pause)) {
      if (this.state === 'playing') { this._setState('paused'); this.input.exitLock(); }
      else if (this.state === 'paused') { this._setState('playing'); }
      return;
    }
    if (this.state !== 'playing') return;
    // 手柄的武器 / 视角 / 锁定切换（键盘侧对应 F / B / C / Tab / V）
    if (pad) {
      if (pad.cycleTarget) this._cycleTarget();
      if (pad.camera) { p.cameraMode = (p.cameraMode + 1) % 4; this._toast(`视角 ${['追尾', '座舱', '电影', '观察'][p.cameraMode]}`); }
    }

    // 武器切换
    if (i.pressed('r') || i.pressed('R')) {
      p.weapons.mode = (p.weapons.mode + 1) % 4;
      this.audio.uiClick();
    }
    if (i.pressed('1')) p.weapons.mode = 0;
    if (i.pressed('2')) p.weapons.mode = 1;
    if (i.pressed('3')) p.weapons.mode = 2;
    if (i.pressed('4')) p.weapons.mode = 3;

    // 视角：0 追尾 1 座舱 2 电影 3 观察（环绕本机）
    if (i.pressed('v') || i.pressed('V')) {
      p.cameraMode = (p.cameraMode + 1) % 4;
      this.cam.orbitAngle = 0;
      this._toast(['追尾视角', '座舱视角', '电影视角', '观察视角（Q/E 环绕）'][p.cameraMode]);
    }
    if (p.cameraMode === 3) {
      if (i.down('q') || i.down('Q') || i.down('ArrowLeft')) this.cam.orbitAngle -= 1.6 * this.lastDt;
      if (i.down('e') || i.down('E') || i.down('ArrowRight')) this.cam.orbitAngle += 1.6 * this.lastDt;
      this.cam.freeFov = clamp((this.cam.freeFov ?? 60) + (i.mouse.wheel || 0) * 4, 30, 100);
    }
    // 静音
    if (i.pressed('m') || i.pressed('M')) {
      const on = this.settings.audio === 'on';
      this.settings.audio = on ? 'off' : 'on';
      this.audio.setEnabled(!on);
      this._persistSettings();
      this._toast(!on ? '音效已开启' : '音效已静音');
    }
    // 目标循环
    if (i.pressed('Tab')) {
      this._cycleTarget();
      this.audio.uiClick();
    }
    // 消息快捷键
    if (i.pressed('x') || i.pressed('X')) this._radioCommand();
    if (i.pressed('h') || i.pressed('H')) {
      this.settings.hud = nextHudMode(this.settings.hud);
      this._persistSettings();
      this._syncHudSegment();
      this._toast({ auto: 'HUD：自动（紧张时简化）', full: 'HUD：完整显示', minimal: 'HUD：简化显示' }[this.settings.hud], 1400);
    }
    // 调试信息（F3）：帧率 / 实体数 / 场景节点数，排查性能问题用
    if (i.pressed('F3')) this._toggleDebug();
    // 单位制切换（公制 ⇄ 航空制）
    if (i.pressed('u') || i.pressed('U')) {
      this.settings.units = this.settings.units === 'imperial' ? 'metric' : 'imperial';
      this._persistSettings();
      this._syncUnitSegment();
      this._toast(this.settings.units === 'imperial' ? '单位：英尺 / 节 / 海里' : '单位：米 / 公里', 1400);
    }

    // 开火
    const padGun = pad && pad.gun;
    if ((i.down(' ') || i.mouse.left || padGun) && p.alive) c.firePlayerGun();
    if (pad && pad.missile && p.alive) {
      if (this.lockedTarget) { if (c.firePlayerMissile(this.lockedTarget)) this.audio.launch(); }
      else this._toast('未锁定目标 · 将敌机置于准星内保持 1 秒', 1200);
    }
    if (pad && pad.bomb && p.alive) { if (!c.dropPlayerBomb()) this._toast('炸弹已用尽', 900); }
    if (pad && pad.flare && p.alive) { if (!c.releasePlayerFlare()) this._toast('干扰弹已用尽', 900); }
    if ((i.pressed('f') || i.pressed('F') || i.mouse.right) && p.alive) {
      if (this.lockedTarget) {
        if (c.firePlayerMissile(this.lockedTarget)) this.audio.launch();
      } else this._toast('未锁定目标 · 将敌机置于准星内保持 1 秒', 1200);
    }
    if ((i.pressed('b') || i.pressed('B')) && p.alive) {
      if (!c.dropPlayerBomb()) this._toast('炸弹已用尽', 900);
    }
    if ((i.pressed('c') || i.pressed('C')) && p.alive) {
      if (!c.releasePlayerFlare()) this._toast('干扰弹已用尽', 900);
    }
    // 紧急补给：跑道上方低空按住 R 加油补弹（点按 R 仍是切换武器）
    if (i.down('r') || i.down('R')) this._tryRearm();
    // 快速视角：数字小键盘无效，用 Z 快速复位视角
    if (i.pressed('z') || i.pressed('Z')) { p.cameraMode = 0; this.cam.lookOff.set(0, 0); this._toast('视角已复位'); }

    // 飞行控制
    const arcade = this.settings.control === 'arcade';
    const ax = i.readAxes();
    p.inputs.pitch = approach(p.inputs.pitch, ax.pitch, 6 * this.lastDt);
    p.inputs.roll = approach(p.inputs.roll, ax.roll, 7 * this.lastDt);
    p.inputs.yaw = approach(p.inputs.yaw, ax.yaw, 5 * this.lastDt);
    // 操纵模式：街机模式倾斜自动转弯更强、更不易失速
    p.bankTurnGain = arcade ? 1.05 : 0.98;
    p.bankDropGain = arcade ? 0.10 : 0.18;
    // 增稳：标准模式只做坡度阻尼 + 轻微俯仰配平；街机模式回正更强
    p.levelAssist = arcade ? 0.72 : 0.5;
    p.trimAssist = arcade ? 0.9 : 0.3;
    // 注：减速板不能用 Alt —— Chrome 里按 Alt 会把焦点切到浏览器菜单，游戏直接收不到 keyup，
    // 结果减速板卡死。统一用 Control（macOS 上再接受 Meta 作兼容）
    p.airbrake = i.down('Control') || i.down('Meta');
    if (i.padThrottle != null) {
      // 手柄：扳机直接给油门绝对值，推过 100% 触加力、松到最低触减速板（与键盘语义对齐）
      p.throttle = clamp(i.padThrottle, 0.2, 1);
      p.afterburner = i.padThrottle > 1.0 && p.fuel > 0;
    } else {
      p.afterburner = i.down('Shift') && p.fuel > 0;
      if (i.down('Shift')) p.throttle = clamp(p.throttle + this.lastDt * 0.6, 0.2, 1);
      else p.throttle = clamp(p.throttle - this.lastDt * (p.airbrake ? 0.8 : 0.15), 0.2, 1);
    }
    if (pad && pad.throttleUp) p.throttle = clamp(p.throttle + 0.1, 0.2, 1);
    if (pad && pad.throttleDown) p.throttle = clamp(p.throttle - 0.1, 0.2, 1);
  }

  // ===================================================================
  // 锁定系统
  // ===================================================================
  _targetCandidates() {
    if (!this.player || !this.player.alive) return [];
    const fwd = this.player.getForward(_v1.clone());
    const list = [];
    for (const e of this.enemies) {
      if (!e.alive) continue;
      const to = e.position.clone().sub(this.player.position);
      const dist = to.length();
      if (dist > 6000) continue;
      const cos = fwd.dot(to.normalize());
      if (cos < 0.86) continue;
      list.push({ e, dist, cos });
    }
    list.sort((a, b) => b.cos - a.cos || a.dist - b.dist);
    return list;
  }

  _cycleTarget() {
    const list = this._targetCandidates();
    if (!list.length) { this.lockedTarget = null; return; }
    const idx = list.findIndex((x) => x.e === this.lockedTarget);
    const next = list[(idx + 1) % list.length];
    this.lockedTarget = next.e;
    this.lockProgress = 1;
    this.audio.lock();
  }

  _updateLock(dt) {
    const cands = this._targetCandidates();
    if (this.lockedTarget && (!this.lockedTarget.alive || this.lockedTarget.spawnGrace > 0.2)) {
      this.lockedTarget = null;
      this.lockProgress = 0;
    }
    if (!this.lockedTarget && cands.length && cands[0].cos > 0.985) {
      const best = cands[0];
      this.lockProgress += dt / 0.85;
      if (this.lockProgress >= 1) {
        this.lockedTarget = best.e;
        this.lockProgress = 1;
        this.audio.lock();
        this._toast(`已锁定 · ${best.e.variant === 'bomber' ? '轰炸机' : best.e.variant === 'ace' ? '王牌机' : '战斗机'} · ${this.fmtDist(best.dist)}`, 1200);
      }
    } else if (!this.lockedTarget) {
      this.lockProgress = Math.max(0, this.lockProgress - dt * 1.2);
    }
    // 目标脱离视野过久则解除
    if (this.lockedTarget) {
      const fwd = this.player.getForward(_v1.clone());
      const to = this.lockedTarget.position.clone().sub(this.player.position);
      if (to.length() > 8000 || fwd.dot(to.normalize()) < 0.55) {
        this._lockLossTimer = (this._lockLossTimer || 0) + dt;
        if (this._lockLossTimer > 2.5) { this.lockedTarget = null; this.lockProgress = 0; this._lockLossTimer = 0; }
      } else this._lockLossTimer = 0;
    }
  }

  // ===================================================================
  // 相机
  // ===================================================================
  _updateCamera(dt) {
    const p = this.player;
    const cam = this.world.camera;
    if (!p) return;

    // 鼠标自由视角（仅在指针锁定时累积位移）
    const i = this.input;
    if (i.locked) {
      this.cam.lookOff.x = clamp(this.cam.lookOff.x - i.mouse.dx * 0.0013, -0.75, 0.75);
      this.cam.lookOff.y = clamp(this.cam.lookOff.y - i.mouse.dy * 0.0013, -0.45, 0.6);
    }
    this.cam.lookOff.multiplyScalar(Math.max(0, 1 - dt * 1.6));

    const fwd = p.getForward(_v1.clone());
    const up = p.getUp(_v2.clone());
    const shakeAmp = this.combat.shake;

    let targetPos = _v3.set(0, 0, 0);
    let targetQuat = new THREE.Quaternion();

    if (p.cameraMode === 1) {
      // 座舱视角
      targetPos.copy(p.position)
        .addScaledVector(fwd, 1.2)
        .addScaledVector(up, 1.35);
      targetQuat.copy(p.quaternion);
      cam.fov = 72;
    } else if (p.cameraMode === 2) {
      // 电影视角：侧后方缓慢环绕
      const t = this.worldTime ? this.worldTime : 0;
      const a = t * 0.18;
      targetPos.set(Math.cos(a) * 26, 8 + Math.sin(a * 0.7) * 4, Math.sin(a) * 26)
        .applyQuaternion(p.quaternion).add(p.position);
      targetQuat.copy(p.quaternion);
      cam.fov = 55;
    } else if (p.cameraMode === 3) {
      // 观察视角：绕本机自由环绕（Q/E 或鼠标滚轮调整），便于欣赏模型
      const t = this.cam.orbitAngle || 0;
      const r = 30 + clamp(p.speed * 0.03, 0, 14);
      const a = t + (this.worldTime || 0) * 0.05;
      targetPos.set(Math.sin(a) * r, 6 + Math.sin(a * 0.5) * 5, Math.cos(a) * r).add(p.position);
      // 始终看向本机
      targetQuat.copy(p.quaternion);
      cam.fov = this.cam.freeFov ?? 60;
      const m = new THREE.Matrix4().lookAt(targetPos, p.position, new THREE.Vector3(0, 1, 0));
      targetQuat.setFromRotationMatrix(m);
    } else {
      // 追尾视角（默认）：带惯性滞后
      const dist = 15 + p.speed * 0.022;
      const height = 3.4;
      targetPos.set(0, height, dist).applyQuaternion(p.quaternion).add(p.position);
      targetQuat.copy(p.quaternion);
      cam.fov = 60 + clamp((p.speed - 150) / 400, 0, 1) * 12;
    }

    // 相机自身姿态（含鼠标偏移，观察模式不叠加）
    const q = targetQuat.clone();
    if (p.cameraMode !== 3 && this.cam.lookOff.lengthSq() > 0.0001) {
      q.multiply(new THREE.Quaternion().setFromEuler(new THREE.Euler(this.cam.lookOff.y, this.cam.lookOff.x, 0, 'YXZ')));
    }

    const posLerp = p.cameraMode === 1 ? 1 : clamp(dt * (p.cameraMode === 2 ? 6 : p.cameraMode === 3 ? 8 : 12), 0, 1);
    cam.position.lerp(targetPos, posLerp);
    cam.quaternion.slerp(q, clamp(dt * (p.cameraMode === 1 ? 30 : 9), 0, 1));

    // 抖动
    if (shakeAmp > 0.001) {
      const s = shakeAmp * 0.55;
      cam.position.x += (Math.random() - 0.5) * s;
      cam.position.y += (Math.random() - 0.5) * s;
      cam.position.z += (Math.random() - 0.5) * s;
      cam.rotateZ((Math.random() - 0.5) * shakeAmp * 0.03);
    }

    // 不允许穿地
    const groundY = this.world.heightAt(cam.position.x, cam.position.z) + 3;
    if (cam.position.y < groundY) cam.position.y = groundY;

    if (Math.abs(cam.fov - this.cam.fov) > 0.05) {
      this.cam.fov = lerp(this.cam.fov, cam.fov, clamp(dt * 4, 0, 1));
      cam.fov = this.cam.fov;
      cam.updateProjectionMatrix();
    }
  }

  // ===================================================================
  // 每帧
  // ===================================================================
  _frame(now) {
    if (!this.lastTime) this.lastTime = now;
    let dt = (now - this.lastTime) / 1000;
    this.lastTime = now;
    // 钳制到 [0, 50ms]：负 dt（时钟回拨/手动步进）会让积分反向，必须挡住
    if (!(dt > 0)) dt = 0;
    else if (dt > 0.05) dt = 0.05;
    this.lastDt = dt;
    this.worldTime = (this.worldTime || 0) + dt;

    if (this.state === 'playing' || this.state === 'paused') {
      const active = this.state === 'playing';
      this.world.update(active ? dt : dt * 0.15, this.world.camera);
      if (active) this._handleActions();
      const step = active ? dt : 0;
      if (step > 0) this._simulate(step);
      else this.player && this.player.syncMesh();
      this._updateCamera(active ? dt : dt * 0.5);
      this._drawHUD();
    } else if (this.world) {
      // 菜单背景：相机缓慢环绕基地
      this.worldTime = (this.worldTime || 0) + dt;
      const a = this.worldTime * 0.06;
      const r = 900;
      const cam = this.world.camera;
      const baseY = this.world.heightAt(0, 0);
      cam.position.set(Math.cos(a) * r, baseY + 320 + Math.sin(a * 0.6) * 90, Math.sin(a) * r);
      cam.lookAt(0, baseY + 60, 0);
      this.world.update(dt, cam);
    }
    this.world.render();
    this._updateDebug();
    this.input.endFrame();
  }

  _simulate(dt) {
    const p = this.player;
    const s = this.stats;
    s.time += dt;
    s.comboTimer = Math.max(0, s.comboTimer - dt);
    if (s.comboTimer <= 0 && s.combo > 0) { s.combo = 0; this.el.combo.classList.add('hidden'); }
    if (this.waveIntro > 0) this.waveIntro -= dt;

    p.update(dt);
    this.particles.update(dt);
    // 玩家战损黑烟 + 翼尖拉烟
    p.emitDamageSmoke(dt, this.particles, this.rng);
    p.emitWingtips(dt, this.particles);
    // 跑道补给（空中/贴地判定都在这里面，离开跑道自动中断）
    this._updateRearm(dt);
    // 坠机点烟雾寿命
    this.world.updateCrashSites(dt);

    const aliveEnemies = this.enemies.filter((e) => e.alive).length;
    const bomberTarget = _v2.set(0, 900, 0);

    // 敌方 AI 上下文（对地攻击型敌机扑向机场，其余照常缠斗）
    const ctx = {
      player: p,
      world: this.world,
      camera: this.world.camera,   // 供武器系统做「听音位置」（高炮炮声远近）
      aggressive: aliveEnemies <= 3,
      bomberTarget,
      baseTarget: _v3.set(0, this.world.heightAt(0, 0) + 420, 0),
      fireEnemyGun: (e, burst) => this.combat.fireEnemyGun(e, burst),
      fireEnemyMissile: (e, t) => this.combat.fireEnemyMissile(e, t),
      releaseEnemyFlare: (e) => this.combat.releaseEnemyFlare(e),
      dropEnemyBomb: (e) => {
        this.combat.dropEnemyBomb(e);
        this._toast('机场遭到轰炸！完整度下降', 1400);
      },
    };

    for (const e of this.enemies) {
      if (!e.alive) continue;
      // 被玩家咬住 → 触发规避
      if (e.state !== 'evasive' && e.spawnGrace <= 0 &&
          e.isThreatenedBy(p, this.lockedTarget === e) && this.rng.next() < dt * 1.3) {
        e.forceEvade();
      }
      e.update(dt, ctx);
      e.emitDamageSmoke(dt, this.particles, this.rng);
      if (e.contrailOn) e.emitWingtips(dt, this.particles);
      // 敌机撞地
      if (this.world.hitsGround(e.position, 1.5)) {
        this.combat.explode(e.position, 34, 2.4, 'player');
        e.alive = false;
        if (!e._deathHandled) { e._deathHandled = true; this._onKill(e, 'ground'); }
      }
      // 敌机越界拉回
      const arena = this.world.clampToArena(e.position, this._clampOut || (this._clampOut = { hit: false }));
      if (arena.hit && e.position.y > WORLD.ceiling - 1) { e.velocity.y = -60; }
    }

    // 僚机 AI：跟随/拦截 + 自动开火
    if (this.allies.length) {
      const allyCtx = {
        player: p, world: this.world, enemies: this.enemies,
        fireGun: (a, burst) => this.combat.fireAllyGun(a, burst),
        fireMissile: (a, t) => this.combat.fireAllyMissile(a, t),
      };
      for (const a of this.allies) {
        a.update(dt, allyCtx);
        if (!a.alive) continue;
        a.emitDamageSmoke(dt, this.particles, this.rng);
        if (this.world.hitsGround(a.position, 1.5)) {
          this.combat.explode(a.position, 32, 2.2, 'enemy');
          a.alive = false;
          this.combat._killAlly(a);
        }
      }
    }

    this.combat.update(dt, ctx);

    // 坠机点：把最近的残骸拖过来冒烟（视觉地标）
    if (!p.alive && this._crashMarked !== true) {
      this._crashMarked = true;
      this.world.showCrash(p.position);
    }

    // 玩家撞地
    if (p.alive && this.world.hitsGround(p.position, 2.2)) {
      this.combat.explode(p.position, 46, 3, 'enemy');
      p.hp = 0; p.alive = false;
      this._crashMarked = false;
      this.world.showCrash(p.position);
      this._onKill(p, 'ground');
    }
    if (p.alive && Math.abs(p.position.x) > WORLD.half - 80) this._toast('已接近战场边界', 900);
    this.world.clampToArena(p.position, this._clampOut || (this._clampOut = { hit: false }));

    // 导弹来袭告警
    this._updateMissileWarning(dt);
    this.audio.updateEngine(p.throttle, p.afterburner, p.alive);

    // 波次推进：以任务目标是否达成为准
    if (this.state === 'playing' && this._missionComplete() && aliveEnemies === 0) {
      if (this.stats.wave >= MAX_WAVES) {
        this.finishMission(true, '');
      } else {
        const bonus = 300 + Math.round(this.stats.base * 2);
        this.stats.score += bonus;
        this.stats.wave++;
        this._spawnWave(this.stats.wave);
      }
    }
    this._updateLock(dt);
    this._updateBossBar();
  }

  /** 王牌机血条 */
  _updateBossBar() {
    const ace = this.enemies.find((e) => e.alive && (e.role === 'commander' || e.role === 'ace'));
    if (!ace) { this.el.bossBarWrap.classList.add('hidden'); return; }
    this.el.bossBarWrap.classList.remove('hidden');
    this.el.bossName.textContent = ace.callsign || (ace.variant === 'ace' ? '敌方王牌机' : '敌方长机');
    this.el.bossBar.style.width = `${clamp(ace.hp / ace.maxHp, 0, 1) * 100}%`;
  }

  _updateMissileWarning(dt) {
    let threat = false;
    let nearest = Infinity;
    for (const m of this.combat.missiles) {
      if (m.owner !== 'enemy' || !this.player || !this.player.alive) continue;
      const d = m.pos.distanceTo(this.player.position);
      // 仅在导弹“冲向自己”时告警，避免误报
      const closing = m.vel.clone().multiplyScalar(-1).dot(this.player.position.clone().sub(m.pos));
      if (d < 1800 && closing > 0) { threat = true; nearest = Math.min(nearest, d); }
    }
    this._warnTimer = (this._warnTimer || 0) - dt;
    if (threat && this._warnTimer <= 0) {
      this.audio.warn();
      this._warnTimer = 0.9;
    }
    // 地面高炮威胁：低于射高时，附近活跃阵地的雷达会照过来
    const flakThreat = this._flakThreatLevel();
    const p = this.player;
    const stalling = !!(p && p.alive && p.stalling);
    const lowAlt = !!(p && p.alive && p.position.y - this.world.heightAt(p.position.x, p.position.z) < 120 && p.velocity.y < -20);
    const lowFuel = !!(p && p.alive && p.fuel < 18);
    const baseAlarm = this.stats.base < 60;
    const shown = threat || stalling || lowAlt || flakThreat.range || lowFuel || baseAlarm;
    this.el.warn.classList.toggle('hidden', !shown);
    if (this.el.warnPanel) {
      this.el.warnRwr.classList.toggle('on', threat);
      this.el.warnStall.classList.toggle('on', stalling);
      this.el.warnGear.classList.toggle('on', !!(p && p.airbrake));
      if (this.el.warnFlak) this.el.warnFlak.classList.toggle('on', flakThreat.range);
      if (this.el.warnFuel) this.el.warnFuel.classList.toggle('on', lowFuel);
      if (this.el.warnBase) this.el.warnBase.classList.toggle('on', baseAlarm);
    }
    if (stalling) this.el.warn.textContent = '⚠ 失速警告 · 增大推力 (Shift)';
    else if (lowAlt) this.el.warn.textContent = `⚠ 低高度 ${this.fmtAlt(p.position.y - this.world.heightAt(p.position.x, p.position.z))} · PULL UP`;
    else if (threat) this.el.warn.textContent = `⚠ 导弹来袭 ${this.fmtDist(nearest)} · 释放干扰弹 (C)`;
    else if (lowFuel) this.el.warn.textContent = `⚠ 燃油不足 ${Math.round(p.fuel)}% · 返回跑道补给 (R)`;
    else if (baseAlarm) this.el.warn.textContent = `⚠ 机场受损 ${this.stats.base}% · 优先拦截对地攻击机`;
    else if (flakThreat.range) this.el.warn.textContent = `⚠ 高炮火力范围 · 爬升脱离 (${this.fmtDist(flakThreat.dist)} · ${flakThreat.sites} 处阵地)`;
  }

  /**
   * 评估当前地面高炮威胁：
   * 只在「低于射高 + 进入开火距离」时才算被覆盖，避免高空巡航时误报。
   */
  _flakThreatLevel() {
    const out = { range: false, dist: Infinity, sites: 0 };
    const p = this.player;
    if (!p || !p.alive) return out;
    if (p.position.y > 2100) return out;
    for (const s of this.world.flakSites || []) {
      if (s.userData.alive === false) continue;
      const wp = this.world.siteWorldPos(s, _v3);
      const d = wp.distanceTo(p.position);
      if (d < 2800) { out.sites++; if (d < out.dist) out.dist = d; }
    }
    out.range = out.sites > 0;
    return out;
  }

  // ===================================================================
  // 提示与击杀播报
  // ===================================================================
  _toast(text, ms = 1600) {
    const d = document.createElement('div');
    d.textContent = text;
    this.el.toasts.appendChild(d);
    setTimeout(() => d.remove(), ms);
  }

  _killFeed(text) {
    const d = document.createElement('div');
    d.textContent = text;
    this.el.killFeed.appendChild(d);
    setTimeout(() => d.remove(), 3200);
    while (this.el.killFeed.children.length > 6) this.el.killFeed.firstChild.remove();
  }

  _centerMsg(text, ms = 1500) {
    const el = this.el.centerMsg;
    el.textContent = text;
    el.classList.remove('hidden');
    clearTimeout(this._centerT);
    this._centerT = setTimeout(() => el.classList.add('hidden'), ms);
  }

  // ===================================================================
  // HUD：DOM 数值 + Canvas 抬头显示 + 雷达
  // ===================================================================
  _resizeHUD() {
    const cv = this.el.hmd;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    this.hud2d.w = window.innerWidth;
    this.hud2d.h = window.innerHeight;
    this.hud2d.dpr = dpr;
    cv.width = Math.floor(this.hud2d.w * dpr);
    cv.height = Math.floor(this.hud2d.h * dpr);
    cv.style.width = '100%';
    cv.style.height = '100%';
    this.hud2d.ctx = cv.getContext('2d');
    this.hud2d.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  _drawHUD() {
    const p = this.player;
    if (!p) return;
    const e = this.el, s = this.stats;

    // ------- DOM 数值 -------
    const hpPct = clamp(p.hp / p.maxHp, 0, 1);
    e.hpBar.style.width = `${hpPct * 100}%`;
    e.hpBar.classList.toggle('low', hpPct < 0.35);
    e.hpTxt.textContent = `${Math.round(hpPct * 100)}%`;
    e.thrBar.style.width = `${Math.round(p.throttle * 100)}%`;
    e.thrTxt.textContent = p.afterburner ? '加力' : `${Math.round(p.throttle * 100)}%`;
    e.altTxt.textContent = this.fmtAlt(p.position.y);
    e.spdTxt.textContent = this.fmtSpeed(p.speed);
    // 燃油 / 机场完好度
    const fuel = clamp(p.fuel / 100, 0, 1);
    if (e.fuelBar) {
      e.fuelBar.style.width = `${fuel * 100}%`;
      e.fuelBar.classList.toggle('low', fuel < 0.22);
      e.fuelTxt.textContent = `${Math.round(p.fuel)}%`;
      e.fuelTxt.style.color = fuel < 0.22 ? '#ff8a5e' : '';
    }
    const integrity = this.baseIntegrity();
    if (e.baseBar) {
      e.baseBar.style.width = `${integrity}%`;
      e.baseBar.classList.toggle('low', integrity < 60);
      e.baseTxt.textContent = `${integrity}%`;
    }
    // 跑道补给提示条
    if (e.rearmTip) {
      const st = this.rearmState(p);
      const needFuel = p.fuel < 99.5;
      const diffD = DIFFICULTY[this.settings.difficulty] || DIFFICULTY.normal;
      const needAmmo = p.weapons.missiles < diffD.missiles || p.weapons.bombs < diffD.bombs;
      if (this.rearm.active) {
        e.rearmTip.textContent = `跑道补给中… 油 ${Math.round(p.fuel)}% 弹 ${p.weapons.missiles}/${p.weapons.bombs}`;
        e.rearmTip.className = 'rearm-tip on';
      } else if (st.ready && (needFuel || needAmmo)) {
        e.rearmTip.textContent = '按住 R 补充燃油与弹药';
        e.rearmTip.className = 'rearm-tip ready';
      } else {
        e.rearmTip.textContent = '';
        e.rearmTip.className = 'rearm-tip hidden';
      }
    }
    // 任务目标
    const m = this.mission;
    if (m) {
      e.missionTxt.textContent = `${m.name} ${s.wave}/${MAX_WAVES}`;
      e.objTxt.textContent = m.brief;
      const prog = this._missionProgress();
      e.objBar.style.width = `${Math.round(prog * 100)}%`;
      e.objBar.classList.toggle('done', prog >= 0.999);
      this.el.objHint.textContent = m.type === 'strike'
        ? `阵地 ${this._flakTargetsDown()}/${m.flakTargets} · B 投弹`
        : m.type === 'escort'
          ? `机场完好度 ≥ ${100 - m.maxBaseLoss}%`
          : m.type === 'defend'
            ? `机场完好度 ≥ ${100 - m.maxBaseLoss}% · 设施 ${integrity}%`
            : m.type === 'ace'
              ? (m.commander && m.commander.alive ? '击落敌方长机' : '长机已击落')
              : `剩余 ${this.enemies.filter((x) => x.alive).length} 架`;
      if (e.allyOrderTxt) {
        e.allyOrderTxt.textContent = ({ follow: '编队跟随', engage: '自由攻击', defend: '保卫基地' })[this.allyOrder || 'follow'];
      }
    }
    e.enemyTxt.textContent = String(this.enemies.filter((x) => x.alive).length);
    e.timeTxt.textContent = fmtTime(s.time);
    e.scoreTxt.textContent = fmtNum(s.score);
    e.gunTxt.textContent = p.gunOverheat ? '过热' : '∞';
    e.gunHeatBar.style.width = `${clamp(p.weapons.gunHeat, 0, 1) * 100}%`;
    e.gunHeatBar.classList.toggle('hot', p.gunOverheat);
    e.missileTxt.textContent = String(p.weapons.missiles);
    e.bombTxt.textContent = String(p.weapons.bombs);
    e.flareTxt.textContent = String(p.weapons.flares);
    e.weaponTxt.textContent = ['机炮', '导弹', '炸弹', '干扰弹'][p.weapons.mode];
    e.allyTxt.textContent = this.allies.length
      ? `${this.allies.filter((a) => a.alive).length}/${this.allies.length}`
      : '无';
    if (p.speed < p.minSpeed) e.spdTxt.style.color = '#ff4d5e'; else e.spdTxt.style.color = '';

    // 战斗紧张度：机场受损 / 燃油告急 / 敌机成群时自动简化（仅 auto 模式生效）
    const busy = integrity < 50 || p.fuel < 15 || this.enemies.filter((x) => x.alive).length >= 6;
    const mode = this._hudMode();
    e.hud.classList.toggle('simplified', mode === 'minimal' || (mode === 'auto' && busy));

    const tgt = this.lockedTarget;
    if (tgt && tgt.alive) {
      e.tgtType.textContent = tgt.variant === 'bomber' ? '轰炸机' : tgt.variant === 'ace' ? '王牌机' : '战斗机';
      const d = tgt.position.distanceTo(p.position);
      e.tgtDist.textContent = this.fmtDist(d);
      const closing = -tgt.position.clone().sub(p.position).normalize().dot(tgt.velocity.clone().sub(p.velocity));
      e.tgtClosure.textContent = this.fmtClosure(closing);
      e.tgtHp.textContent = `${Math.round((tgt.hp / tgt.maxHp) * 100)}%`;
    } else {
      e.tgtType.textContent = '无'; e.tgtDist.textContent = '—';
      e.tgtClosure.textContent = '—'; e.tgtHp.textContent = '—';
    }

    this._drawHMD();
    this._drawRadar();
  }

  _drawHMD() {
    const ctx = this.hud2d.ctx;
    if (!ctx) return;
    const W = this.hud2d.w, H = this.hud2d.h;
    const p = this.player;
    const cam = this.world.camera;
    if (!p || !p.alive) { ctx.clearRect(0, 0, W, H); if (p) this._drawDeathHUD(ctx, W, H); return; }
    ctx.clearRect(0, 0, W, H);

    const cx = W / 2, cy = H / 2;
    ctx.lineWidth = 1.4;
    ctx.font = '11px ui-monospace, monospace';
    ctx.textBaseline = 'middle';

    // ---- 俯仰梯 ----
    const fwd = p.getForward(_v1.clone());
    const pitchDeg = Math.asin(clamp(fwd.y, -1, 1)) * 180 / Math.PI;
    const up = p.getUp(_v2.clone());
    const right = p.getRight(_v3.clone());
    const rollRad = Math.atan2(right.y, up.y);
    ctx.save();
    ctx.translate(cx, cy);
    ctx.rotate(-rollRad);
    ctx.strokeStyle = 'rgba(120, 255, 190, 0.55)';
    ctx.fillStyle = 'rgba(120, 255, 190, 0.75)';
    const pxPerDeg = H / 70;
    for (let a = -60; a <= 60; a += 10) {
      if (a === 0) continue;
      const y = (pitchDeg - a) * pxPerDeg;
      if (Math.abs(y) > H * 0.42) continue;
      const half = a % 20 === 0 ? 90 : 52;
      ctx.beginPath();
      ctx.moveTo(-half, y); ctx.lineTo(half, y);
      ctx.stroke();
      if (a % 20 === 0) {
        ctx.beginPath();
        ctx.moveTo(-half, y); ctx.lineTo(-half, y + (a > 0 ? 7 : -7));
        ctx.moveTo(half, y); ctx.lineTo(half, y + (a > 0 ? 7 : -7));
        ctx.stroke();
        ctx.fillText(`${a}`, half + 8, y);
        ctx.fillText(`${a}`, -half - 22, y);
      }
    }
    // 地平线
    const hzY = pitchDeg * pxPerDeg;
    if (Math.abs(hzY) < H * 0.45) {
      ctx.strokeStyle = 'rgba(120, 255, 190, 0.85)';
      ctx.beginPath();
      ctx.moveTo(-W * 0.34, hzY); ctx.lineTo(-70, hzY);
      ctx.moveTo(70, hzY); ctx.lineTo(W * 0.34, hzY);
      ctx.stroke();
    }
    ctx.restore();

    // ---- 速度矢量标记 ----
    const velNdc = p.position.clone().addScaledVector(p.velocity.clone().normalize(), 1200).project(cam);
    if (velNdc.z < 1 && Math.abs(velNdc.x) < 1 && Math.abs(velNdc.y) < 1) {
      const vx = (velNdc.x * 0.5 + 0.5) * W, vy = (-velNdc.y * 0.5 + 0.5) * H;
      ctx.strokeStyle = 'rgba(160,255,210,0.9)';
      ctx.beginPath();
      ctx.arc(vx, vy, 8, 0, Math.PI * 2);
      ctx.moveTo(vx - 14, vy); ctx.lineTo(vx - 8, vy);
      ctx.moveTo(vx + 8, vy); ctx.lineTo(vx + 14, vy);
      ctx.moveTo(vx, vy - 14); ctx.lineTo(vx, vy - 8);
      ctx.stroke();
    }

    // ---- 机炮准星 + 弹道提前量 ----
    const gunHit = p.position.clone().addScaledVector(fwd, 900);
    const gunNdc = gunHit.project(cam);
    let gx = cx, gy = cy;
    if (gunNdc.z < 1) {
      gx = (gunNdc.x * 0.5 + 0.5) * W;
      gy = (-gunNdc.y * 0.5 + 0.5) * H;
    }
    ctx.strokeStyle = 'rgba(120, 255, 160, 0.95)';
    ctx.lineWidth = 1.6;
    ctx.beginPath();
    ctx.arc(gx, gy, 16, 0, Math.PI * 2);
    ctx.moveTo(gx - 26, gy); ctx.lineTo(gx - 12, gy);
    ctx.moveTo(gx + 12, gy); ctx.lineTo(gx + 26, gy);
    ctx.moveTo(gx, gy - 26); ctx.lineTo(gx, gy - 12);
    ctx.moveTo(gx, gy + 12); ctx.lineTo(gx, gy + 26);
    ctx.stroke();
    ctx.fillStyle = 'rgba(120,255,160,0.95)';
    ctx.fillRect(gx - 1.5, gy - 1.5, 3, 3);

    // ---- 目标框 ----
    let lockedScreen = null;
    for (const e of this.enemies) {
      if (!e.alive) continue;
      const proj = e.position.clone().project(cam);
      const isLocked = e === this.lockedTarget;
      if (proj.z > 1 || Math.abs(proj.x) > 1.15 || Math.abs(proj.y) > 1.15) {
        // 屏幕外：画方向箭头
        if (isLocked || e.position.distanceTo(p.position) < 4000) {
          const ang = Math.atan2(proj.y, proj.x);
          const rx = Math.cos(ang) * Math.min(W, H) * 0.38;
          const ry = Math.sin(ang) * Math.min(W, H) * 0.38;
          ctx.save();
          ctx.translate(cx + rx, cy - ry);
          ctx.rotate(-ang);
          ctx.fillStyle = isLocked ? '#ff3b4e' : 'rgba(255,190,70,0.85)';
          ctx.beginPath();
          ctx.moveTo(12, 0); ctx.lineTo(-6, -7); ctx.lineTo(-6, 7);
          ctx.closePath(); ctx.fill();
          ctx.restore();
        }
        continue;
      }
      const sx = (proj.x * 0.5 + 0.5) * W, sy = (-proj.y * 0.5 + 0.5) * H;
      const dist = e.position.distanceTo(p.position);
      const size = clamp(2600 / Math.max(60, dist) * e.radius * 2.2, 10, 90);
      const color = isLocked ? '#ff3b4e' : 'rgba(255,190,70,0.9)';
      ctx.strokeStyle = color;
      ctx.lineWidth = isLocked ? 2.2 : 1.4;
      const half = size / 2;
      ctx.beginPath();
      // 四角括号
      const c = half * 0.35;
      ctx.moveTo(sx - half, sy - half + c); ctx.lineTo(sx - half, sy - half); ctx.lineTo(sx - half + c, sy - half);
      ctx.moveTo(sx + half - c, sy - half); ctx.lineTo(sx + half, sy - half); ctx.lineTo(sx + half, sy - half + c);
      ctx.moveTo(sx + half, sy + half - c); ctx.lineTo(sx + half, sy + half); ctx.lineTo(sx + half - c, sy + half);
      ctx.moveTo(sx - half + c, sy + half); ctx.lineTo(sx - half, sy + half); ctx.lineTo(sx - half, sy + half - c);
      ctx.stroke();
      // 敌机血量条
      if (e.hp < e.maxHp) {
        const w = size, hpw = w * clamp(e.hp / e.maxHp, 0, 1);
        ctx.fillStyle = 'rgba(0,0,0,0.5)';
        ctx.fillRect(sx - w / 2, sy - half - 9, w, 4);
        ctx.fillStyle = '#ff6a5e';
        ctx.fillRect(sx - w / 2, sy - half - 9, hpw, 4);
      }
      ctx.font = '11px ui-monospace, monospace';
      ctx.fillStyle = color;
      const label = isLocked ? `LOCK ${this.fmtDist(dist)}` : this.fmtDist(dist);
      ctx.fillText(label, sx + half + 6, sy);
      if (e.role === 'commander' || e.role === 'ace') {
        ctx.fillStyle = '#ff7ad9';
        ctx.fillText('★ 长机', sx - half - 42, sy - half - 6);
      }
      if (isLocked) lockedScreen = { x: sx, y: sy, size };

      // 机炮提前量指示
      if (isLocked) {
        const t = dist / GUN.speed;
        const lead = e.position.clone().addScaledVector(e.velocity, t)
          .addScaledVector(p.velocity, -t);
        const lp = lead.project(cam);
        if (lp.z < 1) {
          const lx = (lp.x * 0.5 + 0.5) * W, ly = (-lp.y * 0.5 + 0.5) * H;
          ctx.strokeStyle = 'rgba(120,255,160,0.9)';
          ctx.beginPath(); ctx.arc(lx, ly, 5, 0, Math.PI * 2); ctx.stroke();
        }
      }
    }

    // ---- 友军僚机标记（绿色小方框 + 呼号）----
    for (const a of this.allies) {
      if (!a.alive) continue;
      const proj = a.position.clone().project(cam);
      if (proj.z > 1 || Math.abs(proj.x) > 1.05 || Math.abs(proj.y) > 1.05) continue;
      const sx = (proj.x * 0.5 + 0.5) * W, sy = (-proj.y * 0.5 + 0.5) * H;
      const d = a.position.distanceTo(p.position);
      const size = clamp(2000 / Math.max(60, d) * a.radius * 2.0, 8, 44);
      ctx.strokeStyle = 'rgba(77,255,161,0.85)';
      ctx.lineWidth = 1.2;
      ctx.strokeRect(sx - size / 2, sy - size / 2, size, size);
      ctx.fillStyle = 'rgba(77,255,161,0.9)';
      ctx.fillText(`${a.callsign || '僚机'} ${this.fmtDist(d)}`, sx + size / 2 + 5, sy);
    }

    // ---- 地面高炮阵地标记（始终显示：低空威胁与对地目标共用）----
    {
      for (const site of this.world.flakSites || []) {
        if (site.userData.alive === false) continue;
        // 阵地是基地组的子节点，标绘前必须换算到世界坐标
        const wp = this.world.siteWorldPos(site, _v1.clone());
        wp.y = this.world.heightAt(wp.x, wp.z) + 14;
        const distS = wp.distanceTo(p.position);
        if (distS > 9000) continue;
        const dangerous = distS < 2800 && p.position.y < 2100;
        const proj = wp.clone().project(cam);
        if (proj.z > 1 || Math.abs(proj.x) > 1.05 || Math.abs(proj.y) > 1.05) continue;
        const sx = (proj.x * 0.5 + 0.5) * W, sy = (-proj.y * 0.5 + 0.5) * H;
        ctx.strokeStyle = dangerous ? 'rgba(255,80,70,0.95)' : 'rgba(255,140,60,0.8)';
        ctx.lineWidth = dangerous ? 2 : 1.4;
        ctx.beginPath();
        ctx.moveTo(sx - 11, sy - 11); ctx.lineTo(sx + 11, sy + 11);
        ctx.moveTo(sx + 11, sy - 11); ctx.lineTo(sx - 11, sy + 11);
        ctx.stroke();
        ctx.beginPath(); ctx.arc(sx, sy, 15, 0, Math.PI * 2); ctx.stroke();
        if (dangerous) {
          ctx.save();
          ctx.setLineDash([4, 4]);
          ctx.beginPath(); ctx.arc(sx, sy, 22, 0, Math.PI * 2); ctx.stroke();
          ctx.restore();
        }
        ctx.fillStyle = dangerous ? 'rgba(255,110,90,0.95)' : 'rgba(255,170,80,0.85)';
        ctx.fillText(`AAA ${site.userData.hp ?? 2}/2 ${this.fmtDist(distS)}`, sx + 18, sy);
      }
    }

    // ---- 投弹落点准星（CCIP）----
    this._drawCCIP(ctx, W, H, cam);
    // ---- 跑道补给引导 ----
    this._drawAirfieldGuide(ctx, W, H, p);

    // 锁定进度环
    if (!this.lockedTarget && this.lockProgress > 0.05) {
      ctx.strokeStyle = 'rgba(255,190,70,0.9)';
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(cx, cy, 46, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * this.lockProgress);
      ctx.stroke();
    }
    if (lockedScreen) {
      const d = Math.sin(this.worldTime * 6) * 4;
      ctx.strokeStyle = 'rgba(255,60,80,0.9)';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.arc(lockedScreen.x, lockedScreen.y, lockedScreen.size * 0.75 + d, 0, Math.PI * 2);
      ctx.stroke();
    }

    // ---- 左下角罗盘 ----
    const heading = (Math.atan2(fwd.x, -fwd.z) * 180 / Math.PI + 360) % 360;
    ctx.strokeStyle = 'rgba(120,255,190,0.65)';
    ctx.fillStyle = 'rgba(120,255,190,0.9)';
    ctx.beginPath();
    ctx.arc(cx, H - 62, 34, 0, Math.PI * 2);
    ctx.stroke();
    for (let i = 0; i < 12; i++) {
      const a = (i / 12) * Math.PI * 2 - Math.PI / 2;
      const len = i % 3 === 0 ? 8 : 4;
      ctx.beginPath();
      ctx.moveTo(cx + Math.cos(a) * 34, H - 62 + Math.sin(a) * 34);
      ctx.lineTo(cx + Math.cos(a) * (34 - len), H - 62 + Math.sin(a) * (34 - len));
      ctx.stroke();
    }
    ctx.save();
    ctx.translate(cx, H - 62);
    ctx.rotate(-heading * Math.PI / 180);
    ctx.fillStyle = '#ff6a5e';
    ctx.beginPath(); ctx.moveTo(0, -22); ctx.lineTo(-6, 8); ctx.lineTo(6, 8); ctx.closePath(); ctx.fill();
    ctx.restore();
    ctx.fillText(`${Math.round(heading).toString().padStart(3, '0')}°`, cx - 12, H - 14);
  }

  /**
   * 投弹落点准星（CCIP）：用与实弹一致的弹道模型预测落点，
   * 屏上画 ⊗ + 竖直落线 + 预计弹着时间，低空对地突击时不用再瞎猜。
   */
  _drawCCIP(ctx, W, H, cam) {
    const p = this.player;
    if (!p || !p.alive) return;
    const m = this.mission;
    // 只在对地突击波、或已切到炸弹/余弹充足时显示，避免常态干扰视野
    const relevant = (m && m.type === 'strike') || p.weapons.mode === 2;
    if (!relevant || p.weapons.bombs <= 0) return;

    const fwd = _v1.set(0, 0, -1).applyQuaternion(p.quaternion);
    const vel = bombLaunchVelocity(fwd, p.velocity);
    const impact = predictImpact(p.position, vel, (x, z) => this.world.heightAt(x, z));
    if (!impact.hit || impact.time > 26) return;

    const groundY = this.world.heightAt(impact.x, impact.z);
    const wp = _v2.set(impact.x, groundY + 2, impact.z);
    const dist = wp.distanceTo(p.position);
    const proj = wp.clone().project(cam);

    // 竖直落线（从炸弹到落点，帮助判断相对位置）
    const near = _v3.copy(p.position).addScaledVector(fwd, 60);
    const nearProj = near.clone().project(cam);
    if (proj.z < 1 && nearProj.z < 1) {
      ctx.strokeStyle = 'rgba(255,190,70,0.35)';
      ctx.lineWidth = 1;
      ctx.setLineDash([6, 6]);
      ctx.beginPath();
      ctx.moveTo((nearProj.x * 0.5 + 0.5) * W, (-nearProj.y * 0.5 + 0.5) * H);
      ctx.lineTo((proj.x * 0.5 + 0.5) * W, (-proj.y * 0.5 + 0.5) * H);
      ctx.stroke();
      ctx.setLineDash([]);
    }

    if (proj.z > 1 || Math.abs(proj.x) > 1.1 || Math.abs(proj.y) > 1.1) return;
    const sx = (proj.x * 0.5 + 0.5) * W, sy = (-proj.y * 0.5 + 0.5) * H;

    // 落点圈随距离收缩（越接近投弹时机圈越小）
    const r = clamp(2200 / Math.max(120, dist) * 16, 9, 46);
    const pulse = 1 + Math.sin(this.worldTime * 5) * 0.08;
    ctx.strokeStyle = 'rgba(255,200,80,0.95)';
    ctx.lineWidth = 1.8;
    ctx.beginPath(); ctx.arc(sx, sy, r * pulse, 0, Math.PI * 2); ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(sx - r - 7, sy); ctx.lineTo(sx - r + 2, sy);
    ctx.moveTo(sx + r - 2, sy); ctx.lineTo(sx + r + 7, sy);
    ctx.moveTo(sx, sy - r - 7); ctx.lineTo(sx, sy - r + 2);
    ctx.moveTo(sx, sy + r - 2); ctx.lineTo(sx, sy + r + 7);
    ctx.stroke();
    ctx.fillStyle = 'rgba(255,200,80,0.95)';
    ctx.fillRect(sx - 1.5, sy - 1.5, 3, 3);

    ctx.font = '11px ui-monospace, monospace';
    ctx.fillText(`投弹落点 ${impact.time.toFixed(1)}s · ${this.fmtDist(dist)}`, sx + r + 10, sy - 4);
    if (dist < 160) {
      ctx.fillStyle = 'rgba(120,255,160,0.95)';
      ctx.fillText('● 可以投弹', sx + r + 10, sy + 10);
    }

    // 落点是否压住某处高炮阵地 → 高亮提示
    for (const site of this.world.flakSites || []) {
      if (site.userData.alive === false) continue;
      const sp = this.world.siteWorldPos(site, _v1.clone());
      if (Math.hypot(sp.x - impact.x, sp.z - impact.z) < 120) {
        ctx.strokeStyle = 'rgba(255,80,70,0.95)';
        ctx.lineWidth = 2.4;
        ctx.beginPath(); ctx.arc(sx, sy, r + 9, 0, Math.PI * 2); ctx.stroke();
        ctx.fillStyle = 'rgba(255,90,80,0.95)';
        ctx.fillText('◎ 命中高炮阵地', sx + r + 10, sy + 24);
        break;
      }
    }
  }

  /**
   * 跑道补给引导：从机场外侧进近时，用方框标出跑道入口方向与补给进度。
   * 只在「机场方向 8km 内」显示，避免高空巡航时画面被占满。
   */
  _drawAirfieldGuide(ctx, W, H, p) {
    const baseX = 0, baseZ = 0;
    const dx = baseX - p.position.x, dz = baseZ - p.position.z;
    const distToBase = Math.hypot(dx, dz);
    if (distToBase > 8000) return;

    const st = this.rearmState(p);
    const lowFuel = p.fuel < 40;
    const needFuel = p.fuel < 99.5;
    const diffD = DIFFICULTY[this.settings.difficulty] || DIFFICULTY.normal;
    const needAmmo = p.weapons.missiles < diffD.missiles || p.weapons.bombs < diffD.bombs;
    if (!lowFuel && !needAmmo && !st.inZone) return;

    // 跑道入口标（世界坐标 → 屏幕）
    const cam = this.world.camera;
    const marker = _v3.set(0, this.world.heightAt(0, 0) + 12, -900);
    const proj = marker.clone().project(cam);
    ctx.save();
    ctx.font = '12px ui-monospace, monospace';
    ctx.textAlign = 'center';
    if (proj.z < 1 && Math.abs(proj.x) < 1 && Math.abs(proj.y) < 1) {
      const sx = (proj.x * 0.5 + 0.5) * W, sy = (-proj.y * 0.5 + 0.5) * H;
      const ok = st.ready;
      ctx.strokeStyle = ok ? 'rgba(120,255,160,0.95)' : 'rgba(255,200,80,0.9)';
      ctx.lineWidth = 2;
      const s = 26;
      ctx.beginPath();
      ctx.moveTo(sx - s, sy - s + 8); ctx.lineTo(sx - s, sy - s); ctx.lineTo(sx - s + 8, sy - s);
      ctx.moveTo(sx + s - 8, sy - s); ctx.lineTo(sx + s, sy - s); ctx.lineTo(sx + s, sy - s + 8);
      ctx.moveTo(sx - s, sy + s - 8); ctx.lineTo(sx - s, sy + s); ctx.lineTo(sx - s + 8, sy + s);
      ctx.moveTo(sx + s - 8, sy + s); ctx.lineTo(sx + s, sy + s); ctx.lineTo(sx + s, sy + s - 8);
      ctx.stroke();
      ctx.fillStyle = ok ? 'rgba(120,255,160,0.95)' : 'rgba(255,200,80,0.9)';
      ctx.fillText(ok ? '跑道补给就绪 · 按住 R' : '跑道入口 · 对正 ±Z 并贴地', sx, sy - s - 8);
    }
    // 底部提示
    ctx.textAlign = 'center';
    ctx.fillStyle = 'rgba(255,200,80,0.9)';
    const hint = st.ready
      ? (needFuel ? `补给中 油 ${Math.round(p.fuel)}%` : '按住 R 补弹')
      : `返回机场 ${this.fmtDist(distToBase)} · 跑道沿南北向`;
    ctx.fillText(hint, W / 2, H - 96);
    ctx.restore();
  }

  /** 阵亡后的屏幕提示 */
  _drawDeathHUD(ctx, W, H) {
    ctx.fillStyle = 'rgba(255, 60, 70, 0.09)';
    ctx.fillRect(0, 0, W, H);
    ctx.fillStyle = 'rgba(255,90,100,0.92)';
    ctx.font = 'bold 30px ui-monospace, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('机 体 损 毁', W / 2, H / 2 - 58);
    ctx.font = '14px ui-monospace, monospace';
    ctx.fillStyle = 'rgba(220,235,245,0.85)';
    ctx.fillText('弹射中…', W / 2, H / 2 - 22);
    ctx.textAlign = 'left';
  }

  _drawRadar() {
    const cv = this.el.radar;
    const ctx = cv.getContext('2d');
    const W = cv.width, H = cv.height;
    const cx = W / 2, cy = H / 2, R = W / 2 - 4;
    ctx.clearRect(0, 0, W, H);
    ctx.strokeStyle = 'rgba(41,224,255,0.45)';
    ctx.lineWidth = 1;
    for (const r of [R * 0.33, R * 0.66, R]) {
      ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.stroke();
    }
    ctx.beginPath();
    ctx.moveTo(cx - R, cy); ctx.lineTo(cx + R, cy);
    ctx.moveTo(cx, cy - R); ctx.lineTo(cx, cy + R);
    ctx.stroke();

    const p = this.player;
    if (!p) return;
    const fwd = p.getForward(_v1.clone());
    const right = p.getRight(_v2.clone());
    const RANGE = 20000;

    // 扫描线
    const sweep = (this.worldTime * 1.4) % (Math.PI * 2);
    ctx.strokeStyle = 'rgba(41,224,255,0.5)';
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx + Math.cos(sweep) * R, cy + Math.sin(sweep) * R);
    ctx.stroke();

    for (const e of this.enemies) {
      if (!e.alive) continue;
      const rel = e.position.clone().sub(p.position);
      const d = rel.length();
      if (d > RANGE) continue;
      const f = rel.clone().normalize().dot(fwd);
      const s = rel.clone().normalize().dot(right);
      const x = cx + (s) * (d / RANGE) * R;
      const y = cy - (f) * (d / RANGE) * R;
      const isLocked = e === this.lockedTarget;
      const alt = clamp(e.position.y / 3000, 0, 1);
      ctx.fillStyle = isLocked ? '#ff3b4e' : `rgba(255, ${Math.round(190 - alt * 90)}, ${Math.round(60 + alt * 120)}, 0.95)`;
      ctx.beginPath();
      const sz = e.variant === 'bomber' ? 5 : isLocked ? 4.5 : 3;
      ctx.rect(x - sz, y - sz, sz * 2, sz * 2);
      ctx.fill();
      if (isLocked) {
        ctx.strokeStyle = '#ff3b4e';
        ctx.beginPath(); ctx.arc(x, y, 8, 0, Math.PI * 2); ctx.stroke();
      }
    }

    // 敌方导弹
    for (const m of this.combat.missiles) {
      if (m.owner !== 'enemy') continue;
      const rel = m.pos.clone().sub(p.position);
      const d = rel.length();
      if (d > RANGE) continue;
      const c = rel.clone().normalize();
      const x = cx + c.dot(right) * (d / RANGE) * R;
      const y = cy - c.dot(fwd) * (d / RANGE) * R;
      ctx.fillStyle = '#ff7a2a';
      ctx.fillRect(x - 2, y - 2, 4, 4);
    }

    // 友军僚机
    for (const a of this.allies) {
      if (!a.alive) continue;
      const rel = a.position.clone().sub(p.position);
      const d = rel.length();
      if (d > RANGE) continue;
      const c = rel.clone().normalize();
      const x = cx + c.dot(right) * (d / RANGE) * R;
      const y = cy - c.dot(fwd) * (d / RANGE) * R;
      ctx.fillStyle = '#4dffa1';
      ctx.beginPath();
      ctx.arc(x, y, 3, 0, Math.PI * 2);
      ctx.fill();
    }

    // 地面高炮阵地（对地目标 + 低空威胁，始终标绘）
    for (const site of this.world.flakSites || []) {
      if (site.userData.alive === false) continue;
      const sp = this.world.siteWorldPos(site, _v3);
      const rel = new THREE.Vector3(sp.x - p.position.x, 0, sp.z - p.position.z);
      const d = rel.length();
      if (d > RANGE) continue;
      const c = rel.normalize();
      const x = cx + c.dot(right) * (d / RANGE) * R;
      const y = cy - c.dot(fwd) * (d / RANGE) * R;
      const danger = d < 2800 && p.position.y < 2100;
      ctx.strokeStyle = danger ? '#ff4d4d' : '#ff8c3c';
      ctx.lineWidth = danger ? 2.2 : 1.6;
      ctx.beginPath();
      ctx.moveTo(x - 4, y - 4); ctx.lineTo(x + 4, y + 4);
      ctx.moveTo(x + 4, y - 4); ctx.lineTo(x - 4, y + 4);
      ctx.stroke();
      if (danger) {
        ctx.beginPath(); ctx.arc(x, y, 7, 0, Math.PI * 2); ctx.stroke();
      }
    }

    // 玩家
    ctx.fillStyle = '#4dffa1';
    ctx.beginPath();
    ctx.moveTo(cx, cy - 6); ctx.lineTo(cx - 5, cy + 5); ctx.lineTo(cx + 5, cy + 5);
    ctx.closePath(); ctx.fill();

    // 目标总数提示 + 雷达量程
    ctx.font = '10px ui-monospace, monospace';
    ctx.fillStyle = 'rgba(41,224,255,0.8)';
    ctx.fillText(`ALT ${this.fmtAlt(p.position.y)}`, 6, 12);
    ctx.fillStyle = 'rgba(41,224,255,0.55)';
    ctx.fillText(`R ${this.fmtDist(RANGE / 3)}`, 6, H - 4);
    ctx.fillText(`OIL ${Math.round(p.fuel)}%`, W - 52, H - 4);
    if (!p.alive) {
      ctx.fillStyle = 'rgba(255,90,100,0.9)';
      ctx.fillText('NO SIGNAL', cx - 26, cy - 10);
    }
  }
}

// 启动
window.addEventListener('error', (ev) => {
  const tip = document.getElementById('loadTip');
  if (tip && document.getElementById('loading') && !document.getElementById('loading').classList.contains('hidden')) {
    tip.innerHTML = `⚠ 运行错误：${ev.message}`;
  }
});

// 启动（module 脚本在解析后立即执行，直接实例化即可）
window.__game = new SkyStrike();
