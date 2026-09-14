// PIC18F452 设备定义 - Dart 库
// 生成自: Microchip Technology/PIC18/PIC18F452
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

class PIC18F452Device {
  static const String deviceName = "PIC18F452";
  static const String manufacturer = "Microchip Technology";
  static const String family = "PIC18";
  static const String version = "1.0";
  static const String architecture = "PIC18";
  static const int bits = 8;
  static const int clockFrequency = 20000000;

  // 寄存器地址定义
  static const int WREG_ADDR = 0xFE8;  // Working Register
  static const int STATUS_ADDR = 0xFD8;  // Status Register
  static const int BSR_ADDR = 0xFE0;  // Bank Select Register
  static const int PCL_ADDR = 0xFF9;  // Program Counter Low
  static const int PCLATH_ADDR = 0xFFA;  // Program Counter Latch High
  static const int PCLATU_ADDR = 0xFFB;  // Program Counter Latch Upper
  static const int TOSU_ADDR = 0xFFF;  // Top of Stack Upper
  static const int TOSH_ADDR = 0xFFE;  // Top of Stack High
  static const int TOSL_ADDR = 0xFFD;  // Top of Stack Low

  // 外设定义
  // Port A
  static const int PORTA_BASE = ;
  static const int PORTA_PORTA_ADDR = 0xF80;
  static const int PORTA_TRISA_ADDR = 0xF92;
  static const int PORTA_LATA_ADDR = 0xF89;
  // Port B
  static const int PORTB_BASE = ;
  static const int PORTB_PORTB_ADDR = 0xF81;
  static const int PORTB_TRISB_ADDR = 0xF93;
  static const int PORTB_LATB_ADDR = 0xF8A;
  // Port C
  static const int PORTC_BASE = ;
  static const int PORTC_PORTC_ADDR = 0xF82;
  static const int PORTC_TRISC_ADDR = 0xF94;
  static const int PORTC_LATC_ADDR = 0xF8B;
  // Port D
  static const int PORTD_BASE = ;
  static const int PORTD_PORTD_ADDR = 0xF83;
  static const int PORTD_TRISD_ADDR = 0xF95;
  static const int PORTD_LATD_ADDR = 0xF8C;
  // Port E
  static const int PORTE_BASE = ;
  static const int PORTE_PORTE_ADDR = 0xF84;
  static const int PORTE_TRISE_ADDR = 0xF96;
  static const int PORTE_LATE_ADDR = 0xF8D;
  // Timer0
  static const int TMR0_BASE = ;
  static const int TMR0_TMR0L_ADDR = 0xFD6;
  static const int TMR0_TMR0H_ADDR = 0xFD7;
  static const int TMR0_T0CON_ADDR = 0xFD5;
  // Timer1
  static const int TMR1_BASE = ;
  static const int TMR1_TMR1L_ADDR = 0xFCE;
  static const int TMR1_TMR1H_ADDR = 0xFCF;
  static const int TMR1_T1CON_ADDR = 0xFCD;
  // Timer2
  static const int TMR2_BASE = ;
  static const int TMR2_TMR2_ADDR = 0xFCC;
  static const int TMR2_PR2_ADDR = 0xFCB;
  static const int TMR2_T2CON_ADDR = 0xFCA;
  // Timer3
  static const int TMR3_BASE = ;
  static const int TMR3_TMR3L_ADDR = 0xFB2;
  static const int TMR3_TMR3H_ADDR = 0xFB3;
  static const int TMR3_T3CON_ADDR = 0xFB1;
  // Analog-to-Digital Converter
  static const int ADC_BASE = ;
  static const int ADC_ADRESL_ADDR = 0xFC3;
  static const int ADC_ADRESH_ADDR = 0xFC4;
  static const int ADC_ADCON0_ADDR = 0xFC2;
  static const int ADC_ADCON1_ADDR = 0xFC1;
  // Universal Synchronous Asynchronous Receiver Transmitter
  static const int USART_BASE = ;
  static const int USART_TXREG_ADDR = 0xFAC;
  static const int USART_RCREG_ADDR = 0xFAB;
  static const int USART_SPBRG_ADDR = 0xFAF;
  static const int USART_TXSTA_ADDR = 0xFAD;
  static const int USART_RCSTA_ADDR = 0xFAE;
  // Synchronous Serial Port
  static const int SSP_BASE = ;
  static const int SSP_SSPBUF_ADDR = 0xFC9;
  static const int SSP_SSPADD_ADDR = 0xFC8;
  static const int SSP_SSPSTAT_ADDR = 0xFC7;
  static const int SSP_SSPCON1_ADDR = 0xFC6;
  static const int SSP_SSPCON2_ADDR = 0xFC5;
  // Capture/Compare/PWM 1
  static const int CCP1_BASE = ;
  static const int CCP1_CCPR1L_ADDR = 0xFBE;
  static const int CCP1_CCPR1H_ADDR = 0xFBF;
  static const int CCP1_CCP1CON_ADDR = 0xFBD;
  // Capture/Compare/PWM 2
  static const int CCP2_BASE = ;
  static const int CCP2_CCPR2L_ADDR = 0xFBA;
  static const int CCP2_CCPR2H_ADDR = 0xFBB;
  static const int CCP2_CCP2CON_ADDR = 0xFB9;

  // 中断向量定义
  static const int INT_HIGH_PRIORITY = 8;  // High priority interrupt
  static const int INT_LOW_PRIORITY = 24;  // Low priority interrupt
  static const int INT_RESET = 0;  // Reset vector

}
