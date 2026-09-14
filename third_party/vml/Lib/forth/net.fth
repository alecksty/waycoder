\ VML 网络 Socket 扩展库 — Forth
\ OS 模式专用，需显式 INCLUDE net.fth

: NET-CREATE ( domain type -- fd )
    SYSCALL 330 ;

: NET-BIND ( fd port -- result )
    SYSCALL 331 ;

: NET-LISTEN ( fd backlog -- result )
    SYSCALL 332 ;

: NET-ACCEPT ( fd -- client-fd )
    SYSCALL 333 ;

: NET-CONNECT ( host port -- result )
    SYSCALL 334 ;

: NET-SEND ( fd data len -- sent )
    SYSCALL 335 ;

: NET-RECV ( fd buf max-len -- received )
    SYSCALL 336 ;

: NET-CLOSE ( fd -- result )
    SYSCALL 337 ;

: DNS-RESOLVE ( hostname -- ip )
    SYSCALL 338 ;
