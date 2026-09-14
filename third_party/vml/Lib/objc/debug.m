// VML 调试扩展库 — Objective-C
// SYSCALL 70-72

void debug_print(const char *str) {
    asm("SYSCALL 70");
}

void debug_print_int(int n) {
    asm("SYSCALL 71");
}

void assert(int condition, const char *message) {
    asm("SYSCALL 72");
}
