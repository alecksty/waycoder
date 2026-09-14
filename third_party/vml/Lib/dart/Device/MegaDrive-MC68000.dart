// Motorola-68000 设备定义 - Dart 库
// 生成自: Motorola/68000/Motorola-68000
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7670452 Hz

class Motorola_68000Device {
  static const String deviceName = "Motorola-68000";
  static const String manufacturer = "Motorola";
  static const String family = "68000";
  static const String version = "1.0";
  static const String architecture = "MC68000";
  static const int bits = 32;
  static const int clockFrequency = 7670452;

  // 寄存器地址定义
  static const int D0_ADDR = 0x00;  // Data Register 0
  static const int D1_ADDR = 0x04;  // Data Register 1
  static const int D2_ADDR = 0x08;  // Data Register 2
  static const int D3_ADDR = 0x0C;  // Data Register 3
  static const int D4_ADDR = 0x10;  // Data Register 4
  static const int D5_ADDR = 0x14;  // Data Register 5
  static const int D6_ADDR = 0x18;  // Data Register 6
  static const int D7_ADDR = 0x1C;  // Data Register 7
  static const int A0_ADDR = 0x20;  // Address Register 0
  static const int A1_ADDR = 0x24;  // Address Register 1
  static const int A2_ADDR = 0x28;  // Address Register 2
  static const int A3_ADDR = 0x2C;  // Address Register 3
  static const int A4_ADDR = 0x30;  // Address Register 4
  static const int A5_ADDR = 0x34;  // Address Register 5
  static const int A6_ADDR = 0x38;  // Address Register 6
  static const int A7_ADDR = 0x3C;  // Stack Pointer (USP)
  static const int PC_ADDR = 0x40;  // Program Counter
  static const int SR_ADDR = 0x44;  // Status Register
  static const int SR_C_BIT = 0;  // Carry
  static const int SR_V_BIT = 1;  // Overflow
  static const int SR_Z_BIT = 2;  // Zero
  static const int SR_N_BIT = 3;  // Negative
  static const int SR_X_BIT = 4;  // Extend
  static const int SR_I0_BIT = 8;  // Interrupt Mask 0
  static const int SR_I1_BIT = 9;  // Interrupt Mask 1
  static const int SR_I2_BIT = 10;  // Interrupt Mask 2
  static const int SR_M_BIT = 11;  // Master/Interrupt
  static const int SR_S_BIT = 13;  // Supervisor/User
  static const int SR_T0_BIT = 14;  // Trace Mode 0
  static const int SR_T1_BIT = 15;  // Trace Mode 1

  // 内存段定义
  static const int RAM_START = 0x000000;
  static const int RAM_END = 0x3FFFFF;
  static const int RAM_SIZE = 4194304;  // System RAM (4MB)
  static const int ROM_START = 0x000000;
  static const int ROM_END = 0x3FFFFF;
  static const int ROM_SIZE = 4194304;  // Cartridge ROM
  static const int IO_START = 0xA00000;
  static const int IO_END = 0xA1FFFF;
  static const int IO_SIZE = 131072;  // I/O Register Area
  static const int VDP_START = 0xC00000;
  static const int VDP_END = 0xC0001F;
  static const int VDP_SIZE = 32;  // VDP Registers
  static const int VRAM_START = 0xE00000;
  static const int VRAM_END = 0xE3FFFF;
  static const int VRAM_SIZE = 262144;  // Video RAM (256KB)

  // 外设定义
  // Video Display Processor (TMS9918A variant)
  static const int VDP_BASE = 0xC00000;
  static const int VDP_DATA_ADDR = 0x00;
  static const int VDP_CTRL_ADDR = 0x04;
  static const int VDP_HVCOUNT_ADDR = 0x08;
  static const int VDP_HVB_STATUS_ADDR = 0x0A;
  // Programmable Sound Generator (AY-3-8910)
  static const int PSG_BASE = 0xC00011;
  static const int PSG_CH_A_FREQ_ADDR = 0x00;
  static const int PSG_CH_A_VOL_ADDR = 0x08;
  static const int PSG_CH_B_FREQ_ADDR = 0x02;
  static const int PSG_CH_B_VOL_ADDR = 0x09;
  static const int PSG_CH_C_FREQ_ADDR = 0x04;
  static const int PSG_CH_C_VOL_ADDR = 0x0A;
  static const int PSG_NOISE_FREQ_ADDR = 0x06;
  static const int PSG_MIXER_ADDR = 0x07;
  static const int PSG_ENV_FREQ_ADDR = 0x0D;
  static const int PSG_ENV_SHAPE_ADDR = 0x0B;
  // Z80 Secondary CPU (Sound)
  static const int Z80_BASE = 0xA00000;
  static const int Z80_Z80_RESET_ADDR = 0x00;
  static const int Z80_Z80_BUSREQ_ADDR = 0x04;
  static const int Z80_Z80_STATUS_ADDR = 0x08;
  // Bank Register
  static const int BANK_REG_BASE = 0xA12000;
  static const int BANK_REG_ROM_BANK_ADDR = 0x00;
  static const int BANK_REG_RAM_BANK_ADDR = 0x04;
  // Hardware Version
  static const int HW_VERSION_BASE = 0xA10001;
  static const int HW_VERSION_VERSION_ADDR = 0x00;
  // Controller Port 1
  static const int CONTROLLER1_BASE = 0xA10003;
  static const int CONTROLLER1_DATA_ADDR = 0x00;
  static const int CONTROLLER1_CTRL_ADDR = 0x04;
  // Controller Port 2
  static const int CONTROLLER2_BASE = 0xA10005;
  static const int CONTROLLER2_DATA_ADDR = 0x00;
  static const int CONTROLLER2_CTRL_ADDR = 0x04;
  // External Port
  static const int EXT_PORT_BASE = 0xA10007;
  static const int EXT_PORT_DATA_ADDR = 0x00;
  // DMA Controller
  static const int DMA_BASE = 0xA10008;
  static const int DMA_SOURCE_ADDR = 0x00;
  static const int DMA_DEST_ADDR = 0x04;
  static const int DMA_COUNT_ADDR = 0x08;
  static const int DMA_CTRL_ADDR = 0x0A;
  // Hardware Timer
  static const int TIMER_BASE = 0xA1000E;
  static const int TIMER_H_COUNTER_ADDR = 0x00;
  static const int TIMER_V_COUNTER_ADDR = 0x04;

  // 中断向量定义
  static const int INT_RESET_SP = 1;  // Reset Initial Stack Pointer
  static const int INT_RESET_PC = 2;  // Reset Initial PC
  static const int INT_BUS_ERROR = 3;  // Bus Error
  static const int INT_ADDRESS_ERROR = 4;  // Address Error
  static const int INT_ILLEGAL_INSTR = 5;  // Illegal Instruction
  static const int INT_ZERO_DIVIDE = 6;  // Zero Divide
  static const int INT_CHK_EXCEPTION = 7;  // CHK Exception
  static const int INT_TRAPV = 8;  // TRAPV Exception
  static const int INT_PRIVILEGE = 9;  // Privilege Violation
  static const int INT_TRACE = 10;  // Trace
  static const int INT_LINE_A = 11;  // Line 1010 Emulator
  static const int INT_LINE_F = 12;  // Line 1111 Emulator
  static const int INT_IRQ1 = 24;  // External Interrupt 1 (H-Blank)
  static const int INT_IRQ2 = 25;  // External Interrupt 2 (V-Blank)
  static const int INT_IRQ3 = 26;  // External Interrupt 3
  static const int INT_IRQ4 = 27;  // External Interrupt 4 (D-Req)
  static const int INT_IRQ5 = 28;  // External Interrupt 5
  static const int INT_IRQ6 = 29;  // External Interrupt 6
  static const int INT_IRQ7 = 30;  // External Interrupt 7
  static const int INT_TRAP0 = 32;  // TRAP #0
  static const int INT_TRAP1 = 33;  // TRAP #1
  static const int INT_TRAP15 = 47;  // TRAP #15

}
