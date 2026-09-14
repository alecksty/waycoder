(*
 * STM32L432梯形图定义
 * 生成自: STMicroelectronics/STM32/STM32L432
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 MCU ultra-low-power with 256KB Flash, 64KB RAM, 80MHz
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 80000000 Hz
 *)

PROGRAM STM32L432
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
    RCC_BASE AT %MW0x40021000 : WORD;
    RCC_CR AT %MW0x00 : DWORD;
    RCC_CFGR AT %MW0x08 : DWORD;
    RCC_PLLCFGR AT %MW0x0C : DWORD;
    RCC_AHB1ENR AT %MW0x38 : DWORD;
    RCC_AHB1ENR_GPIOAEN AT %MW0x38.0 : BOOL; (* GPIOA clock enable *)
    RCC_AHB1ENR_GPIOBEN AT %MW0x38.1 : BOOL; (* GPIOB clock enable *)
    RCC_APB1ENR1 AT %MW0x58 : DWORD;
    RCC_APB2ENR AT %MW0x60 : DWORD;
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x48000000 : WORD;
    GPIOA_MODER AT %MW0x00 : DWORD;
    GPIOA_OTYPER AT %MW0x04 : DWORD;
    GPIOA_OSPEEDR AT %MW0x08 : DWORD;
    GPIOA_PUPDR AT %MW0x0C : DWORD;
    GPIOA_IDR AT %MW0x10 : DWORD;
    GPIOA_ODR AT %MW0x14 : DWORD;
    GPIOA_BSRR AT %MW0x18 : DWORD;
    GPIOA_BRR AT %MW0x28 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x48000400 : WORD;
    GPIOB_MODER AT %MW0x00 : DWORD;
    GPIOB_OTYPER AT %MW0x04 : DWORD;
    GPIOB_OSPEEDR AT %MW0x08 : DWORD;
    GPIOB_PUPDR AT %MW0x0C : DWORD;
    GPIOB_IDR AT %MW0x10 : DWORD;
    GPIOB_ODR AT %MW0x14 : DWORD;
    GPIOB_BSRR AT %MW0x18 : DWORD;
    GPIOB_BRR AT %MW0x28 : DWORD;
    (* Low-power UART 1 *)
    LPUART1_BASE AT %MW0x40008000 : WORD;
    LPUART1_CR1 AT %MW0x00 : DWORD;
    LPUART1_BRR AT %MW0x0C : DWORD;
    LPUART1_RDR AT %MW0x24 : DWORD;
    LPUART1_TDR AT %MW0x28 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_LPUART1 : BOOL; (* LPUART1 Global Interrupt *)

END_VAR

BEGIN
    (* STM32L432初始化梯形图 *)
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
    --|INT_LPUART1|--[中断处理]--

END_PROGRAM
