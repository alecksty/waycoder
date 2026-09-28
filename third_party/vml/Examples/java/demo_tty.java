// demo_tty.java —— **第 2 层：彩色命令行**（`tty_*`）
// demo_tty.java — **Layer 2: colored command line** (`tty_*`)
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
// ## ⚠ Java 调库必须 `static native` 声明
// ## ⚠ Java library calls must be declared `static native`
//
// `asm()` 已从 Java 前端移除（`CodeGenerator.Expressions.cs` 注明「仅限 C/ObjC/C++」），
// `asm()` has been removed from the Java frontend (`CodeGenerator.Expressions.cs` notes "C/ObjC/C++ only"),
// 不声明就发 `CALL method_xxx`，链接期找不到标签。声明里的 `String` 对应 C 的 `char*`。
// and without a declaration it emits `CALL method_xxx`, whose label cannot be found at link time. The `String` in the declaration corresponds to C's `char*`.
//
// ## ⚠⚠ 它需要把 `tty.vml` 链进来，而**默认配置没链**
// ## ⚠⚠ It needs `tty.vml` linked in, and **the default configuration does not link it**
//
// 实测（2026-09-24）：直接跑报 `<input>:N: error: 未定义的函数 'tty_init'（引用 1 次）`。
// Measured (2026-09-24): running it directly reports `<input>:N: error: undefined function 'tty_init' (referenced 1 time)`.
// `vmltool.config.xml` 里 java（以及所有其它语言）的 `Libs` 只有 `vmlui.vml`，
// In `vmltool.config.xml` the `Libs` of java (and every other language) contains only `vmlui.vml`,
// **新落地的 `tty.vml` 一门语言都没挂上**。C 是唯一例外 —— `Lib/c/tty.h` 里那句
// **the newly added `tty.vml` is hooked to no language at all**. C is the only exception — the
// `#param lib("tty")` 生效了，所以 C 不加参数就能用。
// `#param lib("tty")` in `Lib/c/tty.h` takes effect, so C can use it without extra arguments.
//
//   · 桌面跑法：`vmlcli Examples/java/demo_tty.java --lib Lib/shared/tty.vml`
//   · Desktop: `vmlcli Examples/java/demo_tty.java --lib Lib/shared/tty.vml`
//   · 上手机：要在 `vmltool.config.xml` 给 java 的 `Libs=` 补 `tty.vml`（共享配置，按约定未代改）
//   · On the phone: add `tty.vml` to java's `Libs=` in `vmltool.config.xml` (shared configuration; by convention not changed on your behalf)
//
// ## ⚠ 两条 Java 前端的写法约束（见 demo_std.java 的文件头）
// ## ⚠ Two style constraints of the Java frontend (see the header of demo_std.java)
//
//   · **不能用 `static final int` 常量**：类字段读出来是它的地址不是值
//   · **`static final int` constants cannot be used**: a class field reads back as its address, not its value
//     （`static int A = 9` 读回 1024）。所以下面一处常量都不定义，数字写死。
//     (`static int A = 9` reads back 1024). So not a single constant is defined below; the numbers are hardcoded.
//   · 字符串拼接 `+` 不可用 ⇒ 标签与值分两次 `tty_puts` / `tty_put_int` 写。
//   · String concatenation `+` is unusable ⇒ write the label and the value with two calls, `tty_puts` / `tty_put_int`.
//
// ## ⚠ UTF-8 的字节预算
// ## ⚠ The UTF-8 byte budget
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（到 80 折行），一个汉字/框线字符 3 字节
// `putch()` in `conio.c` counts **bytes** as **columns** (wrapping at 80), and one Chinese character / box-drawing char is 3 bytes
// ⇒ 某段连续输出跨过第 80 列时折行落在字符中间，宿主按字节重组 UTF-8 的校验随即失败
// ⇒ when a continuous run of output crosses column 80 the wrap lands mid-character, the host's byte-wise UTF-8 reassembly check then fails
// 并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。版面遵守「起始列 + 3×字符数 ≤ 80」。
// and **stickily** switches to a single-byte legacy encoding ⇒ everything after that is garbled. The layout obeys "start column + 3×chars ≤ 80".
// 细节与最小复现见 `Examples/c/demo_tty.c` 文件头。
// For details and a minimal reproduction see the header of `Examples/c/demo_tty.c`.

public class DemoTty {

    static native int  tty_init(int width, int height, int clear);
    static native void tty_cls();
    static native void tty_goto(int x, int y);
    static native void tty_color(int fg, int bg);
    static native void tty_puts(String s);
    static native void tty_put_int(int v);
    static native void tty_print_at(int x, int y, String s, int fg, int bg);
    static native void tty_box(int x1, int y1, int x2, int y2, int style);
    static native int  tty_wherex();
    static native int  tty_wherey();
    static native int  tty_width();
    static native int  tty_height();

    // 在第 (x,y) 处用指定前景/背景打一串
    // Print a string at (x,y) with the given foreground/background
    static void say(int x, int y, int fg, int bg, String s) {
        tty_print_at(x, y, s, fg, bg);
    }

    public static void main(String[] args) {
        int i;

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
        tty_puts("WayCoder  tty_* demo (Java)  --  color / cursor / box / int");

        tty_color(8, 0);
        tty_goto(3, 2);
        tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int");

        // ── 3. ASCII 面板（61 列，安全）──
        // ── 3. ASCII panel (61 columns, safe) ──
        tty_color(7, 0);
        tty_box(2, 3, 62, 12, 0);              // style 0 = ASCII(`+ - |`)

        // 面板里的内容：每行一种前景色
        // Panel contents: one foreground color per line
        say(4, 4,  7,  0, "color  7  lightgray    普通正文");
        say(4, 5,  11, 0, "color 11  lightcyan    次要信息");
        say(4, 6,  10, 0, "color 10  lightgreen   ok / 成功");
        say(4, 7,  14, 0, "color 14  yellow       状态栏高亮");
        say(4, 8,  12, 0, "color 12  lightred     错误 / 警告");
        say(4, 9,  13, 0, "color 13  lightmagenta 强调");

        // 反白一行（"选中项"的长相）：黑字白底
        // Reverse-video one line (what a "selected item" looks like): black on white
        say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ");

        // ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）──
        // ── 4. UTF-8 single-line frame (19 cells wide × 3 bytes = 57, starting at column 4 ⇒ 61 bytes, safe) ──
        tty_color(11, 0);
        tty_box(4, 14, 22, 19, 1);             // style 1 = ┌ ─ │ ┐ └ ┘ 单线框
        // style 1 = single-line box ┌ ─ │ ┐ └ ┘

        say(6, 16, 14, 0, "中文也");
        say(6, 17, 10, 0, "没问题");

        // ── 5. 右侧说明（ASCII，列 26 起）──
        // ── 5. Right-hand notes (ASCII, starting at column 26) ──
        say(26, 14, 11, 0, "tty_box(..., style=1)");
        say(26, 15, 7,  0, "  = UTF-8 single-line box");
        say(26, 17, 11, 0, "tty_box(..., style=0)");
        say(26, 18, 7,  0, "  = ASCII box (used above)");

        // ── 6. 16 色色带 ──
        // ── 6. 16-color bar ──
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
        tty_puts("demo_tty (Java) 结束 —— 画完即退出");

        tty_color(7, 0);
    }
}
