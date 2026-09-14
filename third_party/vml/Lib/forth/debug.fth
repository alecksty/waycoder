\ VML 调试扩展库 — Forth
\ 需显式 include debug.fth

: DEBUG-PRINT ( addr -- )
    asm("SYSCALL 70") ;

: DEBUG-PRINT-INT ( n -- )
    asm("SYSCALL 71") ;

: ASSERT ( condition addr -- )
    asm("SYSCALL 72") ;
