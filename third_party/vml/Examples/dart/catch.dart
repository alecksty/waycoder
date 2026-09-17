// 接方块 —— 用 **Dart** 写的手机游戏
//
// 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
//
// ◆ 手机那套 UI
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="dart" Libs="vmlui.vml">` 挂上来。
//
// ◆ 写法要求
//
//   · 调库函数**必须 `external` 声明** —— 这是本前端唯一能产出**裸标签** CALL 的形式
//     （`CodeGenerator.Expressions.cs:141`；`asm()` 已移除）。不声明就解析不到 lib_vmlui_*。
//   · 颜色写负数十进制（词法器是十进制专用）。
//   · `print` 不加分隔符、不补换行 ⇒ 显式补 "\n"。
//   · **状态与逻辑全内联在 `main` 里**：文件级可变状态在 Kotlin / Swift / Go 那几门上都出过问题
//     （最典型是 Kotlin 的文件级 `arrayOf` 读回 0），这里不冒这个险。
//
// ◆ 已修的前端缺陷（2026-09-17 前后）
//
//   · 数组：早期「没有下标表达式、`a[i]` 退化成一个裸名、`a[i] = x` 左值被丢弃」；
//     patch 0030 修过。本份实测 `a[0]=7; a[3]=5;` 求和得 12 ✓。
//   · **栈帧**：早期 `GenerateVarDecl` 只写 `[R12-N]` 却不 `SUB R13`，带嵌套 push 的表达式
//     会踩坏局部变量（语料文件头记的就是这条）；patch 0030 一并修了。

external int ui_scr_w();
external int ui_scr_h();
external int ui_win_open(String t, int w, int h);
external int ui_win_closed();
external int ui_win_close();
external void ui_clear(int c);
external void ui_rect(int x, int y, int w, int h, int c, int f, int lw, int r);
external void ui_circle(int cx, int cy, int r, int c, int f, int lw);
external void ui_text(int x, int y, String s, int c, int size, int anchor);
external void ui_present();
external int ui_timer_set(int ms, int tag);
external void ui_timer_kill(int id);
external int ui_wait_msg(int timeout);
external int ui_msg_a();
external void ui_beep(int freq, int ms);
external void ui_keep_on(int v);
external void ui_dlg_msg(String title, String body, int style);

void main() {
  // 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高
  List<int> A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

  int w = ui_scr_w();
  int h = ui_scr_h();
  if (w <= 0) { w = 360; }
  if (h <= 0) { h = 620; }
  A[8] = w;
  A[9] = h;
  ui_win_open("接方块", w, h);
  ui_keep_on(1);

  A[0] = w / 2 - 40;
  A[1] = w / 2;
  A[2] = 70;
  A[3] = 3;
  A[4] = 5;
  A[5] = 0;
  A[6] = 0;
  A[7] = 1;

  int tid = ui_timer_set(40, 0);

  while (ui_win_closed() == 0) {
    // ── draw ──
    ui_clear(-15724520);
    ui_text(8, 8, "得分", -6643536, 13, 0);
    ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
    ui_text(A[8] / 2, 8, "最高", -6643536, 13, 1);
    ui_rect(A[8] / 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
    ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
    ui_circle(A[1], A[2], 9, -131246, 1, 0);
    if (A[7] == 0) {
      ui_text(A[8] / 2, A[9] / 2, "按回车重开", -131246, 16, 1);
    }
    ui_present();

    int t = ui_wait_msg(0);
    if (t == 10) { break; }

    if (t == 9) {
      if (A[7] != 0) {
        A[1] = A[1] + A[3];
        A[2] = A[2] + A[4];
        if (A[1] < 10) { A[1] = 10; A[3] = 0 - A[3]; }
        if (A[1] > A[8] - 10) { A[1] = A[8] - 10; A[3] = 0 - A[3]; }
        if (A[2] < 30) { A[2] = 30; A[4] = 0 - A[4]; }
        // 接住：球落到挡板带上、横向也在挡板范围内（多条件用嵌套 if）
        if (A[2] > A[9] - 52) {
          if (A[2] < A[9] - 30) {
            if (A[1] > A[0] - 9) {
              if (A[1] < A[0] + 89) {
                A[4] = 0 - A[4];
                A[2] = A[9] - 52;
                A[5] = A[5] + 10;
                if (A[5] > A[6]) { A[6] = A[5]; }
                ui_beep(880, 30);
              }
            }
          }
        }
        if (A[2] > A[9]) {
          A[7] = 0;
          ui_beep(220, 260);
          ui_dlg_msg("接方块", "没接住，这一局结束。\n再来一局？（选「否」退出）", 0);
          A[0] = A[8] / 2 - 40;
          A[1] = A[8] / 2;
          A[2] = 70;
          A[3] = 3;
          A[4] = 5;
          A[5] = 0;
          A[7] = 1;
        }
      }
    }

    if (t == 1) {
      int k = ui_msg_a();
      if (k == 27) { break; }
      if (k == 37) { A[0] = A[0] - 20; if (A[0] < 4) { A[0] = 4; } }
      if (k == 39) { A[0] = A[0] + 20; if (A[0] > A[8] - 84) { A[0] = A[8] - 84; } }
      if (k == 13) {
        A[0] = A[8] / 2 - 40;
        A[1] = A[8] / 2;
        A[2] = 70;
        A[3] = 3;
        A[4] = 5;
        A[5] = 0;
        A[7] = 1;
      }
    }
  }

  ui_timer_kill(tid);
  ui_keep_on(0);
  ui_win_close();
}
