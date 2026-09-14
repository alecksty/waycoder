# VML 线程扩展库 — Python (OS 模式)
def thread_create(entry, stack_size):
    asm("SYSCALL 300")

def thread_exit():
    asm("SYSCALL 301")

def thread_join(tid):
    asm("SYSCALL 302")

def thread_yield():
    asm("SYSCALL 303")
