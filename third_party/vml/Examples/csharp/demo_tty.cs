// demo_tty.cs —— **第 2 层：彩色命令行**（`tty_*`）
// demo_tty.cs — **Layer 2: colored command line** (`tty_*`)
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
// ⚠ **New programs use `tty_*`; only old programs use `conio` / `Crt`** (the layering standard the user set:
//   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
//   "new programs use the tty library in text mode and the ui library in graphics mode; only old programs use conio, graphics and the like").
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI 或 `ui_*`。
//   `tty_*` is characters only, with no graphics capability whatsoever; to draw, use BGI or `ui_*`.
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//   Coordinates are **1-based**; colors are **indexed 0–15**.
//
// ## 怎么调（C# 一侧）
// ## How to call it (on the C# side)
//
// 调库函数**不用声明**（对不认识的函数名发裸标签 CALL、实参右到左）——
// Library calls **need no declaration** (an unknown function name is emitted as a bare-label CALL with arguments right-to-left) —
// 与 Java（必须 `static native`）/ Dart（必须 `external`）都不一样。
// Unlike Java (which requires `static native`) / Dart (which requires `external`).
// `tty_*` 的实现是 C 写的（`Lib/shared/src/tty.c` → `Lib/shared/tty.vml`），
// `tty_*` is implemented in C (`Lib/shared/src/tty.c` → `Lib/shared/tty.vml`),
// C# 前端只要能 `call` 那个标签就行。
// and the C# frontend just has to `call` that label.
//
// ## ⚠⚠ 它需要把 `tty.vml` 链进来，而**默认配置没链**
// ## ⚠⚠ It needs `tty.vml` linked in, and **the default configuration does not link it**
//
// 实测（2026-09-24）：直接跑会报
// Measured (2026-09-24): running it directly reports
//     <input>:N: error: 未定义的函数 'tty_init'（引用 1 次）…
//     <input>:N: error: undefined function 'tty_init' (referenced 1 time)…
// 原因：`vmltool.config.xml` 里 csharp（以及**所有**其它语言）的 `Libs` 只有 `vmlui.vml`
// Cause: in `vmltool.config.xml` the `Libs` of csharp (and of **every** other language) contains only `vmlui.vml`
//（`builtins.vml` 来自 `DefaultLibs`），**新落地的 `tty.vml` 一门语言都没挂上**。
// (`builtins.vml` comes from `DefaultLibs`), and **the newly added `tty.vml` is hooked to no language at all**.
// C 是唯一例外 —— `Lib/c/tty.h` 里那句 `#param lib("tty")` 生效了，所以**不加任何参数就能用**。
// C is the only exception — the `#param lib("tty")` in `Lib/c/tty.h` takes effect, so **it works with no extra arguments**.
//
// 两种跑法：
// Two ways to run it:
//   ① **桌面**：加 `--lib` 指到它（本文件就是这么验的）
//        vmlcli Examples/csharp/demo_tty.cs --lib Lib/shared/tty.vml
//   ② **要能进包/上手机**：在 `vmltool.config.xml` 里 csharp（以及 java/kotlin/dart/…）
//   ② **To be packaged / run on the phone**: in `vmltool.config.xml` add to the `Libs=` lines of csharp (and java/kotlin/dart/…)
//      那几行 `Libs=` 后面补 `tty.vml` —— 共享配置，本份例程按约定没有代改（见交付报告）。
//      ... `tty.vml` — shared configuration; by convention this example does not make the change (see the delivery report).
//
// ## ⚠ UTF-8 的字节预算
// ## ⚠ The UTF-8 byte budget
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（每字节 `conX++`、到 80 折行），
// `putch()` in `conio.c` counts **bytes** as **columns** (each byte does `conX++`, wrapping at 80),
// 一个汉字/框线字符是 3 字节 ⇒ 某段连续输出**跨过第 80 列**时折行会落在字符中间，
// one Chinese character / box-drawing char is 3 bytes ⇒ when a continuous run **crosses column 80** the wrap lands mid-character,
// 宿主按字节重组 UTF-8 的校验随即失败并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。
// the host's byte-wise UTF-8 reassembly check then fails and **stickily** switches to a single-byte legacy encoding ⇒ everything after that is garbled.
// 所以版面遵守「**起始列 + 3×字符数 ≤ 80**」。细节与最小复现见 `Examples/c/demo_tty.c` 文件头。
// So the layout obeys "**start column + 3×chars ≤ 80**". For details and a minimal reproduction see the header of `Examples/c/demo_tty.c`.

class DemoTty
{
    // 在第 (x,y) 处用指定前景/背景打一串
    // Print a string at (x,y) with the given foreground/background
    static void say(int x, int y, int fg, int bg, string s)
    {
        tty_print_at(x, y, s, fg, bg);
    }

    static void Main()
    {
        int i;
        int lang = ui_get_language();

        // ── 1. 初始化 + 清屏 ──
        // ── 1. Init + clear screen ──
        tty_init(0, 0, 0);
        tty_cls();

        // ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ──
        // ── 2. Title bar: bright yellow on blue, filling row 1 ──
        tty_color(14, 1);
        tty_goto(1, 1);
        tty_puts("                                                                                ");
        tty_goto(3, 1);
        tty_puts("Dolaima  tty_* demo (C#)  --  color / cursor / box / int");

        tty_color(8, 0);
        tty_goto(3, 2);
        tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

        // ── 3. ASCII 面板（61 列，安全）──
        // ── 3. ASCII panel (61 columns, safe) ──
        tty_color(7, 0);
        tty_box(2, 3, 62, 12, 0);              // style 0 = ASCII(`+ - |`)

        // 面板里的内容：每行一种前景色
        // Panel contents: one foreground color per line
        say(4, 4,  7,  0, lang == 0 ? "color  7  lightgray    普通正文" : "color  7  lightgray    body text");
        say(4, 5,  11, 0, lang == 0 ? "color 11  lightcyan    次要信息" : "color 11  lightcyan    secondary info");
        say(4, 6,  10, 0, lang == 0 ? "color 10  lightgreen   ok / 成功" : "color 10  lightgreen   ok / success");
        say(4, 7,  14, 0, lang == 0 ? "color 14  yellow       状态栏高亮" : "color 14  yellow       status bar");
        say(4, 8,  12, 0, lang == 0 ? "color 12  lightred     错误 / 警告" : "color 12  lightred     error / warning");
        say(4, 9,  13, 0, lang == 0 ? "color 13  lightmagenta 强调" : "color 13  lightmagenta emphasis");

        // 反白一行（"选中项"的长相）：黑字白底
        // Reverse-video one line (what a "selected item" looks like): black on white
        say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

        // ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）──
        // ── 4. UTF-8 single-line frame (19 cells wide × 3 bytes = 57, starting at column 4 ⇒ 61 bytes, safe) ──
        tty_color(11, 0);
        tty_box(4, 14, 22, 19, 1);             // style 1 = ┌ ─ │ ┐ └ ┘ 单线框
        // style 1 = single-line box ┌ ─ │ ┐ └ ┘

        say(6, 16, 14, 0, lang == 0 ? "中文也" : "CJK also");
        say(6, 17, 10, 0, lang == 0 ? "没问题" : "works fine");

        // ── 5. 右侧说明（ASCII，列 26 起）──
        // ── 5. Right-hand notes (ASCII, starting at column 26) ──
        say(26, 14, 11, 0, "tty_box(..., style=1)");
        say(26, 15, 7,  0, "  = UTF-8 single-line box");
        say(26, 17, 11, 0, "tty_box(..., style=0)");
        say(26, 18, 7,  0, "  = ASCII box (used above)");

        // ── 6. 16 色色带 ──
        // ── 6. 16-color bar ──
        for (i = 0; i < 16; i++) {
            tty_color(0, i);
            tty_goto(3 + i * 4, 21);
            tty_puts("    ");
        }
        tty_color(7, 0);
        tty_goto(3, 22);
        tty_puts("index 0..15: black blue green cyan red magenta brown gray / +8 = bright");

        // ── 7. 数字与光标 ──
        // ── 7. Numbers and the cursor ──
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
        // ── 8. Status bar + wrap-up ──
        tty_color(0, 3);
        tty_goto(1, 24);
        tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
        tty_color(8, 0);
        tty_goto(3, 25);
        tty_puts(lang == 0 ? "demo_tty (C#) 结束 —— 画完即退出" : "demo_tty (C#) done -- draws then exits");

        tty_color(7, 0);
    }
}
