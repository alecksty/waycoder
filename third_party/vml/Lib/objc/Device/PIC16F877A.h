// PIC16F877A 设备定义 - Objective-C 头文件
// 生成自: Microchip/PIC/PIC16F877A
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
// CPU架构: PIC16
// 位宽: 8位
// 时钟频率: 4000000 Hz

#ifndef PIC16F877A_DEVICE_H
#define PIC16F877A_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define W_ADDR 0x00  // Working Register
#define STATUS_ADDR 0x03  // Status Register
#define STATUS_C_BIT 0  // Carry flag
#define STATUS_DC_BIT 1  // Digit carry flag
#define STATUS_Z_BIT 2  // Zero flag
#define STATUS_PD_BIT 3  // Power-down flag
#define STATUS_TO_BIT 4  // Time-out flag
#define STATUS_RP_BIT 5  // Register bank select
#define STATUS_IRP_BIT 7  // Indirect register bank select
#define INTCON_ADDR 0x0B  // Interrupt Control Register
#define INTCON_RBIF_BIT 0  // PORTB change interrupt flag
#define INTCON_INTF_BIT 1  // External interrupt flag
#define INTCON_TMR0IF_BIT 2  // TMR0 overflow interrupt flag
#define INTCON_RBIE_BIT 3  // PORTB change interrupt enable
#define INTCON_INTE_BIT 4  // External interrupt enable
#define INTCON_TMR0IE_BIT 5  // TMR0 overflow interrupt enable
#define INTCON_PEIE_BIT 6  // Peripheral interrupt enable
#define INTCON_GIE_BIT 7  // Global interrupt enable
#define PORTB_ADDR 0x06  // PORT B
#define TRISB_ADDR 0x86  // TRIS B
#define PORTC_ADDR 0x07  // PORT C
#define TRISC_ADDR 0x87  // TRIS C
#define PORTD_ADDR 0x08  // PORT D
#define TRISD_ADDR 0x88  // TRIS D
#define PORTE_ADDR 0x09  // PORT E
#define TRISE_ADDR 0x89  // TRIS E
#define TMR0_ADDR 0x01  // Timer 0
#define OPTION_REG_ADDR 0x81  // Option Register
#define PCL_ADDR 0x02  // Program Counter Low
#define PCLATH_ADDR 0x0A  // Program Counter Latch High
#define FSR_ADDR 0x04  // File Select Register
#define EEDATA_ADDR 0x10C  // EEPROM Data
#define EEADR_ADDR 0x10D  // EEPROM Address
#define EECON1_ADDR 0x18C  // EEPROM Control 1
#define EECON1_RD_BIT 0  // Read control
#define EECON1_WR_BIT 1  // Write control
#define EECON1_WREN_BIT 2  // Write enable
#define EECON1_WRERR_BIT 3  // Write error flag
#define EECON1_EEPGD_BIT 7  // EEPROM program/data select
#define EECON2_ADDR 0x18D  // EEPROM Control 2
#define ADRESH_ADDR 0x1E  // A/D Result High
#define ADRESL_ADDR 0x1F  // A/D Result Low
#define ADCON0_ADDR 0x1F  // A/D Control 0
#define ADCON0_ADON_BIT 0  // A/D enable
#define ADCON0_GO_DONE_BIT 2  // A/D conversion status
#define ADCON0_CHS_BIT 3  // Channel select
#define ADCON1_ADDR 0x9F  // A/D Control 1
#define SSPSTAT_ADDR 0x94  // MSSP Status
#define SSPCON_ADDR 0x14  // MSSP Control
#define SSPBUF_ADDR 0x13  // SSP Buffer
#define TXREG_ADDR 0x19  // USART Transmit Register
#define RCREG_ADDR 0x1A  // USART Receive Register
#define SPBRG_ADDR 0x99  // Baud Rate Generator
#define TXSTA_ADDR 0x98  // TX Status and Control
#define RCSTA_ADDR 0x18  // RX Status and Control
#define CCP1CON_ADDR 0x17  // CCP1 Control
#define CCPR1L_ADDR 0x15  // CCP1 Low
#define CCPR1H_ADDR 0x16  // CCP1 High
#define CCP2CON_ADDR 0x1D  // CCP2 Control
#define CCPR2L_ADDR 0x1B  // CCP2 Low
#define CCPR2H_ADDR 0x1C  // CCP2 High
#define T1CON_ADDR 0x10  // Timer 1 Control
#define TMR1L_ADDR 0x0E  // Timer 1 Low
#define TMR1H_ADDR 0x0F  // Timer 1 High
#define T2CON_ADDR 0x12  // Timer 2 Control
#define TMR2_ADDR 0x11  // Timer 2
#define PR2_ADDR 0x92  // Timer 2 Period

// 内存段定义
#define PROGRAM_START 0x0000
#define PROGRAM_END 0x1FFF
#define PROGRAM_SIZE 8192  // Program Memory (8KB)
#define DATA_START 0x20
#define DATA_END 0x7F
#define DATA_SIZE 96  // General Purpose RAM Bank 0
#define SRAM_START 0xA0
#define SRAM_END 0xFF
#define SRAM_SIZE 96  // General Purpose RAM Bank 1
#define EEPROM_START 0x2100
#define EEPROM_END 0x21FF
#define EEPROM_SIZE 256  // EEPROM Data Memory

// 外设定义
// Port B
#define GPIO_PORTB_BASE 0x06
#define GPIO_PORTB_PORTB_ADDR 0x06
#define GPIO_PORTB_TRISB_ADDR 0x86
// Port C
#define GPIO_PORTC_BASE 0x07
#define GPIO_PORTC_PORTC_ADDR 0x07
#define GPIO_PORTC_TRISC_ADDR 0x87
// Port D
#define GPIO_PORTD_BASE 0x08
#define GPIO_PORTD_PORTD_ADDR 0x08
#define GPIO_PORTD_TRISD_ADDR 0x88
// Timer 0
#define TIMER0_BASE 0x01
#define TIMER0_TMR0_ADDR 0x01
#define TIMER0_OPTION_REG_ADDR 0x81
// Timer 1
#define TIMER1_BASE 0x0E
#define TIMER1_T1CON_ADDR 0x10
#define TIMER1_TMR1L_ADDR 0x0E
#define TIMER1_TMR1H_ADDR 0x0F
// Timer 2
#define TIMER2_BASE 0x11
#define TIMER2_T2CON_ADDR 0x12
#define TIMER2_TMR2_ADDR 0x11
#define TIMER2_PR2_ADDR 0x92
// A/D Converter
#define ADC_BASE 0x1E
#define ADC_ADRESH_ADDR 0x1E
#define ADC_ADRESL_ADDR 0x9F
#define ADC_ADCON0_ADDR 0x1F
#define ADC_ADCON1_ADDR 0x9F
// Master Synchronous Serial Port
#define MSSP_BASE 0x13
#define MSSP_SSPSTAT_ADDR 0x94
#define MSSP_SSPCON_ADDR 0x14
#define MSSP_SSPBUF_ADDR 0x13
// USART
#define USART_BASE 0x19
#define USART_TXREG_ADDR 0x19
#define USART_RCREG_ADDR 0x1A
#define USART_SPBRG_ADDR 0x99
#define USART_TXSTA_ADDR 0x98
#define USART_RCSTA_ADDR 0x18
// Capture/Compare/PWM 1
#define CCP1_BASE 0x15
#define CCP1_CCP1CON_ADDR 0x17
#define CCP1_CCPR1L_ADDR 0x15
#define CCP1_CCPR1H_ADDR 0x16
// Capture/Compare/PWM 2
#define CCP2_BASE 0x1B
#define CCP2_CCP2CON_ADDR 0x1D
#define CCP2_CCPR2L_ADDR 0x1B
#define CCP2_CCPR2H_ADDR 0x1C

// 中断向量定义
#define INT_INT 1  // External Interrupt
#define INT_TMR0 2  // Timer 0 Overflow
#define INT_RB 3  // PORTB Change
#define INT_CCP1 4  // CCP1
#define INT_CCP2 5  // CCP2
#define INT_TMR1 6  // Timer 1 Overflow
#define INT_TMR2 8  // Timer 2 Overflow
#define INT_SPI 9  // SPI/I2C
#define INT_SCI 10  // USART Receive
#define INT_SCI 11  // USART Transmit
#define INT_ADC 12  // A/D Converter
#define INT_EEPROM 13  // EEPROM Write Complete

// 引脚定义
#define PIN_MCLR_VPP 1  // Master Clear (Reset)
#define PIN_RA0_AN0 2  // PORTA Bit 0 / Analog 0
#define PIN_RA1_AN1 3  // PORTA Bit 1 / Analog 1
#define PIN_RA2_AN2_VREF 4  // PORTA Bit 2 / Analog 2 / VREF-
#define PIN_RA3_AN3_VREFP 5  // PORTA Bit 3 / Analog 3 / VREF+
#define PIN_RA4_T0CKI 6  // PORTA Bit 4 / Timer 0 Clock Input
#define PIN_RA5_AN4_SS 7  // PORTA Bit 4 / Analog 4 / SPI Slave Select
#define PIN_RE0_RD_AN5 8  // PORTE Bit 0 / Read Control / Analog 5
#define PIN_RE1_WR_AN6 9  // PORTE Bit 1 / Write Control / Analog 6
#define PIN_RE2_CS_AN7 10  // PORTE Bit 2 / Chip Select / Analog 7
#define PIN_VDD 11  // Positive Supply
#define PIN_VSS 12  // Ground
#define PIN_OSC1_CLKIN 13  // Oscillator/Clock Input
#define PIN_OSC2_CLKOUT 14  // Oscillator/Clock Output
#define PIN_RC0_T1OSO 15  // PORTC Bit 0 / Timer 1 Oscillator
#define PIN_RC1_T1OSI 16  // PORTC Bit 1 / Timer 1 Oscillator
#define PIN_RC2_CCP1 17  // PORTC Bit 2 / Capture/Compare/PWM 1
#define PIN_RC3_SCK_SCL 18  // PORTC Bit 3 / SPI Clock / I2C Clock
#define PIN_RC4_SDI_SDA 23  // PORTC Bit 4 / SPI Data In / I2C Data
#define PIN_RC5_SDO 24  // PORTC Bit 5 / SPI Data Out
#define PIN_RC6_TX 25  // PORTC Bit 6 / USART Transmit
#define PIN_RC7_RX 26  // PORTC Bit 7 / USART Receive
#define PIN_RD0 19  // PORTD Bit 0
#define PIN_RD1 20  // PORTD Bit 1
#define PIN_RD2 21  // PORTD Bit 2
#define PIN_RD3 22  // PORTD Bit 3
#define PIN_RD4 27  // PORTD Bit 4
#define PIN_RD5 28  // PORTD Bit 5
#define PIN_RD6 29  // PORTD Bit 6
#define PIN_RD7 30  // PORTD Bit 7
#define PIN_VSS 31  // Ground
#define PIN_VDD 32  // Positive Supply
#define PIN_RB0_INT 33  // PORTB Bit 0 / External Interrupt
#define PIN_RB1 34  // PORTB Bit 1
#define PIN_RB2 35  // PORTB Bit 2
#define PIN_RB3_PGC 36  // PORTB Bit 3 / Programming Clock
#define PIN_RB4_PGD 37  // PORTB Bit 4 / Programming Data
#define PIN_RB5 38  // PORTB Bit 5
#define PIN_RB6_PGC 39  // PORTB Bit 6 / Programming Clock
#define PIN_RB7_PGD 40  // PORTB Bit 7 / Programming Data

#endif /* PIC16F877A_DEVICE_H */
