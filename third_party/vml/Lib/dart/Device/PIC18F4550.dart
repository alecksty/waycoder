// PIC18F4550 设备定义 - Dart 库
// 生成自: Microchip/PIC18/PIC18F4550
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

class PIC18F4550Device {
  static const String deviceName = "PIC18F4550";
  static const String manufacturer = "Microchip";
  static const String family = "PIC18";
  static const String version = "1.0";
  static const String architecture = "PIC18";
  static const int bits = 8;
  static const int clockFrequency = 20000000;

  // 寄存器地址定义
  static const int W_ADDR = 0x0E;  // Working Register
  static const int STATUS_ADDR = 0xFD8;  // Status Register
  static const int STATUS_C_BIT = 0;  // Carry Flag
  static const int STATUS_DC_BIT = 1;  // Digit Carry Flag
  static const int STATUS_Z_BIT = 2;  // Zero Flag
  static const int STATUS_PD_BIT = 3;  // Power-Down Flag
  static const int STATUS_TO_BIT = 4;  // Time-out Flag
  static const int STATUS_RP_BIT = 0;  // Register Bank Select
  static const int STATUS_IRP_BIT = 7;  // Indirect Register Bank Select
  static const int BSR_ADDR = 0xFE0;  // Bank Select Register
  static const int PORTA_ADDR = 0xF80;  // Port A
  static const int PORTB_ADDR = 0xF81;  // Port B
  static const int PORTC_ADDR = 0xF82;  // Port C
  static const int PORTD_ADDR = 0xF83;  // Port D
  static const int PORTE_ADDR = 0xF84;  // Port E
  static const int TRISA_ADDR = 0xF92;  // Tri-state Port A
  static const int TRISB_ADDR = 0xF93;  // Tri-state Port B
  static const int TRISC_ADDR = 0xF94;  // Tri-state Port C
  static const int TRISD_ADDR = 0xF95;  // Tri-state Port D
  static const int TRISE_ADDR = 0xF96;  // Tri-state Port E
  static const int LATA_ADDR = 0xF89;  // Latch Port A
  static const int LATB_ADDR = 0xF8A;  // Latch Port B
  static const int LATC_ADDR = 0xF8B;  // Latch Port C
  static const int LATD_ADDR = 0xF8C;  // Latch Port D
  static const int LATE_ADDR = 0xF8D;  // Latch Port E
  static const int INTCON_ADDR = 0xFF2;  // Interrupt Control
  static const int INTCON_RBIF_BIT = 0;  // Port B Interrupt Flag
  static const int INTCON_INT0IF_BIT = 1;  // INT0 Interrupt Flag
  static const int INTCON_TMR0IF_BIT = 2;  // Timer 0 Interrupt Flag
  static const int INTCON_RBIE_BIT = 3;  // Port B Interrupt Enable
  static const int INTCON_INT0IE_BIT = 4;  // INT0 Interrupt Enable
  static const int INTCON_TMR0IE_BIT = 5;  // Timer 0 Interrupt Enable
  static const int INTCON_PEIE_BIT = 6;  // Peripheral Interrupt Enable
  static const int INTCON_GIE_BIT = 7;  // Global Interrupt Enable
  static const int PIR1_ADDR = 0xF9E;  // Peripheral Interrupt 1
  static const int PIR2_ADDR = 0xF9F;  // Peripheral Interrupt 2
  static const int PIE1_ADDR = 0xF9D;  // Peripheral Interrupt Enable 1
  static const int PIE2_ADDR = 0xF9C;  // Peripheral Interrupt Enable 2
  static const int IPR1_ADDR = 0xF9B;  // Interrupt Priority 1
  static const int IPR2_ADDR = 0xF9A;  // Interrupt Priority 2
  static const int RCON_ADDR = 0xFD0;  // Reset Control
  static const int RCON_NOT_TO_BIT = 3;  // Time-out Flag
  static const int RCON_NOT_PD_BIT = 4;  // Power-Down Flag
  static const int RCON_NOT_RI_BIT = 5;  // RESET Flag
  static const int RCON_NOT_POR_BIT = 6;  // Power-on Reset Flag
  static const int RCON_NOT_BOR_BIT = 7;  // Brown-out Reset Flag
  static const int T0CON_ADDR = 0xFD1;  // Timer 0 Control
  static const int TMR0_ADDR = 0xFD6;  // Timer 0 Register
  static const int T1CON_ADDR = 0xFCD;  // Timer 1 Control
  static const int TMR1_ADDR = 0xFCE;  // Timer 1 Register High
  static const int TMR1L_ADDR = 0xFCF;  // Timer 1 Register Low
  static const int T2CON_ADDR = 0xFCA;  // Timer 2 Control
  static const int TMR2_ADDR = 0xFCB;  // Timer 2 Register
  static const int T3CON_ADDR = 0xFB1;  // Timer 3 Control
  static const int TMR3_ADDR = 0xFB3;  // Timer 3 Register High
  static const int TMR3L_ADDR = 0xFB2;  // Timer 3 Register Low
  static const int SSPCON1_ADDR = 0xFC6;  // SSP Control 1
  static const int SSPCON2_ADDR = 0xFC5;  // SSP Control 2
  static const int SSPSTAT_ADDR = 0xFC7;  // SSP Status
  static const int SSPBUF_ADDR = 0xFC9;  // SSP Buffer
  static const int SSPOR_ADDR = 0xFC8;  // SSP Shift Register
  static const int ADCON0_ADDR = 0xFC2;  // A/D Control 0
  static const int ADCON1_ADDR = 0xFC1;  // A/D Control 1
  static const int ADCON2_ADDR = 0xFC0;  // A/D Control 2
  static const int ADRES_ADDR = 0xFC3;  // A/D Result
  static const int ADRESL_ADDR = 0xFC4;  // A/D Result Low
  static const int CCP1CON_ADDR = 0xFD4;  // CCP 1 Control
  static const int CCPR1_ADDR = 0xFD6;  // CCP 1 Register High
  static const int CCPR1L_ADDR = 0xFD5;  // CCP 1 Register Low
  static const int CCP2CON_ADDR = 0xFBA;  // CCP 2 Control
  static const int CCPR2_ADDR = 0xFBB;  // CCP 2 Register High
  static const int CCPR2L_ADDR = 0xFBC;  // CCP 2 Register Low
  static const int USBCON_ADDR = 0xF75;  // USB Control
  static const int USBSTAT_ADDR = 0xF74;  // USB Status
  static const int UIE_ADDR = 0xF73;  // USB Interrupt Enable
  static const int UIR_ADDR = 0xF72;  // USB Interrupt Flag
  static const int UCON_ADDR = 0xF71;  // USB Control
  static const int USTAT_ADDR = 0xF70;  // USB Status
  static const int UEP0_ADDR = 0xF60;  // USB Endpoint 0
  static const int UEP1_ADDR = 0xF61;  // USB Endpoint 1
  static const int UEP2_ADDR = 0xF62;  // USB Endpoint 2
  static const int UEP3_ADDR = 0xF63;  // USB Endpoint 3
  static const int UEP4_ADDR = 0xF64;  // USB Endpoint 4

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x7FFF;
  static const int FLASH_SIZE = 32768;  // Program Flash (32KB)
  static const int EEPROM_START = 0xF00000;
  static const int EEPROM_END = 0xF000FF;
  static const int EEPROM_SIZE = 256;  // EEPROM (256B)
  static const int SRAM_START = 0x0000;
  static const int SRAM_END = 0x07FF;
  static const int SRAM_SIZE = 2048;  // SRAM (2KB)
  static const int ACCESS_START = 0x0000;
  static const int ACCESS_END = ;
  static const int ACCESS_SIZE = 1;  // Access Bank

  // 外设定义
  // Port A
  static const int PORTA_BASE = 0xF80;
  static const int PORTA_PORT_ADDR = 0xF80;
  static const int PORTA_TRIS_ADDR = 0xF92;
  static const int PORTA_LAT_ADDR = 0xF89;
  // Port B
  static const int PORTB_BASE = 0xF81;
  static const int PORTB_PORT_ADDR = 0xF81;
  static const int PORTB_TRIS_ADDR = 0xF93;
  static const int PORTB_LAT_ADDR = 0xF8A;
  // Port C
  static const int PORTC_BASE = 0xF82;
  static const int PORTC_PORT_ADDR = 0xF82;
  static const int PORTC_TRIS_ADDR = 0xF94;
  static const int PORTC_LAT_ADDR = 0xF8B;
  // Port D
  static const int PORTD_BASE = 0xF83;
  static const int PORTD_PORT_ADDR = 0xF83;
  static const int PORTD_TRIS_ADDR = 0xF95;
  static const int PORTD_LAT_ADDR = 0xF8C;
  // Port E
  static const int PORTE_BASE = 0xF84;
  static const int PORTE_PORT_ADDR = 0xF84;
  static const int PORTE_TRIS_ADDR = 0xF96;
  static const int PORTE_LAT_ADDR = 0xF8D;
  // Timer 0
  static const int TIMER0_BASE = 0xFD1;
  static const int TIMER0_T0CON_ADDR = 0xFD1;
  static const int TIMER0_TMR0_ADDR = 0xFD6;
  // Timer 1
  static const int TIMER1_BASE = 0xFCD;
  static const int TIMER1_T1CON_ADDR = 0xFCD;
  static const int TIMER1_TMR1_ADDR = 0xFCF;
  static const int TIMER1_TMR1L_ADDR = 0xFCE;
  // Timer 2
  static const int TIMER2_BASE = 0xFCA;
  static const int TIMER2_T2CON_ADDR = 0xFCA;
  static const int TIMER2_TMR2_ADDR = 0xFCB;
  // Timer 3
  static const int TIMER3_BASE = 0xFB0;
  static const int TIMER3_T3CON_ADDR = 0xFB0;
  static const int TIMER3_TMR3_ADDR = 0xFB2;
  // A/D Converter
  static const int ADC_BASE = 0xFC2;
  static const int ADC_ADCON0_ADDR = 0xFC2;
  static const int ADC_ADCON1_ADDR = 0xFC1;
  static const int ADC_ADCON2_ADDR = 0xFC0;
  static const int ADC_ADRES_ADDR = 0xFC3;
  static const int ADC_ADRESL_ADDR = 0xFC4;
  // CCP 1
  static const int CCP1_BASE = 0xFD4;
  static const int CCP1_CCP1CON_ADDR = 0xFD4;
  static const int CCP1_CCPR1_ADDR = 0xFD6;
  static const int CCP1_CCPR1L_ADDR = 0xFD5;
  // CCP 2
  static const int CCP2_BASE = 0xFBA;
  static const int CCP2_CCP2CON_ADDR = 0xFBA;
  static const int CCP2_CCPR2_ADDR = 0xFBB;
  static const int CCP2_CCPR2L_ADDR = 0xFBC;
  // SSP (I2C/SPI)
  static const int SSP_BASE = 0xFC6;
  static const int SSP_SSPCON1_ADDR = 0xFC6;
  static const int SSP_SSPCON2_ADDR = 0xFC5;
  static const int SSP_SSPSTAT_ADDR = 0xFC7;
  static const int SSP_SSPBUF_ADDR = 0xFC9;
  static const int SSP_SSPOV_ADDR = 0xFC8;
  // EUSART
  static const int EUSART_BASE = 0xF15;
  static const int EUSART_TXSTA_ADDR = 0xFE2;
  static const int EUSART_RCSTA_ADDR = 0xFE3;
  static const int EUSART_TXREG_ADDR = 0xFAD;
  static const int EUSART_RCREG_ADDR = 0xFAE;
  static const int EUSART_SPBRG_ADDR = 0xFAF;
  static const int EUSART_SPBRGH_ADDR = 0xFB0;
  static const int EUSART_BAUDCON_ADDR = 0xFB8;
  // Comparators
  static const int COMPARATOR_BASE = 0xFB4;
  static const int COMPARATOR_CMCON_ADDR = 0xFB4;
  static const int COMPARATOR_CVRCON_ADDR = 0xFB5;
  // USB Module
  static const int USB_BASE = 0xF70;
  static const int USB_UCON_ADDR = 0xF71;
  static const int USB_USTAT_ADDR = 0xF72;
  static const int USB_UIR_ADDR = 0xF73;
  static const int USB_UIE_ADDR = 0xF74;
  static const int USB_UEP0_ADDR = 0xF80;
  static const int USB_UEP1_ADDR = 0xF81;
  static const int USB_UEP2_ADDR = 0xF82;
  static const int USB_UEP3_ADDR = 0xF83;
  static const int USB_BD0_ADDR = 0xF00;
  static const int USB_BD1_ADDR = 0xF08;
  static const int USB_BD2_ADDR = 0xF10;
  static const int USB_BD3_ADDR = 0xF18;
  // Oscillator
  static const int OSCCON_BASE = 0xFD3;
  static const int OSCCON_OSCCON_ADDR = 0xFD3;
  static const int OSCCON_OSCTUNE_ADDR = 0xFD9;
  // Watchdog Timer
  static const int WDTCON_BASE = 0xFD1;
  static const int WDTCON_WDTCON_ADDR = 0xFD1;

  // 中断向量定义
  static const int INT_RESET = 0;  // RESET
  static const int INT_INT0 = 1;  // External Interrupt 0
  static const int INT_INT1 = 2;  // External Interrupt 1
  static const int INT_INT2 = 3;  // External Interrupt 2
  static const int INT_TMR0 = 4;  // Timer 0 Overflow
  static const int INT_TMR1 = 5;  // Timer 1 Overflow
  static const int INT_TMR2 = 6;  // Timer 2 Match
  static const int INT_TMR3 = 7;  // Timer 3 Overflow
  static const int INT_CCP1 = 8;  // CCP 1
  static const int INT_CCP2 = 9;  // CCP 2
  static const int INT_SSP = 10;  // SSP
  static const int INT_TX = 11;  // USART TX
  static const int INT_RC = 12;  // USART RX
  static const int INT_ADC = 13;  // A/D
  static const int INT_RBO = 14;  // Port B Change
  static const int INT_EXT = 15;  // External

  // 引脚定义
  static const int PIN_RE3 = 1;  // MCLR/VPP/RE3
  static const int PIN_RA0 = 2;  // AN0/RA0
  static const int PIN_RA1 = 3;  // AN1/RA1
  static const int PIN_RA2 = 4;  // AN2/VREF-/RA2
  static const int PIN_RA3 = 5;  // AN3/VREF+/RA3
  static const int PIN_RA4 = 6;  // AN4/T0CKI/RA4
  static const int PIN_RA5 = 7;  // AN5/RE5
  static const int PIN_VSS = 8;  // Ground
  static const int PIN_RA7 = 9;  // OSC1/CLKI/RA7
  static const int PIN_RA6 = 10;  // OSC2/CLKO/RA6
  static const int PIN_RC0 = 11;  // T1OSO/T1CKI/RC0
  static const int PIN_RC1 = 12;  // T1OSI/RC1
  static const int PIN_RC2 = 13;  // CCP1/RC2
  static const int PIN_RC3 = 14;  // SCK/SCL/RC3
  static const int PIN_RD0 = 15;  // SDO/RD0
  static const int PIN_RD1 = 16;  // SDI/RD1
  static const int PIN_RD2 = 17;  // RD2
  static const int PIN_RC6 = 18;  // TX/CK/RC6
  static const int PIN_RC7 = 19;  // RX/DT/RC7
  static const int PIN_VSS = 20;  // Ground
  static const int PIN_RD3 = 21;  // RD3
  static const int PIN_RD4 = 22;  // RD4
  static const int PIN_RD5 = 23;  // PWRB/RD5
  static const int PIN_RD6 = 24;  // PBC/RD6
  static const int PIN_RD7 = 25;  // PCD/RD7
  static const int PIN_RC4 = 26;  // D-/RC4
  static const int PIN_RC5 = 27;  // D+/RC5
  static const int PIN_RE0 = 28;  // AN5/RE0
  static const int PIN_RE1 = 29;  // AN6/RE1
  static const int PIN_RE2 = 30;  // AN7/RE2
  static const int PIN_VSS = 31;  // Ground
  static const int PIN_VDD = 32;  // Vdd

}
