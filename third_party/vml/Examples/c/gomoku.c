/* gomoku.c —— 五子棋（人机对战），跑在手机端 VML 上
 *
 * 用到的都是现成的宿主接口：`waycoder_ui.h` 那套（窗体 / 绘图 / 触摸消息队列）。
 * 棋盘与棋子全部用绘图指令画出来，不依赖任何图片资源；棋盘尺寸按实际可用绘图区
 * （syscall #566/#567）自适应，所以同一份代码在手机、平板、模拟器上都能铺满。
 *
 * 编译运行（手机 App 的 vml 工具）：
 *     vml run gomoku.c
 * 桌面（无 UI 号段，只能验证编译与算法）：
 *     vmlhost run Examples/c/gomoku.c
 *
 * ⚠ 这里**不能**用全局变量去接 ${} 参数：`GenerateAsmStatement` 的 `${名}` 只查局部
 *   变量表（`variables`），全局变量在 `dataSection` 里、查不到，会退化成 `R12+0`。
 *   所以：棋盘是 main 的局部数组，靠参数传给各个函数；调 syscall 一律走
 *   `waycoder_ui.h` 的包装函数（它们的形参就是普通 C 参数，安全）。
 */

#include <waycoder_ui.h>

#define N 15          /* 15 路棋盘 */
#define EMPTY 0
#define BLACK 1       /* 人 */
#define WHITE 2       /* 电脑 */

#define COL_BG      0xFF1B1B22
#define COL_LINE    0xFF5A5A66
#define COL_BOARD   0xFFE8C48A
#define COL_BLACK   0xFF14141A
#define COL_WHITE   0xFFF2F2F6
#define COL_TEXT    0xFFEDEDF2
#define COL_ACCENT  0xFF4ADE80

/* ─────────── 棋盘几何（都是 main 算好、按参数传下去，避免全局变量）─────────── */

/* 把落点像素坐标换算成格线序号；越界返回 -1。 */
int hit_col(int x, int pad, int cell) {
    int c;
    c = (x - pad + cell / 2) / cell;
    if (c < 0) return -1;
    if (c >= N) return -1;
    return c;
}

/* ─────────── 胜负判定 ─────────── */

/* 从 (x,y) 出发沿 (dx,dy) 数同色连子（含自己），≥5 即胜。 */
int count_dir(int* b, int x, int y, int dx, int dy, int who) {
    int cnt;
    int cx;
    int cy;
    cnt = 1;
    cx = x + dx;
    cy = y + dy;
    while (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == who) {
        cnt = cnt + 1;
        cx = cx + dx;
        cy = cy + dy;
    }
    cx = x - dx;
    cy = y - dy;
    while (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == who) {
        cnt = cnt + 1;
        cx = cx - dx;
        cy = cy - dy;
    }
    return cnt;
}

int has_won(int* b, int x, int y, int who) {
    if (count_dir(b, x, y, 1, 0, who) >= 5) return 1;    /* 横 */
    if (count_dir(b, x, y, 0, 1, who) >= 5) return 1;    /* 竖 */
    if (count_dir(b, x, y, 1, 1, who) >= 5) return 1;    /* 撇 */
    if (count_dir(b, x, y, 1, -1, who) >= 5) return 1;   /* 捺 */
    return 0;
}

/* ─────────── 电脑棋力：单方向估值 ─────────── */

/* 假设 (x,y) 已被 who 占，沿 (dx,dy) 这条线的价值。
 * 两头都开放(活)与只有一头开放(眠)给的分差一个量级 —— 这是最简单的棋力来源。 */
int score_dir(int* b, int x, int y, int dx, int dy, int who) {
    int cnt;
    int openA;
    int openB;
    int cx;
    int cy;

    cnt = 1;
    openA = 0;
    openB = 0;

    cx = x + dx;
    cy = y + dy;
    while (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == who) {
        cnt = cnt + 1;
        cx = cx + dx;
        cy = cy + dy;
    }
    if (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == EMPTY) openA = 1;

    cx = x - dx;
    cy = y - dy;
    while (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == who) {
        cnt = cnt + 1;
        cx = cx - dx;
        cy = cy - dy;
    }
    if (cx >= 0 && cx < N && cy >= 0 && cy < N && b[cy * N + cx] == EMPTY) openB = 1;

    if (cnt >= 5) return 1000000;
    if (cnt == 4) {
        if (openA == 1 && openB == 1) return 100000;
        if (openA == 1 || openB == 1) return 10000;
        return 0;
    }
    if (cnt == 3) {
        if (openA == 1 && openB == 1) return 8000;
        if (openA == 1 || openB == 1) return 800;
        return 0;
    }
    if (cnt == 2) {
        if (openA == 1 && openB == 1) return 500;
        if (openA == 1 || openB == 1) return 50;
        return 0;
    }
    if (openA == 1 && openB == 1) return 10;
    return 3;
}

int score_cell(int* b, int x, int y, int who) {
    int s;
    s = 0;
    s = s + score_dir(b, x, y, 1, 0, who);
    s = s + score_dir(b, x, y, 0, 1, who);
    s = s + score_dir(b, x, y, 1, 1, who);
    s = s + score_dir(b, x, y, 1, -1, who);
    return s;
}

/* 挑一个最好的空点。返回 y*N+x；没有空点返回 -1。
 *
 * 打分 = 进攻分 + 防守分：
 *   · 自己能成五 → 进攻分 1000000，再加 5000000 保证"能赢就先赢"，
 *     不会被"挡住对方成五"同分抢走（两个都是 1000000，谁先扫到谁赢，那是掷骰子）；
 *   · 对方能成五 → 防守分 1000000 → 必然去堵。
 * 平手时选**更靠近中心**的点（中心先手价值高）。 */
int ai_pick(int* b, int firstEmpty) {
    int x;
    int y;
    int best;
    int bx;
    int by;
    int off;
    int def;
    int sc;
    int bd;
    int d;
    int cd;

    best = -1;
    bx = -1;
    by = -1;
    bd = 9999;

    for (y = 0; y < N; y = y + 1) {
        for (x = 0; x < N; x = x + 1) {
            if (b[y * N + x] != EMPTY) continue;
            off = score_cell(b, x, y, WHITE);
            def = score_cell(b, x, y, BLACK);
            sc = off + def;
            if (off >= 1000000) sc = sc + 5000000;

            d = x - 7;
            if (d < 0) d = 0 - d;
            cd = y - 7;
            if (cd < 0) cd = 0 - cd;
            d = d + cd;

            if (sc > best || (sc == best && d < bd)) {
                best = sc;
                bx = x;
                by = y;
                bd = d;
            }
        }
    }
    if (bx < 0) return -1;
    return by * N + bx;
}

/* ─────────── 绘制 ─────────── */

void draw_board(int* b, int pad, int padY, int cell, int lastIdx, int over) {
    int i;
    int x0;
    int y0;
    int x1;
    int y1;
    int cx;
    int cy;
    int r;
    int v;
    int half;
    char* s;

    half = (N - 1) * cell;
    ui_clear(COL_BG);

    /* 棋盘底 */
    ui_rect(pad - cell / 2, padY - cell / 2, half + cell, half + cell, COL_BOARD, 1, 0, 6);

    /* 格线 */
    for (i = 0; i < N; i = i + 1) {
        ui_line(pad, padY + i * cell, pad + half, padY + i * cell, COL_LINE, 1);
        ui_line(pad + i * cell, padY, pad + i * cell, padY + half, COL_LINE, 1);
    }

    /* 星位（15 路：4 个 (3,3) 型 + 天元） */
    ui_circle(pad + 3 * cell, padY + 3 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 11 * cell, padY + 3 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 3 * cell, padY + 11 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 11 * cell, padY + 11 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 7 * cell, padY + 7 * cell, 3, COL_LINE, 1, 0);

    /* 棋子。半径取格宽的五分之二 —— 留出缝，挨着的子不会糊成一片 */
    r = cell * 2 / 5;
    if (r < 3) r = 3;

    for (i = 0; i < N * N; i = i + 1) {
        v = b[i];
        if (v == EMPTY) continue;
        cx = pad + (i % N) * cell;
        cy = padY + (i / N) * cell;
        if (v == BLACK) {
            ui_circle(cx, cy, r, COL_BLACK, 1, 0);
        } else {
            ui_circle(cx, cy, r, COL_WHITE, 1, 0);
        }
        /* 最后一手加个记号，方便看清电脑刚下在哪 */
        if (i == lastIdx) ui_circle(cx, cy, r / 3, COL_ACCENT, 1, 0);
    }

    /* 状态行：用**状态式**文字接口 —— 属性设一次，之后只管给坐标和字符串 */
    if (over == 1) s = "你赢了！点任意处再来一局";
    else if (over == 2) s = "电脑赢了。点任意处再来一局";
    else if (over == 3) s = "平局。点任意处再来一局";
    else s = "你执黑，点棋盘落子";

    ui_set_font(15, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(pad, padY + half + cell, s);

    /* 结束时有结果，用强调色再画一遍（同一条字符串、同一套接口，只换了颜色） */
    if (over != 0) {
        ui_set_font(15, VML_FONT_BOLD | VML_FONT_ITALIC, COL_ACCENT, VML_ANCHOR_RIGHT);
        ui_text_cur(pad + half, padY + half + cell, s);
    }

    ui_present();
}

/* ─────────── 主循环 ─────────── */

int main(void) {
    int b[225];
    int msg[4];
    int sw;
    int sh;
    int cell;
    int pad;
    int padY;
    int avail;
    int i;
    int t;
    int x;
    int y;
    int col;
    int row;
    int idx;
    int over;      /* 0=进行中 1=人胜 2=电脑胜 3=平局 */
    int moves;
    int lastIdx;
    int aiIdx;
    int t0;
    int t1;
    char* title;

    /* 开局：棋盘清空 */
    for (i = 0; i < N * N; i = i + 1) b[i] = EMPTY;

    title = "五子棋";

    /* **先问可用绘图区，再开窗** —— 顺序不能反。
     *
     * `ui_scr_w/h` 读的是设备显示信息（不依赖窗口），所以开窗前就能拿到。
     * 而窗口的宽高就是**画布的坐标空间**：按 395 的宽度排版、却开一个 360 宽的窗口，
     * 棋盘右半边会被画到画布外面（实测就是"屏幕右边超出"——左边留 17、右边直接切掉）。
     * 所以尺寸必须与排版用的那个数**同源**。 */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;

    ui_win_open(title, sw, sh);

    /* 底下留一行状态文字的高度 */
    avail = sh - 34;
    if (avail > sw) avail = sw;

    cell = avail / (N + 1);
    if (cell < 4) cell = 4;

    pad = (sw - (N - 1) * cell) / 2;
    padY = (avail - (N - 1) * cell) / 2 + cell / 2;
    if (padY < cell / 2) padY = cell / 2;

    t0 = ui_dlg_msg("五子棋", "你执黑先行。点棋盘落子，返回箭头退出。", VML_DLG_INFO);

    over = 0;
    moves = 0;
    lastIdx = -1;
    draw_board(b, pad, padY, cell, lastIdx, over);

    /* 主循环：一个统一的消息队列，取到触摸就换算格子 */
    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;                     /* 超时（这里不会发生，0=无限等） */
        if (t == VML_MSG_WINDOWCLOSE) break;
        if (t != VML_MSG_TOUCHDOWN && t != VML_MSG_MOUSEDOWN) continue;

        x = msg[1];
        y = msg[2];

        /* 已经结束 → 点任意处重开 */
        if (over != 0) {
            for (i = 0; i < N * N; i = i + 1) b[i] = EMPTY;
            over = 0;
            moves = 0;
            lastIdx = -1;
            draw_board(b, pad, padY, cell, lastIdx, over);
            continue;
        }

        col = hit_col(x, pad, cell);
        row = hit_col(y, padY, cell);
        if (col < 0 || row < 0) continue;

        idx = row * N + col;
        if (b[idx] != EMPTY) continue;             /* 这儿有子了 */

        /* 人落子 */
        b[idx] = BLACK;
        moves = moves + 1;
        lastIdx = idx;
        if (has_won(b, col, row, BLACK) == 1) {
            over = 1;
            draw_board(b, pad, padY, cell, lastIdx, over);
            continue;
        }
        if (moves >= N * N) {
            over = 3;
            draw_board(b, pad, padY, cell, lastIdx, over);
            continue;
        }

        /* 先画一手人的，让手感立刻有反馈，再算电脑的 */
        draw_board(b, pad, padY, cell, lastIdx, over);

        /* 电脑落子 */
        aiIdx = ai_pick(b, 0);
        if (aiIdx < 0) {
            over = 3;
            draw_board(b, pad, padY, cell, lastIdx, over);
            continue;
        }
        b[aiIdx] = WHITE;
        moves = moves + 1;
        lastIdx = aiIdx;
        col = aiIdx % N;
        row = aiIdx / N;
        if (has_won(b, col, row, WHITE) == 1) over = 2;
        else if (moves >= N * N) over = 3;

        draw_board(b, pad, padY, cell, lastIdx, over);
    }

    /* 收尾：等一小会儿再关，免得窗口一闪而过（宿主定时刷新，这里只是留个缓冲） */
    ui_timer_set(120, 0);
    t1 = ui_wait(msg, 400);
    ui_win_close();
    return 0;
}
