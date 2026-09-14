(*
 * Commodore-PET梯形图定义
 * 生成自: Commodore International/PET/Commodore-PET
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
 * CPU架构: MOS 6502
 * 位宽: 8位
 * 时钟频率: 1000000 Hz
 *)

PROGRAM Commodore_PET
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0 : BYTE; (* Accumulator *)
    X AT %M0 : BYTE; (* Index Register X *)
    Y AT %M0 : BYTE; (* Index Register Y *)
    SP AT %M0 : BYTE; (* Stack Pointer *)
    PC AT %M0 : WORD; (* Program Counter *)
    P AT %M0 : BYTE; (* Status Register *)

    (* 外设变量 *)
    (* Peripheral Interface Adapter 1 (6520) *)
    PIA1_BASE AT %MW : WORD;
    PIA1_PIA1_DDRA AT %MW0xE810 : BYTE;
    PIA1_PIA1_ORA AT %MW0xE811 : BYTE;
    PIA1_PIA1_DDRB AT %MW0xE812 : BYTE;
    PIA1_PIA1_ORB AT %MW0xE813 : BYTE;
    PIA1_PIA1_CRA AT %MW0xE814 : BYTE;
    PIA1_PIA1_CRB AT %MW0xE815 : BYTE;
    (* Peripheral Interface Adapter 2 (6520) *)
    PIA2_BASE AT %MW : WORD;
    PIA2_PIA2_DDRA AT %MW0xE820 : BYTE;
    PIA2_PIA2_ORA AT %MW0xE821 : BYTE;
    PIA2_PIA2_DDRB AT %MW0xE822 : BYTE;
    PIA2_PIA2_ORB AT %MW0xE823 : BYTE;
    PIA2_PIA2_CRA AT %MW0xE824 : BYTE;
    PIA2_PIA2_CRB AT %MW0xE825 : BYTE;
    (* Versatile Interface Adapter (6522) *)
    VIA_BASE AT %MW : WORD;
    VIA_VIA_ORB AT %MW0xE840 : BYTE;
    VIA_VIA_ORA AT %MW0xE841 : BYTE;
    VIA_VIA_DDRB AT %MW0xE842 : BYTE;
    VIA_VIA_DDRA AT %MW0xE843 : BYTE;
    VIA_VIA_T1CL AT %MW0xE844 : BYTE;
    VIA_VIA_T1CH AT %MW0xE845 : BYTE;
    VIA_VIA_T1LL AT %MW0xE846 : BYTE;
    VIA_VIA_T1LH AT %MW0xE847 : BYTE;
    VIA_VIA_T2CL AT %MW0xE848 : BYTE;
    VIA_VIA_T2CH AT %MW0xE849 : BYTE;
    VIA_VIA_SR AT %MW0xE84A : BYTE;
    VIA_VIA_ACR AT %MW0xE84B : BYTE;
    VIA_VIA_PCR AT %MW0xE84C : BYTE;
    VIA_VIA_IFR AT %MW0xE84D : BYTE;
    VIA_VIA_IER AT %MW0xE84E : BYTE;
    (* CRT Controller (6545) *)
    CRTC_BASE AT %MW : WORD;
    CRTC_CRTC_ADDR AT %MW0xE880 : BYTE;
    CRTC_CRTC_DATA AT %MW0xE881 : BYTE;
    (* Cassette tape interface *)
    CASSETTE_BASE AT %MW : WORD;
    CASSETTE_CASS_MOTOR AT %MW0xE840 : BYTE;
    CASSETTE_CASS_WRITE AT %MW0xE842 : BYTE;
    CASSETTE_CASS_READ AT %MW0xE812 : BYTE;
    (* IEEE-488 bus interface *)
    IEEE488_BASE AT %MW : WORD;
    IEEE488_IEEE_DATA AT %MW0xE801 : BYTE;
    IEEE488_IEEE_STATUS AT %MW0xE802 : BYTE;
    IEEE488_IEEE_CONTROL AT %MW0xE803 : BYTE;

    (* 中断变量 *)
    INT_NMI : BOOL; (* Non-maskable interrupt *)
    INT_RESET : BOOL; (* Reset vector *)
    INT_IRQ : BOOL; (* Interrupt request *)
    INT_BRK : BOOL; (* Break instruction *)

END_VAR

BEGIN
    (* Commodore-PET初始化梯形图 *)
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
    --|INT_NMI|--[中断处理]--
    --|INT_RESET|--[中断处理]--
    --|INT_IRQ|--[中断处理]--
    --|INT_BRK|--[中断处理]--

END_PROGRAM
