\ i.MX RT1062设备定义 - Forth文件
\ 生成自: NXP/i.MX RT/i.MX RT1062
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
\ CPU架构: ARM-Cortex-M7
\ 位宽: 32位
\ 时钟频率: 528000000 Hz

\ =========================================
\ i.MX RT1062设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" i.MX RT1062" ;
: MANUFACTURER  S" NXP" ;
: FAMILY        S" i.MX RT" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M7" ;
32 CONSTANT BITS
528000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ LPUART 1
0x40184000 CONSTANT UART1-BASE
0x000 CONSTANT UART1-VERID
0x010 CONSTANT UART1-CTRL
0x014 CONSTANT UART1-STAT
0x01C CONSTANT UART1-DATA
0x024 CONSTANT UART1-BAUD
\ LPUART 2
0x40188000 CONSTANT UART2-BASE
0x010 CONSTANT UART2-CTRL
0x014 CONSTANT UART2-STAT
0x01C CONSTANT UART2-DATA
0x024 CONSTANT UART2-BAUD
\ GPIO 1
0x401B8000 CONSTANT GPIO1-BASE
0x000 CONSTANT GPIO1-DR
0x004 CONSTANT GPIO1-GDIR
0x008 CONSTANT GPIO1-PSR
0x00C CONSTANT GPIO1-ICR1
0x010 CONSTANT GPIO1-ICR2
0x014 CONSTANT GPIO1-IMR
0x018 CONSTANT GPIO1-ISR
0x01C CONSTANT GPIO1-EDGE_SEL
\ GPT 定时器 1
0x401EC000 CONSTANT GPT1-BASE
0x000 CONSTANT GPT1-CR
0x004 CONSTANT GPT1-PR
0x008 CONSTANT GPT1-SR
0x00C CONSTANT GPT1-IR
0x010 CONSTANT GPT1-OCR1
0x024 CONSTANT GPT1-CNT
\ USB OTG 1
0x402E0000 CONSTANT USB1-BASE
0x000 CONSTANT USB1-ID
0x00C CONSTANT USB1-OTGSC
0x100 CONSTANT USB1-USBCMD
0x184 CONSTANT USB1-PORTSC1

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ UART1外设
: UART1-VERID@ ( -- n ) UART1-VERID L@ ;
: UART1-VERID! ( n -- ) UART1-VERID L! ;
: UART1-CTRL@ ( -- n ) UART1-CTRL L@ ;
: UART1-CTRL! ( n -- ) UART1-CTRL L! ;
: UART1-STAT@ ( -- n ) UART1-STAT L@ ;
: UART1-STAT! ( n -- ) UART1-STAT L! ;
: UART1-DATA@ ( -- n ) UART1-DATA L@ ;
: UART1-DATA! ( n -- ) UART1-DATA L! ;
: UART1-BAUD@ ( -- n ) UART1-BAUD L@ ;
: UART1-BAUD! ( n -- ) UART1-BAUD L! ;

\ UART2外设
: UART2-CTRL@ ( -- n ) UART2-CTRL L@ ;
: UART2-CTRL! ( n -- ) UART2-CTRL L! ;
: UART2-STAT@ ( -- n ) UART2-STAT L@ ;
: UART2-STAT! ( n -- ) UART2-STAT L! ;
: UART2-DATA@ ( -- n ) UART2-DATA L@ ;
: UART2-DATA! ( n -- ) UART2-DATA L! ;
: UART2-BAUD@ ( -- n ) UART2-BAUD L@ ;
: UART2-BAUD! ( n -- ) UART2-BAUD L! ;

\ GPIO1外设
: GPIO1-DR@ ( -- n ) GPIO1-DR L@ ;
: GPIO1-DR! ( n -- ) GPIO1-DR L! ;
: GPIO1-GDIR@ ( -- n ) GPIO1-GDIR L@ ;
: GPIO1-GDIR! ( n -- ) GPIO1-GDIR L! ;
: GPIO1-PSR@ ( -- n ) GPIO1-PSR L@ ;
: GPIO1-PSR! ( n -- ) GPIO1-PSR L! ;
: GPIO1-ICR1@ ( -- n ) GPIO1-ICR1 L@ ;
: GPIO1-ICR1! ( n -- ) GPIO1-ICR1 L! ;
: GPIO1-ICR2@ ( -- n ) GPIO1-ICR2 L@ ;
: GPIO1-ICR2! ( n -- ) GPIO1-ICR2 L! ;
: GPIO1-IMR@ ( -- n ) GPIO1-IMR L@ ;
: GPIO1-IMR! ( n -- ) GPIO1-IMR L! ;
: GPIO1-ISR@ ( -- n ) GPIO1-ISR L@ ;
: GPIO1-ISR! ( n -- ) GPIO1-ISR L! ;
: GPIO1-EDGE_SEL@ ( -- n ) GPIO1-EDGE_SEL L@ ;
: GPIO1-EDGE_SEL! ( n -- ) GPIO1-EDGE_SEL L! ;

\ GPT1外设
: GPT1-CR@ ( -- n ) GPT1-CR L@ ;
: GPT1-CR! ( n -- ) GPT1-CR L! ;
: GPT1-PR@ ( -- n ) GPT1-PR L@ ;
: GPT1-PR! ( n -- ) GPT1-PR L! ;
: GPT1-SR@ ( -- n ) GPT1-SR L@ ;
: GPT1-SR! ( n -- ) GPT1-SR L! ;
: GPT1-IR@ ( -- n ) GPT1-IR L@ ;
: GPT1-IR! ( n -- ) GPT1-IR L! ;
: GPT1-OCR1@ ( -- n ) GPT1-OCR1 L@ ;
: GPT1-OCR1! ( n -- ) GPT1-OCR1 L! ;
: GPT1-CNT@ ( -- n ) GPT1-CNT L@ ;
: GPT1-CNT! ( n -- ) GPT1-CNT L! ;

\ USB1外设
: USB1-ID@ ( -- n ) USB1-ID L@ ;
: USB1-ID! ( n -- ) USB1-ID L! ;
: USB1-OTGSC@ ( -- n ) USB1-OTGSC L@ ;
: USB1-OTGSC! ( n -- ) USB1-OTGSC L! ;
: USB1-USBCMD@ ( -- n ) USB1-USBCMD L@ ;
: USB1-USBCMD! ( n -- ) USB1-USBCMD L! ;
: USB1-PORTSC1@ ( -- n ) USB1-PORTSC1 L@ ;
: USB1-PORTSC1! ( n -- ) USB1-PORTSC1 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: I_MX_RT1062-INIT ( -- )
  \ 初始化i.MX RT1062设备
  ." 初始化i.MX RT1062..." CR


  \ 初始化外设
  \ 初始化UART1
  0 UART1-VERID!  \ VERID寄存器
  0 UART1-CTRL!  \ CTRL寄存器
  0 UART1-STAT!  \ STAT寄存器
  0 UART1-DATA!  \ DATA寄存器
  0 UART1-BAUD!  \ BAUD寄存器
  \ 初始化UART2
  0 UART2-CTRL!  \ CTRL寄存器
  0 UART2-STAT!  \ STAT寄存器
  0 UART2-DATA!  \ DATA寄存器
  0 UART2-BAUD!  \ BAUD寄存器
  \ 初始化GPIO1
  0 GPIO1-DR!  \ DR寄存器
  0 GPIO1-GDIR!  \ GDIR寄存器
  0 GPIO1-PSR!  \ PSR寄存器
  0 GPIO1-ICR1!  \ ICR1寄存器
  0 GPIO1-ICR2!  \ ICR2寄存器
  0 GPIO1-IMR!  \ IMR寄存器
  0 GPIO1-ISR!  \ ISR寄存器
  0 GPIO1-EDGE_SEL!  \ EDGE_SEL寄存器
  \ 初始化GPT1
  0 GPT1-CR!  \ CR寄存器
  0 GPT1-PR!  \ PR寄存器
  0 GPT1-SR!  \ SR寄存器
  0 GPT1-IR!  \ IR寄存器
  0 GPT1-OCR1!  \ OCR1寄存器
  0 GPT1-CNT!  \ CNT寄存器
  \ 初始化USB1
  0 USB1-ID!  \ ID寄存器
  0 USB1-OTGSC!  \ OTGSC寄存器
  0 USB1-USBCMD!  \ USBCMD寄存器
  0 USB1-PORTSC1!  \ PORTSC1寄存器

  ." i.MX RT1062初始化完成" CR
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
  I_MX_RT1062-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
