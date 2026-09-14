\ VML 设备 I/O + 文件 + 键鼠扩展库 — Forth
\ 需显式 include device.fth

\ 键盘
: KB-HIT? ( -- flag ) asm("SYSCALL 83") 0 ;
: KB-GETCH ( -- c ) asm("SYSCALL 84") 0 ;

\ 鼠标
: MOUSE-GET-X ( -- x ) asm("SYSCALL 85") 0 ;
: MOUSE-GET-Y ( -- y ) asm("SYSCALL 86") 0 ;
: MOUSE-LEFT? ( -- flag ) asm("SYSCALL 87") 0 ;
: MOUSE-RIGHT? ( -- flag ) asm("SYSCALL 88") 0 ;

\ 统一设备接口
: DEV-OPEN ( addr len -- handle ) asm("SYSCALL 100") 0 ;
: DEV-CLOSE ( handle -- result ) asm("SYSCALL 101") 0 ;
: DEV-READ ( handle buf offset count -- result ) asm("SYSCALL 102") 0 ;
: DEV-WRITE ( handle buf offset count -- result ) asm("SYSCALL 103") 0 ;
: DEV-CONTROL ( handle cmd data len -- result ) asm("SYSCALL 104") 0 ;

