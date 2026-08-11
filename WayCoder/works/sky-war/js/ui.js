/* ============================================================
   ui.js — HUD 绘制（分数/生命/波次/连击/Boss 血条）
            + DOM 覆盖层（菜单/暂停/结算）
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class UI {
    constructor() {
      this.overlay = document.getElementById('overlay');
      this.title = document.getElementById('overlay-title');
      this.subtitle = document.getElementById('overlay-subtitle');
      this.body = document.getElementById('overlay-body');
      this.actions = document.getElementById('overlay-actions');
      this.touchHint = document.getElementById('touch-hint');
      this.cbs = {};
    }

    setCallbacks(cbs) { this.cbs = Object.assign(this.cbs, cbs); }

    /* ==================== 覆盖层 ==================== */
    _show(title, subtitle, bodyHTML, buttons) {
      this.title.textContent = title;
      this.subtitle.textContent = subtitle;
      this.body.innerHTML = bodyHTML;
      // 构建按钮
      this.actions.innerHTML = '';
      for (const b of buttons) {
        const el = document.createElement('button');
        el.className = 'btn' + (b.ghost ? ' btn-ghost' : '');
        el.textContent = b.label;
        el.addEventListener('click', () => { if (b.cb) b.cb(); });
        this.actions.appendChild(el);
      }
      this.overlay.classList.remove('hidden');
      this.touchHint.classList.add('hidden');
    }

    hide() { this.overlay.classList.add('hidden'); }

    showMenu(highScore) {
      const body = `
        <div>
          <div>移动 <span class="key">W</span><span class="key">A</span><span class="key">S</span><span class="key">D</span>
             / <span class="key">↑←↓→</span> / 按住鼠标或触屏拖动</div>
          <div>射击 按住 <span class="key">空格</span> 或鼠标左键</div>
          <div>炸弹 <span class="key">B</span>（清屏，消灭场上所有敌弹）</div>
          <div>暂停 <span class="key">P</span> / <span class="key">Esc</span></div>
        </div>
        <div style="margin-top:14px;color:#8fb3e8;">
          击落敌机得分 · 快速击杀可积累连击倍率<br>
          拾取 <b style="color:#ff9d3c">火力</b> / <b style="color:#7fd8ff">护盾</b> / <b style="color:#ff6a5a">炸弹</b> 道具强化自己<br>
          每 5 波出现 Boss，坚持得越久分数越高！
        </div>
        <div class="stat-line" style="margin-top:14px;">
          <span>历史最高分</span><b>${highScore.toLocaleString()}</b>
        </div>`;
      this._show('SKY STRIKE', '苍穹突袭', body, [
        { label: '▶ 开始游戏', cb: () => this.cbs.onStart && this.cbs.onStart() },
        { label: '音效开关', ghost: true, cb: () => {
            G.Audio.toggle();
            this.showMenu(U.getHighScore());
        }},
      ]);
    }

    showPause() {
      const body = `
        <div>游戏已暂停</div>
        <div style="margin-top:10px;font-size:12px;color:#7f9bc0;">
          <span class="key">P</span> / <span class="key">Esc</span> 继续 · <span class="key">R</span> 重新开始
        </div>`;
      this._show('PAUSED', '已暂停', body, [
        { label: '继续游戏', cb: () => this.cbs.onResume && this.cbs.onResume() },
        { label: '重新开始', ghost: true, cb: () => this.cbs.onRestart && this.cbs.onRestart() },
      ]);
    }

    showGameover(score, high, wave, kills) {
      const isNew = score > high;
      const body = `
        <div class="big-score">${score.toLocaleString()}</div>
        <div style="margin-top:4px;color:#8fb3e8;font-size:13px;">
          ${isNew ? '🏆 新纪录！' : `历史最高 ${high.toLocaleString()}`}
        </div>
        <div class="stat-line" style="margin-top:16px;">
          <span>坚持波次</span><b>${wave}</b>
        </div>
        <div class="stat-line">
          <span>击落敌机</span><b>${kills}</b>
        </div>
        <div class="stat-line">
          <span>最高连击</span><b>x${(U.getHighScore() ? '' : '')}${''}</b>
        </div>`;
      this._show('GAME OVER', '任务失败', body, [
        { label: '↻ 再来一局', cb: () => this.cbs.onRestart && this.cbs.onRestart() },
        { label: '返回菜单', ghost: true, cb: () => this.cbs.onMenu && this.cbs.onMenu() },
      ]);
    }

    /* ==================== Canvas HUD ==================== */
    drawHUD(ctx, s) {
      const { score, wave, combo, comboLevel, comboMaxTimer, player, boss, bombs } = s;

      // 顶部信息条（半透明底）
      ctx.save();
      ctx.fillStyle = 'rgba(4,8,20,0.45)';
      ctx.fillRect(0, 0, CFG.LOGIC_W, 34);

      // 分数
      ctx.fillStyle = '#ffd76a';
      ctx.font = 'bold 17px Consolas, monospace';
      ctx.textAlign = 'left';
      ctx.textBaseline = 'middle';
      ctx.fillText(`SCORE ${String(score).padStart(8, '0')}`, 10, 17);

      // 波次
      ctx.fillStyle = '#9fd4ff';
      ctx.font = 'bold 13px Consolas, monospace';
      ctx.textAlign = 'center';
      ctx.fillText(`WAVE ${wave}`, CFG.LOGIC_W / 2, 17);

      // 炸弹数
      ctx.textAlign = 'right';
      ctx.fillStyle = '#ff8a7a';
      ctx.font = 'bold 14px Consolas, monospace';
      let bombTxt = '';
      for (let i = 0; i < (player ? player.bombs : 0); i++) bombTxt += '💣';
      ctx.fillText(bombTxt || '—', CFG.LOGIC_W - 10, 17);
      ctx.restore();

      // 生命（左下小飞机）
      if (player) {
        ctx.save();
        for (let i = 0; i < Math.max(0, player.lives); i++) {
          this._drawMiniShip(ctx, 24 + i * 26, CFG.LOGIC_H - 20);
        }
        // 护盾标记
        if (player.shield) {
          ctx.strokeStyle = 'rgba(90,200,255,0.9)';
          ctx.lineWidth = 1.5;
          ctx.beginPath();
          ctx.arc(24, CFG.LOGIC_H - 20, 12, 0, Math.PI * 2);
          ctx.stroke();
        }
        ctx.restore();
      }

      // 连击
      if (comboLevel >= 2 && comboMaxTimer > 0) {
        ctx.save();
        const scale = 1 + Math.min(0.35, comboMaxTimer * 0.8);
        ctx.translate(CFG.LOGIC_W / 2, 78);
        ctx.scale(scale, scale);
        ctx.textAlign = 'center';
        ctx.font = 'bold 26px Consolas, monospace';
        ctx.fillStyle = `rgba(255,157,60,${0.75 + Math.sin(s.time * 12) * 0.2})`;
        ctx.shadowColor = '#ff9d3c';
        ctx.shadowBlur = 16;
        ctx.fillText(`COMBO x${comboLevel}`, 0, 0);
        // 剩余时间条
        ctx.shadowBlur = 0;
        ctx.fillStyle = 'rgba(255,157,60,0.25)';
        ctx.fillRect(-40, 12, 80, 3);
        ctx.fillStyle = '#ff9d3c';
        ctx.fillRect(-40, 12, 80 * U.clamp(combo / comboMaxTimer, 0, 1), 3);
        ctx.restore();
      }

      // Boss 血条
      if (boss && boss.state === 'active') {
        const bw = CFG.LOGIC_W - 60;
        const bx = 30, by = 46;
        ctx.save();
        ctx.fillStyle = 'rgba(10,16,32,0.6)';
        ctx.fillRect(bx - 2, by - 2, bw + 4, 12);
        const pct = U.clamp(boss.hp / boss.maxHp, 0, 1);
        const grad = ctx.createLinearGradient(bx, 0, bx + bw, 0);
        grad.addColorStop(0, '#ff5a3c');
        grad.addColorStop(1, '#ffb23c');
        ctx.fillStyle = grad;
        ctx.fillRect(bx, by, bw * pct, 8);
        ctx.strokeStyle = 'rgba(255,140,80,0.5)';
        ctx.lineWidth = 1;
        ctx.strokeRect(bx - 0.5, by - 0.5, bw + 1, 9);
        ctx.fillStyle = '#ffb0a0';
        ctx.font = 'bold 12px Consolas, monospace';
        ctx.textAlign = 'center';
        ctx.fillText('⚠ BOSS ⚠', CFG.LOGIC_W / 2, by - 8);
        ctx.restore();
      }

      // 移动端提示
      if (s.isTouch) {
        this.touchHint.classList.remove('hidden');
      }
    }

    /* 小飞机图标（生命） */
    _drawMiniShip(ctx, x, y) {
      ctx.fillStyle = '#5fb0f0';
      ctx.beginPath();
      ctx.moveTo(x, y - 8);
      ctx.lineTo(x - 5, y + 6);
      ctx.lineTo(x + 5, y + 6);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = '#c8ecff';
      ctx.beginPath();
      ctx.arc(x, y - 1, 2.4, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  G.UI = UI;

})(window.Game = window.Game || {});
