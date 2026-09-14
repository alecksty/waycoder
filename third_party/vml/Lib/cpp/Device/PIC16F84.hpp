#ifndef PIC16F84_HPP
#define PIC16F84_HPP

// PIC16F84寄存器定义
// 生成自: Microchip Technology/PIC16/PIC16F84
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: PIC16
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// 8-bit timer/counter with prescaler
#define TIMER0_BASE 
#define TIMER0_TMR0 (*(volatile uint64_t*)0x00000001)

// 16-bit timer/counter with prescaler
#define TIMER1_BASE 
#define TIMER1_TMR1L (*(volatile uint64_t*)0x0000000E)
#define TIMER1_TMR1H (*(volatile uint64_t*)0x0000000F)
#define TIMER1_T1CON (*(volatile uint64_t*)0x00000010)
#define TIMER1_T1CON_TMR1ON 0  // Timer1 On
#define TIMER1_T1CON_TMR1CS 1  // Timer1 Clock Source
#define TIMER1_T1CON_T1SYNC 2  // Timer1 External Clock Input Synchronization
#define TIMER1_T1CON_T1OSCEN 3  // Timer1 Oscillator Enable
#define TIMER1_T1CON_T1CKPS0 4  // Timer1 Input Clock Prescale Select bit 0
#define TIMER1_T1CON_T1CKPS1 5  // Timer1 Input Clock Prescale Select bit 1

// Watchdog Timer
#define WATCHDOG_BASE 
#define WATCHDOG_WDTCON (*(volatile uint64_t*)0x00000007)
#define WATCHDOG_WDTCON_SWDTEN 0  // Software Watchdog Timer Enable

// 64-byte EEPROM data memory
#define EEPROM_BASE 
#define EEPROM_EEDATA (*(volatile uint64_t*)0x00000008)
#define EEPROM_EEADR (*(volatile uint64_t*)0x00000009)
#define EEPROM_EECON1 (*(volatile uint64_t*)0x00000088)
#define EEPROM_EECON2 (*(volatile uint64_t*)0x00000089)

// General Purpose I/O
#define GPIO_BASE 
#define GPIO_PORTA (*(volatile uint64_t*)0x00000005)
#define GPIO_PORTB (*(volatile uint64_t*)0x00000006)
#define GPIO_TRISA (*(volatile uint64_t*)0x00000085)
#define GPIO_TRISB (*(volatile uint64_t*)0x00000086)

// 中断向量定义
#define INT_VECTOR 4  // External interrupt on RB0/INT pin
#define TMR0_VECTOR 4  // Timer0 overflow interrupt
#define PORTB_VECTOR 4  // PORTB change interrupt (RB4-RB7)
#define EEPROM_VECTOR 4  // EEPROM write complete interrupt

void pic16f84_init(void);

#ifdef __cplusplus
}
#endif

#endif // PIC16F84_HPP
