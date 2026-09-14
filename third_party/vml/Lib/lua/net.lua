-- VML 网络 Socket 扩展库 — Lua
-- OS 模式专用，需显式 require("net")

net = {}
function net.create(domain, typ)
    asm("SYSCALL 330")
    return 0
end
function net.bind(fd, port)
    asm("SYSCALL 331")
    return 0
end
function net.listen(fd, backlog)
    asm("SYSCALL 332")
    return 0
end
function net.accept(fd)
    asm("SYSCALL 333")
    return 0
end
function net.connect(host, port)
    asm("SYSCALL 334")
    return 0
end
function net.send(fd, data, len)
    asm("SYSCALL 335")
    return 0
end
function net.recv(fd, buf, max_len)
    asm("SYSCALL 336")
    return 0
end
function net.close(fd)
    asm("SYSCALL 337")
    return 0
end
function net.dns_resolve(hostname)
    asm("SYSCALL 338")
    return 0
end
