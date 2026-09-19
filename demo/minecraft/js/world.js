// world.js — 体素世界：管理无限区块（Chunk）的加载/卸载，提供跨区块的 getBlock/setBlock，
// 使用噪声生成地形（草地/泥土/石头/沙/水）与树木，按需构建网格并注入场景。

import * as THREE from 'three';
import { Chunk, CHUNK_SIZE, CHUNK_HEIGHT, Block, buildChunkGeometry, getBlockMaterial } from './chunk.js';
import { fbm2 } from './noise.js';

// 世界配置
const SEA_LEVEL = 24;      // 水面高度
const REBUILD_RADIUS = 1;  // 编辑某区块时同时重建相邻 8 个区块（边缘体素变化会跨越区块边界）
const MAX_HEIGHT = CHUNK_HEIGHT - 2;

export class World {
  /**
   * @param {THREE.Scene} scene 注入网格的场景
   * @param {number} seed 世界种子
   */
  constructor(scene, seed = 1337) {
    this.scene = scene;
    this.seed = seed;
    this.chunks = new Map(); // key "cx,cz" -> Chunk
    this.chunkMeshes = new Map(); // key -> THREE.Mesh
    this.material = getBlockMaterial();
    this.renderAwait = null;   // 用于限制每帧重建数量
  }

  key(cx, cz) { return cx + ',' + cz; }

  /** 由世界坐标转化到区块索引与局部坐标 */
  toChunkCoord(x, z) {
    const cx = Math.floor(x / CHUNK_SIZE);
    const cz = Math.floor(z / CHUNK_SIZE);
    const lx = x - cx * CHUNK_SIZE;
    const lz = z - cz * CHUNK_SIZE;
    return { cx, cz, lx, lz };
  }

  /** 获取任意世界坐标处的方块类型 */
  getBlockAt(x, y, z) {
    const yc = Math.floor(y);
    if (yc < 0 || yc >= CHUNK_HEIGHT) return Block.Air;
    const { cx, cz, lx, lz } = this.toChunkCoord(Math.floor(x), Math.floor(z));
    const chunk = this.chunks.get(this.key(cx, cz));
    if (!chunk) return Block.Air;
    return chunk.get(lx, yc, lz);
  }

  /** 设置任意世界坐标处的方块，并触发受影响区块重建 */
  setBlockAt(x, y, z, block) {
    const yc = Math.floor(y);
    if (yc < 0 || yc >= CHUNK_HEIGHT) return;
    const { cx, cz, lx, lz } = this.toChunkCoord(Math.floor(x), Math.floor(z));
    const chunk = this.chunks.get(this.key(cx, cz));
    if (!chunk) return;
    chunk.set(lx, yc, lz, block);
    // 标记自身与相邻区块重建（边缘变化会跨区块）
    for (let dcx = -REBUILD_RADIUS; dcx <= REBUILD_RADIUS; dcx++) {
      for (let dcz = -REBUILD_RADIUS; dcz <= REBUILD_RADIUS; dcz++) {
        this.markDirty(cx + dcx, cz + dcz);
      }
    }
  }

  /** 让某个区块标记为需要重建网格 */
  markDirty(cx, cz) {
    const chunk = this.chunks.get(this.key(cx, cz));
    if (chunk) chunk.dirty = true;
  }

  // ---------- 地形生成 ----------
  /**
   * 为指定区块填充体素（生成地形 + 树木）。
   * @param {Chunk} chunk
   */
  generateChunk(chunk) {
    const baseX = chunk.cx * CHUNK_SIZE;
    const baseZ = chunk.cz * CHUNK_SIZE;
    const rng = mulberry(this.seed ^ (chunk.cx * 73856093) ^ (chunk.cz * 19349663));

    for (let lz = 0; lz < CHUNK_SIZE; lz++) {
      for (let lx = 0; lx < CHUNK_SIZE; lx++) {
        const wx = baseX + lx;
        const wz = baseZ + lz;

        // 地形高度：fbm 噪声 -> [0,1] -> 高度
        const n = fbm2(wx * 0.008, wz * 0.008, { seed: this.seed, octaves: 4 }) * 0.5 + 0.5;
        let h = Math.floor(SEA_LEVEL - 6 + n * 18);
        h = Math.max(1, Math.min(MAX_HEIGHT, h));

        // 水中放沙+石头，陆地放草地
        for (let y = 0; y <= h; y++) {
          let block = Block.Stone;
          if (y === h) {
            if (h <= SEA_LEVEL) block = Block.Sand;
            else if (Math.abs(wx) % 7 === 0 && Math.abs(wz) % 5 === 0) block = Block.Grass;
            else block = Block.Grass;
          } else if (y >= h - 3) {
            block = h <= SEA_LEVEL ? Block.Sand : (Math.abs(wx * 7 + wz * 13) % 11 === 0 ? Block.Dirt : Block.Dirt);
          } else if (y < 3) {
            block = Block.Bedrock;
          }
          chunk.set(lx, y, lz, block);
        }

        // 水面填充（浅水，只到空气之上）
        if (h < SEA_LEVEL) {
          for (let y = h + 1; y <= SEA_LEVEL; y++) {
            chunk.set(lx, y, lz, Block.Water);
          }
        }

        // 矿石分布
        if (h > 4) {
          for (const ore of [Block.Coal, Block.Iron]) {
            if (rng() < 0.03) {
              const oreY = Math.max(1, h - Math.floor(rng() * 6));
              // 避免覆盖顶部
              if (chunk.get(lx, oreY, lz) === Block.Stone) chunk.set(lx, oreY, lz, ore);
            }
          }
        }

        // 树生成（草地表面，随机）
        if (Math.abs(wx) % 13 === 0 && Math.abs(wz) % 11 === 0 && h > SEA_LEVEL) {
          if (rng() < 0.65) this.growTree(chunk, lx, h + 1, lz);
        }
      }
    }
  }

  /** 在指定表面位置种植一棵树 */
  growTree(chunk, lx, yBase, lz) {
    const treeH = 4 + Math.floor(Math.random() * 2); // 4~5
    // 树干
    for (let y = 0; y < treeH; y++) {
      chunk.set(lx, yBase + y, lz, Block.Wood);
    }
    // 树叶（三层，越上越大）
    const topY = yBase + treeH - 1;
    for (let dy = -2; dy <= 1; dy++) {
      const radius = dy >= 1 ? 1 : 2;
      const yy = topY + dy;
      for (let ox = -radius; ox <= radius; ox++) {
        for (let oz = -radius; oz <= radius; oz++) {
          if (Math.abs(ox) === radius && Math.abs(oz) === radius && dy < 1) continue; // 去掉四角
          if ((ctxCorner(ox, oz) && dy >= 1)) continue;
          if (chunk.get(lx + ox, yy, lz + oz) === Block.Air) {
            chunk.set(lx + ox, yy, lz + oz, Block.Leaves);
          }
        }
      }
    }
  }

  // ---------- 区块加载/卸载与网格 ----------
  /**
   * 确保围绕中心坐标的 R×R 区块被加载，并卸载过远区块。
   * @param {number} cx 中心区块 X
   * @param {number} cz 中心区块 Z
   * @param {number} r 半径
   */
  updateAround(cx, cz, r) {
    const needed = new Set();
    for (let dx = -r; dx <= r; dx++) {
      for (let dz = -r; dz <= r; dz++) {
        needed.add(this.key(cx + dx, cz + dz));
      }
    }
    // 卸载过远的
    for (const [k, chunk] of this.chunks) {
      if (!needed.has(k)) {
        this.unloadChunk(k);
      }
    }
    // 加载缺失的
    for (const k of needed) {
      if (!this.chunks.has(k)) {
        const [cxk, czk] = k.split(',').map(Number);
        this.loadChunk(cxk, czk);
      }
    }
  }

  /** 加载并生成区块，必要时立即重建网格 */
  loadChunk(cx, cz) {
    const k = this.key(cx, cz);
    if (this.chunks.has(k)) return;
    const chunk = new Chunk(this, cx, cz);
    this.generateChunk(chunk);
    this.chunks.set(k, chunk);
  }

  /** 卸载区块并移除网格 */
  unloadChunk(k) {
    const mesh = this.chunkMeshes.get(k);
    if (mesh) {
      this.scene.remove(mesh);
      mesh.geometry.dispose();
      this.chunkMeshes.delete(k);
    }
    this.chunks.delete(k);
  }

  /** 重建所有 dirty 区块的网格（每帧调用；分帧避免卡顿） */
  rebuildDirty(maxBuilds = 4) {
    let count = 0;
    for (const [k, chunk] of this.chunks) {
      if (!chunk.dirty) continue;
      // 仅当所有邻居都已加载时才构建，避免半边网格
      if (!this.neighborsReady(chunk)) continue;
      this.buildChunkMesh(chunk);
      chunk.dirty = false;
      if (++count >= maxBuilds) break;
    }
  }

  neighborsReady(chunk) {
    for (let dx = -1; dx <= 1; dx++) {
      for (let dz = -1; dz <= 1; dz++) {
        if (!this.chunks.has(this.key(chunk.cx + dx, chunk.cz + dz))) return false;
      }
    }
    return true;
  }

  /** 构建区块网格（若有可见面），替换旧网格 */
  buildChunkMesh(chunk) {
    const k = this.key(chunk.cx, chunk.cz);
    const old = this.chunkMeshes.get(k);
    if (old) {
      this.scene.remove(old);
      old.geometry.dispose();
      this.chunkMeshes.delete(k);
    }
    const geo = buildChunkGeometry(chunk);
    if (geo) {
      const mesh = new THREE.Mesh(geo, this.material);
      mesh.frustumCulled = true;
      this.scene.add(mesh);
      this.chunkMeshes.set(k, mesh);
    }
  }

  /** 世界高度：返回某 (x,z) 列的最高实心方块 Y（用于落脚/树/水面上）。 */
  heightAt(x, z) {
    for (let y = CHUNK_HEIGHT - 1; y >= 0; y--) {
      const b = this.getBlockAt(x, y, z);
      if (b !== Block.Air && b !== Block.Water) return y;
    }
    return 0;
  }

  /** 获取某列的顶面 Y（方块上表面，用于放置方块吸底）。 */
  topSolidAt(x, z) {
    return this.heightAt(x, z) + 1;
  }
}

// 辅助伪随机（chunk 局部）
function mulberry(a) {
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
// 树冠四角判断辅助
function ctxCorner(ox, oz) {
  return Math.abs(ox) === 2 && Math.abs(oz) === 2;
}
