-- VML 线程扩展库 — Lua (OS 模式)
function thread_create(entry, stack_size) asm("SYSCALL 300") return 0 end
function thread_exit() asm("SYSCALL 301") end
function thread_join(tid) asm("SYSCALL 302") return 0 end
function thread_yield() asm("SYSCALL 303") return 0 end
