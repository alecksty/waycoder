// main.js — 游戏主循环入口：组装 Three.js 场景/渲染器/相机、世界、玩家、输入、UI。
// 负责：昼夜循环、区块流式加载、方块选中高亮、放置/破坏交互、HUD 与菜单、存档。

import * as THREE from 'three';
import { World } from './world.js';
import { Player } from './player.js';
import { Input } from './input.js';
import { raycast } from './raycast.js';
import { AudioFX } from './audio.js';
import { Block } from './chunk.js';
import { saveWorld, loadWorld, clearWorld, backupWorld } from './storage.js';

// ---------- 方块快捷栏（含图标 emoji + 名称） ----------
const HOTBAR = [
  { block: Block.Grass, icon: '🌿', name: '草地' },
  { block: Block.Dirt,  icon: '🟫', name: '泥土' },
  { block: Block.Stone, icon: '🪨', name: '石头' },
  { block: Block.Sand,  icon: '🏖️', name: '沙子' },
  { block: Block.Wood,  icon: '🪵', name: '木材' },
  { block: Block.Leaves,icon: '🍃', name: '树叶' },
  { block: Block.Coal,  icon: '⬛', name: '煤矿' },
  { block: Block.Iron,  icon: '⚙️', name: '铁矿' },
];

const DAY_LENGTH = 600;      // 一天时长（秒）
const DAY_FRACTION = 0.5;    // 白天占比

class Game {
  constructor() {
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(72, innerWidth / innerHeight, 0.1, 1000);
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setSize(innerWidth, innerHeight);
    this.renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = false;
    document.getElementById('gameMount').appendChild(this.renderer.domElement);

    // 存档：优先读取已保存，否则新生成
    const saved = loadWorld();
    this.seed = saved ? saved.seed : Math.floor(Math.random() * 1e9);

    this.world = new World(this.scene, this.seed);
    this.player = new Player(this.camera, this.world);
    this.audio = new AudioFX();

    // 光照（太阳方向光 + 环境光）
    this.sun = new THREE.DirectionalLight(0xffffff, 1.4);
    this.sun.position.set(60, 100, 40);
    this.scene.add(this.sun);
    this.ambient = new THREE.AmbientLight(0xffffff, 0.55);
    this.scene.add(this.ambient);
    this.hemi = new THREE.HemisphereLight(0xbfdfff, 0x8a7a55, 0.4);
    this.scene.add(this.hemi);

    // 天空/雾
    this.fog = new THREE.Fog(0x87ceeb, 60, 220);
    this.scene.fog = this.fog;
    this.scene.background = new THREE.Color(0x87ceeb);

    // 方块选中高亮框
    this.highlight = new THREE.LineSegments(
      new THREE.EdgesGeometry(new THREE.BoxGeometry(1.001, 1.001, 1.001)),
      new THREE.LineBasicMaterial({ color: 0x000000 })
    );
    this.highlight.visible = false;
    this.scene.add(this.highlight);

    // 玩家出生点（从存档或地面）
    if (saved && saved.playerPosition) {
      const [px, py, pz] = saved.playerPosition;
      this.player.spawn(px, py, pz);
      this.player.yaw = saved.yaw ?? 0;
      this.player.pitch = saved.pitch ?? 0;
    } else {
      const sy = this.world.heightAt(0.5, 0.5) + 2;
      this.player.spawn(0.5, sy, 0.5);
    }

    // 输入
    this.input = new Input(this.renderer.domElement, {
      onLock: () => this.onLock(),
      onUnlock: () => this.onUnlock(),
      onHotbar: (i) => this.setHotbar(i),
      onDig: () => this.dig(),
      onPlace: () => this.place(),
      onSelect: () => {},
    });
    this.hotbarIndex = 0;

    // 世界时间（昼夜）
    this.timeOfDay = saved ? (saved.time ?? DAY_LENGTH * 0.3) : DAY_LENGTH * 0.3;

    // 编辑记录（用于存档）
    this.edits = [];   // {x,y,z,block}
    this.blockBreaks = 0;
    this.blockPlaces = 0;

    this.loadUI();
    this.setupResize();
    this.setupCrosshair();
    this.buildHotbarDOM();

    window.addEventListener('error', (e) => this.onError(e));
  }

  // ---------- UI 构建 ----------
  loadUI() {
    const menu = document.getElementById('menu');
    document.getElementById('btnStart').addEventListener('click', () => this.startGame());
    document.getElementById('btnNewWorld').addEventListener('click', () => this.newWorld());
    document.getElementById('btnHowto').addEventListener('click', () => this.showMenu('howto'));
    document.getElementById('btnHowtoClose').addEventListener('click', () => this.closeOverlay('howto'));
    document.getElementById('btnMenu').addEventListener('click', () => this.quitToMenu());
    document.getElementById('btnResume').addEventListener('click', () => this.resumeGame());
    // 菜单默认显示
    menu.classList.remove('hidden');
    this.startLoop();
  }

  setupResize() {
    window.addEventListener('resize', () => {
      this.camera.aspect = innerWidth / innerHeight;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(innerWidth, innerHeight);
    });
  }

  // 准星
  setupCrosshair() {
    const cross = document.getElementById('crosshair');
    if (cross) cross.style.display = 'none';
  }

  // 快捷栏 DOM
  buildHotbarDOM() {
    const bar = document.getElementById('hotbar');
    bar.innerHTML = '';
    HOTBAR.forEach((h, i) => {
      const el = document.createElement('div');
      el.className = 'hb-slot' + (i === this.hotbarIndex ? ' active' : '');
      el.innerHTML = `<span class="hb-icon">${h.icon}</span><span class="hb-key">${i + 1}</span>`;
      el.title = h.name;
      el.addEventListener('click', () => { this.setHotbar(i); this.input.requestLock(); });
      bar.appendChild(el);
    });
  }

  setHotbar(i) {
    const n = HOTBAR.length;
    this.hotbarIndex = ((i % n) + n) % n;
    const bar = document.getElementById('hotbar');
    [...bar.children].forEach((el, idx) => {
      el.classList.toggle('active', idx === this.hotbarIndex);
    });
    this.audio.select();
    this.updateHotbarLabel();
  }

  updateHotbarLabel() {
    const h = HOTBAR[this.hotbarIndex];
    const lab = document.getElementById('hotbar-label');
    if (lab) lab.textContent = h.name;
  }

  onLock() {
    document.getElementById('menu').classList.add('hidden');
    document.getElementById('crosshair').style.display = 'block';
    this.audio.init();
    this.audio.resume();
    this.locked = true;
  }

  onUnlock() {
    document.getElementById('crosshair').style.display = 'none';
    this.showMenu('pause');
    this.input.reset();
    this.locked = false;
  }

  // ---------- 菜单/流程 ----------
  startGame() {
    this.audio.init();
    this.input.requestLock();
  }

  resumeGame() {
    this.audio.init();
    this.input.requestLock();
  }

  newWorld() {
    backupWorld();
    clearWorld();
    location.reload();
  }

  quitToMenu() {
    document.exitPointerLock?.();
    this.showMenu('menu');
    this.saveGame();
  }

  showMenu(name) {
    const menus = ['menu', 'pause', 'howto'];
    menus.forEach(m => {
      document.getElementById(m).classList.toggle('hidden', m !== name);
    });
  }

  closeOverlay(name) {
    document.getElementById(name).classList.add('hidden');
    this.showMenu('menu');
  }

  onError(e) {
    const box = document.getElementById('err');
    if (box) { box.style.display = 'block'; box.textContent = '⚠️ ' + e.message; }
  }

  // ---------- 交互：破坏/放置 ----------
  getTarget() {
    const dir = new THREE.Vector3();
    this.camera.getWorldDirection(dir);
    return raycast(this.world, this.camera.position, dir, 6);
  }

  dig() {
    const target = this.getTarget();
    if (!target) return;
    const { x, y, z } = target;
    const b = this.world.getBlockAt(x, y, z);
    if (b === Block.Air || b === Block.Bedrock) return; // 基岩不可破坏
    // 记录编辑（block=0 表示移除）
    this.edits.push({ x, y, z, block: Block.Air });
    this.world.setBlockAt(x, y, z, Block.Air);
    this.blockBreaks++;
    this.audio.dig();
    this.updateStats();
  }

  place() {
    const target = this.getTarget();
    if (!target) return;
    const px = target.x + target.normal[0];
    const py = target.y + target.normal[1];
    const pz = target.z + target.normal[2];
    // 防止在玩家体内放置
    if (this.player.collides(px + 0.5, py, pz + 0.5)) return;
    const block = HOTBAR[this.hotbarIndex].block;
    if (this.world.getBlockAt(px, py, pz) !== Block.Air) return;
    this.edits.push({ x: px, y: py, z: pz, block });
    this.world.setBlockAt(px, py, pz, block);
    this.blockPlaces++;
    this.audio.place();
    this.updateStats();
  }

  updateStats() {
    const el = document.getElementById('stats');
    if (el) el.textContent = `挖:${this.blockBreaks} 放:${this.blockPlaces}`;
  }

  // ---------- 交互提示（十字准星下方） ----------
  updatePrompt() {
    const t = this.getTarget();
    const prompt = document.getElementById('prompt');
    if (!prompt) return;
    if (this.locked && t) {
      const bname = this.blockName(t.block);
      prompt.style.display = 'block';
      prompt.textContent = `[${t.x},${t.y},${t.z}] ${bname}`;
    } else {
      prompt.style.display = 'none';
    }
  }

  blockName(id) {
    const map = {
      [Block.Grass]: '草地', [Block.Dirt]: '泥土', [Block.Stone]: '石头',
      [Block.Sand]: '沙子', [Block.Water]: '水', [Block.Wood]: '木材',
      [Block.Leaves]: '树叶', [Block.Coal]: '煤矿', [Block.Iron]: '铁矿', [Block.Bedrock]: '基岩',
    };
    return map[id] || '空气';
  }

  // ---------- 存档 ----------
  saveGame() {
    const edits = this.edits;
    const playerPosition = [this.player.pos.x, this.player.pos.y, this.player.pos.z];
    saveWorld({ seed: this.seed, edits, playerPosition, time: this.timeOfDay, yaw: this.player.yaw, pitch: this.player.pitch });
  }

  // ---------- 主循环 ----------
  startLoop() {
    this.clock = new THREE.Clock();
    this.renderer.setAnimationLoop(() => this.animate());
  }

  animate() {
    const dt = Math.min(this.clock.getDelta(), 0.05); // 钳制帧时间
    this.timeOfDay += dt;

    // 输入->玩家
    const { dx, dy } = this.input.consumeMouse();
    if (this.locked) {
      this.player.yaw -= dx * this.input.sens;
      this.player.pitch -= dy * this.input.sens;
      this.player.pitch = Math.max(-Math.PI / 2 + 0.01, Math.min(Math.PI / 2 - 0.01, this.player.pitch));
    }
    this.player.update(dt, this.input.state);

    // 世界区块流式加载（围绕玩家）
    const pcx = Math.floor(this.player.pos.x / 16);
    const pcz = Math.floor(this.player.pos.z / 16);
    this.world.updateAround(pcx, pcz, 3);
    this.world.rebuildDirty();

    // 昼夜循环：根据时间太阳位置 + 天空色/雾色
    this.updateDayNight();

    // 方块高亮
    this.updateHighlight();

    // 在准星下方显示目标方块提示
    this.updatePrompt();

    // 渲染
    this.renderer.render(this.scene, this.camera);
  }

  updateHighlight() {
    const t = this.getTarget();
    if (t && this.locked) {
      this.highlight.visible = true;
      this.highlight.position.set(t.x + 0.5, t.y + 0.5, t.z + 0.5);
    } else {
      this.highlight.visible = false;
    }
  }

  updateDayNight() {
    // 0..DAY_LENGTH 周期，白天用正弦插值太阳角度
    const t = (this.timeOfDay % DAY_LENGTH) / DAY_LENGTH;
    const sunAngle = (t - 0.25) * Math.PI * 2;
    const height = Math.sin(sunAngle); // -1..1
    const dayness = Math.max(0, Math.min(1, height * 1.4 + 0.3));

    // 太阳位置
    this.sun.position.set(Math.cos(sunAngle) * 80, Math.max(0.1, height) * 100, 30);

    // 光照强度变化
    this.sun.intensity = 0.3 + dayness * 1.2;
    this.ambient.intensity = 0.25 + dayness * 0.45;
    this.hemi.intensity = 0.2 + dayness * 0.35;

    // 天空/雾颜色渐变（白天蓝 -> 黄昏橙 -> 夜晚深蓝）
    const sky = new THREE.Color();
    const daySky = new THREE.Color(0x87ceeb);
    const duskSky = new THREE.Color(0xff8c5a);
    const nightSky = new THREE.Color(0x0a1a3a);
    if (height > 0.2) sky.copy(daySky).lerp(duskSky, Math.max(0, 1 - height) * 0.6);
    else if (height > -0.2) sky.copy(duskSky).lerp(nightSky, Math.max(0, -height) * 1.5);
    else sky.copy(nightSky);

    this.scene.background = sky;
    this.fog.color.copy(sky);
  }
}

// 启动
waitForDOM(() => {
  new Game();
});

/** 等待 DOM 就绪 */
function waitForDOM(cb) {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', cb);
  } else cb();
}
