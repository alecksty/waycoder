(*
 * STM32H743梯形图定义
 * 生成自: STMicroelectronics/STM32/STM32H743
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M7 MCU with 2MB Flash, 1MB RAM, 400MHz
 * CPU架构: ARM-Cortex-M7
 * 位宽: 32位
 * 时钟频率: 400000000 Hz
 *)

PROGRAM STM32H743
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (*  *)
    R1 AT %M0x04 : DWORD; (*  *)
    R2 AT %M0x08 : DWORD; (*  *)
    R3 AT %M0x0C : DWORD; (*  *)
    R4 AT %M0x10 : DWORD; (*  *)
    R5 AT %M0x14 : DWORD; (*  *)
    SP AT %M0x34 : DWORD; (*  *)
    LR AT %M0x38 : DWORD; (*  *)
    PC AT %M0x3C : DWORD; (*  *)

    (* 外设变量 *)
    (* Reset and Clock Control *)
    RCC_BASE AT %MW0x58024400 : WORD;
    RCC_CR AT %MW0x00 : DWORD;
    RCC_CFGR AT %MW0x04 : DWORD;
    RCC_PLL1CFGR AT %MW0x0C : DWORD;
    RCC_AHB1ENR AT %MW0x30 : DWORD;
    RCC_AHB1ENR_GPIOAEN AT %MW0x30.0 : BOOL; (* GPIOA clock enable *)
    RCC_AHB1ENR_GPIOBEN AT %MW0x30.1 : BOOL; (* GPIOB clock enable *)
    RCC_AHB1ENR_GPIOCEN AT %MW0x30.2 : BOOL; (* GPIOC clock enable *)
    RCC_AHB1ENR_GPIODEN AT %MW0x30.3 : BOOL; (* GPIOD clock enable *)
    RCC_AHB1ENR_GPIOEEN AT %MW0x30.4 : BOOL; (* GPIOE clock enable *)
    RCC_AHB1ENR_DMA1EN AT %MW0x30.21 : BOOL; (* DMA1 clock enable *)
    RCC_AHB1ENR_DMA2EN AT %MW0x30.22 : BOOL; (* DMA2 clock enable *)
    RCC_AHB2ENR AT %MW0x34 : DWORD;
    RCC_AHB4ENR AT %MW0x3C : DWORD;
    RCC_APB1LENR AT %MW0x50 : DWORD;
    RCC_APB2ENR AT %MW0x58 : DWORD;
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x58020000 : WORD;
    GPIOA_MODER AT %MW0x00 : DWORD;
    GPIOA_OTYPER AT %MW0x04 : DWORD;
    GPIOA_OSPEEDR AT %MW0x08 : DWORD;
    GPIOA_PUPDR AT %MW0x0C : DWORD;
    GPIOA_IDR AT %MW0x10 : DWORD;
    GPIOA_ODR AT %MW0x14 : DWORD;
    GPIOA_BSRR AT %MW0x18 : DWORD;
    GPIOA_BRR AT %MW0x28 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x58020400 : WORD;
    GPIOB_MODER AT %MW0x00 : DWORD;
    GPIOB_OTYPER AT %MW0x04 : DWORD;
    GPIOB_OSPEEDR AT %MW0x08 : DWORD;
    GPIOB_PUPDR AT %MW0x0C : DWORD;
    GPIOB_IDR AT %MW0x10 : DWORD;
    GPIOB_ODR AT %MW0x14 : DWORD;
    GPIOB_BSRR AT %MW0x18 : DWORD;
    GPIOB_BRR AT %MW0x28 : DWORD;
    (* General Purpose I/O Port C *)
    GPIOC_BASE AT %MW0x58020800 : WORD;
    GPIOC_MODER AT %MW0x00 : DWORD;
    GPIOC_OTYPER AT %MW0x04 : DWORD;
    GPIOC_IDR AT %MW0x10 : DWORD;
    GPIOC_ODR AT %MW0x14 : DWORD;
    GPIOC_BSRR AT %MW0x18 : DWORD;
    (* General Purpose I/O Port D *)
    GPIOD_BASE AT %MW0x58020C00 : WORD;
    GPIOD_MODER AT %MW0x00 : DWORD;
    GPIOD_OTYPER AT %MW0x04 : DWORD;
    GPIOD_IDR AT %MW0x10 : DWORD;
    GPIOD_ODR AT %MW0x14 : DWORD;
    GPIOD_BSRR AT %MW0x18 : DWORD;
    (* General Purpose I/O Port E *)
    GPIOE_BASE AT %MW0x58021000 : WORD;
    GPIOE_MODER AT %MW0x00 : DWORD;
    GPIOE_OTYPER AT %MW0x04 : DWORD;
    GPIOE_IDR AT %MW0x10 : DWORD;
    GPIOE_ODR AT %MW0x14 : DWORD;
    GPIOE_BSRR AT %MW0x18 : DWORD;
    (* USART1 *)
    USART1_BASE AT %MW0x40011000 : WORD;
    USART1_CR1 AT %MW0x00 : DWORD;
    USART1_BRR AT %MW0x0C : DWORD;
    USART1_RDR AT %MW0x24 : DWORD;
    USART1_TDR AT %MW0x28 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_SYSTICK : BOOL; (*  *)
    INT_USART1 : BOOL; (* USART1 Global Interrupt *)

END_VAR

BEGIN
    (* STM32H743初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_SVCALL|--[中断处理]--
    --|INT_SYSTICK|--[中断处理]--
    --|INT_USART1|--[中断处理]--

END_PROGRAM
