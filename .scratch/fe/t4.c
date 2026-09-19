#include <stdio.h>
char b[64];
int main()
{
    sprintf(b, "ab%dcd", 9);
    puts(b);
    return 0;
}
