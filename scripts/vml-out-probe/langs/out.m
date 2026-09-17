// out.m —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
#include <stdio.h>
int main() {
    printf("OUT-STR=abc
");
    printf("%d
", 42);
    printf("OUT-PUN=hello, world
");
    return 0;
}
