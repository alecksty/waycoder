\ VML 系统扩展库 — Forth
\ 需显式 include sys.fth

: SPEAKER-BEEP ( freq duration -- )
    asm("SYSCALL 57") ;

: SET-RTC ( timestamp -- result )
    asm("SYSCALL 58") ;
