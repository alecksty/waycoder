# VML 文件操作扩展库 — Ruby
def fopen(name, mode)
    asm("SYSCALL 110")
    return 0
end

def fclose(handle)
    asm("SYSCALL 111")
    return 0
end

def fread(handle, buf, count)
    asm("SYSCALL 112")
    return 0
end

def fwrite(handle, buf, count)
    asm("SYSCALL 113")
    return 0
end

def fseek(handle, offset)
    asm("SYSCALL 114")
    return 0
end

def ftell(handle)
    asm("SYSCALL 114")
    return 0
end
