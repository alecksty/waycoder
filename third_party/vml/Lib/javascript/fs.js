// VML 文件系统扩展库 — JavaScript (OS 模式)
// 需显式 import fs

function fs_mkdir(path) {
    asm("SYSCALL 340");
    return 0;
}

function fs_remove(path) {
    asm("SYSCALL 341");
    return 0;
}

function fs_rename(oldPath, newPath) {
    asm("SYSCALL 342");
    return 0;
}

function fs_readdir(path, buffer) {
    asm("SYSCALL 343");
    return 0;
}

function fs_stat(path, info) {
    asm("SYSCALL 344");
    return 0;
}
