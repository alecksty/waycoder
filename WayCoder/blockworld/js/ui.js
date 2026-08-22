/*
 * ui.js —— 渲染层：方块/物品图标、HUD、背包合成界面、标题/暂停/死亡/帮助界面
 * 所有绘制基于当前 viewport (vw, vh)，并记录可点击区域供 Game 做命中测试
 */
'use strict';

const UI = {
  craftTab: 0,          // 0=徒手 1=工作台 2=熔炉
  /* 命中区域缓存（绘制时记录，点击时读取） */
  hitTitle: [],         // {id, x, y, w, h}
  hitPause: [],
  hitDead: [],
  hitInv: { slots: [], craft: [], tabs: [], craftBtn: null },
  hoverSlot: -1,
  hoverCraft: -1,

  resetHits() { this.hitTitle = []; this.hitPause = []; this.hitDead = []; this.hitInv = { slots: [], craft: [], tabs: [], craftBtn: null }; },

  /* ================= 图标绘制 ================= */
  /* 方块图标：size 为边长（像素） */
  drawBlockIcon(ctx, id, x, y, size) {
    const t = Tiles[id];
    if (!t) return;
    const rng = Core.makeRng(id * 7919 + size);
    ctx.fillStyle = t.color;
    ctx.fillRect(x, y, size, size);
    /* 纹理细节 */
    if (id === 1) { // 草：顶部深色条
      ctx.fillStyle = '#3e8f3e';
      ctx.fillRect(x, y, size, size * 0.28);
      ctx.fillStyle = 'rgba(0,0,0,0.15)';
      ctx.fillRect(x + size * 0.15, y + size * 0.35, size * 0.2, size * 0.12);
    } else if (id === 5) { // 原木：竖纹
      ctx.fillStyle = 'rgba(0,0,0,0.25)';
      ctx.fillRect(x + size * 0.35, y, size * 0.14, size);
    } else if (id === 10) { // 火把
      ctx.fillStyle = '#8a6a3a';
      ctx.fillRect(x + size * 0.42, y + size * 0.25, size * 0.16, size * 0.75);
      ctx.fillStyle = '#ffb840';
      ctx.fillRect(x + size * 0.28, y, size * 0.44, size * 0.3);
      ctx.fillStyle = '#ffe080';
      ctx.fillRect(x + size * 0.36, y - size * 0.06, size * 0.28, size * 0.2);
    } else if (id === 15) { // 水
      ctx.fillStyle = 'rgba(120,180,255,0.5)';
      ctx.fillRect(x, y, size, size);
      ctx.fillStyle = 'rgba(255,255,255,0.35)';
      ctx.fillRect(x + size * 0.2, y + size * 0.4, size * 0.4, size * 0.08);
    } else if (id === 9) { // 熔炉：洞口
      ctx.fillStyle = '#3a3a3a';
      ctx.fillRect(x + size * 0.2, y + size * 0.45, size * 0.6, size * 0.4);
      ctx.fillStyle = '#ff8830';
      ctx.fillRect(x + size * 0.32, y + size * 0.55, size * 0.36, size * 0.18);
    } else if (id === 8) { // 工作台：桌面纹理
      ctx.fillStyle = 'rgba(0,0,0,0.2)';
      ctx.fillRect(x, y + size * 0.62, size, size * 0.14);
      ctx.fillStyle = 'rgba(255,255,255,0.14)';
      ctx.fillRect(x, y + size * 0.18, size, size * 0.1);
    }
    if (t.dot) {
      ctx.fillStyle = t.dot;
      const n = id === 11 ? 4 : 3;
      for (let i = 0; i < n; i++) {
        const px = x + rng() * (size - size * 0.3);
        const py = y + rng() * (size - size * 0.3);
        ctx.fillRect(px, py, size * 0.18, size * 0.18);
      }
    }
    ctx.strokeStyle = 'rgba(0,0,0,0.4)';
    ctx.lineWidth = 1;
    ctx.strokeRect(x + 0.5, y + 0.5, size - 1, size - 1);
  },

  /* 物品图标：方块/剑/镐/材料 */
  drawItemIcon(ctx, itemId, x, y, size) {
    const def = Items.get(itemId);
    if (!def) return;
    if (def.type === 'block') { this.drawBlockIcon(ctx, def.tileId, x, y, size); return; }
    const c = def.color;
    if (def.type === 'sword') {
      ctx.save();
      ctx.translate(x + size / 2, y + size / 2);
      ctx.rotate(Math.PI / 4);
      ctx.fillStyle = c;
      ctx.fillRect(-size * 0.09, -size * 0.5, size * 0.18, size * 0.62);
      ctx.fillStyle = 'rgba(255,255,255,0.4)';
      ctx.fillRect(-size * 0.05, -size * 0.5, size * 0.05, size * 0.5);
      ctx.fillStyle = '#6b4a2f';
      ctx.fillRect(-size * 0.11, size * 0.12, size * 0.22, size * 0.34);
      ctx.restore();
    } else if (def.type === 'pick') {
      ctx.fillStyle = '#8a6a3a';
      ctx.fillRect(x + size * 0.44, y + size * 0.18, size * 0.12, size * 0.66);
      ctx.fillStyle = c;
      ctx.fillRect(x + size * 0.08, y + size * 0.12, size * 0.84, size * 0.18);
      ctx.fillRect(x + size * 0.14, y + size * 0.12, size * 0.16, size * 0.3);
      ctx.fillRect(x + size * 0.7, y + size * 0.12, size * 0.16, size * 0.3);
    } else if (def.type === 'food') { // 苹果/烤肉
      ctx.fillStyle = c;
      ctx.beginPath();
      ctx.arc(x + size / 2, y + size / 2, size * 0.32, 0, Core.TAU);
      ctx.fill();
      ctx.fillStyle = '#8a6a3a';
      ctx.fillRect(x + size * 0.46, y + size * 0.14, size * 0.08, size * 0.16);
      ctx.fillStyle = '#4aa84a';
      ctx.fillRect(x + size * 0.5, y + size * 0.08, size * 0.16, size * 0.09);
      ctx.fillStyle = 'rgba(255,255,255,0.5)';
      ctx.beginPath();
      ctx.arc(x + size * 0.4, y + size * 0.4, size * 0.09, 0, Core.TAU);
      ctx.fill();
    } else { // 材料：圆点 + 高光
      ctx.fillStyle = c;
      ctx.beginPath();
      ctx.arc(x + size / 2, y + size / 2, size * 0.32, 0, Core.TAU);
      ctx.fill();
      ctx.fillStyle = 'rgba(255,255,255,0.5)';
      ctx.beginPath();
      ctx.arc(x + size * 0.4, y + size * 0.38, size * 0.1, 0, Core.TAU);
      ctx.fill();
      ctx.strokeStyle = 'rgba(0,0,0,0.35)';
      ctx.lineWidth = 1;
      ctx.stroke();
    }
  },

  /* 物品格子（含数量角标） */
  drawSlot(ctx, slotIdx, x, y, size, highlight) {
    ctx.fillStyle = 'rgba(20,24,40,0.75)';
    ctx.fillRect(x, y, size, size);
    ctx.strokeStyle = highlight ? '#ffd75e' : 'rgba(255,255,255,0.25)';
    ctx.lineWidth = highlight ? 2 : 1;
    ctx.strokeRect(x + 0.5, y + 0.5, size - 1, size - 1);
    const s = Inv.slots[slotIdx];
    if (!s) return;
    this.drawItemIcon(ctx, s.id, x + 3, y + 3, size - 6);
    if (s.count > 1) {
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 12px monospace';
      ctx.textAlign = 'right';
      ctx.textBaseline = 'bottom';
      ctx.fillText(s.count, x + size - 3, y + size - 2);
    }
  },

  /* 心形 path */
  heartPath(ctx, x, y, s) {
    ctx.beginPath();
    ctx.moveTo(x, y + s * 0.35);
    ctx.bezierCurveTo(x - s * 1.1, y - s * 0.5, x - s * 0.55, y - s * 1.1, x, y - s * 0.35);
    ctx.bezierCurveTo(x + s * 0.55, y - s * 1.1, x + s * 1.1, y - s * 0.5, x, y + s * 0.35);
    ctx.closePath();
  },

  /* ================= HUD ================= */
  drawHUD(ctx, vw, vh) {
    const pad = 10;

    /* 生命心 */
    const hearts = Player.maxHp / 2;
    for (let i = 0; i < hearts; i++) {
      const hx = pad + i * 24, hy = pad;
      const val = Player.hp - i * 2;
      this.heartPath(ctx, hx, hy, 10);
      ctx.fillStyle = val >= 2 ? '#e84040' : val === 1 ? '#e89040' : '#3a3a3a';
      ctx.fill();
      if (val === 1) {
        this.heartPath(ctx, hx, hy, 10);
        ctx.fillStyle = '#3a3a3a';
        ctx.beginPath();
        ctx.arc(hx, hy + 2, 6, 0, Core.TAU);
        ctx.fill();
      }
      ctx.strokeStyle = 'rgba(0,0,0,0.6)';
      ctx.lineWidth = 1.5;
      this.heartPath(ctx, hx, hy, 10);
      ctx.stroke();
    }
    /* 击杀 / 坐标 */
    ctx.fillStyle = 'rgba(255,255,255,0.85)';
    ctx.font = '13px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'top';
    ctx.fillText('击杀 ' + Game.kills, pad, pad + 26);
    ctx.fillText('X ' + Math.floor(Player.cx / Core.BLOCK) + '  Y ' + Math.floor(Player.cy / Core.BLOCK), pad, pad + 44);

    /* 右上：日夜 + 时间 */
    const t = Game.timeOfDay;   // 0..1
    const night = Game.isNight;
    ctx.save();
    ctx.translate(vw - 90, pad + 10);
    if (!night) { // 太阳
      const g = ctx.createRadialGradient(0, 0, 4, 0, 0, 16);
      g.addColorStop(0, '#fff8d0'); g.addColorStop(1, '#ffd060');
      ctx.fillStyle = g;
      ctx.beginPath(); ctx.arc(0, 0, 12, 0, Core.TAU); ctx.fill();
    } else { // 月亮
      ctx.fillStyle = '#e8ecf8';
      ctx.beginPath(); ctx.arc(0, 0, 10, 0, Core.TAU); ctx.fill();
      ctx.fillStyle = '#b8c0d8';
      ctx.beginPath(); ctx.arc(4, -3, 8, 0, Core.TAU); ctx.fill();
    }
    ctx.restore();
    ctx.fillStyle = 'rgba(255,255,255,0.9)';
    ctx.font = 'bold 13px monospace';
    ctx.textAlign = 'right';
    ctx.fillText('第 ' + Game.day + ' 天  ' + (night ? '夜晚' : '白天'), vw - 14, pad + 34);
    ctx.font = '12px monospace';
    ctx.fillStyle = 'rgba(255,255,255,0.65)';
    ctx.fillText(Core.formatTime(Game.t) + ' / ' + Core.formatTime(Game.cycle), vw - 14, pad + 50);

    /* 左下：快捷栏 */
    const barW = 9 * 50 + 4, barX = (vw - barW) / 2, barY = vh - 58;
    for (let i = 0; i < 9; i++) {
      this.drawSlot(ctx, i, barX + i * 50, barY, 46, i === Game.hotbarSel);
      if (i === Game.hotbarSel) {
        ctx.fillStyle = '#ffd75e';
        ctx.font = 'bold 10px monospace';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'bottom';
        ctx.fillText(String(i + 1), barX + i * 50 + 23, barY - 2);
      }
    }
    /* 右下角 fps */
    ctx.fillStyle = 'rgba(255,255,255,0.45)';
    ctx.font = '11px monospace';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'alphabetic';
    ctx.fillText(Game.fps.value + ' fps', vw - 8, vh - 8);
  },

  /* ================= 背包 + 合成界面 ================= */
  /* 布局：左侧 4x9 物品格，右侧合成区 */
  invLayout(vw, vh) {
    const pw = 720, ph = 480;
    const px = Math.round(vw / 2 - pw / 2), py = Math.round(vh / 2 - ph / 2);
    const cell = 46, gap = 5;
    const x0 = px + 20, y0 = py + 52;
    const slots = [];
    for (let i = 0; i < Inv.SLOT_COUNT; i++) {
      const row = Math.floor(i / 9), col = i % 9;
      slots[i] = { x: x0 + col * (cell + gap), y: y0 + row * (cell + gap), w: cell, h: cell };
    }
    const craftX = x0 + 9 * (cell + gap) + 18;
    const craftW = px + pw - 16 - craftX;
    return { px, py, pw, ph, slots, craftX, craftW, y0 };
  },

  drawInventory(ctx, vw, vh) {
    const lay = this.invLayout(vw, vh);
    this.hoverSlot = -1;
    this.hoverCraft = -1;
    ctx.fillStyle = 'rgba(6,10,26,0.82)';
    ctx.fillRect(0, 0, vw, vh);

    /* 面板 */
    ctx.fillStyle = 'rgba(24,30,52,0.96)';
    ctx.fillRect(lay.px, lay.py, lay.pw, lay.ph);
    ctx.strokeStyle = '#ffd75e';
    ctx.lineWidth = 2;
    ctx.strokeRect(lay.px, lay.py, lay.pw, lay.ph);

    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 20px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('背包', lay.px + 20, lay.py + 26);

    /* 物品格 */
    const hit = { slots: [], craft: [], tabs: [], craftBtn: null };
    for (let i = 0; i < Inv.SLOT_COUNT; i++) {
      const r = lay.slots[i];
      const sel = i === Game.hotbarSel && Game.state === 'inventory';
      this.drawSlot(ctx, i, r.x, r.y, r.w, sel);
      /* 悬停检测 */
      const m = Game.mouse;
      if (m.sx >= r.x && m.sx < r.x + r.w && m.sy >= r.y && m.sy < r.y + r.h) {
        this.hoverSlot = i;
        ctx.strokeStyle = 'rgba(255,255,255,0.7)';
        ctx.lineWidth = 1;
        ctx.strokeRect(r.x + 0.5, r.y + 0.5, r.w - 1, r.h - 1);
      }
      hit.slots[i] = r;
    }

    /* 合成区 */
    const hasWb = Game.hasWorkbench, hasFu = Game.hasFurnace;
    const tabs = [
      { label: '徒手', enabled: true },
      { label: '工作台', enabled: hasWb },
      { label: '熔炉', enabled: hasFu }
    ];
    const tx = lay.craftX, ty = lay.y0;
    for (let i = 0; i < 3; i++) {
      const bx = tx + i * 84;
      const active = this.craftTab === i;
      ctx.fillStyle = !tabs[i].enabled ? 'rgba(60,60,70,0.6)' : active ? '#ffd75e' : 'rgba(90,100,140,0.8)';
      ctx.fillRect(bx, ty, 78, 30);
      ctx.strokeStyle = 'rgba(255,255,255,0.3)';
      ctx.strokeRect(bx, ty, 78, 30);
      ctx.fillStyle = !tabs[i].enabled ? '#666' : active ? '#1a1a2a' : '#eee';
      ctx.font = 'bold 13px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(tabs[i].label, bx + 39, ty + 16);
      hit.tabs[i] = { x: bx, y: ty, w: 78, h: 30 };
    }

    /* 可用配方网格 */
    const recipes = Craft.available(hasWb, hasFu).filter(r => r.recipe.station === this.craftTab);
    const cy = ty + 44, cw = 44, cg = 4;
    const show = recipes.slice(0, 8);
    for (let i = 0; i < 8; i++) {
      const cx2 = tx + (i % 2) * (cw + cg), cy2 = cy + Math.floor(i / 2) * (cw + cg);
      const entry = show[i];
      ctx.fillStyle = 'rgba(10,14,30,0.7)';
      ctx.fillRect(cx2, cy2, cw, cw);
      ctx.strokeStyle = entry && entry.canCraft ? 'rgba(120,220,120,0.8)' : 'rgba(255,255,255,0.2)';
      ctx.lineWidth = entry && entry.canCraft ? 1.5 : 1;
      ctx.strokeRect(cx2, cy2, cw, cw);
      if (entry) {
        this.drawItemIcon(ctx, entry.recipe.out, cx2 + 5, cy2 + 5, cw - 10);
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 11px monospace';
        ctx.textAlign = 'right';
        ctx.textBaseline = 'bottom';
        ctx.fillText('x' + entry.recipe.outCount, cx2 + cw - 3, cy2 + cw - 3);
      }
      hit.craft[i] = { x: cx2, y: cy2, w: cw, h: cw, entry: entry || null };
      /* 悬停检测 */
      const m = Game.mouse;
      if (m.sx >= cx2 && m.sx < cx2 + cw && m.sy >= cy2 && m.sy < cy2 + cw) this.hoverCraft = i;
    }

    /* 选中配方详情 + 合成按钮 */
    const selCraft = this.hoverCraft >= 0 ? hit.craft[this.hoverCraft] : null;
    if (selCraft && selCraft.entry) {
      const r = selCraft.entry.recipe;
      const dx = tx, dy = cy + 2 * (cw + cg) + 6;
      ctx.fillStyle = 'rgba(10,14,30,0.85)';
      ctx.fillRect(dx, dy, 300, 86);
      ctx.strokeStyle = 'rgba(255,255,255,0.2)';
      ctx.strokeRect(dx, dy, 300, 86);
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 14px monospace';
      ctx.textAlign = 'left';
      ctx.fillText(Items.nameOf(r.out) + ' x' + r.outCount, dx + 10, dy + 20);
      let nx = dx + 10;
      for (const [id, n] of r.needs) {
        this.drawItemIcon(ctx, id, nx, dy + 30, 20);
        ctx.fillStyle = Inv.countOf(id) >= n ? '#8f8' : '#f88';
        ctx.font = '11px monospace';
        ctx.textAlign = 'left';
        ctx.fillText('x' + n, nx + 22, dy + 44);
        nx += 56;
      }
      const btn = { x: dx + 10, y: dy + 58, w: 120, h: 22 };
      ctx.fillStyle = selCraft.entry.canCraft ? '#4a9e4a' : '#555';
      ctx.fillRect(btn.x, btn.y, btn.w, btn.h);
      ctx.strokeStyle = 'rgba(255,255,255,0.35)';
      ctx.strokeRect(btn.x, btn.y, btn.w, btn.h);
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 12px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(selCraft.entry.canCraft ? '合成' : '材料不足', btn.x + 60, btn.y + 12);
      hit.craftBtn = btn;
    }

    /* 拖拽中的物品跟随鼠标 */
    if (Inv.held) {
      const mx = Game.mouse.sx, my = Game.mouse.sy;
      this.drawItemIcon(ctx, Inv.held.id, mx - 12, my - 12, 24);
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 12px monospace';
      ctx.textAlign = 'left';
      ctx.fillText(Inv.held.count, mx + 6, my + 10);
    }
    this.hitInv = hit;
  },

  /* ================= 标题界面 ================= */
  drawTitle(ctx, vw, vh) {
    this.hitTitle = [];
    /* 背景：夜空渐变 + 装饰方块 */
    const g = ctx.createLinearGradient(0, 0, 0, vh);
    g.addColorStop(0, '#0a0e2a');
    g.addColorStop(0.5, '#14204a');
    g.addColorStop(1, '#2a2a4a');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, vw, vh);
    /* 星 */
    const rng = Core.makeRng(7);
    for (let i = 0; i < 60; i++) {
      ctx.fillStyle = 'rgba(255,255,255,' + (0.3 + rng() * 0.7) + ')';
      ctx.fillRect(rng() * vw, rng() * vh * 0.6, 2, 2);
    }
    /* 地面方块带 */
    for (let i = 0; i < vw / 16 + 1; i++) {
      const ids = [1, 2, 3, 5, 11, 12, 7, 8];
      this.drawBlockIcon(ctx, ids[i % ids.length], i * 16 - 8, vh - 90 + ((i * 7) % 24), 16);
    }

    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.font = 'bold 64px monospace';
    ctx.fillStyle = '#ffd75e';
    ctx.shadowColor = 'rgba(0,0,0,0.8)';
    ctx.shadowBlur = 12;
    ctx.fillText('方 块 世 界', vw / 2, vh * 0.24);
    ctx.shadowBlur = 0;
    ctx.font = '18px monospace';
    ctx.fillStyle = 'rgba(255,255,255,0.75)';
    ctx.fillText('— BlockWorld 2D Sandbox —  挖矿 · 建造 · 合成 · 生存', vw / 2, vh * 0.24 + 46);

    /* 按钮 */
    const hasSave = Game.hasSave;
    const btns = [
      { id: 'continue', label: '继续游戏', enabled: hasSave },
      { id: 'new', label: '新的世界', enabled: true },
      { id: 'help', label: '玩法说明', enabled: true }
    ];
    let by = vh * 0.45;
    for (const b of btns) {
      const w = 260, h = 46;
      const bx = vw / 2 - w / 2;
      ctx.fillStyle = b.enabled ? 'rgba(40,60,110,0.9)' : 'rgba(40,40,50,0.6)';
      ctx.fillRect(bx, by, w, h);
      ctx.strokeStyle = b.enabled ? '#ffd75e' : 'rgba(255,255,255,0.2)';
      ctx.lineWidth = 1.5;
      ctx.strokeRect(bx, by, w, h);
      ctx.fillStyle = b.enabled ? '#fff' : '#666';
      ctx.font = 'bold 18px monospace';
      ctx.fillText(b.label, vw / 2, by + h / 2 + 1);
      this.hitTitle.push({ id: b.id, x: bx, y: by, w, h });
      by += h + 14;
    }

    ctx.font = '12px monospace';
    ctx.fillStyle = 'rgba(255,255,255,0.5)';
    ctx.fillText('WASD/方向键移动 · 空格跳跃 · 左键挖掘/攻击 · 右键放置 · E 背包 · F5 保存 · Esc 菜单', vw / 2, vh - 24);
  },

  /* ================= 暂停 / 死亡 / 帮助 ================= */
  drawPause(ctx, vw, vh) {
    this.hitPause = [];
    ctx.fillStyle = 'rgba(4,8,20,0.72)';
    ctx.fillRect(0, 0, vw, vh);
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 34px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('暂  停', vw / 2, vh * 0.28);

    const btns = [
      { id: 'resume', label: '继续游戏' },
      { id: 'save', label: '保存游戏' },
      { id: 'title', label: '保存并返回标题' },
      { id: 'quit', label: '放弃并返回标题' }
    ];
    let by = vh * 0.42;
    for (const b of btns) {
      const w = 240, h = 42;
      const bx = vw / 2 - w / 2;
      ctx.fillStyle = 'rgba(40,60,110,0.9)';
      ctx.fillRect(bx, by, w, h);
      ctx.strokeStyle = '#ffd75e';
      ctx.lineWidth = 1.5;
      ctx.strokeRect(bx, by, w, h);
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 16px monospace';
      ctx.fillText(b.label, vw / 2, by + h / 2 + 1);
      this.hitPause.push({ id: b.id, x: bx, y: by, w, h });
      by += h + 12;
    }
  },

  drawDead(ctx, vw, vh) {
    this.hitDead = [];
    ctx.fillStyle = 'rgba(30,4,8,0.78)';
    ctx.fillRect(0, 0, vw, vh);
    ctx.fillStyle = '#ff5a5a';
    ctx.font = 'bold 44px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('你 死 了', vw / 2, vh * 0.3);
    ctx.fillStyle = 'rgba(255,255,255,0.8)';
    ctx.font = '16px monospace';
    ctx.fillText('坚持了 ' + Core.formatTime(Game.t) + '，击杀 ' + Game.kills + ' 个敌人', vw / 2, vh * 0.3 + 34);

    const btns = [
      { id: 'respawn', label: '重生（保留背包）' },
      { id: 'title', label: '返回标题' }
    ];
    let by = vh * 0.5;
    for (const b of btns) {
      const w = 240, h = 44;
      const bx = vw / 2 - w / 2;
      ctx.fillStyle = 'rgba(120,50,60,0.9)';
      ctx.fillRect(bx, by, w, h);
      ctx.strokeStyle = '#ff8a6a';
      ctx.lineWidth = 1.5;
      ctx.strokeRect(bx, by, w, h);
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 17px monospace';
      ctx.fillText(b.label, vw / 2, by + h / 2 + 1);
      this.hitDead.push({ id: b.id, x: bx, y: by, w, h });
      by += h + 14;
    }
  },

  drawHelp(ctx, vw, vh) {
    ctx.fillStyle = 'rgba(6,10,26,0.9)';
    ctx.fillRect(0, 0, vw, vh);
    ctx.fillStyle = '#ffd75e';
    ctx.font = 'bold 28px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('玩 法 说 明', vw / 2, 60);

    const lines = [
      '【目标】砍树 → 合成木板/木棒 → 做工作台 → 做镐 → 挖矿 → 冶炼 → 打造高级装备',
      '【移动】WASD 或 方向键，空格跳跃，S/↓ 在水中下潜',
      '【挖掘】鼠标左键按住挖方块（镐挖得更快），右键放置选中方块',
      '【战斗】左键点击敌人挥剑（装备剑伤害更高），小心僵尸与史莱姆',
      '【背包】E 打开，点击物品拿起/放下，合成在右侧选择配方',
      '【合成】徒手配方随时可用；工作台/熔炉需放置在工作台/熔炉旁边',
      '【日夜】白天 10 分钟，夜晚 5 分钟——夜晚僵尸会刷新，记得点火把',
      '【存档】F5 快速保存，Esc 菜单可保存/退出（自动存档每 30 秒）',
      '【食物】砍树叶有机会掉树苗/苹果，F 吃苹果回血，苹果可烤成烤肉(回6)',
      '【种植】树苗右键种下，6 秒后长成大树——木材可再生！',
      '【建材】石砖(石头x4)坚固耐造，玻璃用熔炉烧沙子',
      '【提示】火把需要煤炭：先做木镐挖煤矿石！出生点已给你 8 根火把',
      '',
      '按 H 或 Esc 关闭本说明'
    ];
    ctx.textAlign = 'center';
    ctx.font = '15px monospace';
    ctx.fillStyle = 'rgba(255,255,255,0.92)';
    let y = 120;
    for (const ln of lines) { ctx.fillText(ln, vw / 2, y); y += 30; }
  }
};
