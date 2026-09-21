// 变参：**能不能实现 curses 的 `printw`/`mvprintw` 家族**，就看这一条
//
// 调用约定是「前 4 个参数走寄存器、第 5 个起压栈」，而 `va_start(ap, last)` 的代码生成
// 是**纯按"最后一个固定参数的地址 +4"**算的（`CodeGenerator.Expressions.Calls.cs`）。
// 所以它能不能取到值，取决于那个地址后面**是不是真的接着栈上的变参** —— 与固定参数个数有关。
//
// 实测两种形状（这正是判据要钉的边界）：
//   · `f(int a, int b, char * fmt, ...)` —— **3 个固定**，变参是第 4 个起 ⇒ **可用** ✅
//     这就是 `mvprintw(y, x, fmt, ...)` 的形状，所以 curses 的格式化输出**能实现**
//   · `f(int n, ...)` —— **1 个固定** ⇒ 取到 0 ❌（`&n+4` 落在局部变量区而不是栈参区）
//
// ⇒ 写库时的规矩：**变参函数的固定参数不要少于 3 个**；不够就补一个占位参数。
#include <stdio.h>
#include <stdarg.h>

/* curses 的 `mvprintw(int y, int x, const char *fmt, ...)` 同形 */
int fmt3(int a, int b, char * f, ...)
{
    va_list ap;
    int v;
    va_start(ap, f);
    v = va_arg(ap, int);
    va_end(ap);
    return v;
}

/* 4 个固定 + 两个变参（第 5 个起压栈） */
int sum4(int a, int b, int c, int d, ...)
{
    va_list ap;
    int s;
    va_start(ap, d);
    s = va_arg(ap, int) + va_arg(ap, int);
    va_end(ap);
    return a + b + c + d + s;
}

int main()
{
    printf("X1=%d\n", fmt3(1, 2, "x", 99));
    printf("X2=%d\n", sum4(1, 2, 3, 4, 10, 20));
    return 0;
}
// EXPECT: X1=99|X2=40
