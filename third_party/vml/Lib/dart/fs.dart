// VML 文件系统扩展库 — Dart (OS 模式)

int mkdir(String path) {
    asm("SYSCALL 340");
    return 0;
}

int remove(String path) {
    asm("SYSCALL 341");
    return 0;
}

int rename(String oldPath, String newPath) {
    asm("SYSCALL 342");
    return 0;
}

int readdir(String path, String buffer) {
    asm("SYSCALL 343");
    return 0;
}

int stat(String path, String info) {
    asm("SYSCALL 344");
    return 0;
}
