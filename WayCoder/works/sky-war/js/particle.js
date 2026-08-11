/* ============================================================
   particle.js — 粒子系统（爆炸 / 引擎尾焰 / 火花）
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;

  class Particle {
    constructor(x, y, vx, vy, life, size, color, drag = 0.98, gravity = 0) {
      this.x = x; this.y = y;
      this.vx = vx; this.vy = vy;
      this.life = life;
      this.maxLife = life;
      this.size = size;
      this.color = color;
      this.drag = drag;
      this.gravity = gravity;
      this.add = 0;           // 增量绘制（每次叠加，用于轨迹）
    }
    update(dt) {
      this.x += this.vx * dt;
      this.y += this.vy * dt;
      this.vx *= Math.pow(this.drag, dt * 60);
      this.vy *= Math.pow(this.drag, dt * 60);
      this.vy += this.gravity * dt;
      this.life -= dt;
      return this.life > 0;
    }
    draw(ctx) {
      const t = U.clamp(this.life / this.maxLife, 0, 1);
      ctx.globalAlpha = t;
      if (this.add) ctx.globalCompositeOperation = 'lighter';
      ctx.fillStyle = this.color;
      ctx.beginPath();
      ctx.arc(this.x, this.y, this.size * (0.4 + 0.6 * t), 0, Math.PI * 2);
      ctx.fill();
      ctx.globalCompositeOperation = 'source-over';
      ctx.globalAlpha = 1;
    }
  }

  class ParticleSystem {
    constructor() {
      this.list = [];
    }

    spawn(p) { this.list.push(p); }

    /* ---------- 预置效果 ---------- */
    explosion(x, y, scale = 1, color = null) {
      const n = Math.floor(26 * scale);
      const colors = color
        ? [color, '#ffd76a', '#ffffff']
        : ['#ff7a3c', '#ffb23c', '#ffd76a', '#ff5a3c', '#ffffff'];
      for (let i = 0; i < n; i++) {
        const a = U.rand(0, Math.PI * 2);
        const sp = U.rand(40, 240) * scale;
        this.spawn(new Particle(
          x + U.rand(-6, 6), y + U.rand(-6, 6),
          Math.cos(a) * sp, Math.sin(a) * sp,
          U.rand(0.35, 0.85), U.rand(2, 5) * scale,
          U.randPick(colors), 0.94, 60
        ));
      }
      // 冲击波环
      this.spawn(new Particle(x, y, 0, 0, 0.32, 6 * scale, '#fff', 1, 0));
    }

    spark(x, y, color = '#aadcff') {
      for (let i = 0; i < 6; i++) {
        const a = U.rand(0, Math.PI * 2);
        const sp = U.rand(30, 120);
        this.spawn(new Particle(x, y, Math.cos(a) * sp, Math.sin(a) * sp,
          U.rand(0.15, 0.35), U.rand(1, 2.5), color, 0.92, 0));
      }
    }

    // 引擎尾焰（从喷口持续喷出）
    thrust(x, y, dirAngle) {
      const a = dirAngle + Math.PI + U.rand(-0.35, 0.35);
      const sp = U.rand(60, 140);
      this.spawn(new Particle(
        x + Math.cos(dirAngle + Math.PI) * 12,
        y + Math.sin(dirAngle + Math.PI) * 12,
        Math.cos(a) * sp, Math.sin(a) * sp,
        U.rand(0.12, 0.26), U.rand(2, 4.5),
        Math.random() < 0.5 ? '#ffd76a' : '#ff8a3c', 0.9, 0
      ));
    }

    // 敌机弹幕被击中的火花（可选，简单化用 spark）

    update(dt) {
      for (let i = this.list.length - 1; i >= 0; i--) {
        if (!this.list[i].update(dt)) this.list.splice(i, 1);
      }
    }

    draw(ctx) {
      for (const p of this.list) p.draw(ctx);
    }

    clear() { this.list.length = 0; }
  }

  G.ParticleSystem = ParticleSystem;

})(window.Game = window.Game || {});
