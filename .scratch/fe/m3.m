#include <waycoder_ui.h>
#include <stdio.h>
int main(void)
{
    char b[64];
    printf("M-INT=%d\n", 42);
    sprintf(b, "M-S=%s", "abc");
    puts(b);
    return 0;
}
