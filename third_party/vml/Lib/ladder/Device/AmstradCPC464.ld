(*
 * Amstrad-CPC-464梯形图定义
 * 生成自: Amstrad/CPC/Amstrad-CPC-464
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
 * CPU架构: Z80A
 * 位宽: 8位
 * 时钟频率: 4000000 Hz
 *)

PROGRAM Amstrad_CPC_464
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    F AT %M0x01 : BYTE; (* Flags *)
    F_C AT %M0x01.0 : BOOL; (* Carry *)
    F_N AT %M0x01.1 : BOOL; (* Subtract *)
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
    I AT %M0x10 : BYTE; (* Interrupt Vector *)
    R AT %M0x11 : BYTE; (* Refresh *)
    IX AT %M0x12 : WORD; (* Index X *)
    IY AT %M0x14 : WORD; (* Index Y *)
    SP AT %M0x16 : WORD; (* Stack Pointer *)
    PC AT %M0x18 : WORD; (* Program Counter *)

    (* 外设变量 *)
    (* Gate Array - Custom ASIC (video/sound/RAM control) *)
    GA_BASE AT %MW0x7F00 : WORD;
    GA_GA_MR AT %MW0x7F00 : BYTE;
    GA_GA_IR AT %MW0x7F01 : BYTE;
    GA_GA_R1 AT %MW0x7F02 : BYTE;
    GA_GA_R2 AT %MW0x7F03 : BYTE;
    GA_GA_R3 AT %MW0x7F04 : BYTE;
    GA_GA_R4 AT %MW0x7F05 : BYTE;
    GA_GA_R5 AT %MW0x7F06 : BYTE;
    GA_GA_R6 AT %MW0x7F07 : BYTE;
    GA_GA_R7 AT %MW0x7F08 : BYTE;
    (* CRT Controller 6845 - Video timing *)
    CRTC_BASE AT %MW0xBC00 : WORD;
    CRTC_CRTC_REG AT %MW0xBC00 : BYTE;
    CRTC_CRTC_DATA AT %MW0xBD00 : BYTE;
    CRTC_CRTC_H_TOTAL AT %MW0xBC01 : BYTE;
    CRTC_CRTC_H_DISP AT %MW0xBC02 : BYTE;
    CRTC_CRTC_HSYNC_POS AT %MW0xBC03 : BYTE;
    CRTC_CRTC_HSYNC_WIDTH AT %MW0xBC04 : BYTE;
    CRTC_CRTC_V_TOTAL AT %MW0xBC05 : BYTE;
    CRTC_CRTC_V_TOTAL_ADJ AT %MW0xBC06 : BYTE;
    CRTC_CRTC_V_DISP AT %MW0xBC07 : BYTE;
    CRTC_CRTC_VSYNC_POS AT %MW0xBC08 : BYTE;
    CRTC_CRTC_INTERLACE AT %MW0xBC09 : BYTE;
    CRTC_CRTC_CURSOR_START AT %MW0xBC0A : BYTE;
    CRTC_CRTC_CURSOR_END AT %MW0xBC0B : BYTE;
    CRTC_CRTC_SA_HI AT %MW0xBC0C : BYTE;
    CRTC_CRTC_SA_LO AT %MW0xBC0D : BYTE;
    CRTC_CRTC_CURSOR_HI AT %MW0xBC0E : BYTE;
    CRTC_CRTC_CURSOR_LO AT %MW0xBC0F : BYTE;
    (* AY-3-8912 Programmable Sound Generator *)
    PSG_BASE AT %MW0xF400 : WORD;
    PSG_PSG_REG AT %MW0xF400 : BYTE;
    PSG_PSG_DATA AT %MW0xF600 : BYTE;
    PSG_FREQ_A_LO AT %MW0xF400 : BYTE;
    PSG_FREQ_A_HI AT %MW0xF401 : BYTE;
    PSG_FREQ_B_LO AT %MW0xF402 : BYTE;
    PSG_FREQ_B_HI AT %MW0xF403 : BYTE;
    PSG_FREQ_C_LO AT %MW0xF404 : BYTE;
    PSG_FREQ_C_HI AT %MW0xF405 : BYTE;
    PSG_NOISE_FREQ AT %MW0xF406 : BYTE;
    PSG_ENABLE AT %MW0xF407 : BYTE;
    PSG_VOL_A AT %MW0xF408 : BYTE;
    PSG_VOL_B AT %MW0xF409 : BYTE;
    PSG_VOL_C AT %MW0xF40A : BYTE;
    PSG_ENV_FREQ_LO AT %MW0xF40B : BYTE;
    PSG_ENV_FREQ_HI AT %MW0xF40C : BYTE;
    PSG_ENV_SHAPE AT %MW0xF40D : BYTE;
    PSG_PORT_A AT %MW0xF40E : BYTE;
    PSG_PORT_B AT %MW0xF40F : BYTE;
    (* WD1772 Floppy Disk Controller (via expansion) *)
    FDC_BASE AT %MW0xF800 : WORD;
    FDC_FDC_STATUS AT %MW0xF8E0 : BYTE;
    FDC_FDC_COMMAND AT %MW0xF8E0 : BYTE;
    FDC_FDC_TRACK AT %MW0xF8E1 : BYTE;
    FDC_FDC_SECTOR AT %MW0xF8E2 : BYTE;
    FDC_FDC_DATA AT %MW0xF8E3 : BYTE;
    (* Centronics Parallel Printer Port *)
    PRINTER_BASE AT %MW0xEE : WORD;
    PRINTER_PRN_DATA AT %MW0xEE : BYTE;
    PRINTER_PRN_STROBE AT %MW0xEF : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Power-on / Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt *)
    INT_INT : BOOL; (* Gate Array interrupt (50Hz vertical blank) *)

END_VAR

BEGIN
    (* Amstrad-CPC-464初始化梯形图 *)
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
