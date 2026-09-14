/**
 * PIC18F452 寄存器定义
 * 生成自: Microchip Technology/PIC18/PIC18F452
 * 版本: 1.0
 */
export const pic18f452 = {
  // CPU: PIC18, 8位, 20000000 Hz

  // 寄存器定义
  // Working Register
  WREG: 0xFE8,
  // Status Register
  STATUS: 0xFD8,
  // Bank Select Register
  BSR: 0xFE0,
  // Program Counter Low
  PCL: 0xFF9,
  // Program Counter Latch High
  PCLATH: 0xFFA,
  // Program Counter Latch Upper
  PCLATU: 0xFFB,
  // Top of Stack Upper
  TOSU: 0xFFF,
  // Top of Stack High
  TOSH: 0xFFE,
  // Top of Stack Low
  TOSL: 0xFFD,

  // 外设定义
  // Port A
  PORTA_BASE: ,
  PORTA_PORTA: 0x00000F80,
  PORTA_TRISA: 0x00000F92,
  PORTA_LATA: 0x00000F89,
  // Port B
  PORTB_BASE: ,
  PORTB_PORTB: 0x00000F81,
  PORTB_TRISB: 0x00000F93,
  PORTB_LATB: 0x00000F8A,
  // Port C
  PORTC_BASE: ,
  PORTC_PORTC: 0x00000F82,
  PORTC_TRISC: 0x00000F94,
  PORTC_LATC: 0x00000F8B,
  // Port D
  PORTD_BASE: ,
  PORTD_PORTD: 0x00000F83,
  PORTD_TRISD: 0x00000F95,
  PORTD_LATD: 0x00000F8C,
  // Port E
  PORTE_BASE: ,
  PORTE_PORTE: 0x00000F84,
  PORTE_TRISE: 0x00000F96,
  PORTE_LATE: 0x00000F8D,
  // Timer0
  TMR0_BASE: ,
  TMR0_TMR0L: 0x00000FD6,
  TMR0_TMR0H: 0x00000FD7,
  TMR0_T0CON: 0x00000FD5,
  // Timer1
  TMR1_BASE: ,
  TMR1_TMR1L: 0x00000FCE,
  TMR1_TMR1H: 0x00000FCF,
  TMR1_T1CON: 0x00000FCD,
  // Timer2
  TMR2_BASE: ,
  TMR2_TMR2: 0x00000FCC,
  TMR2_PR2: 0x00000FCB,
  TMR2_T2CON: 0x00000FCA,
  // Timer3
  TMR3_BASE: ,
  TMR3_TMR3L: 0x00000FB2,
  TMR3_TMR3H: 0x00000FB3,
  TMR3_T3CON: 0x00000FB1,
  // Analog-to-Digital Converter
  ADC_BASE: ,
  ADC_ADRESL: 0x00000FC3,
  ADC_ADRESH: 0x00000FC4,
  ADC_ADCON0: 0x00000FC2,
  ADC_ADCON1: 0x00000FC1,
  // Universal Synchronous Asynchronous Receiver Transmitter
  USART_BASE: ,
  USART_TXREG: 0x00000FAC,
  USART_RCREG: 0x00000FAB,
  USART_SPBRG: 0x00000FAF,
  USART_TXSTA: 0x00000FAD,
  USART_RCSTA: 0x00000FAE,
  // Synchronous Serial Port
  SSP_BASE: ,
  SSP_SSPBUF: 0x00000FC9,
  SSP_SSPADD: 0x00000FC8,
  SSP_SSPSTAT: 0x00000FC7,
  SSP_SSPCON1: 0x00000FC6,
  SSP_SSPCON2: 0x00000FC5,
  // Capture/Compare/PWM 1
  CCP1_BASE: ,
  CCP1_CCPR1L: 0x00000FBE,
  CCP1_CCPR1H: 0x00000FBF,
  CCP1_CCP1CON: 0x00000FBD,
  // Capture/Compare/PWM 2
  CCP2_BASE: ,
  CCP2_CCPR2L: 0x00000FBA,
  CCP2_CCPR2H: 0x00000FBB,
  CCP2_CCP2CON: 0x00000FB9,

  // 中断向量
  IRQ_HIGH_PRIORITY: 8,  // High priority interrupt
  IRQ_LOW_PRIORITY: 24,  // Low priority interrupt
  IRQ_RESET: 0,  // Reset vector

  init: function() {
    // 硬件初始化
  }
};
