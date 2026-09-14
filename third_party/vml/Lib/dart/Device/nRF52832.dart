// nRF52832 设备定义 - Dart 库
// 生成自: Nordic/nRF52/nRF52832
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 64000000 Hz

class nRF52832Device {
  static const String deviceName = "nRF52832";
  static const String manufacturer = "Nordic";
  static const String family = "nRF52";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4F";
  static const int bits = 32;
  static const int clockFrequency = 64000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x04;  // 
  static const int R2_ADDR = 0x08;  // 
  static const int R3_ADDR = 0x0C;  // 
  static const int SP_ADDR = 0x34;  // 
  static const int LR_ADDR = 0x38;  // 
  static const int PC_ADDR = 0x3C;  // 

  // 内存段定义
  static const int FLASH_START = 0x00000000;
  static const int FLASH_END = 0x0007FFFF;
  static const int FLASH_SIZE = 524288;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x2000FFFF;
  static const int SRAM_SIZE = 65536;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x400FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 
  static const int FICR_START = 0x10000000;
  static const int FICR_END = 0x10000FFF;
  static const int FICR_SIZE = 4096;  // Factory Information Configuration Registers

  // 外设定义
  // General Purpose I/O Port 0
  static const int GPIO_P0_BASE = 0x50000000;
  static const int GPIO_P0_OUT_ADDR = 0x504;
  static const int GPIO_P0_OUTSET_ADDR = 0x508;
  static const int GPIO_P0_OUTCLR_ADDR = 0x50C;
  static const int GPIO_P0_IN_ADDR = 0x510;
  static const int GPIO_P0_DIR_ADDR = 0x514;
  static const int GPIO_P0_DIRSET_ADDR = 0x518;
  static const int GPIO_P0_DIRCLR_ADDR = 0x51C;
  // Power Control
  static const int POWER_BASE = 0x40000000;
  static const int POWER_DCDCEN_ADDR = 0x1C4;
  static const int POWER_RAMSTATUS_ADDR = 0x268;
  // Clock Control
  static const int CLOCK_BASE = 0x40000000;
  static const int CLOCK_HFCLKSTART_ADDR = 0x108;
  static const int CLOCK_HFCLKSTARTED_ADDR = 0x208;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 

}
