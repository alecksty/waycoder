# VML FFI 动态库调用扩展库 — R (OS 模式)

dl_open <- function(path) {
    asm("SYSCALL 370")
    0
}

dl_sym <- function(handle, name) {
    asm("SYSCALL 371")
    0
}

dl_close <- function(handle) {
    asm("SYSCALL 372")
    0
}

native_call <- function(func_id, args_ptr, count, flags) {
    asm("SYSCALL 373")
    0
}

get_platform <- function() {
    asm("SYSCALL 374")
    0
}

native_call_f <- function(func_id, fargs_ptr, count, flags) {
    asm("SYSCALL 375")
    0
}
