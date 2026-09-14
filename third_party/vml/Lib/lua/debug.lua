-- VML 调试扩展库 — Lua
-- 需显式 import debug

function debug_print(s)
    asm("SYSCALL 70")
end

function debug_print_int(n)
    asm("SYSCALL 71")
end

function assert(condition, message)
    asm("SYSCALL 72")
end
