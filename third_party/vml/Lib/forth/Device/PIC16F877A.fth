\ PIC16F877A设备定义 - Forth文件
\ 生成自: Microchip/PIC/PIC16F877A
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
\ CPU架构: PIC16
\ 位宽: 8位
\ 时钟频率: 4000000 Hz

\ =========================================
\ PIC16F877A设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PIC16F877A" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" PIC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" PIC16" ;
8 CONSTANT BITS
4000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT W  \ Working Register
0x03 CONSTANT STATUS  \ Status Register
0 CONSTANT STATUS-C  \ Carry flag
1 CONSTANT STATUS-DC  \ Digit carry flag
2 CONSTANT STATUS-Z  \ Zero flag
3 CONSTANT STATUS-PD  \ Power-down flag
4 CONSTANT STATUS-TO  \ Time-out flag
5 CONSTANT STATUS-RP  \ Register bank select
7 CONSTANT STATUS-IRP  \ Indirect register bank select
0x0B CONSTANT INTCON  \ Interrupt Control Register
0 CONSTANT INTCON-RBIF  \ PORTB change interrupt flag
1 CONSTANT INTCON-INTF  \ External interrupt flag
2 CONSTANT INTCON-TMR0IF  \ TMR0 overflow interrupt flag
3 CONSTANT INTCON-RBIE  \ PORTB change interrupt enable
4 CONSTANT INTCON-INTE  \ External interrupt enable
5 CONSTANT INTCON-TMR0IE  \ TMR0 overflow interrupt enable
6 CONSTANT INTCON-PEIE  \ Peripheral interrupt enable
7 CONSTANT INTCON-GIE  \ Global interrupt enable
0x06 CONSTANT PORTB  \ PORT B
0x86 CONSTANT TRISB  \ TRIS B
0x07 CONSTANT PORTC  \ PORT C
0x87 CONSTANT TRISC  \ TRIS C
0x08 CONSTANT PORTD  \ PORT D
0x88 CONSTANT TRISD  \ TRIS D
0x09 CONSTANT PORTE  \ PORT E
0x89 CONSTANT TRISE  \ TRIS E
0x01 CONSTANT TMR0  \ Timer 0
0x81 CONSTANT OPTION_REG  \ Option Register
0x02 CONSTANT PCL  \ Program Counter Low
0x0A CONSTANT PCLATH  \ Program Counter Latch High
0x04 CONSTANT FSR  \ File Select Register
0x10C CONSTANT EEDATA  \ EEPROM Data
0x10D CONSTANT EEADR  \ EEPROM Address
0x18C CONSTANT EECON1  \ EEPROM Control 1
0 CONSTANT EECON1-RD  \ Read control
1 CONSTANT EECON1-WR  \ Write control
2 CONSTANT EECON1-WREN  \ Write enable
3 CONSTANT EECON1-WRERR  \ Write error flag
7 CONSTANT EECON1-EEPGD  \ EEPROM program/data select
0x18D CONSTANT EECON2  \ EEPROM Control 2
0x1E CONSTANT ADRESH  \ A/D Result High
0x1F CONSTANT ADRESL  \ A/D Result Low
0x1F CONSTANT ADCON0  \ A/D Control 0
0 CONSTANT ADCON0-ADON  \ A/D enable
2 CONSTANT ADCON0-GO_DONE  \ A/D conversion status
3 CONSTANT ADCON0-CHS  \ Channel select
0x9F CONSTANT ADCON1  \ A/D Control 1
0x94 CONSTANT SSPSTAT  \ MSSP Status
0x14 CONSTANT SSPCON  \ MSSP Control
0x13 CONSTANT SSPBUF  \ SSP Buffer
0x19 CONSTANT TXREG  \ USART Transmit Register
0x1A CONSTANT RCREG  \ USART Receive Register
0x99 CONSTANT SPBRG  \ Baud Rate Generator
0x98 CONSTANT TXSTA  \ TX Status and Control
0x18 CONSTANT RCSTA  \ RX Status and Control
0x17 CONSTANT CCP1CON  \ CCP1 Control
0x15 CONSTANT CCPR1L  \ CCP1 Low
0x16 CONSTANT CCPR1H  \ CCP1 High
0x1D CONSTANT CCP2CON  \ CCP2 Control
0x1B CONSTANT CCPR2L  \ CCP2 Low
0x1C CONSTANT CCPR2H  \ CCP2 High
0x10 CONSTANT T1CON  \ Timer 1 Control
0x0E CONSTANT TMR1L  \ Timer 1 Low
0x0F CONSTANT TMR1H  \ Timer 1 High
0x12 CONSTANT T2CON  \ Timer 2 Control
0x11 CONSTANT TMR2  \ Timer 2
0x92 CONSTANT PR2  \ Timer 2 Period

\ 内存段定义
0x0000 CONSTANT PROGRAM-START
0x1FFF CONSTANT PROGRAM-END
8192 CONSTANT PROGRAM-SIZE  \ Program Memory (8KB)
0x20 CONSTANT DATA-START
0x7F CONSTANT DATA-END
96 CONSTANT DATA-SIZE  \ General Purpose RAM Bank 0
0xA0 CONSTANT SRAM-START
0xFF CONSTANT SRAM-END
96 CONSTANT SRAM-SIZE  \ General Purpose RAM Bank 1
0x2100 CONSTANT EEPROM-START
0x21FF CONSTANT EEPROM-END
256 CONSTANT EEPROM-SIZE  \ EEPROM Data Memory

\ 外设定义
\ Port B
0x06 CONSTANT GPIO_PORTB-BASE
0x06 CONSTANT GPIO_PORTB-PORTB
0x86 CONSTANT GPIO_PORTB-TRISB
\ Port C
0x07 CONSTANT GPIO_PORTC-BASE
0x07 CONSTANT GPIO_PORTC-PORTC
0x87 CONSTANT GPIO_PORTC-TRISC
\ Port D
0x08 CONSTANT GPIO_PORTD-BASE
0x08 CONSTANT GPIO_PORTD-PORTD
0x88 CONSTANT GPIO_PORTD-TRISD
\ Timer 0
0x01 CONSTANT TIMER0-BASE
0x01 CONSTANT TIMER0-TMR0
0x81 CONSTANT TIMER0-OPTION_REG
\ Timer 1
0x0E CONSTANT TIMER1-BASE
0x10 CONSTANT TIMER1-T1CON
0x0E CONSTANT TIMER1-TMR1L
0x0F CONSTANT TIMER1-TMR1H
\ Timer 2
0x11 CONSTANT TIMER2-BASE
0x12 CONSTANT TIMER2-T2CON
0x11 CONSTANT TIMER2-TMR2
0x92 CONSTANT TIMER2-PR2
\ A/D Converter
0x1E CONSTANT ADC-BASE
0x1E CONSTANT ADC-ADRESH
0x9F CONSTANT ADC-ADRESL
0x1F CONSTANT ADC-ADCON0
0x9F CONSTANT ADC-ADCON1
\ Master Synchronous Serial Port
0x13 CONSTANT MSSP-BASE
0x94 CONSTANT MSSP-SSPSTAT
0x14 CONSTANT MSSP-SSPCON
0x13 CONSTANT MSSP-SSPBUF
\ USART
0x19 CONSTANT USART-BASE
0x19 CONSTANT USART-TXREG
0x1A CONSTANT USART-RCREG
0x99 CONSTANT USART-SPBRG
0x98 CONSTANT USART-TXSTA
0x18 CONSTANT USART-RCSTA
\ Capture/Compare/PWM 1
0x15 CONSTANT CCP1-BASE
0x17 CONSTANT CCP1-CCP1CON
0x15 CONSTANT CCP1-CCPR1L
0x16 CONSTANT CCP1-CCPR1H
\ Capture/Compare/PWM 2
0x1B CONSTANT CCP2-BASE
0x1D CONSTANT CCP2-CCP2CON
0x1B CONSTANT CCP2-CCPR2L
0x1C CONSTANT CCP2-CCPR2H

\ 中断向量定义
1 CONSTANT INT-INT  \ External Interrupt
2 CONSTANT INT-TMR0  \ Timer 0 Overflow
3 CONSTANT INT-RB  \ PORTB Change
4 CONSTANT INT-CCP1  \ CCP1
5 CONSTANT INT-CCP2  \ CCP2
6 CONSTANT INT-TMR1  \ Timer 1 Overflow
8 CONSTANT INT-TMR2  \ Timer 2 Overflow
9 CONSTANT INT-SPI  \ SPI/I2C
10 CONSTANT INT-SCI  \ USART Receive
11 CONSTANT INT-SCI  \ USART Transmit
12 CONSTANT INT-ADC  \ A/D Converter
13 CONSTANT INT-EEPROM  \ EEPROM Write Complete

\ 引脚定义
1 CONSTANT PIN-MCLR_VPP  \ Master Clear (Reset)
2 CONSTANT PIN-RA0_AN0  \ PORTA Bit 0 / Analog 0
3 CONSTANT PIN-RA1_AN1  \ PORTA Bit 1 / Analog 1
4 CONSTANT PIN-RA2_AN2_VREF  \ PORTA Bit 2 / Analog 2 / VREF-
5 CONSTANT PIN-RA3_AN3_VREFP  \ PORTA Bit 3 / Analog 3 / VREF+
6 CONSTANT PIN-RA4_T0CKI  \ PORTA Bit 4 / Timer 0 Clock Input
7 CONSTANT PIN-RA5_AN4_SS  \ PORTA Bit 4 / Analog 4 / SPI Slave Select
8 CONSTANT PIN-RE0_RD_AN5  \ PORTE Bit 0 / Read Control / Analog 5
9 CONSTANT PIN-RE1_WR_AN6  \ PORTE Bit 1 / Write Control / Analog 6
10 CONSTANT PIN-RE2_CS_AN7  \ PORTE Bit 2 / Chip Select / Analog 7
11 CONSTANT PIN-VDD  \ Positive Supply
12 CONSTANT PIN-VSS  \ Ground
13 CONSTANT PIN-OSC1_CLKIN  \ Oscillator/Clock Input
14 CONSTANT PIN-OSC2_CLKOUT  \ Oscillator/Clock Output
15 CONSTANT PIN-RC0_T1OSO  \ PORTC Bit 0 / Timer 1 Oscillator
16 CONSTANT PIN-RC1_T1OSI  \ PORTC Bit 1 / Timer 1 Oscillator
17 CONSTANT PIN-RC2_CCP1  \ PORTC Bit 2 / Capture/Compare/PWM 1
18 CONSTANT PIN-RC3_SCK_SCL  \ PORTC Bit 3 / SPI Clock / I2C Clock
23 CONSTANT PIN-RC4_SDI_SDA  \ PORTC Bit 4 / SPI Data In / I2C Data
24 CONSTANT PIN-RC5_SDO  \ PORTC Bit 5 / SPI Data Out
25 CONSTANT PIN-RC6_TX  \ PORTC Bit 6 / USART Transmit
26 CONSTANT PIN-RC7_RX  \ PORTC Bit 7 / USART Receive
19 CONSTANT PIN-RD0  \ PORTD Bit 0
20 CONSTANT PIN-RD1  \ PORTD Bit 1
21 CONSTANT PIN-RD2  \ PORTD Bit 2
22 CONSTANT PIN-RD3  \ PORTD Bit 3
27 CONSTANT PIN-RD4  \ PORTD Bit 4
28 CONSTANT PIN-RD5  \ PORTD Bit 5
29 CONSTANT PIN-RD6  \ PORTD Bit 6
30 CONSTANT PIN-RD7  \ PORTD Bit 7
31 CONSTANT PIN-VSS  \ Ground
32 CONSTANT PIN-VDD  \ Positive Supply
33 CONSTANT PIN-RB0_INT  \ PORTB Bit 0 / External Interrupt
34 CONSTANT PIN-RB1  \ PORTB Bit 1
35 CONSTANT PIN-RB2  \ PORTB Bit 2
36 CONSTANT PIN-RB3_PGC  \ PORTB Bit 3 / Programming Clock
37 CONSTANT PIN-RB4_PGD  \ PORTB Bit 4 / Programming Data
38 CONSTANT PIN-RB5  \ PORTB Bit 5
39 CONSTANT PIN-RB6_PGC  \ PORTB Bit 6 / Programming Clock
40 CONSTANT PIN-RB7_PGD  \ PORTB Bit 7 / Programming Data

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: W@ ( -- n ) W C@ ;
: W! ( n -- ) W C! ;

: STATUS@ ( -- n ) STATUS C@ ;
: STATUS! ( n -- ) STATUS C! ;
: STATUS-C@ ( -- flag ) STATUS@ 0 BIT@ ;
: STATUS-C! ( flag -- ) STATUS@ 0 BIT! STATUS! ;
: STATUS-C-SET ( -- ) TRUE STATUS-C! ;
: STATUS-C-CLR ( -- ) FALSE STATUS-C! ;
: STATUS-DC@ ( -- flag ) STATUS@ 1 BIT@ ;
: STATUS-DC! ( flag -- ) STATUS@ 1 BIT! STATUS! ;
: STATUS-DC-SET ( -- ) TRUE STATUS-DC! ;
: STATUS-DC-CLR ( -- ) FALSE STATUS-DC! ;
: STATUS-Z@ ( -- flag ) STATUS@ 2 BIT@ ;
: STATUS-Z! ( flag -- ) STATUS@ 2 BIT! STATUS! ;
: STATUS-Z-SET ( -- ) TRUE STATUS-Z! ;
: STATUS-Z-CLR ( -- ) FALSE STATUS-Z! ;
: STATUS-PD@ ( -- flag ) STATUS@ 3 BIT@ ;
: STATUS-PD! ( flag -- ) STATUS@ 3 BIT! STATUS! ;
: STATUS-PD-SET ( -- ) TRUE STATUS-PD! ;
: STATUS-PD-CLR ( -- ) FALSE STATUS-PD! ;
: STATUS-TO@ ( -- flag ) STATUS@ 4 BIT@ ;
: STATUS-TO! ( flag -- ) STATUS@ 4 BIT! STATUS! ;
: STATUS-TO-SET ( -- ) TRUE STATUS-TO! ;
: STATUS-TO-CLR ( -- ) FALSE STATUS-TO! ;
: STATUS-RP@ ( -- flag ) STATUS@ 5 BIT@ ;
: STATUS-RP! ( flag -- ) STATUS@ 5 BIT! STATUS! ;
: STATUS-RP-SET ( -- ) TRUE STATUS-RP! ;
: STATUS-RP-CLR ( -- ) FALSE STATUS-RP! ;
: STATUS-IRP@ ( -- flag ) STATUS@ 7 BIT@ ;
: STATUS-IRP! ( flag -- ) STATUS@ 7 BIT! STATUS! ;
: STATUS-IRP-SET ( -- ) TRUE STATUS-IRP! ;
: STATUS-IRP-CLR ( -- ) FALSE STATUS-IRP! ;

: INTCON@ ( -- n ) INTCON C@ ;
: INTCON! ( n -- ) INTCON C! ;
: INTCON-RBIF@ ( -- flag ) INTCON@ 0 BIT@ ;
: INTCON-RBIF! ( flag -- ) INTCON@ 0 BIT! INTCON! ;
: INTCON-RBIF-SET ( -- ) TRUE INTCON-RBIF! ;
: INTCON-RBIF-CLR ( -- ) FALSE INTCON-RBIF! ;
: INTCON-INTF@ ( -- flag ) INTCON@ 1 BIT@ ;
: INTCON-INTF! ( flag -- ) INTCON@ 1 BIT! INTCON! ;
: INTCON-INTF-SET ( -- ) TRUE INTCON-INTF! ;
: INTCON-INTF-CLR ( -- ) FALSE INTCON-INTF! ;
: INTCON-TMR0IF@ ( -- flag ) INTCON@ 2 BIT@ ;
: INTCON-TMR0IF! ( flag -- ) INTCON@ 2 BIT! INTCON! ;
: INTCON-TMR0IF-SET ( -- ) TRUE INTCON-TMR0IF! ;
: INTCON-TMR0IF-CLR ( -- ) FALSE INTCON-TMR0IF! ;
: INTCON-RBIE@ ( -- flag ) INTCON@ 3 BIT@ ;
: INTCON-RBIE! ( flag -- ) INTCON@ 3 BIT! INTCON! ;
: INTCON-RBIE-SET ( -- ) TRUE INTCON-RBIE! ;
: INTCON-RBIE-CLR ( -- ) FALSE INTCON-RBIE! ;
: INTCON-INTE@ ( -- flag ) INTCON@ 4 BIT@ ;
: INTCON-INTE! ( flag -- ) INTCON@ 4 BIT! INTCON! ;
: INTCON-INTE-SET ( -- ) TRUE INTCON-INTE! ;
: INTCON-INTE-CLR ( -- ) FALSE INTCON-INTE! ;
: INTCON-TMR0IE@ ( -- flag ) INTCON@ 5 BIT@ ;
: INTCON-TMR0IE! ( flag -- ) INTCON@ 5 BIT! INTCON! ;
: INTCON-TMR0IE-SET ( -- ) TRUE INTCON-TMR0IE! ;
: INTCON-TMR0IE-CLR ( -- ) FALSE INTCON-TMR0IE! ;
: INTCON-PEIE@ ( -- flag ) INTCON@ 6 BIT@ ;
: INTCON-PEIE! ( flag -- ) INTCON@ 6 BIT! INTCON! ;
: INTCON-PEIE-SET ( -- ) TRUE INTCON-PEIE! ;
: INTCON-PEIE-CLR ( -- ) FALSE INTCON-PEIE! ;
: INTCON-GIE@ ( -- flag ) INTCON@ 7 BIT@ ;
: INTCON-GIE! ( flag -- ) INTCON@ 7 BIT! INTCON! ;
: INTCON-GIE-SET ( -- ) TRUE INTCON-GIE! ;
: INTCON-GIE-CLR ( -- ) FALSE INTCON-GIE! ;

: PORTB@ ( -- n ) PORTB C@ ;
: PORTB! ( n -- ) PORTB C! ;

: TRISB@ ( -- n ) TRISB C@ ;
: TRISB! ( n -- ) TRISB C! ;

: PORTC@ ( -- n ) PORTC C@ ;
: PORTC! ( n -- ) PORTC C! ;

: TRISC@ ( -- n ) TRISC C@ ;
: TRISC! ( n -- ) TRISC C! ;

: PORTD@ ( -- n ) PORTD C@ ;
: PORTD! ( n -- ) PORTD C! ;

: TRISD@ ( -- n ) TRISD C@ ;
: TRISD! ( n -- ) TRISD C! ;

: PORTE@ ( -- n ) PORTE C@ ;
: PORTE! ( n -- ) PORTE C! ;

: TRISE@ ( -- n ) TRISE C@ ;
: TRISE! ( n -- ) TRISE C! ;

: TMR0@ ( -- n ) TMR0 C@ ;
: TMR0! ( n -- ) TMR0 C! ;

: OPTION_REG@ ( -- n ) OPTION_REG C@ ;
: OPTION_REG! ( n -- ) OPTION_REG C! ;

: PCL@ ( -- n ) PCL C@ ;
: PCL! ( n -- ) PCL C! ;

: PCLATH@ ( -- n ) PCLATH C@ ;
: PCLATH! ( n -- ) PCLATH C! ;

: FSR@ ( -- n ) FSR C@ ;
: FSR! ( n -- ) FSR C! ;

: EEDATA@ ( -- n ) EEDATA C@ ;
: EEDATA! ( n -- ) EEDATA C! ;

: EEADR@ ( -- n ) EEADR C@ ;
: EEADR! ( n -- ) EEADR C! ;

: EECON1@ ( -- n ) EECON1 C@ ;
: EECON1! ( n -- ) EECON1 C! ;
: EECON1-RD@ ( -- flag ) EECON1@ 0 BIT@ ;
: EECON1-RD! ( flag -- ) EECON1@ 0 BIT! EECON1! ;
: EECON1-RD-SET ( -- ) TRUE EECON1-RD! ;
: EECON1-RD-CLR ( -- ) FALSE EECON1-RD! ;
: EECON1-WR@ ( -- flag ) EECON1@ 1 BIT@ ;
: EECON1-WR! ( flag -- ) EECON1@ 1 BIT! EECON1! ;
: EECON1-WR-SET ( -- ) TRUE EECON1-WR! ;
: EECON1-WR-CLR ( -- ) FALSE EECON1-WR! ;
: EECON1-WREN@ ( -- flag ) EECON1@ 2 BIT@ ;
: EECON1-WREN! ( flag -- ) EECON1@ 2 BIT! EECON1! ;
: EECON1-WREN-SET ( -- ) TRUE EECON1-WREN! ;
: EECON1-WREN-CLR ( -- ) FALSE EECON1-WREN! ;
: EECON1-WRERR@ ( -- flag ) EECON1@ 3 BIT@ ;
: EECON1-WRERR! ( flag -- ) EECON1@ 3 BIT! EECON1! ;
: EECON1-WRERR-SET ( -- ) TRUE EECON1-WRERR! ;
: EECON1-WRERR-CLR ( -- ) FALSE EECON1-WRERR! ;
: EECON1-EEPGD@ ( -- flag ) EECON1@ 7 BIT@ ;
: EECON1-EEPGD! ( flag -- ) EECON1@ 7 BIT! EECON1! ;
: EECON1-EEPGD-SET ( -- ) TRUE EECON1-EEPGD! ;
: EECON1-EEPGD-CLR ( -- ) FALSE EECON1-EEPGD! ;

: EECON2@ ( -- n ) EECON2 C@ ;
: EECON2! ( n -- ) EECON2 C! ;

: ADRESH@ ( -- n ) ADRESH C@ ;
: ADRESH! ( n -- ) ADRESH C! ;

: ADRESL@ ( -- n ) ADRESL C@ ;
: ADRESL! ( n -- ) ADRESL C! ;

: ADCON0@ ( -- n ) ADCON0 C@ ;
: ADCON0! ( n -- ) ADCON0 C! ;
: ADCON0-ADON@ ( -- flag ) ADCON0@ 0 BIT@ ;
: ADCON0-ADON! ( flag -- ) ADCON0@ 0 BIT! ADCON0! ;
: ADCON0-ADON-SET ( -- ) TRUE ADCON0-ADON! ;
: ADCON0-ADON-CLR ( -- ) FALSE ADCON0-ADON! ;
: ADCON0-GO_DONE@ ( -- flag ) ADCON0@ 2 BIT@ ;
: ADCON0-GO_DONE! ( flag -- ) ADCON0@ 2 BIT! ADCON0! ;
: ADCON0-GO_DONE-SET ( -- ) TRUE ADCON0-GO_DONE! ;
: ADCON0-GO_DONE-CLR ( -- ) FALSE ADCON0-GO_DONE! ;
: ADCON0-CHS@ ( -- flag ) ADCON0@ 3 BIT@ ;
: ADCON0-CHS! ( flag -- ) ADCON0@ 3 BIT! ADCON0! ;
: ADCON0-CHS-SET ( -- ) TRUE ADCON0-CHS! ;
: ADCON0-CHS-CLR ( -- ) FALSE ADCON0-CHS! ;

: ADCON1@ ( -- n ) ADCON1 C@ ;
: ADCON1! ( n -- ) ADCON1 C! ;

: SSPSTAT@ ( -- n ) SSPSTAT C@ ;
: SSPSTAT! ( n -- ) SSPSTAT C! ;

: SSPCON@ ( -- n ) SSPCON C@ ;
: SSPCON! ( n -- ) SSPCON C! ;

: SSPBUF@ ( -- n ) SSPBUF C@ ;
: SSPBUF! ( n -- ) SSPBUF C! ;

: TXREG@ ( -- n ) TXREG C@ ;
: TXREG! ( n -- ) TXREG C! ;

: RCREG@ ( -- n ) RCREG C@ ;
: RCREG! ( n -- ) RCREG C! ;

: SPBRG@ ( -- n ) SPBRG C@ ;
: SPBRG! ( n -- ) SPBRG C! ;

: TXSTA@ ( -- n ) TXSTA C@ ;
: TXSTA! ( n -- ) TXSTA C! ;

: RCSTA@ ( -- n ) RCSTA C@ ;
: RCSTA! ( n -- ) RCSTA C! ;

: CCP1CON@ ( -- n ) CCP1CON C@ ;
: CCP1CON! ( n -- ) CCP1CON C! ;

: CCPR1L@ ( -- n ) CCPR1L C@ ;
: CCPR1L! ( n -- ) CCPR1L C! ;

: CCPR1H@ ( -- n ) CCPR1H C@ ;
: CCPR1H! ( n -- ) CCPR1H C! ;

: CCP2CON@ ( -- n ) CCP2CON C@ ;
: CCP2CON! ( n -- ) CCP2CON C! ;

: CCPR2L@ ( -- n ) CCPR2L C@ ;
: CCPR2L! ( n -- ) CCPR2L C! ;

: CCPR2H@ ( -- n ) CCPR2H C@ ;
: CCPR2H! ( n -- ) CCPR2H C! ;

: T1CON@ ( -- n ) T1CON C@ ;
: T1CON! ( n -- ) T1CON C! ;

: TMR1L@ ( -- n ) TMR1L C@ ;
: TMR1L! ( n -- ) TMR1L C! ;

: TMR1H@ ( -- n ) TMR1H C@ ;
: TMR1H! ( n -- ) TMR1H C! ;

: T2CON@ ( -- n ) T2CON C@ ;
: T2CON! ( n -- ) T2CON C! ;

: TMR2@ ( -- n ) TMR2 C@ ;
: TMR2! ( n -- ) TMR2 C! ;

: PR2@ ( -- n ) PR2 C@ ;
: PR2! ( n -- ) PR2 C! ;

\ 外设访问
\ GPIO_PORTB外设
: GPIO_PORTB-PORTB@ ( -- n ) GPIO_PORTB-PORTB C@ ;
: GPIO_PORTB-PORTB! ( n -- ) GPIO_PORTB-PORTB C! ;
: GPIO_PORTB-TRISB@ ( -- n ) GPIO_PORTB-TRISB C@ ;
: GPIO_PORTB-TRISB! ( n -- ) GPIO_PORTB-TRISB C! ;

\ GPIO_PORTC外设
: GPIO_PORTC-PORTC@ ( -- n ) GPIO_PORTC-PORTC C@ ;
: GPIO_PORTC-PORTC! ( n -- ) GPIO_PORTC-PORTC C! ;
: GPIO_PORTC-TRISC@ ( -- n ) GPIO_PORTC-TRISC C@ ;
: GPIO_PORTC-TRISC! ( n -- ) GPIO_PORTC-TRISC C! ;

\ GPIO_PORTD外设
: GPIO_PORTD-PORTD@ ( -- n ) GPIO_PORTD-PORTD C@ ;
: GPIO_PORTD-PORTD! ( n -- ) GPIO_PORTD-PORTD C! ;
: GPIO_PORTD-TRISD@ ( -- n ) GPIO_PORTD-TRISD C@ ;
: GPIO_PORTD-TRISD! ( n -- ) GPIO_PORTD-TRISD C! ;

\ TIMER0外设
: TIMER0-TMR0@ ( -- n ) TIMER0-TMR0 C@ ;
: TIMER0-TMR0! ( n -- ) TIMER0-TMR0 C! ;
: TIMER0-OPTION_REG@ ( -- n ) TIMER0-OPTION_REG C@ ;
: TIMER0-OPTION_REG! ( n -- ) TIMER0-OPTION_REG C! ;

\ TIMER1外设
: TIMER1-T1CON@ ( -- n ) TIMER1-T1CON C@ ;
: TIMER1-T1CON! ( n -- ) TIMER1-T1CON C! ;
: TIMER1-TMR1L@ ( -- n ) TIMER1-TMR1L C@ ;
: TIMER1-TMR1L! ( n -- ) TIMER1-TMR1L C! ;
: TIMER1-TMR1H@ ( -- n ) TIMER1-TMR1H C@ ;
: TIMER1-TMR1H! ( n -- ) TIMER1-TMR1H C! ;

\ TIMER2外设
: TIMER2-T2CON@ ( -- n ) TIMER2-T2CON C@ ;
: TIMER2-T2CON! ( n -- ) TIMER2-T2CON C! ;
: TIMER2-TMR2@ ( -- n ) TIMER2-TMR2 C@ ;
: TIMER2-TMR2! ( n -- ) TIMER2-TMR2 C! ;
: TIMER2-PR2@ ( -- n ) TIMER2-PR2 C@ ;
: TIMER2-PR2! ( n -- ) TIMER2-PR2 C! ;

\ ADC外设
: ADC-ADRESH@ ( -- n ) ADC-ADRESH C@ ;
: ADC-ADRESH! ( n -- ) ADC-ADRESH C! ;
: ADC-ADRESL@ ( -- n ) ADC-ADRESL C@ ;
: ADC-ADRESL! ( n -- ) ADC-ADRESL C! ;
: ADC-ADCON0@ ( -- n ) ADC-ADCON0 C@ ;
: ADC-ADCON0! ( n -- ) ADC-ADCON0 C! ;
: ADC-ADCON1@ ( -- n ) ADC-ADCON1 C@ ;
: ADC-ADCON1! ( n -- ) ADC-ADCON1 C! ;

\ MSSP外设
: MSSP-SSPSTAT@ ( -- n ) MSSP-SSPSTAT C@ ;
: MSSP-SSPSTAT! ( n -- ) MSSP-SSPSTAT C! ;
: MSSP-SSPCON@ ( -- n ) MSSP-SSPCON C@ ;
: MSSP-SSPCON! ( n -- ) MSSP-SSPCON C! ;
: MSSP-SSPBUF@ ( -- n ) MSSP-SSPBUF C@ ;
: MSSP-SSPBUF! ( n -- ) MSSP-SSPBUF C! ;

\ USART外设
: USART-TXREG@ ( -- n ) USART-TXREG C@ ;
: USART-TXREG! ( n -- ) USART-TXREG C! ;
: USART-RCREG@ ( -- n ) USART-RCREG C@ ;
: USART-RCREG! ( n -- ) USART-RCREG C! ;
: USART-SPBRG@ ( -- n ) USART-SPBRG C@ ;
: USART-SPBRG! ( n -- ) USART-SPBRG C! ;
: USART-TXSTA@ ( -- n ) USART-TXSTA C@ ;
: USART-TXSTA! ( n -- ) USART-TXSTA C! ;
: USART-RCSTA@ ( -- n ) USART-RCSTA C@ ;
: USART-RCSTA! ( n -- ) USART-RCSTA C! ;

\ CCP1外设
: CCP1-CCP1CON@ ( -- n ) CCP1-CCP1CON C@ ;
: CCP1-CCP1CON! ( n -- ) CCP1-CCP1CON C! ;
: CCP1-CCPR1L@ ( -- n ) CCP1-CCPR1L C@ ;
: CCP1-CCPR1L! ( n -- ) CCP1-CCPR1L C! ;
: CCP1-CCPR1H@ ( -- n ) CCP1-CCPR1H C@ ;
: CCP1-CCPR1H! ( n -- ) CCP1-CCPR1H C! ;

\ CCP2外设
: CCP2-CCP2CON@ ( -- n ) CCP2-CCP2CON C@ ;
: CCP2-CCP2CON! ( n -- ) CCP2-CCP2CON C! ;
: CCP2-CCPR2L@ ( -- n ) CCP2-CCPR2L C@ ;
: CCP2-CCPR2L! ( n -- ) CCP2-CCPR2L C! ;
: CCP2-CCPR2H@ ( -- n ) CCP2-CCPR2H C@ ;
: CCP2-CCPR2H! ( n -- ) CCP2-CCPR2H C! ;

\ =========================================
\ 设备初始化
\ =========================================

: PIC16F877A-INIT ( -- )
  \ 初始化PIC16F877A设备
  ." 初始化PIC16F877A..." CR

  \ 初始化寄存器
  0 W!  \ Working Register
  0 STATUS!  \ Status Register
  0 INTCON!  \ Interrupt Control Register
  0 PORTB!  \ PORT B
  0 TRISB!  \ TRIS B
  0 PORTC!  \ PORT C
  0 TRISC!  \ TRIS C
  0 PORTD!  \ PORT D
  0 TRISD!  \ TRIS D
  0 PORTE!  \ PORT E
  0 TRISE!  \ TRIS E
  0 TMR0!  \ Timer 0
  0 OPTION_REG!  \ Option Register
  0 PCL!  \ Program Counter Low
  0 PCLATH!  \ Program Counter Latch High
  0 FSR!  \ File Select Register
  0 EEDATA!  \ EEPROM Data
  0 EEADR!  \ EEPROM Address
  0 EECON1!  \ EEPROM Control 1
  0 EECON2!  \ EEPROM Control 2
  0 ADRESH!  \ A/D Result High
  0 ADRESL!  \ A/D Result Low
  0 ADCON0!  \ A/D Control 0
  0 ADCON1!  \ A/D Control 1
  0 SSPSTAT!  \ MSSP Status
  0 SSPCON!  \ MSSP Control
  0 SSPBUF!  \ SSP Buffer
  0 TXREG!  \ USART Transmit Register
  0 RCREG!  \ USART Receive Register
  0 SPBRG!  \ Baud Rate Generator
  0 TXSTA!  \ TX Status and Control
  0 RCSTA!  \ RX Status and Control
  0 CCP1CON!  \ CCP1 Control
  0 CCPR1L!  \ CCP1 Low
  0 CCPR1H!  \ CCP1 High
  0 CCP2CON!  \ CCP2 Control
  0 CCPR2L!  \ CCP2 Low
  0 CCPR2H!  \ CCP2 High
  0 T1CON!  \ Timer 1 Control
  0 TMR1L!  \ Timer 1 Low
  0 TMR1H!  \ Timer 1 High
  0 T2CON!  \ Timer 2 Control
  0 TMR2!  \ Timer 2
  0 PR2!  \ Timer 2 Period

  \ 初始化外设
  \ 初始化GPIO_PORTB
  0 GPIO_PORTB-PORTB!  \ PORTB寄存器
  0 GPIO_PORTB-TRISB!  \ TRISB寄存器
  \ 初始化GPIO_PORTC
  0 GPIO_PORTC-PORTC!  \ PORTC寄存器
  0 GPIO_PORTC-TRISC!  \ TRISC寄存器
  \ 初始化GPIO_PORTD
  0 GPIO_PORTD-PORTD!  \ PORTD寄存器
  0 GPIO_PORTD-TRISD!  \ TRISD寄存器
  \ 初始化TIMER0
  0 TIMER0-TMR0!  \ TMR0寄存器
  0 TIMER0-OPTION_REG!  \ OPTION_REG寄存器
  \ 初始化TIMER1
  0 TIMER1-T1CON!  \ T1CON寄存器
  0 TIMER1-TMR1L!  \ TMR1L寄存器
  0 TIMER1-TMR1H!  \ TMR1H寄存器
  \ 初始化TIMER2
  0 TIMER2-T2CON!  \ T2CON寄存器
  0 TIMER2-TMR2!  \ TMR2寄存器
  0 TIMER2-PR2!  \ PR2寄存器
  \ 初始化ADC
  0 ADC-ADRESH!  \ ADRESH寄存器
  0 ADC-ADRESL!  \ ADRESL寄存器
  0 ADC-ADCON0!  \ ADCON0寄存器
  0 ADC-ADCON1!  \ ADCON1寄存器
  \ 初始化MSSP
  0 MSSP-SSPSTAT!  \ SSPSTAT寄存器
  0 MSSP-SSPCON!  \ SSPCON寄存器
  0 MSSP-SSPBUF!  \ SSPBUF寄存器
  \ 初始化USART
  0 USART-TXREG!  \ TXREG寄存器
  0 USART-RCREG!  \ RCREG寄存器
  0 USART-SPBRG!  \ SPBRG寄存器
  0 USART-TXSTA!  \ TXSTA寄存器
  0 USART-RCSTA!  \ RCSTA寄存器
  \ 初始化CCP1
  0 CCP1-CCP1CON!  \ CCP1CON寄存器
  0 CCP1-CCPR1L!  \ CCPR1L寄存器
  0 CCP1-CCPR1H!  \ CCPR1H寄存器
  \ 初始化CCP2
  0 CCP2-CCP2CON!  \ CCP2CON寄存器
  0 CCP2-CCPR2L!  \ CCPR2L寄存器
  0 CCP2-CCPR2H!  \ CCPR2H寄存器

  ." PIC16F877A初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  W@ W .R 8 .R SPACE ."  W: " W@ .
  STATUS@ STATUS .R 8 .R SPACE ."  STATUS: " STATUS@ .
  INTCON@ INTCON .R 8 .R SPACE ."  INTCON: " INTCON@ .
  PORTB@ PORTB .R 8 .R SPACE ."  PORTB: " PORTB@ .
  TRISB@ TRISB .R 8 .R SPACE ."  TRISB: " TRISB@ .
  PORTC@ PORTC .R 8 .R SPACE ."  PORTC: " PORTC@ .
  TRISC@ TRISC .R 8 .R SPACE ."  TRISC: " TRISC@ .
  PORTD@ PORTD .R 8 .R SPACE ."  PORTD: " PORTD@ .
  TRISD@ TRISD .R 8 .R SPACE ."  TRISD: " TRISD@ .
  PORTE@ PORTE .R 8 .R SPACE ."  PORTE: " PORTE@ .
  TRISE@ TRISE .R 8 .R SPACE ."  TRISE: " TRISE@ .
  TMR0@ TMR0 .R 8 .R SPACE ."  TMR0: " TMR0@ .
  OPTION_REG@ OPTION_REG .R 8 .R SPACE ."  OPTION_REG: " OPTION_REG@ .
  PCL@ PCL .R 8 .R SPACE ."  PCL: " PCL@ .
  PCLATH@ PCLATH .R 8 .R SPACE ."  PCLATH: " PCLATH@ .
  FSR@ FSR .R 8 .R SPACE ."  FSR: " FSR@ .
  EEDATA@ EEDATA .R 8 .R SPACE ."  EEDATA: " EEDATA@ .
  EEADR@ EEADR .R 8 .R SPACE ."  EEADR: " EEADR@ .
  EECON1@ EECON1 .R 8 .R SPACE ."  EECON1: " EECON1@ .
  EECON2@ EECON2 .R 8 .R SPACE ."  EECON2: " EECON2@ .
  ADRESH@ ADRESH .R 8 .R SPACE ."  ADRESH: " ADRESH@ .
  ADRESL@ ADRESL .R 8 .R SPACE ."  ADRESL: " ADRESL@ .
  ADCON0@ ADCON0 .R 8 .R SPACE ."  ADCON0: " ADCON0@ .
  ADCON1@ ADCON1 .R 8 .R SPACE ."  ADCON1: " ADCON1@ .
  SSPSTAT@ SSPSTAT .R 8 .R SPACE ."  SSPSTAT: " SSPSTAT@ .
  SSPCON@ SSPCON .R 8 .R SPACE ."  SSPCON: " SSPCON@ .
  SSPBUF@ SSPBUF .R 8 .R SPACE ."  SSPBUF: " SSPBUF@ .
  TXREG@ TXREG .R 8 .R SPACE ."  TXREG: " TXREG@ .
  RCREG@ RCREG .R 8 .R SPACE ."  RCREG: " RCREG@ .
  SPBRG@ SPBRG .R 8 .R SPACE ."  SPBRG: " SPBRG@ .
  TXSTA@ TXSTA .R 8 .R SPACE ."  TXSTA: " TXSTA@ .
  RCSTA@ RCSTA .R 8 .R SPACE ."  RCSTA: " RCSTA@ .
  CCP1CON@ CCP1CON .R 8 .R SPACE ."  CCP1CON: " CCP1CON@ .
  CCPR1L@ CCPR1L .R 8 .R SPACE ."  CCPR1L: " CCPR1L@ .
  CCPR1H@ CCPR1H .R 8 .R SPACE ."  CCPR1H: " CCPR1H@ .
  CCP2CON@ CCP2CON .R 8 .R SPACE ."  CCP2CON: " CCP2CON@ .
  CCPR2L@ CCPR2L .R 8 .R SPACE ."  CCPR2L: " CCPR2L@ .
  CCPR2H@ CCPR2H .R 8 .R SPACE ."  CCPR2H: " CCPR2H@ .
  T1CON@ T1CON .R 8 .R SPACE ."  T1CON: " T1CON@ .
  TMR1L@ TMR1L .R 8 .R SPACE ."  TMR1L: " TMR1L@ .
  TMR1H@ TMR1H .R 8 .R SPACE ."  TMR1H: " TMR1H@ .
  T2CON@ T2CON .R 8 .R SPACE ."  T2CON: " T2CON@ .
  TMR2@ TMR2 .R 8 .R SPACE ."  TMR2: " TMR2@ .
  PR2@ PR2 .R 8 .R SPACE ."  PR2: " PR2@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ External Interrupt
: INT-INT-HANDLER ( -- )
  ." INT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT-ENABLE ( -- )
  INT-INT INT-ENABLE
;

: INT-INT-DISABLE ( -- )
  INT-INT INT-DISABLE
;

\ Timer 0 Overflow
: INT-TMR0-HANDLER ( -- )
  ." TMR0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TMR0-ENABLE ( -- )
  INT-TMR0 INT-ENABLE
;

: INT-TMR0-DISABLE ( -- )
  INT-TMR0 INT-DISABLE
;

\ PORTB Change
: INT-RB-HANDLER ( -- )
  ." RB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RB-ENABLE ( -- )
  INT-RB INT-ENABLE
;

: INT-RB-DISABLE ( -- )
  INT-RB INT-DISABLE
;

\ CCP1
: INT-CCP1-HANDLER ( -- )
  ." CCP1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-CCP1-ENABLE ( -- )
  INT-CCP1 INT-ENABLE
;

: INT-CCP1-DISABLE ( -- )
  INT-CCP1 INT-DISABLE
;

\ CCP2
: INT-CCP2-HANDLER ( -- )
  ." CCP2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-CCP2-ENABLE ( -- )
  INT-CCP2 INT-ENABLE
;

: INT-CCP2-DISABLE ( -- )
  INT-CCP2 INT-DISABLE
;

\ Timer 1 Overflow
: INT-TMR1-HANDLER ( -- )
  ." TMR1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TMR1-ENABLE ( -- )
  INT-TMR1 INT-ENABLE
;

: INT-TMR1-DISABLE ( -- )
  INT-TMR1 INT-DISABLE
;

\ Timer 2 Overflow
: INT-TMR2-HANDLER ( -- )
  ." TMR2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TMR2-ENABLE ( -- )
  INT-TMR2 INT-ENABLE
;

: INT-TMR2-DISABLE ( -- )
  INT-TMR2 INT-DISABLE
;

\ SPI/I2C
: INT-SPI-HANDLER ( -- )
  ." SPI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPI-ENABLE ( -- )
  INT-SPI INT-ENABLE
;

: INT-SPI-DISABLE ( -- )
  INT-SPI INT-DISABLE
;

\ USART Receive
: INT-SCI-HANDLER ( -- )
  ." SCI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SCI-ENABLE ( -- )
  INT-SCI INT-ENABLE
;

: INT-SCI-DISABLE ( -- )
  INT-SCI INT-DISABLE
;

\ USART Transmit
: INT-SCI-HANDLER ( -- )
  ." SCI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SCI-ENABLE ( -- )
  INT-SCI INT-ENABLE
;

: INT-SCI-DISABLE ( -- )
  INT-SCI INT-DISABLE
;

\ A/D Converter
: INT-ADC-HANDLER ( -- )
  ." ADC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-ADC-ENABLE ( -- )
  INT-ADC INT-ENABLE
;

: INT-ADC-DISABLE ( -- )
  INT-ADC INT-DISABLE
;

\ EEPROM Write Complete
: INT-EEPROM-HANDLER ( -- )
  ." EEPROM中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EEPROM-ENABLE ( -- )
  INT-EEPROM INT-ENABLE
;

: INT-EEPROM-DISABLE ( -- )
  INT-EEPROM INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  PIC16F877A-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
