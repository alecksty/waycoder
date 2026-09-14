// ATmega32U4 设备定义 - Objective-C 头文件
// 生成自: Atmel/AVR/ATmega32U4
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

#ifndef ATMEGA32U4_DEVICE_H
#define ATMEGA32U4_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x01  // 
#define R2_ADDR 0x02  // 
#define R3_ADDR 0x03  // 
#define R4_ADDR 0x04  // 
#define R5_ADDR 0x05  // 
#define R6_ADDR 0x06  // 
#define R7_ADDR 0x07  // 
#define R8_ADDR 0x08  // 
#define R9_ADDR 0x09  // 
#define R10_ADDR 0x0A  // 
#define R11_ADDR 0x0B  // 
#define R12_ADDR 0x0C  // 
#define R13_ADDR 0x0D  // 
#define R14_ADDR 0x0E  // 
#define R15_ADDR 0x0F  // 
#define R16_ADDR 0x10  // 
#define R17_ADDR 0x11  // 
#define R18_ADDR 0x12  // 
#define R19_ADDR 0x13  // 
#define R20_ADDR 0x14  // 
#define R21_ADDR 0x15  // 
#define R22_ADDR 0x16  // 
#define R23_ADDR 0x17  // 
#define R24_ADDR 0x18  // 
#define R25_ADDR 0x19  // 
#define R26_ADDR 0x1A  // 
#define R27_ADDR 0x1B  // 
#define R28_ADDR 0x1C  // 
#define R29_ADDR 0x1D  // 
#define R30_ADDR 0x1E  // 
#define R31_ADDR 0x1F  // 
#define SPL_ADDR 0x5D  // 
#define SPH_ADDR 0x5E  // 
#define SREG_ADDR 0x5F  // 

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768  // Program Flash Memory
#define SRAM_START 0x0100
#define SRAM_END 0x0AFF
#define SRAM_SIZE 2560  // Static RAM
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
// Port B
#define PORTB_BASE 0x23
#define PORTB_PORTB_ADDR 0x25
#define PORTB_DDRB_ADDR 0x24
#define PORTB_PINB_ADDR 0x23
// Port C
#define PORTC_BASE 0x26
#define PORTC_PORTC_ADDR 0x28
#define PORTC_DDRC_ADDR 0x27
#define PORTC_PINC_ADDR 0x26
// Port D
#define PORTD_BASE 0x29
#define PORTD_PORTD_ADDR 0x2B
#define PORTD_DDRD_ADDR 0x2A
#define PORTD_PIND_ADDR 0x29
// Port E
#define PORTE_BASE 0x2C
#define PORTE_PORTE_ADDR 0x2E
#define PORTE_DDRE_ADDR 0x2D
#define PORTE_PINE_ADDR 0x2C
// USART1
#define UART1_BASE 0xC8
#define UART1_UDR1_ADDR 0xCE
#define UART1_UCSR1A_ADDR 0xC8
#define UART1_UCSR1B_ADDR 0xC9
#define UART1_UCSR1C_ADDR 0xCA
#define UART1_UBRR1_ADDR 0xCC
// USB Controller
#define USB_BASE 0xD0
#define USB_UDCON_ADDR 0xD0
#define USB_UDIEN_ADDR 0xD1
#define USB_UDINT_ADDR 0xD2

// 中断向量定义
#define INT_INT0 1  // External Interrupt 0
#define INT_INT1 2  // External Interrupt 1
#define INT_INT2 3  // External Interrupt 2
#define INT_INT3 4  // External Interrupt 3
#define INT_INT4 5  // External Interrupt 4
#define INT_INT5 6  // External Interrupt 5
#define INT_INT6 7  // External Interrupt 6
#define INT_PCINT0 8  // Pin Change Interrupt 0
#define INT_USB_GENERAL 9  // USB General
#define INT_USB_ENDPOINT 10  // USB Endpoint
#define INT_WDT 11  // Watchdog Timeout
#define INT_TIMER1_CAPT 12  // Timer1 Capture
#define INT_TIMER1_COMPA 13  // Timer1 Compare A
#define INT_TIMER1_COMPB 14  // Timer1 Compare B
#define INT_TIMER1_OVF 15  // Timer1 Overflow
#define INT_TIMER0_COMPA 16  // Timer0 Compare A
#define INT_TIMER0_COMPB 17  // Timer0 Compare B
#define INT_TIMER0_OVF 18  // Timer0 Overflow
#define INT_SPI_STC 19  // SPI Transfer Complete
#define INT_UART1_RX 20  // UART1 Receive
#define INT_UART1_UDRE 21  // UART1 Data Register Empty
#define INT_UART1_TX 22  // UART1 Transmit
#define INT_ADC 23  // ADC Conversion Complete

#endif /* ATMEGA32U4_DEVICE_H */
