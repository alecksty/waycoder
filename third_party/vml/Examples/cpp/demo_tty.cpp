// demo_tty.cpp —— **第 2 层：彩色命令行**（`tty_*` / `Lib/c/tty.h`）
// demo_tty.cpp — **Layer 2: the colorful command line** (`tty_*` / `Lib/c/tty.h`)
//
// 与 `Examples/c/demo_tty.c` 是**同一个界面、同一串调用**，只是用 C++ 前端再编一遍。
// It is **the same interface with the same sequence of calls** as `Examples/c/demo_tty.c`, just compiled again with the C++ frontend.
// C 与 C++ 是两个独立前端，同一份源码两边行为不保证一致，所以两份都要有。
// C and C++ are two independent frontends, and the same source is not guaranteed to behave the same on both, so both copies are needed.
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准）。
// ⚠ **New programs use `tty_*`; only old programs use `conio` / `Crt`** (the layering standard set by the user).
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI（`graphics.h`）或 `ui_*`。
//   `tty_*` is characters only and has no graphics at all; to draw, use BGI (`graphics.h`) or `ui_*`.
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//   Coordinates are **1-based**; colors are **indexed 0–15**.
//
// ## ⚠⚠ 两条实测出来的坑（都是 C++ 前端特有的）
// ## ⚠⚠ Two pitfalls measured in practice (both specific to the C++ frontend)
//
// ### ① `#param lib("tty")` 在 C++ 上**不生效** —— 必须用 `--lib`
// ### ① `#param lib("tty")` **has no effect** on C++ — you must use `--lib`
//
// 协调文档说「C++ 前端不处理 C 头文件里的 `#param`，要自己在源码顶部写」。
// The coordination document says "the C++ frontend does not process `#param` in C headers; you have to write it yourself at the top of the source".
// 实测（2026-09-24）：**在源码顶部写也不行**。五种写法全试过，一个都没链上：
// Measured (2026-09-24): **writing it at the top of the source does not work either**. All five forms were tried, and not one of them linked:
//
//     #param lib(tty)            → 未定义的函数 'tty_init'（引用 1 次）
//     #param lib(tty)            → undefined function 'tty_init' (referenced 1 time)
//     #param lib(tty.vml)        → 同上
//     #param lib(tty.vml)        → same as above
//     #param lib(shared/tty.vml) → 同上
//     #param lib(shared/tty.vml) → same as above
//     #param lib("tty.vml")      → 同上
//     #param lib("tty.vml")      → same as above
//     #param lib("shared/tty.vml")→ 同上
//     #param lib("shared/tty.vml")→ same as above
//
//   而**同一份内容换个扩展名给 C 编（`.c`）就 0 个未定义** ⇒ 是 C++ 前端不处理
//   Yet **the same content compiled as C under a different extension (`.c`) gives 0 undefined** ⇒ it is the C++ frontend that does not handle
//   `#param`，不是写法问题。（本文件顶部仍然留着那一行 —— 它没有副作用，
//   `#param` is simply not processed, so it is not a syntax problem. (The line is still kept at the top of this file — it has no side effects,
//   而且等前端补上之后就该由它起作用。）
//   and once the frontend catches up it should take effect.)
//
//   ⇒ **跑法**（`--lib` 是桌面脚手架的路子；手机上要靠 `vmltool.config.xml`
//   ⇒ **How to run** (`--lib` is the desktop-scaffold route; on the phone you rely on `vmltool.config.xml`
//     给 cpp 的 `Libs=` 补上 `tty.vml`，那是共享配置，本份例程没有代改）：
//     to add `tty.vml` to cpp's `Libs=`; that is shared configuration, and this example does not change it on your behalf):
//
//         vmlcli Examples/cpp/demo_tty.cpp --lib Lib/shared/tty.vml
//
// ### ② UTF-8 的字节预算（与 C 那份同一个理由）
// ### ② The UTF-8 byte budget (the same reason as the C copy)
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（每字节 `conX++`、到 80 折行），
// `conio.c`'s `putch()` counts **bytes** as **columns** (`conX++` per byte, wrapping at 80),
// 一个汉字/框线字符是 3 字节 ⇒ 某段连续输出**跨过第 80 列**时，折行会落在字符中间，
// and a CJK character or a box-drawing character is 3 bytes ⇒ when a stretch of output **crosses column 80**, the wrap lands in the middle of a character,
// 宿主按字节重组 UTF-8 的校验随即失败并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。
// the host's byte-wise UTF-8 reassembly check then fails and **stickily** switches to a single-byte legacy encoding ⇒ all output after that is garbled.
// 所以版面遵守「**起始列 + 3×字符数 ≤ 80**」，细节见 `demo_tty.c` 的文件头。
// So the layout obeys "**start column + 3 × character count ≤ 80**"; for details see the file header of `demo_tty.c`.
//
// 跑法（桌面）：vmlcli Examples/cpp/demo_tty.cpp --lib Lib/shared/tty.vml
// How to run (desktop): vmlcli Examples/cpp/demo_tty.cpp --lib Lib/shared/tty.vml

#param lib("tty")
#include <tty.h>
#include <waycoder_ui.h>     /* 只为 `ui_get_language()` 的声明（本头文件只给声明与常量，不会让程序变大） */
                            /* only for the `ui_get_language()` declaration (this header carries declarations and constants only; it does not make the program bigger) */

#define ROWS 25

// 在第 (x,y) 处用指定前景/背景打一串（打完颜色恢复成 7/0）
// Print a string at (x,y) with the given foreground/background (colors are restored to 7/0 afterwards)
static void say(int x, int y, int fg, int bg, char *s)
{
    tty_print_at(x, y, s, fg, bg);
}

int main()
{
    int i;
    // 界面语言：开局查一次（`ui_get_language` 是 syscall，别每帧调）—— 文案按它取
    // UI language: queried once at startup (`ui_get_language` is a syscall, do not call it every frame) — the text is picked by it
    int Lang = ui_get_language();

    // ── 1. 初始化 + 清屏 ──
    // ── 1. Initialize + clear the screen ──
    tty_init(0, 0, 0);
    tty_cls();

    // ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ──
    // ── 2. Title bar: bright yellow text on a blue background, filling row 1 ──
    tty_color(14, 1);
    tty_goto(1, 1);
    tty_puts("                                                                                ");
    tty_goto(3, 1);
    tty_puts("WayCoder  tty_* demo (C++)  --  color / cursor / box / int");

    tty_color(8, 0);
    tty_goto(3, 2);
    tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

    // ── 3. ASCII 面板（61 列，安全）──
    // ── 3. ASCII panel (61 columns, safe) ──
    tty_color(7, 0);
    tty_box(2, 3, 62, 12, 0);              // style 0 = ASCII(`+ - |`)

    // 面板里的内容：每行一种前景色
    // Content inside the panel: one foreground color per line
    say(4, 4,  7,  0, Lang == 0 ? "color  7  lightgray    普通正文" : "color  7  lightgray    body text");
    say(4, 5,  11, 0, Lang == 0 ? "color 11  lightcyan    次要信息" : "color 11  lightcyan    secondary info");
    say(4, 6,  10, 0, Lang == 0 ? "color 10  lightgreen   ok / 成功" : "color 10  lightgreen   ok / success");
    say(4, 7,  14, 0, Lang == 0 ? "color 14  yellow       状态栏高亮" : "color 14  yellow       status bar highlight");
    say(4, 8,  12, 0, Lang == 0 ? "color 12  lightred     错误 / 警告" : "color 12  lightred     error / warning");
    say(4, 9,  13, 0, Lang == 0 ? "color 13  lightmagenta 强调" : "color 13  lightmagenta emphasis");

    // 反白一行（"选中项"的长相）：黑字白底
    // One reversed line (what a "selected item" looks like): black text on a white background
    say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

    // ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）──
    // ── 4. UTF-8 single-line box (19 cells wide × 3 bytes = 57, starting at column 4 ⇒ 61 bytes, safe) ──
    tty_color(11, 0);
    tty_box(4, 14, 22, 19, 1);             // style 1 = ┌ ─ │ ┐ └ ┘ 单线框
    // style 1 = ┌ ─ │ ┐ └ ┘ single-line box

    say(6, 16, 14, 0, Lang == 0 ? "中文也" : "Chinese too");
    say(6, 17, 10, 0, Lang == 0 ? "没问题" : "works fine");

    // ── 5. 右侧说明（ASCII，列 26 起）──
    // ── 5. Notes on the right (ASCII, starting at column 26) ──
    say(26, 14, 11, 0, "tty_box(..., style=1)");
    say(26, 15, 7,  0, "  = UTF-8 single-line box");
    say(26, 17, 11, 0, "tty_box(..., style=0)");
    say(26, 18, 7,  0, "  = ASCII box (used above)");

    // ── 6. 16 色色带 ──
    // ── 6. The 16-color band ──
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
    tty_goto(1, ROWS - 1);
    tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
    tty_color(8, 0);
    tty_goto(3, ROWS);
    tty_puts(Lang == 0 ? "demo_tty (C++) 结束 —— 画完即退出" : "demo_tty (C++) done -- exits by itself");

    tty_color(7, 0);
    return 0;
}
