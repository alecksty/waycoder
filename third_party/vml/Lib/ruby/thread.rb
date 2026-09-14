# VML 线程扩展库 — Ruby (OS 模式)
def thread_create(entry, stack_size)
    asm("SYSCALL 300")
end

def thread_exit()
    asm("SYSCALL 301")
end

def thread_join(tid)
    asm("SYSCALL 302")
end

def thread_yield()
    asm("SYSCALL 303")
end
