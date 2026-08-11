/* ============================================================
   input.js — 键盘 / 鼠标 / 触屏统一输入
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class Input {
    constructor(canvas) {
      this.canvas = canvas;
      this.keys = {};          // 按键按下集合
      this.pointerDown = false;
      this.pointer = { x: 0, y: 0 };   // 画布逻辑坐标
      this.downPos = null;     // 按下位置（用于死区判断）
      this._onKeyDown = this._onKeyDown.bind(this);
      this._onKeyUp = this._onKeyUp.bind(this);
      this._onPointerDown = this._onPointerDown.bind(this);
      this._onPointerMove = this._onPointerMove.bind(this);
      this._onPointerUp = this._onPointerUp.bind(this);
      this._onBlur = this._onBlur.bind(this);
      this._bind();
    }

    /* ---------- 绑定事件 ---------- */
    _bind() {
      window.addEventListener('keydown', this._onKeyDown);
      window.addEventListener('keyup', this._onKeyUp);
      window.addEventListener('blur', this._onBlur);
      const c = this.canvas;
      c.addEventListener('mousedown', this._onPointerDown);
      window.addEventListener('mousemove', this._onPointerMove);
      window.addEventListener('mouseup', this._onPointerUp);
      c.addEventListener('touchstart', this._onPointerDown, { passive: false });
      window.addEventListener('touchmove', this._onPointerMove, { passive: false });
      window.addEventListener('touchend', this._onPointerUp, { passive: false });
      window.addEventListener('touchcancel', this._onPointerUp, { passive: false });
    }

    /* ---------- 键盘 ---------- */
    _onKeyDown(e) {
      const k = e.key.toLowerCase();
      // 阻止方向键/空格滚动页面
      if (['arrowup', 'arrowdown', 'arrowleft', 'arrowright', ' '].includes(k)) {
        e.preventDefault();
      }
      if (!this.keys[k]) {
        // 边缘触发回调（供 UI 用，如 Enter 开始）
        if (this.onKeyPress) this.onKeyPress(k);
      }
      this.keys[k] = true;
    }
    _onKeyUp(e) { this.keys[e.key.toLowerCase()] = false; }
    _onBlur() { this.keys = {}; this.pointerDown = false; }

    /* ---------- 鼠标 / 触屏 ---------- */
    _toLogic(clientX, clientY) {
      const rect = this.canvas.getBoundingClientRect();
      const sx = CFG.LOGIC_W / rect.width;
      const sy = CFG.LOGIC_H / rect.height;
      return {
        x: (clientX - rect.left) * sx,
        y: (clientY - rect.top) * sy,
      };
    }
    _onPointerDown(e) {
      e.preventDefault();
      const p = this._toLogic(e.clientX ?? e.touches[0].clientX,
                              e.clientY ?? e.touches[0].clientY);
      this.pointerDown = true;
      this.downPos = { x: p.x, y: p.y };
      this.pointer.x = p.x; this.pointer.y = p.y;
      if (this.onPointerDown) this.onPointerDown(p);
    }
    _onPointerMove(e) {
      const cx = e.clientX ?? (e.touches && e.touches[0] ? e.touches[0].clientX : null);
      const cy = e.clientY ?? (e.touches && e.touches[0] ? e.touches[0].clientY : null);
      if (cx == null) return;
      const p = this._toLogic(cx, cy);
      this.pointer.x = p.x; this.pointer.y = p.y;
      if (this.onPointerMove) this.onPointerMove(p, this.pointerDown);
    }
    _onPointerUp(e) {
      this.pointerDown = false;
      this.downPos = null;
      if (this.onPointerUp) this.onPointerUp();
    }

    /* ---------- 查询接口 ---------- */
    down(...keys) {
      for (const k of keys) if (this.keys[k]) return true;
      return false;
    }
    // 方向向量（归一化）
    dir() {
      let x = 0, y = 0;
      if (this.down('a', 'arrowleft'))  x -= 1;
      if (this.down('d', 'arrowright')) x += 1;
      if (this.down('w', 'arrowup'))    y -= 1;
      if (this.down('s', 'arrowdown'))  y += 1;
      const len = Math.hypot(x, y);
      if (len > 0) { x /= len; y /= len; }
      return { x, y };
    }
    firing() { return this.down(' ') || this.pointerDown; }

    destroy() {
      window.removeEventListener('keydown', this._onKeyDown);
      window.removeEventListener('keyup', this._onKeyUp);
      window.removeEventListener('blur', this._onBlur);
    }
  }

  G.Input = Input;

})(window.Game = window.Game || {});
