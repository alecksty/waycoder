// VML 网络 Socket 扩展库 — JavaScript
// OS 模式专用，需显式 require("net")

var net = {
    create: function(domain, type) {
        asm("SYSCALL #330");
        return 0;
    },
    bind: function(fd, port) {
        asm("SYSCALL #331");
        return 0;
    },
    listen: function(fd, backlog) {
        asm("SYSCALL #332");
        return 0;
    },
    accept: function(fd) {
        asm("SYSCALL #333");
        return 0;
    },
    connect: function(host, port) {
        asm("SYSCALL #334");
        return 0;
    },
    send: function(fd, data, len) {
        asm("SYSCALL #335");
        return 0;
    },
    recv: function(fd, buf, maxLen) {
        asm("SYSCALL #336");
        return 0;
    },
    close: function(fd) {
        asm("SYSCALL #337");
        return 0;
    },
    dnsResolve: function(hostname) {
        asm("SYSCALL #338");
        return 0;
    }
};
