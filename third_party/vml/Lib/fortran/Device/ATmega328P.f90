! ATmega328P 设备定义 - Fortran 模块
! 生成自: Atmel/AVR/ATmega328P
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
! CPU架构: AVR
! 位宽: 8位
! 时钟频率: 16000000 Hz

module atmega328p_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! General Purpose Register 0
  integer, parameter :: R1_ADDR = 0x01  ! General Purpose Register 1
  integer, parameter :: R2_ADDR = 0x02  ! General Purpose Register 2
  integer, parameter :: R3_ADDR = 0x03  ! General Purpose Register 3
  integer, parameter :: R4_ADDR = 0x04  ! General Purpose Register 4
  integer, parameter :: R5_ADDR = 0x05  ! General Purpose Register 5
  integer, parameter :: R6_ADDR = 0x06  ! General Purpose Register 6
  integer, parameter :: R7_ADDR = 0x07  ! General Purpose Register 7
  integer, parameter :: R8_ADDR = 0x08  ! General Purpose Register 8
  integer, parameter :: R9_ADDR = 0x09  ! General Purpose Register 9
  integer, parameter :: R10_ADDR = 0x0A  ! General Purpose Register 10
  integer, parameter :: R11_ADDR = 0x0B  ! General Purpose Register 11
  integer, parameter :: R12_ADDR = 0x0C  ! General Purpose Register 12
  integer, parameter :: R13_ADDR = 0x0D  ! General Purpose Register 13
  integer, parameter :: R14_ADDR = 0x0E  ! General Purpose Register 14
  integer, parameter :: R15_ADDR = 0x0F  ! General Purpose Register 15
  integer, parameter :: R16_ADDR = 0x10  ! General Purpose Register 16
  integer, parameter :: R17_ADDR = 0x11  ! General Purpose Register 17
  integer, parameter :: R18_ADDR = 0x12  ! General Purpose Register 18
  integer, parameter :: R19_ADDR = 0x13  ! General Purpose Register 19
  integer, parameter :: R20_ADDR = 0x14  ! General Purpose Register 20
  integer, parameter :: R21_ADDR = 0x15  ! General Purpose Register 21
  integer, parameter :: R22_ADDR = 0x16  ! General Purpose Register 22
  integer, parameter :: R23_ADDR = 0x17  ! General Purpose Register 23
  integer, parameter :: R24_ADDR = 0x18  ! General Purpose Register 24
  integer, parameter :: R25_ADDR = 0x19  ! General Purpose Register 25
  integer, parameter :: R26_ADDR = 0x1A  ! General Purpose Register 26 (XL)
  integer, parameter :: R27_ADDR = 0x1B  ! General Purpose Register 27 (XH)
  integer, parameter :: R28_ADDR = 0x1C  ! General Purpose Register 28 (YL)
  integer, parameter :: R29_ADDR = 0x1D  ! General Purpose Register 29 (YH)
  integer, parameter :: R30_ADDR = 0x1E  ! General Purpose Register 30 (ZL)
  integer, parameter :: R31_ADDR = 0x1F  ! General Purpose Register 31 (ZH)
  integer, parameter :: SPL_ADDR = 0x5D  ! Stack Pointer Low
  integer, parameter :: SPH_ADDR = 0x5E  ! Stack Pointer High
  integer, parameter :: SREG_ADDR = 0x5F  ! Status Register
  integer, parameter :: SREG_C_BIT = 0  ! Carry Flag
  integer, parameter :: SREG_Z_BIT = 1  ! Zero Flag
  integer, parameter :: SREG_N_BIT = 2  ! Negative Flag
  integer, parameter :: SREG_V_BIT = 3  ! Two's Complement Overflow Flag
  integer, parameter :: SREG_S_BIT = 4  ! Sign Flag (N ⊕ V)
  integer, parameter :: SREG_H_BIT = 5  ! Half Carry Flag
  integer, parameter :: SREG_T_BIT = 6  ! Transfer Bit
  integer, parameter :: SREG_I_BIT = 7  ! Global Interrupt Enable

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x7FFF
  integer, parameter :: FLASH_SIZE = 32768  ! Program Flash Memory
  integer, parameter :: SRAM_START = 0x0100
  integer, parameter :: SRAM_END = 0x08FF
  integer, parameter :: SRAM_SIZE = 2048  ! Static RAM
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
  ! Port B Data Register
  integer, parameter :: PORTB_BASE = 0x23
  integer, parameter :: PORTB_PORTB_ADDR = 0x25
  integer, parameter :: PORTB_DDRB_ADDR = 0x24
  integer, parameter :: PORTB_PINB_ADDR = 0x23
  ! Port C Data Register
  integer, parameter :: PORTC_BASE = 0x26
  integer, parameter :: PORTC_PORTC_ADDR = 0x28
  integer, parameter :: PORTC_DDRC_ADDR = 0x27
  integer, parameter :: PORTC_PINC_ADDR = 0x26
  ! Port D Data Register
  integer, parameter :: PORTD_BASE = 0x29
  integer, parameter :: PORTD_PORTD_ADDR = 0x2B
  integer, parameter :: PORTD_DDRD_ADDR = 0x2A
  integer, parameter :: PORTD_PIND_ADDR = 0x29
  ! 8-bit Timer/Counter0
  integer, parameter :: TIMER0_BASE = 0x44
  integer, parameter :: TIMER0_TCCR0A_ADDR = 0x44
  integer, parameter :: TIMER0_TCCR0A_WGM00_BIT = 0  ! Waveform Generation Mode
  integer, parameter :: TIMER0_TCCR0A_WGM01_BIT = 1  ! Waveform Generation Mode
  integer, parameter :: TIMER0_TCCR0A_COM0B0_BIT = 4  ! Compare Output Mode for Channel B
  integer, parameter :: TIMER0_TCCR0A_COM0B1_BIT = 5  ! Compare Output Mode for Channel B
  integer, parameter :: TIMER0_TCCR0A_COM0A0_BIT = 6  ! Compare Output Mode for Channel A
  integer, parameter :: TIMER0_TCCR0A_COM0A1_BIT = 7  ! Compare Output Mode for Channel A
  integer, parameter :: TIMER0_TCCR0B_ADDR = 0x45
  integer, parameter :: TIMER0_TCCR0B_CS00_BIT = 0  ! Clock Select
  integer, parameter :: TIMER0_TCCR0B_CS01_BIT = 1  ! Clock Select
  integer, parameter :: TIMER0_TCCR0B_CS02_BIT = 2  ! Clock Select
  integer, parameter :: TIMER0_TCCR0B_WGM02_BIT = 3  ! Waveform Generation Mode
  integer, parameter :: TIMER0_TCCR0B_FOC0B_BIT = 6  ! Force Output Compare B
  integer, parameter :: TIMER0_TCCR0B_FOC0A_BIT = 7  ! Force Output Compare A
  integer, parameter :: TIMER0_TCNT0_ADDR = 0x46
  integer, parameter :: TIMER0_OCR0A_ADDR = 0x47
  integer, parameter :: TIMER0_OCR0B_ADDR = 0x48
  integer, parameter :: TIMER0_TIMSK0_ADDR = 0x6E
  integer, parameter :: TIMER0_TIMSK0_TOIE0_BIT = 0  ! Timer/Counter0 Overflow Interrupt Enable
  integer, parameter :: TIMER0_TIMSK0_OCIE0A_BIT = 1  ! Timer/Counter0 Output Compare A Match Interrupt Enable
  integer, parameter :: TIMER0_TIMSK0_OCIE0B_BIT = 2  ! Timer/Counter0 Output Compare B Match Interrupt Enable
  integer, parameter :: TIMER0_TIFR0_ADDR = 0x35
  integer, parameter :: TIMER0_TIFR0_TOV0_BIT = 0  ! Timer/Counter0 Overflow Flag
  integer, parameter :: TIMER0_TIFR0_OCF0A_BIT = 1  ! Output Compare Flag 0A
  integer, parameter :: TIMER0_TIFR0_OCF0B_BIT = 2  ! Output Compare Flag 0B
  ! Universal Synchronous/Asynchronous Receiver/Transmitter
  integer, parameter :: USART0_BASE = 0xC0
  integer, parameter :: USART0_UDR0_ADDR = 0xC6
  integer, parameter :: USART0_UCSR0A_ADDR = 0xC0
  integer, parameter :: USART0_UCSR0A_MPCM0_BIT = 0  ! Multi-processor Communication Mode
  integer, parameter :: USART0_UCSR0A_U2X0_BIT = 1  ! Double the USART Transmission Speed
  integer, parameter :: USART0_UCSR0A_UPE0_BIT = 2  ! Parity Error
  integer, parameter :: USART0_UCSR0A_DOR0_BIT = 3  ! Data OverRun
  integer, parameter :: USART0_UCSR0A_FE0_BIT = 4  ! Frame Error
  integer, parameter :: USART0_UCSR0A_UDRE0_BIT = 5  ! USART Data Register Empty
  integer, parameter :: USART0_UCSR0A_TXC0_BIT = 6  ! USART Transmit Complete
  integer, parameter :: USART0_UCSR0A_RXC0_BIT = 7  ! USART Receive Complete
  integer, parameter :: USART0_UCSR0B_ADDR = 0xC1
  integer, parameter :: USART0_UCSR0B_TXB80_BIT = 0  ! Transmit Data Bit 8
  integer, parameter :: USART0_UCSR0B_RXB80_BIT = 1  ! Receive Data Bit 8
  integer, parameter :: USART0_UCSR0B_UCSZ02_BIT = 2  ! Character Size
  integer, parameter :: USART0_UCSR0B_TXEN0_BIT = 3  ! Transmitter Enable
  integer, parameter :: USART0_UCSR0B_RXEN0_BIT = 4  ! Receiver Enable
  integer, parameter :: USART0_UCSR0B_UDRIE0_BIT = 5  ! USART Data Register Empty Interrupt Enable
  integer, parameter :: USART0_UCSR0B_TXCIE0_BIT = 6  ! TX Complete Interrupt Enable
  integer, parameter :: USART0_UCSR0B_RXCIE0_BIT = 7  ! RX Complete Interrupt Enable
  integer, parameter :: USART0_UCSR0C_ADDR = 0xC2
  integer, parameter :: USART0_UCSR0C_UCPOL0_BIT = 0  ! Clock Polarity
  integer, parameter :: USART0_UCSR0C_UCSZ00_BIT = 1  ! Character Size
  integer, parameter :: USART0_UCSR0C_UCSZ01_BIT = 2  ! Character Size
  integer, parameter :: USART0_UCSR0C_USBS0_BIT = 3  ! Stop Bit Select
  integer, parameter :: USART0_UCSR0C_UPM00_BIT = 4  ! Parity Mode
  integer, parameter :: USART0_UCSR0C_UPM01_BIT = 5  ! Parity Mode
  integer, parameter :: USART0_UCSR0C_UMSEL00_BIT = 6  ! USART Mode Select
  integer, parameter :: USART0_UCSR0C_UMSEL01_BIT = 7  ! USART Mode Select
  integer, parameter :: USART0_UBRR0_ADDR = 0xC4
  ! Analog-to-Digital Converter
  integer, parameter :: ADC_BASE = 0x78
  integer, parameter :: ADC_ADMUX_ADDR = 0x7C
  integer, parameter :: ADC_ADMUX_MUX0_BIT = 0  ! Analog Channel Selection
  integer, parameter :: ADC_ADMUX_MUX1_BIT = 1  ! Analog Channel Selection
  integer, parameter :: ADC_ADMUX_MUX2_BIT = 2  ! Analog Channel Selection
  integer, parameter :: ADC_ADMUX_MUX3_BIT = 3  ! Analog Channel Selection
  integer, parameter :: ADC_ADMUX_ADLAR_BIT = 5  ! ADC Left Adjust Result
  integer, parameter :: ADC_ADMUX_REFS0_BIT = 6  ! Reference Selection
  integer, parameter :: ADC_ADMUX_REFS1_BIT = 7  ! Reference Selection
  integer, parameter :: ADC_ADCSRA_ADDR = 0x7A
  integer, parameter :: ADC_ADCSRA_ADPS0_BIT = 0  ! ADC Prescaler Select
  integer, parameter :: ADC_ADCSRA_ADPS1_BIT = 1  ! ADC Prescaler Select
  integer, parameter :: ADC_ADCSRA_ADPS2_BIT = 2  ! ADC Prescaler Select
  integer, parameter :: ADC_ADCSRA_ADIE_BIT = 3  ! ADC Interrupt Enable
  integer, parameter :: ADC_ADCSRA_ADIF_BIT = 4  ! ADC Interrupt Flag
  integer, parameter :: ADC_ADCSRA_ADATE_BIT = 5  ! ADC Auto Trigger Enable
  integer, parameter :: ADC_ADCSRA_ADSC_BIT = 6  ! ADC Start Conversion
  integer, parameter :: ADC_ADCSRA_ADEN_BIT = 7  ! ADC Enable
  integer, parameter :: ADC_ADCH_ADDR = 0x79
  integer, parameter :: ADC_ADCL_ADDR = 0x78

  ! 中断向量定义
  integer, parameter :: INT_INT0 = 1  ! External Interrupt Request 0
  integer, parameter :: INT_INT1 = 2  ! External Interrupt Request 1
  integer, parameter :: INT_PCINT0 = 3  ! Pin Change Interrupt Request 0
  integer, parameter :: INT_PCINT1 = 4  ! Pin Change Interrupt Request 1
  integer, parameter :: INT_PCINT2 = 5  ! Pin Change Interrupt Request 2
  integer, parameter :: INT_WDT = 6  ! Watchdog Time-out Interrupt
  integer, parameter :: INT_TIMER2_COMPA = 7  ! Timer/Counter2 Compare Match A
  integer, parameter :: INT_TIMER2_COMPB = 8  ! Timer/Counter2 Compare Match B
  integer, parameter :: INT_TIMER2_OVF = 9  ! Timer/Counter2 Overflow
  integer, parameter :: INT_TIMER1_CAPT = 10  ! Timer/Counter1 Capture Event
  integer, parameter :: INT_TIMER1_COMPA = 11  ! Timer/Counter1 Compare Match A
  integer, parameter :: INT_TIMER1_COMPB = 12  ! Timer/Counter1 Compare Match B
  integer, parameter :: INT_TIMER1_OVF = 13  ! Timer/Counter1 Overflow
  integer, parameter :: INT_TIMER0_COMPA = 14  ! Timer/Counter0 Compare Match A
  integer, parameter :: INT_TIMER0_COMPB = 15  ! Timer/Counter0 Compare Match B
  integer, parameter :: INT_TIMER0_OVF = 16  ! Timer/Counter0 Overflow
  integer, parameter :: INT_SPI_STC = 17  ! SPI Serial Transfer Complete
  integer, parameter :: INT_USART_RX = 18  ! USART Rx Complete
  integer, parameter :: INT_USART_UDRE = 19  ! USART Data Register Empty
  integer, parameter :: INT_USART_TX = 20  ! USART Tx Complete
  integer, parameter :: INT_ADC = 21  ! ADC Conversion Complete
  integer, parameter :: INT_EE_READY = 22  ! EEPROM Ready
  integer, parameter :: INT_ANALOG_COMP = 23  ! Analog Comparator
  integer, parameter :: INT_TWI = 24  ! Two-wire Serial Interface
  integer, parameter :: INT_SPM_READY = 25  ! Store Program Memory Ready

  ! 引脚定义
  integer, parameter :: PIN_PC6 = 1  ! Reset Pin
  integer, parameter :: PIN_PD0 = 2  ! Digital I/O, RX (USART)
  integer, parameter :: PIN_PD1 = 3  ! Digital I/O, TX (USART)
  integer, parameter :: PIN_PD2 = 4  ! Digital I/O, INT0
  integer, parameter :: PIN_PD3 = 5  ! Digital I/O, INT1, OC2B
  integer, parameter :: PIN_PD4 = 6  ! Digital I/O, T0, XCK
  integer, parameter :: PIN_VCC = 7  ! Supply Voltage
  integer, parameter :: PIN_GND = 8  ! Ground
  integer, parameter :: PIN_PB6 = 9  ! Digital I/O, XTAL1
  integer, parameter :: PIN_PB7 = 10  ! Digital I/O, XTAL2
  integer, parameter :: PIN_PD5 = 11  ! Digital I/O, T1, OC0B
  integer, parameter :: PIN_PD6 = 12  ! Digital I/O, AIN0, OC0A
  integer, parameter :: PIN_PD7 = 13  ! Digital I/O, AIN1
  integer, parameter :: PIN_PB0 = 14  ! Digital I/O, ICP1, CLKO
  integer, parameter :: PIN_PB1 = 15  ! Digital I/O, OC1A
  integer, parameter :: PIN_PB2 = 16  ! Digital I/O, SS, OC1B
  integer, parameter :: PIN_PB3 = 17  ! Digital I/O, MOSI, OC2A
  integer, parameter :: PIN_PB4 = 18  ! Digital I/O, MISO
  integer, parameter :: PIN_PB5 = 19  ! Digital I/O, SCK
  integer, parameter :: PIN_AVCC = 20  ! Supply Voltage for ADC
  integer, parameter :: PIN_AREF = 21  ! Analog Reference
  integer, parameter :: PIN_GND = 22  ! Ground
  integer, parameter :: PIN_PC0 = 23  ! Digital I/O, ADC0
  integer, parameter :: PIN_PC1 = 24  ! Digital I/O, ADC1
  integer, parameter :: PIN_PC2 = 25  ! Digital I/O, ADC2
  integer, parameter :: PIN_PC3 = 26  ! Digital I/O, ADC3
  integer, parameter :: PIN_PC4 = 27  ! Digital I/O, ADC4, SDA
  integer, parameter :: PIN_PC5 = 28  ! Digital I/O, ADC5, SCL

end module atmega328p_device
