/*
 * tiles.js —— 方块与物品定义表
 * Tiles:  方块表（索引 = 方块 id），属性含颜色/硬度/掉落/是否实体
 * Items:  物品表（字典 id → 定义），方块物品与方块同 id，工具/材料用 100+ 段
 * 纯数据模块，供 world / player / ui / crafting 使用
 */
'use strict';

/* ================= 方块表 =================
 * 字段：
 *  name   方块名
 *  color  主色
 *  dot    斑点色（矿石等，null 则无斑点）
 *  solid  是否实体（可碰撞）
 *  hard   挖掘硬度（秒 * 手速系数，见 player.js）
 *  light  自发光 0..1（0=不发光）
 *  drop   挖掉掉落的物品 id（默认 = 方块 id 自身）
 *  fluid  是否为流体（不碰撞，仅渲染）
 */
const Tiles = [];
(function buildTiles() {
  const add = (id, def) => { def.id = id; Tiles[id] = def; };

  add(0,  { name: '空气',       color: '#000000', solid: false, hard: 0,    light: 0 });
  add(1,  { name: '草方块',     color: '#5caf50', dot: '#4a9644', solid: true, hard: 0.6, light: 0 });
  add(2,  { name: '泥土',       color: '#8a6a4a', dot: '#7a5c3e', solid: true, hard: 0.5, light: 0 });
  add(3,  { name: '石头',       color: '#8d8d8d', dot: '#7a7a7a', solid: true, hard: 2.0, light: 0 });
  add(4,  { name: '沙子',       color: '#e6daa0', dot: '#d8ca8c', solid: true, hard: 0.4, light: 0 });
  add(5,  { name: '原木',       color: '#7a5230', dot: '#5f3f22', solid: true, hard: 1.2, light: 0 });
  add(6,  { name: '树叶',       color: '#3e8f3e', dot: '#357a35', solid: true, hard: 0.2, light: 0 });
  add(7,  { name: '木板',       color: '#b08c52', dot: '#a07c44', solid: true, hard: 0.8, light: 0 });
  add(8,  { name: '工作台',     color: '#a07048', dot: '#8a5c36', solid: true, hard: 1.4, light: 0 });
  add(9,  { name: '熔炉',       color: '#5a5a5a', dot: '#3e3e3e', solid: true, hard: 2.5, light: 0.45 });
  add(10, { name: '火把',       color: '#e0a030', dot: null,      solid: false, hard: 0.1, light: 1.0 });
  add(11, { name: '煤矿石',     color: '#6a6a6a', dot: '#222222', solid: true, hard: 3.0, light: 0, drop: 130 });
  add(12, { name: '铁矿石',     color: '#b08a72', dot: '#c98e62', solid: true, hard: 3.2, light: 0, drop: 131 });
  add(13, { name: '金矿石',     color: '#c8b24a', dot: '#f0d840', solid: true, hard: 3.6, light: 0, drop: 132 });
  add(14, { name: '钻石矿',     color: '#5ab8c0', dot: '#7ef0f8', solid: true, hard: 4.0, light: 0, drop: 133 });
  add(15, { name: '水',         color: '#3a6fc8', dot: null,      solid: false, hard: 0,    light: 0, fluid: true });
  add(16, { name: '石砖',       color: '#9a9a9a', dot: '#7e7e7e', solid: true, hard: 2.4, light: 0 });
  add(17, { name: '玻璃',       color: '#c8ecf4', dot: null,      solid: true, hard: 0.3, light: 0.15 });
  add(18, { name: '树苗',       color: '#3e9a3e', dot: null,      solid: false, hard: 0.1, light: 0 });
})();

/* 方块名速查（物品显示用） */
Tiles.nameOf = id => (Tiles[id] ? Tiles[id].name : '未知');

/* ================= 物品表 =================
 * 类型：
 *  block    方块物品（tileId 指向方块）
 *  sword    剑（dmg 伤害）
 *  pick     镐（power 挖掘力）
 *  material 材料
 */
const Items = {};
(function buildItems() {
  const add = def => { Items[def.id] = def; };

  // 方块物品（与方块 id 相同）
  for (let i = 1; i < Tiles.length; i++) {
    const t = Tiles[i];
    add({ id: i, name: t.name, type: 'block', tileId: i, color: t.color });
  }
  // 工具：剑
  add({ id: 100, name: '木剑',     type: 'sword', dmg: 4,  color: '#a07850' });
  add({ id: 101, name: '石剑',     type: 'sword', dmg: 6,  color: '#9a9a9a' });
  add({ id: 102, name: '铁剑',     type: 'sword', dmg: 9,  color: '#d8d8d8' });
  add({ id: 103, name: '钻石剑',   type: 'sword', dmg: 13, color: '#6af0e0' });
  // 工具：镐
  add({ id: 110, name: '木镐',     type: 'pick', power: 2.2, color: '#a07850' });
  add({ id: 111, name: '石镐',     type: 'pick', power: 3.4, color: '#9a9a9a' });
  add({ id: 112, name: '铁镐',     type: 'pick', power: 5.2, color: '#d8d8d8' });
  add({ id: 113, name: '钻石镐',   type: 'pick', power: 9.0, color: '#6af0e0' });
  // 材料
  add({ id: 120, name: '木棒',     type: 'material', color: '#8a6a3a' });
  add({ id: 130, name: '煤炭',     type: 'material', color: '#333333' });
  add({ id: 131, name: '铁矿石',   type: 'material', color: '#c98e62' });
  add({ id: 132, name: '金矿石',   type: 'material', color: '#f0d840' });
  add({ id: 133, name: '钻石',     type: 'material', color: '#7ef0f8' });
  add({ id: 141, name: '铁锭',     type: 'material', color: '#d8d8d8' });
  add({ id: 142, name: '金锭',     type: 'material', color: '#f0d840' });
  // 食物
  add({ id: 150, name: '苹果',     type: 'food', heal: 3, color: '#e84040' });
  add({ id: 151, name: '烤肉',     type: 'food', heal: 6, color: '#b07048' });
})();

Items.get = id => Items[id] || null;
Items.nameOf = id => (Items[id] ? Items[id].name : '未知');
Items.isBlock = id => (Items[id] ? Items[id].type === 'block' : false);
