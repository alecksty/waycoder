\ VML 条件变量扩展库 — Forth (OS 模式)
: COND-CREATE ( -- id ) asm("SYSCALL 313") ;
: COND-WAIT ( cond-id mutex-id -- result ) asm("SYSCALL 314") ;
: COND-SIGNAL ( cond-id -- result ) asm("SYSCALL 315") ;
: COND-BROADCAST ( cond-id -- result ) asm("SYSCALL 316") ;
