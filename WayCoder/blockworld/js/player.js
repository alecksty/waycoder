/*
 * player.js —— 玩家：物理移动 / 挖掘 / 放置 / 近战攻击 / 受击
 * 像素坐标系（1 块 = Core.BLOCK px）。碰撞与重力由 World.moveEntity 处理
 */
'use strict';

const Player = {
  /* 物理 */
  x: 0, y: 0,          // 左上角（像素）
  w: 10, h: 26,        // 碰撞箱
  vx: 0, vy: 0,
  grav: 780, maxFall: 40,
  onGround: false,
  dir: 1,              // 朝向 1/-1
  inWater: false,

  /* 属性 */
  hp: 20, maxHp: 20,
  invuln: 0,           // 受击无敌时间
  attackCd: 0,         // 攻击冷却

  /* 交互 */
  mining: null,        // 正在挖掘 {bx, by, progress} | null
  attackAnim: 0,       // 挥剑动画计时
  hurtFlash: 0,

  speed: 150, jumpV: -290,
  reach: 6.5,          // 交互距离（块）

  /* 原地重置（保留背包由 Game 决定） */
  reset(x, y) {
    this.x = x; this.y = y;
    this.vx = 0; this.vy = 0;
    this.hp = this.maxHp;
    this.invuln = 0; this.attackCd = 0;
    this.mining = null; this.attackAnim = 0;
    this.onGround = false;
  },

  get cx() { return this.x + this.w / 2; },
  get cy() { return this.y + this.h / 2; },

  /* ---------- 更新 ---------- */
  update(dt, input) {
    /* 水平移动 */
    let move = 0;
    if (input.left) move -= 1;
    if (input.right) move += 1;
    if (move !== 0) { this.vx = move * this.speed; this.dir = move; }
    else this.vx *= Math.pow(0.0001, dt);   // 快速衰减

    /* 跳跃（地面 / 水中） */
    if (input.jump && (this.onGround || this.inWater)) {
      this.vy = this.jumpV * (this.inWater ? 0.7 : 1);
      input.jump = false;
      AudioSys.sfx('jump');
    }

    /* 水中减速 + 上浮 */
    this.inWater = World.rectInWater(this);
    if (this.inWater) {
      this.vx *= 0.7;
      if (this.vy > 10) this.vy = 10;
      if (input.up) this.vy = -60;
    }

    World.moveEntity(this, dt);

    /* 受击/攻击冷却计时 */
    if (this.invuln > 0) this.invuln -= dt;
    if (this.attackCd > 0) this.attackCd -= dt;
    if (this.attackAnim > 0) this.attackAnim -= dt;
    if (this.hurtFlash > 0) this.hurtFlash -= dt;

    /* 挖掘持续 */
    if (this.mining) {
      const { bx, by, progress } = this.mining;
      const t = Tiles[World.get(bx, by)];
      if (!t || !t.solid) { this.mining = null; }
      else {
        const power = this.minePower();
        this.mining.progress += power * dt / t.hard;
        if (this.mining.progress >= 1) {
          const tx = bx * Core.BLOCK + 8, ty = by * Core.BLOCK + 8;
          const broken = World.breakBlock(bx, by);
          if (broken > 0) {
            let drop = Tiles[broken].drop || broken;
            if (broken === 6) {                       // 树叶：树苗/苹果/树叶
              const roll = Math.random();
              if (roll < 0.5) drop = 18;
              else if (roll < 0.58) drop = 150;
            }
            Entities.dropItem(tx, ty, drop, 1);
            Entities.burst(tx, ty, Tiles[broken].color, 6);
            AudioSys.sfx('dig');
          }
          this.mining = null;
        }
      }
    }

    /* 防止掉出世界底部 */
    if (this.y > World.H * Core.BLOCK) this.hurt(10);
  },

  /* 挖掘力：手持镐则用镐力，否则徒手 */
  minePower() {
    const sel = Inv.selected();
    if (sel) {
      const def = Items.get(sel.id);
      if (def && def.type === 'pick') return def.power;
    }
    return 1.0;
  },

  /* ---------- 交互 ---------- */
  /* 左键：优先攻击鼠标附近的敌人，否则开始挖掘 */
  interactPrimary(mx, my) {
    // 攻击有冷却；冷却中继续挖掘
    if (this.attackCd <= 0) {
      const hit = Entities.hitEnemiesAt(mx, my, 1.2 * Core.BLOCK, this.attackDamage(), this);
      if (hit > 0) {
        this.attackCd = 0.4;
        this.attackAnim = 0.25;
        AudioSys.sfx('swing');
        return;
      }
    }
    this.startMine(mx, my);
  },

  /* 开始挖掘鼠标指向的块 */
  startMine(mx, my) {
    const bx = Math.floor(mx / Core.BLOCK);
    const by = Math.floor(my / Core.BLOCK);
    if (!World.inBounds(bx, by)) return;
    const t = Tiles[World.get(bx, by)];
    if (!t || !t.solid) { this.mining = null; return; }
    if (Core.dist(this.cx, this.cy, bx * Core.BLOCK + 8, by * Core.BLOCK + 8) > this.reach * Core.BLOCK) {
      this.mining = null;
      return;
    }
    // 换目标则重置进度
    if (!this.mining || this.mining.bx !== bx || this.mining.by !== by) {
      this.mining = { bx, by, progress: 0 };
    }
  },

  /* 右键放置选中方块 */
  placeBlock(mx, my) {
    const sel = Inv.selected();
    if (!sel) return;
    const def = Items.get(sel.id);
    if (!def || def.type !== 'block') return;
    const bx = Math.floor(mx / Core.BLOCK);
    const by = Math.floor(my / Core.BLOCK);
    if (!World.inBounds(bx, by)) return;
    if (World.get(bx, by) !== 0) return;                       // 目标非空
    if (Core.dist(this.cx, this.cy, bx * Core.BLOCK + 8, by * Core.BLOCK + 8) > this.reach * Core.BLOCK) return;
    // 不能放在玩家身上
    const rect = { x: bx * Core.BLOCK, y: by * Core.BLOCK, w: Core.BLOCK, h: Core.BLOCK };
    if (Core.rectsOverlap(rect, this)) return;
    World.set(bx, by, def.tileId);
    if (def.tileId === 18) World.plantSapling(bx, by);   // 树苗登记生长
    Inv.removeSlot(Game.hotbarSel, 1);
    AudioSys.sfx('place');
    // 放置后若在挖同一格则取消
    if (this.mining && this.mining.bx === bx && this.mining.by === by) this.mining = null;
  },

  /* ---------- 受击 ---------- */
  hurt(dmg) {
    if (this.invuln > 0 || this.hp <= 0) return;
    this.hp -= dmg;
    this.invuln = 0.8;
    this.hurtFlash = 0.3;
    AudioSys.sfx('hurt');
    if (this.hp <= 0) {
      this.hp = 0;
      Game.onPlayerDeath();
    }
  },

  heal(n) {
    this.hp = Math.min(this.maxHp, this.hp + n);
  },

  /* 近战伤害：手持剑取剑伤，否则徒手 2 */
  attackDamage() {
    const sel = Inv.selected();
    if (sel) {
      const def = Items.get(sel.id);
      if (def && def.type === 'sword') return def.dmg;
    }
    return 2;
  }
};
