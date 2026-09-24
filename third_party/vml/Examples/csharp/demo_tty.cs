// demo_tty.cs —— **第 2 层：彩色命令行**（`tty_*`）
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
//   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI 或 `ui_*`。
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//
// ## 怎么调（C# 一侧）
//
// 调库函数**不用声明**（对不认识的函数名发裸标签 CALL、实参右到左）——
// 与 Java（必须 `static native`）/ Dart（必须 `external`）都不一样。
// `tty_*` 的实现是 C 写的（`Lib/shared/src/tty.c` → `Lib/shared/tty.vml`），
// C# 前端只要能 `call` 那个标签就行。
//
// ## ⚠⚠ 它需要把 `tty.vml` 链进来，而**默认配置没链**
//
// 实测（2026-09-24）：直接跑会报
//     <input>:N: error: 未定义的函数 'tty_init'（引用 1 次）…
// 原因：`vmltool.config.xml` 里 csharp（以及**所有**其它语言）的 `Libs` 只有 `vmlui.vml`
//（`builtins.vml` 来自 `DefaultLibs`），**新落地的 `tty.vml` 一门语言都没挂上**。
// C 是唯一例外 —— `Lib/c/tty.h` 里那句 `#param lib("tty")` 生效了，所以**不加任何参数就能用**。
//
// 两种跑法：
//   ① **桌面**：加 `--lib` 指到它（本文件就是这么验的）
//        vmlcli Examples/csharp/demo_tty.cs --lib Lib/shared/tty.vml
//   ② **要能进包/上手机**：在 `vmltool.config.xml` 里 csharp（以及 java/kotlin/dart/…）
//      那几行 `Libs=` 后面补 `tty.vml` —— 共享配置，本份例程按约定没有代改（见交付报告）。
//
// ## ⚠ UTF-8 的字节预算
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（每字节 `conX++`、到 80 折行），
// 一个汉字/框线字符是 3 字节 ⇒ 某段连续输出**跨过第 80 列**时折行会落在字符中间，
// 宿主按字节重组 UTF-8 的校验随即失败并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。
// 所以版面遵守「**起始列 + 3×字符数 ≤ 80**」。细节与最小复现见 `Examples/c/demo_tty.c` 文件头。

class DemoTty
{
    // 在第 (x,y) 处用指定前景/背景打一串
    static void say(int x, int y, int fg, int bg, string s)
    {
        tty_print_at(x, y, s, fg, bg);
    }

    static void Main()
    {
        int i;

        // ── 1. 初始化 + 清屏 ──
        tty_init(0, 0, 0);
        tty_cls();

        // ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ──
        tty_color(14, 1);
        tty_goto(1, 1);
        tty_puts("                                                                                ");
        tty_goto(3, 1);
        tty_puts("WayCoder  tty_* demo (C#)  --  color / cursor / box / int");

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
        for (i = 0; i < 16; i++) {
            tty_color(0, i);
            tty_goto(3 + i * 4, 21);
            tty_puts("    ");
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
        tty_puts("demo_tty (C#) 结束 —— 画完即退出");

        tty_color(7, 0);
    }
}
