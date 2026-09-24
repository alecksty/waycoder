/* chess.c —— 中国象棋（人机对战），跑在手机端 VML 上
 *
 * 用法（手机 App 的 vml 工具）：  vml run examples/c/chess.c
 * 桌面（无 UI 号段，只能验编译与规则）：
 *     vmlcli chess.c -I . --vml /tmp/chess.vml        # 只编译
 *     vmlcli .scratch/chessprobe/probe.c -I Examples/c   # 跑规则自测（见 .scratch/chessprobe）
 *
 * 界面全部用绘图指令画出来（棋盘线、九宫斜线、楚河汉界、棋子圆 + 居中汉字），
 * 不依赖任何图片与字体资源；尺寸按实际可用绘图区自适应。
 *
 * ## 规则部分与界面部分是**分开的**
 *
 * 上面那一半（`can_move` / `in_check` / `legal_move` / `gen_moves` / `has_legal` /
 * `ai_pick`）**没有一处开窗、画图或读输入**，因此可以在桌面被一个探针 `#include`
 * 进来单测（`#ifndef CHESS_LIB_ONLY` 包住 main 就是为这个）—— 象棋的规则漏洞（蹩马腿、
 * 塞象眼、炮翻山、将帅照面）在手机上靠点屏幕是**测不完**的，必须有可跑的判据。
 * 判据里最硬的一条：**开局红方合法着法恰好 44 步**（象棋 perft(1) 的标准值），
 * 数不对就是走法生成器错了。
 *
 * ⚠ 唯一的例外是 `ai_pick` 末尾那句 `ui_rand(9)`（同分抖动）—— 它**不是**开窗/绘图，
 *   落到 VM 的 `#50` 号 syscall 上，所以探针里照样调得动（实测：
 *   `.scratch/chessprobe/aiprobe.c` 直接调 `ai_pick` 断言它该不该吃白送的子）。
 *   真要在没有 `ui_*` 号段的宿主上复用这一半，把那句换成常量即可。
 *
 * ## 为什么不用全局变量
 *
 * 与 gomoku.c 同一条约束：`GenerateAsmStatement` 的 `${名}` 只查**局部**变量表，
 * 全局变量在 dataSection 里、查不到，会退化成 `R12+0`。所以棋盘是 main 的局部数组、
 * 靠参数传下去；调宿主一律走 `waycoder_ui.h` 的包装函数（形参是普通 C 参数，安全）。
 *
 * ## 棋盘坐标
 *
 * 9 列 × 10 行，扁平数组 `b[y * 9 + x]`。**y = 0 是黑方底线（屏幕上边）**，
 * y = 9 是红方底线（屏幕下边）。人是红方（在下），电脑是黑方（在上）。
 * 棋子编码 = `方 * 10 + 种类`：红 1..7、黑 11..17，0 = 空 —— 于是
 * `v / 10` 是方、`v % 10` 是种类，两个判断都是一次整除，没有负号参与。
 */

#include <waycoder_ui.h>

#define BW 9
#define BH 10
/* 数组维度一律写这个字面量：`int t[BW * BH]` 要求前端对宏表达式做常量折叠，不值得赌。
 * ⚠ 注释**不能跨行**跟在 `#define` 同一行后面 —— 前端按行处理 `#define`，跨行的块注释
 *   会在换行处被截断，第二行（以 `*` 开头）就当成代码了，里面的全角标点直接报
 *   「未知字符：，」。实测过（本文件 v1 就是这么红的）。 */
#define CELLS 90

/* 种类（v % 10） */
#define T_K 1   /* 帅 / 将 */
#define T_A 2   /* 仕 / 士 */
#define T_B 3   /* 相 / 象 */
#define T_N 4   /* 马 */
#define T_R 5   /* 车 */
#define T_C 6   /* 炮 */
#define T_P 7   /* 兵 / 卒 */

/* 方（v / 10） */
#define S_RED 0
#define S_BLK 1

#define MAXM 240   /* 着法上限：合法着法远不到这里，纯粹防越界 */

/* 配色 */
#define COL_BG      0xFF14100C
#define COL_BOARD   0xFFE9C88F
#define COL_LINE    0xFF6B4A22
#define COL_RED     0xFFB3261E
#define COL_BLACK   0xFF14100C
#define COL_PIECE   0xFFF8EEDC
#define COL_TEXT    0xFFEDEDF2
#define COL_DIM     0xFF8A8A93
#define COL_SEL     0xFF1B7F3B
#define COL_BTN     0xFF2A2A33
#define COL_BTN_TX  0xFFEDEDF2
#define COL_ACCENT  0xFFE8B23A

/* ═══════════════════════ 规则（不碰 ui_*，可在桌面单测）═══════════════════════ */

/* 象/相 不过河：红方只能待在 y>=5，黑方只能 y<=4 */
int elephant_side_ok(int y, int s) {
    if (s == S_RED) { if (y >= 5) return 1; return 0; }
    if (y <= 4) return 1;
    return 0;
}

/* 九宫：列恒为 3..5；红方行 7..9，黑方行 0..2 */
int in_palace(int x, int y, int s) {
    if (x < 3 || x > 5) return 0;
    if (s == S_RED) { if (y >= 7 && y <= 9) return 1; return 0; }
    if (y >= 0 && y <= 2) return 1;
    return 0;
}

/* 兵/卒 过河了吗：红方向前是 y 变小，过河即 y<=4；黑方相反 */
int pawn_crossed(int y, int s) {
    if (s == S_RED) { if (y <= 4) return 1; return 0; }
    if (y >= 5) return 1;
    return 0;
}

/* (fx,fy)→(tx,ty) 之间夹着几个子（不含两端）。不同行不同列返回 -1。
 * 车/炮 共用它 —— 炮"翻山"的炮架计数与车的"路径必须干净"是同一件事的两种用法。 */
int between_count(int* b, int fx, int fy, int tx, int ty) {
    int dx;
    int dy;
    int x;
    int y;
    int n;

    if (fx != tx && fy != ty) return -1;
    dx = 0;
    dy = 0;
    if (tx > fx) dx = 1;
    if (tx < fx) dx = -1;
    if (ty > fy) dy = 1;
    if (ty < fy) dy = -1;
    x = fx + dx;
    y = fy + dy;
    n = 0;
    while (x != tx || y != ty) {
        if (b[y * BW + x] != 0) n = n + 1;
        x = x + dx;
        y = y + dy;
    }
    return n;
}

/* 这步棋**符合该子的走法**吗（只看走法，不看走完会不会自己被将）。
 * 这是走法规则的**唯一实现**：走法生成本身也只管"落点候选"，成不成由它判。 */
int can_move(int* b, int fx, int fy, int tx, int ty) {
    int v;
    int s;
    int tp;
    int dx;
    int dy;
    int ax;
    int ay;
    int n;
    int mid;

    if (fx < 0 || fx >= BW || fy < 0 || fy >= BH) return 0;
    if (tx < 0 || tx >= BW || ty < 0 || ty >= BH) return 0;
    if (fx == tx && fy == ty) return 0;

    v = b[fy * BW + fx];
    if (v == 0) return 0;
    s = v / 10;
    tp = v % 10;

    /* 目标点不能是自己的子 */
    if (b[ty * BW + tx] != 0 && b[ty * BW + tx] / 10 == s) return 0;

    dx = tx - fx;
    dy = ty - fy;
    ax = dx;
    if (ax < 0) ax = 0 - ax;
    ay = dy;
    if (ay < 0) ay = 0 - ay;

    if (tp == T_R) {                       /* 车：直线、路径干净 */
        if (dx != 0 && dy != 0) return 0;
        if (between_count(b, fx, fy, tx, ty) != 0) return 0;
        return 1;
    }
    if (tp == T_C) {                       /* 炮：不吃子时同车；吃子必须正好翻一个炮架 */
        if (dx != 0 && dy != 0) return 0;
        n = between_count(b, fx, fy, tx, ty);
        if (b[ty * BW + tx] == 0) { if (n == 0) return 1; return 0; }
        if (n == 1) return 1;
        return 0;
    }
    if (tp == T_N) {                       /* 马：走日 + **蹩马腿** */
        if (!((ax == 1 && ay == 2) || (ax == 2 && ay == 1))) return 0;
        if (ax == 2) {
            mid = (fx + tx) / 2;           /* 先横着走两格 → 横方向的相邻格是腿 */
            if (b[fy * BW + mid] != 0) return 0;
        } else {
            mid = (fy + ty) / 2;           /* 先竖着走两格 → 竖方向的相邻格是腿 */
            if (b[mid * BW + fx] != 0) return 0;
        }
        return 1;
    }
    if (tp == T_B) {                       /* 相/象：走田 + **塞象眼** + 不过河 */
        if (ax != 2 || ay != 2) return 0;
        if (elephant_side_ok(ty, s) == 0) return 0;
        if (b[((fy + ty) / 2) * BW + (fx + tx) / 2] != 0) return 0;
        return 1;
    }
    if (tp == T_A) {                       /* 仕/士：走斜一格，不出九宫 */
        if (ax != 1 || ay != 1) return 0;
        if (in_palace(tx, ty, s) == 0) return 0;
        return 1;
    }
    if (tp == T_K) {                       /* 帅/将：走直一格，不出九宫（照面规则在 in_check 里） */
        if (ax + ay != 1) return 0;
        if (in_palace(tx, ty, s) == 0) return 0;
        return 1;
    }
    if (tp == T_P) {                       /* 兵/卒：只进不退；过河后才能横走 */
        if (s == S_RED) {
            if (dx == 0 && dy == -1) return 1;
            if (pawn_crossed(fy, s) == 1 && dy == 0 && ax == 1) return 1;
            return 0;
        }
        if (dx == 0 && dy == 1) return 1;
        if (pawn_crossed(fy, s) == 1 && dy == 0 && ax == 1) return 1;
        return 0;
    }
    return 0;
}

/* 两个帅/将是否**照面**（同一列且中间无子）—— 这是象棋特有的禁着：
 * 谁走成照面就等于把帅送给对方吃，所以它必须算作"被将"。 */
int kings_face(int* b) {
    int rk;
    int bk;
    int i;
    int x;
    int y;
    int y1;
    int y2;
    int t;
    int cnt;

    rk = -1;
    bk = -1;
    for (i = 0; i < BW * BH; i = i + 1) {
        if (b[i] == S_RED * 10 + T_K) rk = i;
        if (b[i] == S_BLK * 10 + T_K) bk = i;
    }
    if (rk < 0 || bk < 0) return 0;
    if (rk % BW != bk % BW) return 0;
    x = rk % BW;
    /* ⚠ 两个帅**谁在谁的上面不定**：红在下（y 大）、黑在上（y 小），所以红帅的下标
     *   **大于**黑将。这里必须比较后取小/大 —— 直接 `y = rk/BW + 1; while (y < bk/BW)`
     *   会在初始局面下**一次都不进循环**（10 < 0 为假），于是把"两个王之间塞满了子"
     *   误判成"照面" ⇒ 任何着法走完都算自己被将 ⇒ 全部着法非法、开局 44 步变 0 步。
     *   这个错是探针的 perft 判据抓出来的（规则自测存在的意义）。 */
    y1 = rk / BW;
    y2 = bk / BW;
    if (y1 > y2) {
        t = y1;
        y1 = y2;
        y2 = t;
    }
    cnt = 0;
    y = y1 + 1;
    while (y < y2) {
        if (b[y * BW + x] != 0) cnt = cnt + 1;
        y = y + 1;
    }
    if (cnt == 0) return 1;
    return 0;
}

/* 试一个落点：在盘内且 can_move 通过就记进 out，返回新的个数。
 * 走法生成全靠它 —— 于是"生成"与"判定"共用 can_move 这一份规则。 */
int try_to(int* b, int fx, int fy, int tx, int ty, int* out, int n) {
    if (tx < 0 || tx >= BW || ty < 0 || ty >= BH) return n;
    if (can_move(b, fx, fy, tx, ty) == 0) return n;
    out[n] = (fy * BW + fx) * 100 + (ty * BW + tx);
    return n + 1;
}

/* 车/炮 沿 (dx,dy) 一条射线上的着法。is_cannon=1 时按"翻一个炮架吃第一个子"办。 */
int ray_moves(int* b, int s, int fx, int fy, int dx, int dy, int is_cannon, int* out, int n) {
    int x;
    int y;
    int v;
    int screen;

    x = fx + dx;
    y = fy + dy;
    screen = 0;
    while (x >= 0 && x < BW && y >= 0 && y < BH) {
        v = b[y * BW + x];
        if (v == 0) {
            if (screen == 0) {
                out[n] = (fy * BW + fx) * 100 + (y * BW + x);
                n = n + 1;
            }
        } else {
            if (screen == 0) {
                if (v / 10 != s) {          /* 车吃到子；炮此时只是搭上炮架 */
                    if (is_cannon == 0) {
                        out[n] = (fy * BW + fx) * 100 + (y * BW + x);
                        n = n + 1;
                    }
                }
                if (is_cannon == 0) return n;
                screen = 1;
            } else {
                if (v / 10 != s) {          /* 炮翻过炮架，吃它后面第一个子 */
                    out[n] = (fy * BW + fx) * 100 + (y * BW + x);
                    n = n + 1;
                }
                return n;
            }
        }
        x = x + dx;
        y = y + dy;
    }
    return n;
}

/* 生成 s 方**全部走法**（只看走法，不看走完是否自己被将）。返回条数。 */
int gen_moves(int* b, int s, int* out) {
    int i;
    int n;
    int fx;
    int fy;
    int tp;

    n = 0;
    for (i = 0; i < BW * BH; i = i + 1) {
        if (b[i] == 0) continue;
        if (b[i] / 10 != s) continue;
        fx = i % BW;
        fy = i / BW;
        tp = b[i] % 10;

        if (tp == T_R) {
            n = ray_moves(b, s, fx, fy, 1, 0, 0, out, n);
            n = ray_moves(b, s, fx, fy, -1, 0, 0, out, n);
            n = ray_moves(b, s, fx, fy, 0, 1, 0, out, n);
            n = ray_moves(b, s, fx, fy, 0, -1, 0, out, n);
        } else if (tp == T_C) {
            n = ray_moves(b, s, fx, fy, 1, 0, 1, out, n);
            n = ray_moves(b, s, fx, fy, -1, 0, 1, out, n);
            n = ray_moves(b, s, fx, fy, 0, 1, 1, out, n);
            n = ray_moves(b, s, fx, fy, 0, -1, 1, out, n);
        } else if (tp == T_N) {
            n = try_to(b, fx, fy, fx + 1, fy - 2, out, n);
            n = try_to(b, fx, fy, fx - 1, fy - 2, out, n);
            n = try_to(b, fx, fy, fx + 2, fy - 1, out, n);
            n = try_to(b, fx, fy, fx - 2, fy - 1, out, n);
            n = try_to(b, fx, fy, fx + 1, fy + 2, out, n);
            n = try_to(b, fx, fy, fx - 1, fy + 2, out, n);
            n = try_to(b, fx, fy, fx + 2, fy + 1, out, n);
            n = try_to(b, fx, fy, fx - 2, fy + 1, out, n);
        } else if (tp == T_B) {
            n = try_to(b, fx, fy, fx + 2, fy - 2, out, n);
            n = try_to(b, fx, fy, fx - 2, fy - 2, out, n);
            n = try_to(b, fx, fy, fx + 2, fy + 2, out, n);
            n = try_to(b, fx, fy, fx - 2, fy + 2, out, n);
        } else if (tp == T_A) {
            n = try_to(b, fx, fy, fx + 1, fy - 1, out, n);
            n = try_to(b, fx, fy, fx - 1, fy - 1, out, n);
            n = try_to(b, fx, fy, fx + 1, fy + 1, out, n);
            n = try_to(b, fx, fy, fx - 1, fy + 1, out, n);
        } else if (tp == T_K) {
            n = try_to(b, fx, fy, fx + 1, fy, out, n);
            n = try_to(b, fx, fy, fx - 1, fy, out, n);
            n = try_to(b, fx, fy, fx, fy + 1, out, n);
            n = try_to(b, fx, fy, fx, fy - 1, out, n);
        } else if (tp == T_P) {
            if (s == S_RED) n = try_to(b, fx, fy, fx, fy - 1, out, n);
            else n = try_to(b, fx, fy, fx, fy + 1, out, n);
            if (pawn_crossed(fy, s) == 1) {
                n = try_to(b, fx, fy, fx + 1, fy, out, n);
                n = try_to(b, fx, fy, fx - 1, fy, out, n);
            }
        }
        if (n >= MAXM - 8) return n;       /* 上限保护：宁可少列也不要越界 */
    }
    return n;
}

/* s 方被将军吗（含将帅照面）。
 *
 * 判据是**"对方有没有哪一步能落到我的帅上"** —— 直接复用 gen_moves，
 * 不另写一套"从帅的位置反推谁在攻击我"的反向代码。反向那份要点名车/炮/马的
 * 攻击几何（马的腿还得反着判），同一套规则写两遍必然漂移；而这里代价只是
 * 一次走法生成（几十条），换来"走法规则只有一份"。 */
int in_check(int* b, int s) {
    int i;
    int k;
    int op;
    int m[MAXM];
    int n;
    int j;

    if (kings_face(b) == 1) return 1;
    k = -1;
    for (i = 0; i < BW * BH; i = i + 1) {
        if (b[i] == s * 10 + T_K) { k = i; break; }
    }
    if (k < 0) return 0;                    /* 帅已被吃（不该走到这儿） */
    op = 1 - s;
    n = gen_moves(b, op, m);
    for (j = 0; j < n; j = j + 1) {
        if (m[j] % 100 == k) return 1;
    }
    return 0;
}

/* 走一步并在副本上试算：符合走法 **且** 走完自己不被将 = 合法。 */
int legal_move(int* b, int fx, int fy, int tx, int ty) {
    int s;
    int t[CELLS];
    int i;

    if (can_move(b, fx, fy, tx, ty) == 0) return 0;
    s = b[fy * BW + fx] / 10;
    for (i = 0; i < BW * BH; i = i + 1) t[i] = b[i];
    t[ty * BW + tx] = t[fy * BW + fx];
    t[fy * BW + fx] = 0;
    if (in_check(t, s) == 1) return 0;
    return 1;
}

/* s 方还有没有合法着法（= 判断将死 / 困毙）。象棋里**困毙也算输**，与国象不同。 */
int has_legal(int* b, int s) {
    int m[MAXM];
    int n;
    int i;
    int f;
    int t;

    n = gen_moves(b, s, m);
    for (i = 0; i < n; i = i + 1) {
        f = m[i] / 100;
        t = m[i] % 100;
        if (legal_move(b, f % BW, f / BW, t % BW, t / BW) == 1) return 1;
    }
    return 0;
}

/* 合法着法条数 —— 单测用（开局应为 44）。 */
int count_legal(int* b, int s) {
    int m[MAXM];
    int n;
    int i;
    int f;
    int t;
    int c;

    c = 0;
    n = gen_moves(b, s, m);
    for (i = 0; i < n; i = i + 1) {
        f = m[i] / 100;
        t = m[i] % 100;
        if (legal_move(b, f % BW, f / BW, t % BW, t / BW) == 1) c = c + 1;
    }
    return c;
}

/* 子力价值（帅极大，保证"被吃帅"永远是最坏局面） */
int piece_value(int tp) {
    if (tp == T_K) return 10000;
    if (tp == T_R) return 900;
    if (tp == T_C) return 450;
    if (tp == T_N) return 400;
    if (tp == T_B) return 200;
    if (tp == T_A) return 200;
    if (tp == T_P) return 100;
    return 0;
}

int material(int* b, int s) {
    int i;
    int sum;

    sum = 0;
    for (i = 0; i < BW * BH; i = i + 1) {
        if (b[i] == 0) continue;
        if (b[i] / 10 != s) continue;
        sum = sum + piece_value(b[i] % 10);
    }
    return sum;
}

/* 局面小项：兵过河往前走值钱，车/炮占中路值一点 —— 只用来打破同分僵局。
 * ⚠ 收的是**动子**的种类（不是落点上那个子）—— 落点上是敌方子，拿它判"我的兵过河了"
 *   是张冠李戴；这个参数是调用方把起点上的子取出来传进来的。 */
int pos_bonus(int moverTp, int fromY, int tx, int ty, int s) {
    int sc;
    int d;

    sc = 0;
    if (fromY != ty) sc = sc + 2;           /* 动子比不动子略好 */
    if (moverTp == T_P) {
        if (s == S_RED && ty <= 4) sc = sc + 12;    /* 红兵过河 */
        if (s == S_BLK && ty >= 5) sc = sc + 12;
    }
    d = tx - 4;
    if (d < 0) d = 0 - d;
    if (moverTp == T_R || moverTp == T_C) sc = sc + (4 - d) * 2;
    return sc;
}

/* 电脑选一步。返回 from*100+to；无棋可走返回 -1。
 *
 * 深度 = **1 层 + 回吃**：对每个候选着法，算走完之后的子力差，再减掉对方
 * 立刻能吃回的最大价值 —— 这样它不会白送子，也会吃白送的子。
 * 只在**将军**时才额外做一次"对方还有没有棋"（将死判断），因为那一步最贵；
 * 于是 CPU 代价大致是"候选数 × 一次走法生成"，手机上零点几秒量级。
 * 同分之间用 ui_rand 抖一下，否则每局走得一模一样。 */
int ai_pick(int* b, int me) {
    int m[MAXM];
    int om[MAXM];
    int n;
    int on;
    int i;
    int j;
    int f;
    int t;
    int fx;
    int fy;
    int tx;
    int ty;
    int trial[CELLS];
    int k;
    int sc;
    int best;
    int bm;
    int opp;
    int worst;

    opp = 1 - me;
    best = -9999999;
    bm = -1;
    n = gen_moves(b, me, m);
    for (i = 0; i < n; i = i + 1) {
        f = m[i] / 100;
        t = m[i] % 100;
        fx = f % BW;
        fy = f / BW;
        tx = t % BW;
        ty = t / BW;
        if (legal_move(b, fx, fy, tx, ty) == 0) continue;

        for (k = 0; k < BW * BH; k = k + 1) trial[k] = b[k];
        trial[ty * BW + tx] = trial[fy * BW + fx];
        trial[fy * BW + fx] = 0;

        sc = material(trial, me) - material(trial, opp);
        sc = sc + pos_bonus(b[fy * BW + fx] % 10, fy, tx, ty, me);

        if (in_check(trial, opp) == 1) {
            sc = sc + 40;                       /* 将军本身有收益 */
            if (has_legal(trial, opp) == 0) return m[i];   /* 将死 / 困毙：立刻走 */
        }

        /* 对方能吃回多少 —— **只算"吃我这一步落的这个子"**。
         *
         * 原来是"对方在这个**新局面**里最好的一口"（在整盘上找最大的吃），
         * 那会把**与我这一步无关**的既有威胁一并算进来：开局时红炮正照着黑马
         * （隔着黑炮当炮架），于是"随便走一步"和"炮打马"都要扣掉那 400 ——
         * 两者都扣 = 等于没扣，两台候选剩下的差别就只有"炮打马多赚 400"，
         * 电脑于是**见马就换炮**（拿 450 的炮去吃 400 的马，白亏 50），
         * 正好与它上面那句注释"不会白送子"相反。实测（`.scratch/chessprobe/aiscore.c`
         * 把 44 个候选的评分逐项打出来）：26 个"随便走"全是 −358 上下，
         * 而两处"炮打马"是 −1 —— 于是每一步都挑炮打马。桌面上头一局就是这么开的。
         *
         * 判据收在**落点**上才是那个"吃**回**"：对方能不能吃掉我刚落的这个子。
         * 于是"把子送进对方嘴里"被扣分（本意），而"我这步与那口无关的旧账"不再重复计。 */
        worst = 0;
        on = gen_moves(trial, opp, om);
        for (j = 0; j < on; j = j + 1) {
            if (om[j] % 100 != ty * BW + tx) continue;   /* 落点以外的吃子与我这一步无关 */
            f = om[j] / 100;
            if (legal_move(trial, f % BW, f / BW, tx, ty) == 0) continue;
            worst = piece_value(trial[ty * BW + tx] % 10);   /* 送掉的就是刚走的这个子 */
            break;
        }
        sc = sc - worst * 9 / 10;
        sc = sc + ui_rand(9);                   /* 同分抖动，免得每局一样 */
        if (sc > best) {
            best = sc;
            bm = m[i];
        }
    }
    return bm;
}

/* 开局摆子。红在下（y=9 底线）、黑在上（y=0 底线），与棋盘画法一致。 */
void init_board(int* b) {
    int i;

    for (i = 0; i < BW * BH; i = i + 1) b[i] = 0;
    /* 黑方（上） */
    b[0 * BW + 0] = S_BLK * 10 + T_R;
    b[0 * BW + 1] = S_BLK * 10 + T_N;
    b[0 * BW + 2] = S_BLK * 10 + T_B;
    b[0 * BW + 3] = S_BLK * 10 + T_A;
    b[0 * BW + 4] = S_BLK * 10 + T_K;
    b[0 * BW + 5] = S_BLK * 10 + T_A;
    b[0 * BW + 6] = S_BLK * 10 + T_B;
    b[0 * BW + 7] = S_BLK * 10 + T_N;
    b[0 * BW + 8] = S_BLK * 10 + T_R;
    b[2 * BW + 1] = S_BLK * 10 + T_C;
    b[2 * BW + 7] = S_BLK * 10 + T_C;
    b[3 * BW + 0] = S_BLK * 10 + T_P;
    b[3 * BW + 2] = S_BLK * 10 + T_P;
    b[3 * BW + 4] = S_BLK * 10 + T_P;
    b[3 * BW + 6] = S_BLK * 10 + T_P;
    b[3 * BW + 8] = S_BLK * 10 + T_P;
    /* 红方（下） */
    b[9 * BW + 0] = S_RED * 10 + T_R;
    b[9 * BW + 1] = S_RED * 10 + T_N;
    b[9 * BW + 2] = S_RED * 10 + T_B;
    b[9 * BW + 3] = S_RED * 10 + T_A;
    b[9 * BW + 4] = S_RED * 10 + T_K;
    b[9 * BW + 5] = S_RED * 10 + T_A;
    b[9 * BW + 6] = S_RED * 10 + T_B;
    b[9 * BW + 7] = S_RED * 10 + T_N;
    b[9 * BW + 8] = S_RED * 10 + T_R;
    b[7 * BW + 1] = S_RED * 10 + T_C;
    b[7 * BW + 7] = S_RED * 10 + T_C;
    b[6 * BW + 0] = S_RED * 10 + T_P;
    b[6 * BW + 2] = S_RED * 10 + T_P;
    b[6 * BW + 4] = S_RED * 10 + T_P;
    b[6 * BW + 6] = S_RED * 10 + T_P;
    b[6 * BW + 8] = S_RED * 10 + T_P;
}

/* ═══════════════════════ 界面 ═══════════════════════ */

char* name_of(int v) {
    int tp;

    tp = v % 10;
    if (v / 10 == S_RED) {
        if (tp == T_K) return "帅";
        if (tp == T_A) return "仕";
        if (tp == T_B) return "相";
        if (tp == T_N) return "马";
        if (tp == T_R) return "车";
        if (tp == T_C) return "炮";
        return "兵";
    }
    if (tp == T_K) return "将";
    if (tp == T_A) return "士";
    if (tp == T_B) return "象";
    if (tp == T_N) return "马";
    if (tp == T_R) return "车";
    if (tp == T_C) return "炮";
    return "卒";
}

/* 像素 → 行列号；越界返回 -1（与 gomoku 的 hit_col 同一套取整）。 */
int hit_line(int p, int base, int cell) {
    int c;

    c = (p - base + cell / 2) / cell;
    if (c < 0) return -1;
    return c;
}

/* 画棋盘 + 棋子 + 选中/可走点 + 顶部状态行。**不调 ui_present** ——
 * 底部的按钮由 main 画（按钮矩形是 main 算的，画与命中判定必须同一份数据）。 */
void draw_board(int* b, int bx, int by, int cell, int sel, char* status) {
    int i;
    int x;
    int y;
    int cx;
    int cy;
    int r;
    int v;
    int s;
    int ink;
    int ring;
    int ds;
    int dx;
    int dy;

    ui_clear(COL_BG);

    /* 木盘底 */
    ui_rect(bx - cell * 3 / 5, by - cell * 3 / 5,
            cell * 8 + cell * 6 / 5, cell * 9 + cell * 6 / 5,
            COL_BOARD, 1, 0, 6);

    /* 横线 10 条（贯通） */
    for (y = 0; y < BH; y = y + 1) {
        ui_line(bx, by + y * cell, bx + 8 * cell, by + y * cell, COL_LINE, 1);
    }
    /* 竖线 9 条：最外两条贯通，里面的在河界处断开 */
    for (x = 0; x < BW; x = x + 1) {
        if (x == 0 || x == BW - 1) {
            ui_line(bx + x * cell, by, bx + x * cell, by + 9 * cell, COL_LINE, 1);
        } else {
            ui_line(bx + x * cell, by, bx + x * cell, by + 4 * cell, COL_LINE, 1);
            ui_line(bx + x * cell, by + 5 * cell, bx + x * cell, by + 9 * cell, COL_LINE, 1);
        }
    }
    /* 两座九宫的斜线 */
    ui_line(bx + 3 * cell, by, bx + 5 * cell, by + 2 * cell, COL_LINE, 1);
    ui_line(bx + 5 * cell, by, bx + 3 * cell, by + 2 * cell, COL_LINE, 1);
    ui_line(bx + 3 * cell, by + 7 * cell, bx + 5 * cell, by + 9 * cell, COL_LINE, 1);
    ui_line(bx + 5 * cell, by + 7 * cell, bx + 3 * cell, by + 9 * cell, COL_LINE, 1);

    /* 楚河汉界（写在两段竖线的空档里，竖排读着别扭，横排即可） */
    ui_text_v(bx + 2 * cell, by + 4 * cell + cell / 2, "楚河", COL_LINE,
              cell * 2 / 5, VML_ANCHOR_CENTER, VML_VANCHOR_MIDDLE, 0);
    ui_text_v(bx + 6 * cell, by + 4 * cell + cell / 2, "汉界", COL_LINE,
              cell * 2 / 5, VML_ANCHOR_CENTER, VML_VANCHOR_MIDDLE, 0);

    /* 棋子 */
    r = cell * 2 / 5;
    if (r < 5) r = 5;
    for (i = 0; i < BW * BH; i = i + 1) {
        v = b[i];
        if (v == 0) continue;
        cx = bx + (i % BW) * cell;
        cy = by + (i / BW) * cell;
        s = v / 10;
        if (s == S_RED) { ink = COL_RED; ring = COL_RED; }
        else { ink = COL_BLACK; ring = COL_BLACK; }
        ui_circle(cx, cy, r, ring, 0, 2);          /* 外圈 */
        ui_circle(cx, cy, r - 2, COL_PIECE, 1, 0); /* 内底 */
        ui_text_v(cx, cy, name_of(v), ink, cell * 3 / 5,
                  VML_ANCHOR_CENTER, VML_VANCHOR_MIDDLE, VML_FONT_BOLD);
        /* 最后一步的落点画个小圈，方便看清电脑刚走哪儿 */
        if (i == sel && sel >= 0) ui_circle(cx, cy, r + 3, COL_SEL, 0, 2);
    }

    /* 选中子的可走点（**现算**，不另存一份 —— 画出来的点与真正可行的点必然一致） */
    if (sel >= 0) {
        x = sel % BW;
        y = sel / BW;
        for (i = 0; i < BW * BH; i = i + 1) {
            dx = i % BW;
            dy = i / BW;
            if (legal_move(b, x, y, dx, dy) == 0) continue;
            ds = cell / 8;
            if (ds < 3) ds = 3;
            if (b[i] != 0) ui_circle(bx + dx * cell, by + dy * cell, r, COL_SEL, 0, 3);
            else ui_circle(bx + dx * cell, by + dy * cell, ds, COL_SEL, 1, 0);
        }
    }

    /* 顶部状态行 */
    ui_text_v(bx, 6, status, COL_TEXT, 15, VML_ANCHOR_LEFT, VML_VANCHOR_TOP, VML_FONT_BOLD);
}

/* btns = {重开x,y,w,h, 悔棋x,y,w,h} —— 画与命中判定读同一个数组，不各算一遍。 */
void draw_buttons(int* btns) {
    int i;
    int x;

    i = 0;
    while (i < 2) {
        x = i * 4;
        ui_rect(btns[x], btns[x + 1], btns[x + 2], btns[x + 3], COL_BTN, 1, 0, 8);
        if (i == 0) {
            ui_text_v(btns[x] + btns[x + 2] / 2, btns[x + 1] + btns[x + 3] / 2, "重开",
                      COL_BTN_TX, 16, VML_ANCHOR_CENTER, VML_VANCHOR_MIDDLE, 0);
        } else {
            ui_text_v(btns[x] + btns[x + 2] / 2, btns[x + 1] + btns[x + 3] / 2, "悔棋",
                      COL_BTN_TX, 16, VML_ANCHOR_CENTER, VML_VANCHOR_MIDDLE, 0);
        }
        i = i + 1;
    }
}

int pt_in(int px, int py, int x, int y, int w, int h) {
    if (px < x || px > x + w) return 0;
    if (py < y || py > y + h) return 0;
    return 1;
}

/* ═══════════════════════ 主程序 ═══════════════════════ */

#ifndef CHESS_LIB_ONLY

int main(void) {
    int b[CELLS];
    int msg[4];
    int btns[8];
    int sw;
    int sh;
    int cell;
    int bx;
    int by;
    int t;
    int x;
    int y;
    int col;
    int row;
    int idx;
    int sel;
    int t0;
    int r;
    int i;
    int f;
    int tt;
    int over;          /* 0 进行中 / 1 人胜 / 2 电脑胜 / 3 和棋（理论上有） */
    int turn;          /* 当前该谁走：S_RED / S_BLK */
    int aiFrom;
    int aiTo;
    int hf[4];         /* 悔棋栈：最近 4 个半回合（人/电脑各 2） */
    int ht[4];
    int hc[4];
    int hn;
    char* status;

    init_board(b);

    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 380;
    if (sh <= 0) sh = 700;

    /* 竖屏 + 不要手柄区：棋盘是竖着看的，全程触摸，手柄一个都用不上 */
    ui_win_open_ex("象棋", sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* 布局：顶部一行状态、底部一行按钮，中间给棋盘。宽高**分开算**再取小者 */
    cell = sw / 11;
    if ((sh - 110) / 12 < cell) cell = (sh - 110) / 12;
    if (cell < 8) cell = 8;
    bx = (sw - 8 * cell) / 2;
    by = 30 + (sh - 110 - 9 * cell) / 2;
    if (by < 30) by = 30;

    /* 两个按钮并排居中放在底部 */
    btns[2] = sw / 3;
    btns[3] = 40;
    btns[0] = sw / 2 - btns[2] - 6;
    btns[1] = sh - btns[3] - 10;
    btns[6] = btns[2];
    btns[7] = btns[3];
    btns[4] = sw / 2 + 6;
    btns[5] = btns[1];

    sel = -1;
    over = 0;
    turn = S_RED;
    hn = 0;
    status = "你执红先行，点自己的子再点落点";

    t0 = ui_dlg_msg("象棋", "你执红先行。点亮自己的棋子，再点亮要去的点；底部可悔棋。",
                    VML_DLG_INFO);

    draw_board(b, bx, by, cell, sel, status);
    draw_buttons(btns);
    ui_present();

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        /* ⚠ **只认 TOUCHDOWN，绝不把 MOUSEDOWN 一起认** —— 这一条是本程序"完全下不了"的根因。
         *
         * 手机上一个物理点按会投出**两条**消息：先一条触摸、紧跟一条**坐标相同**的鼠标
         * （`DrawWindowPage.PostTouch` 里那个 `calls.PostInput(MouseDown…)` 在
         *  `if (wantTouch)` **之外**，凡是图形窗口两族都发；`VmlUi.SuppressTouch` 只对
         *  电脑屏窗口抑制触摸）。桌面脚手架下我按 `touchdown`+`mousedown` 成对投进去复现了：
         * 一次点按只听到 760 那一声（选中），选中位随即被抹掉 —— 因为第二次进到
         * `else if (idx == sel) sel = -1;` 那条分支，把刚选中的子**取消**掉了。
         *
         * 五子棋同样是"两族都认"，却看不出来：它落子后 `if (b[idx] != EMPTY) continue;`
         * 天然幂等，重复那一下是空操作。**本程序是"先选子、再点落点"的两步状态机，
         * 对同一个点重复输入不幂等**，于是「点自己的子」永远只闪一下、根本选不中。
         *
         * 鼠标那条不认即可 —— 它已经被这次 `ui_wait` 取走，不会留在队列里变成下一次点按。
         * 约定与 `calc.c` 一致（它也只认 TOUCHDOWN），`waycoder_ui.h` 头部的示例同样如此。 */
        if (t != VML_MSG_TOUCHDOWN) continue;

        x = msg[1];
        y = msg[2];

        /* 按钮 */
        if (pt_in(x, y, btns[4], btns[5], btns[6], btns[7]) == 1) {
            if (hn >= 2 && over == 0) {
                /* 悔一个整回合（电脑 + 人）—— 只退一半的话又轮不到人走，等于白给。
                 * 恢复顺序**不能反**：先把子从落点搬回起点，再把落点还原成"原来被吃的那个子"
                 * （没吃就是 0 = 空）。反过来的话读到的落点已经被自己覆盖了。 */
                i = 0;
                while (i < 2) {
                    hn = hn - 1;
                    b[hf[hn]] = b[ht[hn]];
                    b[ht[hn]] = hc[hn];
                    i = i + 1;
                }
                sel = -1;
                turn = S_RED;
                status = "已悔一步，该你走";
                ui_beep(700, 40);
            } else {
                ui_beep(300, 60);
            }
            draw_board(b, bx, by, cell, sel, status);
            draw_buttons(btns);
            ui_present();
            continue;
        }
        if (pt_in(x, y, btns[0], btns[1], btns[2], btns[3]) == 1) {
            init_board(b);
            sel = -1;
            over = 0;
            turn = S_RED;
            hn = 0;
            status = "新的一局，你执红先行";
            ui_beep(900, 70);
            ui_msg_clear();
            draw_board(b, bx, by, cell, sel, status);
            draw_buttons(btns);
            ui_present();
            continue;
        }

        if (over != 0) continue;

        /* 棋盘 */
        col = hit_line(x, bx, cell);
        row = hit_line(y, by, cell);
        if (col < 0 || col >= BW || row < 0 || row >= BH) { sel = -1; }
        else {
            idx = row * BW + col;
            if (turn != S_RED) {
                /* 电脑在走，忽略触摸 */
            } else if (sel < 0) {
                if (b[idx] != 0 && b[idx] / 10 == S_RED) {
                    sel = idx;
                    ui_beep(760, 20);
                }
            } else if (idx == sel) {
                sel = -1;
            } else if (b[idx] != 0 && b[idx] / 10 == S_RED) {
                sel = idx;
                ui_beep(760, 20);
            } else if (legal_move(b, sel % BW, sel / BW, col, row) == 1) {
                /* 人走子 */
                f = sel;
                tt = idx;
                if (hn >= 4) {
                    /* 栈满：整体前移一位（只留最近 4 个半回合，够悔两次） */
                    i = 0;
                    while (i < 3) {
                        hf[i] = hf[i + 1];
                        ht[i] = ht[i + 1];
                        hc[i] = hc[i + 1];
                        i = i + 1;
                    }
                    hn = 3;
                }
                hf[hn] = f;
                ht[hn] = tt;
                hc[hn] = b[tt];
                hn = hn + 1;
                b[tt] = b[f];
                b[f] = 0;
                sel = -1;
                turn = S_BLK;
                ui_beep(880, 25);
                ui_vibrate(15, 0);

                if (in_check(b, S_BLK) == 1) ui_beep(1200, 90);
                if (has_legal(b, S_BLK) == 0) {
                    over = 1;
                    status = "将死！红方胜";
                } else {
                    status = "电脑思考中…";
                    draw_board(b, bx, by, cell, sel, status);
                    draw_buttons(btns);
                    ui_present();

                    /* 电脑走 */
                    r = ai_pick(b, S_BLK);
                    if (r < 0) {
                        over = 1;
                        status = "电脑无棋可走，红方胜";
                    } else {
                        aiFrom = r / 100;
                        aiTo = r % 100;
                        if (hn >= 4) {
                            i = 0;
                            while (i < 3) {
                                hf[i] = hf[i + 1];
                                ht[i] = ht[i + 1];
                                hc[i] = hc[i + 1];
                                i = i + 1;
                            }
                            hn = 3;
                        }
                        hf[hn] = aiFrom;
                        ht[hn] = aiTo;
                        hc[hn] = b[aiTo];
                        hn = hn + 1;
                        b[aiTo] = b[aiFrom];
                        b[aiFrom] = 0;
                        ui_beep(560, 25);
                        turn = S_RED;

                        if (in_check(b, S_RED) == 1) {
                            ui_beep(1200, 90);
                            ui_vibrate(30, 0);
                            status = "将军！该你走";
                        } else {
                            status = "该你走";
                        }
                        if (has_legal(b, S_RED) == 0) {
                            over = 2;
                            status = "你被将死了，电脑胜";
                        }
                    }
                }
            } else {
                sel = -1;
            }
        }

        draw_board(b, bx, by, cell, sel, status);
        draw_buttons(btns);
        ui_present();

        /* 一局结束：收在**这一处**（人将死电脑 / 电脑将死人 / 无棋可走都汇到这里）——
         * 先让终局画面留在屏上，再出声弹框问要不要再来。 */
        if (over != 0) {
            if (over == 1) {
                ui_beep(1320, 320);
                ui_vibrate(60, 0);
                r = ui_dlg_msg("象棋", "恭喜，你赢了！再来一局？", VML_DLG_QUESTION);
            } else {
                ui_beep(240, 420);
                ui_vibrate(220, 0);
                r = ui_dlg_msg("象棋", "电脑赢了。再来一局？", VML_DLG_QUESTION);
            }
            if (r == 1) break;                 /* 弹框失败(-1)也当"再来"，别把局面卡死 */
            init_board(b);
            sel = -1;
            over = 0;
            turn = S_RED;
            hn = 0;
            status = "新的一局，你执红先行";
            ui_msg_clear();
            ui_beep(900, 70);
            draw_board(b, bx, by, cell, sel, status);
            draw_buttons(btns);
            ui_present();
        }
    }

    ui_keep_on(0);
    ui_timer_set(120, 0);
    t = ui_wait(msg, 400);
    ui_win_close();
    return 0;
}

#endif /* CHESS_LIB_ONLY */
