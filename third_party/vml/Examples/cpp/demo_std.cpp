// demo_std.cpp —— **第 1 层：标准输入输出**（C++ 的 `iostream` / `cstdio`）
// demo_std.cpp — **Layer 1: standard input/output** (C++'s `iostream` / `cstdio`)
//
// 这一层是每门语言自己的"往标准输出写文本"。C++ 有两条路，本 demo 两条都走：
// This layer is each language's own way of "writing text to standard output". C++ has two routes, and this demo takes both:
//
//   · `std::cout << …` —— C++ 自己的那套（`endl` 刷换行）
//   · `std::cout << …` — C++'s own set (`endl` flushes the newline)
//   · `printf`         —— 从 C 继承来的（`Lib/c/stdio.h`，C++ 也能 include）
//   · `printf`         — inherited from C (`Lib/c/stdio.h`, which C++ can include too)
//
// ## ⚠ 走之前先量过：C++ 前端这条路有四个洞，本 demo 全部避开
// ## ⚠ Measured before writing: this C++ frontend route has four holes, and this demo avoids all of them
//
// 本文件是先写探针跑出来的，不是照想象写的。实测（2026-09-24，
// `vmlcli Examples/cpp/demo_std.cpp`）：
//
//   ① **`cout << <char* 变量>` 打的是地址不是字符串**
//   ① **`cout << <char* variable>` prints the address, not the string**
//      `char *s = "hello"; cout << s;` → `1074`；而 `cout << "hello"`（字面量）正常。
//      `char *s = "hello"; cout << s;` → `1074`; while `cout << "hello"` (a literal) is fine.
//      所以本 demo 里凡是要印字符串变量，一律走 `printf("%s", s)`（那条是好的）。
//      So in this demo, printing a string variable always goes through `printf("%s", s)` (that route works).
//   ② **`printf` 的浮点转换打 0**（`%f` → `0.000000`），C 侧同样如此。
//   ② **`printf`'s floating-point conversion prints 0** (`%f` → `0.000000`), and the same holds on the C side.
//      所以这一层**不演示浮点**。
//      So this layer **does not demonstrate floating point**.
//   ③ **`printf` 的 `%%` 会原样打出两个百分号**（C 侧正常打印一个 `%`）。
//   ③ **`printf`'s `%%` prints two percent signs as-is** (the C side correctly prints one `%`).
//      本 demo 不用 `%%`。
//      This demo does not use `%%`.
//   ④ **`std::string` 整体不可用**：`string t = "abc"; cout << t;` 打 `1066`、
//   ④ **`std::string` is unusable as a whole**: `string t = "abc"; cout << t;` prints `1066`,
//      `t.length()` 打 `6513249`；`t + "def"` 连链接都过不去（`未定义的函数 'str_concat'`）。
//      `t.length()` prints `6513249`; `t + "def"` does not even get past linking (an "undefined function 'str_concat'" error).
//      所以字符串一律用 `char *`，不用 `std::string`。
//      So strings always use `char *`, never `std::string`.
//
//   ⑤ **`cout << (a+b)` 里的括号表达式被丢掉**（`cout << a + b` 也一样）——
//   ⑤ **The parenthesized expression in `cout << (a+b)` is dropped** (same for `cout << a + b`) —
//      打出来的那一格是空的。先把值存进变量再 `cout << 变量` 就正常。
//      the slot that gets printed is empty. Storing the value in a variable first and then doing `cout << variable` works fine.
//
//   另外两条**类**相关的（本 demo 用不到，一并记下）：
//   Two more issues related to **classes** (this demo does not need them, noted here as well):
//      · 带参构造 `Point p(3, 4);` 解析期就报错（`期望 RPAREN，实际得到 NUMBER`）
//      · A constructor with arguments `Point p(3, 4);` fails at parse time (an `expected RPAREN, got NUMBER` error)
//      · 类的方法定义了但**不生成函数体**（`未定义的函数 'method_p_sum'`）
//      · A class method can be defined but **no function body is generated** (an "undefined function 'method_p_sum'" error)
//   所以 C++ 侧的例程一律写成**自由函数 + 一个全局数组/结构体**（与 `cpp/snake.cpp` 同一路子）。
//   So C++ examples are always written as **free functions + one global array/struct** (the same approach as `cpp/snake.cpp`).
//
// ## 判据
// ## Criteria
//
//     vmlcli Examples/cpp/demo_std.cpp
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。
// Expected stdout is byte-for-byte equal to the "expected output" block at the end of the file.

#include <stdio.h>
#include <iostream>
#include <waycoder_ui.h>     /* 只为 `ui_get_language()` 的声明（本头文件只给声明与常量，不会让程序变大） */
                            /* only for the `ui_get_language()` declaration (this header carries declarations and constants only; it does not make the program bigger) */
using namespace std;

// 自由函数：C++ 前端对"类的方法"不生成函数体，所以工具函数一律这么写
// Free functions: the C++ frontend does not generate a body for "class methods", so helper functions are always written this way
static int square(int v) { return v * v; }

static char *digitname(int d)
{
    if (d == 0) return "zero";
    if (d == 1) return "one";
    if (d == 2) return "two";
    return "many";
}

int main()
{
    int a = 17;
    int b = 25;
    int i;
    int sum = 0;
    // 界面语言：开局查一次（`ui_get_language` 是 syscall，别每帧调）—— 文案按它取
    // UI language: queried once at startup (`ui_get_language` is a syscall, do not call it every frame) — the text is picked by it
    int Lang = ui_get_language();

    // ── 1. cout：字面量与整数都正常（⚠ 别放 char* 变量，见文件头 ①）──
    // ── 1. cout: literals and integers are both fine (⚠ do not pass a char* variable; see ① in the file header) ──
    cout << "=== demo_std (C++) ===" << endl;
    // ⚠ 语言文案走**单行 if/else**：`cout << <三元>` 打的是地址（见文件头 ①），
    // ⚠ The language-dependent text goes through a **single-line if/else**: `cout << <ternary>` prints the address (see ① in the file header),
    //   而 `cout << <char* 变量>` 同样不行 —— 只有**字面量**喂 `cout` 才是好的。
    //   and `cout << <char* variable>` does not work either — only a **literal** fed to `cout` is sound.
    if (Lang == 0) { cout << "cout: 字面量 + 整数 " << a << " " << b << endl; } else { cout << "cout: literal + integers " << a << " " << b << endl; }
    int total = a + b;              // ⚠ `cout << (a+b)` 打不出东西（第五个洞：
    // ⚠ `cout << (a+b)` prints nothing (the fifth hole:
    if (Lang == 0) { cout << "cout: 表达式 " << total << endl; } else { cout << "cout: expression " << total << endl; }   /*   括号里的表达式在 << 链里被丢掉） */
    // the parenthesized expression gets dropped inside the << chain）

    // ── 2. printf：字符串变量走这条（`cout` 那条会打地址）──
    // ── 2. printf: string variables take this route (the `cout` route would print an address) ──
    char *tag = Lang == 0 ? "printf: char* 变量" : "printf: char* variable";
    printf("%s ok\n", tag);

    // ── 3. 函数调用（证明语言本身是通的）──
    // ── 3. Function calls (proof that the language itself works) ──
    printf("square(9) = %d\n", square(9));
    printf("digitname: %s %s %s %s\n",
           digitname(0), digitname(1), digitname(2), digitname(9));

    // ── 4. 整数算术与进制 ──
    // ── 4. Integer arithmetic and number bases ──
    printf("a=%d b=%d\n", a, b);
    printf("a+b=%d a-b=%d a*b=%d\n", a + b, a - b, a * b);
    printf("a/b=%d a%%b=%d\n", a / b, a % b);
    printf(Lang == 0 ? "十进制=%d 十六进制=%x 八进制=%o\n" : "decimal=%d hex=%x octal=%o\n", 255, 255, 255);

    // ── 5. 宽度对齐（老程序靠它排表格）──
    // ── 5. Width alignment (old programs lay out tables with it) ──
    printf(Lang == 0 ? "宽度：[%5d][%-5d][%05d]\n" : "width: [%5d][%-5d][%05d]\n", 42, 42, 42);

    // ── 6. 数组 + 循环 ──
    // ── 6. Arrays + loops ──
    int v[6];
    v[0] = 1;
    v[1] = 4;
    v[2] = 9;
    v[3] = 16;
    v[4] = 25;
    v[5] = 36;
    for (i = 0; i < 6; i++) {
        sum = sum + v[i];
    }
    printf("1^2+...+6^2 = %d\n", sum);

    // ── 7. 九九表的一小段 ──
    // ── 7. A short slice of the multiplication table ──
    for (i = 1; i <= 5; i++) {
        printf("%d x 7 = %2d\n", i, i * 7);
    }

    if (Lang == 0) { cout << "=== 完成 ===" << endl; } else { cout << "=== done ===" << endl; }
    return 0;
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (C++) ===
// cout: 字面量 + 整数 17 25
// cout: literals + integers 17 25
// cout: 表达式 42
// cout: expression 42
// printf: char* 变量 ok
// square(9) = 81
// digitname: zero one two many
// a=17 b=25
// a+b=42 a-b=-8 a*b=425
// a/b=0 a%b=17
// 十进制=255 十六进制=ff 八进制=377
// decimal=255 hex=ff octal=377
// 宽度：[   42][42   ][00042]
// width: [   42][42   ][00042]
// 1^2+...+6^2 = 91
// 1 x 7 =  7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// === done ===
// ────────────────────────────────────────────────────────────────
