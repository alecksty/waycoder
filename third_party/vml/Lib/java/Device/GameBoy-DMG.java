package vml.device.sharp.sharp_lr35902;

/**
 * Sharp-LR35902 寄存器定义
 * 生成自: Sharp/Z80/Sharp-LR35902
 * 版本: 1.0
 */
public final class Sharp_LR35902 {
    private Sharp_LR35902() {} // 工具类
    // CPU架构: LR35902, 8位, 4194304 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // B Register
    public static final int B_ADDR = (int)0x01;

    // C Register
    public static final int C_ADDR = (int)0x02;

    // D Register
    public static final int D_ADDR = (int)0x03;

    // E Register
    public static final int E_ADDR = (int)0x04;

    // Flags Register
    public static final int F_ADDR = (int)0x05;
    public static final int F_C = 4;  // Carry
    public static final int F_H = 5;  // Half Carry
    public static final int F_N = 6;  // Subtract
    public static final int F_Z = 7;  // Zero

    // H Register
    public static final int H_ADDR = (int)0x06;

    // L Register
    public static final int L_ADDR = (int)0x07;

    // AF Register Pair (Accumulator + Flags)
    public static final int AF_ADDR = (int)0x08;

    // BC Register Pair
    public static final int BC_ADDR = (int)0x0A;

    // DE Register Pair
    public static final int DE_ADDR = (int)0x0C;

    // HL Register Pair
    public static final int HL_ADDR = (int)0x0E;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x10;

    // Program Counter
    public static final int PC_ADDR = (int)0x12;

    // 内存段定义
    // Work RAM (4KB)
    public static final int WRAM_START = (int)0xC000;
    public static final int WRAM_END = (int)0xCFFF;
    public static final int WRAM_SIZE = 4096;

    // Work RAM Shadow (Echo RAM)
    public static final int WRAM_SHADOW_START = (int)0xE000;
    public static final int WRAM_SHADOW_END = (int)0xEFFF;
    public static final int WRAM_SHADOW_SIZE = 4096;

    // High RAM (127 bytes)
    public static final int HRAM_START = (int)0xFF80;
    public static final int HRAM_END = (int)0xFFFE;
    public static final int HRAM_SIZE = 127;

    // I/O Registers
    public static final int IO_REGISTERS_START = (int)0xFF00;
    public static final int IO_REGISTERS_END = (int)0xFF7F;
    public static final int IO_REGISTERS_SIZE = 128;

    // Sprite Attribute Table (OAM)
    public static final int OAM_START = (int)0xFE00;
    public static final int OAM_END = (int)0xFE9F;
    public static final int OAM_SIZE = 160;

    // Video RAM (8KB)
    public static final int VRAM_START = (int)0x8000;
    public static final int VRAM_END = (int)0x9FFF;
    public static final int VRAM_SIZE = 8192;

    // Background Map 1
    public static final int BG_MAP_1_START = (int)0x9800;
    public static final int BG_MAP_1_END = (int)0x9BFF;
    public static final int BG_MAP_1_SIZE = 1024;

    // Background Map 2
    public static final int BG_MAP_2_START = (int)0x9C00;
    public static final int BG_MAP_2_END = (int)0x9FFF;
    public static final int BG_MAP_2_SIZE = 1024;

    // ROM Bank 0 (Cartridge Header)
    public static final int ROM_BANK0_START = (int)0x0000;
    public static final int ROM_BANK0_END = (int)0x3FFF;
    public static final int ROM_BANK0_SIZE = 16384;

    // ROM Bank 1 (Switchable)
    public static final int ROM_BANK1_START = (int)0x4000;
    public static final int ROM_BANK1_END = (int)0x7FFF;
    public static final int ROM_BANK1_SIZE = 16384;

    // Cartridge RAM / MBC
    public static final int CART_RAM_START = (int)0xA000;
    public static final int CART_RAM_END = (int)0xBFFF;
    public static final int CART_RAM_SIZE = 8192;

    // 外设定义
    // LCD Controller / Picture Processing Unit
    public static final int PPU_BASE = (int)0xFF40;
    public static final int PPU_LCDC = (int)0x0001FE80;
    public static final int PPU_LCDC_BG_ENABLE = 0;  // Background Display Enable
    public static final int PPU_LCDC_SPRITE_ENABLE = 1;  // Sprite Display Enable
    public static final int PPU_LCDC_SPRITE_SIZE = 2;  // Sprite Size (0=8x8, 1=8x16)
    public static final int PPU_LCDC_BG_TILE_MAP = 3;  // BG Tile Map Area (0=9800, 1=9C00)
    public static final int PPU_LCDC_TILE_DATA = 4;  // Tile Data Area (0=8800, 1=8000)
    public static final int PPU_LCDC_WINDOW_ENABLE = 5;  // Window Display Enable
    public static final int PPU_LCDC_WINDOW_MAP = 6;  // Window Tile Map Area (0=9800, 1=9C00)
    public static final int PPU_LCDC_LCD_ENABLE = 7;  // LCD Display Enable
    public static final int PPU_STAT = (int)0x0001FE81;
    public static final int PPU_STAT_MODE = 0;  // LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
    public static final int PPU_STAT_LYC_FLAG = 2;  // LY=LYC Compare Flag
    public static final int PPU_STAT_HBLANK_IRQ = 3;  // H-Blank Interrupt Enable
    public static final int PPU_STAT_VBLANK_IRQ = 4;  // V-Blank Interrupt Enable
    public static final int PPU_STAT_OAM_IRQ = 5;  // OAM Interrupt Enable
    public static final int PPU_STAT_LYC_IRQ = 6;  // LYC Interrupt Enable
    public static final int PPU_SCY = (int)0x0001FE82;
    public static final int PPU_SCX = (int)0x0001FE83;
    public static final int PPU_LY = (int)0x0001FE84;
    public static final int PPU_LYC = (int)0x0001FE85;
    public static final int PPU_DMA = (int)0x0001FE86;
    public static final int PPU_BGP = (int)0x0001FE87;
    public static final int PPU_OBP0 = (int)0x0001FE88;
    public static final int PPU_OBP1 = (int)0x0001FE89;
    public static final int PPU_WY = (int)0x0001FE8A;
    public static final int PPU_WX = (int)0x0001FE8B;

    // Audio Processing Unit
    public static final int APU_BASE = (int)0xFF10;
    public static final int APU_NR10 = (int)0x0001FE20;
    public static final int APU_NR10_SWEEP_TIME = 0;  // Sweep Time
    public static final int APU_NR10_SWEEP_INCREASE = 3;  // Sweep Increase/Decrease
    public static final int APU_NR10_SWEEP_SHIFTS = 0;  // Sweep Number of Shifts
    public static final int APU_NR11 = (int)0x0001FE21;
    public static final int APU_NR12 = (int)0x0001FE22;
    public static final int APU_NR13 = (int)0x0001FE23;
    public static final int APU_NR14 = (int)0x0001FE24;
    public static final int APU_NR21 = (int)0x0001FE26;
    public static final int APU_NR22 = (int)0x0001FE27;
    public static final int APU_NR23 = (int)0x0001FE28;
    public static final int APU_NR24 = (int)0x0001FE29;
    public static final int APU_NR30 = (int)0x0001FE2A;
    public static final int APU_NR31 = (int)0x0001FE2B;
    public static final int APU_NR32 = (int)0x0001FE2C;
    public static final int APU_NR33 = (int)0x0001FE2D;
    public static final int APU_NR34 = (int)0x0001FE2E;
    public static final int APU_NR41 = (int)0x0001FE30;
    public static final int APU_NR42 = (int)0x0001FE31;
    public static final int APU_NR43 = (int)0x0001FE32;
    public static final int APU_NR44 = (int)0x0001FE33;
    public static final int APU_NR50 = (int)0x0001FE34;
    public static final int APU_NR51 = (int)0x0001FE35;
    public static final int APU_NR52 = (int)0x0001FE36;
    public static final int APU_NR52_CH1_ON = 0;  // Channel 1 ON
    public static final int APU_NR52_CH2_ON = 1;  // Channel 2 ON
    public static final int APU_NR52_CH3_ON = 2;  // Channel 3 ON
    public static final int APU_NR52_CH4_ON = 3;  // Channel 4 ON
    public static final int APU_NR52_ALL_ON = 7;  // All Sound ON

    // Timer Unit
    public static final int TIMER_BASE = (int)0xFF04;
    public static final int TIMER_DIV = (int)0x0001FE08;
    public static final int TIMER_TIMA = (int)0x0001FE09;
    public static final int TIMER_TMA = (int)0x0001FE0A;
    public static final int TIMER_TAC = (int)0x0001FE0B;
    public static final int TIMER_TAC_TIMER_ENABLE = 2;  // Timer Enable
    public static final int TIMER_TAC_CLOCK_SEL = 0;  // Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)

    // Joypad Controller
    public static final int JOYPAD_BASE = (int)0xFF00;
    public static final int JOYPAD_P1 = (int)0x0001FE00;
    public static final int JOYPAD_P1_A_BTN = 0;  // A Button (1=Pressed when selected)
    public static final int JOYPAD_P1_B_BTN = 1;  // B Button (1=Pressed when selected)
    public static final int JOYPAD_P1_SELECT = 2;  // Select Button (1=Pressed)
    public static final int JOYPAD_P1_START = 3;  // Start Button (1=Pressed)
    public static final int JOYPAD_P1_DIR_DOWN = 4;  // Direction Down (1=Pressed when selected)
    public static final int JOYPAD_P1_DIR_UP = 5;  // Direction Up (1=Pressed when selected)
    public static final int JOYPAD_P1_DIR_LEFT = 6;  // Direction Left (1=Pressed when selected)
    public static final int JOYPAD_P1_DIR_RIGHT = 7;  // Direction Right (1=Pressed when selected)

    // Serial I/O (Link Cable)
    public static final int SERIAL_BASE = (int)0xFF01;
    public static final int SERIAL_SB = (int)0x0001FE02;
    public static final int SERIAL_SC = (int)0x0001FE03;
    public static final int SERIAL_SC_TRANSFER_START = 7;  // Transfer Start
    public static final int SERIAL_SC_CLOCK_SPEED = 1;  // Clock Select (0=External, 1=Internal 8192Hz)

    // Interrupt Flag Register
    public static final int INTERRUPT_BASE = (int)0xFF0F;
    public static final int INTERRUPT_IF = (int)0x0001FE1E;
    public static final int INTERRUPT_IF_VBLANK = 0;  // V-Blank Interrupt Request
    public static final int INTERRUPT_IF_LCDC = 1;  // LCDC Status Interrupt Request
    public static final int INTERRUPT_IF_TIMER = 2;  // Timer Overflow Interrupt Request
    public static final int INTERRUPT_IF_SERIAL = 3;  // Serial Transfer Complete Interrupt Request
    public static final int INTERRUPT_IF_JOYPAD = 4;  // Joypad Interrupt Request

    // Interrupt Enable Register
    public static final int IE_BASE = (int)0xFFFF;
    public static final int IE_IE = (int)0x0001FFFE;
    public static final int IE_IE_VBLANK_IE = 0;  // V-Blank Interrupt Enable
    public static final int IE_IE_LCDC_IE = 1;  // LCDC Status Interrupt Enable
    public static final int IE_IE_TIMER_IE = 2;  // Timer Interrupt Enable
    public static final int IE_IE_SERIAL_IE = 3;  // Serial Interrupt Enable
    public static final int IE_IE_JOYPAD_IE = 4;  // Joypad Interrupt Enable

    // 中断向量定义
    public static final int IRQ_VBLANK = 0;  // V-Blank Interrupt (LY=144, during vertical blanking)
    public static final int IRQ_LCDC_STATUS = 1;  // LCDC Status Interrupt (H-Blank/OAM/V-Count match)
    public static final int IRQ_TIMER_OVERFLOW = 2;  // Timer Overflow Interrupt (TIMA overflow)
    public static final int IRQ_SERIAL_COMPLETE = 3;  // Serial Transfer Complete Interrupt
    public static final int IRQ_JOYPAD = 4;  // Joypad Interrupt (button press/release)

    // 引脚定义
    public static final int PIN_VSS = 1;  // Ground
    public static final int PIN_VDD = 2;  // Power Supply
    public static final int PIN_PHI = 3;  // System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
    public static final int PIN_RESET = 4;  // Reset Signal (active low)
    public static final int PIN_INT = 5;  // Interrupt Request
    public static final int PIN_BUSREQ = 6;  // Bus Request (external DMA access)
    public static final int PIN_A0 = 7;  // Address Bus Bit 0
    public static final int PIN_A1 = 8;  // Address Bus Bit 1
    public static final int PIN_A2 = 9;  // Address Bus Bit 2
    public static final int PIN_A3 = 10;  // Address Bus Bit 3
    public static final int PIN_A4 = 11;  // Address Bus Bit 4
    public static final int PIN_A5 = 12;  // Address Bus Bit 5
    public static final int PIN_A6 = 13;  // Address Bus Bit 6
    public static final int PIN_A7 = 14;  // Address Bus Bit 7
    public static final int PIN_A8 = 15;  // Address Bus Bit 8
    public static final int PIN_A9 = 16;  // Address Bus Bit 9
    public static final int PIN_A10 = 17;  // Address Bus Bit 10
    public static final int PIN_A11 = 18;  // Address Bus Bit 11
    public static final int PIN_A12 = 19;  // Address Bus Bit 12
    public static final int PIN_A13 = 20;  // Address Bus Bit 13
    public static final int PIN_A14 = 21;  // Address Bus Bit 14
    public static final int PIN_A15 = 22;  // Address Bus Bit 15
    public static final int PIN_D0 = 23;  // Data Bus Bit 0
    public static final int PIN_D1 = 24;  // Data Bus Bit 1
    public static final int PIN_D2 = 25;  // Data Bus Bit 2
    public static final int PIN_D3 = 26;  // Data Bus Bit 3
    public static final int PIN_D4 = 27;  // Data Bus Bit 4
    public static final int PIN_D5 = 28;  // Data Bus Bit 5
    public static final int PIN_D6 = 29;  // Data Bus Bit 6
    public static final int PIN_D7 = 30;  // Data Bus Bit 7
    public static final int PIN_RD = 31;  // Read Strobe (active low)
    public static final int PIN_WR = 32;  // Write Strobe (active low)
    public static final int PIN_CS = 33;  // Chip Select (active low)
    public static final int PIN_SOUND_OUT = 34;  // Audio Output
    public static final int PIN_LCD_DATA0 = 35;  // LCD Data Bus Bit 0
    public static final int PIN_LCD_DATA1 = 36;  // LCD Data Bus Bit 1
    public static final int PIN_LCD_DATA2 = 37;  // LCD Data Bus Bit 2
    public static final int PIN_LCD_DATA3 = 38;  // LCD Data Bus Bit 3
    public static final int PIN_LCD_DATA4 = 39;  // LCD Data Bus Bit 4
    public static final int PIN_LCD_DATA5 = 40;  // LCD Data Bus Bit 5
    public static final int PIN_LCD_DATA6 = 41;  // LCD Data Bus Bit 6
    public static final int PIN_LCD_DATA7 = 42;  // LCD Data Bus Bit 7
    public static final int PIN_IR = 43;  // Infrared Port (DMG-CGB-01)

    public static native void sharp_lr35902_init();
}
