// =====================================================================
// input.js — Keyboard + pointer input manager
// Tracks pressed keys, single-press edges, mouse position.
// =====================================================================

export class InputManager {
  constructor(target = window) {
    this.target = target;
    this.keys = new Set();
    this.justPressed = new Set();   // keys pressed this frame
    this.justReleased = new Set();  // keys released this frame
    this._prevKeys = new Set();
    this._handlers = new Map();     // key -> [cb] for onPress
    this.enabled = true;

    this._onKD = (e) => this._handleKeyDown(e);
    this._onKU = (e) => this._handleKeyUp(e);
    this._onBlur = () => this.clearAll();

    window.addEventListener('keydown', this._onKD, { passive: false });
    window.addEventListener('keyup', this._onKU, { passive: false });
    window.addEventListener('blur', this._onBlur);
  }

  _normKey(e) {
    // Normalize so 'a' and 'A' both register as 'a'; arrows as 'arrowup' etc.
    const k = e.key.length === 1 ? e.key.toLowerCase() : e.key.toLowerCase();
    return k;
  }

  _handleKeyDown(e) {
    if (!this.enabled) return;
    const k = this._normKey(e);
    // Prevent default for game keys
    const gameKeys = [' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright', 'w', 'a', 's', 'd', 'shift', 'p', 'escape', 'm', 'r'];
    if (gameKeys.includes(k)) e.preventDefault();
    if (!this.keys.has(k)) {
      this.justPressed.add(k);
      const list = this._handlers.get(k);
      if (list) for (const fn of list) try { fn(); } catch (err) { console.error(err); }
    }
    this.keys.add(k);
  }

  _handleKeyUp(e) {
    const k = this._normKey(e);
    this.keys.delete(k);
    this.justReleased.add(k);
  }

  clearAll() { this.keys.clear(); this.justPressed.clear(); this.justReleased.clear(); }

  // Call once per frame AFTER consumers read input
  endFrame() {
    this._prevKeys = new Set(this.keys);
    this.justPressed.clear();
    this.justReleased.clear();
  }

  isDown(key) { return this.keys.has(key); }
  wasPressed(key) { return this.justPressed.has(key); }
  wasReleased(key) { return this.justReleased.has(key); }

  onPress(key, fn) {
    if (!this._handlers.has(key)) this._handlers.set(key, []);
    this._handlers.get(key).push(fn);
  }

  destroy() {
    window.removeEventListener('keydown', this._onKD);
    window.removeEventListener('keyup', this._onKU);
    window.removeEventListener('blur', this._onBlur);
  }
}
