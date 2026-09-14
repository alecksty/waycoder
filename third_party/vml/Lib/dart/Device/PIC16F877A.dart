// PIC16F877A 设备定义 - Dart 库
// 生成自: Microchip/PIC/PIC16F877A
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
// CPU架构: PIC16
// 位宽: 8位
// 时钟频率: 4000000 Hz

class PIC16F877ADevice {
  static const String deviceName = "PIC16F877A";
  static const String manufacturer = "Microchip";
  static const String family = "PIC";
  static const String version = "1.0";
  static const String architecture = "PIC16";
  static const int bits = 8;
  static const int clockFrequency = 4000000;

  // 寄存器地址定义
  static const int W_ADDR = 0x00;  // Working Register
  static const int STATUS_ADDR = 0x03;  // Status Register
  static const int STATUS_C_BIT = 0;  // Carry flag
  static const int STATUS_DC_BIT = 1;  // Digit carry flag
  static const int STATUS_Z_BIT = 2;  // Zero flag
  static const int STATUS_PD_BIT = 3;  // Power-down flag
  static const int STATUS_TO_BIT = 4;  // Time-out flag
  static const int STATUS_RP_BIT = 5;  // Register bank select
  static const int STATUS_IRP_BIT = 7;  // Indirect register bank select
  static const int INTCON_ADDR = 0x0B;  // Interrupt Control Register
  static const int INTCON_RBIF_BIT = 0;  // PORTB change interrupt flag
  static const int INTCON_INTF_BIT = 1;  // External interrupt flag
  static const int INTCON_TMR0IF_BIT = 2;  // TMR0 overflow interrupt flag
  static const int INTCON_RBIE_BIT = 3;  // PORTB change interrupt enable
  static const int INTCON_INTE_BIT = 4;  // External interrupt enable
  static const int INTCON_TMR0IE_BIT = 5;  // TMR0 overflow interrupt enable
  static const int INTCON_PEIE_BIT = 6;  // Peripheral interrupt enable
  static const int INTCON_GIE_BIT = 7;  // Global interrupt enable
  static const int PORTB_ADDR = 0x06;  // PORT B
  static const int TRISB_ADDR = 0x86;  // TRIS B
  static const int PORTC_ADDR = 0x07;  // PORT C
  static const int TRISC_ADDR = 0x87;  // TRIS C
  static const int PORTD_ADDR = 0x08;  // PORT D
  static const int TRISD_ADDR = 0x88;  // TRIS D
  static const int PORTE_ADDR = 0x09;  // PORT E
  static const int TRISE_ADDR = 0x89;  // TRIS E
  static const int TMR0_ADDR = 0x01;  // Timer 0
  static const int OPTION_REG_ADDR = 0x81;  // Option Register
  static const int PCL_ADDR = 0x02;  // Program Counter Low
  static const int PCLATH_ADDR = 0x0A;  // Program Counter Latch High
  static const int FSR_ADDR = 0x04;  // File Select Register
  static const int EEDATA_ADDR = 0x10C;  // EEPROM Data
  static const int EEADR_ADDR = 0x10D;  // EEPROM Address
  static const int EECON1_ADDR = 0x18C;  // EEPROM Control 1
  static const int EECON1_RD_BIT = 0;  // Read control
  static const int EECON1_WR_BIT = 1;  // Write control
  static const int EECON1_WREN_BIT = 2;  // Write enable
  static const int EECON1_WRERR_BIT = 3;  // Write error flag
  static const int EECON1_EEPGD_BIT = 7;  // EEPROM program/data select
  static const int EECON2_ADDR = 0x18D;  // EEPROM Control 2
  static const int ADRESH_ADDR = 0x1E;  // A/D Result High
  static const int ADRESL_ADDR = 0x1F;  // A/D Result Low
  static const int ADCON0_ADDR = 0x1F;  // A/D Control 0
  static const int ADCON0_ADON_BIT = 0;  // A/D enable
  static const int ADCON0_GO_DONE_BIT = 2;  // A/D conversion status
  static const int ADCON0_CHS_BIT = 3;  // Channel select
  static const int ADCON1_ADDR = 0x9F;  // A/D Control 1
  static const int SSPSTAT_ADDR = 0x94;  // MSSP Status
  static const int SSPCON_ADDR = 0x14;  // MSSP Control
  static const int SSPBUF_ADDR = 0x13;  // SSP Buffer
  static const int TXREG_ADDR = 0x19;  // USART Transmit Register
  static const int RCREG_ADDR = 0x1A;  // USART Receive Register
  static const int SPBRG_ADDR = 0x99;  // Baud Rate Generator
  static const int TXSTA_ADDR = 0x98;  // TX Status and Control
  static const int RCSTA_ADDR = 0x18;  // RX Status and Control
  static const int CCP1CON_ADDR = 0x17;  // CCP1 Control
  static const int CCPR1L_ADDR = 0x15;  // CCP1 Low
  static const int CCPR1H_ADDR = 0x16;  // CCP1 High
  static const int CCP2CON_ADDR = 0x1D;  // CCP2 Control
  static const int CCPR2L_ADDR = 0x1B;  // CCP2 Low
  static const int CCPR2H_ADDR = 0x1C;  // CCP2 High
  static const int T1CON_ADDR = 0x10;  // Timer 1 Control
  static const int TMR1L_ADDR = 0x0E;  // Timer 1 Low
  static const int TMR1H_ADDR = 0x0F;  // Timer 1 High
  static const int T2CON_ADDR = 0x12;  // Timer 2 Control
  static const int TMR2_ADDR = 0x11;  // Timer 2
  static const int PR2_ADDR = 0x92;  // Timer 2 Period

  // 内存段定义
  static const int PROGRAM_START = 0x0000;
  static const int PROGRAM_END = 0x1FFF;
  static const int PROGRAM_SIZE = 8192;  // Program Memory (8KB)
  static const int DATA_START = 0x20;
  static const int DATA_END = 0x7F;
  static const int DATA_SIZE = 96;  // General Purpose RAM Bank 0
  static const int SRAM_START = 0xA0;
  static const int SRAM_END = 0xFF;
  static const int SRAM_SIZE = 96;  // General Purpose RAM Bank 1
  static const int EEPROM_START = 0x2100;
  static const int EEPROM_END = 0x21FF;
  static const int EEPROM_SIZE = 256;  // EEPROM Data Memory

  // 外设定义
  // Port B
  static const int GPIO_PORTB_BASE = 0x06;
  static const int GPIO_PORTB_PORTB_ADDR = 0x06;
  static const int GPIO_PORTB_TRISB_ADDR = 0x86;
  // Port C
  static const int GPIO_PORTC_BASE = 0x07;
  static const int GPIO_PORTC_PORTC_ADDR = 0x07;
  static const int GPIO_PORTC_TRISC_ADDR = 0x87;
  // Port D
  static const int GPIO_PORTD_BASE = 0x08;
  static const int GPIO_PORTD_PORTD_ADDR = 0x08;
  static const int GPIO_PORTD_TRISD_ADDR = 0x88;
  // Timer 0
  static const int TIMER0_BASE = 0x01;
  static const int TIMER0_TMR0_ADDR = 0x01;
  static const int TIMER0_OPTION_REG_ADDR = 0x81;
  // Timer 1
  static const int TIMER1_BASE = 0x0E;
  static const int TIMER1_T1CON_ADDR = 0x10;
  static const int TIMER1_TMR1L_ADDR = 0x0E;
  static const int TIMER1_TMR1H_ADDR = 0x0F;
  // Timer 2
  static const int TIMER2_BASE = 0x11;
  static const int TIMER2_T2CON_ADDR = 0x12;
  static const int TIMER2_TMR2_ADDR = 0x11;
  static const int TIMER2_PR2_ADDR = 0x92;
  // A/D Converter
  static const int ADC_BASE = 0x1E;
  static const int ADC_ADRESH_ADDR = 0x1E;
  static const int ADC_ADRESL_ADDR = 0x9F;
  static const int ADC_ADCON0_ADDR = 0x1F;
  static const int ADC_ADCON1_ADDR = 0x9F;
  // Master Synchronous Serial Port
  static const int MSSP_BASE = 0x13;
  static const int MSSP_SSPSTAT_ADDR = 0x94;
  static const int MSSP_SSPCON_ADDR = 0x14;
  static const int MSSP_SSPBUF_ADDR = 0x13;
  // USART
  static const int USART_BASE = 0x19;
  static const int USART_TXREG_ADDR = 0x19;
  static const int USART_RCREG_ADDR = 0x1A;
  static const int USART_SPBRG_ADDR = 0x99;
  static const int USART_TXSTA_ADDR = 0x98;
  static const int USART_RCSTA_ADDR = 0x18;
  // Capture/Compare/PWM 1
  static const int CCP1_BASE = 0x15;
  static const int CCP1_CCP1CON_ADDR = 0x17;
  static const int CCP1_CCPR1L_ADDR = 0x15;
  static const int CCP1_CCPR1H_ADDR = 0x16;
  // Capture/Compare/PWM 2
  static const int CCP2_BASE = 0x1B;
  static const int CCP2_CCP2CON_ADDR = 0x1D;
  static const int CCP2_CCPR2L_ADDR = 0x1B;
  static const int CCP2_CCPR2H_ADDR = 0x1C;

  // 中断向量定义
  static const int INT_INT = 1;  // External Interrupt
  static const int INT_TMR0 = 2;  // Timer 0 Overflow
  static const int INT_RB = 3;  // PORTB Change
  static const int INT_CCP1 = 4;  // CCP1
  static const int INT_CCP2 = 5;  // CCP2
  static const int INT_TMR1 = 6;  // Timer 1 Overflow
  static const int INT_TMR2 = 8;  // Timer 2 Overflow
  static const int INT_SPI = 9;  // SPI/I2C
  static const int INT_SCI = 10;  // USART Receive
  static const int INT_SCI = 11;  // USART Transmit
  static const int INT_ADC = 12;  // A/D Converter
  static const int INT_EEPROM = 13;  // EEPROM Write Complete

  // 引脚定义
  static const int PIN_MCLR_VPP = 1;  // Master Clear (Reset)
  static const int PIN_RA0_AN0 = 2;  // PORTA Bit 0 / Analog 0
  static const int PIN_RA1_AN1 = 3;  // PORTA Bit 1 / Analog 1
  static const int PIN_RA2_AN2_VREF = 4;  // PORTA Bit 2 / Analog 2 / VREF-
  static const int PIN_RA3_AN3_VREFP = 5;  // PORTA Bit 3 / Analog 3 / VREF+
  static const int PIN_RA4_T0CKI = 6;  // PORTA Bit 4 / Timer 0 Clock Input
  static const int PIN_RA5_AN4_SS = 7;  // PORTA Bit 4 / Analog 4 / SPI Slave Select
  static const int PIN_RE0_RD_AN5 = 8;  // PORTE Bit 0 / Read Control / Analog 5
  static const int PIN_RE1_WR_AN6 = 9;  // PORTE Bit 1 / Write Control / Analog 6
  static const int PIN_RE2_CS_AN7 = 10;  // PORTE Bit 2 / Chip Select / Analog 7
  static const int PIN_VDD = 11;  // Positive Supply
  static const int PIN_VSS = 12;  // Ground
  static const int PIN_OSC1_CLKIN = 13;  // Oscillator/Clock Input
  static const int PIN_OSC2_CLKOUT = 14;  // Oscillator/Clock Output
  static const int PIN_RC0_T1OSO = 15;  // PORTC Bit 0 / Timer 1 Oscillator
  static const int PIN_RC1_T1OSI = 16;  // PORTC Bit 1 / Timer 1 Oscillator
  static const int PIN_RC2_CCP1 = 17;  // PORTC Bit 2 / Capture/Compare/PWM 1
  static const int PIN_RC3_SCK_SCL = 18;  // PORTC Bit 3 / SPI Clock / I2C Clock
  static const int PIN_RC4_SDI_SDA = 23;  // PORTC Bit 4 / SPI Data In / I2C Data
  static const int PIN_RC5_SDO = 24;  // PORTC Bit 5 / SPI Data Out
  static const int PIN_RC6_TX = 25;  // PORTC Bit 6 / USART Transmit
  static const int PIN_RC7_RX = 26;  // PORTC Bit 7 / USART Receive
  static const int PIN_RD0 = 19;  // PORTD Bit 0
  static const int PIN_RD1 = 20;  // PORTD Bit 1
  static const int PIN_RD2 = 21;  // PORTD Bit 2
  static const int PIN_RD3 = 22;  // PORTD Bit 3
  static const int PIN_RD4 = 27;  // PORTD Bit 4
  static const int PIN_RD5 = 28;  // PORTD Bit 5
  static const int PIN_RD6 = 29;  // PORTD Bit 6
  static const int PIN_RD7 = 30;  // PORTD Bit 7
  static const int PIN_VSS = 31;  // Ground
  static const int PIN_VDD = 32;  // Positive Supply
  static const int PIN_RB0_INT = 33;  // PORTB Bit 0 / External Interrupt
  static const int PIN_RB1 = 34;  // PORTB Bit 1
  static const int PIN_RB2 = 35;  // PORTB Bit 2
  static const int PIN_RB3_PGC = 36;  // PORTB Bit 3 / Programming Clock
  static const int PIN_RB4_PGD = 37;  // PORTB Bit 4 / Programming Data
  static const int PIN_RB5 = 38;  // PORTB Bit 5
  static const int PIN_RB6_PGC = 39;  // PORTB Bit 6 / Programming Clock
  static const int PIN_RB7_PGD = 40;  // PORTB Bit 7 / Programming Data

}
