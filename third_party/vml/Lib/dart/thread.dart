// VML 线程扩展库 — Dart (OS 模式)

int threadCreate(int entry, int stackSize) {
    asm("SYSCALL 300");
    return 0;
}

void threadExit() {
    asm("SYSCALL 301");
}

int threadJoin(int tid) {
    asm("SYSCALL 302");
    return 0;
}

int threadYield() {
    asm("SYSCALL 303");
    return 0;
}
