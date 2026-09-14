\ ATtiny85设备定义 - Forth文件
\ 生成自: Microchip/AVR/ATtiny85
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
\ CPU架构: AVR
\ 位宽: 8位
\ 时钟频率: 1000000 Hz

\ =========================================
\ ATtiny85设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ATtiny85" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" AVR" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" AVR" ;
8 CONSTANT BITS
1000000 CONSTANT CLOCK-FREQ

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
0x1A CONSTANT X  \ Register pair X (R27:R26)
0x1C CONSTANT Y  \ Register pair Y (R29:R28)
0x1E CONSTANT Z  \ Register pair Z (R31:R30)
0x3D CONSTANT SP  \ Stack Pointer
0x3F CONSTANT SREG  \ Status Register
0 CONSTANT SREG-C  \ Carry Flag
1 CONSTANT SREG-Z  \ Zero Flag
2 CONSTANT SREG-N  \ Negative Flag
3 CONSTANT SREG-V  \ Two's Complement Overflow Flag
4 CONSTANT SREG-S  \ Sign Flag (N xor V)
5 CONSTANT SREG-H  \ Half Carry Flag
6 CONSTANT SREG-T  \ Transfer Bit
7 CONSTANT SREG-I  \ Global Interrupt Enable

\ 内存段定义
0x0000 CONSTANT FLASH-START
0x1FFF CONSTANT FLASH-END
8192 CONSTANT FLASH-SIZE  \ Program Flash (8KB)
0x0060 CONSTANT SRAM-START
0x025F CONSTANT SRAM-END
512 CONSTANT SRAM-SIZE  \ Internal SRAM (512B)
0x0000 CONSTANT EEPROM-START
0x01FF CONSTANT EEPROM-END
512 CONSTANT EEPROM-SIZE  \ EEPROM (512B)
0x00 CONSTANT IO-START
0x3F CONSTANT IO-END
64 CONSTANT IO-SIZE  \ I/O Registers

\ 外设定义
\ Port A
0x20 CONSTANT PORTA-BASE
0x20 CONSTANT PORTA-PINA
0x21 CONSTANT PORTA-DDRA
0x22 CONSTANT PORTA-PORTA
\ Port B
0x18 CONSTANT PORTB-BASE
0x16 CONSTANT PORTB-PINB
0x17 CONSTANT PORTB-DDRB
0x18 CONSTANT PORTB-PORTB
\ Timer/Counter0
0x20 CONSTANT TIPO-BASE
0x20 CONSTANT TIPO-TCCR0A
0 CONSTANT TIPO-TCCR0A-WGM00  \ Waveform Generation Mode
1 CONSTANT TIPO-TCCR0A-WGM01  \ Waveform Generation Mode
4 CONSTANT TIPO-TCCR0A-COM0B0  \ Compare Output Mode B
5 CONSTANT TIPO-TCCR0A-COM0B1  \ Compare Output Mode B
6 CONSTANT TIPO-TCCR0A-COM0A0  \ Compare Output Mode A
7 CONSTANT TIPO-TCCR0A-COM0A1  \ Compare Output Mode A
0x21 CONSTANT TIPO-TCCR0B
0 CONSTANT TIPO-TCCR0B-CS00  \ Clock Select
1 CONSTANT TIPO-TCCR0B-CS01  \ Clock Select
2 CONSTANT TIPO-TCCR0B-CS02  \ Clock Select
3 CONSTANT TIPO-TCCR0B-WGM02  \ Waveform Generation Mode
6 CONSTANT TIPO-TCCR0B-FOC0B  \ Force Output Compare B
7 CONSTANT TIPO-TCCR0B-FOC0A  \ Force Output Compare A
0x22 CONSTANT TIPO-TCNT0
0x23 CONSTANT TIPO-OCR0A
0x24 CONSTANT TIPO-OCR0B
0x39 CONSTANT TIPO-TIMSK
0 CONSTANT TIPO-TIMSK-TOIE0  \ Timer/Counter0 Overflow Interrupt Enable
1 CONSTANT TIPO-TIMSK-OCIE0A  \ Output Compare A Match Interrupt Enable
2 CONSTANT TIPO-TIMSK-OCIE0B  \ Output Compare B Match Interrupt Enable
0x38 CONSTANT TIPO-TIFR
0 CONSTANT TIPO-TIFR-TOV0  \ Timer/Counter0 Overflow Flag
1 CONSTANT TIPO-TIFR-OCF0A  \ Output Compare A Flag
2 CONSTANT TIPO-TIFR-OCF0B  \ Output Compare B Flag
\ Timer/Counter1
0x28 CONSTANT TMR1-BASE
0x28 CONSTANT TMR1-TCCR1A
0 CONSTANT TMR1-TCCR1A-PCM1  \ PWM Mode
0 CONSTANT TMR1-TCCR1A-COM1A  \ Compare Output Mode A
0 CONSTANT TMR1-TCCR1A-COM1B  \ Compare Output Mode B
1 CONSTANT TMR1-TCCR1A-WG13  \ Waveform Generation Mode
0 CONSTANT TMR1-TCCR1A-WG10  \ Waveform Generation Mode
0x29 CONSTANT TMR1-TCCR1B
7 CONSTANT TMR1-TCCR1B-CTC1  \ Clear Timer on Compare
4 CONSTANT TMR1-TCCR1B-WGM13  \ Waveform Generation Mode
3 CONSTANT TMR1-TCCR1B-WGM12  \ Waveform Generation Mode
0 CONSTANT TMR1-TCCR1B-CS1  \ Clock Select
0x2A CONSTANT TMR1-TCNT1
0x2C CONSTANT TMR1-OCR1A
0x2E CONSTANT TMR1-OCR1B
0x30 CONSTANT TMR1-OCR1C
0x33 CONSTANT TMR1-TIMSK1
0x32 CONSTANT TMR1-TIFR1
\ ADC Multiplexer
0x12 CONSTANT ADMUX-BASE
0x12 CONSTANT ADMUX-ADMUX
0 CONSTANT ADMUX-ADMUX-MUX  \ Analog Channel Selection
5 CONSTANT ADMUX-ADMUX-ADLAR  \ ADC Left Adjust Result
0 CONSTANT ADMUX-ADMUX-REFS  \ Reference Selection
0x13 CONSTANT ADMUX-ADCSRA
0 CONSTANT ADMUX-ADCSRA-ADPS  \ ADC Prescaler Select
3 CONSTANT ADMUX-ADCSRA-ADIE  \ ADC Interrupt Enable
4 CONSTANT ADMUX-ADCSRA-ADIF  \ ADC Interrupt Flag
5 CONSTANT ADMUX-ADCSRA-ADATE  \ ADC Auto Trigger Enable
6 CONSTANT ADMUX-ADCSRA-ADSC  \ ADC Start Conversion
7 CONSTANT ADMUX-ADCSRA-ADEN  \ ADC Enable
0x14 CONSTANT ADMUX-ADCH
0x15 CONSTANT ADMUX-ADCL
\ Universal Serial Interface
0x18 CONSTANT USI-BASE
0x18 CONSTANT USI-USIDR
0x19 CONSTANT USI-USISR
0 CONSTANT USI-USISR-USICNT  \ Counter
4 CONSTANT USI-USISR-USIDC  \ Data Register
5 CONSTANT USI-USISR-USIPF  \ Stop Cond Flag
6 CONSTANT USI-USISR-USIOV  \ Overflow Flag
7 CONSTANT USI-USISR-USISIF  \ Start Cond Interrupt Flag
0x1A CONSTANT USI-USICR
0 CONSTANT USI-USICR-USICS  \ Clock Source Select
2 CONSTANT USI-USICR-USISCL  \ SCL strobe
3 CONSTANT USI-USICR-USIOW  \ SDA output override
4 CONSTANT USI-USICR-USIOE  \ Output Enable
5 CONSTANT USI-USICR-USISRE  \ Start Recognition Enable
6 CONSTANT USI-USICR-USIORE  \ Stop Recognition Enable
7 CONSTANT USI-USICR-USIGIE  \ Global Interrupt Enable
0x1B CONSTANT USI-USIPORT
\ MCU Control
0x35 CONSTANT MCUCR-BASE
0x35 CONSTANT MCUCR-MCUCR
0 CONSTANT MCUCR-MCUCR-ISC  \ Interrupt Sense Control
4 CONSTANT MCUCR-MCUCR-SE  \ Sleep Enable
0 CONSTANT MCUCR-MCUCR-SM  \ Sleep Mode
0x36 CONSTANT MCUCR-MCUCSR
0 CONSTANT MCUCR-MCUCSR-PORF  \ Power-on Reset Flag
1 CONSTANT MCUCR-MCUCSR-EXTRF  \ External Reset Flag
2 CONSTANT MCUCR-MCUCSR-WDRF  \ Watchdog Reset Flag
4 CONSTANT MCUCR-MCUCSR-BORF  \ Brown-out Reset Flag
\ Watchdog Timer
0x21 CONSTANT WDTCR-BASE
0x21 CONSTANT WDTCR-WDTCR
0 CONSTANT WDTCR-WDTCR-WDP  \ Watchdog Prescaler
3 CONSTANT WDTCR-WDTCR-WDE  \ Watchdog Enable
4 CONSTANT WDTCR-WDTCR-WDIE  \ Watchdog Interrupt Enable
\ EEPROM
0x1C CONSTANT EEPR-BASE
0x1E CONSTANT EEPR-EEAR
0x1D CONSTANT EEPR-EEDR
0x1F CONSTANT EEPR-EECR
0 CONSTANT EEPR-EECR-EEPM  \ EEPROM Programming Mode
3 CONSTANT EEPR-EECR-EERIE  \ EEPROM Ready Interrupt Enable
2 CONSTANT EEPR-EECR-EEWE  \ EEPROM Write Enable
1 CONSTANT EEPR-EECR-EEMWE  \ EEPROM Master Write Enable
0 CONSTANT EEPR-EECR-EERE  \ EEPROM Read Enable
\ External Interrupt
0x3B CONSTANT GIMSK-BASE
0x3B CONSTANT GIMSK-GIMSK
0 CONSTANT GIMSK-GIMSK-INT0  \ External Interrupt Request 0 Enable
1 CONSTANT GIMSK-GIMSK-PCIE  \ Pin Change Interrupt Enable
0x3C CONSTANT GIMSK-GIFR
0 CONSTANT GIMSK-GIFR-INTF0  \ External Interrupt Flag 0
1 CONSTANT GIMSK-GIFR-PCIF  \ Pin Change Interrupt Flag
\ Pin Change Mask
0x15 CONSTANT PCMSK-BASE
0x15 CONSTANT PCMSK-PCMSK
\ Store Program Memory
0x37 CONSTANT SPMCSR-BASE
0x37 CONSTANT SPMCSR-SPMCSR
0 CONSTANT SPMCSR-SPMCSR-SPMCR  \ SPM Mode
1 CONSTANT SPMCSR-SPMCSR-PGERS  \ Page Erase
2 CONSTANT SPMCSR-SPMCSR-PGWRT  \ Page Write
3 CONSTANT SPMCSR-SPMCSR-BLBSET  \ Boot Lock Bits Set
4 CONSTANT SPMCSR-SPMCSR-RWWSRE  \ Read-While-Read Strobe Enable
5 CONSTANT SPMCSR-SPMCSR-SIGRD  \ Signature Row Read
7 CONSTANT SPMCSR-SPMCSR-SPMEN  \ SPM Enable

\ 中断向量定义
0 CONSTANT INT-RESET  \ External Reset, Power-on Reset, Brown-out Reset
1 CONSTANT INT-INT0  \ External Interrupt Request 0
2 CONSTANT INT-PCINT0  \ Pin Change
3 CONSTANT INT-WDT  \ Watchdog Timeout
4 CONSTANT INT-TIM1_COMPA  \ Timer/Counter1 Compare Match A
5 CONSTANT INT-TIM1_OVF  \ Timer/Counter1 Overflow
6 CONSTANT INT-TIM0_COMPA  \ Timer/Counter0 Compare Match A
7 CONSTANT INT-TIM0_OVF  \ Timer/Counter0 Overflow
8 CONSTANT INT-SPI_STC  \ SPI Serial Transfer Complete
9 CONSTANT INT-ADC  \ ADC Conversion Complete
10 CONSTANT INT-USI_START  \ USI Start Condition
11 CONSTANT INT-USI_OVF  \ USI Overflow
12 CONSTANT INT-EE_READY  \ EEPROM Ready

\ 引脚定义
1 CONSTANT PIN-PB5  \ RESET - ADC0 - dW
2 CONSTANT PIN-PB3  \ XTAL1 - CLKI - ADC3
3 CONSTANT PIN-PB4  \ XTAL2 - ADC2
4 CONSTANT PIN-PB0  \ MOSI - AI - ADC0 - T0 - INT0
5 CONSTANT PIN-PB1  \ MISO - AI - ADC1 - OC1A - INT1
6 CONSTANT PIN-PB2  \ SCK - AI - ADC3 - OC1B
7 CONSTANT PIN-VCC  \ Supply Voltage
8 CONSTANT PIN-GND  \ Ground

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

: X@ ( -- n ) X @ ;
: X! ( n -- ) X ! ;

: Y@ ( -- n ) Y @ ;
: Y! ( n -- ) Y ! ;

: Z@ ( -- n ) Z @ ;
: Z! ( n -- ) Z ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

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
\ PORTA外设
: PORTA-PINA@ ( -- n ) PORTA-PINA C@ ;
: PORTA-PINA! ( n -- ) PORTA-PINA C! ;
: PORTA-DDRA@ ( -- n ) PORTA-DDRA C@ ;
: PORTA-DDRA! ( n -- ) PORTA-DDRA C! ;
: PORTA-PORTA@ ( -- n ) PORTA-PORTA C@ ;
: PORTA-PORTA! ( n -- ) PORTA-PORTA C! ;

\ PORTB外设
: PORTB-PINB@ ( -- n ) PORTB-PINB C@ ;
: PORTB-PINB! ( n -- ) PORTB-PINB C! ;
: PORTB-DDRB@ ( -- n ) PORTB-DDRB C@ ;
: PORTB-DDRB! ( n -- ) PORTB-DDRB C! ;
: PORTB-PORTB@ ( -- n ) PORTB-PORTB C@ ;
: PORTB-PORTB! ( n -- ) PORTB-PORTB C! ;

\ TIPO外设
: TIPO-TCCR0A@ ( -- n ) TIPO-TCCR0A C@ ;
: TIPO-TCCR0A! ( n -- ) TIPO-TCCR0A C! ;
: TIPO-TCCR0A-WGM00@ ( -- flag ) TIPO-TCCR0A@ 0 BIT@ ;
: TIPO-TCCR0A-WGM00! ( flag -- ) TIPO-TCCR0A@ 0 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0A-WGM01@ ( -- flag ) TIPO-TCCR0A@ 1 BIT@ ;
: TIPO-TCCR0A-WGM01! ( flag -- ) TIPO-TCCR0A@ 1 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0A-COM0B0@ ( -- flag ) TIPO-TCCR0A@ 4 BIT@ ;
: TIPO-TCCR0A-COM0B0! ( flag -- ) TIPO-TCCR0A@ 4 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0A-COM0B1@ ( -- flag ) TIPO-TCCR0A@ 5 BIT@ ;
: TIPO-TCCR0A-COM0B1! ( flag -- ) TIPO-TCCR0A@ 5 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0A-COM0A0@ ( -- flag ) TIPO-TCCR0A@ 6 BIT@ ;
: TIPO-TCCR0A-COM0A0! ( flag -- ) TIPO-TCCR0A@ 6 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0A-COM0A1@ ( -- flag ) TIPO-TCCR0A@ 7 BIT@ ;
: TIPO-TCCR0A-COM0A1! ( flag -- ) TIPO-TCCR0A@ 7 BIT! TIPO-TCCR0A! ;
: TIPO-TCCR0B@ ( -- n ) TIPO-TCCR0B C@ ;
: TIPO-TCCR0B! ( n -- ) TIPO-TCCR0B C! ;
: TIPO-TCCR0B-CS00@ ( -- flag ) TIPO-TCCR0B@ 0 BIT@ ;
: TIPO-TCCR0B-CS00! ( flag -- ) TIPO-TCCR0B@ 0 BIT! TIPO-TCCR0B! ;
: TIPO-TCCR0B-CS01@ ( -- flag ) TIPO-TCCR0B@ 1 BIT@ ;
: TIPO-TCCR0B-CS01! ( flag -- ) TIPO-TCCR0B@ 1 BIT! TIPO-TCCR0B! ;
: TIPO-TCCR0B-CS02@ ( -- flag ) TIPO-TCCR0B@ 2 BIT@ ;
: TIPO-TCCR0B-CS02! ( flag -- ) TIPO-TCCR0B@ 2 BIT! TIPO-TCCR0B! ;
: TIPO-TCCR0B-WGM02@ ( -- flag ) TIPO-TCCR0B@ 3 BIT@ ;
: TIPO-TCCR0B-WGM02! ( flag -- ) TIPO-TCCR0B@ 3 BIT! TIPO-TCCR0B! ;
: TIPO-TCCR0B-FOC0B@ ( -- flag ) TIPO-TCCR0B@ 6 BIT@ ;
: TIPO-TCCR0B-FOC0B! ( flag -- ) TIPO-TCCR0B@ 6 BIT! TIPO-TCCR0B! ;
: TIPO-TCCR0B-FOC0A@ ( -- flag ) TIPO-TCCR0B@ 7 BIT@ ;
: TIPO-TCCR0B-FOC0A! ( flag -- ) TIPO-TCCR0B@ 7 BIT! TIPO-TCCR0B! ;
: TIPO-TCNT0@ ( -- n ) TIPO-TCNT0 C@ ;
: TIPO-TCNT0! ( n -- ) TIPO-TCNT0 C! ;
: TIPO-OCR0A@ ( -- n ) TIPO-OCR0A C@ ;
: TIPO-OCR0A! ( n -- ) TIPO-OCR0A C! ;
: TIPO-OCR0B@ ( -- n ) TIPO-OCR0B C@ ;
: TIPO-OCR0B! ( n -- ) TIPO-OCR0B C! ;
: TIPO-TIMSK@ ( -- n ) TIPO-TIMSK C@ ;
: TIPO-TIMSK! ( n -- ) TIPO-TIMSK C! ;
: TIPO-TIMSK-TOIE0@ ( -- flag ) TIPO-TIMSK@ 0 BIT@ ;
: TIPO-TIMSK-TOIE0! ( flag -- ) TIPO-TIMSK@ 0 BIT! TIPO-TIMSK! ;
: TIPO-TIMSK-OCIE0A@ ( -- flag ) TIPO-TIMSK@ 1 BIT@ ;
: TIPO-TIMSK-OCIE0A! ( flag -- ) TIPO-TIMSK@ 1 BIT! TIPO-TIMSK! ;
: TIPO-TIMSK-OCIE0B@ ( -- flag ) TIPO-TIMSK@ 2 BIT@ ;
: TIPO-TIMSK-OCIE0B! ( flag -- ) TIPO-TIMSK@ 2 BIT! TIPO-TIMSK! ;
: TIPO-TIFR@ ( -- n ) TIPO-TIFR C@ ;
: TIPO-TIFR! ( n -- ) TIPO-TIFR C! ;
: TIPO-TIFR-TOV0@ ( -- flag ) TIPO-TIFR@ 0 BIT@ ;
: TIPO-TIFR-TOV0! ( flag -- ) TIPO-TIFR@ 0 BIT! TIPO-TIFR! ;
: TIPO-TIFR-OCF0A@ ( -- flag ) TIPO-TIFR@ 1 BIT@ ;
: TIPO-TIFR-OCF0A! ( flag -- ) TIPO-TIFR@ 1 BIT! TIPO-TIFR! ;
: TIPO-TIFR-OCF0B@ ( -- flag ) TIPO-TIFR@ 2 BIT@ ;
: TIPO-TIFR-OCF0B! ( flag -- ) TIPO-TIFR@ 2 BIT! TIPO-TIFR! ;

\ TMR1外设
: TMR1-TCCR1A@ ( -- n ) TMR1-TCCR1A C@ ;
: TMR1-TCCR1A! ( n -- ) TMR1-TCCR1A C! ;
: TMR1-TCCR1A-PCM1@ ( -- flag ) TMR1-TCCR1A@ 0 BIT@ ;
: TMR1-TCCR1A-PCM1! ( flag -- ) TMR1-TCCR1A@ 0 BIT! TMR1-TCCR1A! ;
: TMR1-TCCR1A-COM1A@ ( -- flag ) TMR1-TCCR1A@ 0 BIT@ ;
: TMR1-TCCR1A-COM1A! ( flag -- ) TMR1-TCCR1A@ 0 BIT! TMR1-TCCR1A! ;
: TMR1-TCCR1A-COM1B@ ( -- flag ) TMR1-TCCR1A@ 0 BIT@ ;
: TMR1-TCCR1A-COM1B! ( flag -- ) TMR1-TCCR1A@ 0 BIT! TMR1-TCCR1A! ;
: TMR1-TCCR1A-WG13@ ( -- flag ) TMR1-TCCR1A@ 1 BIT@ ;
: TMR1-TCCR1A-WG13! ( flag -- ) TMR1-TCCR1A@ 1 BIT! TMR1-TCCR1A! ;
: TMR1-TCCR1A-WG10@ ( -- flag ) TMR1-TCCR1A@ 0 BIT@ ;
: TMR1-TCCR1A-WG10! ( flag -- ) TMR1-TCCR1A@ 0 BIT! TMR1-TCCR1A! ;
: TMR1-TCCR1B@ ( -- n ) TMR1-TCCR1B C@ ;
: TMR1-TCCR1B! ( n -- ) TMR1-TCCR1B C! ;
: TMR1-TCCR1B-CTC1@ ( -- flag ) TMR1-TCCR1B@ 7 BIT@ ;
: TMR1-TCCR1B-CTC1! ( flag -- ) TMR1-TCCR1B@ 7 BIT! TMR1-TCCR1B! ;
: TMR1-TCCR1B-WGM13@ ( -- flag ) TMR1-TCCR1B@ 4 BIT@ ;
: TMR1-TCCR1B-WGM13! ( flag -- ) TMR1-TCCR1B@ 4 BIT! TMR1-TCCR1B! ;
: TMR1-TCCR1B-WGM12@ ( -- flag ) TMR1-TCCR1B@ 3 BIT@ ;
: TMR1-TCCR1B-WGM12! ( flag -- ) TMR1-TCCR1B@ 3 BIT! TMR1-TCCR1B! ;
: TMR1-TCCR1B-CS1@ ( -- flag ) TMR1-TCCR1B@ 0 BIT@ ;
: TMR1-TCCR1B-CS1! ( flag -- ) TMR1-TCCR1B@ 0 BIT! TMR1-TCCR1B! ;
: TMR1-TCNT1@ ( -- n ) TMR1-TCNT1 @ ;
: TMR1-TCNT1! ( n -- ) TMR1-TCNT1 ! ;
: TMR1-OCR1A@ ( -- n ) TMR1-OCR1A @ ;
: TMR1-OCR1A! ( n -- ) TMR1-OCR1A ! ;
: TMR1-OCR1B@ ( -- n ) TMR1-OCR1B @ ;
: TMR1-OCR1B! ( n -- ) TMR1-OCR1B ! ;
: TMR1-OCR1C@ ( -- n ) TMR1-OCR1C @ ;
: TMR1-OCR1C! ( n -- ) TMR1-OCR1C ! ;
: TMR1-TIMSK1@ ( -- n ) TMR1-TIMSK1 C@ ;
: TMR1-TIMSK1! ( n -- ) TMR1-TIMSK1 C! ;
: TMR1-TIFR1@ ( -- n ) TMR1-TIFR1 C@ ;
: TMR1-TIFR1! ( n -- ) TMR1-TIFR1 C! ;

\ ADMUX外设
: ADMUX-ADMUX@ ( -- n ) ADMUX-ADMUX C@ ;
: ADMUX-ADMUX! ( n -- ) ADMUX-ADMUX C! ;
: ADMUX-ADMUX-MUX@ ( -- flag ) ADMUX-ADMUX@ 0 BIT@ ;
: ADMUX-ADMUX-MUX! ( flag -- ) ADMUX-ADMUX@ 0 BIT! ADMUX-ADMUX! ;
: ADMUX-ADMUX-ADLAR@ ( -- flag ) ADMUX-ADMUX@ 5 BIT@ ;
: ADMUX-ADMUX-ADLAR! ( flag -- ) ADMUX-ADMUX@ 5 BIT! ADMUX-ADMUX! ;
: ADMUX-ADMUX-REFS@ ( -- flag ) ADMUX-ADMUX@ 0 BIT@ ;
: ADMUX-ADMUX-REFS! ( flag -- ) ADMUX-ADMUX@ 0 BIT! ADMUX-ADMUX! ;
: ADMUX-ADCSRA@ ( -- n ) ADMUX-ADCSRA C@ ;
: ADMUX-ADCSRA! ( n -- ) ADMUX-ADCSRA C! ;
: ADMUX-ADCSRA-ADPS@ ( -- flag ) ADMUX-ADCSRA@ 0 BIT@ ;
: ADMUX-ADCSRA-ADPS! ( flag -- ) ADMUX-ADCSRA@ 0 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCSRA-ADIE@ ( -- flag ) ADMUX-ADCSRA@ 3 BIT@ ;
: ADMUX-ADCSRA-ADIE! ( flag -- ) ADMUX-ADCSRA@ 3 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCSRA-ADIF@ ( -- flag ) ADMUX-ADCSRA@ 4 BIT@ ;
: ADMUX-ADCSRA-ADIF! ( flag -- ) ADMUX-ADCSRA@ 4 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCSRA-ADATE@ ( -- flag ) ADMUX-ADCSRA@ 5 BIT@ ;
: ADMUX-ADCSRA-ADATE! ( flag -- ) ADMUX-ADCSRA@ 5 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCSRA-ADSC@ ( -- flag ) ADMUX-ADCSRA@ 6 BIT@ ;
: ADMUX-ADCSRA-ADSC! ( flag -- ) ADMUX-ADCSRA@ 6 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCSRA-ADEN@ ( -- flag ) ADMUX-ADCSRA@ 7 BIT@ ;
: ADMUX-ADCSRA-ADEN! ( flag -- ) ADMUX-ADCSRA@ 7 BIT! ADMUX-ADCSRA! ;
: ADMUX-ADCH@ ( -- n ) ADMUX-ADCH C@ ;
: ADMUX-ADCH! ( n -- ) ADMUX-ADCH C! ;
: ADMUX-ADCL@ ( -- n ) ADMUX-ADCL C@ ;
: ADMUX-ADCL! ( n -- ) ADMUX-ADCL C! ;

\ USI外设
: USI-USIDR@ ( -- n ) USI-USIDR C@ ;
: USI-USIDR! ( n -- ) USI-USIDR C! ;
: USI-USISR@ ( -- n ) USI-USISR C@ ;
: USI-USISR! ( n -- ) USI-USISR C! ;
: USI-USISR-USICNT@ ( -- flag ) USI-USISR@ 0 BIT@ ;
: USI-USISR-USICNT! ( flag -- ) USI-USISR@ 0 BIT! USI-USISR! ;
: USI-USISR-USIDC@ ( -- flag ) USI-USISR@ 4 BIT@ ;
: USI-USISR-USIDC! ( flag -- ) USI-USISR@ 4 BIT! USI-USISR! ;
: USI-USISR-USIPF@ ( -- flag ) USI-USISR@ 5 BIT@ ;
: USI-USISR-USIPF! ( flag -- ) USI-USISR@ 5 BIT! USI-USISR! ;
: USI-USISR-USIOV@ ( -- flag ) USI-USISR@ 6 BIT@ ;
: USI-USISR-USIOV! ( flag -- ) USI-USISR@ 6 BIT! USI-USISR! ;
: USI-USISR-USISIF@ ( -- flag ) USI-USISR@ 7 BIT@ ;
: USI-USISR-USISIF! ( flag -- ) USI-USISR@ 7 BIT! USI-USISR! ;
: USI-USICR@ ( -- n ) USI-USICR C@ ;
: USI-USICR! ( n -- ) USI-USICR C! ;
: USI-USICR-USICS@ ( -- flag ) USI-USICR@ 0 BIT@ ;
: USI-USICR-USICS! ( flag -- ) USI-USICR@ 0 BIT! USI-USICR! ;
: USI-USICR-USISCL@ ( -- flag ) USI-USICR@ 2 BIT@ ;
: USI-USICR-USISCL! ( flag -- ) USI-USICR@ 2 BIT! USI-USICR! ;
: USI-USICR-USIOW@ ( -- flag ) USI-USICR@ 3 BIT@ ;
: USI-USICR-USIOW! ( flag -- ) USI-USICR@ 3 BIT! USI-USICR! ;
: USI-USICR-USIOE@ ( -- flag ) USI-USICR@ 4 BIT@ ;
: USI-USICR-USIOE! ( flag -- ) USI-USICR@ 4 BIT! USI-USICR! ;
: USI-USICR-USISRE@ ( -- flag ) USI-USICR@ 5 BIT@ ;
: USI-USICR-USISRE! ( flag -- ) USI-USICR@ 5 BIT! USI-USICR! ;
: USI-USICR-USIORE@ ( -- flag ) USI-USICR@ 6 BIT@ ;
: USI-USICR-USIORE! ( flag -- ) USI-USICR@ 6 BIT! USI-USICR! ;
: USI-USICR-USIGIE@ ( -- flag ) USI-USICR@ 7 BIT@ ;
: USI-USICR-USIGIE! ( flag -- ) USI-USICR@ 7 BIT! USI-USICR! ;
: USI-USIPORT@ ( -- n ) USI-USIPORT C@ ;
: USI-USIPORT! ( n -- ) USI-USIPORT C! ;

\ MCUCR外设
: MCUCR-MCUCR@ ( -- n ) MCUCR-MCUCR C@ ;
: MCUCR-MCUCR! ( n -- ) MCUCR-MCUCR C! ;
: MCUCR-MCUCR-ISC@ ( -- flag ) MCUCR-MCUCR@ 0 BIT@ ;
: MCUCR-MCUCR-ISC! ( flag -- ) MCUCR-MCUCR@ 0 BIT! MCUCR-MCUCR! ;
: MCUCR-MCUCR-SE@ ( -- flag ) MCUCR-MCUCR@ 4 BIT@ ;
: MCUCR-MCUCR-SE! ( flag -- ) MCUCR-MCUCR@ 4 BIT! MCUCR-MCUCR! ;
: MCUCR-MCUCR-SM@ ( -- flag ) MCUCR-MCUCR@ 0 BIT@ ;
: MCUCR-MCUCR-SM! ( flag -- ) MCUCR-MCUCR@ 0 BIT! MCUCR-MCUCR! ;
: MCUCR-MCUCSR@ ( -- n ) MCUCR-MCUCSR C@ ;
: MCUCR-MCUCSR! ( n -- ) MCUCR-MCUCSR C! ;
: MCUCR-MCUCSR-PORF@ ( -- flag ) MCUCR-MCUCSR@ 0 BIT@ ;
: MCUCR-MCUCSR-PORF! ( flag -- ) MCUCR-MCUCSR@ 0 BIT! MCUCR-MCUCSR! ;
: MCUCR-MCUCSR-EXTRF@ ( -- flag ) MCUCR-MCUCSR@ 1 BIT@ ;
: MCUCR-MCUCSR-EXTRF! ( flag -- ) MCUCR-MCUCSR@ 1 BIT! MCUCR-MCUCSR! ;
: MCUCR-MCUCSR-WDRF@ ( -- flag ) MCUCR-MCUCSR@ 2 BIT@ ;
: MCUCR-MCUCSR-WDRF! ( flag -- ) MCUCR-MCUCSR@ 2 BIT! MCUCR-MCUCSR! ;
: MCUCR-MCUCSR-BORF@ ( -- flag ) MCUCR-MCUCSR@ 4 BIT@ ;
: MCUCR-MCUCSR-BORF! ( flag -- ) MCUCR-MCUCSR@ 4 BIT! MCUCR-MCUCSR! ;

\ WDTCR外设
: WDTCR-WDTCR@ ( -- n ) WDTCR-WDTCR C@ ;
: WDTCR-WDTCR! ( n -- ) WDTCR-WDTCR C! ;
: WDTCR-WDTCR-WDP@ ( -- flag ) WDTCR-WDTCR@ 0 BIT@ ;
: WDTCR-WDTCR-WDP! ( flag -- ) WDTCR-WDTCR@ 0 BIT! WDTCR-WDTCR! ;
: WDTCR-WDTCR-WDE@ ( -- flag ) WDTCR-WDTCR@ 3 BIT@ ;
: WDTCR-WDTCR-WDE! ( flag -- ) WDTCR-WDTCR@ 3 BIT! WDTCR-WDTCR! ;
: WDTCR-WDTCR-WDIE@ ( -- flag ) WDTCR-WDTCR@ 4 BIT@ ;
: WDTCR-WDTCR-WDIE! ( flag -- ) WDTCR-WDTCR@ 4 BIT! WDTCR-WDTCR! ;

\ EEPR外设
: EEPR-EEAR@ ( -- n ) EEPR-EEAR C@ ;
: EEPR-EEAR! ( n -- ) EEPR-EEAR C! ;
: EEPR-EEDR@ ( -- n ) EEPR-EEDR C@ ;
: EEPR-EEDR! ( n -- ) EEPR-EEDR C! ;
: EEPR-EECR@ ( -- n ) EEPR-EECR C@ ;
: EEPR-EECR! ( n -- ) EEPR-EECR C! ;
: EEPR-EECR-EEPM@ ( -- flag ) EEPR-EECR@ 0 BIT@ ;
: EEPR-EECR-EEPM! ( flag -- ) EEPR-EECR@ 0 BIT! EEPR-EECR! ;
: EEPR-EECR-EERIE@ ( -- flag ) EEPR-EECR@ 3 BIT@ ;
: EEPR-EECR-EERIE! ( flag -- ) EEPR-EECR@ 3 BIT! EEPR-EECR! ;
: EEPR-EECR-EEWE@ ( -- flag ) EEPR-EECR@ 2 BIT@ ;
: EEPR-EECR-EEWE! ( flag -- ) EEPR-EECR@ 2 BIT! EEPR-EECR! ;
: EEPR-EECR-EEMWE@ ( -- flag ) EEPR-EECR@ 1 BIT@ ;
: EEPR-EECR-EEMWE! ( flag -- ) EEPR-EECR@ 1 BIT! EEPR-EECR! ;
: EEPR-EECR-EERE@ ( -- flag ) EEPR-EECR@ 0 BIT@ ;
: EEPR-EECR-EERE! ( flag -- ) EEPR-EECR@ 0 BIT! EEPR-EECR! ;

\ GIMSK外设
: GIMSK-GIMSK@ ( -- n ) GIMSK-GIMSK C@ ;
: GIMSK-GIMSK! ( n -- ) GIMSK-GIMSK C! ;
: GIMSK-GIMSK-INT0@ ( -- flag ) GIMSK-GIMSK@ 0 BIT@ ;
: GIMSK-GIMSK-INT0! ( flag -- ) GIMSK-GIMSK@ 0 BIT! GIMSK-GIMSK! ;
: GIMSK-GIMSK-PCIE@ ( -- flag ) GIMSK-GIMSK@ 1 BIT@ ;
: GIMSK-GIMSK-PCIE! ( flag -- ) GIMSK-GIMSK@ 1 BIT! GIMSK-GIMSK! ;
: GIMSK-GIFR@ ( -- n ) GIMSK-GIFR C@ ;
: GIMSK-GIFR! ( n -- ) GIMSK-GIFR C! ;
: GIMSK-GIFR-INTF0@ ( -- flag ) GIMSK-GIFR@ 0 BIT@ ;
: GIMSK-GIFR-INTF0! ( flag -- ) GIMSK-GIFR@ 0 BIT! GIMSK-GIFR! ;
: GIMSK-GIFR-PCIF@ ( -- flag ) GIMSK-GIFR@ 1 BIT@ ;
: GIMSK-GIFR-PCIF! ( flag -- ) GIMSK-GIFR@ 1 BIT! GIMSK-GIFR! ;

\ PCMSK外设
: PCMSK-PCMSK@ ( -- n ) PCMSK-PCMSK C@ ;
: PCMSK-PCMSK! ( n -- ) PCMSK-PCMSK C! ;

\ SPMCSR外设
: SPMCSR-SPMCSR@ ( -- n ) SPMCSR-SPMCSR C@ ;
: SPMCSR-SPMCSR! ( n -- ) SPMCSR-SPMCSR C! ;
: SPMCSR-SPMCSR-SPMCR@ ( -- flag ) SPMCSR-SPMCSR@ 0 BIT@ ;
: SPMCSR-SPMCSR-SPMCR! ( flag -- ) SPMCSR-SPMCSR@ 0 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-PGERS@ ( -- flag ) SPMCSR-SPMCSR@ 1 BIT@ ;
: SPMCSR-SPMCSR-PGERS! ( flag -- ) SPMCSR-SPMCSR@ 1 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-PGWRT@ ( -- flag ) SPMCSR-SPMCSR@ 2 BIT@ ;
: SPMCSR-SPMCSR-PGWRT! ( flag -- ) SPMCSR-SPMCSR@ 2 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-BLBSET@ ( -- flag ) SPMCSR-SPMCSR@ 3 BIT@ ;
: SPMCSR-SPMCSR-BLBSET! ( flag -- ) SPMCSR-SPMCSR@ 3 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-RWWSRE@ ( -- flag ) SPMCSR-SPMCSR@ 4 BIT@ ;
: SPMCSR-SPMCSR-RWWSRE! ( flag -- ) SPMCSR-SPMCSR@ 4 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-SIGRD@ ( -- flag ) SPMCSR-SPMCSR@ 5 BIT@ ;
: SPMCSR-SPMCSR-SIGRD! ( flag -- ) SPMCSR-SPMCSR@ 5 BIT! SPMCSR-SPMCSR! ;
: SPMCSR-SPMCSR-SPMEN@ ( -- flag ) SPMCSR-SPMCSR@ 7 BIT@ ;
: SPMCSR-SPMCSR-SPMEN! ( flag -- ) SPMCSR-SPMCSR@ 7 BIT! SPMCSR-SPMCSR! ;

\ =========================================
\ 设备初始化
\ =========================================

: ATTINY85-INIT ( -- )
  \ 初始化ATtiny85设备
  ." 初始化ATtiny85..." CR

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
  0 X!  \ Register pair X (R27:R26)
  0 Y!  \ Register pair Y (R29:R28)
  0 Z!  \ Register pair Z (R31:R30)
  0 SP!  \ Stack Pointer
  0 SREG!  \ Status Register

  \ 初始化外设
  \ 初始化PORTA
  0 PORTA-PINA!  \ PINA寄存器
  0 PORTA-DDRA!  \ DDRA寄存器
  0 PORTA-PORTA!  \ PORTA寄存器
  \ 初始化PORTB
  0 PORTB-PINB!  \ PINB寄存器
  0 PORTB-DDRB!  \ DDRB寄存器
  0 PORTB-PORTB!  \ PORTB寄存器
  \ 初始化TIPO
  0 TIPO-TCCR0A!  \ TCCR0A寄存器
  0 TIPO-TCCR0B!  \ TCCR0B寄存器
  0 TIPO-TCNT0!  \ TCNT0寄存器
  0 TIPO-OCR0A!  \ OCR0A寄存器
  0 TIPO-OCR0B!  \ OCR0B寄存器
  0 TIPO-TIMSK!  \ TIMSK寄存器
  0 TIPO-TIFR!  \ TIFR寄存器
  \ 初始化TMR1
  0 TMR1-TCCR1A!  \ TCCR1A寄存器
  0 TMR1-TCCR1B!  \ TCCR1B寄存器
  0 TMR1-TCNT1!  \ TCNT1寄存器
  0 TMR1-OCR1A!  \ OCR1A寄存器
  0 TMR1-OCR1B!  \ OCR1B寄存器
  0 TMR1-OCR1C!  \ OCR1C寄存器
  0 TMR1-TIMSK1!  \ TIMSK1寄存器
  0 TMR1-TIFR1!  \ TIFR1寄存器
  \ 初始化ADMUX
  0 ADMUX-ADMUX!  \ ADMUX寄存器
  0 ADMUX-ADCSRA!  \ ADCSRA寄存器
  0 ADMUX-ADCH!  \ ADCH寄存器
  0 ADMUX-ADCL!  \ ADCL寄存器
  \ 初始化USI
  0 USI-USIDR!  \ USIDR寄存器
  0 USI-USISR!  \ USISR寄存器
  0 USI-USICR!  \ USICR寄存器
  0 USI-USIPORT!  \ USIPORT寄存器
  \ 初始化MCUCR
  0 MCUCR-MCUCR!  \ MCUCR寄存器
  0 MCUCR-MCUCSR!  \ MCUCSR寄存器
  \ 初始化WDTCR
  0 WDTCR-WDTCR!  \ WDTCR寄存器
  \ 初始化EEPR
  0 EEPR-EEAR!  \ EEAR寄存器
  0 EEPR-EEDR!  \ EEDR寄存器
  0 EEPR-EECR!  \ EECR寄存器
  \ 初始化GIMSK
  0 GIMSK-GIMSK!  \ GIMSK寄存器
  0 GIMSK-GIFR!  \ GIFR寄存器
  \ 初始化PCMSK
  0 PCMSK-PCMSK!  \ PCMSK寄存器
  \ 初始化SPMCSR
  0 SPMCSR-SPMCSR!  \ SPMCSR寄存器

  ." ATtiny85初始化完成" CR
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
  X@ X .R 8 .R SPACE ."  X: " X@ .
  Y@ Y .R 8 .R SPACE ."  Y: " Y@ .
  Z@ Z .R 8 .R SPACE ."  Z: " Z@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  SREG@ SREG .R 8 .R SPACE ."  SREG: " SREG@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ External Reset, Power-on Reset, Brown-out Reset
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

\ Pin Change
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

\ Watchdog Timeout
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

\ Timer/Counter1 Compare Match A
: INT-TIM1_COMPA-HANDLER ( -- )
  ." TIM1_COMPA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_COMPA-ENABLE ( -- )
  INT-TIM1_COMPA INT-ENABLE
;

: INT-TIM1_COMPA-DISABLE ( -- )
  INT-TIM1_COMPA INT-DISABLE
;

\ Timer/Counter1 Overflow
: INT-TIM1_OVF-HANDLER ( -- )
  ." TIM1_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_OVF-ENABLE ( -- )
  INT-TIM1_OVF INT-ENABLE
;

: INT-TIM1_OVF-DISABLE ( -- )
  INT-TIM1_OVF INT-DISABLE
;

\ Timer/Counter0 Compare Match A
: INT-TIM0_COMPA-HANDLER ( -- )
  ." TIM0_COMPA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM0_COMPA-ENABLE ( -- )
  INT-TIM0_COMPA INT-ENABLE
;

: INT-TIM0_COMPA-DISABLE ( -- )
  INT-TIM0_COMPA INT-DISABLE
;

\ Timer/Counter0 Overflow
: INT-TIM0_OVF-HANDLER ( -- )
  ." TIM0_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM0_OVF-ENABLE ( -- )
  INT-TIM0_OVF INT-ENABLE
;

: INT-TIM0_OVF-DISABLE ( -- )
  INT-TIM0_OVF INT-DISABLE
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

\ USI Start Condition
: INT-USI_START-HANDLER ( -- )
  ." USI_START中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USI_START-ENABLE ( -- )
  INT-USI_START INT-ENABLE
;

: INT-USI_START-DISABLE ( -- )
  INT-USI_START INT-DISABLE
;

\ USI Overflow
: INT-USI_OVF-HANDLER ( -- )
  ." USI_OVF中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USI_OVF-ENABLE ( -- )
  INT-USI_OVF INT-ENABLE
;

: INT-USI_OVF-DISABLE ( -- )
  INT-USI_OVF INT-DISABLE
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ATTINY85-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
