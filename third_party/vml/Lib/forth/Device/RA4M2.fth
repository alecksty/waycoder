\ RA4M2设备定义 - Forth文件
\ 生成自: Renesas/RA/RA4M2
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
\ CPU架构: ARM-Cortex-M4
\ 位宽: 32位
\ 时钟频率: 100000000 Hz

\ =========================================
\ RA4M2设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" RA4M2" ;
: MANUFACTURER  S" Renesas" ;
: FAMILY        S" RA" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4" ;
32 CONSTANT BITS
100000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x04 CONSTANT R1  \ 
0x08 CONSTANT R2  \ 
0x0C CONSTANT R3  \ 
0x10 CONSTANT R4  \ 
0x14 CONSTANT R5  \ 
0x34 CONSTANT SP  \ 
0x38 CONSTANT LR  \ 
0x3C CONSTANT PC  \ 

\ 内存段定义
0x00000000 CONSTANT FLASH-START
0x0003FFFF CONSTANT FLASH-END
262144 CONSTANT FLASH-SIZE  \ 
0x1FFE0000 CONSTANT SRAM-START
0x1FFE7FFF CONSTANT SRAM-END
32768 CONSTANT SRAM-SIZE  \ SRAM0
0x20000000 CONSTANT SRAM1-START
0x20017FFF CONSTANT SRAM1-END
98304 CONSTANT SRAM1-SIZE  \ SRAM1
0x40000000 CONSTANT PERIPHERAL-START
0x400FFFFF CONSTANT PERIPHERAL-END
1048576 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Module Stop Control
0x40020000 CONSTANT MSTP-BASE
0x20 CONSTANT MSTP-MSTPCR_A
9 CONSTANT MSTP-MSTPCR_A-MSTP41  \ GPIO A stop
10 CONSTANT MSTP-MSTPCR_A-MSTP42  \ GPIO B stop
0x24 CONSTANT MSTP-MSTPCR_B
0x28 CONSTANT MSTP-MSTPCR_C
0x2C CONSTANT MSTP-MSTPCR_D
\ Interrupt Controller Unit
0x40030000 CONSTANT ICU-BASE
0x600 CONSTANT ICU-IRQCR0
0x602 CONSTANT ICU-IRQCR1
\ General Purpose I/O Port A
0x40040000 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-PDR
0x04 CONSTANT GPIOA-PODR
0x08 CONSTANT GPIOA-PIDR
0x10 CONSTANT GPIOA-PMR
0x18 CONSTANT GPIOA-PCR
\ General Purpose I/O Port B
0x40040020 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-PDR
0x04 CONSTANT GPIOB-PODR
0x08 CONSTANT GPIOB-PIDR
0x10 CONSTANT GPIOB-PMR
\ SCI UART 0
0x40070000 CONSTANT SCIUART0-BASE
0x00 CONSTANT SCIUART0-SCR
0x04 CONSTANT SCIUART0-BRR
0x08 CONSTANT SCIUART0-TDR
0x0C CONSTANT SCIUART0-RDR
0x10 CONSTANT SCIUART0-SSR

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
24 CONSTANT INT-SCIUART0_RXI  \ SCI UART0 Receive Interrupt
25 CONSTANT INT-SCIUART0_TXI  \ SCI UART0 Transmit Interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 L@ ;
: R0! ( n -- ) R0 L! ;

: R1@ ( -- n ) R1 L@ ;
: R1! ( n -- ) R1 L! ;

: R2@ ( -- n ) R2 L@ ;
: R2! ( n -- ) R2 L! ;

: R3@ ( -- n ) R3 L@ ;
: R3! ( n -- ) R3 L! ;

: R4@ ( -- n ) R4 L@ ;
: R4! ( n -- ) R4 L! ;

: R5@ ( -- n ) R5 L@ ;
: R5! ( n -- ) R5 L! ;

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ MSTP外设
: MSTP-MSTPCR_A@ ( -- n ) MSTP-MSTPCR_A L@ ;
: MSTP-MSTPCR_A! ( n -- ) MSTP-MSTPCR_A L! ;
: MSTP-MSTPCR_A-MSTP41@ ( -- flag ) MSTP-MSTPCR_A@ 9 BIT@ ;
: MSTP-MSTPCR_A-MSTP41! ( flag -- ) MSTP-MSTPCR_A@ 9 BIT! MSTP-MSTPCR_A! ;
: MSTP-MSTPCR_A-MSTP42@ ( -- flag ) MSTP-MSTPCR_A@ 10 BIT@ ;
: MSTP-MSTPCR_A-MSTP42! ( flag -- ) MSTP-MSTPCR_A@ 10 BIT! MSTP-MSTPCR_A! ;
: MSTP-MSTPCR_B@ ( -- n ) MSTP-MSTPCR_B L@ ;
: MSTP-MSTPCR_B! ( n -- ) MSTP-MSTPCR_B L! ;
: MSTP-MSTPCR_C@ ( -- n ) MSTP-MSTPCR_C L@ ;
: MSTP-MSTPCR_C! ( n -- ) MSTP-MSTPCR_C L! ;
: MSTP-MSTPCR_D@ ( -- n ) MSTP-MSTPCR_D L@ ;
: MSTP-MSTPCR_D! ( n -- ) MSTP-MSTPCR_D L! ;

\ ICU外设
: ICU-IRQCR0@ ( -- n ) ICU-IRQCR0 @ ;
: ICU-IRQCR0! ( n -- ) ICU-IRQCR0 ! ;
: ICU-IRQCR1@ ( -- n ) ICU-IRQCR1 @ ;
: ICU-IRQCR1! ( n -- ) ICU-IRQCR1 ! ;

\ GPIOA外设
: GPIOA-PDR@ ( -- n ) GPIOA-PDR @ ;
: GPIOA-PDR! ( n -- ) GPIOA-PDR ! ;
: GPIOA-PODR@ ( -- n ) GPIOA-PODR @ ;
: GPIOA-PODR! ( n -- ) GPIOA-PODR ! ;
: GPIOA-PIDR@ ( -- n ) GPIOA-PIDR @ ;
: GPIOA-PIDR! ( n -- ) GPIOA-PIDR ! ;
: GPIOA-PMR@ ( -- n ) GPIOA-PMR @ ;
: GPIOA-PMR! ( n -- ) GPIOA-PMR ! ;
: GPIOA-PCR@ ( -- n ) GPIOA-PCR L@ ;
: GPIOA-PCR! ( n -- ) GPIOA-PCR L! ;

\ GPIOB外设
: GPIOB-PDR@ ( -- n ) GPIOB-PDR @ ;
: GPIOB-PDR! ( n -- ) GPIOB-PDR ! ;
: GPIOB-PODR@ ( -- n ) GPIOB-PODR @ ;
: GPIOB-PODR! ( n -- ) GPIOB-PODR ! ;
: GPIOB-PIDR@ ( -- n ) GPIOB-PIDR @ ;
: GPIOB-PIDR! ( n -- ) GPIOB-PIDR ! ;
: GPIOB-PMR@ ( -- n ) GPIOB-PMR @ ;
: GPIOB-PMR! ( n -- ) GPIOB-PMR ! ;

\ SCIUART0外设
: SCIUART0-SCR@ ( -- n ) SCIUART0-SCR C@ ;
: SCIUART0-SCR! ( n -- ) SCIUART0-SCR C! ;
: SCIUART0-BRR@ ( -- n ) SCIUART0-BRR C@ ;
: SCIUART0-BRR! ( n -- ) SCIUART0-BRR C! ;
: SCIUART0-TDR@ ( -- n ) SCIUART0-TDR C@ ;
: SCIUART0-TDR! ( n -- ) SCIUART0-TDR C! ;
: SCIUART0-RDR@ ( -- n ) SCIUART0-RDR C@ ;
: SCIUART0-RDR! ( n -- ) SCIUART0-RDR C! ;
: SCIUART0-SSR@ ( -- n ) SCIUART0-SSR C@ ;
: SCIUART0-SSR! ( n -- ) SCIUART0-SSR C! ;

\ =========================================
\ 设备初始化
\ =========================================

: RA4M2-INIT ( -- )
  \ 初始化RA4M2设备
  ." 初始化RA4M2..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R3!  \ 
  0 R4!  \ 
  0 R5!  \ 
  0 SP!  \ 
  0 LR!  \ 
  0 PC!  \ 

  \ 初始化外设
  \ 初始化MSTP
  0 MSTP-MSTPCR_A!  \ MSTPCR_A寄存器
  0 MSTP-MSTPCR_B!  \ MSTPCR_B寄存器
  0 MSTP-MSTPCR_C!  \ MSTPCR_C寄存器
  0 MSTP-MSTPCR_D!  \ MSTPCR_D寄存器
  \ 初始化ICU
  0 ICU-IRQCR0!  \ IRQCR0寄存器
  0 ICU-IRQCR1!  \ IRQCR1寄存器
  \ 初始化GPIOA
  0 GPIOA-PDR!  \ PDR寄存器
  0 GPIOA-PODR!  \ PODR寄存器
  0 GPIOA-PIDR!  \ PIDR寄存器
  0 GPIOA-PMR!  \ PMR寄存器
  0 GPIOA-PCR!  \ PCR寄存器
  \ 初始化GPIOB
  0 GPIOB-PDR!  \ PDR寄存器
  0 GPIOB-PODR!  \ PODR寄存器
  0 GPIOB-PIDR!  \ PIDR寄存器
  0 GPIOB-PMR!  \ PMR寄存器
  \ 初始化SCIUART0
  0 SCIUART0-SCR!  \ SCR寄存器
  0 SCIUART0-BRR!  \ BRR寄存器
  0 SCIUART0-TDR!  \ TDR寄存器
  0 SCIUART0-RDR!  \ RDR寄存器
  0 SCIUART0-SSR!  \ SSR寄存器

  ." RA4M2初始化完成" CR
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
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  LR@ LR .R 8 .R SPACE ."  LR: " LR@ .
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

\ 
: INT-SVCALL-HANDLER ( -- )
  ." SVCall中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SVCALL-ENABLE ( -- )
  INT-SVCALL INT-ENABLE
;

: INT-SVCALL-DISABLE ( -- )
  INT-SVCALL INT-DISABLE
;

\ SCI UART0 Receive Interrupt
: INT-SCIUART0_RXI-HANDLER ( -- )
  ." SCIUART0_RXI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SCIUART0_RXI-ENABLE ( -- )
  INT-SCIUART0_RXI INT-ENABLE
;

: INT-SCIUART0_RXI-DISABLE ( -- )
  INT-SCIUART0_RXI INT-DISABLE
;

\ SCI UART0 Transmit Interrupt
: INT-SCIUART0_TXI-HANDLER ( -- )
  ." SCIUART0_TXI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SCIUART0_TXI-ENABLE ( -- )
  INT-SCIUART0_TXI INT-ENABLE
;

: INT-SCIUART0_TXI-DISABLE ( -- )
  INT-SCIUART0_TXI INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  RA4M2-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
