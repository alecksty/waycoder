(*
 * Macintosh-128K梯形图定义
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
 * CPU架构: MC68000
 * 位宽: 32位
 * 时钟频率: 7833600 Hz
 *)

PROGRAM Macintosh_128K
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    D0 AT %M0x00 : DWORD; (* Data Register 0 *)
    D1 AT %M0x04 : DWORD; (* Data Register 1 *)
    D2 AT %M0x08 : DWORD; (* Data Register 2 *)
    D3 AT %M0x0C : DWORD; (* Data Register 3 *)
    D4 AT %M0x10 : DWORD; (* Data Register 4 *)
    D5 AT %M0x14 : DWORD; (* Data Register 5 *)
    D6 AT %M0x18 : DWORD; (* Data Register 6 *)
    D7 AT %M0x1C : DWORD; (* Data Register 7 *)
    A0 AT %M0x20 : DWORD; (* Address Register 0 *)
    A1 AT %M0x24 : DWORD; (* Address Register 1 *)
    A2 AT %M0x28 : DWORD; (* Address Register 2 *)
    A3 AT %M0x2C : DWORD; (* Address Register 3 *)
    A4 AT %M0x30 : DWORD; (* Address Register 4 *)
    A5 AT %M0x34 : DWORD; (* Address Register 5 *)
    A6 AT %M0x38 : DWORD; (* Address Register 6 *)
    A7 AT %M0x3C : DWORD; (* Stack Pointer (USP) *)
    PC AT %M0x40 : DWORD; (* Program Counter *)
    SR AT %M0x44 : WORD; (* Status Register *)
    SR_C AT %M0x44.0 : BOOL; (* Carry *)
    SR_V AT %M0x44.1 : BOOL; (* Overflow *)
    SR_Z AT %M0x44.2 : BOOL; (* Zero *)
    SR_N AT %M0x44.3 : BOOL; (* Negative *)
    SR_X AT %M0x44.4 : BOOL; (* Extend *)
    SR_I0 AT %M0x44.8 : BOOL; (* Interrupt Mask 0 *)
    SR_I1 AT %M0x44.9 : BOOL; (* Interrupt Mask 1 *)
    SR_I2 AT %M0x44.10 : BOOL; (* Interrupt Mask 2 *)
    SR_S AT %M0x44.13 : BOOL; (* Supervisor/User *)
    SR_T0 AT %M0x44.14 : BOOL; (* Trace Mode 0 *)
    SR_T1 AT %M0x44.15 : BOOL; (* Trace Mode 1 *)

    (* 外设变量 *)
    (* Versatile Interface Adapter 6522 *)
    VIA_BASE AT %MW0xE00000 : WORD;
    VIA_ORB AT %MW0xE00000 : BYTE;
    VIA_ORA AT %MW0xE00002 : BYTE;
    VIA_DDRB AT %MW0xE00004 : BYTE;
    VIA_DDRA AT %MW0xE00006 : BYTE;
    VIA_T1C_L AT %MW0xE00008 : WORD;
    VIA_T1C_H AT %MW0xE0000A : WORD;
    VIA_T1L_L AT %MW0xE0000C : WORD;
    VIA_T1L_H AT %MW0xE0000E : WORD;
    VIA_T2C_L AT %MW0xE00010 : WORD;
    VIA_T2C_H AT %MW0xE00012 : WORD;
    VIA_SR AT %MW0xE00014 : BYTE;
    VIA_ACR AT %MW0xE00016 : BYTE;
    VIA_PCR AT %MW0xE00018 : BYTE;
    VIA_IFR AT %MW0xE0001E : BYTE;
    VIA_IER AT %MW0xE0001E : BYTE;
    (* SCC 8530 Serial Communications Controller *)
    SCC_BASE AT %MW0xF00000 : WORD;
    SCC_SCC_CHA_B AT %MW0xF00000 : BYTE;
    SCC_SCC_CHA_C AT %MW0xF00002 : BYTE;
    SCC_SCC_CHB_D AT %MW0xF00004 : BYTE;
    SCC_SCC_CHB_CT AT %MW0xF00006 : BYTE;
    (* Integrated Woz Machine - Floppy Disk Controller *)
    IWM_BASE AT %MW0x1E00000 : WORD;
    IWM_IWM_DATA AT %MW0x1E00000 : BYTE;
    IWM_IWM_MODE AT %MW0x1E00008 : BYTE;
    IWM_IWM_Q6L AT %MW0x1E00020 : BYTE;
    IWM_IWM_Q7L AT %MW0x1E00022 : BYTE;
    IWM_IWM_Q6R AT %MW0x1E00024 : BYTE;
    IWM_IWM_Q7R AT %MW0x1E00026 : BYTE;
    (* Video Graphics Controller (custom Apple chip) *)
    VGC_BASE AT %MW0x00F20000 : WORD;
    VGC_VGC_MODE AT %MW0x00F20000 : BYTE;
    VGC_VGC_START_HI AT %MW0x00F20002 : BYTE;
    VGC_VGC_START_LO AT %MW0x00F20004 : BYTE;
    (* Apple Desktop Bus *)
    ADB_BASE AT %MW0x01600000 : WORD;
    ADB_ADB_DATA AT %MW0x01600000 : BYTE;
    ADB_ADB_STATUS AT %MW0x01600004 : BYTE;
    ADB_ADB_CMD AT %MW0x01600008 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Reset Initial SP *)
    INT_RESET_PC : BOOL; (* Reset Initial PC *)
    INT_IRQ1 : BOOL; (* VIA interrupt (level 1) *)
    INT_IRQ2 : BOOL; (* SCC interrupt (level 2) *)
    INT_IRQ3 : BOOL; (* ADB / VIA (level 3) *)
    INT_IRQ4 : BOOL; (* ADB / VIA (level 4) *)

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
    --|INT_RESET|--[中断处理]--
    --|INT_RESET_PC|--[中断处理]--
    --|INT_IRQ1|--[中断处理]--
    --|INT_IRQ2|--[中断处理]--
    --|INT_IRQ3|--[中断处理]--
    --|INT_IRQ4|--[中断处理]--

END_PROGRAM
