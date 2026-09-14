# VML 互斥锁扩展库 — Ruby (OS 模式)
def mutex_create()
    asm("SYSCALL 310")
end

def mutex_lock(mutex_id)
    asm("SYSCALL 311")
end

def mutex_unlock(mutex_id)
    asm("SYSCALL 312")
end
