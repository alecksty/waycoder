// 结构体：定义 / 指针 + `->` / 结构体数组 / 嵌套 / 传指针
//
// curses 的 `WINDOW` 就是一个结构体（行列数、光标、属性、缓冲区指针…），
// 而 `initscr()` 返回 `WINDOW *`、`newwin()` 返回 `WINDOW *` —— 库的每个函数都在解引用它。
#include <stdio.h>

struct pt {
    int x;
    int y;
};

struct win {
    int w;
    int h;
    struct pt org;
};

int area(struct win * p)
{
    return p->w * p->h;
}

int main()
{
    struct pt a;
    a.x = 3;
    a.y = 4;
    printf("S1=%d\n", a.x + a.y);

    struct pt * pa = &a;
    printf("S2=%d\n", pa->x * pa->y);

    struct pt arr[3];
    arr[0].x = 7;
    arr[2].y = 9;
    printf("S3=%d\n", arr[0].x + arr[2].y);

    struct win w;
    w.w = 5;
    w.h = 6;
    w.org.x = 1;
    printf("S4=%d\n", area(&w));
    printf("S5=%d\n", w.org.x);
    return 0;
}
// EXPECT: S1=7|S2=12|S3=16|S4=30|S5=1
