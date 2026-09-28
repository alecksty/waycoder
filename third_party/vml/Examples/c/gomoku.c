/* gomoku.c —— 五子棋（人机对战），跑在手机端 VML 上
 * gomoku.c -- Gomoku (human vs computer), running on mobile VML
 *
 * 用到的都是现成的宿主接口：`waycoder_ui.h` 那套（窗体 / 绘图 / 触摸消息队列），
 * Everything it uses is an existing host API: the `waycoder_ui.h` set (window, drawing, touch message queue),
 * 外加 v0.96.173 的**手感接口**（`ui_beep` 落子与胜负、`ui_vibrate`、`ui_dlg_msg` 报胜负）。
 * plus the v0.96.173 **feel APIs** (`ui_beep` for moves and results, `ui_vibrate`, `ui_dlg_msg` for the result dialog).
 * 棋盘与棋子全部用绘图指令画出来，不依赖任何图片资源；棋盘尺寸按实际可用绘图区
 * The board and stones are all drawn with drawing commands and depend on no image assets; the board size follows the actual usable drawing area
 * （syscall #566/#567）自适应，所以同一份代码在手机、平板、模拟器上都能铺满。
 * (syscall #566/#567) so the same code fills the screen on a phone, a tablet or an emulator.
 *
 * 编译运行（手机 App 的 vml 工具）：
 * Build and run (with the vml tool in the phone app):
 *     vml run gomoku.c
 * 桌面（无 UI 号段，只能验证编译与算法）：
 * On the desktop (no UI syscall range, so only compilation and the algorithm can be checked):
 *     vmlhost run Examples/c/gomoku.c
 *
 * ⚠ 这里**不能**用全局变量去接 ${} 参数：`GenerateAsmStatement` 的 `${名}` 只查局部
 * ⚠ You **cannot** use a global variable to receive a ${} argument here: the `${name}` of `GenerateAsmStatement` only searches the local
 *   变量表（`variables`），全局变量在 `dataSection` 里、查不到，会退化成 `R12+0`。
 *   variable table (`variables`); globals live in `dataSection` and are not found, so it degrades to `R12+0`.
 *   所以：棋盘是 main 的局部数组，靠参数传给各个函数；调 syscall 一律走
 *   Therefore: the board is a local array in main and is passed to each function as a parameter; syscalls always go through
 *   `waycoder_ui.h` 的包装函数（它们的形参就是普通 C 参数，安全）。
 *   the `waycoder_ui.h` wrappers (their formal parameters are plain C parameters, which is safe).
 */

#include <waycoder_ui.h>

#define N 15          /* 15 路棋盘 */
                      /* 15x15 board */
#define EMPTY 0
#define BLACK 1       /* 人 */
                      /* Human */
#define WHITE 2       /* 电脑 */
                      /* Computer */

#define COL_BG      0xFF1B1B22
#define COL_LINE    0xFF5A5A66
#define COL_BOARD   0xFFE8C48A
#define COL_BLACK   0xFF14141A
#define COL_WHITE   0xFFF2F2F6
#define COL_TEXT    0xFFEDEDF2
#define COL_ACCENT  0xFF4ADE80

/* ─────────── 棋盘几何（都是 main 算好、按参数传下去，避免全局变量）─────────── */
/* ----------- Board geometry (all computed in main and passed down as parameters, avoiding globals) ----------- */

/* 把落点像素坐标换算成格线序号；越界返回 -1。 */
/* Convert a tapped pixel coordinate into a grid line index; returns -1 when out of range. */
int hit_col(int x, int pad, int cell) {
    int c;
    c = (x - pad + cell / 2) / cell;
    if (c < 0) return -1;
    if (c >= N) return -1;
    return c;
}

/* ─────────── 胜负判定 ─────────── */
/* ----------- Win detection ----------- */

/* 从 (x,y) 出发沿 (dx,dy) 数同色连子（含自己），≥5 即胜。 */
/* Starting at (x,y), count same-colored stones along (dx,dy) including itself; 5 or more means a win. */
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
                                                         /* Horizontal */
    if (count_dir(b, x, y, 0, 1, who) >= 5) return 1;    /* 竖 */
                                                         /* Vertical */
    if (count_dir(b, x, y, 1, 1, who) >= 5) return 1;    /* 撇 */
                                                         /* Down-right diagonal */
    if (count_dir(b, x, y, 1, -1, who) >= 5) return 1;   /* 捺 */
                                                         /* Up-right diagonal */
    return 0;
}

/* ─────────── 电脑棋力：单方向估值 ─────────── */
/* ----------- Computer strength: single-direction evaluation ----------- */

/* 假设 (x,y) 已被 who 占，沿 (dx,dy) 这条线的价值。
 * Assume (x,y) is already taken by who; this is the value of the line along (dx,dy).
 * 两头都开放(活)与只有一头开放(眠)给的分差一个量级 —— 这是最简单的棋力来源。
 * Both ends open versus only one end open differ by an order of magnitude -- this is the simplest source of playing strength.
 */
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
 * Pick the best empty point. Returns y*N+x; returns -1 when there is no empty point.
 *
 * 打分 = 进攻分 + 防守分：
 * The score is the attack score plus the defense score:
 *   · 自己能成五 → 进攻分 1000000，再加 5000000 保证"能赢就先赢"，
 *   · If we can make five -> attack score 1000000, plus 5000000 to guarantee "win first when we can",
 *     不会被"挡住对方成五"同分抢走（两个都是 1000000，谁先扫到谁赢，那是掷骰子）；
 *     so it is not stolen by an equal score for blocking the opponent's five (both are 1000000, so whoever scans first wins, which is a dice roll);
 *   · 对方能成五 → 防守分 1000000 → 必然去堵。
 *   · If the opponent can make five -> defense score 1000000 -> we will certainly block it.
 * 平手时选**更靠近中心**的点（中心先手价值高）。
 * On a tie, pick the point **closer to the center** (the center is worth more to the first player).
 */
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
/* ----------- Drawing ----------- */

void draw_board(int* b, int pad, int padY, int cell, int lastIdx, int over, int lang) {
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
    /* Board background */
    ui_rect(pad - cell / 2, padY - cell / 2, half + cell, half + cell, COL_BOARD, 1, 0, 6);

    /* 格线 */
    /* Grid lines */
    for (i = 0; i < N; i = i + 1) {
        ui_line(pad, padY + i * cell, pad + half, padY + i * cell, COL_LINE, 1);
        ui_line(pad + i * cell, padY, pad + i * cell, padY + half, COL_LINE, 1);
    }

    /* 星位（15 路：4 个 (3,3) 型 + 天元） */
    /* Star points (15x15: four (3,3) type points plus the center) */
    ui_circle(pad + 3 * cell, padY + 3 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 11 * cell, padY + 3 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 3 * cell, padY + 11 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 11 * cell, padY + 11 * cell, 3, COL_LINE, 1, 0);
    ui_circle(pad + 7 * cell, padY + 7 * cell, 3, COL_LINE, 1, 0);

    /* 棋子。半径取格宽的五分之二 —— 留出缝，挨着的子不会糊成一片 */
    /* Stones. The radius is two fifths of the cell width -- that leaves a gap so adjacent stones do not blur together */
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
        /* Mark the last move so it is easy to see where the computer just played */
        if (i == lastIdx) ui_circle(cx, cy, r / 3, COL_ACCENT, 1, 0);
    }

    /* 状态行：用**状态式**文字接口 —— 属性设一次，之后只管给坐标和字符串。
     * Status line: use the **stateful** text API -- set the attributes once, then only pass coordinates and strings.
     * 结束时不再写"点任意处再来一局" —— 那件事现在由 `finish()` 的弹框交代，
     * When the game is over we no longer write "tap anywhere to play again" -- the `finish()` dialog now handles that,
     * 一行小字既没人看、又与弹框重复。这行只管"当前该谁下"。
     * such a small line is neither read nor needed next to the dialog. This line only says whose turn it is.
     */
    if (over == 1) s = lang == 0 ? "你赢了！" : "You win!";
    else if (over == 2) s = lang == 0 ? "电脑赢了" : "Computer wins";
    else if (over == 3) s = lang == 0 ? "平局" : "Draw";
    else s = lang == 0 ? "你执黑，点棋盘落子" : "You are black — tap to place";

    ui_set_font(15, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(pad, padY + half + cell, s);

    /* 结束时有结果，用强调色再画一遍（同一条字符串、同一套接口，只换了颜色） */
    /* When the game is over, draw the result again in the accent color (same string, same API, only the color changed) */
    if (over != 0) {
        ui_set_font(15, VML_FONT_BOLD | VML_FONT_ITALIC, COL_ACCENT, VML_ANCHOR_RIGHT);
        ui_text_cur(pad + half, padY + half + cell, s);
    }

    ui_present();
}

/* ─────────── 一局结束 ─────────── */
/* ----------- End of a round ----------- */

/* 出声 + 弹框问要不要再来一局。返回 1 = 再来，0 = 退出。
 * Play a sound and ask with a dialog whether to play again. Returns 1 = play again, 0 = quit.
 *
 * **为什么必须弹框**：原来只在棋盘下面写了一行 15px 的小字（「你赢了！点任意处再来一局」），
 * **Why a dialog is required**: previously only a 15px line was written under the board ("You win! Tap anywhere to play again"),
 * 而玩家盯着的是棋盘 —— 那一行在屏幕下方、又不闪不动，实测用户的原话是
 * while the player is staring at the board -- that line sits at the bottom of the screen and neither blinks nor moves; the user's actual words were
 * 「赢了输了都没看到输赢的提示框，只是棋盘清空了，重新开始了」。
 * "for a win or a loss I never saw a result dialog, the board just cleared and the game started over".
 * 胜负是这一局唯一必须让玩家知道的事，**用一行小字交代等于没交代**：
 * The result is the one thing the player must be told this round; **conveying it in one small line is the same as not conveying it at all**:
 * 要么弹框（挡住视线、必须点一下才消失），要么根本别做这个游戏。
 * either use a dialog (it blocks the view and must be dismissed with a tap) or do not make this game at all.
 *
/* ── 音效：**用 ui_beep 单音**（v0.96.507 换回来） ────────────────
 * -- Sound effects: **single tones via ui_beep** (switched back in v0.96.507) --
 *
 * ⚠⚠ **这里一度改用共享库的音序器（`ui_sfx_add`）做过一版，手机上破音，换回来了。**
 * ⚠⚠ **One version of this used the shared-library sequencer (`ui_sfx_add`), which crackled on a phone, so it was switched back.**
 *   音序器本身没问题（tetris / pacman 那些用得好好的），破音的是**这里配的音**：
 *   The sequencer itself is fine (the tetris and pacman ones use it happily); what crackled was **the tuning used here**:
 *   胜负那一组是**多个音叠着响**（大三和弦 3 个声部 + 收尾音，输的那组也是 3 个），
 *   the win/lose group has **several tones sounding together** (a major triad of 3 voices plus a closing note, and the losing group is also 3),
 *   多声部混音一叠加，音量就顶到削波；再加上赢的收尾音留了 14 拍那么长，
 *   mixing several voices pushes the volume into clipping; and the closing note of the win sound was held for 14 ticks,
 *   两个人在同一小段时间里一起响，外放就"滋滋"了。
 *   so two of them sounding within a short window made the speaker buzz.
 *
 *   `ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音在响，
 *   `ui_beep` is **single channel** (a new tone cuts off the previous one) => an event can never have more than one tone sounding,
 *   结构上不可能削波。代价是没有和弦、没有音色，但对落子/胜负这类**一次性提示音**
 *   so clipping is structurally impossible. The cost is no chords and no timbre, but for **one-shot cues** like a move or a result
 *   够用 —— 而且这正是它"以前一直没出问题"的原因。
 *   that is enough -- and that is exactly why it never caused trouble before.
 *
 * ⚠ 频率仍然照着「不看屏幕也分得出」配：
 * ⚠ The frequencies still follow "tell them apart without looking at the screen":
 *   人 880Hz（清亮） / 电脑 620Hz（低一截） / 赢 1320Hz 长音 / 输 260Hz 长音 / 平局 500Hz。
 *   human 880Hz (bright) / computer 620Hz (a step lower) / win 1320Hz long / lose 260Hz long / draw 500Hz.
 *   这几个数是 v0.96.173 起就在用的，**别随手调**（调了要真机听）。
 *   These values have been in use since v0.96.173; **do not tweak them casually** (if you change them, listen on a real device).
 */

/* 人落子：清亮、短。 */
/* Human move: bright and short. */
void sfx_put_human(void) { ui_beep(880, 25); }

/* 电脑落子：低一截 —— 与人的那条**一耳朵分得出**。 */
/* Computer move: a step lower -- **tellable apart at a glance** from the human one. */
void sfx_put_ai(void) { ui_beep(620, 25); }

/* 重开：一声干脆的起手音。 */
/* New game: one crisp opening tone. */
void sfx_newgame(void) { ui_beep(760, 60); }

/* 赢：高、长，明亮。 */
/* Win: high, long and bright. */
void sfx_win(void) {
    ui_beep(1320, 320);
    ui_vibrate(60, 0);
}

/* 输：低、长 —— 与"赢"是**两个方向**，不会听错。 */
/* Lose: low and long -- **the opposite direction** from the win sound, so it cannot be misheard. */
void sfx_lose(void) {
    ui_beep(260, 420);
    ui_vibrate(220, 0);
}

/* 平局：中性（既不欢快也不沮丧）。 */
/* Draw: neutral (neither cheerful nor sad). */
void sfx_draw(void) {
    ui_beep(500, 300);
    ui_vibrate(40, 0);
}
int finish(int over, int lang) {
    int r;
    if (over == 1) {
        sfx_win();
        r = ui_dlg_msg(lang == 0 ? "五子棋" : "Gomoku",
                       lang == 0 ? "你赢了！再来一局？" : "You win! Play again?", VML_DLG_QUESTION);
    } else if (over == 2) {
        sfx_lose();
        r = ui_dlg_msg(lang == 0 ? "五子棋" : "Gomoku",
                       lang == 0 ? "电脑赢了。再来一局？" : "Computer wins. Play again?", VML_DLG_QUESTION);
    } else {
        sfx_draw();
        r = ui_dlg_msg(lang == 0 ? "五子棋" : "Gomoku",
                       lang == 0 ? "平局。再来一局？" : "Draw. Play again?", VML_DLG_QUESTION);
    }
    /* 弹框失败（返回 -1）也当"再来" —— 总不能因为宿主弹不出框就把整局卡死在这儿 */
    /* A failed dialog (returns -1) also counts as "play again" -- we should not freeze the whole round just because the host could not show a dialog */
    if (r == 1) return 0;
    return 1;
}

/* ─────────── 主循环 ─────────── */
/* ----------- Main loop ----------- */

int main(void) {
    int b[225];
    int msg[4];
    int sw;
    int sh;
    int cell;
    int pad;
    int padY;
    int availW;
    int availH;
    int i;
    int t;
    int x;
    int y;
    int col;
    int row;
    int idx;
    int over;      /* 0=进行中 1=人胜 2=电脑胜 3=平局 */
                   /* 0=running 1=human won 2=computer won 3=draw */
    int moves;
    int lastIdx;
    int aiIdx;
    int t0;
    int t1;
    int r;
    char* title;
    int lang;      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                   /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    /* 开局：棋盘清空 */
    /* Start: clear the board */
    for (i = 0; i < N * N; i = i + 1) b[i] = EMPTY;

    lang = ui_get_language();
    title = lang == 0 ? "五子棋" : "Gomoku";

    /* **先问可用绘图区，再开窗** —— 顺序不能反。
     * **Ask for the usable drawing area first, then open the window** -- the order must not be reversed.
     *
     * `ui_scr_w/h` 读的是设备显示信息（不依赖窗口），所以开窗前就能拿到。
     * `ui_scr_w/h` read device display information (they do not depend on the window), so they are available before the window opens.
     * 而窗口的宽高就是**画布的坐标空间**：按 395 的宽度排版、却开一个 360 宽的窗口，
     * The window width and height are the **coordinate space of the canvas**: lay out for a width of 395 but open a 360-wide window
     * 棋盘右半边会被画到画布外面（实测就是"屏幕右边超出"——左边留 17、右边直接切掉）。
     * and the right half of the board is drawn outside the canvas (measured as "the right side is cut off" -- 17 left on the left, the right simply clipped).
     * 所以尺寸必须与排版用的那个数**同源**。
     * So the size must come from the **same source** as the number used for layout.
     */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;

    /* **只支持竖屏 + 不要手柄区**：棋盘是竖着看的，转屏只会让格子重排一次、
     * **Portrait only, plus no gamepad area**: the board is viewed upright, so rotating the screen would only make the cells re-layout once
     * 玩家还得多转回来；而五子棋全程用触摸落子，屏幕手柄一个都用不到 ——
     * and the player would have to rotate back; and gomoku places stones entirely by touch, so not one on-screen pad button is used --
     * 留着一整块手柄区等于白白吃掉一百多像素的棋盘高度。
     * keeping a whole gamepad area would waste more than a hundred pixels of board height for nothing.
     */
    ui_win_open_ex(title, sw, sh, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);

    /* 布局：**宽和高分开算约束**，再在整块画布里居中。
     * Layout: **compute the width and height constraints separately**, then center inside the whole canvas.
     *
     * 这里原来写的是 `avail = sh - 34; if (avail > sw) avail = sw;` —— 把两个方向压成
     * This used to read `avail = sh - 34; if (avail > sw) avail = sw;` -- squeezing both directions into
     * 一个数取小者：手机上 sh≈744、sw≈395 ⇒ avail 被压成 395，于是格子按宽度算完之后
     * one number by taking the smaller: on a phone sh is about 744 and sw about 395 => avail was squeezed to 395, so after sizing the cells by width
     * **棋盘被居中在"顶部那 395px"里**，屏幕下面空掉一大半（棋盘挤在上半屏、
     * **the board was centered inside "the top 395px"** and more than half the screen below was left empty (the board squeezed into the upper half
     * 状态文字浮在屏幕中间）。宽高各自约束才不会互相吃掉。
     * and the status text floating in the middle of the screen). Separate width and height constraints stop them from eating each other.
     */
    availW = sw;
    availH = sh - 34;                     /* 底下留一行状态文字 */
                                          /* Leave one line for the status text at the bottom */
    if (availH < 40) availH = 40;

    cell = availW / (N + 1);
    if (availH / (N + 1) < cell) cell = availH / (N + 1);
    if (cell < 4) cell = 4;

    pad = (sw - (N - 1) * cell) / 2;
    padY = (availH - (N - 1) * cell) / 2 + cell / 2;
    if (padY < cell / 2) padY = cell / 2;

    t0 = ui_dlg_msg(lang == 0 ? "五子棋" : "Gomoku",
                    lang == 0 ? "你执黑先行。点棋盘落子，返回箭头退出。"
                              : "You are black. Tap to place, Back to quit.", VML_DLG_INFO);

    over = 0;
    moves = 0;
    lastIdx = -1;
    draw_board(b, pad, padY, cell, lastIdx, over, lang);

    /* 主循环：一个统一的消息队列，取到触摸就换算格子 */
    /* Main loop: one unified message queue; when a touch arrives, convert it into a cell */
    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;                     /* 超时（这里不会发生，0=无限等） */
                                                  /* Timeout (cannot happen here, 0 means wait forever) */
        if (t == VML_MSG_WINDOWCLOSE) break;
        if (t != VML_MSG_TOUCHDOWN && t != VML_MSG_MOUSEDOWN) continue;

        x = msg[1];
        y = msg[2];

        col = hit_col(x, pad, cell);
        row = hit_col(y, padY, cell);
        if (col < 0 || row < 0) continue;

        idx = row * N + col;
        if (b[idx] != EMPTY) continue;             /* 这儿有子了 */
                                                   /* There is already a stone here */

        /* 人落子 */
        /* The human plays */
        b[idx] = BLACK;
        moves = moves + 1;
        lastIdx = idx;
        sfx_put_human();          /* 人：清亮一点 */
                                  /* Human: a bit brighter */

        if (has_won(b, col, row, BLACK) == 1) {
            over = 1;
        } else if (moves >= N * N) {
            over = 3;
        } else {
            /* 先画一手人的，让手感立刻有反馈，再算电脑的 */
            /* Draw the human's move first so there is instant feedback, then work out the computer's */
            draw_board(b, pad, padY, cell, lastIdx, over, lang);

            /* 电脑落子 */
            /* The computer plays */
            aiIdx = ai_pick(b, 0);
            if (aiIdx < 0) {
                over = 3;
            } else {
                b[aiIdx] = WHITE;
                moves = moves + 1;
                lastIdx = aiIdx;
                col = aiIdx % N;
                row = aiIdx / N;
                sfx_put_ai();              /* 电脑：低一点，一耳朵分得出是谁下的 */
                                           /* Computer: a bit lower, so you can tell who played */
                if (has_won(b, col, row, WHITE) == 1) over = 2;
                else if (moves >= N * N) over = 3;
            }
        }

        draw_board(b, pad, padY, cell, lastIdx, over, lang);

        /* 一局结束：**收在这一处**（四种结束方式都汇到这里）——
         * End of a round: **collected in this one place** (all four ways of ending converge here) --
         * 出声、弹框问要不要再来；不想再来就退出窗口，而不是默默重开。
         * play a sound, ask with a dialog whether to play again; if not, close the window rather than silently restarting.
         */
        if (over != 0) {
            if (finish(over, lang) == 0) break;
            /* 清掉上一局没读完的输入。一次点击会产生**多条**消息（按下/抬起/移动），
             * Clear the unread input left from the previous round. One tap produces **several** messages (down/up/move),
             * 而主循环通常只读它要的那条，剩下的就留在队列里 —— 不清的话新一局
             * and the main loop usually reads only the one it needs, leaving the rest in the queue -- without clearing them, the new round
             * 一上来就会把上一局最后那点读出来，黑子立刻落到那个位置上。
             * would immediately read the tail of the previous round and a black stone would land on that spot at once.
             */
            ui_msg_clear();
            for (i = 0; i < N * N; i = i + 1) b[i] = EMPTY;
            over = 0;
            moves = 0;
            lastIdx = -1;
            sfx_newgame();
            draw_board(b, pad, padY, cell, lastIdx, over, lang);
        }
    }

    /* 收尾：等一小会儿再关，免得窗口一闪而过（宿主定时刷新，这里只是留个缓冲） */
    /* Wrap up: wait a moment before closing so the window does not flash by (the host refreshes on a timer; this is just a buffer) */
    ui_timer_set(120, 0);
    t1 = ui_wait(msg, 400);
    ui_win_close();
    return 0;
}
