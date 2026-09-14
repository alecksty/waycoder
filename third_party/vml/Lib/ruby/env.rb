# VML 环境变量扩展库 — Ruby (OS 模式)
# 需显式 import env

def get_env(name)
    asm("SYSCALL 130")
end

def set_env(name, value)
    asm("SYSCALL 131")
end
