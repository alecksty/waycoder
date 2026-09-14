# VML 条件变量扩展库 — R (OS 模式)

cond_create <- function() {
    asm("SYSCALL 313")
    0
}

cond_wait <- function(cond_id, mutex_id) {
    asm("SYSCALL 314")
    0
}

cond_signal <- function(cond_id) {
    asm("SYSCALL 315")
    0
}

cond_broadcast <- function(cond_id) {
    asm("SYSCALL 316")
    0
}
