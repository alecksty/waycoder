// VML 网络 Socket 扩展库 — Objective-C
// SYSCALL 330-338 (OS mode)

int net_create(int domain, int type) {
    asm("SYSCALL 330");
    return 0;
}

int net_bind(int fd, int port) {
    asm("SYSCALL 331");
    return 0;
}

int net_listen(int fd, int backlog) {
    asm("SYSCALL 332");
    return 0;
}

int net_accept(int fd) {
    asm("SYSCALL 333");
    return 0;
}

int net_connect(const char *host, int port) {
    asm("SYSCALL 334");
    return 0;
}

int net_send(int fd, const void *data, int len) {
    asm("SYSCALL 335");
    return 0;
}

int net_recv(int fd, void *buf, int max_len) {
    asm("SYSCALL 336");
    return 0;
}

int net_close(int fd) {
    asm("SYSCALL 337");
    return 0;
}

int dns_resolve(const char *hostname) {
    asm("SYSCALL 338");
    return 0;
}
