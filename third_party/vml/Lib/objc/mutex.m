// VML 互斥锁扩展库 — Objective-C
// SYSCALL 310-312 (OS mode)

int mutex_create(void) {
    asm("SYSCALL 310");
    return 0;
}

int mutex_lock(int mutex_id) {
    asm("SYSCALL 311");
    return 0;
}

int mutex_unlock(int mutex_id) {
    asm("SYSCALL 312");
    return 0;
}
