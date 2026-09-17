// nat.c —— 测**这门语言自己的**标准输出函数（与 out.c 的共享库那条路分开）
#include <stdio.h>
int main() {
    printf("OUT-STR=abc
");
    printf("OUT-INT=%d
", 42);
    printf("OUT-PUN=hello, world
");
    return 0;
}
