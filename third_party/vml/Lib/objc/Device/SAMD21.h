// SAMD21 设备定义 - Objective-C 头文件
// 生成自: Atmel (Microchip)/SAM D/SAMD21
// 版本: 
// 日期: 
// 作者: 
// 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
// CPU架构: ARM Cortex-M0+
// 位宽: 0位
// 时钟频率: 0 Hz

#ifndef SAMD21_DEVICE_H
#define SAMD21_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// Power Manager
#define PM_BASE 
#define PM_PM_CTRL_ADDR 0x40000400
// System Controller
#define SYSCTRL_BASE 
#define SYSCTRL_SYSCTRL_INTENCLR_ADDR 0x40000800
// Generic Clock Generator
#define GCLK_BASE 
#define GCLK_GCLK_CTRL_ADDR 0x40000C00
// Watchdog Timer
#define WDT_BASE 
#define WDT_WDT_CTRL_ADDR 0x40001000
// Real-Time Clock
#define RTC_BASE 
#define RTC_RTC_CTRL_ADDR 0x40001400
// External Interrupt Controller
#define EIC_BASE 
#define EIC_EIC_CTRL_ADDR 0x40001800
// Serial Communication Interface 0
#define SERCOM0_BASE 
#define SERCOM0_SERCOM0_I2CM_CTRLA_ADDR 0x42000800
// Analog-to-Digital Converter
#define ADC_BASE 
#define ADC_ADC_CTRLA_ADDR 0x42002000
// Digital-to-Analog Converter
#define DAC_BASE 
#define DAC_DAC_CTRLA_ADDR 0x42002400
// General Purpose I/O
#define PORT_BASE 
#define PORT_PORT_DIR_ADDR 0x41004400
// Timer/Counter 0
#define TC0_BASE 
#define TC0_TC0_CTRLA_ADDR 0x42002800
// USB Device Controller
#define USB_BASE 
#define USB_USB_CTRLA_ADDR 0x41005000

// 中断向量定义
#define INT_RESET 0  // Reset vector
#define INT_NONMASKABLEINT 1  // Non-maskable interrupt
#define INT_HARDFAULT 2  // Hard fault
#define INT_SVCALL 3  // Supervisor call
#define INT_PENDSV 4  // Pendable service call
#define INT_SYSTICK 5  // System tick timer
#define INT_PM 6  // Power Manager
#define INT_SYSCTRL 7  // System Controller
#define INT_WDT 8  // Watchdog Timer
#define INT_RTC 9  // Real-Time Clock
#define INT_EIC 10  // External Interrupt Controller
#define INT_NVMCTRL 11  // Non-Volatile Memory Controller
#define INT_DMAC 12  // Direct Memory Access Controller
#define INT_USB 13  // USB Device Controller
#define INT_EVSYS 14  // Event System
#define INT_SERCOM0 15  // Serial Communication Interface 0
#define INT_SERCOM1 16  // Serial Communication Interface 1
#define INT_SERCOM2 17  // Serial Communication Interface 2
#define INT_SERCOM3 18  // Serial Communication Interface 3
#define INT_SERCOM4 19  // Serial Communication Interface 4
#define INT_SERCOM5 20  // Serial Communication Interface 5
#define INT_TCC0 21  // Timer/Counter for Control 0
#define INT_TCC1 22  // Timer/Counter for Control 1
#define INT_TCC2 23  // Timer/Counter for Control 2
#define INT_TC3 24  // Timer/Counter 3
#define INT_TC4 25  // Timer/Counter 4
#define INT_TC5 26  // Timer/Counter 5
#define INT_TC6 27  // Timer/Counter 6
#define INT_TC7 28  // Timer/Counter 7
#define INT_ADC 29  // Analog-to-Digital Converter
#define INT_AC 30  // Analog Comparator
#define INT_DAC 31  // Digital-to-Analog Converter
#define INT_PTC 32  // Peripheral Touch Controller
#define INT_I2S 33  // Inter-IC Sound Interface

#endif /* SAMD21_DEVICE_H */
