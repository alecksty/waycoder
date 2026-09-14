(*
 * Macintosh-128K梯形图定义
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
 * CPU架构: Motorola 68000
 * 位宽: 32位
 * 时钟频率: 7998000 Hz
 *)

PROGRAM Macintosh_128K
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
    (* Versatile Interface Adapter (6522) *)
    VIA_BASE AT %MW : WORD;
    VIA_VIA_ORB AT %MW0xE80000 : BYTE;
    VIA_VIA_ORA AT %MW0xE80001 : BYTE;
    VIA_VIA_DDRB AT %MW0xE80002 : BYTE;
    VIA_VIA_DDRA AT %MW0xE80003 : BYTE;
    VIA_VIA_T1CL AT %MW0xE80004 : BYTE;
    VIA_VIA_T1CH AT %MW0xE80005 : BYTE;
    VIA_VIA_T1LL AT %MW0xE80006 : BYTE;
    VIA_VIA_T1LH AT %MW0xE80007 : BYTE;
    VIA_VIA_T2CL AT %MW0xE80008 : BYTE;
    VIA_VIA_T2CH AT %MW0xE80009 : BYTE;
    VIA_VIA_SR AT %MW0xE8000A : BYTE;
    VIA_VIA_ACR AT %MW0xE8000B : BYTE;
    VIA_VIA_PCR AT %MW0xE8000C : BYTE;
    VIA_VIA_IFR AT %MW0xE8000D : BYTE;
    VIA_VIA_IER AT %MW0xE8000E : BYTE;
    VIA_VIA_ORA2 AT %MW0xE8000F : BYTE;
    (* Integrated Woz Machine (floppy controller) *)
    IWM_BASE AT %MW : WORD;
    IWM_IWM_Q6 AT %MW0xD00000 : BYTE;
    IWM_IWM_Q7 AT %MW0xD00002 : BYTE;
    IWM_IWM_PH0 AT %MW0xD00004 : BYTE;
    IWM_IWM_PH1 AT %MW0xD00006 : BYTE;
    IWM_IWM_PH2 AT %MW0xD00008 : BYTE;
    IWM_IWM_PH3 AT %MW0xD0000A : BYTE;
    (* Zilog 8530 Serial Communications Controller *)
    SCC_BASE AT %MW : WORD;
    SCC_SCC_CA AT %MW0x500000 : BYTE;
    SCC_SCC_DA AT %MW0x500002 : BYTE;
    SCC_SCC_CB AT %MW0x500004 : BYTE;
    SCC_SCC_DB AT %MW0x500006 : BYTE;
    (* Built-in speaker *)
    SOUND_BASE AT %MW : WORD;
    SOUND_SOUND_VOL AT %MW0xE80100 : BYTE;
    SOUND_SOUND_FREQ AT %MW0xE80102 : BYTE;

    (* 中断变量 *)
    INT_RESET_SP : BOOL; (* Reset (Initial SP) *)
    INT_RESET_PC : BOOL; (* Reset (Initial PC) *)
    INT_AUTOVECTOR1 : BOOL; (* Auto vector 1 *)
    INT_AUTOVECTOR2 : BOOL; (* Auto vector 2 *)
    INT_AUTOVECTOR3 : BOOL; (* Auto vector 3 *)
    INT_AUTOVECTOR4 : BOOL; (* Auto vector 4 *)
    INT_AUTOVECTOR5 : BOOL; (* Auto vector 5 *)
    INT_AUTOVECTOR6 : BOOL; (* Auto vector 6 *)
    INT_AUTOVECTOR7 : BOOL; (* Auto vector 7 *)
    INT_SPURIOUS : BOOL; (* Spurious interrupt *)

END_VAR

BEGIN
    (* Macintosh-128K初始化梯形图 *)
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
    --|INT_AUTOVECTOR1|--[中断处理]--
    --|INT_AUTOVECTOR2|--[中断处理]--
    --|INT_AUTOVECTOR3|--[中断处理]--
    --|INT_AUTOVECTOR4|--[中断处理]--
    --|INT_AUTOVECTOR5|--[中断处理]--
    --|INT_AUTOVECTOR6|--[中断处理]--
    --|INT_AUTOVECTOR7|--[中断处理]--
    --|INT_SPURIOUS|--[中断处理]--

END_PROGRAM
