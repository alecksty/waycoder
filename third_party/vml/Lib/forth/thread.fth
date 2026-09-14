\ VML 线程扩展库 — Forth (OS 模式)
: THREAD-CREATE ( entry stack-size -- tid ) asm("SYSCALL 300") ;
: THREAD-EXIT ( -- ) asm("SYSCALL 301") ;
: THREAD-JOIN ( tid -- result ) asm("SYSCALL 302") ;
: THREAD-YIELD ( -- result ) asm("SYSCALL 303") ;
