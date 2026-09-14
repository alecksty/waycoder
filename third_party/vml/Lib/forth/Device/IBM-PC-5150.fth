\ IBM-PC-5150设备定义 - Forth文件
\ 生成自: IBM/Personal Computer/IBM-PC-5150
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: Original IBM Personal Computer Model 5150
\ CPU架构: x86
\ 位宽: 16位
\ 时钟频率: 4772727 Hz

\ =========================================
\ IBM-PC-5150设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" IBM-PC-5150" ;
: MANUFACTURER  S" IBM" ;
: FAMILY        S" Personal Computer" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" x86" ;
16 CONSTANT BITS
4772727 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x0 CONSTANT AX  \ Accumulator Register
0x1 CONSTANT BX  \ Base Register
0x2 CONSTANT CX  \ Count Register
0x3 CONSTANT DX  \ Data Register
0x4 CONSTANT SI  \ Source Index
0x5 CONSTANT DI  \ Destination Index
0x6 CONSTANT BP  \ Base Pointer
0x7 CONSTANT SP  \ Stack Pointer
0x8 CONSTANT CS  \ Code Segment
0x9 CONSTANT DS  \ Data Segment
0xA CONSTANT ES  \ Extra Segment
0xB CONSTANT SS  \ Stack Segment
0xC CONSTANT IP  \ Instruction Pointer
0xD CONSTANT FLAGS  \ Flags Register
0 CONSTANT FLAGS-CF  \ Carry Flag
2 CONSTANT FLAGS-PF  \ Parity Flag
4 CONSTANT FLAGS-AF  \ Auxiliary Carry Flag
6 CONSTANT FLAGS-ZF  \ Zero Flag
7 CONSTANT FLAGS-SF  \ Sign Flag
8 CONSTANT FLAGS-TF  \ Trap Flag
9 CONSTANT FLAGS-IF  \ Interrupt Enable Flag
10 CONSTANT FLAGS-DF  \ Direction Flag
11 CONSTANT FLAGS-OF  \ Overflow Flag

\ 内存段定义
0xF0000 CONSTANT BIOS-START
0xFFFFF CONSTANT BIOS-END
65536 CONSTANT BIOS-SIZE  \ BIOS ROM
0xB8000 CONSTANT VIDEO-START
0xBFFFF CONSTANT VIDEO-END
32768 CONSTANT VIDEO-SIZE  \ Video Memory
0x00000 CONSTANT CONVENTIONAL-START
0x9FFFF CONSTANT CONVENTIONAL-END
640 CONSTANT CONVENTIONAL-SIZE  \ Conventional Memory (640KB)
0x100000 CONSTANT EXTENDED-START
0x10FFFF CONSTANT EXTENDED-END
64 CONSTANT EXTENDED-SIZE  \ Extended Memory (64KB)

\ 外设定义
\ Programmable Interrupt Controller
0x20 CONSTANT PIC-BASE
0x20 CONSTANT PIC-PIC1_CMD
0x21 CONSTANT PIC-PIC1_DATA
0xA0 CONSTANT PIC-PIC2_CMD
0xA1 CONSTANT PIC-PIC2_DATA
\ Programmable Interval Timer
0x40 CONSTANT PIT-BASE
0x40 CONSTANT PIT-PIT_CH0
0x41 CONSTANT PIT-PIT_CH1
0x42 CONSTANT PIT-PIT_CH2
0x43 CONSTANT PIT-PIT_CTRL
\ Programmable Peripheral Interface
0x60 CONSTANT PPI-BASE
0x60 CONSTANT PPI-PPI_PA
0x61 CONSTANT PPI-PPI_PB
0x62 CONSTANT PPI-PPI_PC
0x63 CONSTANT PPI-PPI_CTRL
\ Direct Memory Access Controller
0x00 CONSTANT DMA-BASE
0x00 CONSTANT DMA-DMA_CH0_ADDR
0x01 CONSTANT DMA-DMA_CH0_COUNT
0x08 CONSTANT DMA-DMA_CMD
0x0A CONSTANT DMA-DMA_MASK
0x0B CONSTANT DMA-DMA_MODE
\ Color Graphics Adapter
0x3D4 CONSTANT CGA-BASE
0x3D4 CONSTANT CGA-CGA_INDEX
0x3D5 CONSTANT CGA-CGA_DATA
0x3D8 CONSTANT CGA-CGA_MODE
0x3D9 CONSTANT CGA-CGA_COLOR

\ 中断向量定义
0 CONSTANT INT-DIVIDE_ERROR  \ Divide Error
1 CONSTANT INT-SINGLE_STEP  \ Single Step
2 CONSTANT INT-NMI  \ Non-Maskable Interrupt
3 CONSTANT INT-BREAKPOINT  \ Breakpoint
4 CONSTANT INT-OVERFLOW  \ Overflow
5 CONSTANT INT-PRINT_SCREEN  \ Print Screen
8 CONSTANT INT-IRQ0  \ Timer Interrupt
9 CONSTANT INT-IRQ1  \ Keyboard Interrupt
10 CONSTANT INT-IRQ2  \ Cascade (8259A)
11 CONSTANT INT-IRQ3  \ COM2
12 CONSTANT INT-IRQ4  \ COM1
13 CONSTANT INT-IRQ5  \ LPT2
14 CONSTANT INT-IRQ6  \ Floppy Disk
15 CONSTANT INT-IRQ7  \ LPT1
16 CONSTANT INT-IRQ8  \ Real Time Clock
19 CONSTANT INT-IRQ11  \ Reserved
21 CONSTANT INT-IRQ13  \ Coprocessor
31 CONSTANT INT-IRQ15  \ Reserved

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power Supply
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-RESET  \ System Reset
4 CONSTANT PIN-CLK  \ System Clock (4.77MHz)
5 CONSTANT PIN-READY  \ CPU Ready Signal
6 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
7 CONSTANT PIN-INTR  \ Interrupt Request
8 CONSTANT PIN-HLDA  \ Hold Acknowledge
9 CONSTANT PIN-HOLD  \ Hold Request
10 CONSTANT PIN-MEMR  \ Memory Read
11 CONSTANT PIN-MEMW  \ Memory Write
12 CONSTANT PIN-IOR  \ I/O Read
13 CONSTANT PIN-IOW  \ I/O Write
14 CONSTANT PIN-ALE  \ Address Latch Enable
15 CONSTANT PIN-DTR  \ Data Terminal Ready (Serial)
16 CONSTANT PIN-RTS  \ Request To Send (Serial)
17 CONSTANT PIN-CTS  \ Clear To Send (Serial)
18 CONSTANT PIN-DSR  \ Data Set Ready (Serial)
19 CONSTANT PIN-RI  \ Ring Indicator (Serial)
20 CONSTANT PIN-DCD  \ Data Carrier Detect (Serial)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: AX@ ( -- n ) AX @ ;
: AX! ( n -- ) AX ! ;

: BX@ ( -- n ) BX @ ;
: BX! ( n -- ) BX ! ;

: CX@ ( -- n ) CX @ ;
: CX! ( n -- ) CX ! ;

: DX@ ( -- n ) DX @ ;
: DX! ( n -- ) DX ! ;

: SI@ ( -- n ) SI @ ;
: SI! ( n -- ) SI ! ;

: DI@ ( -- n ) DI @ ;
: DI! ( n -- ) DI ! ;

: BP@ ( -- n ) BP @ ;
: BP! ( n -- ) BP ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: CS@ ( -- n ) CS @ ;
: CS! ( n -- ) CS ! ;

: DS@ ( -- n ) DS @ ;
: DS! ( n -- ) DS ! ;

: ES@ ( -- n ) ES @ ;
: ES! ( n -- ) ES ! ;

: SS@ ( -- n ) SS @ ;
: SS! ( n -- ) SS ! ;

: IP@ ( -- n ) IP @ ;
: IP! ( n -- ) IP ! ;

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
: PIT-PIT_CTRL@ ( -- n ) PIT-PIT_CTRL C@ ;
: PIT-PIT_CTRL! ( n -- ) PIT-PIT_CTRL C! ;

\ PPI外设
: PPI-PPI_PA@ ( -- n ) PPI-PPI_PA C@ ;
: PPI-PPI_PA! ( n -- ) PPI-PPI_PA C! ;
: PPI-PPI_PB@ ( -- n ) PPI-PPI_PB C@ ;
: PPI-PPI_PB! ( n -- ) PPI-PPI_PB C! ;
: PPI-PPI_PC@ ( -- n ) PPI-PPI_PC C@ ;
: PPI-PPI_PC! ( n -- ) PPI-PPI_PC C! ;
: PPI-PPI_CTRL@ ( -- n ) PPI-PPI_CTRL C@ ;
: PPI-PPI_CTRL! ( n -- ) PPI-PPI_CTRL C! ;

\ DMA外设
: DMA-DMA_CH0_ADDR@ ( -- n ) DMA-DMA_CH0_ADDR @ ;
: DMA-DMA_CH0_ADDR! ( n -- ) DMA-DMA_CH0_ADDR ! ;
: DMA-DMA_CH0_COUNT@ ( -- n ) DMA-DMA_CH0_COUNT @ ;
: DMA-DMA_CH0_COUNT! ( n -- ) DMA-DMA_CH0_COUNT ! ;
: DMA-DMA_CMD@ ( -- n ) DMA-DMA_CMD C@ ;
: DMA-DMA_CMD! ( n -- ) DMA-DMA_CMD C! ;
: DMA-DMA_MASK@ ( -- n ) DMA-DMA_MASK C@ ;
: DMA-DMA_MASK! ( n -- ) DMA-DMA_MASK C! ;
: DMA-DMA_MODE@ ( -- n ) DMA-DMA_MODE C@ ;
: DMA-DMA_MODE! ( n -- ) DMA-DMA_MODE C! ;

\ CGA外设
: CGA-CGA_INDEX@ ( -- n ) CGA-CGA_INDEX C@ ;
: CGA-CGA_INDEX! ( n -- ) CGA-CGA_INDEX C! ;
: CGA-CGA_DATA@ ( -- n ) CGA-CGA_DATA C@ ;
: CGA-CGA_DATA! ( n -- ) CGA-CGA_DATA C! ;
: CGA-CGA_MODE@ ( -- n ) CGA-CGA_MODE C@ ;
: CGA-CGA_MODE! ( n -- ) CGA-CGA_MODE C! ;
: CGA-CGA_COLOR@ ( -- n ) CGA-CGA_COLOR C@ ;
: CGA-CGA_COLOR! ( n -- ) CGA-CGA_COLOR C! ;

\ =========================================
\ 设备初始化
\ =========================================

: IBM_PC_5150-INIT ( -- )
  \ 初始化IBM-PC-5150设备
  ." 初始化IBM-PC-5150..." CR

  \ 初始化寄存器
  0 AX!  \ Accumulator Register
  0 BX!  \ Base Register
  0 CX!  \ Count Register
  0 DX!  \ Data Register
  0 SI!  \ Source Index
  0 DI!  \ Destination Index
  0 BP!  \ Base Pointer
  0 SP!  \ Stack Pointer
  0 CS!  \ Code Segment
  0 DS!  \ Data Segment
  0 ES!  \ Extra Segment
  0 SS!  \ Stack Segment
  0 IP!  \ Instruction Pointer
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
  0 PIT-PIT_CTRL!  \ PIT_CTRL寄存器
  \ 初始化PPI
  0 PPI-PPI_PA!  \ PPI_PA寄存器
  0 PPI-PPI_PB!  \ PPI_PB寄存器
  0 PPI-PPI_PC!  \ PPI_PC寄存器
  0 PPI-PPI_CTRL!  \ PPI_CTRL寄存器
  \ 初始化DMA
  0 DMA-DMA_CH0_ADDR!  \ DMA_CH0_ADDR寄存器
  0 DMA-DMA_CH0_COUNT!  \ DMA_CH0_COUNT寄存器
  0 DMA-DMA_CMD!  \ DMA_CMD寄存器
  0 DMA-DMA_MASK!  \ DMA_MASK寄存器
  0 DMA-DMA_MODE!  \ DMA_MODE寄存器
  \ 初始化CGA
  0 CGA-CGA_INDEX!  \ CGA_INDEX寄存器
  0 CGA-CGA_DATA!  \ CGA_DATA寄存器
  0 CGA-CGA_MODE!  \ CGA_MODE寄存器
  0 CGA-CGA_COLOR!  \ CGA_COLOR寄存器

  ." IBM-PC-5150初始化完成" CR
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
  CS@ CS .R 8 .R SPACE ."  CS: " CS@ .
  DS@ DS .R 8 .R SPACE ."  DS: " DS@ .
  ES@ ES .R 8 .R SPACE ."  ES: " ES@ .
  SS@ SS .R 8 .R SPACE ."  SS: " SS@ .
  IP@ IP .R 8 .R SPACE ."  IP: " IP@ .
  FLAGS@ FLAGS .R 8 .R SPACE ."  FLAGS: " FLAGS@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Divide Error
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

\ Single Step
: INT-SINGLE_STEP-HANDLER ( -- )
  ." SINGLE_STEP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SINGLE_STEP-ENABLE ( -- )
  INT-SINGLE_STEP INT-ENABLE
;

: INT-SINGLE_STEP-DISABLE ( -- )
  INT-SINGLE_STEP INT-DISABLE
;

\ Non-Maskable Interrupt
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

\ Overflow
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

\ Print Screen
: INT-PRINT_SCREEN-HANDLER ( -- )
  ." PRINT_SCREEN中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PRINT_SCREEN-ENABLE ( -- )
  INT-PRINT_SCREEN INT-ENABLE
;

: INT-PRINT_SCREEN-DISABLE ( -- )
  INT-PRINT_SCREEN INT-DISABLE
;

\ Timer Interrupt
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

\ Keyboard Interrupt
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

\ Cascade (8259A)
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

\ Floppy Disk
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

\ Real Time Clock
: INT-IRQ8-HANDLER ( -- )
  ." IRQ8中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ8-ENABLE ( -- )
  INT-IRQ8 INT-ENABLE
;

: INT-IRQ8-DISABLE ( -- )
  INT-IRQ8 INT-DISABLE
;

\ Reserved
: INT-IRQ11-HANDLER ( -- )
  ." IRQ11中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ11-ENABLE ( -- )
  INT-IRQ11 INT-ENABLE
;

: INT-IRQ11-DISABLE ( -- )
  INT-IRQ11 INT-DISABLE
;

\ Coprocessor
: INT-IRQ13-HANDLER ( -- )
  ." IRQ13中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ13-ENABLE ( -- )
  INT-IRQ13 INT-ENABLE
;

: INT-IRQ13-DISABLE ( -- )
  INT-IRQ13 INT-DISABLE
;

\ Reserved
: INT-IRQ15-HANDLER ( -- )
  ." IRQ15中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ15-ENABLE ( -- )
  INT-IRQ15 INT-ENABLE
;

: INT-IRQ15-DISABLE ( -- )
  INT-IRQ15 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  IBM_PC_5150-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
