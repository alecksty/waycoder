# VML 文件系统扩展库 — Ruby (OS 模式)
# 需显式 import fs

def fs_exists(path)
    asm("SYSCALL 140")
    return 0
end

def fs_delete(path)
    asm("SYSCALL 141")
    return 0
end

def fs_mkdir(path)
    asm("SYSCALL 142")
    return 0
end

def fs_readdir(path, buffer)
    asm("SYSCALL 143")
    return 0
end
