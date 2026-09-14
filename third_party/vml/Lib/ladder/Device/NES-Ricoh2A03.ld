(*
 * Ricoh-2A03梯形图定义
 * 生成自: Ricoh/MOS-6502/Ricoh-2A03
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
 * CPU架构: MOS-6502
 * 位宽: 8位
 * 时钟频率: 10765930 Hz
 *)

PROGRAM Ricoh_2A03
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    X AT %M0x01 : BYTE; (* X Index *)
    Y AT %M0x02 : BYTE; (* Y Index *)
    SP AT %M0x03 : BYTE; (* Stack Pointer *)
    PC AT %M0x04 : WORD; (* Program Counter (16-bit) *)
    P AT %M0x06 : BYTE; (* Processor Status *)
    P_C AT %M0x06.0 : BOOL; (* Carry *)
    P_Z AT %M0x06.1 : BOOL; (* Zero *)
    P_I AT %M0x06.2 : BOOL; (* Interrupt Disable *)
    P_D AT %M0x06.3 : BOOL; (* Decimal Mode *)
    P_B AT %M0x06.4 : BOOL; (* Break *)
    P_U AT %M0x06.5 : BOOL; (* Unused *)
    P_V AT %M0x06.6 : BOOL; (* Overflow *)
    P_N AT %M0x06.7 : BOOL; (* Negative *)

    (* 外设变量 *)
    (* Picture Processing Unit *)
    PPU_BASE AT %MW0x2000 : WORD;
    PPU_PPUCTRL AT %MW0x2000 : BYTE;
    PPU_PPUMASK AT %MW0x2001 : BYTE;
    PPU_PPUSTATUS AT %MW0x2002 : BYTE;
    PPU_OAMADDR AT %MW0x2003 : BYTE;
    PPU_OAMDATA AT %MW0x2004 : BYTE;
    PPU_PPUSCROLL AT %MW0x2005 : BYTE;
    PPU_PPUADDR AT %MW0x2006 : BYTE;
    PPU_PPUDATA AT %MW0x2007 : BYTE;
    (* Audio Processing Unit *)
    APU_BASE AT %MW0x4000 : WORD;
    APU_PULSE1_VOL AT %MW0x4000 : BYTE;
    APU_PULSE1_SWEEP AT %MW0x4001 : BYTE;
    APU_PULSE1_LO AT %MW0x4002 : BYTE;
    APU_PULSE1_HI AT %MW0x4003 : BYTE;
    APU_PULSE2_VOL AT %MW0x4004 : BYTE;
    APU_PULSE2_SWEEP AT %MW0x4005 : BYTE;
    APU_PULSE2_LO AT %MW0x4006 : BYTE;
    APU_PULSE2_HI AT %MW0x4007 : BYTE;
    APU_TRIANGLE AT %MW0x4008 : BYTE;
    APU_TRIANGLE_HI AT %MW0x400B : BYTE;
    APU_NOISE_VOL AT %MW0x400C : BYTE;
    APU_NOISE_HI AT %MW0x400E : BYTE;
    APU_NOISE_LENGTH AT %MW0x400F : BYTE;
    APU_DMC_RATE AT %MW0x4010 : BYTE;
    APU_DMC_RAW AT %MW0x4011 : BYTE;
    APU_DMC_START AT %MW0x4012 : BYTE;
    APU_DMC_LENGTH AT %MW0x4013 : BYTE;
    APU_OAMDMA AT %MW0x4014 : BYTE;
    APU_SNDCHN AT %MW0x4015 : BYTE;
    APU_JOY1 AT %MW0x4016 : BYTE;
    APU_JOY2 AT %MW0x4017 : BYTE;
    (* Controller Port 1 *)
    INPUT1_BASE AT %MW0x4016 : WORD;
    INPUT1_JOYPAD1 AT %MW0x4016 : BYTE;
    (* Controller Port 2 *)
    INPUT2_BASE AT %MW0x4017 : WORD;
    INPUT2_JOYPAD2 AT %MW0x4017 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt (VBlank) *)
    INT_IRQ : BOOL; (* IRQ / BRK *)

END_VAR

BEGIN
    (* Ricoh-2A03初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|A>0|--(监控灯)--
    --|X>0|--(监控灯)--
    --|Y>0|--(监控灯)--
    --|SP>0|--(监控灯)--
    --|PC>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_IRQ|--[中断处理]--

END_PROGRAM
