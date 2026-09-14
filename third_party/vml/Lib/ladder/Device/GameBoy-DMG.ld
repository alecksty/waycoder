(*
 * Sharp-LR35902梯形图定义
 * 生成自: Sharp/Z80/Sharp-LR35902
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
 * CPU架构: LR35902
 * 位宽: 8位
 * 时钟频率: 4194304 Hz
 *)

PROGRAM Sharp_LR35902
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    B AT %M0x01 : BYTE; (* B Register *)
    C AT %M0x02 : BYTE; (* C Register *)
    D AT %M0x03 : BYTE; (* D Register *)
    E AT %M0x04 : BYTE; (* E Register *)
    F AT %M0x05 : BYTE; (* Flags Register *)
    F_C AT %M0x05.4 : BOOL; (* Carry *)
    F_H AT %M0x05.5 : BOOL; (* Half Carry *)
    F_N AT %M0x05.6 : BOOL; (* Subtract *)
    F_Z AT %M0x05.7 : BOOL; (* Zero *)
    H AT %M0x06 : BYTE; (* H Register *)
    L AT %M0x07 : BYTE; (* L Register *)
    AF AT %M0x08 : WORD; (* AF Register Pair (Accumulator + Flags) *)
    BC AT %M0x0A : WORD; (* BC Register Pair *)
    DE AT %M0x0C : WORD; (* DE Register Pair *)
    HL AT %M0x0E : WORD; (* HL Register Pair *)
    SP AT %M0x10 : WORD; (* Stack Pointer *)
    PC AT %M0x12 : WORD; (* Program Counter *)

    (* 外设变量 *)
    (* LCD Controller / Picture Processing Unit *)
    PPU_BASE AT %MW0xFF40 : WORD;
    PPU_LCDC AT %MW0xFF40 : BYTE;
    PPU_LCDC_BG_ENABLE AT %MW0xFF40.0 : BOOL; (* Background Display Enable *)
    PPU_LCDC_SPRITE_ENABLE AT %MW0xFF40.1 : BOOL; (* Sprite Display Enable *)
    PPU_LCDC_SPRITE_SIZE AT %MW0xFF40.2 : BOOL; (* Sprite Size (0=8x8, 1=8x16) *)
    PPU_LCDC_BG_TILE_MAP AT %MW0xFF40.3 : BOOL; (* BG Tile Map Area (0=9800, 1=9C00) *)
    PPU_LCDC_TILE_DATA AT %MW0xFF40.4 : BOOL; (* Tile Data Area (0=8800, 1=8000) *)
    PPU_LCDC_WINDOW_ENABLE AT %MW0xFF40.5 : BOOL; (* Window Display Enable *)
    PPU_LCDC_WINDOW_MAP AT %MW0xFF40.6 : BOOL; (* Window Tile Map Area (0=9800, 1=9C00) *)
    PPU_LCDC_LCD_ENABLE AT %MW0xFF40.7 : BOOL; (* LCD Display Enable *)
    PPU_STAT AT %MW0xFF41 : BYTE;
    PPU_STAT_MODE AT %MW0xFF41.0 : BOOL; (* LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM) *)
    PPU_STAT_LYC_FLAG AT %MW0xFF41.2 : BOOL; (* LY=LYC Compare Flag *)
    PPU_STAT_HBLANK_IRQ AT %MW0xFF41.3 : BOOL; (* H-Blank Interrupt Enable *)
    PPU_STAT_VBLANK_IRQ AT %MW0xFF41.4 : BOOL; (* V-Blank Interrupt Enable *)
    PPU_STAT_OAM_IRQ AT %MW0xFF41.5 : BOOL; (* OAM Interrupt Enable *)
    PPU_STAT_LYC_IRQ AT %MW0xFF41.6 : BOOL; (* LYC Interrupt Enable *)
    PPU_SCY AT %MW0xFF42 : BYTE;
    PPU_SCX AT %MW0xFF43 : BYTE;
    PPU_LY AT %MW0xFF44 : BYTE;
    PPU_LYC AT %MW0xFF45 : BYTE;
    PPU_DMA AT %MW0xFF46 : BYTE;
    PPU_BGP AT %MW0xFF47 : BYTE;
    PPU_OBP0 AT %MW0xFF48 : BYTE;
    PPU_OBP1 AT %MW0xFF49 : BYTE;
    PPU_WY AT %MW0xFF4A : BYTE;
    PPU_WX AT %MW0xFF4B : BYTE;
    (* Audio Processing Unit *)
    APU_BASE AT %MW0xFF10 : WORD;
    APU_NR10 AT %MW0xFF10 : BYTE;
    APU_NR10_SWEEP_TIME AT %MW0xFF10.0 : BOOL; (* Sweep Time *)
    APU_NR10_SWEEP_INCREASE AT %MW0xFF10.3 : BOOL; (* Sweep Increase/Decrease *)
    APU_NR10_SWEEP_SHIFTS AT %MW0xFF10.0 : BOOL; (* Sweep Number of Shifts *)
    APU_NR11 AT %MW0xFF11 : BYTE;
    APU_NR12 AT %MW0xFF12 : BYTE;
    APU_NR13 AT %MW0xFF13 : BYTE;
    APU_NR14 AT %MW0xFF14 : BYTE;
    APU_NR21 AT %MW0xFF16 : BYTE;
    APU_NR22 AT %MW0xFF17 : BYTE;
    APU_NR23 AT %MW0xFF18 : BYTE;
    APU_NR24 AT %MW0xFF19 : BYTE;
    APU_NR30 AT %MW0xFF1A : BYTE;
    APU_NR31 AT %MW0xFF1B : BYTE;
    APU_NR32 AT %MW0xFF1C : BYTE;
    APU_NR33 AT %MW0xFF1D : BYTE;
    APU_NR34 AT %MW0xFF1E : BYTE;
    APU_NR41 AT %MW0xFF20 : BYTE;
    APU_NR42 AT %MW0xFF21 : BYTE;
    APU_NR43 AT %MW0xFF22 : BYTE;
    APU_NR44 AT %MW0xFF23 : BYTE;
    APU_NR50 AT %MW0xFF24 : BYTE;
    APU_NR51 AT %MW0xFF25 : BYTE;
    APU_NR52 AT %MW0xFF26 : BYTE;
    APU_NR52_CH1_ON AT %MW0xFF26.0 : BOOL; (* Channel 1 ON *)
    APU_NR52_CH2_ON AT %MW0xFF26.1 : BOOL; (* Channel 2 ON *)
    APU_NR52_CH3_ON AT %MW0xFF26.2 : BOOL; (* Channel 3 ON *)
    APU_NR52_CH4_ON AT %MW0xFF26.3 : BOOL; (* Channel 4 ON *)
    APU_NR52_ALL_ON AT %MW0xFF26.7 : BOOL; (* All Sound ON *)
    (* Timer Unit *)
    TIMER_BASE AT %MW0xFF04 : WORD;
    TIMER_DIV AT %MW0xFF04 : BYTE;
    TIMER_TIMA AT %MW0xFF05 : BYTE;
    TIMER_TMA AT %MW0xFF06 : BYTE;
    TIMER_TAC AT %MW0xFF07 : BYTE;
    TIMER_TAC_TIMER_ENABLE AT %MW0xFF07.2 : BOOL; (* Timer Enable *)
    TIMER_TAC_CLOCK_SEL AT %MW0xFF07.0 : BOOL; (* Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz) *)
    (* Joypad Controller *)
    JOYPAD_BASE AT %MW0xFF00 : WORD;
    JOYPAD_P1 AT %MW0xFF00 : BYTE;
    JOYPAD_P1_A_BTN AT %MW0xFF00.0 : BOOL; (* A Button (1=Pressed when selected) *)
    JOYPAD_P1_B_BTN AT %MW0xFF00.1 : BOOL; (* B Button (1=Pressed when selected) *)
    JOYPAD_P1_SELECT AT %MW0xFF00.2 : BOOL; (* Select Button (1=Pressed) *)
    JOYPAD_P1_START AT %MW0xFF00.3 : BOOL; (* Start Button (1=Pressed) *)
    JOYPAD_P1_DIR_DOWN AT %MW0xFF00.4 : BOOL; (* Direction Down (1=Pressed when selected) *)
    JOYPAD_P1_DIR_UP AT %MW0xFF00.5 : BOOL; (* Direction Up (1=Pressed when selected) *)
    JOYPAD_P1_DIR_LEFT AT %MW0xFF00.6 : BOOL; (* Direction Left (1=Pressed when selected) *)
    JOYPAD_P1_DIR_RIGHT AT %MW0xFF00.7 : BOOL; (* Direction Right (1=Pressed when selected) *)
    (* Serial I/O (Link Cable) *)
    SERIAL_BASE AT %MW0xFF01 : WORD;
    SERIAL_SB AT %MW0xFF01 : BYTE;
    SERIAL_SC AT %MW0xFF02 : BYTE;
    SERIAL_SC_TRANSFER_START AT %MW0xFF02.7 : BOOL; (* Transfer Start *)
    SERIAL_SC_CLOCK_SPEED AT %MW0xFF02.1 : BOOL; (* Clock Select (0=External, 1=Internal 8192Hz) *)
    (* Interrupt Flag Register *)
    INTERRUPT_BASE AT %MW0xFF0F : WORD;
    INTERRUPT_IF AT %MW0xFF0F : BYTE;
    INTERRUPT_IF_VBLANK AT %MW0xFF0F.0 : BOOL; (* V-Blank Interrupt Request *)
    INTERRUPT_IF_LCDC AT %MW0xFF0F.1 : BOOL; (* LCDC Status Interrupt Request *)
    INTERRUPT_IF_TIMER AT %MW0xFF0F.2 : BOOL; (* Timer Overflow Interrupt Request *)
    INTERRUPT_IF_SERIAL AT %MW0xFF0F.3 : BOOL; (* Serial Transfer Complete Interrupt Request *)
    INTERRUPT_IF_JOYPAD AT %MW0xFF0F.4 : BOOL; (* Joypad Interrupt Request *)
    (* Interrupt Enable Register *)
    IE_BASE AT %MW0xFFFF : WORD;
    IE_IE AT %MW0xFFFF : BYTE;
    IE_IE_VBLANK_IE AT %MW0xFFFF.0 : BOOL; (* V-Blank Interrupt Enable *)
    IE_IE_LCDC_IE AT %MW0xFFFF.1 : BOOL; (* LCDC Status Interrupt Enable *)
    IE_IE_TIMER_IE AT %MW0xFFFF.2 : BOOL; (* Timer Interrupt Enable *)
    IE_IE_SERIAL_IE AT %MW0xFFFF.3 : BOOL; (* Serial Interrupt Enable *)
    IE_IE_JOYPAD_IE AT %MW0xFFFF.4 : BOOL; (* Joypad Interrupt Enable *)

    (* 中断变量 *)
    INT_VBLANK : BOOL; (* V-Blank Interrupt (LY=144, during vertical blanking) *)
    INT_LCDC_STATUS : BOOL; (* LCDC Status Interrupt (H-Blank/OAM/V-Count match) *)
    INT_TIMER_OVERFLOW : BOOL; (* Timer Overflow Interrupt (TIMA overflow) *)
    INT_SERIAL_COMPLETE : BOOL; (* Serial Transfer Complete Interrupt *)
    INT_JOYPAD : BOOL; (* Joypad Interrupt (button press/release) *)

END_VAR

BEGIN
    (* Sharp-LR35902初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|A>0|--(监控灯)--
    --|B>0|--(监控灯)--
    --|C>0|--(监控灯)--
    --|D>0|--(监控灯)--
    --|E>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_VBLANK|--[中断处理]--
    --|INT_LCDC_STATUS|--[中断处理]--
    --|INT_TIMER_OVERFLOW|--[中断处理]--
    --|INT_SERIAL_COMPLETE|--[中断处理]--
    --|INT_JOYPAD|--[中断处理]--

END_PROGRAM
