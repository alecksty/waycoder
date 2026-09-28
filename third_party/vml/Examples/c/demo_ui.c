/* demo_ui.c —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
 * demo_ui.c -- **Layer 4: the newest UI interface** (`ui_*` / `Lib/c/waycoder_ui.h`)
 *
 * 这一层和第 3 层（BGI）正好相反：
 * This layer is the exact opposite of layer 3 (BGI):
 *
 *   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
 *   · **No fixed resolution**. The host decides the canvas size (`ui_scr_w()` / `ui_scr_h()`),
 *     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
 *     and the program lays out against whatever it gets -- on mobile the gamepad area can collapse and rotation changes it.
 *   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。`0xFFE06C50` 就是那个橙。
 *   · **Colors are true color** (`0xAARRGGBB`), not 16 indexed colors. `0xFFE06C50` is that orange.
 *   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait` 出来，
 *   · **There is a message loop**. Touches, keys, timers, window close / rotation all come out of `ui_wait`,
 *     程序按 `msg[0]` 分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
 *     and the program dispatches on `msg[0]` -- this is the divide between "an interactive program" and "draws once and dies".
 *
 * ## 这个 demo 演示什么
 * ## What this demo shows
 *
 *   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定"上下排还是左右排"。
 *   ① **Ask the screen orientation before opening the window** (`ui_orientation()`) -- it decides "stack vertically or side by side".
 *      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
 *      Orientation is a property of the **device**; do not infer it from `ui_scr_w() > ui_scr_h()` (that is the drawing area,
 *      手柄收起/展开会变）。
 *      which changes when the gamepad collapses/expands).
 *   ② 开窗（`ui_win_open_ex` 声明"支持旋转 + 要手柄"）→ 拿画布尺寸 → 按尺寸排版。
 *   ② Open the window (`ui_win_open_ex` declares "rotatable + want gamepad") -> get the canvas size -> lay out to it.
 *   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，**每个位置都由 sw/sh 算出来**。
 *   ③ Draw primitives: rect / rounded rect / circle / ellipse / line / text, **every position computed from sw/sh**.
 *   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号（不出图判据也认它）。
 *   ④ `ui_present()` hands over the frame -- it is the only "this frame is finished" signal (the no-render check honors it too).
 *   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
 *   ⑤ **Message loop**: a timer drives redraws, keys / touches respond, and it **exits within a bound**
 *      （收够 N 帧 或 按任意键 / 点任意处）。
 *      (after N frames, or on any key / any tap).
 *
 * ## 退出是有界的，这一点是刻意的
 * ## The bounded exit is deliberate
 *
 * 「按 N 帧或收到键就退出」——没有输入也要能自己停下来。所以主循环数**定时器拍数**，
 * "Exit after N frames or on a key" -- it must stop by itself even with no input. So the main loop counts **timer ticks**
 * 到 `MAX_FRAMES` 就收尾。手机上按任意处 / 任意键可以提前退出。
 * and wraps up at `MAX_FRAMES`. On mobile, any tap / any key exits earlier.
 *
 * 跑法：
 * How to run:
 *   手机    vml run examples/c/demo_ui.c
 *   mobile  vml run examples/c/demo_ui.c
 *   桌面    vmlcli Examples/c/demo_ui.c --frames /tmp/fr        （每帧一张 PNG）
 *   desktop vmlcli Examples/c/demo_ui.c --frames /tmp/fr        (one PNG per frame)
 * 桌面喂输入：加 `--input 脚本`，一行一条，如 `touchdown 100 200` / `keydown 27`。
 * To feed input on the desktop, add `--input <script>` with one command per line, e.g. `touchdown 100 200` / `keydown 27`.
 */

#include <waycoder_ui.h>
#include <stdio.h>

#define MAX_FRAMES 60          /* 到点自己停：60 拍 × 60ms ≈ 3.6 秒 */
/* stops by itself at the limit: 60 ticks x 60ms ~ 3.6 seconds */

/* 版面变量：每次重排都由当前画布尺寸算出来（**不写死**，见文件头第 ① ② 条）*/
/* Layout variables: every relayout computes them from the current canvas size (**never hard-coded**, see items 1-2 above) */
static int SW;                 /* 画布宽 */
/* canvas width */
static int SH;                 /* 画布高 */
/* canvas height */
static int GY;                 /* 标题行基线 */
/* title row baseline */

/* 计数：用来证明这些事件真的到过 */
/* Counters: used to prove these events really arrived */
static int g_frames;
static int g_keys;
static int g_touches;
static int g_orient;
static int g_lang;             /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调）*/
/* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

/* 最近一次触摸/点击的位置（没点过就是 -1）*/
/* Position of the most recent touch/click (-1 if never touched) */
static int g_tx = -1;
static int g_ty = -1;

/* 数字 → 字符串。**不用 sprintf**：这一层要能在任意前端形态下跑，
 * number -> string. **No sprintf**: this layer must run under any frontend,
 * 自己按位拆最省事（各语言的例程都这么做）。
 * and splitting digits by hand is the simplest way (every language sample does it). */
static char *numstr(int v)
{
    static char buf[12];
    int n = 0;
    if (v < 0) { buf[0] = '-'; v = -v; n = 1; }
    if (v == 0) { buf[n] = '0'; buf[n + 1] = 0; return buf; }
    {
        char tmp[12];
        int m = 0;
        while (v > 0) { tmp[m] = (char)('0' + v % 10); v = v / 10; m = m + 1; }
        while (m > 0) { m = m - 1; buf[n] = tmp[m]; n = n + 1; }
    }
    buf[n] = 0;
    return buf;
}

static void relayout(void)
{
    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;
    GY = 14;
}

static void draw(void)
{
    int cx = SW / 2;
    int pad = SW / 16;
    int i;

    ui_clear(0xFF101018);                                   /* 背景 */
    /* background */

    /* ── 标题 + 屏宽高 + 方向 ── */
    /* ── Title + screen width/height + orientation ── */
    ui_text_styled(cx, GY, g_lang == 0 ? "UI 接口 / demo_ui.c" : "UI interface / demo_ui.c", 0xFFE8E8F0, 16,
                   VML_ANCHOR_CENTER, VML_FONT_BOLD);
    ui_text(cx, GY + 26, g_orient == VML_ORIENT_LANDSCAPE
                          ? (g_lang == 0 ? "屏幕方向 = 横屏 (LANDSCAPE)" : "Orientation = LANDSCAPE")
                          : (g_lang == 0 ? "屏幕方向 = 竖屏 (PORTRAIT)" : "Orientation = PORTRAIT"),
            0xFF51E86E, 13, VML_ANCHOR_CENTER);
    ui_text(cx, GY + 46, g_lang == 0 ? "画布按宿主给的尺寸现排" : "Canvas laid out from the host size", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 一个跟随尺寸的方框（旋转后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）── */
    /* ── A box that follows the size (after rotation it gets wider and shorter -- proof that no coordinate is hard-coded) ── */
    ui_rect(pad, GY + 70, SW - pad * 2, 90, 0xFF1A1A24, 1, 0, 10);
    ui_rect(pad + 6, GY + 76, SW - pad * 2 - 12, 30, 0xFF4A90D9, 1, 0, 6);
    ui_text(pad + 16, GY + 84, g_lang == 0 ? "rect / 圆角矩形（随屏宽伸缩）" : "rect / rounded rect (follows width)", 0xFF101018, 12,
            VML_ANCHOR_LEFT);

    /* ── 圆 / 椭圆 / 直线：三个基本形 ── */
    /* ── Circle / ellipse / line: three basic shapes ── */
    ui_circle(cx - SW / 6, GY + 140, SW / 12, 0xFFE06C50, 1, 0);
    ui_ellipse(cx + SW / 6, GY + 140, SW / 9, SW / 18, 0xFFD9B44A, 1, 0);
    ui_line(pad, GY + 172, SW - pad, GY + 172, 0xFF50C878, 3);

    /* ── 一条 8 格色带（真彩：这些都是 RGB，不是调色板索引）── */
    /* ── An 8-cell color band (true color: these are RGB, not palette indexes) ── */
    for (i = 0; i < 8; i++) {
        int w = (SW - pad * 2) / 8;
        ui_rect(pad + i * w, GY + 186, w - 2, 16,
                (i & 1) ? 0xFF4A90D9 : 0xFFE06C50, 1, 0, 2);
    }
    ui_text(cx, GY + 210, g_lang == 0 ? "真彩 0xAARRGGBB（不是索引色）" : "true color 0xAARRGGBB (not indexed)", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 事件计数：按模拟器和真机都看得出"消息到了没有" ── */
    /* ── Event counters: on both emulator and device you can see whether messages arrived ── */
    ui_text(pad, GY + 236, g_lang == 0 ? "帧" : "frames", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 22, GY + 236, numstr(g_frames), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 256, g_lang == 0 ? "按键" : "keys", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 256, numstr(g_keys), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 276, g_lang == 0 ? "触摸" : "touches", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 276, numstr(g_touches), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);

    /* ── 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）── */
    /* ── Touch marker: leave a circle wherever the tap landed (proof that the coordinates work) ── */
    if (g_tx >= 0) {
        ui_circle(g_tx, g_ty, 18, 0xFFFFE060, 0, 2);
        ui_circle(g_tx, g_ty, 4, 0xFFFFE060, 1, 0);
        ui_text(cx, SH - 44, g_lang == 0 ? "触摸坐标已经用上了" : "touch coordinates in use", 0xFFFFE060, 12, VML_ANCHOR_CENTER);
    } else {
        ui_text(cx, SH - 44, g_lang == 0 ? "点一下屏幕 / 按任意键退出" : "Tap anywhere / any key to exit", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);
    }

    ui_text(cx, SH - 24, g_lang == 0 ? "退出：按任意键或点任意处（或等 N 帧到点）" : "Exit: any key or any tap (or wait for N frames)",
            0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_present();
}

int main(void)
{
    int msg[4];
    int tid;
    int done = 0;

    /* ── ① 开窗**之前**就问方向：程序据此决定排版 ── */
    /* ── 1. Ask the orientation **before** opening the window: the program lays out from it ── */
    g_orient = ui_orientation();
    g_lang = ui_get_language();

    /* 开窗：声明"支持旋转 + 要手柄"。
     * Open the window: declare "rotatable + want gamepad".
     * 支持旋转 = 转屏时宿主会把**新的坐标空间**整个给过来（发 WINDOWORIENT +
     * Rotatable means that on rotation the host hands over the **whole new coordinate space** (sending WINDOWORIENT +
     * WINDOWRESIZE），所以下面的排版必须按当前尺寸算、并且收到消息后重排。
     * WINDOWRESIZE), so the layout below must be computed from the current size and redone when those messages arrive. */
    ui_win_open_ex("demo_ui.c", ui_scr_w() > 0 ? ui_scr_w() : 360,
                                  ui_scr_h() > 0 ? ui_scr_h() : 620,
                   VML_WIN_ROTATABLE, VML_WIN_NEED_GAMEPAD);
    relayout();
    draw();

    /* ── ⑤ 消息循环：定时器驱动重画，有界退出 ── */
    /* ── 5. Message loop: the timer drives redraws, the exit is bounded ── */
    tid = ui_timer_set(60, 1);
    while (!done && ui_win_closed() == 0) {
        int t = ui_wait(msg, 0);          /* 阻塞等一条（桌面最多等 --timeout 秒）*/
        /* block for one message (on the desktop it waits at most --timeout seconds) */
        if (t == VML_MSG_NONE) continue;

        if (t == VML_MSG_TIMER) {
            g_frames = g_frames + 1;
            draw();
            if (g_frames >= MAX_FRAMES) done = 1;    /* ← 有界：到点自己停 */
            /* <- bounded: stops by itself when the limit is reached */
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            g_keys = g_keys + 1;
            draw();
            done = 1;                                 /* ← 有界：收到键就退 */
            /* <- bounded: exits on a key */
            continue;
        }

        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            g_touches = g_touches + 1;
            g_tx = msg[1];                            /* msg[1]=x msg[2]=y */
            g_ty = msg[2];
            draw();
            done = 1;                                 /* ← 有界：点到就退 */
            /* <- bounded: exits on a tap */
            continue;
        }

        if (t == VML_MSG_WINDOWORIENT) {
            g_orient = msg[1];                        /* A = 新方向 */
            /* A = the new orientation */
            continue;
        }

        if (t == VML_MSG_WINDOWRESIZE) {
            /* 宿主要求换坐标系（声明了 ROTATABLE 才会来）。重新问尺寸、重排版。 */
            /* The host asks for a new coordinate space (only if ROTATABLE was declared). Re-query the size, re-layout. */
            relayout();
            draw();
            continue;
        }

        if (t == VML_MSG_WINDOWCLOSE) break;          /* 用户把窗口关了 */
        /* the user closed the window */
    }

    /* ── 收尾：定时器要杀，屏幕常亮要还回去（开了才要还）── */
    /* ── Wrap-up: kill the timer and give back the keep-screen-on flag (only if it was taken) ── */
    ui_timer_kill(tid);
    ui_win_close();

    /* 给无头验证留一行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）。 */
    /* Leave a line of **deterministic** output for headless verification (the graphics need eyes, but these numbers can be compared automatically). */
    printf("demo_ui: orient=%s\n", g_orient == VML_ORIENT_LANDSCAPE ? "LANDSCAPE" : "PORTRAIT");
    printf("demo_ui: canvas=%dx%d\n", SW, SH);
    printf("demo_ui: frames=%d keys=%d touches=%d\n", g_frames, g_keys, g_touches);
    printf("demo_ui: done\n");
    return 0;
}
