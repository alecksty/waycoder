# VML 文件操作扩展库 — R

fopen <- function(name, mode) {
    asm("SYSCALL 110")
    0
}

fclose <- function(handle) {
    asm("SYSCALL 111")
    0
}

fread <- function(handle, buf, count) {
    asm("SYSCALL 112")
    0
}

fwrite <- function(handle, buf, count) {
    asm("SYSCALL 113")
    0
}

fseek <- function(handle, offset) {
    asm("SYSCALL 114")
    0
}

ftell <- function(handle) {
    asm("SYSCALL 114")
    0
}
