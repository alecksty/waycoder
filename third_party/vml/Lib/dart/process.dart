// VML 进程扩展库 — Dart (OS 模式)

int exec(String path) {
    asm("SYSCALL 320");
    return 0;
}

int getPid() {
    asm("SYSCALL 322");
    return 0;
}
