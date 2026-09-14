! PIC18F4550 设备定义 - Fortran 模块
! 生成自: Microchip/PIC18/PIC18F4550
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
! CPU架构: PIC18
! 位宽: 8位
! 时钟频率: 20000000 Hz

module pic18f4550_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: W_ADDR = 0x0E  ! Working Register
  integer, parameter :: STATUS_ADDR = 0xFD8  ! Status Register
  integer, parameter :: STATUS_C_BIT = 0  ! Carry Flag
  integer, parameter :: STATUS_DC_BIT = 1  ! Digit Carry Flag
  integer, parameter :: STATUS_Z_BIT = 2  ! Zero Flag
  integer, parameter :: STATUS_PD_BIT = 3  ! Power-Down Flag
  integer, parameter :: STATUS_TO_BIT = 4  ! Time-out Flag
  integer, parameter :: STATUS_RP_BIT = 0  ! Register Bank Select
  integer, parameter :: STATUS_IRP_BIT = 7  ! Indirect Register Bank Select
  integer, parameter :: BSR_ADDR = 0xFE0  ! Bank Select Register
  integer, parameter :: PORTA_ADDR = 0xF80  ! Port A
  integer, parameter :: PORTB_ADDR = 0xF81  ! Port B
  integer, parameter :: PORTC_ADDR = 0xF82  ! Port C
  integer, parameter :: PORTD_ADDR = 0xF83  ! Port D
  integer, parameter :: PORTE_ADDR = 0xF84  ! Port E
  integer, parameter :: TRISA_ADDR = 0xF92  ! Tri-state Port A
  integer, parameter :: TRISB_ADDR = 0xF93  ! Tri-state Port B
  integer, parameter :: TRISC_ADDR = 0xF94  ! Tri-state Port C
  integer, parameter :: TRISD_ADDR = 0xF95  ! Tri-state Port D
  integer, parameter :: TRISE_ADDR = 0xF96  ! Tri-state Port E
  integer, parameter :: LATA_ADDR = 0xF89  ! Latch Port A
  integer, parameter :: LATB_ADDR = 0xF8A  ! Latch Port B
  integer, parameter :: LATC_ADDR = 0xF8B  ! Latch Port C
  integer, parameter :: LATD_ADDR = 0xF8C  ! Latch Port D
  integer, parameter :: LATE_ADDR = 0xF8D  ! Latch Port E
  integer, parameter :: INTCON_ADDR = 0xFF2  ! Interrupt Control
  integer, parameter :: INTCON_RBIF_BIT = 0  ! Port B Interrupt Flag
  integer, parameter :: INTCON_INT0IF_BIT = 1  ! INT0 Interrupt Flag
  integer, parameter :: INTCON_TMR0IF_BIT = 2  ! Timer 0 Interrupt Flag
  integer, parameter :: INTCON_RBIE_BIT = 3  ! Port B Interrupt Enable
  integer, parameter :: INTCON_INT0IE_BIT = 4  ! INT0 Interrupt Enable
  integer, parameter :: INTCON_TMR0IE_BIT = 5  ! Timer 0 Interrupt Enable
  integer, parameter :: INTCON_PEIE_BIT = 6  ! Peripheral Interrupt Enable
  integer, parameter :: INTCON_GIE_BIT = 7  ! Global Interrupt Enable
  integer, parameter :: PIR1_ADDR = 0xF9E  ! Peripheral Interrupt 1
  integer, parameter :: PIR2_ADDR = 0xF9F  ! Peripheral Interrupt 2
  integer, parameter :: PIE1_ADDR = 0xF9D  ! Peripheral Interrupt Enable 1
  integer, parameter :: PIE2_ADDR = 0xF9C  ! Peripheral Interrupt Enable 2
  integer, parameter :: IPR1_ADDR = 0xF9B  ! Interrupt Priority 1
  integer, parameter :: IPR2_ADDR = 0xF9A  ! Interrupt Priority 2
  integer, parameter :: RCON_ADDR = 0xFD0  ! Reset Control
  integer, parameter :: RCON_NOT_TO_BIT = 3  ! Time-out Flag
  integer, parameter :: RCON_NOT_PD_BIT = 4  ! Power-Down Flag
  integer, parameter :: RCON_NOT_RI_BIT = 5  ! RESET Flag
  integer, parameter :: RCON_NOT_POR_BIT = 6  ! Power-on Reset Flag
  integer, parameter :: RCON_NOT_BOR_BIT = 7  ! Brown-out Reset Flag
  integer, parameter :: T0CON_ADDR = 0xFD1  ! Timer 0 Control
  integer, parameter :: TMR0_ADDR = 0xFD6  ! Timer 0 Register
  integer, parameter :: T1CON_ADDR = 0xFCD  ! Timer 1 Control
  integer, parameter :: TMR1_ADDR = 0xFCE  ! Timer 1 Register High
  integer, parameter :: TMR1L_ADDR = 0xFCF  ! Timer 1 Register Low
  integer, parameter :: T2CON_ADDR = 0xFCA  ! Timer 2 Control
  integer, parameter :: TMR2_ADDR = 0xFCB  ! Timer 2 Register
  integer, parameter :: T3CON_ADDR = 0xFB1  ! Timer 3 Control
  integer, parameter :: TMR3_ADDR = 0xFB3  ! Timer 3 Register High
  integer, parameter :: TMR3L_ADDR = 0xFB2  ! Timer 3 Register Low
  integer, parameter :: SSPCON1_ADDR = 0xFC6  ! SSP Control 1
  integer, parameter :: SSPCON2_ADDR = 0xFC5  ! SSP Control 2
  integer, parameter :: SSPSTAT_ADDR = 0xFC7  ! SSP Status
  integer, parameter :: SSPBUF_ADDR = 0xFC9  ! SSP Buffer
  integer, parameter :: SSPOR_ADDR = 0xFC8  ! SSP Shift Register
  integer, parameter :: ADCON0_ADDR = 0xFC2  ! A/D Control 0
  integer, parameter :: ADCON1_ADDR = 0xFC1  ! A/D Control 1
  integer, parameter :: ADCON2_ADDR = 0xFC0  ! A/D Control 2
  integer, parameter :: ADRES_ADDR = 0xFC3  ! A/D Result
  integer, parameter :: ADRESL_ADDR = 0xFC4  ! A/D Result Low
  integer, parameter :: CCP1CON_ADDR = 0xFD4  ! CCP 1 Control
  integer, parameter :: CCPR1_ADDR = 0xFD6  ! CCP 1 Register High
  integer, parameter :: CCPR1L_ADDR = 0xFD5  ! CCP 1 Register Low
  integer, parameter :: CCP2CON_ADDR = 0xFBA  ! CCP 2 Control
  integer, parameter :: CCPR2_ADDR = 0xFBB  ! CCP 2 Register High
  integer, parameter :: CCPR2L_ADDR = 0xFBC  ! CCP 2 Register Low
  integer, parameter :: USBCON_ADDR = 0xF75  ! USB Control
  integer, parameter :: USBSTAT_ADDR = 0xF74  ! USB Status
  integer, parameter :: UIE_ADDR = 0xF73  ! USB Interrupt Enable
  integer, parameter :: UIR_ADDR = 0xF72  ! USB Interrupt Flag
  integer, parameter :: UCON_ADDR = 0xF71  ! USB Control
  integer, parameter :: USTAT_ADDR = 0xF70  ! USB Status
  integer, parameter :: UEP0_ADDR = 0xF60  ! USB Endpoint 0
  integer, parameter :: UEP1_ADDR = 0xF61  ! USB Endpoint 1
  integer, parameter :: UEP2_ADDR = 0xF62  ! USB Endpoint 2
  integer, parameter :: UEP3_ADDR = 0xF63  ! USB Endpoint 3
  integer, parameter :: UEP4_ADDR = 0xF64  ! USB Endpoint 4

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x7FFF
  integer, parameter :: FLASH_SIZE = 32768  ! Program Flash (32KB)
  integer, parameter :: EEPROM_START = 0xF00000
  integer, parameter :: EEPROM_END = 0xF000FF
  integer, parameter :: EEPROM_SIZE = 256  ! EEPROM (256B)
  integer, parameter :: SRAM_START = 0x0000
  integer, parameter :: SRAM_END = 0x07FF
  integer, parameter :: SRAM_SIZE = 2048  ! SRAM (2KB)
  integer, parameter :: ACCESS_START = 0x0000
  integer, parameter :: ACCESS_END = 
  integer, parameter :: ACCESS_SIZE = 1  ! Access Bank

  ! 外设定义
  ! Port A
  integer, parameter :: PORTA_BASE = 0xF80
  integer, parameter :: PORTA_PORT_ADDR = 0xF80
  integer, parameter :: PORTA_TRIS_ADDR = 0xF92
  integer, parameter :: PORTA_LAT_ADDR = 0xF89
  ! Port B
  integer, parameter :: PORTB_BASE = 0xF81
  integer, parameter :: PORTB_PORT_ADDR = 0xF81
  integer, parameter :: PORTB_TRIS_ADDR = 0xF93
  integer, parameter :: PORTB_LAT_ADDR = 0xF8A
  ! Port C
  integer, parameter :: PORTC_BASE = 0xF82
  integer, parameter :: PORTC_PORT_ADDR = 0xF82
  integer, parameter :: PORTC_TRIS_ADDR = 0xF94
  integer, parameter :: PORTC_LAT_ADDR = 0xF8B
  ! Port D
  integer, parameter :: PORTD_BASE = 0xF83
  integer, parameter :: PORTD_PORT_ADDR = 0xF83
  integer, parameter :: PORTD_TRIS_ADDR = 0xF95
  integer, parameter :: PORTD_LAT_ADDR = 0xF8C
  ! Port E
  integer, parameter :: PORTE_BASE = 0xF84
  integer, parameter :: PORTE_PORT_ADDR = 0xF84
  integer, parameter :: PORTE_TRIS_ADDR = 0xF96
  integer, parameter :: PORTE_LAT_ADDR = 0xF8D
  ! Timer 0
  integer, parameter :: TIMER0_BASE = 0xFD1
  integer, parameter :: TIMER0_T0CON_ADDR = 0xFD1
  integer, parameter :: TIMER0_TMR0_ADDR = 0xFD6
  ! Timer 1
  integer, parameter :: TIMER1_BASE = 0xFCD
  integer, parameter :: TIMER1_T1CON_ADDR = 0xFCD
  integer, parameter :: TIMER1_TMR1_ADDR = 0xFCF
  integer, parameter :: TIMER1_TMR1L_ADDR = 0xFCE
  ! Timer 2
  integer, parameter :: TIMER2_BASE = 0xFCA
  integer, parameter :: TIMER2_T2CON_ADDR = 0xFCA
  integer, parameter :: TIMER2_TMR2_ADDR = 0xFCB
  ! Timer 3
  integer, parameter :: TIMER3_BASE = 0xFB0
  integer, parameter :: TIMER3_T3CON_ADDR = 0xFB0
  integer, parameter :: TIMER3_TMR3_ADDR = 0xFB2
  ! A/D Converter
  integer, parameter :: ADC_BASE = 0xFC2
  integer, parameter :: ADC_ADCON0_ADDR = 0xFC2
  integer, parameter :: ADC_ADCON1_ADDR = 0xFC1
  integer, parameter :: ADC_ADCON2_ADDR = 0xFC0
  integer, parameter :: ADC_ADRES_ADDR = 0xFC3
  integer, parameter :: ADC_ADRESL_ADDR = 0xFC4
  ! CCP 1
  integer, parameter :: CCP1_BASE = 0xFD4
  integer, parameter :: CCP1_CCP1CON_ADDR = 0xFD4
  integer, parameter :: CCP1_CCPR1_ADDR = 0xFD6
  integer, parameter :: CCP1_CCPR1L_ADDR = 0xFD5
  ! CCP 2
  integer, parameter :: CCP2_BASE = 0xFBA
  integer, parameter :: CCP2_CCP2CON_ADDR = 0xFBA
  integer, parameter :: CCP2_CCPR2_ADDR = 0xFBB
  integer, parameter :: CCP2_CCPR2L_ADDR = 0xFBC
  ! SSP (I2C/SPI)
  integer, parameter :: SSP_BASE = 0xFC6
  integer, parameter :: SSP_SSPCON1_ADDR = 0xFC6
  integer, parameter :: SSP_SSPCON2_ADDR = 0xFC5
  integer, parameter :: SSP_SSPSTAT_ADDR = 0xFC7
  integer, parameter :: SSP_SSPBUF_ADDR = 0xFC9
  integer, parameter :: SSP_SSPOV_ADDR = 0xFC8
  ! EUSART
  integer, parameter :: EUSART_BASE = 0xF15
  integer, parameter :: EUSART_TXSTA_ADDR = 0xFE2
  integer, parameter :: EUSART_RCSTA_ADDR = 0xFE3
  integer, parameter :: EUSART_TXREG_ADDR = 0xFAD
  integer, parameter :: EUSART_RCREG_ADDR = 0xFAE
  integer, parameter :: EUSART_SPBRG_ADDR = 0xFAF
  integer, parameter :: EUSART_SPBRGH_ADDR = 0xFB0
  integer, parameter :: EUSART_BAUDCON_ADDR = 0xFB8
  ! Comparators
  integer, parameter :: COMPARATOR_BASE = 0xFB4
  integer, parameter :: COMPARATOR_CMCON_ADDR = 0xFB4
  integer, parameter :: COMPARATOR_CVRCON_ADDR = 0xFB5
  ! USB Module
  integer, parameter :: USB_BASE = 0xF70
  integer, parameter :: USB_UCON_ADDR = 0xF71
  integer, parameter :: USB_USTAT_ADDR = 0xF72
  integer, parameter :: USB_UIR_ADDR = 0xF73
  integer, parameter :: USB_UIE_ADDR = 0xF74
  integer, parameter :: USB_UEP0_ADDR = 0xF80
  integer, parameter :: USB_UEP1_ADDR = 0xF81
  integer, parameter :: USB_UEP2_ADDR = 0xF82
  integer, parameter :: USB_UEP3_ADDR = 0xF83
  integer, parameter :: USB_BD0_ADDR = 0xF00
  integer, parameter :: USB_BD1_ADDR = 0xF08
  integer, parameter :: USB_BD2_ADDR = 0xF10
  integer, parameter :: USB_BD3_ADDR = 0xF18
  ! Oscillator
  integer, parameter :: OSCCON_BASE = 0xFD3
  integer, parameter :: OSCCON_OSCCON_ADDR = 0xFD3
  integer, parameter :: OSCCON_OSCTUNE_ADDR = 0xFD9
  ! Watchdog Timer
  integer, parameter :: WDTCON_BASE = 0xFD1
  integer, parameter :: WDTCON_WDTCON_ADDR = 0xFD1

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! RESET
  integer, parameter :: INT_INT0 = 1  ! External Interrupt 0
  integer, parameter :: INT_INT1 = 2  ! External Interrupt 1
  integer, parameter :: INT_INT2 = 3  ! External Interrupt 2
  integer, parameter :: INT_TMR0 = 4  ! Timer 0 Overflow
  integer, parameter :: INT_TMR1 = 5  ! Timer 1 Overflow
  integer, parameter :: INT_TMR2 = 6  ! Timer 2 Match
  integer, parameter :: INT_TMR3 = 7  ! Timer 3 Overflow
  integer, parameter :: INT_CCP1 = 8  ! CCP 1
  integer, parameter :: INT_CCP2 = 9  ! CCP 2
  integer, parameter :: INT_SSP = 10  ! SSP
  integer, parameter :: INT_TX = 11  ! USART TX
  integer, parameter :: INT_RC = 12  ! USART RX
  integer, parameter :: INT_ADC = 13  ! A/D
  integer, parameter :: INT_RBO = 14  ! Port B Change
  integer, parameter :: INT_EXT = 15  ! External

  ! 引脚定义
  integer, parameter :: PIN_RE3 = 1  ! MCLR/VPP/RE3
  integer, parameter :: PIN_RA0 = 2  ! AN0/RA0
  integer, parameter :: PIN_RA1 = 3  ! AN1/RA1
  integer, parameter :: PIN_RA2 = 4  ! AN2/VREF-/RA2
  integer, parameter :: PIN_RA3 = 5  ! AN3/VREF+/RA3
  integer, parameter :: PIN_RA4 = 6  ! AN4/T0CKI/RA4
  integer, parameter :: PIN_RA5 = 7  ! AN5/RE5
  integer, parameter :: PIN_VSS = 8  ! Ground
  integer, parameter :: PIN_RA7 = 9  ! OSC1/CLKI/RA7
  integer, parameter :: PIN_RA6 = 10  ! OSC2/CLKO/RA6
  integer, parameter :: PIN_RC0 = 11  ! T1OSO/T1CKI/RC0
  integer, parameter :: PIN_RC1 = 12  ! T1OSI/RC1
  integer, parameter :: PIN_RC2 = 13  ! CCP1/RC2
  integer, parameter :: PIN_RC3 = 14  ! SCK/SCL/RC3
  integer, parameter :: PIN_RD0 = 15  ! SDO/RD0
  integer, parameter :: PIN_RD1 = 16  ! SDI/RD1
  integer, parameter :: PIN_RD2 = 17  ! RD2
  integer, parameter :: PIN_RC6 = 18  ! TX/CK/RC6
  integer, parameter :: PIN_RC7 = 19  ! RX/DT/RC7
  integer, parameter :: PIN_VSS = 20  ! Ground
  integer, parameter :: PIN_RD3 = 21  ! RD3
  integer, parameter :: PIN_RD4 = 22  ! RD4
  integer, parameter :: PIN_RD5 = 23  ! PWRB/RD5
  integer, parameter :: PIN_RD6 = 24  ! PBC/RD6
  integer, parameter :: PIN_RD7 = 25  ! PCD/RD7
  integer, parameter :: PIN_RC4 = 26  ! D-/RC4
  integer, parameter :: PIN_RC5 = 27  ! D+/RC5
  integer, parameter :: PIN_RE0 = 28  ! AN5/RE0
  integer, parameter :: PIN_RE1 = 29  ! AN6/RE1
  integer, parameter :: PIN_RE2 = 30  ! AN7/RE2
  integer, parameter :: PIN_VSS = 31  ! Ground
  integer, parameter :: PIN_VDD = 32  ! Vdd

end module pic18f4550_device
