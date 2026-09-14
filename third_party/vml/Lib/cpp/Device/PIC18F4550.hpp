#ifndef PIC18F4550_HPP
#define PIC18F4550_HPP

// PIC18F4550寄存器定义
// 生成自: Microchip/PIC18/PIC18F4550
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

// 寄存器定义
// Working Register
#define W (*(volatile uint8_t*)0x0E)

// Status Register
#define STATUS (*(volatile uint8_t*)0xFD8)
#define STATUS_C 0  // Carry Flag
#define STATUS_DC 1  // Digit Carry Flag
#define STATUS_Z 2  // Zero Flag
#define STATUS_PD 3  // Power-Down Flag
#define STATUS_TO 4  // Time-out Flag
#define STATUS_RP 0  // Register Bank Select
#define STATUS_IRP 7  // Indirect Register Bank Select

// Bank Select Register
#define BSR (*(volatile uint8_t*)0xFE0)

// Port A
#define PORTA (*(volatile uint8_t*)0xF80)

// Port B
#define PORTB (*(volatile uint8_t*)0xF81)

// Port C
#define PORTC (*(volatile uint8_t*)0xF82)

// Port D
#define PORTD (*(volatile uint8_t*)0xF83)

// Port E
#define PORTE (*(volatile uint8_t*)0xF84)

// Tri-state Port A
#define TRISA (*(volatile uint8_t*)0xF92)

// Tri-state Port B
#define TRISB (*(volatile uint8_t*)0xF93)

// Tri-state Port C
#define TRISC (*(volatile uint8_t*)0xF94)

// Tri-state Port D
#define TRISD (*(volatile uint8_t*)0xF95)

// Tri-state Port E
#define TRISE (*(volatile uint8_t*)0xF96)

// Latch Port A
#define LATA (*(volatile uint8_t*)0xF89)

// Latch Port B
#define LATB (*(volatile uint8_t*)0xF8A)

// Latch Port C
#define LATC (*(volatile uint8_t*)0xF8B)

// Latch Port D
#define LATD (*(volatile uint8_t*)0xF8C)

// Latch Port E
#define LATE (*(volatile uint8_t*)0xF8D)

// Interrupt Control
#define INTCON (*(volatile uint8_t*)0xFF2)
#define INTCON_RBIF 0  // Port B Interrupt Flag
#define INTCON_INT0IF 1  // INT0 Interrupt Flag
#define INTCON_TMR0IF 2  // Timer 0 Interrupt Flag
#define INTCON_RBIE 3  // Port B Interrupt Enable
#define INTCON_INT0IE 4  // INT0 Interrupt Enable
#define INTCON_TMR0IE 5  // Timer 0 Interrupt Enable
#define INTCON_PEIE 6  // Peripheral Interrupt Enable
#define INTCON_GIE 7  // Global Interrupt Enable

// Peripheral Interrupt 1
#define PIR1 (*(volatile uint8_t*)0xF9E)

// Peripheral Interrupt 2
#define PIR2 (*(volatile uint8_t*)0xF9F)

// Peripheral Interrupt Enable 1
#define PIE1 (*(volatile uint8_t*)0xF9D)

// Peripheral Interrupt Enable 2
#define PIE2 (*(volatile uint8_t*)0xF9C)

// Interrupt Priority 1
#define IPR1 (*(volatile uint8_t*)0xF9B)

// Interrupt Priority 2
#define IPR2 (*(volatile uint8_t*)0xF9A)

// Reset Control
#define RCON (*(volatile uint8_t*)0xFD0)
#define RCON_NOT_TO 3  // Time-out Flag
#define RCON_NOT_PD 4  // Power-Down Flag
#define RCON_NOT_RI 5  // RESET Flag
#define RCON_NOT_POR 6  // Power-on Reset Flag
#define RCON_NOT_BOR 7  // Brown-out Reset Flag

// Timer 0 Control
#define T0CON (*(volatile uint8_t*)0xFD1)

// Timer 0 Register
#define TMR0 (*(volatile uint8_t*)0xFD6)

// Timer 1 Control
#define T1CON (*(volatile uint8_t*)0xFCD)

// Timer 1 Register High
#define TMR1 (*(volatile uint8_t*)0xFCE)

// Timer 1 Register Low
#define TMR1L (*(volatile uint8_t*)0xFCF)

// Timer 2 Control
#define T2CON (*(volatile uint8_t*)0xFCA)

// Timer 2 Register
#define TMR2 (*(volatile uint8_t*)0xFCB)

// Timer 3 Control
#define T3CON (*(volatile uint8_t*)0xFB1)

// Timer 3 Register High
#define TMR3 (*(volatile uint8_t*)0xFB3)

// Timer 3 Register Low
#define TMR3L (*(volatile uint8_t*)0xFB2)

// SSP Control 1
#define SSPCON1 (*(volatile uint8_t*)0xFC6)

// SSP Control 2
#define SSPCON2 (*(volatile uint8_t*)0xFC5)

// SSP Status
#define SSPSTAT (*(volatile uint8_t*)0xFC7)

// SSP Buffer
#define SSPBUF (*(volatile uint8_t*)0xFC9)

// SSP Shift Register
#define SSPOR (*(volatile uint8_t*)0xFC8)

// A/D Control 0
#define ADCON0 (*(volatile uint8_t*)0xFC2)

// A/D Control 1
#define ADCON1 (*(volatile uint8_t*)0xFC1)

// A/D Control 2
#define ADCON2 (*(volatile uint8_t*)0xFC0)

// A/D Result
#define ADRES (*(volatile uint8_t*)0xFC3)

// A/D Result Low
#define ADRESL (*(volatile uint8_t*)0xFC4)

// CCP 1 Control
#define CCP1CON (*(volatile uint8_t*)0xFD4)

// CCP 1 Register High
#define CCPR1 (*(volatile uint8_t*)0xFD6)

// CCP 1 Register Low
#define CCPR1L (*(volatile uint8_t*)0xFD5)

// CCP 2 Control
#define CCP2CON (*(volatile uint8_t*)0xFBA)

// CCP 2 Register High
#define CCPR2 (*(volatile uint8_t*)0xFBB)

// CCP 2 Register Low
#define CCPR2L (*(volatile uint8_t*)0xFBC)

// USB Control
#define USBCON (*(volatile uint8_t*)0xF75)

// USB Status
#define USBSTAT (*(volatile uint8_t*)0xF74)

// USB Interrupt Enable
#define UIE (*(volatile uint8_t*)0xF73)

// USB Interrupt Flag
#define UIR (*(volatile uint8_t*)0xF72)

// USB Control
#define UCON (*(volatile uint8_t*)0xF71)

// USB Status
#define USTAT (*(volatile uint8_t*)0xF70)

// USB Endpoint 0
#define UEP0 (*(volatile uint8_t*)0xF60)

// USB Endpoint 1
#define UEP1 (*(volatile uint8_t*)0xF61)

// USB Endpoint 2
#define UEP2 (*(volatile uint8_t*)0xF62)

// USB Endpoint 3
#define UEP3 (*(volatile uint8_t*)0xF63)

// USB Endpoint 4
#define UEP4 (*(volatile uint8_t*)0xF64)

// 内存段定义
// Program Flash (32KB)
#define FLASH_START 0x0000
#define FLASH_END 0x7FFF
#define FLASH_SIZE 32768

// EEPROM (256B)
#define EEPROM_START 0xF00000
#define EEPROM_END 0xF000FF
#define EEPROM_SIZE 256

// SRAM (2KB)
#define SRAM_START 0x0000
#define SRAM_END 0x07FF
#define SRAM_SIZE 2048

// Access Bank
#define ACCESS_START 0x0000
#define ACCESS_END 
#define ACCESS_SIZE 1

// 外设定义
// Port A
#define PORTA_BASE 0xF80
#define PORTA_PORT (*(volatile uint8_t*)0x00001F00)
#define PORTA_TRIS (*(volatile uint8_t*)0x00001F12)
#define PORTA_LAT (*(volatile uint8_t*)0x00001F09)

// Port B
#define PORTB_BASE 0xF81
#define PORTB_PORT (*(volatile uint8_t*)0x00001F02)
#define PORTB_TRIS (*(volatile uint8_t*)0x00001F14)
#define PORTB_LAT (*(volatile uint8_t*)0x00001F0B)

// Port C
#define PORTC_BASE 0xF82
#define PORTC_PORT (*(volatile uint8_t*)0x00001F04)
#define PORTC_TRIS (*(volatile uint8_t*)0x00001F16)
#define PORTC_LAT (*(volatile uint8_t*)0x00001F0D)

// Port D
#define PORTD_BASE 0xF83
#define PORTD_PORT (*(volatile uint8_t*)0x00001F06)
#define PORTD_TRIS (*(volatile uint8_t*)0x00001F18)
#define PORTD_LAT (*(volatile uint8_t*)0x00001F0F)

// Port E
#define PORTE_BASE 0xF84
#define PORTE_PORT (*(volatile uint8_t*)0x00001F08)
#define PORTE_TRIS (*(volatile uint8_t*)0x00001F1A)
#define PORTE_LAT (*(volatile uint8_t*)0x00001F11)

// Timer 0
#define TIMER0_BASE 0xFD1
#define TIMER0_T0CON (*(volatile uint8_t*)0x00001FA2)
#define TIMER0_TMR0 (*(volatile uint8_t*)0x00001FA7)

// Timer 1
#define TIMER1_BASE 0xFCD
#define TIMER1_T1CON (*(volatile uint8_t*)0x00001F9A)
#define TIMER1_TMR1 (*(volatile uint8_t*)0x00001F9C)
#define TIMER1_TMR1L (*(volatile uint8_t*)0x00001F9B)

// Timer 2
#define TIMER2_BASE 0xFCA
#define TIMER2_T2CON (*(volatile uint8_t*)0x00001F94)
#define TIMER2_TMR2 (*(volatile uint8_t*)0x00001F95)

// Timer 3
#define TIMER3_BASE 0xFB0
#define TIMER3_T3CON (*(volatile uint8_t*)0x00001F60)
#define TIMER3_TMR3 (*(volatile uint8_t*)0x00001F62)

// A/D Converter
#define ADC_BASE 0xFC2
#define ADC_ADCON0 (*(volatile uint8_t*)0x00001F84)
#define ADC_ADCON1 (*(volatile uint8_t*)0x00001F83)
#define ADC_ADCON2 (*(volatile uint8_t*)0x00001F82)
#define ADC_ADRES (*(volatile uint8_t*)0x00001F85)
#define ADC_ADRESL (*(volatile uint8_t*)0x00001F86)

// CCP 1
#define CCP1_BASE 0xFD4
#define CCP1_CCP1CON (*(volatile uint8_t*)0x00001FA8)
#define CCP1_CCPR1 (*(volatile uint8_t*)0x00001FAA)
#define CCP1_CCPR1L (*(volatile uint8_t*)0x00001FA9)

// CCP 2
#define CCP2_BASE 0xFBA
#define CCP2_CCP2CON (*(volatile uint8_t*)0x00001F74)
#define CCP2_CCPR2 (*(volatile uint8_t*)0x00001F75)
#define CCP2_CCPR2L (*(volatile uint8_t*)0x00001F76)

// SSP (I2C/SPI)
#define SSP_BASE 0xFC6
#define SSP_SSPCON1 (*(volatile uint8_t*)0x00001F8C)
#define SSP_SSPCON2 (*(volatile uint8_t*)0x00001F8B)
#define SSP_SSPSTAT (*(volatile uint8_t*)0x00001F8D)
#define SSP_SSPBUF (*(volatile uint8_t*)0x00001F8F)
#define SSP_SSPOV (*(volatile uint8_t*)0x00001F8E)

// EUSART
#define EUSART_BASE 0xF15
#define EUSART_TXSTA (*(volatile uint8_t*)0x00001EF7)
#define EUSART_RCSTA (*(volatile uint8_t*)0x00001EF8)
#define EUSART_TXREG (*(volatile uint8_t*)0x00001EC2)
#define EUSART_RCREG (*(volatile uint8_t*)0x00001EC3)
#define EUSART_SPBRG (*(volatile uint8_t*)0x00001EC4)
#define EUSART_SPBRGH (*(volatile uint8_t*)0x00001EC5)
#define EUSART_BAUDCON (*(volatile uint8_t*)0x00001ECD)

// Comparators
#define COMPARATOR_BASE 0xFB4
#define COMPARATOR_CMCON (*(volatile uint8_t*)0x00001F68)
#define COMPARATOR_CVRCON (*(volatile uint8_t*)0x00001F69)

// USB Module
#define USB_BASE 0xF70
#define USB_UCON (*(volatile uint8_t*)0x00001EE1)
#define USB_USTAT (*(volatile uint8_t*)0x00001EE2)
#define USB_UIR (*(volatile uint8_t*)0x00001EE3)
#define USB_UIE (*(volatile uint8_t*)0x00001EE4)
#define USB_UEP0 (*(volatile uint8_t*)0x00001EF0)
#define USB_UEP1 (*(volatile uint8_t*)0x00001EF1)
#define USB_UEP2 (*(volatile uint8_t*)0x00001EF2)
#define USB_UEP3 (*(volatile uint8_t*)0x00001EF3)
#define USB_BD0 (*(volatile uint8_t*)0x00001E70)
#define USB_BD1 (*(volatile uint8_t*)0x00001E78)
#define USB_BD2 (*(volatile uint8_t*)0x00001E80)
#define USB_BD3 (*(volatile uint8_t*)0x00001E88)

// Oscillator
#define OSCCON_BASE 0xFD3
#define OSCCON_OSCCON (*(volatile uint8_t*)0x00001FA6)
#define OSCCON_OSCTUNE (*(volatile uint8_t*)0x00001FAC)

// Watchdog Timer
#define WDTCON_BASE 0xFD1
#define WDTCON_WDTCON (*(volatile uint8_t*)0x00001FA2)

// 中断向量定义
#define RESET_VECTOR 0  // RESET
#define INT0_VECTOR 1  // External Interrupt 0
#define INT1_VECTOR 2  // External Interrupt 1
#define INT2_VECTOR 3  // External Interrupt 2
#define TMR0_VECTOR 4  // Timer 0 Overflow
#define TMR1_VECTOR 5  // Timer 1 Overflow
#define TMR2_VECTOR 6  // Timer 2 Match
#define TMR3_VECTOR 7  // Timer 3 Overflow
#define CCP1_VECTOR 8  // CCP 1
#define CCP2_VECTOR 9  // CCP 2
#define SSP_VECTOR 10  // SSP
#define TX_VECTOR 11  // USART TX
#define RC_VECTOR 12  // USART RX
#define ADC_VECTOR 13  // A/D
#define RBO_VECTOR 14  // Port B Change
#define EXT_VECTOR 15  // External

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

void pic18f4550_init(void);

#ifdef __cplusplus
}
#endif

#endif // PIC18F4550_HPP
