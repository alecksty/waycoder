// demo_tty.dart —— **第 2 层：彩色命令行**（`tty_*`）
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
//   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI 或 `ui_*`。
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//
// ## ⚠⚠ 本份**当前跑不起来** —— `tty.vml` 链不上（原因两条，都在这份例程之外）
//
//   ① **`tty.vml` 没有被任何语言挂上**：`vmltool.config.xml` 里 dart（以及所有其它语言）
//      的 `Libs` 只有 `vmlui.vml`（`builtins.vml` 来自 `DefaultLibs`）。
//      **C 是唯一例外** —— `Lib/c/tty.h` 里那句 `#param lib("tty")` 生效，
//      所以 C 不加任何参数就能用（`Examples/c/demo_tty.c` 已跑通）。
//   ② **`--lib` 对 Dart 前端不生效**（本轮实测；与 `demo_bgi.dart` 同一个根因）：
//
//          vmlcli Examples/dart/demo_tty.dart --lib Lib/shared/tty.vml
//            → <input>:6: error: 未定义的函数 'tty_init'（引用 1 次）
//
//      同一份调用给 **Java / C# / C++** 加同一个 `--lib` 就**都能跑**
//      （三份 `demo_tty` 均已出画面）；给 Kotlin / Dart 加就还是未定义。
//      最小对照见 `Examples/kotlin/demo_tty.kt` 的文件头（两个文件只改扩展名）。
//
// **修法（都动共享代码，本份例程没有代改）**：把 `tty.vml` 加进 `vmltool.config.xml`
//   里 dart / kotlin 那两行 `Libs=`。
//
// ## 本文件的状态：**写法已验证、运行未验证**
//
//   · 版面与调用串与 `Examples/java/demo_tty.java`（已跑通出画面）**逐句同构**。
//   · Dart 这边能编到**只剩 `tty_*` 未定义**为止（没有别的语法/调用错误）。
//   ⚠ 它现在编不过，但**不是空壳** —— 配置补齐后应当直接可用。
//
// ## ⚠ Dart 前端的写法约束（见 demo_std.dart 的文件头）
//
//   · 调库函数**必须 `external` 声明**；
//   · **没有 `println`**（`func_println` 不在链接进来的库里）；
//   · 字符串拼接 `+` 与 `.toString()` 不可用 ⇒ 标签与值分两次写。
//
// ## ⚠ UTF-8 的字节预算
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（到 80 折行），一个汉字/框线字符 3 字节
// ⇒ 某段连续输出跨过第 80 列时折行落在字符中间，宿主按字节重组 UTF-8 的校验随即失败
// 并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。版面遵守「起始列 + 3×字符数 ≤ 80」。
// 细节与最小复现见 `Examples/c/demo_tty.c` 文件头。

external int  tty_init(int width, int height, int clear);
external void tty_cls();
external void tty_goto(int x, int y);
external void tty_color(int fg, int bg);
external void tty_puts(String s);
external void tty_put_int(int v);
external void tty_print_at(int x, int y, String s, int fg, int bg);
external void tty_box(int x1, int y1, int x2, int y2, int style);
external int  tty_wherex();
external int  tty_wherey();
external int  tty_width();
external int  tty_height();

// 在第 (x,y) 处用指定前景/背景打一串
void say(int x, int y, int fg, int bg, String s) {
  tty_print_at(x, y, s, fg, bg);
}

void main() {
  int i = 0;

  // ── 1. 初始化 + 清屏 ──
  tty_init(0, 0, 0);
  tty_cls();

  // ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ──
  tty_color(14, 1);
  tty_goto(1, 1);
  tty_puts("                                                                                ");
  tty_goto(3, 1);
  tty_puts("WayCoder  tty_* demo (Dart)  --  color / cursor / box / int");

  tty_color(8, 0);
  tty_goto(3, 2);
  tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

  // ── 3. ASCII 面板（61 列，安全）──
  tty_color(7, 0);
  tty_box(2, 3, 62, 12, 0);              // style 0 = ASCII(`+ - |`)

  // 面板里的内容：每行一种前景色
  say(4, 4,  7,  0, "color  7  lightgray    普通正文");
  say(4, 5,  11, 0, "color 11  lightcyan    次要信息");
  say(4, 6,  10, 0, "color 10  lightgreen   ok / 成功");
  say(4, 7,  14, 0, "color 14  yellow       状态栏高亮");
  say(4, 8,  12, 0, "color 12  lightred     错误 / 警告");
  say(4, 9,  13, 0, "color 13  lightmagenta 强调");

  // 反白一行（"选中项"的长相）：黑字白底
  say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

  // ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）──
  tty_color(11, 0);
  tty_box(4, 14, 22, 19, 1);             // style 1 = ┌ ─ │ ┐ └ ┘ 单线框

  say(6, 16, 14, 0, "中文也");
  say(6, 17, 10, 0, "没问题");

  // ── 5. 右侧说明（ASCII，列 26 起）──
  say(26, 14, 11, 0, "tty_box(..., style=1)");
  say(26, 15, 7,  0, "  = UTF-8 single-line box");
  say(26, 17, 11, 0, "tty_box(..., style=0)");
  say(26, 18, 7,  0, "  = ASCII box (used above)");

  // ── 6. 16 色色带 ──
  i = 0;
  while (i < 16) {
    tty_color(0, i);
    tty_goto(3 + i * 4, 21);
    tty_puts("    ");
    i = i + 1;
  }
  tty_color(7, 0);
  tty_goto(3, 22);
  tty_puts("index 0..15: black blue green cyan red magenta brown gray / +8 = bright");

  // ── 7. 数字与光标 ──
  tty_color(11, 0);
  tty_goto(3, 24);
  tty_puts("tty_put_int: ");
  tty_color(14, 0);
  tty_put_int(2026);
  tty_color(11, 0);
  tty_puts(" / ");
  tty_color(14, 0);
  tty_put_int(-42);
  tty_color(11, 0);
  tty_puts("   cursor=");
  tty_put_int(tty_wherex());
  tty_puts(",");
  tty_put_int(tty_wherey());
  tty_puts("   screen=");
  tty_put_int(tty_width());
  tty_puts("x");
  tty_put_int(tty_height());

  // ── 8. 状态栏 + 收尾 ──
  tty_color(0, 3);
  tty_goto(1, 24);
  tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
  tty_color(8, 0);
  tty_goto(3, 25);
  tty_puts("demo_tty (Dart) 结束 —— 画完即退出");

  tty_color(7, 0);
}
