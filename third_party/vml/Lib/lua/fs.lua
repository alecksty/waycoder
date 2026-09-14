-- VML 文件系统扩展库 — Lua (OS 模式)
-- 需显式 import fs

function fs_mkdir(path)
    asm("SYSCALL 340")
    return 0
end

function fs_remove(path)
    asm("SYSCALL 341")
    return 0
end

function fs_rename(old_path, new_path)
    asm("SYSCALL 342")
    return 0
end

function fs_readdir(path, buffer)
    asm("SYSCALL 343")
    return 0
end

function fs_stat(path, info)
    asm("SYSCALL 344")
    return 0
end
