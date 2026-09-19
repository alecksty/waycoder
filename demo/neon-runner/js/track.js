// =====================================================================
// track.js — Procedural endless track generator
// Manages chunks of ground, obstacles, coins, and pickups.
// =====================================================================
import * as THREE from 'three';
import { LANES } from './player.js';

const CHUNK_LENGTH = 24;
const CHUNKS_AHEAD = 8;
const OBSTACLE_TYPES = ['barrier', 'low', 'overhead'];

export class Track {
  constructor(scene) {
    this.scene = scene;
    this.chunks = [];
    this.obstacles = [];
    this.coins = [];
    this.pickups = [];
    this.nextChunkZ = 0;
    this.chunkCounter = 0;
    this.difficulty = 0;
    this._rngSeed = 12345;
    this._buildMaterials();
    this._buildInitialChunks();
  }

  _rand() {
    this._rngSeed = (this._rngSeed * 1664525 + 1013904223) >>> 0;
    return (this._rngSeed % 100000) / 100000;
  }

  _buildMaterials() {
    this.groundMat = new THREE.MeshStandardMaterial({
      color: 0x070a1a, metalness: 0.6, roughness: 0.3,
      emissive: 0x0a1230, emissiveIntensity: 0.5
    });
    this.gridLineMat = new THREE.MeshBasicMaterial({ color: 0x00f0ff, transparent: true, opacity: 0.55 });
    this.sideRailMat = new THREE.MeshBasicMaterial({ color: 0xff00d4, transparent: true, opacity: 0.6 });
    this.groundEdgeMat = new THREE.MeshBasicMaterial({ color: 0x00f0ff });
    this.coinMat = new THREE.MeshStandardMaterial({
      color: 0xffd54a, emissive: 0xffaa00, emissiveIntensity: 1.4,
      metalness: 0.95, roughness: 0.15
    });
  }

  _buildChunk(z) {
    const group = new THREE.Group();
    group.position.z = z;

    // Main ground plane
    const ground = new THREE.Mesh(
      new THREE.PlaneGeometry(8, CHUNK_LENGTH, 1, 1),
      this.groundMat
    );
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    group.add(ground);

    // Lane grid lines
    for (const lx of LANES) {
      const line = new THREE.Mesh(
        new THREE.PlaneGeometry(0.06, CHUNK_LENGTH),
        this.gridLineMat
      );
      line.rotation.x = -Math.PI / 2;
      line.position.set(lx, 0.01, 0);
      group.add(line);
    }

    // Edge glow strips
    for (const ex of [-4, 4]) {
      const edge = new THREE.Mesh(
        new THREE.BoxGeometry(0.15, 0.15, CHUNK_LENGTH),
        this.groundEdgeMat
      );
      edge.position.set(ex, 0.08, 0);
      group.add(edge);
    }

    // Side rails
    for (const sx of [-5, 5]) {
      const rail = new THREE.Mesh(
        new THREE.BoxGeometry(0.15, 1.4, CHUNK_LENGTH),
        this.sideRailMat.clone()
      );
      rail.position.set(sx, 0.7, 0);
      group.add(rail);
    }

    // Populate
    const id = this.chunkCounter++;
    this._populateChunk(group, z, id);

    this.scene.add(group);
    this.chunks.push({ group, z, id });
  }

  _populateChunk(group, chunkZ, id) {
    if (id < 2) return; // Safe spawn area

    const d = this.difficulty;
    const obstacleCount = (d < 0.2) ? 0 :
      (this._rand() < 0.4 + d * 0.5 ? 1 : 0) +
      (this._rand() < d * 0.5 ? 1 : 0);

    const usedLanes = new Set();
    for (let i = 0; i < obstacleCount; i++) {
      const lane = Math.floor(this._rand() * 3);
      if (usedLanes.has(lane) && usedLanes.size < 3) continue;
      usedLanes.add(lane);
      const type = OBSTACLE_TYPES[Math.floor(this._rand() * OBSTACLE_TYPES.length)];
      const safeType = (d < 0.3 && type === 'overhead') ? 'barrier' : type;
      const oz = chunkZ - CHUNK_LENGTH / 2 + (this._rand() * 0.6 + 0.2) * CHUNK_LENGTH;
      this._addObstacle(group, safeType, lane, oz);
    }

    // Coin row
    if (this._rand() < 0.7) {
      const lane = Math.floor(this._rand() * 3);
      const count = 4 + Math.floor(this._rand() * 4);
      const startZ = chunkZ - CHUNK_LENGTH / 2 + 1;
      const spacing = 1.4;
      const y = 1.2 + (this._rand() < 0.4 ? 0.5 : 0);
      for (let i = 0; i < count; i++) {
        const cz = startZ + i * spacing;
        if (cz > chunkZ + CHUNK_LENGTH / 2 - 0.5) break;
        this._addCoin(group, lane, cz, y);
      }
    }

    // Power-up
    if (this._rand() < 0.10) {
      const lane = Math.floor(this._rand() * 3);
      const pz = chunkZ - CHUNK_LENGTH / 2 + this._rand() * CHUNK_LENGTH;
      this._addPickup(group, lane, pz);
    }
  }

  _addObstacle(group, type, lane, z) {
    const x = LANES[lane];
    let mesh, hitbox;
    if (type === 'barrier') {
      const mat = new THREE.MeshStandardMaterial({
        color: 0x4a0a1a, emissive: 0xff3b6b, emissiveIntensity: 1.0,
        metalness: 0.4, roughness: 0.5
      });
      mesh = new THREE.Mesh(new THREE.BoxGeometry(1.2, 1.8, 0.3), mat);
      mesh.position.set(x, 0.9, z);
      mesh.castShadow = true;
      hitbox = { w: 1.0, h: 1.6, d: 0.3 };
    } else if (type === 'low') {
      const mat = new THREE.MeshStandardMaterial({
        color: 0x4a1a0a, emissive: 0xff8800, emissiveIntensity: 1.0,
        metalness: 0.4, roughness: 0.5
      });
      mesh = new THREE.Mesh(new THREE.BoxGeometry(1.4, 0.6, 1.0), mat);
      mesh.position.set(x, 0.3, z);
      mesh.castShadow = true;
      hitbox = { w: 1.2, h: 0.6, d: 1.0 };
    } else {
      const mat = new THREE.MeshStandardMaterial({
        color: 0x0a4a1a, emissive: 0x4dff9b, emissiveIntensity: 0.9,
        metalness: 0.4, roughness: 0.5
      });
      mesh = new THREE.Mesh(new THREE.BoxGeometry(1.4, 0.5, 1.2), mat);
      mesh.position.set(x, 1.2, z);
      mesh.castShadow = true;
      hitbox = { w: 1.4, h: 0.5, d: 1.2 };
    }
    group.add(mesh);
    this.obstacles.push({ mesh, lane, type, z, hitbox });
  }

  _addCoin(group, lane, z, y = 1.2) {
    const x = LANES[lane];
    const coin = new THREE.Mesh(
      new THREE.CylinderGeometry(0.32, 0.32, 0.08, 16),
      this.coinMat
    );
    coin.position.set(x, y, z);
    coin.rotation.z = Math.PI / 2;
    coin.castShadow = true;
    group.add(coin);
    this.coins.push({ mesh: coin, lane, z, y, collected: false, anim: Math.random() * Math.PI * 2 });
  }

  _addPickup(group, lane, z) {
    const x = LANES[lane];
    const isMagnet = Math.random() < 0.5;
    const color = isMagnet ? 0xff00d4 : 0x4dff9b;
    const mat = new THREE.MeshStandardMaterial({
      color, emissive: color, emissiveIntensity: 1.5,
      metalness: 0.7, roughness: 0.2
    });
    const geo = isMagnet
      ? new THREE.OctahedronGeometry(0.4, 0)
      : new THREE.IcosahedronGeometry(0.4, 0);
    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.set(x, 1.5, z);
    mesh.castShadow = true;
    group.add(mesh);
    this.pickups.push({ mesh, lane, z, y: 1.5, type: isMagnet ? 'magnet' : 'shield', collected: false, anim: Math.random() * Math.PI * 2 });
  }

  _buildInitialChunks() {
    for (let i = 0; i < CHUNKS_AHEAD + 3; i++) {
      this._buildChunk(this.nextChunkZ);
      this.nextChunkZ -= CHUNK_LENGTH;
    }
  }

  reset() {
    for (const c of this.chunks) {
      this.scene.remove(c.group);
      c.group.traverse((o) => { if (o.geometry) o.geometry.dispose(); });
    }
    this.chunks = [];
    this.obstacles = [];
    this.coins = [];
    this.pickups = [];
    this.nextChunkZ = 0;
    this.chunkCounter = 0;
    this.difficulty = 0;
    this._rngSeed = 12345 + Math.floor(Math.random() * 1000);
    this._buildInitialChunks();
  }

  update(dt, speed) {
    const dz = speed * dt;
    for (const c of this.chunks) { c.group.position.z += dz; c.z += dz; }
    for (const o of this.obstacles) o.z += dz;
    for (const c of this.coins) { c.z += dz; c.anim += dt * 5; }
    for (const p of this.pickups) { p.z += dz; p.anim += dt * 3; }

    // Recycle passed chunks
    const recycleThreshold = 12;
    while (this.chunks.length > 0 && this.chunks[this.chunks.length - 1].z > recycleThreshold) {
      const old = this.chunks.pop();
      this.scene.remove(old.group);
      old.group.traverse((o) => { if (o.geometry) o.geometry.dispose(); });
    }
    this.obstacles = this.obstacles.filter((o) => o.z < recycleThreshold);
    this.coins = this.coins.filter((c) => c.z < recycleThreshold);
    this.pickups = this.pickups.filter((p) => p.z < recycleThreshold);

    // Build ahead
    while (this.chunks.length > 0 && this.chunks[0].z > -CHUNK_LENGTH * (CHUNKS_AHEAD - 1)) {
      this._buildChunk(this.nextChunkZ);
      this.nextChunkZ -= CHUNK_LENGTH;
    }

    // Animations
    for (const c of this.coins) {
      c.mesh.rotation.x += dt * 5;
      c.mesh.position.y = c.y + Math.sin(c.anim) * 0.1;
    }
    for (const p of this.pickups) {
      p.mesh.rotation.y += dt * 3;
      p.mesh.rotation.x += dt * 2;
      p.mesh.position.y = p.y + Math.sin(p.anim) * 0.18;
    }

    // Rail pulse
    const t = performance.now() * 0.003;
    for (const c of this.chunks) {
      for (const child of c.group.children) {
        if (child.material && child.material.color &&
            child.material.color.getHex() === 0xff00d4 &&
            child.material.transparent) {
          child.material.opacity = 0.4 + Math.sin(t) * 0.2;
        }
      }
    }
  }

  checkCollisions(player) {
    const events = [];
    const px = player.x, py = player.y, pz = 0;
    const playerBox = {
      xMin: px - 0.3, xMax: px + 0.3,
      yMin: py + 0.0, yMax: py + (player.isSliding ? 0.7 : 1.8),
      zMin: pz - 0.2, zMax: pz + 0.2
    };

    for (const c of this.coins) {
      if (c.collected || Math.abs(c.z) > 1.5) continue;
      const dx = Math.abs(c.mesh.position.x - px);
      if (dx < 0.8 && Math.abs(c.mesh.position.y - py - 1.0) < 0.8) {
        c.collected = true;
        c.mesh.visible = false;
        events.push({ type: 'coin', value: 1 });
      }
    }

    for (const p of this.pickups) {
      if (p.collected || Math.abs(p.z) > 1.5) continue;
      const dx = Math.abs(p.mesh.position.x - px);
      if (dx < 0.8 && Math.abs(p.mesh.position.y - py - 1.0) < 1.0) {
        p.collected = true;
        p.mesh.visible = false;
        events.push({ type: 'pickup', ref: p.type });
      }
    }

    for (const o of this.obstacles) {
      if (Math.abs(o.z) > 1.0) continue;
      const dx = Math.abs(o.mesh.position.x - px);
      if (dx > 0.9) continue;
      const hb = o.hitbox;
      const oy = o.mesh.position.y;
      const oz = o.z;
      if (playerBox.zMax < oz - hb.d / 2) continue;
      if (playerBox.zMin > oz + hb.d / 2) continue;
      if (playerBox.xMax < o.mesh.position.x - hb.w / 2) continue;
      if (playerBox.xMin > o.mesh.position.x + hb.w / 2) continue;
      if (playerBox.yMax > oy - hb.h / 2 && playerBox.yMin < oy + hb.h / 2) {
        if (o.type === 'low' && playerBox.yMin > oy - hb.h / 2 + 0.15) continue;
        if (o.type === 'overhead' && player.isSliding) continue;
        events.push({ type: 'obstacle', ref: o });
      }
    }

    return events;
  }

  setDifficulty(d) { this.difficulty = d; }
}
