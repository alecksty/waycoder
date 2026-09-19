// chunk.js — 体素区块（Chunk）：16×16 列 × 高度 64，存储体素数据并构建只含暴露面的 BufferGeometry。
// 为了提高性能，这里用「每个面独立四边形 + 按颜色分组材质」的方式，避免逐面绘制调用。

import * as THREE from 'three';

export const CHUNK_SIZE = 16;   // 水平 X/Z 边长（格）
export const CHUNK_HEIGHT = 64; // 垂直 Y 高度（格）

// 方块类型（体素 ID）
export const Block = {
  Air: 0,
  Grass: 1,
  Dirt: 2,
  Stone: 3,
  Sand: 4,
  Water: 5,
  Wood: 6,
  Leaves: 7,
  Coal: 8,
  Iron: 9,
  Bedrock: 10,
};

// 每种方块的顶/侧/底颜色（经典 MC 风格，用 RGB）
const BLOCK_COLORS = {
  [Block.Grass]: { top: 0x6ba43a, side: 0x8a5a3b, bottom: 0x8a5a3b },
  [Block.Dirt]: { top: 0x8a5a3b, side: 0x8a5a3b, bottom: 0x8a5a3b },
  [Block.Stone]: { top: 0x8f8f8f, side: 0x8f8f8f, bottom: 0x8f8f8f },
  [Block.Sand]: { top: 0xe8d8a0, side: 0xe8d8a0, bottom: 0xe8d8a0 },
  [Block.Water]: { top: 0x3a6fd8, side: 0x3a6fd8, bottom: 0x3a6fd8 },
  [Block.Wood]: { top: 0x9c7a4a, side: 0x6b4a2a, bottom: 0x6b4a2a },
  [Block.Leaves]: { top: 0x3d8b2f, side: 0x3d8b2f, bottom: 0x3d8b2f },
  [Block.Coal]: { top: 0x4a4a4a, side: 0x7a7a72, bottom: 0x4a4a4a },
  [Block.Iron]: { top: 0xd8c8b0, side: 0x9d8470, bottom: 0xd8c8b0 },
  [Block.Bedrock]: { top: 0x2a2a2a, side: 0x2a2a2a, bottom: 0x2a2a2a },
};

// 半透明方块（水）：用于材质透明设置
const TRANSPARENT = new Set([Block.Water]);

export class Chunk {
  /**
   * @param {object} world 持有 getBlock/setBlock 的世界（用于跨区块查询邻居）
   * @param {number} cx 区块在 X 轴上的索引（含负数）
   * @param {number} cz 区块在 Z 轴上的索引（含负数）
   */
  constructor(world, cx, cz) {
    this.world = world;
    this.cx = cx;
    this.cz = cz;
    this.data = new Uint8Array(CHUNK_SIZE * CHUNK_HEIGHT * CHUNK_SIZE);
    this.mesh = null;        // THREE.Mesh（由 buildMesh 创建）
    this.dirty = true;       // 是否需要重建几何
    this.isEmpty = true;     // 是否全空（跳过网格）
  }

  /** 世界坐标 -> 区块内体素索引 */
  static index(lx, y, lz) {
    return (lx * CHUNK_HEIGHT + y) * CHUNK_SIZE + lz;
  }

  /** 世界坐标 -> 局部坐标（含负数取模修正） */
  static toLocal(wx, wcz, wy) {
    // 注意：这里签名按调用约定，实际在 World 内处理更清晰。
  }

  /** 设置一个体素（局部坐标，lx/lz 0..15, y 0..63）。返回是否变化。 */
  set(lx, y, lz, block) {
    if (lx < 0 || lx >= CHUNK_SIZE || y < 0 || y >= CHUNK_HEIGHT || lz < 0 || lz >= CHUNK_SIZE) return false;
    const idx = Chunk.index(lx, y, lz);
    if (this.data[idx] === block) return false;
    this.data[idx] = block;
    this.dirty = true;
    if (block !== Block.Air) this.isEmpty = false;
    return true;
  }

  /** 获取体素（局部坐标）。越界返回 Block.Air。 */
  get(lx, y, lz) {
    if (lx < 0 || lx >= CHUNK_SIZE || y < 0 || y >= CHUNK_HEIGHT || lz < 0 || lz >= CHUNK_SIZE) return Block.Air;
    return this.data[Chunk.index(lx, y, lz)];
  }
}

// ---------- 网格构建 ----------

// 6 个面的顶点相对偏移（每面 4 顶点，逆时针朝外）
const FACES = [
  { dir: [1, 0, 0], corners: [[1, 0, 0], [1, 1, 0], [1, 1, 1], [1, 0, 1]] },   // +X
  { dir: [-1, 0, 0], corners: [[0, 0, 1], [0, 1, 1], [0, 1, 0], [0, 0, 0]] },   // -X
  { dir: [0, 1, 0], corners: [[0, 1, 0], [0, 1, 1], [1, 1, 1], [1, 1, 0]] },    // +Y (顶)
  { dir: [0, -1, 0], corners: [[0, 0, 1], [0, 0, 0], [1, 0, 0], [1, 0, 1]] },   // -Y (底)
  { dir: [0, 0, 1], corners: [[1, 0, 1], [1, 1, 1], [0, 1, 1], [0, 0, 1]] },    // +Z
  { dir: [0, 0, -1], corners: [[0, 0, 0], [0, 1, 0], [1, 1, 0], [1, 0, 0]] },   // -Z
];

// 每个面的两个三角形拆分（0,1,2 / 0,2,3）
const TRI = [0, 1, 2, 0, 2, 3];

/**
 * 为区块构建 BufferGeometry，只添加「面朝空气/透明方块」的暴露面。
 * 为提高渲染效率，把同类型方块按面分组，并加上顶点色（块面色 / 简单 AO 模拟）。
 * @returns {THREE.BufferGeometry | null}
 */
export function buildChunkGeometry(chunk) {
  const positions = [];
  const normals = [];
  const colors = [];
  const indices = [];

  const world = chunk.world;
  const baseX = chunk.cx * CHUNK_SIZE;
  const baseZ = chunk.cz * CHUNK_SIZE;

  for (let ly = 0; ly < CHUNK_HEIGHT; ly++) {
    for (let lz = 0; lz < CHUNK_SIZE; lz++) {
      for (let lx = 0; lx < CHUNK_SIZE; lx++) {
        const block = chunk.get(lx, ly, lz);
        if (block === Block.Air) continue;

        const wb = BLOCK_COLORS[block] || BLOCK_COLORS[Block.Stone];
        // 世界坐标（体素中心用格坐标，面偏移由 corners 决定）
        const wx = baseX + lx;
        const wy = ly;
        const wz = baseZ + lz;

        for (const face of FACES) {
          const [dx, dy, dz] = face.dir;
          const nx = wx + dx;
          const ny = wy + dy;
          const nz = wz + dz;
          // 邻居方块
          const nb = world.getBlockAt(nx, ny, nz);
          // 若邻居是空气，或当前是透明（水）且邻居也是透明，则跳过阴影重叠的面（仅当邻居透明时才暴露给水）
          if (nb !== Block.Air) {
            const curTrans = TRANSPARENT.has(block);
            const nbTrans = TRANSPARENT.has(nb);
            if (!(curTrans && nbTrans)) continue; // 实心对实心 或 实心对水（不暴露）-> 跳过
          }

          // 该面颜色：顶面用 top，底面用 bottom，侧面用 side
          let col;
          if (dy > 0) col = wb.top;
          else if (dy < 0) col = wb.bottom;
          else col = wb.side;

          // 简单 AO 模拟：给相邻面轻微明暗差，增强立体感。
          // 用一个伪随机的方向性调光（基于位置哈希），避免完全平涂。
          const shade = 0.82 + 0.18 * (((wx * 13 + wy * 7 + wz * 11) & 3) / 3);
          const cr = ((col >> 16) & 0xff) * shade;
          const cg = ((col >> 8) & 0xff) * shade;
          const cb = (col & 0xff) * shade;

          const base = positions.length / 3;
          for (let ci = 0; ci < 4; ci++) {
            const [ox, oy, oz] = face.corners[ci];
            positions.push(wx + ox, wy + oy, wz + oz);
            normals.push(dx, dy, dz);
            colors.push(cr / 255, cg / 255, cb / 255);
          }
          for (const t of TRI) indices.push(base + t);
        }
      }
    }
  }

  if (indices.length === 0) return null;

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geometry.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
  geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
  geometry.setIndex(indices);
  geometry.computeBoundingSphere();
  return geometry;
}

/** 根据方块类型返回材质（共享 single-pass 顶点色材质 = 灵活通用） */
export function getBlockMaterial() {
  // 用 vertexColors 让每面不同颜色，一套材质即可
  const mat = new THREE.MeshStandardMaterial({ vertexColors: true });
  return mat;
}
