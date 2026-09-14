\ VML 进程扩展库 — Forth (OS 模式)
: EXEC ( path-addr -- pid ) asm("SYSCALL 320") ;
: GET-PID ( -- pid ) asm("SYSCALL 322") ;
