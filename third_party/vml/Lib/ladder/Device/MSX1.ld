(*
 * MSX1梯形图定义
 * 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
 * CPU架构: Z80A
 * 位宽: 8位
 * 时钟频率: 3579545 Hz
 *)

PROGRAM MSX1
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
    IY AT %M0x14 : WORD; (* Index Y (usually = 0xF38F) *)
    SP AT %M0x16 : WORD; (* Stack Pointer *)
    PC AT %M0x18 : WORD; (* Program Counter *)

    (* 外设变量 *)
    (* TMS9918A Video Display Processor *)
    VDP_BASE AT %MW0x98 : WORD;
    VDP_VDP_REG0 AT %MW0x99 : BYTE;
    VDP_VDP_REG1 AT %MW0x99 : BYTE;
    VDP_VDP_REG2 AT %MW0x99 : BYTE;
    VDP_VDP_REG3 AT %MW0x99 : BYTE;
    VDP_VDP_REG4 AT %MW0x99 : BYTE;
    VDP_VDP_REG5 AT %MW0x99 : BYTE;
    VDP_VDP_REG6 AT %MW0x99 : BYTE;
    VDP_VDP_REG7 AT %MW0x99 : BYTE;
    VDP_VDP_STATUS AT %MW0x99 : BYTE;
    VDP_VDP_DATA AT %MW0x98 : BYTE;
    VDP_VDP_POT AT %MW0x98 : BYTE;
    (* AY-3-8910 Programmable Sound Generator *)
    PSG_BASE AT %MW0xA0 : WORD;
    PSG_PSG_REG AT %MW0xA1 : BYTE;
    PSG_PSG_DATA AT %MW0xA3 : BYTE;
    PSG_FREQ_A_LO AT %MW0xA0 : BYTE;
    PSG_FREQ_A_HI AT %MW0xA1 : BYTE;
    PSG_FREQ_B_LO AT %MW0xA2 : BYTE;
    PSG_FREQ_B_HI AT %MW0xA3 : BYTE;
    PSG_FREQ_C_LO AT %MW0xA4 : BYTE;
    PSG_FREQ_C_HI AT %MW0xA5 : BYTE;
    PSG_NOISE_FREQ AT %MW0xA6 : BYTE;
    PSG_ENABLE AT %MW0xA7 : BYTE;
    PSG_VOL_A AT %MW0xA8 : BYTE;
    PSG_VOL_B AT %MW0xA9 : BYTE;
    PSG_VOL_C AT %MW0xAA : BYTE;
    PSG_ENV_FREQ_LO AT %MW0xAB : BYTE;
    PSG_ENV_FREQ_HI AT %MW0xAC : BYTE;
    PSG_ENV_SHAPE AT %MW0xAD : BYTE;
    PSG_PORT_A AT %MW0xAE : BYTE;
    PSG_PORT_B AT %MW0xAF : BYTE;
    (* PPI 8255 Programmable Peripheral Interface *)
    PPI_BASE AT %MW0xA8 : WORD;
    PPI_PPI_PA AT %MW0xA8 : BYTE;
    PPI_PPI_PB AT %MW0xA9 : BYTE;
    PPI_PPI_PC AT %MW0xAA : BYTE;
    PPI_PPI_CTRL AT %MW0xAB : BYTE;
    (* MSX Slot Expansion System *)
    SLOTEXP_BASE AT %MW0x0000 : WORD;
    SLOTEXP_SLOT0 AT %MW0xFCC0 : BYTE;
    SLOTEXP_SLOT1 AT %MW0xFCC1 : BYTE;
    SLOTEXP_SLOT2 AT %MW0xFCC2 : BYTE;
    SLOTEXP_SLOT3 AT %MW0xFCC3 : BYTE;
    SLOTEXP_EXPTBL0 AT %MW0xFCC4 : BYTE;
    SLOTEXP_EXPTBL1 AT %MW0xFCC5 : BYTE;
    SLOTEXP_EXPTBL2 AT %MW0xFCC6 : BYTE;
    SLOTEXP_EXPTBL3 AT %MW0xFCC7 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Power-on / Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt *)
    INT_INT : BOOL; (* VDP Vertical Interrupt (frame) *)

END_VAR

BEGIN
    (* MSX1初始化梯形图 *)
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
