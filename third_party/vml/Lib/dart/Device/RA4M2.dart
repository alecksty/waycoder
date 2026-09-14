// RA4M2 设备定义 - Dart 库
// 生成自: Renesas/RA/RA4M2
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 100000000 Hz

class RA4M2Device {
  static const String deviceName = "RA4M2";
  static const String manufacturer = "Renesas";
  static const String family = "RA";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4";
  static const int bits = 32;
  static const int clockFrequency = 100000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x04;  // 
  static const int R2_ADDR = 0x08;  // 
  static const int R3_ADDR = 0x0C;  // 
  static const int R4_ADDR = 0x10;  // 
  static const int R5_ADDR = 0x14;  // 
  static const int SP_ADDR = 0x34;  // 
  static const int LR_ADDR = 0x38;  // 
  static const int PC_ADDR = 0x3C;  // 

  // 内存段定义
  static const int FLASH_START = 0x00000000;
  static const int FLASH_END = 0x0003FFFF;
  static const int FLASH_SIZE = 262144;  // 
  static const int SRAM_START = 0x1FFE0000;
  static const int SRAM_END = 0x1FFE7FFF;
  static const int SRAM_SIZE = 32768;  // SRAM0
  static const int SRAM1_START = 0x20000000;
  static const int SRAM1_END = 0x20017FFF;
  static const int SRAM1_SIZE = 98304;  // SRAM1
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x400FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 

  // 外设定义
  // Module Stop Control
  static const int MSTP_BASE = 0x40020000;
  static const int MSTP_MSTPCR_A_ADDR = 0x20;
  static const int MSTP_MSTPCR_A_MSTP41_BIT = 9;  // GPIO A stop
  static const int MSTP_MSTPCR_A_MSTP42_BIT = 10;  // GPIO B stop
  static const int MSTP_MSTPCR_B_ADDR = 0x24;
  static const int MSTP_MSTPCR_C_ADDR = 0x28;
  static const int MSTP_MSTPCR_D_ADDR = 0x2C;
  // Interrupt Controller Unit
  static const int ICU_BASE = 0x40030000;
  static const int ICU_IRQCR0_ADDR = 0x600;
  static const int ICU_IRQCR1_ADDR = 0x602;
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x40040000;
  static const int GPIOA_PDR_ADDR = 0x00;
  static const int GPIOA_PODR_ADDR = 0x04;
  static const int GPIOA_PIDR_ADDR = 0x08;
  static const int GPIOA_PMR_ADDR = 0x10;
  static const int GPIOA_PCR_ADDR = 0x18;
  // General Purpose I/O Port B
  static const int GPIOB_BASE = 0x40040020;
  static const int GPIOB_PDR_ADDR = 0x00;
  static const int GPIOB_PODR_ADDR = 0x04;
  static const int GPIOB_PIDR_ADDR = 0x08;
  static const int GPIOB_PMR_ADDR = 0x10;
  // SCI UART 0
  static const int SCIUART0_BASE = 0x40070000;
  static const int SCIUART0_SCR_ADDR = 0x00;
  static const int SCIUART0_BRR_ADDR = 0x04;
  static const int SCIUART0_TDR_ADDR = 0x08;
  static const int SCIUART0_RDR_ADDR = 0x0C;
  static const int SCIUART0_SSR_ADDR = 0x10;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_SCIUART0_RXI = 24;  // SCI UART0 Receive Interrupt
  static const int INT_SCIUART0_TXI = 25;  // SCI UART0 Transmit Interrupt

}
