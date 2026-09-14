#ifndef ATTINY13_HPP
#define ATTINY13_HPP

// ATtiny13寄存器定义
// 生成自: Atmel/AVR/ATtiny13
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 20000000 Hz

// 寄存器定义
#define R0 (*(volatile uint8_t*)0x00)

#define R1 (*(volatile uint8_t*)0x01)

#define R2 (*(volatile uint8_t*)0x02)

#define R16 (*(volatile uint8_t*)0x10)

#define R17 (*(volatile uint8_t*)0x11)

// XL
#define R26 (*(volatile uint8_t*)0x1A)

// XH
#define R27 (*(volatile uint8_t*)0x1B)

// YL
#define R28 (*(volatile uint8_t*)0x1C)

// YH
#define R29 (*(volatile uint8_t*)0x1D)

// ZL
#define R30 (*(volatile uint8_t*)0x1E)

// ZH
#define R31 (*(volatile uint8_t*)0x1F)

// Stack Pointer Low
#define SPL (*(volatile uint8_t*)0x5D)

// Stack Pointer High
#define SPH (*(volatile uint8_t*)0x5E)

// Status Register
#define SREG (*(volatile uint8_t*)0x5F)

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x03FF
#define FLASH_SIZE 1024

#define SRAM_START 0x0060
#define SRAM_END 0x009F
#define SRAM_SIZE 64

#define EEPROM_START 0x0000
#define EEPROM_END 0x003F
#define EEPROM_SIZE 64

#define IO_START 0x00
#define IO_END 0x1F
#define IO_SIZE 32

#define EXTIO_START 0x20
#define EXTIO_END 0x5F
#define EXTIO_SIZE 64

// 外设定义
// Port B (only port)
#define PORTB_BASE 0x18
#define PORTB_DDRB (*(volatile uint8_t*)0x0000002F)
#define PORTB_PORTB (*(volatile uint8_t*)0x00000030)
#define PORTB_PINB (*(volatile uint8_t*)0x00000031)
#define PORTB_PB0 0  // Port B bit 0
#define PORTB_PB1 1  // Port B bit 1
#define PORTB_PB2 2  // Port B bit 2
#define PORTB_PB3 3  // Port B bit 3
#define PORTB_PB4 4  // Port B bit 4
#define PORTB_PB5 5  // Port B bit 5

// 8-bit Timer/Counter0
#define TIMER0_BASE 0x33
#define TIMER0_TCCR0A (*(volatile uint8_t*)0x00000066)
#define TIMER0_TCCR0B (*(volatile uint8_t*)0x00000066)
#define TIMER0_TCNT0 (*(volatile uint8_t*)0x00000065)
#define TIMER0_OCR0A (*(volatile uint8_t*)0x00000069)
#define TIMER0_OCR0B (*(volatile uint8_t*)0x00000068)
#define TIMER0_TIMSK0 (*(volatile uint8_t*)0x0000006C)
#define TIMER0_TIFR0 (*(volatile uint8_t*)0x0000006B)

// Analog-to-Digital
#define ADC_BASE 0x04
#define ADC_ADMUX (*(volatile uint8_t*)0x0000000B)
#define ADC_ADCSRA (*(volatile uint8_t*)0x0000000A)
#define ADC_ADCL (*(volatile uint8_t*)0x00000008)
#define ADC_ADCH (*(volatile uint8_t*)0x00000009)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define INT0_VECTOR 2  // External Interrupt 0
#define PCINT0_VECTOR 3  // Pin Change Interrupt
#define TIM0_OVF_VECTOR 4  // Timer0 Overflow
#define TIM0_COMPA_VECTOR 5  // Timer0 Compare A
#define WDT_VECTOR 6  // Watchdog Timeout
#define ADC_VECTOR 7  // ADC Conversion Complete

void attiny13_init(void);

#ifdef __cplusplus
}
#endif

#endif // ATTINY13_HPP
