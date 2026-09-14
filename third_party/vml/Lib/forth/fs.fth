\ VML 文件系统扩展库 — Forth (OS 模式)
\ 需显式 include fs.fth

: FS-MKDIR ( addr -- result ) asm("SYSCALL 340") ;
: FS-REMOVE ( addr -- result ) asm("SYSCALL 341") ;
: FS-RENAME ( old new -- result ) asm("SYSCALL 342") ;
: FS-READDIR ( addr buf -- result ) asm("SYSCALL 343") ;
: FS-STAT ( addr info -- result ) asm("SYSCALL 344") ;
