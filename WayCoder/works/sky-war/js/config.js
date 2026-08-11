/* ============================================================
   config.js — 游戏全局配置（集中调参，改完刷新即生效）
   ============================================================ */
(function (G) {
  'use strict';

  G.Config = {

    /* ---------- 画布 ---------- */
    LOGIC_W: 480,          // 逻辑宽度
    LOGIC_H: 720,          // 逻辑高度

    /* ---------- 玩家 ---------- */
    PLAYER: {
      w: 42,               // 机身宽（碰撞用半径取 min/2 * 0.8）
      h: 46,
      speed: 270,          // 移动速度 px/s
      accel: 12,           // 平滑加速度（越大越跟手）
      fireInterval: 0.20,  // 射击间隔秒
      bulletSpeed: 640,
      lives: 3,
      invincible: 1.8,     // 复活无敌秒数
      hitFlash: 0.35,      // 受击红光闪秒数
      thrust: 0.16,        // 引擎尾焰粒子间隔秒
    },

    /* ---------- 玩家子弹 ---------- */
    PLAYER_BULLET: {
      speed: 640,
      r: 4,
      damage: 1,
      life: 2.2,           // 存活秒数
    },

    /* ---------- 敌机子弹 ---------- */
    ENEMY_BULLET: {
      speed: 210,
      r: 5,
      damage: 1,
      life: 4.5,
    },

    /* ---------- 敌机定义 ---------- */
    ENEMIES: {
      scout:   { hp: 1, speed: 135, score: 100, r: 12, fire: 0,        sway: 0,   drop: 0.05 },
      fighter: { hp: 2, speed: 92,  score: 200, r: 16, fire: 0.035,    sway: 1.6, drop: 0.10 },
      bomber:  { hp: 6, speed: 58,  score: 400, r: 22, fire: 0.012,    sway: 0,   drop: 0.18 },
      drone:   { hp: 1, speed: 185, score: 150, r: 11, fire: 0,        sway: 0,   drop: 0.03 },
    },

    BOSS: {
      hp: 220,             // 基础血量（随波次放大）
      speed: 26,
      r: 42,
      score: 5000,
      // 弹幕：周期秒 / 每次弹数 / 扇形角度(rad)
      barrageInterval: 2.2,
      barrageCount: 9,
      barrageSpread: 0.9,
      // 自机狙
      aimedInterval: 0.9,
    },

    /* ---------- 道具 ---------- */
    POWERUP: {
      fallSpeed: 90,
      kinds: ['power', 'power', 'power', 'shield', 'bomb'],  // 掉落权重
    },

    /* ---------- 波次 ---------- */
    WAVE: {
      spawnEvery: 1.0,     // 每波每 1.0s 出一个编队（可缩放）
      betweenWaves: 2.2,   // 波次间休息秒
      speedScale: 0.045,   // 每波速度放大系数
      hpScale: 0.09,       // 每波血量放大系数
      bossEvery: 5,        // 每 5 波一个 Boss
    },

    /* ---------- 计分 / 连击 ---------- */
    SCORE: {
      comboWindow: 2.2,    // 连击有效窗口秒
      comboBonus: 0.1,     // 每级连击加分比例 (x1.1, x1.2 ...)
      maxCombo: 10,
    },

    /* ---------- 背景 ---------- */
    BG: {
      layers: [
        { count: 60, speed: 18, size: 1, alpha: 0.35, color: '#7f9fd9' },
        { count: 40, speed: 42, size: 2, alpha: 0.55, color: '#b7d4ff' },
        { count: 22, speed: 78, size: 3, alpha: 0.85, color: '#ffffff' },
      ],
      meteorEvery: 5.5,    // 流星间隔秒
      nebulaAlpha: 0.5,
    },

    /* ---------- 特效 ---------- */
    FX: {
      shakeMax: 6,         // 屏幕震动最大像素
      shakeDecay: 8,       // 震动衰减速度
    },

    /* ---------- 音效开关 ---------- */
    SOUND: { enabled: true },

    /* ---------- 触屏 ---------- */
    TOUCH: {
      // 鼠标/触屏拖拽时飞机跟随的平滑系数
      follow: 10,
      // 拖拽灵敏度（像素），避免手抖
      deadzone: 6,
    },
  };

})(window.Game = window.Game || {});
