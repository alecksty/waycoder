\ 8051设备定义 - Forth文件
\ 生成自: Intel/MCS-51/8051
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
\ CPU架构: MCS-51
\ 位宽: 8位
\ 时钟频率: 11059200 Hz

\ =========================================
\ 8051设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" 8051" ;
: MANUFACTURER  S" Intel" ;
: FAMILY        S" MCS-51" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MCS-51" ;
8 CONSTANT BITS
11059200 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0xE0 CONSTANT ACC  \ Accumulator
0xF0 CONSTANT B  \ B Register
0xD0 CONSTANT PSW  \ Program Status Word
0 CONSTANT PSW-P  \ Parity Flag
2 CONSTANT PSW-OV  \ Overflow Flag
3 CONSTANT PSW-RS0  \ Register Bank Select 0
4 CONSTANT PSW-RS1  \ Register Bank Select 1
5 CONSTANT PSW-F0  \ Flag 0
6 CONSTANT PSW-AC  \ Auxiliary Carry Flag
7 CONSTANT PSW-CY  \ Carry Flag
0x81 CONSTANT SP  \ Stack Pointer
0x82 CONSTANT DPTR  \ Data Pointer (DPL/DPH)

\ 内存段定义
0x0000 CONSTANT CODE-START
0x0FFF CONSTANT CODE-END
4096 CONSTANT CODE-SIZE  \ Program Memory
0x00 CONSTANT IDATA-START
0x7F CONSTANT IDATA-END
128 CONSTANT IDATA-SIZE  \ Internal Data Memory
0x80 CONSTANT SFR-START
0xFF CONSTANT SFR-END
128 CONSTANT SFR-SIZE  \ Special Function Registers
0x0000 CONSTANT XDATA-START
0xFFFF CONSTANT XDATA-END
65536 CONSTANT XDATA-SIZE  \ External Data Memory

\ 外设定义
\ Port 0
0x80 CONSTANT PORT0-BASE
0x80 CONSTANT PORT0-P0
\ Port 1
0x90 CONSTANT PORT1-BASE
0x90 CONSTANT PORT1-P1
\ Port 2
0xA0 CONSTANT PORT2-BASE
0xA0 CONSTANT PORT2-P2
\ Port 3
0xB0 CONSTANT PORT3-BASE
0xB0 CONSTANT PORT3-P3
\ Timer/Counter 0
0x8A CONSTANT TIMER0-BASE
0x8C CONSTANT TIMER0-TH0
0x8A CONSTANT TIMER0-TL0
0x89 CONSTANT TIMER0-TMOD
0 CONSTANT TIMER0-TMOD-M0_0  \ Timer 0 Mode bit 0
1 CONSTANT TIMER0-TMOD-M1_0  \ Timer 0 Mode bit 1
2 CONSTANT TIMER0-TMOD-C_T0  \ Timer 0 Counter/Timer Select
3 CONSTANT TIMER0-TMOD-GATE0  \ Timer 0 Gate Control
0x88 CONSTANT TIMER0-TCON
4 CONSTANT TIMER0-TCON-TR0  \ Timer 0 Run Control
5 CONSTANT TIMER0-TCON-TF0  \ Timer 0 Overflow Flag
\ Serial Port
0x98 CONSTANT UART-BASE
0x99 CONSTANT UART-SBUF
0x98 CONSTANT UART-SCON
0 CONSTANT UART-SCON-RI  \ Receive Interrupt Flag
1 CONSTANT UART-SCON-TI  \ Transmit Interrupt Flag
4 CONSTANT UART-SCON-REN  \ Receive Enable
6 CONSTANT UART-SCON-SM0  \ Serial Mode bit 0
7 CONSTANT UART-SCON-SM1  \ Serial Mode bit 1

\ 中断向量定义
0 CONSTANT INT-RESET  \ Reset Vector
1 CONSTANT INT-INT0  \ External Interrupt 0
2 CONSTANT INT-TIMER0  \ Timer 0 Interrupt
3 CONSTANT INT-INT1  \ External Interrupt 1
4 CONSTANT INT-TIMER1  \ Timer 1 Interrupt
5 CONSTANT INT-UART  \ Serial Port Interrupt

\ 引脚定义
1 CONSTANT PIN-P1_0  \ Port 1, bit 0
2 CONSTANT PIN-P1_1  \ Port 1, bit 1
3 CONSTANT PIN-P1_2  \ Port 1, bit 2
4 CONSTANT PIN-P1_3  \ Port 1, bit 3
5 CONSTANT PIN-P1_4  \ Port 1, bit 4
6 CONSTANT PIN-P1_5  \ Port 1, bit 5
7 CONSTANT PIN-P1_6  \ Port 1, bit 6
8 CONSTANT PIN-P1_7  \ Port 1, bit 7
9 CONSTANT PIN-RST  \ Reset Pin
10 CONSTANT PIN-RX  \ Serial Receive (P3.0)
11 CONSTANT PIN-TX  \ Serial Transmit (P3.1)
12 CONSTANT PIN-INT0  \ External Interrupt 0 (P3.2)
13 CONSTANT PIN-INT1  \ External Interrupt 1 (P3.3)
14 CONSTANT PIN-T0  \ Timer 0 Input (P3.4)
15 CONSTANT PIN-T1  \ Timer 1 Input (P3.5)
16 CONSTANT PIN-WR  \ External Memory Write Strobe (P3.6)
17 CONSTANT PIN-RD  \ External Memory Read Strobe (P3.7)
18 CONSTANT PIN-XTAL1  \ Crystal Oscillator Input
19 CONSTANT PIN-XTAL2  \ Crystal Oscillator Output
20 CONSTANT PIN-VCC  \ Power Supply (+5V)
21 CONSTANT PIN-GND  \ Ground

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: ACC@ ( -- n ) ACC C@ ;
: ACC! ( n -- ) ACC C! ;

: B@ ( -- n ) B C@ ;
: B! ( n -- ) B C! ;

: PSW@ ( -- n ) PSW C@ ;
: PSW! ( n -- ) PSW C! ;
: PSW-P@ ( -- flag ) PSW@ 0 BIT@ ;
: PSW-P! ( flag -- ) PSW@ 0 BIT! PSW! ;
: PSW-P-SET ( -- ) TRUE PSW-P! ;
: PSW-P-CLR ( -- ) FALSE PSW-P! ;
: PSW-OV@ ( -- flag ) PSW@ 2 BIT@ ;
: PSW-OV! ( flag -- ) PSW@ 2 BIT! PSW! ;
: PSW-OV-SET ( -- ) TRUE PSW-OV! ;
: PSW-OV-CLR ( -- ) FALSE PSW-OV! ;
: PSW-RS0@ ( -- flag ) PSW@ 3 BIT@ ;
: PSW-RS0! ( flag -- ) PSW@ 3 BIT! PSW! ;
: PSW-RS0-SET ( -- ) TRUE PSW-RS0! ;
: PSW-RS0-CLR ( -- ) FALSE PSW-RS0! ;
: PSW-RS1@ ( -- flag ) PSW@ 4 BIT@ ;
: PSW-RS1! ( flag -- ) PSW@ 4 BIT! PSW! ;
: PSW-RS1-SET ( -- ) TRUE PSW-RS1! ;
: PSW-RS1-CLR ( -- ) FALSE PSW-RS1! ;
: PSW-F0@ ( -- flag ) PSW@ 5 BIT@ ;
: PSW-F0! ( flag -- ) PSW@ 5 BIT! PSW! ;
: PSW-F0-SET ( -- ) TRUE PSW-F0! ;
: PSW-F0-CLR ( -- ) FALSE PSW-F0! ;
: PSW-AC@ ( -- flag ) PSW@ 6 BIT@ ;
: PSW-AC! ( flag -- ) PSW@ 6 BIT! PSW! ;
: PSW-AC-SET ( -- ) TRUE PSW-AC! ;
: PSW-AC-CLR ( -- ) FALSE PSW-AC! ;
: PSW-CY@ ( -- flag ) PSW@ 7 BIT@ ;
: PSW-CY! ( flag -- ) PSW@ 7 BIT! PSW! ;
: PSW-CY-SET ( -- ) TRUE PSW-CY! ;
: PSW-CY-CLR ( -- ) FALSE PSW-CY! ;

: SP@ ( -- n ) SP C@ ;
: SP! ( n -- ) SP C! ;

: DPTR@ ( -- n ) DPTR @ ;
: DPTR! ( n -- ) DPTR ! ;

\ 外设访问
\ PORT0外设
: PORT0-P0@ ( -- n ) PORT0-P0 C@ ;
: PORT0-P0! ( n -- ) PORT0-P0 C! ;

\ PORT1外设
: PORT1-P1@ ( -- n ) PORT1-P1 C@ ;
: PORT1-P1! ( n -- ) PORT1-P1 C! ;

\ PORT2外设
: PORT2-P2@ ( -- n ) PORT2-P2 C@ ;
: PORT2-P2! ( n -- ) PORT2-P2 C! ;

\ PORT3外设
: PORT3-P3@ ( -- n ) PORT3-P3 C@ ;
: PORT3-P3! ( n -- ) PORT3-P3 C! ;

\ TIMER0外设
: TIMER0-TH0@ ( -- n ) TIMER0-TH0 C@ ;
: TIMER0-TH0! ( n -- ) TIMER0-TH0 C! ;
: TIMER0-TL0@ ( -- n ) TIMER0-TL0 C@ ;
: TIMER0-TL0! ( n -- ) TIMER0-TL0 C! ;
: TIMER0-TMOD@ ( -- n ) TIMER0-TMOD C@ ;
: TIMER0-TMOD! ( n -- ) TIMER0-TMOD C! ;
: TIMER0-TMOD-M0_0@ ( -- flag ) TIMER0-TMOD@ 0 BIT@ ;
: TIMER0-TMOD-M0_0! ( flag -- ) TIMER0-TMOD@ 0 BIT! TIMER0-TMOD! ;
: TIMER0-TMOD-M1_0@ ( -- flag ) TIMER0-TMOD@ 1 BIT@ ;
: TIMER0-TMOD-M1_0! ( flag -- ) TIMER0-TMOD@ 1 BIT! TIMER0-TMOD! ;
: TIMER0-TMOD-C_T0@ ( -- flag ) TIMER0-TMOD@ 2 BIT@ ;
: TIMER0-TMOD-C_T0! ( flag -- ) TIMER0-TMOD@ 2 BIT! TIMER0-TMOD! ;
: TIMER0-TMOD-GATE0@ ( -- flag ) TIMER0-TMOD@ 3 BIT@ ;
: TIMER0-TMOD-GATE0! ( flag -- ) TIMER0-TMOD@ 3 BIT! TIMER0-TMOD! ;
: TIMER0-TCON@ ( -- n ) TIMER0-TCON C@ ;
: TIMER0-TCON! ( n -- ) TIMER0-TCON C! ;
: TIMER0-TCON-TR0@ ( -- flag ) TIMER0-TCON@ 4 BIT@ ;
: TIMER0-TCON-TR0! ( flag -- ) TIMER0-TCON@ 4 BIT! TIMER0-TCON! ;
: TIMER0-TCON-TF0@ ( -- flag ) TIMER0-TCON@ 5 BIT@ ;
: TIMER0-TCON-TF0! ( flag -- ) TIMER0-TCON@ 5 BIT! TIMER0-TCON! ;

\ UART外设
: UART-SBUF@ ( -- n ) UART-SBUF C@ ;
: UART-SBUF! ( n -- ) UART-SBUF C! ;
: UART-SCON@ ( -- n ) UART-SCON C@ ;
: UART-SCON! ( n -- ) UART-SCON C! ;
: UART-SCON-RI@ ( -- flag ) UART-SCON@ 0 BIT@ ;
: UART-SCON-RI! ( flag -- ) UART-SCON@ 0 BIT! UART-SCON! ;
: UART-SCON-TI@ ( -- flag ) UART-SCON@ 1 BIT@ ;
: UART-SCON-TI! ( flag -- ) UART-SCON@ 1 BIT! UART-SCON! ;
: UART-SCON-REN@ ( -- flag ) UART-SCON@ 4 BIT@ ;
: UART-SCON-REN! ( flag -- ) UART-SCON@ 4 BIT! UART-SCON! ;
: UART-SCON-SM0@ ( -- flag ) UART-SCON@ 6 BIT@ ;
: UART-SCON-SM0! ( flag -- ) UART-SCON@ 6 BIT! UART-SCON! ;
: UART-SCON-SM1@ ( -- flag ) UART-SCON@ 7 BIT@ ;
: UART-SCON-SM1! ( flag -- ) UART-SCON@ 7 BIT! UART-SCON! ;

\ =========================================
\ 设备初始化
\ =========================================

: _8051-INIT ( -- )
  \ 初始化8051设备
  ." 初始化8051..." CR

  \ 初始化寄存器
  0 ACC!  \ Accumulator
  0 B!  \ B Register
  0 PSW!  \ Program Status Word
  0 SP!  \ Stack Pointer
  0 DPTR!  \ Data Pointer (DPL/DPH)

  \ 初始化外设
  \ 初始化PORT0
  0 PORT0-P0!  \ P0寄存器
  \ 初始化PORT1
  0 PORT1-P1!  \ P1寄存器
  \ 初始化PORT2
  0 PORT2-P2!  \ P2寄存器
  \ 初始化PORT3
  0 PORT3-P3!  \ P3寄存器
  \ 初始化TIMER0
  0 TIMER0-TH0!  \ TH0寄存器
  0 TIMER0-TL0!  \ TL0寄存器
  0 TIMER0-TMOD!  \ TMOD寄存器
  0 TIMER0-TCON!  \ TCON寄存器
  \ 初始化UART
  0 UART-SBUF!  \ SBUF寄存器
  0 UART-SCON!  \ SCON寄存器

  ." 8051初始化完成" CR
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
  ACC@ ACC .R 8 .R SPACE ."  ACC: " ACC@ .
  B@ B .R 8 .R SPACE ."  B: " B@ .
  PSW@ PSW .R 8 .R SPACE ."  PSW: " PSW@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  DPTR@ DPTR .R 8 .R SPACE ."  DPTR: " DPTR@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Reset Vector
: INT-RESET-HANDLER ( -- )
  ." RESET中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ External Interrupt 0
: INT-INT0-HANDLER ( -- )
  ." INT0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT0-ENABLE ( -- )
  INT-INT0 INT-ENABLE
;

: INT-INT0-DISABLE ( -- )
  INT-INT0 INT-DISABLE
;

\ Timer 0 Interrupt
: INT-TIMER0-HANDLER ( -- )
  ." TIMER0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER0-ENABLE ( -- )
  INT-TIMER0 INT-ENABLE
;

: INT-TIMER0-DISABLE ( -- )
  INT-TIMER0 INT-DISABLE
;

\ External Interrupt 1
: INT-INT1-HANDLER ( -- )
  ." INT1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT1-ENABLE ( -- )
  INT-INT1 INT-ENABLE
;

: INT-INT1-DISABLE ( -- )
  INT-INT1 INT-DISABLE
;

\ Timer 1 Interrupt
: INT-TIMER1-HANDLER ( -- )
  ." TIMER1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER1-ENABLE ( -- )
  INT-TIMER1 INT-ENABLE
;

: INT-TIMER1-DISABLE ( -- )
  INT-TIMER1 INT-DISABLE
;

\ Serial Port Interrupt
: INT-UART-HANDLER ( -- )
  ." UART中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UART-ENABLE ( -- )
  INT-UART INT-ENABLE
;

: INT-UART-DISABLE ( -- )
  INT-UART INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  _8051-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
