// PIC18F452 设备定义 - Objective-C 头文件
// 生成自: Microchip Technology/PIC18/PIC18F452
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

#ifndef PIC18F452_DEVICE_H
#define PIC18F452_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define WREG_ADDR 0xFE8  // Working Register
#define STATUS_ADDR 0xFD8  // Status Register
#define BSR_ADDR 0xFE0  // Bank Select Register
#define PCL_ADDR 0xFF9  // Program Counter Low
#define PCLATH_ADDR 0xFFA  // Program Counter Latch High
#define PCLATU_ADDR 0xFFB  // Program Counter Latch Upper
#define TOSU_ADDR 0xFFF  // Top of Stack Upper
#define TOSH_ADDR 0xFFE  // Top of Stack High
#define TOSL_ADDR 0xFFD  // Top of Stack Low

// 外设定义
// Port A
#define PORTA_BASE 
#define PORTA_PORTA_ADDR 0xF80
#define PORTA_TRISA_ADDR 0xF92
#define PORTA_LATA_ADDR 0xF89
// Port B
#define PORTB_BASE 
#define PORTB_PORTB_ADDR 0xF81
#define PORTB_TRISB_ADDR 0xF93
#define PORTB_LATB_ADDR 0xF8A
// Port C
#define PORTC_BASE 
#define PORTC_PORTC_ADDR 0xF82
#define PORTC_TRISC_ADDR 0xF94
#define PORTC_LATC_ADDR 0xF8B
// Port D
#define PORTD_BASE 
#define PORTD_PORTD_ADDR 0xF83
#define PORTD_TRISD_ADDR 0xF95
#define PORTD_LATD_ADDR 0xF8C
// Port E
#define PORTE_BASE 
#define PORTE_PORTE_ADDR 0xF84
#define PORTE_TRISE_ADDR 0xF96
#define PORTE_LATE_ADDR 0xF8D
// Timer0
#define TMR0_BASE 
#define TMR0_TMR0L_ADDR 0xFD6
#define TMR0_TMR0H_ADDR 0xFD7
#define TMR0_T0CON_ADDR 0xFD5
// Timer1
#define TMR1_BASE 
#define TMR1_TMR1L_ADDR 0xFCE
#define TMR1_TMR1H_ADDR 0xFCF
#define TMR1_T1CON_ADDR 0xFCD
// Timer2
#define TMR2_BASE 
#define TMR2_TMR2_ADDR 0xFCC
#define TMR2_PR2_ADDR 0xFCB
#define TMR2_T2CON_ADDR 0xFCA
// Timer3
#define TMR3_BASE 
#define TMR3_TMR3L_ADDR 0xFB2
#define TMR3_TMR3H_ADDR 0xFB3
#define TMR3_T3CON_ADDR 0xFB1
// Analog-to-Digital Converter
#define ADC_BASE 
#define ADC_ADRESL_ADDR 0xFC3
#define ADC_ADRESH_ADDR 0xFC4
#define ADC_ADCON0_ADDR 0xFC2
#define ADC_ADCON1_ADDR 0xFC1
// Universal Synchronous Asynchronous Receiver Transmitter
#define USART_BASE 
#define USART_TXREG_ADDR 0xFAC
#define USART_RCREG_ADDR 0xFAB
#define USART_SPBRG_ADDR 0xFAF
#define USART_TXSTA_ADDR 0xFAD
#define USART_RCSTA_ADDR 0xFAE
// Synchronous Serial Port
#define SSP_BASE 
#define SSP_SSPBUF_ADDR 0xFC9
#define SSP_SSPADD_ADDR 0xFC8
#define SSP_SSPSTAT_ADDR 0xFC7
#define SSP_SSPCON1_ADDR 0xFC6
#define SSP_SSPCON2_ADDR 0xFC5
// Capture/Compare/PWM 1
#define CCP1_BASE 
#define CCP1_CCPR1L_ADDR 0xFBE
#define CCP1_CCPR1H_ADDR 0xFBF
#define CCP1_CCP1CON_ADDR 0xFBD
// Capture/Compare/PWM 2
#define CCP2_BASE 
#define CCP2_CCPR2L_ADDR 0xFBA
#define CCP2_CCPR2H_ADDR 0xFBB
#define CCP2_CCP2CON_ADDR 0xFB9

// 中断向量定义
#define INT_HIGH_PRIORITY 8  // High priority interrupt
#define INT_LOW_PRIORITY 24  // Low priority interrupt
#define INT_RESET 0  // Reset vector

#endif /* PIC18F452_DEVICE_H */
