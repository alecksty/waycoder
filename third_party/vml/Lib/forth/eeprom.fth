\ VML EEPROM 扩展库 — Forth
\ 需显式 include eeprom.fth

: EEPROM-READ ( offset buffer count -- result )
    asm("SYSCALL 106") ;

: EEPROM-WRITE ( offset data count -- result )
    asm("SYSCALL 107") ;
