! PIC18F452 设备定义 - Fortran 模块
! 生成自: Microchip Technology/PIC18/PIC18F452
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
! CPU架构: PIC18
! 位宽: 8位
! 时钟频率: 20000000 Hz

module pic18f452_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: WREG_ADDR = 0xFE8  ! Working Register
  integer, parameter :: STATUS_ADDR = 0xFD8  ! Status Register
  integer, parameter :: BSR_ADDR = 0xFE0  ! Bank Select Register
  integer, parameter :: PCL_ADDR = 0xFF9  ! Program Counter Low
  integer, parameter :: PCLATH_ADDR = 0xFFA  ! Program Counter Latch High
  integer, parameter :: PCLATU_ADDR = 0xFFB  ! Program Counter Latch Upper
  integer, parameter :: TOSU_ADDR = 0xFFF  ! Top of Stack Upper
  integer, parameter :: TOSH_ADDR = 0xFFE  ! Top of Stack High
  integer, parameter :: TOSL_ADDR = 0xFFD  ! Top of Stack Low

  ! 外设定义
  ! Port A
  integer, parameter :: PORTA_BASE = 
  integer, parameter :: PORTA_PORTA_ADDR = 0xF80
  integer, parameter :: PORTA_TRISA_ADDR = 0xF92
  integer, parameter :: PORTA_LATA_ADDR = 0xF89
  ! Port B
  integer, parameter :: PORTB_BASE = 
  integer, parameter :: PORTB_PORTB_ADDR = 0xF81
  integer, parameter :: PORTB_TRISB_ADDR = 0xF93
  integer, parameter :: PORTB_LATB_ADDR = 0xF8A
  ! Port C
  integer, parameter :: PORTC_BASE = 
  integer, parameter :: PORTC_PORTC_ADDR = 0xF82
  integer, parameter :: PORTC_TRISC_ADDR = 0xF94
  integer, parameter :: PORTC_LATC_ADDR = 0xF8B
  ! Port D
  integer, parameter :: PORTD_BASE = 
  integer, parameter :: PORTD_PORTD_ADDR = 0xF83
  integer, parameter :: PORTD_TRISD_ADDR = 0xF95
  integer, parameter :: PORTD_LATD_ADDR = 0xF8C
  ! Port E
  integer, parameter :: PORTE_BASE = 
  integer, parameter :: PORTE_PORTE_ADDR = 0xF84
  integer, parameter :: PORTE_TRISE_ADDR = 0xF96
  integer, parameter :: PORTE_LATE_ADDR = 0xF8D
  ! Timer0
  integer, parameter :: TMR0_BASE = 
  integer, parameter :: TMR0_TMR0L_ADDR = 0xFD6
  integer, parameter :: TMR0_TMR0H_ADDR = 0xFD7
  integer, parameter :: TMR0_T0CON_ADDR = 0xFD5
  ! Timer1
  integer, parameter :: TMR1_BASE = 
  integer, parameter :: TMR1_TMR1L_ADDR = 0xFCE
  integer, parameter :: TMR1_TMR1H_ADDR = 0xFCF
  integer, parameter :: TMR1_T1CON_ADDR = 0xFCD
  ! Timer2
  integer, parameter :: TMR2_BASE = 
  integer, parameter :: TMR2_TMR2_ADDR = 0xFCC
  integer, parameter :: TMR2_PR2_ADDR = 0xFCB
  integer, parameter :: TMR2_T2CON_ADDR = 0xFCA
  ! Timer3
  integer, parameter :: TMR3_BASE = 
  integer, parameter :: TMR3_TMR3L_ADDR = 0xFB2
  integer, parameter :: TMR3_TMR3H_ADDR = 0xFB3
  integer, parameter :: TMR3_T3CON_ADDR = 0xFB1
  ! Analog-to-Digital Converter
  integer, parameter :: ADC_BASE = 
  integer, parameter :: ADC_ADRESL_ADDR = 0xFC3
  integer, parameter :: ADC_ADRESH_ADDR = 0xFC4
  integer, parameter :: ADC_ADCON0_ADDR = 0xFC2
  integer, parameter :: ADC_ADCON1_ADDR = 0xFC1
  ! Universal Synchronous Asynchronous Receiver Transmitter
  integer, parameter :: USART_BASE = 
  integer, parameter :: USART_TXREG_ADDR = 0xFAC
  integer, parameter :: USART_RCREG_ADDR = 0xFAB
  integer, parameter :: USART_SPBRG_ADDR = 0xFAF
  integer, parameter :: USART_TXSTA_ADDR = 0xFAD
  integer, parameter :: USART_RCSTA_ADDR = 0xFAE
  ! Synchronous Serial Port
  integer, parameter :: SSP_BASE = 
  integer, parameter :: SSP_SSPBUF_ADDR = 0xFC9
  integer, parameter :: SSP_SSPADD_ADDR = 0xFC8
  integer, parameter :: SSP_SSPSTAT_ADDR = 0xFC7
  integer, parameter :: SSP_SSPCON1_ADDR = 0xFC6
  integer, parameter :: SSP_SSPCON2_ADDR = 0xFC5
  ! Capture/Compare/PWM 1
  integer, parameter :: CCP1_BASE = 
  integer, parameter :: CCP1_CCPR1L_ADDR = 0xFBE
  integer, parameter :: CCP1_CCPR1H_ADDR = 0xFBF
  integer, parameter :: CCP1_CCP1CON_ADDR = 0xFBD
  ! Capture/Compare/PWM 2
  integer, parameter :: CCP2_BASE = 
  integer, parameter :: CCP2_CCPR2L_ADDR = 0xFBA
  integer, parameter :: CCP2_CCPR2H_ADDR = 0xFBB
  integer, parameter :: CCP2_CCP2CON_ADDR = 0xFB9

  ! 中断向量定义
  integer, parameter :: INT_HIGH_PRIORITY = 8  ! High priority interrupt
  integer, parameter :: INT_LOW_PRIORITY = 24  ! Low priority interrupt
  integer, parameter :: INT_RESET = 0  ! Reset vector

end module pic18f452_device
