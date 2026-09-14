// VML 条件变量扩展库 — JavaScript (OS 模式)
function condCreate() { asm("SYSCALL 313"); return 0; }
function condWait(condId, mutexId) { asm("SYSCALL 314"); return 0; }
function condSignal(condId) { asm("SYSCALL 315"); return 0; }
function condBroadcast(condId) { asm("SYSCALL 316"); return 0; }
