// Acorn-Archimedes-A310 设备定义 - Dart 库
// 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
// CPU架构: ARM250
// 位宽: 32位
// 时钟频率: 26000000 Hz

class Acorn_Archimedes_A310Device {
  static const String deviceName = "Acorn-Archimedes-A310";
  static const String manufacturer = "Acorn Computers";
  static const String family = "Archimedes";
  static const String version = "1.0";
  static const String architecture = "ARM250";
  static const int bits = 32;
  static const int clockFrequency = 26000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // General Purpose Register 0
  static const int R1_ADDR = 0x04;  // General Purpose Register 1
  static const int R2_ADDR = 0x08;  // General Purpose Register 2
  static const int R3_ADDR = 0x0C;  // General Purpose Register 3
  static const int R4_ADDR = 0x10;  // General Purpose Register 4
  static const int R5_ADDR = 0x14;  // General Purpose Register 5
  static const int R6_ADDR = 0x18;  // General Purpose Register 6
  static const int R7_ADDR = 0x1C;  // General Purpose Register 7
  static const int R8_ADDR = 0x20;  // General Purpose Register 8
  static const int R9_ADDR = 0x24;  // General Purpose Register 9
  static const int R10_ADDR = 0x28;  // General Purpose Register 10
  static const int R11_ADDR = 0x2C;  // General Purpose Register 11 (fp)
  static const int R12_ADDR = 0x30;  // General Purpose Register 12
  static const int SP_ADDR = 0x34;  // Stack Pointer (R13)
  static const int LR_ADDR = 0x38;  // Link Register (R14)
  static const int PC_ADDR = 0x3C;  // Program Counter (R15)
  static const int PSR_ADDR = 0x40;  // Processor Status Register
  static const int PSR_MODE_BIT = 0;  // Mode bits (0-4)
  static const int PSR_T_BIT = 5;  // Thumb state
  static const int PSR_F_BIT = 6;  // FIQ disable
  static const int PSR_I_BIT = 7;  // IRQ disable
  static const int PSR_V_BIT = 28;  // Overflow
  static const int PSR_C_BIT = 29;  // Carry
  static const int PSR_Z_BIT = 30;  // Zero
  static const int PSR_N_BIT = 31;  // Negative

  // 内存段定义
  static const int ROM_START = 0x00000000;
  static const int ROM_END = 0x0007FFFF;
  static const int ROM_SIZE = 524288;  // RISC OS ROM (512KB)
  static const int RAM_START = 0x00080000;
  static const int RAM_END = 0x003FFFFF;
  static const int RAM_SIZE = 3932160;  // Main RAM (up to 4MB)
  static const int VRAM_START = 0x00400000;
  static const int VRAM_END = 0x007FFFFF;
  static const int VRAM_SIZE = 4194304;  // Video RAM (4MB, VIDC)
  static const int IO_START = 0x03000000;
  static const int IO_END = 0x0301FFFF;
  static const int IO_SIZE = 131072;  // I/O controller (IOC)
  static const int MEMC_START = 0x03200000;
  static const int MEMC_END = 0x0320FFFF;
  static const int MEMC_SIZE = 4096;  // Memory Controller (MEMC)
  static const int VIDC_START = 0x03400000;
  static const int VIDC_END = 0x0340FFFF;
  static const int VIDC_SIZE = 4096;  // Video Controller (VIDC)
  static const int IOMD_START = 0x03300000;
  static const int IOMD_END = 0x0330FFFF;
  static const int IOMD_SIZE = 4096;  // I/O and Memory DMA

  // 外设定义
  // I/O Controller (IOC) - Interrupt/Keyboard/RTC
  static const int IOC_BASE = 0x03000000;
  static const int IOC_IOC_TIMER1_ADDR = 0x03000000;
  static const int IOC_IOC_TIMER2_ADDR = 0x03000004;
  static const int IOC_IOC_IOSEL_ADDR = 0x03000008;
  static const int IOC_IOC_IRQST_ADDR = 0x0300000C;
  static const int IOC_IOC_IRQLATCH_ADDR = 0x03000010;
  static const int IOC_IOC_FIQST_ADDR = 0x03000014;
  static const int IOC_IOC_FIQEN_ADDR = 0x03000018;
  static const int IOC_IOC_IRQEN_ADDR = 0x0300001C;
  static const int IOC_IOC_KBDDATA_ADDR = 0x03000020;
  static const int IOC_IOC_KBDCR_ADDR = 0x03000024;
  static const int IOC_IOC_RTCDR_ADDR = 0x03000028;
  static const int IOC_IOC_RTCCR_ADDR = 0x0300002C;
  static const int IOC_IOC_PRST_ADDR = 0x03000030;
  static const int IOC_IOC_PORTA_ADDR = 0x03000034;
  static const int IOC_IOC_PORTB_ADDR = 0x03000038;
  static const int IOC_IOC_PORTC_ADDR = 0x0300003C;
  // Memory Controller (MEMC1)
  static const int MEMC_BASE = 0x03200000;
  static const int MEMC_MEMC_PT_ADDR = 0x03200000;
  static const int MEMC_MEMC_CTRL_ADDR = 0x03200004;
  static const int MEMC_MEMC_DRAM_ADDR = 0x03200008;
  static const int MEMC_MEMC_ERR_ADDR = 0x0320000C;
  // Video Controller - VIDC1
  static const int VIDC_BASE = 0x03400000;
  static const int VIDC_VIDC_PALETTE_ADDR = 0x03400000;
  static const int VIDC_VIDC_STARTL_ADDR = 0x03400004;
  static const int VIDC_VIDC_STARTH_ADDR = 0x03400008;
  static const int VIDC_VIDC_CONFIG_ADDR = 0x0340000C;
  static const int VIDC_VIDC_HDISP_ADDR = 0x03400010;
  static const int VIDC_VIDC_VDISP_ADDR = 0x03400014;
  static const int VIDC_VIDC_HSYNC_ADDR = 0x03400018;
  static const int VIDC_VIDC_VSYNC_ADDR = 0x0340001C;
  static const int VIDC_VIDC_BORDER_ADDR = 0x03400020;
  static const int VIDC_VIDC_CURSOR_ADDR = 0x03400024;
  static const int VIDC_VIDC_SOUND_ADDR = 0x03400028;
  // Intel 82710 Floppy Disk Controller
  static const int FDC_BASE = 0x03010000;
  static const int FDC_FDC_STATUS_ADDR = 0x03010000;
  static const int FDC_FDC_COMMAND_ADDR = 0x03010000;
  static const int FDC_FDC_TRACK_ADDR = 0x03010004;
  static const int FDC_FDC_SECTOR_ADDR = 0x03010008;
  static const int FDC_FDC_DATA_ADDR = 0x0301000C;
  // Serial Port (via IOC)
  static const int SERIAL_BASE = 0x03010010;
  static const int SERIAL_SERIAL_TX_ADDR = 0x03010010;
  static const int SERIAL_SERIAL_RX_ADDR = 0x03010014;
  static const int SERIAL_SERIAL_CTRL_ADDR = 0x03010018;

  // 中断向量定义
  static const int INT_RESET = 0;  // Reset
  static const int INT_UND = 1;  // Undefined instruction
  static const int INT_SWI = 2;  // Software Interrupt (SWI/SVC)
  static const int INT_PABORT = 3;  // Prefetch Abort
  static const int INT_DABORT = 4;  // Data Abort
  static const int INT_ADDRESS = 5;  // Address Exception
  static const int INT_IRQ = 6;  // IRQ interrupt (IOC)
  static const int INT_FIQ = 7;  // FIQ interrupt (VIDC)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // ARM clock (26MHz)
  static const int PIN_NRESET = 4;  // Reset (active low)
  static const int PIN_NMREQ = 5;  // Memory Request (active low)
  static const int PIN_NIORQ = 6;  // I/O Request (active low)
  static const int PIN_NRW = 7;  // Read/Write (0=write, 1=read)
  static const int PIN_MAS0 = 8;  // Master address bit 0
  static const int PIN_MAS1 = 9;  // Master address bit 1
  static const int PIN_MAS2 = 10;  // Master address bit 2
  static const int PIN_LOCK = 11;  // Bus lock
  static const int PIN_NMREQ = 12;  // Memory request (active low)
  static const int PIN_NWAIT = 13;  // Wait state (active low)
  static const int PIN_NIRQLINE = 14;  // IRQ line (active low)
  static const int PIN_NFIRQLINE = 15;  // FIQ line (active low)
  static const int PIN_A1_A25 = 16;  // Address Bus (26-bit)
  static const int PIN_D0_D31 = 17;  // Data Bus (32-bit)

}
