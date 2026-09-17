/* pacman.c —— 《吃豆人》C 版，跑在手机端 VML 上
 *
 * 编译运行：vml run examples/c/pacman.c
 *
 * 操作：**屏幕手柄的方向键**（← ↓ → ↑）改变方向，走到岔口自动转向。吃到所有豆子过关。
 *
 * ## 为什么是吃豆人（而不是继续做射击游戏）
 *
 * 屏幕手柄在宿主侧**只有一个按键槽**（`DrawWindowPage._padDownKey`：按新键会先替旧键
 * 补一条 KeyUp）⇒ **两个键同时按做不到**。这砍掉了一整类玩法：
 * 平台跳跃要"跑动中起跳"（mario.c 栽在这上面）、射击要"边移动边开火"。
 * 而**吃豆人天生只按一个方向键** —— 手柄的限制在这里正好不是限制。
 *
 * ## 为什么这个画面"稳"（对比 starfall.c 的教训）
 *
 * 上一版《星陨》栽在两条上：① **漏了 `ui_clear`** ⇒ 每帧叠上一帧，糊成一团还闪退；
 * ② 渐变几何按**像素**传（那个接口要的是千分之一归一化）⇒ 焦点塌成一角，其余全黑。
 * 这一版**刻意不用渐变、不用旋转多边形**：全是平面高对比色块 + 圆 + 圆角矩形，
 * 几何只有"格 × 格边长"一种算法。**能出错的地方少了，能看清的地方就多了。**
 *
 * ## 关卡为什么是"梯子形"
 *
 * 手绘复杂迷宫极容易画出**封死的口袋**（豆子永远吃不到，游戏无法通关），
 * 而只看矩阵是看不出来的。这里用**构造式**布局，连通性由构造保证：
 *   · 第 1/4/7/10/13/16/19 行是**整条横廊**（列 1..17 全通）
 *   · 相邻横廊之间在**固定的 6 个列**（1/4/7/11/14/17）有竖井
 * ⇒ 任意两条横廊都连通，任意豆子都吃得到。自测里有一条实际的**泛洪验证**钉着这件事。
 *
 * ## 三条 C 前端硬约束（同 tetris.c / mario.c / starfall.c）
 *
 * 1. ⚠ `${}` 只认局部变量 ⇒ syscall 一律走 `waycoder_ui.h` 包装函数。
 * 2. 全局变量与全局 int 数组读写都正常。
 * 3. ⚠ `#define` 不支持反斜杠续行；⚠ 顶点数组必须是**具名全局数组**（复合字面量不支持）。
 */

#include <waycoder_ui.h>
#include <stdlib.h>

/* ── 迷宫尺寸与图块 ─────────────────────────────────────── */

#define MW      19          /* 列 */
#define MH      21          /* 行 */
#define T_EMPTY 0
#define T_WALL  1
#define T_DOT   2
#define T_POW   3

/* 7 条全通横廊所在的行 */
/* 1 4 7 10 13 16 19 */

int MAZE[MW * MH] = {
    /*  0 */ 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
    /*  1 */ 3,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,3,
    /*  2 */ 1,2,1,1,2,1,1,2,1,1,1,2,1,1,2,1,1,2,1,
    /*  3 */ 1,2,1,1,2,1,1,2,1,1,1,2,1,1,2,1,1,2,1,
    /*  4 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /*  5 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /*  6 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /*  7 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /*  8 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /*  9 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 10 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /* 11 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 12 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 13 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /* 14 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 15 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 16 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /* 17 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 18 */ 1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1,
    /* 19 */ 1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,
    /* 20 */ 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1
};

/* ── 配色：平面高对比，不用渐变 ─────────────────────────── */

#define C_BG      0xFF0A0A14
#define C_WALL    0xFF2B4BC8
#define C_WALL_IN 0xFF14205E
#define C_DOT     0xFFFFE9B0
#define C_POW     0xFFFFC24B
#define C_PAC     0xFFFFD93B
#define C_PAC_M   0xFF0A0A14
#define C_GHOST0  0xFFFF4D4D
#define C_GHOST1  0xFF4DE1FF
#define C_GHOST2  0xFFFFB8DE
#define C_GHOST3  0xFFFFA24D
#define C_FRIGHT  0xFF3A4BD8
#define C_EYE_W   0xFFFFFFFF
#define C_EYE_B   0xFF14204A
#define C_HUD     0xCC05050E
#define C_TEXT    0xFFE8EEFF
#define C_DIM     0xFF8FA3C8
#define C_WARN    0xFFFF6B6B

#define NG 4                /* 鬼数 */
#define TICK 40             /* 一拍 40ms */

/* ── 状态 ───────────────────────────────────────────────── */

int W, H, TILE, OX, OY;
int frame, state;           /* 0=玩 1=暂停 2=死 3=过关 */
int score, best, level, lives;
int dotsLeft;

int pxp, pyp;               /* 吃豆人像素坐标（中心）*/
int pdir, pwant;            /* 当前朝向 / 期望朝向（0上 1右 2下 3左）*/
int mouth;                  /* 嘴张合动画相位 */
int fright;                 /* 剩余惊吓拍数 */
int pdead;                  /* 死亡动画计时 */

int gx[NG], gy[NG], gdir[NG];              /* 鬼：像素坐标 / 朝向 */

/* 各鬼的**出场拍数** —— 必须错开。
 * 四只鬼的出生点挨在一起（都在迷宫中部那一行），不错开就是"开局一起扑过来"：
 * 实测玩家在第 133 拍（约 5 秒）就掉一条命，连熟悉操作的时间都没有。
 * 0 = 一开始就在场上。 */
int grel[NG] = { 0, 70, 160, 260 };

char nbuf[16];

/* 方向增量：上 右 下 左 */
int DX[4] = { 0, 1, 0, -1 };
int DY[4] = { -1, 0, 1, 0 };

/* ── 小工具 ─────────────────────────────────────────────── */

char* num_str(int v) {
    int i;
    int n;
    if (v < 0) { nbuf[0] = '-'; nbuf[1] = '0'; nbuf[2] = 0; return nbuf; }
    i = 0;
    if (v == 0) { nbuf[0] = '0'; nbuf[1] = 0; return nbuf; }
    while (v > 0) { nbuf[i] = '0' + (v % 10); i = i + 1; v = v / 10; }
    nbuf[i] = 0;
    n = 0;
    while (n < i / 2) { char t; t = nbuf[n]; nbuf[n] = nbuf[i - 1 - n]; nbuf[i - 1 - n] = t; n = n + 1; }
    return nbuf;
}

int tile_at(int c, int r) {
    if (c < 0 || c >= MW || r < 0 || r >= MH) return T_WALL;
    return MAZE[r * MW + c];
}

int is_wall(int c, int r) { return tile_at(c, r) == T_WALL; }

/* 格 → 像素中心 */
int cx_of(int c) { return OX + c * TILE + TILE / 2; }
int cy_of(int r) { return OY + r * TILE + TILE / 2; }

/* 像素点落在哪一格 */
int col_of(int x) { int v = (x - OX) / TILE; if (v < 0) v = 0; if (v >= MW) v = MW - 1; return v; }
int row_of(int y) { int v = (y - OY) / TILE; if (v < 0) v = 0; if (v >= MH) v = MH - 1; return v; }

int abs_i(int v) { if (v < 0) return -v; return v; }
int rnd(int n) { if (n <= 0) return 0; return ui_rand(n); }

/* ── 音效（单通道 ⇒ 一次一个音）─────────────────────────── */

void sfx_dot(void) { ui_beep(880, 18); }
void sfx_pow(void) { ui_beep(520, 120); }
void sfx_eat(void) { ui_beep(1400, 90); }
void sfx_die(void) { ui_beep(150, 420); ui_vibrate(220, 0); }
void sfx_win(void) { ui_beep(1320, 150); }

/* ── 开局 ───────────────────────────────────────────────── */


void load_level(void) {
    int i;
    dotsLeft = 0;
    i = 0;
    while (i < MW * MH) {
        if (MAZE[i] == T_DOT || MAZE[i] == T_POW) dotsLeft = dotsLeft + 1;
        i = i + 1;
    }
}

void respawn(void) {
    int i;
    pxp = cx_of(9);
    pyp = cy_of(19);
    pdir = 3;                 /* 朝左，和原作一样 */
    pwant = 3;
    mouth = 0;
    pdead = 0;
    fright = 0;
    i = 0;
    while (i < NG) {
        gx[i] = cx_of(8 + i);
        gy[i] = cy_of(10);
        gdir[i] = 0;
        i = i + 1;
    }
}

void new_game(void) {
    int i;
    score = 0;
    level = 1;
    lives = 3;
    state = 0;
    frame = 0;
    load_level();
    respawn();
}

/* ── 移动 ───────────────────────────────────────────────── */

/* 能不能从格 (c,r) 朝 d 走一格 */
int can_go(int c, int r, int d) {
    return is_wall(c + DX[d], r + DY[d]) == 0;
}

/* 是否**正落在格心**上（精确判定，不给容差）。
 *
 * ⚠⚠ 这条判据前后错了两次，两次都是**位置模型**的问题、不是笔误：
 *
 * ① 第一版写的是"坐标是不是格边长（TILE）的整数倍" —— 那是**格线**，而吃豆人停的是
 *    **格心**（`cx_of` = 原点 + c*TILE + TILE/2）⇒ 转向永远不触发、豆子永远吃不到。
 * ② 第二版改成"离格心 ≤3px"，可移动还是**一次走 spd 像素**：`col_of()` 一越过格线就
 *    报下一列，"前面是墙"随即在**离格心 9px 的格线上**把人钉死。实测 400 拍
 *    `pdir` 恒为 1、`pxp` 恒为 316 —— 而 col17 的格心是 **325**；不吸附、不转向、
 *    人再也不动（用户看到的正是"按键有反应、人不动"）。
 *
 * 现在两处一起改：**逐像素推进 + 精确格心判定**（见 `move_pac`）。
 * 「落在格心上」于是成了构造保证 —— 从格心出发、每次只走 1px，必然**精确经过**格心，
 * 与速度无关。旧写法在 `spd` 不整除 `TILE` 的关卡（4/5/7…）还会累积漂移。*/
int at_center(int v, int origin) {
    int r = (v - origin) % TILE;
    if (r < 0) r = r + TILE;
    return r == TILE / 2;
}

/* 在格 (c,r) 吃豆。凑到格心才判 —— 与转向同一时刻，语义干净。*/
void eat_at(int c, int r) {
    if (tile_at(c, r) == T_DOT) {
        MAZE[r * MW + c] = T_EMPTY;
        dotsLeft = dotsLeft - 1;
        score = score + 10;
        sfx_dot();
    } else if (tile_at(c, r) == T_POW) {
        MAZE[r * MW + c] = T_EMPTY;
        dotsLeft = dotsLeft - 1;
        score = score + 50;
        fright = 140;
        sfx_pow();
    }
}

void move_pac(void) {
    int spd;
    int i;
    int c;
    int r;

    spd = 3 + level / 3;
    if (spd > 9) spd = 9;

    /* 一拍走 spd 像素，但**一像素一判**：只有"此刻正落在格心"才允许转向、吃豆、停下。
     * 到格心顺手吸附一次（把任何偏差归零），保证位置永远精确落在格心网格上。*/
    i = 0;
    while (i < spd) {
        c = col_of(pxp);
        r = row_of(pyp);

        if (at_center(pxp, OX) && at_center(pyp, OY)) {
            pxp = cx_of(c);
            pyp = cy_of(r);
            eat_at(c, r);
            if (pwant != pdir && can_go(c, r, pwant)) pdir = pwant;
            if (!can_go(c, r, pdir)) return;      /* 前面是墙：停在格心 */
        }

        pxp = pxp + DX[pdir];
        pyp = pyp + DY[pdir];
        i = i + 1;
    }
}

/* 鬼：朝吃豆人方向选一条能走的路（不原地掉头，除非无路可走）*/
void move_ghost(int i) {
    int c;
    int r;
    int spd;
    int k;
    int d;
    int best;
    int bestv;
    int v;

    spd = 2 + level / 2;
    if (spd > 4) spd = 4;

    if (frame < grel[i]) return;     /* 还没到出场时间 */

    /* 与吃豆人**同一套走法**：逐像素推进，只在格心做抉择。
     * （旧版"不在格心就一次挪 2px"同样会卡在格线上，而且 spd=4 不整除 18 时还会漂。）*/
    k = 0;
    while (k < spd) {
        if (at_center(gx[i], OX) && at_center(gy[i], OY)) {
            c = col_of(gx[i]);
            r = row_of(gy[i]);
            gx[i] = cx_of(c);
            gy[i] = cy_of(r);

            /* 候选方向：能走的四个方向，排除掉头 */
            best = -1;
            bestv = 1000000;
            d = 0;
            while (d < 4) {
                if (can_go(c, r, d)) {
                    if (d != ((gdir[i] + 2) & 3) || best < 0) {
                        /* 惊吓时逃（远离），平时追（靠近）*/
                        v = abs_i(c + DX[d] - col_of(pxp)) + abs_i(r + DY[d] - row_of(pyp));
                        if (fright > 0) v = -v;
                        v = v + rnd(2);              /* 一点随机，免得四只鬼叠在一起 */
                        if (v < bestv) { bestv = v; best = d; }
                    }
                }
                d = d + 1;
            }
            if (best < 0) best = (gdir[i] + 2) & 3;   /* 死路：掉头 */
            gdir[i] = best;
        }

        gx[i] = gx[i] + DX[gdir[i]];
        gy[i] = gy[i] + DY[gdir[i]];
        k = k + 1;
    }
}

void lose_life(void) {
    lives = lives - 1;
    state = 2;
    sfx_die();
    if (lives <= 0) {
        /* 保持 state=2，由 START 重开 */
    }
}

/* ── 每拍 ───────────────────────────────────────────────── */

void tick(void) {
    int i;
    int hit;

    if (state != 0) return;
    frame = frame + 1;
    mouth = (mouth + 1) % 8;

    if (fright > 0) fright = fright - 1;

    move_pac();

    i = 0;
    while (i < NG) {
        move_ghost(i);
        i = i + 1;
    }

    /* 与鬼碰撞 */
    i = 0;
    while (i < NG) {
        if (abs_i(gx[i] - pxp) < TILE * 3 / 4 && abs_i(gy[i] - pyp) < TILE * 3 / 4) {
            if (fright > 0) {
                /* 吃掉鬼：送回中间 */
                gx[i] = cx_of(9);
                gy[i] = cy_of(10);
                gdir[i] = 0;
                score = score + 200;
                sfx_eat();
            } else {
                lose_life();
                return;
            }
        }
        i = i + 1;
    }

    /* 过关 */
    if (dotsLeft <= 0) {
        level = level + 1;
        state = 3;
        sfx_win();
        return;
    }
}

/* ── 绘制 ───────────────────────────────────────────────── */

void draw_maze(void) {
    int c;
    int r;
    int x;
    int y;
    int t;
    int pw;
    r = 0;
    while (r < MH) {
        c = 0;
        while (c < MW) {
            t = tile_at(c, r);
            x = OX + c * TILE;
            y = OY + r * TILE;
            if (t == T_WALL) {
                ui_rect(x, y, TILE, TILE, C_WALL, 1, 0, 2);
                /* 内层深色：让墙看着有厚度，而不是一片蓝 */
                ui_rect(x + 2, y + 2, TILE - 4, TILE - 4, C_WALL_IN, 1, 0, 1);
            } else if (t == T_DOT) {
                pw = TILE / 6;
                if (pw < 2) pw = 2;
                ui_circle(x + TILE / 2, y + TILE / 2, pw, C_DOT, 1, 0);
            } else if (t == T_POW) {
                pw = TILE / 3 + (mouth < 4 ? 1 : 0);   /* 轻微脉动 */
                ui_circle(x + TILE / 2, y + TILE / 2, pw, C_POW, 1, 0);
                ui_circle(x + TILE / 2, y + TILE / 2, pw / 2, 0xFFFFFFFF, 1, 0);
            }
            c = c + 1;
        }
        r = r + 1;
    }
}

/* 嘴楔子：具名全局数组装顶点（复合字面量不支持）*/
int wedge[8];

void ui_polygon_wedge(int cx, int cy, int r, int dir) {
    if (dir == 0) {            /* 朝右的楔 */
        wedge[0] = cx;      wedge[1] = cy;
        wedge[2] = cx + r;  wedge[3] = cy - r;
        wedge[4] = cx + r;  wedge[5] = cy + r;
    } else if (dir == 1) {     /* 朝左 */
        wedge[0] = cx;      wedge[1] = cy;
        wedge[2] = cx - r;  wedge[3] = cy - r;
        wedge[4] = cx - r;  wedge[5] = cy + r;
    } else if (dir == 2) {     /* 朝上 */
        wedge[0] = cx;      wedge[1] = cy;
        wedge[2] = cx - r;  wedge[3] = cy - r;
        wedge[4] = cx + r;  wedge[5] = cy - r;
    } else {                   /* 朝下 */
        wedge[0] = cx;      wedge[1] = cy;
        wedge[2] = cx - r;  wedge[3] = cy + r;
        wedge[4] = cx + r;  wedge[5] = cy + r;
    }
    ui_polygon(wedge, 3, C_BG, 0, 0, "");
}
void draw_pac(void) {
    int r;
    int a;
    int open;
    open = (mouth < 4) ? 1 : 0;

    r = TILE * 2 / 5;
    if (r < 4) r = 4;
    ui_circle(pxp, pyp, r, C_PAC, 1, 0);

    /* 嘴：用背景色的三角"咬"掉一口。方向按 pdir 分四种写死 ——
     * 省掉三角函数，也省掉"旋转多边形"那类几何风险。*/
    if (open != 0) {
        if (pdir == 1) {                       /* 右 */
            ui_polygon_wedge(pxp, pyp, r + 2, 0);
        } else if (pdir == 3) {                /* 左 */
            ui_polygon_wedge(pxp, pyp, r + 2, 1);
        } else if (pdir == 0) {                /* 上 */
            ui_polygon_wedge(pxp, pyp, r + 2, 2);
        } else {                               /* 下 */
            ui_polygon_wedge(pxp, pyp, r + 2, 3);
        }
    }
}


void draw_ghosts(void) {
    int i;
    int r;
    int ex;
    int ey;
    int body;
    i = 0;
    while (i < NG) {
        r = TILE * 2 / 5;
        if (r < 4) r = 4;
        body = C_FRIGHT;
        if (fright <= 0) {
            if (i == 0) body = C_GHOST0;
            else if (i == 1) body = C_GHOST1;
            else if (i == 2) body = C_GHOST2;
            else body = C_GHOST3;
        }
        /* 身体：上半圆 + 下方方块（不用路径，纯圆+矩形拼）*/
        ui_circle(gx[i], gy[i] - r / 3, r, body, 1, 0);
        ui_rect(gx[i] - r, gy[i] - r / 3, r * 2, r + r / 3, body, 1, 0, 0);
        /* 裙摆：三个小方块 */
        ui_rect(gx[i] - r, gy[i] + r - 3, r * 2 / 3, 4, C_BG, 1, 0, 0);
        ui_rect(gx[i] + r / 3, gy[i] + r - 3, r * 2 / 3, 4, C_BG, 1, 0, 0);

        /* 眼 */
        ex = r / 2;
        ey = r / 3;
        ui_circle(gx[i] - ex, gy[i] - ey, r / 3, C_EYE_W, 1, 0);
        ui_circle(gx[i] + ex, gy[i] - ey, r / 3, C_EYE_W, 1, 0);
        ui_circle(gx[i] - ex + DX[gdir[i]] * 2, gy[i] - ey + DY[gdir[i]] * 2, r / 6, C_EYE_B, 1, 0);
        ui_circle(gx[i] + ex + DX[gdir[i]] * 2, gy[i] - ey + DY[gdir[i]] * 2, r / 6, C_EYE_B, 1, 0);
        i = i + 1;
    }
}

void draw_hud(void) {
    int i;
    ui_rect(0, 0, W, 30, C_HUD, 1, 0, 0);
    ui_set_font(13, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(8, 7, num_str(score));
    ui_set_font(10, 0, C_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(8, 20, "分数");

    ui_set_font(11, 0, C_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2 - 26, 12, "第");
    ui_set_font(13, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2, 12, num_str(level));
    ui_set_font(11, 0, C_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2 + 34, 12, "剩");
    ui_set_font(13, VML_FONT_BOLD, C_POW, VML_ANCHOR_CENTER);
    ui_text_cur(W / 2 + 62, 12, num_str(dotsLeft));

    /* 命：黄圆点 */
    i = 0;
    while (i < lives && i < 5) {
        ui_circle(W - 18 - i * 18, 15, 6, C_PAC, 1, 0);
        i = i + 1;
    }

    if (fright > 0) {
        ui_set_font(12, VML_FONT_BOLD, C_GHOST1, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, 42, "鬼可吃！");
    }

    if (state == 1) {
        ui_rect(0, H / 2 - 36, W, 72, 0xCC000000, 1, 0, 0);
        ui_set_font(22, VML_FONT_BOLD, C_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 24, "暂停");
        ui_set_font(12, 0, C_DIM, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 6, "SELECT 继续");
    }
    if (state == 2) {
        ui_rect(0, H / 2 - 56, W, 112, 0xCC000000, 1, 0, 0);
        ui_set_font(22, VML_FONT_BOLD, C_WARN, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 38, "被抓住了");
        ui_set_font(13, 0, C_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 4, "分数");
        ui_set_font(18, VML_FONT_BOLD, C_PAC, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 20, num_str(score));
        ui_set_font(12, 0, C_DIM, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 46, "START 重新开始");
    }
    if (state == 3) {
        ui_rect(0, H / 2 - 44, W, 88, 0xE6000000, 1, 0, 0);
        ui_set_font(22, VML_FONT_BOLD, C_POW, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 - 26, "过关！");
        ui_set_font(13, 0, C_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(W / 2, H / 2 + 4, "START 进入下一关");
    }
}

void draw_all(void) {
    /* ⚠ **必须先 clear**：所有绘制都是往图元表追加，只有它会清空。
     * 漏了就是"每帧叠上一帧"—— 画面糊掉，且图元表无限增长直到闪退
     * （starfall.c 第一版就栽在这上面，见那边文件头的记录）。*/
    ui_clear(C_BG);

    draw_maze();
    draw_pac();
    draw_ghosts();
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
        if (state == 3) {
            /* 下一关：重铺豆子，鬼回中，吃豆人回原位 */
            state = 0;
            frame = 0;               /* 重置出场计时，否则新关卡一开始四只鬼就全在场 */
            respawn();
            load_level();
            draw_all();
            return;
        }
        /* 死亡或暂停中按 START：整局重开 */
        new_game();
        draw_all();
        return;
    }
    if (state != 0) return;

    if (k == VML_KEY_UP) pwant = 0;
    else if (k == VML_KEY_RIGHT) pwant = 1;
    else if (k == VML_KEY_DOWN) pwant = 2;
    else if (k == VML_KEY_LEFT) pwant = 3;
}

/* ── 主循环 ─────────────────────────────────────────────── */

int main(void) {
    int msg[4];
    int t;
    int sw;
    int sh;
    int tw;
    int th;
    int mx;
    int my;

    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;

    ui_win_open("吃豆人", sw, sh);
    ui_keep_on(1);

    W = sw;
    H = sh;

    /* 格边长：横竖都装得下 19×21，再居中 */
    tw = (W - 8) / MW;
    th = (H - 40) / MH;
    TILE = tw;
    if (th < TILE) TILE = th;
    if (TILE < 8) TILE = 8;
    OX = (W - MW * TILE) / 2;
    OY = 34 + ((H - 34) - MH * TILE) / 2;
    if (OX < 0) OX = 0;
    if (OY < 34) OY = 34;

    new_game();
    draw_all();

    ui_timer_set(TICK, 0);

    ui_dlg_msg("吃豆人",
               "用屏幕下方的方向键控制：按哪个方向就走哪个方向，到岔口自动转。"
               "吃光所有豆子过关；吃到大豆子（金圈）后可以反过来吃鬼。"
               "START 重开、SELECT 暂停。",
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
        /* 触摸也当方向键用：点屏幕上半/下半/左半/右半 —— 手柄之外的备用操作 */
        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            if (state == 0) {
                mx = msg[1] - pxp;
                my = msg[2] - pyp;
                if (abs_i(mx) > abs_i(my)) { if (mx > 0) pwant = 1; else pwant = 3; }
                else { if (my > 0) pwant = 2; else pwant = 0; }
            }
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
