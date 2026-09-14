// VML 线程扩展库 — JavaScript (OS 模式)
function threadCreate(entry, stackSize) { asm("SYSCALL 300"); return 0; }
function threadExit() { asm("SYSCALL 301"); }
function threadJoin(tid) { asm("SYSCALL 302"); return 0; }
function threadYield() { asm("SYSCALL 303"); return 0; }
