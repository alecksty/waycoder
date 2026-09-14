# PIC18F4550 设备定义 - Ruby 模块
# 生成自: Microchip/PIC18/PIC18F4550
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
# CPU架构: PIC18
# 位宽: 8位
# 时钟频率: 20000000 Hz

module PIC18F4550

  # 寄存器地址定义
  W_ADDR = 0x0E  # Working Register
  STATUS_ADDR = 0xFD8  # Status Register
  STATUS_C_BIT = 0  # Carry Flag
  STATUS_DC_BIT = 1  # Digit Carry Flag
  STATUS_Z_BIT = 2  # Zero Flag
  STATUS_PD_BIT = 3  # Power-Down Flag
  STATUS_TO_BIT = 4  # Time-out Flag
  STATUS_RP_BIT = 0  # Register Bank Select
  STATUS_IRP_BIT = 7  # Indirect Register Bank Select
  BSR_ADDR = 0xFE0  # Bank Select Register
  PORTA_ADDR = 0xF80  # Port A
  PORTB_ADDR = 0xF81  # Port B
  PORTC_ADDR = 0xF82  # Port C
  PORTD_ADDR = 0xF83  # Port D
  PORTE_ADDR = 0xF84  # Port E
  TRISA_ADDR = 0xF92  # Tri-state Port A
  TRISB_ADDR = 0xF93  # Tri-state Port B
  TRISC_ADDR = 0xF94  # Tri-state Port C
  TRISD_ADDR = 0xF95  # Tri-state Port D
  TRISE_ADDR = 0xF96  # Tri-state Port E
  LATA_ADDR = 0xF89  # Latch Port A
  LATB_ADDR = 0xF8A  # Latch Port B
  LATC_ADDR = 0xF8B  # Latch Port C
  LATD_ADDR = 0xF8C  # Latch Port D
  LATE_ADDR = 0xF8D  # Latch Port E
  INTCON_ADDR = 0xFF2  # Interrupt Control
  INTCON_RBIF_BIT = 0  # Port B Interrupt Flag
  INTCON_INT0IF_BIT = 1  # INT0 Interrupt Flag
  INTCON_TMR0IF_BIT = 2  # Timer 0 Interrupt Flag
  INTCON_RBIE_BIT = 3  # Port B Interrupt Enable
  INTCON_INT0IE_BIT = 4  # INT0 Interrupt Enable
  INTCON_TMR0IE_BIT = 5  # Timer 0 Interrupt Enable
  INTCON_PEIE_BIT = 6  # Peripheral Interrupt Enable
  INTCON_GIE_BIT = 7  # Global Interrupt Enable
  PIR1_ADDR = 0xF9E  # Peripheral Interrupt 1
  PIR2_ADDR = 0xF9F  # Peripheral Interrupt 2
  PIE1_ADDR = 0xF9D  # Peripheral Interrupt Enable 1
  PIE2_ADDR = 0xF9C  # Peripheral Interrupt Enable 2
  IPR1_ADDR = 0xF9B  # Interrupt Priority 1
  IPR2_ADDR = 0xF9A  # Interrupt Priority 2
  RCON_ADDR = 0xFD0  # Reset Control
  RCON_NOT_TO_BIT = 3  # Time-out Flag
  RCON_NOT_PD_BIT = 4  # Power-Down Flag
  RCON_NOT_RI_BIT = 5  # RESET Flag
  RCON_NOT_POR_BIT = 6  # Power-on Reset Flag
  RCON_NOT_BOR_BIT = 7  # Brown-out Reset Flag
  T0CON_ADDR = 0xFD1  # Timer 0 Control
  TMR0_ADDR = 0xFD6  # Timer 0 Register
  T1CON_ADDR = 0xFCD  # Timer 1 Control
  TMR1_ADDR = 0xFCE  # Timer 1 Register High
  TMR1L_ADDR = 0xFCF  # Timer 1 Register Low
  T2CON_ADDR = 0xFCA  # Timer 2 Control
  TMR2_ADDR = 0xFCB  # Timer 2 Register
  T3CON_ADDR = 0xFB1  # Timer 3 Control
  TMR3_ADDR = 0xFB3  # Timer 3 Register High
  TMR3L_ADDR = 0xFB2  # Timer 3 Register Low
  SSPCON1_ADDR = 0xFC6  # SSP Control 1
  SSPCON2_ADDR = 0xFC5  # SSP Control 2
  SSPSTAT_ADDR = 0xFC7  # SSP Status
  SSPBUF_ADDR = 0xFC9  # SSP Buffer
  SSPOR_ADDR = 0xFC8  # SSP Shift Register
  ADCON0_ADDR = 0xFC2  # A/D Control 0
  ADCON1_ADDR = 0xFC1  # A/D Control 1
  ADCON2_ADDR = 0xFC0  # A/D Control 2
  ADRES_ADDR = 0xFC3  # A/D Result
  ADRESL_ADDR = 0xFC4  # A/D Result Low
  CCP1CON_ADDR = 0xFD4  # CCP 1 Control
  CCPR1_ADDR = 0xFD6  # CCP 1 Register High
  CCPR1L_ADDR = 0xFD5  # CCP 1 Register Low
  CCP2CON_ADDR = 0xFBA  # CCP 2 Control
  CCPR2_ADDR = 0xFBB  # CCP 2 Register High
  CCPR2L_ADDR = 0xFBC  # CCP 2 Register Low
  USBCON_ADDR = 0xF75  # USB Control
  USBSTAT_ADDR = 0xF74  # USB Status
  UIE_ADDR = 0xF73  # USB Interrupt Enable
  UIR_ADDR = 0xF72  # USB Interrupt Flag
  UCON_ADDR = 0xF71  # USB Control
  USTAT_ADDR = 0xF70  # USB Status
  UEP0_ADDR = 0xF60  # USB Endpoint 0
  UEP1_ADDR = 0xF61  # USB Endpoint 1
  UEP2_ADDR = 0xF62  # USB Endpoint 2
  UEP3_ADDR = 0xF63  # USB Endpoint 3
  UEP4_ADDR = 0xF64  # USB Endpoint 4

  # 内存段定义
  FLASH_START = 0x0000
  FLASH_END = 0x7FFF
  FLASH_SIZE = 32768  # Program Flash (32KB)
  EEPROM_START = 0xF00000
  EEPROM_END = 0xF000FF
  EEPROM_SIZE = 256  # EEPROM (256B)
  SRAM_START = 0x0000
  SRAM_END = 0x07FF
  SRAM_SIZE = 2048  # SRAM (2KB)
  ACCESS_START = 0x0000
  ACCESS_END = 
  ACCESS_SIZE = 1  # Access Bank

  # 外设定义
  # Port A
  PORTA_BASE = 0xF80
  PORTA_PORT_ADDR = 0xF80
  PORTA_TRIS_ADDR = 0xF92
  PORTA_LAT_ADDR = 0xF89
  # Port B
  PORTB_BASE = 0xF81
  PORTB_PORT_ADDR = 0xF81
  PORTB_TRIS_ADDR = 0xF93
  PORTB_LAT_ADDR = 0xF8A
  # Port C
  PORTC_BASE = 0xF82
  PORTC_PORT_ADDR = 0xF82
  PORTC_TRIS_ADDR = 0xF94
  PORTC_LAT_ADDR = 0xF8B
  # Port D
  PORTD_BASE = 0xF83
  PORTD_PORT_ADDR = 0xF83
  PORTD_TRIS_ADDR = 0xF95
  PORTD_LAT_ADDR = 0xF8C
  # Port E
  PORTE_BASE = 0xF84
  PORTE_PORT_ADDR = 0xF84
  PORTE_TRIS_ADDR = 0xF96
  PORTE_LAT_ADDR = 0xF8D
  # Timer 0
  TIMER0_BASE = 0xFD1
  TIMER0_T0CON_ADDR = 0xFD1
  TIMER0_TMR0_ADDR = 0xFD6
  # Timer 1
  TIMER1_BASE = 0xFCD
  TIMER1_T1CON_ADDR = 0xFCD
  TIMER1_TMR1_ADDR = 0xFCF
  TIMER1_TMR1L_ADDR = 0xFCE
  # Timer 2
  TIMER2_BASE = 0xFCA
  TIMER2_T2CON_ADDR = 0xFCA
  TIMER2_TMR2_ADDR = 0xFCB
  # Timer 3
  TIMER3_BASE = 0xFB0
  TIMER3_T3CON_ADDR = 0xFB0
  TIMER3_TMR3_ADDR = 0xFB2
  # A/D Converter
  ADC_BASE = 0xFC2
  ADC_ADCON0_ADDR = 0xFC2
  ADC_ADCON1_ADDR = 0xFC1
  ADC_ADCON2_ADDR = 0xFC0
  ADC_ADRES_ADDR = 0xFC3
  ADC_ADRESL_ADDR = 0xFC4
  # CCP 1
  CCP1_BASE = 0xFD4
  CCP1_CCP1CON_ADDR = 0xFD4
  CCP1_CCPR1_ADDR = 0xFD6
  CCP1_CCPR1L_ADDR = 0xFD5
  # CCP 2
  CCP2_BASE = 0xFBA
  CCP2_CCP2CON_ADDR = 0xFBA
  CCP2_CCPR2_ADDR = 0xFBB
  CCP2_CCPR2L_ADDR = 0xFBC
  # SSP (I2C/SPI)
  SSP_BASE = 0xFC6
  SSP_SSPCON1_ADDR = 0xFC6
  SSP_SSPCON2_ADDR = 0xFC5
  SSP_SSPSTAT_ADDR = 0xFC7
  SSP_SSPBUF_ADDR = 0xFC9
  SSP_SSPOV_ADDR = 0xFC8
  # EUSART
  EUSART_BASE = 0xF15
  EUSART_TXSTA_ADDR = 0xFE2
  EUSART_RCSTA_ADDR = 0xFE3
  EUSART_TXREG_ADDR = 0xFAD
  EUSART_RCREG_ADDR = 0xFAE
  EUSART_SPBRG_ADDR = 0xFAF
  EUSART_SPBRGH_ADDR = 0xFB0
  EUSART_BAUDCON_ADDR = 0xFB8
  # Comparators
  COMPARATOR_BASE = 0xFB4
  COMPARATOR_CMCON_ADDR = 0xFB4
  COMPARATOR_CVRCON_ADDR = 0xFB5
  # USB Module
  USB_BASE = 0xF70
  USB_UCON_ADDR = 0xF71
  USB_USTAT_ADDR = 0xF72
  USB_UIR_ADDR = 0xF73
  USB_UIE_ADDR = 0xF74
  USB_UEP0_ADDR = 0xF80
  USB_UEP1_ADDR = 0xF81
  USB_UEP2_ADDR = 0xF82
  USB_UEP3_ADDR = 0xF83
  USB_BD0_ADDR = 0xF00
  USB_BD1_ADDR = 0xF08
  USB_BD2_ADDR = 0xF10
  USB_BD3_ADDR = 0xF18
  # Oscillator
  OSCCON_BASE = 0xFD3
  OSCCON_OSCCON_ADDR = 0xFD3
  OSCCON_OSCTUNE_ADDR = 0xFD9
  # Watchdog Timer
  WDTCON_BASE = 0xFD1
  WDTCON_WDTCON_ADDR = 0xFD1

  # 中断向量定义
  INT_RESET = 0  # RESET
  INT_INT0 = 1  # External Interrupt 0
  INT_INT1 = 2  # External Interrupt 1
  INT_INT2 = 3  # External Interrupt 2
  INT_TMR0 = 4  # Timer 0 Overflow
  INT_TMR1 = 5  # Timer 1 Overflow
  INT_TMR2 = 6  # Timer 2 Match
  INT_TMR3 = 7  # Timer 3 Overflow
  INT_CCP1 = 8  # CCP 1
  INT_CCP2 = 9  # CCP 2
  INT_SSP = 10  # SSP
  INT_TX = 11  # USART TX
  INT_RC = 12  # USART RX
  INT_ADC = 13  # A/D
  INT_RBO = 14  # Port B Change
  INT_EXT = 15  # External

  # 引脚定义
  PIN_RE3 = 1  # MCLR/VPP/RE3
  PIN_RA0 = 2  # AN0/RA0
  PIN_RA1 = 3  # AN1/RA1
  PIN_RA2 = 4  # AN2/VREF-/RA2
  PIN_RA3 = 5  # AN3/VREF+/RA3
  PIN_RA4 = 6  # AN4/T0CKI/RA4
  PIN_RA5 = 7  # AN5/RE5
  PIN_VSS = 8  # Ground
  PIN_RA7 = 9  # OSC1/CLKI/RA7
  PIN_RA6 = 10  # OSC2/CLKO/RA6
  PIN_RC0 = 11  # T1OSO/T1CKI/RC0
  PIN_RC1 = 12  # T1OSI/RC1
  PIN_RC2 = 13  # CCP1/RC2
  PIN_RC3 = 14  # SCK/SCL/RC3
  PIN_RD0 = 15  # SDO/RD0
  PIN_RD1 = 16  # SDI/RD1
  PIN_RD2 = 17  # RD2
  PIN_RC6 = 18  # TX/CK/RC6
  PIN_RC7 = 19  # RX/DT/RC7
  PIN_VSS = 20  # Ground
  PIN_RD3 = 21  # RD3
  PIN_RD4 = 22  # RD4
  PIN_RD5 = 23  # PWRB/RD5
  PIN_RD6 = 24  # PBC/RD6
  PIN_RD7 = 25  # PCD/RD7
  PIN_RC4 = 26  # D-/RC4
  PIN_RC5 = 27  # D+/RC5
  PIN_RE0 = 28  # AN5/RE0
  PIN_RE1 = 29  # AN6/RE1
  PIN_RE2 = 30  # AN7/RE2
  PIN_VSS = 31  # Ground
  PIN_VDD = 32  # Vdd

end
