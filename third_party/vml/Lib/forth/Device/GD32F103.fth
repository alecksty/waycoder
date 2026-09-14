\ GD32F103设备定义 - Forth文件
\ 生成自: GigaDevice/GD32/GD32F103
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
\ CPU架构: ARM-Cortex-M3
\ 位宽: 32位
\ 时钟频率: 108000000 Hz

\ =========================================
\ GD32F103设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" GD32F103" ;
: MANUFACTURER  S" GigaDevice" ;
: FAMILY        S" GD32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M3" ;
32 CONSTANT BITS
108000000 CONSTANT CLOCK-FREQ

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
0x08000000 CONSTANT FLASH-START
0x0801FFFF CONSTANT FLASH-END
131072 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x20004FFF CONSTANT SRAM-END
20480 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x4003FFFF CONSTANT PERIPHERAL-END
262144 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCC-BASE
0x00 CONSTANT RCC-CTLR
0x04 CONSTANT RCC-CFGR0
0x18 CONSTANT RCC-APB2PCENR
2 CONSTANT RCC-APB2PCENR-IOPAEN  \ GPIOA clock enable
3 CONSTANT RCC-APB2PCENR-IOPBEN  \ GPIOB clock enable
4 CONSTANT RCC-APB2PCENR-IOPCEN  \ GPIOC clock enable
14 CONSTANT RCC-APB2PCENR-USART0EN  \ USART0 clock enable
0x1C CONSTANT RCC-APB1PCENR
17 CONSTANT RCC-APB1PCENR-USART1EN  \ USART1 clock enable
\ General Purpose I/O Port A
0x40010800 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-CTL0
0x04 CONSTANT GPIOA-CTL1
0x08 CONSTANT GPIOA-ISTAT
0x0C CONSTANT GPIOA-OCTL
0x10 CONSTANT GPIOA-BOP
0x14 CONSTANT GPIOA-BC
\ General Purpose I/O Port B
0x40010C00 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-CTL0
0x04 CONSTANT GPIOB-CTL1
0x08 CONSTANT GPIOB-ISTAT
0x0C CONSTANT GPIOB-OCTL
0x10 CONSTANT GPIOB-BOP
0x14 CONSTANT GPIOB-BC
\ General Purpose I/O Port C
0x40011000 CONSTANT GPIOC-BASE
0x00 CONSTANT GPIOC-CTL0
0x04 CONSTANT GPIOC-CTL1
0x08 CONSTANT GPIOC-ISTAT
0x0C CONSTANT GPIOC-OCTL
0x10 CONSTANT GPIOC-BOP
0x14 CONSTANT GPIOC-BC
\ USART0
0x40013800 CONSTANT USART0-BASE
0x00 CONSTANT USART0-STATR
0x04 CONSTANT USART0-DATAR
0x08 CONSTANT USART0-BRR
0x0C CONSTANT USART0-CTLR1
\ USART1
0x40004400 CONSTANT USART1-BASE
0x00 CONSTANT USART1-STATR
0x04 CONSTANT USART1-DATAR
0x08 CONSTANT USART1-BRR
0x0C CONSTANT USART1-CTLR1

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
25 CONSTANT INT-USART0  \ USART0 Global Interrupt
37 CONSTANT INT-USART1  \ USART1 Global Interrupt

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
\ RCC外设
: RCC-CTLR@ ( -- n ) RCC-CTLR L@ ;
: RCC-CTLR! ( n -- ) RCC-CTLR L! ;
: RCC-CFGR0@ ( -- n ) RCC-CFGR0 L@ ;
: RCC-CFGR0! ( n -- ) RCC-CFGR0 L! ;
: RCC-APB2PCENR@ ( -- n ) RCC-APB2PCENR L@ ;
: RCC-APB2PCENR! ( n -- ) RCC-APB2PCENR L! ;
: RCC-APB2PCENR-IOPAEN@ ( -- flag ) RCC-APB2PCENR@ 2 BIT@ ;
: RCC-APB2PCENR-IOPAEN! ( flag -- ) RCC-APB2PCENR@ 2 BIT! RCC-APB2PCENR! ;
: RCC-APB2PCENR-IOPBEN@ ( -- flag ) RCC-APB2PCENR@ 3 BIT@ ;
: RCC-APB2PCENR-IOPBEN! ( flag -- ) RCC-APB2PCENR@ 3 BIT! RCC-APB2PCENR! ;
: RCC-APB2PCENR-IOPCEN@ ( -- flag ) RCC-APB2PCENR@ 4 BIT@ ;
: RCC-APB2PCENR-IOPCEN! ( flag -- ) RCC-APB2PCENR@ 4 BIT! RCC-APB2PCENR! ;
: RCC-APB2PCENR-USART0EN@ ( -- flag ) RCC-APB2PCENR@ 14 BIT@ ;
: RCC-APB2PCENR-USART0EN! ( flag -- ) RCC-APB2PCENR@ 14 BIT! RCC-APB2PCENR! ;
: RCC-APB1PCENR@ ( -- n ) RCC-APB1PCENR L@ ;
: RCC-APB1PCENR! ( n -- ) RCC-APB1PCENR L! ;
: RCC-APB1PCENR-USART1EN@ ( -- flag ) RCC-APB1PCENR@ 17 BIT@ ;
: RCC-APB1PCENR-USART1EN! ( flag -- ) RCC-APB1PCENR@ 17 BIT! RCC-APB1PCENR! ;

\ GPIOA外设
: GPIOA-CTL0@ ( -- n ) GPIOA-CTL0 L@ ;
: GPIOA-CTL0! ( n -- ) GPIOA-CTL0 L! ;
: GPIOA-CTL1@ ( -- n ) GPIOA-CTL1 L@ ;
: GPIOA-CTL1! ( n -- ) GPIOA-CTL1 L! ;
: GPIOA-ISTAT@ ( -- n ) GPIOA-ISTAT L@ ;
: GPIOA-ISTAT! ( n -- ) GPIOA-ISTAT L! ;
: GPIOA-OCTL@ ( -- n ) GPIOA-OCTL L@ ;
: GPIOA-OCTL! ( n -- ) GPIOA-OCTL L! ;
: GPIOA-BOP@ ( -- n ) GPIOA-BOP L@ ;
: GPIOA-BOP! ( n -- ) GPIOA-BOP L! ;
: GPIOA-BC@ ( -- n ) GPIOA-BC L@ ;
: GPIOA-BC! ( n -- ) GPIOA-BC L! ;

\ GPIOB外设
: GPIOB-CTL0@ ( -- n ) GPIOB-CTL0 L@ ;
: GPIOB-CTL0! ( n -- ) GPIOB-CTL0 L! ;
: GPIOB-CTL1@ ( -- n ) GPIOB-CTL1 L@ ;
: GPIOB-CTL1! ( n -- ) GPIOB-CTL1 L! ;
: GPIOB-ISTAT@ ( -- n ) GPIOB-ISTAT L@ ;
: GPIOB-ISTAT! ( n -- ) GPIOB-ISTAT L! ;
: GPIOB-OCTL@ ( -- n ) GPIOB-OCTL L@ ;
: GPIOB-OCTL! ( n -- ) GPIOB-OCTL L! ;
: GPIOB-BOP@ ( -- n ) GPIOB-BOP L@ ;
: GPIOB-BOP! ( n -- ) GPIOB-BOP L! ;
: GPIOB-BC@ ( -- n ) GPIOB-BC L@ ;
: GPIOB-BC! ( n -- ) GPIOB-BC L! ;

\ GPIOC外设
: GPIOC-CTL0@ ( -- n ) GPIOC-CTL0 L@ ;
: GPIOC-CTL0! ( n -- ) GPIOC-CTL0 L! ;
: GPIOC-CTL1@ ( -- n ) GPIOC-CTL1 L@ ;
: GPIOC-CTL1! ( n -- ) GPIOC-CTL1 L! ;
: GPIOC-ISTAT@ ( -- n ) GPIOC-ISTAT L@ ;
: GPIOC-ISTAT! ( n -- ) GPIOC-ISTAT L! ;
: GPIOC-OCTL@ ( -- n ) GPIOC-OCTL L@ ;
: GPIOC-OCTL! ( n -- ) GPIOC-OCTL L! ;
: GPIOC-BOP@ ( -- n ) GPIOC-BOP L@ ;
: GPIOC-BOP! ( n -- ) GPIOC-BOP L! ;
: GPIOC-BC@ ( -- n ) GPIOC-BC L@ ;
: GPIOC-BC! ( n -- ) GPIOC-BC L! ;

\ USART0外设
: USART0-STATR@ ( -- n ) USART0-STATR L@ ;
: USART0-STATR! ( n -- ) USART0-STATR L! ;
: USART0-DATAR@ ( -- n ) USART0-DATAR L@ ;
: USART0-DATAR! ( n -- ) USART0-DATAR L! ;
: USART0-BRR@ ( -- n ) USART0-BRR L@ ;
: USART0-BRR! ( n -- ) USART0-BRR L! ;
: USART0-CTLR1@ ( -- n ) USART0-CTLR1 L@ ;
: USART0-CTLR1! ( n -- ) USART0-CTLR1 L! ;

\ USART1外设
: USART1-STATR@ ( -- n ) USART1-STATR L@ ;
: USART1-STATR! ( n -- ) USART1-STATR L! ;
: USART1-DATAR@ ( -- n ) USART1-DATAR L@ ;
: USART1-DATAR! ( n -- ) USART1-DATAR L! ;
: USART1-BRR@ ( -- n ) USART1-BRR L@ ;
: USART1-BRR! ( n -- ) USART1-BRR L! ;
: USART1-CTLR1@ ( -- n ) USART1-CTLR1 L@ ;
: USART1-CTLR1! ( n -- ) USART1-CTLR1 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: GD32F103-INIT ( -- )
  \ 初始化GD32F103设备
  ." 初始化GD32F103..." CR

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
  \ 初始化RCC
  0 RCC-CTLR!  \ CTLR寄存器
  0 RCC-CFGR0!  \ CFGR0寄存器
  0 RCC-APB2PCENR!  \ APB2PCENR寄存器
  0 RCC-APB1PCENR!  \ APB1PCENR寄存器
  \ 初始化GPIOA
  0 GPIOA-CTL0!  \ CTL0寄存器
  0 GPIOA-CTL1!  \ CTL1寄存器
  0 GPIOA-ISTAT!  \ ISTAT寄存器
  0 GPIOA-OCTL!  \ OCTL寄存器
  0 GPIOA-BOP!  \ BOP寄存器
  0 GPIOA-BC!  \ BC寄存器
  \ 初始化GPIOB
  0 GPIOB-CTL0!  \ CTL0寄存器
  0 GPIOB-CTL1!  \ CTL1寄存器
  0 GPIOB-ISTAT!  \ ISTAT寄存器
  0 GPIOB-OCTL!  \ OCTL寄存器
  0 GPIOB-BOP!  \ BOP寄存器
  0 GPIOB-BC!  \ BC寄存器
  \ 初始化GPIOC
  0 GPIOC-CTL0!  \ CTL0寄存器
  0 GPIOC-CTL1!  \ CTL1寄存器
  0 GPIOC-ISTAT!  \ ISTAT寄存器
  0 GPIOC-OCTL!  \ OCTL寄存器
  0 GPIOC-BOP!  \ BOP寄存器
  0 GPIOC-BC!  \ BC寄存器
  \ 初始化USART0
  0 USART0-STATR!  \ STATR寄存器
  0 USART0-DATAR!  \ DATAR寄存器
  0 USART0-BRR!  \ BRR寄存器
  0 USART0-CTLR1!  \ CTLR1寄存器
  \ 初始化USART1
  0 USART1-STATR!  \ STATR寄存器
  0 USART1-DATAR!  \ DATAR寄存器
  0 USART1-BRR!  \ BRR寄存器
  0 USART1-CTLR1!  \ CTLR1寄存器

  ." GD32F103初始化完成" CR
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

\ USART0 Global Interrupt
: INT-USART0-HANDLER ( -- )
  ." USART0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART0-ENABLE ( -- )
  INT-USART0 INT-ENABLE
;

: INT-USART0-DISABLE ( -- )
  INT-USART0 INT-DISABLE
;

\ USART1 Global Interrupt
: INT-USART1-HANDLER ( -- )
  ." USART1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART1-ENABLE ( -- )
  INT-USART1 INT-ENABLE
;

: INT-USART1-DISABLE ( -- )
  INT-USART1 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  GD32F103-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
