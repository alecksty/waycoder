// VML 互斥锁扩展库 — Dart (OS 模式)

int mutexCreate() {
    asm("SYSCALL 310");
    return 0;
}

int mutexLock(int id) {
    asm("SYSCALL 311");
    return 0;
}

int mutexUnlock(int id) {
    asm("SYSCALL 312");
    return 0;
}
