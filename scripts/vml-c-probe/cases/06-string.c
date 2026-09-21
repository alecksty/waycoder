// 字符串函数 / sprintf / switch / 递归
//
// curses 库内部要拼状态栏、要按字符串分派键名（`keyname()` 那张表）、
// 属性名解析也要 `strcmp` 链 —— 这几条都是常用件。
#include <stdio.h>
#include <string.h>

int fib(int n)
{
    if (n < 2) return n;
    return fib(n - 1) + fib(n - 2);
}

char * name(int k)
{
    switch (k) {
        case 0: return "zero";
        case 1: return "one";
        default: return "many";
    }
}

int main()
{
    char buf[32];
    strcpy(buf, "ab");
    strcat(buf, "cd");
    printf("T1=%s\n", buf);
    printf("T2=%d\n", strcmp(buf, "abcd"));
    printf("T3=%d\n", strcmp(buf, "abce"));

    // ⚠ `sprintf` 单独一条用例钉着（`08-sprintf.c`，已知缺陷：末尾多吐一个格式字符）——
    // 混在这里会让 T1~T3/T5/T6 那几条能用的断言失去回归保护。
    printf("T5=%d\n", fib(10));
    printf("T6=%s\n", name(1));
    return 0;
}
// EXPECT: T1=abcd|T2=0|T3=-1|T5=55|T6=one
