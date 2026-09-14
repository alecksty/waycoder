(*
 * ZX-Spectrum-48K梯形图定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
 * CPU架构: Z80A
 * 位宽: 8位
 * 时钟频率: 3500000 Hz
 *)

PROGRAM ZX_Spectrum_48K
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    F AT %M0x01 : BYTE; (* Flags Register *)
    F_C AT %M0x01.0 : BOOL; (* Carry *)
    F_N AT %M0x01.1 : BOOL; (* Add/Subtract *)
    F_PV AT %M0x01.2 : BOOL; (* Parity/Overflow *)
    F_H AT %M0x01.4 : BOOL; (* Half Carry *)
    F_Z AT %M0x01.6 : BOOL; (* Zero *)
    F_S AT %M0x01.7 : BOOL; (* Sign *)
    B AT %M0x02 : BYTE; (* B Register *)
    C AT %M0x03 : BYTE; (* C Register *)
    D AT %M0x04 : BYTE; (* D Register *)
    E AT %M0x05 : BYTE; (* E Register *)
    H AT %M0x06 : BYTE; (* H Register *)
    L AT %M0x07 : BYTE; (* L Register *)
    AF AT %M0x08 : WORD; (* Alternate AF *)
    BC AT %M0x0A : WORD; (* Alternate BC *)
    DE AT %M0x0C : WORD; (* Alternate DE *)
    HL AT %M0x0E : WORD; (* Alternate HL *)
    I AT %M0x10 : BYTE; (* Interrupt Vector Register *)
    R AT %M0x11 : BYTE; (* Refresh Counter *)
    IX AT %M0x12 : WORD; (* Index X *)
    IY AT %M0x14 : WORD; (* Index Y *)
    SP AT %M0x16 : WORD; (* Stack Pointer *)
    PC AT %M0x18 : WORD; (* Program Counter *)

    (* 外设变量 *)
    (* Uncommitted Logic Array - Sinclair custom IC *)
    ULA_BASE AT %MW0xFE : WORD;
    ULA_BORDER AT %MW0xFE : BYTE;
    ULA_KBD_ROW0 AT %MW0xFE : BYTE;
    ULA_KBD_ROW1 AT %MW0xFE : BYTE;
    ULA_KBD_ROW2 AT %MW0xFE : BYTE;
    ULA_KBD_ROW3 AT %MW0xFE : BYTE;
    ULA_KBD_ROW4 AT %MW0xFE : BYTE;
    ULA_KBD_ROW5 AT %MW0xFE : BYTE;
    ULA_KBD_ROW6 AT %MW0xFE : BYTE;
    ULA_KBD_ROW7 AT %MW0xFE : BYTE;
    ULA_KBD_ROW8 AT %MW0xFE : BYTE;
    (* Keyboard Matrix (40 keys, 8 rows x 5 cols) *)
    KEYBOARD_BASE AT %MW0xFE : WORD;
    KEYBOARD_KBD_IN AT %MW0xFE : BYTE;
    (* Internal Beeper *)
    BEEPER_BASE AT %MW0xFE : WORD;
    BEEPER_BEEP AT %MW0xFE : BYTE;
    (* Tape Interface *)
    TAPE_BASE AT %MW0xFE : WORD;
    TAPE_EAR_IN AT %MW0xFE : BYTE;
    TAPE_MIC_OUT AT %MW0xFE : BYTE;
    (* Kempston Joystick Interface *)
    JOYSTICK_BASE AT %MW0xF7FE : WORD;
    JOYSTICK_KEMPSTON AT %MW0xF7FE : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Power-on / Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt (BREAK key) *)
    INT_INT : BOOL; (* Maskable Interrupt (ULA vertical blank, 50Hz) *)

END_VAR

BEGIN
    (* ZX-Spectrum-48K初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|A>0|--(监控灯)--
    --|F>0|--(监控灯)--
    --|B>0|--(监控灯)--
    --|C>0|--(监控灯)--
    --|D>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_INT|--[中断处理]--

END_PROGRAM
