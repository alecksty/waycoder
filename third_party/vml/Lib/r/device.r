# VML 设备 I/O 扩展库 — R

# 键盘
kb_hit <- function() {
    asm("SYSCALL 83")
    0
}

kb_getch <- function() {
    asm("SYSCALL 84")
    0
}

# 鼠标
mouse_get_x <- function() {
    asm("SYSCALL 85")
    0
}

mouse_get_y <- function() {
    asm("SYSCALL 86")
    0
}

mouse_left <- function() {
    asm("SYSCALL 87")
    0
}

mouse_right <- function() {
    asm("SYSCALL 88")
    0
}

# 统一设备接口
dev_open <- function(name) {
    asm("SYSCALL 100")
    0
}

dev_close <- function(handle) {
    asm("SYSCALL 101")
    0
}

dev_read <- function(handle, buf, count) {
    asm("SYSCALL 102")
    0
}

dev_write <- function(handle, buf, count) {
    asm("SYSCALL 103")
    0
}

dev_control <- function(handle, command, data, length) {
    asm("SYSCALL 104")
    0
}
