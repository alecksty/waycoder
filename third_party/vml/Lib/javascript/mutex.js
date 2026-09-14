// VML 互斥锁扩展库 — JavaScript (OS 模式)
function mutexCreate() { asm("SYSCALL 310"); return 0; }
function mutexLock(id) { asm("SYSCALL 311"); return 0; }
function mutexUnlock(id) { asm("SYSCALL 312"); return 0; }
