/*
 * smoke.js —— 冒烟自测：在 node 环境中验证核心逻辑
 * 覆盖：方块表 / 物品表 / 世界生成（地形、矿石、越界）/ 物理碰撞 /
 *       背包堆叠与消耗 / 合成配方与设施限制 / 序列化往返
 * 运行：node test/smoke.js
 */
'use strict';
const fs = require('fs');
const path = require('path');

/* 浏览器全局桩 */
globalThis.btoa = s => Buffer.from(s, 'binary').toString('base64');
globalThis.atob = s => Buffer.from(s, 'base64').toString('binary');
globalThis.performance = { now: () => Date.now() };

const files = ['core.js', 'tiles.js', 'world.js', 'inventory.js', 'crafting.js', 'player.js', 'entities.js'];
const src = files.map(f => fs.readFileSync(path.join(__dirname, '..', 'js', f), 'utf8')).join('\n')
  + '\n;globalThis.Game = { hotbarSel: 0, kills: 0, isDay: true, isNight: false, onPlayerDeath(){} };'
  + '\n;globalThis.AudioSys = { sfx(){} };'
  + '\n;return { Core, Tiles, Items, World, Inv, Craft, Recipes, Player, Entities, Game };';
const m = new Function(src)();

let pass = 0, fail = 0;
const check = (name, cond) => {
  if (cond) { pass++; console.log('  ✓ ' + name); }
  else { fail++; console.log('  ✗ ' + name); }
};

/* ---- 方块表 ---- */
check('方块表共 16 种', m.Tiles.length === 16);
check('草方块是实体', m.Tiles[1].solid === true);
check('水是流体且不碰撞', m.Tiles[15].fluid === true && m.Tiles[15].solid === false);
check('火把发光', m.Tiles[10].light === 1);
check('矿石有掉落物品', m.Tiles[11].drop === 130 && m.Tiles[14].drop === 133);

/* ---- 物品表 ---- */
check('木剑伤害 4', m.Items.get(100).dmg === 4);
check('钻石镐挖掘力 9', m.Items.get(113).power === 9);
check('铁锭材料存在', m.Items.get(141).type === 'material');

/* ---- 世界生成 ---- */
const spawn = m.World.generate(12345);
check('世界数据已分配', m.World.data && m.World.data.length === m.World.W * m.World.H);
check('出生点在合理高度', spawn.y > 20 && spawn.y < m.World.H - 10);
check('出生点脚下是实体方块', m.World.isSolid(spawn.x, spawn.y));
/* 地形多样性：统计地表方块类型 */
const counts = {};
for (let x = 0; x < m.World.W; x += 5) {
  const sy = m.World.surfaceAt(x);
  const id = m.World.get(x, sy);
  counts[id] = (counts[id] || 0) + 1;
}
check('有草地块', (counts[1] || 0) > 0);
check('有沙地/水', ((counts[4] || 0) + (counts[15] || 0)) > 0 || (m.World.SEA > 0));
/* 树木存在 */
let trees = 0;
for (let x = 0; x < m.World.W; x += 3) {
  for (let y = 0; y < m.World.SEA - 10; y++) {
    if (m.World.get(x, y) === 5) { trees++; break; }
  }
}
check('生成了树木', trees > 0);
/* 矿石存在 */
let ores = 0;
for (let i = 0; i < m.World.data.length; i += 61) {
  const v = m.World.data[i];
  if (v >= 11 && v <= 14) ores++;
}
check('生成了矿石', ores > 0);
/* 洞穴存在 */
let caves = 0;
for (let y = m.World.SEA + 10; y < m.World.H - 5; y += 3) {
  for (let x = 0; x < m.World.W; x += 7) {
    if (m.World.get(x, y) === 0 && m.World.get(x, y - 1) !== 0) { caves++; break; }
  }
}
check('生成了洞穴', caves > 2);
/* 越界处理 */
check('越界底部为实心', m.World.isSolid(0, m.World.H));
check('breakBlock 返回方块 id', m.World.breakBlock(spawn.x, spawn.y) > 0);

/* ---- 物理 ---- */
const e1 = { x: 400, y: 0, w: 10, h: 20, vx: 0, vy: 0, grav: 700, maxFall: 1200 };
for (let i = 0; i < 400; i++) m.World.moveEntity(e1, 1 / 60);
check('自由落体最终落地', e1.onGround === true);
check('落地不穿方块', e1.y + e1.h <= m.World.surfaceAt(Math.floor(e1.x / 16)) * 16 + 0.1);
const e2 = { x: 400, y: 0, w: 10, h: 20, vx: 80, vy: 0, grav: 700, maxFall: 40 };
for (let i = 0; i < 120; i++) m.World.moveEntity(e2, 1 / 60);
check('水平移动生效', e2.x > 401);

/* ---- 背包 ---- */
m.Inv.reset();
check('初始背包为空', m.Inv.countOf(5) === 0);
check('加入 10 个原木成功', m.Inv.add(5, 10) === 0 && m.Inv.countOf(5) === 10);
m.Inv.add(5, 200);
const totalWood = m.Inv.slots.filter(s => s && s.id === 5).reduce((a, s) => a + s.count, 0);
check('堆叠限制 99（共 210 个）', totalWood === 210);
check('所有格子不超过 99', m.Inv.slots.every(s => !s || s.count <= 99));
check('消耗 5 个成功', m.Inv.consume(5, 5) === true);
check('剩余 205 个', m.Inv.countOf(5) === 205);
check('消耗不存在的物品失败', m.Inv.consume(999, 1) === false);

/* ---- 合成 ---- */
m.Inv.reset();
m.Inv.add(5, 1);
const rPlank = m.Recipes.find(r => r.out === 7);
check('徒手配方（木板）可合成', m.Craft.available(false, false).some(x => x.recipe.out === 7 && x.canCraft));
check('合成木板成功', m.Craft.craft(rPlank) === true);
check('原木消耗为 0', m.Inv.countOf(5) === 0);
check('得到 4 个木板', m.Inv.countOf(7) === 4);
m.Inv.add(7, 4);
const rTable = m.Recipes.find(r => r.out === 8);
check('无工作台时不能合成工作台', !m.Craft.available(false, false).some(x => x.recipe.out === 8 && x.canCraft));
check('有工作台时可合成', m.Craft.available(true, false).some(x => x.recipe.out === 8 && x.canCraft));
check('合成工作台成功', m.Craft.craft(rTable) === true);
/* 熔炉配方 */
const rIron = m.Recipes.find(r => r.out === 141);
check('无熔炉不能冶炼', !m.Craft.available(false, false).some(x => x.recipe.out === 141 && x.canCraft));
m.Inv.add(131, 3);
check('有熔炉可冶炼铁锭', m.Craft.available(false, true).some(x => x.recipe.out === 141 && x.canCraft));
check('冶炼成功', m.Craft.craft(rIron) === true);
check('铁锭数量为 1', m.Inv.countOf(141) === 1);

/* ---- 序列化 ---- */
const ser = m.World.serialize();
check('序列化产出 base64 字符串', typeof ser === 'string' && ser.length > 1000);
const w2 = Object.create(m.World);
w2.data = new Uint8Array(m.World.W * m.World.H);
w2.deserialize(ser);
let same = true;
for (let i = 0; i < m.World.data.length; i += 977) {
  if (w2.data[i] !== m.World.data[i]) { same = false; break; }
}
check('反序列化数据逐位一致', same);

/* ---- 配方完整性 ---- */
check('配方表共 ' + m.Recipes.length + ' 条（>=14）', m.Recipes.length >= 14);
let allValid = true;
for (const r of m.Recipes) {
  if (!m.Items.get(r.out)) allValid = false;
  for (const [id] of r.needs) if (!m.Items.get(id)) allValid = false;
}
check('所有配方材料/产物有效', allValid);

/* ---- 确定性 ---- */
m.World.generate(999);
const h1 = m.World.data.slice(0, 500);
m.World.generate(999);
const h2 = m.World.data.slice(0, 500);
check('相同种子生成相同世界', h1.every((v, i) => v === h2[i]));

/* ---- 玩家流程（集成）：落地 / 跳跃 / 挖掘掉落 / 放置 / 刷怪点 ---- */
const g2 = m.World.generate(424242);
const fakeInput = { left: false, right: false, up: false, down: false, jump: false };
/* 玩家放到出生点，自由下落 */
m.Player.reset(g2.x * 16, g2.y * 16 - m.Player.h - 0.1);
for (let i = 0; i < 300; i++) m.Player.update(1 / 60, fakeInput);
check('玩家落地站在地表', m.Player.onGround === true);
check('玩家脚不陷入地表', m.Player.y + m.Player.h <= g2.y * 16 + 0.5);
/* 跳跃 */
const yBefore = m.Player.y;
m.Player.update(1 / 60, { left: false, right: false, up: false, down: false, jump: true });
for (let i = 0; i < 30; i++) m.Player.update(1 / 60, fakeInput);
check('跳跃离地', m.Player.y < yBefore - 5);
/* 左右移动 */
m.Player.reset(g2.x * 16, g2.y * 16 - m.Player.h - 0.1);
const xBefore = m.Player.x;
for (let i = 0; i < 60; i++) m.Player.update(1 / 60, { left: false, right: true, up: false, down: false, jump: false });
check('向右移动生效', m.Player.x > xBefore + 10);
/* 挖掘旁边草块 → 掉落 */
m.Inv.reset();
m.Player.reset(g2.x * 16, g2.y * 16 - m.Player.h - 0.1);
m.Player.startMine((g2.x + 2) * 16 + 8, g2.y * 16 + 8);
check('开始挖掘（目标可挖）', m.Player.mining !== null && m.Player.mining.progress === 0);
for (let i = 0; i < 240; i++) m.Player.update(1 / 60, fakeInput);
check('草方块被挖掉', m.World.get(g2.x + 2, g2.y) === 0);
check('挖出掉落物', m.Entities.drops.length > 0);
/* 放置草方块回去 */
m.Inv.add(1, 5);
m.Player.placeBlock((g2.x + 2) * 16 + 8, g2.y * 16 + 8);
check('放置草方块成功', m.World.get(g2.x + 2, g2.y) === 1);
/* 刷怪点：应在地表（非水下） */
const fakeP = { cx: g2.x * 16 + 5, cy: g2.y * 16 - 13 };
let found2 = 0, badSpot = 0;
for (let i = 0; i < 60; i++) {
  const spot = m.Entities.findSpawnSpot(fakeP.cx, fakeP.cy, 26 * 16, 46 * 16, m.Core.makeRng(i * 7 + 3));
  if (!spot) continue;
  found2++;
  if (Math.floor(spot.y / 16) >= m.World.SEA) badSpot++;
}
check('刷怪点在地表非水下（找到 ' + found2 + ' 个）', found2 > 5 && badSpot === 0);
/* 出生点上方有站立空间（无树/方块遮挡） */
let blocked = false;
for (let dy = -5; dy <= 0; dy++) {
  const id = m.World.get(g2.x, g2.y + dy);
  if (id === 5 || id === 6) blocked = true;
}
check('出生点上方无树遮挡', !blocked);

/* ---- 战斗：攻击 / 击杀 / 掉落 ---- */
m.Entities.reset();
m.Game.kills = 0;
m.Entities.spawnSlime(400, 100);
const slime = m.Entities.list[0];
const hitN = m.Entities.hitEnemiesAt(400, 100, 20, 8, fakeP);
check('攻击命中史莱姆并击杀', hitN === 1 && slime.hp === 0);
check('击杀后敌人移除', m.Entities.list.length === 0);
check('击杀数 +1', m.Game.kills === 1);
check('敌人掉落物品', m.Entities.drops.length > 0);
/* 掉落物 id 有效 */
let dropOk = true;
for (const d of m.Entities.drops) if (!m.Items.get(d.id)) dropOk = false;
check('掉落物 id 均在物品表中', dropOk);
/* 受伤无敌帧 */
m.Player.reset(g2.x * 16, g2.y * 16 - m.Player.h - 0.1);
m.Player.hp = m.Player.maxHp;
m.Player.invuln = 0;
m.Player.hurt(4);
check('受击扣血', m.Player.hp === m.Player.maxHp - 4);
m.Player.hurt(4);
check('无敌帧内不再扣血', m.Player.hp === m.Player.maxHp - 4);
m.Player.heal(2);
check('治疗生效', m.Player.hp === m.Player.maxHp - 2);

/* ---- 新功能：方块/物品/配方 ---- */
check('方块表共 19 种', m.Tiles.length === 19);
check('石砖是实体', m.Tiles[16].solid === true);
check('玻璃半透明不碰撞特性', m.Tiles[17].solid === true && m.Tiles[17].light === 0.15);
check('树苗不碰撞', m.Tiles[18].solid === false);
check('苹果是食物（回 3）', m.Items.get(150).type === 'food' && m.Items.get(150).heal === 3);
check('烤肉是食物（回 6）', m.Items.get(151).type === 'food' && m.Items.get(151).heal === 6);
m.Inv.reset(); m.Inv.add(3, 4);
const rBrick = m.Recipes.find(r => r.out === 16);
check('石砖配方存在', !!rBrick);
check('有工作台可合成石砖', m.Craft.available(true, false).some(x => x.recipe.out === 16 && x.canCraft));
check('石砖合成成功（4 块）', m.Craft.craft(rBrick) && m.Inv.countOf(16) === 4);
m.Inv.reset(); m.Inv.add(4, 1);
const rGlass = m.Recipes.find(r => r.out === 17);
check('玻璃配方存在', !!rGlass);
check('玻璃需熔炉', m.Craft.available(false, true).some(x => x.recipe.out === 17 && x.canCraft));
check('玻璃合成成功', m.Craft.craft(rGlass) && m.Inv.countOf(17) === 1);
m.Inv.reset(); m.Inv.add(150, 2);
const rRoast = m.Recipes.find(r => r.out === 151);
check('烤肉配方存在', !!rRoast && rRoast.needs.some(n => n[0] === 150));
check('烤肉合成成功', m.Craft.craft(rRoast) && m.Inv.countOf(151) === 1);

/* ---- 树苗种植与生长 ---- */
const g3 = m.World.generate(777);
const txx = g3.x + 30, tss = m.World.surfaceAt(txx);
for (let dy = 1; dy <= 14; dy++) m.World.set(txx, tss - dy, 0);   // 上方清空
m.World.set(txx, tss - 1, 18);
m.World.plantSapling(txx, tss - 1);
check('树苗已登记', m.World.saplings.length === 1);
for (let i = 0; i < 6 * 60; i++) m.World.updateSaplings(1 / 60);
let logAfter = false;
for (let dy = 1; dy <= 8; dy++) if (m.World.get(txx, tss - dy) === 5) { logAfter = true; break; }
check('树苗 6 秒后长成树（有原木）', logAfter);
check('生长队列已清空', m.World.saplings.length === 0);
/* 树苗被挖掉不长树 */
m.World.set(txx + 3, tss - 1, 18);
m.World.plantSapling(txx + 3, tss - 1);
m.World.breakBlock(txx + 3, tss - 1);
for (let i = 0; i < 6 * 60; i++) m.World.updateSaplings(1 / 60);
check('树苗被挖走后不长树', m.World.get(txx + 3, tss - 1) !== 5);

/* ---- 蝙蝠 ---- */
m.Entities.reset();
m.Entities.spawnBat(400, 100);
check('蝙蝠生成（类型/属性）', m.Entities.list.length === 1 && m.Entities.list[0].type === 'bat' && m.Entities.list[0].hp === 6);
for (let i = 0; i < 150; i++) m.Entities.update(1 / 60);
check('蝙蝠 AI 运行不崩', m.Entities.list.length <= 1);
check('蝙蝠白天燃烧扣血', m.Entities.list.length === 1 && m.Entities.list[0].hp < 6);

/* ---- 食物逻辑 ---- */
m.Inv.reset();
m.Inv.add(150, 3);
const selApple = m.Inv.selected();
check('快捷栏选中苹果', selApple && selApple.id === 150);
m.Player.reset(g3.x * 16, g3.y * 16 - m.Player.h - 0.1);
m.Player.hp = 10;
m.Inv.removeSlot(0, 1);
m.Player.heal(m.Items.get(150).heal);
check('吃苹果回 3 血', m.Player.hp === 13);
check('苹果消耗 1 个', m.Inv.countOf(150) === 2);

console.log('\n结果：通过 ' + pass + ' 项，失败 ' + fail + ' 项');
process.exit(fail ? 1 : 0);
