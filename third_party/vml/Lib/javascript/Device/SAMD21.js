/**
 * SAMD21 寄存器定义
 * 生成自: Atmel (Microchip)/SAM D/SAMD21
 * 版本: 
 */
export const samd21 = {
  // CPU: ARM Cortex-M0+, 0位, 0 Hz

  // 外设定义
  // Power Manager
  PM_BASE: ,
  PM_PM_CTRL: 0x40000400,
  // System Controller
  SYSCTRL_BASE: ,
  SYSCTRL_SYSCTRL_INTENCLR: 0x40000800,
  // Generic Clock Generator
  GCLK_BASE: ,
  GCLK_GCLK_CTRL: 0x40000C00,
  // Watchdog Timer
  WDT_BASE: ,
  WDT_WDT_CTRL: 0x40001000,
  // Real-Time Clock
  RTC_BASE: ,
  RTC_RTC_CTRL: 0x40001400,
  // External Interrupt Controller
  EIC_BASE: ,
  EIC_EIC_CTRL: 0x40001800,
  // Serial Communication Interface 0
  SERCOM0_BASE: ,
  SERCOM0_SERCOM0_I2CM_CTRLA: 0x42000800,
  // Analog-to-Digital Converter
  ADC_BASE: ,
  ADC_ADC_CTRLA: 0x42002000,
  // Digital-to-Analog Converter
  DAC_BASE: ,
  DAC_DAC_CTRLA: 0x42002400,
  // General Purpose I/O
  PORT_BASE: ,
  PORT_PORT_DIR: 0x41004400,
  // Timer/Counter 0
  TC0_BASE: ,
  TC0_TC0_CTRLA: 0x42002800,
  // USB Device Controller
  USB_BASE: ,
  USB_USB_CTRLA: 0x41005000,

  // 中断向量
  IRQ_Reset: 0,  // Reset vector
  IRQ_NonMaskableInt: 1,  // Non-maskable interrupt
  IRQ_HardFault: 2,  // Hard fault
  IRQ_SVCall: 3,  // Supervisor call
  IRQ_PendSV: 4,  // Pendable service call
  IRQ_SysTick: 5,  // System tick timer
  IRQ_PM: 6,  // Power Manager
  IRQ_SYSCTRL: 7,  // System Controller
  IRQ_WDT: 8,  // Watchdog Timer
  IRQ_RTC: 9,  // Real-Time Clock
  IRQ_EIC: 10,  // External Interrupt Controller
  IRQ_NVMCTRL: 11,  // Non-Volatile Memory Controller
  IRQ_DMAC: 12,  // Direct Memory Access Controller
  IRQ_USB: 13,  // USB Device Controller
  IRQ_EVSYS: 14,  // Event System
  IRQ_SERCOM0: 15,  // Serial Communication Interface 0
  IRQ_SERCOM1: 16,  // Serial Communication Interface 1
  IRQ_SERCOM2: 17,  // Serial Communication Interface 2
  IRQ_SERCOM3: 18,  // Serial Communication Interface 3
  IRQ_SERCOM4: 19,  // Serial Communication Interface 4
  IRQ_SERCOM5: 20,  // Serial Communication Interface 5
  IRQ_TCC0: 21,  // Timer/Counter for Control 0
  IRQ_TCC1: 22,  // Timer/Counter for Control 1
  IRQ_TCC2: 23,  // Timer/Counter for Control 2
  IRQ_TC3: 24,  // Timer/Counter 3
  IRQ_TC4: 25,  // Timer/Counter 4
  IRQ_TC5: 26,  // Timer/Counter 5
  IRQ_TC6: 27,  // Timer/Counter 6
  IRQ_TC7: 28,  // Timer/Counter 7
  IRQ_ADC: 29,  // Analog-to-Digital Converter
  IRQ_AC: 30,  // Analog Comparator
  IRQ_DAC: 31,  // Digital-to-Analog Converter
  IRQ_PTC: 32,  // Peripheral Touch Controller
  IRQ_I2S: 33,  // Inter-IC Sound Interface

  init: function() {
    // 硬件初始化
  }
};
