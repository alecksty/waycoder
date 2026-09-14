(*
 * STM32F401梯形图定义
 * 生成自: STMicroelectronics/STM32/STM32F401
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 64KB RAM, 84MHz
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 84000000 Hz
 *)

PROGRAM STM32F401
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
    RCC_BASE AT %MW0x40023800 : WORD;
    RCC_CR AT %MW0x00 : DWORD;
    RCC_PLLCFGR AT %MW0x04 : DWORD;
    RCC_CFGR AT %MW0x08 : DWORD;
    RCC_AHB1ENR AT %MW0x30 : DWORD;
    RCC_AHB1ENR_GPIOAEN AT %MW0x30.0 : BOOL; (* GPIOA clock enable *)
    RCC_AHB1ENR_GPIOBEN AT %MW0x30.1 : BOOL; (* GPIOB clock enable *)
    RCC_AHB1ENR_GPIOCEN AT %MW0x30.2 : BOOL; (* GPIOC clock enable *)
    RCC_APB1ENR AT %MW0x40 : DWORD;
    RCC_APB2ENR AT %MW0x44 : DWORD;
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x40020000 : WORD;
    GPIOA_MODER AT %MW0x00 : DWORD;
    GPIOA_OTYPER AT %MW0x04 : DWORD;
    GPIOA_OSPEEDR AT %MW0x08 : DWORD;
    GPIOA_PUPDR AT %MW0x0C : DWORD;
    GPIOA_IDR AT %MW0x10 : DWORD;
    GPIOA_ODR AT %MW0x14 : DWORD;
    GPIOA_BSRR AT %MW0x18 : DWORD;
    GPIOA_BRR AT %MW0x28 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x40020400 : WORD;
    GPIOB_MODER AT %MW0x00 : DWORD;
    GPIOB_OTYPER AT %MW0x04 : DWORD;
    GPIOB_OSPEEDR AT %MW0x08 : DWORD;
    GPIOB_PUPDR AT %MW0x0C : DWORD;
    GPIOB_IDR AT %MW0x10 : DWORD;
    GPIOB_ODR AT %MW0x14 : DWORD;
    GPIOB_BSRR AT %MW0x18 : DWORD;
    GPIOB_BRR AT %MW0x28 : DWORD;
    (* General Purpose I/O Port C *)
    GPIOC_BASE AT %MW0x40020800 : WORD;
    GPIOC_MODER AT %MW0x00 : DWORD;
    GPIOC_OTYPER AT %MW0x04 : DWORD;
    GPIOC_IDR AT %MW0x10 : DWORD;
    GPIOC_ODR AT %MW0x14 : DWORD;
    GPIOC_BSRR AT %MW0x18 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)

END_VAR

BEGIN
    (* STM32F401初始化梯形图 *)
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

END_PROGRAM
