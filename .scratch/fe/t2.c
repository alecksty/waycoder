#include <stdio.h>

int main()
{
    char b[64];
    puts("--1--"); sprintf(b, "%d", 42);        puts(b);
    puts("--2--"); sprintf(b, "x");             puts(b);
    puts("--3--"); sprintf(b, "%d", 7);         puts(b);
    puts("--4--"); printf("%d\n", 42);
    puts("--5--"); sprintf(b, "%s", "abc");     puts(b);
    puts("--6--"); b[0] = 'A'; b[1] = 'B'; b[2] = 0; puts(b);
    return 0;
}
