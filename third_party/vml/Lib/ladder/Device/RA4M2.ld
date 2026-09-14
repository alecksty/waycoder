(*
 * RA4M2梯形图定义
 * 生成自: Renesas/RA/RA4M2
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 100000000 Hz
 *)

PROGRAM RA4M2
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
    (* Module Stop Control *)
    MSTP_BASE AT %MW0x40020000 : WORD;
    MSTP_MSTPCR_A AT %MW0x20 : DWORD;
    MSTP_MSTPCR_A_MSTP41 AT %MW0x20.9 : BOOL; (* GPIO A stop *)
    MSTP_MSTPCR_A_MSTP42 AT %MW0x20.10 : BOOL; (* GPIO B stop *)
    MSTP_MSTPCR_B AT %MW0x24 : DWORD;
    MSTP_MSTPCR_C AT %MW0x28 : DWORD;
    MSTP_MSTPCR_D AT %MW0x2C : DWORD;
    (* Interrupt Controller Unit *)
    ICU_BASE AT %MW0x40030000 : WORD;
    ICU_IRQCR0 AT %MW0x600 : WORD;
    ICU_IRQCR1 AT %MW0x602 : WORD;
    (* General Purpose I/O Port A *)
    GPIOA_BASE AT %MW0x40040000 : WORD;
    GPIOA_PDR AT %MW0x00 : WORD;
    GPIOA_PODR AT %MW0x04 : WORD;
    GPIOA_PIDR AT %MW0x08 : WORD;
    GPIOA_PMR AT %MW0x10 : WORD;
    GPIOA_PCR AT %MW0x18 : DWORD;
    (* General Purpose I/O Port B *)
    GPIOB_BASE AT %MW0x40040020 : WORD;
    GPIOB_PDR AT %MW0x00 : WORD;
    GPIOB_PODR AT %MW0x04 : WORD;
    GPIOB_PIDR AT %MW0x08 : WORD;
    GPIOB_PMR AT %MW0x10 : WORD;
    (* SCI UART 0 *)
    SCIUART0_BASE AT %MW0x40070000 : WORD;
    SCIUART0_SCR AT %MW0x00 : BYTE;
    SCIUART0_BRR AT %MW0x04 : BYTE;
    SCIUART0_TDR AT %MW0x08 : BYTE;
    SCIUART0_RDR AT %MW0x0C : BYTE;
    SCIUART0_SSR AT %MW0x10 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_SCIUART0_RXI : BOOL; (* SCI UART0 Receive Interrupt *)
    INT_SCIUART0_TXI : BOOL; (* SCI UART0 Transmit Interrupt *)

END_VAR

BEGIN
    (* RA4M2初始化梯形图 *)
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
    --|INT_SCIUART0_RXI|--[中断处理]--
    --|INT_SCIUART0_TXI|--[中断处理]--

END_PROGRAM
