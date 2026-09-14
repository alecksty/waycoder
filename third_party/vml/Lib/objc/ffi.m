// VML FFI 动态库调用扩展库 — Objective-C
// SYSCALL 370-375 (OS mode)

int dl_open(const char *path) {
    asm("SYSCALL 370");
    return 0;
}

int dl_sym(int handle, const char *name) {
    asm("SYSCALL 371");
    return 0;
}

int dl_close(int handle) {
    asm("SYSCALL 372");
    return 0;
}

int native_call(int func_id, const int *args, int count, int flags) {
    asm("SYSCALL 373");
    return 0;
}

float native_call_f(int func_id, const float *args, int count, int flags) {
    asm("SYSCALL 375");
    return 0.0;
}

int get_platform(void) {
    asm("SYSCALL 374");
    return 0;
}
