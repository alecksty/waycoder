(*
 * CH32V003梯形图定义
 * 生成自: WCH/CH32V0/CH32V003
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
 * CPU架构: RISC-V
 * 位宽: 32位
 * 时钟频率: 48000000 Hz
 *)

PROGRAM CH32V003
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    X1 AT %M0x04 : DWORD; (* Return Address *)
    X2 AT %M0x08 : DWORD; (* Stack Pointer (SP) *)
    X3 AT %M0x0C : DWORD; (* Global Pointer (GP) *)
    PC AT %M0x3C : DWORD; (* Program Counter *)

    (* 外设变量 *)
    (* Reset and Clock Control *)
    RCC_BASE AT %MW0x40021000 : WORD;
    RCC_CTLR AT %MW0x00 : DWORD;
    RCC_CFGR0 AT %MW0x04 : DWORD;
    RCC_APB2PCENR AT %MW0x18 : DWORD;
    RCC_APB2PCENR_IOPAEN AT %MW0x18.2 : BOOL; (* GPIOA clock enable *)
    RCC_APB2PCENR_IOPCEN AT %MW0x18.4 : BOOL; (* GPIOC clock enable *)
    RCC_APB2PCENR_IOPDEN AT %MW0x18.5 : BOOL; (* GPIOD clock enable *)
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x40010800 : WORD;
    GPIOA_CFGLR AT %MW0x00 : DWORD;
    GPIOA_CFGHR AT %MW0x04 : DWORD;
    GPIOA_INDR AT %MW0x08 : DWORD;
    GPIOA_OUTDR AT %MW0x0C : DWORD;
    GPIOA_BSHR AT %MW0x10 : DWORD;
    GPIOA_BCR AT %MW0x14 : DWORD;
    (* General Purpose I/O Port C *)
    GPIOC_BASE AT %MW0x40011000 : WORD;
    GPIOC_CFGLR AT %MW0x00 : DWORD;
    GPIOC_CFGHR AT %MW0x04 : DWORD;
    GPIOC_INDR AT %MW0x08 : DWORD;
    GPIOC_OUTDR AT %MW0x0C : DWORD;
    GPIOC_BSHR AT %MW0x10 : DWORD;
    GPIOC_BCR AT %MW0x14 : DWORD;
    (* General Purpose I/O Port D *)
    GPIOD_BASE AT %MW0x40011400 : WORD;
    GPIOD_CFGLR AT %MW0x00 : DWORD;
    GPIOD_CFGHR AT %MW0x04 : DWORD;
    GPIOD_INDR AT %MW0x08 : DWORD;
    GPIOD_OUTDR AT %MW0x0C : DWORD;
    GPIOD_BSHR AT %MW0x10 : DWORD;
    GPIOD_BCR AT %MW0x14 : DWORD;
    (* USART1 *)
    USART1_BASE AT %MW0x40013800 : WORD;
    USART1_STATR AT %MW0x00 : DWORD;
    USART1_DATAR AT %MW0x04 : DWORD;
    USART1_BRR AT %MW0x08 : DWORD;
    USART1_CTLR1 AT %MW0x0C : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_MACHINESOFTWARE : BOOL; (*  *)
    INT_MACHINETIMER : BOOL; (*  *)
    INT_MACHINEEXTERNAL : BOOL; (*  *)
    INT_USART1 : BOOL; (* USART1 Global Interrupt *)

END_VAR

BEGIN
    (* CH32V003初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|X1>0|--(监控灯)--
    --|X2>0|--(监控灯)--
    --|X3>0|--(监控灯)--
    --|PC>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_MACHINESOFTWARE|--[中断处理]--
    --|INT_MACHINETIMER|--[中断处理]--
    --|INT_MACHINEEXTERNAL|--[中断处理]--
    --|INT_USART1|--[中断处理]--

END_PROGRAM
