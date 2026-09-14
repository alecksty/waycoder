// ESP32-C3 设备定义 - Dart 库
// 生成自: Espressif/ESP32-C/ESP32-C3
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 160000000 Hz

class ESP32_C3Device {
  static const String deviceName = "ESP32-C3";
  static const String manufacturer = "Espressif";
  static const String family = "ESP32-C";
  static const String version = "1.0";
  static const String architecture = "RISC-V";
  static const int bits = 32;
  static const int clockFrequency = 160000000;

  // 寄存器地址定义
  static const int X1_ADDR = 0x04;  // Return Address
  static const int X2_ADDR = 0x08;  // Stack Pointer (SP)
  static const int X3_ADDR = 0x0C;  // Global Pointer (GP)
  static const int X8_ADDR = 0x20;  // Frame Pointer (FP)
  static const int X10_ADDR = 0x28;  // Function Argument (A0)
  static const int X11_ADDR = 0x2C;  // Function Argument (A1)
  static const int PC_ADDR = 0x3C;  // Program Counter

  // 内存段定义
  static const int FLASH_START = 0x42000000;
  static const int FLASH_END = 0x427FFFFF;
  static const int FLASH_SIZE = 8388608;  // Flash via Cache
  static const int SRAM_START = 0x3FC80000;
  static const int SRAM_END = 0x3FCE3FFF;
  static const int SRAM_SIZE = 409600;  // Internal SRAM
  static const int PERIPHERAL_START = 0x60000000;
  static const int PERIPHERAL_END = 0x600FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 

  // 外设定义
  // General Purpose I/O
  static const int GPIO_BASE = 0x60004000;
  static const int GPIO_OUT_ADDR = 0x04;
  static const int GPIO_OUT_W1TS_ADDR = 0x08;
  static const int GPIO_OUT_W1TC_ADDR = 0x0C;
  static const int GPIO_IN_ADDR = 0x10;
  static const int GPIO_ENABLE_ADDR = 0x20;
  static const int GPIO_ENABLE_W1TS_ADDR = 0x24;
  static const int GPIO_ENABLE_W1TC_ADDR = 0x28;
  // I/O MUX
  static const int IO_MUX_BASE = 0x60009000;
  static const int IO_MUX_GPIO0_ADDR = 0x00;
  static const int IO_MUX_GPIO1_ADDR = 0x04;
  static const int IO_MUX_GPIO2_ADDR = 0x08;
  static const int IO_MUX_GPIO3_ADDR = 0x0C;
  // RTC Control
  static const int RTC_CNTL_BASE = 0x60008000;
  static const int RTC_CNTL_OPTIONS0_ADDR = 0x00;
  static const int RTC_CNTL_CLK_CONF_ADDR = 0x30;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_MACHINESOFTWARE = 3;  // 
  static const int INT_MACHINETIMER = 7;  // 
  static const int INT_MACHINEEXTERNAL = 11;  // 

}
