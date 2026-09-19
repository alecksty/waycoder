#include <stdio.h>
int g;
void wrp(int* p) { *p = 77; }            /* 通过指针形参写回 */
void rdv(void)
{
    int a;
    int* q = &a;                          /* 取局部地址 */
    a = 5;
    printf("LOC-ADDR-OK=%d\n", (int)(q == &a));
    printf("LOC-RD=%d\n", *q);
    *q = 9;
    printf("LOC-WR=%d\n", a);
}
void par(int v)
{
    int* p = &v;                          /* 取**形参**地址 */
    printf("PAR-RD=%d\n", *p);
    *p = 123;
    printf("PAR-WR=%d\n", v);
}
int main(void)
{
    int x;
    x = 1; wrp(&x); printf("PTR-OUT=%d\n", x);
    *(&g) = 3;            printf("GLB-WR=%d\n", g);
    rdv();
    par(42);
    return 0;
}
