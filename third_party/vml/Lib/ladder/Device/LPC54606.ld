(*
 * LPC54606梯形图定义
 * 生成自: NXP/LPC/LPC54606
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 180000000 Hz
 *)

PROGRAM LPC54606
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
    (* System Control *)
    SYSCON_BASE AT %MW0x40000000 : WORD;
    SYSCON_SYSAHBCLKCTRL AT %MW0x80 : DWORD;
    SYSCON_MAINCLKSEL AT %MW0x04 : DWORD;
    SYSCON_MAINCLKUEN AT %MW0x08 : DWORD;
    SYSCON_SYSPLLCTRL AT %MW0x0C : DWORD;
    (* General Purpose I/O *)
    GPIO_BASE AT %MW0x400F4000 : WORD;
    GPIO_DIR0 AT %MW0x0000 : DWORD;
    GPIO_PIN0 AT %MW0x1000 : DWORD;
    GPIO_SET0 AT %MW0x2000 : DWORD;
    GPIO_CLR0 AT %MW0x3000 : DWORD;
    GPIO_NOT0 AT %MW0x4000 : DWORD;
    GPIO_DIR1 AT %MW0x0004 : DWORD;
    GPIO_PIN1 AT %MW0x1004 : DWORD;
    GPIO_SET1 AT %MW0x2004 : DWORD;
    GPIO_CLR1 AT %MW0x3004 : DWORD;
    GPIO_NOT1 AT %MW0x4004 : DWORD;
    (* USART0 *)
    USART0_BASE AT %MW0x40086000 : WORD;
    USART0_CFG AT %MW0x00 : DWORD;
    USART0_CTRL AT %MW0x04 : DWORD;
    USART0_STAT AT %MW0x08 : DWORD;
    USART0_TXDAT AT %MW0x10 : DWORD;
    USART0_RXDAT AT %MW0x14 : DWORD;
    USART0_BRG AT %MW0x20 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_USART0 : BOOL; (* USART0 Interrupt *)

END_VAR

BEGIN
    (* LPC54606初始化梯形图 *)
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

END_PROGRAM
