// Ricoh-2A03 设备定义 - Dart 库
// 生成自: Ricoh/MOS-6502/Ricoh-2A03
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 10765930 Hz

class Ricoh_2A03Device {
  static const String deviceName = "Ricoh-2A03";
  static const String manufacturer = "Ricoh";
  static const String family = "MOS-6502";
  static const String version = "1.0";
  static const String architecture = "MOS-6502";
  static const int bits = 8;
  static const int clockFrequency = 10765930;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int X_ADDR = 0x01;  // X Index
  static const int Y_ADDR = 0x02;  // Y Index
  static const int SP_ADDR = 0x03;  // Stack Pointer
  static const int PC_ADDR = 0x04;  // Program Counter (16-bit)
  static const int P_ADDR = 0x06;  // Processor Status
  static const int P_C_BIT = 0;  // Carry
  static const int P_Z_BIT = 1;  // Zero
  static const int P_I_BIT = 2;  // Interrupt Disable
  static const int P_D_BIT = 3;  // Decimal Mode
  static const int P_B_BIT = 4;  // Break
  static const int P_U_BIT = 5;  // Unused
  static const int P_V_BIT = 6;  // Overflow
  static const int P_N_BIT = 7;  // Negative

  // 内存段定义
  static const int CPU_RAM_START = 0x0000;
  static const int CPU_RAM_END = 0x07FF;
  static const int CPU_RAM_SIZE = 2048;  // CPU 2KB RAM (mirrored)
  static const int PPU_REGISTERS_START = 0x2000;
  static const int PPU_REGISTERS_END = 0x3FFF;
  static const int PPU_REGISTERS_SIZE = 8192;  // PPU Registers (mirrored every 8 bytes)
  static const int APU_REGISTERS_START = 0x4000;
  static const int APU_REGISTERS_END = 0x401F;
  static const int APU_REGISTERS_SIZE = 32;  // APU and I/O Registers
  static const int EXPANSION_START = 0x4020;
  static const int EXPANSION_END = 0x5FFF;
  static const int EXPANSION_SIZE = 8160;  // Expansion ROM
  static const int SRAM_START = 0x6000;
  static const int SRAM_END = 0x7FFF;
  static const int SRAM_SIZE = 8192;  // Save RAM
  static const int PRG_ROM_LOW_START = 0x8000;
  static const int PRG_ROM_LOW_END = 0xBFFF;
  static const int PRG_ROM_LOW_SIZE = 16384;  // PRG ROM Lower Bank (16KB)
  static const int PRG_ROM_HIGH_START = 0xC000;
  static const int PRG_ROM_HIGH_END = 0xFFFF;
  static const int PRG_ROM_HIGH_SIZE = 16384;  // PRG ROM Higher Bank (16KB)

  // 外设定义
  // Picture Processing Unit
  static const int PPU_BASE = 0x2000;
  static const int PPU_PPUCTRL_ADDR = 0x2000;
  static const int PPU_PPUMASK_ADDR = 0x2001;
  static const int PPU_PPUSTATUS_ADDR = 0x2002;
  static const int PPU_OAMADDR_ADDR = 0x2003;
  static const int PPU_OAMDATA_ADDR = 0x2004;
  static const int PPU_PPUSCROLL_ADDR = 0x2005;
  static const int PPU_PPUADDR_ADDR = 0x2006;
  static const int PPU_PPUDATA_ADDR = 0x2007;
  // Audio Processing Unit
  static const int APU_BASE = 0x4000;
  static const int APU_PULSE1_VOL_ADDR = 0x4000;
  static const int APU_PULSE1_SWEEP_ADDR = 0x4001;
  static const int APU_PULSE1_LO_ADDR = 0x4002;
  static const int APU_PULSE1_HI_ADDR = 0x4003;
  static const int APU_PULSE2_VOL_ADDR = 0x4004;
  static const int APU_PULSE2_SWEEP_ADDR = 0x4005;
  static const int APU_PULSE2_LO_ADDR = 0x4006;
  static const int APU_PULSE2_HI_ADDR = 0x4007;
  static const int APU_TRIANGLE_ADDR = 0x4008;
  static const int APU_TRIANGLE_HI_ADDR = 0x400B;
  static const int APU_NOISE_VOL_ADDR = 0x400C;
  static const int APU_NOISE_HI_ADDR = 0x400E;
  static const int APU_NOISE_LENGTH_ADDR = 0x400F;
  static const int APU_DMC_RATE_ADDR = 0x4010;
  static const int APU_DMC_RAW_ADDR = 0x4011;
  static const int APU_DMC_START_ADDR = 0x4012;
  static const int APU_DMC_LENGTH_ADDR = 0x4013;
  static const int APU_OAMDMA_ADDR = 0x4014;
  static const int APU_SNDCHN_ADDR = 0x4015;
  static const int APU_JOY1_ADDR = 0x4016;
  static const int APU_JOY2_ADDR = 0x4017;
  // Controller Port 1
  static const int INPUT1_BASE = 0x4016;
  static const int INPUT1_JOYPAD1_ADDR = 0x4016;
  // Controller Port 2
  static const int INPUT2_BASE = 0x4017;
  static const int INPUT2_JOYPAD2_ADDR = 0x4017;

  // 中断向量定义
  static const int INT_RESET = 0;  // Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt (VBlank)
  static const int INT_IRQ = 2;  // IRQ / BRK

}
