# SAMD21 设备定义 - Ruby 模块
# 生成自: Atmel (Microchip)/SAM D/SAMD21
# 版本: 
# 日期: 
# 作者: 
# 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
# CPU架构: ARM Cortex-M0+
# 位宽: 0位
# 时钟频率: 0 Hz

module SAMD21

  # 外设定义
  # Power Manager
  PM_BASE = 
  PM_PM_CTRL_ADDR = 0x40000400
  # System Controller
  SYSCTRL_BASE = 
  SYSCTRL_SYSCTRL_INTENCLR_ADDR = 0x40000800
  # Generic Clock Generator
  GCLK_BASE = 
  GCLK_GCLK_CTRL_ADDR = 0x40000C00
  # Watchdog Timer
  WDT_BASE = 
  WDT_WDT_CTRL_ADDR = 0x40001000
  # Real-Time Clock
  RTC_BASE = 
  RTC_RTC_CTRL_ADDR = 0x40001400
  # External Interrupt Controller
  EIC_BASE = 
  EIC_EIC_CTRL_ADDR = 0x40001800
  # Serial Communication Interface 0
  SERCOM0_BASE = 
  SERCOM0_SERCOM0_I2CM_CTRLA_ADDR = 0x42000800
  # Analog-to-Digital Converter
  ADC_BASE = 
  ADC_ADC_CTRLA_ADDR = 0x42002000
  # Digital-to-Analog Converter
  DAC_BASE = 
  DAC_DAC_CTRLA_ADDR = 0x42002400
  # General Purpose I/O
  PORT_BASE = 
  PORT_PORT_DIR_ADDR = 0x41004400
  # Timer/Counter 0
  TC0_BASE = 
  TC0_TC0_CTRLA_ADDR = 0x42002800
  # USB Device Controller
  USB_BASE = 
  USB_USB_CTRLA_ADDR = 0x41005000

  # 中断向量定义
  INT_RESET = 0  # Reset vector
  INT_NONMASKABLEINT = 1  # Non-maskable interrupt
  INT_HARDFAULT = 2  # Hard fault
  INT_SVCALL = 3  # Supervisor call
  INT_PENDSV = 4  # Pendable service call
  INT_SYSTICK = 5  # System tick timer
  INT_PM = 6  # Power Manager
  INT_SYSCTRL = 7  # System Controller
  INT_WDT = 8  # Watchdog Timer
  INT_RTC = 9  # Real-Time Clock
  INT_EIC = 10  # External Interrupt Controller
  INT_NVMCTRL = 11  # Non-Volatile Memory Controller
  INT_DMAC = 12  # Direct Memory Access Controller
  INT_USB = 13  # USB Device Controller
  INT_EVSYS = 14  # Event System
  INT_SERCOM0 = 15  # Serial Communication Interface 0
  INT_SERCOM1 = 16  # Serial Communication Interface 1
  INT_SERCOM2 = 17  # Serial Communication Interface 2
  INT_SERCOM3 = 18  # Serial Communication Interface 3
  INT_SERCOM4 = 19  # Serial Communication Interface 4
  INT_SERCOM5 = 20  # Serial Communication Interface 5
  INT_TCC0 = 21  # Timer/Counter for Control 0
  INT_TCC1 = 22  # Timer/Counter for Control 1
  INT_TCC2 = 23  # Timer/Counter for Control 2
  INT_TC3 = 24  # Timer/Counter 3
  INT_TC4 = 25  # Timer/Counter 4
  INT_TC5 = 26  # Timer/Counter 5
  INT_TC6 = 27  # Timer/Counter 6
  INT_TC7 = 28  # Timer/Counter 7
  INT_ADC = 29  # Analog-to-Digital Converter
  INT_AC = 30  # Analog Comparator
  INT_DAC = 31  # Digital-to-Analog Converter
  INT_PTC = 32  # Peripheral Touch Controller
  INT_I2S = 33  # Inter-IC Sound Interface

end
