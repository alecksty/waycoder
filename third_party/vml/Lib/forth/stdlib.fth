\ Forth 完整标准库 - VML 实现 (ANS Forth + 常用扩展)

\ ============================================================
\ 栈操作
\ ============================================================
: DUP ( n -- n n ) ;
: DROP ( n -- ) ;
: SWAP ( n1 n2 -- n2 n1 ) ;
: OVER ( n1 n2 -- n1 n2 n1 ) ;
: ROT ( n1 n2 n3 -- n2 n3 n1 ) ;
: -ROT ( n1 n2 n3 -- n3 n1 n2 ) ;
: NIP ( n1 n2 -- n2 ) ;
: TUCK ( n1 n2 -- n2 n1 n2 ) ;
: 2DUP ( n1 n2 -- n1 n2 n1 n2 ) OVER OVER ;
: 2DROP ( n1 n2 -- ) DROP DROP ;
: 2SWAP ( n1 n2 n3 n4 -- n3 n4 n1 n2 ) ROT >R ROT R> ;
: 2OVER ( n1 n2 n3 n4 -- n1 n2 n3 n4 n1 n2 ) 3 PICK 3 PICK ;
: PICK ( n -- val ) 1+ 2* SP@ + @ ;
: ROLL ( x0 x1 ... xn n -- x1 ... xn x0 ) ;

\ ============================================================
\ 算术运算
\ ============================================================
: + ( n1 n2 -- sum ) asm("ADD R0 R1") ;
: - ( n1 n2 -- diff ) asm("SUB R0 R1") ;
: * ( n1 n2 -- prod ) asm("MUL R0 R1") ;
: / ( n1 n2 -- quot ) asm("DIV R0 R1") ;
: MOD ( n1 n2 -- rem ) asm("MOD R0 R1") ;
: /MOD ( n1 n2 -- rem quot ) 2DUP MOD ROT ROT / SWAP ;
: NEGATE ( n -- -n ) -1 * ;
: ABS ( n -- |n| ) asm("SYSCALL 43") ;
: MIN ( n1 n2 -- min ) asm("SYSCALL 45") ;
: MAX ( n1 n2 -- max ) asm("SYSCALL 46") ;

\ ============================================================
\ 逻辑运算
\ ============================================================
: AND ( n1 n2 -- n3 ) asm("AND R0 R1") ;
: OR ( n1 n2 -- n3 ) asm("OR R0 R1") ;
: XOR ( n1 n2 -- n3 ) asm("XOR R0 R1") ;
: NOT ( n1 -- n2 ) asm("NOT R0") ;
: LSHIFT ( n1 n2 -- n3 ) asm("SHL R0 R1") ;
: RSHIFT ( n1 n2 -- n3 ) asm("SHR R0 R1") ;
: 2* ( n -- n*2 ) 1 LSHIFT ;
: 2/ ( n -- n/2 ) 1 RSHIFT ;

\ ============================================================
\ 比较运算
\ ============================================================
: 0< ( n -- flag ) 0 < ;
: 0= ( n -- flag ) 0 = ;
: 0> ( n -- flag ) 0 > ;
: < ( n1 n2 -- flag ) asm("SYSCALL 48") ;
: = ( n1 n2 -- flag ) asm("SYSCALL 47") ;
: > ( n1 n2 -- flag ) asm("SYSCALL 49") ;
: <> ( n1 n2 -- flag ) = NOT ;
: <= ( n1 n2 -- flag ) > NOT ;
: >= ( n1 n2 -- flag ) < NOT ;
: WITHIN ( n lo hi -- flag ) OVER - ROT ROT - U> ;

\ ============================================================
\ 内存操作
\ ============================================================
: ! ( n addr -- ) asm("STORE [R0] R1") ;
: @ ( addr -- n ) asm("LOAD R0 [R0]") ;
: C! ( c addr -- ) asm("STOREB [R0] R1") ;
: C@ ( addr -- c ) asm("LOADB R0 [R0]") ;
: +! ( n addr -- ) DUP @ ROT + SWAP ! ;
: ALLOT ( n -- ) ;
: HERE ( -- addr ) asm("SYSCALL 40") ;

\ ============================================================
\ 浮点运算 (VML 浮点栈)
\ ============================================================
: F+ ( f1 f2 -- f3 ) asm("FADD R0 R0 R1") ;
: F- ( f1 f2 -- f3 ) asm("FSUB R0 R0 R1") ;
: F* ( f1 f2 -- f3 ) asm("FMUL R0 R0 R1") ;
: F/ ( f1 f2 -- f3 ) asm("FDIV R0 R0 R1") ;
: F. ( f -- ) asm("SYSCALL 8") ;
: F@ ( addr -- f ) asm("FLOAD R0 [R0]") ;
: F! ( f addr -- ) asm("FSTORE [R0] R1") ;
: FDUP ( f -- f f ) ;
: FDROP ( f -- ) ;
: FSWAP ( f1 f2 -- f2 f1 ) ;

\ ============================================================
\ 浮点常量
\ ============================================================
: FCONSTANT ( f -- ) ;
: FVARIABLE ( -- ) ;

\ ============================================================
\ 数学函数
\ ============================================================
: SIN ( f -- f ) asm("SYSCALL 21") ;
: COS ( f -- f ) asm("SYSCALL 22") ;
: TAN ( f -- f ) asm("SYSCALL 23") ;
: ASIN ( f -- f ) asm("SYSCALL 24") ;
: ACOS ( f -- f ) asm("SYSCALL 25") ;
: ATAN ( f -- f ) asm("SYSCALL 32") ;
: ATAN2 ( fy fx -- f ) asm("SYSCALL 33") ;
: SQRT ( f -- f ) asm("SYSCALL 20") ;
: POW ( fy fx -- f ) asm("SYSCALL 26") ;
: EXP ( f -- f ) asm("SYSCALL 27") ;
: LOG ( f -- f ) asm("SYSCALL 28") ;
: FLOOR ( f -- f ) asm("SYSCALL 29") ;
: CEIL ( f -- f ) asm("SYSCALL 30") ;
: ROUND ( f -- f ) asm("SYSCALL 31") ;

: PI ( -- f ) 3.141592653589793 ;
: E ( -- f ) 2.718281828459045 ;
: DEG>RAD ( deg -- rad ) PI * 180.0 / ;
: RAD>DEG ( rad -- deg ) 180.0 * PI / ;

\ ============================================================
\ 随机数
\ ============================================================
: RANDOM ( -- n ) asm("SYSCALL 50") ;
: RANDOMIZE ( seed -- ) asm("SYSCALL 51") ;
: RAND ( n -- r ) RANDOM SWAP MOD ;

\ ============================================================
\ 控制结构
\ ============================================================
: IF ( flag -- ) ;
: THEN ( -- ) ;
: ELSE ( -- ) ;
: BEGIN ( -- ) ;
: AGAIN ( -- ) ;
: UNTIL ( flag -- ) ;
: WHILE ( flag -- ) ;
: REPEAT ( -- ) ;
: DO ( -- ) ;
: LOOP ( -- ) ;
: +LOOP ( -- ) ;
: ?DO ( -- ) ;
: EXIT ( -- ) ;
: RECURSE ( -- ) ;
: LEAVE ( -- ) ;
: UNLOOP ( -- ) ;

\ ============================================================
\ 字符串操作
\ ============================================================
: TYPE ( addr len -- ) asm("SYSCALL 1") ;
: EMIT ( c -- ) asm("SYSCALL 4") ;
: CR ( -- ) 10 EMIT ;
: SPACE ( -- ) 32 EMIT ;
: SPACES ( n -- ) 0 DO SPACE LOOP ;

: ." ( -- ) (编译时) ;
: S" ( -- addr len ) (编译时) ;
: C" ( -- addr ) (编译时) ;

\ 字符串长度
: S+LEN ( addr -- len ) asm("SYSCALL 60") ;

\ 字符串比较
: S= ( a1 a2 -- flag ) asm("SYSCALL 62") 0= ;

\ 字符串复制
: S+COPY ( dest src -- ) asm("SYSCALL 61") ;

\ 字符串拼接
: S+CAT ( dest src -- ) asm("SYSCALL 63") ;

\ 数字转字符串
: NUM>S ( n -- addr len ) asm("SYSCALL 42") ;

\ 字符串转数字
: S>NUM ( addr -- n ) asm("SYSCALL 40") ;

\ 字符串逆序
: S+REVERSE ( dest src -- ) ;

\ 字符串大小写
: S>UPPER ( dest src -- ) ;
: S>LOWER ( dest src -- ) ;

\ 字符串分割与拼接
: SPLIT ( addr delim -- addr2 len2 ) ;
: STR-JOIN ( addr1 len1 addr2 len2 -- addr3 len3 ) ;

\ ============================================================
\ 数字输出
\ ============================================================
: . ( n -- ) asm("SYSCALL 6") SPACE ;
: U. ( u -- ) . ;
: .R ( n width -- ) SWAP . ;
: U.R ( u width -- ) SWAP U. ;
: DECIMAL ( -- ) ;
: HEX ( -- ) ;
: BINARY ( -- ) ;
: .S ( -- ) \ 打印栈内容 ;

: .HEX ( n -- ) asm("SYSCALL 10") ;

\ ============================================================
\ 字典和定义
\ ============================================================
: CREATE ( -- ) ;
: DOES> ( -- ) ;
: VARIABLE ( -- ) CREATE 0 , ;
: CONSTANT ( n -- ) CREATE , DOES> @ ;
: VALUE ( n -- ) ;
: TO ( n -- ) ;
: DEFER ( -- ) ;
: IS ( xt -- ) ;
: IMMEDIATE ( -- ) ;

\ ============================================================
\ 文件操作 (OS 模式)
\ ============================================================
: OPEN-FILE ( addr mode -- handle ) asm("SYSCALL 110") ;
: CLOSE-FILE ( handle -- ) asm("SYSCALL 111") ;
: READ-FILE ( buf size handle -- n ) asm("SYSCALL 112") ;
: WRITE-FILE ( buf size handle -- n ) asm("SYSCALL 113") ;
: FILE-SIZE ( handle -- n ) asm("SYSCALL 117") ;
: FILE-POSITION ( handle -- n ) asm("SYSCALL 116") ;
: REPOSITION-FILE ( pos handle -- ) asm("SYSCALL 115") ;
: FILE-EOF? ( handle -- flag ) asm("SYSCALL 114") ;

: R/O ( -- 0 ) 0 ;
: W/O ( -- 1 ) 1 ;
: R/W ( -- 2 ) 2 ;

: INCLUDED ( filename -- ) ;
: INCLUDE ( "name" -- ) ;

\ ============================================================
\ 内存扩展
\ ============================================================
: MEM-ALLOC ( size -- addr ) asm("SYSCALL 40") ;
: MEM-FREE ( addr -- ) asm("SYSCALL 41") ;
: MEM-READ ( addr -- n ) @ ;
: MEM-WRITE ( n addr -- ) ! ;
: MEM-COMPARE ( addr1 addr2 n -- result ) asm("SYSCALL 13") ;
: MEM-SET ( addr value count -- ) asm("SYSCALL 70") ;
: MEM-COPY ( dest src count -- ) asm("SYSCALL 71") ;
: DUMP ( addr len -- ) ;

\ ============================================================
\ 系统扩展
\ ============================================================
: MS ( n -- ) asm("SYSCALL 52") ;
: EXIT ( code -- ) asm("SYSCALL 3") ;
: BYE ( -- ) 0 EXIT ;
: TICKS ( -- n ) asm("SYSCALL 53") ;
: TIME&DATE ( -- sec min hour day month year ) ;
: GET-DATE ( -- addr ) asm("SYSCALL 55") ;
: GET-TIME ( -- addr ) asm("SYSCALL 56") ;
: GET-DATETIME ( -- n ) asm("SYSCALL 54") ;
: GET-CONFIG ( key -- val ) asm("SYSCALL 60") ;
: KEY ( -- c ) asm("SYSCALL 5") ;
: KEY? ( -- flag ) asm("SYSCALL 12") ;

\ ============================================================
\ 用户输入
\ ============================================================
: ACCEPT ( addr len -- n ) asm("SYSCALL 2") ;
: READ-LINE ( -- addr ) asm("SYSCALL 2") ;
: READ-INT ( -- n ) asm("SYSCALL 7") ;

\ ============================================================
\ 编译工具
\ ============================================================
: [ ( -- ) ;
: ] ( -- ) ;
: POSTPONE ( -- ) ;
: ' ( "name" -- xt ) ;
: ['] ( "name" -- xt ) (编译时) ;
: EXECUTE ( xt -- ) ;
: COMPILE, ( xt -- ) ;
: LITERAL ( n -- ) ;

\ ============================================================
\ 错误处理
\ ============================================================
: ABORT ( -- ) -1 EXIT ;
: ABORT" ( "msg" -- ) (编译时) ;
: CATCH ( xt -- exception ) ;
: THROW ( exception -- ) ;

\ ============================================================
\ 字符串常量
\ ============================================================
: WORDS_MESSAGE ( -- addr len ) S" Core words loaded" ;
