// demo_ui.cpp —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
// demo_ui.cpp — **Layer 4: the newest UI interface** (`ui_*` / `Lib/c/waycoder_ui.h`)
//
// 与 `Examples/c/demo_ui.c` 同构：开 UI 窗口 → 查屏幕方向 → 按画布尺寸排版 → 画图元
// It has the same structure as `Examples/c/demo_ui.c`: open a UI window → query the screen orientation → lay out per canvas size → draw primitives
// → `ui_present()` → 消息循环（触摸 / 按键 / 定时器 / 转屏）→ **有界退出**。
// → `ui_present()` → message loop (touch / keys / timer / rotation) → **bounded exit**.
//
// ## 这一层与 BGI（第 3 层）的差别
// ## How this layer differs from BGI (Layer 3)
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w/h`），程序现排版。
//   · **No fixed resolution**: the host decides how big the canvas is (`ui_scr_w/h`), and the program lays out on the fly.
//   · **真彩**：`0xAARRGGBB`，不是 16 个索引色。
//   · **True color**: `0xAARRGGBB`, not 16 indexed colors.
//   · **有消息循环**：触摸/按键/定时器/窗口被关/被转屏都从 `ui_wait` 出来。
//   · **It has a message loop**: touch/key/timer/window-closed/rotation all come out of `ui_wait`.
//
// ## 为什么这一层不走 `graphics.h`
// ## Why this layer does not go through `graphics.h`
//
// `graphics.h` 是给**老程序**的兼容垫层（BGI 那套写死的坐标系）。新写的程序直接用
// `graphics.h` is a compatibility shim for **old programs** (BGI's hardcoded coordinate system). Newly written programs go
// `waycoder_ui.h` —— 尤其是"要把版面跟着屏幕尺寸排"的程序，BGI 那套固定坐标反而碍事。
// straight to `waycoder_ui.h` — especially programs that have to lay their layout out following the screen size, where BGI's fixed coordinates get in the way.
//
// ## ⚠ C++ 前端的两条坑（本 demo 的写法就是为绕开它们）
// ## ⚠ Two pitfalls of the C++ frontend (this demo is written the way it is to avoid them)
//
//   · `cout << <char* 变量>` 打的是**地址**（详见 `demo_std.cpp` 的文件头）⇒
//   · `cout << <char* variable>` prints the **address** (see the file header of `demo_std.cpp` for details) ⇒
//     这里要印字符串变量一律 `printf("%s", …)`。
//     here, printing a string variable always goes through `printf("%s", …)`.
//   · 类的方法**不生成函数体**（`未定义的函数 'method_…'`）⇒ 全部写成自由函数 +
//   · Class methods **do not get a generated body** (an "undefined function 'method_…'" error) ⇒ write everything as free functions +
//     一个文件级数组（与 `cpp/snake.cpp` 同一路子）。
//     one file-level array (the same approach as `cpp/snake.cpp`).
//
// 跑法：手机 `vml run examples/cpp/demo_ui.cpp`；
// How to run: on the phone `vml run examples/cpp/demo_ui.cpp`;
//       桌面 `vmlcli Examples/cpp/demo_ui.cpp --frames /tmp/fr`
//       on the desktop `vmlcli Examples/cpp/demo_ui.cpp --frames /tmp/fr`

#include <waycoder_ui.h>
#include <stdio.h>

#define MAX_FRAMES 60          // 到点自己停：60 拍 × 60ms ≈ 3.6 秒
// stops by itself when it is reached: 60 ticks × 60ms ≈ 3.6 seconds

// 版面变量：每次重排都按当前画布尺寸算（**不写死**）
// Layout variables: recomputed from the current canvas size on every re-layout (**never hardcoded**)
static int SW;
static int SH;
static int GY;

// 计数：证明这些事件真的到过
// Counters: proof that these events really did arrive
static int g_frames;
static int g_keys;
static int g_touches;
static int g_orient;

// 最近一次触摸的位置（-1 = 还没点过）
// Position of the most recent touch (-1 = not touched yet)
static int g_tx = -1;
static int g_ty = -1;

// 界面语言：开局查一次（`ui_get_language` 是 syscall，别每帧调）
// UI language: queried once at startup (`ui_get_language` is a syscall, do not call it every frame)
static int gLang;

// 数字 → 字符串。不用 std::string（C++ 前端整体不可用），也不用 sprintf（各前端形态不一），
// Number → string. No std::string (unusable across the C++ frontend), and no sprintf (its form differs per frontend),
// 自己按位拆最省事 —— 与仓库里其它语言的例程同一做法。
// so splitting the digits out yourself is the least hassle — the same approach as the examples in the other languages in this repo.
static char g_num[12];

static char *numstr(int v)
{
    int n = 0;
    int m = 0;
    char tmp[12];
    if (v < 0) { g_num[0] = '-'; v = -v; n = 1; }
    if (v == 0) { g_num[n] = '0'; g_num[n + 1] = 0; return g_num; }
    while (v > 0) { tmp[m] = (char)('0' + v % 10); v = v / 10; m = m + 1; }
    while (m > 0) { m = m - 1; g_num[n] = tmp[m]; n = n + 1; }
    g_num[n] = 0;
    return g_num;
}

static void relayout()
{
    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;
    GY = 14;
}

static void draw()
{
    int cx = SW / 2;
    int pad = SW / 16;
    int i;
    int w;

    ui_clear(0xFF101018);

    // ── 标题 + 方向 + 画布尺寸 ──
    // ── Title + orientation + canvas size ──
    ui_text_styled(cx, GY, gLang == 0 ? "UI 接口 / demo_ui.cpp" : "UI interface / demo_ui.cpp", 0xFFE8E8F0, 16,
                   VML_ANCHOR_CENTER, VML_FONT_BOLD);
    if (g_orient == VML_ORIENT_LANDSCAPE) {
        ui_text(cx, GY + 26, gLang == 0 ? "屏幕方向 = 横屏 (LANDSCAPE)" : "Orientation = LANDSCAPE", 0xFF51E86E, 13, VML_ANCHOR_CENTER);
    } else {
        ui_text(cx, GY + 26, gLang == 0 ? "屏幕方向 = 竖屏 (PORTRAIT)" : "Orientation = PORTRAIT", 0xFF51E86E, 13, VML_ANCHOR_CENTER);
    }
    ui_text(cx, GY + 46, gLang == 0 ? "画布按宿主给的尺寸现排" : "Canvas laid out from the host's size", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    // ── 跟随尺寸的方框：旋转后跟着变宽变矮，这就是"不写死坐标"的证明 ──
    // ── A box that follows the size: after rotation it gets wider and shorter, and that is the proof of "no hardcoded coordinates" ──
    ui_rect(pad, GY + 70, SW - pad * 2, 90, 0xFF1A1A24, 1, 0, 10);
    ui_rect(pad + 6, GY + 76, SW - pad * 2 - 12, 30, 0xFF4A90D9, 1, 0, 6);
    ui_text(pad + 16, GY + 84, gLang == 0 ? "rect / round-rect（随屏宽伸缩）" : "rect / round-rect (stretches with the width)", 0xFF101018, 12,
            VML_ANCHOR_LEFT);

    // ── 圆 / 椭圆 / 直线 ──
    // ── Circle / ellipse / line ──
    ui_circle(cx - SW / 6, GY + 140, SW / 12, 0xFFE06C50, 1, 0);
    ui_ellipse(cx + SW / 6, GY + 140, SW / 9, SW / 18, 0xFFD9B44A, 1, 0);
    ui_line(pad, GY + 172, SW - pad, GY + 172, 0xFF50C878, 3);

    // ── 8 格真彩色带 ──
    // ── The 8-cell true-color band ──
    w = (SW - pad * 2) / 8;
    for (i = 0; i < 8; i++) {
        if ((i & 1) != 0) {
            ui_rect(pad + i * w, GY + 186, w - 2, 16, 0xFF4A90D9, 1, 0, 2);
        } else {
            ui_rect(pad + i * w, GY + 186, w - 2, 16, 0xFFE06C50, 1, 0, 2);
        }
    }
    ui_text(cx, GY + 210, gLang == 0 ? "真彩 0xAARRGGBB（不是索引色）" : "True color 0xAARRGGBB (not indexed)", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    // ── 事件计数 ──
    // ── Event counters ──
    ui_text(pad, GY + 236, gLang == 0 ? "帧" : "Frames", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 22, GY + 236, numstr(g_frames), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 256, gLang == 0 ? "按键" : "Keys", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 256, numstr(g_keys), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);
    ui_text(pad, GY + 276, gLang == 0 ? "触摸" : "Touches", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(pad + 44, GY + 276, numstr(g_touches), 0xFFFFFFFF, 13, VML_ANCHOR_LEFT);

    // ── 触摸标记：点哪儿就在哪儿留个圈 ──
    // ── Touch marks: wherever you tap, a circle is left there ──
    if (g_tx >= 0) {
        ui_circle(g_tx, g_ty, 18, 0xFFFFE060, 0, 2);
        ui_circle(g_tx, g_ty, 4, 0xFFFFE060, 1, 0);
        ui_text(cx, SH - 44, gLang == 0 ? "触摸坐标已经用上了" : "Touch coordinates are in use", 0xFFFFE060, 12, VML_ANCHOR_CENTER);
    } else {
        ui_text(cx, SH - 44, gLang == 0 ? "点一下屏幕 / 按任意键退出" : "Tap the screen / press any key to exit", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);
    }

    ui_text(cx, SH - 24, gLang == 0 ? "退出：按任意键或点任意处（或等 N 帧到点）" : "Exit: any key or any tap (or wait out the frame count)",
            0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_present();
}

int main()
{
    int msg[4];
    int tid;
    int done = 0;
    int t;

    // ── ① 开窗**之前**就问方向 ──
    // ── ① Ask for the orientation **before** opening the window ──
    g_orient = ui_orientation();
    gLang = ui_get_language();

    // 开窗：声明"支持旋转 + 要手柄"（转屏时宿主会把新坐标空间整个给过来）
    // Open the window: declare "supports rotation + wants the gamepad" (on rotation the host hands over the whole new coordinate space)
    ui_win_open_ex("demo_ui.cpp",
                   ui_scr_w() > 0 ? ui_scr_w() : 360,
                   ui_scr_h() > 0 ? ui_scr_h() : 620,
                   VML_WIN_ROTATABLE, VML_WIN_NEED_GAMEPAD);
    relayout();
    draw();

    // ── ⑤ 消息循环：定时器驱动重画，**有界退出** ──
    // ── ⑤ Message loop: the timer drives the redraws, **bounded exit** ──
    tid = ui_timer_set(60, 1);
    while (done == 0 && ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == VML_MSG_NONE) continue;

        if (t == VML_MSG_TIMER) {
            g_frames = g_frames + 1;
            draw();
            if (g_frames >= MAX_FRAMES) done = 1;      // ← 有界：到点自己停
            // ← bounded: stops by itself when the frame budget runs out
            continue;
        }

        if (t == VML_MSG_KEYDOWN) {
            g_keys = g_keys + 1;
            draw();
            done = 1;                                  // ← 有界：收到键就退
            // ← bounded: exits as soon as a key arrives
            continue;
        }

        if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_MOUSEDOWN) {
            g_touches = g_touches + 1;
            g_tx = msg[1];                             // msg[1]=x msg[2]=y
            g_ty = msg[2];
            draw();
            done = 1;                                  // ← 有界：点到就退
            // ← bounded: exits as soon as a tap arrives
            continue;
        }

        if (t == VML_MSG_WINDOWORIENT) {
            g_orient = msg[1];                         // A = 新方向
            // A = the new orientation
            continue;
        }

        if (t == VML_MSG_WINDOWRESIZE) {
            relayout();                                // 换坐标系了 ⇒ 重排 + 重画
            // the coordinate system changed ⇒ re-layout + redraw
            draw();
            continue;
        }

        if (t == VML_MSG_WINDOWCLOSE) break;           // 用户把窗口关了
        // the user closed the window
    }

    ui_timer_kill(tid);
    ui_win_close();

    // 给无头验证留一行确定性判据（图形只能肉眼看，这几个数能自动比）
    // Leave one line of deterministic criteria for headless verification (graphics can only be eyeballed; these numbers can be compared automatically)
    if (g_orient == VML_ORIENT_LANDSCAPE) {
        printf("demo_ui: orient=LANDSCAPE\n");
    } else {
        printf("demo_ui: orient=PORTRAIT\n");
    }
    printf("demo_ui: canvas=%dx%d\n", SW, SH);
    // ⚠ 三次 `numstr` **必须分开 printf**：它返回的是**同一个静态缓冲区**，
    // ⚠ The three `numstr` calls **must be printed with separate printf calls**: it returns **the same static buffer**,
    //   三个一起当实参传进去会全部指向那块内存、打印出同一个值（经典别名坑）。
    //   so passing all three together as arguments makes them all point at that memory and print the same value (the classic aliasing trap).
    printf("demo_ui: frames=%s\n", numstr(g_frames));
    printf("demo_ui: keys=%s\n", numstr(g_keys));
    printf("demo_ui: touches=%s\n", numstr(g_touches));
    printf("demo_ui: done\n");
    return 0;
}
