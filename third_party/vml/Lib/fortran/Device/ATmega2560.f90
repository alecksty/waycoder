! ATmega2560 设备定义 - Fortran 模块
! 生成自: Atmel/AVR/ATmega2560
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
! CPU架构: AVR
! 位宽: 8位
! 时钟频率: 16000000 Hz

module atmega2560_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x01  ! 
  integer, parameter :: R2_ADDR = 0x02  ! 
  integer, parameter :: SPL_ADDR = 0x5D  ! 
  integer, parameter :: SPH_ADDR = 0x5E  ! 
  integer, parameter :: SREG_ADDR = 0x5F  ! 

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x3FFFF
  integer, parameter :: FLASH_SIZE = 262144  ! 
  integer, parameter :: SRAM_START = 0x0200
  integer, parameter :: SRAM_END = 0x21FF
  integer, parameter :: SRAM_SIZE = 8192  ! 
  integer, parameter :: EEPROM_START = 0x0000
  integer, parameter :: EEPROM_END = 0x0FFF
  integer, parameter :: EEPROM_SIZE = 4096  ! 
  integer, parameter :: IO_START = 0x00
  integer, parameter :: IO_END = 0x3F
  integer, parameter :: IO_SIZE = 64  ! 
  integer, parameter :: EXTIO_START = 0x40
  integer, parameter :: EXTIO_END = 0xFF
  integer, parameter :: EXTIO_SIZE = 192  ! 

  ! 外设定义
  ! Port A
  integer, parameter :: PORTA_BASE = 0x22
  integer, parameter :: PORTA_DDRA_ADDR = 0x21
  integer, parameter :: PORTA_PORTA_ADDR = 0x22
  integer, parameter :: PORTA_PINA_ADDR = 0x20
  ! Port B
  integer, parameter :: PORTB_BASE = 0x25
  integer, parameter :: PORTB_DDRB_ADDR = 0x24
  integer, parameter :: PORTB_PORTB_ADDR = 0x25
  integer, parameter :: PORTB_PINB_ADDR = 0x23
  ! Port C
  integer, parameter :: PORTC_BASE = 0x28
  integer, parameter :: PORTC_DDRC_ADDR = 0x27
  integer, parameter :: PORTC_PORTC_ADDR = 0x28
  integer, parameter :: PORTC_PINC_ADDR = 0x26
  ! Port D
  integer, parameter :: PORTD_BASE = 0x2B
  integer, parameter :: PORTD_DDRD_ADDR = 0x2A
  integer, parameter :: PORTD_PORTD_ADDR = 0x2B
  integer, parameter :: PORTD_PIND_ADDR = 0x29
  ! Port E
  integer, parameter :: PORTE_BASE = 0x2E
  integer, parameter :: PORTE_DDRE_ADDR = 0x2D
  integer, parameter :: PORTE_PORTE_ADDR = 0x2E
  integer, parameter :: PORTE_PINE_ADDR = 0x2C
  ! Port F
  integer, parameter :: PORTF_BASE = 0x31
  integer, parameter :: PORTF_DDRF_ADDR = 0x30
  integer, parameter :: PORTF_PORTF_ADDR = 0x31
  integer, parameter :: PORTF_PINF_ADDR = 0x2F
  ! Port G
  integer, parameter :: PORTG_BASE = 0x34
  integer, parameter :: PORTG_DDRG_ADDR = 0x33
  integer, parameter :: PORTG_PORTG_ADDR = 0x34
  integer, parameter :: PORTG_PING_ADDR = 0x32
  ! USART 0
  integer, parameter :: USART0_BASE = 0xC0
  integer, parameter :: USART0_UDR0_ADDR = 0xC6
  integer, parameter :: USART0_UCSR0A_ADDR = 0xC0
  integer, parameter :: USART0_UCSR0B_ADDR = 0xC1
  integer, parameter :: USART0_UCSR0C_ADDR = 0xC2
  integer, parameter :: USART0_UBRR0L_ADDR = 0xC4
  integer, parameter :: USART0_UBRR0H_ADDR = 0xC5

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_INT0 = 2  ! 
  integer, parameter :: INT_INT1 = 3  ! 
  integer, parameter :: INT_INT2 = 4  ! 
  integer, parameter :: INT_INT3 = 5  ! 
  integer, parameter :: INT_INT4 = 6  ! 
  integer, parameter :: INT_INT5 = 7  ! 
  integer, parameter :: INT_INT6 = 8  ! 
  integer, parameter :: INT_INT7 = 9  ! 
  integer, parameter :: INT_PCINT0 = 10  ! 
  integer, parameter :: INT_PCINT1 = 11  ! 
  integer, parameter :: INT_PCINT2 = 12  ! 
  integer, parameter :: INT_WDT = 13  ! 
  integer, parameter :: INT_TIM2_COMPA = 14  ! 
  integer, parameter :: INT_TIM2_COMPB = 15  ! 
  integer, parameter :: INT_TIM2_OVF = 16  ! 
  integer, parameter :: INT_TIM1_CAPT = 17  ! 
  integer, parameter :: INT_TIM1_COMPA = 18  ! 
  integer, parameter :: INT_TIM1_COMPB = 19  ! 
  integer, parameter :: INT_TIM1_OVF = 20  ! 
  integer, parameter :: INT_TIM0_COMPA = 21  ! 
  integer, parameter :: INT_TIM0_COMPB = 22  ! 
  integer, parameter :: INT_TIM0_OVF = 23  ! 
  integer, parameter :: INT_SPI_STC = 24  ! 
  integer, parameter :: INT_USART0_RX = 25  ! 
  integer, parameter :: INT_USART0_UDRE = 26  ! 
  integer, parameter :: INT_USART0_TX = 27  ! 

end module atmega2560_device
