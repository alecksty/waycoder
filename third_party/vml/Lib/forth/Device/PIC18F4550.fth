\ PIC18F4550设备定义 - Forth文件
\ 生成自: Microchip/PIC18/PIC18F4550
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
\ CPU架构: PIC18
\ 位宽: 8位
\ 时钟频率: 20000000 Hz

\ =========================================
\ PIC18F4550设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PIC18F4550" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" PIC18" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" PIC18" ;
8 CONSTANT BITS
20000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x0E CONSTANT W  \ Working Register
0xFD8 CONSTANT STATUS  \ Status Register
0 CONSTANT STATUS-C  \ Carry Flag
1 CONSTANT STATUS-DC  \ Digit Carry Flag
2 CONSTANT STATUS-Z  \ Zero Flag
3 CONSTANT STATUS-PD  \ Power-Down Flag
4 CONSTANT STATUS-TO  \ Time-out Flag
0 CONSTANT STATUS-RP  \ Register Bank Select
7 CONSTANT STATUS-IRP  \ Indirect Register Bank Select
0xFE0 CONSTANT BSR  \ Bank Select Register
0xF80 CONSTANT PORTA  \ Port A
0xF81 CONSTANT PORTB  \ Port B
0xF82 CONSTANT PORTC  \ Port C
0xF83 CONSTANT PORTD  \ Port D
0xF84 CONSTANT PORTE  \ Port E
0xF92 CONSTANT TRISA  \ Tri-state Port A
0xF93 CONSTANT TRISB  \ Tri-state Port B
0xF94 CONSTANT TRISC  \ Tri-state Port C
0xF95 CONSTANT TRISD  \ Tri-state Port D
0xF96 CONSTANT TRISE  \ Tri-state Port E
0xF89 CONSTANT LATA  \ Latch Port A
0xF8A CONSTANT LATB  \ Latch Port B
0xF8B CONSTANT LATC  \ Latch Port C
0xF8C CONSTANT LATD  \ Latch Port D
0xF8D CONSTANT LATE  \ Latch Port E
0xFF2 CONSTANT INTCON  \ Interrupt Control
0 CONSTANT INTCON-RBIF  \ Port B Interrupt Flag
1 CONSTANT INTCON-INT0IF  \ INT0 Interrupt Flag
2 CONSTANT INTCON-TMR0IF  \ Timer 0 Interrupt Flag
3 CONSTANT INTCON-RBIE  \ Port B Interrupt Enable
4 CONSTANT INTCON-INT0IE  \ INT0 Interrupt Enable
5 CONSTANT INTCON-TMR0IE  \ Timer 0 Interrupt Enable
6 CONSTANT INTCON-PEIE  \ Peripheral Interrupt Enable
7 CONSTANT INTCON-GIE  \ Global Interrupt Enable
0xF9E CONSTANT PIR1  \ Peripheral Interrupt 1
0xF9F CONSTANT PIR2  \ Peripheral Interrupt 2
0xF9D CONSTANT PIE1  \ Peripheral Interrupt Enable 1
0xF9C CONSTANT PIE2  \ Peripheral Interrupt Enable 2
0xF9B CONSTANT IPR1  \ Interrupt Priority 1
0xF9A CONSTANT IPR2  \ Interrupt Priority 2
0xFD0 CONSTANT RCON  \ Reset Control
3 CONSTANT RCON-NOT_TO  \ Time-out Flag
4 CONSTANT RCON-NOT_PD  \ Power-Down Flag
5 CONSTANT RCON-NOT_RI  \ RESET Flag
6 CONSTANT RCON-NOT_POR  \ Power-on Reset Flag
7 CONSTANT RCON-NOT_BOR  \ Brown-out Reset Flag
0xFD1 CONSTANT T0CON  \ Timer 0 Control
0xFD6 CONSTANT TMR0  \ Timer 0 Register
0xFCD CONSTANT T1CON  \ Timer 1 Control
0xFCE CONSTANT TMR1  \ Timer 1 Register High
0xFCF CONSTANT TMR1L  \ Timer 1 Register Low
0xFCA CONSTANT T2CON  \ Timer 2 Control
0xFCB CONSTANT TMR2  \ Timer 2 Register
0xFB1 CONSTANT T3CON  \ Timer 3 Control
0xFB3 CONSTANT TMR3  \ Timer 3 Register High
0xFB2 CONSTANT TMR3L  \ Timer 3 Register Low
0xFC6 CONSTANT SSPCON1  \ SSP Control 1
0xFC5 CONSTANT SSPCON2  \ SSP Control 2
0xFC7 CONSTANT SSPSTAT  \ SSP Status
0xFC9 CONSTANT SSPBUF  \ SSP Buffer
0xFC8 CONSTANT SSPOR  \ SSP Shift Register
0xFC2 CONSTANT ADCON0  \ A/D Control 0
0xFC1 CONSTANT ADCON1  \ A/D Control 1
0xFC0 CONSTANT ADCON2  \ A/D Control 2
0xFC3 CONSTANT ADRES  \ A/D Result
0xFC4 CONSTANT ADRESL  \ A/D Result Low
0xFD4 CONSTANT CCP1CON  \ CCP 1 Control
0xFD6 CONSTANT CCPR1  \ CCP 1 Register High
0xFD5 CONSTANT CCPR1L  \ CCP 1 Register Low
0xFBA CONSTANT CCP2CON  \ CCP 2 Control
0xFBB CONSTANT CCPR2  \ CCP 2 Register High
0xFBC CONSTANT CCPR2L  \ CCP 2 Register Low
0xF75 CONSTANT USBCON  \ USB Control
0xF74 CONSTANT USBSTAT  \ USB Status
0xF73 CONSTANT UIE  \ USB Interrupt Enable
0xF72 CONSTANT UIR  \ USB Interrupt Flag
0xF71 CONSTANT UCON  \ USB Control
0xF70 CONSTANT USTAT  \ USB Status
0xF60 CONSTANT UEP0  \ USB Endpoint 0
0xF61 CONSTANT UEP1  \ USB Endpoint 1
0xF62 CONSTANT UEP2  \ USB Endpoint 2
0xF63 CONSTANT UEP3  \ USB Endpoint 3
0xF64 CONSTANT UEP4  \ USB Endpoint 4

\ 内存段定义
0x0000 CONSTANT FLASH-START
0x7FFF CONSTANT FLASH-END
32768 CONSTANT FLASH-SIZE  \ Program Flash (32KB)
0xF00000 CONSTANT EEPROM-START
0xF000FF CONSTANT EEPROM-END
256 CONSTANT EEPROM-SIZE  \ EEPROM (256B)
0x0000 CONSTANT SRAM-START
0x07FF CONSTANT SRAM-END
2048 CONSTANT SRAM-SIZE  \ SRAM (2KB)
0x0000 CONSTANT ACCESS-START
 CONSTANT ACCESS-END
1 CONSTANT ACCESS-SIZE  \ Access Bank

\ 外设定义
\ Port A
0xF80 CONSTANT PORTA-BASE
0xF80 CONSTANT PORTA-PORT
0xF92 CONSTANT PORTA-TRIS
0xF89 CONSTANT PORTA-LAT
\ Port B
0xF81 CONSTANT PORTB-BASE
0xF81 CONSTANT PORTB-PORT
0xF93 CONSTANT PORTB-TRIS
0xF8A CONSTANT PORTB-LAT
\ Port C
0xF82 CONSTANT PORTC-BASE
0xF82 CONSTANT PORTC-PORT
0xF94 CONSTANT PORTC-TRIS
0xF8B CONSTANT PORTC-LAT
\ Port D
0xF83 CONSTANT PORTD-BASE
0xF83 CONSTANT PORTD-PORT
0xF95 CONSTANT PORTD-TRIS
0xF8C CONSTANT PORTD-LAT
\ Port E
0xF84 CONSTANT PORTE-BASE
0xF84 CONSTANT PORTE-PORT
0xF96 CONSTANT PORTE-TRIS
0xF8D CONSTANT PORTE-LAT
\ Timer 0
0xFD1 CONSTANT TIMER0-BASE
0xFD1 CONSTANT TIMER0-T0CON
0xFD6 CONSTANT TIMER0-TMR0
\ Timer 1
0xFCD CONSTANT TIMER1-BASE
0xFCD CONSTANT TIMER1-T1CON
0xFCF CONSTANT TIMER1-TMR1
0xFCE CONSTANT TIMER1-TMR1L
\ Timer 2
0xFCA CONSTANT TIMER2-BASE
0xFCA CONSTANT TIMER2-T2CON
0xFCB CONSTANT TIMER2-TMR2
\ Timer 3
0xFB0 CONSTANT TIMER3-BASE
0xFB0 CONSTANT TIMER3-T3CON
0xFB2 CONSTANT TIMER3-TMR3
\ A/D Converter
0xFC2 CONSTANT ADC-BASE
0xFC2 CONSTANT ADC-ADCON0
0xFC1 CONSTANT ADC-ADCON1
0xFC0 CONSTANT ADC-ADCON2
0xFC3 CONSTANT ADC-ADRES
0xFC4 CONSTANT ADC-ADRESL
\ CCP 1
0xFD4 CONSTANT CCP1-BASE
0xFD4 CONSTANT CCP1-CCP1CON
0xFD6 CONSTANT CCP1-CCPR1
0xFD5 CONSTANT CCP1-CCPR1L
\ CCP 2
0xFBA CONSTANT CCP2-BASE
0xFBA CONSTANT CCP2-CCP2CON
0xFBB CONSTANT CCP2-CCPR2
0xFBC CONSTANT CCP2-CCPR2L
\ SSP (I2C/SPI)
0xFC6 CONSTANT SSP-BASE
0xFC6 CONSTANT SSP-SSPCON1
0xFC5 CONSTANT SSP-SSPCON2
0xFC7 CONSTANT SSP-SSPSTAT
0xFC9 CONSTANT SSP-SSPBUF
0xFC8 CONSTANT SSP-SSPOV
\ EUSART
0xF15 CONSTANT EUSART-BASE
0xFE2 CONSTANT EUSART-TXSTA
0xFE3 CONSTANT EUSART-RCSTA
0xFAD CONSTANT EUSART-TXREG
0xFAE CONSTANT EUSART-RCREG
0xFAF CONSTANT EUSART-SPBRG
0xFB0 CONSTANT EUSART-SPBRGH
0xFB8 CONSTANT EUSART-BAUDCON
\ Comparators
0xFB4 CONSTANT COMPARATOR-BASE
0xFB4 CONSTANT COMPARATOR-CMCON
0xFB5 CONSTANT COMPARATOR-CVRCON
\ USB Module
0xF70 CONSTANT USB-BASE
0xF71 CONSTANT USB-UCON
0xF72 CONSTANT USB-USTAT
0xF73 CONSTANT USB-UIR
0xF74 CONSTANT USB-UIE
0xF80 CONSTANT USB-UEP0
0xF81 CONSTANT USB-UEP1
0xF82 CONSTANT USB-UEP2
0xF83 CONSTANT USB-UEP3
0xF00 CONSTANT USB-BD0
0xF08 CONSTANT USB-BD1
0xF10 CONSTANT USB-BD2
0xF18 CONSTANT USB-BD3
\ Oscillator
0xFD3 CONSTANT OSCCON-BASE
0xFD3 CONSTANT OSCCON-OSCCON
0xFD9 CONSTANT OSCCON-OSCTUNE
\ Watchdog Timer
0xFD1 CONSTANT WDTCON-BASE
0xFD1 CONSTANT WDTCON-WDTCON

\ 中断向量定义
0 CONSTANT INT-RESET  \ RESET
1 CONSTANT INT-INT0  \ External Interrupt 0
2 CONSTANT INT-INT1  \ External Interrupt 1
3 CONSTANT INT-INT2  \ External Interrupt 2
4 CONSTANT INT-TMR0  \ Timer 0 Overflow
5 CONSTANT INT-TMR1  \ Timer 1 Overflow
6 CONSTANT INT-TMR2  \ Timer 2 Match
7 CONSTANT INT-TMR3  \ Timer 3 Overflow
8 CONSTANT INT-CCP1  \ CCP 1
9 CONSTANT INT-CCP2  \ CCP 2
10 CONSTANT INT-SSP  \ SSP
11 CONSTANT INT-TX  \ USART TX
12 CONSTANT INT-RC  \ USART RX
13 CONSTANT INT-ADC  \ A/D
14 CONSTANT INT-RBO  \ Port B Change
15 CONSTANT INT-EXT  \ External

\ 引脚定义
1 CONSTANT PIN-RE3  \ MCLR/VPP/RE3
2 CONSTANT PIN-RA0  \ AN0/RA0
3 CONSTANT PIN-RA1  \ AN1/RA1
4 CONSTANT PIN-RA2  \ AN2/VREF-/RA2
5 CONSTANT PIN-RA3  \ AN3/VREF+/RA3
6 CONSTANT PIN-RA4  \ AN4/T0CKI/RA4
7 CONSTANT PIN-RA5  \ AN5/RE5
8 CONSTANT PIN-VSS  \ Ground
9 CONSTANT PIN-RA7  \ OSC1/CLKI/RA7
10 CONSTANT PIN-RA6  \ OSC2/CLKO/RA6
11 CONSTANT PIN-RC0  \ T1OSO/T1CKI/RC0
12 CONSTANT PIN-RC1  \ T1OSI/RC1
13 CONSTANT PIN-RC2  \ CCP1/RC2
14 CONSTANT PIN-RC3  \ SCK/SCL/RC3
15 CONSTANT PIN-RD0  \ SDO/RD0
16 CONSTANT PIN-RD1  \ SDI/RD1
17 CONSTANT PIN-RD2  \ RD2
18 CONSTANT PIN-RC6  \ TX/CK/RC6
19 CONSTANT PIN-RC7  \ RX/DT/RC7
20 CONSTANT PIN-VSS  \ Ground
21 CONSTANT PIN-RD3  \ RD3
22 CONSTANT PIN-RD4  \ RD4
23 CONSTANT PIN-RD5  \ PWRB/RD5
24 CONSTANT PIN-RD6  \ PBC/RD6
25 CONSTANT PIN-RD7  \ PCD/RD7
26 CONSTANT PIN-RC4  \ D-/RC4
27 CONSTANT PIN-RC5  \ D+/RC5
28 CONSTANT PIN-RE0  \ AN5/RE0
29 CONSTANT PIN-RE1  \ AN6/RE1
30 CONSTANT PIN-RE2  \ AN7/RE2
31 CONSTANT PIN-VSS  \ Ground
32 CONSTANT PIN-VDD  \ Vdd

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
: STATUS-RP@ ( -- flag ) STATUS@ 0 BIT@ ;
: STATUS-RP! ( flag -- ) STATUS@ 0 BIT! STATUS! ;
: STATUS-RP-SET ( -- ) TRUE STATUS-RP! ;
: STATUS-RP-CLR ( -- ) FALSE STATUS-RP! ;
: STATUS-IRP@ ( -- flag ) STATUS@ 7 BIT@ ;
: STATUS-IRP! ( flag -- ) STATUS@ 7 BIT! STATUS! ;
: STATUS-IRP-SET ( -- ) TRUE STATUS-IRP! ;
: STATUS-IRP-CLR ( -- ) FALSE STATUS-IRP! ;

: BSR@ ( -- n ) BSR C@ ;
: BSR! ( n -- ) BSR C! ;

: PORTA@ ( -- n ) PORTA C@ ;
: PORTA! ( n -- ) PORTA C! ;

: PORTB@ ( -- n ) PORTB C@ ;
: PORTB! ( n -- ) PORTB C! ;

: PORTC@ ( -- n ) PORTC C@ ;
: PORTC! ( n -- ) PORTC C! ;

: PORTD@ ( -- n ) PORTD C@ ;
: PORTD! ( n -- ) PORTD C! ;

: PORTE@ ( -- n ) PORTE C@ ;
: PORTE! ( n -- ) PORTE C! ;

: TRISA@ ( -- n ) TRISA C@ ;
: TRISA! ( n -- ) TRISA C! ;

: TRISB@ ( -- n ) TRISB C@ ;
: TRISB! ( n -- ) TRISB C! ;

: TRISC@ ( -- n ) TRISC C@ ;
: TRISC! ( n -- ) TRISC C! ;

: TRISD@ ( -- n ) TRISD C@ ;
: TRISD! ( n -- ) TRISD C! ;

: TRISE@ ( -- n ) TRISE C@ ;
: TRISE! ( n -- ) TRISE C! ;

: LATA@ ( -- n ) LATA C@ ;
: LATA! ( n -- ) LATA C! ;

: LATB@ ( -- n ) LATB C@ ;
: LATB! ( n -- ) LATB C! ;

: LATC@ ( -- n ) LATC C@ ;
: LATC! ( n -- ) LATC C! ;

: LATD@ ( -- n ) LATD C@ ;
: LATD! ( n -- ) LATD C! ;

: LATE@ ( -- n ) LATE C@ ;
: LATE! ( n -- ) LATE C! ;

: INTCON@ ( -- n ) INTCON C@ ;
: INTCON! ( n -- ) INTCON C! ;
: INTCON-RBIF@ ( -- flag ) INTCON@ 0 BIT@ ;
: INTCON-RBIF! ( flag -- ) INTCON@ 0 BIT! INTCON! ;
: INTCON-RBIF-SET ( -- ) TRUE INTCON-RBIF! ;
: INTCON-RBIF-CLR ( -- ) FALSE INTCON-RBIF! ;
: INTCON-INT0IF@ ( -- flag ) INTCON@ 1 BIT@ ;
: INTCON-INT0IF! ( flag -- ) INTCON@ 1 BIT! INTCON! ;
: INTCON-INT0IF-SET ( -- ) TRUE INTCON-INT0IF! ;
: INTCON-INT0IF-CLR ( -- ) FALSE INTCON-INT0IF! ;
: INTCON-TMR0IF@ ( -- flag ) INTCON@ 2 BIT@ ;
: INTCON-TMR0IF! ( flag -- ) INTCON@ 2 BIT! INTCON! ;
: INTCON-TMR0IF-SET ( -- ) TRUE INTCON-TMR0IF! ;
: INTCON-TMR0IF-CLR ( -- ) FALSE INTCON-TMR0IF! ;
: INTCON-RBIE@ ( -- flag ) INTCON@ 3 BIT@ ;
: INTCON-RBIE! ( flag -- ) INTCON@ 3 BIT! INTCON! ;
: INTCON-RBIE-SET ( -- ) TRUE INTCON-RBIE! ;
: INTCON-RBIE-CLR ( -- ) FALSE INTCON-RBIE! ;
: INTCON-INT0IE@ ( -- flag ) INTCON@ 4 BIT@ ;
: INTCON-INT0IE! ( flag -- ) INTCON@ 4 BIT! INTCON! ;
: INTCON-INT0IE-SET ( -- ) TRUE INTCON-INT0IE! ;
: INTCON-INT0IE-CLR ( -- ) FALSE INTCON-INT0IE! ;
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

: PIR1@ ( -- n ) PIR1 C@ ;
: PIR1! ( n -- ) PIR1 C! ;

: PIR2@ ( -- n ) PIR2 C@ ;
: PIR2! ( n -- ) PIR2 C! ;

: PIE1@ ( -- n ) PIE1 C@ ;
: PIE1! ( n -- ) PIE1 C! ;

: PIE2@ ( -- n ) PIE2 C@ ;
: PIE2! ( n -- ) PIE2 C! ;

: IPR1@ ( -- n ) IPR1 C@ ;
: IPR1! ( n -- ) IPR1 C! ;

: IPR2@ ( -- n ) IPR2 C@ ;
: IPR2! ( n -- ) IPR2 C! ;

: RCON@ ( -- n ) RCON C@ ;
: RCON! ( n -- ) RCON C! ;
: RCON-NOT_TO@ ( -- flag ) RCON@ 3 BIT@ ;
: RCON-NOT_TO! ( flag -- ) RCON@ 3 BIT! RCON! ;
: RCON-NOT_TO-SET ( -- ) TRUE RCON-NOT_TO! ;
: RCON-NOT_TO-CLR ( -- ) FALSE RCON-NOT_TO! ;
: RCON-NOT_PD@ ( -- flag ) RCON@ 4 BIT@ ;
: RCON-NOT_PD! ( flag -- ) RCON@ 4 BIT! RCON! ;
: RCON-NOT_PD-SET ( -- ) TRUE RCON-NOT_PD! ;
: RCON-NOT_PD-CLR ( -- ) FALSE RCON-NOT_PD! ;
: RCON-NOT_RI@ ( -- flag ) RCON@ 5 BIT@ ;
: RCON-NOT_RI! ( flag -- ) RCON@ 5 BIT! RCON! ;
: RCON-NOT_RI-SET ( -- ) TRUE RCON-NOT_RI! ;
: RCON-NOT_RI-CLR ( -- ) FALSE RCON-NOT_RI! ;
: RCON-NOT_POR@ ( -- flag ) RCON@ 6 BIT@ ;
: RCON-NOT_POR! ( flag -- ) RCON@ 6 BIT! RCON! ;
: RCON-NOT_POR-SET ( -- ) TRUE RCON-NOT_POR! ;
: RCON-NOT_POR-CLR ( -- ) FALSE RCON-NOT_POR! ;
: RCON-NOT_BOR@ ( -- flag ) RCON@ 7 BIT@ ;
: RCON-NOT_BOR! ( flag -- ) RCON@ 7 BIT! RCON! ;
: RCON-NOT_BOR-SET ( -- ) TRUE RCON-NOT_BOR! ;
: RCON-NOT_BOR-CLR ( -- ) FALSE RCON-NOT_BOR! ;

: T0CON@ ( -- n ) T0CON C@ ;
: T0CON! ( n -- ) T0CON C! ;

: TMR0@ ( -- n ) TMR0 C@ ;
: TMR0! ( n -- ) TMR0 C! ;

: T1CON@ ( -- n ) T1CON C@ ;
: T1CON! ( n -- ) T1CON C! ;

: TMR1@ ( -- n ) TMR1 C@ ;
: TMR1! ( n -- ) TMR1 C! ;

: TMR1L@ ( -- n ) TMR1L C@ ;
: TMR1L! ( n -- ) TMR1L C! ;

: T2CON@ ( -- n ) T2CON C@ ;
: T2CON! ( n -- ) T2CON C! ;

: TMR2@ ( -- n ) TMR2 C@ ;
: TMR2! ( n -- ) TMR2 C! ;

: T3CON@ ( -- n ) T3CON C@ ;
: T3CON! ( n -- ) T3CON C! ;

: TMR3@ ( -- n ) TMR3 C@ ;
: TMR3! ( n -- ) TMR3 C! ;

: TMR3L@ ( -- n ) TMR3L C@ ;
: TMR3L! ( n -- ) TMR3L C! ;

: SSPCON1@ ( -- n ) SSPCON1 C@ ;
: SSPCON1! ( n -- ) SSPCON1 C! ;

: SSPCON2@ ( -- n ) SSPCON2 C@ ;
: SSPCON2! ( n -- ) SSPCON2 C! ;

: SSPSTAT@ ( -- n ) SSPSTAT C@ ;
: SSPSTAT! ( n -- ) SSPSTAT C! ;

: SSPBUF@ ( -- n ) SSPBUF C@ ;
: SSPBUF! ( n -- ) SSPBUF C! ;

: SSPOR@ ( -- n ) SSPOR C@ ;
: SSPOR! ( n -- ) SSPOR C! ;

: ADCON0@ ( -- n ) ADCON0 C@ ;
: ADCON0! ( n -- ) ADCON0 C! ;

: ADCON1@ ( -- n ) ADCON1 C@ ;
: ADCON1! ( n -- ) ADCON1 C! ;

: ADCON2@ ( -- n ) ADCON2 C@ ;
: ADCON2! ( n -- ) ADCON2 C! ;

: ADRES@ ( -- n ) ADRES C@ ;
: ADRES! ( n -- ) ADRES C! ;

: ADRESL@ ( -- n ) ADRESL C@ ;
: ADRESL! ( n -- ) ADRESL C! ;

: CCP1CON@ ( -- n ) CCP1CON C@ ;
: CCP1CON! ( n -- ) CCP1CON C! ;

: CCPR1@ ( -- n ) CCPR1 C@ ;
: CCPR1! ( n -- ) CCPR1 C! ;

: CCPR1L@ ( -- n ) CCPR1L C@ ;
: CCPR1L! ( n -- ) CCPR1L C! ;

: CCP2CON@ ( -- n ) CCP2CON C@ ;
: CCP2CON! ( n -- ) CCP2CON C! ;

: CCPR2@ ( -- n ) CCPR2 C@ ;
: CCPR2! ( n -- ) CCPR2 C! ;

: CCPR2L@ ( -- n ) CCPR2L C@ ;
: CCPR2L! ( n -- ) CCPR2L C! ;

: USBCON@ ( -- n ) USBCON C@ ;
: USBCON! ( n -- ) USBCON C! ;

: USBSTAT@ ( -- n ) USBSTAT C@ ;
: USBSTAT! ( n -- ) USBSTAT C! ;

: UIE@ ( -- n ) UIE C@ ;
: UIE! ( n -- ) UIE C! ;

: UIR@ ( -- n ) UIR C@ ;
: UIR! ( n -- ) UIR C! ;

: UCON@ ( -- n ) UCON C@ ;
: UCON! ( n -- ) UCON C! ;

: USTAT@ ( -- n ) USTAT C@ ;
: USTAT! ( n -- ) USTAT C! ;

: UEP0@ ( -- n ) UEP0 C@ ;
: UEP0! ( n -- ) UEP0 C! ;

: UEP1@ ( -- n ) UEP1 C@ ;
: UEP1! ( n -- ) UEP1 C! ;

: UEP2@ ( -- n ) UEP2 C@ ;
: UEP2! ( n -- ) UEP2 C! ;

: UEP3@ ( -- n ) UEP3 C@ ;
: UEP3! ( n -- ) UEP3 C! ;

: UEP4@ ( -- n ) UEP4 C@ ;
: UEP4! ( n -- ) UEP4 C! ;

\ 外设访问
\ PORTA外设
: PORTA-PORT@ ( -- n ) PORTA-PORT C@ ;
: PORTA-PORT! ( n -- ) PORTA-PORT C! ;
: PORTA-TRIS@ ( -- n ) PORTA-TRIS C@ ;
: PORTA-TRIS! ( n -- ) PORTA-TRIS C! ;
: PORTA-LAT@ ( -- n ) PORTA-LAT C@ ;
: PORTA-LAT! ( n -- ) PORTA-LAT C! ;

\ PORTB外设
: PORTB-PORT@ ( -- n ) PORTB-PORT C@ ;
: PORTB-PORT! ( n -- ) PORTB-PORT C! ;
: PORTB-TRIS@ ( -- n ) PORTB-TRIS C@ ;
: PORTB-TRIS! ( n -- ) PORTB-TRIS C! ;
: PORTB-LAT@ ( -- n ) PORTB-LAT C@ ;
: PORTB-LAT! ( n -- ) PORTB-LAT C! ;

\ PORTC外设
: PORTC-PORT@ ( -- n ) PORTC-PORT C@ ;
: PORTC-PORT! ( n -- ) PORTC-PORT C! ;
: PORTC-TRIS@ ( -- n ) PORTC-TRIS C@ ;
: PORTC-TRIS! ( n -- ) PORTC-TRIS C! ;
: PORTC-LAT@ ( -- n ) PORTC-LAT C@ ;
: PORTC-LAT! ( n -- ) PORTC-LAT C! ;

\ PORTD外设
: PORTD-PORT@ ( -- n ) PORTD-PORT C@ ;
: PORTD-PORT! ( n -- ) PORTD-PORT C! ;
: PORTD-TRIS@ ( -- n ) PORTD-TRIS C@ ;
: PORTD-TRIS! ( n -- ) PORTD-TRIS C! ;
: PORTD-LAT@ ( -- n ) PORTD-LAT C@ ;
: PORTD-LAT! ( n -- ) PORTD-LAT C! ;

\ PORTE外设
: PORTE-PORT@ ( -- n ) PORTE-PORT C@ ;
: PORTE-PORT! ( n -- ) PORTE-PORT C! ;
: PORTE-TRIS@ ( -- n ) PORTE-TRIS C@ ;
: PORTE-TRIS! ( n -- ) PORTE-TRIS C! ;
: PORTE-LAT@ ( -- n ) PORTE-LAT C@ ;
: PORTE-LAT! ( n -- ) PORTE-LAT C! ;

\ TIMER0外设
: TIMER0-T0CON@ ( -- n ) TIMER0-T0CON C@ ;
: TIMER0-T0CON! ( n -- ) TIMER0-T0CON C! ;
: TIMER0-TMR0@ ( -- n ) TIMER0-TMR0 C@ ;
: TIMER0-TMR0! ( n -- ) TIMER0-TMR0 C! ;

\ TIMER1外设
: TIMER1-T1CON@ ( -- n ) TIMER1-T1CON C@ ;
: TIMER1-T1CON! ( n -- ) TIMER1-T1CON C! ;
: TIMER1-TMR1@ ( -- n ) TIMER1-TMR1 C@ ;
: TIMER1-TMR1! ( n -- ) TIMER1-TMR1 C! ;
: TIMER1-TMR1L@ ( -- n ) TIMER1-TMR1L C@ ;
: TIMER1-TMR1L! ( n -- ) TIMER1-TMR1L C! ;

\ TIMER2外设
: TIMER2-T2CON@ ( -- n ) TIMER2-T2CON C@ ;
: TIMER2-T2CON! ( n -- ) TIMER2-T2CON C! ;
: TIMER2-TMR2@ ( -- n ) TIMER2-TMR2 C@ ;
: TIMER2-TMR2! ( n -- ) TIMER2-TMR2 C! ;

\ TIMER3外设
: TIMER3-T3CON@ ( -- n ) TIMER3-T3CON C@ ;
: TIMER3-T3CON! ( n -- ) TIMER3-T3CON C! ;
: TIMER3-TMR3@ ( -- n ) TIMER3-TMR3 C@ ;
: TIMER3-TMR3! ( n -- ) TIMER3-TMR3 C! ;

\ ADC外设
: ADC-ADCON0@ ( -- n ) ADC-ADCON0 C@ ;
: ADC-ADCON0! ( n -- ) ADC-ADCON0 C! ;
: ADC-ADCON1@ ( -- n ) ADC-ADCON1 C@ ;
: ADC-ADCON1! ( n -- ) ADC-ADCON1 C! ;
: ADC-ADCON2@ ( -- n ) ADC-ADCON2 C@ ;
: ADC-ADCON2! ( n -- ) ADC-ADCON2 C! ;
: ADC-ADRES@ ( -- n ) ADC-ADRES C@ ;
: ADC-ADRES! ( n -- ) ADC-ADRES C! ;
: ADC-ADRESL@ ( -- n ) ADC-ADRESL C@ ;
: ADC-ADRESL! ( n -- ) ADC-ADRESL C! ;

\ CCP1外设
: CCP1-CCP1CON@ ( -- n ) CCP1-CCP1CON C@ ;
: CCP1-CCP1CON! ( n -- ) CCP1-CCP1CON C! ;
: CCP1-CCPR1@ ( -- n ) CCP1-CCPR1 C@ ;
: CCP1-CCPR1! ( n -- ) CCP1-CCPR1 C! ;
: CCP1-CCPR1L@ ( -- n ) CCP1-CCPR1L C@ ;
: CCP1-CCPR1L! ( n -- ) CCP1-CCPR1L C! ;

\ CCP2外设
: CCP2-CCP2CON@ ( -- n ) CCP2-CCP2CON C@ ;
: CCP2-CCP2CON! ( n -- ) CCP2-CCP2CON C! ;
: CCP2-CCPR2@ ( -- n ) CCP2-CCPR2 C@ ;
: CCP2-CCPR2! ( n -- ) CCP2-CCPR2 C! ;
: CCP2-CCPR2L@ ( -- n ) CCP2-CCPR2L C@ ;
: CCP2-CCPR2L! ( n -- ) CCP2-CCPR2L C! ;

\ SSP外设
: SSP-SSPCON1@ ( -- n ) SSP-SSPCON1 C@ ;
: SSP-SSPCON1! ( n -- ) SSP-SSPCON1 C! ;
: SSP-SSPCON2@ ( -- n ) SSP-SSPCON2 C@ ;
: SSP-SSPCON2! ( n -- ) SSP-SSPCON2 C! ;
: SSP-SSPSTAT@ ( -- n ) SSP-SSPSTAT C@ ;
: SSP-SSPSTAT! ( n -- ) SSP-SSPSTAT C! ;
: SSP-SSPBUF@ ( -- n ) SSP-SSPBUF C@ ;
: SSP-SSPBUF! ( n -- ) SSP-SSPBUF C! ;
: SSP-SSPOV@ ( -- n ) SSP-SSPOV C@ ;
: SSP-SSPOV! ( n -- ) SSP-SSPOV C! ;

\ EUSART外设
: EUSART-TXSTA@ ( -- n ) EUSART-TXSTA C@ ;
: EUSART-TXSTA! ( n -- ) EUSART-TXSTA C! ;
: EUSART-RCSTA@ ( -- n ) EUSART-RCSTA C@ ;
: EUSART-RCSTA! ( n -- ) EUSART-RCSTA C! ;
: EUSART-TXREG@ ( -- n ) EUSART-TXREG C@ ;
: EUSART-TXREG! ( n -- ) EUSART-TXREG C! ;
: EUSART-RCREG@ ( -- n ) EUSART-RCREG C@ ;
: EUSART-RCREG! ( n -- ) EUSART-RCREG C! ;
: EUSART-SPBRG@ ( -- n ) EUSART-SPBRG C@ ;
: EUSART-SPBRG! ( n -- ) EUSART-SPBRG C! ;
: EUSART-SPBRGH@ ( -- n ) EUSART-SPBRGH C@ ;
: EUSART-SPBRGH! ( n -- ) EUSART-SPBRGH C! ;
: EUSART-BAUDCON@ ( -- n ) EUSART-BAUDCON C@ ;
: EUSART-BAUDCON! ( n -- ) EUSART-BAUDCON C! ;

\ COMPARATOR外设
: COMPARATOR-CMCON@ ( -- n ) COMPARATOR-CMCON C@ ;
: COMPARATOR-CMCON! ( n -- ) COMPARATOR-CMCON C! ;
: COMPARATOR-CVRCON@ ( -- n ) COMPARATOR-CVRCON C@ ;
: COMPARATOR-CVRCON! ( n -- ) COMPARATOR-CVRCON C! ;

\ USB外设
: USB-UCON@ ( -- n ) USB-UCON C@ ;
: USB-UCON! ( n -- ) USB-UCON C! ;
: USB-USTAT@ ( -- n ) USB-USTAT C@ ;
: USB-USTAT! ( n -- ) USB-USTAT C! ;
: USB-UIR@ ( -- n ) USB-UIR C@ ;
: USB-UIR! ( n -- ) USB-UIR C! ;
: USB-UIE@ ( -- n ) USB-UIE C@ ;
: USB-UIE! ( n -- ) USB-UIE C! ;
: USB-UEP0@ ( -- n ) USB-UEP0 C@ ;
: USB-UEP0! ( n -- ) USB-UEP0 C! ;
: USB-UEP1@ ( -- n ) USB-UEP1 C@ ;
: USB-UEP1! ( n -- ) USB-UEP1 C! ;
: USB-UEP2@ ( -- n ) USB-UEP2 C@ ;
: USB-UEP2! ( n -- ) USB-UEP2 C! ;
: USB-UEP3@ ( -- n ) USB-UEP3 C@ ;
: USB-UEP3! ( n -- ) USB-UEP3 C! ;
: USB-BD0@ ( -- n ) USB-BD0 C@ ;
: USB-BD0! ( n -- ) USB-BD0 C! ;
: USB-BD1@ ( -- n ) USB-BD1 C@ ;
: USB-BD1! ( n -- ) USB-BD1 C! ;
: USB-BD2@ ( -- n ) USB-BD2 C@ ;
: USB-BD2! ( n -- ) USB-BD2 C! ;
: USB-BD3@ ( -- n ) USB-BD3 C@ ;
: USB-BD3! ( n -- ) USB-BD3 C! ;

\ OSCCON外设
: OSCCON-OSCCON@ ( -- n ) OSCCON-OSCCON C@ ;
: OSCCON-OSCCON! ( n -- ) OSCCON-OSCCON C! ;
: OSCCON-OSCTUNE@ ( -- n ) OSCCON-OSCTUNE C@ ;
: OSCCON-OSCTUNE! ( n -- ) OSCCON-OSCTUNE C! ;

\ WDTCON外设
: WDTCON-WDTCON@ ( -- n ) WDTCON-WDTCON C@ ;
: WDTCON-WDTCON! ( n -- ) WDTCON-WDTCON C! ;

\ =========================================
\ 设备初始化
\ =========================================

: PIC18F4550-INIT ( -- )
  \ 初始化PIC18F4550设备
  ." 初始化PIC18F4550..." CR

  \ 初始化寄存器
  0 W!  \ Working Register
  0 STATUS!  \ Status Register
  0 BSR!  \ Bank Select Register
  0 PORTA!  \ Port A
  0 PORTB!  \ Port B
  0 PORTC!  \ Port C
  0 PORTD!  \ Port D
  0 PORTE!  \ Port E
  0 TRISA!  \ Tri-state Port A
  0 TRISB!  \ Tri-state Port B
  0 TRISC!  \ Tri-state Port C
  0 TRISD!  \ Tri-state Port D
  0 TRISE!  \ Tri-state Port E
  0 LATA!  \ Latch Port A
  0 LATB!  \ Latch Port B
  0 LATC!  \ Latch Port C
  0 LATD!  \ Latch Port D
  0 LATE!  \ Latch Port E
  0 INTCON!  \ Interrupt Control
  0 PIR1!  \ Peripheral Interrupt 1
  0 PIR2!  \ Peripheral Interrupt 2
  0 PIE1!  \ Peripheral Interrupt Enable 1
  0 PIE2!  \ Peripheral Interrupt Enable 2
  0 IPR1!  \ Interrupt Priority 1
  0 IPR2!  \ Interrupt Priority 2
  0 RCON!  \ Reset Control
  0 T0CON!  \ Timer 0 Control
  0 TMR0!  \ Timer 0 Register
  0 T1CON!  \ Timer 1 Control
  0 TMR1!  \ Timer 1 Register High
  0 TMR1L!  \ Timer 1 Register Low
  0 T2CON!  \ Timer 2 Control
  0 TMR2!  \ Timer 2 Register
  0 T3CON!  \ Timer 3 Control
  0 TMR3!  \ Timer 3 Register High
  0 TMR3L!  \ Timer 3 Register Low
  0 SSPCON1!  \ SSP Control 1
  0 SSPCON2!  \ SSP Control 2
  0 SSPSTAT!  \ SSP Status
  0 SSPBUF!  \ SSP Buffer
  0 SSPOR!  \ SSP Shift Register
  0 ADCON0!  \ A/D Control 0
  0 ADCON1!  \ A/D Control 1
  0 ADCON2!  \ A/D Control 2
  0 ADRES!  \ A/D Result
  0 ADRESL!  \ A/D Result Low
  0 CCP1CON!  \ CCP 1 Control
  0 CCPR1!  \ CCP 1 Register High
  0 CCPR1L!  \ CCP 1 Register Low
  0 CCP2CON!  \ CCP 2 Control
  0 CCPR2!  \ CCP 2 Register High
  0 CCPR2L!  \ CCP 2 Register Low
  0 USBCON!  \ USB Control
  0 USBSTAT!  \ USB Status
  0 UIE!  \ USB Interrupt Enable
  0 UIR!  \ USB Interrupt Flag
  0 UCON!  \ USB Control
  0 USTAT!  \ USB Status
  0 UEP0!  \ USB Endpoint 0
  0 UEP1!  \ USB Endpoint 1
  0 UEP2!  \ USB Endpoint 2
  0 UEP3!  \ USB Endpoint 3
  0 UEP4!  \ USB Endpoint 4

  \ 初始化外设
  \ 初始化PORTA
  0 PORTA-PORT!  \ PORT寄存器
  0 PORTA-TRIS!  \ TRIS寄存器
  0 PORTA-LAT!  \ LAT寄存器
  \ 初始化PORTB
  0 PORTB-PORT!  \ PORT寄存器
  0 PORTB-TRIS!  \ TRIS寄存器
  0 PORTB-LAT!  \ LAT寄存器
  \ 初始化PORTC
  0 PORTC-PORT!  \ PORT寄存器
  0 PORTC-TRIS!  \ TRIS寄存器
  0 PORTC-LAT!  \ LAT寄存器
  \ 初始化PORTD
  0 PORTD-PORT!  \ PORT寄存器
  0 PORTD-TRIS!  \ TRIS寄存器
  0 PORTD-LAT!  \ LAT寄存器
  \ 初始化PORTE
  0 PORTE-PORT!  \ PORT寄存器
  0 PORTE-TRIS!  \ TRIS寄存器
  0 PORTE-LAT!  \ LAT寄存器
  \ 初始化TIMER0
  0 TIMER0-T0CON!  \ T0CON寄存器
  0 TIMER0-TMR0!  \ TMR0寄存器
  \ 初始化TIMER1
  0 TIMER1-T1CON!  \ T1CON寄存器
  0 TIMER1-TMR1!  \ TMR1寄存器
  0 TIMER1-TMR1L!  \ TMR1L寄存器
  \ 初始化TIMER2
  0 TIMER2-T2CON!  \ T2CON寄存器
  0 TIMER2-TMR2!  \ TMR2寄存器
  \ 初始化TIMER3
  0 TIMER3-T3CON!  \ T3CON寄存器
  0 TIMER3-TMR3!  \ TMR3寄存器
  \ 初始化ADC
  0 ADC-ADCON0!  \ ADCON0寄存器
  0 ADC-ADCON1!  \ ADCON1寄存器
  0 ADC-ADCON2!  \ ADCON2寄存器
  0 ADC-ADRES!  \ ADRES寄存器
  0 ADC-ADRESL!  \ ADRESL寄存器
  \ 初始化CCP1
  0 CCP1-CCP1CON!  \ CCP1CON寄存器
  0 CCP1-CCPR1!  \ CCPR1寄存器
  0 CCP1-CCPR1L!  \ CCPR1L寄存器
  \ 初始化CCP2
  0 CCP2-CCP2CON!  \ CCP2CON寄存器
  0 CCP2-CCPR2!  \ CCPR2寄存器
  0 CCP2-CCPR2L!  \ CCPR2L寄存器
  \ 初始化SSP
  0 SSP-SSPCON1!  \ SSPCON1寄存器
  0 SSP-SSPCON2!  \ SSPCON2寄存器
  0 SSP-SSPSTAT!  \ SSPSTAT寄存器
  0 SSP-SSPBUF!  \ SSPBUF寄存器
  0 SSP-SSPOV!  \ SSPOV寄存器
  \ 初始化EUSART
  0 EUSART-TXSTA!  \ TXSTA寄存器
  0 EUSART-RCSTA!  \ RCSTA寄存器
  0 EUSART-TXREG!  \ TXREG寄存器
  0 EUSART-RCREG!  \ RCREG寄存器
  0 EUSART-SPBRG!  \ SPBRG寄存器
  0 EUSART-SPBRGH!  \ SPBRGH寄存器
  0 EUSART-BAUDCON!  \ BAUDCON寄存器
  \ 初始化COMPARATOR
  0 COMPARATOR-CMCON!  \ CMCON寄存器
  0 COMPARATOR-CVRCON!  \ CVRCON寄存器
  \ 初始化USB
  0 USB-UCON!  \ UCON寄存器
  0 USB-USTAT!  \ USTAT寄存器
  0 USB-UIR!  \ UIR寄存器
  0 USB-UIE!  \ UIE寄存器
  0 USB-UEP0!  \ UEP0寄存器
  0 USB-UEP1!  \ UEP1寄存器
  0 USB-UEP2!  \ UEP2寄存器
  0 USB-UEP3!  \ UEP3寄存器
  0 USB-BD0!  \ BD0寄存器
  0 USB-BD1!  \ BD1寄存器
  0 USB-BD2!  \ BD2寄存器
  0 USB-BD3!  \ BD3寄存器
  \ 初始化OSCCON
  0 OSCCON-OSCCON!  \ OSCCON寄存器
  0 OSCCON-OSCTUNE!  \ OSCTUNE寄存器
  \ 初始化WDTCON
  0 WDTCON-WDTCON!  \ WDTCON寄存器

  ." PIC18F4550初始化完成" CR
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
  BSR@ BSR .R 8 .R SPACE ."  BSR: " BSR@ .
  PORTA@ PORTA .R 8 .R SPACE ."  PORTA: " PORTA@ .
  PORTB@ PORTB .R 8 .R SPACE ."  PORTB: " PORTB@ .
  PORTC@ PORTC .R 8 .R SPACE ."  PORTC: " PORTC@ .
  PORTD@ PORTD .R 8 .R SPACE ."  PORTD: " PORTD@ .
  PORTE@ PORTE .R 8 .R SPACE ."  PORTE: " PORTE@ .
  TRISA@ TRISA .R 8 .R SPACE ."  TRISA: " TRISA@ .
  TRISB@ TRISB .R 8 .R SPACE ."  TRISB: " TRISB@ .
  TRISC@ TRISC .R 8 .R SPACE ."  TRISC: " TRISC@ .
  TRISD@ TRISD .R 8 .R SPACE ."  TRISD: " TRISD@ .
  TRISE@ TRISE .R 8 .R SPACE ."  TRISE: " TRISE@ .
  LATA@ LATA .R 8 .R SPACE ."  LATA: " LATA@ .
  LATB@ LATB .R 8 .R SPACE ."  LATB: " LATB@ .
  LATC@ LATC .R 8 .R SPACE ."  LATC: " LATC@ .
  LATD@ LATD .R 8 .R SPACE ."  LATD: " LATD@ .
  LATE@ LATE .R 8 .R SPACE ."  LATE: " LATE@ .
  INTCON@ INTCON .R 8 .R SPACE ."  INTCON: " INTCON@ .
  PIR1@ PIR1 .R 8 .R SPACE ."  PIR1: " PIR1@ .
  PIR2@ PIR2 .R 8 .R SPACE ."  PIR2: " PIR2@ .
  PIE1@ PIE1 .R 8 .R SPACE ."  PIE1: " PIE1@ .
  PIE2@ PIE2 .R 8 .R SPACE ."  PIE2: " PIE2@ .
  IPR1@ IPR1 .R 8 .R SPACE ."  IPR1: " IPR1@ .
  IPR2@ IPR2 .R 8 .R SPACE ."  IPR2: " IPR2@ .
  RCON@ RCON .R 8 .R SPACE ."  RCON: " RCON@ .
  T0CON@ T0CON .R 8 .R SPACE ."  T0CON: " T0CON@ .
  TMR0@ TMR0 .R 8 .R SPACE ."  TMR0: " TMR0@ .
  T1CON@ T1CON .R 8 .R SPACE ."  T1CON: " T1CON@ .
  TMR1@ TMR1 .R 8 .R SPACE ."  TMR1: " TMR1@ .
  TMR1L@ TMR1L .R 8 .R SPACE ."  TMR1L: " TMR1L@ .
  T2CON@ T2CON .R 8 .R SPACE ."  T2CON: " T2CON@ .
  TMR2@ TMR2 .R 8 .R SPACE ."  TMR2: " TMR2@ .
  T3CON@ T3CON .R 8 .R SPACE ."  T3CON: " T3CON@ .
  TMR3@ TMR3 .R 8 .R SPACE ."  TMR3: " TMR3@ .
  TMR3L@ TMR3L .R 8 .R SPACE ."  TMR3L: " TMR3L@ .
  SSPCON1@ SSPCON1 .R 8 .R SPACE ."  SSPCON1: " SSPCON1@ .
  SSPCON2@ SSPCON2 .R 8 .R SPACE ."  SSPCON2: " SSPCON2@ .
  SSPSTAT@ SSPSTAT .R 8 .R SPACE ."  SSPSTAT: " SSPSTAT@ .
  SSPBUF@ SSPBUF .R 8 .R SPACE ."  SSPBUF: " SSPBUF@ .
  SSPOR@ SSPOR .R 8 .R SPACE ."  SSPOR: " SSPOR@ .
  ADCON0@ ADCON0 .R 8 .R SPACE ."  ADCON0: " ADCON0@ .
  ADCON1@ ADCON1 .R 8 .R SPACE ."  ADCON1: " ADCON1@ .
  ADCON2@ ADCON2 .R 8 .R SPACE ."  ADCON2: " ADCON2@ .
  ADRES@ ADRES .R 8 .R SPACE ."  ADRES: " ADRES@ .
  ADRESL@ ADRESL .R 8 .R SPACE ."  ADRESL: " ADRESL@ .
  CCP1CON@ CCP1CON .R 8 .R SPACE ."  CCP1CON: " CCP1CON@ .
  CCPR1@ CCPR1 .R 8 .R SPACE ."  CCPR1: " CCPR1@ .
  CCPR1L@ CCPR1L .R 8 .R SPACE ."  CCPR1L: " CCPR1L@ .
  CCP2CON@ CCP2CON .R 8 .R SPACE ."  CCP2CON: " CCP2CON@ .
  CCPR2@ CCPR2 .R 8 .R SPACE ."  CCPR2: " CCPR2@ .
  CCPR2L@ CCPR2L .R 8 .R SPACE ."  CCPR2L: " CCPR2L@ .
  USBCON@ USBCON .R 8 .R SPACE ."  USBCON: " USBCON@ .
  USBSTAT@ USBSTAT .R 8 .R SPACE ."  USBSTAT: " USBSTAT@ .
  UIE@ UIE .R 8 .R SPACE ."  UIE: " UIE@ .
  UIR@ UIR .R 8 .R SPACE ."  UIR: " UIR@ .
  UCON@ UCON .R 8 .R SPACE ."  UCON: " UCON@ .
  USTAT@ USTAT .R 8 .R SPACE ."  USTAT: " USTAT@ .
  UEP0@ UEP0 .R 8 .R SPACE ."  UEP0: " UEP0@ .
  UEP1@ UEP1 .R 8 .R SPACE ."  UEP1: " UEP1@ .
  UEP2@ UEP2 .R 8 .R SPACE ."  UEP2: " UEP2@ .
  UEP3@ UEP3 .R 8 .R SPACE ."  UEP3: " UEP3@ .
  UEP4@ UEP4 .R 8 .R SPACE ."  UEP4: " UEP4@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ RESET
: INT-RESET-HANDLER ( -- )
  ." RESET中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ External Interrupt 0
: INT-INT0-HANDLER ( -- )
  ." INT0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT0-ENABLE ( -- )
  INT-INT0 INT-ENABLE
;

: INT-INT0-DISABLE ( -- )
  INT-INT0 INT-DISABLE
;

\ External Interrupt 1
: INT-INT1-HANDLER ( -- )
  ." INT1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT1-ENABLE ( -- )
  INT-INT1 INT-ENABLE
;

: INT-INT1-DISABLE ( -- )
  INT-INT1 INT-DISABLE
;

\ External Interrupt 2
: INT-INT2-HANDLER ( -- )
  ." INT2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT2-ENABLE ( -- )
  INT-INT2 INT-ENABLE
;

: INT-INT2-DISABLE ( -- )
  INT-INT2 INT-DISABLE
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

\ Timer 2 Match
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

\ Timer 3 Overflow
: INT-TMR3-HANDLER ( -- )
  ." TMR3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TMR3-ENABLE ( -- )
  INT-TMR3 INT-ENABLE
;

: INT-TMR3-DISABLE ( -- )
  INT-TMR3 INT-DISABLE
;

\ CCP 1
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

\ CCP 2
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

\ SSP
: INT-SSP-HANDLER ( -- )
  ." SSP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SSP-ENABLE ( -- )
  INT-SSP INT-ENABLE
;

: INT-SSP-DISABLE ( -- )
  INT-SSP INT-DISABLE
;

\ USART TX
: INT-TX-HANDLER ( -- )
  ." TX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TX-ENABLE ( -- )
  INT-TX INT-ENABLE
;

: INT-TX-DISABLE ( -- )
  INT-TX INT-DISABLE
;

\ USART RX
: INT-RC-HANDLER ( -- )
  ." RC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RC-ENABLE ( -- )
  INT-RC INT-ENABLE
;

: INT-RC-DISABLE ( -- )
  INT-RC INT-DISABLE
;

\ A/D
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

\ Port B Change
: INT-RBO-HANDLER ( -- )
  ." RBO中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RBO-ENABLE ( -- )
  INT-RBO INT-ENABLE
;

: INT-RBO-DISABLE ( -- )
  INT-RBO INT-DISABLE
;

\ External
: INT-EXT-HANDLER ( -- )
  ." EXT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXT-ENABLE ( -- )
  INT-EXT INT-ENABLE
;

: INT-EXT-DISABLE ( -- )
  INT-EXT INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  PIC18F4550-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
