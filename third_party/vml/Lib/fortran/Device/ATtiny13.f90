! ATtiny13 设备定义 - Fortran 模块
! 生成自: Atmel/AVR/ATtiny13
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
! CPU架构: AVR
! 位宽: 8位
! 时钟频率: 20000000 Hz

module attiny13_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x01  ! 
  integer, parameter :: R2_ADDR = 0x02  ! 
  integer, parameter :: R16_ADDR = 0x10  ! 
  integer, parameter :: R17_ADDR = 0x11  ! 
  integer, parameter :: R26_ADDR = 0x1A  ! XL
  integer, parameter :: R27_ADDR = 0x1B  ! XH
  integer, parameter :: R28_ADDR = 0x1C  ! YL
  integer, parameter :: R29_ADDR = 0x1D  ! YH
  integer, parameter :: R30_ADDR = 0x1E  ! ZL
  integer, parameter :: R31_ADDR = 0x1F  ! ZH
  integer, parameter :: SPL_ADDR = 0x5D  ! Stack Pointer Low
  integer, parameter :: SPH_ADDR = 0x5E  ! Stack Pointer High
  integer, parameter :: SREG_ADDR = 0x5F  ! Status Register

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x03FF
  integer, parameter :: FLASH_SIZE = 1024  ! 
  integer, parameter :: SRAM_START = 0x0060
  integer, parameter :: SRAM_END = 0x009F
  integer, parameter :: SRAM_SIZE = 64  ! 
  integer, parameter :: EEPROM_START = 0x0000
  integer, parameter :: EEPROM_END = 0x003F
  integer, parameter :: EEPROM_SIZE = 64  ! 
  integer, parameter :: IO_START = 0x00
  integer, parameter :: IO_END = 0x1F
  integer, parameter :: IO_SIZE = 32  ! 
  integer, parameter :: EXTIO_START = 0x20
  integer, parameter :: EXTIO_END = 0x5F
  integer, parameter :: EXTIO_SIZE = 64  ! 

  ! 外设定义
  ! Port B (only port)
  integer, parameter :: PORTB_BASE = 0x18
  integer, parameter :: PORTB_DDRB_ADDR = 0x17
  integer, parameter :: PORTB_PORTB_ADDR = 0x18
  integer, parameter :: PORTB_PINB_ADDR = 0x19
  ! 8-bit Timer/Counter0
  integer, parameter :: TIMER0_BASE = 0x33
  integer, parameter :: TIMER0_TCCR0A_ADDR = 0x33
  integer, parameter :: TIMER0_TCCR0B_ADDR = 0x33
  integer, parameter :: TIMER0_TCNT0_ADDR = 0x32
  integer, parameter :: TIMER0_OCR0A_ADDR = 0x36
  integer, parameter :: TIMER0_OCR0B_ADDR = 0x35
  integer, parameter :: TIMER0_TIMSK0_ADDR = 0x39
  integer, parameter :: TIMER0_TIFR0_ADDR = 0x38
  ! Analog-to-Digital
  integer, parameter :: ADC_BASE = 0x04
  integer, parameter :: ADC_ADMUX_ADDR = 0x07
  integer, parameter :: ADC_ADCSRA_ADDR = 0x06
  integer, parameter :: ADC_ADCL_ADDR = 0x04
  integer, parameter :: ADC_ADCH_ADDR = 0x05

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_INT0 = 2  ! External Interrupt 0
  integer, parameter :: INT_PCINT0 = 3  ! Pin Change Interrupt
  integer, parameter :: INT_TIM0_OVF = 4  ! Timer0 Overflow
  integer, parameter :: INT_TIM0_COMPA = 5  ! Timer0 Compare A
  integer, parameter :: INT_WDT = 6  ! Watchdog Timeout
  integer, parameter :: INT_ADC = 7  ! ADC Conversion Complete

end module attiny13_device
