' PIC16F877A寄存器定义
' 生成自: Microchip/PIC/PIC16F877A
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM

' CPU架构: PIC16
' 位宽: 8位
' 时钟频率: 4000000 Hz

' 寄存器定义
' Working Register
CONST W = 0x00

' Status Register
CONST STATUS = 0x03
CONST STATUS_C = 0  ' Carry flag
CONST STATUS_DC = 1  ' Digit carry flag
CONST STATUS_Z = 2  ' Zero flag
CONST STATUS_PD = 3  ' Power-down flag
CONST STATUS_TO = 4  ' Time-out flag
CONST STATUS_RP = 5  ' Register bank select
CONST STATUS_IRP = 7  ' Indirect register bank select

' Interrupt Control Register
CONST INTCON = 0x0B
CONST INTCON_RBIF = 0  ' PORTB change interrupt flag
CONST INTCON_INTF = 1  ' External interrupt flag
CONST INTCON_TMR0IF = 2  ' TMR0 overflow interrupt flag
CONST INTCON_RBIE = 3  ' PORTB change interrupt enable
CONST INTCON_INTE = 4  ' External interrupt enable
CONST INTCON_TMR0IE = 5  ' TMR0 overflow interrupt enable
CONST INTCON_PEIE = 6  ' Peripheral interrupt enable
CONST INTCON_GIE = 7  ' Global interrupt enable

' PORT B
CONST PORTB = 0x06

' TRIS B
CONST TRISB = 0x86

' PORT C
CONST PORTC = 0x07

' TRIS C
CONST TRISC = 0x87

' PORT D
CONST PORTD = 0x08

' TRIS D
CONST TRISD = 0x88

' PORT E
CONST PORTE = 0x09

' TRIS E
CONST TRISE = 0x89

' Timer 0
CONST TMR0 = 0x01

' Option Register
CONST OPTION_REG = 0x81

' Program Counter Low
CONST PCL = 0x02

' Program Counter Latch High
CONST PCLATH = 0x0A

' File Select Register
CONST FSR = 0x04

' EEPROM Data
CONST EEDATA = 0x10C

' EEPROM Address
CONST EEADR = 0x10D

' EEPROM Control 1
CONST EECON1 = 0x18C
CONST EECON1_RD = 0  ' Read control
CONST EECON1_WR = 1  ' Write control
CONST EECON1_WREN = 2  ' Write enable
CONST EECON1_WRERR = 3  ' Write error flag
CONST EECON1_EEPGD = 7  ' EEPROM program/data select

' EEPROM Control 2
CONST EECON2 = 0x18D

' A/D Result High
CONST ADRESH = 0x1E

' A/D Result Low
CONST ADRESL = 0x1F

' A/D Control 0
CONST ADCON0 = 0x1F
CONST ADCON0_ADON = 0  ' A/D enable
CONST ADCON0_GO_DONE = 2  ' A/D conversion status
CONST ADCON0_CHS = 3  ' Channel select

' A/D Control 1
CONST ADCON1 = 0x9F

' MSSP Status
CONST SSPSTAT = 0x94

' MSSP Control
CONST SSPCON = 0x14

' SSP Buffer
CONST SSPBUF = 0x13

' USART Transmit Register
CONST TXREG = 0x19

' USART Receive Register
CONST RCREG = 0x1A

' Baud Rate Generator
CONST SPBRG = 0x99

' TX Status and Control
CONST TXSTA = 0x98

' RX Status and Control
CONST RCSTA = 0x18

' CCP1 Control
CONST CCP1CON = 0x17

' CCP1 Low
CONST CCPR1L = 0x15

' CCP1 High
CONST CCPR1H = 0x16

' CCP2 Control
CONST CCP2CON = 0x1D

' CCP2 Low
CONST CCPR2L = 0x1B

' CCP2 High
CONST CCPR2H = 0x1C

' Timer 1 Control
CONST T1CON = 0x10

' Timer 1 Low
CONST TMR1L = 0x0E

' Timer 1 High
CONST TMR1H = 0x0F

' Timer 2 Control
CONST T2CON = 0x12

' Timer 2
CONST TMR2 = 0x11

' Timer 2 Period
CONST PR2 = 0x92

' 内存段定义
' Program Memory (8KB)
CONST PROGRAM_START = 0x0000
CONST PROGRAM_END = 0x1FFF
CONST PROGRAM_SIZE = 8192

' General Purpose RAM Bank 0
CONST DATA_START = 0x20
CONST DATA_END = 0x7F
CONST DATA_SIZE = 96

' General Purpose RAM Bank 1
CONST SRAM_START = 0xA0
CONST SRAM_END = 0xFF
CONST SRAM_SIZE = 96

' EEPROM Data Memory
CONST EEPROM_START = 0x2100
CONST EEPROM_END = 0x21FF
CONST EEPROM_SIZE = 256

' 外设定义
' Port B
CONST GPIO_PORTB_BASE = 0x06
CONST GPIO_PORTB_PORTB = 0x06
CONST GPIO_PORTB_TRISB = 0x86

' Port C
CONST GPIO_PORTC_BASE = 0x07
CONST GPIO_PORTC_PORTC = 0x07
CONST GPIO_PORTC_TRISC = 0x87

' Port D
CONST GPIO_PORTD_BASE = 0x08
CONST GPIO_PORTD_PORTD = 0x08
CONST GPIO_PORTD_TRISD = 0x88

' Timer 0
CONST TIMER0_BASE = 0x01
CONST TIMER0_TMR0 = 0x01
CONST TIMER0_OPTION_REG = 0x81

' Timer 1
CONST TIMER1_BASE = 0x0E
CONST TIMER1_T1CON = 0x10
CONST TIMER1_TMR1L = 0x0E
CONST TIMER1_TMR1H = 0x0F

' Timer 2
CONST TIMER2_BASE = 0x11
CONST TIMER2_T2CON = 0x12
CONST TIMER2_TMR2 = 0x11
CONST TIMER2_PR2 = 0x92

' A/D Converter
CONST ADC_BASE = 0x1E
CONST ADC_ADRESH = 0x1E
CONST ADC_ADRESL = 0x9F
CONST ADC_ADCON0 = 0x1F
CONST ADC_ADCON1 = 0x9F

' Master Synchronous Serial Port
CONST MSSP_BASE = 0x13
CONST MSSP_SSPSTAT = 0x94
CONST MSSP_SSPCON = 0x14
CONST MSSP_SSPBUF = 0x13

' USART
CONST USART_BASE = 0x19
CONST USART_TXREG = 0x19
CONST USART_RCREG = 0x1A
CONST USART_SPBRG = 0x99
CONST USART_TXSTA = 0x98
CONST USART_RCSTA = 0x18

' Capture/Compare/PWM 1
CONST CCP1_BASE = 0x15
CONST CCP1_CCP1CON = 0x17
CONST CCP1_CCPR1L = 0x15
CONST CCP1_CCPR1H = 0x16

' Capture/Compare/PWM 2
CONST CCP2_BASE = 0x1B
CONST CCP2_CCP2CON = 0x1D
CONST CCP2_CCPR2L = 0x1B
CONST CCP2_CCPR2H = 0x1C

' 中断向量定义
CONST INT_VECTOR = 1  ' External Interrupt
CONST TMR0_VECTOR = 2  ' Timer 0 Overflow
CONST RB_VECTOR = 3  ' PORTB Change
CONST CCP1_VECTOR = 4  ' CCP1
CONST CCP2_VECTOR = 5  ' CCP2
CONST TMR1_VECTOR = 6  ' Timer 1 Overflow
CONST TMR2_VECTOR = 8  ' Timer 2 Overflow
CONST SPI_VECTOR = 9  ' SPI/I2C
CONST SCI_VECTOR = 10  ' USART Receive
CONST SCI_VECTOR = 11  ' USART Transmit
CONST ADC_VECTOR = 12  ' A/D Converter
CONST EEPROM_VECTOR = 13  ' EEPROM Write Complete

' 引脚定义
CONST PIN_MCLR_VPP = 1  ' Master Clear (Reset)
CONST PIN_RA0_AN0 = 2  ' PORTA Bit 0 / Analog 0
CONST PIN_RA1_AN1 = 3  ' PORTA Bit 1 / Analog 1
CONST PIN_RA2_AN2_VREF = 4  ' PORTA Bit 2 / Analog 2 / VREF-
CONST PIN_RA3_AN3_VREFP = 5  ' PORTA Bit 3 / Analog 3 / VREF+
CONST PIN_RA4_T0CKI = 6  ' PORTA Bit 4 / Timer 0 Clock Input
CONST PIN_RA5_AN4_SS = 7  ' PORTA Bit 4 / Analog 4 / SPI Slave Select
CONST PIN_RE0_RD_AN5 = 8  ' PORTE Bit 0 / Read Control / Analog 5
CONST PIN_RE1_WR_AN6 = 9  ' PORTE Bit 1 / Write Control / Analog 6
CONST PIN_RE2_CS_AN7 = 10  ' PORTE Bit 2 / Chip Select / Analog 7
CONST PIN_VDD = 11  ' Positive Supply
CONST PIN_VSS = 12  ' Ground
CONST PIN_OSC1_CLKIN = 13  ' Oscillator/Clock Input
CONST PIN_OSC2_CLKOUT = 14  ' Oscillator/Clock Output
CONST PIN_RC0_T1OSO = 15  ' PORTC Bit 0 / Timer 1 Oscillator
CONST PIN_RC1_T1OSI = 16  ' PORTC Bit 1 / Timer 1 Oscillator
CONST PIN_RC2_CCP1 = 17  ' PORTC Bit 2 / Capture/Compare/PWM 1
CONST PIN_RC3_SCK_SCL = 18  ' PORTC Bit 3 / SPI Clock / I2C Clock
CONST PIN_RC4_SDI_SDA = 23  ' PORTC Bit 4 / SPI Data In / I2C Data
CONST PIN_RC5_SDO = 24  ' PORTC Bit 5 / SPI Data Out
CONST PIN_RC6_TX = 25  ' PORTC Bit 6 / USART Transmit
CONST PIN_RC7_RX = 26  ' PORTC Bit 7 / USART Receive
CONST PIN_RD0 = 19  ' PORTD Bit 0
CONST PIN_RD1 = 20  ' PORTD Bit 1
CONST PIN_RD2 = 21  ' PORTD Bit 2
CONST PIN_RD3 = 22  ' PORTD Bit 3
CONST PIN_RD4 = 27  ' PORTD Bit 4
CONST PIN_RD5 = 28  ' PORTD Bit 5
CONST PIN_RD6 = 29  ' PORTD Bit 6
CONST PIN_RD7 = 30  ' PORTD Bit 7
CONST PIN_VSS = 31  ' Ground
CONST PIN_VDD = 32  ' Positive Supply
CONST PIN_RB0_INT = 33  ' PORTB Bit 0 / External Interrupt
CONST PIN_RB1 = 34  ' PORTB Bit 1
CONST PIN_RB2 = 35  ' PORTB Bit 2
CONST PIN_RB3_PGC = 36  ' PORTB Bit 3 / Programming Clock
CONST PIN_RB4_PGD = 37  ' PORTB Bit 4 / Programming Data
CONST PIN_RB5 = 38  ' PORTB Bit 5
CONST PIN_RB6_PGC = 39  ' PORTB Bit 6 / Programming Clock
CONST PIN_RB7_PGD = 40  ' PORTB Bit 7 / Programming Data

' 设备初始化子程序
SUB pic16f877a_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
