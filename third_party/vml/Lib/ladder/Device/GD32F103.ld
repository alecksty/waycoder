(*
 * GD32F103梯形图定义
 * 生成自: GigaDevice/GD32/GD32F103
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
 * CPU架构: ARM-Cortex-M3
 * 位宽: 32位
 * 时钟频率: 108000000 Hz
 *)

PROGRAM GD32F103
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
    RCC_CTLR AT %MW0x00 : DWORD;
    RCC_CFGR0 AT %MW0x04 : DWORD;
    RCC_APB2PCENR AT %MW0x18 : DWORD;
    RCC_APB2PCENR_IOPAEN AT %MW0x18.2 : BOOL; (* GPIOA clock enable *)
    RCC_APB2PCENR_IOPBEN AT %MW0x18.3 : BOOL; (* GPIOB clock enable *)
    RCC_APB2PCENR_IOPCEN AT %MW0x18.4 : BOOL; (* GPIOC clock enable *)
    RCC_APB2PCENR_USART0EN AT %MW0x18.14 : BOOL; (* USART0 clock enable *)
    RCC_APB1PCENR AT %MW0x1C : DWORD;
    RCC_APB1PCENR_USART1EN AT %MW0x1C.17 : BOOL; (* USART1 clock enable *)
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x40010800 : WORD;
    GPIOA_CTL0 AT %MW0x00 : DWORD;
    GPIOA_CTL1 AT %MW0x04 : DWORD;
    GPIOA_ISTAT AT %MW0x08 : DWORD;
    GPIOA_OCTL AT %MW0x0C : DWORD;
    GPIOA_BOP AT %MW0x10 : DWORD;
    GPIOA_BC AT %MW0x14 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x40010C00 : WORD;
    GPIOB_CTL0 AT %MW0x00 : DWORD;
    GPIOB_CTL1 AT %MW0x04 : DWORD;
    GPIOB_ISTAT AT %MW0x08 : DWORD;
    GPIOB_OCTL AT %MW0x0C : DWORD;
    GPIOB_BOP AT %MW0x10 : DWORD;
    GPIOB_BC AT %MW0x14 : DWORD;
    (* General Purpose I/O Port C *)
    GPIOC_BASE AT %MW0x40011000 : WORD;
    GPIOC_CTL0 AT %MW0x00 : DWORD;
    GPIOC_CTL1 AT %MW0x04 : DWORD;
    GPIOC_ISTAT AT %MW0x08 : DWORD;
    GPIOC_OCTL AT %MW0x0C : DWORD;
    GPIOC_BOP AT %MW0x10 : DWORD;
    GPIOC_BC AT %MW0x14 : DWORD;
    (* USART0 *)
    USART0_BASE AT %MW0x40013800 : WORD;
    USART0_STATR AT %MW0x00 : DWORD;
    USART0_DATAR AT %MW0x04 : DWORD;
    USART0_BRR AT %MW0x08 : DWORD;
    USART0_CTLR1 AT %MW0x0C : DWORD;
    (* USART1 *)
    USART1_BASE AT %MW0x40004400 : WORD;
    USART1_STATR AT %MW0x00 : DWORD;
    USART1_DATAR AT %MW0x04 : DWORD;
    USART1_BRR AT %MW0x08 : DWORD;
    USART1_CTLR1 AT %MW0x0C : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_USART0 : BOOL; (* USART0 Global Interrupt *)
    INT_USART1 : BOOL; (* USART1 Global Interrupt *)

END_VAR

BEGIN
    (* GD32F103初始化梯形图 *)
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
    --|INT_USART0|--[中断处理]--
    --|INT_USART1|--[中断处理]--

END_PROGRAM
