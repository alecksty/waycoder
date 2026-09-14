#ifndef PIC16F877A_HPP
#define PIC16F877A_HPP

// PIC16F877A寄存器定义
// 生成自: Microchip/PIC/PIC16F877A
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: PIC16
// 位宽: 8位
// 时钟频率: 4000000 Hz

// 寄存器定义
// Working Register
#define W (*(volatile uint8_t*)0x00)

// Status Register
#define STATUS (*(volatile uint8_t*)0x03)
#define STATUS_C 0  // Carry flag
#define STATUS_DC 1  // Digit carry flag
#define STATUS_Z 2  // Zero flag
#define STATUS_PD 3  // Power-down flag
#define STATUS_TO 4  // Time-out flag
#define STATUS_RP 5  // Register bank select
#define STATUS_IRP 7  // Indirect register bank select

// Interrupt Control Register
#define INTCON (*(volatile uint8_t*)0x0B)
#define INTCON_RBIF 0  // PORTB change interrupt flag
#define INTCON_INTF 1  // External interrupt flag
#define INTCON_TMR0IF 2  // TMR0 overflow interrupt flag
#define INTCON_RBIE 3  // PORTB change interrupt enable
#define INTCON_INTE 4  // External interrupt enable
#define INTCON_TMR0IE 5  // TMR0 overflow interrupt enable
#define INTCON_PEIE 6  // Peripheral interrupt enable
#define INTCON_GIE 7  // Global interrupt enable

// PORT B
#define PORTB (*(volatile uint8_t*)0x06)

// TRIS B
#define TRISB (*(volatile uint8_t*)0x86)

// PORT C
#define PORTC (*(volatile uint8_t*)0x07)

// TRIS C
#define TRISC (*(volatile uint8_t*)0x87)

// PORT D
#define PORTD (*(volatile uint8_t*)0x08)

// TRIS D
#define TRISD (*(volatile uint8_t*)0x88)

// PORT E
#define PORTE (*(volatile uint8_t*)0x09)

// TRIS E
#define TRISE (*(volatile uint8_t*)0x89)

// Timer 0
#define TMR0 (*(volatile uint8_t*)0x01)

// Option Register
#define OPTION_REG (*(volatile uint8_t*)0x81)

// Program Counter Low
#define PCL (*(volatile uint8_t*)0x02)

// Program Counter Latch High
#define PCLATH (*(volatile uint8_t*)0x0A)

// File Select Register
#define FSR (*(volatile uint8_t*)0x04)

// EEPROM Data
#define EEDATA (*(volatile uint8_t*)0x10C)

// EEPROM Address
#define EEADR (*(volatile uint8_t*)0x10D)

// EEPROM Control 1
#define EECON1 (*(volatile uint8_t*)0x18C)
#define EECON1_RD 0  // Read control
#define EECON1_WR 1  // Write control
#define EECON1_WREN 2  // Write enable
#define EECON1_WRERR 3  // Write error flag
#define EECON1_EEPGD 7  // EEPROM program/data select

// EEPROM Control 2
#define EECON2 (*(volatile uint8_t*)0x18D)

// A/D Result High
#define ADRESH (*(volatile uint8_t*)0x1E)

// A/D Result Low
#define ADRESL (*(volatile uint8_t*)0x1F)

// A/D Control 0
#define ADCON0 (*(volatile uint8_t*)0x1F)
#define ADCON0_ADON 0  // A/D enable
#define ADCON0_GO_DONE 2  // A/D conversion status
#define ADCON0_CHS 3  // Channel select

// A/D Control 1
#define ADCON1 (*(volatile uint8_t*)0x9F)

// MSSP Status
#define SSPSTAT (*(volatile uint8_t*)0x94)

// MSSP Control
#define SSPCON (*(volatile uint8_t*)0x14)

// SSP Buffer
#define SSPBUF (*(volatile uint8_t*)0x13)

// USART Transmit Register
#define TXREG (*(volatile uint8_t*)0x19)

// USART Receive Register
#define RCREG (*(volatile uint8_t*)0x1A)

// Baud Rate Generator
#define SPBRG (*(volatile uint8_t*)0x99)

// TX Status and Control
#define TXSTA (*(volatile uint8_t*)0x98)

// RX Status and Control
#define RCSTA (*(volatile uint8_t*)0x18)

// CCP1 Control
#define CCP1CON (*(volatile uint8_t*)0x17)

// CCP1 Low
#define CCPR1L (*(volatile uint8_t*)0x15)

// CCP1 High
#define CCPR1H (*(volatile uint8_t*)0x16)

// CCP2 Control
#define CCP2CON (*(volatile uint8_t*)0x1D)

// CCP2 Low
#define CCPR2L (*(volatile uint8_t*)0x1B)

// CCP2 High
#define CCPR2H (*(volatile uint8_t*)0x1C)

// Timer 1 Control
#define T1CON (*(volatile uint8_t*)0x10)

// Timer 1 Low
#define TMR1L (*(volatile uint8_t*)0x0E)

// Timer 1 High
#define TMR1H (*(volatile uint8_t*)0x0F)

// Timer 2 Control
#define T2CON (*(volatile uint8_t*)0x12)

// Timer 2
#define TMR2 (*(volatile uint8_t*)0x11)

// Timer 2 Period
#define PR2 (*(volatile uint8_t*)0x92)

// 内存段定义
// Program Memory (8KB)
#define PROGRAM_START 0x0000
#define PROGRAM_END 0x1FFF
#define PROGRAM_SIZE 8192

// General Purpose RAM Bank 0
#define DATA_START 0x20
#define DATA_END 0x7F
#define DATA_SIZE 96

// General Purpose RAM Bank 1
#define SRAM_START 0xA0
#define SRAM_END 0xFF
#define SRAM_SIZE 96

// EEPROM Data Memory
#define EEPROM_START 0x2100
#define EEPROM_END 0x21FF
#define EEPROM_SIZE 256

// 外设定义
// Port B
#define GPIO_PORTB_BASE 0x06
#define GPIO_PORTB_PORTB (*(volatile uint8_t*)0x0000000C)
#define GPIO_PORTB_TRISB (*(volatile uint8_t*)0x0000008C)

// Port C
#define GPIO_PORTC_BASE 0x07
#define GPIO_PORTC_PORTC (*(volatile uint8_t*)0x0000000E)
#define GPIO_PORTC_TRISC (*(volatile uint8_t*)0x0000008E)

// Port D
#define GPIO_PORTD_BASE 0x08
#define GPIO_PORTD_PORTD (*(volatile uint8_t*)0x00000010)
#define GPIO_PORTD_TRISD (*(volatile uint8_t*)0x00000090)

// Timer 0
#define TIMER0_BASE 0x01
#define TIMER0_TMR0 (*(volatile uint8_t*)0x00000002)
#define TIMER0_OPTION_REG (*(volatile uint8_t*)0x00000082)

// Timer 1
#define TIMER1_BASE 0x0E
#define TIMER1_T1CON (*(volatile uint8_t*)0x0000001E)
#define TIMER1_TMR1L (*(volatile uint8_t*)0x0000001C)
#define TIMER1_TMR1H (*(volatile uint8_t*)0x0000001D)

// Timer 2
#define TIMER2_BASE 0x11
#define TIMER2_T2CON (*(volatile uint8_t*)0x00000023)
#define TIMER2_TMR2 (*(volatile uint8_t*)0x00000022)
#define TIMER2_PR2 (*(volatile uint8_t*)0x000000A3)

// A/D Converter
#define ADC_BASE 0x1E
#define ADC_ADRESH (*(volatile uint8_t*)0x0000003C)
#define ADC_ADRESL (*(volatile uint8_t*)0x000000BD)
#define ADC_ADCON0 (*(volatile uint8_t*)0x0000003D)
#define ADC_ADCON1 (*(volatile uint8_t*)0x000000BD)

// Master Synchronous Serial Port
#define MSSP_BASE 0x13
#define MSSP_SSPSTAT (*(volatile uint8_t*)0x000000A7)
#define MSSP_SSPCON (*(volatile uint8_t*)0x00000027)
#define MSSP_SSPBUF (*(volatile uint8_t*)0x00000026)

// USART
#define USART_BASE 0x19
#define USART_TXREG (*(volatile uint8_t*)0x00000032)
#define USART_RCREG (*(volatile uint8_t*)0x00000033)
#define USART_SPBRG (*(volatile uint8_t*)0x000000B2)
#define USART_TXSTA (*(volatile uint8_t*)0x000000B1)
#define USART_RCSTA (*(volatile uint8_t*)0x00000031)

// Capture/Compare/PWM 1
#define CCP1_BASE 0x15
#define CCP1_CCP1CON (*(volatile uint8_t*)0x0000002C)
#define CCP1_CCPR1L (*(volatile uint8_t*)0x0000002A)
#define CCP1_CCPR1H (*(volatile uint8_t*)0x0000002B)

// Capture/Compare/PWM 2
#define CCP2_BASE 0x1B
#define CCP2_CCP2CON (*(volatile uint8_t*)0x00000038)
#define CCP2_CCPR2L (*(volatile uint8_t*)0x00000036)
#define CCP2_CCPR2H (*(volatile uint8_t*)0x00000037)

// 中断向量定义
#define INT_VECTOR 1  // External Interrupt
#define TMR0_VECTOR 2  // Timer 0 Overflow
#define RB_VECTOR 3  // PORTB Change
#define CCP1_VECTOR 4  // CCP1
#define CCP2_VECTOR 5  // CCP2
#define TMR1_VECTOR 6  // Timer 1 Overflow
#define TMR2_VECTOR 8  // Timer 2 Overflow
#define SPI_VECTOR 9  // SPI/I2C
#define SCI_VECTOR 10  // USART Receive
#define SCI_VECTOR 11  // USART Transmit
#define ADC_VECTOR 12  // A/D Converter
#define EEPROM_VECTOR 13  // EEPROM Write Complete

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

void pic16f877a_init(void);

#ifdef __cplusplus
}
#endif

#endif // PIC16F877A_HPP
