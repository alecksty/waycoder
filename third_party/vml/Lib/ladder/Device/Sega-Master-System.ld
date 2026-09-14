(*
 * Sega-Master-System梯形图定义
 * 生成自: Sega/Master System/Sega-Master-System
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Sega Master System 8-bit video game console with Z80 CPU
 * CPU架构: Zilog Z80
 * 位宽: 8位
 * 时钟频率: 3579545 Hz
 *)

PROGRAM Sega_Master_System
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

    (* 外设变量 *)
    (* Video Display Processor (TMS9918A) *)
    VDP_BASE AT %MW : WORD;
    VDP_VDP_DATA AT %MW0xBE : BYTE;
    VDP_VDP_ADDR AT %MW0xBF : BYTE;
    VDP_VDP_STATUS AT %MW0xBF : BYTE;
    (* Programmable Sound Generator (SN76489) *)
    PSG_BASE AT %MW : WORD;
    PSG_PSG_DATA AT %MW0x7F : BYTE;
    (* I/O ports *)
    IO_BASE AT %MW : WORD;
    IO_IO_PORT_A AT %MW0xDC : BYTE;
    IO_IO_PORT_B AT %MW0xDD : BYTE;
    IO_IO_PORT_MISC AT %MW0xDE : BYTE;
    IO_IO_PORT_VDP AT %MW0xDF : BYTE;
    (* Memory mapper *)
    MEMORYMAPPER_BASE AT %MW : WORD;
    MEMORYMAPPER_MAPPER_0 AT %MW0xFFFC : BYTE;
    MEMORYMAPPER_MAPPER_1 AT %MW0xFFFD : BYTE;
    MEMORYMAPPER_MAPPER_2 AT %MW0xFFFE : BYTE;
    MEMORYMAPPER_MAPPER_3 AT %MW0xFFFF : BYTE;
    (* FM Sound Unit (optional) *)
    FMUNIT_BASE AT %MW : WORD;
    FMUNIT_FM_ADDR AT %MW0xF0 : BYTE;
    FMUNIT_FM_DATA AT %MW0xF1 : BYTE;
    FMUNIT_FM_DETECT AT %MW0xF2 : BYTE;

    (* 中断变量 *)
    INT_RST_00 : BOOL; (* Restart 00h *)
    INT_IM1 : BOOL; (* Interrupt Mode 1 *)
    INT_VBLANK : BOOL; (* Vertical blank interrupt *)
    INT_LINE : BOOL; (* Line interrupt *)

END_VAR

BEGIN
    (* Sega-Master-System初始化梯形图 *)
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
    --|INT_RST_00|--[中断处理]--
    --|INT_IM1|--[中断处理]--
    --|INT_VBLANK|--[中断处理]--
    --|INT_LINE|--[中断处理]--

END_PROGRAM
