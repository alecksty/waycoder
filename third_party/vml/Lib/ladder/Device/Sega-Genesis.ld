(*
 * Sega-Genesis梯形图定义
 * 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
 * CPU架构: Motorola 68000
 * 位宽: 32位
 * 时钟频率: 7670000 Hz
 *)

PROGRAM Sega_Genesis
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    D0 AT %M0 : DWORD; (* Data Register 0 *)
    D1 AT %M0 : DWORD; (* Data Register 1 *)
    D2 AT %M0 : DWORD; (* Data Register 2 *)
    D3 AT %M0 : DWORD; (* Data Register 3 *)
    D4 AT %M0 : DWORD; (* Data Register 4 *)
    D5 AT %M0 : DWORD; (* Data Register 5 *)
    D6 AT %M0 : DWORD; (* Data Register 6 *)
    D7 AT %M0 : DWORD; (* Data Register 7 *)
    A0 AT %M0 : DWORD; (* Address Register 0 *)
    A1 AT %M0 : DWORD; (* Address Register 1 *)
    A2 AT %M0 : DWORD; (* Address Register 2 *)
    A3 AT %M0 : DWORD; (* Address Register 3 *)
    A4 AT %M0 : DWORD; (* Address Register 4 *)
    A5 AT %M0 : DWORD; (* Address Register 5 *)
    A6 AT %M0 : DWORD; (* Address Register 6 *)
    A7 AT %M0 : DWORD; (* Address Register 7 (SP) *)
    PC AT %M0 : DWORD; (* Program Counter *)
    SR AT %M0 : WORD; (* Status Register *)

    (* 外设变量 *)
    (* Video Display Processor (315-5313) *)
    VDP_BASE AT %MW : WORD;
    VDP_VDP_DATA AT %MW0xC00000 : WORD;
    VDP_VDP_CONTROL AT %MW0xC00004 : WORD;
    VDP_VDP_HVCOUNTER AT %MW0xC00008 : WORD;
    VDP_VDP_PSG AT %MW0xC00011 : BYTE;
    (* FM synthesis sound chip *)
    YM2612_BASE AT %MW : WORD;
    YM2612_YM2612_ADDR0 AT %MW0xA04000 : BYTE;
    YM2612_YM2612_DATA0 AT %MW0xA04001 : BYTE;
    YM2612_YM2612_ADDR1 AT %MW0xA04002 : BYTE;
    YM2612_YM2612_DATA1 AT %MW0xA04003 : BYTE;
    (* I/O ports *)
    IOPORTS_BASE AT %MW : WORD;
    IOPORTS_IO_DATA1 AT %MW0xA10002 : BYTE;
    IOPORTS_IO_DATA2 AT %MW0xA10004 : BYTE;
    IOPORTS_IO_DATA3 AT %MW0xA10006 : BYTE;
    IOPORTS_IO_CTRL1 AT %MW0xA10008 : BYTE;
    IOPORTS_IO_CTRL2 AT %MW0xA1000A : BYTE;
    IOPORTS_IO_CTRL3 AT %MW0xA1000C : BYTE;
    (* TradeMark Security System *)
    TMSS_BASE AT %MW : WORD;
    TMSS_TMSS AT %MW0xA14000 : BYTE;
    (* Z80 bus control *)
    Z80BUS_BASE AT %MW : WORD;
    Z80BUS_Z80_BUSREQ AT %MW0xA11100 : WORD;
    Z80BUS_Z80_RESET AT %MW0xA11200 : WORD;
    Z80BUS_Z80_YM2612 AT %MW0xA04000 : DWORD;

    (* 中断变量 *)
    INT_RESET_SP : BOOL; (* Reset (Initial SP) *)
    INT_RESET_PC : BOOL; (* Reset (Initial PC) *)
    INT_HBLANK : BOOL; (* Horizontal blank interrupt *)
    INT_VBLANK : BOOL; (* Vertical blank interrupt *)
    INT_EXTINT1 : BOOL; (* External interrupt 1 *)
    INT_EXTINT2 : BOOL; (* External interrupt 2 *)
    INT_EXTINT3 : BOOL; (* External interrupt 3 *)
    INT_EXTINT4 : BOOL; (* External interrupt 4 *)
    INT_EXTINT5 : BOOL; (* External interrupt 5 *)
    INT_EXTINT6 : BOOL; (* External interrupt 6 *)
    INT_EXTINT7 : BOOL; (* External interrupt 7 *)

END_VAR

BEGIN
    (* Sega-Genesis初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|D0>0|--(监控灯)--
    --|D1>0|--(监控灯)--
    --|D2>0|--(监控灯)--
    --|D3>0|--(监控灯)--
    --|D4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET_SP|--[中断处理]--
    --|INT_RESET_PC|--[中断处理]--
    --|INT_HBLANK|--[中断处理]--
    --|INT_VBLANK|--[中断处理]--
    --|INT_EXTINT1|--[中断处理]--
    --|INT_EXTINT2|--[中断处理]--
    --|INT_EXTINT3|--[中断处理]--
    --|INT_EXTINT4|--[中断处理]--
    --|INT_EXTINT5|--[中断处理]--
    --|INT_EXTINT6|--[中断处理]--
    --|INT_EXTINT7|--[中断处理]--

END_PROGRAM
