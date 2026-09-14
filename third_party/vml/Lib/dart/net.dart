// VML 网络 Socket 扩展库 — Dart
// OS 模式专用

int netCreate(int domain, int typ) {
    asm("SYSCALL 330");
    return 0;
}

int netBind(int fd, int port) {
    asm("SYSCALL 331");
    return 0;
}

int netListen(int fd, int backlog) {
    asm("SYSCALL 332");
    return 0;
}

int netAccept(int fd) {
    asm("SYSCALL 333");
    return 0;
}

int netConnect(String host, int port) {
    asm("SYSCALL 334");
    return 0;
}

int netSend(int fd, String data, int length) {
    asm("SYSCALL 335");
    return 0;
}

int netRecv(int fd, String buf, int maxLen) {
    asm("SYSCALL 336");
    return 0;
}

int netClose(int fd) {
    asm("SYSCALL 337");
    return 0;
}

int netDnsResolve(String hostname) {
    asm("SYSCALL 338");
    return 0;
}
