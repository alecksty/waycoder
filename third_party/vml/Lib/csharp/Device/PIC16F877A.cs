using System;

namespace VML.Device.Microchip.PIC16F877A
{
    /// <summary>
    /// PIC16F877A 寄存器定义
    /// 生成自: Microchip/PIC/PIC16F877A
    /// 版本: 1.0
    /// </summary>
    public static class PIC16F877A
    {
        // CPU架构: PIC16, 8位, 4000000 Hz

        // 寄存器定义
        // Working Register
        public const int W_ADDR = 0x00;
        public static unsafe byte* W => (byte*)0x00;

        // Status Register
        public const int STATUS_ADDR = 0x03;
        public static unsafe byte* STATUS => (byte*)0x03;
        public const int STATUS_C = 0;  // Carry flag
        public const int STATUS_DC = 1;  // Digit carry flag
        public const int STATUS_Z = 2;  // Zero flag
        public const int STATUS_PD = 3;  // Power-down flag
        public const int STATUS_TO = 4;  // Time-out flag
        public const int STATUS_RP = 5;  // Register bank select
        public const int STATUS_IRP = 7;  // Indirect register bank select

        // Interrupt Control Register
        public const int INTCON_ADDR = 0x0B;
        public static unsafe byte* INTCON => (byte*)0x0B;
        public const int INTCON_RBIF = 0;  // PORTB change interrupt flag
        public const int INTCON_INTF = 1;  // External interrupt flag
        public const int INTCON_TMR0IF = 2;  // TMR0 overflow interrupt flag
        public const int INTCON_RBIE = 3;  // PORTB change interrupt enable
        public const int INTCON_INTE = 4;  // External interrupt enable
        public const int INTCON_TMR0IE = 5;  // TMR0 overflow interrupt enable
        public const int INTCON_PEIE = 6;  // Peripheral interrupt enable
        public const int INTCON_GIE = 7;  // Global interrupt enable

        // PORT B
        public const int PORTB_ADDR = 0x06;
        public static unsafe byte* PORTB => (byte*)0x06;

        // TRIS B
        public const int TRISB_ADDR = 0x86;
        public static unsafe byte* TRISB => (byte*)0x86;

        // PORT C
        public const int PORTC_ADDR = 0x07;
        public static unsafe byte* PORTC => (byte*)0x07;

        // TRIS C
        public const int TRISC_ADDR = 0x87;
        public static unsafe byte* TRISC => (byte*)0x87;

        // PORT D
        public const int PORTD_ADDR = 0x08;
        public static unsafe byte* PORTD => (byte*)0x08;

        // TRIS D
        public const int TRISD_ADDR = 0x88;
        public static unsafe byte* TRISD => (byte*)0x88;

        // PORT E
        public const int PORTE_ADDR = 0x09;
        public static unsafe byte* PORTE => (byte*)0x09;

        // TRIS E
        public const int TRISE_ADDR = 0x89;
        public static unsafe byte* TRISE => (byte*)0x89;

        // Timer 0
        public const int TMR0_ADDR = 0x01;
        public static unsafe byte* TMR0 => (byte*)0x01;

        // Option Register
        public const int OPTION_REG_ADDR = 0x81;
        public static unsafe byte* OPTION_REG => (byte*)0x81;

        // Program Counter Low
        public const int PCL_ADDR = 0x02;
        public static unsafe byte* PCL => (byte*)0x02;

        // Program Counter Latch High
        public const int PCLATH_ADDR = 0x0A;
        public static unsafe byte* PCLATH => (byte*)0x0A;

        // File Select Register
        public const int FSR_ADDR = 0x04;
        public static unsafe byte* FSR => (byte*)0x04;

        // EEPROM Data
        public const int EEDATA_ADDR = 0x10C;
        public static unsafe byte* EEDATA => (byte*)0x10C;

        // EEPROM Address
        public const int EEADR_ADDR = 0x10D;
        public static unsafe byte* EEADR => (byte*)0x10D;

        // EEPROM Control 1
        public const int EECON1_ADDR = 0x18C;
        public static unsafe byte* EECON1 => (byte*)0x18C;
        public const int EECON1_RD = 0;  // Read control
        public const int EECON1_WR = 1;  // Write control
        public const int EECON1_WREN = 2;  // Write enable
        public const int EECON1_WRERR = 3;  // Write error flag
        public const int EECON1_EEPGD = 7;  // EEPROM program/data select

        // EEPROM Control 2
        public const int EECON2_ADDR = 0x18D;
        public static unsafe byte* EECON2 => (byte*)0x18D;

        // A/D Result High
        public const int ADRESH_ADDR = 0x1E;
        public static unsafe byte* ADRESH => (byte*)0x1E;

        // A/D Result Low
        public const int ADRESL_ADDR = 0x1F;
        public static unsafe byte* ADRESL => (byte*)0x1F;

        // A/D Control 0
        public const int ADCON0_ADDR = 0x1F;
        public static unsafe byte* ADCON0 => (byte*)0x1F;
        public const int ADCON0_ADON = 0;  // A/D enable
        public const int ADCON0_GO_DONE = 2;  // A/D conversion status
        public const int ADCON0_CHS = 3;  // Channel select

        // A/D Control 1
        public const int ADCON1_ADDR = 0x9F;
        public static unsafe byte* ADCON1 => (byte*)0x9F;

        // MSSP Status
        public const int SSPSTAT_ADDR = 0x94;
        public static unsafe byte* SSPSTAT => (byte*)0x94;

        // MSSP Control
        public const int SSPCON_ADDR = 0x14;
        public static unsafe byte* SSPCON => (byte*)0x14;

        // SSP Buffer
        public const int SSPBUF_ADDR = 0x13;
        public static unsafe byte* SSPBUF => (byte*)0x13;

        // USART Transmit Register
        public const int TXREG_ADDR = 0x19;
        public static unsafe byte* TXREG => (byte*)0x19;

        // USART Receive Register
        public const int RCREG_ADDR = 0x1A;
        public static unsafe byte* RCREG => (byte*)0x1A;

        // Baud Rate Generator
        public const int SPBRG_ADDR = 0x99;
        public static unsafe byte* SPBRG => (byte*)0x99;

        // TX Status and Control
        public const int TXSTA_ADDR = 0x98;
        public static unsafe byte* TXSTA => (byte*)0x98;

        // RX Status and Control
        public const int RCSTA_ADDR = 0x18;
        public static unsafe byte* RCSTA => (byte*)0x18;

        // CCP1 Control
        public const int CCP1CON_ADDR = 0x17;
        public static unsafe byte* CCP1CON => (byte*)0x17;

        // CCP1 Low
        public const int CCPR1L_ADDR = 0x15;
        public static unsafe byte* CCPR1L => (byte*)0x15;

        // CCP1 High
        public const int CCPR1H_ADDR = 0x16;
        public static unsafe byte* CCPR1H => (byte*)0x16;

        // CCP2 Control
        public const int CCP2CON_ADDR = 0x1D;
        public static unsafe byte* CCP2CON => (byte*)0x1D;

        // CCP2 Low
        public const int CCPR2L_ADDR = 0x1B;
        public static unsafe byte* CCPR2L => (byte*)0x1B;

        // CCP2 High
        public const int CCPR2H_ADDR = 0x1C;
        public static unsafe byte* CCPR2H => (byte*)0x1C;

        // Timer 1 Control
        public const int T1CON_ADDR = 0x10;
        public static unsafe byte* T1CON => (byte*)0x10;

        // Timer 1 Low
        public const int TMR1L_ADDR = 0x0E;
        public static unsafe byte* TMR1L => (byte*)0x0E;

        // Timer 1 High
        public const int TMR1H_ADDR = 0x0F;
        public static unsafe byte* TMR1H => (byte*)0x0F;

        // Timer 2 Control
        public const int T2CON_ADDR = 0x12;
        public static unsafe byte* T2CON => (byte*)0x12;

        // Timer 2
        public const int TMR2_ADDR = 0x11;
        public static unsafe byte* TMR2 => (byte*)0x11;

        // Timer 2 Period
        public const int PR2_ADDR = 0x92;
        public static unsafe byte* PR2 => (byte*)0x92;

        // 内存段定义
        // Program Memory (8KB)
        public const int PROGRAM_START = 0x0000;
        public const int PROGRAM_END = 0x1FFF;
        public const int PROGRAM_SIZE = 8192;

        // General Purpose RAM Bank 0
        public const int DATA_START = 0x20;
        public const int DATA_END = 0x7F;
        public const int DATA_SIZE = 96;

        // General Purpose RAM Bank 1
        public const int SRAM_START = 0xA0;
        public const int SRAM_END = 0xFF;
        public const int SRAM_SIZE = 96;

        // EEPROM Data Memory
        public const int EEPROM_START = 0x2100;
        public const int EEPROM_END = 0x21FF;
        public const int EEPROM_SIZE = 256;

        // 外设定义
        // Port B
        public const int GPIO_PORTB_BASE = 0x06;
        public static unsafe byte* GPIO_PORTB_PORTB => (byte*)0x0000000C;
        public static unsafe byte* GPIO_PORTB_TRISB => (byte*)0x0000008C;

        // Port C
        public const int GPIO_PORTC_BASE = 0x07;
        public static unsafe byte* GPIO_PORTC_PORTC => (byte*)0x0000000E;
        public static unsafe byte* GPIO_PORTC_TRISC => (byte*)0x0000008E;

        // Port D
        public const int GPIO_PORTD_BASE = 0x08;
        public static unsafe byte* GPIO_PORTD_PORTD => (byte*)0x00000010;
        public static unsafe byte* GPIO_PORTD_TRISD => (byte*)0x00000090;

        // Timer 0
        public const int TIMER0_BASE = 0x01;
        public static unsafe byte* TIMER0_TMR0 => (byte*)0x00000002;
        public static unsafe byte* TIMER0_OPTION_REG => (byte*)0x00000082;

        // Timer 1
        public const int TIMER1_BASE = 0x0E;
        public static unsafe byte* TIMER1_T1CON => (byte*)0x0000001E;
        public static unsafe byte* TIMER1_TMR1L => (byte*)0x0000001C;
        public static unsafe byte* TIMER1_TMR1H => (byte*)0x0000001D;

        // Timer 2
        public const int TIMER2_BASE = 0x11;
        public static unsafe byte* TIMER2_T2CON => (byte*)0x00000023;
        public static unsafe byte* TIMER2_TMR2 => (byte*)0x00000022;
        public static unsafe byte* TIMER2_PR2 => (byte*)0x000000A3;

        // A/D Converter
        public const int ADC_BASE = 0x1E;
        public static unsafe byte* ADC_ADRESH => (byte*)0x0000003C;
        public static unsafe byte* ADC_ADRESL => (byte*)0x000000BD;
        public static unsafe byte* ADC_ADCON0 => (byte*)0x0000003D;
        public static unsafe byte* ADC_ADCON1 => (byte*)0x000000BD;

        // Master Synchronous Serial Port
        public const int MSSP_BASE = 0x13;
        public static unsafe byte* MSSP_SSPSTAT => (byte*)0x000000A7;
        public static unsafe byte* MSSP_SSPCON => (byte*)0x00000027;
        public static unsafe byte* MSSP_SSPBUF => (byte*)0x00000026;

        // USART
        public const int USART_BASE = 0x19;
        public static unsafe byte* USART_TXREG => (byte*)0x00000032;
        public static unsafe byte* USART_RCREG => (byte*)0x00000033;
        public static unsafe byte* USART_SPBRG => (byte*)0x000000B2;
        public static unsafe byte* USART_TXSTA => (byte*)0x000000B1;
        public static unsafe byte* USART_RCSTA => (byte*)0x00000031;

        // Capture/Compare/PWM 1
        public const int CCP1_BASE = 0x15;
        public static unsafe byte* CCP1_CCP1CON => (byte*)0x0000002C;
        public static unsafe byte* CCP1_CCPR1L => (byte*)0x0000002A;
        public static unsafe byte* CCP1_CCPR1H => (byte*)0x0000002B;

        // Capture/Compare/PWM 2
        public const int CCP2_BASE = 0x1B;
        public static unsafe byte* CCP2_CCP2CON => (byte*)0x00000038;
        public static unsafe byte* CCP2_CCPR2L => (byte*)0x00000036;
        public static unsafe byte* CCP2_CCPR2H => (byte*)0x00000037;

        // 中断向量定义
        public const int IRQ_INT = 1;  // External Interrupt
        public const int IRQ_TMR0 = 2;  // Timer 0 Overflow
        public const int IRQ_RB = 3;  // PORTB Change
        public const int IRQ_CCP1 = 4;  // CCP1
        public const int IRQ_CCP2 = 5;  // CCP2
        public const int IRQ_TMR1 = 6;  // Timer 1 Overflow
        public const int IRQ_TMR2 = 8;  // Timer 2 Overflow
        public const int IRQ_SPI = 9;  // SPI/I2C
        public const int IRQ_SCI = 10;  // USART Receive
        public const int IRQ_SCI = 11;  // USART Transmit
        public const int IRQ_ADC = 12;  // A/D Converter
        public const int IRQ_EEPROM = 13;  // EEPROM Write Complete

        // 引脚定义
        public const int PIN_MCLR_VPP = 1;  // Master Clear (Reset)
        public const int PIN_RA0_AN0 = 2;  // PORTA Bit 0 / Analog 0
        public const int PIN_RA1_AN1 = 3;  // PORTA Bit 1 / Analog 1
        public const int PIN_RA2_AN2_VREF = 4;  // PORTA Bit 2 / Analog 2 / VREF-
        public const int PIN_RA3_AN3_VREFP = 5;  // PORTA Bit 3 / Analog 3 / VREF+
        public const int PIN_RA4_T0CKI = 6;  // PORTA Bit 4 / Timer 0 Clock Input
        public const int PIN_RA5_AN4_SS = 7;  // PORTA Bit 4 / Analog 4 / SPI Slave Select
        public const int PIN_RE0_RD_AN5 = 8;  // PORTE Bit 0 / Read Control / Analog 5
        public const int PIN_RE1_WR_AN6 = 9;  // PORTE Bit 1 / Write Control / Analog 6
        public const int PIN_RE2_CS_AN7 = 10;  // PORTE Bit 2 / Chip Select / Analog 7
        public const int PIN_VDD = 11;  // Positive Supply
        public const int PIN_VSS = 12;  // Ground
        public const int PIN_OSC1_CLKIN = 13;  // Oscillator/Clock Input
        public const int PIN_OSC2_CLKOUT = 14;  // Oscillator/Clock Output
        public const int PIN_RC0_T1OSO = 15;  // PORTC Bit 0 / Timer 1 Oscillator
        public const int PIN_RC1_T1OSI = 16;  // PORTC Bit 1 / Timer 1 Oscillator
        public const int PIN_RC2_CCP1 = 17;  // PORTC Bit 2 / Capture/Compare/PWM 1
        public const int PIN_RC3_SCK_SCL = 18;  // PORTC Bit 3 / SPI Clock / I2C Clock
        public const int PIN_RC4_SDI_SDA = 23;  // PORTC Bit 4 / SPI Data In / I2C Data
        public const int PIN_RC5_SDO = 24;  // PORTC Bit 5 / SPI Data Out
        public const int PIN_RC6_TX = 25;  // PORTC Bit 6 / USART Transmit
        public const int PIN_RC7_RX = 26;  // PORTC Bit 7 / USART Receive
        public const int PIN_RD0 = 19;  // PORTD Bit 0
        public const int PIN_RD1 = 20;  // PORTD Bit 1
        public const int PIN_RD2 = 21;  // PORTD Bit 2
        public const int PIN_RD3 = 22;  // PORTD Bit 3
        public const int PIN_RD4 = 27;  // PORTD Bit 4
        public const int PIN_RD5 = 28;  // PORTD Bit 5
        public const int PIN_RD6 = 29;  // PORTD Bit 6
        public const int PIN_RD7 = 30;  // PORTD Bit 7
        public const int PIN_VSS = 31;  // Ground
        public const int PIN_VDD = 32;  // Positive Supply
        public const int PIN_RB0_INT = 33;  // PORTB Bit 0 / External Interrupt
        public const int PIN_RB1 = 34;  // PORTB Bit 1
        public const int PIN_RB2 = 35;  // PORTB Bit 2
        public const int PIN_RB3_PGC = 36;  // PORTB Bit 3 / Programming Clock
        public const int PIN_RB4_PGD = 37;  // PORTB Bit 4 / Programming Data
        public const int PIN_RB5 = 38;  // PORTB Bit 5
        public const int PIN_RB6_PGC = 39;  // PORTB Bit 6 / Programming Clock
        public const int PIN_RB7_PGD = 40;  // PORTB Bit 7 / Programming Data

        public static void pic16f877a_init()
        {
            // 硬件初始化代码
        }
    }
}
