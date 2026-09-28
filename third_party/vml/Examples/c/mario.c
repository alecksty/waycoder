/* mario.c —— 超级玛丽式横版跳跃（C 版），跑在手机端 VML 上
 * mario.c — Super Mario style side-scrolling platformer (C version), running on VML on mobile
 *
 * 编译运行（手机 App 的 vml 工具）：
 * Build and run (with the vml tool of the phone App):
 *     vml run examples/c/mario.c
 *
 * 操作全部走**绘图窗口底部那排屏幕手柄**（与 tetris.c / gomoku.c 同一套）：
 * All controls go through **the row of on-screen gamepad buttons at the bottom of the drawing window** (the same set as tetris.c / gomoku.c):
 *     ← →      左右跑（按住即持续跑）
 *     ← →      run left/right (hold to keep running)
 *     ↑ / A / X 跳
 *     ↑ / A / X jump
 *     START    重开一局
 *     START    restart the round
 *     SELECT   暂停 / 继续
 *     SELECT   pause / resume
 *     返回箭头  退出
 *     back arrow  quit
 *
 * 目标：一路向右，踩敌人、收金币，走到终点旗杆即通关。
 * Goal: run right the whole way, stomp enemies, collect coins, and reach the goal flag to clear the level.
 *
 * ## 三条 C 前端的硬约束（与 tetris.c 同源，踩过才写的）
 * ## Three hard constraints of the C front end (same origin as tetris.c, written after stepping on them)
 *
 * 1. ⚠ **`${}` 占位符只认局部变量** ⇒ 调 syscall 一律走 `waycoder_ui.h` 的包装函数，
 * 1. ⚠ **the `${}` placeholder only recognizes local variables** ⇒ always call syscalls through the wrapper functions of `waycoder_ui.h`,
 *    **绝不**自己写 `asm("SYSCALL #525, ${gx}, ...")` 去传全局变量（会退化成 R12+0）。
 *    **never** write `asm("SYSCALL #525, ${gx}, ...")` yourself to pass a global variable (it degrades to R12+0).
 * 2. **文件级全局变量是好的** —— 关卡与实体状态全放全局。
 * 2. **file-level globals are fine** — level and entity state all live in globals.
 * 3. ⚠ **`#define` 不支持反斜杠续行** ⇒ 宏一行写完，多行说明另起块注释。
 * 3. ⚠ **`#define` does not support backslash line continuation** ⇒ write the macro on one line and put multi-line notes in a separate block comment.
 *
 * ## 为什么关卡用「矩形列表」而不是瓦片地图
 * ## Why the level uses a "rectangle list" instead of a tile map
 *
 * 瓦片地图要一张 `int map[200*16]` 的大表（3200 项）—— 编成 `.word` 数据段又长又难改。
 * A tile map would need a big `int map[200*16]` table (3200 entries) — compiled into a `.word` data section it is long and hard to edit.
 * 平台游戏真正需要的「碰撞体」本来就是**一组矩形**，所以直接摆矩形：
 * What a platformer really needs as "collision bodies" is **a set of rectangles** in the first place, so we lay out rectangles directly:
 * `PLAT[]` 每 4 项一个平台（列, 行, 宽, 高）。碰撞、绘制都扫这一份，**不会两处不一致**。
 * `PLAT[]` holds one platform per 4 entries (column, row, width, height). Both collision and drawing scan this single copy, so **the two can never disagree**.
 *
 * ## 物理为什么全用整数
 * ## Why the physics is entirely integer
 *
 * 位置/速度都是「像素·每拍」的整数：`40ms` 一拍 ⇒ 25fps。用浮点在这里没有收益
 * Positions and velocities are all integers in "pixels per tick": one tick is `40ms` ⇒ 25fps. Floating point buys nothing here
 * （像素坐标最终也要取整），却要多担一份「前端浮点语义」的风险 —— 整数确定、好复现。
 * (pixel coordinates get rounded in the end anyway), yet it adds one more risk about "front end floating-point semantics" — integers are deterministic and easy to reproduce.
 * 调参就动 `GRAV` / `JUMPV` / `RUNV` 三个宏，跳跃高度 = `JUMPV²/(2·GRAV)` 像素。
 * To tune, just touch the three macros `GRAV` / `JUMPV` / `RUNV`; jump height = `JUMPV²/(2·GRAV)` pixels.
 */

#include <waycoder_ui.h>
#include <stdlib.h>

/* ── 世界与物理 ─────────────────────────────────────────── */
/* ── World and physics ──────────────────────────────────── */

#define ROWS      16        /* 关卡纵向格数（第 14、15 行是地面） */
/* Number of rows in the level (rows 14 and 15 are the ground). */
#define WORLDCOL  200       /* 世界横向格数 */
/* Number of columns in the world. */
#define TICK_MS   40        /* 一拍 = 40ms（25fps） */
/* One tick = 40ms (25fps). */
#define GRAV      1         /* 每拍重力加速度（像素/拍²） */
/* Gravity per tick (pixels/tick²). */
#define JUMPV     (-13)     /* 起跳初速（像素/拍，负=向上） */
/* Initial jump velocity (pixels/tick, negative = upward). */
#define MAXVY     15        /* 下落终速（防穿模） */
/* Terminal falling speed (prevents tunneling). */
#define RUNV      4         /* 跑速（像素/拍） */
/* Running speed (pixels/tick). */
/* 一跃能跨多远 = 2·|JUMPV|/GRAV · RUNV = 2·13·4 = 104px（4.2 格）。
 * How far one jump carries = 2·|JUMPV|/GRAV · RUNV = 2·13·4 = 104px (4.2 tiles).
 * 悬崖宽 2 格 = 50px，而人要"整个人"过去（宽 19px）实际需要 69px
 * A chasm is 2 tiles = 50px wide, but getting the whole character across (19px wide) actually needs 69px
 * —— 原先 JUMPV=-12/RUNV=3 只有 72px，仅剩 3px 余量，等于跳不过去（用户实测）。
 * — with the old JUMPV=-12/RUNV=3 there were only 72px, leaving a 3px margin, i.e. it could not be cleared (measured by the user). */
#define COYOTE    5         /* 土狼时间：离地后还有几拍可以起跳 */
/* Coyote time: how many ticks after leaving the ground you may still jump. */
#define FOEV      1         /* 敌人巡逻速度 */
/* Enemy patrol speed. */
#define START_LIFE 3
#define TIME_LIMIT 300

/* ── 配色（0xAARRGGBB）───────────────────────────────────── */
/* ── Colors (0xAARRGGBB) ────────────────────────────────── */

#define COL_SKY     0xFF5C94FC
#define COL_HILL    0xFF3EA23E
#define COL_CLOUD   0xFFFFFFFF
#define COL_GROUND  0xFFB5651D
#define COL_GRASS   0xFF4CC44C
#define COL_BRICK   0xFFC8703C
#define COL_BRICK_T 0xFFE8A06C
#define COL_GOLD    0xFFFACC15
#define COL_GOLD_D  0xFFC79A0A
#define COL_MARIO_R 0xFFE23B2E
#define COL_SKIN    0xFFF7C89B
#define COL_OVERALL 0xFF2B5BD7
#define COL_SHOE    0xFF6B3410
#define COL_FOE     0xFF9A5B2C
#define COL_FOE_D   0xFF6E3F1B
#define COL_HUD     0xCC101018
#define COL_TEXT    0xFFEDEDF2
#define COL_WARN    0xFFF87171
#define COL_FLAG    0xFF25C24A
#define COL_POLE    0xFFCFCFD8

/* ── 关卡：平台 (列,行,宽,高) ───────────────────────────── */
/* ── Level: platforms (column,row,width,height) ─────────── */

int PLAT[120] = {
    /* 地面段（中间留坑，掉下去丢一条命）*/
    /* Ground segments (gaps left in the middle; falling in costs a life). */
     0, 14, 24, 2,   26, 14, 16, 2,   44, 14, 10, 2,   56, 14, 22, 2,
    80, 14, 14, 2,   96, 14, 20, 2,  118, 14, 12, 2,  132, 14, 24, 2,
   158, 14, 18, 2,  178, 14, 22, 2,
    /* 中层（第 10 行）*/
    /* Middle tier (row 10). */
    10, 10,  4, 1,   30, 10,  5, 1,   48, 10,  4, 1,   62, 10,  6, 1,
    86, 10,  4, 1,  104, 10,  5, 1,  124, 10,  4, 1,  140, 10,  6, 1,
   166, 10,  4, 1,  186, 10,  5, 1,
    /* 高层（第 7 行）*/
    /* Upper tier (row 7). */
    20,  7,  3, 1,   40,  7,  3, 1,   72,  7,  4, 1,  112,  7,  3, 1,
   150,  7,  4, 1,  172,  7,  3, 1,
    /* 终点前的台阶 */
    /* The steps just before the goal. */
   180, 12,  2, 1,  182, 11,  2, 1,  184, 10,  2, 1
};

int PLATN = 29;             /* 实际平台数（上面摆了 29 组）*/
/* Actual platform count (29 groups are laid out above). */

/* ── 关卡：金币 (列,行) ─────────────────────────────────── */
/* ── Level: coins (column,row) ──────────────────────────── */

int COIN[100] = {
    11, 9,  12, 9,  13, 9,   31, 9,  32, 9,  33, 9,   49, 9,  50, 9,
    63, 9,  64, 9,  65, 9,   87, 9,  88, 9,  105, 9, 106, 9,
   125, 9, 126, 9,  141, 9, 142, 9, 143, 9,  167, 9, 168, 9, 187, 9, 188, 9,
    21, 6,  41, 6,   73, 6,  74, 6, 113, 6,  151, 6, 152, 6, 173, 6,
     5,13,   6,13,    7,13,   20,13,  21,13,   46,13,  47,13,  60,13,  61,13,
   100,13, 101,13, 136,13, 137,13, 162,13, 163,13, 190,13, 191,13
};

int COINN = 49;

/* ── 关卡：敌人 (列,行) ─────────────────────────────────── */
/* ── Level: enemies (column,row) ────────────────────────── */

int FOE[24] = {
    20, 13,  34, 13,  50, 13,  66, 13,  88, 13,
   106, 13, 126, 13, 144, 13, 170, 13
};

int FOEN = 9;

#define GOALCOL 194         /* 终点旗杆所在列 */
/* Column where the goal flag stands. */

/* ── 运行时状态 ─────────────────────────────────────────── */
/* ── Runtime state ──────────────────────────────────────── */

int g_lang;                 /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
/* UI language: looked up once at start (`ui_get_language` is a syscall, do not call it every frame). */
int TILE;                   /* 一格边长（像素），开窗后按画布算 */
/* Side length of one tile in pixels, computed from the canvas after the window opens. */
int VIEWW;                  /* 画布宽（= 窗口宽）*/
/* Canvas width (= window width). */
int VIEWH;
int HUDH;                   /* 顶部 HUD 高度 */
/* Height of the HUD at the top. */

int px, py;                 /* 玩家左上角（世界像素坐标）*/
/* Player's top-left corner (world pixel coordinates). */
int pw, ph;                 /* 玩家碰撞盒尺寸 */
/* Player collision box size. */
int vx, vy;
int onGround;
int face;                   /* 朝向：1 右 / -1 左 */
/* Facing: 1 right / -1 left. */

int camX;                   /* 摄像机左上角世界 x */
/* World x of the camera's top-left corner. */
int score, coins, lives, timer_, tick10;
int state;                  /* 0=进行 1=暂停 2=结束 3=通关 */
/* 0=running 1=paused 2=over 3=cleared. */
int held;                   /* 当前按住的方向键（0/LEFT/RIGHT）*/
/* The direction key currently held (0/LEFT/RIGHT). */
int lastkey;                /* 最后一次收到的键码（临时调试用）*/
/* The last key code received (for temporary debugging). */
int holdTicks;              /* 同一方向连续按住的拍数（丢 KeyUp 时的刹车）*/
/* Ticks the same direction has been held non-stop (the brake for a lost KeyUp). */
int coyote;                 /* 土狼时间剩余拍数（>0 时允许起跳）*/
/* Remaining coyote-time ticks (a jump is allowed while >0). */
int lastvx;                 /* 空中保留的水平动量（见 tick_world 里的说明）*/
/* Horizontal momentum kept in the air (see the notes inside tick_world). */
int frame;

int ex[16], ey[16], ed[16], ealive[16];
int cgot[50];

char hbuf[24];              /* 数字转字符串的暂存（全局，见文件头注释 2）*/
/* Scratch buffer for number-to-string (global, see note 2 in the file header). */

/* ── 小工具 ─────────────────────────────────────────────── */
/* ── Small helpers ──────────────────────────────────────── */

/* 整数转十进制字符串 —— 不依赖 sprintf（本环境 printf 家族在桌面脚手架上有已知问题）。*/
/* Integer to decimal string — does not rely on sprintf (in this environment the printf family has known problems on the desktop scaffold). */
char* num_str(int v) {
    int i;
    int n;
    int neg;
    neg = 0;
    if (v < 0) { neg = 1; v = -v; }
    i = 0;
    if (v == 0) { hbuf[0] = '0'; hbuf[1] = 0; return hbuf; }
    while (v > 0) {
        hbuf[i] = '0' + (v % 10);
        i = i + 1;
        v = v / 10;
    }
    if (neg) { hbuf[i] = '-'; i = i + 1; }
    hbuf[i] = 0;
    /* 反转 */
    /* Reverse. */
    n = 0;
    while (n < i / 2) {
        char t;
        t = hbuf[n];
        hbuf[n] = hbuf[i - 1 - n];
        hbuf[i - 1 - n] = t;
        n = n + 1;
    }
    return hbuf;
}

int abs_i(int v) { if (v < 0) return -v; return v; }

/* ── 碰撞：这一格是不是实心 ─────────────────────────────── */
/* ── Collision: is this tile solid ──────────────────────── */

int solid(int c, int r) {
    int i;
    int x;
    int y;
    int w;
    int h;
    if (r >= ROWS) return 0;            /* 掉出世界底部 = 坑 */
    /* Falling out of the bottom of the world = a pit. */
    if (c < 0 || c >= WORLDCOL) return 1; /* 世界左右当墙 */
    /* The left and right edges of the world act as walls. */
    i = 0;
    while (i < PLATN) {
        x = PLAT[i * 4];
        y = PLAT[i * 4 + 1];
        w = PLAT[i * 4 + 2];
        h = PLAT[i * 4 + 3];
        if (c >= x && c < x + w && r >= y && r < y + h) return 1;
        i = i + 1;
    }
    return 0;
}

/* ── 开局 / 复位 ────────────────────────────────────────── */
/* ── Start / reset ──────────────────────────────────────── */

void spawn_player(void) {
    px = 2 * TILE;
    py = 14 * TILE - ph;
    vx = 0;
    vy = 0;
    onGround = 1;
    face = 1;
    lastvx = 0;
}

void reset_round(void) {
    int i;
    score = 0;
    coins = 0;
    lives = START_LIFE;      /* 重开一局连命一起复位 —— 否则「游戏结束」后按 START
                              * Restarting a round resets the lives along with everything else — otherwise pressing START after "Game Over"
                              * 会带着 0 条命立刻再结束一次（看着像没反应）
                              * would end the round again at once with 0 lives (which looks like nothing happened). */
    timer_ = TIME_LIMIT;
    tick10 = 0;
    state = 0;
    held = 0;
    holdTicks = 0;
    coyote = 0;
    lastvx = 0;
    frame = 0;
    camX = 0;
    i = 0;
    while (i < COINN) { cgot[i] = 0; i = i + 1; }
    i = 0;
    while (i < FOEN) {
        ex[i] = FOE[i * 2] * TILE;
        ey[i] = FOE[i * 2 + 1] * TILE;
        ed[i] = 1;
        ealive[i] = 1;
        i = i + 1;
    }
    spawn_player();
}

/* ── 音效：**用 ui_beep 单音**（v0.96.509 统一换回来） ────────────
 * ── Sound: **single-tone ui_beep** (switched back wholesale in v0.96.509) ────
 *
 * ⚠⚠ 这些音一度走共享库的音序器（`ui_sfx_add`），**真机上破音**，全部换回来了。
 * ⚠⚠ These sounds once went through the shared library's sequencer (`ui_sfx_add`) and **distorted on a real device**, so they were all switched back.
 *   破音的是**这里配的音**：通关那一版是**上行大三和弦 + 高八度**（4 个声部叠着响），
 *   What distorted was **the tuning done here**: the win version was **an ascending major triad + an octave up** (4 voices ringing at once),
 *   多声部混音一叠加，音量就顶到削波；受伤的末音还拖了 8 拍（一拍 33ms）。
 *   and once those voices were mixed together the volume hit clipping; the last note of the hurt sound also dragged on for 8 ticks (one tick = 33ms).
 *   `ui_beep` 是**单通道**的（后一个音掐掉前一个）⇒ 一个事件永远只有一个音在响，
 *   `ui_beep` is **single channel** (the next note cuts off the previous one) ⇒ an event never has more than one note ringing,
 *   **结构上不可能削波**。代价是没有和弦、没有音色。
 *   so clipping is **structurally impossible**. The price is no chords and no timbre.
 *
 * ⚠ 频率取整块的**首音**（哪个事件什么音是设计，不是机制），时长取整块时长、
 * ⚠ The frequency is the **first note** of the whole block (which event gets which note is design, not mechanism), the duration is the block's duration,
 *   封顶 320ms。**通关是例外**：取整块**最高音** —— 「不看屏幕也分得出」靠这个
 *   capped at 320ms. **The win sound is the exception**: it takes the block's **highest note** — that is what makes it recognizable without looking at the screen
 *   （五子棋/象棋两版也是这么配的：赢 1320 / 输 240）。
 *   (the gomoku and chess versions are tuned the same way: win 1320 / lose 240).
 *   低音不低于 C3(131Hz) —— 手机外放在 200Hz 以下衰减很快，玩家听着像没响。
 *   Keep low notes at or above C3 (131Hz) — a phone speaker rolls off fast below 200Hz, so the player hears it as silence.
 */

void sfx_jump(void)   { ui_beep(392, 99); }     /* 跳：中音一挑，短 */
/* Jump: a short flick in the mid range. */
void sfx_coin(void)   { ui_beep(1319, 198); }   /* 金币：**最高最长** —— 最有辨识度 */
/* Coin: **the highest and the longest** — the most recognizable. */
void sfx_stomp(void)  { ui_beep(131, 99); }     /* 踩敌：一声闷响（最低） */
/* Stomp: a dull thud (the lowest). */
void sfx_hurt(void) {
    ui_beep(262, 320);                          /* 受伤：低而长 */
    /* Hurt: low and long. */
    ui_vibrate(180, 0);
}
void sfx_win(void) {
    ui_beep(1047, 320);                         /* 通关（结局）：**最高音**、最长 */
    /* Cleared (ending): **the highest note**, and the longest. */
    ui_vibrate(120, 0);
}

/* ── 玩家物理 ───────────────────────────────────────────── */
/* ── Player physics ─────────────────────────────────────── */

/* 水平：先位移、再按格推出。直接操作全局（见 §3 约束 2）。
 * Horizontal: move first, then push out tile by tile. Operates directly on globals (see constraint 2 in §3).
 *
 * ⚠ **别把这里的「不走指针形参」当成某个 bug 的修复** —— 它不是。
 * ⚠ **do not read "no pointer parameters here" as the fix for some bug** — it is not one.
 * 起初怀疑「通过指针形参写回不生效」（现象：按键收得到、`face` 在变、唯独 `px` 不动），
 * At first we suspected "writing back through a pointer parameter has no effect" (symptom: keys are received, `face` changes, only `px` never moves),
 * 但最小用例 `.scratch/ptrtest.c` 实测 **`*p = 99` 对全局和局部都正常写回**，该假设已推翻。
 * but the minimal case `.scratch/ptrtest.c` measured that **`*p = 99` writes back fine for both globals and locals**, so that hypothesis was refuted.
 * 改成直接操作全局只是顺手简化 + 与指南口径一致；**「人不动」的真因当时未查明**。
 * Switching to operating directly on globals was just a convenient simplification consistent with the guide; **the real cause of "the character does not move" was never found back then**.
 */
void move_x(void) {
    int c0;
    int c1;
    int r0;
    int r1;
    int r;
    px = px + vx;
    if (px < 0) { px = 0; vx = 0; }
    c0 = px / TILE;
    c1 = (px + pw - 1) / TILE;
    r0 = py / TILE;
    r1 = (py + ph - 1) / TILE;
    r = r0;
    while (r <= r1) {
        if (vx > 0 && solid(c1, r)) { px = c1 * TILE - pw; vx = 0; }
        if (vx < 0 && solid(c0, r)) { px = (c0 + 1) * TILE; vx = 0; }
        r = r + 1;
    }
}

/* 垂直：落地判定写在里面（onGround）。同样直接操作全局。*/
/* Vertical: the landing test lives inside it (onGround). Also operates directly on globals. */
void move_y(void) {
    int c0;
    int c1;
    int r0;
    int r1;
    int c;
    onGround = 0;
    vy = vy + GRAV;
    if (vy > MAXVY) vy = MAXVY;
    py = py + vy;
    c0 = px / TILE;
    c1 = (px + pw - 1) / TILE;
    r0 = py / TILE;
    r1 = (py + ph - 1) / TILE;
    c = c0;
    while (c <= c1) {
        if (vy > 0 && solid(c, r1)) { py = r1 * TILE - ph; vy = 0; onGround = 1; }
        if (vy < 0 && solid(c, r0)) { py = (r0 + 1) * TILE; vy = 0; }
        c = c + 1;
    }
}

/* 摄像头跟随：玩家保持在画面中间偏左，并夹在世界内。*/
/* Camera follow: keep the player left of the screen center, and clamp the camera inside the world. */
void follow_cam(void) {
    int want;
    int camMax;
    want = px - VIEWW / 3;
    if (want < 0) want = 0;
    camMax = WORLDCOL * TILE - VIEWW;
    if (camMax < 0) camMax = 0;
    if (want > camMax) want = camMax;
    camX = want;
}

/* ── 每拍逻辑 ───────────────────────────────────────────── */
/* ── Per-tick logic ─────────────────────────────────────── */

void hurt(void) {
    sfx_hurt();
    lives = lives - 1;
    if (lives <= 0) {
        state = 2;
        return;
    }
    spawn_player();
    camX = 0;
}

void hit_goal(void) {
    state = 3;
    sfx_win();
    score = score + 1000;
    /* 只弹**一个**对话框 —— 先 msg 再 select 会连弹两次，用户要连点两下才回得来。*/
    /* Pop up **one** dialog only — doing msg and then select pops two in a row, and the user has to tap twice to get back. */
    if (ui_dlg_select(g_lang == 0 ? "超级玛丽" : "Mario",
                      g_lang == 0 ? "通关！你走到了终点旗杆。再来一局吗？"
                                  : "You win! You reached the goal flag. Play again?",
                      g_lang == 0 ? "再来一局\n退出" : "Play again\nQuit", 2, 0) == 0) {
        reset_round();
    } else {
        ui_win_close();
    }
}

void tick_world(void) {
    int i;
    int ebox;
    int pbox;
    int cx;
    int cy;
    int hittop;

    if (state != 0) return;

    frame = frame + 1;

    /* 计时（每 25 拍扣 1 秒）*/
    /* Timing (1 second is deducted every 25 ticks). */
    tick10 = tick10 + 1;
    if (tick10 >= 25) {
        tick10 = 0;
        timer_ = timer_ - 1;
        if (timer_ <= 0) { timer_ = 0; hurt(); return; }
    }

    /* ── 输入 → 水平速度 ────────────────────────────────────
     * ── Input → horizontal speed ───────────────────────────
     *
     * ⚠ **空中必须保留水平动量**，不能每拍都按"当前按着哪个方向键"重算。
     * ⚠ **horizontal momentum must be kept in the air**; it cannot be recomputed every tick from "which direction key is held right now".
     * 原因是屏幕手柄**只有一个按键槽**（`DrawWindowPage._padDownKey`）：
     * The reason is that the on-screen gamepad has **only one key slot** (`DrawWindowPage._padDownKey`):
     * 按新键时宿主会先替旧键补一条 `KeyUp`（那是为了修"滑键丢 KeyUp"），
     * pressing a new key makes the host first send a `KeyUp` for the old one (that was to fix "a lost KeyUp while sliding"),
     * 所以**按住 → 的同时按跳，在按键层面做不到** —— 收到跳的瞬间 → 已经被抬起。
     * so **holding → while pressing jump cannot be done at the key-event level** — the moment the jump arrives, → has already been released.
     * 若空中也按按键重算，起跳瞬间水平速度就是 0，人只会直上直下掉进坑里
     * If the speed were recomputed from the keys in the air too, the horizontal speed at the instant of the jump would be 0, and the character would just drop straight down into the pit
     * （用户实测：「不能跳跃和移动一起按」）。
     * (measured by the user: "cannot press jump and move at the same time").
     *
     * 保留动量之后，一根手指就够：按住 → 跑起来 → 松手按跳，动量把这一跳带过去；
     * Once the momentum is kept, one finger is enough: hold → to get running → release and press jump, and the momentum carries the jump across;
     * 空中再按方向键仍可微调（那是有意允许的）。
     * pressing a direction key in the air still fine-tunes the speed (that is deliberately allowed).
     */
    if (onGround != 0) {
        vx = 0;
        if (held == VML_KEY_LEFT) { vx = -RUNV; face = -1; }
        if (held == VML_KEY_RIGHT) { vx = RUNV; face = 1; }
        lastvx = vx;
    } else {
        if (held == VML_KEY_LEFT) { lastvx = -RUNV; face = -1; }
        else if (held == VML_KEY_RIGHT) { lastvx = RUNV; face = 1; }
        vx = lastvx;
    }

    /* 丢 KeyUp 的刹车：同一个方向连续按超过 6 秒就当松开了。
     * The brake for a lost KeyUp: holding the same direction non-stop for more than 6 seconds counts as released.
     * （手机上手指出按键范围、系统吃掉 CANCEL 都可能让 KeyUp 永远不来。）
     * (On a phone the finger sliding off the button or the system eating the CANCEL can both make the KeyUp never arrive.)
     */
    if (held != 0) {
        holdTicks = holdTicks + 1;
        if (holdTicks > 150) { held = 0; holdTicks = 0; }
    }

    move_x();
    move_y();

    /* 土狼时间：刚离开地面（走下平台边缘 / 起跳后）的头几拍仍允许起跳。
     * Coyote time: during the first few ticks after leaving the ground (walking off a platform edge / after a jump) a jump is still allowed.
     * 没有它的话，玩家必须"在还在平台上时"就按跳 —— 手感上就是
     * Without it the player has to press jump "while still on the platform" — which feels like
     * 「明明按了却没跳」、看着像按键失灵（这正是"跳不过悬崖"的一大半体感来源）。
     * "I clearly pressed it but it did not jump", which looks like the keys are broken (this is more than half of the "cannot clear the chasm" feel).
     */
    if (onGround != 0) coyote = COYOTE;
    else if (coyote > 0) coyote = coyote - 1;

    /* 掉进坑里 */
    /* Fell into a pit. */
    if (py > (ROWS + 2) * TILE) { hurt(); return; }

    /* ── 金币 ── */
    /* ── Coins ── */
    i = 0;
    while (i < COINN) {
        if (cgot[i] == 0) {
            cx = COIN[i * 2] * TILE + TILE / 2;
            cy = COIN[i * 2 + 1] * TILE + TILE / 2;
            if (cx > px - TILE / 2 && cx < px + pw + TILE / 2 &&
                cy > py - TILE / 2 && cy < py + ph + TILE / 2) {
                cgot[i] = 1;
                coins = coins + 1;
                score = score + 100;
                sfx_coin();
            }
        }
        i = i + 1;
    }

    /* ── 敌人 ── */
    /* ── Enemies ── */
    i = 0;
    while (i < FOEN) {
        if (ealive[i] != 0) {
            /* 巡逻：撞墙或走到平台边缘就掉头 */
            /* Patrol: turn around on hitting a wall or on reaching the edge of a platform. */
            ex[i] = ex[i] + ed[i] * FOEV;
            if (solid((ex[i] + (ed[i] > 0 ? TILE - 1 : 0)) / TILE, ey[i] / TILE)) {
                ed[i] = -ed[i];
                ex[i] = ex[i] + ed[i] * FOEV * 2;
            } else if (!solid((ex[i] + (ed[i] > 0 ? TILE - 1 : 0)) / TILE, ey[i] / TILE + 1)) {
                ed[i] = -ed[i];
            }

            /* 与玩家的碰撞盒 */
            /* Collision box against the player. */
            ebox = 0;
            if (ex[i] < px + pw && ex[i] + TILE > px &&
                ey[i] < py + ph && ey[i] + TILE > py) ebox = 1;

            if (ebox != 0) {
                hittop = 0;
                /* 踩头：正在下落，且上一拍还在敌人头顶之上 */
                /* Stomp on the head: falling, and on the previous tick still above the top of the enemy. */
                if (vy > 0 && py + ph - vy <= ey[i] + TILE / 2) hittop = 1;
                if (hittop != 0) {
                    ealive[i] = 0;
                    vy = JUMPV / 2;           /* 踩完弹一下 */
                    /* Bounce a little after the stomp. */
                    score = score + 200;
                    sfx_stomp();
                } else {
                    hurt();
                    return;
                }
            }
        }
        i = i + 1;
    }

    /* ── 终点 ── */
    /* ── Goal ── */
    if (px + pw > GOALCOL * TILE) { hit_goal(); return; }

    follow_cam();
}

/* ── 绘制 ───────────────────────────────────────────────── */
/* ── Drawing ────────────────────────────────────────────── */

/* 世界矩形 → 屏幕矩形（越界直接返回 0，省掉无效绘制）。*/
/* World rectangle → screen rectangle (returns 0 when off-screen, saving useless drawing). */
int to_screen(int wx, int* sx) {
    *sx = wx - camX;
    if (*sx > VIEWW || *sx < -TILE * 4) return 0;
    return 1;
}

void draw_sky(void) {
    int i;
    int sx;
    int hy;
    ui_clear(COL_SKY);
    /* 云：位置只跟世界走，滚动时有视差感（用 camX/3）*/
    /* Clouds: the position follows the world only, which gives a parallax feel while scrolling (using camX/3). */
    i = 0;
    while (i < 14) {
        sx = i * 340 + 40 - camX / 3;
        if (sx > -80 && sx < VIEWW + 80) {
            ui_circle(sx, 60, 22, COL_CLOUD, 1, 0);
            ui_circle(sx + 26, 68, 18, COL_CLOUD, 1, 0);
            ui_circle(sx - 26, 68, 16, COL_CLOUD, 1, 0);
        }
        i = i + 1;
    }
    /* 远山 */
    /* Distant hills. */
    i = 0;
    while (i < 10) {
        sx = i * 520 + 120 - camX / 2;
        if (sx > -160 && sx < VIEWW + 160) {
            ui_circle(sx, 14 * TILE - 10, 90, COL_HILL, 1, 0);
        }
        i = i + 1;
    }
}

void draw_platforms(void) {
    int i;
    int x;
    int y;
    int w;
    int h;
    int sx;
    int sy;
    int j;
    int k;
    i = 0;
    while (i < PLATN) {
        x = PLAT[i * 4];
        y = PLAT[i * 4 + 1];
        w = PLAT[i * 4 + 2];
        h = PLAT[i * 4 + 3];
        sx = x * TILE - camX;
        if (sx < VIEWW && sx + w * TILE > 0) {
            sy = y * TILE;
            ui_rect(sx, sy, w * TILE, h * TILE, COL_GROUND, 1, 0, 0);
            ui_rect(sx, sy, w * TILE, 6, COL_GRASS, 1, 0, 0);
            /* 砖缝：让它看得出是一格一格的 */
            /* Brick seams: so you can see it is made of individual tiles. */
            j = 0;
            while (j < w) {
                k = 0;
                while (k < h) {
                    ui_rect(sx + j * TILE, sy + k * TILE, 2, TILE, COL_BRICK, 1, 0, 0);
                    k = k + 1;
                }
                ui_rect(sx + j * TILE, sy, TILE, 2, COL_BRICK_T, 1, 0, 0);
                j = j + 1;
            }
        }
        i = i + 1;
    }
}

void draw_coins(void) {
    int i;
    int sx;
    int sy;
    int r;
    i = 0;
    r = TILE / 5;
    if (r < 3) r = 3;
    while (i < COINN) {
        if (cgot[i] == 0) {
            sx = COIN[i * 2] * TILE + TILE / 2 - camX;
            sy = COIN[i * 2 + 1] * TILE + TILE / 2;
            if (sx > -TILE && sx < VIEWW + TILE) {
                /* 金币做成"扁的"，随帧数轻微变化 ⇒ 有点旋转的错觉 */
                /* The coin is drawn "flat" and changes slightly with the frame count ⇒ a hint of rotation. */
                ui_circle(sx, sy, r, COL_GOLD_D, 1, 0);
                ui_circle(sx, sy, r - 1, COL_GOLD, 1, 0);
                if ((frame / 4 + i) % 2 == 0) ui_rect(sx - 1, sy - r + 1, 2, r * 2 - 2, COL_GOLD_D, 1, 0, 0);
            }
        }
        i = i + 1;
    }
}

void draw_foes(void) {
    int i;
    int sx;
    int sy;
    int wob;
    i = 0;
    while (i < FOEN) {
        if (ealive[i] != 0) {
            sx = ex[i] - camX;
            sy = ey[i];
            if (sx > -TILE * 2 && sx < VIEWW + TILE) {
                wob = (frame / 4 + i) % 2;
                ui_rect(sx + 2, sy + 4, TILE - 4, TILE - 4, COL_FOE, 1, 0, TILE / 3);
                ui_rect(sx + 2, sy + TILE - 5, TILE - 4, 4, COL_FOE_D, 1, 0, 2);
                /* 两只眼睛 */
                /* The two eyes. */
                ui_rect(sx + TILE / 3, sy + TILE / 2 - 2, 3, 4, COL_TEXT, 1, 0, 0);
                ui_rect(sx + TILE - TILE / 3 - 3, sy + TILE / 2 - 2, 3, 4, COL_TEXT, 1, 0, 0);
                /* 两只脚：交替抬起 ⇒ 看着在走 */
                /* The two feet lift alternately ⇒ it looks like walking. */
                if (wob == 0) {
                    ui_rect(sx + 3, sy + TILE - 1, TILE / 3, 3, COL_FOE_D, 1, 0, 1);
                    ui_rect(sx + TILE - 3 - TILE / 3, sy + TILE - 4, TILE / 3, 3, COL_FOE_D, 1, 0, 1);
                } else {
                    ui_rect(sx + 3, sy + TILE - 4, TILE / 3, 3, COL_FOE_D, 1, 0, 1);
                    ui_rect(sx + TILE - 3 - TILE / 3, sy + TILE - 1, TILE / 3, 3, COL_FOE_D, 1, 0, 1);
                }
            }
        }
        i = i + 1;
    }
}

void draw_goal(void) {
    int sx;
    int top;
    sx = GOALCOL * TILE - camX;
    if (sx < -TILE * 2 || sx > VIEWW + TILE) return;
    top = 14 * TILE - 5 * TILE;      /* 旗杆高 5 格 */
    /* The flagpole is 5 tiles tall. */
    ui_rect(sx, top, 4, 5 * TILE, COL_POLE, 1, 0, 0);
    ui_circle(sx + 2, top, 5, COL_POLE, 1, 0);
    ui_rect(sx + 4, top + 8, TILE + 10, TILE - 2, COL_FLAG, 1, 0, 0);
}

void draw_player(void) {
    int sx;
    int sy;
    int step;
    sx = px - camX;
    sy = py;
    step = (frame / 3) % 2;

    /* 影子 */
    /* Shadow. */
    ui_rect(sx + 2, sy + ph - 3, pw - 4, 3, 0x55000000, 1, 0, 1);

    /* 腿（跑动时交替）*/
    /* Legs (alternating while running). */
    if (onGround != 0 && vx != 0) {
        if (step == 0) {
            ui_rect(sx + 1, sy + ph - 6, pw / 2 - 1, 6, COL_OVERALL, 1, 0, 1);
            ui_rect(sx + pw / 2, sy + ph - 6, pw / 2 - 1, 4, COL_OVERALL, 1, 0, 1);
        } else {
            ui_rect(sx + 1, sy + ph - 6, pw / 2 - 1, 4, COL_OVERALL, 1, 0, 1);
            ui_rect(sx + pw / 2, sy + ph - 6, pw / 2 - 1, 6, COL_OVERALL, 1, 0, 1);
        }
    } else {
        ui_rect(sx + 1, sy + ph - 6, pw - 2, 6, COL_OVERALL, 1, 0, 1);
    }
    /* 鞋 */
    /* Shoes. */
    ui_rect(sx, sy + ph - 3, pw, 3, COL_SHOE, 1, 0, 1);

    /* 身体 */
    /* Body. */
    ui_rect(sx + 1, sy + ph / 3, pw - 2, ph / 2, COL_MARIO_R, 1, 0, 2);
    ui_rect(sx + pw / 2 - 2, sy + ph / 3 + 2, 4, ph / 3 - 2, COL_OVERALL, 1, 0, 1);

    /* 头 */
    /* Head. */
    ui_rect(sx + 1, sy + 2, pw - 2, ph / 3, COL_SKIN, 1, 0, 2);
    /* 帽子 + 帽檐（帽檐朝面朝的方向）*/
    /* Cap + brim (the brim points the way the character faces). */
    ui_rect(sx, sy, pw, 5, COL_MARIO_R, 1, 0, 2);
    if (face > 0) ui_rect(sx + pw - 5, sy + 4, 7, 3, COL_MARIO_R, 1, 0, 1);
    else ui_rect(sx - 2, sy + 4, 7, 3, COL_MARIO_R, 1, 0, 1);
    /* 眼睛 */
    /* Eyes. */
    if (face > 0) ui_rect(sx + pw - 6, sy + 8, 3, 3, 0xFF202030, 1, 0, 0);
    else ui_rect(sx + 3, sy + 8, 3, 3, 0xFF202030, 1, 0, 0);
}

void draw_hud(void) {
    ui_rect(0, 0, VIEWW, HUDH, COL_HUD, 1, 0, 0);

    /* ── 临时调试行（查「方向键不生效」用）────────────────────
     * ── Temporary debug line (used to look into "the direction keys have no effect") ──
     * `帧` 在涨 = 定时器活着；`键`/`持` 有值 = 按键收得到。
     * the frame counter climbing = the timer is alive; the key / hold fields having values = key presses are being received.
     * 两行一起看，一次就能分清「定时器没跑」还是「按键没收」。查完删掉。
     * Read both lines together and you can tell "the timer is not running" from "the keys are not being received" in one go. Delete it once done.
     */
    ui_set_font(11, 0, COL_GOLD, VML_ANCHOR_LEFT);
    ui_text_cur(6, HUDH - 16, g_lang == 0 ? "帧" : "FPS");
    ui_text_cur(26, HUDH - 16, num_str(frame));
    ui_text_cur(96, HUDH - 16, g_lang == 0 ? "键" : "Key");
    ui_text_cur(116, HUDH - 16, num_str(lastkey));
    ui_text_cur(166, HUDH - 16, g_lang == 0 ? "持" : "Hold");
    /* "Hold"（4 个半角 ≈ 22px）比"持"（13px）宽，数值起点得让开，否则压在一起 */
    /* "Hold" (4 half-width chars ≈ 22px) is wider than the single-character Chinese label (13px), so the value's start must move over or the two collide. */
    ui_text_cur(g_lang == 0 ? 186 : 192, HUDH - 16, num_str(held));
    ui_text_cur(236, HUDH - 16, "px");
    ui_text_cur(262, HUDH - 16, num_str(px));

    ui_set_font(13, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(10, 6, g_lang == 0 ? "分数" : "Score");
    ui_text_cur(46, 6, num_str(score));
    ui_set_font(13, VML_FONT_BOLD, COL_GOLD, VML_ANCHOR_LEFT);
    ui_text_cur(VIEWW / 2 - 26, 6, g_lang == 0 ? "金币" : "Coins");
    ui_text_cur(VIEWW / 2 + 10, 6, num_str(coins));
    ui_set_font(13, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_LEFT);
    /* "Lives"（≈33px）比"命"（13px）宽得多 ⇒ 整组往左挪，别压到数字上（数字右边界不动）。*/
    /* "Lives" (≈33px) is far wider than the single-character Chinese label (13px) ⇒ shift the whole group left so it does not run into the number (the number's right edge stays put). */
    ui_text_cur(VIEWW - (g_lang == 0 ? 96 : 132), 6, g_lang == 0 ? "命" : "Lives");
    ui_text_cur(VIEWW - (g_lang == 0 ? 78 : 114), 6, num_str(lives));
    ui_set_font(11, 0, COL_TEXT, VML_ANCHOR_LEFT);
    ui_text_cur(VIEWW - 52, 8, num_str(timer_));
}

void draw_all(void) {
    draw_sky();
    draw_platforms();
    draw_coins();
    draw_goal();
    draw_foes();
    draw_player();
    draw_hud();

    if (state == 1) {
        ui_rect(0, VIEWH / 2 - 30, VIEWW, 60, 0xCC000000, 1, 0, 0);
        ui_set_font(20, VML_FONT_BOLD, COL_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(VIEWW / 2, VIEWH / 2 - 10, g_lang == 0 ? "已暂停" : "Paused");
        ui_set_font(12, 0, COL_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(VIEWW / 2, VIEWH / 2 + 14, g_lang == 0 ? "按 SELECT 继续" : "Press SELECT to resume");
    }
    if (state == 2) {
        ui_rect(0, VIEWH / 2 - 40, VIEWW, 80, 0xCC000000, 1, 0, 0);
        ui_set_font(20, VML_FONT_BOLD, COL_WARN, VML_ANCHOR_CENTER);
        ui_text_cur(VIEWW / 2, VIEWH / 2 - 18, g_lang == 0 ? "游戏结束" : "Game Over");
        ui_set_font(13, 0, COL_TEXT, VML_ANCHOR_CENTER);
        ui_text_cur(VIEWW / 2, VIEWH / 2 + 10, g_lang == 0 ? "按 START 重新开始" : "Press START to restart");
    }
    ui_present();
}

/* ── 按键 ───────────────────────────────────────────────── */
/* ── Keys ───────────────────────────────────────────────── */

int key_to_dir(int k) {
    if (k == VML_KEY_LEFT) return VML_KEY_LEFT;
    if (k == VML_KEY_RIGHT) return VML_KEY_RIGHT;
    return 0;
}

int is_jump_key(int k) {
    if (k == VML_KEY_UP) return 1;
    if (k == VML_KEY_PAD_A) return 1;
    if (k == VML_KEY_PAD_X) return 1;
    if (k == VML_KEY_SPACE) return 1;
    return 0;
}

void on_keydown(int k) {
    int d;
    lastkey = k;            /* 最先记 —— 放在早退之前，任何键都留痕 */
    /* Record it first — placed before the early returns so every key leaves a trace. */
    if (k == VML_KEY_ESCAPE) return;

    if (k == VML_KEY_SELECT) {
        if (state == 0) state = 1;
        else if (state == 1) state = 0;
        draw_all();
        return;
    }
    if (k == VML_KEY_ENTER) {
        reset_round();
        draw_all();
        return;
    }
    if (state != 0) return;

    d = key_to_dir(k);
    if (d != 0) {
        if (held != d) { holdTicks = 0; }   /* 换键即接管 */
        /* A new key takes over immediately. */
        held = d;
        return;
    }
    if (is_jump_key(k) != 0 && coyote > 0) {
        vy = JUMPV;
        onGround = 0;
        coyote = 0;                 /* 用掉就清零，防连跳 */
        /* Clear it once used, to prevent double jumps. */
        sfx_jump();
    }
}

void on_keyup(int k) {
    int d;
    d = key_to_dir(k);
    /* 只有"抬起的是当前按住的那个方向"才结束 —— 否则同时按两个键时，
     * Only end it when "the key released is the direction currently held" — otherwise, with two keys pressed at once,
     * 另一个键抬起会把还在按着的方向一起停掉。
     * releasing the other key would stop the direction that is still being held. */
    if (d != 0 && d == held) { held = 0; holdTicks = 0; }
}

/* ── 主循环 ─────────────────────────────────────────────── */
/* ── Main loop ──────────────────────────────────────────── */

int main(void) {
    int msg[4];
    int t;
    int sw;
    int sh;
    int gh;
    int byh;

    /* **先问可用绘图区，再开窗** —— 窗口宽高就是画布坐标空间，必须与排版同源。 */
    /* **ask for the available drawing area first, then open the window** — the window's width/height is the canvas coordinate space, so it must come from the same source as the layout. */
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) sw = 360;
    if (sh <= 0) sh = 620;

    g_lang = ui_get_language();
    ui_win_open(g_lang == 0 ? "超级玛丽" : "Mario", sw, sh);
    ui_keep_on(1);              /* 玩的时候别熄屏；退出时关掉 */
    /* Do not let the screen go dark while playing; turn it off again on exit. */

    VIEWW = sw;
    VIEWH = sh;
    HUDH = 44;              /* 两行：状态行 + 临时调试行 */
    /* Two lines: the status line + the temporary debug line. */

    /* 一格边长：竖向先按"地面以上留 15 格"算，再夹到横向别超过 14 格宽 */
    /* Tile side length: first compute it vertically as "leave 15 rows above the ground", then clamp it so that horizontally it stays within 14 tiles. */
    gh = sh - HUDH - 8;
    TILE = gh / (ROWS - 1);
    byh = sw / 14;
    if (byh < TILE) TILE = byh;
    if (TILE < 10) TILE = 10;

    pw = TILE - 6;
    ph = TILE + TILE / 3;
    if (pw < 6) pw = 6;

    state = 0;
    reset_round();
    draw_all();

    /* **必须开定时器，否则整个游戏一动不动** —— 所有逻辑都在 VML_MSG_TIMER 那一支里，
     * **the timer must be started, otherwise the whole game stands still** — all the logic lives in the VML_MSG_TIMER branch,
     * 没有它主循环只会干等按键消息（现象是"界面出来了但角色永远站着"）。
     * and without it the main loop just waits for key messages (the symptom is "the screen appears but the character never moves").
     * 这是本文件最容易漏的一行，tetris.c 用同一个节拍（40ms ⇒ 25fps）。
     * This is the line most easily forgotten in this file; tetris.c uses the same beat (40ms ⇒ 25fps).
     */
    ui_timer_set(TICK_MS, 0);

    /* ⚠ 中文这条整句写在同一行里：跨行拼接时第二段会落在没有 `g_lang == 0` 的行上
       ⚠ The Chinese sentence is kept on a single line: if it were wrapped, the second piece would land on a line without `g_lang == 0`
       （语言审计脚本按行认），而相邻字面量拼接本来就只是 C 的语法糖。
       (the language audit script works line by line), and adjacent literal concatenation is just C syntactic sugar anyway. */
    if (g_lang == 0) ui_dlg_msg("超级玛丽", "用屏幕下方的游戏按键操作：← → 跑、↑/A/X 跳。踩敌人得分，收金币，走到右边的旗杆通关。START 重开、SELECT 暂停。", VML_DLG_INFO);
    else             ui_dlg_msg("Mario", "Use the on-screen gamepad: left/right to run, up/A/X to jump. Stomp foes for score, grab coins, reach the flag to win. START restart, SELECT pause.", VML_DLG_INFO);

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;

        if (t == VML_MSG_TIMER) {
            tick_world();
            draw_all();
            continue;
        }
        if (t == VML_MSG_KEYUP) { on_keyup(msg[1]); continue; }
        if (t == VML_MSG_KEYDOWN) {
            if (msg[1] == VML_KEY_ESCAPE) break;
            on_keydown(msg[1]);
            continue;
        }
    }

    ui_keep_on(0);
    ui_sfx_panic();
    ui_win_close();
    return 0;
}
