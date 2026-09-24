// demo_tty.cpp —— **第 2 层：彩色命令行**（`tty_*` / `Lib/c/tty.h`）
//
// 与 `Examples/c/demo_tty.c` 是**同一个界面、同一串调用**，只是用 C++ 前端再编一遍。
// C 与 C++ 是两个独立前端，同一份源码两边行为不保证一致，所以两份都要有。
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准）。
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI（`graphics.h`）或 `ui_*`。
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//
// ## ⚠⚠ 两条实测出来的坑（都是 C++ 前端特有的）
//
// ### ① `#param lib("tty")` 在 C++ 上**不生效** —— 必须用 `--lib`
//
// 协调文档说「C++ 前端不处理 C 头文件里的 `#param`，要自己在源码顶部写」。
// 实测（2026-09-24）：**在源码顶部写也不行**。五种写法全试过，一个都没链上：
//
//     #param lib(tty)            → 未定义的函数 'tty_init'（引用 1 次）
//     #param lib(tty.vml)        → 同上
//     #param lib(shared/tty.vml) → 同上
//     #param lib("tty.vml")      → 同上
//     #param lib("shared/tty.vml")→ 同上
//
//   而**同一份内容换个扩展名给 C 编（`.c`）就 0 个未定义** ⇒ 是 C++ 前端不处理
//   `#param`，不是写法问题。（本文件顶部仍然留着那一行 —— 它没有副作用，
//   而且等前端补上之后就该由它起作用。）
//
//   ⇒ **跑法**（`--lib` 是桌面脚手架的路子；手机上要靠 `vmltool.config.xml`
//     给 cpp 的 `Libs=` 补上 `tty.vml`，那是共享配置，本份例程没有代改）：
//
//         vmlcli Examples/cpp/demo_tty.cpp --lib Lib/shared/tty.vml
//
// ### ② UTF-8 的字节预算（与 C 那份同一个理由）
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（每字节 `conX++`、到 80 折行），
// 一个汉字/框线字符是 3 字节 ⇒ 某段连续输出**跨过第 80 列**时，折行会落在字符中间，
// 宿主按字节重组 UTF-8 的校验随即失败并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。
// 所以版面遵守「**起始列 + 3×字符数 ≤ 80**」，细节见 `demo_tty.c` 的文件头。
//
// 跑法（桌面）：vmlcli Examples/cpp/demo_tty.cpp --lib Lib/shared/tty.vml

#param lib("tty")
#include <tty.h>

#define ROWS 25

// 在第 (x,y) 处用指定前景/背景打一串（打完颜色恢复成 7/0）
static void say(int x, int y, int fg, int bg, char *s)
{
    tty_print_at(x, y, s, fg, bg);
}

int main()
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
    tty_puts("WayCoder  tty_* demo (C++)  --  color / cursor / box / int");

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
    tty_goto(1, ROWS - 1);
    tty_puts(" tty_* demo done -- no key wait, exits by itself                              ");
    tty_color(8, 0);
    tty_goto(3, ROWS);
    tty_puts("demo_tty (C++) 结束 —— 画完即退出");

    tty_color(7, 0);
    return 0;
}
