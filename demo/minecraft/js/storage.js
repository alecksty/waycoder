// storage.js — 存档/读档模块：用 localStorage 保存玩家位置、世界种子与「用户编辑记录」。
// 用户放置/破坏的方块被记录为差异（paint 编辑），加载时叠加在程序化地形之上，实现可持久化。

const KEY = 'waycoder-minecraft-world-v1';

/**
 * 序列化当前世界状态到 localStorage。
 * @param {object} state
 * @param {{seed:number, edits:Array, playerPosition:Array, time:number}} state
 */
export function saveWorld({ seed, edits, playerPosition, time }) {
  try {
    const payload = JSON.stringify({
      v: 1,
      seed,
      edits,
      playerPosition,
      time,
      savedAt: Date.now(),
    });
    localStorage.setItem(KEY, payload);
    return true;
  } catch (e) {
    console.warn('[storage] 保存失败', e);
    return false;
  }
}

/**
 * 读取存档；若不存在或损坏返回 null。
 * @returns {{seed:number, edits:Array, playerPosition:Array, time:number}|null}
 */
export function loadWorld() {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const data = JSON.parse(raw);
    if (!data || data.v !== 1) return null;
    return {
      seed: data.seed,
      edits: data.edits || [],
      playerPosition: data.playerPosition,
      time: data.time,
    };
  } catch (e) {
    console.warn('[storage] 读取失败', e);
    return null;
  }
}

/** 清空存档 */
export function clearWorld() {
  try { localStorage.removeItem(KEY); } catch (e) { /* 忽略 */ }
}

/** 备份存档到一个新 key（当用户覆盖/新建时用） */
export function backupWorld() {
  const data = loadWorld();
  if (!data) return false;
  try {
    localStorage.setItem(KEY + '-backup-' + Date.now(), JSON.stringify(data));
    return true;
  } catch (e) { return false; }
}
