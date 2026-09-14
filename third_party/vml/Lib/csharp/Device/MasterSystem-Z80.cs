using System;

namespace VML.Device.Zilog.Zilog_Z80
{
    /// <summary>
    /// Zilog-Z80 寄存器定义
    /// 生成自: Zilog/Z80/Zilog-Z80
    /// 版本: 1.0
    /// </summary>
    public static class Zilog_Z80
    {
        // CPU架构: Z80, 8位, 3580000 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // Flags Register
        public const int F_ADDR = 0x01;
        public static unsafe byte* F => (byte*)0x01;
        public const int F_C = 0;  // Carry
        public const int F_N = 1;  // Subtract
        public const int F_P = 2;  // Parity/Overflow
        public const int F_H = 4;  // Half Carry
        public const int F_Z = 6;  // Zero
        public const int F_S = 7;  // Sign/Negative

        // B Register
        public const int B_ADDR = 0x02;
        public static unsafe byte* B => (byte*)0x02;

        // C Register
        public const int C_ADDR = 0x03;
        public static unsafe byte* C => (byte*)0x03;

        // D Register
        public const int D_ADDR = 0x04;
        public static unsafe byte* D => (byte*)0x04;

        // E Register
        public const int E_ADDR = 0x05;
        public static unsafe byte* E => (byte*)0x05;

        // H Register
        public const int H_ADDR = 0x06;
        public static unsafe byte* H => (byte*)0x06;

        // L Register
        public const int L_ADDR = 0x07;
        public static unsafe byte* L => (byte*)0x07;

        // Alternate AF
        public const int AF_ADDR = 0x08;
        public static unsafe ushort* AF_ => (ushort*)0x08;

        // Alternate BC
        public const int BC_ADDR = 0x0A;
        public static unsafe ushort* BC_ => (ushort*)0x0A;

        // Alternate DE
        public const int DE_ADDR = 0x0C;
        public static unsafe ushort* DE_ => (ushort*)0x0C;

        // Alternate HL
        public const int HL_ADDR = 0x0E;
        public static unsafe ushort* HL_ => (ushort*)0x0E;

        // Index Register X
        public const int IX_ADDR = 0x10;
        public static unsafe ushort* IX => (ushort*)0x10;

        // Index Register Y
        public const int IY_ADDR = 0x12;
        public static unsafe ushort* IY => (ushort*)0x12;

        // Stack Pointer
        public const int SP_ADDR = 0x14;
        public static unsafe ushort* SP => (ushort*)0x14;

        // Program Counter
        public const int PC_ADDR = 0x16;
        public static unsafe ushort* PC => (ushort*)0x16;

        // Interrupt Vector Register
        public const int I_ADDR = 0x18;
        public static unsafe byte* I => (byte*)0x18;

        // Memory Refresh Register
        public const int R_ADDR = 0x19;
        public static unsafe byte* R => (byte*)0x19;

        // Interrupt Mode (0/1/2)
        public const int IM_ADDR = 0x1A;
        public static unsafe byte* IM => (byte*)0x1A;

        // 内存段定义
        // Work RAM (2KB internal)
        public const int WRAM_START = 0xC000;
        public const int WRAM_END = 0xC7FF;
        public const int WRAM_SIZE = 2048;

        // Work RAM Shadow (Echo RAM)
        public const int WRAM_SHADOW_START = 0xE000;
        public const int WRAM_SHADOW_END = 0xE7FF;
        public const int WRAM_SHADOW_SIZE = 2048;

        // Video RAM (16KB)
        public const int VRAM_START = 0x4000;
        public const int VRAM_END = 0x7FFF;
        public const int VRAM_SIZE = 16384;

        // Cartridge SRAM (if present)
        public const int SRAM_START = 0x8000;
        public const int SRAM_END = 0xBFFF;
        public const int SRAM_SIZE = 16384;

        // Cartridge ROM (up to 48KB)
        public const int CART_ROM_START = 0x0000;
        public const int CART_ROM_END = 0x7FFF;
        public const int CART_ROM_SIZE = 32768;

        // BIOS ROM (Master System built-in, 8KB)
        public const int BIOS_START = 0x0000;
        public const int BIOS_END = 0x1FFF;
        public const int BIOS_SIZE = 8192;

        // I/O Register Area
        public const int IO_REGS_START = 0x3F00;
        public const int IO_REGS_END = 0x3FFF;
        public const int IO_REGS_SIZE = 256;

        // 外设定义
        // Video Display Processor (TMS9918A variant)
        public const int VDP_BASE = 0xBE;
        public static unsafe byte* VDP_VDP_CTRL => (byte*)0x0000017D;
        public static unsafe byte* VDP_VDP_DATA => (byte*)0x0000017C;
        public static unsafe byte* VDP_VDP_STATUS => (byte*)0x0000017D;
        public const int VDP_VDP_STATUS_FIFO_FULL = 0;  // VRAM to CPU Transfer Pending
        public const int VDP_VDP_STATUS_FIFO_EMPTY = 1;  // VRAM Write FIFO Empty
        public const int VDP_VDP_STATUS_INT_FLAG = 7;  // V-Blank / Sprite Collision Flag
        public static unsafe byte* VDP_R0 => (byte*)0x000000BE;
        public const int VDP_R0_M3 = 0;  // Mode 3 Enable
        public const int VDP_R0_M2 = 1;  // Mode 2 Enable
        public const int VDP_R0_M1 = 2;  // Mode 1 Enable
        public const int VDP_R0_DISPLAY_DISABLE = 3;  // Display Disable (1=blank screen)
        public const int VDP_R0_VIRQ_EN = 4;  // Vertical Interrupt Enable
        public const int VDP_R0_M4 = 5;  // Mode 4 Enable (SMS2 only)
        public const int VDP_R0_SPRITE_SHIFT = 6;  // Sprite Double Height
        public const int VDP_R0_HVC_LATCH = 7;  // H-Counter Latch Enable
        public static unsafe byte* VDP_R1 => (byte*)0x000000BF;
        public const int VDP_R1_DISPLAY = 3;  // Display Enable (1=active)
        public const int VDP_R1_FRAME_INT = 4;  // Frame Interrupt (V-Blank) Enable
        public const int VDP_R1_M4 = 5;  // Mode 4 (256-color)
        public const int VDP_R1_SMS_MODE = 6;  // SMS Display Mode (vs Coleco)
        public const int VDP_R1_EXT_VIDEO = 7;  // External Video Enable
        public static unsafe byte* VDP_R2 => (byte*)0x000000C0;
        public static unsafe byte* VDP_R3 => (byte*)0x000000C1;
        public static unsafe byte* VDP_R4 => (byte*)0x000000C2;
        public static unsafe byte* VDP_R5 => (byte*)0x000000C3;
        public static unsafe byte* VDP_R6 => (byte*)0x000000C4;
        public static unsafe byte* VDP_R7 => (byte*)0x000000C5;
        public static unsafe byte* VDP_R8 => (byte*)0x000000C6;
        public const int VDP_R8_HSCROLL_EN = 0;  // Horizontal Scroll Enable
        public const int VDP_R8_VSCROLL_EN = 1;  // Vertical Scroll Enable
        public const int VDP_R8_LINE_INT = 4;  // Line Interrupt Enable
        public const int VDP_R8_VSCROLL_2X = 7;  // Vertical Scroll 2x Speed
        public static unsafe byte* VDP_R9 => (byte*)0x000000C7;
        public static unsafe byte* VDP_R10 => (byte*)0x000000C8;
        public static unsafe byte* VDP_R11 => (byte*)0x000000C9;
        public static unsafe byte* VDP_R12 => (byte*)0x000000CA;
        public static unsafe byte* VDP_R13 => (byte*)0x000000CB;
        public static unsafe byte* VDP_R14 => (byte*)0x000000CC;
        public static unsafe byte* VDP_R15 => (byte*)0x000000CD;
        public static unsafe byte* VDP_VCOUNTER => (byte*)0x0000013C;
        public static unsafe byte* VDP_HCOUNTER => (byte*)0x0000013D;

        // SN76489 Programmable Sound Generator (3 Square + 1 Noise)
        public const int PSG_BASE = 0x7F;
        public static unsafe byte* PSG_CH0_FREQ => (byte*)0x0000007F;
        public static unsafe byte* PSG_CH1_FREQ => (byte*)0x00000081;
        public static unsafe byte* PSG_CH2_FREQ => (byte*)0x00000083;
        public static unsafe byte* PSG_CH3_CONFIG => (byte*)0x00000085;
        public const int PSG_CH3_CONFIG_TYPE = 0;  // Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
        public const int PSG_CH3_CONFIG_VOLUME = 0;  // Volume (0-15)
        public static unsafe byte* PSG_CH0_VOLUME => (byte*)0x00000080;
        public static unsafe byte* PSG_CH1_VOLUME => (byte*)0x00000082;
        public static unsafe byte* PSG_CH2_VOLUME => (byte*)0x00000084;

        // I/O Port Registers
        public const int PORTS_BASE = 0x3F;
        public static unsafe byte* PORTS_PORT_A => (byte*)0x0000007E;
        public const int PORTS_PORT_A_UP = 0;  // Up (0=pressed)
        public const int PORTS_PORT_A_DOWN = 1;  // Down (0=pressed)
        public const int PORTS_PORT_A_LEFT = 2;  // Left (0=pressed)
        public const int PORTS_PORT_A_RIGHT = 3;  // Right (0=pressed)
        public const int PORTS_PORT_A_TR = 4;  // Button TR (0=pressed)
        public const int PORTS_PORT_A_TL = 5;  // Button TL (0=pressed)
        public static unsafe byte* PORTS_PORT_B => (byte*)0x0000007E;
        public const int PORTS_PORT_B_UP = 0;  // Up (0=pressed)
        public const int PORTS_PORT_B_DOWN = 1;  // Down (0=pressed)
        public const int PORTS_PORT_B_LEFT = 2;  // Left (0=pressed)
        public const int PORTS_PORT_B_RIGHT = 3;  // Right (0=pressed)
        public const int PORTS_PORT_B_TR = 4;  // Button TR (0=pressed)
        public const int PORTS_PORT_B_TL = 5;  // Button TL (0=pressed)
        public static unsafe byte* PORTS_PORT_A_DDR => (byte*)0x0000007E;
        public static unsafe byte* PORTS_PORT_B_DDR => (byte*)0x0000007E;

        // Sega Mapper (Memory Bank Switching)
        public const int SEGAMAPPER_BASE = 0xFFFD;
        public static unsafe byte* SEGAMAPPER_ROM_BANK0 => (byte*)0x0001FFFA;
        public static unsafe byte* SEGAMAPPER_ROM_BANK1 => (byte*)0x0001FFFB;
        public static unsafe byte* SEGAMAPPER_ROM_BANK2 => (byte*)0x0001FFFC;

        // Memory Mapper Control
        public const int MAPPER_BASE = 0xFFFF;
        public static unsafe byte* MAPPER_SRAM_BANK => (byte*)0x0001FFF7;

        // 中断向量定义
        public const int IRQ_NMI = 0;  // Non-Maskable Interrupt (Pause button / V-Blank)
        public const int IRQ_INT_VBLANK = 1;  // V-Blank Interrupt (Frame end)
        public const int IRQ_INT_LINE = 2;  // Scanline Interrupt (Line counter match)
        public const int IRQ_INT_EXT = 3;  // External I/O Interrupt

        // 引脚定义
        public const int PIN_A = 1;  // Power Supply
        public const int PIN_GND = 2;  // Ground
        public const int PIN_PHI = 3;  // System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
        public const int PIN_RESET = 4;  // Reset (active low)
        public const int PIN_M1 = 5;  // Machine Cycle 1 (instruction fetch)
        public const int PIN_MREQ = 6;  // Memory Request
        public const int PIN_IORQ = 7;  // I/O Request
        public const int PIN_RD = 8;  // Read Strobe
        public const int PIN_WR = 9;  // Write Strobe
        public const int PIN_HALT = 10;  // Halt State
        public const int PIN_WAIT = 11;  // Wait State Request
        public const int PIN_INT = 12;  // Interrupt Request (active low)
        public const int PIN_NMI = 13;  // Non-Maskable Interrupt (active low)
        public const int PIN_BUSRQ = 14;  // Bus Request (active low)
        public const int PIN_BUSAK = 15;  // Bus Acknowledge (active low)
        public const int PIN_A0 = 16;  // Address Bus Bit 0
        public const int PIN_A1 = 17;  // Address Bus Bit 1
        public const int PIN_A2 = 18;  // Address Bus Bit 2
        public const int PIN_A3 = 19;  // Address Bus Bit 3
        public const int PIN_A4 = 20;  // Address Bus Bit 4
        public const int PIN_A5 = 21;  // Address Bus Bit 5
        public const int PIN_A6 = 22;  // Address Bus Bit 6
        public const int PIN_A7 = 23;  // Address Bus Bit 7
        public const int PIN_A8 = 24;  // Address Bus Bit 8
        public const int PIN_A9 = 25;  // Address Bus Bit 9
        public const int PIN_A10 = 26;  // Address Bus Bit 10
        public const int PIN_A11 = 27;  // Address Bus Bit 11
        public const int PIN_A12 = 28;  // Address Bus Bit 12
        public const int PIN_A13 = 29;  // Address Bus Bit 13
        public const int PIN_A14 = 30;  // Address Bus Bit 14
        public const int PIN_A15 = 31;  // Address Bus Bit 15
        public const int PIN_D0 = 32;  // Data Bus Bit 0
        public const int PIN_D1 = 33;  // Data Bus Bit 1
        public const int PIN_D2 = 34;  // Data Bus Bit 2
        public const int PIN_D3 = 35;  // Data Bus Bit 3
        public const int PIN_D4 = 36;  // Data Bus Bit 4
        public const int PIN_D5 = 37;  // Data Bus Bit 5
        public const int PIN_D6 = 38;  // Data Bus Bit 6
        public const int PIN_D7 = 39;  // Data Bus Bit 7
        public const int PIN_AUDIO_OUT = 40;  // Audio Output
        public const int PIN_VIDEO_SYNC = 41;  // Composite Video Sync
        public const int PIN_VIDEO_OUT = 42;  // Composite Video Output

        public static void zilog_z80_init()
        {
            // 硬件初始化代码
        }
    }
}
