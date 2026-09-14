! SAMD21 设备定义 - Fortran 模块
! 生成自: Atmel (Microchip)/SAM D/SAMD21
! 版本: 
! 日期: 
! 作者: 
! 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
! CPU架构: ARM Cortex-M0+
! 位宽: 0位
! 时钟频率: 0 Hz

module samd21_device
  implicit none

  ! 外设定义
  ! Power Manager
  integer, parameter :: PM_BASE = 
  integer, parameter :: PM_PM_CTRL_ADDR = 0x40000400
  ! System Controller
  integer, parameter :: SYSCTRL_BASE = 
  integer, parameter :: SYSCTRL_SYSCTRL_INTENCLR_ADDR = 0x40000800
  ! Generic Clock Generator
  integer, parameter :: GCLK_BASE = 
  integer, parameter :: GCLK_GCLK_CTRL_ADDR = 0x40000C00
  ! Watchdog Timer
  integer, parameter :: WDT_BASE = 
  integer, parameter :: WDT_WDT_CTRL_ADDR = 0x40001000
  ! Real-Time Clock
  integer, parameter :: RTC_BASE = 
  integer, parameter :: RTC_RTC_CTRL_ADDR = 0x40001400
  ! External Interrupt Controller
  integer, parameter :: EIC_BASE = 
  integer, parameter :: EIC_EIC_CTRL_ADDR = 0x40001800
  ! Serial Communication Interface 0
  integer, parameter :: SERCOM0_BASE = 
  integer, parameter :: SERCOM0_SERCOM0_I2CM_CTRLA_ADDR = 0x42000800
  ! Analog-to-Digital Converter
  integer, parameter :: ADC_BASE = 
  integer, parameter :: ADC_ADC_CTRLA_ADDR = 0x42002000
  ! Digital-to-Analog Converter
  integer, parameter :: DAC_BASE = 
  integer, parameter :: DAC_DAC_CTRLA_ADDR = 0x42002400
  ! General Purpose I/O
  integer, parameter :: PORT_BASE = 
  integer, parameter :: PORT_PORT_DIR_ADDR = 0x41004400
  ! Timer/Counter 0
  integer, parameter :: TC0_BASE = 
  integer, parameter :: TC0_TC0_CTRLA_ADDR = 0x42002800
  ! USB Device Controller
  integer, parameter :: USB_BASE = 
  integer, parameter :: USB_USB_CTRLA_ADDR = 0x41005000

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Reset vector
  integer, parameter :: INT_NONMASKABLEINT = 1  ! Non-maskable interrupt
  integer, parameter :: INT_HARDFAULT = 2  ! Hard fault
  integer, parameter :: INT_SVCALL = 3  ! Supervisor call
  integer, parameter :: INT_PENDSV = 4  ! Pendable service call
  integer, parameter :: INT_SYSTICK = 5  ! System tick timer
  integer, parameter :: INT_PM = 6  ! Power Manager
  integer, parameter :: INT_SYSCTRL = 7  ! System Controller
  integer, parameter :: INT_WDT = 8  ! Watchdog Timer
  integer, parameter :: INT_RTC = 9  ! Real-Time Clock
  integer, parameter :: INT_EIC = 10  ! External Interrupt Controller
  integer, parameter :: INT_NVMCTRL = 11  ! Non-Volatile Memory Controller
  integer, parameter :: INT_DMAC = 12  ! Direct Memory Access Controller
  integer, parameter :: INT_USB = 13  ! USB Device Controller
  integer, parameter :: INT_EVSYS = 14  ! Event System
  integer, parameter :: INT_SERCOM0 = 15  ! Serial Communication Interface 0
  integer, parameter :: INT_SERCOM1 = 16  ! Serial Communication Interface 1
  integer, parameter :: INT_SERCOM2 = 17  ! Serial Communication Interface 2
  integer, parameter :: INT_SERCOM3 = 18  ! Serial Communication Interface 3
  integer, parameter :: INT_SERCOM4 = 19  ! Serial Communication Interface 4
  integer, parameter :: INT_SERCOM5 = 20  ! Serial Communication Interface 5
  integer, parameter :: INT_TCC0 = 21  ! Timer/Counter for Control 0
  integer, parameter :: INT_TCC1 = 22  ! Timer/Counter for Control 1
  integer, parameter :: INT_TCC2 = 23  ! Timer/Counter for Control 2
  integer, parameter :: INT_TC3 = 24  ! Timer/Counter 3
  integer, parameter :: INT_TC4 = 25  ! Timer/Counter 4
  integer, parameter :: INT_TC5 = 26  ! Timer/Counter 5
  integer, parameter :: INT_TC6 = 27  ! Timer/Counter 6
  integer, parameter :: INT_TC7 = 28  ! Timer/Counter 7
  integer, parameter :: INT_ADC = 29  ! Analog-to-Digital Converter
  integer, parameter :: INT_AC = 30  ! Analog Comparator
  integer, parameter :: INT_DAC = 31  ! Digital-to-Analog Converter
  integer, parameter :: INT_PTC = 32  ! Peripheral Touch Controller
  integer, parameter :: INT_I2S = 33  ! Inter-IC Sound Interface

end module samd21_device
