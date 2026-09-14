# VML 进程扩展库 — Ruby (OS 模式)
# 需显式 import process

def exec(path)
    asm("SYSCALL 320")
end

def get_pid()
    asm("SYSCALL 322")
end
