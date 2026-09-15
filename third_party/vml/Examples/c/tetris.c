/* tetris.c —— 俄罗斯方块（C 版），跑在手机端 VML 上
 *
 * 用到的都是现成的宿主接口（`waycoder_ui.h` 那套：窗体 / 绘图 / 消息队列 / 定时器）。
 * 布局全部按实际可用绘图区（syscall #566/#567）算，所以手机、平板、模拟器上都能铺满。
 *
 * 编译运行（手机 App 的 vml 工具）：
 *     vml run tetris.c
 *
 * ## 桌面无头模拟器（改完先在这里跑一遍，别为了看一眼效果重打 APK）
 *
 *     vmlhost run Examples/c/tetris.c --sim "t;t;87,671;297,639" --frames 输出目录
 *
 *   `--sim` 脚本里每个 `x,y` 投一次触摸、`t` 投一次**重力节拍**（定时器消息）；
 *   `--frames <目录>` 把每一帧走**和手机同一条渲染链**（VmlScene → DSL → ToPng）出成 PNG，
 *   可以直接看排版对不对。屏幕上没有键盘，所以操作用手柄坐标点：屏幕 395×744 时
 *   左(87,671) 右(171,671) 下(129,713) 旋转(230,639) 直落(297,639) 暂停(230,692) 重开(297,692)。
 *
 *   `vmlhost bench` 能量出图成本（这套"场景→DSL→PNG"路线每帧要多久，游戏卡不卡就看它）。
 *
 * ## 为什么用 C 而不是 BASIC（用户决定）
 *
 * BASIC 版先写了，卡在**三个既有前端缺陷**上：SUB 内局部 FOR 循环死循环、SUB 内局部数组
 * 赋值读回 0、FUNCTION+SUB 组合挂起（见 `scripts/basic-tests/README.md` 的 t9/t10/t11）。
 * 那三个缺陷与本程序无关，但 `draw_board` 这类函数整片都是 SUB 内局部 FOR 循环，
 * 一个都绕不过去 —— 所以先用 C 交付，BASIC 的那几个 bug 以后单独修。
 *
 * ## 三条 C 前端的硬约束（踩过才写的）
 *
 * 1. ⚠ **`${}` 占位符只认局部变量**（`GenerateAsmStatement` 只查 `variables` 表）。
 *    所以调 syscall 一律走 `waycoder_ui.h` 的包装函数 —— 它们的形参就是普通 C 形参，
 *    安全；而**绝不能**自己写 `asm("SYSCALL #525, ${gx}, ...")` 去传全局变量，那会退化成 R12+0。
 * 2. **文件级全局变量是好的**（实测：`int b[BW * BH]` 这种全局数组读写都正确），
 *    所以游戏状态就放全局，不必像五子棋那样把棋盘当参数到处传 —— 那个写法是被
 *    "全局变量不能进 ${}"这条误推出来的，实际上只影响 asm，不影响普通读写。
 *    但为了统一，本文件仍全部通过包装函数调 syscall。
 * 3. ⚠ **`#define` 不支持折行续行**（反斜杠续行会让词法器在下一行报"未知字符"），
 *    所以宏本身一行写完，多行说明另起一段块注释。
 *
 * ## 手柄与命中判定共用一份几何
 *
 * `gx/gy/gk/gstep/rs/rx` 这些都在 `main` 里算**一次**，`draw_pad` 画它、`hit_button` 判它。
 * 各算一遍的话，改个间距就会出现"看着在键上、点下去没反应"。
 */

#include <waycoder_ui.h>

#define BW 10              /* 棋盘宽（格） */
#define BH 20              /* 棋盘高（格） */
#define COL_BG      0xFF0E0E14
#define COL_BOARD   0xFF16161F
#define COL_FRAME   0xFF2A2A38
#define COL_GRID    0xFF21212C
#define COL_TEXT    0xFFEDEDF2
#define COL_DIM     0xFF8A8A99
#define COL_ACCENT  0xFF4ADE80
#define COL_WARN    0xFFF87171
#define COL_KEY     0xFF2E2E3C
#define COL_KEYEDGE 0xFF44445A
#define COL_PANEL   0xFF1B1B26
#define COL_SHADE   0xCC000000

/* ── 游戏状态 ───────────────────────────────────────────── */

int b[BW * BH];     /* 棋盘：0 = 空，否则 = 方块号 + 1。
                     * 维度写成**宏 × 宏**是故意的 —— 它原先会因为 C 前端不折常量表达式
                     * 而静默只分配 1 个元素（编得过、跑起来数据全乱），v0.96.170 修掉了，
                     * 这行就是那个修复的现场回归。 */
int pid;            /* 当前方块 0..6 */
int rot;            /* 旋转 0..3 */
int px;             /* 4×4 包围盒左上角（棋盘格坐标） */
int py;
int npid;           /* 下一个方块 */

int score;
int nlines;
int level;

int state;          /* 0 = 运行 1 = 暂停 2 = 结束 */
int tid;            /* 重力定时器 id（0 = 没有） */
int rid;            /* 长按连发定时器 id（0 = 没有） */
int held;           /* 正在按住的方向键（1 左 2 右 3 下，0 = 没按） */
int dropMs;

/* ── 布局（main 里算一次，画与命中都读它） ───────────────── */

int sw;
int sh;
int cell;           /* 格子边长 */
int bx;             /* 棋盘左上角 */
int by;
int panelX;         /* 右侧信息面板 */
int panelW;
int gx;             /* 手柄左上角 */
int gy;
int gk;             /* 方向键边长 */
int gstep;          /* 方向键步进 = gk + 间隙 */
int dpw;            /* 方向键整块宽 */
int rs;             /* 旋转 / 直落键边长 */
int rx;             /* 右侧键区左上角 */

/* 数字的中间缓冲（`draw_int` 用；全局 int 数组，实测可用） */
int digs[12];

/* ── 方块颜色 ───────────────────────────────────────────── */

int piece_color(int p) {
    if (p == 0) return 0xFF22D3EE;   /* I 青 */
    if (p == 1) return 0xFFFACC15;   /* O 黄 */
    if (p == 2) return 0xFF4ADE80;   /* S 绿 */
    if (p == 3) return 0xFFF87171;   /* Z 红 */
    if (p == 4) return 0xFFC084FC;   /* T 紫 */
    if (p == 5) return 0xFFFB923C;   /* L 橙 */
    return 0xFF60A5FA;               /* J 蓝 */
}

/* 同色高光（块面顶部那一条）。用查表而不是移位算 —— 免得依赖位运算的可用性。 */
int piece_light(int p) {
    if (p == 0) return 0xFF7DE8F7;
    if (p == 1) return 0xFFFDE68A;
    if (p == 2) return 0xFF86EFAC;
    if (p == 3) return 0xFFFCA5A5;
    if (p == 4) return 0xFFD8B4FE;
    if (p == 5) return 0xFFFDBA74;
    return 0xFF93C5FD;
}

char* digit_str(int d) {
    if (d == 0) return "0";
    if (d == 1) return "1";
    if (d == 2) return "2";
    if (d == 3) return "3";
    if (d == 4) return "4";
    if (d == 5) return "5";
    if (d == 6) return "6";
    if (d == 7) return "7";
    if (d == 8) return "8";
    return "9";
}

/* ── 方块几何 ───────────────────────────────────────────── */

/* 某个方块某次旋转的**包围盒左上角**，打包成 minx*16 + miny。
 *
 * 为什么需要它：`ui_piece_cell` 的旋转是绕 4×4 包围盒中心转的，I 这种长条转完
 * 整个形状会"跳一格"。做法是"旋转后把包围盒左上角对回原处"，看起来才稳。 */
int piece_min(int p, int r) {
    int i;
    int v;
    int x;
    int y;
    int mx;
    int my;
    mx = 3;
    my = 3;
    i = 0;
    while (i < 4) {
        v = ui_piece_cell(p, r, i);
        if (v >= 0) {
            x = v / 16;
            y = v % 16;
            if (x < mx) mx = x;
            if (y < my) my = y;
        }
        i = i + 1;
    }
    return mx * 16 + my;
}

/* 方块放在 (x0,y0) 时会不会撞墙 / 撞地 / 撞已有块。y<0 表示还在顶部之外（允许）。 */
int collide(int p, int r, int x0, int y0) {
    int i;
    int v;
    int x;
    int y;
    i = 0;
    while (i < 4) {
        v = ui_piece_cell(p, r, i);
        if (v >= 0) {
            x = x0 + v / 16;
            y = y0 + v % 16;
            if (x < 0 || x >= BW) return 1;
            if (y >= BH) return 1;
            if (y >= 0 && b[y * BW + x] != 0) return 1;
        }
        i = i + 1;
    }
    return 0;
}

void set_speed(void) {
    if (tid > 0) ui_timer_kill(tid);
    dropMs = 620 - (level - 1) * 55;
    if (dropMs < 110) dropMs = 110;
    tid = ui_timer_set(dropMs, 0);
}

/* ── 游戏动作 ───────────────────────────────────────────── */

void spawn(void) {
    int m;
    pid = npid;
    rot = 0;
    npid = ui_rand(7);
    px = 3;
    m = piece_min(pid, 0);
    py = 0 - m % 16;                    /* 让形状最高的一行贴在第 0 行 */
    if (collide(pid, rot, px, py) != 0) state = 2;   /* 出生位就满了 = 结束 */
}

/* 把当前方块写进棋盘、消行、算分。 */
void lock_piece(void) {
    int i;
    int v;
    int x;
    int y;
    int row;
    int col;
    int full;
    int n;
    int k;

    i = 0;
    while (i < 4) {
        v = ui_piece_cell(pid, rot, i);
        if (v >= 0) {
            x = px + v / 16;
            y = py + v % 16;
            if (y >= 0 && y < BH && x >= 0 && x < BW) b[y * BW + x] = pid + 1;
        }
        i = i + 1;
    }

    n = 0;
    row = BH - 1;
    while (row >= 0) {
        full = 1;
        col = 0;
        while (col < BW) {
            if (b[row * BW + col] == 0) full = 0;
            col = col + 1;
        }
        if (full == 1) {
            n = n + 1;
            /* 本行以上整体下移一格；**row 不前进** —— 掉下来的那行还得再判一次 */
            k = row;
            while (k > 0) {
                col = 0;
                while (col < BW) {
                    b[k * BW + col] = b[(k - 1) * BW + col];
                    col = col + 1;
                }
                k = k - 1;
            }
            col = 0;
            while (col < BW) {
                b[col] = 0;
                col = col + 1;
            }
        } else {
            row = row - 1;
        }
    }

    if (n > 0) {
        if (n == 1) score = score + 100 * level;
        if (n == 2) score = score + 300 * level;
        if (n == 3) score = score + 500 * level;
        if (n >= 4) score = score + 800 * level;
        nlines = nlines + n;
        level = nlines / 10 + 1;
        if (level > 12) level = 12;
        set_speed();
    }
}

/* 下落一格；落不下去就锁定并出新方块。 */
void step_down(void) {
    if (collide(pid, rot, px, py + 1) == 0) {
        py = py + 1;
    } else {
        lock_piece();
        spawn();
    }
}

/* 旋转（带踢墙：原位不行就往左右各试 1、2 格）。 */
void rotate_piece(void) {
    int nr;
    int om;
    int nm;
    int nx;
    int ny;
    nr = (rot + 1) % 4;
    om = piece_min(pid, rot);
    nm = piece_min(pid, nr);
    nx = px + om / 16 - nm / 16;
    ny = py + om % 16 - nm % 16;
    if (collide(pid, nr, nx, ny) == 0) {
        rot = nr;
        px = nx;
        py = ny;
        return;
    }
    if (collide(pid, nr, nx - 1, ny) == 0) {
        rot = nr;
        px = nx - 1;
        py = ny;
        return;
    }
    if (collide(pid, nr, nx + 1, ny) == 0) {
        rot = nr;
        px = nx + 1;
        py = ny;
        return;
    }
    if (collide(pid, nr, nx - 2, ny) == 0) {
        rot = nr;
        px = nx - 2;
        py = ny;
        return;
    }
    if (collide(pid, nr, nx + 2, ny) == 0) {
        rot = nr;
        px = nx + 2;
        py = ny;
    }
}

void hard_drop(void) {
    int n;
    n = 0;
    while (collide(pid, rot, px, py + 1) == 0) {
        py = py + 1;
        n = n + 1;
    }
    score = score + n * 2;
    lock_piece();
    spawn();
}

void restart(void) {
    int i;
    i = 0;
    while (i < BW * BH) {
        b[i] = 0;
        i = i + 1;
    }
    score = 0;
    nlines = 0;
    level = 1;
    state = 0;
    held = 0;
    if (rid > 0) {
        ui_timer_kill(rid);
        rid = 0;
    }
    npid = ui_rand(7);
    spawn();
    set_speed();
}

/* ── 绘图 ───────────────────────────────────────────────── */

/* 一个方块格：圆角实心 + 顶部一条高光。 */
void draw_block(int x, int y, int p, int size) {
    int pad;
    int r;
    int hi;
    if (size < 6) {
        ui_rect(x, y, size, size, piece_color(p), 1, 0, 0);
        return;
    }
    pad = size / 9;
    if (pad < 1) pad = 1;
    r = size / 5;
    if (r < 2) r = 2;
    hi = size / 6;
    if (hi < 2) hi = 2;
    ui_rect(x + pad, y + pad, size - pad * 2, size - pad * 2, piece_color(p), 1, 0, r);
    ui_rect(x + pad + r, y + pad + hi, size - (pad + r) * 2, hi, piece_light(p), 1, 0, hi / 2);
}

/* 数字：**逐位画**，不拼字符串（C 前端的字符串拼接没法可靠地做，而十个数字字面量就够了）。 */
void draw_int(int x, int y, int v, int size, int color) {
    int n;
    int i;
    int t;
    int half;
    if (v < 0) v = 0;
    n = 0;
    t = v;
    while (t > 0 || n == 0) {
        digs[n] = t % 10;
        t = t / 10;
        n = n + 1;
    }
    half = size / 2;
    if (half < 5) half = 5;
    ui_set_font(size, VML_FONT_BOLD, color, VML_ANCHOR_LEFT);
    i = n;
    while (i > 0) {
        i = i - 1;
        ui_text_cur(x + (n - 1 - i) * half, y, digit_str(digs[i]));
    }
}

/* 手柄上的一个圆角按键。 */
void draw_key(int x, int y, int w, int h) {
    ui_rect(x, y, w, h, COL_KEY, 1, 0, 9);
    ui_rect(x + 1, y + 1, w - 2, h - 2, COL_KEYEDGE, 0, 2, 8);
}

/* 方向箭头：用两条粗线画成的折角。**不用 ▲ 这类字符** —— 那要赌字体里有这个字形，
 * 系统字体一换就成豆腐块；线是自己画的，到哪都一样。dir: 0 上 1 左 2 右 3 下 */
void draw_arrow(int x, int y, int k, int dir) {
    int cx;
    int cy;
    int a;
    int lw;
    cx = x + k / 2;
    cy = y + k / 2;
    a = k / 5;
    if (a < 5) a = 5;
    lw = k / 10;
    if (lw < 3) lw = 3;
    if (dir == 0) {
        ui_line(cx - a, cy + a / 2, cx, cy - a / 2, COL_TEXT, lw);
        ui_line(cx, cy - a / 2, cx + a, cy + a / 2, COL_TEXT, lw);
    } else if (dir == 1) {
        ui_line(cx + a / 2, cy - a, cx - a / 2, cy, COL_TEXT, lw);
        ui_line(cx - a / 2, cy, cx + a / 2, cy + a, COL_TEXT, lw);
    } else if (dir == 2) {
        ui_line(cx - a / 2, cy - a, cx + a / 2, cy, COL_TEXT, lw);
        ui_line(cx + a / 2, cy, cx - a / 2, cy + a, COL_TEXT, lw);
    } else {
        ui_line(cx - a, cy - a / 2, cx, cy + a / 2, COL_TEXT, lw);
        ui_line(cx, cy + a / 2, cx + a, cy - a / 2, COL_TEXT, lw);
    }
}

void draw_pad(void) {
    int w;
    int cy;
    int bx2;
    w = dpw + 10 + rs * 2 + 8;
    ui_rect(gx - 6, gy - 6, w + 12, gk * 3 + 18, COL_PANEL, 1, 0, 12);

    /* 方向键：十字排布的三个步进位 */
    draw_key(gx + gstep, gy, gk, gk);
    draw_arrow(gx + gstep, gy, gk, 0);
    draw_key(gx, gy + gstep, gk, gk);
    draw_arrow(gx, gy + gstep, gk, 1);
    draw_key(gx + gstep * 2, gy + gstep, gk, gk);
    draw_arrow(gx + gstep * 2, gy + gstep, gk, 2);
    draw_key(gx + gstep, gy + gstep * 2, gk, gk);
    draw_arrow(gx + gstep, gy + gstep * 2, gk, 3);

    /* 旋转 / 直落 */
    draw_key(rx, gy, rs, rs);
    ui_set_font(13, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_CENTER);
    ui_text_cur(rx + rs / 2, gy + rs / 2 - 16, "旋转");
    bx2 = rx + rs + 8;
    draw_key(bx2, gy, rs, rs);
    ui_text_cur(bx2 + rs / 2, gy + rs / 2 - 16, "直落");

    /* SELECT / START：**小尺寸、单独一行，不与上面叠** */
    cy = gy + rs + 10;
    draw_key(rx, cy, rs, 26);
    ui_set_font(11, 0, COL_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(rx + rs / 2, cy + 7, "暂停");
    draw_key(bx2, cy, rs, 26);
    ui_text_cur(bx2 + rs / 2, cy + 7, "重开");
}

void draw_panel(void) {
    int i;
    int v;
    int x;
    int y;
    int mini;
    int pw;
    int ty;

    ui_rect(panelX, by, panelW, BH * cell, COL_PANEL, 1, 0, 8);

    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(panelX + panelW / 2, by + 10, "下一个");

    mini = cell / 2;
    if (mini < 7) mini = 7;
    pw = mini * 4;
    i = 0;
    while (i < 4) {
        v = ui_piece_cell(npid, 0, i);
        if (v >= 0) {
            x = v / 16;
            y = v % 16;
            draw_block(panelX + (panelW - pw) / 2 + x * mini, by + 32 + y * mini, npid, mini);
        }
        i = i + 1;
    }

    ty = by + 32 + mini * 4 + 18;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "分数");
    draw_int(panelX + 10, ty + 16, score, 17, COL_TEXT);

    ty = ty + 56;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "消行");
    draw_int(panelX + 10, ty + 16, nlines, 17, COL_TEXT);

    ty = ty + 56;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "等级");
    draw_int(panelX + 10, ty + 16, level, 17, COL_ACCENT);
}

/* 暂停 / 结束的遮罩 + 一行提示。 */
void draw_overlay(void) {
    int w;
    int h;
    int x;
    int y;
    char* s;
    char* s2;
    if (state == 0) return;
    w = BW * cell - 16;
    h = 76;
    x = bx + 8;
    y = by + (BH * cell - h) / 2;
    ui_rect(bx, by, BW * cell, BH * cell, COL_SHADE, 1, 0, 0);
    ui_rect(x, y, w, h, COL_PANEL, 1, 0, 10);
    if (state == 2) {
        s = "游戏结束";
        s2 = "点「重开」再来一局";
    } else {
        s = "已暂停";
        s2 = "点「暂停」继续";
    }
    ui_set_font(20, VML_FONT_BOLD, COL_WARN, VML_ANCHOR_CENTER);
    ui_text_cur(x + w / 2, y + 16, s);
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(x + w / 2, y + 48, s2);
}

void draw_all(void) {
    int i;
    int v;
    int x;
    int y;
    ui_clear(COL_BG);

    /* 棋盘：外框 + 底 + 网格 */
    ui_rect(bx - 3, by - 3, BW * cell + 6, BH * cell + 6, COL_FRAME, 1, 0, 8);
    ui_rect(bx, by, BW * cell, BH * cell, COL_BOARD, 1, 0, 4);
    i = 1;
    while (i < BW) {
        ui_line(bx + i * cell, by + 1, bx + i * cell, by + BH * cell - 1, COL_GRID, 1);
        i = i + 1;
    }
    i = 1;
    while (i < BH) {
        ui_line(bx + 1, by + i * cell, bx + BW * cell - 1, by + i * cell, COL_GRID, 1);
        i = i + 1;
    }

    /* 已落定的方块 */
    i = 0;
    while (i < BW * BH) {
        v = b[i];
        if (v != 0) {
            x = i % BW;
            y = i / BW;
            draw_block(bx + x * cell, by + y * cell, v - 1, cell);
        }
        i = i + 1;
    }

    /* 当前方块（结束后不画） */
    if (state != 2) {
        i = 0;
        while (i < 4) {
            v = ui_piece_cell(pid, rot, i);
            if (v >= 0) {
                x = px + v / 16;
                y = py + v % 16;
                if (y >= 0) draw_block(bx + x * cell, by + y * cell, pid, cell);
            }
            i = i + 1;
        }
    }

    draw_panel();
    draw_pad();
    draw_overlay();
    ui_present();
}

/* ── 手柄命中 ───────────────────────────────────────────── */

/* 按键码：1 左 2 右 3 下 4 旋转 5 直落 6 暂停 7 重开；0 = 没命中 */
int in_box(int x, int y, int x0, int y0, int w, int h) {
    if (x < x0) return 0;
    if (x > x0 + w) return 0;
    if (y < y0) return 0;
    if (y > y0 + h) return 0;
    return 1;
}

int hit_button(int x, int y) {
    int bx2;
    bx2 = rx + rs + 8;
    if (in_box(x, y, gx + gstep, gy, gk, gk)) return 4;
    if (in_box(x, y, gx, gy + gstep, gk, gk)) return 1;
    if (in_box(x, y, gx + gstep * 2, gy + gstep, gk, gk)) return 2;
    if (in_box(x, y, gx + gstep, gy + gstep * 2, gk, gk)) return 3;
    if (in_box(x, y, rx, gy, rs, rs)) return 4;
    if (in_box(x, y, bx2, gy, rs, rs)) return 5;
    if (in_box(x, y, rx, gy + rs + 10, rs, 26)) return 6;
    if (in_box(x, y, bx2, gy + rs + 10, rs, 26)) return 7;
    return 0;
}

/* 按一下某个键。返回 **画面是否需要重画** —— 每次重画都要把整幅场景光栅化一遍
 * （手机上是几百毫秒），所以"撞墙没动""结束后乱点"这些情况必须直接跳过重画。 */
int press(int btn) {
    if (btn == 7) {
        restart();
        return 1;
    }
    if (state == 2) return 0;              /* 结束了：除「重开」外一律无反应 */
    if (btn == 6) {
        if (state == 0) {
            state = 1;
            if (tid > 0) ui_timer_kill(tid);
            tid = 0;
        } else {
            state = 0;
            set_speed();
        }
        return 1;
    }
    if (state != 0) return 0;              /* 暂停中 */
    if (btn == 1) {
        if (collide(pid, rot, px - 1, py) != 0) return 0;
        px = px - 1;
        return 1;
    }
    if (btn == 2) {
        if (collide(pid, rot, px + 1, py) != 0) return 0;
        px = px + 1;
        return 1;
    }
    if (btn == 3) {
        step_down();
        score = score + 1;
        return 1;
    }
    if (btn == 4) {
        rotate_piece();
        return 1;
    }
    if (btn == 5) {
        hard_drop();
        return 1;
    }
    return 0;
}

/* 长按连发：左/右/下按住不放就每 130ms 再来一次（手机上点着走太累了）。 */
void hold_repeat(int btn) {
    held = btn;
    if (rid > 0) ui_timer_kill(rid);
    rid = ui_timer_set(130, 1);
}

void release(void) {
    held = 0;
    if (rid > 0) {
        ui_timer_kill(rid);
        rid = 0;
    }
}

/* ── 主循环 ─────────────────────────────────────────────── */

int main(void) {
    int msg[4];
    int t;
    int k;
    int btn;
    int reserveP;
    int availH;
    int cw;
    int chh;
    int padH;

    ui_piece_init();          /* 形状表一次初始化（几何都在共享库里，各语言共用一份） */

    /* **先问可用绘图区，再开窗** —— 窗口宽高就是画布的坐标空间，必须与排版同源。
     * 反过来（按屏幕 744 排版 / 开 360 宽的窗）会把内容画到画布外，实测就是"右边被切掉"。 */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;
    ui_win_open("俄罗斯方块", sw, sh);

    /* ── 布局（只算这一处）──
     *
     * 手柄：`padH` 是手柄区总高，`gk` 是方向键边长。三行键 + 两道 3px 缝 = 3*gk+6，
     * 底板再上下各留 6 ⇒ `gk` 取 39 时正好装得下 140。（底板高度 3*gk+18 = 135，
     * 键块 3*gk+6 = 123，`gy` 落在底板顶往下 6px 处，底边留 6px —— 上下都不贴边。）*/
    padH = 140;
    gk = 39;
    gstep = gk + 3;
    dpw = gk * 3 + 6;
    rs = gk + 20;
    gx = (sw - (dpw + 10 + rs * 2 + 8)) / 2;
    if (gx < 8) gx = 8;
    gy = sh - padH + 6;
    rx = gx + dpw + 10;

    reserveP = 84;
    if (sw < 340) reserveP = 72;
    availH = sh - padH - 16;
    cw = (sw - reserveP - 30) / BW;
    chh = availH / BH;
    cell = cw;
    if (chh < cell) cell = chh;
    if (cell < 8) cell = 8;
    bx = 10 + (sw - reserveP - 20 - BW * cell) / 2;
    if (bx < 6) bx = 6;
    by = 8 + (availH - BH * cell) / 2;
    if (by < 6) by = 6;
    panelX = bx + BW * cell + 10;
    panelW = sw - panelX - 8;
    if (panelW < 56) panelW = 56;

    /* ── 开局 ── */
    tid = 0;
    rid = 0;
    held = 0;
    score = 0;
    nlines = 0;
    level = 1;
    state = 0;
    npid = ui_rand(7);
    spawn();
    set_speed();
    btn = ui_dlg_msg("俄罗斯方块", "方向键移动与旋转，右边「直落」一放到底。"
                                  "返回箭头退出。", VML_DLG_INFO);
    draw_all();

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER) {
            if (msg[2] == 1) {                 /* tag 1 = 长按连发 */
                if (held != 0 && state == 0 && press(held) != 0) draw_all();
            } else if (state == 0) {           /* tag 0 = 重力 */
                step_down();
                draw_all();
            } else if (tid > 0) {
                /* 结束/暂停时把重力停掉 —— 否则定时器会一直往队列里投消息，
                 * 主循环空转不说，暂停期间还白耗电。重开时 set_speed 会重建。 */
                ui_timer_kill(tid);
                tid = 0;
            }
            continue;
        }

        if (t == VML_MSG_TOUCHUP || t == VML_MSG_MOUSEUP) {
            release();
            continue;
        }

        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            btn = hit_button(msg[1], msg[2]);
            if (btn == 0) continue;
            if (press(btn) == 0) continue;      /* 没变化就不重画 */
            if (btn == 1 || btn == 2 || btn == 3) hold_repeat(btn);
            else release();
            draw_all();
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            k = msg[1];
            if (k == VML_KEY_ESCAPE) break;
            if (k == VML_KEY_LEFT) btn = 1;
            else if (k == VML_KEY_RIGHT) btn = 2;
            else if (k == VML_KEY_DOWN) btn = 3;
            else if (k == VML_KEY_UP) btn = 4;
            else if (k == VML_KEY_SPACE) btn = 5;
            else if (k == VML_KEY_PAD_A) btn = 4;
            else if (k == VML_KEY_PAD_B) btn = 5;
            else if (k == VML_KEY_PAD_X) btn = 4;
            else if (k == VML_KEY_PAD_Y) btn = 5;
            else if (k == VML_KEY_SELECT) btn = 6;
            else if (k == VML_KEY_ENTER) btn = 7;
            else continue;
            if (press(btn) != 0) draw_all();
        }
    }

    release();
    if (tid > 0) ui_timer_kill(tid);
    ui_win_close();
    return 0;
}
