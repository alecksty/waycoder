// VML 条件变量扩展库 — Dart (OS 模式)

int condCreate() {
    asm("SYSCALL 313");
    return 0;
}

int condWait(int condId, int mutexId) {
    asm("SYSCALL 314");
    return 0;
}

int condSignal(int condId) {
    asm("SYSCALL 315");
    return 0;
}

int condBroadcast(int condId) {
    asm("SYSCALL 316");
    return 0;
}
