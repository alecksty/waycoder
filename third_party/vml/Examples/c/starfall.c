/* starfall.c —— 《星陨》竖版太空射击（C 版），跑在手机端 VML 上
 *
 * 编译运行（手机 App 的 vml 工具）：
 *     vml run examples/c/starfall.c
 *
 * ## 操作：**单指拖动**，不用屏幕手柄
 *
 * 手指按住屏幕任意处，飞船就朝那儿飞（有阻尼跟随，不是硬贴）。自动开火，不用按开火键。
 *
 * ⚠ **这不是随便选的**：屏幕手柄在宿主侧只有一个按键槽（`DrawWindowPage._padDownKey`），
 * 按新键会先替旧键补一条 KeyUp ⇒ **两个键同时按做不到**。写 mario.c 时就栽在这上面
 * （"跑动中起跳"做不出来，跳不过悬崖）。射击游戏天生要"边移动边开火"，
 * 所以这里**彻底不用手柄、走触摸**，从根上避开那个限制。
 *
 * 顺带：屏幕上已有的东西不要在程序里再画一遍 —— 手柄区既然用不上，也就不用占那 140px。
 *
 * ## 视觉手段（都在既有绘图接口里，没有新接口）
 *
 * - **渐变**：`ui_gradient(id, radial, 色A, 色B, 归一化几何)` + `ui_rect_grad` / `ui_circle_grad`。
 *   几何是**千分之一（0..1000）**的归一化值，不是像素 —— 传像素会让渐变塌成纯色。
 * - **发光**：没有 blur 之类的接口，用"**同色多层递减透明度 + 递增半径**"叠出来。
 * - **星空视差**：三层星星、速度与亮度各不同，营造纵深。
 * - **粒子**：爆炸是 N 个小圆，半径与透明度随时间衰减。
 * - **多边形**：飞船/敌机都是 `ui_polygon`，顶点在 C 里算好（见下面"为什么不拼路径字符串"）。
 *
 * ## 为什么不拼 SVG 路径字符串（`ui_path`）
 *
 * `ui_path` 收的是**字符串**（`"M12 40 L..."`）。飞船每帧都在动，用它就得**每帧拼字符串**
 * —— 而 C 里拼字符串要么 `sprintf`（本环境格式化路径有已知问题），要么手写数字转字符串
 * 再拼接，既慢又容易出界。多边形直接收 int 数组、顶点在 C 里算，零字符串操作。
 * （静态不动的装饰图形才值得用 `ui_path`。）
 *
 * ## 三条 C 前端硬约束（同 tetris.c / mario.c）
 *
 * 1. ⚠ `${}` 只认局部变量 ⇒ syscall 一律走 `waycoder_ui.h` 包装函数。
 * 2. 全局变量与全局 int 数组读写都正常。
 * 3. ⚠ `#define` 不支持反斜杠续行；⚠ **`ui_polygon((int[]){…})` 复合字面量不支持且不报错**
 *    —— 顶点数组必须是**具名全局数组**。
 */

#include <waycoder_ui.h>
#include <stdlib.h>

/* ── 规模上限 ───────────────────────────────────────────── */

#define MAXB  20        /* 我方子弹 */
#define MAXE  10        /* 敌机 */
#define MAXP  70        /* 粒子 */
#define MAXS  54        /* 星星 */
#define TICK  33        /* 一拍 33ms ≈ 30fps */
#define STARS 3         /* 星空层数 */

/* ── 配色（0xAARRGGBB，alpha 在前）────────────────────────── */

#define C_SPACE_TOP  0xFF070B1C
#define C_SPACE_BOT  0xFF1A2A5C
#define C_NEB_A      0xFF7A3FB0
#define C_NEB_B      0xFF2FA8C8
#define C_STAR       0xFFFFFFFF
#define C_STAR_DIM   0x66FFFFFF
#define C_STAR_MID   0xAAFFFFFF
#define C_HULL       0xFFE8F6FF
#define C_HULL_EDGE  0xFF5CE1FF
#define C_ENGINE     0xFFFF8A2B
#define C_ENGINE_HOT 0xFFFFE9A8
#define C_BULLET     0xFF5CE1FF
#define C_BULLET_HOT 0xFFEAFDFF
#define C_FOE        0xFFFF3D6E
#define C_FOE_EDGE   0xFFFFB199
#define C_FOE_ALT    0xFFFFC24B
#define C_GOLD       0xFFFFD34D
#define C_HUD_BG     0xCC060A18
#define C_TEXT       0xFFDCE8FF
#define C_DIM        0xFF8FA3C8
#define C_WARN       0xFFFF6B6B

/* 分层透明度（预置常量，避免运行时算 alpha 时溢出 int）*/
#define A90  0xE6000000
#define A60  0x99000000
#define A40  0x66000000
#define A25  0x40000000
#define A12  0x1F000000

/* ── 正弦表（×1000，32 点一整圈）─────────────────────────────
 * 不用 math.h：本仓对前端浮点语义踩过坑，整数表确定、可比对。*/
int SIN[32] = {
        0,  195,  383,  556,  707,  831,  924,  981,
     1000,  981,  924,  831,  707,  556,  383,  195,
        0, -195, -383, -556, -707, -831, -924, -981,
    -1000, -981, -924, -831, -707, -556, -383, -195
};

int sin_i(int a) { return SIN[a & 31]; }
int cos_i(int a) { return SIN[(a + 8) & 31]; }   /* cos = sin 移 1/4 圈（8/32）*/

/* ── 状态 ───────────────────────────────────────────────── */

int W, H;                       /* 画布宽高 */
int frame;
int state;                      /* 0=玩 1=暂停 2=结束 */
int score, best, wave, lives, combo, comboTimer;
int shake;                      /* 震屏剩余帧数 */
int shx, shy;                   /* 本帧震屏偏移（只在绘制时加，不动游戏状态）*/

int px, py;                     /* 飞船中心 */
int tx, ty;                     /* 手指目标点 */
int touching;
int invuln;                     /* 无敌帧（复活后短暂） */
int fireCd;

int bx[MAXB], by[MAXB], balive[MAXB];
int ex_[MAXE], ey_[MAXE], eh_[MAXE], erot[MAXE], ekind[MAXE], ealive[MAXE], ecd[MAXE];
int ppx[MAXP], ppy[MAXP], pvx[MAXP], pvy[MAXP], plife[MAXP], pcol[MAXP], palive[MAXP];
int stx[MAXS], sty[MAXS], ssp[MAXS], slay[MAXS], ssz[MAXS];   /* +t=star，避开与局部 sx/sy 同名遮蔽 */

int shipPts[16];                /* 飞船多边形顶点（x,y 交替）—— 具名数组，不能用复合字面量 */
int foePts[16];                 /* 敌机多边形顶点 */
/* 飞船机身的**相对**顶点（以中心为原点，y 向上为负）。倾斜时按角度旋转后再加中心点 ——
 * 写成相对量是因为旋转必须绕中心做，绝对坐标转起来会绕到屏幕原点去。*/
int shipRel[14] = { 0,-18,  11,2,  4,0,  7,13,  -7,13,  -4,0,  -11,2 };

char nbuf[16];                  /* 数字转字符串暂存 */

/* ── 小工具 ─────────────────────────────────────────────── */

char* num_str(int v) {
    int i;
    int n;
    if (v < 0) { nbuf[0] = '-'; nbuf[1] = '0'; nbuf[2] = 0; return nbuf; }
    i = 0;
    if (v == 0) { nbuf[0] = '0'; nbuf[1] = 0; return nbuf; }
    while (v > 0) {
        nbuf[i] = '0' + (v % 10);
        i = i + 1;
        v = v / 10;
    }
    nbuf[i] = 0;
    n = 0;
    while (n < i / 2) {
        char t;
        t = nbuf[n];
        nbuf[n] = nbuf[i - 1 - n];
        nbuf[i - 1 - n] = t;
        n = n + 1;
    }
    return nbuf;
}

/* 发光：同色多层递减透明度 + 递增半径（没有 blur 接口，只能这么叠）*/
void glow(int cx, int cy, int r, int color) {
    ui_circle(cx, cy, r, (color & 0x00FFFFFF) | A12, 1, 0);
    ui_circle(cx, cy, (r * 2) / 3, (color & 0x00FFFFFF) | A25, 1, 0);
    ui_circle(cx, cy, r / 3, (color & 0x00FFFFFF) | A60, 1, 0);
}

int rnd(int n) { if (n <= 0) return 0; return ui_rand(n); }

/* ── 生成 / 复位 ────────────────────────────────────────── */

void spawn_particles(int x, int y, int count, int color, int power) {
    int i;
    int k;
    i = 0;
    while (i < count) {
        k = 0;
        while (k < MAXP) {
            if (palive[k] == 0) {
                palive[k] = 1;
                ppx[k] = x;
                ppy[k] = y;
                pvx[k] = rnd(2 * power + 1) - power;
                pvy[k] = rnd(2 * power + 1) - power;
                plife[k] = 14 + rnd(14);
                pcol[k] = color;
                break;
            }
            k = k + 1;
        }
        i = i + 1;
    }
}

void spawn_foe(void) {
    int i;
    i = 0;
    while (i < MAXE) {
        if (ealive[i] == 0) {
            ealive[i] = 1;
            ex_[i] = 30 + rnd(W - 60);
            ey_[i] = -40;
            eh_[i] = 18 + rnd(10);
            erot[i] = rnd(32);
            ekind[i] = rnd(3);              /* 0/1 直线 2 蛇形 */
            ecd[i] = 0;
            return;
        }
        i = i + 1;
    }
}

void reset_game(void) {
    int i;
    score = 0;
    wave = 1;
    lives = 3;
    combo = 0;
    comboTimer = 0;
    shake = 0;
    state = 0;
    frame = 0;
    px = W / 2;
    py = H - 120;
    tx = px;
    ty = py;
    touching = 0;
    invuln = 90;
    fireCd = 0;
    i = 0;
    while (i < MAXB) { balive[i] = 0; i = i + 1; }
    i = 0;
    while (i < MAXE) { ealive[i] = 0; i = i + 1; }
    i = 0;
    while (i < MAXP) { palive[i] = 0; i = i + 1; }
    i = 0;
    while (i < MAXS) {
        slay[i] = i % STARS;
        stx[i] = rnd(W);
        sty[i] = rnd(H);
        ssp[i] = 1 + slay[i] * 2;           /* 越远的层越慢 */
        ssz[i] = 1 + slay[i];
        i = i + 1;
    }
}

/* ── 音效（合成音单通道 ⇒ 一次只发一个音）───────────────── */

void sfx_shoot(void) { ui_beep(900, 22); }
void sfx_boom(void) { ui_beep(160, 110); }
void sfx_hit(void) { ui_beep(90, 260); ui_vibrate(140, 0); }
void sfx_wave(void) { ui_beep(1320, 90); }

/* ── 每拍 ───────────────────────────────────────────────── */

void fire(void) {
    int i;
    i = 0;
    while (i < MAXB) {
        if (balive[i] == 0) {
            balive[i] = 1;
            bx[i] = px;
            by[i] = py - 18;
            sfx_shoot();
            return;
        }
        i = i + 1;
    }
}

void hurt_player(void) {
    if (invuln > 0) return;
    lives = lives - 1;
    combo = 0;
    shake = 12;
    sfx_hit();
    spawn_particles(px, py, 26, C_FOE, 4);
    if (lives <= 0) { state = 2; return; }
    invuln = 90;
    px = W / 2;
    py = H - 120;
    tx = px;
    ty = py;
}

void tick(void) {
    int i;
    int k;
    int dx;
    int dy;
    int hitdx;
    int hity;

    if (state != 0) return;
    frame = frame + 1;
    if (shake > 0) shake = shake - 1;
    if (invuln > 0) invuln = invuln - 1;
    if (comboTimer > 0) { comboTimer = comboTimer - 1; if (comboTimer == 0) combo = 0; }

    /* ── 飞船跟随手指（阻尼，不是硬贴）── */
    dx = tx - px;
    dy = ty - py;
    px = px + dx / 5;
    py = py + dy / 5;
    if (px < 16) px = 16;
    if (px > W - 16) px = W - 16;
    if (py < 50) py = 50;
    if (py > H - 30) py = H - 30;

    /* ── 自动开火 ── */
    fireCd = fireCd - 1;
    if (fireCd <= 0) { fire(); fireCd = 7; }

    /* ── 子弹 ── */
    i = 0;
    while (i < MAXB) {
        if (balive[i] != 0) {
            by[i] = by[i] - 13;
            if (by[i] < -20) balive[i] = 0;
        }
        i = i + 1;
    }

    /* ── 敌机 ── */
    i = 0;
    while (i < MAXE) {
        if (ealive[i] != 0) {
            ey_[i] = ey_[i] + 2 + wave / 3;
            if (ekind[i] == 2) ex_[i] = ex_[i] + sin_i(frame / 3 + i * 7) / 3;
            erot[i] = erot[i] + 1;
            if (ex_[i] < 20) ex_[i] = 20;
            if (ex_[i] > W - 20) ex_[i] = W - 20;
            /* 漏过去：扣连击 */
            if (ey_[i] > H + 30) { ealive[i] = 0; combo = 0; }

            /* 与我方子弹 */
            k = 0;
            while (k < MAXB) {
                if (balive[k] != 0) {
                    hitdx = bx[k] - ex_[i];
                    hity = by[k] - ey_[i];
                    if (hitdx < 0) hitdx = -hitdx;
                    if (hity < 0) hity = -hity;
                    if (hitdx < eh_[i] + 6 && hity < eh_[i] + 6) {
                        balive[k] = 0;
                        ealive[i] = 0;
                        combo = combo + 1;
                        comboTimer = 90;
                        score = score + 10 + combo * 2;
                        shake = 4;
                        sfx_boom();
                        spawn_particles(ex_[i], ey_[i], 14,
                                        ekind[i] == 2 ? C_FOE_ALT : C_FOE, 3);
                        break;
                    }
                }
                k = k + 1;
            }

            /* 与飞船 */
            if (ealive[i] != 0) {
                hitdx = px - ex_[i];
                hity = py - ey_[i];
                if (hitdx < 0) hitdx = -hitdx;
                if (hity < 0) hity = -hity;
                if (hitdx < eh_[i] + 10 && hity < eh_[i] + 10) {
                    ealive[i] = 0;
                    spawn_particles(ex_[i], ey_[i], 12, C_FOE, 3);
                    hurt_player();
                    if (state != 0) return;
                }
            }
        }
        i = i + 1;
    }

    /* ── 粒子 ── */
    i = 0;
    while (i < MAXP) {
        if (palive[i] != 0) {
            ppx[i] = ppx[i] + pvx[i];
            ppy[i] = ppy[i] + pvy[i];
            pvx[i] = (pvx[i] * 9) / 10;
            pvy[i] = (pvy[i] * 9) / 10;
            plife[i] = plife[i] - 1;
            if (plife[i] <= 0) palive[i] = 0;
        }
        i = i + 1;
    }

    /* ── 星空 ── */
    i = 0;
    while (i < MAXS) {
        sty[i] = sty[i] + ssp[i];
        if (sty[i] > H) { sty[i] = -2; stx[i] = rnd(W); }
        i = i + 1;
    }

    /* ── 波次 ── */
    /* 出敌节奏：55 拍（≈1.8 秒）一个；第 2 波起再补一个错开的 ——
     * 原先是 90 拍一个，实测 16 秒才 5 个，画面太冷清。*/
    if (frame % 55 == 0) {
        spawn_foe();
        if (wave >= 2 && (frame % 110) == 55) spawn_foe();
        if (frame % 450 == 0) { wave = wave + 1; sfx_wave(); }
    }
}

/* ── 绘制 ───────────────────────────────────────────────── */

void draw_space(void) {
    /* ⚠⚠ **`ui_clear` 不是"可选的美化"，它是"重置这一帧"的唯一入口。**
     *
     * 宿主那边的语义是：所有绘制（含 `ui_gradient` 这条 DSL 指令）都往同一个图元表里
     * **追加**，只有 `ui_clear` 会把它清空（`VmlScene.Clear` → `_figures.Clear()`）。
     * 漏了它 ⇒ **每帧都叠在上一帧上面**：画面糊成一团（"看不清"），
     * 且图元表无限增长（一秒 170 个 → 十秒五万个）直到撑爆（"闪退"）。
     *
     * 本文件第一版就是这么错的：把"清屏"换成了"画渐变背景"，于是漏掉了清屏本身。
     * 渐变铺满全屏**看起来**像清了底，但那是"盖上去"不是"重置"。*/
    ui_clear(C_SPACE_TOP);

    /* 背景：纵向渐变（几何是千分之一的归一化值：从上到下）*/
    ui_gradient("bg", 0, C_SPACE_TOP, C_SPACE_BOT, 0, 0, 0, 1000);
    ui_rect_grad(0, 0, W, H, "bg", 0);

    /* 星云：两团径向渐变（随帧极缓慢"呼吸"，让静止画面也有生气）。
     *
     * ⚠⚠ **渐变几何是「千分之一」的归一化值（0..1000），不是像素！**
     * 第一版按像素传了 `W/3, H/4, W/2`（如 90,114,180）—— 宿主把 180 读成
     * "半径 = 包围盒的 18%"，于是焦点缩在左上角一小块、**其余全是 ColorB**，
     * 屏幕上就是**一个硬边深色圆盘**（用户实测「看不清是什么」）。
     * 径向居中只有一种正确写法：**500,500,500**（位置由 ui_circle_grad 的 cx,cy 给）。*/
    ui_gradient("neb1", 1, (C_NEB_A & 0x00FFFFFF) | A60, (C_SPACE_TOP & 0x00FFFFFF) | A90,
                500, 500, 500, 0);
    ui_circle_grad(W / 3, H / 4 + sin_i(frame / 16) * 8, W / 2, "neb1");
    ui_gradient("neb2", 1, (C_NEB_B & 0x00FFFFFF) | A40, (C_SPACE_TOP & 0x00FFFFFF) | A90,
                500, 500, 500, 0);
    ui_circle_grad((W * 3) / 4, (H * 2) / 3 + cos_i(frame / 20) * 10, W / 2, "neb2");
}

void draw_stars(void) {
    int i;
    int b;
    i = 0;
    while (i < MAXS) {
        /* 越近的层越亮越大；最远那层还会轻微闪烁 */
        if (slay[i] == 0) b = C_STAR_DIM;
        else if (slay[i] == 1) b = C_STAR_MID;
        else b = C_STAR;
        if (slay[i] == 0 && ((frame / 8 + i) & 1) == 0) b = (C_STAR & 0x00FFFFFF) | A25;
        ui_circle(stx[i], sty[i], ssz[i], b, 1, 0);
        i = i + 1;
    }
}

void draw_ship(void) {
    int cx;
    int cy;
    int i;
    int tilt;
    int c;
    int s;
    int rx;
    int ry;
    /* 无敌时闪烁：不画（不是画成半透明 —— 圆接口没有统一的 alpha 混色保证）*/
    if (invuln > 0 && ((invuln / 4) & 1) == 1) return;
    cx = px + shx;      /* 震屏只在**画**的时候加偏移 */
    cy = py + shy;

    /* 机身随横向速度倾斜 —— 一点"juice"，让移动有重量感。
     * 角度单位与正弦表同制（32 格一整圈，1 格 = 11.25°），限幅 ±2 格 ≈ ±22°。*/
    tilt = tx - px;
    if (tilt > 5) tilt = 5;
    if (tilt < -5) tilt = -5;
    tilt = tilt;                      /* 1px 偏移 ≈ 1 格角度，够明显又不夸张 */
    c = cos_i(tilt);
    s = sin_i(tilt);

    /* 引擎焰：跟着机身一起转，长度随帧抖动 */
    ry = 10;
    shipPts[0] = cx + ((-5) * c - ry * s) / 1000;
    shipPts[1] = cy + ((-5) * s + ry * c) / 1000;
    shipPts[2] = cx + (5 * c - ry * s) / 1000;
    shipPts[3] = cy + (5 * s + ry * c) / 1000;
    ry = 24 + sin_i(frame) / 60;
    shipPts[4] = cx + (0 * c - ry * s) / 1000;
    shipPts[5] = cy + (0 * s + ry * c) / 1000;
    glow(cx, cy + 16, 10, C_ENGINE);
    ui_polygon(shipPts, 3, C_ENGINE_HOT, C_ENGINE, 1, "");

    /* 机身：相对顶点按 tilt 旋转后落到画布上 */
    i = 0;
    while (i < 7) {
        rx = shipRel[i * 2];
        ry = shipRel[i * 2 + 1];
        shipPts[i * 2]     = cx + (rx * c - ry * s) / 1000;
        shipPts[i * 2 + 1] = cy + (rx * s + ry * c) / 1000;
        i = i + 1;
    }
    glow(cx, cy, 16, C_HULL_EDGE);
    ui_polygon(shipPts, 7, C_HULL, C_HULL_EDGE, 2, "");

    /* 座舱 */
    ui_circle(cx + (0 * c - (-4) * s) / 1000, cy + (0 * s + (-4) * c) / 1000, 3, C_HULL_EDGE, 1, 0);
}

void draw_foes(void) {
    int i;
    int fx;
    int fy;
    int k;
    int a;
    int r;
    int col;
    i = 0;
    while (i < MAXE) {
        if (ealive[i] != 0) {
            r = eh_[i];
            fx = ex_[i] + shx;
            fy = ey_[i] + shy;
            /* 八边形：顶点由旋转角算出来（不拼字符串，见文件头那段）*/
            k = 0;
            while (k < 8) {
                a = erot[i] + k * 4;          /* 45° = 4/32 圈 */
                foePts[k * 2] = fx + (r * cos_i(a)) / 1000;
                foePts[k * 2 + 1] = fy + (r * sin_i(a)) / 1000;
                k = k + 1;
            }
            col = ekind[i] == 2 ? C_FOE_ALT : C_FOE;
            glow(fx, fy, r, col);
            ui_polygon(foePts, 8, (col & 0x00FFFFFF) | A90, C_FOE_EDGE, 2, "");
            /* 核心 */
            ui_circle(fx, fy, r / 3, (col & 0x00FFFFFF) | A90, 1, 0);
            ui_circle(fx, fy, r / 5, C_HULL, 1, 0);
        }
        i = i + 1;
    }
}

void draw_bullets(void) {
    int i;
    i = 0;
    while (i < MAXB) {
        if (balive[i] != 0) {
            glow(bx[i] + shx, by[i] + shy, 7, C_BULLET);
            ui_rect(bx[i] + shx - 2, by[i] + shy - 9, 4, 16, C_BULLET, 1, 0, 2);
            ui_rect(bx[i] + shx - 1, by[i] + shy - 7, 2, 12, C_BULLET_HOT, 1, 0, 1);
        }
        i = i + 1;
    }
}

void draw_particles(void) {
    int i;
    int r;
    int a;
    i = 0;
    while (i < MAXP) {
        if (palive[i] != 0) {
            r = plife[i] / 3;
            if (r < 1) r = 1;
            if (plife[i] > 18) a = A90;
            else if (plife[i] > 10) a = A60;
            else if (plife[i] > 5) a = A40;
            else a = A25;
            ui_circle(ppx[i] + shx, ppy[i] + shy, r, (pcol[i] & 0x00FFFFFF) | a, 1, 0);
        }
        i = i + 1;
    }
}

/* 命数用的小飞船图标（独立函数：几何与主飞船不同，别混用同一张顶点表）*/
void ui_polygon_small(int cx, int cy) {
    int a[12];
    a[0] = cx;      a[1] = cy - 6;
    a[2] = cx + 5;  a[3] = cy + 2;
    a[4] = cx + 2;  a[5] = cy + 1;
    a[6] = cx + 3;  a[7] = cy + 5;
    a[8] = cx - 3;  a[9] = cy + 5;
    a[10] = cx - 2; a[11] = cy + 1;
    ui_polygon(a, 6, C_HULL, C_HULL_EDGE, 1, "");
}

void draw_hud(void) {
    int i;
    ui_rect(0, 0, W, 34, C_HUD_BG, 1, 0, 0);
    ui_set_font(13, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(10, 9, num_str(score));
    ui_set_font(10, 0, C_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(10, 23, "分数");

    ui_set_font(12, VML_FONT_BOLD, C_GOLD, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 9, "第");
    ui_set_font(12, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2 + 22, 9, num_str(wave));
    if (combo > 1) {
        ui_set_font(10, VML_FONT_BOLD, C_GOLD, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, 24, "连击");
    }

    /* 命：画成小飞船图标，比数字直观 */
    i = 0;
    while (i < lives && i < 5) {
        ui_polygon_small(W - 22 - i * 20, 17);
        i = i + 1;
    }

    if (state == 1) {
        ui_rect(0, H / 2 - 34, W, 68, 0xCC000000, 1, 0, 0);
        ui_set_font(22, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 22, "暂停");
        ui_set_font(12, 0, C_DIM, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 8, "按 START 继续");
    }
    if (state == 2) {
        ui_rect(0, H / 2 - 60, W, 120, 0xCC000000, 1, 0, 0);
        ui_set_font(24, VML_FONT_BOLD, C_WARN, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 40, "舰体损毁");
        ui_set_font(14, 0, C_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 4, "最终分数");
        ui_set_font(20, VML_FONT_BOLD, C_GOLD, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 22, num_str(score));
        ui_set_font(12, 0, C_DIM, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 50, "按 START 重新出击");
    }
}

void draw_all(void) {
    /* 震屏：只算一个偏移量，由各实体绘制函数在**画**的时候加上。
     * ⚠ 不要在这里改 px/py 再改回来 —— 绘制期改游戏状态，一旦重入或中途返回，
     *   状态就留在偏移过的位置上（这是"看着偶然、其实必错"的一类写法）。*/
    shx = 0;
    shy = 0;
    if (shake > 0) { shx = sin_i(frame * 5) / 90; shy = cos_i(frame * 7) / 90; }

    draw_space();
    draw_stars();
    draw_bullets();
    draw_foes();
    draw_particles();
    draw_ship();
    draw_hud();
    ui_present();
}

/* ── 输入 ───────────────────────────────────────────────── */

void on_key(int k) {
    if (k == VML_KEY_SELECT || k == VML_KEY_PAUSE) {
        if (state == 0) state = 1;
        else if (state == 1) state = 0;
        draw_all();
        return;
    }
    if (k == VML_KEY_ENTER) {
        reset_game();
        draw_all();
        return;
    }
}

/* ── 主循环 ─────────────────────────────────────────────── */

int main(void) {
    int msg[4];
    int t;
    int sw;
    int sh;
    int i;

    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;

    ui_win_open("星陨", sw, sh);
    ui_keep_on(1);

    W = sw;
    H = sh;

    /* 全部初始化为"空"，免得 reset 之前就画到未初始化的状态上 */
    i = 0;
    while (i < MAXB) { balive[i] = 0; i = i + 1; }
    i = 0;
    while (i < MAXE) { ealive[i] = 0; i = i + 1; }
    i = 0;
    while (i < MAXP) { palive[i] = 0; i = i + 1; }

    reset_game();
    draw_all();

    ui_timer_set(TICK, 0);

    ui_dlg_msg("星陨",
               "单指按住屏幕任意处拖动，飞船会跟过来；自动开火。"
               "打掉敌机攒连击，别被撞到。START 重开、SELECT 暂停。",
               VML_DLG_INFO);

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER) {
            tick();
            draw_all();
            continue;
        }

        /* 触摸：按下/移动都更新目标点，抬起则停在原地 */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            touching = 1;
            tx = msg[1];
            ty = msg[2];
            continue;
        }
        if (t == VML_MSG_TOUCHMOVE || t == VML_MSG_MOUSEMOVE) {
            if (touching != 0) { tx = msg[1]; ty = msg[2]; }
            continue;
        }
        if (t == VML_MSG_TOUCHUP || t == VML_MSG_MOUSEUP) {
            touching = 0;
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            if (msg[1] == VML_KEY_ESCAPE) break;
            on_key(msg[1]);
            continue;
        }
    }

    ui_keep_on(0);
    ui_win_close();
    return 0;
}
