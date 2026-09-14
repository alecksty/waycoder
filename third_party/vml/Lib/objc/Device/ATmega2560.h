// ATmega2560 设备定义 - Objective-C 头文件
// 生成自: Atmel/AVR/ATmega2560
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

#ifndef ATMEGA2560_DEVICE_H
#define ATMEGA2560_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x01  // 
#define R2_ADDR 0x02  // 
#define SPL_ADDR 0x5D  // 
#define SPH_ADDR 0x5E  // 
#define SREG_ADDR 0x5F  // 

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x3FFFF
#define FLASH_SIZE 262144  // 
#define SRAM_START 0x0200
#define SRAM_END 0x21FF
#define SRAM_SIZE 8192  // 
#define EEPROM_START 0x0000
#define EEPROM_END 0x0FFF
#define EEPROM_SIZE 4096  // 
#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64  // 
#define EXTIO_START 0x40
#define EXTIO_END 0xFF
#define EXTIO_SIZE 192  // 

// 外设定义
// Port A
#define PORTA_BASE 0x22
#define PORTA_DDRA_ADDR 0x21
#define PORTA_PORTA_ADDR 0x22
#define PORTA_PINA_ADDR 0x20
// Port B
#define PORTB_BASE 0x25
#define PORTB_DDRB_ADDR 0x24
#define PORTB_PORTB_ADDR 0x25
#define PORTB_PINB_ADDR 0x23
// Port C
#define PORTC_BASE 0x28
#define PORTC_DDRC_ADDR 0x27
#define PORTC_PORTC_ADDR 0x28
#define PORTC_PINC_ADDR 0x26
// Port D
#define PORTD_BASE 0x2B
#define PORTD_DDRD_ADDR 0x2A
#define PORTD_PORTD_ADDR 0x2B
#define PORTD_PIND_ADDR 0x29
// Port E
#define PORTE_BASE 0x2E
#define PORTE_DDRE_ADDR 0x2D
#define PORTE_PORTE_ADDR 0x2E
#define PORTE_PINE_ADDR 0x2C
// Port F
#define PORTF_BASE 0x31
#define PORTF_DDRF_ADDR 0x30
#define PORTF_PORTF_ADDR 0x31
#define PORTF_PINF_ADDR 0x2F
// Port G
#define PORTG_BASE 0x34
#define PORTG_DDRG_ADDR 0x33
#define PORTG_PORTG_ADDR 0x34
#define PORTG_PING_ADDR 0x32
// USART 0
#define USART0_BASE 0xC0
#define USART0_UDR0_ADDR 0xC6
#define USART0_UCSR0A_ADDR 0xC0
#define USART0_UCSR0B_ADDR 0xC1
#define USART0_UCSR0C_ADDR 0xC2
#define USART0_UBRR0L_ADDR 0xC4
#define USART0_UBRR0H_ADDR 0xC5

// 中断向量定义
#define INT_RESET 1  // 
#define INT_INT0 2  // 
#define INT_INT1 3  // 
#define INT_INT2 4  // 
#define INT_INT3 5  // 
#define INT_INT4 6  // 
#define INT_INT5 7  // 
#define INT_INT6 8  // 
#define INT_INT7 9  // 
#define INT_PCINT0 10  // 
#define INT_PCINT1 11  // 
#define INT_PCINT2 12  // 
#define INT_WDT 13  // 
#define INT_TIM2_COMPA 14  // 
#define INT_TIM2_COMPB 15  // 
#define INT_TIM2_OVF 16  // 
#define INT_TIM1_CAPT 17  // 
#define INT_TIM1_COMPA 18  // 
#define INT_TIM1_COMPB 19  // 
#define INT_TIM1_OVF 20  // 
#define INT_TIM0_COMPA 21  // 
#define INT_TIM0_COMPB 22  // 
#define INT_TIM0_OVF 23  // 
#define INT_SPI_STC 24  // 
#define INT_USART0_RX 25  // 
#define INT_USART0_UDRE 26  // 
#define INT_USART0_TX 27  // 

#endif /* ATMEGA2560_DEVICE_H */
