' ATmega328P寄存器定义
' 生成自: Atmel/AVR/ATmega328P
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM

' CPU架构: AVR
' 位宽: 8位
' 时钟频率: 16000000 Hz

' 寄存器定义
' General Purpose Register 0
CONST R0 = 0x00

' General Purpose Register 1
CONST R1 = 0x01

' General Purpose Register 2
CONST R2 = 0x02

' General Purpose Register 3
CONST R3 = 0x03

' General Purpose Register 4
CONST R4 = 0x04

' General Purpose Register 5
CONST R5 = 0x05

' General Purpose Register 6
CONST R6 = 0x06

' General Purpose Register 7
CONST R7 = 0x07

' General Purpose Register 8
CONST R8 = 0x08

' General Purpose Register 9
CONST R9 = 0x09

' General Purpose Register 10
CONST R10 = 0x0A

' General Purpose Register 11
CONST R11 = 0x0B

' General Purpose Register 12
CONST R12 = 0x0C

' General Purpose Register 13
CONST R13 = 0x0D

' General Purpose Register 14
CONST R14 = 0x0E

' General Purpose Register 15
CONST R15 = 0x0F

' General Purpose Register 16
CONST R16 = 0x10

' General Purpose Register 17
CONST R17 = 0x11

' General Purpose Register 18
CONST R18 = 0x12

' General Purpose Register 19
CONST R19 = 0x13

' General Purpose Register 20
CONST R20 = 0x14

' General Purpose Register 21
CONST R21 = 0x15

' General Purpose Register 22
CONST R22 = 0x16

' General Purpose Register 23
CONST R23 = 0x17

' General Purpose Register 24
CONST R24 = 0x18

' General Purpose Register 25
CONST R25 = 0x19

' General Purpose Register 26 (XL)
CONST R26 = 0x1A

' General Purpose Register 27 (XH)
CONST R27 = 0x1B

' General Purpose Register 28 (YL)
CONST R28 = 0x1C

' General Purpose Register 29 (YH)
CONST R29 = 0x1D

' General Purpose Register 30 (ZL)
CONST R30 = 0x1E

' General Purpose Register 31 (ZH)
CONST R31 = 0x1F

' Stack Pointer Low
CONST SPL = 0x5D

' Stack Pointer High
CONST SPH = 0x5E

' Status Register
CONST SREG = 0x5F
CONST SREG_C = 0  ' Carry Flag
CONST SREG_Z = 1  ' Zero Flag
CONST SREG_N = 2  ' Negative Flag
CONST SREG_V = 3  ' Two's Complement Overflow Flag
CONST SREG_S = 4  ' Sign Flag (N ⊕ V)
CONST SREG_H = 5  ' Half Carry Flag
CONST SREG_T = 6  ' Transfer Bit
CONST SREG_I = 7  ' Global Interrupt Enable

' 内存段定义
' Program Flash Memory
CONST FLASH_START = 0x0000
CONST FLASH_END = 0x7FFF
CONST FLASH_SIZE = 32768

' Static RAM
CONST SRAM_START = 0x0100
CONST SRAM_END = 0x08FF
CONST SRAM_SIZE = 2048

' EEPROM
CONST EEPROM_START = 0x0000
CONST EEPROM_END = 0x03FF
CONST EEPROM_SIZE = 1024

' I/O Registers
CONST IO_START = 0x00
CONST IO_END = 0x3F
CONST IO_SIZE = 64

' Extended I/O Registers
CONST EXTIO_START = 0x40
CONST EXTIO_END = 0xFF
CONST EXTIO_SIZE = 192

' 外设定义
' Port B Data Register
CONST PORTB_BASE = 0x23
CONST PORTB_PORTB = 0x25
CONST PORTB_DDRB = 0x24
CONST PORTB_PINB = 0x23
CONST PORTB_PB0 = 0  ' Port B, bit 0
CONST PORTB_PB1 = 1  ' Port B, bit 1
CONST PORTB_PB2 = 2  ' Port B, bit 2
CONST PORTB_PB3 = 3  ' Port B, bit 3
CONST PORTB_PB4 = 4  ' Port B, bit 4
CONST PORTB_PB5 = 5  ' Port B, bit 5
CONST PORTB_PB6 = 6  ' Port B, bit 6
CONST PORTB_PB7 = 7  ' Port B, bit 7

' Port C Data Register
CONST PORTC_BASE = 0x26
CONST PORTC_PORTC = 0x28
CONST PORTC_DDRC = 0x27
CONST PORTC_PINC = 0x26
CONST PORTC_PC0 = 0  ' Port C, bit 0
CONST PORTC_PC1 = 1  ' Port C, bit 1
CONST PORTC_PC2 = 2  ' Port C, bit 2
CONST PORTC_PC3 = 3  ' Port C, bit 3
CONST PORTC_PC4 = 4  ' Port C, bit 4
CONST PORTC_PC5 = 5  ' Port C, bit 5
CONST PORTC_PC6 = 6  ' Port C, bit 6

' Port D Data Register
CONST PORTD_BASE = 0x29
CONST PORTD_PORTD = 0x2B
CONST PORTD_DDRD = 0x2A
CONST PORTD_PIND = 0x29
CONST PORTD_PD0 = 0  ' Port D, bit 0
CONST PORTD_PD1 = 1  ' Port D, bit 1
CONST PORTD_PD2 = 2  ' Port D, bit 2
CONST PORTD_PD3 = 3  ' Port D, bit 3
CONST PORTD_PD4 = 4  ' Port D, bit 4
CONST PORTD_PD5 = 5  ' Port D, bit 5
CONST PORTD_PD6 = 6  ' Port D, bit 6
CONST PORTD_PD7 = 7  ' Port D, bit 7

' 8-bit Timer/Counter0
CONST TIMER0_BASE = 0x44
CONST TIMER0_TCCR0A = 0x44
CONST TIMER0_TCCR0A_WGM00 = 0  ' Waveform Generation Mode
CONST TIMER0_TCCR0A_WGM01 = 1  ' Waveform Generation Mode
CONST TIMER0_TCCR0A_COM0B0 = 4  ' Compare Output Mode for Channel B
CONST TIMER0_TCCR0A_COM0B1 = 5  ' Compare Output Mode for Channel B
CONST TIMER0_TCCR0A_COM0A0 = 6  ' Compare Output Mode for Channel A
CONST TIMER0_TCCR0A_COM0A1 = 7  ' Compare Output Mode for Channel A
CONST TIMER0_TCCR0B = 0x45
CONST TIMER0_TCCR0B_CS00 = 0  ' Clock Select
CONST TIMER0_TCCR0B_CS01 = 1  ' Clock Select
CONST TIMER0_TCCR0B_CS02 = 2  ' Clock Select
CONST TIMER0_TCCR0B_WGM02 = 3  ' Waveform Generation Mode
CONST TIMER0_TCCR0B_FOC0B = 6  ' Force Output Compare B
CONST TIMER0_TCCR0B_FOC0A = 7  ' Force Output Compare A
CONST TIMER0_TCNT0 = 0x46
CONST TIMER0_OCR0A = 0x47
CONST TIMER0_OCR0B = 0x48
CONST TIMER0_TIMSK0 = 0x6E
CONST TIMER0_TIMSK0_TOIE0 = 0  ' Timer/Counter0 Overflow Interrupt Enable
CONST TIMER0_TIMSK0_OCIE0A = 1  ' Timer/Counter0 Output Compare A Match Interrupt Enable
CONST TIMER0_TIMSK0_OCIE0B = 2  ' Timer/Counter0 Output Compare B Match Interrupt Enable
CONST TIMER0_TIFR0 = 0x35
CONST TIMER0_TIFR0_TOV0 = 0  ' Timer/Counter0 Overflow Flag
CONST TIMER0_TIFR0_OCF0A = 1  ' Output Compare Flag 0A
CONST TIMER0_TIFR0_OCF0B = 2  ' Output Compare Flag 0B

' Universal Synchronous/Asynchronous Receiver/Transmitter
CONST USART0_BASE = 0xC0
CONST USART0_UDR0 = 0xC6
CONST USART0_UCSR0A = 0xC0
CONST USART0_UCSR0A_MPCM0 = 0  ' Multi-processor Communication Mode
CONST USART0_UCSR0A_U2X0 = 1  ' Double the USART Transmission Speed
CONST USART0_UCSR0A_UPE0 = 2  ' Parity Error
CONST USART0_UCSR0A_DOR0 = 3  ' Data OverRun
CONST USART0_UCSR0A_FE0 = 4  ' Frame Error
CONST USART0_UCSR0A_UDRE0 = 5  ' USART Data Register Empty
CONST USART0_UCSR0A_TXC0 = 6  ' USART Transmit Complete
CONST USART0_UCSR0A_RXC0 = 7  ' USART Receive Complete
CONST USART0_UCSR0B = 0xC1
CONST USART0_UCSR0B_TXB80 = 0  ' Transmit Data Bit 8
CONST USART0_UCSR0B_RXB80 = 1  ' Receive Data Bit 8
CONST USART0_UCSR0B_UCSZ02 = 2  ' Character Size
CONST USART0_UCSR0B_TXEN0 = 3  ' Transmitter Enable
CONST USART0_UCSR0B_RXEN0 = 4  ' Receiver Enable
CONST USART0_UCSR0B_UDRIE0 = 5  ' USART Data Register Empty Interrupt Enable
CONST USART0_UCSR0B_TXCIE0 = 6  ' TX Complete Interrupt Enable
CONST USART0_UCSR0B_RXCIE0 = 7  ' RX Complete Interrupt Enable
CONST USART0_UCSR0C = 0xC2
CONST USART0_UCSR0C_UCPOL0 = 0  ' Clock Polarity
CONST USART0_UCSR0C_UCSZ00 = 1  ' Character Size
CONST USART0_UCSR0C_UCSZ01 = 2  ' Character Size
CONST USART0_UCSR0C_USBS0 = 3  ' Stop Bit Select
CONST USART0_UCSR0C_UPM00 = 4  ' Parity Mode
CONST USART0_UCSR0C_UPM01 = 5  ' Parity Mode
CONST USART0_UCSR0C_UMSEL00 = 6  ' USART Mode Select
CONST USART0_UCSR0C_UMSEL01 = 7  ' USART Mode Select
CONST USART0_UBRR0 = 0xC4

' Analog-to-Digital Converter
CONST ADC_BASE = 0x78
CONST ADC_ADMUX = 0x7C
CONST ADC_ADMUX_MUX0 = 0  ' Analog Channel Selection
CONST ADC_ADMUX_MUX1 = 1  ' Analog Channel Selection
CONST ADC_ADMUX_MUX2 = 2  ' Analog Channel Selection
CONST ADC_ADMUX_MUX3 = 3  ' Analog Channel Selection
CONST ADC_ADMUX_ADLAR = 5  ' ADC Left Adjust Result
CONST ADC_ADMUX_REFS0 = 6  ' Reference Selection
CONST ADC_ADMUX_REFS1 = 7  ' Reference Selection
CONST ADC_ADCSRA = 0x7A
CONST ADC_ADCSRA_ADPS0 = 0  ' ADC Prescaler Select
CONST ADC_ADCSRA_ADPS1 = 1  ' ADC Prescaler Select
CONST ADC_ADCSRA_ADPS2 = 2  ' ADC Prescaler Select
CONST ADC_ADCSRA_ADIE = 3  ' ADC Interrupt Enable
CONST ADC_ADCSRA_ADIF = 4  ' ADC Interrupt Flag
CONST ADC_ADCSRA_ADATE = 5  ' ADC Auto Trigger Enable
CONST ADC_ADCSRA_ADSC = 6  ' ADC Start Conversion
CONST ADC_ADCSRA_ADEN = 7  ' ADC Enable
CONST ADC_ADCH = 0x79
CONST ADC_ADCL = 0x78

' 中断向量定义
CONST INT0_VECTOR = 1  ' External Interrupt Request 0
CONST INT1_VECTOR = 2  ' External Interrupt Request 1
CONST PCINT0_VECTOR = 3  ' Pin Change Interrupt Request 0
CONST PCINT1_VECTOR = 4  ' Pin Change Interrupt Request 1
CONST PCINT2_VECTOR = 5  ' Pin Change Interrupt Request 2
CONST WDT_VECTOR = 6  ' Watchdog Time-out Interrupt
CONST TIMER2_COMPA_VECTOR = 7  ' Timer/Counter2 Compare Match A
CONST TIMER2_COMPB_VECTOR = 8  ' Timer/Counter2 Compare Match B
CONST TIMER2_OVF_VECTOR = 9  ' Timer/Counter2 Overflow
CONST TIMER1_CAPT_VECTOR = 10  ' Timer/Counter1 Capture Event
CONST TIMER1_COMPA_VECTOR = 11  ' Timer/Counter1 Compare Match A
CONST TIMER1_COMPB_VECTOR = 12  ' Timer/Counter1 Compare Match B
CONST TIMER1_OVF_VECTOR = 13  ' Timer/Counter1 Overflow
CONST TIMER0_COMPA_VECTOR = 14  ' Timer/Counter0 Compare Match A
CONST TIMER0_COMPB_VECTOR = 15  ' Timer/Counter0 Compare Match B
CONST TIMER0_OVF_VECTOR = 16  ' Timer/Counter0 Overflow
CONST SPI_STC_VECTOR = 17  ' SPI Serial Transfer Complete
CONST USART_RX_VECTOR = 18  ' USART Rx Complete
CONST USART_UDRE_VECTOR = 19  ' USART Data Register Empty
CONST USART_TX_VECTOR = 20  ' USART Tx Complete
CONST ADC_VECTOR = 21  ' ADC Conversion Complete
CONST EE_READY_VECTOR = 22  ' EEPROM Ready
CONST ANALOG_COMP_VECTOR = 23  ' Analog Comparator
CONST TWI_VECTOR = 24  ' Two-wire Serial Interface
CONST SPM_READY_VECTOR = 25  ' Store Program Memory Ready

' 引脚定义
CONST PIN_PC6 = 1  ' Reset Pin
CONST PIN_PD0 = 2  ' Digital I/O, RX (USART)
CONST PIN_PD1 = 3  ' Digital I/O, TX (USART)
CONST PIN_PD2 = 4  ' Digital I/O, INT0
CONST PIN_PD3 = 5  ' Digital I/O, INT1, OC2B
CONST PIN_PD4 = 6  ' Digital I/O, T0, XCK
CONST PIN_VCC = 7  ' Supply Voltage
CONST PIN_GND = 8  ' Ground
CONST PIN_PB6 = 9  ' Digital I/O, XTAL1
CONST PIN_PB7 = 10  ' Digital I/O, XTAL2
CONST PIN_PD5 = 11  ' Digital I/O, T1, OC0B
CONST PIN_PD6 = 12  ' Digital I/O, AIN0, OC0A
CONST PIN_PD7 = 13  ' Digital I/O, AIN1
CONST PIN_PB0 = 14  ' Digital I/O, ICP1, CLKO
CONST PIN_PB1 = 15  ' Digital I/O, OC1A
CONST PIN_PB2 = 16  ' Digital I/O, SS, OC1B
CONST PIN_PB3 = 17  ' Digital I/O, MOSI, OC2A
CONST PIN_PB4 = 18  ' Digital I/O, MISO
CONST PIN_PB5 = 19  ' Digital I/O, SCK
CONST PIN_AVCC = 20  ' Supply Voltage for ADC
CONST PIN_AREF = 21  ' Analog Reference
CONST PIN_GND = 22  ' Ground
CONST PIN_PC0 = 23  ' Digital I/O, ADC0
CONST PIN_PC1 = 24  ' Digital I/O, ADC1
CONST PIN_PC2 = 25  ' Digital I/O, ADC2
CONST PIN_PC3 = 26  ' Digital I/O, ADC3
CONST PIN_PC4 = 27  ' Digital I/O, ADC4, SDA
CONST PIN_PC5 = 28  ' Digital I/O, ADC5, SCL

' 设备初始化子程序
SUB atmega328p_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
