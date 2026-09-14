// SAMD21 设备定义 - Dart 库
// 生成自: Atmel (Microchip)/SAM D/SAMD21
// 版本: 
// 日期: 
// 作者: 
// 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
// CPU架构: ARM Cortex-M0+
// 位宽: 0位
// 时钟频率: 0 Hz

class SAMD21Device {
  static const String deviceName = "SAMD21";
  static const String manufacturer = "Atmel (Microchip)";
  static const String family = "SAM D";
  static const String version = "";
  static const String architecture = "ARM Cortex-M0+";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // Power Manager
  static const int PM_BASE = ;
  static const int PM_PM_CTRL_ADDR = 0x40000400;
  // System Controller
  static const int SYSCTRL_BASE = ;
  static const int SYSCTRL_SYSCTRL_INTENCLR_ADDR = 0x40000800;
  // Generic Clock Generator
  static const int GCLK_BASE = ;
  static const int GCLK_GCLK_CTRL_ADDR = 0x40000C00;
  // Watchdog Timer
  static const int WDT_BASE = ;
  static const int WDT_WDT_CTRL_ADDR = 0x40001000;
  // Real-Time Clock
  static const int RTC_BASE = ;
  static const int RTC_RTC_CTRL_ADDR = 0x40001400;
  // External Interrupt Controller
  static const int EIC_BASE = ;
  static const int EIC_EIC_CTRL_ADDR = 0x40001800;
  // Serial Communication Interface 0
  static const int SERCOM0_BASE = ;
  static const int SERCOM0_SERCOM0_I2CM_CTRLA_ADDR = 0x42000800;
  // Analog-to-Digital Converter
  static const int ADC_BASE = ;
  static const int ADC_ADC_CTRLA_ADDR = 0x42002000;
  // Digital-to-Analog Converter
  static const int DAC_BASE = ;
  static const int DAC_DAC_CTRLA_ADDR = 0x42002400;
  // General Purpose I/O
  static const int PORT_BASE = ;
  static const int PORT_PORT_DIR_ADDR = 0x41004400;
  // Timer/Counter 0
  static const int TC0_BASE = ;
  static const int TC0_TC0_CTRLA_ADDR = 0x42002800;
  // USB Device Controller
  static const int USB_BASE = ;
  static const int USB_USB_CTRLA_ADDR = 0x41005000;

  // 中断向量定义
  static const int INT_RESET = 0;  // Reset vector
  static const int INT_NONMASKABLEINT = 1;  // Non-maskable interrupt
  static const int INT_HARDFAULT = 2;  // Hard fault
  static const int INT_SVCALL = 3;  // Supervisor call
  static const int INT_PENDSV = 4;  // Pendable service call
  static const int INT_SYSTICK = 5;  // System tick timer
  static const int INT_PM = 6;  // Power Manager
  static const int INT_SYSCTRL = 7;  // System Controller
  static const int INT_WDT = 8;  // Watchdog Timer
  static const int INT_RTC = 9;  // Real-Time Clock
  static const int INT_EIC = 10;  // External Interrupt Controller
  static const int INT_NVMCTRL = 11;  // Non-Volatile Memory Controller
  static const int INT_DMAC = 12;  // Direct Memory Access Controller
  static const int INT_USB = 13;  // USB Device Controller
  static const int INT_EVSYS = 14;  // Event System
  static const int INT_SERCOM0 = 15;  // Serial Communication Interface 0
  static const int INT_SERCOM1 = 16;  // Serial Communication Interface 1
  static const int INT_SERCOM2 = 17;  // Serial Communication Interface 2
  static const int INT_SERCOM3 = 18;  // Serial Communication Interface 3
  static const int INT_SERCOM4 = 19;  // Serial Communication Interface 4
  static const int INT_SERCOM5 = 20;  // Serial Communication Interface 5
  static const int INT_TCC0 = 21;  // Timer/Counter for Control 0
  static const int INT_TCC1 = 22;  // Timer/Counter for Control 1
  static const int INT_TCC2 = 23;  // Timer/Counter for Control 2
  static const int INT_TC3 = 24;  // Timer/Counter 3
  static const int INT_TC4 = 25;  // Timer/Counter 4
  static const int INT_TC5 = 26;  // Timer/Counter 5
  static const int INT_TC6 = 27;  // Timer/Counter 6
  static const int INT_TC7 = 28;  // Timer/Counter 7
  static const int INT_ADC = 29;  // Analog-to-Digital Converter
  static const int INT_AC = 30;  // Analog Comparator
  static const int INT_DAC = 31;  // Digital-to-Analog Converter
  static const int INT_PTC = 32;  // Peripheral Touch Controller
  static const int INT_I2S = 33;  // Inter-IC Sound Interface

}
