# ATmega32U4 设备定义 - Ruby 模块
# 生成自: Atmel/AVR/ATmega32U4
# 版本: 1.0
# 日期: 2026-04-28
# 作者: VML Team
# 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
# CPU架构: AVR
# 位宽: 8位
# 时钟频率: 16000000 Hz

module ATmega32U4

  # 寄存器地址定义
  R0_ADDR = 0x00  # 
  R1_ADDR = 0x01  # 
  R2_ADDR = 0x02  # 
  R3_ADDR = 0x03  # 
  R4_ADDR = 0x04  # 
  R5_ADDR = 0x05  # 
  R6_ADDR = 0x06  # 
  R7_ADDR = 0x07  # 
  R8_ADDR = 0x08  # 
  R9_ADDR = 0x09  # 
  R10_ADDR = 0x0A  # 
  R11_ADDR = 0x0B  # 
  R12_ADDR = 0x0C  # 
  R13_ADDR = 0x0D  # 
  R14_ADDR = 0x0E  # 
  R15_ADDR = 0x0F  # 
  R16_ADDR = 0x10  # 
  R17_ADDR = 0x11  # 
  R18_ADDR = 0x12  # 
  R19_ADDR = 0x13  # 
  R20_ADDR = 0x14  # 
  R21_ADDR = 0x15  # 
  R22_ADDR = 0x16  # 
  R23_ADDR = 0x17  # 
  R24_ADDR = 0x18  # 
  R25_ADDR = 0x19  # 
  R26_ADDR = 0x1A  # 
  R27_ADDR = 0x1B  # 
  R28_ADDR = 0x1C  # 
  R29_ADDR = 0x1D  # 
  R30_ADDR = 0x1E  # 
  R31_ADDR = 0x1F  # 
  SPL_ADDR = 0x5D  # 
  SPH_ADDR = 0x5E  # 
  SREG_ADDR = 0x5F  # 

  # 内存段定义
  FLASH_START = 0x0000
  FLASH_END = 0x7FFF
  FLASH_SIZE = 32768  # Program Flash Memory
  SRAM_START = 0x0100
  SRAM_END = 0x0AFF
  SRAM_SIZE = 2560  # Static RAM
  EEPROM_START = 0x0000
  EEPROM_END = 0x03FF
  EEPROM_SIZE = 1024  # EEPROM
  IO_START = 0x00
  IO_END = 0x3F
  IO_SIZE = 64  # I/O Registers
  EXTIO_START = 0x40
  EXTIO_END = 0xFF
  EXTIO_SIZE = 192  # Extended I/O Registers

  # 外设定义
  # Port B
  PORTB_BASE = 0x23
  PORTB_PORTB_ADDR = 0x25
  PORTB_DDRB_ADDR = 0x24
  PORTB_PINB_ADDR = 0x23
  # Port C
  PORTC_BASE = 0x26
  PORTC_PORTC_ADDR = 0x28
  PORTC_DDRC_ADDR = 0x27
  PORTC_PINC_ADDR = 0x26
  # Port D
  PORTD_BASE = 0x29
  PORTD_PORTD_ADDR = 0x2B
  PORTD_DDRD_ADDR = 0x2A
  PORTD_PIND_ADDR = 0x29
  # Port E
  PORTE_BASE = 0x2C
  PORTE_PORTE_ADDR = 0x2E
  PORTE_DDRE_ADDR = 0x2D
  PORTE_PINE_ADDR = 0x2C
  # USART1
  UART1_BASE = 0xC8
  UART1_UDR1_ADDR = 0xCE
  UART1_UCSR1A_ADDR = 0xC8
  UART1_UCSR1B_ADDR = 0xC9
  UART1_UCSR1C_ADDR = 0xCA
  UART1_UBRR1_ADDR = 0xCC
  # USB Controller
  USB_BASE = 0xD0
  USB_UDCON_ADDR = 0xD0
  USB_UDIEN_ADDR = 0xD1
  USB_UDINT_ADDR = 0xD2

  # 中断向量定义
  INT_INT0 = 1  # External Interrupt 0
  INT_INT1 = 2  # External Interrupt 1
  INT_INT2 = 3  # External Interrupt 2
  INT_INT3 = 4  # External Interrupt 3
  INT_INT4 = 5  # External Interrupt 4
  INT_INT5 = 6  # External Interrupt 5
  INT_INT6 = 7  # External Interrupt 6
  INT_PCINT0 = 8  # Pin Change Interrupt 0
  INT_USB_GENERAL = 9  # USB General
  INT_USB_ENDPOINT = 10  # USB Endpoint
  INT_WDT = 11  # Watchdog Timeout
  INT_TIMER1_CAPT = 12  # Timer1 Capture
  INT_TIMER1_COMPA = 13  # Timer1 Compare A
  INT_TIMER1_COMPB = 14  # Timer1 Compare B
  INT_TIMER1_OVF = 15  # Timer1 Overflow
  INT_TIMER0_COMPA = 16  # Timer0 Compare A
  INT_TIMER0_COMPB = 17  # Timer0 Compare B
  INT_TIMER0_OVF = 18  # Timer0 Overflow
  INT_SPI_STC = 19  # SPI Transfer Complete
  INT_UART1_RX = 20  # UART1 Receive
  INT_UART1_UDRE = 21  # UART1 Data Register Empty
  INT_UART1_TX = 22  # UART1 Transmit
  INT_ADC = 23  # ADC Conversion Complete

end
