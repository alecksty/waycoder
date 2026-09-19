// audio.js — 程序化音效模块：全部声音用 Web Audio API 实时合成，无任何音频文件。
// 提供放置/破坏/跳跃/落地/拾取等方块音效，以及可选的环境风声循环。

export class AudioFX {
  constructor() {
    this.ctx = null;
    this.master = null;
    this.enabled = true;
    this._initPad = null;
  }

  /** 需在用户手势后调用（浏览器要求音频上下文由手势激活） */
  init() {
    if (this.ctx) return;
    try {
      const Ctx = window.AudioContext || window.webkitAudioContext;
      this.ctx = new Ctx();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.5;
      this.master.connect(this.ctx.destination);
      this._startAmbient();
    } catch (e) { /* 静默失败 */ }
  }

  _startAmbient() {
    // 极轻的白噪风声背景
    const len = this.ctx.sampleRate * 4;
    const buf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
    const data = buf.getChannelData(0);
    for (let i = 0; i < len; i++) data[i] = (Math.random() * 2 - 1) * 0.5;
    const src = this.ctx.createBufferSource();
    src.buffer = buf;
    src.loop = true;
    const filt = this.ctx.createBiquadFilter();
    filt.type = 'lowpass';
    filt.frequency.value = 500;
    const g = this.ctx.createGain();
    g.gain.value = 0.02;
    src.connect(filt).connect(g).connect(this.master);
    src.start();
  }

  _env(gainNode, peak, dur) {
    const t = this.ctx.currentTime;
    gainNode.gain.setValueAtTime(0, t);
    gainNode.gain.linearRampToValueAtTime(peak, t + 0.01);
    gainNode.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  }

  _tone(freq, dur, type = 'oscillator', peak = 0.3) {
    if (!this.ctx || !this.enabled) return;
    const osc = this.ctx.createOscillator();
    osc.type = type;
    osc.frequency.value = freq;
    const g = this.ctx.createGain();
    this._env(g, peak, dur);
    osc.connect(g).connect(this.master);
    osc.start();
    osc.stop(this.ctx.currentTime + dur + 0.05);
  }

  _noise(dur, peak = 0.2, freq = 1000) {
    if (!this.ctx || !this.enabled) return;
    const len = Math.floor(this.ctx.sampleRate * dur);
    const buf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / len);
    const src = this.ctx.createBufferSource();
    src.buffer = buf;
    const filt = this.ctx.createBiquadFilter();
    filt.type = 'bandpass';
    filt.frequency.value = freq;
    const g = this.ctx.createGain();
    this._env(g, peak, dur);
    src.connect(filt).connect(g).connect(this.master);
    src.start();
  }

  dig()   { this._noise(0.18, 0.32, 700); this._tone(180, 0.12, 'square', 0.12); }
  place() { this._tone(240, 0.15, 'triangle', 0.28); this._noise(0.1, 0.2, 900); }
  jump()  { this._tone(330, 0.18, 'sine', 0.25); this._tone(440, 0.1, 'sine', 0.18); }
  land()  { this._noise(0.12, 0.22, 400); }
  select(){ this._tone(660, 0.05, 'square', 0.15); }
  step()  { this._noise(0.05, 0.08, 320); }

  setEnabled(on) { this.enabled = on; }
  resume() { if (this.ctx && this.ctx.state === 'suspended') this.ctx.resume(); }
}
