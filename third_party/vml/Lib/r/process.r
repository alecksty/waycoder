# VML 进程扩展库 — R (OS 模式)

exec <- function(path) {
    asm("SYSCALL 320")
    0
}

get_pid <- function() {
    asm("SYSCALL 322")
    0
}
