# VML 环境变量扩展库 — R (OS 模式)

get_env <- function(name) {
    asm("SYSCALL 360")
    0
}

set_env <- function(name, value) {
    asm("SYSCALL 361")
    0
}

get_args <- function(buffer) {
    asm("SYSCALL 362")
    0
}
