// Zilog-Z80 设备定义 - Dart 库
// 生成自: Zilog/Z80/Zilog-Z80
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
// CPU架构: Z80
// 位宽: 8位
// 时钟频率: 3580000 Hz

class Zilog_Z80Device {
  static const String deviceName = "Zilog-Z80";
  static const String manufacturer = "Zilog";
  static const String family = "Z80";
  static const String version = "1.0";
  static const String architecture = "Z80";
  static const int bits = 8;
  static const int clockFrequency = 3580000;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int F_ADDR = 0x01;  // Flags Register
  static const int F_C_BIT = 0;  // Carry
  static const int F_N_BIT = 1;  // Subtract
  static const int F_P_BIT = 2;  // Parity/Overflow
  static const int F_H_BIT = 4;  // Half Carry
  static const int F_Z_BIT = 6;  // Zero
  static const int F_S_BIT = 7;  // Sign/Negative
  static const int B_ADDR = 0x02;  // B Register
  static const int C_ADDR = 0x03;  // C Register
  static const int D_ADDR = 0x04;  // D Register
  static const int E_ADDR = 0x05;  // E Register
  static const int H_ADDR = 0x06;  // H Register
  static const int L_ADDR = 0x07;  // L Register
  static const int AF_ADDR = 0x08;  // Alternate AF
  static const int BC_ADDR = 0x0A;  // Alternate BC
  static const int DE_ADDR = 0x0C;  // Alternate DE
  static const int HL_ADDR = 0x0E;  // Alternate HL
  static const int IX_ADDR = 0x10;  // Index Register X
  static const int IY_ADDR = 0x12;  // Index Register Y
  static const int SP_ADDR = 0x14;  // Stack Pointer
  static const int PC_ADDR = 0x16;  // Program Counter
  static const int I_ADDR = 0x18;  // Interrupt Vector Register
  static const int R_ADDR = 0x19;  // Memory Refresh Register
  static const int IM_ADDR = 0x1A;  // Interrupt Mode (0/1/2)

  // 内存段定义
  static const int WRAM_START = 0xC000;
  static const int WRAM_END = 0xC7FF;
  static const int WRAM_SIZE = 2048;  // Work RAM (2KB internal)
  static const int WRAM_SHADOW_START = 0xE000;
  static const int WRAM_SHADOW_END = 0xE7FF;
  static const int WRAM_SHADOW_SIZE = 2048;  // Work RAM Shadow (Echo RAM)
  static const int VRAM_START = 0x4000;
  static const int VRAM_END = 0x7FFF;
  static const int VRAM_SIZE = 16384;  // Video RAM (16KB)
  static const int SRAM_START = 0x8000;
  static const int SRAM_END = 0xBFFF;
  static const int SRAM_SIZE = 16384;  // Cartridge SRAM (if present)
  static const int CART_ROM_START = 0x0000;
  static const int CART_ROM_END = 0x7FFF;
  static const int CART_ROM_SIZE = 32768;  // Cartridge ROM (up to 48KB)
  static const int BIOS_START = 0x0000;
  static const int BIOS_END = 0x1FFF;
  static const int BIOS_SIZE = 8192;  // BIOS ROM (Master System built-in, 8KB)
  static const int IO_REGS_START = 0x3F00;
  static const int IO_REGS_END = 0x3FFF;
  static const int IO_REGS_SIZE = 256;  // I/O Register Area

  // 外设定义
  // Video Display Processor (TMS9918A variant)
  static const int VDP_BASE = 0xBE;
  static const int VDP_VDP_CTRL_ADDR = 0xBF;
  static const int VDP_VDP_DATA_ADDR = 0xBE;
  static const int VDP_VDP_STATUS_ADDR = 0xBF;
  static const int VDP_VDP_STATUS_FIFO_FULL_BIT = 0;  // VRAM to CPU Transfer Pending
  static const int VDP_VDP_STATUS_FIFO_EMPTY_BIT = 1;  // VRAM Write FIFO Empty
  static const int VDP_VDP_STATUS_INT_FLAG_BIT = 7;  // V-Blank / Sprite Collision Flag
  static const int VDP_R0_ADDR = 0x00;
  static const int VDP_R0_M3_BIT = 0;  // Mode 3 Enable
  static const int VDP_R0_M2_BIT = 1;  // Mode 2 Enable
  static const int VDP_R0_M1_BIT = 2;  // Mode 1 Enable
  static const int VDP_R0_DISPLAY_DISABLE_BIT = 3;  // Display Disable (1=blank screen)
  static const int VDP_R0_VIRQ_EN_BIT = 4;  // Vertical Interrupt Enable
  static const int VDP_R0_M4_BIT = 5;  // Mode 4 Enable (SMS2 only)
  static const int VDP_R0_SPRITE_SHIFT_BIT = 6;  // Sprite Double Height
  static const int VDP_R0_HVC_LATCH_BIT = 7;  // H-Counter Latch Enable
  static const int VDP_R1_ADDR = 0x01;
  static const int VDP_R1_DISPLAY_BIT = 3;  // Display Enable (1=active)
  static const int VDP_R1_FRAME_INT_BIT = 4;  // Frame Interrupt (V-Blank) Enable
  static const int VDP_R1_M4_BIT = 5;  // Mode 4 (256-color)
  static const int VDP_R1_SMS_MODE_BIT = 6;  // SMS Display Mode (vs Coleco)
  static const int VDP_R1_EXT_VIDEO_BIT = 7;  // External Video Enable
  static const int VDP_R2_ADDR = 0x02;
  static const int VDP_R3_ADDR = 0x03;
  static const int VDP_R4_ADDR = 0x04;
  static const int VDP_R5_ADDR = 0x05;
  static const int VDP_R6_ADDR = 0x06;
  static const int VDP_R7_ADDR = 0x07;
  static const int VDP_R8_ADDR = 0x08;
  static const int VDP_R8_HSCROLL_EN_BIT = 0;  // Horizontal Scroll Enable
  static const int VDP_R8_VSCROLL_EN_BIT = 1;  // Vertical Scroll Enable
  static const int VDP_R8_LINE_INT_BIT = 4;  // Line Interrupt Enable
  static const int VDP_R8_VSCROLL_2X_BIT = 7;  // Vertical Scroll 2x Speed
  static const int VDP_R9_ADDR = 0x09;
  static const int VDP_R10_ADDR = 0x0A;
  static const int VDP_R11_ADDR = 0x0B;
  static const int VDP_R12_ADDR = 0x0C;
  static const int VDP_R13_ADDR = 0x0D;
  static const int VDP_R14_ADDR = 0x0E;
  static const int VDP_R15_ADDR = 0x0F;
  static const int VDP_VCOUNTER_ADDR = 0x7E;
  static const int VDP_HCOUNTER_ADDR = 0x7F;
  // SN76489 Programmable Sound Generator (3 Square + 1 Noise)
  static const int PSG_BASE = 0x7F;
  static const int PSG_CH0_FREQ_ADDR = 0x00;
  static const int PSG_CH1_FREQ_ADDR = 0x02;
  static const int PSG_CH2_FREQ_ADDR = 0x04;
  static const int PSG_CH3_CONFIG_ADDR = 0x06;
  static const int PSG_CH3_CONFIG_TYPE_BIT = 0;  // Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
  static const int PSG_CH3_CONFIG_VOLUME_BIT = 0;  // Volume (0-15)
  static const int PSG_CH0_VOLUME_ADDR = 0x01;
  static const int PSG_CH1_VOLUME_ADDR = 0x03;
  static const int PSG_CH2_VOLUME_ADDR = 0x05;
  // I/O Port Registers
  static const int PORTS_BASE = 0x3F;
  static const int PORTS_PORT_A_ADDR = 0x3F;
  static const int PORTS_PORT_A_UP_BIT = 0;  // Up (0=pressed)
  static const int PORTS_PORT_A_DOWN_BIT = 1;  // Down (0=pressed)
  static const int PORTS_PORT_A_LEFT_BIT = 2;  // Left (0=pressed)
  static const int PORTS_PORT_A_RIGHT_BIT = 3;  // Right (0=pressed)
  static const int PORTS_PORT_A_TR_BIT = 4;  // Button TR (0=pressed)
  static const int PORTS_PORT_A_TL_BIT = 5;  // Button TL (0=pressed)
  static const int PORTS_PORT_B_ADDR = 0x3F;
  static const int PORTS_PORT_B_UP_BIT = 0;  // Up (0=pressed)
  static const int PORTS_PORT_B_DOWN_BIT = 1;  // Down (0=pressed)
  static const int PORTS_PORT_B_LEFT_BIT = 2;  // Left (0=pressed)
  static const int PORTS_PORT_B_RIGHT_BIT = 3;  // Right (0=pressed)
  static const int PORTS_PORT_B_TR_BIT = 4;  // Button TR (0=pressed)
  static const int PORTS_PORT_B_TL_BIT = 5;  // Button TL (0=pressed)
  static const int PORTS_PORT_A_DDR_ADDR = 0x3F;
  static const int PORTS_PORT_B_DDR_ADDR = 0x3F;
  // Sega Mapper (Memory Bank Switching)
  static const int SEGAMAPPER_BASE = 0xFFFD;
  static const int SEGAMAPPER_ROM_BANK0_ADDR = 0xFFFD;
  static const int SEGAMAPPER_ROM_BANK1_ADDR = 0xFFFE;
  static const int SEGAMAPPER_ROM_BANK2_ADDR = 0xFFFF;
  // Memory Mapper Control
  static const int MAPPER_BASE = 0xFFFF;
  static const int MAPPER_SRAM_BANK_ADDR = 0xFFF8;

  // 中断向量定义
  static const int INT_NMI = 0;  // Non-Maskable Interrupt (Pause button / V-Blank)
  static const int INT_INT_VBLANK = 1;  // V-Blank Interrupt (Frame end)
  static const int INT_INT_LINE = 2;  // Scanline Interrupt (Line counter match)
  static const int INT_INT_EXT = 3;  // External I/O Interrupt

  // 引脚定义
  static const int PIN_A = 1;  // Power Supply
  static const int PIN_GND = 2;  // Ground
  static const int PIN_PHI = 3;  // System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
  static const int PIN_RESET = 4;  // Reset (active low)
  static const int PIN_M1 = 5;  // Machine Cycle 1 (instruction fetch)
  static const int PIN_MREQ = 6;  // Memory Request
  static const int PIN_IORQ = 7;  // I/O Request
  static const int PIN_RD = 8;  // Read Strobe
  static const int PIN_WR = 9;  // Write Strobe
  static const int PIN_HALT = 10;  // Halt State
  static const int PIN_WAIT = 11;  // Wait State Request
  static const int PIN_INT = 12;  // Interrupt Request (active low)
  static const int PIN_NMI = 13;  // Non-Maskable Interrupt (active low)
  static const int PIN_BUSRQ = 14;  // Bus Request (active low)
  static const int PIN_BUSAK = 15;  // Bus Acknowledge (active low)
  static const int PIN_A0 = 16;  // Address Bus Bit 0
  static const int PIN_A1 = 17;  // Address Bus Bit 1
  static const int PIN_A2 = 18;  // Address Bus Bit 2
  static const int PIN_A3 = 19;  // Address Bus Bit 3
  static const int PIN_A4 = 20;  // Address Bus Bit 4
  static const int PIN_A5 = 21;  // Address Bus Bit 5
  static const int PIN_A6 = 22;  // Address Bus Bit 6
  static const int PIN_A7 = 23;  // Address Bus Bit 7
  static const int PIN_A8 = 24;  // Address Bus Bit 8
  static const int PIN_A9 = 25;  // Address Bus Bit 9
  static const int PIN_A10 = 26;  // Address Bus Bit 10
  static const int PIN_A11 = 27;  // Address Bus Bit 11
  static const int PIN_A12 = 28;  // Address Bus Bit 12
  static const int PIN_A13 = 29;  // Address Bus Bit 13
  static const int PIN_A14 = 30;  // Address Bus Bit 14
  static const int PIN_A15 = 31;  // Address Bus Bit 15
  static const int PIN_D0 = 32;  // Data Bus Bit 0
  static const int PIN_D1 = 33;  // Data Bus Bit 1
  static const int PIN_D2 = 34;  // Data Bus Bit 2
  static const int PIN_D3 = 35;  // Data Bus Bit 3
  static const int PIN_D4 = 36;  // Data Bus Bit 4
  static const int PIN_D5 = 37;  // Data Bus Bit 5
  static const int PIN_D6 = 38;  // Data Bus Bit 6
  static const int PIN_D7 = 39;  // Data Bus Bit 7
  static const int PIN_AUDIO_OUT = 40;  // Audio Output
  static const int PIN_VIDEO_SYNC = 41;  // Composite Video Sync
  static const int PIN_VIDEO_OUT = 42;  // Composite Video Output

}
