// PIC18F4550 设备定义 - Objective-C 头文件
// 生成自: Microchip/PIC18/PIC18F4550
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

#ifndef PIC18F4550_DEVICE_H
#define PIC18F4550_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define W_ADDR 0x0E  // Working Register
#define STATUS_ADDR 0xFD8  // Status Register
#define STATUS_C_BIT 0  // Carry Flag
#define STATUS_DC_BIT 1  // Digit Carry Flag
#define STATUS_Z_BIT 2  // Zero Flag
#define STATUS_PD_BIT 3  // Power-Down Flag
#define STATUS_TO_BIT 4  // Time-out Flag
#define STATUS_RP_BIT 0  // Register Bank Select
#define STATUS_IRP_BIT 7  // Indirect Register Bank Select
#define BSR_ADDR 0xFE0  // Bank Select Register
#define PORTA_ADDR 0xF80  // Port A
#define PORTB_ADDR 0xF81  // Port B
#define PORTC_ADDR 0xF82  // Port C
#define PORTD_ADDR 0xF83  // Port D
#define PORTE_ADDR 0xF84  // Port E
#define TRISA_ADDR 0xF92  // Tri-state Port A
#define TRISB_ADDR 0xF93  // Tri-state Port B
#define TRISC_ADDR 0xF94  // Tri-state Port C
#define TRISD_ADDR 0xF95  // Tri-state Port D
#define TRISE_ADDR 0xF96  // Tri-state Port E
#define LATA_ADDR 0xF89  // Latch Port A
#define LATB_ADDR 0xF8A  // Latch Port B
#define LATC_ADDR 0xF8B  // Latch Port C
#define LATD_ADDR 0xF8C  // Latch Port D
#define LATE_ADDR 0xF8D  // Latch Port E
#define INTCON_ADDR 0xFF2  // Interrupt Control
#define INTCON_RBIF_BIT 0  // Port B Interrupt Flag
#define INTCON_INT0IF_BIT 1  // INT0 Interrupt Flag
#define INTCON_TMR0IF_BIT 2  // Timer 0 Interrupt Flag
#define INTCON_RBIE_BIT 3  // Port B Interrupt Enable
#define INTCON_INT0IE_BIT 4  // INT0 Interrupt Enable
#define INTCON_TMR0IE_BIT 5  // Timer 0 Interrupt Enable
#define INTCON_PEIE_BIT 6  // Peripheral Interrupt Enable
#define INTCON_GIE_BIT 7  // Global Interrupt Enable
#define PIR1_ADDR 0xF9E  // Peripheral Interrupt 1
#define PIR2_ADDR 0xF9F  // Peripheral Interrupt 2
#define PIE1_ADDR 0xF9D  // Peripheral Interrupt Enable 1
#define PIE2_ADDR 0xF9C  // Peripheral Interrupt Enable 2
#define IPR1_ADDR 0xF9B  // Interrupt Priority 1
#define IPR2_ADDR 0xF9A  // Interrupt Priority 2
#define RCON_ADDR 0xFD0  // Reset Control
#define RCON_NOT_TO_BIT 3  // Time-out Flag
#define RCON_NOT_PD_BIT 4  // Power-Down Flag
#define RCON_NOT_RI_BIT 5  // RESET Flag
#define RCON_NOT_POR_BIT 6  // Power-on Reset Flag
#define RCON_NOT_BOR_BIT 7  // Brown-out Reset Flag
#define T0CON_ADDR 0xFD1  // Timer 0 Control
#define TMR0_ADDR 0xFD6  // Timer 0 Register
#define T1CON_ADDR 0xFCD  // Timer 1 Control
#define TMR1_ADDR 0xFCE  // Timer 1 Register High
#define TMR1L_ADDR 0xFCF  // Timer 1 Register Low
#define T2CON_ADDR 0xFCA  // Timer 2 Control
#define TMR2_ADDR 0xFCB  // Timer 2 Register
#define T3CON_ADDR 0xFB1  // Timer 3 Control
#define TMR3_ADDR 0xFB3  // Timer 3 Register High
#define TMR3L_ADDR 0xFB2  // Timer 3 Register Low
#define SSPCON1_ADDR 0xFC6  // SSP Control 1
#define SSPCON2_ADDR 0xFC5  // SSP Control 2
#define SSPSTAT_ADDR 0xFC7  // SSP Status
#define SSPBUF_ADDR 0xFC9  // SSP Buffer
#define SSPOR_ADDR 0xFC8  // SSP Shift Register
#define ADCON0_ADDR 0xFC2  // A/D Control 0
#define ADCON1_ADDR 0xFC1  // A/D Control 1
#define ADCON2_ADDR 0xFC0  // A/D Control 2
#define ADRES_ADDR 0xFC3  // A/D Result
#define ADRESL_ADDR 0xFC4  // A/D Result Low
#define CCP1CON_ADDR 0xFD4  // CCP 1 Control
#define CCPR1_ADDR 0xFD6  // CCP 1 Register High
#define CCPR1L_ADDR 0xFD5  // CCP 1 Register Low
#define CCP2CON_ADDR 0xFBA  // CCP 2 Control
#define CCPR2_ADDR 0xFBB  // CCP 2 Register High
#define CCPR2L_ADDR 0xFBC  // CCP 2 Register Low
#define USBCON_ADDR 0xF75  // USB Control
#define USBSTAT_ADDR 0xF74  // USB Status
#define UIE_ADDR 0xF73  // USB Interrupt Enable
#define UIR_ADDR 0xF72  // USB Interrupt Flag
#define UCON_ADDR 0xF71  // USB Control
#define USTAT_ADDR 0xF70  // USB Status
#define UEP0_ADDR 0xF60  // USB Endpoint 0
#define UEP1_ADDR 0xF61  // USB Endpoint 1
#define UEP2_ADDR 0xF62  // USB Endpoint 2
#define UEP3_ADDR 0xF63  // USB Endpoint 3
#define UEP4_ADDR 0xF64  // USB Endpoint 4

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768  // Program Flash (32KB)
#define EEPROM_START 0xF00000
#define EEPROM_END 0xF000FF
#define EEPROM_SIZE 256  // EEPROM (256B)
#define SRAM_START 0x0000
#define SRAM_END 0x07FF
#define SRAM_SIZE 2048  // SRAM (2KB)
#define ACCESS_START 0x0000
#define ACCESS_END 
#define ACCESS_SIZE 1  // Access Bank

// 外设定义
// Port A
#define PORTA_BASE 0xF80
#define PORTA_PORT_ADDR 0xF80
#define PORTA_TRIS_ADDR 0xF92
#define PORTA_LAT_ADDR 0xF89
// Port B
#define PORTB_BASE 0xF81
#define PORTB_PORT_ADDR 0xF81
#define PORTB_TRIS_ADDR 0xF93
#define PORTB_LAT_ADDR 0xF8A
// Port C
#define PORTC_BASE 0xF82
#define PORTC_PORT_ADDR 0xF82
#define PORTC_TRIS_ADDR 0xF94
#define PORTC_LAT_ADDR 0xF8B
// Port D
#define PORTD_BASE 0xF83
#define PORTD_PORT_ADDR 0xF83
#define PORTD_TRIS_ADDR 0xF95
#define PORTD_LAT_ADDR 0xF8C
// Port E
#define PORTE_BASE 0xF84
#define PORTE_PORT_ADDR 0xF84
#define PORTE_TRIS_ADDR 0xF96
#define PORTE_LAT_ADDR 0xF8D
// Timer 0
#define TIMER0_BASE 0xFD1
#define TIMER0_T0CON_ADDR 0xFD1
#define TIMER0_TMR0_ADDR 0xFD6
// Timer 1
#define TIMER1_BASE 0xFCD
#define TIMER1_T1CON_ADDR 0xFCD
#define TIMER1_TMR1_ADDR 0xFCF
#define TIMER1_TMR1L_ADDR 0xFCE
// Timer 2
#define TIMER2_BASE 0xFCA
#define TIMER2_T2CON_ADDR 0xFCA
#define TIMER2_TMR2_ADDR 0xFCB
// Timer 3
#define TIMER3_BASE 0xFB0
#define TIMER3_T3CON_ADDR 0xFB0
#define TIMER3_TMR3_ADDR 0xFB2
// A/D Converter
#define ADC_BASE 0xFC2
#define ADC_ADCON0_ADDR 0xFC2
#define ADC_ADCON1_ADDR 0xFC1
#define ADC_ADCON2_ADDR 0xFC0
#define ADC_ADRES_ADDR 0xFC3
#define ADC_ADRESL_ADDR 0xFC4
// CCP 1
#define CCP1_BASE 0xFD4
#define CCP1_CCP1CON_ADDR 0xFD4
#define CCP1_CCPR1_ADDR 0xFD6
#define CCP1_CCPR1L_ADDR 0xFD5
// CCP 2
#define CCP2_BASE 0xFBA
#define CCP2_CCP2CON_ADDR 0xFBA
#define CCP2_CCPR2_ADDR 0xFBB
#define CCP2_CCPR2L_ADDR 0xFBC
// SSP (I2C/SPI)
#define SSP_BASE 0xFC6
#define SSP_SSPCON1_ADDR 0xFC6
#define SSP_SSPCON2_ADDR 0xFC5
#define SSP_SSPSTAT_ADDR 0xFC7
#define SSP_SSPBUF_ADDR 0xFC9
#define SSP_SSPOV_ADDR 0xFC8
// EUSART
#define EUSART_BASE 0xF15
#define EUSART_TXSTA_ADDR 0xFE2
#define EUSART_RCSTA_ADDR 0xFE3
#define EUSART_TXREG_ADDR 0xFAD
#define EUSART_RCREG_ADDR 0xFAE
#define EUSART_SPBRG_ADDR 0xFAF
#define EUSART_SPBRGH_ADDR 0xFB0
#define EUSART_BAUDCON_ADDR 0xFB8
// Comparators
#define COMPARATOR_BASE 0xFB4
#define COMPARATOR_CMCON_ADDR 0xFB4
#define COMPARATOR_CVRCON_ADDR 0xFB5
// USB Module
#define USB_BASE 0xF70
#define USB_UCON_ADDR 0xF71
#define USB_USTAT_ADDR 0xF72
#define USB_UIR_ADDR 0xF73
#define USB_UIE_ADDR 0xF74
#define USB_UEP0_ADDR 0xF80
#define USB_UEP1_ADDR 0xF81
#define USB_UEP2_ADDR 0xF82
#define USB_UEP3_ADDR 0xF83
#define USB_BD0_ADDR 0xF00
#define USB_BD1_ADDR 0xF08
#define USB_BD2_ADDR 0xF10
#define USB_BD3_ADDR 0xF18
// Oscillator
#define OSCCON_BASE 0xFD3
#define OSCCON_OSCCON_ADDR 0xFD3
#define OSCCON_OSCTUNE_ADDR 0xFD9
// Watchdog Timer
#define WDTCON_BASE 0xFD1
#define WDTCON_WDTCON_ADDR 0xFD1

// 中断向量定义
#define INT_RESET 0  // RESET
#define INT_INT0 1  // External Interrupt 0
#define INT_INT1 2  // External Interrupt 1
#define INT_INT2 3  // External Interrupt 2
#define INT_TMR0 4  // Timer 0 Overflow
#define INT_TMR1 5  // Timer 1 Overflow
#define INT_TMR2 6  // Timer 2 Match
#define INT_TMR3 7  // Timer 3 Overflow
#define INT_CCP1 8  // CCP 1
#define INT_CCP2 9  // CCP 2
#define INT_SSP 10  // SSP
#define INT_TX 11  // USART TX
#define INT_RC 12  // USART RX
#define INT_ADC 13  // A/D
#define INT_RBO 14  // Port B Change
#define INT_EXT 15  // External

// 引脚定义
#define PIN_RE3 1  // MCLR/VPP/RE3
#define PIN_RA0 2  // AN0/RA0
#define PIN_RA1 3  // AN1/RA1
#define PIN_RA2 4  // AN2/VREF-/RA2
#define PIN_RA3 5  // AN3/VREF+/RA3
#define PIN_RA4 6  // AN4/T0CKI/RA4
#define PIN_RA5 7  // AN5/RE5
#define PIN_VSS 8  // Ground
#define PIN_RA7 9  // OSC1/CLKI/RA7
#define PIN_RA6 10  // OSC2/CLKO/RA6
#define PIN_RC0 11  // T1OSO/T1CKI/RC0
#define PIN_RC1 12  // T1OSI/RC1
#define PIN_RC2 13  // CCP1/RC2
#define PIN_RC3 14  // SCK/SCL/RC3
#define PIN_RD0 15  // SDO/RD0
#define PIN_RD1 16  // SDI/RD1
#define PIN_RD2 17  // RD2
#define PIN_RC6 18  // TX/CK/RC6
#define PIN_RC7 19  // RX/DT/RC7
#define PIN_VSS 20  // Ground
#define PIN_RD3 21  // RD3
#define PIN_RD4 22  // RD4
#define PIN_RD5 23  // PWRB/RD5
#define PIN_RD6 24  // PBC/RD6
#define PIN_RD7 25  // PCD/RD7
#define PIN_RC4 26  // D-/RC4
#define PIN_RC5 27  // D+/RC5
#define PIN_RE0 28  // AN5/RE0
#define PIN_RE1 29  // AN6/RE1
#define PIN_RE2 30  // AN7/RE2
#define PIN_VSS 31  // Ground
#define PIN_VDD 32  // Vdd

#endif /* PIC18F4550_DEVICE_H */
