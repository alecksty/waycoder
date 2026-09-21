// 函数指针 / enum / 二维数组 / static 函数
//
// curses 用到函数指针的地方不多，但 `enum`（`A_BOLD`/`COLOR_PAIR` 那些都是宏或枚举）
// 与二维数组（屏缓冲按 `buf[y][x]` 寻址，虽然多数实现走一维展平）很常见。
#include <stdio.h>

enum color { RED, GREEN, BLUE };

static int twice(int n)
{
    return n * 2;
}

int apply(int (*f)(int), int v)
{
    return f(v);
}

int main()
{
    printf("F1=%d\n", RED + BLUE);
    printf("F2=%d\n", twice(21));
    printf("F3=%d\n", apply(twice, 5));

    int g[2][3];
    g[0][0] = 1;
    g[1][2] = 6;
    printf("F4=%d\n", g[0][0] + g[1][2]);
    return 0;
}
// EXPECT: F1=2|F2=42|F3=10|F4=7
