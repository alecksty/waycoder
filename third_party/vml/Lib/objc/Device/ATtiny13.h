// ATtiny13 设备定义 - Objective-C 头文件
// 生成自: Atmel/AVR/ATtiny13
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 20000000 Hz

#ifndef ATTINY13_DEVICE_H
#define ATTINY13_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x01  // 
#define R2_ADDR 0x02  // 
#define R16_ADDR 0x10  // 
#define R17_ADDR 0x11  // 
#define R26_ADDR 0x1A  // XL
#define R27_ADDR 0x1B  // XH
#define R28_ADDR 0x1C  // YL
#define R29_ADDR 0x1D  // YH
#define R30_ADDR 0x1E  // ZL
#define R31_ADDR 0x1F  // ZH
#define SPL_ADDR 0x5D  // Stack Pointer Low
#define SPH_ADDR 0x5E  // Stack Pointer High
#define SREG_ADDR 0x5F  // Status Register

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x03FF
#define FLASH_SIZE 1024  // 
#define SRAM_START 0x0060
#define SRAM_END 0x009F
#define SRAM_SIZE 64  // 
#define EEPROM_START 0x0000
#define EEPROM_END 0x003F
#define EEPROM_SIZE 64  // 
#define IO_START 0x00
#define IO_END 0x1F
#define IO_SIZE 32  // 
#define EXTIO_START 0x20
#define EXTIO_END 0x5F
#define EXTIO_SIZE 64  // 

// 外设定义
// Port B (only port)
#define PORTB_BASE 0x18
#define PORTB_DDRB_ADDR 0x17
#define PORTB_PORTB_ADDR 0x18
#define PORTB_PINB_ADDR 0x19
// 8-bit Timer/Counter0
#define TIMER0_BASE 0x33
#define TIMER0_TCCR0A_ADDR 0x33
#define TIMER0_TCCR0B_ADDR 0x33
#define TIMER0_TCNT0_ADDR 0x32
#define TIMER0_OCR0A_ADDR 0x36
#define TIMER0_OCR0B_ADDR 0x35
#define TIMER0_TIMSK0_ADDR 0x39
#define TIMER0_TIFR0_ADDR 0x38
// Analog-to-Digital
#define ADC_BASE 0x04
#define ADC_ADMUX_ADDR 0x07
#define ADC_ADCSRA_ADDR 0x06
#define ADC_ADCL_ADDR 0x04
#define ADC_ADCH_ADDR 0x05

// 中断向量定义
#define INT_RESET 1  // 
#define INT_INT0 2  // External Interrupt 0
#define INT_PCINT0 3  // Pin Change Interrupt
#define INT_TIM0_OVF 4  // Timer0 Overflow
#define INT_TIM0_COMPA 5  // Timer0 Compare A
#define INT_WDT 6  // Watchdog Timeout
#define INT_ADC 7  // ADC Conversion Complete

#endif /* ATTINY13_DEVICE_H */
