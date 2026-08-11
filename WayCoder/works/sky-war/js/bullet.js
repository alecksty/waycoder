/* ============================================================
   bullet.js — 子弹实体（玩家 / 敌机 / 激光）
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class Bullet {
    /**
     * @param {number} x,y     出生位置
     * @param {number} angle   方向角（弧度）
     * @param {number} speed   速度
     * @param {number} r       半径
     * @param {number} damage  伤害
     * @param {string} owner   'player' | 'enemy'
     * @param {string} kind    外观：'bolt'(玩家) | 'orb'(敌机) | 'laser'(Boss)
     */
    constructor(x, y, angle, speed, r, damage, owner, kind = 'orb') {
      this.x = x; this.y = y;
      this.vx = Math.cos(angle) * speed;
      this.vy = Math.sin(angle) * speed;
      this.r = r;
      this.damage = damage;
      this.owner = owner;
      this.kind = kind;
      this.life = owner === 'player'
        ? CFG.PLAYER_BULLET.life
        : CFG.ENEMY_BULLET.life;
      this.dead = false;
    }

    update(dt) {
      this.x += this.vx * dt;
      this.y += this.vy * dt;
      this.life -= dt;
      if (this.life <= 0) this.dead = true;
      // 出界即死
      if (this.x < -20 || this.x > CFG.LOGIC_W + 20 ||
          this.y < -20 || this.y > CFG.LOGIC_H + 20) this.dead = true;
    }

    draw(ctx) {
      if (this.owner === 'player') this._drawPlayer(ctx);
      else if (this.kind === 'laser') this._drawLaser(ctx);
      else this._drawOrb(ctx);
    }

    /* 玩家子弹：青色光弹 */
    _drawPlayer(ctx) {
      U.glowCircle(ctx, this.x, this.y, 3.5, '#c8f4ff', 'rgba(60,220,255,0.55)', 12);
      ctx.fillStyle = '#eaffff';
      ctx.fillRect(this.x - 1.5, this.y - 5, 3, 10);
    }

    /* 敌机子弹：红色能量球 */
    _drawOrb(ctx) {
      U.glowCircle(ctx, this.x, this.y, 4, '#ff8a7a', 'rgba(255,80,60,0.5)', 13);
      ctx.fillStyle = '#ffd0c8';
      ctx.beginPath();
      ctx.arc(this.x, this.y, 2, 0, Math.PI * 2);
      ctx.fill();
    }

    /* Boss 激光：细长红色光束 */
    _drawLaser(ctx) {
      const a = Math.atan2(this.vy, this.vx);
      const len = 26;
      ctx.save();
      ctx.translate(this.x, this.y);
      ctx.rotate(a);
      U.radial(ctx, 0, 0, 0, 12, 'rgba(255,60,40,0.4)', 'rgba(255,60,40,0)');
      ctx.fillStyle = '#ffb0a0';
      ctx.fillRect(-len, -1.5, len * 2, 3);
      ctx.restore();
    }
  }

  /* ---------- 子弹池管理 ---------- */
  class BulletManager {
    constructor() {
      this.list = [];
    }
    spawnPlayer(x, y, angle = -Math.PI / 2, speed = CFG.PLAYER_BULLET.speed) {
      this.list.push(new Bullet(x, y, angle, speed, CFG.PLAYER_BULLET.r,
        CFG.PLAYER_BULLET.damage, 'player', 'bolt'));
    }
    spawnEnemy(x, y, angle, speed = CFG.ENEMY_BULLET.speed, kind = 'orb') {
      this.list.push(new Bullet(x, y, angle, speed, CFG.ENEMY_BULLET.r,
        CFG.ENEMY_BULLET.damage, 'enemy', kind));
    }
    update(dt) {
      for (let i = this.list.length - 1; i >= 0; i--) {
        const b = this.list[i];
        b.update(dt);
        if (b.dead) this.list.splice(i, 1);
      }
    }
    draw(ctx) {
      for (const b of this.list) b.draw(ctx);
    }
    clear() { this.list.length = 0; }
    get size() { return this.list.length; }
  }

  G.Bullet = Bullet;
  G.BulletManager = BulletManager;

})(window.Game = window.Game || {});
