\ 8086设备定义 - Forth文件
\ 生成自: Intel/x86/8086
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: 16-bit microprocessor, first x86 processor
\ CPU架构: x86
\ 位宽: 16位
\ 时钟频率: 5000000 Hz

\ =========================================
\ 8086设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" 8086" ;
: MANUFACTURER  S" Intel" ;
: FAMILY        S" x86" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" x86" ;
16 CONSTANT BITS
5000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT AX  \ Accumulator
8 CONSTANT AX-AH  \ High byte of AX
0 CONSTANT AX-AL  \ Low byte of AX
1 CONSTANT BX  \ Base
8 CONSTANT BX-BH  \ High byte of BX
0 CONSTANT BX-BL  \ Low byte of BX
2 CONSTANT CX  \ Counter
8 CONSTANT CX-CH  \ High byte of CX
0 CONSTANT CX-CL  \ Low byte of CX
3 CONSTANT DX  \ Data
8 CONSTANT DX-DH  \ High byte of DX
0 CONSTANT DX-DL  \ Low byte of DX
4 CONSTANT SI  \ Source Index
5 CONSTANT DI  \ Destination Index
6 CONSTANT BP  \ Base Pointer
7 CONSTANT SP  \ Stack Pointer
8 CONSTANT IP  \ Instruction Pointer
9 CONSTANT CS  \ Code Segment
10 CONSTANT DS  \ Data Segment
11 CONSTANT ES  \ Extra Segment
12 CONSTANT SS  \ Stack Segment
13 CONSTANT FLAGS  \ Flags Register
0 CONSTANT FLAGS-CF  \ Carry Flag
2 CONSTANT FLAGS-PF  \ Parity Flag
4 CONSTANT FLAGS-AF  \ Auxiliary Flag
6 CONSTANT FLAGS-ZF  \ Zero Flag
7 CONSTANT FLAGS-SF  \ Sign Flag
8 CONSTANT FLAGS-TF  \ Trap Flag
9 CONSTANT FLAGS-IF  \ Interrupt Enable Flag
10 CONSTANT FLAGS-DF  \ Direction Flag
11 CONSTANT FLAGS-OF  \ Overflow Flag

\ 内存段定义
0x00000 CONSTANT CODE-START
0xFFFFF CONSTANT CODE-END
1048576 CONSTANT CODE-SIZE  \ 1MB address space
0x00000 CONSTANT DATA-START
0xFFFFF CONSTANT DATA-END
1048576 CONSTANT DATA-SIZE  \ Data memory
0xF0000 CONSTANT STACK-START
0xFFFFF CONSTANT STACK-END
65536 CONSTANT STACK-SIZE  \ Stack memory
0xF0000 CONSTANT BIOS-START
0xFFFFF CONSTANT BIOS-END
65536 CONSTANT BIOS-SIZE  \ BIOS ROM

\ 外设定义
\ Programmable Interrupt Controller
0x0020 CONSTANT PIC-BASE
0x0020 CONSTANT PIC-PIC1_CMD
0x0021 CONSTANT PIC-PIC1_DATA
0x00A0 CONSTANT PIC-PIC2_CMD
0x00A1 CONSTANT PIC-PIC2_DATA
\ Programmable Interval Timer
0x0040 CONSTANT PIT-BASE
0x0040 CONSTANT PIT-PIT_CH0
0x0041 CONSTANT PIT-PIT_CH1
0x0042 CONSTANT PIT-PIT_CH2
0x0043 CONSTANT PIT-PIT_CMD
\ Programmable Peripheral Interface
0x0060 CONSTANT PPI-BASE
0x0060 CONSTANT PPI-PPI_PA
0x0061 CONSTANT PPI-PPI_PB
0x0062 CONSTANT PPI-PPI_PC
0x0063 CONSTANT PPI-PPI_CMD

\ 中断向量定义
0 CONSTANT INT-DIVIDE_ERROR  \ Divide by zero
1 CONSTANT INT-DEBUG  \ Single step
2 CONSTANT INT-NMI  \ Non-maskable interrupt
3 CONSTANT INT-BREAKPOINT  \ Breakpoint
4 CONSTANT INT-OVERFLOW  \ INTO detected overflow
8 CONSTANT INT-IRQ0  \ Timer interrupt
9 CONSTANT INT-IRQ1  \ Keyboard interrupt
10 CONSTANT INT-IRQ2  \ Cascade
11 CONSTANT INT-IRQ3  \ COM2
12 CONSTANT INT-IRQ4  \ COM1
13 CONSTANT INT-IRQ5  \ LPT2
14 CONSTANT INT-IRQ6  \ Floppy disk
15 CONSTANT INT-IRQ7  \ LPT1

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: AX@ ( -- n ) AX @ ;
: AX! ( n -- ) AX ! ;
: AX-AH@ ( -- flag ) AX@ 8 BIT@ ;
: AX-AH! ( flag -- ) AX@ 8 BIT! AX! ;
: AX-AH-SET ( -- ) TRUE AX-AH! ;
: AX-AH-CLR ( -- ) FALSE AX-AH! ;
: AX-AL@ ( -- flag ) AX@ 0 BIT@ ;
: AX-AL! ( flag -- ) AX@ 0 BIT! AX! ;
: AX-AL-SET ( -- ) TRUE AX-AL! ;
: AX-AL-CLR ( -- ) FALSE AX-AL! ;

: BX@ ( -- n ) BX @ ;
: BX! ( n -- ) BX ! ;
: BX-BH@ ( -- flag ) BX@ 8 BIT@ ;
: BX-BH! ( flag -- ) BX@ 8 BIT! BX! ;
: BX-BH-SET ( -- ) TRUE BX-BH! ;
: BX-BH-CLR ( -- ) FALSE BX-BH! ;
: BX-BL@ ( -- flag ) BX@ 0 BIT@ ;
: BX-BL! ( flag -- ) BX@ 0 BIT! BX! ;
: BX-BL-SET ( -- ) TRUE BX-BL! ;
: BX-BL-CLR ( -- ) FALSE BX-BL! ;

: CX@ ( -- n ) CX @ ;
: CX! ( n -- ) CX ! ;
: CX-CH@ ( -- flag ) CX@ 8 BIT@ ;
: CX-CH! ( flag -- ) CX@ 8 BIT! CX! ;
: CX-CH-SET ( -- ) TRUE CX-CH! ;
: CX-CH-CLR ( -- ) FALSE CX-CH! ;
: CX-CL@ ( -- flag ) CX@ 0 BIT@ ;
: CX-CL! ( flag -- ) CX@ 0 BIT! CX! ;
: CX-CL-SET ( -- ) TRUE CX-CL! ;
: CX-CL-CLR ( -- ) FALSE CX-CL! ;

: DX@ ( -- n ) DX @ ;
: DX! ( n -- ) DX ! ;
: DX-DH@ ( -- flag ) DX@ 8 BIT@ ;
: DX-DH! ( flag -- ) DX@ 8 BIT! DX! ;
: DX-DH-SET ( -- ) TRUE DX-DH! ;
: DX-DH-CLR ( -- ) FALSE DX-DH! ;
: DX-DL@ ( -- flag ) DX@ 0 BIT@ ;
: DX-DL! ( flag -- ) DX@ 0 BIT! DX! ;
: DX-DL-SET ( -- ) TRUE DX-DL! ;
: DX-DL-CLR ( -- ) FALSE DX-DL! ;

: SI@ ( -- n ) SI @ ;
: SI! ( n -- ) SI ! ;

: DI@ ( -- n ) DI @ ;
: DI! ( n -- ) DI ! ;

: BP@ ( -- n ) BP @ ;
: BP! ( n -- ) BP ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: IP@ ( -- n ) IP @ ;
: IP! ( n -- ) IP ! ;

: CS@ ( -- n ) CS @ ;
: CS! ( n -- ) CS ! ;

: DS@ ( -- n ) DS @ ;
: DS! ( n -- ) DS ! ;

: ES@ ( -- n ) ES @ ;
: ES! ( n -- ) ES ! ;

: SS@ ( -- n ) SS @ ;
: SS! ( n -- ) SS ! ;

: FLAGS@ ( -- n ) FLAGS @ ;
: FLAGS! ( n -- ) FLAGS ! ;
: FLAGS-CF@ ( -- flag ) FLAGS@ 0 BIT@ ;
: FLAGS-CF! ( flag -- ) FLAGS@ 0 BIT! FLAGS! ;
: FLAGS-CF-SET ( -- ) TRUE FLAGS-CF! ;
: FLAGS-CF-CLR ( -- ) FALSE FLAGS-CF! ;
: FLAGS-PF@ ( -- flag ) FLAGS@ 2 BIT@ ;
: FLAGS-PF! ( flag -- ) FLAGS@ 2 BIT! FLAGS! ;
: FLAGS-PF-SET ( -- ) TRUE FLAGS-PF! ;
: FLAGS-PF-CLR ( -- ) FALSE FLAGS-PF! ;
: FLAGS-AF@ ( -- flag ) FLAGS@ 4 BIT@ ;
: FLAGS-AF! ( flag -- ) FLAGS@ 4 BIT! FLAGS! ;
: FLAGS-AF-SET ( -- ) TRUE FLAGS-AF! ;
: FLAGS-AF-CLR ( -- ) FALSE FLAGS-AF! ;
: FLAGS-ZF@ ( -- flag ) FLAGS@ 6 BIT@ ;
: FLAGS-ZF! ( flag -- ) FLAGS@ 6 BIT! FLAGS! ;
: FLAGS-ZF-SET ( -- ) TRUE FLAGS-ZF! ;
: FLAGS-ZF-CLR ( -- ) FALSE FLAGS-ZF! ;
: FLAGS-SF@ ( -- flag ) FLAGS@ 7 BIT@ ;
: FLAGS-SF! ( flag -- ) FLAGS@ 7 BIT! FLAGS! ;
: FLAGS-SF-SET ( -- ) TRUE FLAGS-SF! ;
: FLAGS-SF-CLR ( -- ) FALSE FLAGS-SF! ;
: FLAGS-TF@ ( -- flag ) FLAGS@ 8 BIT@ ;
: FLAGS-TF! ( flag -- ) FLAGS@ 8 BIT! FLAGS! ;
: FLAGS-TF-SET ( -- ) TRUE FLAGS-TF! ;
: FLAGS-TF-CLR ( -- ) FALSE FLAGS-TF! ;
: FLAGS-IF@ ( -- flag ) FLAGS@ 9 BIT@ ;
: FLAGS-IF! ( flag -- ) FLAGS@ 9 BIT! FLAGS! ;
: FLAGS-IF-SET ( -- ) TRUE FLAGS-IF! ;
: FLAGS-IF-CLR ( -- ) FALSE FLAGS-IF! ;
: FLAGS-DF@ ( -- flag ) FLAGS@ 10 BIT@ ;
: FLAGS-DF! ( flag -- ) FLAGS@ 10 BIT! FLAGS! ;
: FLAGS-DF-SET ( -- ) TRUE FLAGS-DF! ;
: FLAGS-DF-CLR ( -- ) FALSE FLAGS-DF! ;
: FLAGS-OF@ ( -- flag ) FLAGS@ 11 BIT@ ;
: FLAGS-OF! ( flag -- ) FLAGS@ 11 BIT! FLAGS! ;
: FLAGS-OF-SET ( -- ) TRUE FLAGS-OF! ;
: FLAGS-OF-CLR ( -- ) FALSE FLAGS-OF! ;

\ 外设访问
\ PIC外设
: PIC-PIC1_CMD@ ( -- n ) PIC-PIC1_CMD C@ ;
: PIC-PIC1_CMD! ( n -- ) PIC-PIC1_CMD C! ;
: PIC-PIC1_DATA@ ( -- n ) PIC-PIC1_DATA C@ ;
: PIC-PIC1_DATA! ( n -- ) PIC-PIC1_DATA C! ;
: PIC-PIC2_CMD@ ( -- n ) PIC-PIC2_CMD C@ ;
: PIC-PIC2_CMD! ( n -- ) PIC-PIC2_CMD C! ;
: PIC-PIC2_DATA@ ( -- n ) PIC-PIC2_DATA C@ ;
: PIC-PIC2_DATA! ( n -- ) PIC-PIC2_DATA C! ;

\ PIT外设
: PIT-PIT_CH0@ ( -- n ) PIT-PIT_CH0 C@ ;
: PIT-PIT_CH0! ( n -- ) PIT-PIT_CH0 C! ;
: PIT-PIT_CH1@ ( -- n ) PIT-PIT_CH1 C@ ;
: PIT-PIT_CH1! ( n -- ) PIT-PIT_CH1 C! ;
: PIT-PIT_CH2@ ( -- n ) PIT-PIT_CH2 C@ ;
: PIT-PIT_CH2! ( n -- ) PIT-PIT_CH2 C! ;
: PIT-PIT_CMD@ ( -- n ) PIT-PIT_CMD C@ ;
: PIT-PIT_CMD! ( n -- ) PIT-PIT_CMD C! ;

\ PPI外设
: PPI-PPI_PA@ ( -- n ) PPI-PPI_PA C@ ;
: PPI-PPI_PA! ( n -- ) PPI-PPI_PA C! ;
: PPI-PPI_PB@ ( -- n ) PPI-PPI_PB C@ ;
: PPI-PPI_PB! ( n -- ) PPI-PPI_PB C! ;
: PPI-PPI_PC@ ( -- n ) PPI-PPI_PC C@ ;
: PPI-PPI_PC! ( n -- ) PPI-PPI_PC C! ;
: PPI-PPI_CMD@ ( -- n ) PPI-PPI_CMD C@ ;
: PPI-PPI_CMD! ( n -- ) PPI-PPI_CMD C! ;

\ =========================================
\ 设备初始化
\ =========================================

: _8086-INIT ( -- )
  \ 初始化8086设备
  ." 初始化8086..." CR

  \ 初始化寄存器
  0 AX!  \ Accumulator
  0 BX!  \ Base
  0 CX!  \ Counter
  0 DX!  \ Data
  0 SI!  \ Source Index
  0 DI!  \ Destination Index
  0 BP!  \ Base Pointer
  0 SP!  \ Stack Pointer
  0 IP!  \ Instruction Pointer
  0 CS!  \ Code Segment
  0 DS!  \ Data Segment
  0 ES!  \ Extra Segment
  0 SS!  \ Stack Segment
  0 FLAGS!  \ Flags Register

  \ 初始化外设
  \ 初始化PIC
  0 PIC-PIC1_CMD!  \ PIC1_CMD寄存器
  0 PIC-PIC1_DATA!  \ PIC1_DATA寄存器
  0 PIC-PIC2_CMD!  \ PIC2_CMD寄存器
  0 PIC-PIC2_DATA!  \ PIC2_DATA寄存器
  \ 初始化PIT
  0 PIT-PIT_CH0!  \ PIT_CH0寄存器
  0 PIT-PIT_CH1!  \ PIT_CH1寄存器
  0 PIT-PIT_CH2!  \ PIT_CH2寄存器
  0 PIT-PIT_CMD!  \ PIT_CMD寄存器
  \ 初始化PPI
  0 PPI-PPI_PA!  \ PPI_PA寄存器
  0 PPI-PPI_PB!  \ PPI_PB寄存器
  0 PPI-PPI_PC!  \ PPI_PC寄存器
  0 PPI-PPI_CMD!  \ PPI_CMD寄存器

  ." 8086初始化完成" CR
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
  AX@ AX .R 8 .R SPACE ."  AX: " AX@ .
  BX@ BX .R 8 .R SPACE ."  BX: " BX@ .
  CX@ CX .R 8 .R SPACE ."  CX: " CX@ .
  DX@ DX .R 8 .R SPACE ."  DX: " DX@ .
  SI@ SI .R 8 .R SPACE ."  SI: " SI@ .
  DI@ DI .R 8 .R SPACE ."  DI: " DI@ .
  BP@ BP .R 8 .R SPACE ."  BP: " BP@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  IP@ IP .R 8 .R SPACE ."  IP: " IP@ .
  CS@ CS .R 8 .R SPACE ."  CS: " CS@ .
  DS@ DS .R 8 .R SPACE ."  DS: " DS@ .
  ES@ ES .R 8 .R SPACE ."  ES: " ES@ .
  SS@ SS .R 8 .R SPACE ."  SS: " SS@ .
  FLAGS@ FLAGS .R 8 .R SPACE ."  FLAGS: " FLAGS@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Divide by zero
: INT-DIVIDE_ERROR-HANDLER ( -- )
  ." DIVIDE_ERROR中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DIVIDE_ERROR-ENABLE ( -- )
  INT-DIVIDE_ERROR INT-ENABLE
;

: INT-DIVIDE_ERROR-DISABLE ( -- )
  INT-DIVIDE_ERROR INT-DISABLE
;

\ Single step
: INT-DEBUG-HANDLER ( -- )
  ." DEBUG中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DEBUG-ENABLE ( -- )
  INT-DEBUG INT-ENABLE
;

: INT-DEBUG-DISABLE ( -- )
  INT-DEBUG INT-DISABLE
;

\ Non-maskable interrupt
: INT-NMI-HANDLER ( -- )
  ." NMI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NMI-ENABLE ( -- )
  INT-NMI INT-ENABLE
;

: INT-NMI-DISABLE ( -- )
  INT-NMI INT-DISABLE
;

\ Breakpoint
: INT-BREAKPOINT-HANDLER ( -- )
  ." BREAKPOINT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-BREAKPOINT-ENABLE ( -- )
  INT-BREAKPOINT INT-ENABLE
;

: INT-BREAKPOINT-DISABLE ( -- )
  INT-BREAKPOINT INT-DISABLE
;

\ INTO detected overflow
: INT-OVERFLOW-HANDLER ( -- )
  ." OVERFLOW中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-OVERFLOW-ENABLE ( -- )
  INT-OVERFLOW INT-ENABLE
;

: INT-OVERFLOW-DISABLE ( -- )
  INT-OVERFLOW INT-DISABLE
;

\ Timer interrupt
: INT-IRQ0-HANDLER ( -- )
  ." IRQ0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ0-ENABLE ( -- )
  INT-IRQ0 INT-ENABLE
;

: INT-IRQ0-DISABLE ( -- )
  INT-IRQ0 INT-DISABLE
;

\ Keyboard interrupt
: INT-IRQ1-HANDLER ( -- )
  ." IRQ1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ1-ENABLE ( -- )
  INT-IRQ1 INT-ENABLE
;

: INT-IRQ1-DISABLE ( -- )
  INT-IRQ1 INT-DISABLE
;

\ Cascade
: INT-IRQ2-HANDLER ( -- )
  ." IRQ2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ2-ENABLE ( -- )
  INT-IRQ2 INT-ENABLE
;

: INT-IRQ2-DISABLE ( -- )
  INT-IRQ2 INT-DISABLE
;

\ COM2
: INT-IRQ3-HANDLER ( -- )
  ." IRQ3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ3-ENABLE ( -- )
  INT-IRQ3 INT-ENABLE
;

: INT-IRQ3-DISABLE ( -- )
  INT-IRQ3 INT-DISABLE
;

\ COM1
: INT-IRQ4-HANDLER ( -- )
  ." IRQ4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ4-ENABLE ( -- )
  INT-IRQ4 INT-ENABLE
;

: INT-IRQ4-DISABLE ( -- )
  INT-IRQ4 INT-DISABLE
;

\ LPT2
: INT-IRQ5-HANDLER ( -- )
  ." IRQ5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ5-ENABLE ( -- )
  INT-IRQ5 INT-ENABLE
;

: INT-IRQ5-DISABLE ( -- )
  INT-IRQ5 INT-DISABLE
;

\ Floppy disk
: INT-IRQ6-HANDLER ( -- )
  ." IRQ6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ6-ENABLE ( -- )
  INT-IRQ6 INT-ENABLE
;

: INT-IRQ6-DISABLE ( -- )
  INT-IRQ6 INT-DISABLE
;

\ LPT1
: INT-IRQ7-HANDLER ( -- )
  ." IRQ7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ7-ENABLE ( -- )
  INT-IRQ7 INT-ENABLE
;

: INT-IRQ7-DISABLE ( -- )
  INT-IRQ7 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  _8086-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
