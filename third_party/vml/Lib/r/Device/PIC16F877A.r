# PIC16F877A 设备定义 - R 脚本
# 生成自: Microchip/PIC/PIC16F877A
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
# CPU架构: PIC16
# 位宽: 8位
# 时钟频率: 4000000 Hz

# 寄存器地址定义
W_ADDR <- 0x00  # Working Register
STATUS_ADDR <- 0x03  # Status Register
STATUS_C_BIT <- 0  # Carry flag
STATUS_DC_BIT <- 1  # Digit carry flag
STATUS_Z_BIT <- 2  # Zero flag
STATUS_PD_BIT <- 3  # Power-down flag
STATUS_TO_BIT <- 4  # Time-out flag
STATUS_RP_BIT <- 5  # Register bank select
STATUS_IRP_BIT <- 7  # Indirect register bank select
INTCON_ADDR <- 0x0B  # Interrupt Control Register
INTCON_RBIF_BIT <- 0  # PORTB change interrupt flag
INTCON_INTF_BIT <- 1  # External interrupt flag
INTCON_TMR0IF_BIT <- 2  # TMR0 overflow interrupt flag
INTCON_RBIE_BIT <- 3  # PORTB change interrupt enable
INTCON_INTE_BIT <- 4  # External interrupt enable
INTCON_TMR0IE_BIT <- 5  # TMR0 overflow interrupt enable
INTCON_PEIE_BIT <- 6  # Peripheral interrupt enable
INTCON_GIE_BIT <- 7  # Global interrupt enable
PORTB_ADDR <- 0x06  # PORT B
TRISB_ADDR <- 0x86  # TRIS B
PORTC_ADDR <- 0x07  # PORT C
TRISC_ADDR <- 0x87  # TRIS C
PORTD_ADDR <- 0x08  # PORT D
TRISD_ADDR <- 0x88  # TRIS D
PORTE_ADDR <- 0x09  # PORT E
TRISE_ADDR <- 0x89  # TRIS E
TMR0_ADDR <- 0x01  # Timer 0
OPTION_REG_ADDR <- 0x81  # Option Register
PCL_ADDR <- 0x02  # Program Counter Low
PCLATH_ADDR <- 0x0A  # Program Counter Latch High
FSR_ADDR <- 0x04  # File Select Register
EEDATA_ADDR <- 0x10C  # EEPROM Data
EEADR_ADDR <- 0x10D  # EEPROM Address
EECON1_ADDR <- 0x18C  # EEPROM Control 1
EECON1_RD_BIT <- 0  # Read control
EECON1_WR_BIT <- 1  # Write control
EECON1_WREN_BIT <- 2  # Write enable
EECON1_WRERR_BIT <- 3  # Write error flag
EECON1_EEPGD_BIT <- 7  # EEPROM program/data select
EECON2_ADDR <- 0x18D  # EEPROM Control 2
ADRESH_ADDR <- 0x1E  # A/D Result High
ADRESL_ADDR <- 0x1F  # A/D Result Low
ADCON0_ADDR <- 0x1F  # A/D Control 0
ADCON0_ADON_BIT <- 0  # A/D enable
ADCON0_GO_DONE_BIT <- 2  # A/D conversion status
ADCON0_CHS_BIT <- 3  # Channel select
ADCON1_ADDR <- 0x9F  # A/D Control 1
SSPSTAT_ADDR <- 0x94  # MSSP Status
SSPCON_ADDR <- 0x14  # MSSP Control
SSPBUF_ADDR <- 0x13  # SSP Buffer
TXREG_ADDR <- 0x19  # USART Transmit Register
RCREG_ADDR <- 0x1A  # USART Receive Register
SPBRG_ADDR <- 0x99  # Baud Rate Generator
TXSTA_ADDR <- 0x98  # TX Status and Control
RCSTA_ADDR <- 0x18  # RX Status and Control
CCP1CON_ADDR <- 0x17  # CCP1 Control
CCPR1L_ADDR <- 0x15  # CCP1 Low
CCPR1H_ADDR <- 0x16  # CCP1 High
CCP2CON_ADDR <- 0x1D  # CCP2 Control
CCPR2L_ADDR <- 0x1B  # CCP2 Low
CCPR2H_ADDR <- 0x1C  # CCP2 High
T1CON_ADDR <- 0x10  # Timer 1 Control
TMR1L_ADDR <- 0x0E  # Timer 1 Low
TMR1H_ADDR <- 0x0F  # Timer 1 High
T2CON_ADDR <- 0x12  # Timer 2 Control
TMR2_ADDR <- 0x11  # Timer 2
PR2_ADDR <- 0x92  # Timer 2 Period

# 内存段定义
PROGRAM_START <- 0x0000
PROGRAM_END <- 0x1FFF
PROGRAM_SIZE <- 8192  # Program Memory (8KB)
DATA_START <- 0x20
DATA_END <- 0x7F
DATA_SIZE <- 96  # General Purpose RAM Bank 0
SRAM_START <- 0xA0
SRAM_END <- 0xFF
SRAM_SIZE <- 96  # General Purpose RAM Bank 1
EEPROM_START <- 0x2100
EEPROM_END <- 0x21FF
EEPROM_SIZE <- 256  # EEPROM Data Memory

# 外设定义
# Port B
GPIO_PORTB_BASE <- 0x06
GPIO_PORTB_PORTB_ADDR <- 0x06
GPIO_PORTB_TRISB_ADDR <- 0x86
# Port C
GPIO_PORTC_BASE <- 0x07
GPIO_PORTC_PORTC_ADDR <- 0x07
GPIO_PORTC_TRISC_ADDR <- 0x87
# Port D
GPIO_PORTD_BASE <- 0x08
GPIO_PORTD_PORTD_ADDR <- 0x08
GPIO_PORTD_TRISD_ADDR <- 0x88
# Timer 0
TIMER0_BASE <- 0x01
TIMER0_TMR0_ADDR <- 0x01
TIMER0_OPTION_REG_ADDR <- 0x81
# Timer 1
TIMER1_BASE <- 0x0E
TIMER1_T1CON_ADDR <- 0x10
TIMER1_TMR1L_ADDR <- 0x0E
TIMER1_TMR1H_ADDR <- 0x0F
# Timer 2
TIMER2_BASE <- 0x11
TIMER2_T2CON_ADDR <- 0x12
TIMER2_TMR2_ADDR <- 0x11
TIMER2_PR2_ADDR <- 0x92
# A/D Converter
ADC_BASE <- 0x1E
ADC_ADRESH_ADDR <- 0x1E
ADC_ADRESL_ADDR <- 0x9F
ADC_ADCON0_ADDR <- 0x1F
ADC_ADCON1_ADDR <- 0x9F
# Master Synchronous Serial Port
MSSP_BASE <- 0x13
MSSP_SSPSTAT_ADDR <- 0x94
MSSP_SSPCON_ADDR <- 0x14
MSSP_SSPBUF_ADDR <- 0x13
# USART
USART_BASE <- 0x19
USART_TXREG_ADDR <- 0x19
USART_RCREG_ADDR <- 0x1A
USART_SPBRG_ADDR <- 0x99
USART_TXSTA_ADDR <- 0x98
USART_RCSTA_ADDR <- 0x18
# Capture/Compare/PWM 1
CCP1_BASE <- 0x15
CCP1_CCP1CON_ADDR <- 0x17
CCP1_CCPR1L_ADDR <- 0x15
CCP1_CCPR1H_ADDR <- 0x16
# Capture/Compare/PWM 2
CCP2_BASE <- 0x1B
CCP2_CCP2CON_ADDR <- 0x1D
CCP2_CCPR2L_ADDR <- 0x1B
CCP2_CCPR2H_ADDR <- 0x1C

# 中断向量定义
INT_INT <- 1  # External Interrupt
INT_TMR0 <- 2  # Timer 0 Overflow
INT_RB <- 3  # PORTB Change
INT_CCP1 <- 4  # CCP1
INT_CCP2 <- 5  # CCP2
INT_TMR1 <- 6  # Timer 1 Overflow
INT_TMR2 <- 8  # Timer 2 Overflow
INT_SPI <- 9  # SPI/I2C
INT_SCI <- 10  # USART Receive
INT_SCI <- 11  # USART Transmit
INT_ADC <- 12  # A/D Converter
INT_EEPROM <- 13  # EEPROM Write Complete

# 引脚定义
PIN_MCLR_VPP <- 1  # Master Clear (Reset)
PIN_RA0_AN0 <- 2  # PORTA Bit 0 / Analog 0
PIN_RA1_AN1 <- 3  # PORTA Bit 1 / Analog 1
PIN_RA2_AN2_VREF <- 4  # PORTA Bit 2 / Analog 2 / VREF-
PIN_RA3_AN3_VREFP <- 5  # PORTA Bit 3 / Analog 3 / VREF+
PIN_RA4_T0CKI <- 6  # PORTA Bit 4 / Timer 0 Clock Input
PIN_RA5_AN4_SS <- 7  # PORTA Bit 4 / Analog 4 / SPI Slave Select
PIN_RE0_RD_AN5 <- 8  # PORTE Bit 0 / Read Control / Analog 5
PIN_RE1_WR_AN6 <- 9  # PORTE Bit 1 / Write Control / Analog 6
PIN_RE2_CS_AN7 <- 10  # PORTE Bit 2 / Chip Select / Analog 7
PIN_VDD <- 11  # Positive Supply
PIN_VSS <- 12  # Ground
PIN_OSC1_CLKIN <- 13  # Oscillator/Clock Input
PIN_OSC2_CLKOUT <- 14  # Oscillator/Clock Output
PIN_RC0_T1OSO <- 15  # PORTC Bit 0 / Timer 1 Oscillator
PIN_RC1_T1OSI <- 16  # PORTC Bit 1 / Timer 1 Oscillator
PIN_RC2_CCP1 <- 17  # PORTC Bit 2 / Capture/Compare/PWM 1
PIN_RC3_SCK_SCL <- 18  # PORTC Bit 3 / SPI Clock / I2C Clock
PIN_RC4_SDI_SDA <- 23  # PORTC Bit 4 / SPI Data In / I2C Data
PIN_RC5_SDO <- 24  # PORTC Bit 5 / SPI Data Out
PIN_RC6_TX <- 25  # PORTC Bit 6 / USART Transmit
PIN_RC7_RX <- 26  # PORTC Bit 7 / USART Receive
PIN_RD0 <- 19  # PORTD Bit 0
PIN_RD1 <- 20  # PORTD Bit 1
PIN_RD2 <- 21  # PORTD Bit 2
PIN_RD3 <- 22  # PORTD Bit 3
PIN_RD4 <- 27  # PORTD Bit 4
PIN_RD5 <- 28  # PORTD Bit 5
PIN_RD6 <- 29  # PORTD Bit 6
PIN_RD7 <- 30  # PORTD Bit 7
PIN_VSS <- 31  # Ground
PIN_VDD <- 32  # Positive Supply
PIN_RB0_INT <- 33  # PORTB Bit 0 / External Interrupt
PIN_RB1 <- 34  # PORTB Bit 1
PIN_RB2 <- 35  # PORTB Bit 2
PIN_RB3_PGC <- 36  # PORTB Bit 3 / Programming Clock
PIN_RB4_PGD <- 37  # PORTB Bit 4 / Programming Data
PIN_RB5 <- 38  # PORTB Bit 5
PIN_RB6_PGC <- 39  # PORTB Bit 6 / Programming Clock
PIN_RB7_PGD <- 40  # PORTB Bit 7 / Programming Data

