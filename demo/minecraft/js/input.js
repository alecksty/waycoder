// input.js — 键盘 + 鼠标输入管理器：WASD/空格/双击疾跑、指针锁定后的鼠标视角、数字键选择快捷栏。
// 通过回调把按键/鼠标事件翻译为游戏内输入状态与动作，供主循环与 UI 使用。

export class Input {
  /**
   * @param {HTMLElement} element 需要请求指针锁定的元素（通常是 canvas）
   * @param {object} hooks 回调：{ onLock, onUnlock, onHotbar(index), onDig, onPlace, onSelect }
   */
  constructor(element, hooks = {}) {
    this.el = element;
    this.hooks = hooks;
    this.keys = {};        // 原始按下状态
    this.state = {
      forward: false, back: false, left: false, right: false,
      jump: false, sprint: false, down: false,
    };
    this.locked = false;
    this.mouseDX = 0;
    this.mouseDY = 0;
    this.sens = 0.0022;    // 鼠标灵敏度
    this._bind();
  }

  _bind() {
    window.addEventListener('keydown', (e) => this.onKeyDown(e));
    window.addEventListener('keyup', (e) => this.onKeyUp(e));
    window.addEventListener('mousemove', (e) => this.onMouseMove(e));

    // 指针锁定
    this.el.addEventListener('click', () => this.requestLock());
    document.addEventListener('pointerlockchange', () => {
      this.locked = document.pointerLockElement === this.el;
      if (this.locked) this.hooks.onLock && this.hooks.onLock();
      else this.hooks.onUnlock && this.hooks.onUnlock();
    });

    // 阻止右键菜单（放置方块用左键/右键）
    this.el.addEventListener('contextmenu', (e) => e.preventDefault());
    this.el.addEventListener('mousedown', (e) => this.onMouseDown(e));
    window.addEventListener('wheel', (e) => this.onWheel(e));
  }

  requestLock() {
    if (!this.locked) this.el.requestPointerLock?.();
  }

  onKeyDown(e) {
    this.keys[e.code] = true;
    this.updateMovement();
    // 快捷栏数字 1-9
    const digit = ['Digit1','Digit2','Digit3','Digit4','Digit5','Digit6','Digit7','Digit8','Digit9'];
    const idx = digit.indexOf(e.code);
    if (idx >= 0 && this.locked) {
      this.hooks.onHotbar && this.hooks.onHotbar(idx);
      e.preventDefault();
    }
  }

  onKeyUp(e) {
    this.keys[e.code] = false;
    this.updateMovement();
  }

  /** 将按键映射为移动状态（含双击 W 疾跑） */
  updateMovement() {
    this.state.forward = !!this.keys['KeyW'];
    this.state.back = !!this.keys['KeyS'];
    this.state.left = !!this.keys['KeyA'];
    this.state.right = !!this.keys['KeyD'];
    this.state.jump = !!this.keys['Space'];
    this.state.down = !!this.keys['ShiftLeft'] || !!this.keys['ShiftRight'];
    this.state.sprint = !!this.keys['ControlLeft'] || this._doubleW;
    this.hooks.onSelect && this.hooks.onSelect();
  }

  onMouseMove(e) {
    if (!this.locked) return;
    this.mouseDX += e.movementX;
    this.mouseDY += e.movementY;
  }

  onMouseDown(e) {
    if (!this.locked) return;
    if (e.button === 0) this.hooks.onDig && this.hooks.onDig();       // 左键破坏
    else if (e.button === 2) this.hooks.onPlace && this.hooks.onPlace(); // 右键放置
  }

  onWheel(e) {
    if (!this.locked) return;
    const dir = Math.sign(e.deltaY);
    if (this.hooks.onHotbar) {
      this.hooks.onHotbar(this.currentHotbar + dir);
    }
  }

  /** 每帧末清空鼠标增量 */
  consumeMouse() {
    const dx = this.mouseDX, dy = this.mouseDY;
    this.mouseDX = 0; this.mouseDY = 0;
    return { dx, dy };
  }

  /** 松开全部按键（指针解锁时调用） */
  reset() {
    this.keys = {};
    this.updateMovement();
  }
}
