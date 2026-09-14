// TMS320F280049 设备定义 - Dart 库
// 生成自: Texas Instruments/C2000/TMS320F280049
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
// CPU架构: C28x-DSP
// 位宽: 32位
// 时钟频率: 100000000 Hz

class TMS320F280049Device {
  static const String deviceName = "TMS320F280049";
  static const String manufacturer = "Texas Instruments";
  static const String family = "C2000";
  static const String version = "1.0";
  static const String architecture = "C28x-DSP";
  static const int bits = 32;
  static const int clockFrequency = 100000000;

  // 寄存器地址定义
  static const int AL_ADDR = 0x00;  // Accumulator Low
  static const int AH_ADDR = 0x02;  // Accumulator High
  static const int PH_ADDR = 0x04;  // Product High
  static const int PL_ADDR = 0x06;  // Product Low
  static const int TREG_ADDR = 0x08;  // Temporary Register
  static const int AR0_ADDR = 0x0A;  // 
  static const int AR1_ADDR = 0x0C;  // 
  static const int ST0_ADDR = 0x20;  // Status 0
  static const int ST1_ADDR = 0x22;  // Status 1
  static const int PC_ADDR = 0x24;  // Program Counter
  static const int SP_ADDR = 0x26;  // Stack Pointer

  // 内存段定义
  static const int FLASH_START = 0x080000;
  static const int FLASH_END = 0x0BFFFF;
  static const int FLASH_SIZE = 262144;  // 
  static const int SRAM_LS_START = 0x008000;
  static const int SRAM_LS_END = 0x00BFFF;
  static const int SRAM_LS_SIZE = 16384;  // Local Shared RAM
  static const int SRAM_GS_START = 0x00C000;
  static const int SRAM_GS_END = 0x01FFFF;
  static const int SRAM_GS_SIZE = 81920;  // Global Shared RAM
  static const int PERIPHERAL_START = 0x400000;
  static const int PERIPHERAL_END = 0x40FFFF;
  static const int PERIPHERAL_SIZE = 65536;  // 

  // 外设定义
  // PLL Clock Control
  static const int PLL_BASE = 0x5C10;
  static const int PLL_SYSPLLCTL1_ADDR = 0x00;
  static const int PLL_SYSPLLCTL2_ADDR = 0x02;
  static const int PLL_CLKSRCCTL1_ADDR = 0x04;
  static const int PLL_CLKSRCCTL2_ADDR = 0x06;
  // GPIO Control Registers
  static const int GPIO_CTRL_BASE = 0x7C00;
  static const int GPIO_CTRL_GPACTRL_ADDR = 0x00;
  static const int GPIO_CTRL_GPAQSEL1_ADDR = 0x02;
  static const int GPIO_CTRL_GPAQSEL2_ADDR = 0x04;
  static const int GPIO_CTRL_GPAMUX1_ADDR = 0x06;
  static const int GPIO_CTRL_GPAMUX2_ADDR = 0x08;
  static const int GPIO_CTRL_GPADIR_ADDR = 0x0A;
  static const int GPIO_CTRL_GPAPUD_ADDR = 0x0C;
  // GPIO Data Registers
  static const int GPIO_DATA_BASE = 0x7F00;
  static const int GPIO_DATA_GPADAT_ADDR = 0x00;
  static const int GPIO_DATA_GPASET_ADDR = 0x02;
  static const int GPIO_DATA_GPACLEAR_ADDR = 0x04;
  static const int GPIO_DATA_GPATOGGLE_ADDR = 0x06;
  static const int GPIO_DATA_GPBDAT_ADDR = 0x08;
  static const int GPIO_DATA_GPBSET_ADDR = 0x0A;
  static const int GPIO_DATA_GPBCLEAR_ADDR = 0x0C;
  static const int GPIO_DATA_GPBTOGGLE_ADDR = 0x0E;
  // GPIO B Control
  static const int GPIO_B_CTRL_BASE = 0x7C20;
  static const int GPIO_B_CTRL_GPBMUX1_ADDR = 0x00;
  static const int GPIO_B_CTRL_GPBMUX2_ADDR = 0x02;
  static const int GPIO_B_CTRL_GPBDIR_ADDR = 0x04;
  static const int GPIO_B_CTRL_GPBPUD_ADDR = 0x06;
  // SCI-A UART
  static const int SCI_A_BASE = 0x7320;
  static const int SCI_A_SCICCR_ADDR = 0x00;
  static const int SCI_A_SCICTL1_ADDR = 0x02;
  static const int SCI_A_SCIBAUD_ADDR = 0x04;
  static const int SCI_A_SCIRXBUF_ADDR = 0x0A;
  static const int SCI_A_SCITXBUF_ADDR = 0x0C;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_SCIA_RX = 8;  // SCI-A Receive Interrupt
  static const int INT_SCIA_TX = 9;  // SCI-A Transmit Interrupt

}
