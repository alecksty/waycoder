-- VML 环境变量扩展库 — Lua (OS 模式)
-- 需显式 import env

function get_env(name)
    asm("SYSCALL 360")
    return ""
end

function set_env(name, value)
    asm("SYSCALL 361")
    return 0
end

function get_args(buffer)
    asm("SYSCALL 362")
    return 0
end
