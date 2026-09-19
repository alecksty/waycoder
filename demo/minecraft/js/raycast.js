// raycast.js — 体素 DDA 网格追踪（3D DDA / Amanatides-Woo 算法）。
// 从相机眼睛沿视线方向推进到世界体素，返回命中的方块坐标与被击中面（用于放置新方块紧贴面）。

import { Block } from './chunk.js';

/**
 * 从起点沿方向做体素射线检测，返回第一个命中实心方块的坐标。
 * @param {object} world 体素世界（需要 getBlockAt）
 * @param {THREE.Vector3} origin 起点（相机眼位置）
 * @param {THREE.Vector3} dir 单位方向向量
 * @param {number} maxDist 最大射线距离
 * @returns {{x,y,z, face:[dx,dy,dz], normal:[nx,ny,nz]} | null}
 */
export function raycast(world, origin, dir, maxDist = 6) {
  let x = Math.floor(origin.x);
  let y = Math.floor(origin.y);
  let z = Math.floor(origin.z);

  const stepX = dir.x > 0 ? 1 : -1;
  const stepY = dir.y > 0 ? 1 : -1;
  const stepZ = dir.z > 0 ? 1 : -1;

  const tDeltaX = dir.x !== 0 ? Math.abs(1 / dir.x) : Infinity;
  const tDeltaY = dir.y !== 0 ? Math.abs(1 / dir.y) : Infinity;
  const tDeltaZ = dir.z !== 0 ? Math.abs(1 / dir.z) : Infinity;

  // 到下一个格子边界的初始 t
  let tMaxX = dir.x !== 0 ? ((dir.x > 0 ? (x + 1 - origin.x) : (origin.x - x)) / Math.abs(dir.x)) : Infinity;
  let tMaxY = dir.y !== 0 ? ((dir.y > 0 ? (y + 1 - origin.y) : (origin.y - y)) / Math.abs(dir.y)) : Infinity;
  let tMaxZ = dir.z !== 0 ? ((dir.z > 0 ? (z + 1 - origin.z) : (origin.z - z)) / Math.abs(dir.z)) : Infinity;

  let face = [0, 0, 0];
  let t = 0;
  while (t <= maxDist) {
    const block = world.getBlockAt(x, y, z);
    if (block !== Block.Air) {
      // 命中：返回方块坐标 + 被击中的面朝向（normal 指向方块外侧）
      return { x, y, z, block, face: face.slice(), normal: face.slice(), t };
    }
    if (tMaxX < tMaxY && tMaxX < tMaxZ) {
      x += stepX; t = tMaxX; tMaxX += tDeltaX; face = [stepX, 0, 0];
    } else if (tMaxY < tMaxZ) {
      y += stepY; t = tMaxY; tMaxY += tDeltaY; face = [0, stepY, 0];
    } else {
      z += stepZ; t = tMaxZ; tMaxZ += tDeltaZ; face = [0, 0, stepZ];
    }
  }
  return null;
}
