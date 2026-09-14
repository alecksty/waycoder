using System;

namespace VML.Device.Sharp.Sharp_LR35902
{
    /// <summary>
    /// Sharp-LR35902 寄存器定义
    /// 生成自: Sharp/Z80/Sharp-LR35902
    /// 版本: 1.0
    /// </summary>
    public static class Sharp_LR35902
    {
        // CPU架构: LR35902, 8位, 4194304 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // B Register
        public const int B_ADDR = 0x01;
        public static unsafe byte* B => (byte*)0x01;

        // C Register
        public const int C_ADDR = 0x02;
        public static unsafe byte* C => (byte*)0x02;

        // D Register
        public const int D_ADDR = 0x03;
        public static unsafe byte* D => (byte*)0x03;

        // E Register
        public const int E_ADDR = 0x04;
        public static unsafe byte* E => (byte*)0x04;

        // Flags Register
        public const int F_ADDR = 0x05;
        public static unsafe byte* F => (byte*)0x05;
        public const int F_C = 4;  // Carry
        public const int F_H = 5;  // Half Carry
        public const int F_N = 6;  // Subtract
        public const int F_Z = 7;  // Zero

        // H Register
        public const int H_ADDR = 0x06;
        public static unsafe byte* H => (byte*)0x06;

        // L Register
        public const int L_ADDR = 0x07;
        public static unsafe byte* L => (byte*)0x07;

        // AF Register Pair (Accumulator + Flags)
        public const int AF_ADDR = 0x08;
        public static unsafe ushort* AF => (ushort*)0x08;

        // BC Register Pair
        public const int BC_ADDR = 0x0A;
        public static unsafe ushort* BC => (ushort*)0x0A;

        // DE Register Pair
        public const int DE_ADDR = 0x0C;
        public static unsafe ushort* DE => (ushort*)0x0C;

        // HL Register Pair
        public const int HL_ADDR = 0x0E;
        public static unsafe ushort* HL => (ushort*)0x0E;

        // Stack Pointer
        public const int SP_ADDR = 0x10;
        public static unsafe ushort* SP => (ushort*)0x10;

        // Program Counter
        public const int PC_ADDR = 0x12;
        public static unsafe ushort* PC => (ushort*)0x12;

        // 内存段定义
        // Work RAM (4KB)
        public const int WRAM_START = 0xC000;
        public const int WRAM_END = 0xCFFF;
        public const int WRAM_SIZE = 4096;

        // Work RAM Shadow (Echo RAM)
        public const int WRAM_SHADOW_START = 0xE000;
        public const int WRAM_SHADOW_END = 0xEFFF;
        public const int WRAM_SHADOW_SIZE = 4096;

        // High RAM (127 bytes)
        public const int HRAM_START = 0xFF80;
        public const int HRAM_END = 0xFFFE;
        public const int HRAM_SIZE = 127;

        // I/O Registers
        public const int IO_REGISTERS_START = 0xFF00;
        public const int IO_REGISTERS_END = 0xFF7F;
        public const int IO_REGISTERS_SIZE = 128;

        // Sprite Attribute Table (OAM)
        public const int OAM_START = 0xFE00;
        public const int OAM_END = 0xFE9F;
        public const int OAM_SIZE = 160;

        // Video RAM (8KB)
        public const int VRAM_START = 0x8000;
        public const int VRAM_END = 0x9FFF;
        public const int VRAM_SIZE = 8192;

        // Background Map 1
        public const int BG_MAP_1_START = 0x9800;
        public const int BG_MAP_1_END = 0x9BFF;
        public const int BG_MAP_1_SIZE = 1024;

        // Background Map 2
        public const int BG_MAP_2_START = 0x9C00;
        public const int BG_MAP_2_END = 0x9FFF;
        public const int BG_MAP_2_SIZE = 1024;

        // ROM Bank 0 (Cartridge Header)
        public const int ROM_BANK0_START = 0x0000;
        public const int ROM_BANK0_END = 0x3FFF;
        public const int ROM_BANK0_SIZE = 16384;

        // ROM Bank 1 (Switchable)
        public const int ROM_BANK1_START = 0x4000;
        public const int ROM_BANK1_END = 0x7FFF;
        public const int ROM_BANK1_SIZE = 16384;

        // Cartridge RAM / MBC
        public const int CART_RAM_START = 0xA000;
        public const int CART_RAM_END = 0xBFFF;
        public const int CART_RAM_SIZE = 8192;

        // 外设定义
        // LCD Controller / Picture Processing Unit
        public const int PPU_BASE = 0xFF40;
        public static unsafe byte* PPU_LCDC => (byte*)0x0001FE80;
        public const int PPU_LCDC_BG_ENABLE = 0;  // Background Display Enable
        public const int PPU_LCDC_SPRITE_ENABLE = 1;  // Sprite Display Enable
        public const int PPU_LCDC_SPRITE_SIZE = 2;  // Sprite Size (0=8x8, 1=8x16)
        public const int PPU_LCDC_BG_TILE_MAP = 3;  // BG Tile Map Area (0=9800, 1=9C00)
        public const int PPU_LCDC_TILE_DATA = 4;  // Tile Data Area (0=8800, 1=8000)
        public const int PPU_LCDC_WINDOW_ENABLE = 5;  // Window Display Enable
        public const int PPU_LCDC_WINDOW_MAP = 6;  // Window Tile Map Area (0=9800, 1=9C00)
        public const int PPU_LCDC_LCD_ENABLE = 7;  // LCD Display Enable
        public static unsafe byte* PPU_STAT => (byte*)0x0001FE81;
        public const int PPU_STAT_MODE = 0;  // LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
        public const int PPU_STAT_LYC_FLAG = 2;  // LY=LYC Compare Flag
        public const int PPU_STAT_HBLANK_IRQ = 3;  // H-Blank Interrupt Enable
        public const int PPU_STAT_VBLANK_IRQ = 4;  // V-Blank Interrupt Enable
        public const int PPU_STAT_OAM_IRQ = 5;  // OAM Interrupt Enable
        public const int PPU_STAT_LYC_IRQ = 6;  // LYC Interrupt Enable
        public static unsafe byte* PPU_SCY => (byte*)0x0001FE82;
        public static unsafe byte* PPU_SCX => (byte*)0x0001FE83;
        public static unsafe byte* PPU_LY => (byte*)0x0001FE84;
        public static unsafe byte* PPU_LYC => (byte*)0x0001FE85;
        public static unsafe byte* PPU_DMA => (byte*)0x0001FE86;
        public static unsafe byte* PPU_BGP => (byte*)0x0001FE87;
        public static unsafe byte* PPU_OBP0 => (byte*)0x0001FE88;
        public static unsafe byte* PPU_OBP1 => (byte*)0x0001FE89;
        public static unsafe byte* PPU_WY => (byte*)0x0001FE8A;
        public static unsafe byte* PPU_WX => (byte*)0x0001FE8B;

        // Audio Processing Unit
        public const int APU_BASE = 0xFF10;
        public static unsafe byte* APU_NR10 => (byte*)0x0001FE20;
        public const int APU_NR10_SWEEP_TIME = 0;  // Sweep Time
        public const int APU_NR10_SWEEP_INCREASE = 3;  // Sweep Increase/Decrease
        public const int APU_NR10_SWEEP_SHIFTS = 0;  // Sweep Number of Shifts
        public static unsafe byte* APU_NR11 => (byte*)0x0001FE21;
        public static unsafe byte* APU_NR12 => (byte*)0x0001FE22;
        public static unsafe byte* APU_NR13 => (byte*)0x0001FE23;
        public static unsafe byte* APU_NR14 => (byte*)0x0001FE24;
        public static unsafe byte* APU_NR21 => (byte*)0x0001FE26;
        public static unsafe byte* APU_NR22 => (byte*)0x0001FE27;
        public static unsafe byte* APU_NR23 => (byte*)0x0001FE28;
        public static unsafe byte* APU_NR24 => (byte*)0x0001FE29;
        public static unsafe byte* APU_NR30 => (byte*)0x0001FE2A;
        public static unsafe byte* APU_NR31 => (byte*)0x0001FE2B;
        public static unsafe byte* APU_NR32 => (byte*)0x0001FE2C;
        public static unsafe byte* APU_NR33 => (byte*)0x0001FE2D;
        public static unsafe byte* APU_NR34 => (byte*)0x0001FE2E;
        public static unsafe byte* APU_NR41 => (byte*)0x0001FE30;
        public static unsafe byte* APU_NR42 => (byte*)0x0001FE31;
        public static unsafe byte* APU_NR43 => (byte*)0x0001FE32;
        public static unsafe byte* APU_NR44 => (byte*)0x0001FE33;
        public static unsafe byte* APU_NR50 => (byte*)0x0001FE34;
        public static unsafe byte* APU_NR51 => (byte*)0x0001FE35;
        public static unsafe byte* APU_NR52 => (byte*)0x0001FE36;
        public const int APU_NR52_CH1_ON = 0;  // Channel 1 ON
        public const int APU_NR52_CH2_ON = 1;  // Channel 2 ON
        public const int APU_NR52_CH3_ON = 2;  // Channel 3 ON
        public const int APU_NR52_CH4_ON = 3;  // Channel 4 ON
        public const int APU_NR52_ALL_ON = 7;  // All Sound ON

        // Timer Unit
        public const int TIMER_BASE = 0xFF04;
        public static unsafe byte* TIMER_DIV => (byte*)0x0001FE08;
        public static unsafe byte* TIMER_TIMA => (byte*)0x0001FE09;
        public static unsafe byte* TIMER_TMA => (byte*)0x0001FE0A;
        public static unsafe byte* TIMER_TAC => (byte*)0x0001FE0B;
        public const int TIMER_TAC_TIMER_ENABLE = 2;  // Timer Enable
        public const int TIMER_TAC_CLOCK_SEL = 0;  // Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)

        // Joypad Controller
        public const int JOYPAD_BASE = 0xFF00;
        public static unsafe byte* JOYPAD_P1 => (byte*)0x0001FE00;
        public const int JOYPAD_P1_A_BTN = 0;  // A Button (1=Pressed when selected)
        public const int JOYPAD_P1_B_BTN = 1;  // B Button (1=Pressed when selected)
        public const int JOYPAD_P1_SELECT = 2;  // Select Button (1=Pressed)
        public const int JOYPAD_P1_START = 3;  // Start Button (1=Pressed)
        public const int JOYPAD_P1_DIR_DOWN = 4;  // Direction Down (1=Pressed when selected)
        public const int JOYPAD_P1_DIR_UP = 5;  // Direction Up (1=Pressed when selected)
        public const int JOYPAD_P1_DIR_LEFT = 6;  // Direction Left (1=Pressed when selected)
        public const int JOYPAD_P1_DIR_RIGHT = 7;  // Direction Right (1=Pressed when selected)

        // Serial I/O (Link Cable)
        public const int SERIAL_BASE = 0xFF01;
        public static unsafe byte* SERIAL_SB => (byte*)0x0001FE02;
        public static unsafe byte* SERIAL_SC => (byte*)0x0001FE03;
        public const int SERIAL_SC_TRANSFER_START = 7;  // Transfer Start
        public const int SERIAL_SC_CLOCK_SPEED = 1;  // Clock Select (0=External, 1=Internal 8192Hz)

        // Interrupt Flag Register
        public const int INTERRUPT_BASE = 0xFF0F;
        public static unsafe byte* INTERRUPT_IF => (byte*)0x0001FE1E;
        public const int INTERRUPT_IF_VBLANK = 0;  // V-Blank Interrupt Request
        public const int INTERRUPT_IF_LCDC = 1;  // LCDC Status Interrupt Request
        public const int INTERRUPT_IF_TIMER = 2;  // Timer Overflow Interrupt Request
        public const int INTERRUPT_IF_SERIAL = 3;  // Serial Transfer Complete Interrupt Request
        public const int INTERRUPT_IF_JOYPAD = 4;  // Joypad Interrupt Request

        // Interrupt Enable Register
        public const int IE_BASE = 0xFFFF;
        public static unsafe byte* IE_IE => (byte*)0x0001FFFE;
        public const int IE_IE_VBLANK_IE = 0;  // V-Blank Interrupt Enable
        public const int IE_IE_LCDC_IE = 1;  // LCDC Status Interrupt Enable
        public const int IE_IE_TIMER_IE = 2;  // Timer Interrupt Enable
        public const int IE_IE_SERIAL_IE = 3;  // Serial Interrupt Enable
        public const int IE_IE_JOYPAD_IE = 4;  // Joypad Interrupt Enable

        // 中断向量定义
        public const int IRQ_VBLANK = 0;  // V-Blank Interrupt (LY=144, during vertical blanking)
        public const int IRQ_LCDC_STATUS = 1;  // LCDC Status Interrupt (H-Blank/OAM/V-Count match)
        public const int IRQ_TIMER_OVERFLOW = 2;  // Timer Overflow Interrupt (TIMA overflow)
        public const int IRQ_SERIAL_COMPLETE = 3;  // Serial Transfer Complete Interrupt
        public const int IRQ_JOYPAD = 4;  // Joypad Interrupt (button press/release)

        // 引脚定义
        public const int PIN_VSS = 1;  // Ground
        public const int PIN_VDD = 2;  // Power Supply
        public const int PIN_PHI = 3;  // System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
        public const int PIN_RESET = 4;  // Reset Signal (active low)
        public const int PIN_INT = 5;  // Interrupt Request
        public const int PIN_BUSREQ = 6;  // Bus Request (external DMA access)
        public const int PIN_A0 = 7;  // Address Bus Bit 0
        public const int PIN_A1 = 8;  // Address Bus Bit 1
        public const int PIN_A2 = 9;  // Address Bus Bit 2
        public const int PIN_A3 = 10;  // Address Bus Bit 3
        public const int PIN_A4 = 11;  // Address Bus Bit 4
        public const int PIN_A5 = 12;  // Address Bus Bit 5
        public const int PIN_A6 = 13;  // Address Bus Bit 6
        public const int PIN_A7 = 14;  // Address Bus Bit 7
        public const int PIN_A8 = 15;  // Address Bus Bit 8
        public const int PIN_A9 = 16;  // Address Bus Bit 9
        public const int PIN_A10 = 17;  // Address Bus Bit 10
        public const int PIN_A11 = 18;  // Address Bus Bit 11
        public const int PIN_A12 = 19;  // Address Bus Bit 12
        public const int PIN_A13 = 20;  // Address Bus Bit 13
        public const int PIN_A14 = 21;  // Address Bus Bit 14
        public const int PIN_A15 = 22;  // Address Bus Bit 15
        public const int PIN_D0 = 23;  // Data Bus Bit 0
        public const int PIN_D1 = 24;  // Data Bus Bit 1
        public const int PIN_D2 = 25;  // Data Bus Bit 2
        public const int PIN_D3 = 26;  // Data Bus Bit 3
        public const int PIN_D4 = 27;  // Data Bus Bit 4
        public const int PIN_D5 = 28;  // Data Bus Bit 5
        public const int PIN_D6 = 29;  // Data Bus Bit 6
        public const int PIN_D7 = 30;  // Data Bus Bit 7
        public const int PIN_RD = 31;  // Read Strobe (active low)
        public const int PIN_WR = 32;  // Write Strobe (active low)
        public const int PIN_CS = 33;  // Chip Select (active low)
        public const int PIN_SOUND_OUT = 34;  // Audio Output
        public const int PIN_LCD_DATA0 = 35;  // LCD Data Bus Bit 0
        public const int PIN_LCD_DATA1 = 36;  // LCD Data Bus Bit 1
        public const int PIN_LCD_DATA2 = 37;  // LCD Data Bus Bit 2
        public const int PIN_LCD_DATA3 = 38;  // LCD Data Bus Bit 3
        public const int PIN_LCD_DATA4 = 39;  // LCD Data Bus Bit 4
        public const int PIN_LCD_DATA5 = 40;  // LCD Data Bus Bit 5
        public const int PIN_LCD_DATA6 = 41;  // LCD Data Bus Bit 6
        public const int PIN_LCD_DATA7 = 42;  // LCD Data Bus Bit 7
        public const int PIN_IR = 43;  // Infrared Port (DMG-CGB-01)

        public static void sharp_lr35902_init()
        {
            // 硬件初始化代码
        }
    }
}
