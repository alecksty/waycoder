-- VML 条件变量扩展库 — Lua (OS 模式)
function cond_create() asm("SYSCALL 313") return 0 end
function cond_wait(cond_id, mutex_id) asm("SYSCALL 314") return 0 end
function cond_signal(cond_id) asm("SYSCALL 315") return 0 end
function cond_broadcast(cond_id) asm("SYSCALL 316") return 0 end
