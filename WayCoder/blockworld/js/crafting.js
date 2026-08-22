/*
 * crafting.js —— 合成配方表与合成逻辑
 * 配方按工作台分类：
 *   station 0 = 徒手（无需设施）
 *   station 1 = 需要工作台
 *   station 2 = 需要熔炉（冶炼）
 * 合成时从背包消耗材料并产出物品
 */
'use strict';

const Recipes = [];
(function buildRecipes() {
  const add = (station, out, outCount, needs) => {
    Recipes.push({ station, out, outCount, needs });
  };

  /* ---- 徒手 ---- */
  add(0, 7,  4, [[5, 1]]);                    // 原木 → 木板x4
  add(0, 120, 4, [[7, 1]]);                   // 木板 → 木棒x4
  add(0, 10, 4, [[130, 1], [120, 1]]);        // 煤+木棒 → 火把x4

  /* ---- 工作台 ---- */
  add(1, 8,  1, [[7, 4]]);                    // 工作台
  add(1, 9,  1, [[3, 8]]);                    // 熔炉
  add(1, 100, 1, [[7, 2], [120, 1]]);         // 木剑
  add(1, 110, 1, [[7, 3], [120, 2]]);         // 木镐
  add(1, 101, 1, [[3, 2], [120, 1]]);         // 石剑
  add(1, 111, 1, [[3, 3], [120, 2]]);         // 石镐
  add(1, 16,  4, [[3, 4]]);                   // 石头x4 → 石砖x4

  /* ---- 熔炉（冶炼 + 高级工具） ---- */
  add(2, 141, 1, [[131, 1]]);                 // 铁矿石 → 铁锭
  add(2, 142, 1, [[132, 1]]);                 // 金矿石 → 金锭
  add(2, 102, 1, [[141, 2], [120, 1]]);       // 铁剑
  add(2, 112, 1, [[141, 3], [120, 2]]);       // 铁镐
  add(2, 103, 1, [[133, 2], [120, 1]]);       // 钻石剑
  add(2, 113, 1, [[133, 3], [120, 2]]);       // 钻石镐
  add(2, 17,  1, [[4, 1]]);                   // 沙子 → 玻璃
  add(2, 151, 1, [[150, 2]]);                 // 苹果x2 → 烤肉
})();

const Craft = {
  /* 当前可用配方（按设施过滤），并附加能否合成的标记 */
  available(hasWorkbench, hasFurnace) {
    return Recipes.map(r => {
      let ok = true;
      if (r.station === 1 && !hasWorkbench) ok = false;
      if (r.station === 2 && !hasFurnace) ok = false;
      if (ok) {
        for (const [id, n] of r.needs) {
          if (Inv.countOf(id) < n) { ok = false; break; }
        }
      }
      return { recipe: r, canCraft: ok };
    });
  },

  /* 执行合成，成功返回 true */
  craft(recipe) {
    for (const [id, n] of recipe.needs) {
      if (Inv.countOf(id) < n) return false;
    }
    for (const [id, n] of recipe.needs) Inv.consume(id, n);
    Inv.add(recipe.out, recipe.outCount);
    return true;
  }
};
