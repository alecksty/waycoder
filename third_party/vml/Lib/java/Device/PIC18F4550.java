package vml.device.microchip.pic18f4550;

/**
 * PIC18F4550 寄存器定义
 * 生成自: Microchip/PIC18/PIC18F4550
 * 版本: 1.0
 */
public final class PIC18F4550 {
    private PIC18F4550() {} // 工具类
    // CPU架构: PIC18, 8位, 20000000 Hz

    // 寄存器定义
    // Working Register
    public static final int W_ADDR = (int)0x0E;

    // Status Register
    public static final int STATUS_ADDR = (int)0xFD8;
    public static final int STATUS_C = 0;  // Carry Flag
    public static final int STATUS_DC = 1;  // Digit Carry Flag
    public static final int STATUS_Z = 2;  // Zero Flag
    public static final int STATUS_PD = 3;  // Power-Down Flag
    public static final int STATUS_TO = 4;  // Time-out Flag
    public static final int STATUS_RP = 0;  // Register Bank Select
    public static final int STATUS_IRP = 7;  // Indirect Register Bank Select

    // Bank Select Register
    public static final int BSR_ADDR = (int)0xFE0;

    // Port A
    public static final int PORTA_ADDR = (int)0xF80;

    // Port B
    public static final int PORTB_ADDR = (int)0xF81;

    // Port C
    public static final int PORTC_ADDR = (int)0xF82;

    // Port D
    public static final int PORTD_ADDR = (int)0xF83;

    // Port E
    public static final int PORTE_ADDR = (int)0xF84;

    // Tri-state Port A
    public static final int TRISA_ADDR = (int)0xF92;

    // Tri-state Port B
    public static final int TRISB_ADDR = (int)0xF93;

    // Tri-state Port C
    public static final int TRISC_ADDR = (int)0xF94;

    // Tri-state Port D
    public static final int TRISD_ADDR = (int)0xF95;

    // Tri-state Port E
    public static final int TRISE_ADDR = (int)0xF96;

    // Latch Port A
    public static final int LATA_ADDR = (int)0xF89;

    // Latch Port B
    public static final int LATB_ADDR = (int)0xF8A;

    // Latch Port C
    public static final int LATC_ADDR = (int)0xF8B;

    // Latch Port D
    public static final int LATD_ADDR = (int)0xF8C;

    // Latch Port E
    public static final int LATE_ADDR = (int)0xF8D;

    // Interrupt Control
    public static final int INTCON_ADDR = (int)0xFF2;
    public static final int INTCON_RBIF = 0;  // Port B Interrupt Flag
    public static final int INTCON_INT0IF = 1;  // INT0 Interrupt Flag
    public static final int INTCON_TMR0IF = 2;  // Timer 0 Interrupt Flag
    public static final int INTCON_RBIE = 3;  // Port B Interrupt Enable
    public static final int INTCON_INT0IE = 4;  // INT0 Interrupt Enable
    public static final int INTCON_TMR0IE = 5;  // Timer 0 Interrupt Enable
    public static final int INTCON_PEIE = 6;  // Peripheral Interrupt Enable
    public static final int INTCON_GIE = 7;  // Global Interrupt Enable

    // Peripheral Interrupt 1
    public static final int PIR1_ADDR = (int)0xF9E;

    // Peripheral Interrupt 2
    public static final int PIR2_ADDR = (int)0xF9F;

    // Peripheral Interrupt Enable 1
    public static final int PIE1_ADDR = (int)0xF9D;

    // Peripheral Interrupt Enable 2
    public static final int PIE2_ADDR = (int)0xF9C;

    // Interrupt Priority 1
    public static final int IPR1_ADDR = (int)0xF9B;

    // Interrupt Priority 2
    public static final int IPR2_ADDR = (int)0xF9A;

    // Reset Control
    public static final int RCON_ADDR = (int)0xFD0;
    public static final int RCON_NOT_TO = 3;  // Time-out Flag
    public static final int RCON_NOT_PD = 4;  // Power-Down Flag
    public static final int RCON_NOT_RI = 5;  // RESET Flag
    public static final int RCON_NOT_POR = 6;  // Power-on Reset Flag
    public static final int RCON_NOT_BOR = 7;  // Brown-out Reset Flag

    // Timer 0 Control
    public static final int T0CON_ADDR = (int)0xFD1;

    // Timer 0 Register
    public static final int TMR0_ADDR = (int)0xFD6;

    // Timer 1 Control
    public static final int T1CON_ADDR = (int)0xFCD;

    // Timer 1 Register High
    public static final int TMR1_ADDR = (int)0xFCE;

    // Timer 1 Register Low
    public static final int TMR1L_ADDR = (int)0xFCF;

    // Timer 2 Control
    public static final int T2CON_ADDR = (int)0xFCA;

    // Timer 2 Register
    public static final int TMR2_ADDR = (int)0xFCB;

    // Timer 3 Control
    public static final int T3CON_ADDR = (int)0xFB1;

    // Timer 3 Register High
    public static final int TMR3_ADDR = (int)0xFB3;

    // Timer 3 Register Low
    public static final int TMR3L_ADDR = (int)0xFB2;

    // SSP Control 1
    public static final int SSPCON1_ADDR = (int)0xFC6;

    // SSP Control 2
    public static final int SSPCON2_ADDR = (int)0xFC5;

    // SSP Status
    public static final int SSPSTAT_ADDR = (int)0xFC7;

    // SSP Buffer
    public static final int SSPBUF_ADDR = (int)0xFC9;

    // SSP Shift Register
    public static final int SSPOR_ADDR = (int)0xFC8;

    // A/D Control 0
    public static final int ADCON0_ADDR = (int)0xFC2;

    // A/D Control 1
    public static final int ADCON1_ADDR = (int)0xFC1;

    // A/D Control 2
    public static final int ADCON2_ADDR = (int)0xFC0;

    // A/D Result
    public static final int ADRES_ADDR = (int)0xFC3;

    // A/D Result Low
    public static final int ADRESL_ADDR = (int)0xFC4;

    // CCP 1 Control
    public static final int CCP1CON_ADDR = (int)0xFD4;

    // CCP 1 Register High
    public static final int CCPR1_ADDR = (int)0xFD6;

    // CCP 1 Register Low
    public static final int CCPR1L_ADDR = (int)0xFD5;

    // CCP 2 Control
    public static final int CCP2CON_ADDR = (int)0xFBA;

    // CCP 2 Register High
    public static final int CCPR2_ADDR = (int)0xFBB;

    // CCP 2 Register Low
    public static final int CCPR2L_ADDR = (int)0xFBC;

    // USB Control
    public static final int USBCON_ADDR = (int)0xF75;

    // USB Status
    public static final int USBSTAT_ADDR = (int)0xF74;

    // USB Interrupt Enable
    public static final int UIE_ADDR = (int)0xF73;

    // USB Interrupt Flag
    public static final int UIR_ADDR = (int)0xF72;

    // USB Control
    public static final int UCON_ADDR = (int)0xF71;

    // USB Status
    public static final int USTAT_ADDR = (int)0xF70;

    // USB Endpoint 0
    public static final int UEP0_ADDR = (int)0xF60;

    // USB Endpoint 1
    public static final int UEP1_ADDR = (int)0xF61;

    // USB Endpoint 2
    public static final int UEP2_ADDR = (int)0xF62;

    // USB Endpoint 3
    public static final int UEP3_ADDR = (int)0xF63;

    // USB Endpoint 4
    public static final int UEP4_ADDR = (int)0xF64;

    // 内存段定义
    // Program Flash (32KB)
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x7FFF;
    public static final int FLASH_SIZE = 32768;

    // EEPROM (256B)
    public static final int EEPROM_START = (int)0xF00000;
    public static final int EEPROM_END = (int)0xF000FF;
    public static final int EEPROM_SIZE = 256;

    // SRAM (2KB)
    public static final int SRAM_START = (int)0x0000;
    public static final int SRAM_END = (int)0x07FF;
    public static final int SRAM_SIZE = 2048;

    // Access Bank
    public static final int ACCESS_START = (int)0x0000;
    public static final int ACCESS_END = (int);
    public static final int ACCESS_SIZE = 1;

    // 外设定义
    // Port A
    public static final int PORTA_BASE = (int)0xF80;
    public static final int PORTA_PORT = (int)0x00001F00;
    public static final int PORTA_TRIS = (int)0x00001F12;
    public static final int PORTA_LAT = (int)0x00001F09;

    // Port B
    public static final int PORTB_BASE = (int)0xF81;
    public static final int PORTB_PORT = (int)0x00001F02;
    public static final int PORTB_TRIS = (int)0x00001F14;
    public static final int PORTB_LAT = (int)0x00001F0B;

    // Port C
    public static final int PORTC_BASE = (int)0xF82;
    public static final int PORTC_PORT = (int)0x00001F04;
    public static final int PORTC_TRIS = (int)0x00001F16;
    public static final int PORTC_LAT = (int)0x00001F0D;

    // Port D
    public static final int PORTD_BASE = (int)0xF83;
    public static final int PORTD_PORT = (int)0x00001F06;
    public static final int PORTD_TRIS = (int)0x00001F18;
    public static final int PORTD_LAT = (int)0x00001F0F;

    // Port E
    public static final int PORTE_BASE = (int)0xF84;
    public static final int PORTE_PORT = (int)0x00001F08;
    public static final int PORTE_TRIS = (int)0x00001F1A;
    public static final int PORTE_LAT = (int)0x00001F11;

    // Timer 0
    public static final int TIMER0_BASE = (int)0xFD1;
    public static final int TIMER0_T0CON = (int)0x00001FA2;
    public static final int TIMER0_TMR0 = (int)0x00001FA7;

    // Timer 1
    public static final int TIMER1_BASE = (int)0xFCD;
    public static final int TIMER1_T1CON = (int)0x00001F9A;
    public static final int TIMER1_TMR1 = (int)0x00001F9C;
    public static final int TIMER1_TMR1L = (int)0x00001F9B;

    // Timer 2
    public static final int TIMER2_BASE = (int)0xFCA;
    public static final int TIMER2_T2CON = (int)0x00001F94;
    public static final int TIMER2_TMR2 = (int)0x00001F95;

    // Timer 3
    public static final int TIMER3_BASE = (int)0xFB0;
    public static final int TIMER3_T3CON = (int)0x00001F60;
    public static final int TIMER3_TMR3 = (int)0x00001F62;

    // A/D Converter
    public static final int ADC_BASE = (int)0xFC2;
    public static final int ADC_ADCON0 = (int)0x00001F84;
    public static final int ADC_ADCON1 = (int)0x00001F83;
    public static final int ADC_ADCON2 = (int)0x00001F82;
    public static final int ADC_ADRES = (int)0x00001F85;
    public static final int ADC_ADRESL = (int)0x00001F86;

    // CCP 1
    public static final int CCP1_BASE = (int)0xFD4;
    public static final int CCP1_CCP1CON = (int)0x00001FA8;
    public static final int CCP1_CCPR1 = (int)0x00001FAA;
    public static final int CCP1_CCPR1L = (int)0x00001FA9;

    // CCP 2
    public static final int CCP2_BASE = (int)0xFBA;
    public static final int CCP2_CCP2CON = (int)0x00001F74;
    public static final int CCP2_CCPR2 = (int)0x00001F75;
    public static final int CCP2_CCPR2L = (int)0x00001F76;

    // SSP (I2C/SPI)
    public static final int SSP_BASE = (int)0xFC6;
    public static final int SSP_SSPCON1 = (int)0x00001F8C;
    public static final int SSP_SSPCON2 = (int)0x00001F8B;
    public static final int SSP_SSPSTAT = (int)0x00001F8D;
    public static final int SSP_SSPBUF = (int)0x00001F8F;
    public static final int SSP_SSPOV = (int)0x00001F8E;

    // EUSART
    public static final int EUSART_BASE = (int)0xF15;
    public static final int EUSART_TXSTA = (int)0x00001EF7;
    public static final int EUSART_RCSTA = (int)0x00001EF8;
    public static final int EUSART_TXREG = (int)0x00001EC2;
    public static final int EUSART_RCREG = (int)0x00001EC3;
    public static final int EUSART_SPBRG = (int)0x00001EC4;
    public static final int EUSART_SPBRGH = (int)0x00001EC5;
    public static final int EUSART_BAUDCON = (int)0x00001ECD;

    // Comparators
    public static final int COMPARATOR_BASE = (int)0xFB4;
    public static final int COMPARATOR_CMCON = (int)0x00001F68;
    public static final int COMPARATOR_CVRCON = (int)0x00001F69;

    // USB Module
    public static final int USB_BASE = (int)0xF70;
    public static final int USB_UCON = (int)0x00001EE1;
    public static final int USB_USTAT = (int)0x00001EE2;
    public static final int USB_UIR = (int)0x00001EE3;
    public static final int USB_UIE = (int)0x00001EE4;
    public static final int USB_UEP0 = (int)0x00001EF0;
    public static final int USB_UEP1 = (int)0x00001EF1;
    public static final int USB_UEP2 = (int)0x00001EF2;
    public static final int USB_UEP3 = (int)0x00001EF3;
    public static final int USB_BD0 = (int)0x00001E70;
    public static final int USB_BD1 = (int)0x00001E78;
    public static final int USB_BD2 = (int)0x00001E80;
    public static final int USB_BD3 = (int)0x00001E88;

    // Oscillator
    public static final int OSCCON_BASE = (int)0xFD3;
    public static final int OSCCON_OSCCON = (int)0x00001FA6;
    public static final int OSCCON_OSCTUNE = (int)0x00001FAC;

    // Watchdog Timer
    public static final int WDTCON_BASE = (int)0xFD1;
    public static final int WDTCON_WDTCON = (int)0x00001FA2;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // RESET
    public static final int IRQ_INT0 = 1;  // External Interrupt 0
    public static final int IRQ_INT1 = 2;  // External Interrupt 1
    public static final int IRQ_INT2 = 3;  // External Interrupt 2
    public static final int IRQ_TMR0 = 4;  // Timer 0 Overflow
    public static final int IRQ_TMR1 = 5;  // Timer 1 Overflow
    public static final int IRQ_TMR2 = 6;  // Timer 2 Match
    public static final int IRQ_TMR3 = 7;  // Timer 3 Overflow
    public static final int IRQ_CCP1 = 8;  // CCP 1
    public static final int IRQ_CCP2 = 9;  // CCP 2
    public static final int IRQ_SSP = 10;  // SSP
    public static final int IRQ_TX = 11;  // USART TX
    public static final int IRQ_RC = 12;  // USART RX
    public static final int IRQ_ADC = 13;  // A/D
    public static final int IRQ_RBO = 14;  // Port B Change
    public static final int IRQ_EXT = 15;  // External

    // 引脚定义
    public static final int PIN_RE3 = 1;  // MCLR/VPP/RE3
    public static final int PIN_RA0 = 2;  // AN0/RA0
    public static final int PIN_RA1 = 3;  // AN1/RA1
    public static final int PIN_RA2 = 4;  // AN2/VREF-/RA2
    public static final int PIN_RA3 = 5;  // AN3/VREF+/RA3
    public static final int PIN_RA4 = 6;  // AN4/T0CKI/RA4
    public static final int PIN_RA5 = 7;  // AN5/RE5
    public static final int PIN_VSS = 8;  // Ground
    public static final int PIN_RA7 = 9;  // OSC1/CLKI/RA7
    public static final int PIN_RA6 = 10;  // OSC2/CLKO/RA6
    public static final int PIN_RC0 = 11;  // T1OSO/T1CKI/RC0
    public static final int PIN_RC1 = 12;  // T1OSI/RC1
    public static final int PIN_RC2 = 13;  // CCP1/RC2
    public static final int PIN_RC3 = 14;  // SCK/SCL/RC3
    public static final int PIN_RD0 = 15;  // SDO/RD0
    public static final int PIN_RD1 = 16;  // SDI/RD1
    public static final int PIN_RD2 = 17;  // RD2
    public static final int PIN_RC6 = 18;  // TX/CK/RC6
    public static final int PIN_RC7 = 19;  // RX/DT/RC7
    public static final int PIN_VSS = 20;  // Ground
    public static final int PIN_RD3 = 21;  // RD3
    public static final int PIN_RD4 = 22;  // RD4
    public static final int PIN_RD5 = 23;  // PWRB/RD5
    public static final int PIN_RD6 = 24;  // PBC/RD6
    public static final int PIN_RD7 = 25;  // PCD/RD7
    public static final int PIN_RC4 = 26;  // D-/RC4
    public static final int PIN_RC5 = 27;  // D+/RC5
    public static final int PIN_RE0 = 28;  // AN5/RE0
    public static final int PIN_RE1 = 29;  // AN6/RE1
    public static final int PIN_RE2 = 30;  // AN7/RE2
    public static final int PIN_VSS = 31;  // Ground
    public static final int PIN_VDD = 32;  // Vdd

    public static native void pic18f4550_init();
}
