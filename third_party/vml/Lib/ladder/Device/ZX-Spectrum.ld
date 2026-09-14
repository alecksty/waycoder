(*
 * ZX-Spectrum梯形图定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
 * CPU架构: Zilog Z80
 * 位宽: 8位
 * 时钟频率: 3500000 Hz
 *)

PROGRAM ZX_Spectrum
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0 : BYTE; (* Accumulator *)
    F AT %M0 : BYTE; (* Flags *)
    B AT %M0 : BYTE; (* B *)
    C AT %M0 : BYTE; (* C *)
    D AT %M0 : BYTE; (* D *)
    E AT %M0 : BYTE; (* E *)
    H AT %M0 : BYTE; (* H *)
    L AT %M0 : BYTE; (* L *)
    IX AT %M0 : WORD; (* Index Register X *)
    IY AT %M0 : WORD; (* Index Register Y *)
    SP AT %M0 : WORD; (* Stack Pointer *)
    PC AT %M0 : WORD; (* Program Counter *)
    I AT %M0 : BYTE; (* Interrupt Vector *)
    R AT %M0 : BYTE; (* Memory Refresh *)
    AF AT %M0 : WORD; (* Alternate AF *)
    BC AT %M0 : WORD; (* Alternate BC *)
    DE AT %M0 : WORD; (* Alternate DE *)
    HL AT %M0 : WORD; (* Alternate HL *)

    (* 外设变量 *)
    (* Uncommitted Logic Array (video and I/O) *)
    ULA_BASE AT %MW : WORD;
    ULA_ULA_PORT_FE AT %MW0xFE : BYTE;
    ULA_ULA_BORDER AT %MW0xFE : BYTE;
    ULA_ULA_BEEPER AT %MW0xFE : BYTE;
    ULA_ULA_MIC AT %MW0xFE : BYTE;
    (* General Instruments AY-3-8912 sound chip *)
    AY_3_8912_BASE AT %MW : WORD;
    AY_3_8912_AY_REG_SEL AT %MW0xFFFD : BYTE;
    AY_3_8912_AY_DATA AT %MW0xBFFD : BYTE;
    AY_3_8912_AY_READ AT %MW0xFFFD : BYTE;
    (* 40-key rubber keyboard *)
    KEYBOARD_BASE AT %MW : WORD;
    KEYBOARD_KEY_ROW0 AT %MW0xFEFE : BYTE;
    KEYBOARD_KEY_ROW1 AT %MW0xFDFE : BYTE;
    KEYBOARD_KEY_ROW2 AT %MW0xFBFE : BYTE;
    KEYBOARD_KEY_ROW3 AT %MW0xF7FE : BYTE;
    KEYBOARD_KEY_ROW4 AT %MW0xEFFE : BYTE;
    KEYBOARD_KEY_ROW5 AT %MW0xDFFE : BYTE;
    KEYBOARD_KEY_ROW6 AT %MW0xBFFE : BYTE;
    KEYBOARD_KEY_ROW7 AT %MW0x7FFE : BYTE;
    (* Kempston joystick interface *)
    KEMPSTON_BASE AT %MW : WORD;
    KEMPSTON_KEMPSTON_JOY AT %MW0x1F : BYTE;
    (* ZX Interface 1 (RS-232 and Microdrive) *)
    INTERFACE1_BASE AT %MW : WORD;
    INTERFACE1_IF1_STATUS AT %MW0x1FFD : BYTE;
    INTERFACE1_IF1_DATA AT %MW0x3FFD : BYTE;
    (* ZX Interface 2 (joystick and ROM cartridge) *)
    INTERFACE2_BASE AT %MW : WORD;
    INTERFACE2_IF2_JOY1 AT %MW0x1F : BYTE;
    INTERFACE2_IF2_JOY2 AT %MW0x37 : BYTE;

    (* 中断变量 *)
    INT_IM1 : BOOL; (* Interrupt Mode 1 *)
    INT_RST_00 : BOOL; (* Restart 00h *)
    INT_RST_08 : BOOL; (* Restart 08h *)
    INT_RST_10 : BOOL; (* Restart 10h *)
    INT_RST_18 : BOOL; (* Restart 18h *)
    INT_RST_20 : BOOL; (* Restart 20h *)
    INT_RST_28 : BOOL; (* Restart 28h *)
    INT_RST_30 : BOOL; (* Restart 30h *)
    INT_RST_38 : BOOL; (* Restart 38h *)

END_VAR

BEGIN
    (* ZX-Spectrum初始化梯形图 *)
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
    --|INT_IM1|--[中断处理]--
    --|INT_RST_00|--[中断处理]--
    --|INT_RST_08|--[中断处理]--
    --|INT_RST_10|--[中断处理]--
    --|INT_RST_18|--[中断处理]--
    --|INT_RST_20|--[中断处理]--
    --|INT_RST_28|--[中断处理]--
    --|INT_RST_30|--[中断处理]--
    --|INT_RST_38|--[中断处理]--

END_PROGRAM
