/* ============================================================
   audio.js — Web Audio API 程序化合成音效（零音频文件）
   ============================================================ */
(function (G) {
  'use strict';

  const CFG = G.Config;

  class AudioSys {
    constructor() {
      this.ctx = null;
      this.master = null;
      this.noiseBuf = null;
      this.enabled = CFG.SOUND.enabled;
    }

    /** 首次用户交互时初始化（浏览器自动播放策略要求） */
    init() {
      if (this.ctx) { if (this.ctx.state === 'suspended') this.ctx.resume(); return; }
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return;
      this.ctx = new AC();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.5;
      this.master.connect(this.ctx.destination);
      // 预生成 1s 白噪声缓冲
      const len = this.ctx.sampleRate;
      this.noiseBuf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
      const data = this.noiseBuf.getChannelData(0);
      for (let i = 0; i < len; i++) data[i] = Math.random() * 2 - 1;
    }

    setEnabled(v) { this.enabled = v; CFG.SOUND.enabled = v; }
    toggle() { this.setEnabled(!this.enabled); return this.enabled; }

    /* ---------- 基础合成器 ---------- */
    _tone(type, f0, f1, dur, vol, delay = 0) {
      if (!this.ctx || !this.enabled) return;
      const t0 = this.ctx.currentTime + delay;
      const osc = this.ctx.createOscillator();
      const g = this.ctx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(f0, t0);
      if (f1 != null) osc.frequency.exponentialRampToValueAtTime(Math.max(1, f1), t0 + dur);
      g.gain.setValueAtTime(vol, t0);
      g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
      osc.connect(g); g.connect(this.master);
      osc.start(t0); osc.stop(t0 + dur + 0.02);
    }

    _noise(dur, vol, filterFreq, delay = 0, filterType = 'lowpass') {
      if (!this.ctx || !this.enabled) return;
      const t0 = this.ctx.currentTime + delay;
      const src = this.ctx.createBufferSource();
      src.buffer = this.noiseBuf;
      src.loop = true;
      const f = this.ctx.createBiquadFilter();
      f.type = filterType;
      f.frequency.setValueAtTime(filterFreq, t0);
      f.frequency.exponentialRampToValueAtTime(Math.max(60, filterFreq * 0.15), t0 + dur);
      const g = this.ctx.createGain();
      g.gain.setValueAtTime(vol, t0);
      g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
      src.connect(f); f.connect(g); g.connect(this.master);
      src.start(t0); src.stop(t0 + dur + 0.02);
    }

    /* ---------- 游戏音效 ---------- */
    shoot()  { this._tone('square', 880, 240, 0.09, 0.12); }
    hit()    { this._tone('square', 220, 90, 0.08, 0.20); }
    explode(big = false) {
      this._noise(big ? 0.9 : 0.42, big ? 0.7 : 0.45, big ? 900 : 1600);
      this._tone('sine', big ? 140 : 200, 40, big ? 0.7 : 0.4, big ? 0.5 : 0.3);
    }
    powerup() {
      this._tone('sine', 520, 0, 0.09, 0.18);
      this._tone('sine', 780, 0, 0.09, 0.18, 0.08);
      this._tone('sine', 1040, 0, 0.14, 0.20, 0.16);
    }
    bomb() {
      this._noise(1.1, 0.8, 700);
      this._tone('sawtooth', 400, 50, 0.8, 0.4);
    }
    playerHit() {
      this._tone('sawtooth', 320, 60, 0.4, 0.4);
      this._noise(0.5, 0.5, 1200);
    }
    gameover() {
      this._tone('sawtooth', 440, 110, 0.5, 0.3);
      this._tone('sawtooth', 330, 82, 0.5, 0.3, 0.45);
      this._tone('sawtooth', 220, 55, 0.9, 0.3, 0.9);
    }
    bossWarn() {
      this._tone('square', 140, 140, 0.4, 0.3);
      this._tone('square', 175, 175, 0.4, 0.3, 0.45);
    }
    uiClick() { this._tone('sine', 660, 660, 0.06, 0.15); }
  }

  G.Audio = new AudioSys();

})(window.Game = window.Game || {});
