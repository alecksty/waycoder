/*
 * game.js —— 游戏主控：状态机 / 输入 / 渲染流水线 / 日夜循环 / 存档
 * 状态：title → play ⇄ inventory | pause | help；play → dead → respawn
 */
'use strict';

const SAVE_KEY = 'blockworld_save_v1';

const Game = {
  state: 'title',
  canvas: null, ctx: null,
  vw: 0, vh: 0, dpr: 1,
  camX: 0, camY: 0,

  /* 鼠标：sx/sy 屏幕坐标，x/y 世界坐标（像素） */
  mouse: { sx: 0, sy: 0, x: 0, y: 0 },

  /* 输入 */
  input: { left: false, right: false, up: false, down: false, jump: false },
  mouseDown: { left: false, right: false },
  hotbarSel: 0,

  /* 时间 */
  t: 0, day: 1,
  dayLen: 600, nightLen: 300,

  kills: 0,
  msg: '', msgT: 0,
  hasSave: false,
  hasWorkbench: false, hasFurnace: false,

  clouds: [],
  spawnTimer: 4,
  saveTimer: 30,
  stationScan: 0,
  fps: null,
  lastT: 0,

  get cycle() { return this.dayLen + this.nightLen; },
  get phase() { return Core.mod(this.t, this.cycle) / this.cycle; },
  get isDay() { return Core.mod(this.t, this.cycle) < this.dayLen; },
  get isNight() { return !this.isDay; },
  get timeOfDay() { return this.phase; },
  /* 环境亮度 0..1（白天 1，深夜 0.2） */
  get lightLevel() {
    const p = this.phase;
    if (p < 0.56) return 1;
    if (p < 0.62) return Core.lerp(1, 0.3, (p - 0.56) / 0.06);        // 黄昏
    if (p < 0.93) return Core.lerp(0.3, 0.2, (p - 0.62) / 0.31);      // 夜
    return Core.lerp(0.2, 1, (p - 0.93) / 0.07);                       // 黎明
  },

  /* ================= 启动 ================= */
  boot() {
    this.canvas = document.getElementById('game');
    this.ctx = this.canvas.getContext('2d');
    this.fps = Core.MakeFpsMeter();
    this.resize();
    window.addEventListener('resize', () => this.resize());
    window.addEventListener('keydown', e => this.onKey(e, true));
    window.addEventListener('keyup', e => this.onKey(e, false));
    window.addEventListener('blur', () => { if (this.state === 'play') this.state = 'pause'; });
    this.canvas.addEventListener('mousedown', e => this.onMouseDown(e));
    window.addEventListener('mouseup', e => this.onMouseUp(e));
    window.addEventListener('mousemove', e => this.onMouseMove(e));
    this.canvas.addEventListener('contextmenu', e => e.preventDefault());
    this.canvas.addEventListener('wheel', e => {
      e.preventDefault();
      this.hotbarSel = Core.mod(this.hotbarSel + Math.sign(e.deltaY), 9);
    }, { passive: false });

    /* 云 */
    const rng = Core.makeRng(42);
    this.clouds = [];
    for (let i = 0; i < 8; i++) {
      this.clouds.push({ x: rng() * 4000, y: 40 + rng() * 150, w: 60 + rng() * 90, h: 14 + rng() * 16, speed: 4 + rng() * 9 });
    }

    this.hasSave = !!localStorage.getItem(SAVE_KEY);
    this.lastT = Core.now();
    requestAnimationFrame(t => this.loop(t));
  },

  resize() {
    this.dpr = window.devicePixelRatio || 1;
    this.vw = window.innerWidth;
    this.vh = window.innerHeight;
    this.canvas.width = Math.round(this.vw * this.dpr);
    this.canvas.height = Math.round(this.vh * this.dpr);
    this.canvas.style.width = this.vw + 'px';
    this.canvas.style.height = this.vh + 'px';
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
  },

  /* ================= 输入 ================= */
  onKey(e, down) {
    const k = e.key;
    if (k === 'F5') { e.preventDefault(); if (down) this.save(); return; }
    if (down && e.repeat) {
      /* 移动键允许自动重复 */
      if (k !== 'ArrowLeft' && k !== 'ArrowRight' && k !== 'ArrowUp' && k !== 'ArrowDown' &&
          k !== 'a' && k !== 'A' && k !== 'd' && k !== 'D' && k !== 'w' && k !== 'W' && k !== 's' && k !== 'S') return;
    }
    switch (k) {
      case 'ArrowLeft': case 'a': case 'A': this.input.left = down; e.preventDefault(); break;
      case 'ArrowRight': case 'd': case 'D': this.input.right = down; e.preventDefault(); break;
      case 'ArrowUp': case 'w': case 'W':
        this.input.up = down;
        if (down && !e.repeat && this.state === 'play') this.input.jump = true;
        e.preventDefault(); break;
      case ' ': if (down && !e.repeat && this.state === 'play') this.input.jump = true; e.preventDefault(); break;
      case 'ArrowDown': case 's': case 'S': this.input.down = down; e.preventDefault(); break;
      case '1': case '2': case '3': case '4': case '5':
      case '6': case '7': case '8': case '9':
        if (down) { this.hotbarSel = +k - 1; AudioSys.sfx('click'); }
        break;
      case 'e': case 'E':
        if (!down) break;
        if (this.state === 'play') { this.state = 'inventory'; UI.craftTab = 0; UI.hoverCraft = -1; AudioSys.sfx('open'); }
        else if (this.state === 'inventory') { this.state = 'play'; AudioSys.sfx('open'); }
        break;
      case 'h': case 'H':
        if (!down) break;
        if (this.state === 'play') this.state = 'help';
        else if (this.state === 'help') this.state = 'play';
        break;
      case 'f': case 'F':
        if (down && !e.repeat && this.state === 'play') this.eatSelected();
        break;
      case 'Escape':
        if (!down) break;
        if (this.state === 'play') this.state = 'pause';
        else if (this.state === 'pause' || this.state === 'inventory' || this.state === 'help') this.state = 'play';
        break;
      default: break;
    }
  },

  onMouseMove(e) {
    const rect = this.canvas.getBoundingClientRect();
    const sx = e.clientX - rect.left, sy = e.clientY - rect.top;
    this.mouse.sx = sx; this.mouse.sy = sy;
    this.mouse.x = sx + this.camX; this.mouse.y = sy + this.camY;
  },

  onMouseDown(e) {
    AudioSys.init();
    AudioSys.resume();
    this.onMouseMove(e);
    if (e.button === 0) {
      this.mouseDown.left = true;
      if (this.state === 'play') Player.interactPrimary(this.mouse.x, this.mouse.y);
      else this.clickAt(this.mouse.sx, this.mouse.sy);
    } else if (e.button === 2) {
      this.mouseDown.right = true;
      if (this.state === 'play') Player.placeBlock(this.mouse.x, this.mouse.y);
    }
  },

  onMouseUp(e) {
    if (e.button === 0) this.mouseDown.left = false;
    if (e.button === 2) this.mouseDown.right = false;
  },

  /* 界面点击命中分发 */
  clickAt(sx, sy) {
    const inside = h => sx >= h.x && sx <= h.x + h.w && sy >= h.y && sy <= h.y + h.h;
    if (this.state === 'title') {
      for (const b of UI.hitTitle) {
        if (!inside(b)) continue;
        AudioSys.sfx('click');
        if (b.id === 'continue') this.continueGame();
        else if (b.id === 'new') this.newGame();
        else if (b.id === 'help') this.state = 'help';
        return;
      }
    } else if (this.state === 'pause') {
      for (const b of UI.hitPause) {
        if (!inside(b)) continue;
        AudioSys.sfx('click');
        if (b.id === 'resume') this.state = 'play';
        else if (b.id === 'save') this.save();
        else if (b.id === 'title') { this.save(); this.backToTitle(); }
        else if (b.id === 'quit') this.backToTitle();
        return;
      }
    } else if (this.state === 'dead') {
      for (const b of UI.hitDead) {
        if (!inside(b)) continue;
        AudioSys.sfx('click');
        if (b.id === 'respawn') this.respawn();
        else if (b.id === 'title') this.backToTitle();
        return;
      }
    } else if (this.state === 'inventory') {
      /* tab 切换 */
      for (let i = 0; i < UI.hitInv.tabs.length; i++) {
        const t = UI.hitInv.tabs[i];
        if (inside(t)) {
          const ok = i === 0 ? true : i === 1 ? this.hasWorkbench : this.hasFurnace;
          if (ok) { UI.craftTab = i; UI.hoverCraft = -1; AudioSys.sfx('click'); }
          else AudioSys.sfx('error');
          return;
        }
      }
      /* 物品格 */
      for (let i = 0; i < UI.hitInv.slots.length; i++) {
        if (inside(UI.hitInv.slots[i])) { Inv.clickSlot(i); AudioSys.sfx('click'); return; }
      }
      /* 合成按钮 */
      const btn = UI.hitInv.craftBtn;
      if (btn && inside(btn)) {
        const entry = UI.hoverCraft >= 0 ? UI.hitInv.craft[UI.hoverCraft] : null;
        if (entry && entry.entry) {
          if (entry.entry.canCraft) { Craft.craft(entry.entry.recipe); AudioSys.sfx('craft'); }
          else AudioSys.sfx('error');
        }
        return;
      }
    }
  },

  /* ================= 流程 ================= */
  newGame() {
    const seed = (Math.random() * 0x7fffffff) | 0;
    World.generate(seed);
    Inv.reset();                          // 新世界清空背包
    Inv.add(10, 8);                       // 出生礼包：8 根火把
    this.prepareWorld();
    this.t = 0; this.day = 1; this.kills = 0;
    this.hotbarSel = 0;
    this.hasSave = false;
    this.state = 'play';
    this.msg('欢迎来到方块世界！先砍树 → 做木板 → 工作台 → 木镐挖矿。H 看玩法', 8);
    AudioSys.sfx('craft');
  },

  continueGame() {
    if (!this.load()) { this.msg('没有可用的存档', 2); this.state = 'title'; return; }
    this.prepareWorld();
    this.state = 'play';
    this.msg('存档已载入', 2);
  },

  /* 玩家/实体/相机就位（新世界或读档后） */
  prepareWorld() {
    const s = World.spawn;
    /* 清空出生点附近的树，防堵 */
    for (let dy = -5; dy <= 0; dy++) {
      for (let dx = -2; dx <= 2; dx++) {
        const id = World.get(s.x + dx, s.y + dy);
        if (id === 5 || id === 6) World.set(s.x + dx, s.y + dy, 0);
      }
    }
    /* 出生点两侧插两根火把 */
    World.set(s.x - 1, s.y - 1, 10);
    World.set(s.x + 1, s.y - 1, 10);
    Player.reset(s.x * Core.BLOCK + (Core.BLOCK - Player.w) / 2, s.y * Core.BLOCK - Player.h - 0.1);
    Player.hp = Player.maxHp;
    Entities.reset();
    this.camX = Player.cx - this.vw / 2;
    this.camY = Player.cy - this.vh / 2 - 20;
    this.spawnTimer = 4;
    this.scanStations();
  },

  backToTitle() {
    this.state = 'title';
    this.hasSave = !!localStorage.getItem(SAVE_KEY);
  },

  onPlayerDeath() {
    this.state = 'dead';
    AudioSys.sfx('death');
  },

  respawn() {
    const s = World.spawn;
    Player.reset(s.x * Core.BLOCK + (Core.BLOCK - Player.w) / 2, s.y * Core.BLOCK - Player.h - 0.1);
    Player.hp = Player.maxHp;
    this.state = 'play';
    this.camX = Player.cx - this.vw / 2;
    this.camY = Player.cy - this.vh / 2;
    AudioSys.sfx('respawn');
    this.msg('你重生了', 2);
  },

  msg(s, dur) { this.msg = s; this.msgT = dur || 3; },

  /* 吃选中格的食物（F 键）：恢复生命 */
  eatSelected() {
    const sel = Inv.selected();
    if (!sel) { this.msg('选中格没有物品', 1.6); return; }
    const def = Items.get(sel.id);
    if (!def || def.type !== 'food') { this.msg('这不是食物，无法食用', 1.6); return; }
    if (Player.hp >= Player.maxHp) { this.msg('生命值已满', 1.6); return; }
    Inv.removeSlot(Game.hotbarSel, 1);
    Player.heal(def.heal);
    AudioSys.sfx('eat');
    this.msg('吃了 ' + def.name + '，恢复 ' + def.heal + ' 点生命', 1.8);
  },

  /* ================= 存档 ================= */
  saveSilent() {
    try {
      const o = {
        v: 1, seed: World.seed, world: World.serialize(),
        px: Player.x, py: Player.y, hp: Player.hp,
        inv: Inv.serialize(), t: this.t, day: this.day, kills: this.kills
      };
      localStorage.setItem(SAVE_KEY, JSON.stringify(o));
      this.hasSave = true;
    } catch (e) { /* 静默失败 */ }
  },

  save() {
    this.saveSilent();
    this.msg('游戏已保存', 1.6);
    AudioSys.sfx('craft');
  },

  load() {
    const raw = localStorage.getItem(SAVE_KEY);
    if (!raw) return false;
    try {
      const o = JSON.parse(raw);
      World.deserialize(o.world);
      World.seed = o.seed;
      World.spawn = { x: Math.floor(World.W / 2), y: World.surfaceAt(Math.floor(World.W / 2)) };
      Player.reset(o.px, o.py);
      Player.hp = o.hp;
      Inv.deserialize(o.inv);
      this.t = o.t || 0; this.day = o.day || 1; this.kills = o.kills || 0;
      this.hasSave = true;
      return true;
    } catch (e) {
      localStorage.removeItem(SAVE_KEY);
      return false;
    }
  },

  /* 扫描周围设施（工作台/熔炉） */
  scanStations() {
    this.hasWorkbench = false;
    this.hasFurnace = false;
    const bx = Math.floor(Player.cx / Core.BLOCK);
    const by = Math.floor(Player.cy / Core.BLOCK);
    for (let dy = -6; dy <= 6 && !(this.hasWorkbench && this.hasFurnace); dy++) {
      for (let dx = -6; dx <= 6; dx++) {
        const id = World.get(bx + dx, by + dy);
        if (id === 8) this.hasWorkbench = true;
        else if (id === 9) this.hasFurnace = true;
      }
    }
  },

  /* ================= 更新 ================= */
  update(dt) {
    this.fps.tick();
    if (this.state !== 'play') return;

    /* 日夜推进 + 跨天检测 */
    const wasDay = this.isDay;
    this.t += dt;
    if (wasDay && this.isNight) this.msg('夜晚降临，僵尸开始出没…', 4);
    if (!wasDay && this.isDay) { this.day++; this.msg('第 ' + this.day + ' 天，太阳升起', 3); }

    /* 云 */
    for (const c of this.clouds) {
      c.x += c.speed * dt;
      if (c.x - c.w > this.vw) { c.x = -c.w * 2; c.y = 40 + Math.random() * 150; }
    }

    Player.update(dt, this.input);

    /* 实体 */
    Entities.update(dt);
    World.updateSaplings(dt);
    this.spawnTimer -= dt;
    if (this.spawnTimer <= 0) { this.spawnTimer = 2.5; Entities.spawnUpdate(dt); }

    /* 设施扫描 */
    this.stationScan -= dt;
    if (this.stationScan <= 0) { this.stationScan = 0.4; this.scanStations(); }

    /* 自动存档 */
    this.saveTimer -= dt;
    if (this.saveTimer <= 0) { this.saveTimer = 30; this.saveSilent(); }

    /* 按住左键持续挖掘/攻击 */
    if (this.mouseDown.left) Player.interactPrimary(this.mouse.x, this.mouse.y);

    /* 消息计时 */
    if (this.msgT > 0) { this.msgT -= dt; if (this.msgT <= 0) this.msg = ''; }

    /* 相机跟随（平滑） */
    const tx = Player.cx - this.vw / 2;
    const ty = Player.cy - this.vh / 2 - 30;
    this.camX += (tx - this.camX) * Math.min(1, dt * 6);
    this.camY += (ty - this.camY) * Math.min(1, dt * 6);
    this.camX = Core.clamp(this.camX, 0, Math.max(0, World.W * Core.BLOCK - this.vw));
    this.camY = Core.clamp(this.camY, 0, Math.max(0, World.H * Core.BLOCK - this.vh));
  },

  /* ================= 渲染 ================= */
  draw() {
    const { ctx } = this;
    ctx.imageSmoothingEnabled = false;
    this.drawSky();
    this.drawWorld();
    this.drawLighting();
    this.drawDrops();
    this.drawEnemies();
    this.drawCrack();
    this.drawPlayer();
    this.drawParticles();

    if (this.state === 'play' || this.state === 'inventory' || this.state === 'help') {
      UI.drawHUD(ctx, this.vw, this.vh);
    }
    if (this.msg && (this.state === 'play' || this.state === 'inventory')) this.drawMsg();

    if (this.state === 'title') UI.drawTitle(ctx, this.vw, this.vh);
    else if (this.state === 'inventory') UI.drawInventory(ctx, this.vw, this.vh);
    else if (this.state === 'pause') UI.drawPause(ctx, this.vw, this.vh);
    else if (this.state === 'dead') UI.drawDead(ctx, this.vw, this.vh);
    else if (this.state === 'help') UI.drawHelp(ctx, this.vw, this.vh);
  },

  /* ---- 天空 ---- */
  drawSky() {
    const { ctx, vw, vh } = this;
    const l = this.lightLevel;
    const dusk = Math.max(0, 1 - Math.abs(this.phase - 0.58) / 0.09);
    let top = Core.mixHex('#3a6fc8', '#070b24', 1 - l);
    let bot = Core.mixHex('#a8d4f8', '#131a38', 1 - l);
    top = Core.mixHex(top, '#e88840', dusk * 0.7);
    bot = Core.mixHex(bot, '#e8a060', dusk * 0.7);
    const g = ctx.createLinearGradient(0, 0, 0, vh);
    g.addColorStop(0, top);
    g.addColorStop(1, bot);
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, vw, vh);

    /* 太阳 / 月亮 */
    if (!this.isNight) {
      const p = this.phase / 0.667;
      const sx = vw * (0.12 + 0.76 * p);
      const sy = vh * (0.3 - Math.sin(p * Math.PI) * 0.14);
      const sg = ctx.createRadialGradient(sx, sy, 4, sx, sy, 22);
      sg.addColorStop(0, '#fff8d0');
      sg.addColorStop(1, 'rgba(255,200,80,0)');
      ctx.fillStyle = sg;
      ctx.beginPath(); ctx.arc(sx, sy, 22, 0, Core.TAU); ctx.fill();
      ctx.fillStyle = '#fff2b0';
      ctx.beginPath(); ctx.arc(sx, sy, 11, 0, Core.TAU); ctx.fill();
    } else {
      const p = (this.phase - 0.667) / 0.333;
      const sx = vw * (0.12 + 0.76 * p);
      const sy = vh * 0.2;
      ctx.fillStyle = '#e8ecf8';
      ctx.beginPath(); ctx.arc(sx, sy, 10, 0, Core.TAU); ctx.fill();
      ctx.fillStyle = '#c8d0e8';
      ctx.beginPath(); ctx.arc(sx + 4, sy - 3, 8, 0, Core.TAU); ctx.fill();
      ctx.fillStyle = Core.mixHex('#c8d0e8', '#0a0f28', 0.5);
      ctx.beginPath(); ctx.arc(sx - 5, sy + 5, 3, 0, Core.TAU); ctx.fill();
    }

    /* 远山剪影 */
    ctx.fillStyle = Core.mixHex('#2e3c60', '#141830', 1 - l);
    ctx.beginPath();
    ctx.moveTo(0, vh);
    for (let x = 0; x <= vw; x += 24) {
      const n = 0.5 + 0.5 * Math.sin(x * 0.006 + 2.1);
      const n2 = 0.5 + 0.5 * Math.sin(x * 0.017 + 7.7);
      ctx.lineTo(x, vh - 40 - n * 90 - n2 * 130);
    }
    ctx.lineTo(vw, vh);
    ctx.closePath();
    ctx.fill();

    /* 云 */
    for (const c of this.clouds) {
      ctx.fillStyle = 'rgba(255,255,255,' + (this.isNight ? 0.08 : 0.5) + ')';
      ctx.beginPath();
      ctx.ellipse(c.x - this.camX * 0.2, c.y, c.w, c.h, 0, 0, Core.TAU);
      ctx.fill();
    }
  },

  /* ---- 世界方块 ---- */
  drawWorld() {
    const { ctx } = this;
    const bx0 = Math.max(0, Math.floor(this.camX / Core.BLOCK) - 1);
    const bx1 = Math.min(World.W - 1, Math.ceil((this.camX + this.vw) / Core.BLOCK) + 1);
    const by0 = Math.max(0, Math.floor(this.camY / Core.BLOCK) - 1);
    const by1 = Math.min(World.H - 1, Math.ceil((this.camY + this.vh) / Core.BLOCK) + 1);
    for (let y = by0; y <= by1; y++) {
      for (let x = bx0; x <= bx1; x++) {
        const id = World.get(x, y);
        if (id === 0) continue;
        const sx = Math.round(x * Core.BLOCK - this.camX);
        const sy = Math.round(y * Core.BLOCK - this.camY);
        this.drawWorldTile(ctx, x, y, id, sx, sy);
      }
    }
  },

  /* 单个方块（轻量、确定性纹理） */
  drawWorldTile(ctx, wx, wy, id, sx, sy) {
    const t = Tiles[id];
    if (t.fluid) {
      ctx.fillStyle = 'rgba(64,116,205,0.55)';
      ctx.fillRect(sx, sy, Core.BLOCK, Core.BLOCK);
      const wave = Math.sin(this.t * 3 + wx * 1.7 + wy * 0.3) * 0.5 + 0.5;
      ctx.fillStyle = 'rgba(255,255,255,' + (0.12 + wave * 0.15) + ')';
      ctx.fillRect(sx + 3, sy + 6, 10, 2);
      return;
    }
    ctx.fillStyle = t.color;
    ctx.fillRect(sx, sy, Core.BLOCK, Core.BLOCK);
    const r = Core.hash2(wx * 131 + 7, wy * 263 + 13);
    switch (id) {
      case 1: /* 草 */
        ctx.fillStyle = '#3e8f3e';
        ctx.fillRect(sx, sy, Core.BLOCK, 4);
        ctx.fillStyle = 'rgba(0,0,0,0.13)';
        ctx.fillRect(sx + 3, sy + 8, 4, 3);
        break;
      case 5: /* 原木 */
        ctx.fillStyle = 'rgba(0,0,0,0.22)';
        ctx.fillRect(sx + 6, sy, 4, Core.BLOCK);
        break;
      case 6: /* 树叶 */
        ctx.fillStyle = r > 0.5 ? '#357a35' : 'rgba(0,0,0,0.18)';
        ctx.fillRect(sx + Math.floor(r * 9), sy + Math.floor(Core.hash2(wx, wy * 3) * 9), 3, 3);
        break;
      case 8: /* 工作台 */
        ctx.fillStyle = 'rgba(0,0,0,0.2)';
        ctx.fillRect(sx, sy + 10, Core.BLOCK, 3);
        ctx.fillStyle = 'rgba(255,255,255,0.15)';
        ctx.fillRect(sx, sy + 2, Core.BLOCK, 2);
        break;
      case 9: /* 熔炉 */
        ctx.fillStyle = '#3a3a3a';
        ctx.fillRect(sx + 3, sy + 7, 10, 6);
        const flick = Math.floor(this.t * 8 + wx * 3) % 2 === 0;
        ctx.fillStyle = flick ? '#ff8830' : '#ff6020';
        ctx.fillRect(sx + 5, sy + 8, 6, 3);
        break;
      case 10: /* 火把 */
        ctx.fillStyle = '#8a6a3a';
        ctx.fillRect(sx + 7, sy + 6, 2, 10);
        const ff = Math.floor(this.t * 10 + wy * 5) % 2;
        ctx.fillStyle = '#ffb840';
        ctx.fillRect(sx + 5, sy + 1, 6, ff ? 5 : 4);
        ctx.fillStyle = '#ffe080';
        ctx.fillRect(sx + 6, sy, 4, 3);
        break;
      case 16: /* 石砖：横竖砖缝 */
        ctx.fillStyle = 'rgba(0,0,0,0.18)';
        ctx.fillRect(sx, sy + 7, Core.BLOCK, 2);
        ctx.fillRect(sx + 7, sy, 2, Core.BLOCK);
        break;
      case 17: /* 玻璃：半透明 + 高光 */
        ctx.fillStyle = 'rgba(255,255,255,0.45)';
        ctx.fillRect(sx + 2, sy + 2, Core.BLOCK - 4, 2);
        ctx.fillStyle = 'rgba(255,255,255,0.18)';
        ctx.fillRect(sx + 2, sy + 4, 2, Core.BLOCK - 6);
        break;
      case 18: /* 树苗 */
        ctx.fillStyle = '#7a5230';
        ctx.fillRect(sx + 7, sy + 10, 2, 6);
        ctx.fillStyle = '#4aa84a';
        ctx.fillRect(sx + 5, sy + 4, 6, 6);
        ctx.fillStyle = '#3e8f3e';
        ctx.fillRect(sx + 4, sy + 2, 8, 3);
        break;
      default:
        if (t.dot) {
          ctx.fillStyle = t.dot;
          const n = id === 11 ? 3 : 2;
          for (let i = 0; i < n; i++) {
            const h = Core.hash2(wx * 17 + i * 5, wy * 31 + i * 7);
            ctx.fillRect(sx + 2 + Math.floor(h * 9), sy + 2 + Math.floor(Core.hash2(wx + i, wy + i * 3) * 9), 3, 3);
          }
        }
    }
  },

  /* ---- 夜晚光晕 ---- */
  drawLighting() {
    const l = this.lightLevel;
    if (l >= 0.97) return;
    const { ctx, vw, vh } = this;
    const alpha = (1 - l) * 0.72;
    ctx.fillStyle = 'rgba(5,9,38,' + alpha + ')';
    ctx.fillRect(0, 0, vw, vh);

    ctx.globalCompositeOperation = 'destination-out';
    /* 方块光源 */
    const bx0 = Math.max(0, Math.floor(this.camX / Core.BLOCK));
    const bx1 = Math.min(World.W - 1, Math.ceil((this.camX + vw) / Core.BLOCK));
    const by0 = Math.max(0, Math.floor(this.camY / Core.BLOCK));
    const by1 = Math.min(World.H - 1, Math.ceil((this.camY + vh) / Core.BLOCK));
    for (let y = by0; y <= by1; y++) {
      for (let x = bx0; x <= bx1; x++) {
        const id = World.get(x, y);
        const t = Tiles[id];
        if (!t || !t.light) continue;
        const sx = x * Core.BLOCK - this.camX + 8;
        const sy = y * Core.BLOCK - this.camY + 8;
        const rad = (2.2 + t.light * 4.6) * Core.BLOCK;
        const g = ctx.createRadialGradient(sx, sy, 2, sx, sy, rad);
        g.addColorStop(0, 'rgba(0,0,0,0.9)');
        g.addColorStop(0.5, 'rgba(0,0,0,0.45)');
        g.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = g;
        ctx.beginPath(); ctx.arc(sx, sy, rad, 0, Core.TAU); ctx.fill();
      }
    }
    /* 玩家小光环 */
    const px = Player.cx - this.camX, py = Player.cy - this.camY;
    const pg = ctx.createRadialGradient(px, py, 2, px, py, 3.2 * Core.BLOCK);
    pg.addColorStop(0, 'rgba(0,0,0,0.7)');
    pg.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = pg;
    ctx.beginPath(); ctx.arc(px, py, 3.2 * Core.BLOCK, 0, Core.TAU); ctx.fill();
    ctx.globalCompositeOperation = 'source-over';
  },

  /* ---- 实体渲染 ---- */
  drawDrops() {
    const { ctx } = this;
    for (const d of Entities.drops) {
      if (d.timer < 30 && Math.floor(d.timer * 6) % 2 === 0) continue;
      UI.drawItemIcon(ctx, d.id, Math.round(d.x - this.camX), Math.round(d.y - this.camY), 12);
    }
  },

  drawEnemies() {
    const { ctx, vw, vh } = this;
    for (const e of Entities.list) {
      const sx = Math.round(e.x - this.camX);
      const sy = Math.round(e.y - this.camY);
      if (sx < -60 || sx > vw + 60 || sy < -60 || sy > vh + 60) continue;
      if (e.type === 'slime') {
        const sq = Math.abs(Math.sin(this.t * 7 + e.x * 0.01));
        const h = 12 - sq * 4;
        ctx.fillStyle = e.color;
        ctx.fillRect(sx, sy + (12 - h), 20, h);
        ctx.fillStyle = 'rgba(255,255,255,0.3)';
        ctx.fillRect(sx + 3, sy + (12 - h) + 2, 6, 2);
        ctx.fillStyle = '#1a2a1a';
        ctx.fillRect(sx + 5, sy + (12 - h) + 5, 3, 3);
        ctx.fillRect(sx + 13, sy + (12 - h) + 5, 3, 3);
      } else if (e.type === 'bat') {
        const flap = Math.sin(this.t * 22 + e.x * 0.05);
        const wing = Math.abs(flap) * 6 + 2;
        ctx.fillStyle = '#8a6ac8';
        ctx.fillRect(sx - wing, sy + 1, wing, 3);            // 左翼
        ctx.fillRect(sx + e.w, sy + 1, wing, 3);             // 右翼
        ctx.fillStyle = e.color;
        ctx.fillRect(sx, sy, e.w, e.h);                       // 身体
        ctx.fillStyle = '#ff5a5a';
        ctx.fillRect(sx + 2, sy + 2, 2, 2);
        ctx.fillRect(sx + 8, sy + 2, 2, 2);
      } else { /* zombie */
        const dir = Player.cx >= e.x + e.w / 2 ? 1 : -1;
        ctx.fillStyle = e.color;
        ctx.fillRect(sx, sy + 6, 12, 18);                 // 身体
        ctx.fillRect(sx + (dir > 0 ? 10 : -4), sy + 9, 6, 4);  // 手臂（朝向玩家）
        ctx.fillStyle = '#4a7a4a';
        ctx.fillRect(sx + 2, sy, 8, 8);                   // 头
        ctx.fillStyle = '#ff3a3a';
        ctx.fillRect(sx + (dir > 0 ? 6 : 3), sy + 3, 2, 2);
        ctx.fillRect(sx + (dir > 0 ? 9 : 6), sy + 3, 2, 2);
        ctx.fillStyle = 'rgba(0,0,0,0.3)';
        ctx.fillRect(sx + 2, sy + 8, 8, 2);               // 眼窝阴影
      }
      /* 血条 */
      if (e.hp < e.maxHp) {
        ctx.fillStyle = 'rgba(0,0,0,0.6)';
        ctx.fillRect(sx, sy - 6, e.w, 3);
        ctx.fillStyle = '#e84040';
        ctx.fillRect(sx, sy - 6, e.w * (e.hp / e.maxHp), 3);
      }
    }
  },

  drawCrack() {
    const m = Player.mining;
    if (!m) return;
    const { ctx } = this;
    const sx = m.bx * Core.BLOCK - this.camX;
    const sy = m.by * Core.BLOCK - this.camY;
    if (sx < -16 || sx > this.vw || sy < -16 || sy > this.vh) return;
    ctx.fillStyle = 'rgba(255,255,255,' + (0.12 + 0.3 * m.progress) + ')';
    ctx.fillRect(sx, sy, Core.BLOCK, Core.BLOCK);
    ctx.strokeStyle = 'rgba(15,15,15,0.85)';
    ctx.lineWidth = 1.6;
    const n = 1 + Math.floor(m.progress * 4);
    for (let i = 0; i < n; i++) {
      const h1 = Core.hash2(m.bx * 31 + i * 7, m.by * 47 + i * 11);
      const h2 = Core.hash2(m.bx * 13 + i * 3, m.by * 29 + i * 5);
      ctx.beginPath();
      ctx.moveTo(sx + 3 + h1 * 10, sy + 3 + h1 * 8);
      ctx.lineTo(sx + 4 + h2 * 10, sy + 4 + h2 * 8);
      ctx.stroke();
    }
  },

  drawPlayer() {
    const p = Player;
    const { ctx } = this;
    const sx = Math.round(p.x - this.camX);
    const sy = Math.round(p.y - this.camY);
    const walk = p.onGround && Math.abs(p.vx) > 10;
    const bob = walk ? Math.sin(this.t * 13) * 2 : 0;
    /* 腿 */
    ctx.fillStyle = '#2b3a66';
    ctx.fillRect(sx + 1, sy + 18 + Math.max(0, bob), 4, 8 - Math.max(0, bob));
    ctx.fillRect(sx + 5, sy + 18 + Math.max(0, -bob), 4, 8 - Math.max(0, -bob));
    /* 身体 */
    ctx.fillStyle = p.inWater ? '#2e5ca8' : '#3a6ec8';
    ctx.fillRect(sx, sy + 8, 10, 11);
    /* 头 */
    ctx.fillStyle = '#e8b98a';
    ctx.fillRect(sx + 1, sy, 8, 8);
    /* 头发 */
    ctx.fillStyle = '#5a3a1a';
    ctx.fillRect(sx + 1, sy, 8, 2);
    /* 眼睛 */
    ctx.fillStyle = '#1a1a1a';
    const ex = p.dir > 0 ? sx + 7 : sx + 2;
    ctx.fillRect(ex, sy + 3, 2, 2);
    /* 受击闪白 */
    if (p.hurtFlash > 0 && Math.floor(p.hurtFlash * 12) % 2 === 0) {
      ctx.fillStyle = 'rgba(255,255,255,0.65)';
      ctx.fillRect(sx - 1, sy - 1, 12, 28);
    }
    /* 挥剑弧光 */
    if (p.attackAnim > 0) {
      const prog = 1 - p.attackAnim / 0.25;
      ctx.strokeStyle = 'rgba(255,255,255,0.85)';
      ctx.lineWidth = 3;
      ctx.beginPath();
      const base = p.dir > 0 ? -1.35 : Math.PI + 0.35;
      ctx.arc(sx + 5, sy + 12, 15, base, base + p.dir * prog * 1.7);
      ctx.stroke();
    }
  },

  drawParticles() {
    const { ctx } = this;
    for (const p of Entities.parts) {
      ctx.globalAlpha = Math.min(1, p.life * 2.5);
      ctx.fillStyle = p.color;
      ctx.fillRect(Math.round(p.x - this.camX), Math.round(p.y - this.camY), p.size, p.size);
    }
    ctx.globalAlpha = 1;
    for (const t of Entities.dmgTxt) {
      ctx.globalAlpha = Math.min(1, t.life * 2);
      ctx.fillStyle = t.color;
      ctx.font = 'bold 13px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(t.text, t.x - this.camX, t.y - this.camY);
    }
    ctx.globalAlpha = 1;
  },

  drawMsg() {
    const { ctx, vw, vh } = this;
    ctx.fillStyle = 'rgba(8,12,28,0.82)';
    ctx.fillRect(vw / 2 - 280, vh - 96, 560, 30);
    ctx.strokeStyle = 'rgba(255,215,94,0.55)';
    ctx.lineWidth = 1;
    ctx.strokeRect(vw / 2 - 280, vh - 96, 560, 30);
    ctx.fillStyle = '#ffd75e';
    ctx.font = '13px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(this.msg, vw / 2, vh - 81);
  },

  /* ================= 主循环 ================= */
  loop(ts) {
    requestAnimationFrame(t => this.loop(t));
    let dt = (ts - this.lastT) / 1000;
    this.lastT = ts;
    if (!(dt > 0)) dt = 0.016;
    if (dt > 0.06) dt = 0.06;
    this.update(dt);
    this.draw();
  }
};
