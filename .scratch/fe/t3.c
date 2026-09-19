#include <stdio.h>
char b[64];
void dump(void)
{
    int i;
    for (i = 0; i < 8; i++) { printf("%d ", b[i]); }
    printf("\n");
}
void zero(void) { int i; for (i = 0; i < 64; i++) b[i] = 0; }
int main()
{
    zero(); sprintf(b, "%d", 42);   puts("A"); dump();
    zero(); sprintf(b, "x");        puts("B"); dump();
    zero(); sprintf(b, "%s", "abc");puts("C"); dump();
    zero(); sprintf(b, "ab%dcd", 9);puts("D"); dump();
    return 0;
}
