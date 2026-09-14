# VML 网络 Socket 扩展库 — Ruby
# OS 模式专用，需显式 import net

def create(domain, type)
    asm("SYSCALL 330")
    return 0
end

def bind(fd, port)
    asm("SYSCALL 331")
    return 0
end

def listen(fd, backlog)
    asm("SYSCALL 332")
    return 0
end

def accept(fd)
    asm("SYSCALL 333")
    return 0
end

def connect(host, port)
    asm("SYSCALL 334")
    return 0
end

def send(fd, data, length)
    asm("SYSCALL 335")
    return 0
end

def recv(fd, buf_size)
    asm("SYSCALL 336")
    return 0
end

def close(fd)
    asm("SYSCALL 337")
    return 0
end

def dns_resolve(hostname)
    asm("SYSCALL 338")
    return 0
end
