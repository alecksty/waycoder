# VML 互斥锁扩展库 — Python (OS 模式)
def mutex_create():
    asm("SYSCALL 310")

def mutex_lock(mutex_id):
    asm("SYSCALL 311")

def mutex_unlock(mutex_id):
    asm("SYSCALL 312")
