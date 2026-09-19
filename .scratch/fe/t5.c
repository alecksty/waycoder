#include <stdio.h>
int f(int a, int b, int c)
{
    int *p = &b;
    return (p[0] == b) + (p[1] == c) * 2 + (p[-1] == a) * 4;   /* 期望 7 */
}
int loc(void)
{
    int x, y, z;
    int *p = &y;
    x = 1; y = 2; z = 3;
    return (p[0] == y) + (p[1] == z) * 2 + (p[-1] == x) * 4;   /* 期望 7 */
}
int main(void)
{
    printf("P=%d\n", f(11, 22, 33));
    printf("L=%d\n", loc());
    return 0;
}
