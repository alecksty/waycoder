\ STM32F303CCT6设备定义 - Forth文件
\ 生成自: STMicroelectronics/STM32/STM32F303CCT6
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
\ CPU架构: ARM-Cortex-M4F
\ 位宽: 32位
\ 时钟频率: 72000000 Hz

\ =========================================
\ STM32F303CCT6设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" STM32F303CCT6" ;
: MANUFACTURER  S" STMicroelectronics" ;
: FAMILY        S" STM32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4F" ;
32 CONSTANT BITS
72000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ USART 1
0x40013800 CONSTANT USART1-BASE
0x00 CONSTANT USART1-SR
0x04 CONSTANT USART1-DR
0x08 CONSTANT USART1-BRR
0x0C CONSTANT USART1-CR1
0x10 CONSTANT USART1-CR2
0x14 CONSTANT USART1-CR3
\ USART 2
0x40004400 CONSTANT USART2-BASE
0x00 CONSTANT USART2-SR
0x04 CONSTANT USART2-DR
0x08 CONSTANT USART2-BRR
0x0C CONSTANT USART2-CR1
\ USART 3
0x40004800 CONSTANT USART3-BASE
0x00 CONSTANT USART3-SR
0x04 CONSTANT USART3-DR
0x08 CONSTANT USART3-BRR
0x0C CONSTANT USART3-CR1
\ GPIO Port A
0x48000000 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-MODER
0x04 CONSTANT GPIOA-OTYPER
0x08 CONSTANT GPIOA-OSPEEDR
0x0C CONSTANT GPIOA-PUPDR
0x10 CONSTANT GPIOA-IDR
0x14 CONSTANT GPIOA-ODR
0x18 CONSTANT GPIOA-BSRR
0x20 CONSTANT GPIOA-AFRL
0x24 CONSTANT GPIOA-AFRH
\ 高级定时器 1
0x40012C00 CONSTANT TIM1-BASE
0x00 CONSTANT TIM1-CR1
0x24 CONSTANT TIM1-CNT
0x28 CONSTANT TIM1-PSC
0x2C CONSTANT TIM1-ARR
0x34 CONSTANT TIM1-CCR1
\ ADC 1
0x50000000 CONSTANT ADC1-BASE
0x00 CONSTANT ADC1-SR
0x08 CONSTANT ADC1-CR
0x0C CONSTANT ADC1-CFGR
0x14 CONSTANT ADC1-SMPR1
0x40 CONSTANT ADC1-DR

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ USART1外设
: USART1-SR@ ( -- n ) USART1-SR L@ ;
: USART1-SR! ( n -- ) USART1-SR L! ;
: USART1-DR@ ( -- n ) USART1-DR L@ ;
: USART1-DR! ( n -- ) USART1-DR L! ;
: USART1-BRR@ ( -- n ) USART1-BRR L@ ;
: USART1-BRR! ( n -- ) USART1-BRR L! ;
: USART1-CR1@ ( -- n ) USART1-CR1 L@ ;
: USART1-CR1! ( n -- ) USART1-CR1 L! ;
: USART1-CR2@ ( -- n ) USART1-CR2 L@ ;
: USART1-CR2! ( n -- ) USART1-CR2 L! ;
: USART1-CR3@ ( -- n ) USART1-CR3 L@ ;
: USART1-CR3! ( n -- ) USART1-CR3 L! ;

\ USART2外设
: USART2-SR@ ( -- n ) USART2-SR L@ ;
: USART2-SR! ( n -- ) USART2-SR L! ;
: USART2-DR@ ( -- n ) USART2-DR L@ ;
: USART2-DR! ( n -- ) USART2-DR L! ;
: USART2-BRR@ ( -- n ) USART2-BRR L@ ;
: USART2-BRR! ( n -- ) USART2-BRR L! ;
: USART2-CR1@ ( -- n ) USART2-CR1 L@ ;
: USART2-CR1! ( n -- ) USART2-CR1 L! ;

\ USART3外设
: USART3-SR@ ( -- n ) USART3-SR L@ ;
: USART3-SR! ( n -- ) USART3-SR L! ;
: USART3-DR@ ( -- n ) USART3-DR L@ ;
: USART3-DR! ( n -- ) USART3-DR L! ;
: USART3-BRR@ ( -- n ) USART3-BRR L@ ;
: USART3-BRR! ( n -- ) USART3-BRR L! ;
: USART3-CR1@ ( -- n ) USART3-CR1 L@ ;
: USART3-CR1! ( n -- ) USART3-CR1 L! ;

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
: GPIOA-AFRL@ ( -- n ) GPIOA-AFRL L@ ;
: GPIOA-AFRL! ( n -- ) GPIOA-AFRL L! ;
: GPIOA-AFRH@ ( -- n ) GPIOA-AFRH L@ ;
: GPIOA-AFRH! ( n -- ) GPIOA-AFRH L! ;

\ TIM1外设
: TIM1-CR1@ ( -- n ) TIM1-CR1 L@ ;
: TIM1-CR1! ( n -- ) TIM1-CR1 L! ;
: TIM1-CNT@ ( -- n ) TIM1-CNT L@ ;
: TIM1-CNT! ( n -- ) TIM1-CNT L! ;
: TIM1-PSC@ ( -- n ) TIM1-PSC L@ ;
: TIM1-PSC! ( n -- ) TIM1-PSC L! ;
: TIM1-ARR@ ( -- n ) TIM1-ARR L@ ;
: TIM1-ARR! ( n -- ) TIM1-ARR L! ;
: TIM1-CCR1@ ( -- n ) TIM1-CCR1 L@ ;
: TIM1-CCR1! ( n -- ) TIM1-CCR1 L! ;

\ ADC1外设
: ADC1-SR@ ( -- n ) ADC1-SR L@ ;
: ADC1-SR! ( n -- ) ADC1-SR L! ;
: ADC1-CR@ ( -- n ) ADC1-CR L@ ;
: ADC1-CR! ( n -- ) ADC1-CR L! ;
: ADC1-CFGR@ ( -- n ) ADC1-CFGR L@ ;
: ADC1-CFGR! ( n -- ) ADC1-CFGR L! ;
: ADC1-SMPR1@ ( -- n ) ADC1-SMPR1 L@ ;
: ADC1-SMPR1! ( n -- ) ADC1-SMPR1 L! ;
: ADC1-DR@ ( -- n ) ADC1-DR L@ ;
: ADC1-DR! ( n -- ) ADC1-DR L! ;

\ =========================================
\ 设备初始化
\ =========================================

: STM32F303CCT6-INIT ( -- )
  \ 初始化STM32F303CCT6设备
  ." 初始化STM32F303CCT6..." CR


  \ 初始化外设
  \ 初始化USART1
  0 USART1-SR!  \ SR寄存器
  0 USART1-DR!  \ DR寄存器
  0 USART1-BRR!  \ BRR寄存器
  0 USART1-CR1!  \ CR1寄存器
  0 USART1-CR2!  \ CR2寄存器
  0 USART1-CR3!  \ CR3寄存器
  \ 初始化USART2
  0 USART2-SR!  \ SR寄存器
  0 USART2-DR!  \ DR寄存器
  0 USART2-BRR!  \ BRR寄存器
  0 USART2-CR1!  \ CR1寄存器
  \ 初始化USART3
  0 USART3-SR!  \ SR寄存器
  0 USART3-DR!  \ DR寄存器
  0 USART3-BRR!  \ BRR寄存器
  0 USART3-CR1!  \ CR1寄存器
  \ 初始化GPIOA
  0 GPIOA-MODER!  \ MODER寄存器
  0 GPIOA-OTYPER!  \ OTYPER寄存器
  0 GPIOA-OSPEEDR!  \ OSPEEDR寄存器
  0 GPIOA-PUPDR!  \ PUPDR寄存器
  0 GPIOA-IDR!  \ IDR寄存器
  0 GPIOA-ODR!  \ ODR寄存器
  0 GPIOA-BSRR!  \ BSRR寄存器
  0 GPIOA-AFRL!  \ AFRL寄存器
  0 GPIOA-AFRH!  \ AFRH寄存器
  \ 初始化TIM1
  0 TIM1-CR1!  \ CR1寄存器
  0 TIM1-CNT!  \ CNT寄存器
  0 TIM1-PSC!  \ PSC寄存器
  0 TIM1-ARR!  \ ARR寄存器
  0 TIM1-CCR1!  \ CCR1寄存器
  \ 初始化ADC1
  0 ADC1-SR!  \ SR寄存器
  0 ADC1-CR!  \ CR寄存器
  0 ADC1-CFGR!  \ CFGR寄存器
  0 ADC1-SMPR1!  \ SMPR1寄存器
  0 ADC1-DR!  \ DR寄存器

  ." STM32F303CCT6初始化完成" CR
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
  STM32F303CCT6-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
