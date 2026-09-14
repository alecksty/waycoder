\ TM4C123GH6PM设备定义 - Forth文件
\ 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
\ CPU架构: ARM-Cortex-M4F
\ 位宽: 32位
\ 时钟频率: 80000000 Hz

\ =========================================
\ TM4C123GH6PM设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" TM4C123GH6PM" ;
: MANUFACTURER  S" Texas Instruments" ;
: FAMILY        S" Tiva C" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4F" ;
32 CONSTANT BITS
80000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ UART 0
0x4000C000 CONSTANT UART0-BASE
0x000 CONSTANT UART0-DR
0x018 CONSTANT UART0-FR
0x024 CONSTANT UART0-IBRD
0x028 CONSTANT UART0-FBRD
0x02C CONSTANT UART0-LCRH
0x030 CONSTANT UART0-CTL
0x038 CONSTANT UART0-IM
0x03C CONSTANT UART0-RIS
0x044 CONSTANT UART0-ICR
\ UART 1
0x4000D000 CONSTANT UART1-BASE
0x000 CONSTANT UART1-DR
0x018 CONSTANT UART1-FR
0x024 CONSTANT UART1-IBRD
0x028 CONSTANT UART1-FBRD
0x02C CONSTANT UART1-LCRH
0x030 CONSTANT UART1-CTL
\ GPIO Port A
0x40004000 CONSTANT GPIOA-BASE
0x3FC CONSTANT GPIOA-DATA
0x400 CONSTANT GPIOA-DIR
0x404 CONSTANT GPIOA-IS
0x408 CONSTANT GPIOA-IBE
0x40C CONSTANT GPIOA-IEV
0x410 CONSTANT GPIOA-IM
0x414 CONSTANT GPIOA-RIS
0x418 CONSTANT GPIOA-MIS
0x41C CONSTANT GPIOA-ICR
0x420 CONSTANT GPIOA-AFSEL
0x51C CONSTANT GPIOA-DEN
\ 16/32-bit Timer 0
0x40030000 CONSTANT TIMER0-BASE
0x000 CONSTANT TIMER0-CFG
0x004 CONSTANT TIMER0-TAMR
0x00C CONSTANT TIMER0-CTL
0x028 CONSTANT TIMER0-ILR
0x038 CONSTANT TIMER0-V
0x024 CONSTANT TIMER0-ICR
\ ADC 0
0x40038000 CONSTANT ADC0-BASE
0x000 CONSTANT ADC0-ACTSS
0x014 CONSTANT ADC0-EMUX
0x040 CONSTANT ADC0-SSMUX0
0x048 CONSTANT ADC0-SSFIFO0
0x030 CONSTANT ADC0-PROC

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ UART0外设
: UART0-DR@ ( -- n ) UART0-DR L@ ;
: UART0-DR! ( n -- ) UART0-DR L! ;
: UART0-FR@ ( -- n ) UART0-FR L@ ;
: UART0-FR! ( n -- ) UART0-FR L! ;
: UART0-IBRD@ ( -- n ) UART0-IBRD L@ ;
: UART0-IBRD! ( n -- ) UART0-IBRD L! ;
: UART0-FBRD@ ( -- n ) UART0-FBRD L@ ;
: UART0-FBRD! ( n -- ) UART0-FBRD L! ;
: UART0-LCRH@ ( -- n ) UART0-LCRH L@ ;
: UART0-LCRH! ( n -- ) UART0-LCRH L! ;
: UART0-CTL@ ( -- n ) UART0-CTL L@ ;
: UART0-CTL! ( n -- ) UART0-CTL L! ;
: UART0-IM@ ( -- n ) UART0-IM L@ ;
: UART0-IM! ( n -- ) UART0-IM L! ;
: UART0-RIS@ ( -- n ) UART0-RIS L@ ;
: UART0-RIS! ( n -- ) UART0-RIS L! ;
: UART0-ICR@ ( -- n ) UART0-ICR L@ ;
: UART0-ICR! ( n -- ) UART0-ICR L! ;

\ UART1外设
: UART1-DR@ ( -- n ) UART1-DR L@ ;
: UART1-DR! ( n -- ) UART1-DR L! ;
: UART1-FR@ ( -- n ) UART1-FR L@ ;
: UART1-FR! ( n -- ) UART1-FR L! ;
: UART1-IBRD@ ( -- n ) UART1-IBRD L@ ;
: UART1-IBRD! ( n -- ) UART1-IBRD L! ;
: UART1-FBRD@ ( -- n ) UART1-FBRD L@ ;
: UART1-FBRD! ( n -- ) UART1-FBRD L! ;
: UART1-LCRH@ ( -- n ) UART1-LCRH L@ ;
: UART1-LCRH! ( n -- ) UART1-LCRH L! ;
: UART1-CTL@ ( -- n ) UART1-CTL L@ ;
: UART1-CTL! ( n -- ) UART1-CTL L! ;

\ GPIOA外设
: GPIOA-DATA@ ( -- n ) GPIOA-DATA L@ ;
: GPIOA-DATA! ( n -- ) GPIOA-DATA L! ;
: GPIOA-DIR@ ( -- n ) GPIOA-DIR L@ ;
: GPIOA-DIR! ( n -- ) GPIOA-DIR L! ;
: GPIOA-IS@ ( -- n ) GPIOA-IS L@ ;
: GPIOA-IS! ( n -- ) GPIOA-IS L! ;
: GPIOA-IBE@ ( -- n ) GPIOA-IBE L@ ;
: GPIOA-IBE! ( n -- ) GPIOA-IBE L! ;
: GPIOA-IEV@ ( -- n ) GPIOA-IEV L@ ;
: GPIOA-IEV! ( n -- ) GPIOA-IEV L! ;
: GPIOA-IM@ ( -- n ) GPIOA-IM L@ ;
: GPIOA-IM! ( n -- ) GPIOA-IM L! ;
: GPIOA-RIS@ ( -- n ) GPIOA-RIS L@ ;
: GPIOA-RIS! ( n -- ) GPIOA-RIS L! ;
: GPIOA-MIS@ ( -- n ) GPIOA-MIS L@ ;
: GPIOA-MIS! ( n -- ) GPIOA-MIS L! ;
: GPIOA-ICR@ ( -- n ) GPIOA-ICR L@ ;
: GPIOA-ICR! ( n -- ) GPIOA-ICR L! ;
: GPIOA-AFSEL@ ( -- n ) GPIOA-AFSEL L@ ;
: GPIOA-AFSEL! ( n -- ) GPIOA-AFSEL L! ;
: GPIOA-DEN@ ( -- n ) GPIOA-DEN L@ ;
: GPIOA-DEN! ( n -- ) GPIOA-DEN L! ;

\ TIMER0外设
: TIMER0-CFG@ ( -- n ) TIMER0-CFG L@ ;
: TIMER0-CFG! ( n -- ) TIMER0-CFG L! ;
: TIMER0-TAMR@ ( -- n ) TIMER0-TAMR L@ ;
: TIMER0-TAMR! ( n -- ) TIMER0-TAMR L! ;
: TIMER0-CTL@ ( -- n ) TIMER0-CTL L@ ;
: TIMER0-CTL! ( n -- ) TIMER0-CTL L! ;
: TIMER0-ILR@ ( -- n ) TIMER0-ILR L@ ;
: TIMER0-ILR! ( n -- ) TIMER0-ILR L! ;
: TIMER0-V@ ( -- n ) TIMER0-V L@ ;
: TIMER0-V! ( n -- ) TIMER0-V L! ;
: TIMER0-ICR@ ( -- n ) TIMER0-ICR L@ ;
: TIMER0-ICR! ( n -- ) TIMER0-ICR L! ;

\ ADC0外设
: ADC0-ACTSS@ ( -- n ) ADC0-ACTSS L@ ;
: ADC0-ACTSS! ( n -- ) ADC0-ACTSS L! ;
: ADC0-EMUX@ ( -- n ) ADC0-EMUX L@ ;
: ADC0-EMUX! ( n -- ) ADC0-EMUX L! ;
: ADC0-SSMUX0@ ( -- n ) ADC0-SSMUX0 L@ ;
: ADC0-SSMUX0! ( n -- ) ADC0-SSMUX0 L! ;
: ADC0-SSFIFO0@ ( -- n ) ADC0-SSFIFO0 L@ ;
: ADC0-SSFIFO0! ( n -- ) ADC0-SSFIFO0 L! ;
: ADC0-PROC@ ( -- n ) ADC0-PROC L@ ;
: ADC0-PROC! ( n -- ) ADC0-PROC L! ;

\ =========================================
\ 设备初始化
\ =========================================

: TM4C123GH6PM-INIT ( -- )
  \ 初始化TM4C123GH6PM设备
  ." 初始化TM4C123GH6PM..." CR


  \ 初始化外设
  \ 初始化UART0
  0 UART0-DR!  \ DR寄存器
  0 UART0-FR!  \ FR寄存器
  0 UART0-IBRD!  \ IBRD寄存器
  0 UART0-FBRD!  \ FBRD寄存器
  0 UART0-LCRH!  \ LCRH寄存器
  0 UART0-CTL!  \ CTL寄存器
  0 UART0-IM!  \ IM寄存器
  0 UART0-RIS!  \ RIS寄存器
  0 UART0-ICR!  \ ICR寄存器
  \ 初始化UART1
  0 UART1-DR!  \ DR寄存器
  0 UART1-FR!  \ FR寄存器
  0 UART1-IBRD!  \ IBRD寄存器
  0 UART1-FBRD!  \ FBRD寄存器
  0 UART1-LCRH!  \ LCRH寄存器
  0 UART1-CTL!  \ CTL寄存器
  \ 初始化GPIOA
  0 GPIOA-DATA!  \ DATA寄存器
  0 GPIOA-DIR!  \ DIR寄存器
  0 GPIOA-IS!  \ IS寄存器
  0 GPIOA-IBE!  \ IBE寄存器
  0 GPIOA-IEV!  \ IEV寄存器
  0 GPIOA-IM!  \ IM寄存器
  0 GPIOA-RIS!  \ RIS寄存器
  0 GPIOA-MIS!  \ MIS寄存器
  0 GPIOA-ICR!  \ ICR寄存器
  0 GPIOA-AFSEL!  \ AFSEL寄存器
  0 GPIOA-DEN!  \ DEN寄存器
  \ 初始化TIMER0
  0 TIMER0-CFG!  \ CFG寄存器
  0 TIMER0-TAMR!  \ TAMR寄存器
  0 TIMER0-CTL!  \ CTL寄存器
  0 TIMER0-ILR!  \ ILR寄存器
  0 TIMER0-V!  \ V寄存器
  0 TIMER0-ICR!  \ ICR寄存器
  \ 初始化ADC0
  0 ADC0-ACTSS!  \ ACTSS寄存器
  0 ADC0-EMUX!  \ EMUX寄存器
  0 ADC0-SSMUX0!  \ SSMUX0寄存器
  0 ADC0-SSFIFO0!  \ SSFIFO0寄存器
  0 ADC0-PROC!  \ PROC寄存器

  ." TM4C123GH6PM初始化完成" CR
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
  TM4C123GH6PM-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
