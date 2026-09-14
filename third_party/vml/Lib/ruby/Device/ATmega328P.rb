# ATmega328P 设备定义 - Ruby 模块
# 生成自: Atmel/AVR/ATmega328P
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
# CPU架构: AVR
# 位宽: 8位
# 时钟频率: 16000000 Hz

module ATmega328P

  # 寄存器地址定义
  R0_ADDR = 0x00  # General Purpose Register 0
  R1_ADDR = 0x01  # General Purpose Register 1
  R2_ADDR = 0x02  # General Purpose Register 2
  R3_ADDR = 0x03  # General Purpose Register 3
  R4_ADDR = 0x04  # General Purpose Register 4
  R5_ADDR = 0x05  # General Purpose Register 5
  R6_ADDR = 0x06  # General Purpose Register 6
  R7_ADDR = 0x07  # General Purpose Register 7
  R8_ADDR = 0x08  # General Purpose Register 8
  R9_ADDR = 0x09  # General Purpose Register 9
  R10_ADDR = 0x0A  # General Purpose Register 10
  R11_ADDR = 0x0B  # General Purpose Register 11
  R12_ADDR = 0x0C  # General Purpose Register 12
  R13_ADDR = 0x0D  # General Purpose Register 13
  R14_ADDR = 0x0E  # General Purpose Register 14
  R15_ADDR = 0x0F  # General Purpose Register 15
  R16_ADDR = 0x10  # General Purpose Register 16
  R17_ADDR = 0x11  # General Purpose Register 17
  R18_ADDR = 0x12  # General Purpose Register 18
  R19_ADDR = 0x13  # General Purpose Register 19
  R20_ADDR = 0x14  # General Purpose Register 20
  R21_ADDR = 0x15  # General Purpose Register 21
  R22_ADDR = 0x16  # General Purpose Register 22
  R23_ADDR = 0x17  # General Purpose Register 23
  R24_ADDR = 0x18  # General Purpose Register 24
  R25_ADDR = 0x19  # General Purpose Register 25
  R26_ADDR = 0x1A  # General Purpose Register 26 (XL)
  R27_ADDR = 0x1B  # General Purpose Register 27 (XH)
  R28_ADDR = 0x1C  # General Purpose Register 28 (YL)
  R29_ADDR = 0x1D  # General Purpose Register 29 (YH)
  R30_ADDR = 0x1E  # General Purpose Register 30 (ZL)
  R31_ADDR = 0x1F  # General Purpose Register 31 (ZH)
  SPL_ADDR = 0x5D  # Stack Pointer Low
  SPH_ADDR = 0x5E  # Stack Pointer High
  SREG_ADDR = 0x5F  # Status Register
  SREG_C_BIT = 0  # Carry Flag
  SREG_Z_BIT = 1  # Zero Flag
  SREG_N_BIT = 2  # Negative Flag
  SREG_V_BIT = 3  # Two's Complement Overflow Flag
  SREG_S_BIT = 4  # Sign Flag (N ⊕ V)
  SREG_H_BIT = 5  # Half Carry Flag
  SREG_T_BIT = 6  # Transfer Bit
  SREG_I_BIT = 7  # Global Interrupt Enable

  # 内存段定义
  FLASH_START = 0x0000
  FLASH_END = 0x7FFF
  FLASH_SIZE = 32768  # Program Flash Memory
  SRAM_START = 0x0100
  SRAM_END = 0x08FF
  SRAM_SIZE = 2048  # Static RAM
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
  # Port B Data Register
  PORTB_BASE = 0x23
  PORTB_PORTB_ADDR = 0x25
  PORTB_DDRB_ADDR = 0x24
  PORTB_PINB_ADDR = 0x23
  # Port C Data Register
  PORTC_BASE = 0x26
  PORTC_PORTC_ADDR = 0x28
  PORTC_DDRC_ADDR = 0x27
  PORTC_PINC_ADDR = 0x26
  # Port D Data Register
  PORTD_BASE = 0x29
  PORTD_PORTD_ADDR = 0x2B
  PORTD_DDRD_ADDR = 0x2A
  PORTD_PIND_ADDR = 0x29
  # 8-bit Timer/Counter0
  TIMER0_BASE = 0x44
  TIMER0_TCCR0A_ADDR = 0x44
  TIMER0_TCCR0A_WGM00_BIT = 0  # Waveform Generation Mode
  TIMER0_TCCR0A_WGM01_BIT = 1  # Waveform Generation Mode
  TIMER0_TCCR0A_COM0B0_BIT = 4  # Compare Output Mode for Channel B
  TIMER0_TCCR0A_COM0B1_BIT = 5  # Compare Output Mode for Channel B
  TIMER0_TCCR0A_COM0A0_BIT = 6  # Compare Output Mode for Channel A
  TIMER0_TCCR0A_COM0A1_BIT = 7  # Compare Output Mode for Channel A
  TIMER0_TCCR0B_ADDR = 0x45
  TIMER0_TCCR0B_CS00_BIT = 0  # Clock Select
  TIMER0_TCCR0B_CS01_BIT = 1  # Clock Select
  TIMER0_TCCR0B_CS02_BIT = 2  # Clock Select
  TIMER0_TCCR0B_WGM02_BIT = 3  # Waveform Generation Mode
  TIMER0_TCCR0B_FOC0B_BIT = 6  # Force Output Compare B
  TIMER0_TCCR0B_FOC0A_BIT = 7  # Force Output Compare A
  TIMER0_TCNT0_ADDR = 0x46
  TIMER0_OCR0A_ADDR = 0x47
  TIMER0_OCR0B_ADDR = 0x48
  TIMER0_TIMSK0_ADDR = 0x6E
  TIMER0_TIMSK0_TOIE0_BIT = 0  # Timer/Counter0 Overflow Interrupt Enable
  TIMER0_TIMSK0_OCIE0A_BIT = 1  # Timer/Counter0 Output Compare A Match Interrupt Enable
  TIMER0_TIMSK0_OCIE0B_BIT = 2  # Timer/Counter0 Output Compare B Match Interrupt Enable
  TIMER0_TIFR0_ADDR = 0x35
  TIMER0_TIFR0_TOV0_BIT = 0  # Timer/Counter0 Overflow Flag
  TIMER0_TIFR0_OCF0A_BIT = 1  # Output Compare Flag 0A
  TIMER0_TIFR0_OCF0B_BIT = 2  # Output Compare Flag 0B
  # Universal Synchronous/Asynchronous Receiver/Transmitter
  USART0_BASE = 0xC0
  USART0_UDR0_ADDR = 0xC6
  USART0_UCSR0A_ADDR = 0xC0
  USART0_UCSR0A_MPCM0_BIT = 0  # Multi-processor Communication Mode
  USART0_UCSR0A_U2X0_BIT = 1  # Double the USART Transmission Speed
  USART0_UCSR0A_UPE0_BIT = 2  # Parity Error
  USART0_UCSR0A_DOR0_BIT = 3  # Data OverRun
  USART0_UCSR0A_FE0_BIT = 4  # Frame Error
  USART0_UCSR0A_UDRE0_BIT = 5  # USART Data Register Empty
  USART0_UCSR0A_TXC0_BIT = 6  # USART Transmit Complete
  USART0_UCSR0A_RXC0_BIT = 7  # USART Receive Complete
  USART0_UCSR0B_ADDR = 0xC1
  USART0_UCSR0B_TXB80_BIT = 0  # Transmit Data Bit 8
  USART0_UCSR0B_RXB80_BIT = 1  # Receive Data Bit 8
  USART0_UCSR0B_UCSZ02_BIT = 2  # Character Size
  USART0_UCSR0B_TXEN0_BIT = 3  # Transmitter Enable
  USART0_UCSR0B_RXEN0_BIT = 4  # Receiver Enable
  USART0_UCSR0B_UDRIE0_BIT = 5  # USART Data Register Empty Interrupt Enable
  USART0_UCSR0B_TXCIE0_BIT = 6  # TX Complete Interrupt Enable
  USART0_UCSR0B_RXCIE0_BIT = 7  # RX Complete Interrupt Enable
  USART0_UCSR0C_ADDR = 0xC2
  USART0_UCSR0C_UCPOL0_BIT = 0  # Clock Polarity
  USART0_UCSR0C_UCSZ00_BIT = 1  # Character Size
  USART0_UCSR0C_UCSZ01_BIT = 2  # Character Size
  USART0_UCSR0C_USBS0_BIT = 3  # Stop Bit Select
  USART0_UCSR0C_UPM00_BIT = 4  # Parity Mode
  USART0_UCSR0C_UPM01_BIT = 5  # Parity Mode
  USART0_UCSR0C_UMSEL00_BIT = 6  # USART Mode Select
  USART0_UCSR0C_UMSEL01_BIT = 7  # USART Mode Select
  USART0_UBRR0_ADDR = 0xC4
  # Analog-to-Digital Converter
  ADC_BASE = 0x78
  ADC_ADMUX_ADDR = 0x7C
  ADC_ADMUX_MUX0_BIT = 0  # Analog Channel Selection
  ADC_ADMUX_MUX1_BIT = 1  # Analog Channel Selection
  ADC_ADMUX_MUX2_BIT = 2  # Analog Channel Selection
  ADC_ADMUX_MUX3_BIT = 3  # Analog Channel Selection
  ADC_ADMUX_ADLAR_BIT = 5  # ADC Left Adjust Result
  ADC_ADMUX_REFS0_BIT = 6  # Reference Selection
  ADC_ADMUX_REFS1_BIT = 7  # Reference Selection
  ADC_ADCSRA_ADDR = 0x7A
  ADC_ADCSRA_ADPS0_BIT = 0  # ADC Prescaler Select
  ADC_ADCSRA_ADPS1_BIT = 1  # ADC Prescaler Select
  ADC_ADCSRA_ADPS2_BIT = 2  # ADC Prescaler Select
  ADC_ADCSRA_ADIE_BIT = 3  # ADC Interrupt Enable
  ADC_ADCSRA_ADIF_BIT = 4  # ADC Interrupt Flag
  ADC_ADCSRA_ADATE_BIT = 5  # ADC Auto Trigger Enable
  ADC_ADCSRA_ADSC_BIT = 6  # ADC Start Conversion
  ADC_ADCSRA_ADEN_BIT = 7  # ADC Enable
  ADC_ADCH_ADDR = 0x79
  ADC_ADCL_ADDR = 0x78

  # 中断向量定义
  INT_INT0 = 1  # External Interrupt Request 0
  INT_INT1 = 2  # External Interrupt Request 1
  INT_PCINT0 = 3  # Pin Change Interrupt Request 0
  INT_PCINT1 = 4  # Pin Change Interrupt Request 1
  INT_PCINT2 = 5  # Pin Change Interrupt Request 2
  INT_WDT = 6  # Watchdog Time-out Interrupt
  INT_TIMER2_COMPA = 7  # Timer/Counter2 Compare Match A
  INT_TIMER2_COMPB = 8  # Timer/Counter2 Compare Match B
  INT_TIMER2_OVF = 9  # Timer/Counter2 Overflow
  INT_TIMER1_CAPT = 10  # Timer/Counter1 Capture Event
  INT_TIMER1_COMPA = 11  # Timer/Counter1 Compare Match A
  INT_TIMER1_COMPB = 12  # Timer/Counter1 Compare Match B
  INT_TIMER1_OVF = 13  # Timer/Counter1 Overflow
  INT_TIMER0_COMPA = 14  # Timer/Counter0 Compare Match A
  INT_TIMER0_COMPB = 15  # Timer/Counter0 Compare Match B
  INT_TIMER0_OVF = 16  # Timer/Counter0 Overflow
  INT_SPI_STC = 17  # SPI Serial Transfer Complete
  INT_USART_RX = 18  # USART Rx Complete
  INT_USART_UDRE = 19  # USART Data Register Empty
  INT_USART_TX = 20  # USART Tx Complete
  INT_ADC = 21  # ADC Conversion Complete
  INT_EE_READY = 22  # EEPROM Ready
  INT_ANALOG_COMP = 23  # Analog Comparator
  INT_TWI = 24  # Two-wire Serial Interface
  INT_SPM_READY = 25  # Store Program Memory Ready

  # 引脚定义
  PIN_PC6 = 1  # Reset Pin
  PIN_PD0 = 2  # Digital I/O, RX (USART)
  PIN_PD1 = 3  # Digital I/O, TX (USART)
  PIN_PD2 = 4  # Digital I/O, INT0
  PIN_PD3 = 5  # Digital I/O, INT1, OC2B
  PIN_PD4 = 6  # Digital I/O, T0, XCK
  PIN_VCC = 7  # Supply Voltage
  PIN_GND = 8  # Ground
  PIN_PB6 = 9  # Digital I/O, XTAL1
  PIN_PB7 = 10  # Digital I/O, XTAL2
  PIN_PD5 = 11  # Digital I/O, T1, OC0B
  PIN_PD6 = 12  # Digital I/O, AIN0, OC0A
  PIN_PD7 = 13  # Digital I/O, AIN1
  PIN_PB0 = 14  # Digital I/O, ICP1, CLKO
  PIN_PB1 = 15  # Digital I/O, OC1A
  PIN_PB2 = 16  # Digital I/O, SS, OC1B
  PIN_PB3 = 17  # Digital I/O, MOSI, OC2A
  PIN_PB4 = 18  # Digital I/O, MISO
  PIN_PB5 = 19  # Digital I/O, SCK
  PIN_AVCC = 20  # Supply Voltage for ADC
  PIN_AREF = 21  # Analog Reference
  PIN_GND = 22  # Ground
  PIN_PC0 = 23  # Digital I/O, ADC0
  PIN_PC1 = 24  # Digital I/O, ADC1
  PIN_PC2 = 25  # Digital I/O, ADC2
  PIN_PC3 = 26  # Digital I/O, ADC3
  PIN_PC4 = 27  # Digital I/O, ADC4, SDA
  PIN_PC5 = 28  # Digital I/O, ADC5, SCL

end
