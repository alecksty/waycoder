/*
 * audio.js —— WebAudio 合成音效 + 简易 8-bit 风格背景音乐
 * 零外部资源：音效全部用振荡器/噪声实时合成
 */
'use strict';

const AudioSys = {
  ctx: null,
  master: null,
  enabled: true,
  bgmOn: false,
  step: 0,
  timer: null,

  /* 必须在用户手势后调用（浏览器自动播放策略） */
  init() {
    if (this.ctx) return;
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return;
      this.ctx = new AC();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.5;
      this.master.connect(this.ctx.destination);
    } catch (e) { /* 无音频环境静默降级 */ }
  },

  resume() {
    if (this.ctx && this.ctx.state === 'suspended') this.ctx.resume();
  },

  /* 基础振荡器音 */
  tone(freq, dur, type, vol, slideTo) {
    if (!this.ctx || !this.enabled) return;
    const t0 = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = type || 'square';
    osc.frequency.setValueAtTime(freq, t0);
    if (slideTo) osc.frequency.exponentialRampToValueAtTime(Math.max(20, slideTo), t0 + dur);
    gain.gain.setValueAtTime(vol || 0.2, t0);
    gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    osc.connect(gain);
    gain.connect(this.master);
    osc.start(t0);
    osc.stop(t0 + dur + 0.02);
  },

  /* 白噪声 */
  noise(dur, vol, filterFreq) {
    if (!this.ctx || !this.enabled) return;
    const t0 = this.ctx.currentTime;
    const len = Math.max(1, Math.floor(this.ctx.sampleRate * dur));
    const buf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
    const data = buf.getChannelData(0);
    for (let i = 0; i < len; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / len);
    const src = this.ctx.createBufferSource();
    src.buffer = buf;
    const gain = this.ctx.createGain();
    gain.gain.setValueAtTime(vol || 0.3, t0);
    gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    let node = src;
    if (filterFreq) {
      const f = this.ctx.createBiquadFilter();
      f.type = 'bandpass';
      f.frequency.value = filterFreq;
      src.connect(f);
      node = f;
    }
    node.connect(gain);
    gain.connect(this.master);
    src.start(t0);
  },

  /* 音效库 */
  sfx(name) {
    if (!this.ctx || !this.enabled) return;
    switch (name) {
      case 'dig':    this.noise(0.09, 0.35, 900); break;
      case 'place':  this.tone(220, 0.06, 'square', 0.25, 130); break;
      case 'pickup': this.tone(880, 0.07, 'sine', 0.28); this.tone(1318, 0.1, 'sine', 0.24); break;
      case 'hurt':   this.tone(170, 0.22, 'sawtooth', 0.4, 70); this.noise(0.1, 0.2, 300); break;
      case 'swing':  this.noise(0.07, 0.18, 1600); break;
      case 'jump':   this.tone(280, 0.12, 'triangle', 0.22, 520); break;
      case 'craft':  this.tone(523, 0.09, 'square', 0.2); this.tone(659, 0.09, 'square', 0.2); this.tone(784, 0.14, 'square', 0.2); break;
      case 'kill':   this.tone(420, 0.16, 'square', 0.28, 90); this.noise(0.12, 0.2, 700); break;
      case 'death':  this.tone(300, 0.6, 'sawtooth', 0.4, 45); break;
      case 'open':   this.tone(620, 0.06, 'sine', 0.18, 820); break;
      case 'click':  this.tone(760, 0.045, 'square', 0.14); break;
      case 'error':  this.tone(140, 0.16, 'square', 0.22, 100); break;
      case 'respawn':this.tone(392, 0.1, 'sine', 0.25); this.tone(523, 0.14, 'sine', 0.25); break;
      case 'eat':   this.tone(300, 0.08, 'square', 0.2, 200); this.tone(240, 0.1, 'square', 0.18, 150); break;
      default: break;
    }
  },

  /* ---------- 背景音乐：16 步音序器 ---------- */
  bgmStart() {
    if (this.bgmOn || !this.ctx) return;
    this.bgmOn = true;
    this.step = 0;
    this.timer = setInterval(() => this.bgmTick(), 150);
  },

  bgmStop() {
    this.bgmOn = false;
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
  },

  bgmTick() {
    if (!this.ctx || !this.enabled) return;
    const day = Game && Game.isDay;
    /* 大调（白天）/ 小调（夜晚） */
    const arp = day ? [261.6, 329.6, 392.0, 523.3] : [220.0, 261.6, 311.1, 392.0];
    const bass = day ? [130.8, 98.0, 110.0, 87.3] : [110.0, 82.4, 87.3, 65.4];
    const step = this.step % 16;
    /* 低音每 4 步 */
    if (step % 4 === 0) this.tone(bass[(step / 4) | 0], 0.24, 'triangle', 0.1);
    /* 琶音 16 分音符，偶尔休止 */
    if (step % 2 === 1 && step !== 9 && step !== 13) {
      this.tone(arp[(step >> 1) % 4] * (step % 4 === 3 ? 2 : 1), 0.1, 'square', 0.05);
    }
    this.step++;
  }
};
