# VML 文件系统扩展库 — R (OS 模式)

fs_mkdir <- function(path) {
    asm("SYSCALL 340")
    0
}

fs_remove <- function(path) {
    asm("SYSCALL 341")
    0
}

fs_rename <- function(old_path, new_path) {
    asm("SYSCALL 342")
    0
}

fs_readdir <- function(path, buffer) {
    asm("SYSCALL 343")
    0
}

fs_stat <- function(path, info) {
    asm("SYSCALL 344")
    0
}
