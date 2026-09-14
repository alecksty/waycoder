# VML 调试扩展库 — Ruby
# 需显式 import debug

def debug_print(s)
    asm("SYSCALL 70")
end

def debug_print_int(n)
    asm("SYSCALL 71")
end

def assert(condition, message)
    asm("SYSCALL 72")
end
