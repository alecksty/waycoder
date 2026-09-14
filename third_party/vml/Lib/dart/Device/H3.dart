// Allwinner H3 设备定义 - Dart 库
// 生成自: Allwinner/H-Series/Allwinner H3
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
// CPU架构: ARM-Cortex-A7
// 位宽: 32位
// 时钟频率: 1200000000 Hz

class Allwinner H3Device {
  static const String deviceName = "Allwinner H3";
  static const String manufacturer = "Allwinner";
  static const String family = "H-Series";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-A7";
  static const int bits = 32;
  static const int clockFrequency = 1200000000;

  // 外设定义
  // UART 0 (debug console)
  static const int UART0_BASE = 0x01C28000;
  static const int UART0_RBR_ADDR = 0x00;
  static const int UART0_THR_ADDR = 0x00;
  static const int UART0_IER_ADDR = 0x04;
  static const int UART0_IIR_ADDR = 0x08;
  static const int UART0_FCR_ADDR = 0x08;
  static const int UART0_LCR_ADDR = 0x0C;
  static const int UART0_MCR_ADDR = 0x10;
  static const int UART0_LSR_ADDR = 0x14;
  static const int UART0_MSR_ADDR = 0x18;
  static const int UART0_DLL_ADDR = 0x00;
  static const int UART0_DLH_ADDR = 0x04;
  // UART 1
  static const int UART1_BASE = 0x01C28400;
  static const int UART1_RBR_ADDR = 0x00;
  static const int UART1_THR_ADDR = 0x00;
  static const int UART1_LSR_ADDR = 0x14;
  // GPIO 控制器
  static const int GPIO_BASE = 0x01C20800;
  static const int GPIO_PA_CFG0_ADDR = 0x00;
  static const int GPIO_PA_CFG1_ADDR = 0x04;
  static const int GPIO_PA_DAT_ADDR = 0x10;
  static const int GPIO_PA_DRV0_ADDR = 0x14;
  static const int GPIO_PA_PUL0_ADDR = 0x1C;
  static const int GPIO_PB_CFG0_ADDR = 0x24;
  static const int GPIO_PB_DAT_ADDR = 0x34;
  static const int GPIO_PC_CFG0_ADDR = 0x48;
  static const int GPIO_PC_DAT_ADDR = 0x58;
  // AVS 定时器
  static const int TIMER_BASE = 0x01C20C00;
  static const int TIMER_CNT0_ADDR = 0x00;
  static const int TIMER_CNT1_ADDR = 0x04;
  static const int TIMER_CTRL_ADDR = 0x08;
  static const int TIMER_INTV_ADDR = 0x0C;
  // 时钟控制单元
  static const int CCU_BASE = 0x01C20000;
  static const int CCU_PLL1_CFG_ADDR = 0x000;
  static const int CCU_PLL3_CFG_ADDR = 0x010;
  static const int CCU_CPU_AXI_CFG_ADDR = 0x050;
  static const int CCU_AHB1_APB1_CFG_ADDR = 0x054;
  static const int CCU_APB2_CFG_ADDR = 0x058;
  static const int CCU_BUS_GATE0_ADDR = 0x060;
  static const int CCU_BUS_GATE1_ADDR = 0x064;
  static const int CCU_BUS_GATE2_ADDR = 0x068;

}
