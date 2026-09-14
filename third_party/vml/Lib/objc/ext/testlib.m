// VML testlib 静态绑定 — Objective-C
// 用法: #include "ext/testlib.m"

int add(int a, int b) {
    asm("SYSCALL 373");
    return 0;
}

int mul(int x, int y) {
    asm("SYSCALL 373");
    return 0;
}

void say_hello(void) {
    asm("SYSCALL 373");
}
