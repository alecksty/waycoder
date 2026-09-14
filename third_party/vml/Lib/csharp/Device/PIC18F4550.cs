using System;

namespace VML.Device.Microchip.PIC18F4550
{
    /// <summary>
    /// PIC18F4550 寄存器定义
    /// 生成自: Microchip/PIC18/PIC18F4550
    /// 版本: 1.0
    /// </summary>
    public static class PIC18F4550
    {
        // CPU架构: PIC18, 8位, 20000000 Hz

        // 寄存器定义
        // Working Register
        public const int W_ADDR = 0x0E;
        public static unsafe byte* W => (byte*)0x0E;

        // Status Register
        public const int STATUS_ADDR = 0xFD8;
        public static unsafe byte* STATUS => (byte*)0xFD8;
        public const int STATUS_C = 0;  // Carry Flag
        public const int STATUS_DC = 1;  // Digit Carry Flag
        public const int STATUS_Z = 2;  // Zero Flag
        public const int STATUS_PD = 3;  // Power-Down Flag
        public const int STATUS_TO = 4;  // Time-out Flag
        public const int STATUS_RP = 0;  // Register Bank Select
        public const int STATUS_IRP = 7;  // Indirect Register Bank Select

        // Bank Select Register
        public const int BSR_ADDR = 0xFE0;
        public static unsafe byte* BSR => (byte*)0xFE0;

        // Port A
        public const int PORTA_ADDR = 0xF80;
        public static unsafe byte* PORTA => (byte*)0xF80;

        // Port B
        public const int PORTB_ADDR = 0xF81;
        public static unsafe byte* PORTB => (byte*)0xF81;

        // Port C
        public const int PORTC_ADDR = 0xF82;
        public static unsafe byte* PORTC => (byte*)0xF82;

        // Port D
        public const int PORTD_ADDR = 0xF83;
        public static unsafe byte* PORTD => (byte*)0xF83;

        // Port E
        public const int PORTE_ADDR = 0xF84;
        public static unsafe byte* PORTE => (byte*)0xF84;

        // Tri-state Port A
        public const int TRISA_ADDR = 0xF92;
        public static unsafe byte* TRISA => (byte*)0xF92;

        // Tri-state Port B
        public const int TRISB_ADDR = 0xF93;
        public static unsafe byte* TRISB => (byte*)0xF93;

        // Tri-state Port C
        public const int TRISC_ADDR = 0xF94;
        public static unsafe byte* TRISC => (byte*)0xF94;

        // Tri-state Port D
        public const int TRISD_ADDR = 0xF95;
        public static unsafe byte* TRISD => (byte*)0xF95;

        // Tri-state Port E
        public const int TRISE_ADDR = 0xF96;
        public static unsafe byte* TRISE => (byte*)0xF96;

        // Latch Port A
        public const int LATA_ADDR = 0xF89;
        public static unsafe byte* LATA => (byte*)0xF89;

        // Latch Port B
        public const int LATB_ADDR = 0xF8A;
        public static unsafe byte* LATB => (byte*)0xF8A;

        // Latch Port C
        public const int LATC_ADDR = 0xF8B;
        public static unsafe byte* LATC => (byte*)0xF8B;

        // Latch Port D
        public const int LATD_ADDR = 0xF8C;
        public static unsafe byte* LATD => (byte*)0xF8C;

        // Latch Port E
        public const int LATE_ADDR = 0xF8D;
        public static unsafe byte* LATE => (byte*)0xF8D;

        // Interrupt Control
        public const int INTCON_ADDR = 0xFF2;
        public static unsafe byte* INTCON => (byte*)0xFF2;
        public const int INTCON_RBIF = 0;  // Port B Interrupt Flag
        public const int INTCON_INT0IF = 1;  // INT0 Interrupt Flag
        public const int INTCON_TMR0IF = 2;  // Timer 0 Interrupt Flag
        public const int INTCON_RBIE = 3;  // Port B Interrupt Enable
        public const int INTCON_INT0IE = 4;  // INT0 Interrupt Enable
        public const int INTCON_TMR0IE = 5;  // Timer 0 Interrupt Enable
        public const int INTCON_PEIE = 6;  // Peripheral Interrupt Enable
        public const int INTCON_GIE = 7;  // Global Interrupt Enable

        // Peripheral Interrupt 1
        public const int PIR1_ADDR = 0xF9E;
        public static unsafe byte* PIR1 => (byte*)0xF9E;

        // Peripheral Interrupt 2
        public const int PIR2_ADDR = 0xF9F;
        public static unsafe byte* PIR2 => (byte*)0xF9F;

        // Peripheral Interrupt Enable 1
        public const int PIE1_ADDR = 0xF9D;
        public static unsafe byte* PIE1 => (byte*)0xF9D;

        // Peripheral Interrupt Enable 2
        public const int PIE2_ADDR = 0xF9C;
        public static unsafe byte* PIE2 => (byte*)0xF9C;

        // Interrupt Priority 1
        public const int IPR1_ADDR = 0xF9B;
        public static unsafe byte* IPR1 => (byte*)0xF9B;

        // Interrupt Priority 2
        public const int IPR2_ADDR = 0xF9A;
        public static unsafe byte* IPR2 => (byte*)0xF9A;

        // Reset Control
        public const int RCON_ADDR = 0xFD0;
        public static unsafe byte* RCON => (byte*)0xFD0;
        public const int RCON_NOT_TO = 3;  // Time-out Flag
        public const int RCON_NOT_PD = 4;  // Power-Down Flag
        public const int RCON_NOT_RI = 5;  // RESET Flag
        public const int RCON_NOT_POR = 6;  // Power-on Reset Flag
        public const int RCON_NOT_BOR = 7;  // Brown-out Reset Flag

        // Timer 0 Control
        public const int T0CON_ADDR = 0xFD1;
        public static unsafe byte* T0CON => (byte*)0xFD1;

        // Timer 0 Register
        public const int TMR0_ADDR = 0xFD6;
        public static unsafe byte* TMR0 => (byte*)0xFD6;

        // Timer 1 Control
        public const int T1CON_ADDR = 0xFCD;
        public static unsafe byte* T1CON => (byte*)0xFCD;

        // Timer 1 Register High
        public const int TMR1_ADDR = 0xFCE;
        public static unsafe byte* TMR1 => (byte*)0xFCE;

        // Timer 1 Register Low
        public const int TMR1L_ADDR = 0xFCF;
        public static unsafe byte* TMR1L => (byte*)0xFCF;

        // Timer 2 Control
        public const int T2CON_ADDR = 0xFCA;
        public static unsafe byte* T2CON => (byte*)0xFCA;

        // Timer 2 Register
        public const int TMR2_ADDR = 0xFCB;
        public static unsafe byte* TMR2 => (byte*)0xFCB;

        // Timer 3 Control
        public const int T3CON_ADDR = 0xFB1;
        public static unsafe byte* T3CON => (byte*)0xFB1;

        // Timer 3 Register High
        public const int TMR3_ADDR = 0xFB3;
        public static unsafe byte* TMR3 => (byte*)0xFB3;

        // Timer 3 Register Low
        public const int TMR3L_ADDR = 0xFB2;
        public static unsafe byte* TMR3L => (byte*)0xFB2;

        // SSP Control 1
        public const int SSPCON1_ADDR = 0xFC6;
        public static unsafe byte* SSPCON1 => (byte*)0xFC6;

        // SSP Control 2
        public const int SSPCON2_ADDR = 0xFC5;
        public static unsafe byte* SSPCON2 => (byte*)0xFC5;

        // SSP Status
        public const int SSPSTAT_ADDR = 0xFC7;
        public static unsafe byte* SSPSTAT => (byte*)0xFC7;

        // SSP Buffer
        public const int SSPBUF_ADDR = 0xFC9;
        public static unsafe byte* SSPBUF => (byte*)0xFC9;

        // SSP Shift Register
        public const int SSPOR_ADDR = 0xFC8;
        public static unsafe byte* SSPOR => (byte*)0xFC8;

        // A/D Control 0
        public const int ADCON0_ADDR = 0xFC2;
        public static unsafe byte* ADCON0 => (byte*)0xFC2;

        // A/D Control 1
        public const int ADCON1_ADDR = 0xFC1;
        public static unsafe byte* ADCON1 => (byte*)0xFC1;

        // A/D Control 2
        public const int ADCON2_ADDR = 0xFC0;
        public static unsafe byte* ADCON2 => (byte*)0xFC0;

        // A/D Result
        public const int ADRES_ADDR = 0xFC3;
        public static unsafe byte* ADRES => (byte*)0xFC3;

        // A/D Result Low
        public const int ADRESL_ADDR = 0xFC4;
        public static unsafe byte* ADRESL => (byte*)0xFC4;

        // CCP 1 Control
        public const int CCP1CON_ADDR = 0xFD4;
        public static unsafe byte* CCP1CON => (byte*)0xFD4;

        // CCP 1 Register High
        public const int CCPR1_ADDR = 0xFD6;
        public static unsafe byte* CCPR1 => (byte*)0xFD6;

        // CCP 1 Register Low
        public const int CCPR1L_ADDR = 0xFD5;
        public static unsafe byte* CCPR1L => (byte*)0xFD5;

        // CCP 2 Control
        public const int CCP2CON_ADDR = 0xFBA;
        public static unsafe byte* CCP2CON => (byte*)0xFBA;

        // CCP 2 Register High
        public const int CCPR2_ADDR = 0xFBB;
        public static unsafe byte* CCPR2 => (byte*)0xFBB;

        // CCP 2 Register Low
        public const int CCPR2L_ADDR = 0xFBC;
        public static unsafe byte* CCPR2L => (byte*)0xFBC;

        // USB Control
        public const int USBCON_ADDR = 0xF75;
        public static unsafe byte* USBCON => (byte*)0xF75;

        // USB Status
        public const int USBSTAT_ADDR = 0xF74;
        public static unsafe byte* USBSTAT => (byte*)0xF74;

        // USB Interrupt Enable
        public const int UIE_ADDR = 0xF73;
        public static unsafe byte* UIE => (byte*)0xF73;

        // USB Interrupt Flag
        public const int UIR_ADDR = 0xF72;
        public static unsafe byte* UIR => (byte*)0xF72;

        // USB Control
        public const int UCON_ADDR = 0xF71;
        public static unsafe byte* UCON => (byte*)0xF71;

        // USB Status
        public const int USTAT_ADDR = 0xF70;
        public static unsafe byte* USTAT => (byte*)0xF70;

        // USB Endpoint 0
        public const int UEP0_ADDR = 0xF60;
        public static unsafe byte* UEP0 => (byte*)0xF60;

        // USB Endpoint 1
        public const int UEP1_ADDR = 0xF61;
        public static unsafe byte* UEP1 => (byte*)0xF61;

        // USB Endpoint 2
        public const int UEP2_ADDR = 0xF62;
        public static unsafe byte* UEP2 => (byte*)0xF62;

        // USB Endpoint 3
        public const int UEP3_ADDR = 0xF63;
        public static unsafe byte* UEP3 => (byte*)0xF63;

        // USB Endpoint 4
        public const int UEP4_ADDR = 0xF64;
        public static unsafe byte* UEP4 => (byte*)0xF64;

        // 内存段定义
        // Program Flash (32KB)
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x7FFF;
        public const int FLASH_SIZE = 32768;

        // EEPROM (256B)
        public const int EEPROM_START = 0xF00000;
        public const int EEPROM_END = 0xF000FF;
        public const int EEPROM_SIZE = 256;

        // SRAM (2KB)
        public const int SRAM_START = 0x0000;
        public const int SRAM_END = 0x07FF;
        public const int SRAM_SIZE = 2048;

        // Access Bank
        public const int ACCESS_START = 0x0000;
        public const int ACCESS_END = ;
        public const int ACCESS_SIZE = 1;

        // 外设定义
        // Port A
        public const int PORTA_BASE = 0xF80;
        public static unsafe byte* PORTA_PORT => (byte*)0x00001F00;
        public static unsafe byte* PORTA_TRIS => (byte*)0x00001F12;
        public static unsafe byte* PORTA_LAT => (byte*)0x00001F09;

        // Port B
        public const int PORTB_BASE = 0xF81;
        public static unsafe byte* PORTB_PORT => (byte*)0x00001F02;
        public static unsafe byte* PORTB_TRIS => (byte*)0x00001F14;
        public static unsafe byte* PORTB_LAT => (byte*)0x00001F0B;

        // Port C
        public const int PORTC_BASE = 0xF82;
        public static unsafe byte* PORTC_PORT => (byte*)0x00001F04;
        public static unsafe byte* PORTC_TRIS => (byte*)0x00001F16;
        public static unsafe byte* PORTC_LAT => (byte*)0x00001F0D;

        // Port D
        public const int PORTD_BASE = 0xF83;
        public static unsafe byte* PORTD_PORT => (byte*)0x00001F06;
        public static unsafe byte* PORTD_TRIS => (byte*)0x00001F18;
        public static unsafe byte* PORTD_LAT => (byte*)0x00001F0F;

        // Port E
        public const int PORTE_BASE = 0xF84;
        public static unsafe byte* PORTE_PORT => (byte*)0x00001F08;
        public static unsafe byte* PORTE_TRIS => (byte*)0x00001F1A;
        public static unsafe byte* PORTE_LAT => (byte*)0x00001F11;

        // Timer 0
        public const int TIMER0_BASE = 0xFD1;
        public static unsafe byte* TIMER0_T0CON => (byte*)0x00001FA2;
        public static unsafe byte* TIMER0_TMR0 => (byte*)0x00001FA7;

        // Timer 1
        public const int TIMER1_BASE = 0xFCD;
        public static unsafe byte* TIMER1_T1CON => (byte*)0x00001F9A;
        public static unsafe byte* TIMER1_TMR1 => (byte*)0x00001F9C;
        public static unsafe byte* TIMER1_TMR1L => (byte*)0x00001F9B;

        // Timer 2
        public const int TIMER2_BASE = 0xFCA;
        public static unsafe byte* TIMER2_T2CON => (byte*)0x00001F94;
        public static unsafe byte* TIMER2_TMR2 => (byte*)0x00001F95;

        // Timer 3
        public const int TIMER3_BASE = 0xFB0;
        public static unsafe byte* TIMER3_T3CON => (byte*)0x00001F60;
        public static unsafe byte* TIMER3_TMR3 => (byte*)0x00001F62;

        // A/D Converter
        public const int ADC_BASE = 0xFC2;
        public static unsafe byte* ADC_ADCON0 => (byte*)0x00001F84;
        public static unsafe byte* ADC_ADCON1 => (byte*)0x00001F83;
        public static unsafe byte* ADC_ADCON2 => (byte*)0x00001F82;
        public static unsafe byte* ADC_ADRES => (byte*)0x00001F85;
        public static unsafe byte* ADC_ADRESL => (byte*)0x00001F86;

        // CCP 1
        public const int CCP1_BASE = 0xFD4;
        public static unsafe byte* CCP1_CCP1CON => (byte*)0x00001FA8;
        public static unsafe byte* CCP1_CCPR1 => (byte*)0x00001FAA;
        public static unsafe byte* CCP1_CCPR1L => (byte*)0x00001FA9;

        // CCP 2
        public const int CCP2_BASE = 0xFBA;
        public static unsafe byte* CCP2_CCP2CON => (byte*)0x00001F74;
        public static unsafe byte* CCP2_CCPR2 => (byte*)0x00001F75;
        public static unsafe byte* CCP2_CCPR2L => (byte*)0x00001F76;

        // SSP (I2C/SPI)
        public const int SSP_BASE = 0xFC6;
        public static unsafe byte* SSP_SSPCON1 => (byte*)0x00001F8C;
        public static unsafe byte* SSP_SSPCON2 => (byte*)0x00001F8B;
        public static unsafe byte* SSP_SSPSTAT => (byte*)0x00001F8D;
        public static unsafe byte* SSP_SSPBUF => (byte*)0x00001F8F;
        public static unsafe byte* SSP_SSPOV => (byte*)0x00001F8E;

        // EUSART
        public const int EUSART_BASE = 0xF15;
        public static unsafe byte* EUSART_TXSTA => (byte*)0x00001EF7;
        public static unsafe byte* EUSART_RCSTA => (byte*)0x00001EF8;
        public static unsafe byte* EUSART_TXREG => (byte*)0x00001EC2;
        public static unsafe byte* EUSART_RCREG => (byte*)0x00001EC3;
        public static unsafe byte* EUSART_SPBRG => (byte*)0x00001EC4;
        public static unsafe byte* EUSART_SPBRGH => (byte*)0x00001EC5;
        public static unsafe byte* EUSART_BAUDCON => (byte*)0x00001ECD;

        // Comparators
        public const int COMPARATOR_BASE = 0xFB4;
        public static unsafe byte* COMPARATOR_CMCON => (byte*)0x00001F68;
        public static unsafe byte* COMPARATOR_CVRCON => (byte*)0x00001F69;

        // USB Module
        public const int USB_BASE = 0xF70;
        public static unsafe byte* USB_UCON => (byte*)0x00001EE1;
        public static unsafe byte* USB_USTAT => (byte*)0x00001EE2;
        public static unsafe byte* USB_UIR => (byte*)0x00001EE3;
        public static unsafe byte* USB_UIE => (byte*)0x00001EE4;
        public static unsafe byte* USB_UEP0 => (byte*)0x00001EF0;
        public static unsafe byte* USB_UEP1 => (byte*)0x00001EF1;
        public static unsafe byte* USB_UEP2 => (byte*)0x00001EF2;
        public static unsafe byte* USB_UEP3 => (byte*)0x00001EF3;
        public static unsafe byte* USB_BD0 => (byte*)0x00001E70;
        public static unsafe byte* USB_BD1 => (byte*)0x00001E78;
        public static unsafe byte* USB_BD2 => (byte*)0x00001E80;
        public static unsafe byte* USB_BD3 => (byte*)0x00001E88;

        // Oscillator
        public const int OSCCON_BASE = 0xFD3;
        public static unsafe byte* OSCCON_OSCCON => (byte*)0x00001FA6;
        public static unsafe byte* OSCCON_OSCTUNE => (byte*)0x00001FAC;

        // Watchdog Timer
        public const int WDTCON_BASE = 0xFD1;
        public static unsafe byte* WDTCON_WDTCON => (byte*)0x00001FA2;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // RESET
        public const int IRQ_INT0 = 1;  // External Interrupt 0
        public const int IRQ_INT1 = 2;  // External Interrupt 1
        public const int IRQ_INT2 = 3;  // External Interrupt 2
        public const int IRQ_TMR0 = 4;  // Timer 0 Overflow
        public const int IRQ_TMR1 = 5;  // Timer 1 Overflow
        public const int IRQ_TMR2 = 6;  // Timer 2 Match
        public const int IRQ_TMR3 = 7;  // Timer 3 Overflow
        public const int IRQ_CCP1 = 8;  // CCP 1
        public const int IRQ_CCP2 = 9;  // CCP 2
        public const int IRQ_SSP = 10;  // SSP
        public const int IRQ_TX = 11;  // USART TX
        public const int IRQ_RC = 12;  // USART RX
        public const int IRQ_ADC = 13;  // A/D
        public const int IRQ_RBO = 14;  // Port B Change
        public const int IRQ_EXT = 15;  // External

        // 引脚定义
        public const int PIN_RE3 = 1;  // MCLR/VPP/RE3
        public const int PIN_RA0 = 2;  // AN0/RA0
        public const int PIN_RA1 = 3;  // AN1/RA1
        public const int PIN_RA2 = 4;  // AN2/VREF-/RA2
        public const int PIN_RA3 = 5;  // AN3/VREF+/RA3
        public const int PIN_RA4 = 6;  // AN4/T0CKI/RA4
        public const int PIN_RA5 = 7;  // AN5/RE5
        public const int PIN_VSS = 8;  // Ground
        public const int PIN_RA7 = 9;  // OSC1/CLKI/RA7
        public const int PIN_RA6 = 10;  // OSC2/CLKO/RA6
        public const int PIN_RC0 = 11;  // T1OSO/T1CKI/RC0
        public const int PIN_RC1 = 12;  // T1OSI/RC1
        public const int PIN_RC2 = 13;  // CCP1/RC2
        public const int PIN_RC3 = 14;  // SCK/SCL/RC3
        public const int PIN_RD0 = 15;  // SDO/RD0
        public const int PIN_RD1 = 16;  // SDI/RD1
        public const int PIN_RD2 = 17;  // RD2
        public const int PIN_RC6 = 18;  // TX/CK/RC6
        public const int PIN_RC7 = 19;  // RX/DT/RC7
        public const int PIN_VSS = 20;  // Ground
        public const int PIN_RD3 = 21;  // RD3
        public const int PIN_RD4 = 22;  // RD4
        public const int PIN_RD5 = 23;  // PWRB/RD5
        public const int PIN_RD6 = 24;  // PBC/RD6
        public const int PIN_RD7 = 25;  // PCD/RD7
        public const int PIN_RC4 = 26;  // D-/RC4
        public const int PIN_RC5 = 27;  // D+/RC5
        public const int PIN_RE0 = 28;  // AN5/RE0
        public const int PIN_RE1 = 29;  // AN6/RE1
        public const int PIN_RE2 = 30;  // AN7/RE2
        public const int PIN_VSS = 31;  // Ground
        public const int PIN_VDD = 32;  // Vdd

        public static void pic18f4550_init()
        {
            // 硬件初始化代码
        }
    }
}
