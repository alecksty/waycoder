# VML 条件变量扩展库 — Ruby (OS 模式)
def cond_create()
    asm("SYSCALL 313")
end

def cond_wait(cond_id, mutex_id)
    asm("SYSCALL 314")
end

def cond_signal(cond_id)
    asm("SYSCALL 315")
end

def cond_broadcast(cond_id)
    asm("SYSCALL 316")
end
