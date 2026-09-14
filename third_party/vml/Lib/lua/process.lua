-- VML 进程扩展库 — Lua (OS 模式)
-- 需显式 import process

function exec(path)
    asm("SYSCALL 320")
    return 0
end

function get_pid()
    asm("SYSCALL 322")
    return 0
end
