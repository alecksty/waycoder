// ATmega328P 设备定义 - Objective-C 头文件
// 生成自: Atmel/AVR/ATmega328P
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

#ifndef ATMEGA328P_DEVICE_H
#define ATMEGA328P_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // General Purpose Register 0
#define R1_ADDR 0x01  // General Purpose Register 1
#define R2_ADDR 0x02  // General Purpose Register 2
#define R3_ADDR 0x03  // General Purpose Register 3
#define R4_ADDR 0x04  // General Purpose Register 4
#define R5_ADDR 0x05  // General Purpose Register 5
#define R6_ADDR 0x06  // General Purpose Register 6
#define R7_ADDR 0x07  // General Purpose Register 7
#define R8_ADDR 0x08  // General Purpose Register 8
#define R9_ADDR 0x09  // General Purpose Register 9
#define R10_ADDR 0x0A  // General Purpose Register 10
#define R11_ADDR 0x0B  // General Purpose Register 11
#define R12_ADDR 0x0C  // General Purpose Register 12
#define R13_ADDR 0x0D  // General Purpose Register 13
#define R14_ADDR 0x0E  // General Purpose Register 14
#define R15_ADDR 0x0F  // General Purpose Register 15
#define R16_ADDR 0x10  // General Purpose Register 16
#define R17_ADDR 0x11  // General Purpose Register 17
#define R18_ADDR 0x12  // General Purpose Register 18
#define R19_ADDR 0x13  // General Purpose Register 19
#define R20_ADDR 0x14  // General Purpose Register 20
#define R21_ADDR 0x15  // General Purpose Register 21
#define R22_ADDR 0x16  // General Purpose Register 22
#define R23_ADDR 0x17  // General Purpose Register 23
#define R24_ADDR 0x18  // General Purpose Register 24
#define R25_ADDR 0x19  // General Purpose Register 25
#define R26_ADDR 0x1A  // General Purpose Register 26 (XL)
#define R27_ADDR 0x1B  // General Purpose Register 27 (XH)
#define R28_ADDR 0x1C  // General Purpose Register 28 (YL)
#define R29_ADDR 0x1D  // General Purpose Register 29 (YH)
#define R30_ADDR 0x1E  // General Purpose Register 30 (ZL)
#define R31_ADDR 0x1F  // General Purpose Register 31 (ZH)
#define SPL_ADDR 0x5D  // Stack Pointer Low
#define SPH_ADDR 0x5E  // Stack Pointer High
#define SREG_ADDR 0x5F  // Status Register
#define SREG_C_BIT 0  // Carry Flag
#define SREG_Z_BIT 1  // Zero Flag
#define SREG_N_BIT 2  // Negative Flag
#define SREG_V_BIT 3  // Two's Complement Overflow Flag
#define SREG_S_BIT 4  // Sign Flag (N ⊕ V)
#define SREG_H_BIT 5  // Half Carry Flag
#define SREG_T_BIT 6  // Transfer Bit
#define SREG_I_BIT 7  // Global Interrupt Enable

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768  // Program Flash Memory
#define SRAM_START 0x0100
#define SRAM_END 0x08FF
#define SRAM_SIZE 2048  // Static RAM
#define EEPROM_START 0x0000
#define EEPROM_END 0x03FF
#define EEPROM_SIZE 1024  // EEPROM
#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64  // I/O Registers
#define EXTIO_START 0x40
#define EXTIO_END 0xFF
#define EXTIO_SIZE 192  // Extended I/O Registers

// 外设定义
// Port B Data Register
#define PORTB_BASE 0x23
#define PORTB_PORTB_ADDR 0x25
#define PORTB_DDRB_ADDR 0x24
#define PORTB_PINB_ADDR 0x23
// Port C Data Register
#define PORTC_BASE 0x26
#define PORTC_PORTC_ADDR 0x28
#define PORTC_DDRC_ADDR 0x27
#define PORTC_PINC_ADDR 0x26
// Port D Data Register
#define PORTD_BASE 0x29
#define PORTD_PORTD_ADDR 0x2B
#define PORTD_DDRD_ADDR 0x2A
#define PORTD_PIND_ADDR 0x29
// 8-bit Timer/Counter0
#define TIMER0_BASE 0x44
#define TIMER0_TCCR0A_ADDR 0x44
#define TIMER0_TCCR0A_WGM00_BIT 0  // Waveform Generation Mode
#define TIMER0_TCCR0A_WGM01_BIT 1  // Waveform Generation Mode
#define TIMER0_TCCR0A_COM0B0_BIT 4  // Compare Output Mode for Channel B
#define TIMER0_TCCR0A_COM0B1_BIT 5  // Compare Output Mode for Channel B
#define TIMER0_TCCR0A_COM0A0_BIT 6  // Compare Output Mode for Channel A
#define TIMER0_TCCR0A_COM0A1_BIT 7  // Compare Output Mode for Channel A
#define TIMER0_TCCR0B_ADDR 0x45
#define TIMER0_TCCR0B_CS00_BIT 0  // Clock Select
#define TIMER0_TCCR0B_CS01_BIT 1  // Clock Select
#define TIMER0_TCCR0B_CS02_BIT 2  // Clock Select
#define TIMER0_TCCR0B_WGM02_BIT 3  // Waveform Generation Mode
#define TIMER0_TCCR0B_FOC0B_BIT 6  // Force Output Compare B
#define TIMER0_TCCR0B_FOC0A_BIT 7  // Force Output Compare A
#define TIMER0_TCNT0_ADDR 0x46
#define TIMER0_OCR0A_ADDR 0x47
#define TIMER0_OCR0B_ADDR 0x48
#define TIMER0_TIMSK0_ADDR 0x6E
#define TIMER0_TIMSK0_TOIE0_BIT 0  // Timer/Counter0 Overflow Interrupt Enable
#define TIMER0_TIMSK0_OCIE0A_BIT 1  // Timer/Counter0 Output Compare A Match Interrupt Enable
#define TIMER0_TIMSK0_OCIE0B_BIT 2  // Timer/Counter0 Output Compare B Match Interrupt Enable
#define TIMER0_TIFR0_ADDR 0x35
#define TIMER0_TIFR0_TOV0_BIT 0  // Timer/Counter0 Overflow Flag
#define TIMER0_TIFR0_OCF0A_BIT 1  // Output Compare Flag 0A
#define TIMER0_TIFR0_OCF0B_BIT 2  // Output Compare Flag 0B
// Universal Synchronous/Asynchronous Receiver/Transmitter
#define USART0_BASE 0xC0
#define USART0_UDR0_ADDR 0xC6
#define USART0_UCSR0A_ADDR 0xC0
#define USART0_UCSR0A_MPCM0_BIT 0  // Multi-processor Communication Mode
#define USART0_UCSR0A_U2X0_BIT 1  // Double the USART Transmission Speed
#define USART0_UCSR0A_UPE0_BIT 2  // Parity Error
#define USART0_UCSR0A_DOR0_BIT 3  // Data OverRun
#define USART0_UCSR0A_FE0_BIT 4  // Frame Error
#define USART0_UCSR0A_UDRE0_BIT 5  // USART Data Register Empty
#define USART0_UCSR0A_TXC0_BIT 6  // USART Transmit Complete
#define USART0_UCSR0A_RXC0_BIT 7  // USART Receive Complete
#define USART0_UCSR0B_ADDR 0xC1
#define USART0_UCSR0B_TXB80_BIT 0  // Transmit Data Bit 8
#define USART0_UCSR0B_RXB80_BIT 1  // Receive Data Bit 8
#define USART0_UCSR0B_UCSZ02_BIT 2  // Character Size
#define USART0_UCSR0B_TXEN0_BIT 3  // Transmitter Enable
#define USART0_UCSR0B_RXEN0_BIT 4  // Receiver Enable
#define USART0_UCSR0B_UDRIE0_BIT 5  // USART Data Register Empty Interrupt Enable
#define USART0_UCSR0B_TXCIE0_BIT 6  // TX Complete Interrupt Enable
#define USART0_UCSR0B_RXCIE0_BIT 7  // RX Complete Interrupt Enable
#define USART0_UCSR0C_ADDR 0xC2
#define USART0_UCSR0C_UCPOL0_BIT 0  // Clock Polarity
#define USART0_UCSR0C_UCSZ00_BIT 1  // Character Size
#define USART0_UCSR0C_UCSZ01_BIT 2  // Character Size
#define USART0_UCSR0C_USBS0_BIT 3  // Stop Bit Select
#define USART0_UCSR0C_UPM00_BIT 4  // Parity Mode
#define USART0_UCSR0C_UPM01_BIT 5  // Parity Mode
#define USART0_UCSR0C_UMSEL00_BIT 6  // USART Mode Select
#define USART0_UCSR0C_UMSEL01_BIT 7  // USART Mode Select
#define USART0_UBRR0_ADDR 0xC4
// Analog-to-Digital Converter
#define ADC_BASE 0x78
#define ADC_ADMUX_ADDR 0x7C
#define ADC_ADMUX_MUX0_BIT 0  // Analog Channel Selection
#define ADC_ADMUX_MUX1_BIT 1  // Analog Channel Selection
#define ADC_ADMUX_MUX2_BIT 2  // Analog Channel Selection
#define ADC_ADMUX_MUX3_BIT 3  // Analog Channel Selection
#define ADC_ADMUX_ADLAR_BIT 5  // ADC Left Adjust Result
#define ADC_ADMUX_REFS0_BIT 6  // Reference Selection
#define ADC_ADMUX_REFS1_BIT 7  // Reference Selection
#define ADC_ADCSRA_ADDR 0x7A
#define ADC_ADCSRA_ADPS0_BIT 0  // ADC Prescaler Select
#define ADC_ADCSRA_ADPS1_BIT 1  // ADC Prescaler Select
#define ADC_ADCSRA_ADPS2_BIT 2  // ADC Prescaler Select
#define ADC_ADCSRA_ADIE_BIT 3  // ADC Interrupt Enable
#define ADC_ADCSRA_ADIF_BIT 4  // ADC Interrupt Flag
#define ADC_ADCSRA_ADATE_BIT 5  // ADC Auto Trigger Enable
#define ADC_ADCSRA_ADSC_BIT 6  // ADC Start Conversion
#define ADC_ADCSRA_ADEN_BIT 7  // ADC Enable
#define ADC_ADCH_ADDR 0x79
#define ADC_ADCL_ADDR 0x78

// 中断向量定义
#define INT_INT0 1  // External Interrupt Request 0
#define INT_INT1 2  // External Interrupt Request 1
#define INT_PCINT0 3  // Pin Change Interrupt Request 0
#define INT_PCINT1 4  // Pin Change Interrupt Request 1
#define INT_PCINT2 5  // Pin Change Interrupt Request 2
#define INT_WDT 6  // Watchdog Time-out Interrupt
#define INT_TIMER2_COMPA 7  // Timer/Counter2 Compare Match A
#define INT_TIMER2_COMPB 8  // Timer/Counter2 Compare Match B
#define INT_TIMER2_OVF 9  // Timer/Counter2 Overflow
#define INT_TIMER1_CAPT 10  // Timer/Counter1 Capture Event
#define INT_TIMER1_COMPA 11  // Timer/Counter1 Compare Match A
#define INT_TIMER1_COMPB 12  // Timer/Counter1 Compare Match B
#define INT_TIMER1_OVF 13  // Timer/Counter1 Overflow
#define INT_TIMER0_COMPA 14  // Timer/Counter0 Compare Match A
#define INT_TIMER0_COMPB 15  // Timer/Counter0 Compare Match B
#define INT_TIMER0_OVF 16  // Timer/Counter0 Overflow
#define INT_SPI_STC 17  // SPI Serial Transfer Complete
#define INT_USART_RX 18  // USART Rx Complete
#define INT_USART_UDRE 19  // USART Data Register Empty
#define INT_USART_TX 20  // USART Tx Complete
#define INT_ADC 21  // ADC Conversion Complete
#define INT_EE_READY 22  // EEPROM Ready
#define INT_ANALOG_COMP 23  // Analog Comparator
#define INT_TWI 24  // Two-wire Serial Interface
#define INT_SPM_READY 25  // Store Program Memory Ready

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

#endif /* ATMEGA328P_DEVICE_H */
