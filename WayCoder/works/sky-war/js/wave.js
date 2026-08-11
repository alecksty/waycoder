/* ============================================================
   wave.js — 波次调度器（生成编队事件队列 / Boss 波 / 难度缩放）
   ============================================================ */
(function (G) {
  'use strict';

  const U = G.Utils;
  const CFG = G.Config;

  class WaveManager {
    /**
     * @param {object} cb {
     *   onSpawnEnemy(type, x, y),
     *   onSpawnBoss(),
     * }
     */
    constructor(cb) {
      this.cb = cb || {};
      this.wave = 0;
      this.events = [];        // { time, spawn() }
      this.elapsed = 0;
      this.phase = 'prep';     // 'prep' 准备 | 'active' 进行中 | 'done' 结束
      this.prepTimer = 0;
      this.bossActive = false;
      this.bossPending = false;
    }

    /* ---------- 新波次 ---------- */
    startWave(wave) {
      this.wave = wave;
      this.events = [];
      this.elapsed = 0;
      this.phase = 'active';
      this.bossPending = false;
      this._buildWave(wave);
    }

    /* ---------- 生成事件列表 ---------- */
    _buildWave(wave) {
      const W = CFG.WAVE;
      let t = 0.6;   // 首波事件延迟

      // Boss 波：先播警报，然后生成 Boss
      if (wave % W.bossEvery === 0) {
        this.bossPending = true;
        this.events.push({
          time: 0.4,
          spawn: () => { if (this.cb.onBossWarn) this.cb.onBossWarn(); },
        });
        this.events.push({
          time: 1.6,
          spawn: () => { if (this.cb.onSpawnBoss) this.cb.onSpawnBoss(); },
        });
        t = 3.2;
      }

      // 普通编队数量随波次增长
      const formations = 4 + Math.min(8, Math.floor(wave * 0.9));
      for (let i = 0; i < formations; i++) {
        const type = this._pickType(wave);
        const pattern = U.randPick(['line', 'v', 'column', 'random']);
        const delay = U.rand(1.4, 2.4) - Math.min(0.6, wave * 0.03);
        this.events.push({ time: t, spawn: this._makeSpawn(type, pattern, wave) });
        t += Math.max(0.9, delay);
      }

      // 波次完成标记（最后一个小延迟，确保最后一批敌人入场）
      this.events.push({ time: t + 0.5, spawn: () => {} });
    }

    /* 敌机类型概率：随波次偏向重型 */
    _pickType(wave) {
      const w = Math.min(wave, 30);
      const pool = [];
      pool.push('scout', 'scout', 'scout');
      if (w >= 2) pool.push('fighter', 'fighter');
      if (w >= 3) pool.push('drone', 'drone');
      if (w >= 4) pool.push('bomber');
      if (w >= 7) pool.push('bomber', 'fighter');
      if (w >= 10) pool.push('bomber');
      return U.randPick(pool);
    }

    /* 编队生成工厂 */
    _makeSpawn(type, pattern, wave) {
      const W = CFG.LOGIC_W;
      return () => {
        const spawn = (x, y) => {
          if (this.cb.onSpawnEnemy) this.cb.onSpawnEnemy(type, x, y, wave);
        };
        switch (pattern) {
          case 'line': {
            const n = U.randInt(3, 5);
            const cx = U.rand(90, W - 90);
            const spacing = U.rand(46, 58);
            for (let i = 0; i < n; i++) {
              const x = cx + (i - (n - 1) / 2) * spacing;
              spawn(U.clamp(x, 24, W - 24), -30);
            }
            break;
          }
          case 'v': {
            const n = 5;
            const cx = U.rand(100, W - 100);
            const spacing = 30;
            for (let i = 0; i < n; i++) {
              const off = (i - 2) * spacing;
              spawn(U.clamp(cx + off, 24, W - 24), -30 - Math.abs(i - 2) * 26);
            }
            break;
          }
          case 'column': {
            const n = U.randInt(3, 4);
            const x = U.rand(60, W - 60);
            for (let i = 0; i < n; i++) {
              spawn(x, -30 - i * 52);
            }
            break;
          }
          case 'random': {
            const n = U.randInt(3, 4);
            for (let i = 0; i < n; i++) {
              spawn(U.rand(30, W - 30), -30 - U.rand(0, 90));
            }
            break;
          }
        }
      };
    }

    /* ---------- 更新 ---------- */
    update(dt, enemiesPresent) {
      if (this.phase === 'prep') {
        this.prepTimer -= dt;
        if (this.prepTimer <= 0) this.startWave(this.wave + 1);
        return;
      }
      if (this.phase !== 'active') return;

      // 触发到期事件
      this.elapsed += dt;
      while (this.events.length > 0 && this.events[0].time <= this.elapsed) {
        const ev = this.events.shift();
        ev.spawn();
      }

      // 波次完成判断：事件清空 && 场上没有普通敌机 && (无 boss 或 boss 已处理)
      const noEvents = this.events.length === 0;
      const noEnemies = !enemiesPresent();
      const bossDone = !this.bossPending && !this.bossActive;
      if (noEvents && noEnemies && bossDone) {
        this.phase = 'done';
      }
    }

    /* 波次结束回调（由 game 调用进入准备期） */
    finishWave() {
      this.phase = 'prep';
      this.prepTimer = CFG.WAVE.betweenWaves;
    }

    /* ---------- 辅助 ---------- */
    onBossSpawned() { this.bossActive = true; this.bossPending = false; }
    onBossKilled() { this.bossActive = false; }

    get isBossWave() { return this.wave > 0 && this.wave % CFG.WAVE.bossEvery === 0; }
  }

  G.WaveManager = WaveManager;

})(window.Game = window.Game || {});
