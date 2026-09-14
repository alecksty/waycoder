// BL618 设备定义 - Dart 库
// 生成自: Bouffalo Lab/BL6/BL618
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 320000000 Hz

class BL618Device {
  static const String deviceName = "BL618";
  static const String manufacturer = "Bouffalo Lab";
  static const String family = "BL6";
  static const String version = "1.0";
  static const String architecture = "RISC-V";
  static const int bits = 32;
  static const int clockFrequency = 320000000;

  // 寄存器地址定义
  static const int X1_ADDR = 0x04;  // Return Address
  static const int X2_ADDR = 0x08;  // Stack Pointer (SP)
  static const int X3_ADDR = 0x0C;  // Global Pointer (GP)
  static const int X8_ADDR = 0x20;  // Frame Pointer (FP)
  static const int X10_ADDR = 0x28;  // Function Argument (A0)
  static const int X11_ADDR = 0x2C;  // Function Argument (A1)
  static const int PC_ADDR = 0x3C;  // Program Counter

  // 内存段定义
  static const int FLASH_START = 0x20000000;
  static const int FLASH_END = 0x203FFFFF;
  static const int FLASH_SIZE = 4194304;  // 
  static const int SRAM_HPSYS_START = 0x22000000;
  static const int SRAM_HPSYS_END = 0x22003FFF;
  static const int SRAM_HPSYS_SIZE = 16384;  // 
  static const int SRAM_DTCM_START = 0x22010000;
  static const int SRAM_DTCM_END = 0x22017FFF;
  static const int SRAM_DTCM_SIZE = 32768;  // DTCM
  static const int SRAM_SYS_START = 0x22020000;
  static const int SRAM_SYS_END = 0x2208FFFF;
  static const int SRAM_SYS_SIZE = 458752;  // 
  static const int PERIPHERAL_START = 0x30000000;
  static const int PERIPHERAL_END = 0x300FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 

  // 外设定义
  // Global Control (Clock and Reset)
  static const int GLB_BASE = 0x30000000;
  static const int GLB_GLB_CLK_EN_ADDR = 0x10;
  static const int GLB_GLB_CLK_EN_GPIO_CLK_EN_BIT = 6;  // GPIO clock enable
  static const int GLB_GLB_CLK_EN_UART0_CLK_EN_BIT = 12;  // UART0 clock enable
  static const int GLB_GLB_SYS_CLK_CTRL_ADDR = 0x14;
  static const int GLB_GLB_PLL_CTRL_ADDR = 0x1C;
  // GPIO Port A
  static const int GPIO_P0_BASE = 0x30007000;
  static const int GPIO_P0_GPIO_CFG0_ADDR = 0x00;
  static const int GPIO_P0_GPIO_CFG1_ADDR = 0x04;
  static const int GPIO_P0_GPIO_OE_ADDR = 0x08;
  static const int GPIO_P0_GPIO_OUT_ADDR = 0x0C;
  static const int GPIO_P0_GPIO_IN_ADDR = 0x10;
  static const int GPIO_P0_GPIO_SET_ADDR = 0x14;
  static const int GPIO_P0_GPIO_CLR_ADDR = 0x18;
  static const int GPIO_P0_GPIO_TOG_ADDR = 0x1C;
  // GPIO Port B
  static const int GPIO_P1_BASE = 0x30007200;
  static const int GPIO_P1_GPIO_CFG0_ADDR = 0x00;
  static const int GPIO_P1_GPIO_CFG1_ADDR = 0x04;
  static const int GPIO_P1_GPIO_OE_ADDR = 0x08;
  static const int GPIO_P1_GPIO_OUT_ADDR = 0x0C;
  static const int GPIO_P1_GPIO_IN_ADDR = 0x10;
  static const int GPIO_P1_GPIO_SET_ADDR = 0x14;
  static const int GPIO_P1_GPIO_CLR_ADDR = 0x18;
  static const int GPIO_P1_GPIO_TOG_ADDR = 0x1C;
  // UART 0
  static const int UART0_BASE = 0x30002000;
  static const int UART0_UART_CR_ADDR = 0x00;
  static const int UART0_UART_BRR_ADDR = 0x04;
  static const int UART0_UART_TDR_ADDR = 0x08;
  static const int UART0_UART_RDR_ADDR = 0x0C;
  static const int UART0_UART_SR_ADDR = 0x10;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_MACHINESOFTWARE = 3;  // 
  static const int INT_MACHINETIMER = 7;  // 
  static const int INT_MACHINEEXTERNAL = 11;  // 
  static const int INT_UART0 = 20;  // UART0 Interrupt

}
