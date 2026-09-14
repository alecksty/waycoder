(*
 * Nintendo Entertainment System梯形图定义
 * 生成自: Nintendo/NES/Nintendo Entertainment System
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
 * CPU架构: 6502
 * 位宽: 0位
 * 时钟频率: 0 Hz
 *)

PROGRAM Nintendo Entertainment System
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* Picture Processing Unit (Ricoh 2C02) *)
    PPU_BASE AT %MW : WORD;
    PPU_PPUCTRL AT %MW0x2000 : LWORD;
    PPU_PPUCTRL_NMI AT %MW0x2000.7 : BOOL; (* VBlank NMI enable *)
    PPU_PPUCTRL_MASTERSLAVE AT %MW0x2000.6 : BOOL; (* Master/slave select *)
    PPU_PPUCTRL_SPRITESIZE AT %MW0x2000.5 : BOOL; (* Sprite size (0=8x8, 1=8x16) *)
    PPU_PPUCTRL_BGPATTERN AT %MW0x2000.4 : BOOL; (* Background pattern table address *)
    PPU_PPUCTRL_SPRITEPATTERN AT %MW0x2000.3 : BOOL; (* Sprite pattern table address *)
    PPU_PPUCTRL_VRAMINCREMENT AT %MW0x2000.2 : BOOL; (* VRAM address increment (0=1, 1=32) *)
    PPU_PPUCTRL_NAMETABLE AT %MW0x2000.0 : BOOL; (* Nametable address *)
    PPU_PPUMASK AT %MW0x2001 : LWORD;
    PPU_PPUMASK_EMPHASIZEBLUE AT %MW0x2001.7 : BOOL; (* Emphasize blue *)
    PPU_PPUMASK_EMPHASIZEGREEN AT %MW0x2001.6 : BOOL; (* Emphasize green *)
    PPU_PPUMASK_EMPHASIZERED AT %MW0x2001.5 : BOOL; (* Emphasize red *)
    PPU_PPUMASK_SHOWSPRITES AT %MW0x2001.4 : BOOL; (* Show sprites *)
    PPU_PPUMASK_SHOWBACKGROUND AT %MW0x2001.3 : BOOL; (* Show background *)
    PPU_PPUMASK_SHOWLEFTSPRITES AT %MW0x2001.2 : BOOL; (* Show sprites in left 8 pixels *)
    PPU_PPUMASK_SHOWLEFTBACKGROUND AT %MW0x2001.1 : BOOL; (* Show background in left 8 pixels *)
    PPU_PPUMASK_GRAYSCALE AT %MW0x2001.0 : BOOL; (* Grayscale mode *)
    PPU_PPUSTATUS AT %MW0x2002 : LWORD;
    PPU_PPUSTATUS_VBLANK AT %MW0x2002.7 : BOOL; (* VBlank started *)
    PPU_PPUSTATUS_SPRITE0HIT AT %MW0x2002.6 : BOOL; (* Sprite 0 hit *)
    PPU_PPUSTATUS_SPRITEOVERFLOW AT %MW0x2002.5 : BOOL; (* Sprite overflow *)
    PPU_OAMADDR AT %MW0x2003 : LWORD;
    PPU_OAMDATA AT %MW0x2004 : LWORD;
    PPU_PPUSCROLL AT %MW0x2005 : LWORD;
    PPU_PPUADDR AT %MW0x2006 : LWORD;
    PPU_PPUDATA AT %MW0x2007 : LWORD;
    PPU_OAMDMA AT %MW0x4014 : LWORD;
    (* Audio Processing Unit (Ricoh 2A03) *)
    APU_BASE AT %MW : WORD;
    APU_SQ1_VOL AT %MW0x4000 : LWORD;
    APU_SQ1_VOL_DUTY AT %MW0x4000.6 : BOOL; (* Duty cycle *)
    APU_SQ1_VOL_LENGTHCOUNTERHALT AT %MW0x4000.5 : BOOL; (* Length counter halt/envelope loop *)
    APU_SQ1_VOL_CONSTANTVOLUME AT %MW0x4000.4 : BOOL; (* Constant volume *)
    APU_SQ1_VOL_VOLUME AT %MW0x4000.0 : BOOL; (* Volume/envelope period *)
    APU_SQ1_SWEEP AT %MW0x4001 : LWORD;
    APU_SQ1_SWEEP_ENABLED AT %MW0x4001.7 : BOOL; (* Sweep enabled *)
    APU_SQ1_SWEEP_PERIOD AT %MW0x4001.4 : BOOL; (* Sweep period *)
    APU_SQ1_SWEEP_NEGATE AT %MW0x4001.3 : BOOL; (* Sweep negate *)
    APU_SQ1_SWEEP_SHIFT AT %MW0x4001.0 : BOOL; (* Sweep shift amount *)
    APU_SQ1_LO AT %MW0x4002 : LWORD;
    APU_SQ1_HI AT %MW0x4003 : LWORD;
    APU_SQ1_HI_LENGTHCOUNTER AT %MW0x4003.3 : BOOL; (* Length counter load *)
    APU_SQ1_HI_TIMERHIGH AT %MW0x4003.0 : BOOL; (* Timer high bits *)
    APU_SQ2_VOL AT %MW0x4004 : LWORD;
    APU_SQ2_SWEEP AT %MW0x4005 : LWORD;
    APU_SQ2_LO AT %MW0x4006 : LWORD;
    APU_SQ2_HI AT %MW0x4007 : LWORD;
    APU_TRI_LINEAR AT %MW0x4008 : LWORD;
    APU_TRI_LINEAR_CONTROL AT %MW0x4008.7 : BOOL; (* Length counter halt/linear counter control *)
    APU_TRI_LINEAR_PERIOD AT %MW0x4008.0 : BOOL; (* Linear counter load *)
    APU_TRI_LO AT %MW0x400A : LWORD;
    APU_TRI_HI AT %MW0x400B : LWORD;
    APU_NOISE_VOL AT %MW0x400C : LWORD;
    APU_NOISE_LO AT %MW0x400E : LWORD;
    APU_NOISE_LO_MODE AT %MW0x400E.7 : BOOL; (* Noise mode *)
    APU_NOISE_LO_PERIOD AT %MW0x400E.0 : BOOL; (* Noise period *)
    APU_NOISE_HI AT %MW0x400F : LWORD;
    APU_DMC_FREQ AT %MW0x4010 : LWORD;
    APU_DMC_FREQ_IRQ AT %MW0x4010.7 : BOOL; (* IRQ enable *)
    APU_DMC_FREQ_LOOP AT %MW0x4010.6 : BOOL; (* Loop flag *)
    APU_DMC_FREQ_FREQUENCY AT %MW0x4010.0 : BOOL; (* Frequency index *)
    APU_DMC_RAW AT %MW0x4011 : LWORD;
    APU_DMC_START AT %MW0x4012 : LWORD;
    APU_DMC_LEN AT %MW0x4013 : LWORD;
    APU_OAMDMA AT %MW0x4014 : LWORD;
    APU_APUSTATUS AT %MW0x4015 : LWORD;
    APU_APUSTATUS_DMCINTERRUPT AT %MW0x4015.7 : BOOL; (* DMC interrupt flag *)
    APU_APUSTATUS_FRAMEINTERRUPT AT %MW0x4015.6 : BOOL; (* Frame interrupt flag *)
    APU_APUSTATUS_DMCENABLED AT %MW0x4015.4 : BOOL; (* DMC enabled *)
    APU_APUSTATUS_NOISEENABLED AT %MW0x4015.3 : BOOL; (* Noise enabled *)
    APU_APUSTATUS_TRIANGLEENABLED AT %MW0x4015.2 : BOOL; (* Triangle enabled *)
    APU_APUSTATUS_SQUARE2ENABLED AT %MW0x4015.1 : BOOL; (* Square 2 enabled *)
    APU_APUSTATUS_SQUARE1ENABLED AT %MW0x4015.0 : BOOL; (* Square 1 enabled *)
    APU_APUFRAME AT %MW0x4017 : LWORD;
    APU_APUFRAME_MODE AT %MW0x4017.7 : BOOL; (* Frame counter mode *)
    APU_APUFRAME_IRQINHIBIT AT %MW0x4017.6 : BOOL; (* IRQ inhibit *)
    (* Controller Interface *)
    CONTROLLER_BASE AT %MW : WORD;
    CONTROLLER_JOY1 AT %MW0x4016 : LWORD;
    CONTROLLER_JOY1_A AT %MW0x4016.7 : BOOL; (* A button *)
    CONTROLLER_JOY1_B AT %MW0x4016.6 : BOOL; (* B button *)
    CONTROLLER_JOY1_SELECT AT %MW0x4016.5 : BOOL; (* Select button *)
    CONTROLLER_JOY1_START AT %MW0x4016.4 : BOOL; (* Start button *)
    CONTROLLER_JOY1_UP AT %MW0x4016.3 : BOOL; (* Up direction *)
    CONTROLLER_JOY1_DOWN AT %MW0x4016.2 : BOOL; (* Down direction *)
    CONTROLLER_JOY1_LEFT AT %MW0x4016.1 : BOOL; (* Left direction *)
    CONTROLLER_JOY1_RIGHT AT %MW0x4016.0 : BOOL; (* Right direction *)
    CONTROLLER_JOY2 AT %MW0x4017 : LWORD;
    (* Memory Mapper (Cartridge) *)
    MAPPER_BASE AT %MW : WORD;
    MAPPER_PRGROM AT %MW0 : ARRAY[0..-1] OF BOOL;
    MAPPER_CHRROM AT %MW0 : ARRAY[0..-1] OF BOOL;
    MAPPER_PRGRAM AT %MW0 : ARRAY[0..-1] OF BOOL;
    MAPPER_CHRRAM AT %MW0 : ARRAY[0..-1] OF BOOL;

    (* 中断变量 *)
    INT_NMI : BOOL; (* Non-maskable interrupt (VBlank) *)
    INT_RESET : BOOL; (* Reset vector *)
    INT_IRQ : BOOL; (* Interrupt request *)

END_VAR

BEGIN
    (* Nintendo Entertainment System初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_NMI|--[中断处理]--
    --|INT_RESET|--[中断处理]--
    --|INT_IRQ|--[中断处理]--

END_PROGRAM
