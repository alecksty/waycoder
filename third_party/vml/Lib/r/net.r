# VML 网络 Socket 扩展库 — R (OS 模式)

create <- function(domain, type) {
    asm("SYSCALL 330")
    0
}

bind <- function(fd, port) {
    asm("SYSCALL 331")
    0
}

listen <- function(fd, backlog) {
    asm("SYSCALL 332")
    0
}

accept <- function(fd) {
    asm("SYSCALL 333")
    0
}

connect <- function(host, port) {
    asm("SYSCALL 334")
    0
}

send <- function(fd, data, length) {
    asm("SYSCALL 335")
    0
}

recv <- function(fd, buf_size) {
    asm("SYSCALL 336")
    0
}

close <- function(fd) {
    asm("SYSCALL 337")
    0
}

dns_resolve <- function(hostname) {
    asm("SYSCALL 338")
    0
}
