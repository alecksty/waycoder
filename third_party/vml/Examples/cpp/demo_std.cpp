// demo_std.cpp —— **第 1 层：标准输入输出**（C++ 的 `iostream` / `cstdio`）
//
// 这一层是每门语言自己的"往标准输出写文本"。C++ 有两条路，本 demo 两条都走：
//
//   · `std::cout << …` —— C++ 自己的那套（`endl` 刷换行）
//   · `printf`         —— 从 C 继承来的（`Lib/c/stdio.h`，C++ 也能 include）
//
// ## ⚠ 走之前先量过：C++ 前端这条路有四个洞，本 demo 全部避开
//
// 本文件是先写探针跑出来的，不是照想象写的。实测（2026-09-24，
// `vmlcli Examples/cpp/demo_std.cpp`）：
//
//   ① **`cout << <char* 变量>` 打的是地址不是字符串**
//      `char *s = "hello"; cout << s;` → `1074`；而 `cout << "hello"`（字面量）正常。
//      所以本 demo 里凡是要印字符串变量，一律走 `printf("%s", s)`（那条是好的）。
//   ② **`printf` 的浮点转换打 0**（`%f` → `0.000000`），C 侧同样如此。
//      所以这一层**不演示浮点**。
//   ③ **`printf` 的 `%%` 会原样打出两个百分号**（C 侧正常打印一个 `%`）。
//      本 demo 不用 `%%`。
//   ④ **`std::string` 整体不可用**：`string t = "abc"; cout << t;` 打 `1066`、
//      `t.length()` 打 `6513249`；`t + "def"` 连链接都过不去（`未定义的函数 'str_concat'`）。
//      所以字符串一律用 `char *`，不用 `std::string`。
//
//   ⑤ **`cout << (a+b)` 里的括号表达式被丢掉**（`cout << a + b` 也一样）——
//      打出来的那一格是空的。先把值存进变量再 `cout << 变量` 就正常。
//
//   另外两条**类**相关的（本 demo 用不到，一并记下）：
//      · 带参构造 `Point p(3, 4);` 解析期就报错（`期望 RPAREN，实际得到 NUMBER`）
//      · 类的方法定义了但**不生成函数体**（`未定义的函数 'method_p_sum'`）
//   所以 C++ 侧的例程一律写成**自由函数 + 一个全局数组/结构体**（与 `cpp/snake.cpp` 同一路子）。
//
// ## 判据
//
//     vmlcli Examples/cpp/demo_std.cpp
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。

#include <stdio.h>
#include <iostream>
using namespace std;

// 自由函数：C++ 前端对"类的方法"不生成函数体，所以工具函数一律这么写
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

    // ── 1. cout：字面量与整数都正常（⚠ 别放 char* 变量，见文件头 ①）──
    cout << "=== demo_std (C++) ===" << endl;
    cout << "cout: 字面量 + 整数 " << a << " " << b << endl;
    int total = a + b;              // ⚠ `cout << (a+b)` 打不出东西（第五个洞：
    cout << "cout: 表达式 " << total << endl;   //   括号里的表达式在 << 链里被丢掉）

    // ── 2. printf：字符串变量走这条（`cout` 那条会打地址）──
    char *tag = "printf: char* 变量";
    printf("%s ok\n", tag);

    // ── 3. 函数调用（证明语言本身是通的）──
    printf("square(9) = %d\n", square(9));
    printf("digitname: %s %s %s %s\n",
           digitname(0), digitname(1), digitname(2), digitname(9));

    // ── 4. 整数算术与进制 ──
    printf("a=%d b=%d\n", a, b);
    printf("a+b=%d a-b=%d a*b=%d\n", a + b, a - b, a * b);
    printf("a/b=%d a%%b=%d\n", a / b, a % b);
    printf("十进制=%d 十六进制=%x 八进制=%o\n", 255, 255, 255);

    // ── 5. 宽度对齐（老程序靠它排表格）──
    printf("宽度：[%5d][%-5d][%05d]\n", 42, 42, 42);

    // ── 6. 数组 + 循环 ──
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
    for (i = 1; i <= 5; i++) {
        printf("%d x 7 = %2d\n", i, i * 7);
    }

    cout << "=== 完成 ===" << endl;
    return 0;
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (C++) ===
// cout: 字面量 + 整数 17 25
// cout: 表达式 42
// printf: char* 变量 ok
// square(9) = 81
// digitname: zero one two many
// a=17 b=25
// a+b=42 a-b=-8 a*b=425
// a/b=0 a%b=17
// 十进制=255 十六进制=ff 八进制=377
// 宽度：[   42][42   ][00042]
// 1^2+...+6^2 = 91
// 1 x 7 =  7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// ────────────────────────────────────────────────────────────────
