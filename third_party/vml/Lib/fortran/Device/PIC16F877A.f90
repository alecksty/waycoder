! PIC16F877A 设备定义 - Fortran 模块
! 生成自: Microchip/PIC/PIC16F877A
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
! CPU架构: PIC16
! 位宽: 8位
! 时钟频率: 4000000 Hz

module pic16f877a_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: W_ADDR = 0x00  ! Working Register
  integer, parameter :: STATUS_ADDR = 0x03  ! Status Register
  integer, parameter :: STATUS_C_BIT = 0  ! Carry flag
  integer, parameter :: STATUS_DC_BIT = 1  ! Digit carry flag
  integer, parameter :: STATUS_Z_BIT = 2  ! Zero flag
  integer, parameter :: STATUS_PD_BIT = 3  ! Power-down flag
  integer, parameter :: STATUS_TO_BIT = 4  ! Time-out flag
  integer, parameter :: STATUS_RP_BIT = 5  ! Register bank select
  integer, parameter :: STATUS_IRP_BIT = 7  ! Indirect register bank select
  integer, parameter :: INTCON_ADDR = 0x0B  ! Interrupt Control Register
  integer, parameter :: INTCON_RBIF_BIT = 0  ! PORTB change interrupt flag
  integer, parameter :: INTCON_INTF_BIT = 1  ! External interrupt flag
  integer, parameter :: INTCON_TMR0IF_BIT = 2  ! TMR0 overflow interrupt flag
  integer, parameter :: INTCON_RBIE_BIT = 3  ! PORTB change interrupt enable
  integer, parameter :: INTCON_INTE_BIT = 4  ! External interrupt enable
  integer, parameter :: INTCON_TMR0IE_BIT = 5  ! TMR0 overflow interrupt enable
  integer, parameter :: INTCON_PEIE_BIT = 6  ! Peripheral interrupt enable
  integer, parameter :: INTCON_GIE_BIT = 7  ! Global interrupt enable
  integer, parameter :: PORTB_ADDR = 0x06  ! PORT B
  integer, parameter :: TRISB_ADDR = 0x86  ! TRIS B
  integer, parameter :: PORTC_ADDR = 0x07  ! PORT C
  integer, parameter :: TRISC_ADDR = 0x87  ! TRIS C
  integer, parameter :: PORTD_ADDR = 0x08  ! PORT D
  integer, parameter :: TRISD_ADDR = 0x88  ! TRIS D
  integer, parameter :: PORTE_ADDR = 0x09  ! PORT E
  integer, parameter :: TRISE_ADDR = 0x89  ! TRIS E
  integer, parameter :: TMR0_ADDR = 0x01  ! Timer 0
  integer, parameter :: OPTION_REG_ADDR = 0x81  ! Option Register
  integer, parameter :: PCL_ADDR = 0x02  ! Program Counter Low
  integer, parameter :: PCLATH_ADDR = 0x0A  ! Program Counter Latch High
  integer, parameter :: FSR_ADDR = 0x04  ! File Select Register
  integer, parameter :: EEDATA_ADDR = 0x10C  ! EEPROM Data
  integer, parameter :: EEADR_ADDR = 0x10D  ! EEPROM Address
  integer, parameter :: EECON1_ADDR = 0x18C  ! EEPROM Control 1
  integer, parameter :: EECON1_RD_BIT = 0  ! Read control
  integer, parameter :: EECON1_WR_BIT = 1  ! Write control
  integer, parameter :: EECON1_WREN_BIT = 2  ! Write enable
  integer, parameter :: EECON1_WRERR_BIT = 3  ! Write error flag
  integer, parameter :: EECON1_EEPGD_BIT = 7  ! EEPROM program/data select
  integer, parameter :: EECON2_ADDR = 0x18D  ! EEPROM Control 2
  integer, parameter :: ADRESH_ADDR = 0x1E  ! A/D Result High
  integer, parameter :: ADRESL_ADDR = 0x1F  ! A/D Result Low
  integer, parameter :: ADCON0_ADDR = 0x1F  ! A/D Control 0
  integer, parameter :: ADCON0_ADON_BIT = 0  ! A/D enable
  integer, parameter :: ADCON0_GO_DONE_BIT = 2  ! A/D conversion status
  integer, parameter :: ADCON0_CHS_BIT = 3  ! Channel select
  integer, parameter :: ADCON1_ADDR = 0x9F  ! A/D Control 1
  integer, parameter :: SSPSTAT_ADDR = 0x94  ! MSSP Status
  integer, parameter :: SSPCON_ADDR = 0x14  ! MSSP Control
  integer, parameter :: SSPBUF_ADDR = 0x13  ! SSP Buffer
  integer, parameter :: TXREG_ADDR = 0x19  ! USART Transmit Register
  integer, parameter :: RCREG_ADDR = 0x1A  ! USART Receive Register
  integer, parameter :: SPBRG_ADDR = 0x99  ! Baud Rate Generator
  integer, parameter :: TXSTA_ADDR = 0x98  ! TX Status and Control
  integer, parameter :: RCSTA_ADDR = 0x18  ! RX Status and Control
  integer, parameter :: CCP1CON_ADDR = 0x17  ! CCP1 Control
  integer, parameter :: CCPR1L_ADDR = 0x15  ! CCP1 Low
  integer, parameter :: CCPR1H_ADDR = 0x16  ! CCP1 High
  integer, parameter :: CCP2CON_ADDR = 0x1D  ! CCP2 Control
  integer, parameter :: CCPR2L_ADDR = 0x1B  ! CCP2 Low
  integer, parameter :: CCPR2H_ADDR = 0x1C  ! CCP2 High
  integer, parameter :: T1CON_ADDR = 0x10  ! Timer 1 Control
  integer, parameter :: TMR1L_ADDR = 0x0E  ! Timer 1 Low
  integer, parameter :: TMR1H_ADDR = 0x0F  ! Timer 1 High
  integer, parameter :: T2CON_ADDR = 0x12  ! Timer 2 Control
  integer, parameter :: TMR2_ADDR = 0x11  ! Timer 2
  integer, parameter :: PR2_ADDR = 0x92  ! Timer 2 Period

  ! 内存段定义
  integer, parameter :: PROGRAM_START = 0x0000
  integer, parameter :: PROGRAM_END = 0x1FFF
  integer, parameter :: PROGRAM_SIZE = 8192  ! Program Memory (8KB)
  integer, parameter :: DATA_START = 0x20
  integer, parameter :: DATA_END = 0x7F
  integer, parameter :: DATA_SIZE = 96  ! General Purpose RAM Bank 0
  integer, parameter :: SRAM_START = 0xA0
  integer, parameter :: SRAM_END = 0xFF
  integer, parameter :: SRAM_SIZE = 96  ! General Purpose RAM Bank 1
  integer, parameter :: EEPROM_START = 0x2100
  integer, parameter :: EEPROM_END = 0x21FF
  integer, parameter :: EEPROM_SIZE = 256  ! EEPROM Data Memory

  ! 外设定义
  ! Port B
  integer, parameter :: GPIO_PORTB_BASE = 0x06
  integer, parameter :: GPIO_PORTB_PORTB_ADDR = 0x06
  integer, parameter :: GPIO_PORTB_TRISB_ADDR = 0x86
  ! Port C
  integer, parameter :: GPIO_PORTC_BASE = 0x07
  integer, parameter :: GPIO_PORTC_PORTC_ADDR = 0x07
  integer, parameter :: GPIO_PORTC_TRISC_ADDR = 0x87
  ! Port D
  integer, parameter :: GPIO_PORTD_BASE = 0x08
  integer, parameter :: GPIO_PORTD_PORTD_ADDR = 0x08
  integer, parameter :: GPIO_PORTD_TRISD_ADDR = 0x88
  ! Timer 0
  integer, parameter :: TIMER0_BASE = 0x01
  integer, parameter :: TIMER0_TMR0_ADDR = 0x01
  integer, parameter :: TIMER0_OPTION_REG_ADDR = 0x81
  ! Timer 1
  integer, parameter :: TIMER1_BASE = 0x0E
  integer, parameter :: TIMER1_T1CON_ADDR = 0x10
  integer, parameter :: TIMER1_TMR1L_ADDR = 0x0E
  integer, parameter :: TIMER1_TMR1H_ADDR = 0x0F
  ! Timer 2
  integer, parameter :: TIMER2_BASE = 0x11
  integer, parameter :: TIMER2_T2CON_ADDR = 0x12
  integer, parameter :: TIMER2_TMR2_ADDR = 0x11
  integer, parameter :: TIMER2_PR2_ADDR = 0x92
  ! A/D Converter
  integer, parameter :: ADC_BASE = 0x1E
  integer, parameter :: ADC_ADRESH_ADDR = 0x1E
  integer, parameter :: ADC_ADRESL_ADDR = 0x9F
  integer, parameter :: ADC_ADCON0_ADDR = 0x1F
  integer, parameter :: ADC_ADCON1_ADDR = 0x9F
  ! Master Synchronous Serial Port
  integer, parameter :: MSSP_BASE = 0x13
  integer, parameter :: MSSP_SSPSTAT_ADDR = 0x94
  integer, parameter :: MSSP_SSPCON_ADDR = 0x14
  integer, parameter :: MSSP_SSPBUF_ADDR = 0x13
  ! USART
  integer, parameter :: USART_BASE = 0x19
  integer, parameter :: USART_TXREG_ADDR = 0x19
  integer, parameter :: USART_RCREG_ADDR = 0x1A
  integer, parameter :: USART_SPBRG_ADDR = 0x99
  integer, parameter :: USART_TXSTA_ADDR = 0x98
  integer, parameter :: USART_RCSTA_ADDR = 0x18
  ! Capture/Compare/PWM 1
  integer, parameter :: CCP1_BASE = 0x15
  integer, parameter :: CCP1_CCP1CON_ADDR = 0x17
  integer, parameter :: CCP1_CCPR1L_ADDR = 0x15
  integer, parameter :: CCP1_CCPR1H_ADDR = 0x16
  ! Capture/Compare/PWM 2
  integer, parameter :: CCP2_BASE = 0x1B
  integer, parameter :: CCP2_CCP2CON_ADDR = 0x1D
  integer, parameter :: CCP2_CCPR2L_ADDR = 0x1B
  integer, parameter :: CCP2_CCPR2H_ADDR = 0x1C

  ! 中断向量定义
  integer, parameter :: INT_INT = 1  ! External Interrupt
  integer, parameter :: INT_TMR0 = 2  ! Timer 0 Overflow
  integer, parameter :: INT_RB = 3  ! PORTB Change
  integer, parameter :: INT_CCP1 = 4  ! CCP1
  integer, parameter :: INT_CCP2 = 5  ! CCP2
  integer, parameter :: INT_TMR1 = 6  ! Timer 1 Overflow
  integer, parameter :: INT_TMR2 = 8  ! Timer 2 Overflow
  integer, parameter :: INT_SPI = 9  ! SPI/I2C
  integer, parameter :: INT_SCI = 10  ! USART Receive
  integer, parameter :: INT_SCI = 11  ! USART Transmit
  integer, parameter :: INT_ADC = 12  ! A/D Converter
  integer, parameter :: INT_EEPROM = 13  ! EEPROM Write Complete

  ! 引脚定义
  integer, parameter :: PIN_MCLR_VPP = 1  ! Master Clear (Reset)
  integer, parameter :: PIN_RA0_AN0 = 2  ! PORTA Bit 0 / Analog 0
  integer, parameter :: PIN_RA1_AN1 = 3  ! PORTA Bit 1 / Analog 1
  integer, parameter :: PIN_RA2_AN2_VREF = 4  ! PORTA Bit 2 / Analog 2 / VREF-
  integer, parameter :: PIN_RA3_AN3_VREFP = 5  ! PORTA Bit 3 / Analog 3 / VREF+
  integer, parameter :: PIN_RA4_T0CKI = 6  ! PORTA Bit 4 / Timer 0 Clock Input
  integer, parameter :: PIN_RA5_AN4_SS = 7  ! PORTA Bit 4 / Analog 4 / SPI Slave Select
  integer, parameter :: PIN_RE0_RD_AN5 = 8  ! PORTE Bit 0 / Read Control / Analog 5
  integer, parameter :: PIN_RE1_WR_AN6 = 9  ! PORTE Bit 1 / Write Control / Analog 6
  integer, parameter :: PIN_RE2_CS_AN7 = 10  ! PORTE Bit 2 / Chip Select / Analog 7
  integer, parameter :: PIN_VDD = 11  ! Positive Supply
  integer, parameter :: PIN_VSS = 12  ! Ground
  integer, parameter :: PIN_OSC1_CLKIN = 13  ! Oscillator/Clock Input
  integer, parameter :: PIN_OSC2_CLKOUT = 14  ! Oscillator/Clock Output
  integer, parameter :: PIN_RC0_T1OSO = 15  ! PORTC Bit 0 / Timer 1 Oscillator
  integer, parameter :: PIN_RC1_T1OSI = 16  ! PORTC Bit 1 / Timer 1 Oscillator
  integer, parameter :: PIN_RC2_CCP1 = 17  ! PORTC Bit 2 / Capture/Compare/PWM 1
  integer, parameter :: PIN_RC3_SCK_SCL = 18  ! PORTC Bit 3 / SPI Clock / I2C Clock
  integer, parameter :: PIN_RC4_SDI_SDA = 23  ! PORTC Bit 4 / SPI Data In / I2C Data
  integer, parameter :: PIN_RC5_SDO = 24  ! PORTC Bit 5 / SPI Data Out
  integer, parameter :: PIN_RC6_TX = 25  ! PORTC Bit 6 / USART Transmit
  integer, parameter :: PIN_RC7_RX = 26  ! PORTC Bit 7 / USART Receive
  integer, parameter :: PIN_RD0 = 19  ! PORTD Bit 0
  integer, parameter :: PIN_RD1 = 20  ! PORTD Bit 1
  integer, parameter :: PIN_RD2 = 21  ! PORTD Bit 2
  integer, parameter :: PIN_RD3 = 22  ! PORTD Bit 3
  integer, parameter :: PIN_RD4 = 27  ! PORTD Bit 4
  integer, parameter :: PIN_RD5 = 28  ! PORTD Bit 5
  integer, parameter :: PIN_RD6 = 29  ! PORTD Bit 6
  integer, parameter :: PIN_RD7 = 30  ! PORTD Bit 7
  integer, parameter :: PIN_VSS = 31  ! Ground
  integer, parameter :: PIN_VDD = 32  ! Positive Supply
  integer, parameter :: PIN_RB0_INT = 33  ! PORTB Bit 0 / External Interrupt
  integer, parameter :: PIN_RB1 = 34  ! PORTB Bit 1
  integer, parameter :: PIN_RB2 = 35  ! PORTB Bit 2
  integer, parameter :: PIN_RB3_PGC = 36  ! PORTB Bit 3 / Programming Clock
  integer, parameter :: PIN_RB4_PGD = 37  ! PORTB Bit 4 / Programming Data
  integer, parameter :: PIN_RB5 = 38  ! PORTB Bit 5
  integer, parameter :: PIN_RB6_PGC = 39  ! PORTB Bit 6 / Programming Clock
  integer, parameter :: PIN_RB7_PGD = 40  ! PORTB Bit 7 / Programming Data

end module pic16f877a_device
