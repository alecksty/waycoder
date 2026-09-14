#ifndef ATMEGA328P_HPP
#define ATMEGA328P_HPP

// ATmega328P寄存器定义
// 生成自: Atmel/AVR/ATmega328P
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint8_t*)0x00)

// General Purpose Register 1
#define R1 (*(volatile uint8_t*)0x01)

// General Purpose Register 2
#define R2 (*(volatile uint8_t*)0x02)

// General Purpose Register 3
#define R3 (*(volatile uint8_t*)0x03)

// General Purpose Register 4
#define R4 (*(volatile uint8_t*)0x04)

// General Purpose Register 5
#define R5 (*(volatile uint8_t*)0x05)

// General Purpose Register 6
#define R6 (*(volatile uint8_t*)0x06)

// General Purpose Register 7
#define R7 (*(volatile uint8_t*)0x07)

// General Purpose Register 8
#define R8 (*(volatile uint8_t*)0x08)

// General Purpose Register 9
#define R9 (*(volatile uint8_t*)0x09)

// General Purpose Register 10
#define R10 (*(volatile uint8_t*)0x0A)

// General Purpose Register 11
#define R11 (*(volatile uint8_t*)0x0B)

// General Purpose Register 12
#define R12 (*(volatile uint8_t*)0x0C)

// General Purpose Register 13
#define R13 (*(volatile uint8_t*)0x0D)

// General Purpose Register 14
#define R14 (*(volatile uint8_t*)0x0E)

// General Purpose Register 15
#define R15 (*(volatile uint8_t*)0x0F)

// General Purpose Register 16
#define R16 (*(volatile uint8_t*)0x10)

// General Purpose Register 17
#define R17 (*(volatile uint8_t*)0x11)

// General Purpose Register 18
#define R18 (*(volatile uint8_t*)0x12)

// General Purpose Register 19
#define R19 (*(volatile uint8_t*)0x13)

// General Purpose Register 20
#define R20 (*(volatile uint8_t*)0x14)

// General Purpose Register 21
#define R21 (*(volatile uint8_t*)0x15)

// General Purpose Register 22
#define R22 (*(volatile uint8_t*)0x16)

// General Purpose Register 23
#define R23 (*(volatile uint8_t*)0x17)

// General Purpose Register 24
#define R24 (*(volatile uint8_t*)0x18)

// General Purpose Register 25
#define R25 (*(volatile uint8_t*)0x19)

// General Purpose Register 26 (XL)
#define R26 (*(volatile uint8_t*)0x1A)

// General Purpose Register 27 (XH)
#define R27 (*(volatile uint8_t*)0x1B)

// General Purpose Register 28 (YL)
#define R28 (*(volatile uint8_t*)0x1C)

// General Purpose Register 29 (YH)
#define R29 (*(volatile uint8_t*)0x1D)

// General Purpose Register 30 (ZL)
#define R30 (*(volatile uint8_t*)0x1E)

// General Purpose Register 31 (ZH)
#define R31 (*(volatile uint8_t*)0x1F)

// Stack Pointer Low
#define SPL (*(volatile uint8_t*)0x5D)

// Stack Pointer High
#define SPH (*(volatile uint8_t*)0x5E)

// Status Register
#define SREG (*(volatile uint8_t*)0x5F)
#define SREG_C 0  // Carry Flag
#define SREG_Z 1  // Zero Flag
#define SREG_N 2  // Negative Flag
#define SREG_V 3  // Two's Complement Overflow Flag
#define SREG_S 4  // Sign Flag (N ⊕ V)
#define SREG_H 5  // Half Carry Flag
#define SREG_T 6  // Transfer Bit
#define SREG_I 7  // Global Interrupt Enable

// 内存段定义
// Program Flash Memory
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768

// Static RAM
#define SRAM_START 0x0100
#define SRAM_END 0x08FF
#define SRAM_SIZE 2048

// EEPROM
#define EEPROM_START 0x0000
#define EEPROM_END 0x03FF
#define EEPROM_SIZE 1024

// I/O Registers
#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64

// Extended I/O Registers
#define EXTIO_START 0x40
#define EXTIO_END 0xFF
#define EXTIO_SIZE 192

// 外设定义
// Port B Data Register
#define PORTB_BASE 0x23
#define PORTB_PORTB (*(volatile uint8_t*)0x00000048)
#define PORTB_DDRB (*(volatile uint8_t*)0x00000047)
#define PORTB_PINB (*(volatile uint8_t*)0x00000046)
#define PORTB_PB0 0  // Port B, bit 0
#define PORTB_PB1 1  // Port B, bit 1
#define PORTB_PB2 2  // Port B, bit 2
#define PORTB_PB3 3  // Port B, bit 3
#define PORTB_PB4 4  // Port B, bit 4
#define PORTB_PB5 5  // Port B, bit 5
#define PORTB_PB6 6  // Port B, bit 6
#define PORTB_PB7 7  // Port B, bit 7

// Port C Data Register
#define PORTC_BASE 0x26
#define PORTC_PORTC (*(volatile uint8_t*)0x0000004E)
#define PORTC_DDRC (*(volatile uint8_t*)0x0000004D)
#define PORTC_PINC (*(volatile uint8_t*)0x0000004C)
#define PORTC_PC0 0  // Port C, bit 0
#define PORTC_PC1 1  // Port C, bit 1
#define PORTC_PC2 2  // Port C, bit 2
#define PORTC_PC3 3  // Port C, bit 3
#define PORTC_PC4 4  // Port C, bit 4
#define PORTC_PC5 5  // Port C, bit 5
#define PORTC_PC6 6  // Port C, bit 6

// Port D Data Register
#define PORTD_BASE 0x29
#define PORTD_PORTD (*(volatile uint8_t*)0x00000054)
#define PORTD_DDRD (*(volatile uint8_t*)0x00000053)
#define PORTD_PIND (*(volatile uint8_t*)0x00000052)
#define PORTD_PD0 0  // Port D, bit 0
#define PORTD_PD1 1  // Port D, bit 1
#define PORTD_PD2 2  // Port D, bit 2
#define PORTD_PD3 3  // Port D, bit 3
#define PORTD_PD4 4  // Port D, bit 4
#define PORTD_PD5 5  // Port D, bit 5
#define PORTD_PD6 6  // Port D, bit 6
#define PORTD_PD7 7  // Port D, bit 7

// 8-bit Timer/Counter0
#define TIMER0_BASE 0x44
#define TIMER0_TCCR0A (*(volatile uint8_t*)0x00000088)
#define TIMER0_TCCR0A_WGM00 0  // Waveform Generation Mode
#define TIMER0_TCCR0A_WGM01 1  // Waveform Generation Mode
#define TIMER0_TCCR0A_COM0B0 4  // Compare Output Mode for Channel B
#define TIMER0_TCCR0A_COM0B1 5  // Compare Output Mode for Channel B
#define TIMER0_TCCR0A_COM0A0 6  // Compare Output Mode for Channel A
#define TIMER0_TCCR0A_COM0A1 7  // Compare Output Mode for Channel A
#define TIMER0_TCCR0B (*(volatile uint8_t*)0x00000089)
#define TIMER0_TCCR0B_CS00 0  // Clock Select
#define TIMER0_TCCR0B_CS01 1  // Clock Select
#define TIMER0_TCCR0B_CS02 2  // Clock Select
#define TIMER0_TCCR0B_WGM02 3  // Waveform Generation Mode
#define TIMER0_TCCR0B_FOC0B 6  // Force Output Compare B
#define TIMER0_TCCR0B_FOC0A 7  // Force Output Compare A
#define TIMER0_TCNT0 (*(volatile uint8_t*)0x0000008A)
#define TIMER0_OCR0A (*(volatile uint8_t*)0x0000008B)
#define TIMER0_OCR0B (*(volatile uint8_t*)0x0000008C)
#define TIMER0_TIMSK0 (*(volatile uint8_t*)0x000000B2)
#define TIMER0_TIMSK0_TOIE0 0  // Timer/Counter0 Overflow Interrupt Enable
#define TIMER0_TIMSK0_OCIE0A 1  // Timer/Counter0 Output Compare A Match Interrupt Enable
#define TIMER0_TIMSK0_OCIE0B 2  // Timer/Counter0 Output Compare B Match Interrupt Enable
#define TIMER0_TIFR0 (*(volatile uint8_t*)0x00000079)
#define TIMER0_TIFR0_TOV0 0  // Timer/Counter0 Overflow Flag
#define TIMER0_TIFR0_OCF0A 1  // Output Compare Flag 0A
#define TIMER0_TIFR0_OCF0B 2  // Output Compare Flag 0B

// Universal Synchronous/Asynchronous Receiver/Transmitter
#define USART0_BASE 0xC0
#define USART0_UDR0 (*(volatile uint8_t*)0x00000186)
#define USART0_UCSR0A (*(volatile uint8_t*)0x00000180)
#define USART0_UCSR0A_MPCM0 0  // Multi-processor Communication Mode
#define USART0_UCSR0A_U2X0 1  // Double the USART Transmission Speed
#define USART0_UCSR0A_UPE0 2  // Parity Error
#define USART0_UCSR0A_DOR0 3  // Data OverRun
#define USART0_UCSR0A_FE0 4  // Frame Error
#define USART0_UCSR0A_UDRE0 5  // USART Data Register Empty
#define USART0_UCSR0A_TXC0 6  // USART Transmit Complete
#define USART0_UCSR0A_RXC0 7  // USART Receive Complete
#define USART0_UCSR0B (*(volatile uint8_t*)0x00000181)
#define USART0_UCSR0B_TXB80 0  // Transmit Data Bit 8
#define USART0_UCSR0B_RXB80 1  // Receive Data Bit 8
#define USART0_UCSR0B_UCSZ02 2  // Character Size
#define USART0_UCSR0B_TXEN0 3  // Transmitter Enable
#define USART0_UCSR0B_RXEN0 4  // Receiver Enable
#define USART0_UCSR0B_UDRIE0 5  // USART Data Register Empty Interrupt Enable
#define USART0_UCSR0B_TXCIE0 6  // TX Complete Interrupt Enable
#define USART0_UCSR0B_RXCIE0 7  // RX Complete Interrupt Enable
#define USART0_UCSR0C (*(volatile uint8_t*)0x00000182)
#define USART0_UCSR0C_UCPOL0 0  // Clock Polarity
#define USART0_UCSR0C_UCSZ00 1  // Character Size
#define USART0_UCSR0C_UCSZ01 2  // Character Size
#define USART0_UCSR0C_USBS0 3  // Stop Bit Select
#define USART0_UCSR0C_UPM00 4  // Parity Mode
#define USART0_UCSR0C_UPM01 5  // Parity Mode
#define USART0_UCSR0C_UMSEL00 6  // USART Mode Select
#define USART0_UCSR0C_UMSEL01 7  // USART Mode Select
#define USART0_UBRR0 (*(volatile uint16_t*)0x00000184)

// Analog-to-Digital Converter
#define ADC_BASE 0x78
#define ADC_ADMUX (*(volatile uint8_t*)0x000000F4)
#define ADC_ADMUX_MUX0 0  // Analog Channel Selection
#define ADC_ADMUX_MUX1 1  // Analog Channel Selection
#define ADC_ADMUX_MUX2 2  // Analog Channel Selection
#define ADC_ADMUX_MUX3 3  // Analog Channel Selection
#define ADC_ADMUX_ADLAR 5  // ADC Left Adjust Result
#define ADC_ADMUX_REFS0 6  // Reference Selection
#define ADC_ADMUX_REFS1 7  // Reference Selection
#define ADC_ADCSRA (*(volatile uint8_t*)0x000000F2)
#define ADC_ADCSRA_ADPS0 0  // ADC Prescaler Select
#define ADC_ADCSRA_ADPS1 1  // ADC Prescaler Select
#define ADC_ADCSRA_ADPS2 2  // ADC Prescaler Select
#define ADC_ADCSRA_ADIE 3  // ADC Interrupt Enable
#define ADC_ADCSRA_ADIF 4  // ADC Interrupt Flag
#define ADC_ADCSRA_ADATE 5  // ADC Auto Trigger Enable
#define ADC_ADCSRA_ADSC 6  // ADC Start Conversion
#define ADC_ADCSRA_ADEN 7  // ADC Enable
#define ADC_ADCH (*(volatile uint8_t*)0x000000F1)
#define ADC_ADCL (*(volatile uint8_t*)0x000000F0)

// 中断向量定义
#define INT0_VECTOR 1  // External Interrupt Request 0
#define INT1_VECTOR 2  // External Interrupt Request 1
#define PCINT0_VECTOR 3  // Pin Change Interrupt Request 0
#define PCINT1_VECTOR 4  // Pin Change Interrupt Request 1
#define PCINT2_VECTOR 5  // Pin Change Interrupt Request 2
#define WDT_VECTOR 6  // Watchdog Time-out Interrupt
#define TIMER2_COMPA_VECTOR 7  // Timer/Counter2 Compare Match A
#define TIMER2_COMPB_VECTOR 8  // Timer/Counter2 Compare Match B
#define TIMER2_OVF_VECTOR 9  // Timer/Counter2 Overflow
#define TIMER1_CAPT_VECTOR 10  // Timer/Counter1 Capture Event
#define TIMER1_COMPA_VECTOR 11  // Timer/Counter1 Compare Match A
#define TIMER1_COMPB_VECTOR 12  // Timer/Counter1 Compare Match B
#define TIMER1_OVF_VECTOR 13  // Timer/Counter1 Overflow
#define TIMER0_COMPA_VECTOR 14  // Timer/Counter0 Compare Match A
#define TIMER0_COMPB_VECTOR 15  // Timer/Counter0 Compare Match B
#define TIMER0_OVF_VECTOR 16  // Timer/Counter0 Overflow
#define SPI_STC_VECTOR 17  // SPI Serial Transfer Complete
#define USART_RX_VECTOR 18  // USART Rx Complete
#define USART_UDRE_VECTOR 19  // USART Data Register Empty
#define USART_TX_VECTOR 20  // USART Tx Complete
#define ADC_VECTOR 21  // ADC Conversion Complete
#define EE_READY_VECTOR 22  // EEPROM Ready
#define ANALOG_COMP_VECTOR 23  // Analog Comparator
#define TWI_VECTOR 24  // Two-wire Serial Interface
#define SPM_READY_VECTOR 25  // Store Program Memory Ready

// 引脚定义
#define PIN_PC6 1  // Reset Pin
#define PIN_PD0 2  // Digital I/O, RX (USART)
#define PIN_PD1 3  // Digital I/O, TX (USART)
#define PIN_PD2 4  // Digital I/O, INT0
#define PIN_PD3 5  // Digital I/O, INT1, OC2B
#define PIN_PD4 6  // Digital I/O, T0, XCK
#define PIN_VCC 7  // Supply Voltage
#define PIN_GND 8  // Ground
#define PIN_PB6 9  // Digital I/O, XTAL1
#define PIN_PB7 10  // Digital I/O, XTAL2
#define PIN_PD5 11  // Digital I/O, T1, OC0B
#define PIN_PD6 12  // Digital I/O, AIN0, OC0A
#define PIN_PD7 13  // Digital I/O, AIN1
#define PIN_PB0 14  // Digital I/O, ICP1, CLKO
#define PIN_PB1 15  // Digital I/O, OC1A
#define PIN_PB2 16  // Digital I/O, SS, OC1B
#define PIN_PB3 17  // Digital I/O, MOSI, OC2A
#define PIN_PB4 18  // Digital I/O, MISO
#define PIN_PB5 19  // Digital I/O, SCK
#define PIN_AVCC 20  // Supply Voltage for ADC
#define PIN_AREF 21  // Analog Reference
#define PIN_GND 22  // Ground
#define PIN_PC0 23  // Digital I/O, ADC0
#define PIN_PC1 24  // Digital I/O, ADC1
#define PIN_PC2 25  // Digital I/O, ADC2
#define PIN_PC3 26  // Digital I/O, ADC3
#define PIN_PC4 27  // Digital I/O, ADC4, SDA
#define PIN_PC5 28  // Digital I/O, ADC5, SCL

void atmega328p_init(void);

#ifdef __cplusplus
}
#endif

#endif // ATMEGA328P_HPP
