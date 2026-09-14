// EFR32MG24 设备定义 - Dart 库
// 生成自: Silicon Labs/EFR32/EFR32MG24
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 78000000 Hz

class EFR32MG24Device {
  static const String deviceName = "EFR32MG24";
  static const String manufacturer = "Silicon Labs";
  static const String family = "EFR32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M33";
  static const int bits = 32;
  static const int clockFrequency = 78000000;

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
  static const int FLASH_END = 0x0817FFFF;
  static const int FLASH_SIZE = 1572864;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x2003FFFF;
  static const int SRAM_SIZE = 262144;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4007FFFF;
  static const int PERIPHERAL_SIZE = 524288;  // 

  // 外设定义
  // Clock Management Unit
  static const int CMU_BASE = 0x40080000;
  static const int CMU_CTRL_ADDR = 0x00;
  static const int CMU_HFCORECLKCFG_ADDR = 0x08;
  static const int CMU_HFPERCLKEN0_ADDR = 0x10;
  static const int CMU_HFPERCLKEN0_GPIOEN_BIT = 4;  // GPIO clock enable
  static const int CMU_HFPERCLKEN0_USART0EN_BIT = 12;  // USART0 clock enable
  static const int CMU_HFPERCLKEN0_USART1EN_BIT = 13;  // USART1 clock enable
  static const int CMU_LFBCLKEN0_ADDR = 0x20;
  // GPIO Controller
  static const int GPIO_BASE = 0x40088000;
  static const int GPIO_PORT_A_CTRL_ADDR = 0x00;
  static const int GPIO_PORT_B_CTRL_ADDR = 0x04;
  static const int GPIO_PORT_C_CTRL_ADDR = 0x08;
  static const int GPIO_PORT_D_CTRL_ADDR = 0x0C;
  static const int GPIO_MODEL_ADDR = 0x10;
  static const int GPIO_MODEH_ADDR = 0x14;
  static const int GPIO_DOUT_ADDR = 0x1C;
  static const int GPIO_DOUTSET_ADDR = 0x20;
  static const int GPIO_DOUTCLR_ADDR = 0x24;
  static const int GPIO_DOUTTGL_ADDR = 0x28;
  static const int GPIO_DIN_ADDR = 0x2C;
  // GPIO Port A extended
  static const int GPIO_PA_BASE = 0x40088400;
  static const int GPIO_PA_PA_CFG_ADDR = 0x00;
  static const int GPIO_PA_PA_PINOUT_ADDR = 0x04;
  // GPIO Port B extended
  static const int GPIO_PB_BASE = 0x40088800;
  static const int GPIO_PB_PB_CFG_ADDR = 0x00;
  // USART 0
  static const int USART0_BASE = 0x40060000;
  static const int USART0_CTRL_ADDR = 0x00;
  static const int USART0_CMD_ADDR = 0x04;
  static const int USART0_STATUS_ADDR = 0x08;
  static const int USART0_RXDATA_ADDR = 0x0C;
  static const int USART0_TXDATA_ADDR = 0x10;
  static const int USART0_CLKDIV_ADDR = 0x14;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_USART0_RX = 12;  // USART0 Receive Interrupt
  static const int INT_USART0_TX = 13;  // USART0 Transmit Interrupt

}
