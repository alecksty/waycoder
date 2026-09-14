(*
 * PIC32MX170F256B梯形图定义
 * 生成自: Microchip/PIC32/PIC32MX170F256B
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
 * CPU架构: MIPS32-M4K
 * 位宽: 32位
 * 时钟频率: 50000000 Hz
 *)

PROGRAM PIC32MX170F256B
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    _0 AT %M0x00 : DWORD; (* Hard-wired zero *)
    _1 AT %M0x04 : DWORD; (* AT *)
    _2 AT %M0x08 : DWORD; (* V0 *)
    _3 AT %M0x0C : DWORD; (* V1 *)
    _4 AT %M0x10 : DWORD; (* A0 *)
    _5 AT %M0x14 : DWORD; (* A1 *)
    _29 AT %M0x74 : DWORD; (* Stack Pointer (SP) *)
    _31 AT %M0x7C : DWORD; (* Return Address (RA) *)
    PC AT %M0x80 : DWORD; (* Program Counter *)

    (* 外设变量 *)
    (* General Purpose I/O Port A *)
    PORTA_BASE AT %MW0xBF886000 : WORD;
    PORTA_TRISA AT %MW0x00 : DWORD;
    PORTA_PORTA AT %MW0x10 : DWORD;
    PORTA_LATA AT %MW0x20 : DWORD;
    PORTA_ODCA AT %MW0x30 : DWORD;
    (* General Purpose I/O Port B *)
    PORTB_BASE AT %MW0xBF886100 : WORD;
    PORTB_TRISB AT %MW0x00 : DWORD;
    PORTB_PORTB AT %MW0x10 : DWORD;
    PORTB_LATB AT %MW0x20 : DWORD;
    PORTB_ODCB AT %MW0x30 : DWORD;
    (* UART1 *)
    UART1_BASE AT %MW0xBF822000 : WORD;
    UART1_UXMODE AT %MW0x00 : DWORD;
    UART1_UXSTA AT %MW0x04 : DWORD;
    UART1_UXTXREG AT %MW0x08 : DWORD;
    UART1_UXRXREG AT %MW0x0C : DWORD;
    UART1_UXBRG AT %MW0x10 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_UART1 : BOOL; (* UART1 Interrupt *)

END_VAR

BEGIN
    (* PIC32MX170F256B初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|_0>0|--(监控灯)--
    --|_1>0|--(监控灯)--
    --|_2>0|--(监控灯)--
    --|_3>0|--(监控灯)--
    --|_4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_UART1|--[中断处理]--

END_PROGRAM
