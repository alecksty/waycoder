# VML 调试扩展库 — R

debug_print <- function(s) {
    asm("SYSCALL 70")
}

debug_print_int <- function(n) {
    asm("SYSCALL 71")
}

assert <- function(condition, message) {
    asm("SYSCALL 72")
}
