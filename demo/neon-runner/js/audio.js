// =====================================================================
// audio.js — Web Audio API procedural SFX & music system
// Generates all sounds on the fly (no audio files needed).
// =====================================================================

export class AudioSystem {
  constructor() {
    this.ctx = null;
    this.master = null;
    this.muted = false;
    this.musicGain = null;
    this.musicNodes = null;
    this.musicTimer = null;
  }

  _ensureCtx() {
    if (this.ctx) return;
    const Ctx = window.AudioContext || window.webkitAudioContext;
    this.ctx = new Ctx();
    this.master = this.ctx.createGain();
    this.master.gain.value = 0.5;
    this.master.connect(this.ctx.destination);
  }

  resume() {
    this._ensureCtx();
    if (this.ctx.state === 'suspended') this.ctx.resume();
  }

  toggleMute() {
    this.muted = !this.muted;
    if (this.master) this.master.gain.value = this.muted ? 0 : 0.5;
    return this.muted;
  }

  // ----------------- One-shot SFX -----------------

  _tone({ freq = 440, dur = 0.15, type = 'sine', vol = 0.3, slide = 0, attack = 0.005, release = 0.05 }) {
    if (!this.ctx || this.muted) return;
    const t0 = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, t0);
    if (slide) osc.frequency.exponentialRampToValueAtTime(Math.max(20, freq + slide), t0 + dur);
    g.gain.setValueAtTime(0, t0);
    g.gain.linearRampToValueAtTime(vol, t0 + attack);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur + release);
    osc.connect(g).connect(this.master);
    osc.start(t0);
    osc.stop(t0 + dur + release + 0.02);
  }

  jump()       { this._tone({ freq: 380, dur: 0.14, type: 'square',   vol: 0.18, slide: 280 }); }
  doubleJump() { this._tone({ freq: 540, dur: 0.12, type: 'square',   vol: 0.18, slide: 360 }); }
  slide()      { this._tone({ freq: 220, dur: 0.18, type: 'sawtooth', vol: 0.10, slide: -100 }); }
  coin()       { this._tone({ freq: 880, dur: 0.08, type: 'triangle', vol: 0.22, slide: 440 }); this._tone({ freq: 1320, dur: 0.06, type: 'sine', vol: 0.14, attack: 0.04 }); }
  crash()      {
    // Noise burst
    if (!this.ctx || this.muted) return;
    const t0 = this.ctx.currentTime;
    const bufSize = this.ctx.sampleRate * 0.4;
    const buf = this.ctx.createBuffer(1, bufSize, this.ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < bufSize; i++) {
      const t = i / bufSize;
      d[i] = (Math.random() * 2 - 1) * Math.pow(1 - t, 2.2);
    }
    const src = this.ctx.createBufferSource();
    src.buffer = buf;
    const g = this.ctx.createGain(); g.gain.value = 0.45;
    const filter = this.ctx.createBiquadFilter();
    filter.type = 'lowpass'; filter.frequency.value = 800;
    src.connect(filter).connect(g).connect(this.master);
    src.start(t0);
  }
  hit()        { this._tone({ freq: 180, dur: 0.16, type: 'sawtooth', vol: 0.28, slide: -80 }); }
  levelUp()    {
    this._tone({ freq: 523, dur: 0.10, type: 'triangle', vol: 0.18 });
    setTimeout(() => this._tone({ freq: 659, dur: 0.10, type: 'triangle', vol: 0.18 }), 80);
    setTimeout(() => this._tone({ freq: 784, dur: 0.18, type: 'triangle', vol: 0.20 }), 160);
  }
  click()      { this._tone({ freq: 700, dur: 0.04, type: 'square', vol: 0.10 }); }
  powerUp()    {
    this._tone({ freq: 440, dur: 0.08, type: 'sine', vol: 0.20, slide: 220 });
    this._tone({ freq: 660, dur: 0.10, type: 'sine', vol: 0.16, attack: 0.04, slide: 220 });
  }
  nearMiss()   { this._tone({ freq: 1200, dur: 0.05, type: 'triangle', vol: 0.10 }); }

  // ----------------- Background music (procedural synthwave) -----------------

  startMusic() {
    this._ensureCtx();
    if (this.musicNodes || this.muted) return;
    const ctx = this.ctx;
    this.musicGain = ctx.createGain();
    this.musicGain.gain.value = 0.13;
    this.musicGain.connect(this.master);

    // Bass arpeggio: A1, E2, A2, C3, E3, A2, C3, E3 — 16th-note loop
    const bassPattern = [55, 82.4, 110, 130.8, 164.8, 110, 130.8, 164.8];
    const stepDur = 0.18; // ~166 BPM
    const totalSteps = bassPattern.length;
    const osc = ctx.createOscillator();
    const g = ctx.createGain();
    osc.type = 'sawtooth';
    g.gain.value = 0;
    const filter = ctx.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.value = 700;
    filter.Q.value = 4;
    osc.connect(filter).connect(g).connect(this.musicGain);
    osc.start();

    // Pad
    const pad = ctx.createOscillator();
    const padG = ctx.createGain();
    pad.type = 'triangle';
    padG.gain.value = 0.04;
    pad.frequency.value = 220;
    pad.connect(padG).connect(this.musicGain);
    pad.start();

    let step = 0;
    const tick = () => {
      if (!this.musicNodes) return;
      const t = ctx.currentTime;
      const f = bassPattern[step % totalSteps];
      osc.frequency.setValueAtTime(f, t);
      g.gain.cancelScheduledValues(t);
      g.gain.setValueAtTime(0, t);
      g.gain.linearRampToValueAtTime(0.22, t + 0.01);
      g.gain.exponentialRampToValueAtTime(0.001, t + stepDur * 0.9);
      step++;
    };
    this.musicTimer = setInterval(tick, stepDur * 1000);
    this.musicNodes = { osc, g, filter, pad, padG };
  }

  stopMusic() {
    if (this.musicTimer) { clearInterval(this.musicTimer); this.musicTimer = null; }
    if (this.musicNodes) {
      try {
        this.musicNodes.osc.stop();
        this.musicNodes.pad.stop();
      } catch (e) { /* already stopped */ }
      this.musicNodes = null;
    }
    if (this.musicGain) { this.musicGain.disconnect(); this.musicGain = null; }
  }
}
