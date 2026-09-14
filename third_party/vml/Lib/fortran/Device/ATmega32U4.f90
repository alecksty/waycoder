! ATmega32U4 设备定义 - Fortran 模块
! 生成自: Atmel/AVR/ATmega32U4
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
! CPU架构: AVR
! 位宽: 8位
! 时钟频率: 16000000 Hz

module atmega32u4_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x01  ! 
  integer, parameter :: R2_ADDR = 0x02  ! 
  integer, parameter :: R3_ADDR = 0x03  ! 
  integer, parameter :: R4_ADDR = 0x04  ! 
  integer, parameter :: R5_ADDR = 0x05  ! 
  integer, parameter :: R6_ADDR = 0x06  ! 
  integer, parameter :: R7_ADDR = 0x07  ! 
  integer, parameter :: R8_ADDR = 0x08  ! 
  integer, parameter :: R9_ADDR = 0x09  ! 
  integer, parameter :: R10_ADDR = 0x0A  ! 
  integer, parameter :: R11_ADDR = 0x0B  ! 
  integer, parameter :: R12_ADDR = 0x0C  ! 
  integer, parameter :: R13_ADDR = 0x0D  ! 
  integer, parameter :: R14_ADDR = 0x0E  ! 
  integer, parameter :: R15_ADDR = 0x0F  ! 
  integer, parameter :: R16_ADDR = 0x10  ! 
  integer, parameter :: R17_ADDR = 0x11  ! 
  integer, parameter :: R18_ADDR = 0x12  ! 
  integer, parameter :: R19_ADDR = 0x13  ! 
  integer, parameter :: R20_ADDR = 0x14  ! 
  integer, parameter :: R21_ADDR = 0x15  ! 
  integer, parameter :: R22_ADDR = 0x16  ! 
  integer, parameter :: R23_ADDR = 0x17  ! 
  integer, parameter :: R24_ADDR = 0x18  ! 
  integer, parameter :: R25_ADDR = 0x19  ! 
  integer, parameter :: R26_ADDR = 0x1A  ! 
  integer, parameter :: R27_ADDR = 0x1B  ! 
  integer, parameter :: R28_ADDR = 0x1C  ! 
  integer, parameter :: R29_ADDR = 0x1D  ! 
  integer, parameter :: R30_ADDR = 0x1E  ! 
  integer, parameter :: R31_ADDR = 0x1F  ! 
  integer, parameter :: SPL_ADDR = 0x5D  ! 
  integer, parameter :: SPH_ADDR = 0x5E  ! 
  integer, parameter :: SREG_ADDR = 0x5F  ! 

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x7FFF
  integer, parameter :: FLASH_SIZE = 32768  ! Program Flash Memory
  integer, parameter :: SRAM_START = 0x0100
  integer, parameter :: SRAM_END = 0x0AFF
  integer, parameter :: SRAM_SIZE = 2560  ! Static RAM
  integer, parameter :: EEPROM_START = 0x0000
  integer, parameter :: EEPROM_END = 0x03FF
  integer, parameter :: EEPROM_SIZE = 1024  ! EEPROM
  integer, parameter :: IO_START = 0x00
  integer, parameter :: IO_END = 0x3F
  integer, parameter :: IO_SIZE = 64  ! I/O Registers
  integer, parameter :: EXTIO_START = 0x40
  integer, parameter :: EXTIO_END = 0xFF
  integer, parameter :: EXTIO_SIZE = 192  ! Extended I/O Registers

  ! 外设定义
  ! Port B
  integer, parameter :: PORTB_BASE = 0x23
  integer, parameter :: PORTB_PORTB_ADDR = 0x25
  integer, parameter :: PORTB_DDRB_ADDR = 0x24
  integer, parameter :: PORTB_PINB_ADDR = 0x23
  ! Port C
  integer, parameter :: PORTC_BASE = 0x26
  integer, parameter :: PORTC_PORTC_ADDR = 0x28
  integer, parameter :: PORTC_DDRC_ADDR = 0x27
  integer, parameter :: PORTC_PINC_ADDR = 0x26
  ! Port D
  integer, parameter :: PORTD_BASE = 0x29
  integer, parameter :: PORTD_PORTD_ADDR = 0x2B
  integer, parameter :: PORTD_DDRD_ADDR = 0x2A
  integer, parameter :: PORTD_PIND_ADDR = 0x29
  ! Port E
  integer, parameter :: PORTE_BASE = 0x2C
  integer, parameter :: PORTE_PORTE_ADDR = 0x2E
  integer, parameter :: PORTE_DDRE_ADDR = 0x2D
  integer, parameter :: PORTE_PINE_ADDR = 0x2C
  ! USART1
  integer, parameter :: UART1_BASE = 0xC8
  integer, parameter :: UART1_UDR1_ADDR = 0xCE
  integer, parameter :: UART1_UCSR1A_ADDR = 0xC8
  integer, parameter :: UART1_UCSR1B_ADDR = 0xC9
  integer, parameter :: UART1_UCSR1C_ADDR = 0xCA
  integer, parameter :: UART1_UBRR1_ADDR = 0xCC
  ! USB Controller
  integer, parameter :: USB_BASE = 0xD0
  integer, parameter :: USB_UDCON_ADDR = 0xD0
  integer, parameter :: USB_UDIEN_ADDR = 0xD1
  integer, parameter :: USB_UDINT_ADDR = 0xD2

  ! 中断向量定义
  integer, parameter :: INT_INT0 = 1  ! External Interrupt 0
  integer, parameter :: INT_INT1 = 2  ! External Interrupt 1
  integer, parameter :: INT_INT2 = 3  ! External Interrupt 2
  integer, parameter :: INT_INT3 = 4  ! External Interrupt 3
  integer, parameter :: INT_INT4 = 5  ! External Interrupt 4
  integer, parameter :: INT_INT5 = 6  ! External Interrupt 5
  integer, parameter :: INT_INT6 = 7  ! External Interrupt 6
  integer, parameter :: INT_PCINT0 = 8  ! Pin Change Interrupt 0
  integer, parameter :: INT_USB_GENERAL = 9  ! USB General
  integer, parameter :: INT_USB_ENDPOINT = 10  ! USB Endpoint
  integer, parameter :: INT_WDT = 11  ! Watchdog Timeout
  integer, parameter :: INT_TIMER1_CAPT = 12  ! Timer1 Capture
  integer, parameter :: INT_TIMER1_COMPA = 13  ! Timer1 Compare A
  integer, parameter :: INT_TIMER1_COMPB = 14  ! Timer1 Compare B
  integer, parameter :: INT_TIMER1_OVF = 15  ! Timer1 Overflow
  integer, parameter :: INT_TIMER0_COMPA = 16  ! Timer0 Compare A
  integer, parameter :: INT_TIMER0_COMPB = 17  ! Timer0 Compare B
  integer, parameter :: INT_TIMER0_OVF = 18  ! Timer0 Overflow
  integer, parameter :: INT_SPI_STC = 19  ! SPI Transfer Complete
  integer, parameter :: INT_UART1_RX = 20  ! UART1 Receive
  integer, parameter :: INT_UART1_UDRE = 21  ! UART1 Data Register Empty
  integer, parameter :: INT_UART1_TX = 22  ! UART1 Transmit
  integer, parameter :: INT_ADC = 23  ! ADC Conversion Complete

end module atmega32u4_device
