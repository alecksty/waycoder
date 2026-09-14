// PIC32MX170F256B 设备定义 - Dart 库
// 生成自: Microchip/PIC32/PIC32MX170F256B
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
// CPU架构: MIPS32-M4K
// 位宽: 32位
// 时钟频率: 50000000 Hz

class PIC32MX170F256BDevice {
  static const String deviceName = "PIC32MX170F256B";
  static const String manufacturer = "Microchip";
  static const String family = "PIC32";
  static const String version = "1.0";
  static const String architecture = "MIPS32-M4K";
  static const int bits = 32;
  static const int clockFrequency = 50000000;

  // 寄存器地址定义
  static const int _0_ADDR = 0x00;  // Hard-wired zero
  static const int _1_ADDR = 0x04;  // AT
  static const int _2_ADDR = 0x08;  // V0
  static const int _3_ADDR = 0x0C;  // V1
  static const int _4_ADDR = 0x10;  // A0
  static const int _5_ADDR = 0x14;  // A1
  static const int _29_ADDR = 0x74;  // Stack Pointer (SP)
  static const int _31_ADDR = 0x7C;  // Return Address (RA)
  static const int PC_ADDR = 0x80;  // Program Counter

  // 内存段定义
  static const int FLASH_START = 0x9D000000;
  static const int FLASH_END = 0x9D03FFFF;
  static const int FLASH_SIZE = 262144;  // Program Flash
  static const int SRAM_START = 0xA0000000;
  static const int SRAM_END = 0xA000FFFF;
  static const int SRAM_SIZE = 65536;  // 
  static const int PERIPHERAL_START = 0xBF800000;
  static const int PERIPHERAL_END = 0xBF8FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 
  static const int BOOTFLASH_START = 0xBFC00000;
  static const int BOOTFLASH_END = 0xBFC02FFF;
  static const int BOOTFLASH_SIZE = 12288;  // Boot Flash

  // 外设定义
  // General Purpose I/O Port A
  static const int PORTA_BASE = 0xBF886000;
  static const int PORTA_TRISA_ADDR = 0x00;
  static const int PORTA_PORTA_ADDR = 0x10;
  static const int PORTA_LATA_ADDR = 0x20;
  static const int PORTA_ODCA_ADDR = 0x30;
  // General Purpose I/O Port B
  static const int PORTB_BASE = 0xBF886100;
  static const int PORTB_TRISB_ADDR = 0x00;
  static const int PORTB_PORTB_ADDR = 0x10;
  static const int PORTB_LATB_ADDR = 0x20;
  static const int PORTB_ODCB_ADDR = 0x30;
  // UART1
  static const int UART1_BASE = 0xBF822000;
  static const int UART1_UXMODE_ADDR = 0x00;
  static const int UART1_UXSTA_ADDR = 0x04;
  static const int UART1_UXTXREG_ADDR = 0x08;
  static const int UART1_UXRXREG_ADDR = 0x0C;
  static const int UART1_UXBRG_ADDR = 0x10;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_UART1 = 8;  // UART1 Interrupt

}
