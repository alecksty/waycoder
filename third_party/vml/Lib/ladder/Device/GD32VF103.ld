(*
 * GD32VF103梯形图定义
 * 生成自: GigaDevice/GD32/GD32VF103
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
 * CPU架构: RISC-V
 * 位宽: 32位
 * 时钟频率: 108000000 Hz
 *)

PROGRAM GD32VF103
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
    RCU_BASE AT %MW0x40021000 : WORD;
    RCU_CTL AT %MW0x00 : DWORD;
    RCU_CFG0 AT %MW0x04 : DWORD;
    RCU_CFG1 AT %MW0x08 : DWORD;
    RCU_APB2EN AT %MW0x18 : DWORD;
    RCU_APB2EN_PAEN AT %MW0x18.2 : BOOL; (* GPIOA enable *)
    RCU_APB2EN_PBEN AT %MW0x18.3 : BOOL; (* GPIOB enable *)
    RCU_APB2EN_PCEN AT %MW0x18.4 : BOOL; (* GPIOC enable *)
    RCU_APB2EN_USART0EN AT %MW0x18.14 : BOOL; (* USART0 enable *)
    RCU_APB1EN AT %MW0x1C : DWORD;
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

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_MACHINESOFTWARE : BOOL; (*  *)
    INT_MACHINETIMER : BOOL; (*  *)
    INT_MACHINEEXTERNAL : BOOL; (*  *)
    INT_USART0 : BOOL; (* USART0 Global Interrupt *)

END_VAR

BEGIN
    (* GD32VF103初始化梯形图 *)
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
    --|INT_USART0|--[中断处理]--

END_PROGRAM
