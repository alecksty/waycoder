\ PIC32MX170F256B设备定义 - Forth文件
\ 生成自: Microchip/PIC32/PIC32MX170F256B
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
\ CPU架构: MIPS32-M4K
\ 位宽: 32位
\ 时钟频率: 50000000 Hz

\ =========================================
\ PIC32MX170F256B设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PIC32MX170F256B" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" PIC32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MIPS32-M4K" ;
32 CONSTANT BITS
50000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT _0  \ Hard-wired zero
0x04 CONSTANT _1  \ AT
0x08 CONSTANT _2  \ V0
0x0C CONSTANT _3  \ V1
0x10 CONSTANT _4  \ A0
0x14 CONSTANT _5  \ A1
0x74 CONSTANT _29  \ Stack Pointer (SP)
0x7C CONSTANT _31  \ Return Address (RA)
0x80 CONSTANT PC  \ Program Counter

\ 内存段定义
0x9D000000 CONSTANT FLASH-START
0x9D03FFFF CONSTANT FLASH-END
262144 CONSTANT FLASH-SIZE  \ Program Flash
0xA0000000 CONSTANT SRAM-START
0xA000FFFF CONSTANT SRAM-END
65536 CONSTANT SRAM-SIZE  \ 
0xBF800000 CONSTANT PERIPHERAL-START
0xBF8FFFFF CONSTANT PERIPHERAL-END
1048576 CONSTANT PERIPHERAL-SIZE  \ 
0xBFC00000 CONSTANT BOOTFLASH-START
0xBFC02FFF CONSTANT BOOTFLASH-END
12288 CONSTANT BOOTFLASH-SIZE  \ Boot Flash

\ 外设定义
\ General Purpose I/O Port A
0xBF886000 CONSTANT PORTA-BASE
0x00 CONSTANT PORTA-TRISA
0x10 CONSTANT PORTA-PORTA
0x20 CONSTANT PORTA-LATA
0x30 CONSTANT PORTA-ODCA
\ General Purpose I/O Port B
0xBF886100 CONSTANT PORTB-BASE
0x00 CONSTANT PORTB-TRISB
0x10 CONSTANT PORTB-PORTB
0x20 CONSTANT PORTB-LATB
0x30 CONSTANT PORTB-ODCB
\ UART1
0xBF822000 CONSTANT UART1-BASE
0x00 CONSTANT UART1-UXMODE
0x04 CONSTANT UART1-UXSTA
0x08 CONSTANT UART1-UXTXREG
0x0C CONSTANT UART1-UXRXREG
0x10 CONSTANT UART1-UXBRG

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
8 CONSTANT INT-UART1  \ UART1 Interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: _0@ ( -- n ) _0 L@ ;
: _0! ( n -- ) _0 L! ;

: _1@ ( -- n ) _1 L@ ;
: _1! ( n -- ) _1 L! ;

: _2@ ( -- n ) _2 L@ ;
: _2! ( n -- ) _2 L! ;

: _3@ ( -- n ) _3 L@ ;
: _3! ( n -- ) _3 L! ;

: _4@ ( -- n ) _4 L@ ;
: _4! ( n -- ) _4 L! ;

: _5@ ( -- n ) _5 L@ ;
: _5! ( n -- ) _5 L! ;

: _29@ ( -- n ) _29 L@ ;
: _29! ( n -- ) _29 L! ;

: _31@ ( -- n ) _31 L@ ;
: _31! ( n -- ) _31 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ PORTA外设
: PORTA-TRISA@ ( -- n ) PORTA-TRISA L@ ;
: PORTA-TRISA! ( n -- ) PORTA-TRISA L! ;
: PORTA-PORTA@ ( -- n ) PORTA-PORTA L@ ;
: PORTA-PORTA! ( n -- ) PORTA-PORTA L! ;
: PORTA-LATA@ ( -- n ) PORTA-LATA L@ ;
: PORTA-LATA! ( n -- ) PORTA-LATA L! ;
: PORTA-ODCA@ ( -- n ) PORTA-ODCA L@ ;
: PORTA-ODCA! ( n -- ) PORTA-ODCA L! ;

\ PORTB外设
: PORTB-TRISB@ ( -- n ) PORTB-TRISB L@ ;
: PORTB-TRISB! ( n -- ) PORTB-TRISB L! ;
: PORTB-PORTB@ ( -- n ) PORTB-PORTB L@ ;
: PORTB-PORTB! ( n -- ) PORTB-PORTB L! ;
: PORTB-LATB@ ( -- n ) PORTB-LATB L@ ;
: PORTB-LATB! ( n -- ) PORTB-LATB L! ;
: PORTB-ODCB@ ( -- n ) PORTB-ODCB L@ ;
: PORTB-ODCB! ( n -- ) PORTB-ODCB L! ;

\ UART1外设
: UART1-UXMODE@ ( -- n ) UART1-UXMODE L@ ;
: UART1-UXMODE! ( n -- ) UART1-UXMODE L! ;
: UART1-UXSTA@ ( -- n ) UART1-UXSTA L@ ;
: UART1-UXSTA! ( n -- ) UART1-UXSTA L! ;
: UART1-UXTXREG@ ( -- n ) UART1-UXTXREG L@ ;
: UART1-UXTXREG! ( n -- ) UART1-UXTXREG L! ;
: UART1-UXRXREG@ ( -- n ) UART1-UXRXREG L@ ;
: UART1-UXRXREG! ( n -- ) UART1-UXRXREG L! ;
: UART1-UXBRG@ ( -- n ) UART1-UXBRG L@ ;
: UART1-UXBRG! ( n -- ) UART1-UXBRG L! ;

\ =========================================
\ 设备初始化
\ =========================================

: PIC32MX170F256B-INIT ( -- )
  \ 初始化PIC32MX170F256B设备
  ." 初始化PIC32MX170F256B..." CR

  \ 初始化寄存器
  0 _0!  \ Hard-wired zero
  0 _1!  \ AT
  0 _2!  \ V0
  0 _3!  \ V1
  0 _4!  \ A0
  0 _5!  \ A1
  0 _29!  \ Stack Pointer (SP)
  0 _31!  \ Return Address (RA)
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化PORTA
  0 PORTA-TRISA!  \ TRISA寄存器
  0 PORTA-PORTA!  \ PORTA寄存器
  0 PORTA-LATA!  \ LATA寄存器
  0 PORTA-ODCA!  \ ODCA寄存器
  \ 初始化PORTB
  0 PORTB-TRISB!  \ TRISB寄存器
  0 PORTB-PORTB!  \ PORTB寄存器
  0 PORTB-LATB!  \ LATB寄存器
  0 PORTB-ODCB!  \ ODCB寄存器
  \ 初始化UART1
  0 UART1-UXMODE!  \ UXMODE寄存器
  0 UART1-UXSTA!  \ UXSTA寄存器
  0 UART1-UXTXREG!  \ UXTXREG寄存器
  0 UART1-UXRXREG!  \ UXRXREG寄存器
  0 UART1-UXBRG!  \ UXBRG寄存器

  ." PIC32MX170F256B初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  _0@ _0 .R 8 .R SPACE ."  $0: " _0@ .
  _1@ _1 .R 8 .R SPACE ."  $1: " _1@ .
  _2@ _2 .R 8 .R SPACE ."  $2: " _2@ .
  _3@ _3 .R 8 .R SPACE ."  $3: " _3@ .
  _4@ _4 .R 8 .R SPACE ."  $4: " _4@ .
  _5@ _5 .R 8 .R SPACE ."  $5: " _5@ .
  _29@ _29 .R 8 .R SPACE ."  $29: " _29@ .
  _31@ _31 .R 8 .R SPACE ."  $31: " _31@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ 
: INT-RESET-HANDLER ( -- )
  ." Reset中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ UART1 Interrupt
: INT-UART1-HANDLER ( -- )
  ." UART1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UART1-ENABLE ( -- )
  INT-UART1 INT-ENABLE
;

: INT-UART1-DISABLE ( -- )
  INT-UART1 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  PIC32MX170F256B-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
