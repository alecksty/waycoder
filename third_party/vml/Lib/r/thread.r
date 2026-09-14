# VML 线程扩展库 — R (OS 模式)

thread_create <- function(entry, stack_size) {
    asm("SYSCALL 300")
    0
}

thread_exit <- function() {
    asm("SYSCALL 301")
}

thread_join <- function(tid) {
    asm("SYSCALL 302")
    0
}

thread_yield <- function() {
    asm("SYSCALL 303")
    0
}
