\ VML 互斥锁扩展库 — Forth (OS 模式)
: MUTEX-CREATE ( -- id ) asm("SYSCALL 310") ;
: MUTEX-LOCK ( id -- result ) asm("SYSCALL 311") ;
: MUTEX-UNLOCK ( id -- result ) asm("SYSCALL 312") ;
