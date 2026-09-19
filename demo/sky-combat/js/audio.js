// =====================================================================
// audio.js — 程序化音效引擎（Web Audio API，无任何音频文件）
//   · 机炮：噪声脉冲 + 带通滤波
//   · 爆炸：低频噪声包络 + 低通扫频
//   · 锁定/告警：方波提示音
//   · 引擎：持续低频锯齿 + 噪声，随推力变化
// =====================================================================
import { clamp } from './utils.js';

export class AudioSystem {
  constructor() {
    this.enabled = true;
    this.ctx = null;
    this.master = null;
    this.engineNodes = null;
    this.lastGun = 0;
    this.noiseBuffer = null;
  }

  /** 必须由用户手势触发才能启动 AudioContext */
  init() {
    if (this.ctx) {
      if (this.ctx.state === 'suspended') this.ctx.resume();
      return;
    }
    const Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) { this.enabled = false; return; }
    this.ctx = new Ctx();
    this.master = this.ctx.createGain();
    this.master.gain.value = 0.42;
    this.master.connect(this.ctx.destination);

    // 预生成白噪声缓冲
    const len = this.ctx.sampleRate * 2;
    const buf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
    const data = buf.getChannelData(0);
    for (let i = 0; i < len; i++) data[i] = Math.random() * 2 - 1;
    this.noiseBuffer = buf;
  }

  setEnabled(on) {
    this.enabled = on;
    if (this.master) this.master.gain.value = on ? 0.42 : 0;
  }

  _noise(dur, gain, filterType, freq, q = 1) {
    if (!this.enabled || !this.ctx) return null;
    const src = this.ctx.createBufferSource();
    src.buffer = this.noiseBuffer;
    src.loop = true;
    const filt = this.ctx.createBiquadFilter();
    filt.type = filterType;
    filt.frequency.value = freq;
    filt.Q.value = q;
    const g = this.ctx.createGain();
    const t = this.ctx.currentTime;
    g.gain.setValueAtTime(gain, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    src.connect(filt).connect(g).connect(this.master);
    src.start(t);
    src.stop(t + dur + 0.02);
    return { src, filt, g };
  }

  _tone(freq, dur, gain, type = 'square', slideTo = null) {
    if (!this.enabled || !this.ctx) return;
    const osc = this.ctx.createOscillator();
    osc.type = type;
    const g = this.ctx.createGain();
    const t = this.ctx.currentTime;
    osc.frequency.setValueAtTime(freq, t);
    if (slideTo) osc.frequency.exponentialRampToValueAtTime(slideTo, t + dur);
    g.gain.setValueAtTime(0.0001, t);
    g.gain.exponentialRampToValueAtTime(gain, t + 0.008);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    osc.connect(g).connect(this.master);
    osc.start(t);
    osc.stop(t + dur + 0.02);
  }

  // -------------------- 武器音效 --------------------
  gun() {
    if (!this.ctx || !this.enabled) return;
    const now = this.ctx.currentTime;
    if (now - this.lastGun < 0.055) return;
    this.lastGun = now;
    this._noise(0.09, 0.32, 'bandpass', 1500 + Math.random() * 500, 1.2);
    this._tone(140, 0.06, 0.1, 'sawtooth', 70);
  }

  enemyGun() { this._noise(0.11, 0.1, 'bandpass', 900, 1.4); }

  missile() {
    this._noise(0.7, 0.3, 'lowpass', 1200, 0.8);
    this._tone(320, 0.55, 0.14, 'sawtooth', 120);
  }

  enemyMissile() { this._tone(260, 0.5, 0.09, 'sawtooth', 100); }

  bombRelease() { this._noise(0.25, 0.16, 'lowpass', 700); }

  flare() {
    this._noise(0.5, 0.22, 'highpass', 2200);
    this._tone(880, 0.22, 0.06, 'triangle', 1400);
  }

  explosion(distance = 300) {
    const att = clamp(1 - distance / 3500, 0.05, 1);
    this._noise(0.9, 0.55 * att, 'lowpass', 340, 0.9);
    this._tone(60, 0.5, 0.35 * att, 'sine', 26);
  }

  hit() { this._noise(0.14, 0.3, 'bandpass', 2600, 2); }

  // -------------------- 界面音效 --------------------
  lock() {
    this._tone(1180, 0.09, 0.1, 'square');
    setTimeout(() => this._tone(1560, 0.1, 0.09, 'square'), 90);
  }

  launch() { this._tone(600, 0.16, 0.12, 'triangle', 1200); }

  warn() { this._tone(900, 0.14, 0.14, 'square', 620); }

  overheat() { this._tone(220, 0.35, 0.13, 'sawtooth', 110); }

  missionComplete() {
    const seq = [523, 659, 784, 1047];
    seq.forEach((f, i) => setTimeout(() => this._tone(f, 0.38, 0.14, 'triangle'), i * 170));
  }

  missionFailed() {
    const seq = [420, 340, 260, 180];
    seq.forEach((f, i) => setTimeout(() => this._tone(f, 0.45, 0.15, 'sawtooth'), i * 190));
  }

  uiClick() { this._tone(760, 0.05, 0.08, 'square'); }

  // -------------------- 战况播报 --------------------
  /** 无线电提示音（僚机指令/消息） */
  radio() {
    this._tone(1320, 0.06, 0.09, 'square');
    setTimeout(() => this._tone(1760, 0.07, 0.08, 'square'), 70);
    setTimeout(() => this._tone(1320, 0.06, 0.07, 'square'), 150);
  }

  /** 命中反馈：短促金属脆音 */
  ping() { this._tone(2200, 0.05, 0.09, 'triangle', 1500); }

  /** 机炮过热警告 */
  overheatWarn() {
    this._tone(320, 0.1, 0.11, 'square');
    this._tone(240, 0.12, 0.1, 'square');
  }

  /** 地面目标摧毁（低沉爆炸） */
  groundHit() {
    this._noise(1.2, 0.5, 'lowpass', 220, 0.7);
    this._tone(48, 0.8, 0.34, 'sine', 22);
  }

  /** 低高度/拉起警告 */
  pullUp() {
    const seq = [880, 660, 880, 660];
    seq.forEach((f, i) => setTimeout(() => this._tone(f, 0.1, 0.11, 'square'), i * 110));
  }

  /** 战损：蒙皮/液压受损警报 */
  damage() {
    this._noise(0.3, 0.28, 'bandpass', 800, 2.2);
    this._tone(180, 0.3, 0.12, 'square', 90);
  }

  // -------------------- 引擎轰鸣 --------------------
  startEngine() {
    if (!this.ctx || this.engineNodes) return;
    const t = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    osc.type = 'sawtooth';
    osc.frequency.value = 62;
    const osc2 = this.ctx.createOscillator();
    osc2.type = 'square';
    osc2.frequency.value = 31;

    const src = this.ctx.createBufferSource();
    src.buffer = this.noiseBuffer;
    src.loop = true;
    const nf = this.ctx.createBiquadFilter();
    nf.type = 'lowpass';
    nf.frequency.value = 420;

    const g = this.ctx.createGain();
    g.gain.value = 0.0001;
    const ng = this.ctx.createGain();
    ng.gain.value = 0.03;

    osc.connect(g); osc2.connect(g);
    src.connect(nf).connect(ng).connect(this.master);
    g.connect(this.master);

    osc.start(t); osc2.start(t); src.start(t);
    this.engineNodes = { osc, osc2, src, g, ng, nf };
  }

  /** 每帧根据油门/加力调整引擎音色 */
  updateEngine(throttle, afterburner, alive) {
    if (!this.engineNodes || !this.ctx) return;
    const t = this.ctx.currentTime;
    const base = alive ? 58 + throttle * 46 + (afterburner ? 26 : 0) : 24;
    this.engineNodes.osc.frequency.setTargetAtTime(base, t, 0.15);
    this.engineNodes.osc2.frequency.setTargetAtTime(base * 0.5, t, 0.2);
    this.engineNodes.nf.frequency.setTargetAtTime(360 + throttle * 620 + (afterburner ? 400 : 0), t, 0.2);
    this.engineNodes.g.gain.setTargetAtTime(alive ? 0.05 + throttle * 0.07 + (afterburner ? 0.05 : 0) : 0.0001, t, 0.2);
  }

  stopEngine() {
    if (!this.engineNodes) return;
    try {
      this.engineNodes.osc.stop();
      this.engineNodes.osc2.stop();
      this.engineNodes.src.stop();
    } catch { /* 已停止 */ }
    this.engineNodes = null;
  }
}
