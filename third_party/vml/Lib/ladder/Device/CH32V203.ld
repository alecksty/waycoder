(*
 * CH32V203梯形图定义
 * 生成自: WCH/CH32V2/CH32V203
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V MCU with 64KB Flash, 20KB RAM, 144MHz
 * CPU架构: RISC-V
 * 位宽: 32位
 * 时钟频率: 144000000 Hz
 *)

PROGRAM CH32V203
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    X1 AT %M0x04 : DWORD; (* Return Address *)
    X2 AT %M0x08 : DWORD; (* Stack Pointer (SP) *)
    X3 AT %M0x0C : DWORD; (* Global Pointer (GP) *)
    X8 AT %M0x20 : DWORD; (* Frame Pointer (FP) *)
    X10 AT %M0x28 : DWORD; (* Function Argument (A0) *)
    X11 AT %M0x2C : DWORD; (* Function Argument (A1) *)
    PC AT %M0x3C : DWORD; (* Program Counter *)

    (* 外设变量 *)
    (* Reset and Clock Control *)
    RCC_BASE AT %MW0x40021000 : WORD;
    RCC_RCC_CTLR AT %MW0x00 : DWORD;
    RCC_RCC_CFGR0 AT %MW0x04 : DWORD;
    RCC_RCC_APB2PCENR AT %MW0x18 : DWORD;
    RCC_RCC_APB2PCENR_IOPAEN AT %MW0x18.2 : BOOL; (* GPIOA clock enable *)
    RCC_RCC_APB2PCENR_IOPBEN AT %MW0x18.3 : BOOL; (* GPIOB clock enable *)
    RCC_RCC_APB2PCENR_IOPCEN AT %MW0x18.4 : BOOL; (* GPIOC clock enable *)
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x40010800 : WORD;
    GPIOA_CFGLR AT %MW0x00 : DWORD;
    GPIOA_CFGHR AT %MW0x04 : DWORD;
    GPIOA_INDR AT %MW0x08 : DWORD;
    GPIOA_OUTDR AT %MW0x0C : DWORD;
    GPIOA_BSHR AT %MW0x10 : DWORD;
    GPIOA_BCR AT %MW0x14 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x40010C00 : WORD;
    GPIOB_CFGLR AT %MW0x00 : DWORD;
    GPIOB_CFGHR AT %MW0x04 : DWORD;
    GPIOB_INDR AT %MW0x08 : DWORD;
    GPIOB_OUTDR AT %MW0x0C : DWORD;
    GPIOB_BSHR AT %MW0x10 : DWORD;
    GPIOB_BCR AT %MW0x14 : DWORD;
    (* General Purpose I/O Port C *)
    GPIOC_BASE AT %MW0x40011000 : WORD;
    GPIOC_CFGLR AT %MW0x00 : DWORD;
    GPIOC_CFGHR AT %MW0x04 : DWORD;
    GPIOC_INDR AT %MW0x08 : DWORD;
    GPIOC_OUTDR AT %MW0x0C : DWORD;
    GPIOC_BSHR AT %MW0x10 : DWORD;
    GPIOC_BCR AT %MW0x14 : DWORD;
    (* USART1 *)
    USART1_BASE AT %MW0x40013800 : WORD;
    USART1_USART_STATR AT %MW0x00 : DWORD;
    USART1_USART_DATAR AT %MW0x04 : DWORD;
    USART1_USART_BRR AT %MW0x08 : DWORD;
    USART1_USART_CTLR1 AT %MW0x0C : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_MACHINESOFTWARE : BOOL; (*  *)
    INT_MACHINETIMER : BOOL; (*  *)
    INT_MACHINEEXTERNAL : BOOL; (*  *)
    INT_USART1 : BOOL; (* USART1 Global Interrupt *)

END_VAR

BEGIN
    (* CH32V203初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|X1>0|--(监控灯)--
    --|X2>0|--(监控灯)--
    --|X3>0|--(监控灯)--
    --|X8>0|--(监控灯)--
    --|X10>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_MACHINESOFTWARE|--[中断处理]--
    --|INT_MACHINETIMER|--[中断处理]--
    --|INT_MACHINEEXTERNAL|--[中断处理]--
    --|INT_USART1|--[中断处理]--

END_PROGRAM
