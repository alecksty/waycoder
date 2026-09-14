// VML 条件变量扩展库 — Objective-C
// SYSCALL 313-316 (OS mode)

int cond_create(void) {
    asm("SYSCALL 313");
    return 0;
}

int cond_wait(int cond_id, int mutex_id) {
    asm("SYSCALL 314");
    return 0;
}

int cond_signal(int cond_id) {
    asm("SYSCALL 315");
    return 0;
}

int cond_broadcast(int cond_id) {
    asm("SYSCALL 316");
    return 0;
}
