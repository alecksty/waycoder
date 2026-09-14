// XMC4500 设备定义 - Dart 库
// 生成自: Infineon/XMC4000/XMC4500
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 120000000 Hz

class XMC4500Device {
  static const String deviceName = "XMC4500";
  static const String manufacturer = "Infineon";
  static const String family = "XMC4000";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4";
  static const int bits = 32;
  static const int clockFrequency = 120000000;

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
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x080FFFFF;
  static const int FLASH_SIZE = 1048576;  // 
  static const int SRAM_START = 0x1FF00000;
  static const int SRAM_END = 0x1FF0FFFF;
  static const int SRAM_SIZE = 65536;  // 
  static const int SRAM_COM_START = 0x20000000;
  static const int SRAM_COM_END = 0x20007FFF;
  static const int SRAM_COM_SIZE = 32768;  // Communication Memory
  static const int SRAM_CPU_START = 0x20010000;
  static const int SRAM_CPU_END = 0x2001FFFF;
  static const int SRAM_CPU_SIZE = 65536;  // CPU SRAM
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4FFFFFFF;
  static const int PERIPHERAL_SIZE = 268435456;  // 

  // 外设定义
  // System Control Unit
  static const int SCU_BASE = 0x40020000;
  static const int SCU_CLKCR_ADDR = 0x00;
  static const int SCU_CLKCR_PCLK_SEL_BIT = 0;  // CPU clock selection
  static const int SCU_CLKCR_FBKDIV_BIT = 16;  // Feedback divider
  static const int SCU_PLLCONFIG_ADDR = 0x04;
  static const int SCU_OSCHPCTRL_ADDR = 0x08;
  static const int SCU_CGATSET0_ADDR = 0x20;
  static const int SCU_CGATSET0_CG_GATE_GPIO_BIT = 4;  // GPIO gate enable
  static const int SCU_CGATCLR0_ADDR = 0x24;
  // Port 0
  static const int PORT0_BASE = 0x48000000;
  static const int PORT0_OUT_ADDR = 0x00;
  static const int PORT0_OMR_ADDR = 0x04;
  static const int PORT0_IOCR0_ADDR = 0x10;
  static const int PORT0_IOCR4_ADDR = 0x14;
  static const int PORT0_IOCR8_ADDR = 0x18;
  static const int PORT0_IOCR12_ADDR = 0x1C;
  static const int PORT0_IN_ADDR = 0x24;
  // Port 1
  static const int PORT1_BASE = 0x48010000;
  static const int PORT1_OUT_ADDR = 0x00;
  static const int PORT1_OMR_ADDR = 0x04;
  static const int PORT1_IOCR0_ADDR = 0x10;
  static const int PORT1_IOCR4_ADDR = 0x14;
  static const int PORT1_IOCR8_ADDR = 0x18;
  static const int PORT1_IOCR12_ADDR = 0x1C;
  static const int PORT1_IN_ADDR = 0x24;
  // Port 2
  static const int PORT2_BASE = 0x48020000;
  static const int PORT2_OUT_ADDR = 0x00;
  static const int PORT2_OMR_ADDR = 0x04;
  static const int PORT2_IOCR0_ADDR = 0x10;
  static const int PORT2_IOCR4_ADDR = 0x14;
  static const int PORT2_IN_ADDR = 0x24;
  // Universal Serial Interface 0 (UART)
  static const int USIC0_BASE = 0x48030000;
  static const int USIC0_CCR_ADDR = 0x00;
  static const int USIC0_PCR_ADDR = 0x04;
  static const int USIC0_RBUF_ADDR = 0x08;
  static const int USIC0_TBUF_ADDR = 0x0C;
  static const int USIC0_BRG_ADDR = 0x10;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_USIC0_SR0 = 12;  // USIC0 Service Request 0

}
