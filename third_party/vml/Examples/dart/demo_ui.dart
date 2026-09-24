// demo_ui.dart —— **第 4 层：最新 UI 接口**（`ui_*` / `Lib/c/waycoder_ui.h`）
//
// 与第 3 层（BGI）正好相反：
//
//   · **没有固定分辨率**：画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），程序现排版。
//   · **真彩 0xAARRGGBB**，不是 16 个索引色。
//   · **有消息循环**：触摸 / 按键 / 定时器 / 窗口被关 / 被转屏都从消息里出来。
//
// ## ⚠ 三条写法约束，全部是实测量出来的（不是风格）
//
// ### ① 状态与绘制**全内联在 `main` 里** —— 不用文件级变量、不抽 `draw()` 函数
//
// 先按"顶层 `List<int> A` 放状态 + `draw()` 函数读它"写的一版，**跑起来是错的**：
// 61 帧都出了，但 `demo_ui: canvas=0x0  frames=0`，画面 99% 是纯背景
//（`draw()` 里读到的 `A[0]/A[1]` 是 0 ⇒ 排版全塌）。
// `catch.dart` 的文件头早就记过这条：「**状态与逻辑全内联在 `main` 里**：
// 文件级可变状态在 Kotlin / Swift / Go 那几门上都出过问题，这里不冒这个险」。
// 本文件照它写 —— 每帧重算版面、绘制直接写在循环里。
//
// ### ② 消息用**标量**版读，不把数组传给库函数
//
// Dart 的 `List<int>` 传给库里 `int*` 形参时**指针落在数据起点前 4 字节**（"长度头"）上。
// 实测：`ui_wait(msg,…)` 里宿主写的 `msg=(类型,A,B,时间戳)`，Dart 这边读到
// `(A,B,时间戳,旧值)`（整体错位一格）。所以只用 `ui_wait_msg` / `ui_msg_a` / `ui_msg_b`
// —— `waycoder_ui.h` 里那组"不碰指针的消息读取（非 C 语言用）"正是为这个准备的。
// 同理 `ui_polygon(int* pts, …)` 这类收数组的图元在 Dart 里不能用，本 demo 不碰。
//
// ### ③ `external` 声明 + 没有 `println`
//
// 调库函数**必须 `external` 声明**；`println` 编出来的 `func_println` 不在链接进来的
// 库里（见 demo_std.dart 的文件头）⇒ 换行用 `print("...\n")`，数值用库里的 `println_int`。
//
// ## 退出是有界的
//
// 主循环数**定时器拍数**，到 60 拍自己停；按任意键 / 点任意处也能提前退。
//
// 跑法：手机 `vml run examples/dart/demo_ui.dart`；
//       桌面 `vmlcli Examples/dart/demo_ui.dart --frames /tmp/fr`

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
  orient = ui_orientation();

  w = ui_scr_w();
  h = ui_scr_h();
  if (w <= 0) { w = 360; }
  if (h <= 0) { h = 620; }
  ui_win_open_ex("demo_ui.dart", w, h, 1, 1);   // 支持旋转 + 要手柄
  int tid = ui_timer_set(60, 1);

  // ── 消息循环：定时器驱动重画，**有界退出** ──
  while (done == 0 && ui_win_closed() == 0) {
    // 每帧重新问一次画布尺寸（转屏 / 手柄收放都会让它变）—— 这就是"不写死坐标"
    sw = ui_scr_w();
    sh = ui_scr_h();
    if (sw <= 0) { sw = 360; }
    if (sh <= 0) { sh = 620; }
    cx = sw ~/ 2;
    pad = sw ~/ 16;

    ui_clear(-15724520);

    ui_text_styled(cx, gy, "UI 接口 / demo_ui.dart", -1513232, 16, 1, 1);
    if (orient == 1) {
      ui_text(cx, gy + 26, "屏幕方向 = 横屏 (LANDSCAPE)", -11409298, 13, 1);
    } else {
      ui_text(cx, gy + 26, "屏幕方向 = 竖屏 (PORTRAIT)", -11409298, 13, 1);
    }
    ui_text(cx, gy + 46, "画布按宿主给的尺寸现排（旋转后跟着变）", -6643536, 12, 1);

    // 跟随尺寸的方框：旋转后跟着变宽变矮
    ui_rect(pad, gy + 70, sw - pad * 2, 90, -15066588, 1, 0, 10);
    ui_rect(pad + 6, gy + 76, sw - pad * 2 - 12, 30, -11890471, 1, 0, 6);
    ui_text(pad + 16, gy + 84, "rect / round-rect（随屏宽伸缩）", -15724520, 12, 0);

    ui_circle(cx - sw ~/ 6, gy + 140, sw ~/ 12, -2069424, 1, 0);
    ui_ellipse(cx + sw ~/ 6, gy + 140, sw ~/ 9, sw ~/ 18, -2509750, 1, 0);
    ui_line(pad, gy + 172, sw - pad, gy + 172, -11483016, 3);

    // 8 格真彩色带
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
    ui_text(cx, gy + 210, "真彩 0xAARRGGBB（不是索引色）", -6643536, 12, 1);

    // 进度用**条形长度**表达（Dart 前端没有 int→string，见 demo_std.dart）
    ui_text(pad, gy + 236, "已跑帧数（条形）", -6643536, 12, 0);
    ui_rect(pad, gy + 254, sw - pad * 2, 14, -15066588, 1, 0, 4);
    ui_rect(pad, gy + 254, (sw - pad * 2) * frames ~/ 60, 14, -11890471, 1, 0, 4);

    ui_text(pad, gy + 278, "按键 / 触摸来了就画一个标记", -6643536, 12, 0);
    if (keys > 0) { ui_circle(pad + 20, gy + 306, 14, -2509750, 1, 0); }
    if (touches > 0) { ui_circle(pad + 60, gy + 306, 14, -11409298, 1, 0); }

    // 触摸标记：点哪儿就在哪儿留个圈（证明坐标真能用）
    if (tx >= 0) {
      ui_circle(tx, ty, 18, -2509750, 0, 2);
      ui_circle(tx, ty, 4, -2509750, 1, 0);
      ui_text(cx, sh - 44, "触摸坐标已经用上了", -2509750, 12, 1);
    } else {
      ui_text(cx, sh - 44, "点一下屏幕 / 按任意键退出", -6643536, 12, 1);
    }

    ui_text(cx, sh - 24, "退出：按任意键或点任意处（或等 N 帧到点）", -6643536, 12, 1);

    ui_present();

    // ── 取一条消息（标量版，见文件头 ②）──
    int t = ui_wait_msg(200);
    if (t == 0) { continue; }                  // MSG_NONE

    if (t == 9) {                              // MSG_TIMER
      frames = frames + 1;
      if (frames >= 60) { done = 1; }          // 有界：到点自己停
      continue;
    }

    if (t == 1) {                              // MSG_KEYDOWN
      keys = keys + 1;
      done = 1;                                // 有界：收到键就退
      continue;
    }

    if (t == 6 || t == 4) {                    // MSG_TOUCHDOWN / MSG_MOUSEDOWN
      touches = touches + 1;
      tx = ui_msg_a();                         // A=x B=y
      ty = ui_msg_b();
      done = 1;                                // 有界：点到就退
      continue;
    }

    if (t == 12) {                             // MSG_WINDOWORIENT
      orient = ui_msg_a();                     // A = 新方向（下一帧按新方向排）
      continue;
    }

    if (t == 11) { continue; }                 // MSG_WINDOWRESIZE（下一帧会重问尺寸）
    if (t == 10) { break; }                    // MSG_WINDOWCLOSE
  }

  ui_timer_kill(tid);
  ui_win_close();

  // 给无头验证留确定性判据（图形只能肉眼看，这几个数能自动比）
  if (orient == 1) { println_str("demo_ui: orient=LANDSCAPE"); }
  else             { println_str("demo_ui: orient=PORTRAIT"); }
  print_str("demo_ui: canvas="); print_int(sw); print_str("x"); println_int(sh);
  print_str("demo_ui: frames="); println_int(frames);
  print_str("demo_ui: keys=");   println_int(keys);
  print_str("demo_ui: touches="); println_int(touches);
  println_str("demo_ui: done");
}
