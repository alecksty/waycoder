/* ============================================================
   enemy.js — 敌机实体（scout / fighter / bomber / drone）
              + Boss 大型母舰
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  /* ===================== 普通敌机 ===================== */
  class Enemy {
    /**
     * @param {string} type    'scout' | 'fighter' | 'bomber' | 'drone'
     * @param {number} x,y     出生位置
     * @param {number} wave    当前波次（用于难度缩放）
     * @param {object} cb      { onShoot(x,y,angle), onDrop(x,y) }
     */
    constructor(type, x, y, wave, cb) {
      const def = CFG.ENEMIES[type];
      this.type = type;
      this.def = def;
      this.cb = cb || {};
      this.x = x;
      this.y = y;
      this.wave = wave;

      const sp = 1 + CFG.WAVE.speedScale * (wave - 1);
      const hp = 1 + CFG.WAVE.hpScale * (wave - 1);
      this.speed = def.speed * sp;
      this.maxHp = def.hp * hp;
      this.hp = this.maxHp;
      this.r = def.r;
      this.score = def.score;
      this.dead = false;      // 被击毁
      this.out = false;       // 出界
      this.removed = false;   // 已从列表移除

      // 摆动（fighter）
      this.swayAmp = def.sway;
      this.swayPhase = U.rand(0, Math.PI * 2);
      this.swaySpeed = U.rand(1.8, 3.2);
      this.baseX = x;

      // 射击
      this.fireTimer = U.rand(0.4, 1.2);
      this.fireInterval = 1 / Math.max(def.fire, 0.0001);

      // 入场抖动
      this.enterTimer = 0.35;
    }

    /* ---------- 更新 ---------- */
    update(dt, playerX, playerY) {
      if (this.enterTimer > 0) this.enterTimer -= dt;

      // 1) 垂直移动
      this.y += this.speed * dt;

      // 2) 水平移动
      if (this.swayAmp > 0) {
        this.swayPhase += this.swaySpeed * dt;
        this.x = this.baseX + Math.sin(this.swayPhase) * this.swayAmp * 40;
      }

      // 3) 射击逻辑
      if (this.def.fire > 0 && this.y > 20 && this.y < CFG.LOGIC_H * 0.7) {
        this.fireTimer -= dt;
        if (this.fireTimer <= 0) {
          this.fireTimer = this.fireInterval;
          // 概率发射（fire 字段即为概率权重）
          if (Math.random() < this.def.fire * 8) {
            const ang = U.angleTo(this.x, this.y, playerX, playerY);
            if (this.cb.onShoot) this.cb.onShoot(this.x, this.y + 10, ang);
          }
        }
      }

      // 4) 出界
      if (this.y > CFG.LOGIC_H + 40) this.out = true;
    }

    /* ---------- 受击 ---------- */
    hit(dmg) {
      this.hp -= dmg;
      if (this.hp <= 0) { this.dead = true; return true; }
      return false;
    }

    /* ---------- 绘制 ---------- */
    draw(ctx, time) {
      ctx.save();
      // 入场闪烁
      if (this.enterTimer > 0 && Math.floor(this.enterTimer * 20) % 2 === 0) {
        ctx.globalAlpha = 0.45;
      }
      switch (this.type) {
        case 'scout':  this._drawScout(ctx); break;
        case 'fighter':this._drawFighter(ctx); break;
        case 'bomber': this._drawBomber(ctx); break;
        case 'drone':  this._drawDrone(ctx); break;
      }
      ctx.restore();
    }

    /* 侦察机：红色小三角 */
    _drawScout(ctx) {
      const { x, y } = this;
      U.glowCircle(ctx, x, y + 8, 6, 'rgba(255,120,60,0.25)', 'rgba(255,90,40,0)', 14);
      ctx.fillStyle = '#e8553c';
      U.polygon(ctx, x, y, [[0, 14], [-9, -10], [0, -4], [9, -10]]);
      ctx.fill();
      ctx.fillStyle = '#7a1e12';
      U.polygon(ctx, x, y, [[0, 12], [-5, -6], [0, -1], [5, -6]]);
      ctx.fill();
    }

    /* 战斗机：橙色带翼 */
    _drawFighter(ctx) {
      const { x, y } = this;
      ctx.fillStyle = '#f0882c';
      U.polygon(ctx, x, y, [[0, 18], [-14, -8], [-8, -12], [0, -2], [8, -12], [14, -8]]);
      ctx.fill();
      ctx.fillStyle = '#c4621a';
      U.polygon(ctx, x, y, [[0, 16], [-8, -8], [0, -4], [8, -8]]);
      ctx.fill();
      // 引擎光
      U.glowCircle(ctx, x, y + 16, 3, '#ffd76a', 'rgba(255,180,60,0.5)', 10);
    }

    /* 轰炸机：紫色重型机 */
    _drawBomber(ctx) {
      const { x, y } = this;
      U.glowCircle(ctx, x, y + 16, 10, 'rgba(170,90,255,0.2)', 'rgba(150,60,255,0)', 22);
      ctx.fillStyle = '#9a5cc8';
      U.polygon(ctx, x, y, [[-26, 8], [-20, -16], [-6, -22], [6, -22], [20, -16], [26, 8], [18, 14], [-18, 14]]);
      ctx.fill();
      ctx.fillStyle = '#5e2f85';
      U.polygon(ctx, x, y, [[-16, 6], [-10, -14], [0, -18], [10, -14], [16, 6]]);
      ctx.fill();
      ctx.fillStyle = '#ffd76a';
      ctx.fillRect(x - 2, y + 6, 4, 4);
    }

    /* 无人机：青色小菱形 */
    _drawDrone(ctx) {
      const { x, y } = this;
      U.glowCircle(ctx, x, y, 7, 'rgba(90,220,255,0.25)', 'rgba(60,190,255,0)', 14);
      ctx.fillStyle = '#35b8d8';
      U.polygon(ctx, x, y, [[0, 12], [-9, 0], [0, -12], [9, 0]]);
      ctx.fill();
      ctx.fillStyle = '#0f5a70';
      U.polygon(ctx, x, y, [[0, 6], [-4, 0], [0, -6], [4, 0]]);
      ctx.fill();
    }
  }

  /* ===================== Boss ===================== */
  class Boss {
    constructor(wave, cb) {
      const B = CFG.BOSS;
      this.cb = cb || {};
      this.x = CFG.LOGIC_W / 2;
      this.y = -80;
      this.wave = wave;
      const hpScale = 1 + CFG.WAVE.hpScale * (wave - 1);
      this.maxHp = B.hp * hpScale;
      this.hp = this.maxHp;
      this.r = B.r;
      this.speed = B.speed * (1 + CFG.WAVE.speedScale * (wave - 1) * 0.5);
      this.score = B.score;
      this.enterY = 118;
      this.state = 'enter';   // enter -> active
      this.dead = false;
      this.removed = false;

      this.dir = 1;
      this.barrageTimer = 1.2;
      this.aimedTimer = 1.8;
      this.moveTimer = 0;
      this.phase = 0;         // 血量阶段（控制弹幕强度）
      this.flash = 0;
      this.hitFlash = 0;
    }

    update(dt, playerX, playerY) {
      // 受击闪烁
      if (this.hitFlash > 0) this.hitFlash -= dt;

      if (this.state === 'enter') {
        this.y += this.speed * 0.7 * dt;
        if (this.y >= this.enterY) { this.y = this.enterY; this.state = 'active'; }
        return;
      }

      // ---- active ----
      this.phase = 1 - this.hp / this.maxHp;   // 0~1

      // 水平往返移动
      this.moveTimer -= dt;
      if (this.moveTimer <= 0) {
        this.dir = -this.dir;
        this.moveTimer = U.rand(1.4, 2.6);
      }
      this.x += this.dir * this.speed * 0.8 * dt;
      this.x = U.clamp(this.x, 70, CFG.LOGIC_W - 70);

      // 轻微上下浮动
      this.y = this.enterY + Math.sin(this.phase * Math.PI * 3 + performance.now() * 0.001) * 6;

      // 扇形弹幕
      this.barrageTimer -= dt;
      if (this.barrageTimer <= 0) {
        this.barrageTimer = Math.max(0.9, CFG.BOSS.barrageInterval - this.phase * 0.7);
        const base = Math.PI / 2;   // 向下
        const count = CFG.BOSS.barrageCount + Math.floor(this.phase * 5);
        const spread = CFG.BOSS.barrageSpread;
        for (let i = 0; i < count; i++) {
          const t = count === 1 ? 0.5 : i / (count - 1);
          const ang = base + (t - 0.5) * spread;
          if (this.cb.onShoot) this.cb.onShoot(this.x, this.y + 30, ang, 'laser');
        }
        if (this.cb.onSound) this.cb.onSound('bossBarrage');
      }

      // 自机狙
      this.aimedTimer -= dt;
      if (this.aimedTimer <= 0) {
        this.aimedTimer = Math.max(0.5, CFG.BOSS.aimedInterval - this.phase * 0.35);
        const ang = U.angleTo(this.x, this.y + 20, playerX, playerY);
        if (this.cb.onShoot) this.cb.onShoot(this.x, this.y + 26, ang, 'orb');
        if (this.cb.onShoot) this.cb.onShoot(this.x, this.y + 26, ang + 0.22, 'orb');
        if (this.cb.onShoot) this.cb.onShoot(this.x, this.y + 26, ang - 0.22, 'orb');
      }
    }

    hit(dmg) {
      this.hp -= dmg;
      this.hitFlash = 0.08;
      if (this.hp <= 0) { this.dead = true; return true; }
      return false;
    }

    draw(ctx, time) {
      const { x, y } = this;
      ctx.save();
      // 母舰发光
      U.radial(ctx, x, y + 20, 0, 70, 'rgba(255,60,40,0.18)', 'rgba(255,60,40,0)');

      // 船体
      const flash = this.hitFlash > 0 ? '#ffffff' : '#a02a20';
      ctx.fillStyle = '#7c1f16';
      U.polygon(ctx, x, y, [[0, 48], [-34, 24], [-46, -14], [-20, -30], [20, -30], [46, -14], [34, 24]]);
      ctx.fill();

      ctx.fillStyle = flash;
      U.polygon(ctx, x, y, [[0, 42], [-28, 20], [-38, -10], [-14, -24], [14, -24], [38, -10], [28, 20]]);
      ctx.fill();

      // 驾驶舱
      ctx.fillStyle = '#ff8a6a';
      U.polygon(ctx, x, y, [[0, 30], [-8, 12], [0, 4], [8, 12]]);
      ctx.fill();
      ctx.fillStyle = 'rgba(255,255,255,0.85)';
      ctx.fillRect(x - 3, y + 8, 6, 4);

      // 两侧引擎
      for (const sx of [-30, 30]) {
        U.glowCircle(ctx, x + sx, y + 34, 6, '#ffb23c', 'rgba(255,140,40,0.6)', 16);
      }

      // 主炮口闪烁（弹幕蓄力提示）
      if (this.state === 'active' && this.barrageTimer < 0.35) {
        const k = Math.sin(time * 40);
        U.glowCircle(ctx, x, y + 44, 5 + k * 2, '#fff', 'rgba(255,60,40,0.8)', 20);
      }
      ctx.restore();
    }
  }

  /* ===================== 敌机管理 ===================== */
  class EnemyManager {
    constructor() {
      this.list = [];
    }
    add(e) { this.list.push(e); }
    update(dt, playerX, playerY) {
      for (let i = this.list.length - 1; i >= 0; i--) {
        const e = this.list[i];
        e.update(dt, playerX, playerY);
        if (e.out || e.removed) this.list.splice(i, 1);
      }
    }
    draw(ctx, time) {
      for (const e of this.list) e.draw(ctx, time);
    }
    clear() { this.list.length = 0; }
    get size() { return this.list.length; }
  }

  G.Enemy = Enemy;
  G.Boss = Boss;
  G.EnemyManager = EnemyManager;

})(window.Game = window.Game || {});
