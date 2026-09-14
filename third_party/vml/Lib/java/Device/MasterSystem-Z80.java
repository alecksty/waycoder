package vml.device.zilog.zilog_z80;

/**
 * Zilog-Z80 寄存器定义
 * 生成自: Zilog/Z80/Zilog-Z80
 * 版本: 1.0
 */
public final class Zilog_Z80 {
    private Zilog_Z80() {} // 工具类
    // CPU架构: Z80, 8位, 3580000 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // Flags Register
    public static final int F_ADDR = (int)0x01;
    public static final int F_C = 0;  // Carry
    public static final int F_N = 1;  // Subtract
    public static final int F_P = 2;  // Parity/Overflow
    public static final int F_H = 4;  // Half Carry
    public static final int F_Z = 6;  // Zero
    public static final int F_S = 7;  // Sign/Negative

    // B Register
    public static final int B_ADDR = (int)0x02;

    // C Register
    public static final int C_ADDR = (int)0x03;

    // D Register
    public static final int D_ADDR = (int)0x04;

    // E Register
    public static final int E_ADDR = (int)0x05;

    // H Register
    public static final int H_ADDR = (int)0x06;

    // L Register
    public static final int L_ADDR = (int)0x07;

    // Alternate AF
    public static final int AF_ADDR = (int)0x08;

    // Alternate BC
    public static final int BC_ADDR = (int)0x0A;

    // Alternate DE
    public static final int DE_ADDR = (int)0x0C;

    // Alternate HL
    public static final int HL_ADDR = (int)0x0E;

    // Index Register X
    public static final int IX_ADDR = (int)0x10;

    // Index Register Y
    public static final int IY_ADDR = (int)0x12;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x14;

    // Program Counter
    public static final int PC_ADDR = (int)0x16;

    // Interrupt Vector Register
    public static final int I_ADDR = (int)0x18;

    // Memory Refresh Register
    public static final int R_ADDR = (int)0x19;

    // Interrupt Mode (0/1/2)
    public static final int IM_ADDR = (int)0x1A;

    // 内存段定义
    // Work RAM (2KB internal)
    public static final int WRAM_START = (int)0xC000;
    public static final int WRAM_END = (int)0xC7FF;
    public static final int WRAM_SIZE = 2048;

    // Work RAM Shadow (Echo RAM)
    public static final int WRAM_SHADOW_START = (int)0xE000;
    public static final int WRAM_SHADOW_END = (int)0xE7FF;
    public static final int WRAM_SHADOW_SIZE = 2048;

    // Video RAM (16KB)
    public static final int VRAM_START = (int)0x4000;
    public static final int VRAM_END = (int)0x7FFF;
    public static final int VRAM_SIZE = 16384;

    // Cartridge SRAM (if present)
    public static final int SRAM_START = (int)0x8000;
    public static final int SRAM_END = (int)0xBFFF;
    public static final int SRAM_SIZE = 16384;

    // Cartridge ROM (up to 48KB)
    public static final int CART_ROM_START = (int)0x0000;
    public static final int CART_ROM_END = (int)0x7FFF;
    public static final int CART_ROM_SIZE = 32768;

    // BIOS ROM (Master System built-in, 8KB)
    public static final int BIOS_START = (int)0x0000;
    public static final int BIOS_END = (int)0x1FFF;
    public static final int BIOS_SIZE = 8192;

    // I/O Register Area
    public static final int IO_REGS_START = (int)0x3F00;
    public static final int IO_REGS_END = (int)0x3FFF;
    public static final int IO_REGS_SIZE = 256;

    // 外设定义
    // Video Display Processor (TMS9918A variant)
    public static final int VDP_BASE = (int)0xBE;
    public static final int VDP_VDP_CTRL = (int)0x0000017D;
    public static final int VDP_VDP_DATA = (int)0x0000017C;
    public static final int VDP_VDP_STATUS = (int)0x0000017D;
    public static final int VDP_VDP_STATUS_FIFO_FULL = 0;  // VRAM to CPU Transfer Pending
    public static final int VDP_VDP_STATUS_FIFO_EMPTY = 1;  // VRAM Write FIFO Empty
    public static final int VDP_VDP_STATUS_INT_FLAG = 7;  // V-Blank / Sprite Collision Flag
    public static final int VDP_R0 = (int)0x000000BE;
    public static final int VDP_R0_M3 = 0;  // Mode 3 Enable
    public static final int VDP_R0_M2 = 1;  // Mode 2 Enable
    public static final int VDP_R0_M1 = 2;  // Mode 1 Enable
    public static final int VDP_R0_DISPLAY_DISABLE = 3;  // Display Disable (1=blank screen)
    public static final int VDP_R0_VIRQ_EN = 4;  // Vertical Interrupt Enable
    public static final int VDP_R0_M4 = 5;  // Mode 4 Enable (SMS2 only)
    public static final int VDP_R0_SPRITE_SHIFT = 6;  // Sprite Double Height
    public static final int VDP_R0_HVC_LATCH = 7;  // H-Counter Latch Enable
    public static final int VDP_R1 = (int)0x000000BF;
    public static final int VDP_R1_DISPLAY = 3;  // Display Enable (1=active)
    public static final int VDP_R1_FRAME_INT = 4;  // Frame Interrupt (V-Blank) Enable
    public static final int VDP_R1_M4 = 5;  // Mode 4 (256-color)
    public static final int VDP_R1_SMS_MODE = 6;  // SMS Display Mode (vs Coleco)
    public static final int VDP_R1_EXT_VIDEO = 7;  // External Video Enable
    public static final int VDP_R2 = (int)0x000000C0;
    public static final int VDP_R3 = (int)0x000000C1;
    public static final int VDP_R4 = (int)0x000000C2;
    public static final int VDP_R5 = (int)0x000000C3;
    public static final int VDP_R6 = (int)0x000000C4;
    public static final int VDP_R7 = (int)0x000000C5;
    public static final int VDP_R8 = (int)0x000000C6;
    public static final int VDP_R8_HSCROLL_EN = 0;  // Horizontal Scroll Enable
    public static final int VDP_R8_VSCROLL_EN = 1;  // Vertical Scroll Enable
    public static final int VDP_R8_LINE_INT = 4;  // Line Interrupt Enable
    public static final int VDP_R8_VSCROLL_2X = 7;  // Vertical Scroll 2x Speed
    public static final int VDP_R9 = (int)0x000000C7;
    public static final int VDP_R10 = (int)0x000000C8;
    public static final int VDP_R11 = (int)0x000000C9;
    public static final int VDP_R12 = (int)0x000000CA;
    public static final int VDP_R13 = (int)0x000000CB;
    public static final int VDP_R14 = (int)0x000000CC;
    public static final int VDP_R15 = (int)0x000000CD;
    public static final int VDP_VCOUNTER = (int)0x0000013C;
    public static final int VDP_HCOUNTER = (int)0x0000013D;

    // SN76489 Programmable Sound Generator (3 Square + 1 Noise)
    public static final int PSG_BASE = (int)0x7F;
    public static final int PSG_CH0_FREQ = (int)0x0000007F;
    public static final int PSG_CH1_FREQ = (int)0x00000081;
    public static final int PSG_CH2_FREQ = (int)0x00000083;
    public static final int PSG_CH3_CONFIG = (int)0x00000085;
    public static final int PSG_CH3_CONFIG_TYPE = 0;  // Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
    public static final int PSG_CH3_CONFIG_VOLUME = 0;  // Volume (0-15)
    public static final int PSG_CH0_VOLUME = (int)0x00000080;
    public static final int PSG_CH1_VOLUME = (int)0x00000082;
    public static final int PSG_CH2_VOLUME = (int)0x00000084;

    // I/O Port Registers
    public static final int PORTS_BASE = (int)0x3F;
    public static final int PORTS_PORT_A = (int)0x0000007E;
    public static final int PORTS_PORT_A_UP = 0;  // Up (0=pressed)
    public static final int PORTS_PORT_A_DOWN = 1;  // Down (0=pressed)
    public static final int PORTS_PORT_A_LEFT = 2;  // Left (0=pressed)
    public static final int PORTS_PORT_A_RIGHT = 3;  // Right (0=pressed)
    public static final int PORTS_PORT_A_TR = 4;  // Button TR (0=pressed)
    public static final int PORTS_PORT_A_TL = 5;  // Button TL (0=pressed)
    public static final int PORTS_PORT_B = (int)0x0000007E;
    public static final int PORTS_PORT_B_UP = 0;  // Up (0=pressed)
    public static final int PORTS_PORT_B_DOWN = 1;  // Down (0=pressed)
    public static final int PORTS_PORT_B_LEFT = 2;  // Left (0=pressed)
    public static final int PORTS_PORT_B_RIGHT = 3;  // Right (0=pressed)
    public static final int PORTS_PORT_B_TR = 4;  // Button TR (0=pressed)
    public static final int PORTS_PORT_B_TL = 5;  // Button TL (0=pressed)
    public static final int PORTS_PORT_A_DDR = (int)0x0000007E;
    public static final int PORTS_PORT_B_DDR = (int)0x0000007E;

    // Sega Mapper (Memory Bank Switching)
    public static final int SEGAMAPPER_BASE = (int)0xFFFD;
    public static final int SEGAMAPPER_ROM_BANK0 = (int)0x0001FFFA;
    public static final int SEGAMAPPER_ROM_BANK1 = (int)0x0001FFFB;
    public static final int SEGAMAPPER_ROM_BANK2 = (int)0x0001FFFC;

    // Memory Mapper Control
    public static final int MAPPER_BASE = (int)0xFFFF;
    public static final int MAPPER_SRAM_BANK = (int)0x0001FFF7;

    // 中断向量定义
    public static final int IRQ_NMI = 0;  // Non-Maskable Interrupt (Pause button / V-Blank)
    public static final int IRQ_INT_VBLANK = 1;  // V-Blank Interrupt (Frame end)
    public static final int IRQ_INT_LINE = 2;  // Scanline Interrupt (Line counter match)
    public static final int IRQ_INT_EXT = 3;  // External I/O Interrupt

    // 引脚定义
    public static final int PIN_A = 1;  // Power Supply
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_PHI = 3;  // System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
    public static final int PIN_RESET = 4;  // Reset (active low)
    public static final int PIN_M1 = 5;  // Machine Cycle 1 (instruction fetch)
    public static final int PIN_MREQ = 6;  // Memory Request
    public static final int PIN_IORQ = 7;  // I/O Request
    public static final int PIN_RD = 8;  // Read Strobe
    public static final int PIN_WR = 9;  // Write Strobe
    public static final int PIN_HALT = 10;  // Halt State
    public static final int PIN_WAIT = 11;  // Wait State Request
    public static final int PIN_INT = 12;  // Interrupt Request (active low)
    public static final int PIN_NMI = 13;  // Non-Maskable Interrupt (active low)
    public static final int PIN_BUSRQ = 14;  // Bus Request (active low)
    public static final int PIN_BUSAK = 15;  // Bus Acknowledge (active low)
    public static final int PIN_A0 = 16;  // Address Bus Bit 0
    public static final int PIN_A1 = 17;  // Address Bus Bit 1
    public static final int PIN_A2 = 18;  // Address Bus Bit 2
    public static final int PIN_A3 = 19;  // Address Bus Bit 3
    public static final int PIN_A4 = 20;  // Address Bus Bit 4
    public static final int PIN_A5 = 21;  // Address Bus Bit 5
    public static final int PIN_A6 = 22;  // Address Bus Bit 6
    public static final int PIN_A7 = 23;  // Address Bus Bit 7
    public static final int PIN_A8 = 24;  // Address Bus Bit 8
    public static final int PIN_A9 = 25;  // Address Bus Bit 9
    public static final int PIN_A10 = 26;  // Address Bus Bit 10
    public static final int PIN_A11 = 27;  // Address Bus Bit 11
    public static final int PIN_A12 = 28;  // Address Bus Bit 12
    public static final int PIN_A13 = 29;  // Address Bus Bit 13
    public static final int PIN_A14 = 30;  // Address Bus Bit 14
    public static final int PIN_A15 = 31;  // Address Bus Bit 15
    public static final int PIN_D0 = 32;  // Data Bus Bit 0
    public static final int PIN_D1 = 33;  // Data Bus Bit 1
    public static final int PIN_D2 = 34;  // Data Bus Bit 2
    public static final int PIN_D3 = 35;  // Data Bus Bit 3
    public static final int PIN_D4 = 36;  // Data Bus Bit 4
    public static final int PIN_D5 = 37;  // Data Bus Bit 5
    public static final int PIN_D6 = 38;  // Data Bus Bit 6
    public static final int PIN_D7 = 39;  // Data Bus Bit 7
    public static final int PIN_AUDIO_OUT = 40;  // Audio Output
    public static final int PIN_VIDEO_SYNC = 41;  // Composite Video Sync
    public static final int PIN_VIDEO_OUT = 42;  // Composite Video Output

    public static native void zilog_z80_init();
}
