(*
 * Zilog-Z80梯形图定义
 * 生成自: Zilog/Z80/Zilog-Z80
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
 * CPU架构: Z80
 * 位宽: 8位
 * 时钟频率: 3580000 Hz
 *)

PROGRAM Zilog_Z80
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    F AT %M0x01 : BYTE; (* Flags Register *)
    F_C AT %M0x01.0 : BOOL; (* Carry *)
    F_N AT %M0x01.1 : BOOL; (* Subtract *)
    F_P AT %M0x01.2 : BOOL; (* Parity/Overflow *)
    F_H AT %M0x01.4 : BOOL; (* Half Carry *)
    F_Z AT %M0x01.6 : BOOL; (* Zero *)
    F_S AT %M0x01.7 : BOOL; (* Sign/Negative *)
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
    IX AT %M0x10 : WORD; (* Index Register X *)
    IY AT %M0x12 : WORD; (* Index Register Y *)
    SP AT %M0x14 : WORD; (* Stack Pointer *)
    PC AT %M0x16 : WORD; (* Program Counter *)
    I AT %M0x18 : BYTE; (* Interrupt Vector Register *)
    R AT %M0x19 : BYTE; (* Memory Refresh Register *)
    IM AT %M0x1A : BYTE; (* Interrupt Mode (0/1/2) *)

    (* 外设变量 *)
    (* Video Display Processor (TMS9918A variant) *)
    VDP_BASE AT %MW0xBE : WORD;
    VDP_VDP_CTRL AT %MW0xBF : BYTE;
    VDP_VDP_DATA AT %MW0xBE : BYTE;
    VDP_VDP_STATUS AT %MW0xBF : BYTE;
    VDP_VDP_STATUS_FIFO_FULL AT %MW0xBF.0 : BOOL; (* VRAM to CPU Transfer Pending *)
    VDP_VDP_STATUS_FIFO_EMPTY AT %MW0xBF.1 : BOOL; (* VRAM Write FIFO Empty *)
    VDP_VDP_STATUS_INT_FLAG AT %MW0xBF.7 : BOOL; (* V-Blank / Sprite Collision Flag *)
    VDP_R0 AT %MW0x00 : BYTE;
    VDP_R0_M3 AT %MW0x00.0 : BOOL; (* Mode 3 Enable *)
    VDP_R0_M2 AT %MW0x00.1 : BOOL; (* Mode 2 Enable *)
    VDP_R0_M1 AT %MW0x00.2 : BOOL; (* Mode 1 Enable *)
    VDP_R0_DISPLAY_DISABLE AT %MW0x00.3 : BOOL; (* Display Disable (1=blank screen) *)
    VDP_R0_VIRQ_EN AT %MW0x00.4 : BOOL; (* Vertical Interrupt Enable *)
    VDP_R0_M4 AT %MW0x00.5 : BOOL; (* Mode 4 Enable (SMS2 only) *)
    VDP_R0_SPRITE_SHIFT AT %MW0x00.6 : BOOL; (* Sprite Double Height *)
    VDP_R0_HVC_LATCH AT %MW0x00.7 : BOOL; (* H-Counter Latch Enable *)
    VDP_R1 AT %MW0x01 : BYTE;
    VDP_R1_DISPLAY AT %MW0x01.3 : BOOL; (* Display Enable (1=active) *)
    VDP_R1_FRAME_INT AT %MW0x01.4 : BOOL; (* Frame Interrupt (V-Blank) Enable *)
    VDP_R1_M4 AT %MW0x01.5 : BOOL; (* Mode 4 (256-color) *)
    VDP_R1_SMS_MODE AT %MW0x01.6 : BOOL; (* SMS Display Mode (vs Coleco) *)
    VDP_R1_EXT_VIDEO AT %MW0x01.7 : BOOL; (* External Video Enable *)
    VDP_R2 AT %MW0x02 : BYTE;
    VDP_R3 AT %MW0x03 : BYTE;
    VDP_R4 AT %MW0x04 : BYTE;
    VDP_R5 AT %MW0x05 : BYTE;
    VDP_R6 AT %MW0x06 : BYTE;
    VDP_R7 AT %MW0x07 : BYTE;
    VDP_R8 AT %MW0x08 : BYTE;
    VDP_R8_HSCROLL_EN AT %MW0x08.0 : BOOL; (* Horizontal Scroll Enable *)
    VDP_R8_VSCROLL_EN AT %MW0x08.1 : BOOL; (* Vertical Scroll Enable *)
    VDP_R8_LINE_INT AT %MW0x08.4 : BOOL; (* Line Interrupt Enable *)
    VDP_R8_VSCROLL_2X AT %MW0x08.7 : BOOL; (* Vertical Scroll 2x Speed *)
    VDP_R9 AT %MW0x09 : BYTE;
    VDP_R10 AT %MW0x0A : BYTE;
    VDP_R11 AT %MW0x0B : BYTE;
    VDP_R12 AT %MW0x0C : BYTE;
    VDP_R13 AT %MW0x0D : BYTE;
    VDP_R14 AT %MW0x0E : BYTE;
    VDP_R15 AT %MW0x0F : BYTE;
    VDP_VCOUNTER AT %MW0x7E : BYTE;
    VDP_HCOUNTER AT %MW0x7F : BYTE;
    (* SN76489 Programmable Sound Generator (3 Square + 1 Noise) *)
    PSG_BASE AT %MW0x7F : WORD;
    PSG_CH0_FREQ AT %MW0x00 : BYTE;
    PSG_CH1_FREQ AT %MW0x02 : BYTE;
    PSG_CH2_FREQ AT %MW0x04 : BYTE;
    PSG_CH3_CONFIG AT %MW0x06 : BYTE;
    PSG_CH3_CONFIG_TYPE AT %MW0x06.0 : BOOL; (* Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type) *)
    PSG_CH3_CONFIG_VOLUME AT %MW0x06.0 : BOOL; (* Volume (0-15) *)
    PSG_CH0_VOLUME AT %MW0x01 : BYTE;
    PSG_CH1_VOLUME AT %MW0x03 : BYTE;
    PSG_CH2_VOLUME AT %MW0x05 : BYTE;
    (* I/O Port Registers *)
    PORTS_BASE AT %MW0x3F : WORD;
    PORTS_PORT_A AT %MW0x3F : BYTE;
    PORTS_PORT_A_UP AT %MW0x3F.0 : BOOL; (* Up (0=pressed) *)
    PORTS_PORT_A_DOWN AT %MW0x3F.1 : BOOL; (* Down (0=pressed) *)
    PORTS_PORT_A_LEFT AT %MW0x3F.2 : BOOL; (* Left (0=pressed) *)
    PORTS_PORT_A_RIGHT AT %MW0x3F.3 : BOOL; (* Right (0=pressed) *)
    PORTS_PORT_A_TR AT %MW0x3F.4 : BOOL; (* Button TR (0=pressed) *)
    PORTS_PORT_A_TL AT %MW0x3F.5 : BOOL; (* Button TL (0=pressed) *)
    PORTS_PORT_B AT %MW0x3F : BYTE;
    PORTS_PORT_B_UP AT %MW0x3F.0 : BOOL; (* Up (0=pressed) *)
    PORTS_PORT_B_DOWN AT %MW0x3F.1 : BOOL; (* Down (0=pressed) *)
    PORTS_PORT_B_LEFT AT %MW0x3F.2 : BOOL; (* Left (0=pressed) *)
    PORTS_PORT_B_RIGHT AT %MW0x3F.3 : BOOL; (* Right (0=pressed) *)
    PORTS_PORT_B_TR AT %MW0x3F.4 : BOOL; (* Button TR (0=pressed) *)
    PORTS_PORT_B_TL AT %MW0x3F.5 : BOOL; (* Button TL (0=pressed) *)
    PORTS_PORT_A_DDR AT %MW0x3F : BYTE;
    PORTS_PORT_B_DDR AT %MW0x3F : BYTE;
    (* Sega Mapper (Memory Bank Switching) *)
    SEGAMAPPER_BASE AT %MW0xFFFD : WORD;
    SEGAMAPPER_ROM_BANK0 AT %MW0xFFFD : BYTE;
    SEGAMAPPER_ROM_BANK1 AT %MW0xFFFE : BYTE;
    SEGAMAPPER_ROM_BANK2 AT %MW0xFFFF : BYTE;
    (* Memory Mapper Control *)
    MAPPER_BASE AT %MW0xFFFF : WORD;
    MAPPER_SRAM_BANK AT %MW0xFFF8 : BYTE;

    (* 中断变量 *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt (Pause button / V-Blank) *)
    INT_INT_VBLANK : BOOL; (* V-Blank Interrupt (Frame end) *)
    INT_INT_LINE : BOOL; (* Scanline Interrupt (Line counter match) *)
    INT_INT_EXT : BOOL; (* External I/O Interrupt *)

END_VAR

BEGIN
    (* Zilog-Z80初始化梯形图 *)
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
    --|INT_NMI|--[中断处理]--
    --|INT_INT_VBLANK|--[中断处理]--
    --|INT_INT_LINE|--[中断处理]--
    --|INT_INT_EXT|--[中断处理]--

END_PROGRAM
