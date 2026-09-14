// PIC16F84 设备定义 - Objective-C 头文件
// 生成自: Microchip Technology/PIC16/PIC16F84
// 版本: 
// 日期: 
// 作者: 
// 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
// CPU架构: PIC16
// 位宽: 0位
// 时钟频率: 0 Hz

#ifndef PIC16F84_DEVICE_H
#define PIC16F84_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// 8-bit timer/counter with prescaler
#define TIMER0_BASE 
#define TIMER0_TMR0_ADDR 0x01
// 16-bit timer/counter with prescaler
#define TIMER1_BASE 
#define TIMER1_TMR1L_ADDR 0x0E
#define TIMER1_TMR1H_ADDR 0x0F
#define TIMER1_T1CON_ADDR 0x10
#define TIMER1_T1CON_TMR1ON_BIT 0  // Timer1 On
#define TIMER1_T1CON_TMR1CS_BIT 1  // Timer1 Clock Source
#define TIMER1_T1CON_T1SYNC_BIT 2  // Timer1 External Clock Input Synchronization
#define TIMER1_T1CON_T1OSCEN_BIT 3  // Timer1 Oscillator Enable
#define TIMER1_T1CON_T1CKPS0_BIT 4  // Timer1 Input Clock Prescale Select bit 0
#define TIMER1_T1CON_T1CKPS1_BIT 5  // Timer1 Input Clock Prescale Select bit 1
// Watchdog Timer
#define WATCHDOG_BASE 
#define WATCHDOG_WDTCON_ADDR 0x07
#define WATCHDOG_WDTCON_SWDTEN_BIT 0  // Software Watchdog Timer Enable
// 64-byte EEPROM data memory
#define EEPROM_BASE 
#define EEPROM_EEDATA_ADDR 0x08
#define EEPROM_EEADR_ADDR 0x09
#define EEPROM_EECON1_ADDR 0x88
#define EEPROM_EECON2_ADDR 0x89
// General Purpose I/O
#define GPIO_BASE 
#define GPIO_PORTA_ADDR 0x05
#define GPIO_PORTB_ADDR 0x06
#define GPIO_TRISA_ADDR 0x85
#define GPIO_TRISB_ADDR 0x86

// 中断向量定义
#define INT_INT 4  // External interrupt on RB0/INT pin
#define INT_TMR0 4  // Timer0 overflow interrupt
#define INT_PORTB 4  // PORTB change interrupt (RB4-RB7)
#define INT_EEPROM 4  // EEPROM write complete interrupt

#endif /* PIC16F84_DEVICE_H */
