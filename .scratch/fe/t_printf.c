#include <stdio.h>

int main()
{
    char b[32];
    puts("A");
    sprintf(b, "d=[%d]", 42);
    puts(b);
    sprintf(b, "s=[%s]", "abc");
    puts(b);
    printf("pf %d\n", 7);
    return 0;
}
