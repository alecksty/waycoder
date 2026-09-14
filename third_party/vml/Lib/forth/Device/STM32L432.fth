\ STM32L432设备定义 - Forth文件
\ 生成自: STMicroelectronics/STM32/STM32L432
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4 MCU ultra-low-power with 256KB Flash, 64KB RAM, 80MHz
\ CPU架构: ARM-Cortex-M4
\ 位宽: 32位
\ 时钟频率: 80000000 Hz

\ =========================================
\ STM32L432设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" STM32L432" ;
: MANUFACTURER  S" STMicroelectronics" ;
: FAMILY        S" STM32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4" ;
32 CONSTANT BITS
80000000 CONSTANT CLOCK-FREQ

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
0x0803FFFF CONSTANT FLASH-END
262144 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x2000FFFF CONSTANT SRAM-END
65536 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x4007FFFF CONSTANT PERIPHERAL-END
524288 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCC-BASE
0x00 CONSTANT RCC-CR
0x08 CONSTANT RCC-CFGR
0x0C CONSTANT RCC-PLLCFGR
0x38 CONSTANT RCC-AHB1ENR
0 CONSTANT RCC-AHB1ENR-GPIOAEN  \ GPIOA clock enable
1 CONSTANT RCC-AHB1ENR-GPIOBEN  \ GPIOB clock enable
0x58 CONSTANT RCC-APB1ENR1
0x60 CONSTANT RCC-APB2ENR
\ General Purpose I/O Port A
0x48000000 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-MODER
0x04 CONSTANT GPIOA-OTYPER
0x08 CONSTANT GPIOA-OSPEEDR
0x0C CONSTANT GPIOA-PUPDR
0x10 CONSTANT GPIOA-IDR
0x14 CONSTANT GPIOA-ODR
0x18 CONSTANT GPIOA-BSRR
0x28 CONSTANT GPIOA-BRR
\ General Purpose I/O Port B
0x48000400 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-MODER
0x04 CONSTANT GPIOB-OTYPER
0x08 CONSTANT GPIOB-OSPEEDR
0x0C CONSTANT GPIOB-PUPDR
0x10 CONSTANT GPIOB-IDR
0x14 CONSTANT GPIOB-ODR
0x18 CONSTANT GPIOB-BSRR
0x28 CONSTANT GPIOB-BRR
\ Low-power UART 1
0x40008000 CONSTANT LPUART1-BASE
0x00 CONSTANT LPUART1-CR1
0x0C CONSTANT LPUART1-BRR
0x24 CONSTANT LPUART1-RDR
0x28 CONSTANT LPUART1-TDR

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
53 CONSTANT INT-LPUART1  \ LPUART1 Global Interrupt

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
: RCC-CR@ ( -- n ) RCC-CR L@ ;
: RCC-CR! ( n -- ) RCC-CR L! ;
: RCC-CFGR@ ( -- n ) RCC-CFGR L@ ;
: RCC-CFGR! ( n -- ) RCC-CFGR L! ;
: RCC-PLLCFGR@ ( -- n ) RCC-PLLCFGR L@ ;
: RCC-PLLCFGR! ( n -- ) RCC-PLLCFGR L! ;
: RCC-AHB1ENR@ ( -- n ) RCC-AHB1ENR L@ ;
: RCC-AHB1ENR! ( n -- ) RCC-AHB1ENR L! ;
: RCC-AHB1ENR-GPIOAEN@ ( -- flag ) RCC-AHB1ENR@ 0 BIT@ ;
: RCC-AHB1ENR-GPIOAEN! ( flag -- ) RCC-AHB1ENR@ 0 BIT! RCC-AHB1ENR! ;
: RCC-AHB1ENR-GPIOBEN@ ( -- flag ) RCC-AHB1ENR@ 1 BIT@ ;
: RCC-AHB1ENR-GPIOBEN! ( flag -- ) RCC-AHB1ENR@ 1 BIT! RCC-AHB1ENR! ;
: RCC-APB1ENR1@ ( -- n ) RCC-APB1ENR1 L@ ;
: RCC-APB1ENR1! ( n -- ) RCC-APB1ENR1 L! ;
: RCC-APB2ENR@ ( -- n ) RCC-APB2ENR L@ ;
: RCC-APB2ENR! ( n -- ) RCC-APB2ENR L! ;

\ GPIOA外设
: GPIOA-MODER@ ( -- n ) GPIOA-MODER L@ ;
: GPIOA-MODER! ( n -- ) GPIOA-MODER L! ;
: GPIOA-OTYPER@ ( -- n ) GPIOA-OTYPER L@ ;
: GPIOA-OTYPER! ( n -- ) GPIOA-OTYPER L! ;
: GPIOA-OSPEEDR@ ( -- n ) GPIOA-OSPEEDR L@ ;
: GPIOA-OSPEEDR! ( n -- ) GPIOA-OSPEEDR L! ;
: GPIOA-PUPDR@ ( -- n ) GPIOA-PUPDR L@ ;
: GPIOA-PUPDR! ( n -- ) GPIOA-PUPDR L! ;
: GPIOA-IDR@ ( -- n ) GPIOA-IDR L@ ;
: GPIOA-IDR! ( n -- ) GPIOA-IDR L! ;
: GPIOA-ODR@ ( -- n ) GPIOA-ODR L@ ;
: GPIOA-ODR! ( n -- ) GPIOA-ODR L! ;
: GPIOA-BSRR@ ( -- n ) GPIOA-BSRR L@ ;
: GPIOA-BSRR! ( n -- ) GPIOA-BSRR L! ;
: GPIOA-BRR@ ( -- n ) GPIOA-BRR L@ ;
: GPIOA-BRR! ( n -- ) GPIOA-BRR L! ;

\ GPIOB外设
: GPIOB-MODER@ ( -- n ) GPIOB-MODER L@ ;
: GPIOB-MODER! ( n -- ) GPIOB-MODER L! ;
: GPIOB-OTYPER@ ( -- n ) GPIOB-OTYPER L@ ;
: GPIOB-OTYPER! ( n -- ) GPIOB-OTYPER L! ;
: GPIOB-OSPEEDR@ ( -- n ) GPIOB-OSPEEDR L@ ;
: GPIOB-OSPEEDR! ( n -- ) GPIOB-OSPEEDR L! ;
: GPIOB-PUPDR@ ( -- n ) GPIOB-PUPDR L@ ;
: GPIOB-PUPDR! ( n -- ) GPIOB-PUPDR L! ;
: GPIOB-IDR@ ( -- n ) GPIOB-IDR L@ ;
: GPIOB-IDR! ( n -- ) GPIOB-IDR L! ;
: GPIOB-ODR@ ( -- n ) GPIOB-ODR L@ ;
: GPIOB-ODR! ( n -- ) GPIOB-ODR L! ;
: GPIOB-BSRR@ ( -- n ) GPIOB-BSRR L@ ;
: GPIOB-BSRR! ( n -- ) GPIOB-BSRR L! ;
: GPIOB-BRR@ ( -- n ) GPIOB-BRR L@ ;
: GPIOB-BRR! ( n -- ) GPIOB-BRR L! ;

\ LPUART1外设
: LPUART1-CR1@ ( -- n ) LPUART1-CR1 L@ ;
: LPUART1-CR1! ( n -- ) LPUART1-CR1 L! ;
: LPUART1-BRR@ ( -- n ) LPUART1-BRR L@ ;
: LPUART1-BRR! ( n -- ) LPUART1-BRR L! ;
: LPUART1-RDR@ ( -- n ) LPUART1-RDR L@ ;
: LPUART1-RDR! ( n -- ) LPUART1-RDR L! ;
: LPUART1-TDR@ ( -- n ) LPUART1-TDR L@ ;
: LPUART1-TDR! ( n -- ) LPUART1-TDR L! ;

\ =========================================
\ 设备初始化
\ =========================================

: STM32L432-INIT ( -- )
  \ 初始化STM32L432设备
  ." 初始化STM32L432..." CR

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
  0 RCC-CR!  \ CR寄存器
  0 RCC-CFGR!  \ CFGR寄存器
  0 RCC-PLLCFGR!  \ PLLCFGR寄存器
  0 RCC-AHB1ENR!  \ AHB1ENR寄存器
  0 RCC-APB1ENR1!  \ APB1ENR1寄存器
  0 RCC-APB2ENR!  \ APB2ENR寄存器
  \ 初始化GPIOA
  0 GPIOA-MODER!  \ MODER寄存器
  0 GPIOA-OTYPER!  \ OTYPER寄存器
  0 GPIOA-OSPEEDR!  \ OSPEEDR寄存器
  0 GPIOA-PUPDR!  \ PUPDR寄存器
  0 GPIOA-IDR!  \ IDR寄存器
  0 GPIOA-ODR!  \ ODR寄存器
  0 GPIOA-BSRR!  \ BSRR寄存器
  0 GPIOA-BRR!  \ BRR寄存器
  \ 初始化GPIOB
  0 GPIOB-MODER!  \ MODER寄存器
  0 GPIOB-OTYPER!  \ OTYPER寄存器
  0 GPIOB-OSPEEDR!  \ OSPEEDR寄存器
  0 GPIOB-PUPDR!  \ PUPDR寄存器
  0 GPIOB-IDR!  \ IDR寄存器
  0 GPIOB-ODR!  \ ODR寄存器
  0 GPIOB-BSRR!  \ BSRR寄存器
  0 GPIOB-BRR!  \ BRR寄存器
  \ 初始化LPUART1
  0 LPUART1-CR1!  \ CR1寄存器
  0 LPUART1-BRR!  \ BRR寄存器
  0 LPUART1-RDR!  \ RDR寄存器
  0 LPUART1-TDR!  \ TDR寄存器

  ." STM32L432初始化完成" CR
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

\ LPUART1 Global Interrupt
: INT-LPUART1-HANDLER ( -- )
  ." LPUART1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-LPUART1-ENABLE ( -- )
  INT-LPUART1 INT-ENABLE
;

: INT-LPUART1-DISABLE ( -- )
  INT-LPUART1 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  STM32L432-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
