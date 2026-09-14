# VML 互斥锁扩展库 — R (OS 模式)

mutex_create <- function() {
    asm("SYSCALL 310")
    0
}

mutex_lock <- function(mutex_id) {
    asm("SYSCALL 311")
    0
}

mutex_unlock <- function(mutex_id) {
    asm("SYSCALL 312")
    0
}
