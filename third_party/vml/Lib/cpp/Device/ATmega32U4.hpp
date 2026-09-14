#ifndef ATMEGA32U4_HPP
#define ATMEGA32U4_HPP

// ATmega32U4寄存器定义
// 生成自: Atmel/AVR/ATmega32U4
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

// 寄存器定义
#define R0 (*(volatile uint8_t*)0x00)

#define R1 (*(volatile uint8_t*)0x01)

#define R2 (*(volatile uint8_t*)0x02)

#define R3 (*(volatile uint8_t*)0x03)

#define R4 (*(volatile uint8_t*)0x04)

#define R5 (*(volatile uint8_t*)0x05)

#define R6 (*(volatile uint8_t*)0x06)

#define R7 (*(volatile uint8_t*)0x07)

#define R8 (*(volatile uint8_t*)0x08)

#define R9 (*(volatile uint8_t*)0x09)

#define R10 (*(volatile uint8_t*)0x0A)

#define R11 (*(volatile uint8_t*)0x0B)

#define R12 (*(volatile uint8_t*)0x0C)

#define R13 (*(volatile uint8_t*)0x0D)

#define R14 (*(volatile uint8_t*)0x0E)

#define R15 (*(volatile uint8_t*)0x0F)

#define R16 (*(volatile uint8_t*)0x10)

#define R17 (*(volatile uint8_t*)0x11)

#define R18 (*(volatile uint8_t*)0x12)

#define R19 (*(volatile uint8_t*)0x13)

#define R20 (*(volatile uint8_t*)0x14)

#define R21 (*(volatile uint8_t*)0x15)

#define R22 (*(volatile uint8_t*)0x16)

#define R23 (*(volatile uint8_t*)0x17)

#define R24 (*(volatile uint8_t*)0x18)

#define R25 (*(volatile uint8_t*)0x19)

#define R26 (*(volatile uint8_t*)0x1A)

#define R27 (*(volatile uint8_t*)0x1B)

#define R28 (*(volatile uint8_t*)0x1C)

#define R29 (*(volatile uint8_t*)0x1D)

#define R30 (*(volatile uint8_t*)0x1E)

#define R31 (*(volatile uint8_t*)0x1F)

#define SPL (*(volatile uint8_t*)0x5D)

#define SPH (*(volatile uint8_t*)0x5E)

#define SREG (*(volatile uint8_t*)0x5F)

// 内存段定义
// Program Flash Memory
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768

// Static RAM
#define SRAM_START 0x0100
#define SRAM_END 0x0AFF
#define SRAM_SIZE 2560

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
// Port B
#define PORTB_BASE 0x23
#define PORTB_PORTB (*(volatile uint8_t*)0x00000048)
#define PORTB_DDRB (*(volatile uint8_t*)0x00000047)
#define PORTB_PINB (*(volatile uint8_t*)0x00000046)

// Port C
#define PORTC_BASE 0x26
#define PORTC_PORTC (*(volatile uint8_t*)0x0000004E)
#define PORTC_DDRC (*(volatile uint8_t*)0x0000004D)
#define PORTC_PINC (*(volatile uint8_t*)0x0000004C)

// Port D
#define PORTD_BASE 0x29
#define PORTD_PORTD (*(volatile uint8_t*)0x00000054)
#define PORTD_DDRD (*(volatile uint8_t*)0x00000053)
#define PORTD_PIND (*(volatile uint8_t*)0x00000052)

// Port E
#define PORTE_BASE 0x2C
#define PORTE_PORTE (*(volatile uint8_t*)0x0000005A)
#define PORTE_DDRE (*(volatile uint8_t*)0x00000059)
#define PORTE_PINE (*(volatile uint8_t*)0x00000058)

// USART1
#define UART1_BASE 0xC8
#define UART1_UDR1 (*(volatile uint8_t*)0x00000196)
#define UART1_UCSR1A (*(volatile uint8_t*)0x00000190)
#define UART1_UCSR1B (*(volatile uint8_t*)0x00000191)
#define UART1_UCSR1C (*(volatile uint8_t*)0x00000192)
#define UART1_UBRR1 (*(volatile uint16_t*)0x00000194)

// USB Controller
#define USB_BASE 0xD0
#define USB_UDCON (*(volatile uint8_t*)0x000001A0)
#define USB_UDIEN (*(volatile uint8_t*)0x000001A1)
#define USB_UDINT (*(volatile uint8_t*)0x000001A2)

// 中断向量定义
#define INT0_VECTOR 1  // External Interrupt 0
#define INT1_VECTOR 2  // External Interrupt 1
#define INT2_VECTOR 3  // External Interrupt 2
#define INT3_VECTOR 4  // External Interrupt 3
#define INT4_VECTOR 5  // External Interrupt 4
#define INT5_VECTOR 6  // External Interrupt 5
#define INT6_VECTOR 7  // External Interrupt 6
#define PCINT0_VECTOR 8  // Pin Change Interrupt 0
#define USB_GENERAL_VECTOR 9  // USB General
#define USB_ENDPOINT_VECTOR 10  // USB Endpoint
#define WDT_VECTOR 11  // Watchdog Timeout
#define TIMER1_CAPT_VECTOR 12  // Timer1 Capture
#define TIMER1_COMPA_VECTOR 13  // Timer1 Compare A
#define TIMER1_COMPB_VECTOR 14  // Timer1 Compare B
#define TIMER1_OVF_VECTOR 15  // Timer1 Overflow
#define TIMER0_COMPA_VECTOR 16  // Timer0 Compare A
#define TIMER0_COMPB_VECTOR 17  // Timer0 Compare B
#define TIMER0_OVF_VECTOR 18  // Timer0 Overflow
#define SPI_STC_VECTOR 19  // SPI Transfer Complete
#define UART1_RX_VECTOR 20  // UART1 Receive
#define UART1_UDRE_VECTOR 21  // UART1 Data Register Empty
#define UART1_TX_VECTOR 22  // UART1 Transmit
#define ADC_VECTOR 23  // ADC Conversion Complete

void atmega32u4_init(void);

#ifdef __cplusplus
}
#endif

#endif // ATMEGA32U4_HPP
