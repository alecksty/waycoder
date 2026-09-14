(*
 * 8051梯形图定义
 * 生成自: Intel/MCS-51/8051
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
 * CPU架构: MCS-51
 * 位宽: 8位
 * 时钟频率: 11059200 Hz
 *)

PROGRAM 8051
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    ACC AT %M0xE0 : BYTE; (* Accumulator *)
    B AT %M0xF0 : BYTE; (* B Register *)
    PSW AT %M0xD0 : BYTE; (* Program Status Word *)
    PSW_P AT %M0xD0.0 : BOOL; (* Parity Flag *)
    PSW_OV AT %M0xD0.2 : BOOL; (* Overflow Flag *)
    PSW_RS0 AT %M0xD0.3 : BOOL; (* Register Bank Select 0 *)
    PSW_RS1 AT %M0xD0.4 : BOOL; (* Register Bank Select 1 *)
    PSW_F0 AT %M0xD0.5 : BOOL; (* Flag 0 *)
    PSW_AC AT %M0xD0.6 : BOOL; (* Auxiliary Carry Flag *)
    PSW_CY AT %M0xD0.7 : BOOL; (* Carry Flag *)
    SP AT %M0x81 : BYTE; (* Stack Pointer *)
    DPTR AT %M0x82 : WORD; (* Data Pointer (DPL/DPH) *)

    (* 外设变量 *)
    (* Port 0 *)
    PORT0_BASE AT %MW0x80 : WORD;
    PORT0_P0 AT %MW0x80 : BYTE;
    (* Port 1 *)
    PORT1_BASE AT %MW0x90 : WORD;
    PORT1_P1 AT %MW0x90 : BYTE;
    (* Port 2 *)
    PORT2_BASE AT %MW0xA0 : WORD;
    PORT2_P2 AT %MW0xA0 : BYTE;
    (* Port 3 *)
    PORT3_BASE AT %MW0xB0 : WORD;
    PORT3_P3 AT %MW0xB0 : BYTE;
    (* Timer/Counter 0 *)
    TIMER0_BASE AT %MW0x8A : WORD;
    TIMER0_TH0 AT %MW0x8C : BYTE;
    TIMER0_TL0 AT %MW0x8A : BYTE;
    TIMER0_TMOD AT %MW0x89 : BYTE;
    TIMER0_TMOD_M0_0 AT %MW0x89.0 : BOOL; (* Timer 0 Mode bit 0 *)
    TIMER0_TMOD_M1_0 AT %MW0x89.1 : BOOL; (* Timer 0 Mode bit 1 *)
    TIMER0_TMOD_C_T0 AT %MW0x89.2 : BOOL; (* Timer 0 Counter/Timer Select *)
    TIMER0_TMOD_GATE0 AT %MW0x89.3 : BOOL; (* Timer 0 Gate Control *)
    TIMER0_TCON AT %MW0x88 : BYTE;
    TIMER0_TCON_TR0 AT %MW0x88.4 : BOOL; (* Timer 0 Run Control *)
    TIMER0_TCON_TF0 AT %MW0x88.5 : BOOL; (* Timer 0 Overflow Flag *)
    (* Serial Port *)
    UART_BASE AT %MW0x98 : WORD;
    UART_SBUF AT %MW0x99 : BYTE;
    UART_SCON AT %MW0x98 : BYTE;
    UART_SCON_RI AT %MW0x98.0 : BOOL; (* Receive Interrupt Flag *)
    UART_SCON_TI AT %MW0x98.1 : BOOL; (* Transmit Interrupt Flag *)
    UART_SCON_REN AT %MW0x98.4 : BOOL; (* Receive Enable *)
    UART_SCON_SM0 AT %MW0x98.6 : BOOL; (* Serial Mode bit 0 *)
    UART_SCON_SM1 AT %MW0x98.7 : BOOL; (* Serial Mode bit 1 *)

    (* 中断变量 *)
    INT_RESET : BOOL; (* Reset Vector *)
    INT_INT0 : BOOL; (* External Interrupt 0 *)
    INT_TIMER0 : BOOL; (* Timer 0 Interrupt *)
    INT_INT1 : BOOL; (* External Interrupt 1 *)
    INT_TIMER1 : BOOL; (* Timer 1 Interrupt *)
    INT_UART : BOOL; (* Serial Port Interrupt *)

END_VAR

BEGIN
    (* 8051初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|ACC>0|--(监控灯)--
    --|B>0|--(监控灯)--
    --|PSW>0|--(监控灯)--
    --|SP>0|--(监控灯)--
    --|DPTR>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_INT0|--[中断处理]--
    --|INT_TIMER0|--[中断处理]--
    --|INT_INT1|--[中断处理]--
    --|INT_TIMER1|--[中断处理]--
    --|INT_UART|--[中断处理]--

END_PROGRAM
