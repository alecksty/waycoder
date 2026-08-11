/* ============================================================
   utils.js — 数学 / 随机 / 绘制通用工具
   ============================================================ */
(function (G) {
  'use strict';

  const U = {

    /* ---------- 数学 ---------- */
    clamp(v, lo, hi) { return v < lo ? lo : v > hi ? hi : v; },
    lerp(a, b, t) { return a + (b - a) * t; },
    rand(a, b) { return a + Math.random() * (b - a); },
    randInt(a, b) { return Math.floor(U.rand(a, b + 1)); },
    randPick(arr) { return arr[Math.floor(Math.random() * arr.length)]; },
    dist(x1, y1, x2, y2) {
      const dx = x2 - x1, dy = y2 - y1;
      return Math.sqrt(dx * dx + dy * dy);
    },
    angleTo(x1, y1, x2, y2) { return Math.atan2(y2 - y1, x2 - x1); },

    /* ---------- 时间 ---------- */
    now() { return performance.now() / 1000; },

    /* ---------- 绘制辅助 ---------- */
    // 圆形渐变填充
    radial(ctx, x, y, r0, r1, c0, c1) {
      const g = ctx.createRadialGradient(x, y, r0, x, y, r1);
      g.addColorStop(0, c0);
      g.addColorStop(1, c1);
      ctx.fillStyle = g;
      ctx.fill();
    },

    // 发光圆（外圈模糊 + 内芯）
    glowCircle(ctx, x, y, r, core, glow, glowR) {
      ctx.save();
      ctx.globalCompositeOperation = 'lighter';
      U.radial(ctx, x, y, 0, glowR, glow, 'rgba(0,0,0,0)');
      ctx.fillStyle = core;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    },

    // 发光线条
    glowLine(ctx, x1, y1, x2, y2, width, color) {
      ctx.save();
      ctx.globalCompositeOperation = 'lighter';
      ctx.strokeStyle = color;
      ctx.lineWidth = width;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(x1, y1);
      ctx.lineTo(x2, y2);
      ctx.stroke();
      ctx.restore();
    },

    /* ---------- 多边形绘制 ---------- */
    // 以 (x,y) 为中心画多边形，pts 为相对坐标数组
    polygon(ctx, x, y, pts, close = true) {
      ctx.beginPath();
      ctx.moveTo(x + pts[0][0], y + pts[0][1]);
      for (let i = 1; i < pts.length; i++) ctx.lineTo(x + pts[i][0], y + pts[i][1]);
      if (close) ctx.closePath();
    },

    /* ---------- 工具 ---------- */
    // 简易深拷贝（对象浅层即可）
    clone(o) { return Object.assign({}, o); },

    // 高分存储
    getHighScore() {
      try { return parseInt(localStorage.getItem('skyStrike.high') || '0', 10) || 0; }
      catch (e) { return 0; }
    },
    setHighScore(v) {
      try { localStorage.setItem('skyStrike.high', String(v)); } catch (e) { /* ignore */ }
    },
  };

  G.Utils = U;
  G.U = U;

})(window.Game = window.Game || {});
