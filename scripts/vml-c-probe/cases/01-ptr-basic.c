// 指针基础：char* 形参 / 指针算术 / 数组退化 / 取地址
//
// 这是"给手机版写 curses 库"最底层的一组写法。此前 `vml-out-probe` / `vml-abi-probe`
// 用的都是十几行的直筒程序，一个指针都没碰 —— 所以这条链上有没有坑，此前无人知道。
#include <stdio.h>
#include <string.h>

int addall(int * a, int n)
{
    int i;
    int s = 0;
    for (i = 0; i < n; i++) {
        s = s + a[i];
    }
    return s;
}

int main()
{
    char * s = "abc";
    printf("P1=%s\n", s);
    printf("P2=%c\n", *(s + 1));

    char buf[16];
    strcpy(buf, "xyz");
    printf("P3=%s\n", buf);
    printf("P4=%d\n", strlen(buf));

    int a[3];
    a[0] = 3;
    a[1] = 4;
    a[2] = 5;
    printf("P5=%d\n", addall(a, 3));

    int v = 42;
    int * p = &v;
    printf("P6=%d\n", *p);
    return 0;
}
// EXPECT: P1=abc|P2=b|P3=xyz|P4=3|P5=12|P6=42
