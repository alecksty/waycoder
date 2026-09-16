/* tetris.c —— 俄罗斯方块（C 版），跑在手机端 VML 上
 *
 * 用到的都是现成的宿主接口（`waycoder_ui.h` 那套：窗体 / 绘图 / 消息队列 / 定时器），
 * 外加 v0.96.172 新加的**手感接口**（`ui_beep` / `ui_vibrate` / `ui_keep_on` / `ui_store_*`）。
 * 布局全部按实际可用绘图区（syscall #566/#567）算，所以手机、平板、模拟器上都能铺满。
 *
 * 编译运行（手机 App 的 vml 工具）：
 *     vml run tetris.c
 *
 * 操作全部走**绘图窗口底部那排屏幕手柄**（方向键 + START/SELECT + X/Y/A/B），
 * 手柄按键就是 Win32 虚拟键值，接物理键盘也是同一套：
 *     ← →      左右移动（按住连发）
 *     ↓        加速下落（按住连发）
 *     ↑ / A / X 旋转
 *     空格 / B / Y 直落到底
 *     START    重开一局
 *     SELECT   暂停 / 继续
 *     返回箭头  退出
 *
 * ## 为什么不自己画手柄、也不判触摸（v0.96.173 去掉的）
 *
 * 上一版在窗口里自绘了一套十字键 + 旋转/直落/暂停/重开，靠触摸命中去判按键。
 * 那是**多此一举**：绘图窗口底部本来就有一排屏幕手柄（`DrawWindowPage`），
 * 程序只要收 `VML_MSG_KEYDOWN` 就行。自绘那套的代价是实打实的 ——
 *   1. 要占掉约 140px 的窗口高度（棋盘就矮一截，手机上少两行）；
 *   2. 几何要在"画"与"命中判定"两处各算一遍（本文件上一版专门写了段注释讲这件事），
 *      改个间距就会出现"看着在键上、点下去没反应"；
 *   3. 每个游戏各画一套，风格互不相同，用户还得重新学一遍；
 *   4. 触摸与按键两条输入路径并存，程序里两套状态（`held` 与键盘）容易不一致。
 * 现在**只认按键消息**，触摸消息一概不处理 —— 窗口就是一块显示区。
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
 * ## 存档那条链上的两条前端约束（v0.96.173 实测出来的）
 *
 * `ui_store_get(key, buf, cap)` 把字符串写进调用方给的缓冲区，用 C 读回来时要绕开两个坑：
 *   · **缓冲区必须放全局**。`char buf[64]` 这种局部数组，把地址传给函数是错的
 *     （实测 `strcpy(局部, "12345")` 之后读出来是空/乱码，换成全局就正确）——
 *     所以 `hibuf` 是文件级全局，不是 `main` 里的局部。
 *   · **不能用 `buf[0] == '1'` 这种下标读**。全局 `char` 数组用下标读会读成 32 位
 *     （`g[0]='A'` 之后 `g[0]=='A'` 是 false），必须走 `atoi` / `strcmp` 这类
 *     按字节读的库函数。这里用 `atoi` 正好——最高分本来就是个数。
 * 两条都在 `.scratch/vmlround` 里量过，是**既有**的前端缺陷，与本次改动无关。
 */

#include <waycoder_ui.h>
#include <stdlib.h>

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
#define COL_PANEL   0xFF1B1B26
#define COL_SHADE   0xCC000000
#define COL_GOLD    0xFFFACC15

/* 存档键（`ui_store_*` 会再加一层 `vml.` 前缀，与 App 自己的配置隔开） */
#define KEY_HI "tetris.hi"

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
int best;           /* 最高分（从存档里读出来，破纪录时写回去） */
int nlines;
int level;

int state;          /* 0 = 运行 1 = 暂停 2 = 结束 */
int tid;            /* 重力定时器 id（0 = 没有） */
int rid;            /* 长按连发定时器 id（0 = 没有） */
int held;           /* 正在按住的方向键（1 左 2 右 3 下，0 = 没按） */
int rptLeft;        /* 这次连发还剩几拍（见 hold_repeat 的"自限"说明） */
int rptStuck;       /* 连续几拍"按了但没动" */
int dropMs;

/* ── 布局（main 里算一次，绘图全读它） ──────────────────── */

int sw;
int sh;
int cell;           /* 格子边长 */
int bx;             /* 棋盘左上角 */
int by;
int panelX;         /* 右侧信息面板 */
int panelW;

/* 数字的中间缓冲（`draw_int` / `score_to_str` 用；全局 int 数组，实测可用） */
int digs[12];
int sdigs[12];

/* 存档缓冲：**必须是全局**（局部数组的地址传给函数是错的，见文件头） */
char hibuf[16];     /* ui_store_get 读进来 */
char hiout[16];     /* 自己逐位拼出去给 ui_store_set */

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

/* ── 手感：音效与震动 ───────────────────────────────────── */

/* 设计原则：**动作用"手感"回话，不堆气氛**。
 *
 * 合成音是**单通道**的（`ui_beep` 一来就把上一个音停掉，见 VmlAudio.ToneCore），
 * 所以这里**一次事件只发一个音**，靠"频率高低"表达好坏，而不是连发一串琶音 ——
 * 连发的话只有最后一个音听得见，等于白写。
 * 频率从低到高：闷响（落地）< 干音（消一行）< 亮音（消四行 / 升级）。 */
void sfx(int hz, int ms) {
    ui_beep(hz, ms);
}

/* 消行：行数越多音越高、越长 —— 一耳朵就能听出"这波赚了"。 */
void sfx_clear(int n) {
    if (n == 1) sfx(880, 110);
    else if (n == 2) sfx(1046, 130);
    else if (n == 3) sfx(1318, 160);
    else sfx(1568, 220);
    ui_vibrate(28);
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

/* ── 存档：最高分 ───────────────────────────────────────── */

/* 把非负整数写成十进制字符串（C 前端的字符串拼接不可靠，逐位自己拼）。
 * 顺序**必须从前到后**、最后补一个 0 —— 全局 char 数组的赋值是 32 位写，
 * 正序写下来每个下标的低字节都是对的，末尾那个 0 顺手把后面几个字节一起清零。 */
void score_to_str(int v, char* dst) {
    int n;
    int t;
    int i;
    if (v < 0) v = 0;
    n = 0;
    t = v;
    while (t > 0) {
        sdigs[n] = t % 10;
        t = t / 10;
        n = n + 1;
    }
    if (n == 0) {
        sdigs[0] = 0;
        n = 1;
    }
    i = 0;
    while (i < n) {
        dst[i] = 48 + sdigs[n - 1 - i];
        i = i + 1;
    }
    dst[n] = 0;
}

void load_best(void) {
    best = 0;
    if (ui_store_get(KEY_HI, hibuf, 16) >= 0) best = atoi(hibuf);
    if (best < 0) best = 0;
}

void save_best(void) {
    score_to_str(best, hiout);
    ui_store_set(KEY_HI, hiout);
}

void submit_score(void) {
    if (score > best) {
        best = score;
        save_best();
    }
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
    if (collide(pid, rot, px, py) != 0) {   /* 出生位就满了 = 结束 */
        state = 2;
        submit_score();
        sfx(220, 420);
        ui_vibrate(220);
    }
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
    int lv;

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
        lv = nlines / 10 + 1;
        if (lv > 12) lv = 12;
        if (lv > level) {
            level = lv;
            sfx(1760, 150);            /* 升级盖过消行音：升级更值得听见 */
            ui_vibrate(60);
        } else {
            sfx_clear(n);
        }
        set_speed();
    } else {
        sfx(200, 35);                  /* 自然落地：一声闷响，不震 */
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
    sfx(1200, 22);
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
    sfx(150, 70);                      /* 低频闷响 = "砸下去了" */
    ui_vibrate(22);
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
    rptLeft = 0;
    rptStuck = 0;
    if (rid > 0) {
        ui_timer_kill(rid);
        rid = 0;
    }
    npid = ui_rand(7);
    spawn();
    set_speed();
    sfx(900, 70);
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

/* 数字：**逐位画**，不拼字符串（拼字符串那条路对本前端不可靠，而十个数字字面量就够了）。 */
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

void draw_panel(void) {
    int i;
    int v;
    int x;
    int y;
    int mini;
    int pw;
    int ty;

    ui_rect(panelX, by, panelW, BH * cell, COL_PANEL, 1, 0, 8);

    x = panelX + panelW / 2;

    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(x, by + 10, "下一个");

    mini = cell / 2;
    if (mini < 7) mini = 7;
    pw = mini * 4;
    i = 0;
    while (i < 4) {
        v = ui_piece_cell(npid, 0, i);
        if (v >= 0) {
            y = v % 16;
            draw_block(panelX + (panelW - pw) / 2 + (v / 16) * mini, by + 32 + y * mini, npid, mini);
        }
        i = i + 1;
    }

    ty = by + 32 + mini * 4 + 18;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "分数");
    draw_int(panelX + 10, ty + 16, score, 17, COL_TEXT);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "最高");
    draw_int(panelX + 10, ty + 16, best, 17, COL_GOLD);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "消行");
    draw_int(panelX + 10, ty + 16, nlines, 17, COL_TEXT);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, "等级");
    draw_int(panelX + 10, ty + 16, level, 17, COL_ACCENT);
}

/* 暂停 / 结束的遮罩 + 两行提示。提示文案要写**手柄上的键名** ——
 * 屏幕上已经没有自绘按键了，写"点「重开」"用户找不到那个东西。 */
void draw_overlay(void) {
    int w;
    int h;
    int x;
    int y;
    char* s;
    char* s2;
    if (state == 0) return;
    w = BW * cell - 16;
    h = 84;
    x = bx + 8;
    y = by + (BH * cell - h) / 2;
    ui_rect(bx, by, BW * cell, BH * cell, COL_SHADE, 1, 0, 0);
    ui_rect(x, y, w, h, COL_PANEL, 1, 0, 10);
    if (state == 2) {
        s = "游戏结束";
        s2 = "按 START 再来一局";
    } else {
        s = "已暂停";
        s2 = "按 SELECT 继续";
    }
    ui_set_font(20, VML_FONT_BOLD, COL_WARN, VML_ANCHOR_CENTER);
    ui_text_cur(x + w / 2, y + 16, s);
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_CENTER);
    ui_text_cur(x + w / 2, y + 52, s2);
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
    draw_overlay();
    ui_present();
}

/* ── 动作派发 ───────────────────────────────────────────── */

/* 按一下某个键。返回 **画面是否需要重画** —— 每次重画都要把整幅场景光栅化一遍
 * （手机上是几百毫秒），所以"撞墙没动""结束后乱按"这些情况必须直接跳过重画。 */
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
            sfx(500, 60);
        } else {
            state = 0;
            set_speed();
            sfx(700, 60);
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

/* 长按连发：左/右/下按住不放就每 130ms 再来一次。
 *
 * 靠 `VML_MSG_KEYUP` 收尾 —— 手柄按下发 KeyDown、抬手发 KeyUp（`DrawWindowPage`
 * 用 Button 的 Pressed/Released，不再是一按就 Down+Up 一起发）。但**不能把"一定会收到
 * KeyUp"当成前提**：手指划出按键范围、系统吃掉 CANCEL、页面被切走，都可能让 KeyUp 永远不来，
 * 而连发一旦跑起来就会一直跑（`ui_timer_set` 是重复定时器）。
 * 所以连发**自带三道刹车**，任何一道都不会让程序卡在"一直往左移"上：
 *   1. **换键即接管** —— 另一个方向键的 KeyDown 直接改写 `held`；
 *   2. **按了没动两次就停** —— 已经贴墙了还按，说明再按也没意义；
 *   3. **总拍数上限 40**（约 5 秒）—— 横穿整个棋盘只要 10 拍、竖到底最多 20 拍，
 *      40 拍远超任何真实操作，纯粹是丢 KeyUp 时的兜底。
 * 三道里前两道是"手感"，第三道是"安全网"；写游戏时**重复定时器都要有这么一道**。 */
void hold_repeat(int btn) {
    if (held == btn) {
        if (rid > 0) return;           /* 同一键重复按下（系统重复键）：不重建定时器 */
        rid = ui_timer_set(130, 1);
        return;
    }
    held = btn;
    rptLeft = 40;
    rptStuck = 0;
    if (rid > 0) ui_timer_kill(rid);
    rid = ui_timer_set(130, 1);
}

void release(void) {
    held = 0;
    rptLeft = 0;
    rptStuck = 0;
    if (rid > 0) {
        ui_timer_kill(rid);
        rid = 0;
    }
}

/* 连发的一拍。返回画面是否要重画。 */
int repeat_tick(void) {
    if (held == 0 || state != 0) return 0;
    rptLeft = rptLeft - 1;
    if (press(held) != 0) {
        rptStuck = 0;
        if (rptLeft <= 0) release();
        return 1;
    }
    rptStuck = rptStuck + 1;
    if (rptStuck >= 2 || rptLeft <= 0) release();
    return 0;
}

/* 键码 → 按钮码；0 = 这个键不做事。
 * 手柄那几个键在 `VmlKeys` 里刻意映射成了自然键盘等价键（A/B/X/Y 就是字母键、
 * START=回车、SELECT=Shift），所以同一份程序接物理键盘也能玩。 */
int key_to_btn(int k) {
    if (k == VML_KEY_LEFT) return 1;
    if (k == VML_KEY_RIGHT) return 2;
    if (k == VML_KEY_DOWN) return 3;
    if (k == VML_KEY_UP) return 4;
    if (k == VML_KEY_PAD_A) return 4;
    if (k == VML_KEY_PAD_X) return 4;
    if (k == VML_KEY_SPACE) return 5;
    if (k == VML_KEY_PAD_B) return 5;
    if (k == VML_KEY_PAD_Y) return 5;
    if (k == VML_KEY_SELECT) return 6;
    if (k == VML_KEY_PAUSE) return 6;
    if (k == VML_KEY_ENTER) return 7;
    return 0;
}

/* ── 主循环 ─────────────────────────────────────────────── */

int main(void) {
    int msg[4];
    int t;
    int k;
    int btn;
    int reserveP;
    int availH;
    int availW;
    int cw;
    int chh;

    ui_piece_init();          /* 形状表一次初始化（几何都在共享库里，各语言共用一份） */

    /* **先问可用绘图区，再开窗** —— 窗口宽高就是画布的坐标空间，必须与排版同源。
     * 反过来（按屏幕 744 排版 / 开 360 宽的窗）会把内容画到画布外，实测就是"右边被切掉"。 */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;
    ui_win_open("俄罗斯方块", sw, sh);

    /* 玩游戏时别熄屏 —— 一手不动盯着棋盘想下一步，屏幕自己黑了最扫兴。
     * 退出时会关掉（见文件末尾），所以不会一直亮着。 */
    ui_keep_on(1);

    /* ── 布局（只算这一处）──
     *
     * 手柄交给系统之后，整个窗口高度都归棋盘用（旧版要留 140px 画手柄）。
     * `reserveP` 是右侧信息面板的宽度，窄屏收一点。 */
    reserveP = 88;
    if (sw < 360) reserveP = 76;
    availW = sw - reserveP - 24;
    availH = sh - 16;
    cw = availW / BW;
    chh = availH / BH;
    cell = cw;
    if (chh < cell) cell = chh;
    if (cell < 8) cell = 8;
    bx = 8 + (availW - BW * cell) / 2;
    if (bx < 6) bx = 6;
    by = 8 + (availH - BH * cell) / 2;
    if (by < 6) by = 6;
    panelX = sw - reserveP - 8;
    panelW = reserveP;
    if (panelX < bx + BW * cell + 6) panelX = bx + BW * cell + 6;

    /* ── 开局 ── */
    tid = 0;
    rid = 0;
    held = 0;
    score = 0;
    nlines = 0;
    level = 1;
    state = 0;
    load_best();
    npid = ui_rand(7);
    spawn();
    set_speed();
    ui_dlg_msg("俄罗斯方块", "用屏幕下方的游戏按键操作："
                             "方向键移动与旋转、A 旋转、B 直落，START 重开、SELECT 暂停。"
                             "返回箭头退出。", VML_DLG_INFO);
    draw_all();

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER) {
            if (msg[2] == 1) {                 /* tag 1 = 长按连发 */
                if (repeat_tick() != 0) draw_all();
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

        if (t == VML_MSG_KEYUP) {
            k = msg[1];
            /* 只有"抬起的是当前按住的键"才结束连发；否则连发的节奏会被
             * 另一个方向键的抬起打断（屏幕上同时按两个键是很常见的）。 */
            if (held != 0 && key_to_btn(k) == held) release();
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            k = msg[1];
            if (k == VML_KEY_ESCAPE) break;
            btn = key_to_btn(k);
            if (btn == 0) continue;
            if (press(btn) != 0) draw_all();
            /* 连发只挂"能连续做的动作"，重开/暂停那种一次性键不挂 */
            if (btn == 1 || btn == 2 || btn == 3) hold_repeat(btn);
        }
    }

    release();
    if (tid > 0) ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
    return 0;
}
