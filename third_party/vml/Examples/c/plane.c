/* plane.c —— 《长空》竖版飞机空战（C 版），跑在手机端 VML 上
 * plane.c -- "Sky", a vertical plane shooter (C version) running on mobile VML
 *
 * 编译运行（手机 App 的 vml 工具）：
 * Build and run (with the vml tool in the phone app):
 *     vml run examples/c/plane.c
 *
 * ## 操作：**只有手指**，不要手柄
 * ## Controls: **finger only**, no gamepad
 *
 * 手指按住屏幕任意处，飞机就朝那儿飞（带阻尼跟随，不是硬贴手指）。自动开火。
 * Hold a finger anywhere on the screen and the plane flies there (damped follow, not glued to the finger). It fires automatically.
 *
 * ⚠ 开窗时声明了 `VML_WIN_NO_GAMEPAD`：**整块手柄区连同折叠条都不显示**，
 * ⚠ The window is opened with `VML_WIN_NO_GAMEPAD`: **the whole gamepad area and its collapse bar are hidden**,
 * 画布吃满整屏 —— 用不上的东西不要占地方（屏幕手势区横着能多出 140px）。
 * so the canvas fills the whole screen -- unused things should not take up room (the touch area gains 140px horizontally).
 * 同时声明 `VML_WIN_PORTRAIT` 锁竖屏：这游戏是竖版，转横向反而不好玩。
 * It also declares `VML_WIN_PORTRAIT` to lock portrait: this game is vertical, landscape is less fun.
 *
 * ## 画面是怎么"好看"的（全部用既有绘图接口，没有新接口）
 * ## How the picture is made to look good (all with existing draw APIs, no new ones)
 *
 * - **天空渐变**：`ui_gradient` + `ui_rect_grad`，一天由浅到深的过渡
 * - **Sky gradient**: `ui_gradient` + `ui_rect_grad`, a light-to-deep transition
 * - **云层视差**：三层云、速度与透明度各不同 —— 纵深就有了
 * - **Cloud parallax**: three layers, each with its own speed and alpha -- that gives depth
 * - **飞机是画出来的**：机身/机翼/尾翼用 `ui_polygon` + `ui_ellipse` 拼，
 * - **The plane is drawn**: body, wings and tail assembled from `ui_polygon` + `ui_ellipse`,
 *   **转弯时机身倾斜**（机翼顶点随横移量左右偏）—— 一个细节就让"纸片"变成"在飞"
 *   **the body banks when turning** (wing vertices shift with the horizontal velocity) -- one detail turns a flat cutout into a plane in flight
 * - **螺旋桨**：机头三片桨叶按时间旋转
 * - **Propeller**: three blades at the nose, rotating with time
 * - **发光**：没有 blur 接口，用「同色多层递减透明度 + 递增半径」叠（枪口焰、爆炸）
 * - **Glow**: there is no blur API, so stack the same color with decreasing alpha and growing radius (muzzle flash, explosions)
 * - **爆炸**：N 个粒子圆，半径与透明度随时间衰减
 * - **Explosion**: N particle circles whose radius and alpha decay over time
 *
 * ## 三条 C 前端硬约束（与 tetris.c / starfall.c 相同）
 * ## Three hard constraints of the C front end (same as tetris.c / starfall.c)
 *
 * 1. ⚠ `${}` 只认局部变量 ⇒ syscall 一律走 `waycoder_ui.h` 的包装函数
 * 1. ⚠ `${}` only recognizes local variables => always use the `waycoder_ui.h` wrapper functions for syscalls
 * 2. ⚠ **`ui_polygon((int[]){…})` 复合字面量不支持、也不报错** ⇒ 顶点必须是**具名全局数组**
 * 2. ⚠ **`ui_polygon((int[]){...})` compound literals are not supported and not reported** => vertices must be a **named global array**
 * 3. ⚠ `#define` 不支持反斜杠续行，多行宏要写成单行
 * 3. ⚠ `#define` does not support backslash continuation; a multi-line macro must be written on one line
 *
 * 另：**不用 `sprintf`**（本环境格式化有已知问题），分数自己转字符串（见 `itoa_`）。
 * Also: **no `sprintf`** (formatting has known issues here); convert the score to a string yourself (see `itoa_`).
 */

#include <waycoder_ui.h>

/* ── 规模 ───────────────────────────────────────────────── */
/* -- Scale -- */

#define MAXEB  24      /* 敌弹 */
                       /* Enemy bullets. */
#define MAXE    8      /* 敌机 */
                       /* Enemy planes. */
#define MAXP   64      /* 粒子 */
                       /* Particles. */
#define MAXC    9       /* 云 */
                        /* Clouds. */
#define TICK   33       /* 一拍 33ms ≈ 30fps */
                        /* One tick is 33ms, about 30fps. */

/* ── 配色（0xAARRGGBB，alpha 在前）────────────────────────── */
/* -- Colors (0xAARRGGBB, alpha first) -- */

#define SKY_TOP    0xFF2E6FC4
#define SKY_BOT    0xFFBEE3F7
#define CLOUD      0xFFFFFFFF
#define SEA_A      0xFF1E5E8C
#define SEA_B      0xFF123C5C

#define PL_BODY    0xFFF2F6FA
#define PL_EDGE    0xFF28527A
#define PL_RED     0xFFE8443A
#define PL_GLASS   0xFF7FD4F0
#define PL_PROP    0x99FFFFFF

#define FO_BODY    0xFF4A4E57
#define FO_EDGE    0xFF20242B
#define FO_RED     0xFFC1352B

#define BUL_ME     0xFFFFF3B0
#define BUL_ME_HOT 0xFFFFFFFF
#define BUL_FO     0xFFFF8B5E

#define EX_CORE    0xFFFFF0C0
#define EX_FIRE    0xFFFF9A3C
#define EX_SMOKE   0x66AAAAAA

#define HUD_BG     0x66102038
#define HUD_FG     0xFFFFFFFF
#define HUD_DIM    0xCCCFE6F5

/* ── 全局状态（C 前端的全局变量与全局 int 数组都正常）──────── */
/* -- Global state (globals and global int arrays both work on the C front end) -- */

int W, H, GFX;              /* 画布尺寸、底部地面高度 */
                            /* Canvas size and the height of the ground at the bottom. */
int g_lang;                 /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                            /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame). */
int shipX, shipY;           /* 飞机（浮点用千分之一定点存）*/
                            /* The plane (floats are kept as thousandths fixed point). */
int shipVX;                 /* 横移速度，决定倾斜 */
                            /* Horizontal velocity, which decides the bank. */
int tgtX, tgtY;             /* 手指位置 */
                            /* Finger position. */
int touching;

int bx1[MAXEB], by1[MAXEB]; /* 我方子弹 */
                            /* Our bullets. */
int bxv[MAXEB], byv[MAXEB];
int blive[MAXEB];

int ex[MAXE], ey[MAXE], evx[MAXE], evy[MAXE], ehp[MAXE], elive[MAXE], ekind[MAXE];
int efx[MAXE], efy[MAXE], efire[MAXE];

int px[MAXP], py[MAXP], pvx[MAXP], pvy[MAXP], pl[MAXP], pc[MAXP], plive[MAXP];
int clx[MAXC], cly[MAXC], clr[MAXC], clsp[MAXC], clyr[MAXC];

int score, best, lives, playing, tick, killer;   /* killer: 撞死我的那架 */
                                                 /* killer: the plane that rammed me. */
int overT;

/* ── 小工具 ─────────────────────────────────────────────── */
/* -- Small helpers -- */

int rnd(int n) { if (n <= 0) return 1; return ui_rand(n); }

/* 自己转字符串：不用 sprintf（见文件头）。两套缓冲轮着用，免得一个表达式里两次调用打架。 */
/* Convert to string by hand: no sprintf (see the file header). Two buffers are used in turn so that two calls in one expression do not clash. */
char g_sbuf1[16];
char g_sbuf2[16];

void itoa_(char* b, int v)
{
    int i = 0, j = 0;
    char t[12];
    if (v < 0) { b[0] = '-'; i = 1; v = -v; }
    if (v == 0) t[j++] = '0';
    while (v > 0 && j < 11) { t[j++] = (char)('0' + v % 10); v = v / 10; }
    while (j > 0) b[i++] = t[--j];
    b[i] = 0;
}

int atoi_(char* b)
{
    int i = 0, v = 0, neg = 0;
    if (b[0] == '-') { neg = 1; i = 1; }
    while (b[i] >= '0' && b[i] <= '9') { v = v * 10 + (b[i] - '0'); i = i + 1; }
    return neg ? -v : v;
}

/* 纯色圆点 + 外发光圈（没有 blur，用递减 alpha 叠出光晕）*/
/* Solid dot plus an outer glow ring (no blur, so stack decreasing alpha to fake a halo) */
void draw_glow(int cx, int cy, int r, int color, int layers)
{
    int i, rr;
    for (i = layers; i >= 1; i--) {
        rr = r + i * 2;
        ui_circle(cx, cy, rr, (color & 0x00FFFFFF) | (0x14 << 24), 1, 0);
    }
    ui_circle(cx, cy, r, color, 1, 0);
}

/* ── 飞机造型 ─────────────────────────────────────────────
 * -- Plane shape --
 * 顶点由函数算好写进**具名全局数组**再交给 ui_polygon —— 复合字面量在这条前端上不支持。
 * Vertices are computed into a **named global array** and then handed to ui_polygon -- compound literals are unsupported on this front end.
 */

int g_pts[24];

void draw_plane(int cx, int cy, int bank)
{
    int i, wob;

    /* 机身（上宽下窄的纺锤形）；bank 是横移量，机头随它偏 —— 这就是"在转弯"的全部秘密 */
    /* Fuselage (a spindle that is wider at the top); bank is the sideways offset and the nose leans with it -- that is the whole secret of "turning" */
    g_pts[0] = cx + bank / 3;          g_pts[1] = cy - 22;
    g_pts[2] = cx + 5 + bank / 4;      g_pts[3] = cy - 6;
    g_pts[4] = cx + 4 + bank / 2;      g_pts[5] = cy + 14;
    g_pts[6] = cx - 4 + bank / 2;      g_pts[7] = cy + 14;
    g_pts[8] = cx - 5 + bank / 4;      g_pts[9] = cy - 6;
    ui_polygon(g_pts, 5, PL_BODY, PL_EDGE, 2, "");

    /* 主翼：一边高一边低（同样是 bank 的效果）*/
    /* Main wings: one side higher than the other (also an effect of bank) */
    g_pts[0] = cx - 26 + bank;         g_pts[1] = cy + 8 - bank / 6;
    g_pts[2] = cx - 5 + bank;          g_pts[3] = cy - 4;
    g_pts[4] = cx + 5 + bank;          g_pts[5] = cy - 4;
    g_pts[6] = cx + 26 + bank;         g_pts[7] = cy + 8 + bank / 6;
    g_pts[8] = cx + 22 + bank;         g_pts[9] = cy + 13 + bank / 6;
    g_pts[10] = cx - 22 + bank;        g_pts[11] = cy + 13 - bank / 6;
    ui_polygon(g_pts, 6, PL_BODY, PL_EDGE, 2, "");

    /* 尾翼 */
    /* Tail fin */
    g_pts[0] = cx - 11 + bank / 2;     g_pts[1] = cy + 15;
    g_pts[2] = cx + 11 + bank / 2;     g_pts[3] = cy + 15;
    g_pts[4] = cx + 7 + bank / 2;      g_pts[5] = cy + 21;
    g_pts[6] = cx - 7 + bank / 2;      g_pts[7] = cy + 21;
    ui_polygon(g_pts, 4, PL_RED, PL_EDGE, 1, "");

    /* 座舱 */
    /* Cockpit */
    ui_ellipse(cx + bank / 3, cy - 8, 5, 8, PL_GLASS, 1, 0);

    /* 螺旋桨：三片，按 tick 转 */
    /* Propeller: three blades, rotating with tick */
    wob = (tick * 37) % 360;
    for (i = 0; i < 3; i++) {
        int a = wob + i * 120;
        int dx = (a < 90) ? 13 - a / 8 : (a < 270 ? -13 + (a - 180) / 8 : 13 - (360 - a) / 8);
        ui_line(cx + bank / 3, cy - 22, cx + bank / 3 + dx, cy - 22 - 4, PL_PROP, 2);
    }
    ui_circle(cx + bank / 3, cy - 22, 3, PL_EDGE, 1, 0);
}

/* 敌机：机头朝下，深色 */
/* Enemy plane: nose pointing down, dark colored */
void draw_foe(int cx, int cy, int kind)
{
    int col = (kind == 0) ? FO_BODY : 0xFF3A2E4A;

    g_pts[0] = cx;      g_pts[1] = cy + 18;
    g_pts[2] = cx + 6;  g_pts[3] = cy + 2;
    g_pts[4] = cx + 5;  g_pts[5] = cy - 12;
    g_pts[6] = cx - 5;  g_pts[7] = cy - 12;
    g_pts[8] = cx - 6;  g_pts[9] = cy + 2;
    ui_polygon(g_pts, 5, col, FO_EDGE, 2, "");

    g_pts[0] = cx - 22; g_pts[1] = cy - 6;
    g_pts[2] = cx + 22; g_pts[3] = cy - 6;
    g_pts[4] = cx + 18; g_pts[5] = cy - 1;
    g_pts[6] = cx - 18; g_pts[7] = cy - 1;
    ui_polygon(g_pts, 4, col, FO_EDGE, 2, "");

    ui_ellipse(cx, cy + 4, 4, 6, FO_RED, 1, 0);
    ui_circle(cx, cy - 12, 3, EX_FIRE, 1, 0);      /* 尾焰 */
                                                   /* Tail flame */
}

/* ── 状态初始化 ─────────────────────────────────────────── */
/* -- State initialization -- */

void reset_cloud(int i)
{
    clx[i] = rnd(W + 120) - 60;
    cly[i] = rnd(H);
    clr[i] = 18 + rnd(30);
    clyr[i] = i % 3;                 /* 0 远 / 1 中 / 2 近 */
                                     /* 0 far / 1 middle / 2 near */
    clsp[i] = 1 + (i % 3) * 2;       /* 越近飘得越快 */
                                     /* The nearer, the faster it drifts */
}

void new_game(void)
{
    int i;

    score = 0; lives = 3; playing = 1; overT = 0; killer = -1;
    shipX = W / 2 * 1000; shipY = (H - GFX) * 620 / 1000 * 1000;
    shipVX = 0; tgtX = shipX / 1000; tgtY = shipY / 1000; touching = 0;

    for (i = 0; i < MAXEB; i++) blive[i] = 0;
    for (i = 0; i < MAXE;  i++) { elive[i] = 0; efire[i] = 0; }
    for (i = 0; i < MAXP;  i++) plive[i] = 0;
    for (i = 0; i < MAXC;  i++) reset_cloud(i);

    ui_msg_clear();
}

int spawn_timer(int n) { return n; }

/* ── 生成一架敌机 ───────────────────────────────────────── */
/* -- Spawn one enemy plane -- */

void spawn_foe(void)
{
    int i;
    for (i = 0; i < MAXE; i++) {
        if (elive[i]) continue;
        elive[i] = 1;
        ex[i] = 30 + rnd(W - 60);
        ey[i] = -40;
        evx[i] = rnd(60) - 30;
        evy[i] = 90 + rnd(70) + score / 30;
        ehp[i] = 2 + (score > 400 ? 1 : 0);
        ekind[i] = (rnd(100) < 30) ? 1 : 0;
        efire[i] = 40 + rnd(60);
        return;
    }
}

/* ── 爆炸 ───────────────────────────────────────────────── */
/* -- Explosion -- */

void boom(int x, int y, int n, int big)
{
    int i;
    for (i = 0; i < MAXP; i++) {
        if (plive[i]) continue;
        plive[i] = 1;
        px[i] = x; py[i] = y;
        pvx[i] = (rnd(200) - 100) * (big > 0 ? 2 : 1) / 3;
        pvy[i] = (rnd(200) - 100) * (big > 0 ? 2 : 1) / 3;
        pl[i] = 16 + rnd(10);
        pc[i] = (rnd(100) < 55) ? EX_FIRE : ((rnd(100) < 60) ? EX_CORE : EX_SMOKE);
        n = n - 1;
        if (n <= 0) return;
    }
}

/* ── 主循环 ─────────────────────────────────────────────── */
/* -- Main loop -- */

void step(void)
{
    int i, j, m[4], t, want, dx, dy, hit;

    t = ui_poll(m);

    /* 触摸：按下/拖动改目标点，抬起就悬停 */
    /* Touch: press or drag changes the target point, lifting the finger just hovers */
    if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_TOUCHMOVE) {
        touching = 1;
        tgtX = m[1];
        tgtY = m[2] - 26;            /* 指尖上方一点，免得手指挡住飞机 */
                                     /* A bit above the finger tip so the finger does not cover the plane */
    } else if (t == VML_MSG_TOUCHUP) {
        touching = 0;
    } else if (t == VML_MSG_WINDOWCLOSE) {
        playing = 0;
    }

    if (playing && overT > 0) {
        overT = overT - 1;
        if (overT == 0) {
            /* 报战果。用 ui_dlg_msg（3 个参数、格式确定）而不是 ui_dlg_select ——
               Report the result. Use ui_dlg_msg (3 args, fixed format) rather than ui_dlg_select --
               后者的选项串分隔约定没核实过，赌错就是"选项糊成一行"。
               the separator convention of the latter's option string was never verified, and a wrong guess means the options merge into one line.
               点掉之后直接开新局：这游戏本来就没有"暂停"以外的状态。
               Dismiss it and a new game starts right away: this game has no state other than "paused".
             */
            itoa_(g_sbuf1, score);
            if (g_lang == 0) ui_dlg_msg("战果", g_sbuf1, 0);
            else             ui_dlg_msg("Result", g_sbuf1, 0);
            new_game();
        }
    }

    if (!playing || overT > 0) { tick = tick + 1; return; }

    /* ── 飞机跟随（阻尼，不是硬贴）── */
    /* -- Plane follow (damped, not glued to the finger) -- */
    want = tgtX * 1000;
    shipVX = (want - shipX) * 18 / 100;
    if (shipVX > 11000) shipVX = 11000;
    if (shipVX < -11000) shipVX = -11000;
    shipX = shipX + shipVX;
    if (shipX < 26000) shipX = 26000;
    if (shipX > (W - 26) * 1000) shipX = (W - 26) * 1000;

    want = tgtY * 1000;
    shipY = shipY + (want - shipY) * 14 / 100;
    if (shipY < 80 * 1000) shipY = 80 * 1000;
    if (shipY > (H - GFX - 30) * 1000) shipY = (H - GFX - 30) * 1000;

    /* ── 自动开火（每 5 拍一发）── */
    /* -- Auto fire (one shot every 5 ticks) -- */
    if (tick % 5 == 0) {
        for (i = 0; i < MAXEB; i++) {
            if (blive[i]) continue;
            blive[i] = 1;
            bx1[i] = shipX / 1000; by1[i] = shipY / 1000 - 24;
            bxv[i] = 0; byv[i] = -17;
            break;
        }
    }

    for (i = 0; i < MAXEB; i++) {
        if (!blive[i]) continue;
        by1[i] = by1[i] + byv[i];
        bx1[i] = bx1[i] + bxv[i];
        if (by1[i] < -20) blive[i] = 0;
    }

    /* ── 敌机 ── */
    /* -- Enemy planes -- */
    if (tick % 26 == 0 && score < 2000) spawn_foe();
    if (tick % 40 == 0 && score >= 400) spawn_foe();

    for (i = 0; i < MAXE; i++) {
        if (!elive[i]) continue;
        ey[i] = ey[i] + evy[i] / 10;
        ex[i] = ex[i] + evx[i] / 10;
        if (ex[i] < 20) { ex[i] = 20; evx[i] = -evx[i]; }
        if (ex[i] > W - 20) { ex[i] = W - 20; evx[i] = -evx[i]; }

        /* 开火 */
        /* Fire */
        efire[i] = efire[i] - 1;
        if (efire[i] <= 0 && ey[i] > 0) {
            efire[i] = 60 + rnd(80);
            for (j = 0; j < MAXEB; j++) {
                if (blive[j]) continue;
                blive[j] = 2;                     /* 2 = 敌弹 */
                                                  /* 2 = enemy bullet */
                bx1[j] = ex[i]; by1[j] = ey[i] + 18;
                bxv[j] = 0; byv[j] = 11;
                break;
            }
        }

        if (ey[i] > H - GFX + 40) { elive[i] = 0; continue; }

        /* 我方子弹打中 */
        /* Hit by one of our bullets */
        for (j = 0; j < MAXEB; j++) {
            if (blive[j] != 1) continue;
            dx = bx1[j] - ex[i]; dy = by1[j] - ey[i];
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            if (dx < 18 && dy < 16) {
                blive[j] = 0;
                ehp[i] = ehp[i] - 1;
                if (ehp[i] <= 0) {
                    elive[i] = 0;
                    boom(ex[i], ey[i], 12, 1);
                    score = score + 10;
                    sfx_kill();
                } else {
                    boom(ex[i], ey[i], 4, 0);
                }
            }
        }

        /* 撞机 */
        /* Collision with the plane */
        dx = shipX / 1000 - ex[i]; dy = shipY / 1000 - ey[i];
        if (dx < 0) dx = -dx;
        if (dy < 0) dy = -dy;
        if (dx < 20 && dy < 20) {
            elive[i] = 0;
            boom(ex[i], ey[i], 18, 1);
            lives = lives - 1;
            ui_vibrate(180, 2);
            sfx_hit();
            if (lives <= 0) { overT = 40; }
        }
    }

    /* ── 敌弹 ── */
    /* -- Enemy bullets -- */
    for (i = 0; i < MAXEB; i++) {
        if (blive[i] != 2) continue;
        by1[i] = by1[i] + byv[i];
        if (by1[i] > H) { blive[i] = 0; continue; }
        dx = shipX / 1000 - bx1[i]; dy = shipY / 1000 - by1[i];
        if (dx < 0) dx = -dx;
        if (dy < 0) dy = -dy;
        if (dx < 14 && dy < 16) {
            blive[i] = 0;
            boom(shipX / 1000, shipY / 1000, 16, 1);
            lives = lives - 1;
            ui_vibrate(180, 2);
            sfx_hit();
            if (lives <= 0) { overT = 40; }
        }
    }

    /* ── 云 ── */
    /* -- Clouds -- */
    for (i = 0; i < MAXC; i++) {
        cly[i] = cly[i] + clsp[i];
        if (cly[i] > H) { reset_cloud(i); cly[i] = -40; }
    }

    /* ── 粒子 ── */
    /* -- Particles -- */
    for (i = 0; i < MAXP; i++) {
        if (!plive[i]) continue;
        px[i] = px[i] + pvx[i] / 4;
        py[i] = py[i] + pvy[i] / 4;
        pvx[i] = pvx[i] * 9 / 10;
        pvy[i] = pvy[i] * 9 / 10;
        pl[i] = pl[i] - 1;
        if (pl[i] <= 0) plive[i] = 0;
    }

    tick = tick + 1;
}

/* ── 渲染 ───────────────────────────────────────────────── */
/* -- Rendering -- */

void draw(void)
{
    int i, a, r, col;
    int nx;      /* 「得分 / Score」后面那个数从哪开始画（英文更宽，得往后挪）*/
                 /* Where to start drawing the number after the "Score" label (the English label is wider, so it must move right) */

    /* ⚠ 先清场。所有绘制都往宿主**同一张图元表**里追加，只有 ui_clear 会清空它 ——
       ⚠ Clear the stage first. Every draw call appends to the **same figure list** in the host, and only ui_clear empties it --
       渐变铺满全屏**看着**像清了底，其实是"盖上去"。漏了这一句，图元表每帧涨约 200 条、
       a full-screen gradient **looks** like it cleared the background, but it really just covers it. Miss this line and the figure list grows by about 200 entries per frame,
       30fps 下每秒 6000 条、只增不减，几十秒后宿主被拖垮 —— 表现就是**打一会自己闪退**。
       6000 per second at 30fps, never shrinking, and the host is dragged down after tens of seconds -- the symptom is that **the game crashes by itself after a while**.
       （starfall.c 踩过同一个坑，那里的复盘同样适用。）
       (starfall.c fell into the same pit; the same post-mortem applies here.)
     */
    ui_clear(SKY_TOP);

    /* 天空 + 海 */
    /* Sky and sea */
    ui_gradient("sky", 0, SKY_TOP, SKY_BOT, 0, 0, 0, 1000);
    ui_rect_grad(0, 0, W, H - GFX, "sky", 0);
    ui_gradient("sea", 0, SEA_A, SEA_B, 0, 0, 0, 1000);
    ui_rect_grad(0, H - GFX, W, GFX, "sea", 0);

    /* 海面波纹 */
    /* Ripples on the sea surface */
    for (i = 0; i < 6; i++) {
        int wy = H - GFX + 8 + i * (GFX / 7);
        ui_line(0, wy, W, wy, 0x22FFFFFF, 1);
    }

    /* 云：越近越白越大 */
    /* Clouds: the nearer, the whiter and bigger */
    for (i = 0; i < MAXC; i++) {
        a = (clyr[i] == 0) ? 0x44 : (clyr[i] == 1 ? 0x77 : 0xAA);
        col = (CLOUD & 0x00FFFFFF) | (a << 24);
        r = clr[i];
        ui_circle(clx[i], cly[i], r, col, 1, 0);
        ui_circle(clx[i] - r * 3 / 4, cly[i] + r / 4, r * 2 / 3, col, 1, 0);
        ui_circle(clx[i] + r * 3 / 4, cly[i] + r / 5, r * 3 / 4, col, 1, 0);
        ui_circle(clx[i] + r / 5, cly[i] - r / 3, r * 2 / 3, col, 1, 0);
    }

    /* 敌机 */
    /* Enemy planes */
    for (i = 0; i < MAXE; i++)
        if (elive[i]) draw_foe(ex[i], ey[i], ekind[i]);

    /* 我的飞机 */
    /* My plane */
    if (playing && overT == 0) {
        int bank = shipVX / 2600;
        if (bank > 9) bank = 9;
        if (bank < -9) bank = -9;
        /* 尾流 */
        /* Wake */
        for (i = 1; i <= 4; i++)
            ui_circle(shipX / 1000 + bank, shipY / 1000 + 16 + i * 5, 5 - i,
                      (PL_GLASS & 0x00FFFFFF) | ((0x30 - i * 8) << 24), 1, 0);
        draw_plane(shipX / 1000, shipY / 1000, bank);
    }

    /* 子弹 */
    /* Bullets */
    for (i = 0; i < MAXEB; i++) {
        if (!blive[i]) continue;
        if (blive[i] == 1) {
            draw_glow(bx1[i], by1[i], 2, BUL_ME, 2);
            ui_rect(bx1[i] - 1, by1[i] - 7, 3, 12, BUL_ME_HOT, 1, 0, 1);
        } else {
            draw_glow(bx1[i], by1[i], 3, BUL_FO, 2);
            ui_ellipse(bx1[i], by1[i], 3, 5, BUL_FO, 1, 0);
        }
    }

    /* 粒子 */
    /* Particles */
    for (i = 0; i < MAXP; i++) {
        if (!plive[i]) continue;
        a = pl[i] * 10;
        if (a > 0xEE) a = 0xEE;
        ui_circle(px[i], py[i], pl[i] / 3 + 1, (pc[i] & 0x00FFFFFF) | (a << 24), 1, 0);
    }

    /* ── HUD ── */
    ui_rect(0, 0, W, 44, HUD_BG, 1, 0, 0);
    ui_set_font(20, VML_FONT_BOLD, HUD_FG, VML_ANCHOR_LEFT);
    /* ⚠ "Score" 比"得分"宽（5×半角 ≈ 50px，而"得分" = 2×全角 = 40px）——
       ⚠ The English label "Score" is wider than the Chinese label (5 half-width chars ~ 50px, while the Chinese one is 2 full-width chars = 40px) --
       数字的起点必须跟着挪，否则英文标签会压到分数上。
       the number's start x must move with it, otherwise the English label runs into the score.
     */
    if (g_lang == 0) { ui_text_cur(14, 12, "得分"); nx = 58; }
    else             { ui_text_cur(14, 12, "Score"); nx = 72; }
    itoa_(g_sbuf1, score);
    ui_set_font(24, VML_FONT_BOLD, EX_CORE, VML_ANCHOR_LEFT);
    ui_text_cur(nx, 10, g_sbuf1);

    /* 命：三颗小心 */
    /* Lives: three little hearts */
    for (i = 0; i < 3; i++) {
        int hx = W - 26 - i * 26;
        if (i < lives) draw_glow(hx, 22, 5, PL_RED, 1);
        else ui_circle(hx, 22, 5, 0x44FFFFFF, 1, 1);
    }

    /* 最高分 */
    /* Best score */
    itoa_(g_sbuf2, best);
    ui_set_font(13, 0, HUD_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(14, 46, g_lang == 0 ? "最高" : "Best");
    ui_text_cur(48, 46, g_sbuf2);

    /* 结束横幅 */
    /* Game over banner */
    if (overT > 0) {
        ui_rect(0, H / 2 - 40, W, 80, 0xAA0A1626, 1, 0, 0);
        ui_set_font(26, VML_FONT_BOLD, EX_FIRE, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 26, g_lang == 0 ? "坠落了" : "Crashed");
    }
}

/* ── 入口 ───────────────────────────────────────────────── */
/* -- Entry point -- */

/* ── 音效：**用 ui_beep 单音**（v0.96.509 统一换回来） ────────────
 * -- Sound effects: **single tones via ui_beep** (switched back to this in v0.96.509) --
 *
 * ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add`），**真机上破音**，换回来了。
 * ⚠⚠ These sounds once went through the shared-library sequencer (`ui_sfx_add`), which **crackled on a real device**, so they were switched back.
 *   破音的是**这里配的音**：被撞那一版末音拖了 10 拍（一拍 33ms ≈ 330ms），
 *   What crackled was **the tuning used here**: the last note of the "got hit" sound was dragged over 10 ticks (a tick is 33ms ~ 330ms),
 *   长音加外放就是"滋滋"。`ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒
 *   and a long note through the speaker is exactly that buzz. `ui_beep` is **single channel** (a new tone cuts off the previous one) =>
 *   一个事件永远只有一个音在响，**结构上不可能长音叠加**。
 *   an event can never have more than one tone sounding, so **long notes structurally cannot stack**.
 * ⚠ 射击是一秒钟好几次的动作，音效**必须又轻又短**，否则玩两分钟就烦。
 * ⚠ Shooting happens several times a second, so the sound **must be light and short**, otherwise it gets annoying after two minutes.
 */

/* 打掉一架敌机：一声高而短的「叮」（打中什么了，一耳朵听得出来）。 */
/* Shot down an enemy plane: a high, short "ding" (you can hear at once that something was hit). */
void sfx_kill(void) { ui_beep(1047, 165); }

/* 被撞掉一条命：低沉、长 + 震动。 */
/* Lost a life to a collision: low, long, plus vibration. */
void sfx_hit(void) {
    ui_beep(196, 320);
    ui_vibrate(180, 2);
}

int main(void)
{
    int i;

    W = ui_scr_w();
    H = ui_scr_h();
    GFX = H / 7;
    tick = 0;

    /* 触摸游戏：不要手柄区（多点地方飞），锁竖屏 */
    /* Touch game: no gamepad area (more room to fly in), locked to portrait */
    g_lang = ui_get_language();
    ui_win_open_ex(g_lang == 0 ? "长空" : "Plane", W, H, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* ui_store_get 是 (key, buf, cap)：**把字符串写进你给的缓冲区**、返回长度
       ui_store_get is (key, buf, cap): it **writes the string into the buffer you pass** and returns the length
       （没有这条键返回 -1）。不是"返回那个整数" —— 别想当然。
       (it returns -1 when the key is absent). It does not "return the integer" -- do not assume.
     */
    if (ui_store_get("plane_best", g_sbuf1, 16) > 0) best = atoi_(g_sbuf1);
    if (best < 0) best = 0;

    new_game();

    /* 一拍 33ms，约 30fps —— 手机上手感正好，也不费电 */
    /* One tick is 33ms, about 30fps -- it feels right on a phone and does not drain the battery */
    ui_timer_set(TICK, 1);

    while (ui_win_closed() == 0) {
        int m[4];
        int t = ui_wait(m, 200);
        if (t == VML_MSG_TIMER) {
            step();
            draw();
            ui_present();
        } else if (t == VML_MSG_WINDOWCLOSE) {
            break;
        } else if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_TOUCHMOVE) {
            touching = 1;
            tgtX = m[1];
            tgtY = m[2] - 26;
        } else if (t == VML_MSG_TOUCHUP) {
            touching = 0;
        }
    }

    if (score > best) {
        itoa_(g_sbuf1, score);
        ui_store_set("plane_best", g_sbuf1);
    }
    ui_keep_on(0);
    ui_sfx_panic();
    return 0;
}
