/* ============================================================
   player.js — 玩家战斗机（移动 / 射击 / 道具状态 / 绘制）
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class Player {
    /**
     * @param {object} cb { onFire(x,y,angle), onThrust(x,y,angle) }
     */
    constructor(cb) {
      this.cb = cb || {};
      const P = CFG.PLAYER;
      this.w = P.w;
      this.h = P.h;
      this.r = Math.min(P.w, P.h) / 2 * 0.8;   // 碰撞半径
      this.x = CFG.LOGIC_W / 2;
      this.y = CFG.LOGIC_H - 90;

      this.lives = P.lives;
      this.powerLevel = 1;        // 1..4
      this.shield = false;
      this.bombs = 1;

      this.alive = true;
      this.invincible = 0;        // 无敌剩余
      this.hitFlash = 0;          // 受击红光
      this.fireTimer = 0;

      // 触屏/鼠标拖拽目标（未拖拽时为 null）
      this.pointerTarget = null;
    }

    reset() {
      const P = CFG.PLAYER;
      this.x = CFG.LOGIC_W / 2;
      this.y = CFG.LOGIC_H - 90;
      this.lives = P.lives;
      this.powerLevel = 1;
      this.shield = false;
      this.bombs = 1;
      this.alive = true;
      this.invincible = P.invincible;
      this.hitFlash = 0;
      this.fireTimer = 0;
      this.pointerTarget = null;
    }

    /* ---------- 更新 ---------- */
    /**
     * @param {number} dt
     * @param {Input}  input
     */
    update(dt, input) {
      const P = CFG.PLAYER;
      const CF = CFG.TOUCH;

      // 1) 键盘方向移动（带平滑加速）
      const dir = input.dir();
      if (dir.x !== 0 || dir.y !== 0) {
        const k = 1 - Math.exp(-P.accel * dt);
        this.x = U.lerp(this.x, this.x + dir.x * P.speed * dt * 3, k);
        this.y = U.lerp(this.y, this.y + dir.y * P.speed * dt * 3, k);
      }

      // 2) 指针拖拽跟随
      if (input.pointerDown && input.downPos) {
        // 超过死区才视为拖拽
        if (U.dist(input.pointer.x, input.pointer.y, input.downPos.x, input.downPos.y) > CF.deadzone) {
          this.pointerTarget = { x: input.pointer.x, y: input.pointer.y };
        }
      } else if (!input.pointerDown) {
        this.pointerTarget = null;
      }
      if (this.pointerTarget) {
        const k = 1 - Math.exp(-CF.follow * dt);
        this.x = U.lerp(this.x, this.pointerTarget.x, k);
        this.y = U.lerp(this.y, this.pointerTarget.y, k);
      }

      // 边界限制
      this.x = U.clamp(this.x, this.w / 2 + 4, CFG.LOGIC_W - this.w / 2 - 4);
      this.y = U.clamp(this.y, this.h / 2 + 30, CFG.LOGIC_H - this.h / 2 - 6);

      // 计时器
      if (this.invincible > 0) this.invincible -= dt;
      if (this.hitFlash > 0) this.hitFlash -= dt;

      // 3) 射击
      if (this.alive && input.firing()) {
        this.fireTimer -= dt;
        if (this.fireTimer <= 0) {
          this.fireTimer = P.fireInterval;
          this._fire();
        }
      }

      // 4) 引擎尾焰
      if (this.cb.onThrust) this.cb.onThrust(this.x, this.y, -Math.PI / 2);
    }

    _fire() {
      const y = this.y - this.h / 2;
      const cb = this.cb.onFire;
      if (!cb) return;
      const L = this.powerLevel;
      const spread = 0.16;

      if (L === 1) {            // 单发
        cb(this.x, y, -Math.PI / 2);
      } else if (L === 2) {     // 双发
        cb(this.x - 8, y + 2, -Math.PI / 2);
        cb(this.x + 8, y + 2, -Math.PI / 2);
      } else if (L === 3) {     // 三发
        cb(this.x, y, -Math.PI / 2);
        cb(this.x - 10, y + 4, -Math.PI / 2 + spread * 0.5);
        cb(this.x + 10, y + 4, -Math.PI / 2 - spread * 0.5);
      } else {                  // 五发（L>=4）
        cb(this.x, y, -Math.PI / 2);
        cb(this.x - 8, y + 2, -Math.PI / 2);
        cb(this.x + 8, y + 2, -Math.PI / 2);
        cb(this.x - 16, y + 6, -Math.PI / 2 + spread);
        cb(this.x + 16, y + 6, -Math.PI / 2 - spread);
      }
    }

    /* ---------- 受击 ---------- */
    /**
     * @returns {boolean} 是否真的受伤（false=被护盾/无敌抵挡）
     */
    takeHit() {
      if (this.invincible > 0) return false;
      if (this.shield) {
        this.shield = false;
        this.invincible = 1.2;      // 破盾后短暂保护
        return false;
      }
      this.lives -= 1;
      this.powerLevel = Math.max(1, this.powerLevel - 1);
      this.hitFlash = CFG.PLAYER.hitFlash;
      this.invincible = CFG.PLAYER.invincible;
      if (this.lives <= 0) this.alive = false;
      return true;
    }

    /* ---------- 道具 ---------- */
    applyPowerup(kind) {
      switch (kind) {
        case 'power':
          this.powerLevel = Math.min(4, this.powerLevel + 1);
          break;
        case 'shield':
          this.shield = true;
          break;
        case 'bomb':
          this.bombs = Math.min(3, this.bombs + 1);
          break;
      }
    }

    /* ---------- 绘制 ---------- */
    draw(ctx, time) {
      if (!this.alive) return;
      const P = CFG.PLAYER;
      const { x, y } = this;

      // 无敌闪烁
      if (this.invincible > 0 && Math.floor(this.invincible * 14) % 2 === 0) {
        ctx.globalAlpha = 0.45;
      }

      // 引擎火焰（脉动）
      const flame = 10 + Math.sin(time * 30) * 4;
      U.glowCircle(ctx, x, y + this.h / 2 - 2, 4, '#ffd76a', 'rgba(255,150,40,0.7)', flame);
      ctx.fillStyle = '#7ec8ff';
      U.polygon(ctx, x, y + this.h / 2, [[-3, 0], [0, flame], [3, 0]]);
      ctx.fill();

      // 机身（蓝白战斗机）
      ctx.fillStyle = '#3f7fd4';
      U.polygon(ctx, x, y, [[0, -this.h / 2], [-7, -10], [-20, 4], [-14, 16], [-6, 12], [0, 18], [6, 12], [14, 16], [20, 4], [7, -10]]);
      ctx.fill();

      // 机身亮部
      ctx.fillStyle = '#7fb4f0';
      U.polygon(ctx, x, y, [[0, -this.h / 2], [-4, -8], [0, 2], [4, -8]]);
      ctx.fill();

      // 机翼高光
      ctx.fillStyle = '#5b93d8';
      ctx.fillRect(x - 18, y + 2, 5, 10);
      ctx.fillRect(x + 13, y + 2, 5, 10);

      // 驾驶舱
      U.glowCircle(ctx, x, y - 6, 5, 'rgba(160,230,255,0.9)', 'rgba(120,200,255,0.4)', 9);

      // 受击红光
      if (this.hitFlash > 0) {
        ctx.globalCompositeOperation = 'lighter';
        U.radial(ctx, x, y, 0, 40, `rgba(255,60,40,${this.hitFlash * 1.6})`, 'rgba(255,60,40,0)');
        ctx.globalCompositeOperation = 'source-over';
      }

      // 护盾
      if (this.shield) {
        const pulse = 0.75 + Math.sin(time * 6) * 0.15;
        ctx.save();
        ctx.strokeStyle = `rgba(90,200,255,${0.5 * pulse})`;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(x, y, 30 + Math.sin(time * 3) * 2, 0, Math.PI * 2);
        ctx.stroke();
        ctx.strokeStyle = `rgba(90,200,255,${0.18 * pulse})`;
        ctx.lineWidth = 8;
        ctx.beginPath();
        ctx.arc(x, y, 30 + Math.sin(time * 3) * 2, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
      }

      ctx.globalAlpha = 1;
    }
  }

  G.Player = Player;

})(window.Game = window.Game || {});
