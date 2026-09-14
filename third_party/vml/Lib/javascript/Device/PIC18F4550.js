/**
 * PIC18F4550 寄存器定义
 * 生成自: Microchip/PIC18/PIC18F4550
 * 版本: 1.0
 */
export const pic18f4550 = {
  // CPU: PIC18, 8位, 20000000 Hz

  // 寄存器定义
  // Working Register
  W: 0x0E,
  // Status Register
  STATUS: 0xFD8,
  STATUS_C: 0,  // Carry Flag
  STATUS_DC: 1,  // Digit Carry Flag
  STATUS_Z: 2,  // Zero Flag
  STATUS_PD: 3,  // Power-Down Flag
  STATUS_TO: 4,  // Time-out Flag
  STATUS_RP: 0,  // Register Bank Select
  STATUS_IRP: 7,  // Indirect Register Bank Select
  // Bank Select Register
  BSR: 0xFE0,
  // Port A
  PORTA: 0xF80,
  // Port B
  PORTB: 0xF81,
  // Port C
  PORTC: 0xF82,
  // Port D
  PORTD: 0xF83,
  // Port E
  PORTE: 0xF84,
  // Tri-state Port A
  TRISA: 0xF92,
  // Tri-state Port B
  TRISB: 0xF93,
  // Tri-state Port C
  TRISC: 0xF94,
  // Tri-state Port D
  TRISD: 0xF95,
  // Tri-state Port E
  TRISE: 0xF96,
  // Latch Port A
  LATA: 0xF89,
  // Latch Port B
  LATB: 0xF8A,
  // Latch Port C
  LATC: 0xF8B,
  // Latch Port D
  LATD: 0xF8C,
  // Latch Port E
  LATE: 0xF8D,
  // Interrupt Control
  INTCON: 0xFF2,
  INTCON_RBIF: 0,  // Port B Interrupt Flag
  INTCON_INT0IF: 1,  // INT0 Interrupt Flag
  INTCON_TMR0IF: 2,  // Timer 0 Interrupt Flag
  INTCON_RBIE: 3,  // Port B Interrupt Enable
  INTCON_INT0IE: 4,  // INT0 Interrupt Enable
  INTCON_TMR0IE: 5,  // Timer 0 Interrupt Enable
  INTCON_PEIE: 6,  // Peripheral Interrupt Enable
  INTCON_GIE: 7,  // Global Interrupt Enable
  // Peripheral Interrupt 1
  PIR1: 0xF9E,
  // Peripheral Interrupt 2
  PIR2: 0xF9F,
  // Peripheral Interrupt Enable 1
  PIE1: 0xF9D,
  // Peripheral Interrupt Enable 2
  PIE2: 0xF9C,
  // Interrupt Priority 1
  IPR1: 0xF9B,
  // Interrupt Priority 2
  IPR2: 0xF9A,
  // Reset Control
  RCON: 0xFD0,
  RCON_NOT_TO: 3,  // Time-out Flag
  RCON_NOT_PD: 4,  // Power-Down Flag
  RCON_NOT_RI: 5,  // RESET Flag
  RCON_NOT_POR: 6,  // Power-on Reset Flag
  RCON_NOT_BOR: 7,  // Brown-out Reset Flag
  // Timer 0 Control
  T0CON: 0xFD1,
  // Timer 0 Register
  TMR0: 0xFD6,
  // Timer 1 Control
  T1CON: 0xFCD,
  // Timer 1 Register High
  TMR1: 0xFCE,
  // Timer 1 Register Low
  TMR1L: 0xFCF,
  // Timer 2 Control
  T2CON: 0xFCA,
  // Timer 2 Register
  TMR2: 0xFCB,
  // Timer 3 Control
  T3CON: 0xFB1,
  // Timer 3 Register High
  TMR3: 0xFB3,
  // Timer 3 Register Low
  TMR3L: 0xFB2,
  // SSP Control 1
  SSPCON1: 0xFC6,
  // SSP Control 2
  SSPCON2: 0xFC5,
  // SSP Status
  SSPSTAT: 0xFC7,
  // SSP Buffer
  SSPBUF: 0xFC9,
  // SSP Shift Register
  SSPOR: 0xFC8,
  // A/D Control 0
  ADCON0: 0xFC2,
  // A/D Control 1
  ADCON1: 0xFC1,
  // A/D Control 2
  ADCON2: 0xFC0,
  // A/D Result
  ADRES: 0xFC3,
  // A/D Result Low
  ADRESL: 0xFC4,
  // CCP 1 Control
  CCP1CON: 0xFD4,
  // CCP 1 Register High
  CCPR1: 0xFD6,
  // CCP 1 Register Low
  CCPR1L: 0xFD5,
  // CCP 2 Control
  CCP2CON: 0xFBA,
  // CCP 2 Register High
  CCPR2: 0xFBB,
  // CCP 2 Register Low
  CCPR2L: 0xFBC,
  // USB Control
  USBCON: 0xF75,
  // USB Status
  USBSTAT: 0xF74,
  // USB Interrupt Enable
  UIE: 0xF73,
  // USB Interrupt Flag
  UIR: 0xF72,
  // USB Control
  UCON: 0xF71,
  // USB Status
  USTAT: 0xF70,
  // USB Endpoint 0
  UEP0: 0xF60,
  // USB Endpoint 1
  UEP1: 0xF61,
  // USB Endpoint 2
  UEP2: 0xF62,
  // USB Endpoint 3
  UEP3: 0xF63,
  // USB Endpoint 4
  UEP4: 0xF64,

  // 内存段
  // Program Flash (32KB)
  flash_START: 0x0000,
  flash_END: 0x7FFF,
  flash_SIZE: 32768,
  // EEPROM (256B)
  eeprom_START: 0xF00000,
  eeprom_END: 0xF000FF,
  eeprom_SIZE: 256,
  // SRAM (2KB)
  sram_START: 0x0000,
  sram_END: 0x07FF,
  sram_SIZE: 2048,
  // Access Bank
  access_START: 0x0000,
  access_END: ,
  access_SIZE: 1,

  // 外设定义
  // Port A
  PORTA_BASE: 0xF80,
  PORTA_PORT: 0x00001F00,
  PORTA_TRIS: 0x00001F12,
  PORTA_LAT: 0x00001F09,
  // Port B
  PORTB_BASE: 0xF81,
  PORTB_PORT: 0x00001F02,
  PORTB_TRIS: 0x00001F14,
  PORTB_LAT: 0x00001F0B,
  // Port C
  PORTC_BASE: 0xF82,
  PORTC_PORT: 0x00001F04,
  PORTC_TRIS: 0x00001F16,
  PORTC_LAT: 0x00001F0D,
  // Port D
  PORTD_BASE: 0xF83,
  PORTD_PORT: 0x00001F06,
  PORTD_TRIS: 0x00001F18,
  PORTD_LAT: 0x00001F0F,
  // Port E
  PORTE_BASE: 0xF84,
  PORTE_PORT: 0x00001F08,
  PORTE_TRIS: 0x00001F1A,
  PORTE_LAT: 0x00001F11,
  // Timer 0
  TIMER0_BASE: 0xFD1,
  TIMER0_T0CON: 0x00001FA2,
  TIMER0_TMR0: 0x00001FA7,
  // Timer 1
  TIMER1_BASE: 0xFCD,
  TIMER1_T1CON: 0x00001F9A,
  TIMER1_TMR1: 0x00001F9C,
  TIMER1_TMR1L: 0x00001F9B,
  // Timer 2
  TIMER2_BASE: 0xFCA,
  TIMER2_T2CON: 0x00001F94,
  TIMER2_TMR2: 0x00001F95,
  // Timer 3
  TIMER3_BASE: 0xFB0,
  TIMER3_T3CON: 0x00001F60,
  TIMER3_TMR3: 0x00001F62,
  // A/D Converter
  ADC_BASE: 0xFC2,
  ADC_ADCON0: 0x00001F84,
  ADC_ADCON1: 0x00001F83,
  ADC_ADCON2: 0x00001F82,
  ADC_ADRES: 0x00001F85,
  ADC_ADRESL: 0x00001F86,
  // CCP 1
  CCP1_BASE: 0xFD4,
  CCP1_CCP1CON: 0x00001FA8,
  CCP1_CCPR1: 0x00001FAA,
  CCP1_CCPR1L: 0x00001FA9,
  // CCP 2
  CCP2_BASE: 0xFBA,
  CCP2_CCP2CON: 0x00001F74,
  CCP2_CCPR2: 0x00001F75,
  CCP2_CCPR2L: 0x00001F76,
  // SSP (I2C/SPI)
  SSP_BASE: 0xFC6,
  SSP_SSPCON1: 0x00001F8C,
  SSP_SSPCON2: 0x00001F8B,
  SSP_SSPSTAT: 0x00001F8D,
  SSP_SSPBUF: 0x00001F8F,
  SSP_SSPOV: 0x00001F8E,
  // EUSART
  EUSART_BASE: 0xF15,
  EUSART_TXSTA: 0x00001EF7,
  EUSART_RCSTA: 0x00001EF8,
  EUSART_TXREG: 0x00001EC2,
  EUSART_RCREG: 0x00001EC3,
  EUSART_SPBRG: 0x00001EC4,
  EUSART_SPBRGH: 0x00001EC5,
  EUSART_BAUDCON: 0x00001ECD,
  // Comparators
  COMPARATOR_BASE: 0xFB4,
  COMPARATOR_CMCON: 0x00001F68,
  COMPARATOR_CVRCON: 0x00001F69,
  // USB Module
  USB_BASE: 0xF70,
  USB_UCON: 0x00001EE1,
  USB_USTAT: 0x00001EE2,
  USB_UIR: 0x00001EE3,
  USB_UIE: 0x00001EE4,
  USB_UEP0: 0x00001EF0,
  USB_UEP1: 0x00001EF1,
  USB_UEP2: 0x00001EF2,
  USB_UEP3: 0x00001EF3,
  USB_BD0: 0x00001E70,
  USB_BD1: 0x00001E78,
  USB_BD2: 0x00001E80,
  USB_BD3: 0x00001E88,
  // Oscillator
  OSCCON_BASE: 0xFD3,
  OSCCON_OSCCON: 0x00001FA6,
  OSCCON_OSCTUNE: 0x00001FAC,
  // Watchdog Timer
  WDTCON_BASE: 0xFD1,
  WDTCON_WDTCON: 0x00001FA2,

  // 中断向量
  IRQ_RESET: 0,  // RESET
  IRQ_INT0: 1,  // External Interrupt 0
  IRQ_INT1: 2,  // External Interrupt 1
  IRQ_INT2: 3,  // External Interrupt 2
  IRQ_TMR0: 4,  // Timer 0 Overflow
  IRQ_TMR1: 5,  // Timer 1 Overflow
  IRQ_TMR2: 6,  // Timer 2 Match
  IRQ_TMR3: 7,  // Timer 3 Overflow
  IRQ_CCP1: 8,  // CCP 1
  IRQ_CCP2: 9,  // CCP 2
  IRQ_SSP: 10,  // SSP
  IRQ_TX: 11,  // USART TX
  IRQ_RC: 12,  // USART RX
  IRQ_ADC: 13,  // A/D
  IRQ_RBO: 14,  // Port B Change
  IRQ_EXT: 15,  // External

  // 引脚定义
  PIN_RE3: 1,  // MCLR/VPP/RE3
  PIN_RA0: 2,  // AN0/RA0
  PIN_RA1: 3,  // AN1/RA1
  PIN_RA2: 4,  // AN2/VREF-/RA2
  PIN_RA3: 5,  // AN3/VREF+/RA3
  PIN_RA4: 6,  // AN4/T0CKI/RA4
  PIN_RA5: 7,  // AN5/RE5
  PIN_VSS: 8,  // Ground
  PIN_RA7: 9,  // OSC1/CLKI/RA7
  PIN_RA6: 10,  // OSC2/CLKO/RA6
  PIN_RC0: 11,  // T1OSO/T1CKI/RC0
  PIN_RC1: 12,  // T1OSI/RC1
  PIN_RC2: 13,  // CCP1/RC2
  PIN_RC3: 14,  // SCK/SCL/RC3
  PIN_RD0: 15,  // SDO/RD0
  PIN_RD1: 16,  // SDI/RD1
  PIN_RD2: 17,  // RD2
  PIN_RC6: 18,  // TX/CK/RC6
  PIN_RC7: 19,  // RX/DT/RC7
  PIN_VSS: 20,  // Ground
  PIN_RD3: 21,  // RD3
  PIN_RD4: 22,  // RD4
  PIN_RD5: 23,  // PWRB/RD5
  PIN_RD6: 24,  // PBC/RD6
  PIN_RD7: 25,  // PCD/RD7
  PIN_RC4: 26,  // D-/RC4
  PIN_RC5: 27,  // D+/RC5
  PIN_RE0: 28,  // AN5/RE0
  PIN_RE1: 29,  // AN6/RE1
  PIN_RE2: 30,  // AN7/RE2
  PIN_VSS: 31,  // Ground
  PIN_VDD: 32,  // Vdd

  init: function() {
    // 硬件初始化
  }
};
