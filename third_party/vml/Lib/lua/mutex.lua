-- VML 互斥锁扩展库 — Lua (OS 模式)
function mutex_create() asm("SYSCALL 310") return 0 end
function mutex_lock(id) asm("SYSCALL 311") return 0 end
function mutex_unlock(id) asm("SYSCALL 312") return 0 end
