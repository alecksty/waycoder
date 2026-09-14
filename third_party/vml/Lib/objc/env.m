// VML 环境变量扩展库 — Objective-C
// SYSCALL 360-362 (OS mode)

const char *get_env(const char *name) {
    asm("SYSCALL 360");
    return 0;
}

int set_env(const char *name, const char *value) {
    asm("SYSCALL 361");
    return 0;
}

int get_args(void *buffer) {
    asm("SYSCALL 362");
    return 0;
}
