\ VML 环境变量扩展库 — Forth (OS 模式)
\ 需显式 include env.fth

: GET-ENV ( addr -- value-addr ) asm("SYSCALL 360") ;
: SET-ENV ( name-addr value-addr -- result ) asm("SYSCALL 361") ;
: GET-ARGS ( buf-addr -- count ) asm("SYSCALL 362") ;
