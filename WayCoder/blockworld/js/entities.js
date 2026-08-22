/*
 * entities.js —— 实体管理：敌人 / 掉落物 / 粒子 / 伤害数字
 * Enemy:   史莱姆（跳）与僵尸（走）两种，AI + 碰撞 + 掉落
 * drops:   物品掉落物（磁吸拾取）
 * parts:   方块碎屑粒子
 * dmgTxt:  伤害飘字
 */
'use strict';

const Entities = {
  list: [],     // 敌人
  drops: [],    // 掉落物
  parts: [],    // 粒子
  dmgTxt: [],   // 伤害文字
  maxEnemies: 24,

  reset() { this.list = []; this.drops = []; this.parts = []; this.dmgTxt = []; },

  /* ---------- 敌人生成 ---------- */
  spawnSlime(x, y) {
    this.list.push({
      type: 'slime', x: x - 10, y: y - 12, w: 20, h: 12,
      vx: 0, vy: 0, grav: 700, maxFall: 30,
      hp: 8, maxHp: 8, dmg: 2, speed: 55, jumpV: -230,
      timer: 1 + Math.random(), color: '#7ec850'
    });
  },

  spawnZombie(x, y) {
    this.list.push({
      type: 'zombie', x: x - 6, y: y - 24, w: 12, h: 24,
      vx: 0, vy: 0, grav: 750, maxFall: 30,
      hp: 16, maxHp: 16, dmg: 3, speed: 34, jumpV: -260,
      timer: 0, color: '#5a8f5a'
    });
  },

  /* 蝙蝠：夜间飞行敌，重力 0，波浪式俯冲 */
  spawnBat(x, y) {
    this.list.push({
      type: 'bat', x: x - 6, y: y - 4, w: 12, h: 8,
      vx: 0, vy: 0, grav: 0, maxFall: 220,
      hp: 6, maxHp: 6, dmg: 2, speed: 95,
      flap: Math.random() * 5, facing: 1, retreat: 0,
      color: '#6a4aa8'
    });
  },

  /* 在世界某处（地表/洞穴）找一个可行走的生成点 */
  findSpawnSpot(px, py, minDist, maxDist, rng) {
    for (let i = 0; i < 20; i++) {
      const ang = rng() * Core.TAU;
      const d = rng.range(minDist, maxDist);
      const tx = Core.clamp(Math.round(px / Core.BLOCK + Math.cos(ang) * d), 3, World.W - 3);
      const sy = World.surfaceAt(tx);
      if (sy >= World.SEA) continue;                      // 水面/水下沙滩不刷
      const groundY = sy * Core.BLOCK;
      if (Math.abs(groundY - py) > 12 * Core.BLOCK) continue;
      return { x: tx * Core.BLOCK + 8, y: groundY };
    }
    return null;
  },

  /* 每帧生成逻辑（由 Game 调用） */
  spawnUpdate(dt) {
    if (this.list.length >= this.maxEnemies) return;
    const night = Game.isNight;
    const rng = Core.makeRng((Math.random() * 1e9) | 0);
    const spot = this.findSpawnSpot(Player.cx, Player.cy, 26 * Core.BLOCK, 46 * Core.BLOCK, rng);
    if (!spot) return;
    if (night) {
      if (rng.chance(0.45)) this.spawnBat(spot.x, spot.y - 8 * Core.BLOCK);
      else this.spawnZombie(spot.x, spot.y - 0.01);
    } else this.spawnSlime(spot.x, spot.y - 0.01);
  },

  /* ---------- 敌人 AI ---------- */
  updateEnemies(dt) {
    for (let i = this.list.length - 1; i >= 0; i--) {
      const e = this.list[i];
      /* 应用击退 */
      if (e.knock) { e.vx = e.knock.x; e.vy = e.knock.y; e.knock = null; }
      /* 面向玩家 */
      const dx = Player.cx - e.x;
      const dy = Player.cy - e.y;
      const dist = Math.hypot(dx, dy);
      const dir = Math.sign(dx) || 1;

      if (e.type === 'slime') {
        e.timer -= dt;
        if (e.timer <= 0 && e.onGround && dist < 12 * Core.BLOCK) {
          e.vx = dir * e.speed * 0.7;
          e.vy = e.jumpV;
          e.timer = 1.4 + Math.random() * 1.2;
        }
        if (e.onGround) e.vx *= Math.pow(0.02, dt);
      } else if (e.type === 'zombie') {
        if (dist < 22 * Core.BLOCK && !Game.isDay || dist < 8 * Core.BLOCK) {
          e.vx = dir * e.speed;
        } else {
          e.vx *= Math.pow(0.02, dt);
        }
        /* 撞墙跳跃 */
        if (e.hitWall && e.onGround) e.vy = e.jumpV;
      } else { // bat：无重力波浪飞行，朝玩家俯冲
        e.flap += dt;
        if (e.retreat > 0) {
          e.retreat -= dt;
          e.vx = -e.facing * e.speed * 0.6;
          e.vy = Math.sin(e.flap * 5) * 50 - 20;
        } else if (dist < 26 * Core.BLOCK) {
          e.facing = dir;
          e.vx = dir * e.speed;
          e.vy = Math.sin(e.flap * 6) * 70 - 30;
        } else {
          e.vx *= Math.pow(0.02, dt);
          e.vy = Math.sin(e.flap * 3) * 40;
        }
      }

      World.moveEntity(e, dt);

      /* 蝙蝠撞墙短暂后退，避免卡墙 */
      if (e.type === 'bat' && e.hitWall) e.retreat = 0.6;
      if (e.type === 'bat') e.y = Core.clamp(e.y, 4, World.H * Core.BLOCK - 30);

      /* 白天僵尸燃烧（贴地冒烟，只扣血） */
      if ((e.type === 'zombie' || e.type === 'bat') && Game.isDay && e.y < World.SEA * Core.BLOCK - 8) {
        e.burnT = (e.burnT || 0) + dt;
        if (e.burnT > 1) { e.burnT = 0; e.hp -= 1; this.burst(e.x + e.w / 2, e.y, '#ff8844', 2); }
      }

      /* 碰到玩家造成伤害 */
      if (Core.rectsOverlap(e, Player) && Player.invuln <= 0) {
        Player.hurt(e.dmg);
      }

      if (e.hp <= 0) this.killEnemy(i, e);
    }
  },

  killEnemy(i, e) {
    this.list.splice(i, 1);
    Game.kills++;
    AudioSys.sfx('kill');
    this.burst(e.x + e.w / 2, e.y + e.h / 2, e.color, 8);
    /* 掉落：僵尸掉铁锭概率高，史莱姆掉煤 */
    const roll = Math.random();
    let drop = 130;
    if (e.type === 'zombie') drop = roll < 0.5 ? 141 : (roll < 0.8 ? 130 : 133);
    else drop = roll < 0.5 ? 130 : (roll < 0.75 ? 120 : 141);
    this.dropItem(e.x + e.w / 2, e.y + e.h / 2, drop, 1);
    this.dmgTxt.push({ x: e.x + e.w / 2, y: e.y, text: '击杀 +1', life: 1.2, color: '#ffd75e' });
  },

  /* 攻击：命中以 (mx,my) 为中心 range 内所有敌人，返回命中数 */
  hitEnemiesAt(mx, my, range, dmg, attacker) {
    let hits = 0;
    for (let i = this.list.length - 1; i >= 0; i--) {
      const e = this.list[i];
      const ex = e.x + e.w / 2, ey = e.y + e.h / 2;
      if (Math.hypot(ex - mx, ey - my) <= range + Math.max(e.w, e.h) / 2) {
        e.hp -= dmg;
        e.knock = { x: Math.sign(ex - attacker.cx) * 120, y: -60 };
        hits++;
        this.dmgTxt.push({ x: ex, y: ey, text: '-' + dmg, life: 0.9, color: '#ff6a6a' });
        this.burst(ex, ey, '#ffffff', 4);
        if (e.hp <= 0) this.killEnemy(i, e);
      }
    }
    return hits;
  },

  /* ---------- 掉落物 ---------- */
  dropItem(x, y, id, count) {
    this.drops.push({
      x: x - 6, y: y - 6, w: 12, h: 12,
      vx: (Math.random() - 0.5) * 70, vy: -70,
      id, count, timer: 180, life: 180
    });
  },

  updateDrops(dt) {
    for (let i = this.drops.length - 1; i >= 0; i--) {
      const d = this.drops[i];
      d.vy = Math.min(d.vy + 600 * dt, 40);
      d.x += d.vx * dt;
      d.y += d.vy * dt;
      /* 落地弹跳 */
      const bx = Math.floor((d.x + 6) / Core.BLOCK);
      const by = Math.floor((d.y + 12) / Core.BLOCK);
      if (World.isSolid(bx, by)) { d.y = by * Core.BLOCK - 12; d.vy = 0; d.vx *= 0.8; }

      d.timer -= dt;

      /* 磁吸 + 拾取 */
      const pdx = Player.cx - (d.x + 6), pdy = Player.cy - (d.y + 6);
      const pd = Math.hypot(pdx, pdy);
      if (pd < 3 * Core.BLOCK) { d.x += pdx / pd * 260 * dt; d.y += pdy / pd * 260 * dt; }
      if (pd < 0.7 * Core.BLOCK) {
        const left = Inv.add(d.id, d.count);
        if (left === 0) {
          this.drops.splice(i, 1);
          AudioSys.sfx('pickup');
          continue;
        }
      }
      if (d.timer <= 0) this.drops.splice(i, 1);
    }
  },

  /* ---------- 粒子 ---------- */
  burst(x, y, color, n) {
    for (let i = 0; i < n; i++) {
      this.parts.push({
        x, y,
        vx: (Math.random() - 0.5) * 130,
        vy: -Math.random() * 120,
        life: 0.5 + Math.random() * 0.4,
        color, size: 2 + Math.random() * 3
      });
    }
  },

  updateParts(dt) {
    for (let i = this.parts.length - 1; i >= 0; i--) {
      const p = this.parts[i];
      p.life -= dt;
      p.vy += 400 * dt;
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      if (p.life <= 0) this.parts.splice(i, 1);
    }
    for (let i = this.dmgTxt.length - 1; i >= 0; i--) {
      const t = this.dmgTxt[i];
      t.life -= dt;
      t.y -= 34 * dt;
      if (t.life <= 0) this.dmgTxt.splice(i, 1);
    }
  },

  update(dt) {
    this.updateEnemies(dt);
    this.updateDrops(dt);
    this.updateParts(dt);
  }
};
