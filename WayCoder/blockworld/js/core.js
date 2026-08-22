/*
 * core.js —— 方块世界：基础工具库
 * 纯逻辑、零 DOM 依赖：随机数、噪声、数学工具、颜色插值、字符串工具
 * 供 world / player / entities / ui / game 等模块共用
 */
'use strict';

const Core = {};

/* ---------- 常量 ---------- */
Core.TAU = Math.PI * 2;
Core.BLOCK = 16;            // 一个方块的像素边长
Core.PIXEL = 1;

/* ---------- 数学工具 ---------- */
Core.clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
Core.lerp = (a, b, t) => a + (b - a) * t;
Core.invLerp = (a, b, v) => (b === a ? 0 : (v - a) / (b - a));
Core.dist = (x1, y1, x2, y2) => Math.hypot(x2 - x1, y2 - y1);
Core.sign = v => (v > 0 ? 1 : v < 0 ? -1 : 0);
Core.floor = v => Math.floor(v);
Core.round = v => Math.round(v);
Core.mod = (a, n) => ((a % n) + n) % n;   // 正模运算
Core.minmax = (a, b) => (a < b ? [a, b] : [b, a]);

/* 矩形重叠判断（像素坐标系） */
Core.rectsOverlap = (a, b) =>
  a.x < b.x + b.w && a.x + a.w > b.x &&
  a.y < b.y + b.h && a.y + a.h > b.y;

/* 点到矩形最近距离 */
Core.distToRect = (px, py, r) => {
  const dx = Math.max(r.x - px, 0, px - (r.x + r.w));
  const dy = Math.max(r.y - py, 0, py - (r.y + r.h));
  return Math.hypot(dx, dy);
};

/* ---------- 确定性随机数（mulberry32） ---------- */
Core.makeRng = seed => {
  let s = seed >>> 0;
  const next = () => {
    s |= 0; s = (s + 0x6D2B79F5) | 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
  next.range = (a, b) => a + (b - a) * next();   // [a,b)
  next.int = (a, b) => Math.floor(next.range(a, b + 1));
  next.pick = arr => arr[Math.floor(next() * arr.length)];
  next.chance = p => next() < p;
  return next;
};

/* ---------- 值噪声（确定性哈希） ---------- */
Core.hash2 = (x, y) => {
  let h = (x * 374761393 + y * 668265263) | 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  h = h ^ (h >>> 16);
  return (h >>> 0) / 4294967296;
};

/* 平滑值噪声，返回 [0,1] */
Core.valueNoise2 = (x, y) => {
  const xi = Math.floor(x), yi = Math.floor(y);
  const xf = x - xi, yf = y - yi;
  const u = xf * xf * (3 - 2 * xf);
  const v = yf * yf * (3 - 2 * yf);
  const a = Core.hash2(xi, yi), b = Core.hash2(xi + 1, yi);
  const c = Core.hash2(xi, yi + 1), d = Core.hash2(xi + 1, yi + 1);
  return Core.lerp(Core.lerp(a, b, u), Core.lerp(c, d, u), v);
};

/* 分形噪声 fbm，返回 [0,1] */
Core.fbm2 = (x, y, octaves = 4) => {
  let sum = 0, amp = 0.5, freq = 1, norm = 0;
  for (let i = 0; i < octaves; i++) {
    sum += Core.valueNoise2(x * freq, y * freq) * amp;
    norm += amp;
    amp *= 0.5;
    freq *= 2;
  }
  return sum / norm;
};

/* ---------- 颜色工具（'#rrggbb' 字符串） ---------- */
Core.hexToRgb = hex => {
  const n = parseInt(hex.slice(1), 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
};

Core.rgbToCss = (r, g, b) => {
  r = Core.clamp(Math.round(r), 0, 255);
  g = Core.clamp(Math.round(g), 0, 255);
  b = Core.clamp(Math.round(b), 0, 255);
  return 'rgb(' + r + ',' + g + ',' + b + ')';
};

/* 颜色明度缩放 factor=1 不变 */
Core.shade = (hex, factor) => {
  const [r, g, b] = Core.hexToRgb(hex);
  return Core.rgbToCss(r * factor, g * factor, b * factor);
};

/* 两个 hex 颜色线性插值，返回 css */
Core.mixHex = (hexA, hexB, t) => {
  const [r1, g1, b1] = Core.hexToRgb(hexA);
  const [r2, g2, b2] = Core.hexToRgb(hexB);
  return Core.rgbToCss(Core.lerp(r1, r2, t), Core.lerp(g1, g2, t), Core.lerp(b1, b2, t));
};

/* ---------- 通用小工具 ---------- */
/* 限定字符串宽度（按显示宽度近似），超出截断加省略号 */
Core.truncate = (str, max) => {
  if (str.length <= max) return str;
  return str.slice(0, Math.max(1, max - 1)) + '…';
};

/* 秒 → mm:ss */
Core.formatTime = sec => {
  sec = Math.max(0, Math.floor(sec));
  const m = Math.floor(sec / 60), s = sec % 60;
  return m + ':' + (s < 10 ? '0' : '') + s;
};

/* 简单数组洗牌（原地） */
Core.shuffle = (arr, rng) => {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor((rng || Math.random)() * (i + 1));
    const t = arr[i]; arr[i] = arr[j]; arr[j] = t;
  }
  return arr;
};

/* 千位分隔 */
Core.thousands = n => String(n).replace(/\B(?=(\d{3})+(?!\d))/g, ',');

/* 帧率统计器 */
Core.MakeFpsMeter = () => {
  let frames = 0, last = performance.now(), fps = 60;
  return {
    tick() { frames++; const now = performance.now(); if (now - last >= 500) { fps = Math.round(frames * 1000 / (now - last)); frames = 0; last = now; } },
    get value() { return fps; }
  };
};

/* ---------- 全局时钟（所有模块共用，便于测试注入） ---------- */
Core.now = () => (typeof performance !== 'undefined' ? performance.now() : Date.now());
