/*
 * inventory.js —— 背包系统
 * 36 格：0-8 快捷栏，9-35 背包。每格 {id, count} 或 null
 * 支持合并堆叠、增删、查找、拖拽交换（UI 层使用 held 字段）
 */
'use strict';

const Inv = {
  SLOT_COUNT: 36,
  HOTBAR: 9,
  slots: [],       // 每格 {id, count} | null
  held: null,      // UI 拖拽中的物品 {id, count} | null
  dirty: true,     // UI 需要重绘标志

  reset() {
    this.slots = new Array(this.SLOT_COUNT).fill(null);
    this.held = null;
    this.dirty = true;
  },

  /* 快捷栏当前选中格索引 */
  selectedSlot() { return Game ? Game.hotbarSel : 0; },

  /* 当前选中格物品 */
  selected() {
    const i = this.selectedSlot();
    return this.slots[i] || null;
  },

  /* 增加物品，自动合并/找空位。返回未能放入的数量（0=全部放入） */
  add(id, count) {
    let remain = count;
    // 先合并已有堆
    for (let i = 0; i < this.slots.length && remain > 0; i++) {
      const s = this.slots[i];
      if (s && s.id === id && s.count < 99) {
        const put = Math.min(99 - s.count, remain);
        s.count += put;
        remain -= put;
      }
    }
    // 再放空位
    for (let i = 0; i < this.slots.length && remain > 0; i++) {
      if (!this.slots[i]) {
        const put = Math.min(99, remain);
        this.slots[i] = { id, count: put };
        remain -= put;
      }
    }
    this.dirty = true;
    return remain;
  },

  /* 从指定格扣除 count，格空则置 null */
  removeSlot(slot, count) {
    const s = this.slots[slot];
    if (!s) return false;
    s.count -= count;
    if (s.count <= 0) this.slots[slot] = null;
    this.dirty = true;
    return true;
  },

  /* 全局统计某物品数量 */
  countOf(id) {
    let n = 0;
    for (const s of this.slots) if (s && s.id === id) n += s.count;
    return n;
  },

  /* 消耗某物品 count 个（从各格扣），不足返回 false */
  consume(id, count) {
    let remain = count;
    for (let i = 0; i < this.slots.length && remain > 0; i++) {
      const s = this.slots[i];
      if (s && s.id === id) {
        const take = Math.min(s.count, remain);
        s.count -= take;
        remain -= take;
        if (s.count <= 0) this.slots[i] = null;
      }
    }
    if (remain > 0) return false;
    this.dirty = true;
    return true;
  },

  /* 背包是否已满 */
  isFull() {
    return this.slots.every(s => s !== null);
  },

  /* UI：点击格子（拖拽/交换逻辑） */
  clickSlot(slot) {
    const s = this.slots[slot];
    if (!this.held) {
      if (s) { this.held = s; this.slots[slot] = null; }
    } else {
      if (!s) { this.slots[slot] = this.held; this.held = null; }
      else if (s.id === this.held.id) {
        // 同物品合并
        const put = Math.min(99 - s.count, this.held.count);
        s.count += put;
        this.held.count -= put;
        if (this.held.count <= 0) this.held = null;
      } else {
        // 交换
        this.slots[slot] = this.held;
        this.held = s;
      }
    }
    this.dirty = true;
  },

  /* 序列化背包（存档） */
  serialize() {
    return this.slots.map(s => (s ? [s.id, s.count] : null));
  },

  deserialize(arr) {
    this.reset();
    for (let i = 0; i < arr.length && i < this.SLOT_COUNT; i++) {
      const e = arr[i];
      this.slots[i] = e ? { id: e[0], count: e[1] } : null;
    }
    this.dirty = true;
  }
};
