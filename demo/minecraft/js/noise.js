// noise.js — 2D Simplex 噪声 + fbm 分形，用于程序化地形高度生成。
// 零依赖、确定性（可选种子），为体素世界提供平滑连贯的高度场。

/**
 * 可播种的伪随机数生成器（mulberry32），返回 0~1 均匀分布。
 * @param {number} seed 任意整数种子
 * @returns {() => number}
 */
export function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0;
    a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * 2D Simplex 噪声。输入整数格子点（用伪随机哈希），返回 [-1, 1]。
 * @param {number} x
 * @param {number} y
 * @param {number} seed 种子偏移
 * @returns {number}
 */
export function simplex2(x, y, seed = 0) {
  const F2 = 0.5 * (Math.sqrt(3) - 1);
  const G2 = (3 - Math.sqrt(3)) / 6;

  const s = (x + y) * F2;
  const i = Math.floor(x + s);
  const j = Math.floor(y + s);
  const t = (i + j) * G2;
  const X0 = i - t;
  const Y0 = j - t;
  const x0 = x - X0;
  const y0 = y - Y0;

  // 确定当前三角形（i,j）内的扇区
  let i1, j1;
  if (x0 > y0) { i1 = 1; j1 = 0; } else { i1 = 0; j1 = 1; }

  const x1 = x0 - i1 + G2;
  const y1 = y0 - j1 + G2;
  const x2 = x0 - 1 + 2 * G2;
  const y2 = y0 - 1 + 2 * G2;

  // 角点渐变方向生成（用整数哈希决定 8 个方向）
  const gi = (a, b) => {
    const h = ((a * 374761393 + b * 668265263) ^ (seed * 0x9E3779B9)) >>> 0;
    const ang = (h % 8) * (Math.PI / 4);
    return [Math.cos(ang), Math.sin(ang)];
  };

  const contrib = (x, y, gx, gy) => {
    const t0 = 0.5 - x * x - y * y;
    if (t0 <= 0) return 0;
    const tt = t0 * t0;
    return tt * tt * (gx * x + gy * y);
  };

  const [gx0, gy0] = gi(i, j);
  const [gx1, gy1] = gi(i + i1, j + j1);
  const [gx2, gy2] = gi(i + 1, j + 1);

  const n0 = contrib(x0, y0, gx0, gy0);
  const n1 = contrib(x1, y1, gx1, gy1);
  const n2 = contrib(x2, y2, gx2, gy2);

  return 70.14 * (n0 + n1 + n2);
}

/**
 * 分形布朗运动（fbm）——多层不同频率/振幅的 simplex 叠加，得到自然的地形。
 * @param {number} x
 * @param {number} y
 * @param {object} opts
 * @param {number} opts.seed
 * @param {number} opts.octaves 层数（默认 4）
 * @param {number} opts.lacunarity 频率倍增（默认 2）
 * @param {number} opts.gain 振幅衰减（默认 0.5）
 * @returns {number} 约 [-1, 1]
 */
export function fbm2(x, y, { seed = 0, octaves = 4, lacunarity = 2, gain = 0.5 } = {}) {
  let amp = 1;
  let freq = 1;
  let sum = 0;
  let norm = 0;
  for (let o = 0; o < octaves; o++) {
    sum += amp * simplex2(x * freq, y * freq, seed + o * 101);
    norm += amp;
    amp *= gain;
    freq *= lacunarity;
  }
  return sum / norm;
}
