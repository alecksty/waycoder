\ VML FFI 动态库调用扩展库 — Forth
\ OS 模式专用，需显式 INCLUDE ffi.fth

: DL-OPEN ( path -- handle )
    SYSCALL 370 ;

: DL-SYM ( handle name -- func-id )
    SYSCALL 371 ;

: DL-CLOSE ( handle -- result )
    SYSCALL 372 ;

: NATIVE-CALL ( func-id args count flags -- result )
    SYSCALL 373 ;

: NATIVE-CALL-F ( func-id fargs count flags -- fresult )
    SYSCALL 375 ;

: GET-PLATFORM ( -- id )
    SYSCALL 374 ;
