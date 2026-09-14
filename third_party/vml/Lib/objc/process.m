// VML 进程管理扩展库 — Objective-C
// SYSCALL 320-322 (OS mode)

int exec(const char *path) {
    asm("SYSCALL 320");
    return 0;
}

int get_pid(void) {
    asm("SYSCALL 322");
    return 0;
}
