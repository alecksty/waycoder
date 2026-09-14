// VML 文件系统操作扩展库 — Objective-C
// SYSCALL 340-344 (OS mode)

int fs_mkdir(const char *path) {
    asm("SYSCALL 340");
    return 0;
}

int fs_remove(const char *path) {
    asm("SYSCALL 341");
    return 0;
}

int fs_rename(const char *old_path, const char *new_path) {
    asm("SYSCALL 342");
    return 0;
}

int fs_readdir(const char *path, void *buffer) {
    asm("SYSCALL 343");
    return 0;
}

int fs_stat(const char *path, void *info) {
    asm("SYSCALL 344");
    return 0;
}
