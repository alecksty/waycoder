/*
 * world.js —— 世界：程序化地形生成 + 方块数据存取 + 通用物理碰撞
 * 世界尺寸 800x400 块，Uint8Array 存储（每元素一个方块 id）
 * 纯逻辑，不依赖 canvas，可在 node 中冒烟测试
 */
'use strict';

const World = {
  W: 800,            // 世界宽（块）
  H: 400,            // 世界高（块）
  SEA: 0,            // 海平面（块 y）
  data: null,        // Uint8Array
  seed: 0,
  spawn: { x: 0, y: 0 },   // 出生点（块坐标，y 为站立表面）

  /* ---------- 基础存取 ---------- */
  inBounds(x, y) { return x >= 0 && x < this.W && y >= 0 && y < this.H; },

  get(x, y) {
    if (!this.inBounds(x, y)) {
      // 越界按实心石墙处理（防掉出世界）
      return y >= this.H ? 3 : 0;
    }
    return this.data[y * this.W + x];
  },

  set(x, y, id) {
    if (this.inBounds(x, y)) this.data[y * this.W + x] = id;
  },

  /* 该坐标是否为实体方块 */
  isSolid(x, y) {
    const t = Tiles[this.get(x, y)];
    return !!t && t.solid;
  },

  /* 该坐标是否为流体（水） */
  isFluid(x, y) {
    const t = Tiles[this.get(x, y)];
    return !!t && !!t.fluid;
  },

  /* 地表高度：从 y=0 向下找第一个非空气块，返回其 y */
  surfaceAt(x) {
    for (let y = 0; y < this.H; y++) {
      const t = Tiles[this.get(x, y)];
      if (t && t.solid) return y;
    }
    return this.H - 1;
  },

  /* ---------- 世界生成 ---------- */
  generate(seed) {
    this.seed = seed >>> 0;
    this.data = new Uint8Array(this.W * this.H);
    const rng = Core.makeRng(this.seed);

    const sea = Math.floor(this.H * 0.62);       // 海平面
    this.SEA = sea;

    /* 1) 高度图：fbm 起伏 */
    const heights = new Int16Array(this.W);
    for (let x = 0; x < this.W; x++) {
      const n = Core.fbm2(x / 150, 3.7, 4);      // 0..1
      const h = sea - Math.round((n - 0.42) * 85);
      heights[x] = Core.clamp(h, 30, this.H - 40);
    }

    /* 2) 逐列填方块（y 向下增大，h 为地表高度：y<h 天空，y==h 表面，y>h 地下） */
    for (let x = 0; x < this.W; x++) {
      const h = heights[x];
      const beach = h >= sea - 1;                // 接近/低于海平面处为沙滩
      for (let y = 0; y < this.H; y++) {
        let id;
        if (y > h + 3) id = 3;                   // 深层石
        else if (y > h) id = 2;                  // 泥土
        else if (y === h) id = beach ? 4 : 1;    // 表面：沙/草
        else if (y >= sea) id = 15;              // 水面及以下（地表低于海平面时）
        else id = 0;                             // 空气
        this.data[y * this.W + x] = id;
      }
    }

    /* 3) 洞穴：深层噪声挖空 */
    for (let y = sea + 8; y < this.H; y++) {
      for (let x = 0; x < this.W; x++) {
        const n = Core.fbm2(x / 46 + 91.7, y / 46, 3);
        if (n > 0.63) this.data[y * this.W + x] = 0;
      }
    }

    /* 4) 矿石：在石头层随机富集（越深越稀有） */
    for (let y = sea + 4; y < this.H; y++) {
      for (let x = 0; x < this.W; x++) {
        if (this.data[y * this.W + x] !== 3) continue;
        const r = rng();
        if (r < 0.0012 && y > sea + 46) this.data[y * this.W + x] = 14;        // 钻石
        else if (r < 0.0035 && y > sea + 26) this.data[y * this.W + x] = 13;    // 金
        else if (r < 0.012 && y > sea + 12) this.data[y * this.W + x] = 12;     // 铁
        else if (r < 0.028) this.data[y * this.W + x] = 11;                     // 煤
      }
    }

    /* 5) 树木：草方块上随机长树 */
    for (let x = 8; x < this.W - 8; x += 1) {
      if (rng.chance(0.12)) {
        const h = heights[x];
        if (Tiles[this.get(x, h)].name !== '草方块') continue;
        this.growTree(x, h, rng);
      }
    }

    /* 6) 出生点：世界中央地表 */
    const sx = Math.floor(this.W / 2);
    let sy = this.surfaceAt(sx);
    // 确保出生点附近平坦且无水
    while (Tiles[this.get(sx, sy)].name === '水' && sy > 30) { sy--; sx += 2; }
    this.spawn = { x: sx, y: sy };
    return this.spawn;
  },

  /* 在 (tx, th)（th=地表 y）种一棵树 */
  growTree(tx, th, rng) {
    const hgt = rng.int(4, 6);
    const leavesR = 2;
    // 树干
    for (let i = 1; i <= hgt; i++) {
      if (this.inBounds(tx, th - i)) this.data[(th - i) * this.W + tx] = 5;
    }
    // 树冠（两层叶片）
    const topY = th - hgt;
    for (let dy = -1; dy <= 1; dy++) {
      for (let dx = -leavesR; dx <= leavesR; dx++) {
        const ax = tx + dx, ay = topY + dy;
        if (!this.inBounds(ax, ay)) continue;
        if (Math.abs(dx) + Math.abs(dy) > leavesR + 1) continue;
        if (this.get(ax, ay) === 0) this.data[ay * this.W + ax] = 6;
      }
    }
    // 顶部
    if (this.inBounds(tx, topY - 1)) this.data[(topY - 1) * this.W + tx] = 6;
    if (this.inBounds(tx - 1, topY - 2) && this.get(tx - 1, topY - 2) === 0) this.data[(topY - 2) * this.W + tx - 1] = 6;
    if (this.inBounds(tx + 1, topY - 2) && this.get(tx + 1, topY - 2) === 0) this.data[(topY - 2) * this.W + tx + 1] = 6;
  },

  /* ---------- 树苗 ---------- */
  saplings: [],      // [{x, y, t}] 待长成的树苗

  /* 种植树苗：登记生长计时（放在该格上方是空气时） */
  plantSapling(x, y) {
    if (!this.inBounds(x, y)) return;
    this.saplings.push({ x, y, t: 6 });
  },

  /* 每帧推进树苗生长，成熟后原地长成一棵树 */
  updateSaplings(dt) {
    for (let i = this.saplings.length - 1; i >= 0; i--) {
      const s = this.saplings[i];
      s.t -= dt;
      if (s.t > 0) continue;
      this.saplings.splice(i, 1);
      if (this.get(s.x, s.y) !== 18) continue;        // 树苗被挖走了
      let ok = true;
      for (let dy = 1; dy <= 9; dy++) {               // 上方 9 格净空
        if (this.isSolid(s.x, s.y - dy)) { ok = false; break; }
      }
      if (!ok) continue;
      const rng = Core.makeRng(s.x * 733 + s.y * 131 + this.seed);
      this.set(s.x, s.y, 0);
      this.growTree(s.x, s.y, rng);
    }
  },

  /* 破坏方块：返回被破坏的方块 id（用于掉落物），越界/空气返回 0 */
  breakBlock(x, y) {
    const id = this.get(x, y);
    if (id === 0) return 0;
    const t = Tiles[id];
    if (t && t.fluid) { this.set(x, y, 0); return 0; }   // 水直接消失
    this.set(x, y, 0);
    return id;
  },

  /* ---------- 通用物理（像素坐标系，1 块 = Core.BLOCK px） ----------
   * e 需有 {x, y, w, h, vx, vy, grav, maxFall}
   * 更新后 e.onGround / e.hitWall / e.hitCeil 反映本帧状态
   */
  moveEntity(e, dt) {
    e.vy = Math.min(e.vy + (e.grav === undefined ? 700 : e.grav) * dt, e.maxFall || 42);
    e.onGround = false;
    e.hitWall = false;
    e.hitCeil = false;

    /* X 轴 */
    e.x += e.vx * dt;
    let hit = this.rectHitsAt(e);
    if (hit) {
      if (e.vx > 0) e.x = hit.x * Core.BLOCK - e.w - 0.01;
      else if (e.vx < 0) e.x = (hit.x + 1) * Core.BLOCK + 0.01;
      e.vx = 0;
      e.hitWall = true;
    }

    /* Y 轴 */
    e.y += e.vy * dt;
    hit = this.rectHitsAt(e);
    if (hit) {
      if (e.vy > 0) { e.y = hit.y * Core.BLOCK - e.h - 0.01; e.onGround = true; }
      else if (e.vy < 0) { e.y = (hit.y + 1) * Core.BLOCK + 0.01; e.hitCeil = true; }
      e.vy = 0;
    }
    /* 世界底兜底 */
    if (e.y > this.H * Core.BLOCK) { e.y = this.H * Core.BLOCK - e.h; e.onGround = true; }
  },

  /* 矩形与实体块碰撞检测：返回首个碰撞块 {x,y} 或 null */
  rectHitsAt(rect) {
    const x0 = Math.floor(rect.x / Core.BLOCK);
    const x1 = Math.floor((rect.x + rect.w - 0.01) / Core.BLOCK);
    const y0 = Math.floor(rect.y / Core.BLOCK);
    const y1 = Math.floor((rect.y + rect.h - 0.01) / Core.BLOCK);
    for (let y = y0; y <= y1; y++) {
      for (let x = x0; x <= x1; x++) {
        if (this.isSolid(x, y)) return { x, y };
      }
    }
    return null;
  },

  /* 矩形与任意流体块碰撞（用于入水检测） */
  rectInWater(rect) {
    const x0 = Math.floor(rect.x / Core.BLOCK);
    const x1 = Math.floor((rect.x + rect.w - 0.01) / Core.BLOCK);
    const y0 = Math.floor(rect.y / Core.BLOCK);
    const y1 = Math.floor((rect.y + rect.h - 0.01) / Core.BLOCK);
    for (let y = y0; y <= y1; y++) {
      for (let x = x0; x <= x1; x++) {
        if (this.isFluid(x, y)) return true;
      }
    }
    return false;
  },

  /* ---------- 序列化（localStorage 存档用） ---------- */
  serialize() {
    const bytes = this.data;
    let bin = '';
    const CH = 8192;
    for (let i = 0; i < bytes.length; i += CH) {
      bin += String.fromCharCode.apply(null, bytes.subarray(i, Math.min(i + CH, bytes.length)));
    }
    return btoa(bin);
  },

  deserialize(str) {
    const bin = atob(str);
    this.data = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) this.data[i] = bin.charCodeAt(i);
  }
};
