\ ATmega32U4设备定义 - Forth文件
\ 生成自: Atmel/AVR/ATmega32U4
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
\ CPU架构: AVR
\ 位宽: 8位
\ 时钟频率: 16000000 Hz

\ =========================================
\ ATmega32U4设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ATmega32U4" ;
: MANUFACTURER  S" Atmel" ;
: FAMILY        S" AVR" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" AVR" ;
8 CONSTANT BITS
16000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x01 CONSTANT R1  \ 
0x02 CONSTANT R2  \ 
0x03 CONSTANT R3  \ 
0x04 CONSTANT R4  \ 
0x05 CONSTANT R5  \ 
0x06 CONSTANT R6  \ 
0x07 CONSTANT R7  \ 
0x08 CONSTANT R8  \ 
0x09 CONSTANT R9  \ 
0x0A CONSTANT R10  \ 
0x0B CONSTANT R11  \ 
0x0C CONSTANT R12  \ 
0x0D CONSTANT R13  \ 
0x0E CONSTANT R14  \ 
0x0F CONSTANT R15  \ 
0x10 CONSTANT R16  \ 
0x11 CONSTANT R17  \ 
0x12 CONSTANT R18  \ 
0x13 CONSTANT R19  \ 
0x14 CONSTANT R20  \ 
0x15 CONSTANT R21  \ 
0x16 CONSTANT R22  \ 
0x17 CONSTANT R23  \ 
0x18 CONSTANT R24  \ 
0x19 CONSTANT R25  \ 
0x1A CONSTANT R26  \ 
0x1B CONSTANT R27  \ 
0x1C CONSTANT R28  \ 
0x1D CONSTANT R29  \ 
0x1E CONSTANT R30  \ 
0x1F CONSTANT R31  \ 
0x5D CONSTANT SPL  \ 
0x5E CONSTANT SPH  \ 
0x5F CONSTANT SREG  \ 

\ 内存段定义
0x0000 CONSTANT FLASH-START
0x7FFF CONSTANT FLASH-END
32768 CONSTANT FLASH-SIZE  \ Program Flash Memory
0x0100 CONSTANT SRAM-START
0x0AFF CONSTANT SRAM-END
2560 CONSTANT SRAM-SIZE  \ Static RAM
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
\ Port B
0x23 CONSTANT PORTB-BASE
0x25 CONSTANT PORTB-PORTB
0x24 CONSTANT PORTB-DDRB
0x23 CONSTANT PORTB-PINB
\ Port C
0x26 CONSTANT PORTC-BASE
0x28 CONSTANT PORTC-PORTC
0x27 CONSTANT PORTC-DDRC
0x26 CONSTANT PORTC-PINC
\ Port D
0x29 CONSTANT PORTD-BASE
0x2B CONSTANT PORTD-PORTD
0x2A CONSTANT PORTD-DDRD
0x29 CONSTANT PORTD-PIND
\ Port E
0x2C CONSTANT PORTE-BASE
0x2E CONSTANT PORTE-PORTE
0x2D CONSTANT PORTE-DDRE
0x2C CONSTANT PORTE-PINE
\ USART1
0xC8 CONSTANT UART1-BASE
0xCE CONSTANT UART1-UDR1
0xC8 CONSTANT UART1-UCSR1A
0xC9 CONSTANT UART1-UCSR1B
0xCA CONSTANT UART1-UCSR1C
0xCC CONSTANT UART1-UBRR1
\ USB Controller
0xD0 CONSTANT USB-BASE
0xD0 CONSTANT USB-UDCON
0xD1 CONSTANT USB-UDIEN
0xD2 CONSTANT USB-UDINT

\ 中断向量定义
1 CONSTANT INT-INT0  \ External Interrupt 0
2 CONSTANT INT-INT1  \ External Interrupt 1
3 CONSTANT INT-INT2  \ External Interrupt 2
4 CONSTANT INT-INT3  \ External Interrupt 3
5 CONSTANT INT-INT4  \ External Interrupt 4
6 CONSTANT INT-INT5  \ External Interrupt 5
7 CONSTANT INT-INT6  \ External Interrupt 6
8 CONSTANT INT-PCINT0  \ Pin Change Interrupt 0
9 CONSTANT INT-USB_GENERAL  \ USB General
10 CONSTANT INT-USB_ENDPOINT  \ USB Endpoint
11 CONSTANT INT-WDT  \ Watchdog Timeout
12 CONSTANT INT-TIMER1_CAPT  \ Timer1 Capture
13 CONSTANT INT-TIMER1_COMPA  \ Timer1 Compare A
14 CONSTANT INT-TIMER1_COMPB  \ Timer1 Compare B
15 CONSTANT INT-TIMER1_OVF  \ Timer1 Overflow
16 CONSTANT INT-TIMER0_COMPA  \ Timer0 Compare A
17 CONSTANT INT-TIMER0_COMPB  \ Timer0 Compare B
18 CONSTANT INT-TIMER0_OVF  \ Timer0 Overflow
19 CONSTANT INT-SPI_STC  \ SPI Transfer Complete
20 CONSTANT INT-UART1_RX  \ UART1 Receive
21 CONSTANT INT-UART1_UDRE  \ UART1 Data Register Empty
22 CONSTANT INT-UART1_TX  \ UART1 Transmit
23 CONSTANT INT-ADC  \ ADC Conversion Complete

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

\ PORTE外设
: PORTE-PORTE@ ( -- n ) PORTE-PORTE C@ ;
: PORTE-PORTE! ( n -- ) PORTE-PORTE C! ;
: PORTE-DDRE@ ( -- n ) PORTE-DDRE C@ ;
: PORTE-DDRE! ( n -- ) PORTE-DDRE C! ;
: PORTE-PINE@ ( -- n ) PORTE-PINE C@ ;
: PORTE-PINE! ( n -- ) PORTE-PINE C! ;

\ UART1外设
: UART1-UDR1@ ( -- n ) UART1-UDR1 C@ ;
: UART1-UDR1! ( n -- ) UART1-UDR1 C! ;
: UART1-UCSR1A@ ( -- n ) UART1-UCSR1A C@ ;
: UART1-UCSR1A! ( n -- ) UART1-UCSR1A C! ;
: UART1-UCSR1B@ ( -- n ) UART1-UCSR1B C@ ;
: UART1-UCSR1B! ( n -- ) UART1-UCSR1B C! ;
: UART1-UCSR1C@ ( -- n ) UART1-UCSR1C C@ ;
: UART1-UCSR1C! ( n -- ) UART1-UCSR1C C! ;
: UART1-UBRR1@ ( -- n ) UART1-UBRR1 @ ;
: UART1-UBRR1! ( n -- ) UART1-UBRR1 ! ;

\ USB外设
: USB-UDCON@ ( -- n ) USB-UDCON C@ ;
: USB-UDCON! ( n -- ) USB-UDCON C! ;
: USB-UDIEN@ ( -- n ) USB-UDIEN C@ ;
: USB-UDIEN! ( n -- ) USB-UDIEN C! ;
: USB-UDINT@ ( -- n ) USB-UDINT C@ ;
: USB-UDINT! ( n -- ) USB-UDINT C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ATMEGA32U4-INIT ( -- )
  \ 初始化ATmega32U4设备
  ." 初始化ATmega32U4..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R3!  \ 
  0 R4!  \ 
  0 R5!  \ 
  0 R6!  \ 
  0 R7!  \ 
  0 R8!  \ 
  0 R9!  \ 
  0 R10!  \ 
  0 R11!  \ 
  0 R12!  \ 
  0 R13!  \ 
  0 R14!  \ 
  0 R15!  \ 
  0 R16!  \ 
  0 R17!  \ 
  0 R18!  \ 
  0 R19!  \ 
  0 R20!  \ 
  0 R21!  \ 
  0 R22!  \ 
  0 R23!  \ 
  0 R24!  \ 
  0 R25!  \ 
  0 R26!  \ 
  0 R27!  \ 
  0 R28!  \ 
  0 R29!  \ 
  0 R30!  \ 
  0 R31!  \ 
  0 SPL!  \ 
  0 SPH!  \ 
  0 SREG!  \ 

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
  \ 初始化PORTE
  0 PORTE-PORTE!  \ PORTE寄存器
  0 PORTE-DDRE!  \ DDRE寄存器
  0 PORTE-PINE!  \ PINE寄存器
  \ 初始化UART1
  0 UART1-UDR1!  \ UDR1寄存器
  0 UART1-UCSR1A!  \ UCSR1A寄存器
  0 UART1-UCSR1B!  \ UCSR1B寄存器
  0 UART1-UCSR1C!  \ UCSR1C寄存器
  0 UART1-UBRR1!  \ UBRR1寄存器
  \ 初始化USB
  0 USB-UDCON!  \ UDCON寄存器
  0 USB-UDIEN!  \ UDIEN寄存器
  0 USB-UDINT!  \ UDINT寄存器

  ." ATmega32U4初始化完成" CR
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
\ 中断处理
\ =========================================

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

\ External Interrupt 3
: INT-INT3-HANDLER ( -- )
  ." INT3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT3-ENABLE ( -- )
  INT-INT3 INT-ENABLE
;

: INT-INT3-DISABLE ( -- )
  INT-INT3 INT-DISABLE
;

\ External Interrupt 4
: INT-INT4-HANDLER ( -- )
  ." INT4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT4-ENABLE ( -- )
  INT-INT4 INT-ENABLE
;

: INT-INT4-DISABLE ( -- )
  INT-INT4 INT-DISABLE
;

\ External Interrupt 5
: INT-INT5-HANDLER ( -- )
  ." INT5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT5-ENABLE ( -- )
  INT-INT5 INT-ENABLE
;

: INT-INT5-DISABLE ( -- )
  INT-INT5 INT-DISABLE
;

\ External Interrupt 6
: INT-INT6-HANDLER ( -- )
  ." INT6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT6-ENABLE ( -- )
  INT-INT6 INT-ENABLE
;

: INT-INT6-DISABLE ( -- )
  INT-INT6 INT-DISABLE
;

\ Pin Change Interrupt 0
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

\ USB General
: INT-USB_GENERAL-HANDLER ( -- )
  ." USB_General中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USB_GENERAL-ENABLE ( -- )
  INT-USB_GENERAL INT-ENABLE
;

: INT-USB_GENERAL-DISABLE ( -- )
  INT-USB_GENERAL INT-DISABLE
;

\ USB Endpoint
: INT-USB_ENDPOINT-HANDLER ( -- )
  ." USB_Endpoint中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USB_ENDPOINT-ENABLE ( -- )
  INT-USB_ENDPOINT INT-ENABLE
;

: INT-USB_ENDPOINT-DISABLE ( -- )
  INT-USB_ENDPOINT INT-DISABLE
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

\ Timer1 Capture
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

\ Timer1 Compare A
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

\ Timer1 Compare B
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

\ Timer1 Overflow
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

\ Timer0 Compare A
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

\ Timer0 Compare B
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

\ Timer0 Overflow
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

\ SPI Transfer Complete
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

\ UART1 Receive
: INT-UART1_RX-HANDLER ( -- )
  ." UART1_RX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UART1_RX-ENABLE ( -- )
  INT-UART1_RX INT-ENABLE
;

: INT-UART1_RX-DISABLE ( -- )
  INT-UART1_RX INT-DISABLE
;

\ UART1 Data Register Empty
: INT-UART1_UDRE-HANDLER ( -- )
  ." UART1_UDRE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UART1_UDRE-ENABLE ( -- )
  INT-UART1_UDRE INT-ENABLE
;

: INT-UART1_UDRE-DISABLE ( -- )
  INT-UART1_UDRE INT-DISABLE
;

\ UART1 Transmit
: INT-UART1_TX-HANDLER ( -- )
  ." UART1_TX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UART1_TX-ENABLE ( -- )
  INT-UART1_TX INT-ENABLE
;

: INT-UART1_TX-DISABLE ( -- )
  INT-UART1_TX INT-DISABLE
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ATMEGA32U4-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
