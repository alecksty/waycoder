\ ATmega328P设备定义 - Forth文件
\ 生成自: Atmel/AVR/ATmega328P
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
\ CPU架构: AVR
\ 位宽: 8位
\ 时钟频率: 16000000 Hz

\ =========================================
\ ATmega328P设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ATmega328P" ;
: MANUFACTURER  S" Atmel" ;
: FAMILY        S" AVR" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" AVR" ;
8 CONSTANT BITS
16000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ General Purpose Register 0
0x01 CONSTANT R1  \ General Purpose Register 1
0x02 CONSTANT R2  \ General Purpose Register 2
0x03 CONSTANT R3  \ General Purpose Register 3
0x04 CONSTANT R4  \ General Purpose Register 4
0x05 CONSTANT R5  \ General Purpose Register 5
0x06 CONSTANT R6  \ General Purpose Register 6
0x07 CONSTANT R7  \ General Purpose Register 7
0x08 CONSTANT R8  \ General Purpose Register 8
0x09 CONSTANT R9  \ General Purpose Register 9
0x0A CONSTANT R10  \ General Purpose Register 10
0x0B CONSTANT R11  \ General Purpose Register 11
0x0C CONSTANT R12  \ General Purpose Register 12
0x0D CONSTANT R13  \ General Purpose Register 13
0x0E CONSTANT R14  \ General Purpose Register 14
0x0F CONSTANT R15  \ General Purpose Register 15
0x10 CONSTANT R16  \ General Purpose Register 16
0x11 CONSTANT R17  \ General Purpose Register 17
0x12 CONSTANT R18  \ General Purpose Register 18
0x13 CONSTANT R19  \ General Purpose Register 19
0x14 CONSTANT R20  \ General Purpose Register 20
0x15 CONSTANT R21  \ General Purpose Register 21
0x16 CONSTANT R22  \ General Purpose Register 22
0x17 CONSTANT R23  \ General Purpose Register 23
0x18 CONSTANT R24  \ General Purpose Register 24
0x19 CONSTANT R25  \ General Purpose Register 25
0x1A CONSTANT R26  \ General Purpose Register 26 (XL)
0x1B CONSTANT R27  \ General Purpose Register 27 (XH)
0x1C CONSTANT R28  \ General Purpose Register 28 (YL)
0x1D CONSTANT R29  \ General Purpose Register 29 (YH)
0x1E CONSTANT R30  \ General Purpose Register 30 (ZL)
0x1F CONSTANT R31  \ General Purpose Register 31 (ZH)
0x5D CONSTANT SPL  \ Stack Pointer Low
0x5E CONSTANT SPH  \ Stack Pointer High
0x5F CONSTANT SREG  \ Status Register
0 CONSTANT SREG-C  \ Carry Flag
1 CONSTANT SREG-Z  \ Zero Flag
2 CONSTANT SREG-N  \ Negative Flag
3 CONSTANT SREG-V  \ Two's Complement Overflow Flag
4 CONSTANT SREG-S  \ Sign Flag (N ⊕ V)
5 CONSTANT SREG-H  \ Half Carry Flag
6 CONSTANT SREG-T  \ Transfer Bit
7 CONSTANT SREG-I  \ Global Interrupt Enable

\ 内存段定义
0x0000 CONSTANT FLASH-START
0x7FFF CONSTANT FLASH-END
32768 CONSTANT FLASH-SIZE  \ Program Flash Memory
0x0100 CONSTANT SRAM-START
0x08FF CONSTANT SRAM-END
2048 CONSTANT SRAM-SIZE  \ Static RAM
0x0000 CONSTANT EEPROM-START
0x03FF CONSTANT EEPROM-END
1024 CONSTANT EEPROM-SIZE  \ EEPROM
0x00 CONSTANT IO-START
0x3F CONSTANT IO-END
64 CONSTANT IO-SIZE  \ I/O Registers
0x40 CONSTANT EXTIO-START
0xFF CONSTANT EXTIO-END
192 CONSTANT EXTIO-SIZE  \ Extended I/O Registers

\ 外设定义
\ Port B Data Register
0x23 CONSTANT PORTB-BASE
0x25 CONSTANT PORTB-PORTB
0x24 CONSTANT PORTB-DDRB
0x23 CONSTANT PORTB-PINB
\ Port C Data Register
0x26 CONSTANT PORTC-BASE
0x28 CONSTANT PORTC-PORTC
0x27 CONSTANT PORTC-DDRC
0x26 CONSTANT PORTC-PINC
\ Port D Data Register
0x29 CONSTANT PORTD-BASE
0x2B CONSTANT PORTD-PORTD
0x2A CONSTANT PORTD-DDRD
0x29 CONSTANT PORTD-PIND
\ 8-bit Timer/Counter0
0x44 CONSTANT TIMER0-BASE
0x44 CONSTANT TIMER0-TCCR0A
0 CONSTANT TIMER0-TCCR0A-WGM00  \ Waveform Generation Mode
1 CONSTANT TIMER0-TCCR0A-WGM01  \ Waveform Generation Mode
4 CONSTANT TIMER0-TCCR0A-COM0B0  \ Compare Output Mode for Channel B
5 CONSTANT TIMER0-TCCR0A-COM0B1  \ Compare Output Mode for Channel B
6 CONSTANT TIMER0-TCCR0A-COM0A0  \ Compare Output Mode for Channel A
7 CONSTANT TIMER0-TCCR0A-COM0A1  \ Compare Output Mode for Channel A
0x45 CONSTANT TIMER0-TCCR0B
0 CONSTANT TIMER0-TCCR0B-CS00  \ Clock Select
1 CONSTANT TIMER0-TCCR0B-CS01  \ Clock Select
2 CONSTANT TIMER0-TCCR0B-CS02  \ Clock Select
3 CONSTANT TIMER0-TCCR0B-WGM02  \ Waveform Generation Mode
6 CONSTANT TIMER0-TCCR0B-FOC0B  \ Force Output Compare B
7 CONSTANT TIMER0-TCCR0B-FOC0A  \ Force Output Compare A
0x46 CONSTANT TIMER0-TCNT0
0x47 CONSTANT TIMER0-OCR0A
0x48 CONSTANT TIMER0-OCR0B
0x6E CONSTANT TIMER0-TIMSK0
0 CONSTANT TIMER0-TIMSK0-TOIE0  \ Timer/Counter0 Overflow Interrupt Enable
1 CONSTANT TIMER0-TIMSK0-OCIE0A  \ Timer/Counter0 Output Compare A Match Interrupt Enable
2 CONSTANT TIMER0-TIMSK0-OCIE0B  \ Timer/Counter0 Output Compare B Match Interrupt Enable
0x35 CONSTANT TIMER0-TIFR0
0 CONSTANT TIMER0-TIFR0-TOV0  \ Timer/Counter0 Overflow Flag
1 CONSTANT TIMER0-TIFR0-OCF0A  \ Output Compare Flag 0A
2 CONSTANT TIMER0-TIFR0-OCF0B  \ Output Compare Flag 0B
\ Universal Synchronous/Asynchronous Receiver/Transmitter
0xC0 CONSTANT USART0-BASE
0xC6 CONSTANT USART0-UDR0
0xC0 CONSTANT USART0-UCSR0A
0 CONSTANT USART0-UCSR0A-MPCM0  \ Multi-processor Communication Mode
1 CONSTANT USART0-UCSR0A-U2X0  \ Double the USART Transmission Speed
2 CONSTANT USART0-UCSR0A-UPE0  \ Parity Error
3 CONSTANT USART0-UCSR0A-DOR0  \ Data OverRun
4 CONSTANT USART0-UCSR0A-FE0  \ Frame Error
5 CONSTANT USART0-UCSR0A-UDRE0  \ USART Data Register Empty
6 CONSTANT USART0-UCSR0A-TXC0  \ USART Transmit Complete
7 CONSTANT USART0-UCSR0A-RXC0  \ USART Receive Complete
0xC1 CONSTANT USART0-UCSR0B
0 CONSTANT USART0-UCSR0B-TXB80  \ Transmit Data Bit 8
1 CONSTANT USART0-UCSR0B-RXB80  \ Receive Data Bit 8
2 CONSTANT USART0-UCSR0B-UCSZ02  \ Character Size
3 CONSTANT USART0-UCSR0B-TXEN0  \ Transmitter Enable
4 CONSTANT USART0-UCSR0B-RXEN0  \ Receiver Enable
5 CONSTANT USART0-UCSR0B-UDRIE0  \ USART Data Register Empty Interrupt Enable
6 CONSTANT USART0-UCSR0B-TXCIE0  \ TX Complete Interrupt Enable
7 CONSTANT USART0-UCSR0B-RXCIE0  \ RX Complete Interrupt Enable
0xC2 CONSTANT USART0-UCSR0C
0 CONSTANT USART0-UCSR0C-UCPOL0  \ Clock Polarity
1 CONSTANT USART0-UCSR0C-UCSZ00  \ Character Size
2 CONSTANT USART0-UCSR0C-UCSZ01  \ Character Size
3 CONSTANT USART0-UCSR0C-USBS0  \ Stop Bit Select
4 CONSTANT USART0-UCSR0C-UPM00  \ Parity Mode
5 CONSTANT USART0-UCSR0C-UPM01  \ Parity Mode
6 CONSTANT USART0-UCSR0C-UMSEL00  \ USART Mode Select
7 CONSTANT USART0-UCSR0C-UMSEL01  \ USART Mode Select
0xC4 CONSTANT USART0-UBRR0
\ Analog-to-Digital Converter
0x78 CONSTANT ADC-BASE
0x7C CONSTANT ADC-ADMUX
0 CONSTANT ADC-ADMUX-MUX0  \ Analog Channel Selection
1 CONSTANT ADC-ADMUX-MUX1  \ Analog Channel Selection
2 CONSTANT ADC-ADMUX-MUX2  \ Analog Channel Selection
3 CONSTANT ADC-ADMUX-MUX3  \ Analog Channel Selection
5 CONSTANT ADC-ADMUX-ADLAR  \ ADC Left Adjust Result
6 CONSTANT ADC-ADMUX-REFS0  \ Reference Selection
7 CONSTANT ADC-ADMUX-REFS1  \ Reference Selection
0x7A CONSTANT ADC-ADCSRA
0 CONSTANT ADC-ADCSRA-ADPS0  \ ADC Prescaler Select
1 CONSTANT ADC-ADCSRA-ADPS1  \ ADC Prescaler Select
2 CONSTANT ADC-ADCSRA-ADPS2  \ ADC Prescaler Select
3 CONSTANT ADC-ADCSRA-ADIE  \ ADC Interrupt Enable
4 CONSTANT ADC-ADCSRA-ADIF  \ ADC Interrupt Flag
5 CONSTANT ADC-ADCSRA-ADATE  \ ADC Auto Trigger Enable
6 CONSTANT ADC-ADCSRA-ADSC  \ ADC Start Conversion
7 CONSTANT ADC-ADCSRA-ADEN  \ ADC Enable
0x79 CONSTANT ADC-ADCH
0x78 CONSTANT ADC-ADCL

\ 中断向量定义
1 CONSTANT INT-INT0  \ External Interrupt Request 0
2 CONSTANT INT-INT1  \ External Interrupt Request 1
3 CONSTANT INT-PCINT0  \ Pin Change Interrupt Request 0
4 CONSTANT INT-PCINT1  \ Pin Change Interrupt Request 1
5 CONSTANT INT-PCINT2  \ Pin Change Interrupt Request 2
6 CONSTANT INT-WDT  \ Watchdog Time-out Interrupt
7 CONSTANT INT-TIMER2_COMPA  \ Timer/Counter2 Compare Match A
8 CONSTANT INT-TIMER2_COMPB  \ Timer/Counter2 Compare Match B
9 CONSTANT INT-TIMER2_OVF  \ Timer/Counter2 Overflow
10 CONSTANT INT-TIMER1_CAPT  \ Timer/Counter1 Capture Event
11 CONSTANT INT-TIMER1_COMPA  \ Timer/Counter1 Compare Match A
12 CONSTANT INT-TIMER1_COMPB  \ Timer/Counter1 Compare Match B
13 CONSTANT INT-TIMER1_OVF  \ Timer/Counter1 Overflow
14 CONSTANT INT-TIMER0_COMPA  \ Timer/Counter0 Compare Match A
15 CONSTANT INT-TIMER0_COMPB  \ Timer/Counter0 Compare Match B
16 CONSTANT INT-TIMER0_OVF  \ Timer/Counter0 Overflow
17 CONSTANT INT-SPI_STC  \ SPI Serial Transfer Complete
18 CONSTANT INT-USART_RX  \ USART Rx Complete
19 CONSTANT INT-USART_UDRE  \ USART Data Register Empty
20 CONSTANT INT-USART_TX  \ USART Tx Complete
21 CONSTANT INT-ADC  \ ADC Conversion Complete
22 CONSTANT INT-EE_READY  \ EEPROM Ready
23 CONSTANT INT-ANALOG_COMP  \ Analog Comparator
24 CONSTANT INT-TWI  \ Two-wire Serial Interface
25 CONSTANT INT-SPM_READY  \ Store Program Memory Ready

\ 引脚定义
1 CONSTANT PIN-PC6  \ Reset Pin
2 CONSTANT PIN-PD0  \ Digital I/O, RX (USART)
3 CONSTANT PIN-PD1  \ Digital I/O, TX (USART)
4 CONSTANT PIN-PD2  \ Digital I/O, INT0
5 CONSTANT PIN-PD3  \ Digital I/O, INT1, OC2B
6 CONSTANT PIN-PD4  \ Digital I/O, T0, XCK
7 CONSTANT PIN-VCC  \ Supply Voltage
8 CONSTANT PIN-GND  \ Ground
9 CONSTANT PIN-PB6  \ Digital I/O, XTAL1
10 CONSTANT PIN-PB7  \ Digital I/O, XTAL2
11 CONSTANT PIN-PD5  \ Digital I/O, T1, OC0B
12 CONSTANT PIN-PD6  \ Digital I/O, AIN0, OC0A
13 CONSTANT PIN-PD7  \ Digital I/O, AIN1
14 CONSTANT PIN-PB0  \ Digital I/O, ICP1, CLKO
15 CONSTANT PIN-PB1  \ Digital I/O, OC1A
16 CONSTANT PIN-PB2  \ Digital I/O, SS, OC1B
17 CONSTANT PIN-PB3  \ Digital I/O, MOSI, OC2A
18 CONSTANT PIN-PB4  \ Digital I/O, MISO
19 CONSTANT PIN-PB5  \ Digital I/O, SCK
20 CONSTANT PIN-AVCC  \ Supply Voltage for ADC
21 CONSTANT PIN-AREF  \ Analog Reference
22 CONSTANT PIN-GND  \ Ground
23 CONSTANT PIN-PC0  \ Digital I/O, ADC0
24 CONSTANT PIN-PC1  \ Digital I/O, ADC1
25 CONSTANT PIN-PC2  \ Digital I/O, ADC2
26 CONSTANT PIN-PC3  \ Digital I/O, ADC3
27 CONSTANT PIN-PC4  \ Digital I/O, ADC4, SDA
28 CONSTANT PIN-PC5  \ Digital I/O, ADC5, SCL

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 C@ ;
: R0! ( n -- ) R0 C! ;

: R1@ ( -- n ) R1 C@ ;
: R1! ( n -- ) R1 C! ;

: R2@ ( -- n ) R2 C@ ;
: R2! ( n -- ) R2 C! ;

: R3@ ( -- n ) R3 C@ ;
: R3! ( n -- ) R3 C! ;

: R4@ ( -- n ) R4 C@ ;
: R4! ( n -- ) R4 C! ;

: R5@ ( -- n ) R5 C@ ;
: R5! ( n -- ) R5 C! ;

: R6@ ( -- n ) R6 C@ ;
: R6! ( n -- ) R6 C! ;

: R7@ ( -- n ) R7 C@ ;
: R7! ( n -- ) R7 C! ;

: R8@ ( -- n ) R8 C@ ;
: R8! ( n -- ) R8 C! ;

: R9@ ( -- n ) R9 C@ ;
: R9! ( n -- ) R9 C! ;

: R10@ ( -- n ) R10 C@ ;
: R10! ( n -- ) R10 C! ;

: R11@ ( -- n ) R11 C@ ;
: R11! ( n -- ) R11 C! ;

: R12@ ( -- n ) R12 C@ ;
: R12! ( n -- ) R12 C! ;

: R13@ ( -- n ) R13 C@ ;
: R13! ( n -- ) R13 C! ;

: R14@ ( -- n ) R14 C@ ;
: R14! ( n -- ) R14 C! ;

: R15@ ( -- n ) R15 C@ ;
: R15! ( n -- ) R15 C! ;

: R16@ ( -- n ) R16 C@ ;
: R16! ( n -- ) R16 C! ;

: R17@ ( -- n ) R17 C@ ;
: R17! ( n -- ) R17 C! ;

: R18@ ( -- n ) R18 C@ ;
: R18! ( n -- ) R18 C! ;

: R19@ ( -- n ) R19 C@ ;
: R19! ( n -- ) R19 C! ;

: R20@ ( -- n ) R20 C@ ;
: R20! ( n -- ) R20 C! ;

: R21@ ( -- n ) R21 C@ ;
: R21! ( n -- ) R21 C! ;

: R22@ ( -- n ) R22 C@ ;
: R22! ( n -- ) R22 C! ;

: R23@ ( -- n ) R23 C@ ;
: R23! ( n -- ) R23 C! ;

: R24@ ( -- n ) R24 C@ ;
: R24! ( n -- ) R24 C! ;

: R25@ ( -- n ) R25 C@ ;
: R25! ( n -- ) R25 C! ;

: R26@ ( -- n ) R26 C@ ;
: R26! ( n -- ) R26 C! ;

: R27@ ( -- n ) R27 C@ ;
: R27! ( n -- ) R27 C! ;

: R28@ ( -- n ) R28 C@ ;
: R28! ( n -- ) R28 C! ;

: R29@ ( -- n ) R29 C@ ;
: R29! ( n -- ) R29 C! ;

: R30@ ( -- n ) R30 C@ ;
: R30! ( n -- ) R30 C! ;

: R31@ ( -- n ) R31 C@ ;
: R31! ( n -- ) R31 C! ;

: SPL@ ( -- n ) SPL C@ ;
: SPL! ( n -- ) SPL C! ;

: SPH@ ( -- n ) SPH C@ ;
: SPH! ( n -- ) SPH C! ;

: SREG@ ( -- n ) SREG C@ ;
: SREG! ( n -- ) SREG C! ;
: SREG-C@ ( -- flag ) SREG@ 0 BIT@ ;
: SREG-C! ( flag -- ) SREG@ 0 BIT! SREG! ;
: SREG-C-SET ( -- ) TRUE SREG-C! ;
: SREG-C-CLR ( -- ) FALSE SREG-C! ;
: SREG-Z@ ( -- flag ) SREG@ 1 BIT@ ;
: SREG-Z! ( flag -- ) SREG@ 1 BIT! SREG! ;
: SREG-Z-SET ( -- ) TRUE SREG-Z! ;
: SREG-Z-CLR ( -- ) FALSE SREG-Z! ;
: SREG-N@ ( -- flag ) SREG@ 2 BIT@ ;
: SREG-N! ( flag -- ) SREG@ 2 BIT! SREG! ;
: SREG-N-SET ( -- ) TRUE SREG-N! ;
: SREG-N-CLR ( -- ) FALSE SREG-N! ;
: SREG-V@ ( -- flag ) SREG@ 3 BIT@ ;
: SREG-V! ( flag -- ) SREG@ 3 BIT! SREG! ;
: SREG-V-SET ( -- ) TRUE SREG-V! ;
: SREG-V-CLR ( -- ) FALSE SREG-V! ;
: SREG-S@ ( -- flag ) SREG@ 4 BIT@ ;
: SREG-S! ( flag -- ) SREG@ 4 BIT! SREG! ;
: SREG-S-SET ( -- ) TRUE SREG-S! ;
: SREG-S-CLR ( -- ) FALSE SREG-S! ;
: SREG-H@ ( -- flag ) SREG@ 5 BIT@ ;
: SREG-H! ( flag -- ) SREG@ 5 BIT! SREG! ;
: SREG-H-SET ( -- ) TRUE SREG-H! ;
: SREG-H-CLR ( -- ) FALSE SREG-H! ;
: SREG-T@ ( -- flag ) SREG@ 6 BIT@ ;
: SREG-T! ( flag -- ) SREG@ 6 BIT! SREG! ;
: SREG-T-SET ( -- ) TRUE SREG-T! ;
: SREG-T-CLR ( -- ) FALSE SREG-T! ;
: SREG-I@ ( -- flag ) SREG@ 7 BIT@ ;
: SREG-I! ( flag -- ) SREG@ 7 BIT! SREG! ;
: SREG-I-SET ( -- ) TRUE SREG-I! ;
: SREG-I-CLR ( -- ) FALSE SREG-I! ;

\ 外设访问
\ PORTB外设
: PORTB-PORTB@ ( -- n ) PORTB-PORTB C@ ;
: PORTB-PORTB! ( n -- ) PORTB-PORTB C! ;
: PORTB-DDRB@ ( -- n ) PORTB-DDRB C@ ;
: PORTB-DDRB! ( n -- ) PORTB-DDRB C! ;
: PORTB-PINB@ ( -- n ) PORTB-PINB C@ ;
: PORTB-PINB! ( n -- ) PORTB-PINB C! ;

\ PORTC外设
: PORTC-PORTC@ ( -- n ) PORTC-PORTC C@ ;
: PORTC-PORTC! ( n -- ) PORTC-PORTC C! ;
: PORTC-DDRC@ ( -- n ) PORTC-DDRC C@ ;
: PORTC-DDRC! ( n -- ) PORTC-DDRC C! ;
: PORTC-PINC@ ( -- n ) PORTC-PINC C@ ;
: PORTC-PINC! ( n -- ) PORTC-PINC C! ;

\ PORTD外设
: PORTD-PORTD@ ( -- n ) PORTD-PORTD C@ ;
: PORTD-PORTD! ( n -- ) PORTD-PORTD C! ;
: PORTD-DDRD@ ( -- n ) PORTD-DDRD C@ ;
: PORTD-DDRD! ( n -- ) PORTD-DDRD C! ;
: PORTD-PIND@ ( -- n ) PORTD-PIND C@ ;
: PORTD-PIND! ( n -- ) PORTD-PIND C! ;

\ TIMER0外设
: TIMER0-TCCR0A@ ( -- n ) TIMER0-TCCR0A C@ ;
: TIMER0-TCCR0A! ( n -- ) TIMER0-TCCR0A C! ;
: TIMER0-TCCR0A-WGM00@ ( -- flag ) TIMER0-TCCR0A@ 0 BIT@ ;
: TIMER0-TCCR0A-WGM00! ( flag -- ) TIMER0-TCCR0A@ 0 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0A-WGM01@ ( -- flag ) TIMER0-TCCR0A@ 1 BIT@ ;
: TIMER0-TCCR0A-WGM01! ( flag -- ) TIMER0-TCCR0A@ 1 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0A-COM0B0@ ( -- flag ) TIMER0-TCCR0A@ 4 BIT@ ;
: TIMER0-TCCR0A-COM0B0! ( flag -- ) TIMER0-TCCR0A@ 4 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0A-COM0B1@ ( -- flag ) TIMER0-TCCR0A@ 5 BIT@ ;
: TIMER0-TCCR0A-COM0B1! ( flag -- ) TIMER0-TCCR0A@ 5 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0A-COM0A0@ ( -- flag ) TIMER0-TCCR0A@ 6 BIT@ ;
: TIMER0-TCCR0A-COM0A0! ( flag -- ) TIMER0-TCCR0A@ 6 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0A-COM0A1@ ( -- flag ) TIMER0-TCCR0A@ 7 BIT@ ;
: TIMER0-TCCR0A-COM0A1! ( flag -- ) TIMER0-TCCR0A@ 7 BIT! TIMER0-TCCR0A! ;
: TIMER0-TCCR0B@ ( -- n ) TIMER0-TCCR0B C@ ;
: TIMER0-TCCR0B! ( n -- ) TIMER0-TCCR0B C! ;
: TIMER0-TCCR0B-CS00@ ( -- flag ) TIMER0-TCCR0B@ 0 BIT@ ;
: TIMER0-TCCR0B-CS00! ( flag -- ) TIMER0-TCCR0B@ 0 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCCR0B-CS01@ ( -- flag ) TIMER0-TCCR0B@ 1 BIT@ ;
: TIMER0-TCCR0B-CS01! ( flag -- ) TIMER0-TCCR0B@ 1 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCCR0B-CS02@ ( -- flag ) TIMER0-TCCR0B@ 2 BIT@ ;
: TIMER0-TCCR0B-CS02! ( flag -- ) TIMER0-TCCR0B@ 2 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCCR0B-WGM02@ ( -- flag ) TIMER0-TCCR0B@ 3 BIT@ ;
: TIMER0-TCCR0B-WGM02! ( flag -- ) TIMER0-TCCR0B@ 3 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCCR0B-FOC0B@ ( -- flag ) TIMER0-TCCR0B@ 6 BIT@ ;
: TIMER0-TCCR0B-FOC0B! ( flag -- ) TIMER0-TCCR0B@ 6 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCCR0B-FOC0A@ ( -- flag ) TIMER0-TCCR0B@ 7 BIT@ ;
: TIMER0-TCCR0B-FOC0A! ( flag -- ) TIMER0-TCCR0B@ 7 BIT! TIMER0-TCCR0B! ;
: TIMER0-TCNT0@ ( -- n ) TIMER0-TCNT0 C@ ;
: TIMER0-TCNT0! ( n -- ) TIMER0-TCNT0 C! ;
: TIMER0-OCR0A@ ( -- n ) TIMER0-OCR0A C@ ;
: TIMER0-OCR0A! ( n -- ) TIMER0-OCR0A C! ;
: TIMER0-OCR0B@ ( -- n ) TIMER0-OCR0B C@ ;
: TIMER0-OCR0B! ( n -- ) TIMER0-OCR0B C! ;
: TIMER0-TIMSK0@ ( -- n ) TIMER0-TIMSK0 C@ ;
: TIMER0-TIMSK0! ( n -- ) TIMER0-TIMSK0 C! ;
: TIMER0-TIMSK0-TOIE0@ ( -- flag ) TIMER0-TIMSK0@ 0 BIT@ ;
: TIMER0-TIMSK0-TOIE0! ( flag -- ) TIMER0-TIMSK0@ 0 BIT! TIMER0-TIMSK0! ;
: TIMER0-TIMSK0-OCIE0A@ ( -- flag ) TIMER0-TIMSK0@ 1 BIT@ ;
: TIMER0-TIMSK0-OCIE0A! ( flag -- ) TIMER0-TIMSK0@ 1 BIT! TIMER0-TIMSK0! ;
: TIMER0-TIMSK0-OCIE0B@ ( -- flag ) TIMER0-TIMSK0@ 2 BIT@ ;
: TIMER0-TIMSK0-OCIE0B! ( flag -- ) TIMER0-TIMSK0@ 2 BIT! TIMER0-TIMSK0! ;
: TIMER0-TIFR0@ ( -- n ) TIMER0-TIFR0 C@ ;
: TIMER0-TIFR0! ( n -- ) TIMER0-TIFR0 C! ;
: TIMER0-TIFR0-TOV0@ ( -- flag ) TIMER0-TIFR0@ 0 BIT@ ;
: TIMER0-TIFR0-TOV0! ( flag -- ) TIMER0-TIFR0@ 0 BIT! TIMER0-TIFR0! ;
: TIMER0-TIFR0-OCF0A@ ( -- flag ) TIMER0-TIFR0@ 1 BIT@ ;
: TIMER0-TIFR0-OCF0A! ( flag -- ) TIMER0-TIFR0@ 1 BIT! TIMER0-TIFR0! ;
: TIMER0-TIFR0-OCF0B@ ( -- flag ) TIMER0-TIFR0@ 2 BIT@ ;
: TIMER0-TIFR0-OCF0B! ( flag -- ) TIMER0-TIFR0@ 2 BIT! TIMER0-TIFR0! ;

\ USART0外设
: USART0-UDR0@ ( -- n ) USART0-UDR0 C@ ;
: USART0-UDR0! ( n -- ) USART0-UDR0 C! ;
: USART0-UCSR0A@ ( -- n ) USART0-UCSR0A C@ ;
: USART0-UCSR0A! ( n -- ) USART0-UCSR0A C! ;
: USART0-UCSR0A-MPCM0@ ( -- flag ) USART0-UCSR0A@ 0 BIT@ ;
: USART0-UCSR0A-MPCM0! ( flag -- ) USART0-UCSR0A@ 0 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-U2X0@ ( -- flag ) USART0-UCSR0A@ 1 BIT@ ;
: USART0-UCSR0A-U2X0! ( flag -- ) USART0-UCSR0A@ 1 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-UPE0@ ( -- flag ) USART0-UCSR0A@ 2 BIT@ ;
: USART0-UCSR0A-UPE0! ( flag -- ) USART0-UCSR0A@ 2 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-DOR0@ ( -- flag ) USART0-UCSR0A@ 3 BIT@ ;
: USART0-UCSR0A-DOR0! ( flag -- ) USART0-UCSR0A@ 3 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-FE0@ ( -- flag ) USART0-UCSR0A@ 4 BIT@ ;
: USART0-UCSR0A-FE0! ( flag -- ) USART0-UCSR0A@ 4 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-UDRE0@ ( -- flag ) USART0-UCSR0A@ 5 BIT@ ;
: USART0-UCSR0A-UDRE0! ( flag -- ) USART0-UCSR0A@ 5 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-TXC0@ ( -- flag ) USART0-UCSR0A@ 6 BIT@ ;
: USART0-UCSR0A-TXC0! ( flag -- ) USART0-UCSR0A@ 6 BIT! USART0-UCSR0A! ;
: USART0-UCSR0A-RXC0@ ( -- flag ) USART0-UCSR0A@ 7 BIT@ ;
: USART0-UCSR0A-RXC0! ( flag -- ) USART0-UCSR0A@ 7 BIT! USART0-UCSR0A! ;
: USART0-UCSR0B@ ( -- n ) USART0-UCSR0B C@ ;
: USART0-UCSR0B! ( n -- ) USART0-UCSR0B C! ;
: USART0-UCSR0B-TXB80@ ( -- flag ) USART0-UCSR0B@ 0 BIT@ ;
: USART0-UCSR0B-TXB80! ( flag -- ) USART0-UCSR0B@ 0 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-RXB80@ ( -- flag ) USART0-UCSR0B@ 1 BIT@ ;
: USART0-UCSR0B-RXB80! ( flag -- ) USART0-UCSR0B@ 1 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-UCSZ02@ ( -- flag ) USART0-UCSR0B@ 2 BIT@ ;
: USART0-UCSR0B-UCSZ02! ( flag -- ) USART0-UCSR0B@ 2 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-TXEN0@ ( -- flag ) USART0-UCSR0B@ 3 BIT@ ;
: USART0-UCSR0B-TXEN0! ( flag -- ) USART0-UCSR0B@ 3 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-RXEN0@ ( -- flag ) USART0-UCSR0B@ 4 BIT@ ;
: USART0-UCSR0B-RXEN0! ( flag -- ) USART0-UCSR0B@ 4 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-UDRIE0@ ( -- flag ) USART0-UCSR0B@ 5 BIT@ ;
: USART0-UCSR0B-UDRIE0! ( flag -- ) USART0-UCSR0B@ 5 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-TXCIE0@ ( -- flag ) USART0-UCSR0B@ 6 BIT@ ;
: USART0-UCSR0B-TXCIE0! ( flag -- ) USART0-UCSR0B@ 6 BIT! USART0-UCSR0B! ;
: USART0-UCSR0B-RXCIE0@ ( -- flag ) USART0-UCSR0B@ 7 BIT@ ;
: USART0-UCSR0B-RXCIE0! ( flag -- ) USART0-UCSR0B@ 7 BIT! USART0-UCSR0B! ;
: USART0-UCSR0C@ ( -- n ) USART0-UCSR0C C@ ;
: USART0-UCSR0C! ( n -- ) USART0-UCSR0C C! ;
: USART0-UCSR0C-UCPOL0@ ( -- flag ) USART0-UCSR0C@ 0 BIT@ ;
: USART0-UCSR0C-UCPOL0! ( flag -- ) USART0-UCSR0C@ 0 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UCSZ00@ ( -- flag ) USART0-UCSR0C@ 1 BIT@ ;
: USART0-UCSR0C-UCSZ00! ( flag -- ) USART0-UCSR0C@ 1 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UCSZ01@ ( -- flag ) USART0-UCSR0C@ 2 BIT@ ;
: USART0-UCSR0C-UCSZ01! ( flag -- ) USART0-UCSR0C@ 2 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-USBS0@ ( -- flag ) USART0-UCSR0C@ 3 BIT@ ;
: USART0-UCSR0C-USBS0! ( flag -- ) USART0-UCSR0C@ 3 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UPM00@ ( -- flag ) USART0-UCSR0C@ 4 BIT@ ;
: USART0-UCSR0C-UPM00! ( flag -- ) USART0-UCSR0C@ 4 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UPM01@ ( -- flag ) USART0-UCSR0C@ 5 BIT@ ;
: USART0-UCSR0C-UPM01! ( flag -- ) USART0-UCSR0C@ 5 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UMSEL00@ ( -- flag ) USART0-UCSR0C@ 6 BIT@ ;
: USART0-UCSR0C-UMSEL00! ( flag -- ) USART0-UCSR0C@ 6 BIT! USART0-UCSR0C! ;
: USART0-UCSR0C-UMSEL01@ ( -- flag ) USART0-UCSR0C@ 7 BIT@ ;
: USART0-UCSR0C-UMSEL01! ( flag -- ) USART0-UCSR0C@ 7 BIT! USART0-UCSR0C! ;
: USART0-UBRR0@ ( -- n ) USART0-UBRR0 @ ;
: USART0-UBRR0! ( n -- ) USART0-UBRR0 ! ;

\ ADC外设
: ADC-ADMUX@ ( -- n ) ADC-ADMUX C@ ;
: ADC-ADMUX! ( n -- ) ADC-ADMUX C! ;
: ADC-ADMUX-MUX0@ ( -- flag ) ADC-ADMUX@ 0 BIT@ ;
: ADC-ADMUX-MUX0! ( flag -- ) ADC-ADMUX@ 0 BIT! ADC-ADMUX! ;
: ADC-ADMUX-MUX1@ ( -- flag ) ADC-ADMUX@ 1 BIT@ ;
: ADC-ADMUX-MUX1! ( flag -- ) ADC-ADMUX@ 1 BIT! ADC-ADMUX! ;
: ADC-ADMUX-MUX2@ ( -- flag ) ADC-ADMUX@ 2 BIT@ ;
: ADC-ADMUX-MUX2! ( flag -- ) ADC-ADMUX@ 2 BIT! ADC-ADMUX! ;
: ADC-ADMUX-MUX3@ ( -- flag ) ADC-ADMUX@ 3 BIT@ ;
: ADC-ADMUX-MUX3! ( flag -- ) ADC-ADMUX@ 3 BIT! ADC-ADMUX! ;
: ADC-ADMUX-ADLAR@ ( -- flag ) ADC-ADMUX@ 5 BIT@ ;
: ADC-ADMUX-ADLAR! ( flag -- ) ADC-ADMUX@ 5 BIT! ADC-ADMUX! ;
: ADC-ADMUX-REFS0@ ( -- flag ) ADC-ADMUX@ 6 BIT@ ;
: ADC-ADMUX-REFS0! ( flag -- ) ADC-ADMUX@ 6 BIT! ADC-ADMUX! ;
: ADC-ADMUX-REFS1@ ( -- flag ) ADC-ADMUX@ 7 BIT@ ;
: ADC-ADMUX-REFS1! ( flag -- ) ADC-ADMUX@ 7 BIT! ADC-ADMUX! ;
: ADC-ADCSRA@ ( -- n ) ADC-ADCSRA C@ ;
: ADC-ADCSRA! ( n -- ) ADC-ADCSRA C! ;
: ADC-ADCSRA-ADPS0@ ( -- flag ) ADC-ADCSRA@ 0 BIT@ ;
: ADC-ADCSRA-ADPS0! ( flag -- ) ADC-ADCSRA@ 0 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADPS1@ ( -- flag ) ADC-ADCSRA@ 1 BIT@ ;
: ADC-ADCSRA-ADPS1! ( flag -- ) ADC-ADCSRA@ 1 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADPS2@ ( -- flag ) ADC-ADCSRA@ 2 BIT@ ;
: ADC-ADCSRA-ADPS2! ( flag -- ) ADC-ADCSRA@ 2 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADIE@ ( -- flag ) ADC-ADCSRA@ 3 BIT@ ;
: ADC-ADCSRA-ADIE! ( flag -- ) ADC-ADCSRA@ 3 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADIF@ ( -- flag ) ADC-ADCSRA@ 4 BIT@ ;
: ADC-ADCSRA-ADIF! ( flag -- ) ADC-ADCSRA@ 4 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADATE@ ( -- flag ) ADC-ADCSRA@ 5 BIT@ ;
: ADC-ADCSRA-ADATE! ( flag -- ) ADC-ADCSRA@ 5 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADSC@ ( -- flag ) ADC-ADCSRA@ 6 BIT@ ;
: ADC-ADCSRA-ADSC! ( flag -- ) ADC-ADCSRA@ 6 BIT! ADC-ADCSRA! ;
: ADC-ADCSRA-ADEN@ ( -- flag ) ADC-ADCSRA@ 7 BIT@ ;
: ADC-ADCSRA-ADEN! ( flag -- ) ADC-ADCSRA@ 7 BIT! ADC-ADCSRA! ;
: ADC-ADCH@ ( -- n ) ADC-ADCH C@ ;
: ADC-ADCH! ( n -- ) ADC-ADCH C! ;
: ADC-ADCL@ ( -- n ) ADC-ADCL C@ ;
: ADC-ADCL! ( n -- ) ADC-ADCL C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ATMEGA328P-INIT ( -- )
  \ 初始化ATmega328P设备
  ." 初始化ATmega328P..." CR

  \ 初始化寄存器
  0 R0!  \ General Purpose Register 0
  0 R1!  \ General Purpose Register 1
  0 R2!  \ General Purpose Register 2
  0 R3!  \ General Purpose Register 3
  0 R4!  \ General Purpose Register 4
  0 R5!  \ General Purpose Register 5
  0 R6!  \ General Purpose Register 6
  0 R7!  \ General Purpose Register 7
  0 R8!  \ General Purpose Register 8
  0 R9!  \ General Purpose Register 9
  0 R10!  \ General Purpose Register 10
  0 R11!  \ General Purpose Register 11
  0 R12!  \ General Purpose Register 12
  0 R13!  \ General Purpose Register 13
  0 R14!  \ General Purpose Register 14
  0 R15!  \ General Purpose Register 15
  0 R16!  \ General Purpose Register 16
  0 R17!  \ General Purpose Register 17
  0 R18!  \ General Purpose Register 18
  0 R19!  \ General Purpose Register 19
  0 R20!  \ General Purpose Register 20
  0 R21!  \ General Purpose Register 21
  0 R22!  \ General Purpose Register 22
  0 R23!  \ General Purpose Register 23
  0 R24!  \ General Purpose Register 24
  0 R25!  \ General Purpose Register 25
  0 R26!  \ General Purpose Register 26 (XL)
  0 R27!  \ General Purpose Register 27 (XH)
  0 R28!  \ General Purpose Register 28 (YL)
  0 R29!  \ General Purpose Register 29 (YH)
  0 R30!  \ General Purpose Register 30 (ZL)
  0 R31!  \ General Purpose Register 31 (ZH)
  0 SPL!  \ Stack Pointer Low
  0 SPH!  \ Stack Pointer High
  0 SREG!  \ Status Register

  \ 初始化外设
  \ 初始化PORTB
  0 PORTB-PORTB!  \ PORTB寄存器
  0 PORTB-DDRB!  \ DDRB寄存器
  0 PORTB-PINB!  \ PINB寄存器
  \ 初始化PORTC
  0 PORTC-PORTC!  \ PORTC寄存器
  0 PORTC-DDRC!  \ DDRC寄存器
  0 PORTC-PINC!  \ PINC寄存器
  \ 初始化PORTD
  0 PORTD-PORTD!  \ PORTD寄存器
  0 PORTD-DDRD!  \ DDRD寄存器
  0 PORTD-PIND!  \ PIND寄存器
  \ 初始化TIMER0
  0 TIMER0-TCCR0A!  \ TCCR0A寄存器
  0 TIMER0-TCCR0B!  \ TCCR0B寄存器
  0 TIMER0-TCNT0!  \ TCNT0寄存器
  0 TIMER0-OCR0A!  \ OCR0A寄存器
  0 TIMER0-OCR0B!  \ OCR0B寄存器
  0 TIMER0-TIMSK0!  \ TIMSK0寄存器
  0 TIMER0-TIFR0!  \ TIFR0寄存器
  \ 初始化USART0
  0 USART0-UDR0!  \ UDR0寄存器
  0 USART0-UCSR0A!  \ UCSR0A寄存器
  0 USART0-UCSR0B!  \ UCSR0B寄存器
  0 USART0-UCSR0C!  \ UCSR0C寄存器
  0 USART0-UBRR0!  \ UBRR0寄存器
  \ 初始化ADC
  0 ADC-ADMUX!  \ ADMUX寄存器
  0 ADC-ADCSRA!  \ ADCSRA寄存器
  0 ADC-ADCH!  \ ADCH寄存器
  0 ADC-ADCL!  \ ADCL寄存器

  ." ATmega328P初始化完成" CR
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
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  R6@ R6 .R 8 .R SPACE ."  R6: " R6@ .
  R7@ R7 .R 8 .R SPACE ."  R7: " R7@ .
  R8@ R8 .R 8 .R SPACE ."  R8: " R8@ .
  R9@ R9 .R 8 .R SPACE ."  R9: " R9@ .
  R10@ R10 .R 8 .R SPACE ."  R10: " R10@ .
  R11@ R11 .R 8 .R SPACE ."  R11: " R11@ .
  R12@ R12 .R 8 .R SPACE ."  R12: " R12@ .
  R13@ R13 .R 8 .R SPACE ."  R13: " R13@ .
  R14@ R14 .R 8 .R SPACE ."  R14: " R14@ .
  R15@ R15 .R 8 .R SPACE ."  R15: " R15@ .
  R16@ R16 .R 8 .R SPACE ."  R16: " R16@ .
  R17@ R17 .R 8 .R SPACE ."  R17: " R17@ .
  R18@ R18 .R 8 .R SPACE ."  R18: " R18@ .
  R19@ R19 .R 8 .R SPACE ."  R19: " R19@ .
  R20@ R20 .R 8 .R SPACE ."  R20: " R20@ .
  R21@ R21 .R 8 .R SPACE ."  R21: " R21@ .
  R22@ R22 .R 8 .R SPACE ."  R22: " R22@ .
  R23@ R23 .R 8 .R SPACE ."  R23: " R23@ .
  R24@ R24 .R 8 .R SPACE ."  R24: " R24@ .
  R25@ R25 .R 8 .R SPACE ."  R25: " R25@ .
  R26@ R26 .R 8 .R SPACE ."  R26: " R26@ .
  R27@ R27 .R 8 .R SPACE ."  R27: " R27@ .
  R28@ R28 .R 8 .R SPACE ."  R28: " R28@ .
  R29@ R29 .R 8 .R SPACE ."  R29: " R29@ .
  R30@ R30 .R 8 .R SPACE ."  R30: " R30@ .
  R31@ R31 .R 8 .R SPACE ."  R31: " R31@ .
  SPL@ SPL .R 8 .R SPACE ."  SPL: " SPL@ .
  SPH@ SPH .R 8 .R SPACE ."  SPH: " SPH@ .
  SREG@ SREG .R 8 .R SPACE ."  SREG: " SREG@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ External Interrupt Request 0
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

\ External Interrupt Request 1
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

\ Pin Change Interrupt Request 0
: INT-PCINT0-HANDLER ( -- )
  ." PCINT0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PCINT0-ENABLE ( -- )
  INT-PCINT0 INT-ENABLE
;

: INT-PCINT0-DISABLE ( -- )
  INT-PCINT0 INT-DISABLE
;

\ Pin Change Interrupt Request 1
: INT-PCINT1-HANDLER ( -- )
  ." PCINT1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PCINT1-ENABLE ( -- )
  INT-PCINT1 INT-ENABLE
;

: INT-PCINT1-DISABLE ( -- )
  INT-PCINT1 INT-DISABLE
;

\ Pin Change Interrupt Request 2
: INT-PCINT2-HANDLER ( -- )
  ." PCINT2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PCINT2-ENABLE ( -- )
  INT-PCINT2 INT-ENABLE
;

: INT-PCINT2-DISABLE ( -- )
  INT-PCINT2 INT-DISABLE
;

\ Watchdog Time-out Interrupt
: INT-WDT-HANDLER ( -- )
  ." WDT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-WDT-ENABLE ( -- )
  INT-WDT INT-ENABLE
;

: INT-WDT-DISABLE ( -- )
  INT-WDT INT-DISABLE
;

\ Timer/Counter2 Compare Match A
: INT-TIMER2_COMPA-HANDLER ( -- )
  ." TIMER2_COMPA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER2_COMPA-ENABLE ( -- )
  INT-TIMER2_COMPA INT-ENABLE
;

: INT-TIMER2_COMPA-DISABLE ( -- )
  INT-TIMER2_COMPA INT-DISABLE
;

\ Timer/Counter2 Compare Match B
: INT-TIMER2_COMPB-HANDLER ( -- )
  ." TIMER2_COMPB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER2_COMPB-ENABLE ( -- )
  INT-TIMER2_COMPB INT-ENABLE
;

: INT-TIMER2_COMPB-DISABLE ( -- )
  INT-TIMER2_COMPB INT-DISABLE
;

\ Timer/Counter2 Overflow
: INT-TIMER2_OVF-HANDLER ( -- )
  ." TIMER2_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER2_OVF-ENABLE ( -- )
  INT-TIMER2_OVF INT-ENABLE
;

: INT-TIMER2_OVF-DISABLE ( -- )
  INT-TIMER2_OVF INT-DISABLE
;

\ Timer/Counter1 Capture Event
: INT-TIMER1_CAPT-HANDLER ( -- )
  ." TIMER1_CAPT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER1_CAPT-ENABLE ( -- )
  INT-TIMER1_CAPT INT-ENABLE
;

: INT-TIMER1_CAPT-DISABLE ( -- )
  INT-TIMER1_CAPT INT-DISABLE
;

\ Timer/Counter1 Compare Match A
: INT-TIMER1_COMPA-HANDLER ( -- )
  ." TIMER1_COMPA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER1_COMPA-ENABLE ( -- )
  INT-TIMER1_COMPA INT-ENABLE
;

: INT-TIMER1_COMPA-DISABLE ( -- )
  INT-TIMER1_COMPA INT-DISABLE
;

\ Timer/Counter1 Compare Match B
: INT-TIMER1_COMPB-HANDLER ( -- )
  ." TIMER1_COMPB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER1_COMPB-ENABLE ( -- )
  INT-TIMER1_COMPB INT-ENABLE
;

: INT-TIMER1_COMPB-DISABLE ( -- )
  INT-TIMER1_COMPB INT-DISABLE
;

\ Timer/Counter1 Overflow
: INT-TIMER1_OVF-HANDLER ( -- )
  ." TIMER1_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER1_OVF-ENABLE ( -- )
  INT-TIMER1_OVF INT-ENABLE
;

: INT-TIMER1_OVF-DISABLE ( -- )
  INT-TIMER1_OVF INT-DISABLE
;

\ Timer/Counter0 Compare Match A
: INT-TIMER0_COMPA-HANDLER ( -- )
  ." TIMER0_COMPA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER0_COMPA-ENABLE ( -- )
  INT-TIMER0_COMPA INT-ENABLE
;

: INT-TIMER0_COMPA-DISABLE ( -- )
  INT-TIMER0_COMPA INT-DISABLE
;

\ Timer/Counter0 Compare Match B
: INT-TIMER0_COMPB-HANDLER ( -- )
  ." TIMER0_COMPB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER0_COMPB-ENABLE ( -- )
  INT-TIMER0_COMPB INT-ENABLE
;

: INT-TIMER0_COMPB-DISABLE ( -- )
  INT-TIMER0_COMPB INT-DISABLE
;

\ Timer/Counter0 Overflow
: INT-TIMER0_OVF-HANDLER ( -- )
  ." TIMER0_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER0_OVF-ENABLE ( -- )
  INT-TIMER0_OVF INT-ENABLE
;

: INT-TIMER0_OVF-DISABLE ( -- )
  INT-TIMER0_OVF INT-DISABLE
;

\ SPI Serial Transfer Complete
: INT-SPI_STC-HANDLER ( -- )
  ." SPI_STC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPI_STC-ENABLE ( -- )
  INT-SPI_STC INT-ENABLE
;

: INT-SPI_STC-DISABLE ( -- )
  INT-SPI_STC INT-DISABLE
;

\ USART Rx Complete
: INT-USART_RX-HANDLER ( -- )
  ." USART_RX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART_RX-ENABLE ( -- )
  INT-USART_RX INT-ENABLE
;

: INT-USART_RX-DISABLE ( -- )
  INT-USART_RX INT-DISABLE
;

\ USART Data Register Empty
: INT-USART_UDRE-HANDLER ( -- )
  ." USART_UDRE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART_UDRE-ENABLE ( -- )
  INT-USART_UDRE INT-ENABLE
;

: INT-USART_UDRE-DISABLE ( -- )
  INT-USART_UDRE INT-DISABLE
;

\ USART Tx Complete
: INT-USART_TX-HANDLER ( -- )
  ." USART_TX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART_TX-ENABLE ( -- )
  INT-USART_TX INT-ENABLE
;

: INT-USART_TX-DISABLE ( -- )
  INT-USART_TX INT-DISABLE
;

\ ADC Conversion Complete
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

\ EEPROM Ready
: INT-EE_READY-HANDLER ( -- )
  ." EE_READY中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EE_READY-ENABLE ( -- )
  INT-EE_READY INT-ENABLE
;

: INT-EE_READY-DISABLE ( -- )
  INT-EE_READY INT-DISABLE
;

\ Analog Comparator
: INT-ANALOG_COMP-HANDLER ( -- )
  ." ANALOG_COMP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-ANALOG_COMP-ENABLE ( -- )
  INT-ANALOG_COMP INT-ENABLE
;

: INT-ANALOG_COMP-DISABLE ( -- )
  INT-ANALOG_COMP INT-DISABLE
;

\ Two-wire Serial Interface
: INT-TWI-HANDLER ( -- )
  ." TWI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TWI-ENABLE ( -- )
  INT-TWI INT-ENABLE
;

: INT-TWI-DISABLE ( -- )
  INT-TWI INT-DISABLE
;

\ Store Program Memory Ready
: INT-SPM_READY-HANDLER ( -- )
  ." SPM_READY中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPM_READY-ENABLE ( -- )
  INT-SPM_READY INT-ENABLE
;

: INT-SPM_READY-DISABLE ( -- )
  INT-SPM_READY INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ATMEGA328P-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
