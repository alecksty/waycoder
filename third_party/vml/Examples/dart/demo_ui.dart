// demo_ui.dart —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
// demo_ui.dart — **Layer 4: the newest UI interface** (`ui_*` / `Lib/c/waycoder_ui.h`)
//
// 与第 3 层（BGI）正好相反：
// Exactly the opposite of layer 3 (BGI):
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），程序现排版。
//   · **No fixed resolution**: how big the canvas is comes from the host (`ui_scr_w()` / `ui_scr_h()`), and the program lays out on the fly.
//   · **真彩 0xAARRGGBB**，不是 16 个索引色。
//   · **True color 0xAARRGGBB**, not 16 indexed colors.
//   · **有消息循环**：触摸 / 按键 / 定时器 / 窗口被关 / 被转屏都从消息里出来。
//   · **There is a message loop**: touch / key / timer / window closed / rotated all arrive as messages.
//
// ## ⚠ 三条写法约束，全部是实测量出来的（不是风格）
// ## ⚠ Three style constraints, all measured (not style)
//
// ### ① 状态与绘制**全内联在 `main` 里** —— 不用文件级变量、不抽 `draw()` 函数
// ### ① State and drawing are **all inlined in `main`** — no file-level variables, no `draw()` helper function
//
// 先按"顶层 `List<int> A` 放状态 + `draw()` 函数读它"写的一版，**跑起来是错的**：
// A first version written as "a top-level `List<int> A` holds the state + a `draw()` function reads it" **ran wrong**:
// 61 帧都出了，但 `demo_ui: canvas=0x0  frames=0`，画面 99% 是纯背景
// all 61 frames came out, but `demo_ui: canvas=0x0  frames=0` and the picture was 99% plain background
//（`draw()` 里读到的 `A[0]/A[1]` 是 0 ⇒ 排版全塌）。
// (the `A[0]/A[1]` read inside `draw()` were 0 ⇒ the whole layout collapsed).
// `catch.dart` 的文件头早就记过这条：「**状态与逻辑全内联在 `main` 里**：
// The header of `catch.dart` recorded this long ago: "**all state and logic is inlined in `main`**:
// 文件级可变状态在 Kotlin / Swift / Go 那几门上都出过问题，这里不冒这个险」。
// file-level mutable state has gone wrong on Kotlin / Swift / Go, and this doesn't take that risk".
// 本文件照它写 —— 每帧重算版面、绘制直接写在循环里。
// This file follows it — the layout is recomputed each frame and drawing is written directly in the loop.
//
// ### ② 消息用**标量**版读，不把数组传给库函数
// ### ② Read messages with the **scalar** version; don't pass arrays to library functions
//
// Dart 的 `List<int>` 传给库里 `int*` 形参时**指针落在数据起点前 4 字节**（"长度头"）上。
// When a Dart `List<int>` is passed to a library `int*` parameter the **pointer lands 4 bytes before the data start** (the "length header").
// 实测：`ui_wait(msg,…)` 里宿主写的 `msg=(类型,A,B,时间戳)`，Dart 这边读到
// Measured: the `msg=(type,A,B,timestamp)` the host writes in `ui_wait(msg,…)` is read on the Dart side as
// `(A,B,时间戳,旧值)`（整体错位一格）。所以只用 `ui_wait_msg` / `ui_msg_a` / `ui_msg_b`
// `(A,B,timestamp,old value)` (shifted by one slot overall). So only `ui_wait_msg` / `ui_msg_a` / `ui_msg_b` are used
// —— `waycoder_ui.h` 里那组"不碰指针的消息读取（非 C 语言用）"正是为这个准备的。
// —— That set of "pointer-free message readers (for non-C languages)" in `waycoder_ui.h` exists precisely for this.
// 同理 `ui_polygon(int* pts, …)` 这类收数组的图元在 Dart 里不能用，本 demo 不碰。
// Likewise array-taking primitives such as `ui_polygon(int* pts, …)` cannot be used from Dart, and this demo does not touch them.
//
// ### ③ `external` 声明 + 没有 `println`
// ### ③ `external` declarations + no `println`
//
// 调库函数**必须 `external` 声明**；`println` 编出来的 `func_println` 不在链接进来的
// Library calls **must be declared `external`**; the `func_println` that `println` compiles to is not in the linked
// 库里（见 demo_std.dart 的文件头）⇒ 换行用 `print("...\n")`，数值用库里的 `println_int`。
// libraries (see the header of demo_std.dart) ⇒ for newlines use `print("...\n")`, for numbers the library's `println_int`.
//
// ## 退出是有界的
// ## Exit is bounded
//
// 主循环数**定时器拍数**，到 60 拍自己停；按任意键 / 点任意处也能提前退。
// The main loop counts **timer ticks** and stops itself at 60; any keypress / any tap also exits early.
//
// 跑法：手机 `vml run examples/dart/demo_ui.dart`；
// How to run: on the phone `vml run examples/dart/demo_ui.dart`;
//       桌面 `vmlcli Examples/dart/demo_ui.dart --frames /tmp/fr`
//       on the desktop `vmlcli Examples/dart/demo_ui.dart --frames /tmp/fr`

external int  ui_scr_w();
external int  ui_scr_h();
external int  ui_orientation();
external int  ui_win_open_ex(String t, int w, int h, int rotatable, int gamepad);
external int  ui_win_close();
external int  ui_win_closed();
external void ui_clear(int c);
external void ui_rect(int x, int y, int w, int h, int c, int fill, int lw, int r);
external void ui_circle(int cx, int cy, int r, int c, int fill, int lw);
external void ui_ellipse(int cx, int cy, int rx, int ry, int c, int fill, int lw);
external void ui_line(int x1, int y1, int x2, int y2, int c, int lw);
external void ui_text(int x, int y, String s, int c, int size, int anchor);
external void ui_text_styled(int x, int y, String s, int c, int size, int anchor, int style);
external void ui_present();
external int  ui_wait_msg(int timeout_ms);
external int  ui_msg_a();
external int  ui_msg_b();
external int  ui_timer_set(int interval_ms, int tag);
external void ui_timer_kill(int id);
external void print_str(String s);
external void print_int(int n);
external void println_int(int n);
external void println_str(String s);
external int  ui_get_language();

// 消息类型 / 锚点 / 方向 / 字体样式（源：Lib/c/waycoder_ui.h）
//   MSG_NONE=0 KEYDOWN=1 TIMER=9 TOUCHDOWN=6 MOUSEDOWN=4
//   WINDOWCLOSE=10 WINDOWRESIZE=11 WINDOWORIENT=12
//   ANCHOR_LEFT=0 CENTER=1 FONT_BOLD=1 ORIENT_LANDSCAPE=1
//   WIN_ROTATABLE=1 WIN_NEED_GAMEPAD=1        MAX_FRAMES=60
//
// 颜色（`0xAARRGGBB` 的十进制负数形式）：
//   BG=0xFF101018=-15724520    PANEL=0xFF1A1A24=-15066588
//   BLUE=0xFF4A90D9=-11890471  ORANGE=0xFFE06C50=-2069424
//   YELLOW=0xFFD9B44A=-2509750 GREEN=0xFF50C878=-11483016
//   TITLE=0xFFE8E8F0=-1513232  ACCENT=0xFF51E86E=-11409298
//   MUTED=0xFF9AA0B0=-6643536

void main() {
  // ── 全部状态都是 main 的局部变量（见文件头 ①）──
  // ── All state is a local variable of main (see header ①) ──
  int sw = 0;
  int sh = 0;
  int gy = 14;
  int frames = 0;
  int keys = 0;
  int touches = 0;
  int orient = 0;
  int tx = -1;
  int ty = -1;
  int done = 0;
  int i = 0;
  int w = 0;
  int h = 0;
  int cx = 0;
  int pad = 0;
  int cw = 0;

  // ── ① 开窗**之前**就问屏幕方向 ──
  // ── ① Ask the screen orientation **before** opening the window ──
  orient = ui_orientation();

  // 界面语言：开局查一次（`ui_get_language` 是 syscall，别每帧调）
  // UI language: queried once at start (`ui_get_language` is a syscall, not once per frame)
  int lang = ui_get_language();

  w = ui_scr_w();
  h = ui_scr_h();
  if (w <= 0) { w = 360; }
  if (h <= 0) { h = 620; }
  ui_win_open_ex("demo_ui.dart", w, h, 1, 1);   // 支持旋转 + 要手柄
  // rotation supported + gamepad wanted
  int tid = ui_timer_set(60, 1);

  // ── 消息循环：定时器驱动重画，**有界退出** ──
  // ── Message loop: the timer drives redraws, **exit is bounded** ──
  while (done == 0 && ui_win_closed() == 0) {
    // 每帧重新问一次画布尺寸（转屏 / 手柄收放都会让它变）—— 这就是"不写死坐标"
    // Ask for the canvas size again every frame (rotation / collapsing the gamepad changes it) — this is what "no hardcoded coordinates" means
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) { sw = 360; }
    if (sh <= 0) { sh = 620; }
    cx = sw ~/ 2;
    pad = sw ~/ 16;

    ui_clear(-15724520);

    ui_text_styled(cx, gy, lang == 0 ? "UI 接口 / demo_ui.dart" : "UI interface / demo_ui.dart", -1513232, 16, 1, 1);
    if (orient == 1) {
      ui_text(cx, gy + 26, lang == 0 ? "屏幕方向 = 横屏 (LANDSCAPE)" : "Orientation = LANDSCAPE", -11409298, 13, 1);
    } else {
      ui_text(cx, gy + 26, lang == 0 ? "屏幕方向 = 竖屏 (PORTRAIT)" : "Orientation = PORTRAIT", -11409298, 13, 1);
    }
    ui_text(cx, gy + 46, lang == 0 ? "画布按宿主给的尺寸现排（旋转后跟着变）" : "Canvas laid out from the host size (follows rotation)", -6643536, 12, 1);

    // 跟随尺寸的方框：旋转后跟着变宽变矮
    // A size-following box: on rotation it follows and gets wider and shorter
    ui_rect(pad, gy + 70, sw - pad * 2, 90, -15066588, 1, 0, 10);
    ui_rect(pad + 6, gy + 76, sw - pad * 2 - 12, 30, -11890471, 1, 0, 6);
    ui_text(pad + 16, gy + 84, lang == 0 ? "rect / round-rect（随屏宽伸缩）" : "rect / round-rect (follows width)", -15724520, 12, 0);

    ui_circle(cx - sw ~/ 6, gy + 140, sw ~/ 12, -2069424, 1, 0);
    ui_ellipse(cx + sw ~/ 6, gy + 140, sw ~/ 9, sw ~/ 18, -2509750, 1, 0);
    ui_line(pad, gy + 172, sw - pad, gy + 172, -11483016, 3);

    // 8 格真彩色带
    // 8-cell true-color bar
    i = 0;
    while (i < 8) {
      cw = (sw - pad * 2) ~/ 8;
      if (i % 2 == 0) {
        ui_rect(pad + i * cw, gy + 186, cw - 2, 16, -2069424, 1, 0, 2);
      } else {
        ui_rect(pad + i * cw, gy + 186, cw - 2, 16, -11890471, 1, 0, 2);
      }
      i = i + 1;
    }
    ui_text(cx, gy + 210, lang == 0 ? "真彩 0xAARRGGBB（不是索引色）" : "true color 0xAARRGGBB (not indexed)", -6643536, 12, 1);

    // 进度用**条形长度**表达（Dart 前端没有 int→string，见 demo_std.dart）
    // Progress is expressed as a **bar length** (the Dart frontend has no int→string, see demo_std.dart)
    ui_text(pad, gy + 236, lang == 0 ? "已跑帧数（条形）" : "frames drawn (as a bar)", -6643536, 12, 0);
    ui_rect(pad, gy + 254, sw - pad * 2, 14, -15066588, 1, 0, 4);
    ui_rect(pad, gy + 254, (sw - pad * 2) * frames ~/ 60, 14, -11890471, 1, 0, 4);

    ui_text(pad, gy + 278, lang == 0 ? "按键 / 触摸来了就画一个标记" : "a marker is drawn per key / touch", -6643536, 12, 0);
    if (keys > 0) { ui_circle(pad + 20, gy + 306, 14, -2509750, 1, 0); }
    if (touches > 0) { ui_circle(pad + 60, gy + 306, 14, -11409298, 1, 0); }

    // 触摸标记：点哪儿就在哪儿留个圈（证明坐标真能用）
    // Touch marker: a circle is left wherever you tap (proof the coordinates really work)
    if (tx >= 0) {
      ui_circle(tx, ty, 18, -2509750, 0, 2);
      ui_circle(tx, ty, 4, -2509750, 1, 0);
      ui_text(cx, sh - 44, lang == 0 ? "触摸坐标已经用上了" : "touch coordinates in use", -2509750, 12, 1);
    } else {
      ui_text(cx, sh - 44, lang == 0 ? "点一下屏幕 / 按任意键退出" : "Tap anywhere / any key to exit", -6643536, 12, 1);
    }

    ui_text(cx, sh - 24, lang == 0 ? "退出：按任意键或点任意处（或等 N 帧到点）" : "Exit: any key or any tap (or wait for N frames)", -6643536, 12, 1);

    ui_present();

    // ── 取一条消息（标量版，见文件头 ②）──
    // ── Take one message (scalar version, see header ②) ──
    int t = ui_wait_msg(200);
    if (t == 0) { continue; }                  // MSG_NONE

    if (t == 9) {                              // MSG_TIMER
      frames = frames + 1;
      if (frames >= 60) { done = 1; }          // 有界：到点自己停
      // bounded: stops by itself when the time is up
      continue;
    }

    if (t == 1) {                              // MSG_KEYDOWN
      keys = keys + 1;
      done = 1;                                // 有界：收到键就退
      // bounded: exits as soon as a key arrives
      continue;
    }

    if (t == 6 || t == 4) {                    // MSG_TOUCHDOWN / MSG_MOUSEDOWN
      touches = touches + 1;
      tx = ui_msg_a();                         // A=x B=y
      ty = ui_msg_b();
      done = 1;                                // 有界：点到就退
      // bounded: exits as soon as a tap arrives
      continue;
    }

    if (t == 12) {                             // MSG_WINDOWORIENT
      orient = ui_msg_a();                     // A = 新方向（下一帧按新方向排）
      // A = the new orientation (the next frame lays out for it)
      continue;
    }

    if (t == 11) { continue; }                 // MSG_WINDOWRESIZE（下一帧会重问尺寸）
    // MSG_WINDOWRESIZE (the size is re-queried on the next frame)
    if (t == 10) { break; }                    // MSG_WINDOWCLOSE
  }

  ui_timer_kill(tid);
  ui_win_close();

  // 给无头验证留确定性判据（图形只能肉眼看，这几个数能自动比）
  // Deterministic checkpoints for headless verification (graphics can only be eyeballed, but these numbers can be compared automatically)
  if (orient == 1) { println_str("demo_ui: orient=LANDSCAPE"); }
  else             { println_str("demo_ui: orient=PORTRAIT"); }
  print_str("demo_ui: canvas="); print_int(sw); print_str("x"); println_int(sh);
  print_str("demo_ui: frames="); println_int(frames);
  print_str("demo_ui: keys=");   println_int(keys);
  print_str("demo_ui: touches="); println_int(touches);
  println_str("demo_ui: done");
}
