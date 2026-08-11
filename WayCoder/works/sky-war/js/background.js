/* ============================================================
   background.js — 三层视差星空 + 流星 + 星云
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class Star {
    constructor(layer) {
      this.layer = layer;
      this.reset(true);
    }
    reset(anywhere) {
      this.x = U.rand(0, CFG.LOGIC_W);
      this.y = anywhere ? U.rand(0, CFG.LOGIC_H) : -5;
      this.tw = U.rand(0.6, 1.4);      // 闪烁相位
      this.ph = U.rand(0, Math.PI * 2);
    }
    update(dt) {
      this.y += this.layer.speed * dt;
      if (this.y > CFG.LOGIC_H + 5) this.reset(false);
    }
    draw(ctx, time) {
      const twinkle = 0.6 + 0.4 * Math.sin(time * this.tw + this.ph);
      ctx.globalAlpha = this.layer.alpha * twinkle;
      ctx.fillStyle = this.layer.color;
      const s = this.layer.size;
      ctx.fillRect(this.x, this.y, s, s);
      // 大星画十字光芒
      if (s >= 3) {
        ctx.fillRect(this.x - 2, this.y, 5, 1);
        ctx.fillRect(this.x, this.y - 2, 1, 5);
      }
      ctx.globalAlpha = 1;
    }
  }

  class Meteor {
    constructor() {
      this.reset();
    }
    reset() {
      const fromLeft = Math.random() < 0.5;
      this.x = fromLeft ? U.rand(-40, CFG.LOGIC_W * 0.3) : U.rand(CFG.LOGIC_W * 0.7, CFG.LOGIC_W + 40);
      this.y = U.rand(-80, -20);
      this.vx = U.rand(140, 240) * (fromLeft ? 1 : -1);
      this.vy = U.rand(200, 320);
      this.len = U.rand(40, 90);
      this.life = 0;
    }
    update(dt) {
      this.x += this.vx * dt;
      this.y += this.vy * dt;
      this.life += dt;
      if (this.y > CFG.LOGIC_H + 60 || this.x < -100 || this.x > CFG.LOGIC_W + 100) this.reset();
    }
    draw(ctx) {
      const head = Math.atan2(this.vy, this.vx);
      U.glowLine(ctx, this.x, this.y,
                 this.x - Math.cos(head) * this.len,
                 this.y - Math.sin(head) * this.len,
                 2, 'rgba(150,200,255,0.7)');
      ctx.fillStyle = '#fff';
      ctx.beginPath();
      ctx.arc(this.x, this.y, 1.6, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  class Background {
    constructor() {
      this.stars = [];
      for (const l of CFG.BG.layers) {
        for (let i = 0; i < l.count; i++) this.stars.push(new Star(l));
      }
      this.meteor = new Meteor();
      this.meteorTimer = U.rand(1, 4);
      // 预渲染星云（静态，避免每帧渐变开销）
      this.nebula = document.createElement('canvas');
      this.nebula.width = CFG.LOGIC_W;
      this.nebula.height = CFG.LOGIC_H;
      this._paintNebula();
    }

    _paintNebula() {
      const c = this.nebula.getContext('2d');
      const pts = [
        { x: 90,  y: 160, r: 150, c: 'rgba(60, 60, 180, 0.28)' },
        { x: 390, y: 380, r: 130, c: 'rgba(120, 40, 160, 0.20)' },
        { x: 240, y: 620, r: 160, c: 'rgba(30, 90, 190, 0.22)' },
        { x: 60,  y: 520, r: 100, c: 'rgba(160, 60, 80, 0.12)' },
      ];
      for (const p of pts) {
        const g = c.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.r);
        g.addColorStop(0, p.c);
        g.addColorStop(1, 'rgba(0,0,0,0)');
        c.fillStyle = g;
        c.fillRect(0, 0, CFG.LOGIC_W, CFG.LOGIC_H);
      }
    }

    update(dt, time) {
      for (const s of this.stars) s.update(dt);
      this.meteorTimer -= dt;
      if (this.meteorTimer <= 0) {
        this.meteor.reset();
        this.meteorTimer = U.rand(3, 8);
      }
      this.meteor.update(dt);
    }

    draw(ctx, time) {
      ctx.drawImage(this.nebula, 0, 0);
      for (const s of this.stars) s.draw(ctx, time);
      this.meteor.draw(ctx);
    }
  }

  G.Background = Background;

})(window.Game = window.Game || {});
