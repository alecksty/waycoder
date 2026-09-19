// player.js — 第一人称玩家控制器：WASD 移动、跳跃、重力，与体素世界做 AABB 碰撞。
// 碰撞采用「分轴移动+分轴检测」策略：先 X，再 Z，最后 Y，避免穿墙。

import * as THREE from 'three';
import { Block } from './chunk.js';

const GRAVITY = 28;      // 重力加速度（格/秒²）
const JUMP_SPEED = 9;    // 起跳初速度
const WALK_SPEED = 5.2;  // 步行速度（格/秒）
const RUN_SPEED = 8.6;   // 疾跑速度
const PLAYER_HALF = 0.3; // 玩家碰撞体水平半宽
const PLAYER_HEIGHT = 1.8; // 玩家碰撞体高
const EPS = 0.001;

export class Player {
  /**
   * @param {THREE.Camera} camera 绑定的相机（也是玩家眼睛）
   * @param {World} world 体素世界
   */
  constructor(camera, world) {
    this.camera = camera;
    this.world = world;
    // 位置（脚底中心）
    this.pos = new THREE.Vector3(0.5, world.heightAt(0.5, 0.5) + 2, 0.5);
    this.vel = new THREE.Vector3(0, 0, 0);
    this.yaw = 0;   // 水平朝向（弧度）
    this.pitch = 0; // 俯仰（弧度）
    this.onGround = false;
    this.sprinting = false;
    this.flying = false; // 可扩展的飞行模式（debug）
  }

  /** 设置出生位置（世界坐标 y 为脚底） */
  spawn(x, y, z) {
    this.pos.set(x, y, z);
    this.vel.set(0, 0, 0);
    this.yaw = 0;
    this.pitch = 0;
  }

  /**
   * 每帧更新玩家的移动与碰撞。
   * @param {number} dt 时间步（秒，已钳制）
   * @param {object} input 输入状态 {forward, back, left, right, jump, sprint, down}
   */
  update(dt, input) {
    // 水平移动方向（基于 yaw，绕 Y 轴）
    const sin = Math.sin(this.yaw);
    const cos = Math.cos(this.yaw);
    // 前向 = (-sin, 0, -cos)，右向 = (cos, 0, -sin)
    let mx = 0, mz = 0;
    const f = (input.forward ? 1 : 0) - (input.back ? 1 : 0);
    const s = (input.right ? 1 : 0) - (input.left ? 1 : 0);
    if (f !== 0 || s !== 0) {
      mx = (-sin * f + cos * s);
      mz = (-cos * f - sin * s);
      const len = Math.hypot(mx, mz);
      if (len > 0) { mx /= len; mz /= len; }
    }

    const speed = this.sprinting ? RUN_SPEED : WALK_SPEED;

    if (!this.flying) {
      // 地面移动（水平方向水平滑动，不随跳跃改变）
      this.vel.x = mx * speed;
      this.vel.z = mz * speed;

      // 垂直速度：重力 / 跳跃
      if (input.jump && this.onGround) {
        this.vel.y = JUMP_SPEED;
      } else if (input.down && this.onGround) {
        // （本版本无下蹲位移，保留接口）
      }
      this.vel.y -= GRAVITY * dt;
      if (this.vel.y < -50) this.vel.y = -50; // 终端速度

      // 分轴移动与碰撞
      this.moveAxis(this.vel.x * dt, 0);
      this.moveAxis(0, this.vel.z * dt);
      this.moveAxis(0, 0, this.vel.y * dt);
      // 上方有阻挡时清零竖直速度，防止持续吸附顶面
      this.onGround = this.checkGround();
    } else {
      // 飞行模式：直接移动，无碰撞
      this.pos.x += mx * speed * dt;
      this.pos.z += mz * speed * dt;
      const vy = (input.jump ? 1 : 0) - (input.down ? 1 : 0);
      this.pos.y += vy * speed * dt;
    }

    // 同步相机到玩家眼睛
    const eyeY = this.pos.y + PLAYER_HEIGHT - 0.2;
    this.camera.position.set(this.pos.x, eyeY, this.pos.z);
    this.camera.rotation.set(this.pitch, this.yaw, 0, 'YXZ');
  }

  /**
   * 沿单个轴移动并检测体素碰撞，若碰撞则贴面停下。
   * @param {number} dx
   * @param {number} dz
   * @param {number} dy
   */
  moveAxis(dx, dz, dy = 0) {
    if (dx !== 0) {
      let nx = this.pos.x + dx;
      if (this.collides(nx, this.pos.y, this.pos.z)) {
        // 撞墙：贴到边界
        nx = dx > 0 ? Math.floor(nx + PLAYER_HALF) - PLAYER_HALF - EPS
                     : Math.ceil(nx - PLAYER_HALF) + PLAYER_HALF + EPS;
        this.vel.x = 0;
      }
      this.pos.x = nx;
    }
    if (dz !== 0) {
      let nz = this.pos.z + dz;
      if (this.collides(this.pos.x, this.pos.y, nz)) {
        nz = dz > 0 ? Math.floor(nz + PLAYER_HALF) - PLAYER_HALF - EPS
                    : Math.ceil(nz - PLAYER_HALF) + PLAYER_HALF + EPS;
        this.vel.z = 0;
      }
      this.pos.z = nz;
    }
    if (dy !== 0) {
      let ny = this.pos.y + dy;
      if (this.collides(this.pos.x, ny, this.pos.z)) {
        if (dy > 0) {
          ny = Math.floor(ny + PLAYER_HEIGHT) - PLAYER_HEIGHT - EPS;
        } else {
          ny = Math.ceil(ny) + EPS;
        }
        this.vel.y = 0;
      }
      this.pos.y = ny;
    }
  }

  /** 判断玩家碰撞体（以脚底 pos 为中心）是否与任何实心方块相交 */
  collides(x, y, z) {
    const minX = x - PLAYER_HALF, maxX = x + PLAYER_HALF;
    const minY = y, maxY = y + PLAYER_HEIGHT;
    const minZ = z - PLAYER_HALF, maxZ = z + PLAYER_HALF;

    for (let bx = Math.floor(minX); bx <= Math.floor(maxX); bx++) {
      for (let by = Math.floor(minY); by <= Math.floor(maxY); by++) {
        for (let bz = Math.floor(minZ); bz <= Math.floor(maxZ); bz++) {
          const block = this.world.getBlockAt(bx, by, bz);
          if (block !== Block.Air && block !== Block.Water) {
            // AABB 相交检测
            if (
              bx + 1 > minX && bx < maxX &&
              by + 1 > minY && by < maxY &&
              bz + 1 > minZ && bz < maxZ
            ) return true;
          }
        }
      }
    }
    return false;
  }

  /** 检测玩家是否站在地面上（脚下相邻实心方块） */
  checkGround() {
    const minX = this.pos.x - PLAYER_HALF, maxX = this.pos.x + PLAYER_HALF;
    const minZ = this.pos.z - PLAYER_HALF, maxZ = this.pos.z + PLAYER_HALF;
    const y = this.pos.y - EPS;
    for (let bx = Math.floor(minX); bx <= Math.floor(maxX); bx++) {
      for (let bz = Math.floor(minZ); bz <= Math.floor(maxZ); bz++) {
        const block = this.world.getBlockAt(bx, Math.floor(y), bz);
        if (block !== Block.Air && block !== Block.Water) return true;
      }
    }
    return false;
  }
}
