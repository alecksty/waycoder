/* tetris.c —— 俄罗斯方块（C 版），跑在手机端 VML 上
 * tetris.c — Tetris (C version), running on VML on the mobile side
 *
 * 用到的都是现成的宿主接口（`waycoder_ui.h` 那套：窗体 / 绘图 / 消息队列 / 定时器），
 * It uses only existing host interfaces (the `waycoder_ui.h` set: window / drawing / message queue / timer),
 * 外加 v0.96.172 新加的**手感接口**（`ui_beep` / `ui_vibrate` / `ui_keep_on` / `ui_store_*`）。
 * plus the **feel interfaces** added in v0.96.172 (`ui_beep` / `ui_vibrate` / `ui_keep_on` / `ui_store_*`).
 * 布局全部按实际可用绘图区（syscall #566/#567）算，所以手机、平板、模拟器上都能铺满。
 * The whole layout is computed from the actual available drawing area (syscall #566/#567), so it fills the screen on phones, tablets and emulators alike.
 *
 * 编译运行（手机 App 的 vml 工具）：
 * Build and run (with the phone App's vml tool):
 *     vml run tetris.c
 *
 * 操作全部走**绘图窗口底部那排屏幕手柄**（方向键 + START/SELECT + X/Y/A/B），
 * All controls go through **the row of on-screen gamepad buttons at the bottom of the drawing window** (arrows + START/SELECT + X/Y/A/B);
 * 手柄按键就是 Win32 虚拟键值，接物理键盘也是同一套：
 * the gamepad buttons are Win32 virtual key codes, so a physical keyboard uses the same set:
 *     ← →      左右移动（按住连发）
 *     ← →      move left/right (hold to auto-repeat)
 *     ↓        加速下落（按住连发）
 *     ↓        soft drop faster (hold to auto-repeat)
 *     ↑ / A / X 旋转
 *     ↑ / A / X rotate
 *     空格 / B / Y 直落到底
 *     Space / B / Y hard drop to the bottom
 *     START    重开一局
 *     START    restart a game
 *     SELECT   暂停 / 继续
 *     SELECT   pause / resume
 *     返回箭头  退出
 *     Back arrow  quit
 *
 * ## 为什么不自己画手柄、也不判触摸（v0.96.173 去掉的）
 * ## Why it neither draws its own gamepad nor handles touch (removed in v0.96.173)
 *
 * 上一版在窗口里自绘了一套十字键 + 旋转/直落/暂停/重开，靠触摸命中去判按键。
 * The previous version drew its own D-pad + rotate/hard-drop/pause/restart inside the window and decided key presses by touch hit-testing.
 * 那是**多此一举**：绘图窗口底部本来就有一排屏幕手柄（`DrawWindowPage`），
 * That was **pointless**: the drawing window already has a row of on-screen gamepad buttons at the bottom (`DrawWindowPage`),
 * 程序只要收 `VML_MSG_KEYDOWN` 就行。自绘那套的代价是实打实的 ——
 * so the program only has to receive `VML_MSG_KEYDOWN`. The cost of the self-drawn version was real —
 *   1. 要占掉约 140px 的窗口高度（棋盘就矮一截，手机上少两行）；
 *   1. it took up about 140px of window height (the board got shorter, two rows fewer on a phone);
 *   2. 几何要在"画"与"命中判定"两处各算一遍（本文件上一版专门写了段注释讲这件事），
 *   2. the geometry had to be computed twice, once for "drawing" and once for "hit-testing" (the previous version of this file had a whole comment about that),
 *      改个间距就会出现"看着在键上、点下去没反应"；
 *      so changing a spacing produced "it looks like it's on the key, but tapping does nothing";
 *   3. 每个游戏各画一套，风格互不相同，用户还得重新学一遍；
 *   3. every game drew its own set with a different look, and the user had to learn it all over again;
 *   4. 触摸与按键两条输入路径并存，程序里两套状态（`held` 与键盘）容易不一致。
 *   4. having both touch and key input paths meant two sets of state in the program (`held` and the keyboard) that easily disagreed.
 * 现在**只认按键消息**，触摸消息一概不处理 —— 窗口就是一块显示区。
 * Now it **honors key messages only** and ignores touch messages entirely — the window is just a display area.
 *
 * ## 三条 C 前端的硬约束（踩过才写的）
 * ## Three hard constraints of the C frontend (written only after tripping over them)
 *
 * 1. ⚠ **`${}` 占位符只认局部变量**（`GenerateAsmStatement` 只查 `variables` 表）。
 * 1. ⚠ **A `${}` placeholder only resolves local variables** (`GenerateAsmStatement` consults only the `variables` table).
 *    所以调 syscall 一律走 `waycoder_ui.h` 的包装函数 —— 它们的形参就是普通 C 形参，
 *    So always call syscalls through the wrapper functions in `waycoder_ui.h` — their parameters are ordinary C parameters and
 *    安全；而**绝不能**自己写 `asm("SYSCALL #525, ${gx}, ...")` 去传全局变量，那会退化成 R12+0。
 *    are safe; **never** write `asm("SYSCALL #525, ${gx}, ...")` yourself to pass a global, since that degrades to R12+0.
 * 2. **文件级全局变量是好的**（实测：`int b[BW * BH]` 这种全局数组读写都正确），
 * 2. **File-level globals are fine** (measured: a global array like `int b[BW * BH]` reads and writes correctly),
 *    所以游戏状态就放全局，不必像五子棋那样把棋盘当参数到处传 —— 那个写法是被
 *    so the game state just lives in globals; there is no need to pass the board around as a parameter the way gomoku does — that style came from
 *    "全局变量不能进 ${}"这条误推出来的，实际上只影响 asm，不影响普通读写。
 *    a wrong inference from "globals cannot go into ${}", which in fact only affects asm, not ordinary reads and writes.
 *    但为了统一，本文件仍全部通过包装函数调 syscall。
 *    For consistency, though, this file still calls every syscall through the wrappers.
 * 3. ⚠ **`#define` 不支持折行续行**（反斜杠续行会让词法器在下一行报"未知字符"），
 * 3. ⚠ **`#define` does not support line continuation** (a backslash continuation makes the lexer report "unknown character" on the next line),
 *    所以宏本身一行写完，多行说明另起一段块注释。
 *    so a macro must be written on one line, and multi-line explanations get their own block comment.
 *
 * ## 存档为什么用「全局缓冲 + atoi」
 * ## Why the high score uses a "global buffer + atoi"
 *
 * `ui_store_get(key, buf, cap)` 把字符串写进调用方给的缓冲区。这里把它读回来用的是
 * `ui_store_get(key, buf, cap)` writes a string into the buffer the caller provides. Reading it back here uses
 * `atoi`（最高分本来就是个数），缓冲区放在文件级全局 —— 这两条**不是硬约束，只是习惯**。
 * `atoi` (the high score is a number anyway), and the buffer sits in a file-level global — neither is **a hard constraint, just a habit**.
 *
 * v0.96.173 时这里曾写着"缓冲区必须放全局、不能用下标读"，那是**误判**：真因是 C 前端
 * In v0.96.173 this spot used to say "the buffer must be global and cannot be read by index"; that was **a misdiagnosis**: the real cause was that the C frontend
 * 对**全局** `char` 数组的元素访问按 32 位读写（局部数组反而正常），症状是「长度对、内容不对」。
 * accesses elements of a **global** `char` array with 32-bit reads and writes (local arrays were fine), giving "right length, wrong contents".
 * 那条已在 v0.96.174 修掉（`patches/0004-c-global-array-elem-type.patch`），所以
 * That was fixed in v0.96.174 (`patches/0004-c-global-array-elem-type.patch`), so
 * `buf[0] == '1'` 现在也能直接读。留个记号在这里：**当时是用 `puts` 当判据才误判的**
 * `buf[0] == '1'` now reads directly too. A note is left here on purpose: **the misdiagnosis came from using `puts` as the criterion**
 * （本环境的 `puts` 在字面量多的程序里会串行），换成一个不经 stdio 的判据，六个格子一次就量清了。
 * (in this environment `puts` serializes in programs with many literals); switching to a criterion that does not go through stdio cleared up all six cells in one pass.
 */

#include <waycoder_ui.h>
#include <stdlib.h>

#define BW 10              /* 棋盘宽（格） */
                           /* Board width (in cells). */
#define BH 20              /* 棋盘高（格） */
                           /* Board height (in cells). */
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
/* Save key (`ui_store_*` adds another `vml.` prefix, keeping it apart from the App's own config). */
#define KEY_HI "tetris.hi"

/* ── 游戏状态 ───────────────────────────────────────────── */
/* ── Game state ─────────────────────────────────────── */

int b[BW * BH];     /* 棋盘：0 = 空，否则 = 方块号 + 1。
                     * Board: 0 = empty, otherwise = piece number + 1.
                     * 维度写成**宏 × 宏**是故意的 —— 它原先会因为 C 前端不折常量表达式
                     * Writing the dimension as **macro × macro** is deliberate — it used to silently allocate
                     * 而静默只分配 1 个元素（编得过、跑起来数据全乱），v0.96.170 修掉了，
                     * only 1 element because the C frontend did not fold constant expressions (it built, but the data was all wrong at run time); v0.96.170 fixed it,
                     * 这行就是那个修复的现场回归。
                     * and this line is that fix's on-the-spot regression. */
int pid;            /* 当前方块 0..6 */
                    /* Current piece 0..6 */
int rot;            /* 旋转 0..3 */
                    /* Rotation 0..3 */
int px;             /* 4×4 包围盒左上角（棋盘格坐标） */
                    /* Top-left of the 4×4 bounding box (board-cell coordinates) */
int py;
int npid;           /* 下一个方块 */
                    /* Next piece */

int score;
int best;           /* 最高分（从存档里读出来，破纪录时写回去） */
                    /* High score (read from the save data, written back when a record is broken) */
int nlines;
int level;

int state;          /* 0 = 运行 1 = 暂停 2 = 结束 */
                    /* 0 = running, 1 = paused, 2 = over */
int tid;            /* 重力定时器 id（0 = 没有） */
                    /* Gravity timer id (0 = none) */
int rid;            /* 长按连发定时器 id（0 = 没有） */
                    /* Hold-to-repeat timer id (0 = none) */
int held;           /* 正在按住的方向键（1 左 2 右 3 下，0 = 没按） */
                    /* Direction key currently held (1 left, 2 right, 3 down; 0 = none) */
int rptLeft;        /* 这次连发还剩几拍（见 hold_repeat 的"自限"说明） */
                    /* How many ticks are left in this repeat burst (see the "self-limiting" note in hold_repeat) */
int rptStuck;       /* 连续几拍"按了但没动" */
                    /* How many ticks in a row were "pressed but nothing moved" */
int dropMs;

/* ── 布局（main 里算一次，绘图全读它） ──────────────────── */
/* ── Layout (computed once in main) ───────── */

int g_lang;         /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                    /* UI language: queried once at game start (ui_get_language is a syscall, don't call it every frame) */
int sw;
int sh;
int cell;           /* 格子边长 */
                    /* Cell side length */
int bx;             /* 棋盘左上角 */
                    /* Board top-left corner */
int by;
int panelX;         /* 右侧信息面板 */
                    /* Right-hand info panel */
int panelW;

/* 数字的中间缓冲（`draw_int` / `score_to_str` 用；全局 int 数组，实测可用） */
/* Scratch buffer for digits (used by `draw_int` / `score_to_str`; a global int array, measured to work) */
int digs[12];
int sdigs[12];

/* 存档缓冲：**必须是全局**（局部数组的地址传给函数是错的，见文件头） */
/* Save buffer: **must be global** (passing the address of a local array to a function is wrong; see the file header) */
char hibuf[16];     /* ui_store_get 读进来 */
                    /* read in by ui_store_get */
char hiout[16];     /* 自己逐位拼出去给 ui_store_set */
                    /* assembled digit by digit for ui_store_set */

/* ── 方块颜色 ───────────────────────────────────────────── */
/* ── Piece colors ───────────────────────────────────── */

int piece_color(int p) {
    if (p == 0) return 0xFF22D3EE;   /* I 青 */
                                     /* I cyan */
    if (p == 1) return 0xFFFACC15;   /* O 黄 */
                                     /* O yellow */
    if (p == 2) return 0xFF4ADE80;   /* S 绿 */
                                     /* S green */
    if (p == 3) return 0xFFF87171;   /* Z 红 */
                                     /* Z red */
    if (p == 4) return 0xFFC084FC;   /* T 紫 */
                                     /* T purple */
    if (p == 5) return 0xFFFB923C;   /* L 橙 */
                                     /* L orange */
    return 0xFF60A5FA;               /* J 蓝 */
                                     /* J blue */
}

/* 同色高光（块面顶部那一条）。用查表而不是移位算 —— 免得依赖位运算的可用性。 */
/* A lighter shade of the same color (the strip at the top of a block face). A lookup table is used instead of shifting — so as not to rely on bit operations being available. */
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
/* ── Feel: sound and vibration ──────────────────── */

/* ── 音效：**用 ui_beep 单音**（v0.96.509 统一换回来） ────────────
 * ── Sound: **use the single-tone ui_beep** (switched back uniformly in v0.96.509) ────────────
 *
 * ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add`），**真机上破音**，换回来了。
 * ⚠⚠ These sounds once went through the shared library's sequencer (`ui_sfx_add`) and **broke up on real hardware**, so they were switched back.
 *   破音的是**这里配的音**（机制本身没问题）—— 那一版同时踩了两条：
 *   What broke up was **the sounds configured here** (the mechanism itself is fine) — that version tripped over two things at once:
 *     · **多个音叠着响**：消行是 1–4 个声部的和弦，多声部混音一叠加，音量顶到削波；
 *     · **several notes sounding together**: a line clear was a 1–4 voice chord, and summing that many voices pushed the volume into clipping;
 *     · **长音拖尾**：结束音末了一个 12 拍（一拍 33ms ≈ 400ms）的低音。
 *     · **a long tail**: the game-over sound ended on a 12-tick (one tick = 33ms ≈ 400ms) low note.
 *   `ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音在响，
 *   `ui_beep` is **single-channel** (a later note cuts off the earlier one) ⇒ one event never has more than one note sounding,
 *   **结构上不可能削波**。代价是没有和弦、没有音色。
 *   so clipping is **structurally impossible**. The price is no chords and no timbre.
 *
 * ⚠ 频率取整块的**首音**（哪个事件什么音是设计，不是机制），时长取整块时长、
 * ⚠ The frequency is the **first note** of the block (which event gets which note is design, not mechanism), the duration is the block's duration
 *   封顶 320ms。**结局类取两端**：升级/胜利取整块**最高音**、结束/失败取**最低音**
 *   capped at 320ms. **Outcome sounds take the two extremes**: level up / win take the block's **highest note**, game over / loss take the **lowest**
 *   —— 五子棋/象棋两版也是这么配的（赢 1320 / 输 240）。
 *   — the gomoku and chess versions are configured the same way (win 1320 / lose 240).
 *   低音不低于 C3(131Hz) —— `waycoder_ui.h` 写着「手机外放在 200Hz 以下衰减很快…
 *   Low notes do not go below C3 (131Hz) — `waycoder_ui.h` says "a phone's speaker rolls off fast below 200Hz …
 *   C2 出来是"噗"一声闷响，玩家听着像没响」。
 *   a C2 comes out as a muffled puff and players think nothing played." */

/* 消行：**行数越多、音越高越长** —— 单音表达"这波赚了多少"只剩这一个杠杆。
 * Line clear: **the more rows, the higher and longer the tone** — with a single tone this is the only lever for "how much did that batch earn".
 * 音高对应原来和弦的第几个音（do / mi / sol / do 高八度）。
 * The pitch matches the old chord's note index (do / mi / sol / do an octave up). */
void sfx_clear(int n) {
    if (n >= 4)      ui_beep(1047, 320);
    else if (n == 3) ui_beep(784, 264);
    else if (n == 2) ui_beep(659, 198);
    else             ui_beep(523, 132);
    ui_vibrate(28, 0);
}

/* 旋转：轻、短、不抢戏 —— 这是个高频动作，响一点就烦。 */
/* Rotate: light, short, unobtrusive — this is a high-frequency action, and anything louder gets annoying. */
void sfx_rotate(void) { ui_beep(1047, 33); }

/* 自然落地：一声闷响（不震）。 */
/* Natural landing: one dull thud (no vibration). */
void sfx_land(void) { ui_beep(165, 66); }

/* 硬降：**更低更长** —— 与自然落地必须分得出（玩家是主动砸的还是没赶上）。
 * Hard drop: **lower and longer** — it must be told apart from a natural landing (did the player slam it, or just not get there in time?).
 * ⚠ 原来两条同为 note 48，靠波形与时长分；波形没了 ⇒ 改用音高（硬降更沉）。
 * ⚠ Both used to be note 48 and were distinguished by waveform and duration; with the waveform gone ⇒ pitch does it (hard drop is heavier). */
void sfx_harddrop(void) { ui_beep(131, 99); }

/* 暂停 / 继续：一低一高，一听就知道是"停"还是"走"。 */
/* Pause / resume: one low and one high, so one listen tells "stop" from "go". */
void sfx_pause(void) { ui_beep(523, 66); }
void sfx_resume(void) { ui_beep(784, 66); }

/* 重开：一声干脆的起手音。 */
/* Restart: one crisp opening note. */
void sfx_restart(void) { ui_beep(659, 99); }

/* 升级：**盖过消行音**（更高更长）—— 升级更值得听见。
 * Level up: **drowns out the line-clear sound** (higher and longer) — a level up is more worth hearing.
 * ⚠ 取整块的**最高音**（原来琶音的顶 96），否则与"四行消"撞成同一个数。
 * ⚠ Takes the block's **highest note** (the top 96 of the old arpeggio), otherwise it would collide with "four-row clear" on the same number. */
void sfx_levelup(void) { ui_beep(2093, 320); }

/* 结束：**低而长** —— 与"升级"正好是两端。 */
/* Game over: **low and long** — exactly the opposite end from "level up". */
void sfx_over(void) {
    ui_beep(131, 320);
    ui_vibrate(220, 0);
}


/* ── 方块几何 ───────────────────────────────────────────── */
/* ── Piece geometry ─────────────────────────────────── */

/* 某个方块某次旋转的**包围盒左上角**，打包成 minx*16 + miny。
 * The **top-left corner of the bounding box** for a given piece and rotation, packed as minx*16 + miny.
 *
 * 为什么需要它：`ui_piece_cell` 的旋转是绕 4×4 包围盒中心转的，I 这种长条转完
 * Why it is needed: `ui_piece_cell` rotates around the center of the 4×4 bounding box, so for a long bar like I the whole shape
 * 整个形状会"跳一格"。做法是"旋转后把包围盒左上角对回原处"，看起来才稳。
 * "jumps one cell" once it is rotated. The trick is to put the bounding box's top-left corner back where it was, which looks stable. */
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
/* Whether the piece placed at (x0,y0) would hit a wall / the floor / an existing block. y<0 means it is still above the top (allowed). */
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
/* ── Save data: high score ────────────────────────── */

/* 把非负整数写成十进制字符串（C 前端的字符串拼接不可靠，逐位自己拼）。
 * Writes a non-negative integer as a decimal string (string concatenation in the C frontend is unreliable, so it is assembled digit by digit).
 * 顺序**必须从前到后**、最后补一个 0 —— 全局 char 数组的赋值是 32 位写，
 * The order **must be front to back**, with a trailing 0 — assigning to a global char array is a 32-bit write,
 * 正序写下来每个下标的低字节都是对的，末尾那个 0 顺手把后面几个字节一起清零。
 * so writing in ascending order leaves the right low byte at every index, and that final 0 conveniently clears the bytes after it as well. */
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
/* ── Game actions ───────────────────────────────────── */

void spawn(void) {
    int m;
    pid = npid;
    rot = 0;
    npid = ui_rand(7);
    px = 3;
    m = piece_min(pid, 0);
    py = 0 - m % 16;                    /* 让形状最高的一行贴在第 0 行 */
                                        /* Puts the shape's topmost row on row 0 */
    if (collide(pid, rot, px, py) != 0) {   /* 出生位就满了 = 结束 */
                                            /* Spawning already collides = game over */
        state = 2;
        submit_score();
        sfx_over();
    }
}

/* 把当前方块写进棋盘、消行、算分。 */
/* Writes the current piece into the board, clears lines and scores. */
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
            /* Shifts everything above this row down by one; **row does not advance** — the row that fell still has to be checked again */
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
            sfx_levelup();             /* 升级盖过消行音：升级更值得听见 */
                                       /* A level up drowns out the line-clear sound: a level up is more worth hearing */
            ui_vibrate(60, 0);
        } else {
            sfx_clear(n);
        }
        set_speed();
    } else {
        sfx_land();                     /* 自然落地：一声闷响，不震 */
                                        /* Natural landing: one dull thud, no vibration */
    }
}

/* 下落一格；落不下去就锁定并出新方块。 */
/* Drops one cell; if it cannot drop, the piece is locked and a new one spawns. */
void step_down(void) {
    if (collide(pid, rot, px, py + 1) == 0) {
        py = py + 1;
    } else {
        lock_piece();
        spawn();
    }
}

/* 旋转（带踢墙：原位不行就往左右各试 1、2 格）。 */
/* Rotate (with wall kicks: if the original position fails, try 1 and then 2 cells to each side). */
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
    sfx_rotate();
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
    sfx_harddrop();                    /* 低频闷响 = "砸下去了" */
                                       /* A low-frequency thud = "slammed it down" */
    ui_vibrate(22, 0);
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
    sfx_restart();
}

/* ── 绘图 ───────────────────────────────────────────────── */
/* ── Drawing ──────────────────────────────────────────── */

/* 一个方块格：圆角实心 + 顶部一条高光。 */
/* One block cell: a filled rounded rectangle plus a highlight strip along the top. */
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
/* Numbers: **drawn digit by digit**, with no string concatenation (that path is unreliable in this frontend, and ten digit literals are enough). */
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
    ui_text_cur(x, by + 10, g_lang == 0 ? "下一个" : "Next");

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
    ui_text_cur(panelX + 10, ty, g_lang == 0 ? "分数" : "Score");
    draw_int(panelX + 10, ty + 16, score, 17, COL_TEXT);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, g_lang == 0 ? "最高" : "Best");
    draw_int(panelX + 10, ty + 16, best, 17, COL_GOLD);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, g_lang == 0 ? "消行" : "Lines");
    draw_int(panelX + 10, ty + 16, nlines, 17, COL_TEXT);

    ty = ty + 52;
    ui_set_font(12, 0, COL_DIM, VML_ANCHOR_LEFT);
    ui_text_cur(panelX + 10, ty, g_lang == 0 ? "等级" : "Level");
    draw_int(panelX + 10, ty + 16, level, 17, COL_ACCENT);
}

/* 暂停 / 结束的遮罩 + 两行提示。提示文案要写**手柄上的键名** ——
 * The pause / game-over overlay plus two lines of text. The wording must name **the keys on the gamepad** —
 * 屏幕上已经没有自绘按键了，写"点「重开」"用户找不到那个东西。
 * there are no self-drawn buttons on screen any more, so telling the user to "tap Restart" leaves them hunting for something that isn't there. */
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
        s = g_lang == 0 ? "游戏结束" : "Game Over";
        s2 = g_lang == 0 ? "按 START 再来一局" : "Press START to restart";
    } else {
        s = g_lang == 0 ? "已暂停" : "Paused";
        s2 = g_lang == 0 ? "按 SELECT 继续" : "Press SELECT to resume";
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
    /* Board: outer frame + background + grid */
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
    /* Settled blocks */
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
    /* Current piece (not drawn once the game is over) */
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
/* ── Action dispatch ────────────────────────────────── */

/* 按一下某个键。返回 **画面是否需要重画** —— 每次重画都要把整幅场景光栅化一遍
 * Presses one key. Returns **whether the frame needs repainting** — every repaint rasterizes the whole scene
 * （手机上是几百毫秒），所以"撞墙没动""结束后乱按"这些情况必须直接跳过重画。
 * (hundreds of milliseconds on a phone), so "hit a wall and did not move" and "random presses after game over" must skip the repaint outright. */
int press(int btn) {
    if (btn == 7) {
        restart();
        return 1;
    }
    if (state == 2) return 0;              /* 结束了：除「重开」外一律无反应 */
                                           /* Game over: nothing responds except "restart" */
    if (btn == 6) {
        if (state == 0) {
            state = 1;
            if (tid > 0) ui_timer_kill(tid);
            tid = 0;
            sfx_pause();
        } else {
            state = 0;
            set_speed();
            sfx_resume();
        }
        return 1;
    }
    if (state != 0) return 0;              /* 暂停中 */
                                           /* paused */
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
 * Hold-to-repeat: holding left/right/down repeats the action every 130ms.
 *
 * 靠 `VML_MSG_KEYUP` 收尾 —— 手柄按下发 KeyDown、抬手发 KeyUp（`DrawWindowPage`
 * It is ended by `VML_MSG_KEYUP` — pressing the gamepad sends KeyDown and releasing sends KeyUp (`DrawWindowPage`
 * 用 Button 的 Pressed/Released，不再是一按就 Down+Up 一起发）。但**不能把"一定会收到
 * uses the Button's Pressed/Released, so a single press no longer fires Down+Up together). But **you must not assume that
 * KeyUp"当成前提**：手指划出按键范围、系统吃掉 CANCEL、页面被切走，都可能让 KeyUp 永远不来，
 * KeyUp will always arrive**: a finger sliding off the button, the system swallowing CANCEL, the page being switched away — any of these can keep KeyUp from ever coming,
 * 而连发一旦跑起来就会一直跑（`ui_timer_set` 是重复定时器）。
 * and once repeat starts running it keeps running (`ui_timer_set` is a repeating timer).
 * 所以连发**自带三道刹车**，任何一道都不会让程序卡在"一直往左移"上：
 * So auto-repeat **carries three brakes of its own**, and none of them lets the program get stuck "drifting left forever":
 *   1. **换键即接管** —— 另一个方向键的 KeyDown 直接改写 `held`；
 *   1. **a new key takes over** — a KeyDown from another arrow key rewrites `held` outright;
 *   2. **按了没动两次就停** —— 已经贴墙了还按，说明再按也没意义；
 *   2. **stop after two presses that moved nothing** — pressing while already against the wall means further presses are pointless;
 *   3. **总拍数上限 40**（约 5 秒）—— 横穿整个棋盘只要 10 拍、竖到底最多 20 拍，
 *   3. **a cap of 40 ticks in total** (about 5 seconds) — crossing the whole board takes only 10 ticks and dropping to the bottom 20,
 *      40 拍远超任何真实操作，纯粹是丢 KeyUp 时的兜底。
 *      so 40 ticks far exceeds any real play and is purely a fallback for a lost KeyUp.
 * 三道里前两道是"手感"，第三道是"安全网"；写游戏时**重复定时器都要有这么一道**。
 * Of the three, the first two are "feel" and the third is a "safety net"; **every repeating timer in a game needs one of these**. */
void hold_repeat(int btn) {
    if (held == btn) {
        if (rid > 0) return;           /* 同一键重复按下（系统重复键）：不重建定时器 */
                                       /* the same key pressed again (system key repeat): do not rebuild the timer */
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
/* One tick of auto-repeat. Returns whether the screen needs repainting. */
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
 * Key code → button code; 0 = this key does nothing.
 * 手柄那几个键在 `VmlKeys` 里刻意映射成了自然键盘等价键（A/B/X/Y 就是字母键、
 * The gamepad keys are deliberately mapped in `VmlKeys` to natural keyboard equivalents (A/B/X/Y are the letter keys,
 * START=回车、SELECT=Shift），所以同一份程序接物理键盘也能玩。
 * START=Enter, SELECT=Shift), so the same program is playable with a physical keyboard. */
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
/* ── Main loop ───────────────────────────────────────── */

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
                              /* Initialize the shape table once (the geometry lives in the shared library and is common to every language) */

    /* **先问可用绘图区，再开窗** —— 窗口宽高就是画布的坐标空间，必须与排版同源。
     * **Ask for the available drawing area first, then open the window** — the window's width and height are the canvas's coordinate space and must come from the same source as the layout.
     * 反过来（按屏幕 744 排版 / 开 360 宽的窗）会把内容画到画布外，实测就是"右边被切掉"。
     * The other way round (laying out for a 744-wide screen but opening a 360-wide window) draws content outside the canvas; measured, that is exactly "the right side gets cut off". */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;
    g_lang = ui_get_language();
    ui_win_open(g_lang == 0 ? "俄罗斯方块" : "Tetris", sw, sh);

    /* 玩游戏时别熄屏 —— 一手不动盯着棋盘想下一步，屏幕自己黑了最扫兴。
     * Don't let the screen sleep while playing — holding still with one hand while you think about the next move, only to have the screen go black on its own, is the most annoying thing.
     * 退出时会关掉（见文件末尾），所以不会一直亮着。
     * It is turned off on exit (see the end of the file), so it does not stay lit forever. */
    ui_keep_on(1);

    /* ── 布局（只算这一处）──
     * ── Layout (computed in this one place only) ──
     *
     * 手柄交给系统之后，整个窗口高度都归棋盘用（旧版要留 140px 画手柄）。
     * With the gamepad handed over to the system, the whole window height goes to the board (the old version had to reserve 140px to draw the gamepad).
     * `reserveP` 是右侧信息面板的宽度，窄屏收一点。
     * `reserveP` is the width of the right-hand info panel, trimmed a little on narrow screens. */
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
    /* ── Game start ── */
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
    /* ⚠ 中文这条整句写在同一行里：跨行拼接时第二段会落在没有 `g_lang == 0` 的行上
       ⚠ The Chinese message is kept whole on one line: if it were split across lines, the second part would land on a line without `g_lang == 0`
       （语言审计脚本按行认），而相邻字面量拼接本来就只是 C 的语法糖。
       (the language audit script works line by line), and adjacent-literal concatenation is only C syntactic sugar anyway. */
    if (g_lang == 0) ui_dlg_msg("俄罗斯方块", "用屏幕下方的游戏按键操作：方向键移动与旋转、A 旋转、B 直落，START 重开、SELECT 暂停。返回箭头退出。", VML_DLG_INFO);
    else             ui_dlg_msg("Tetris", "Use the on-screen gamepad: arrows to move/rotate, A rotate, B hard drop, START restart, SELECT pause. Back arrow to quit.", VML_DLG_INFO);
    draw_all();

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER) {
            if (msg[2] == 1) {                 /* tag 1 = 长按连发 */
                                               /* tag 1 = hold-to-repeat */
                if (repeat_tick() != 0) draw_all();
            } else if (state == 0) {           /* tag 0 = 重力 */
                                               /* tag 0 = gravity */
                step_down();
                draw_all();
            } else if (tid > 0) {
                /* 结束/暂停时把重力停掉 —— 否则定时器会一直往队列里投消息，
                 * Stop gravity on game over / pause — otherwise the timer keeps posting messages into the queue,
                 * 主循环空转不说，暂停期间还白耗电。重开时 set_speed 会重建。
                 * which not only spins the main loop for nothing but also burns power while paused. set_speed rebuilds it on restart. */
                ui_timer_kill(tid);
                tid = 0;
            }
            continue;
        }

        if (t == VML_MSG_KEYUP) {
            k = msg[1];
            /* 只有"抬起的是当前按住的键"才结束连发；否则连发的节奏会被
             * Only a KeyUp for the key currently held ends auto-repeat; otherwise the repeat rhythm would be
             * 另一个方向键的抬起打断（屏幕上同时按两个键是很常见的）。
             * interrupted by the release of the other arrow key (pressing two keys at once on screen is very common). */
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
            /* Auto-repeat is attached only to "actions that can be done continuously", not to one-shot keys like restart/pause */
            if (btn == 1 || btn == 2 || btn == 3) hold_repeat(btn);
        }
    }

    release();
    if (tid > 0) ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
    return 0;
}
