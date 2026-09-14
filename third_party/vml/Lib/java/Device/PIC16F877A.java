package vml.device.microchip.pic16f877a;

/**
 * PIC16F877A 寄存器定义
 * 生成自: Microchip/PIC/PIC16F877A
 * 版本: 1.0
 */
public final class PIC16F877A {
    private PIC16F877A() {} // 工具类
    // CPU架构: PIC16, 8位, 4000000 Hz

    // 寄存器定义
    // Working Register
    public static final int W_ADDR = (int)0x00;

    // Status Register
    public static final int STATUS_ADDR = (int)0x03;
    public static final int STATUS_C = 0;  // Carry flag
    public static final int STATUS_DC = 1;  // Digit carry flag
    public static final int STATUS_Z = 2;  // Zero flag
    public static final int STATUS_PD = 3;  // Power-down flag
    public static final int STATUS_TO = 4;  // Time-out flag
    public static final int STATUS_RP = 5;  // Register bank select
    public static final int STATUS_IRP = 7;  // Indirect register bank select

    // Interrupt Control Register
    public static final int INTCON_ADDR = (int)0x0B;
    public static final int INTCON_RBIF = 0;  // PORTB change interrupt flag
    public static final int INTCON_INTF = 1;  // External interrupt flag
    public static final int INTCON_TMR0IF = 2;  // TMR0 overflow interrupt flag
    public static final int INTCON_RBIE = 3;  // PORTB change interrupt enable
    public static final int INTCON_INTE = 4;  // External interrupt enable
    public static final int INTCON_TMR0IE = 5;  // TMR0 overflow interrupt enable
    public static final int INTCON_PEIE = 6;  // Peripheral interrupt enable
    public static final int INTCON_GIE = 7;  // Global interrupt enable

    // PORT B
    public static final int PORTB_ADDR = (int)0x06;

    // TRIS B
    public static final int TRISB_ADDR = (int)0x86;

    // PORT C
    public static final int PORTC_ADDR = (int)0x07;

    // TRIS C
    public static final int TRISC_ADDR = (int)0x87;

    // PORT D
    public static final int PORTD_ADDR = (int)0x08;

    // TRIS D
    public static final int TRISD_ADDR = (int)0x88;

    // PORT E
    public static final int PORTE_ADDR = (int)0x09;

    // TRIS E
    public static final int TRISE_ADDR = (int)0x89;

    // Timer 0
    public static final int TMR0_ADDR = (int)0x01;

    // Option Register
    public static final int OPTION_REG_ADDR = (int)0x81;

    // Program Counter Low
    public static final int PCL_ADDR = (int)0x02;

    // Program Counter Latch High
    public static final int PCLATH_ADDR = (int)0x0A;

    // File Select Register
    public static final int FSR_ADDR = (int)0x04;

    // EEPROM Data
    public static final int EEDATA_ADDR = (int)0x10C;

    // EEPROM Address
    public static final int EEADR_ADDR = (int)0x10D;

    // EEPROM Control 1
    public static final int EECON1_ADDR = (int)0x18C;
    public static final int EECON1_RD = 0;  // Read control
    public static final int EECON1_WR = 1;  // Write control
    public static final int EECON1_WREN = 2;  // Write enable
    public static final int EECON1_WRERR = 3;  // Write error flag
    public static final int EECON1_EEPGD = 7;  // EEPROM program/data select

    // EEPROM Control 2
    public static final int EECON2_ADDR = (int)0x18D;

    // A/D Result High
    public static final int ADRESH_ADDR = (int)0x1E;

    // A/D Result Low
    public static final int ADRESL_ADDR = (int)0x1F;

    // A/D Control 0
    public static final int ADCON0_ADDR = (int)0x1F;
    public static final int ADCON0_ADON = 0;  // A/D enable
    public static final int ADCON0_GO_DONE = 2;  // A/D conversion status
    public static final int ADCON0_CHS = 3;  // Channel select

    // A/D Control 1
    public static final int ADCON1_ADDR = (int)0x9F;

    // MSSP Status
    public static final int SSPSTAT_ADDR = (int)0x94;

    // MSSP Control
    public static final int SSPCON_ADDR = (int)0x14;

    // SSP Buffer
    public static final int SSPBUF_ADDR = (int)0x13;

    // USART Transmit Register
    public static final int TXREG_ADDR = (int)0x19;

    // USART Receive Register
    public static final int RCREG_ADDR = (int)0x1A;

    // Baud Rate Generator
    public static final int SPBRG_ADDR = (int)0x99;

    // TX Status and Control
    public static final int TXSTA_ADDR = (int)0x98;

    // RX Status and Control
    public static final int RCSTA_ADDR = (int)0x18;

    // CCP1 Control
    public static final int CCP1CON_ADDR = (int)0x17;

    // CCP1 Low
    public static final int CCPR1L_ADDR = (int)0x15;

    // CCP1 High
    public static final int CCPR1H_ADDR = (int)0x16;

    // CCP2 Control
    public static final int CCP2CON_ADDR = (int)0x1D;

    // CCP2 Low
    public static final int CCPR2L_ADDR = (int)0x1B;

    // CCP2 High
    public static final int CCPR2H_ADDR = (int)0x1C;

    // Timer 1 Control
    public static final int T1CON_ADDR = (int)0x10;

    // Timer 1 Low
    public static final int TMR1L_ADDR = (int)0x0E;

    // Timer 1 High
    public static final int TMR1H_ADDR = (int)0x0F;

    // Timer 2 Control
    public static final int T2CON_ADDR = (int)0x12;

    // Timer 2
    public static final int TMR2_ADDR = (int)0x11;

    // Timer 2 Period
    public static final int PR2_ADDR = (int)0x92;

    // 内存段定义
    // Program Memory (8KB)
    public static final int PROGRAM_START = (int)0x0000;
    public static final int PROGRAM_END = (int)0x1FFF;
    public static final int PROGRAM_SIZE = 8192;

    // General Purpose RAM Bank 0
    public static final int DATA_START = (int)0x20;
    public static final int DATA_END = (int)0x7F;
    public static final int DATA_SIZE = 96;

    // General Purpose RAM Bank 1
    public static final int SRAM_START = (int)0xA0;
    public static final int SRAM_END = (int)0xFF;
    public static final int SRAM_SIZE = 96;

    // EEPROM Data Memory
    public static final int EEPROM_START = (int)0x2100;
    public static final int EEPROM_END = (int)0x21FF;
    public static final int EEPROM_SIZE = 256;

    // 外设定义
    // Port B
    public static final int GPIO_PORTB_BASE = (int)0x06;
    public static final int GPIO_PORTB_PORTB = (int)0x0000000C;
    public static final int GPIO_PORTB_TRISB = (int)0x0000008C;

    // Port C
    public static final int GPIO_PORTC_BASE = (int)0x07;
    public static final int GPIO_PORTC_PORTC = (int)0x0000000E;
    public static final int GPIO_PORTC_TRISC = (int)0x0000008E;

    // Port D
    public static final int GPIO_PORTD_BASE = (int)0x08;
    public static final int GPIO_PORTD_PORTD = (int)0x00000010;
    public static final int GPIO_PORTD_TRISD = (int)0x00000090;

    // Timer 0
    public static final int TIMER0_BASE = (int)0x01;
    public static final int TIMER0_TMR0 = (int)0x00000002;
    public static final int TIMER0_OPTION_REG = (int)0x00000082;

    // Timer 1
    public static final int TIMER1_BASE = (int)0x0E;
    public static final int TIMER1_T1CON = (int)0x0000001E;
    public static final int TIMER1_TMR1L = (int)0x0000001C;
    public static final int TIMER1_TMR1H = (int)0x0000001D;

    // Timer 2
    public static final int TIMER2_BASE = (int)0x11;
    public static final int TIMER2_T2CON = (int)0x00000023;
    public static final int TIMER2_TMR2 = (int)0x00000022;
    public static final int TIMER2_PR2 = (int)0x000000A3;

    // A/D Converter
    public static final int ADC_BASE = (int)0x1E;
    public static final int ADC_ADRESH = (int)0x0000003C;
    public static final int ADC_ADRESL = (int)0x000000BD;
    public static final int ADC_ADCON0 = (int)0x0000003D;
    public static final int ADC_ADCON1 = (int)0x000000BD;

    // Master Synchronous Serial Port
    public static final int MSSP_BASE = (int)0x13;
    public static final int MSSP_SSPSTAT = (int)0x000000A7;
    public static final int MSSP_SSPCON = (int)0x00000027;
    public static final int MSSP_SSPBUF = (int)0x00000026;

    // USART
    public static final int USART_BASE = (int)0x19;
    public static final int USART_TXREG = (int)0x00000032;
    public static final int USART_RCREG = (int)0x00000033;
    public static final int USART_SPBRG = (int)0x000000B2;
    public static final int USART_TXSTA = (int)0x000000B1;
    public static final int USART_RCSTA = (int)0x00000031;

    // Capture/Compare/PWM 1
    public static final int CCP1_BASE = (int)0x15;
    public static final int CCP1_CCP1CON = (int)0x0000002C;
    public static final int CCP1_CCPR1L = (int)0x0000002A;
    public static final int CCP1_CCPR1H = (int)0x0000002B;

    // Capture/Compare/PWM 2
    public static final int CCP2_BASE = (int)0x1B;
    public static final int CCP2_CCP2CON = (int)0x00000038;
    public static final int CCP2_CCPR2L = (int)0x00000036;
    public static final int CCP2_CCPR2H = (int)0x00000037;

    // 中断向量定义
    public static final int IRQ_INT = 1;  // External Interrupt
    public static final int IRQ_TMR0 = 2;  // Timer 0 Overflow
    public static final int IRQ_RB = 3;  // PORTB Change
    public static final int IRQ_CCP1 = 4;  // CCP1
    public static final int IRQ_CCP2 = 5;  // CCP2
    public static final int IRQ_TMR1 = 6;  // Timer 1 Overflow
    public static final int IRQ_TMR2 = 8;  // Timer 2 Overflow
    public static final int IRQ_SPI = 9;  // SPI/I2C
    public static final int IRQ_SCI = 10;  // USART Receive
    public static final int IRQ_SCI = 11;  // USART Transmit
    public static final int IRQ_ADC = 12;  // A/D Converter
    public static final int IRQ_EEPROM = 13;  // EEPROM Write Complete

    // 引脚定义
    public static final int PIN_MCLR_VPP = 1;  // Master Clear (Reset)
    public static final int PIN_RA0_AN0 = 2;  // PORTA Bit 0 / Analog 0
    public static final int PIN_RA1_AN1 = 3;  // PORTA Bit 1 / Analog 1
    public static final int PIN_RA2_AN2_VREF = 4;  // PORTA Bit 2 / Analog 2 / VREF-
    public static final int PIN_RA3_AN3_VREFP = 5;  // PORTA Bit 3 / Analog 3 / VREF+
    public static final int PIN_RA4_T0CKI = 6;  // PORTA Bit 4 / Timer 0 Clock Input
    public static final int PIN_RA5_AN4_SS = 7;  // PORTA Bit 4 / Analog 4 / SPI Slave Select
    public static final int PIN_RE0_RD_AN5 = 8;  // PORTE Bit 0 / Read Control / Analog 5
    public static final int PIN_RE1_WR_AN6 = 9;  // PORTE Bit 1 / Write Control / Analog 6
    public static final int PIN_RE2_CS_AN7 = 10;  // PORTE Bit 2 / Chip Select / Analog 7
    public static final int PIN_VDD = 11;  // Positive Supply
    public static final int PIN_VSS = 12;  // Ground
    public static final int PIN_OSC1_CLKIN = 13;  // Oscillator/Clock Input
    public static final int PIN_OSC2_CLKOUT = 14;  // Oscillator/Clock Output
    public static final int PIN_RC0_T1OSO = 15;  // PORTC Bit 0 / Timer 1 Oscillator
    public static final int PIN_RC1_T1OSI = 16;  // PORTC Bit 1 / Timer 1 Oscillator
    public static final int PIN_RC2_CCP1 = 17;  // PORTC Bit 2 / Capture/Compare/PWM 1
    public static final int PIN_RC3_SCK_SCL = 18;  // PORTC Bit 3 / SPI Clock / I2C Clock
    public static final int PIN_RC4_SDI_SDA = 23;  // PORTC Bit 4 / SPI Data In / I2C Data
    public static final int PIN_RC5_SDO = 24;  // PORTC Bit 5 / SPI Data Out
    public static final int PIN_RC6_TX = 25;  // PORTC Bit 6 / USART Transmit
    public static final int PIN_RC7_RX = 26;  // PORTC Bit 7 / USART Receive
    public static final int PIN_RD0 = 19;  // PORTD Bit 0
    public static final int PIN_RD1 = 20;  // PORTD Bit 1
    public static final int PIN_RD2 = 21;  // PORTD Bit 2
    public static final int PIN_RD3 = 22;  // PORTD Bit 3
    public static final int PIN_RD4 = 27;  // PORTD Bit 4
    public static final int PIN_RD5 = 28;  // PORTD Bit 5
    public static final int PIN_RD6 = 29;  // PORTD Bit 6
    public static final int PIN_RD7 = 30;  // PORTD Bit 7
    public static final int PIN_VSS = 31;  // Ground
    public static final int PIN_VDD = 32;  // Positive Supply
    public static final int PIN_RB0_INT = 33;  // PORTB Bit 0 / External Interrupt
    public static final int PIN_RB1 = 34;  // PORTB Bit 1
    public static final int PIN_RB2 = 35;  // PORTB Bit 2
    public static final int PIN_RB3_PGC = 36;  // PORTB Bit 3 / Programming Clock
    public static final int PIN_RB4_PGD = 37;  // PORTB Bit 4 / Programming Data
    public static final int PIN_RB5 = 38;  // PORTB Bit 5
    public static final int PIN_RB6_PGC = 39;  // PORTB Bit 6 / Programming Clock
    public static final int PIN_RB7_PGD = 40;  // PORTB Bit 7 / Programming Data

    public static native void pic16f877a_init();
}
