\ MSP432P401R设备定义 - Forth文件
\ 生成自: Texas Instruments/MSP432/MSP432P401R
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 64KB SRAM, 48MHz, FPU
\ CPU架构: ARM-Cortex-M4F
\ 位宽: 32位
\ 时钟频率: 48000000 Hz

\ =========================================
\ MSP432P401R设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MSP432P401R" ;
: MANUFACTURER  S" Texas Instruments" ;
: FAMILY        S" MSP432" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4F" ;
32 CONSTANT BITS
48000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ eUSCI_A0 UART
0x40001000 CONSTANT UART0-BASE
0x00 CONSTANT UART0-CTLW0
0x06 CONSTANT UART0-BRW
0x08 CONSTANT UART0-UCA0TXBUF
0x0A CONSTANT UART0-UCA0RXBUF
0x0C CONSTANT UART0-IFG
0x0E CONSTANT UART0-IE
\ eUSCI_A1 UART
0x40002000 CONSTANT UART1-BASE
0x00 CONSTANT UART1-CTLW0
0x06 CONSTANT UART1-BRW
0x08 CONSTANT UART1-TXBUF
0x0A CONSTANT UART1-RXBUF
0x0C CONSTANT UART1-IFG
0x0E CONSTANT UART1-IE
\ Timer_A0 16bit
0x40003000 CONSTANT TIMER0-BASE
0x00 CONSTANT TIMER0-CTL
0x10 CONSTANT TIMER0-R
0x12 CONSTANT TIMER0-CCR0
0x14 CONSTANT TIMER0-CCR1
0x16 CONSTANT TIMER0-CCR2
0x20 CONSTANT TIMER0-EX0
\ ADC14 14-bit
0x40006000 CONSTANT ADC14-BASE
0x00 CONSTANT ADC14-CTL0
0x02 CONSTANT ADC14-CTL1
0x04 CONSTANT ADC14-LO
0x06 CONSTANT ADC14-HI
0x08 CONSTANT ADC14-MCTL0
0x20 CONSTANT ADC14-MEM0

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ UART0外设
: UART0-CTLW0@ ( -- n ) UART0-CTLW0 @ ;
: UART0-CTLW0! ( n -- ) UART0-CTLW0 ! ;
: UART0-BRW@ ( -- n ) UART0-BRW @ ;
: UART0-BRW! ( n -- ) UART0-BRW ! ;
: UART0-UCA0TXBUF@ ( -- n ) UART0-UCA0TXBUF @ ;
: UART0-UCA0TXBUF! ( n -- ) UART0-UCA0TXBUF ! ;
: UART0-UCA0RXBUF@ ( -- n ) UART0-UCA0RXBUF @ ;
: UART0-UCA0RXBUF! ( n -- ) UART0-UCA0RXBUF ! ;
: UART0-IFG@ ( -- n ) UART0-IFG @ ;
: UART0-IFG! ( n -- ) UART0-IFG ! ;
: UART0-IE@ ( -- n ) UART0-IE @ ;
: UART0-IE! ( n -- ) UART0-IE ! ;

\ UART1外设
: UART1-CTLW0@ ( -- n ) UART1-CTLW0 @ ;
: UART1-CTLW0! ( n -- ) UART1-CTLW0 ! ;
: UART1-BRW@ ( -- n ) UART1-BRW @ ;
: UART1-BRW! ( n -- ) UART1-BRW ! ;
: UART1-TXBUF@ ( -- n ) UART1-TXBUF @ ;
: UART1-TXBUF! ( n -- ) UART1-TXBUF ! ;
: UART1-RXBUF@ ( -- n ) UART1-RXBUF @ ;
: UART1-RXBUF! ( n -- ) UART1-RXBUF ! ;
: UART1-IFG@ ( -- n ) UART1-IFG @ ;
: UART1-IFG! ( n -- ) UART1-IFG ! ;
: UART1-IE@ ( -- n ) UART1-IE @ ;
: UART1-IE! ( n -- ) UART1-IE ! ;

\ TIMER0外设
: TIMER0-CTL@ ( -- n ) TIMER0-CTL @ ;
: TIMER0-CTL! ( n -- ) TIMER0-CTL ! ;
: TIMER0-R@ ( -- n ) TIMER0-R @ ;
: TIMER0-R! ( n -- ) TIMER0-R ! ;
: TIMER0-CCR0@ ( -- n ) TIMER0-CCR0 @ ;
: TIMER0-CCR0! ( n -- ) TIMER0-CCR0 ! ;
: TIMER0-CCR1@ ( -- n ) TIMER0-CCR1 @ ;
: TIMER0-CCR1! ( n -- ) TIMER0-CCR1 ! ;
: TIMER0-CCR2@ ( -- n ) TIMER0-CCR2 @ ;
: TIMER0-CCR2! ( n -- ) TIMER0-CCR2 ! ;
: TIMER0-EX0@ ( -- n ) TIMER0-EX0 @ ;
: TIMER0-EX0! ( n -- ) TIMER0-EX0 ! ;

\ ADC14外设
: ADC14-CTL0@ ( -- n ) ADC14-CTL0 @ ;
: ADC14-CTL0! ( n -- ) ADC14-CTL0 ! ;
: ADC14-CTL1@ ( -- n ) ADC14-CTL1 @ ;
: ADC14-CTL1! ( n -- ) ADC14-CTL1 ! ;
: ADC14-LO@ ( -- n ) ADC14-LO @ ;
: ADC14-LO! ( n -- ) ADC14-LO ! ;
: ADC14-HI@ ( -- n ) ADC14-HI @ ;
: ADC14-HI! ( n -- ) ADC14-HI ! ;
: ADC14-MCTL0@ ( -- n ) ADC14-MCTL0 @ ;
: ADC14-MCTL0! ( n -- ) ADC14-MCTL0 ! ;
: ADC14-MEM0@ ( -- n ) ADC14-MEM0 @ ;
: ADC14-MEM0! ( n -- ) ADC14-MEM0 ! ;

\ =========================================
\ 设备初始化
\ =========================================

: MSP432P401R-INIT ( -- )
  \ 初始化MSP432P401R设备
  ." 初始化MSP432P401R..." CR


  \ 初始化外设
  \ 初始化UART0
  0 UART0-CTLW0!  \ CTLW0寄存器
  0 UART0-BRW!  \ BRW寄存器
  0 UART0-UCA0TXBUF!  \ UCA0TXBUF寄存器
  0 UART0-UCA0RXBUF!  \ UCA0RXBUF寄存器
  0 UART0-IFG!  \ IFG寄存器
  0 UART0-IE!  \ IE寄存器
  \ 初始化UART1
  0 UART1-CTLW0!  \ CTLW0寄存器
  0 UART1-BRW!  \ BRW寄存器
  0 UART1-TXBUF!  \ TXBUF寄存器
  0 UART1-RXBUF!  \ RXBUF寄存器
  0 UART1-IFG!  \ IFG寄存器
  0 UART1-IE!  \ IE寄存器
  \ 初始化TIMER0
  0 TIMER0-CTL!  \ CTL寄存器
  0 TIMER0-R!  \ R寄存器
  0 TIMER0-CCR0!  \ CCR0寄存器
  0 TIMER0-CCR1!  \ CCR1寄存器
  0 TIMER0-CCR2!  \ CCR2寄存器
  0 TIMER0-EX0!  \ EX0寄存器
  \ 初始化ADC14
  0 ADC14-CTL0!  \ CTL0寄存器
  0 ADC14-CTL1!  \ CTL1寄存器
  0 ADC14-LO!  \ LO寄存器
  0 ADC14-HI!  \ HI寄存器
  0 ADC14-MCTL0!  \ MCTL0寄存器
  0 ADC14-MEM0!  \ MEM0寄存器

  ." MSP432P401R初始化完成" CR
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  MSP432P401R-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
