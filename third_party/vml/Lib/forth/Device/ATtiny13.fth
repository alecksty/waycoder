\ ATtiny13设备定义 - Forth文件
\ 生成自: Atmel/AVR/ATtiny13
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
\ CPU架构: AVR
\ 位宽: 8位
\ 时钟频率: 20000000 Hz

\ =========================================
\ ATtiny13设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ATtiny13" ;
: MANUFACTURER  S" Atmel" ;
: FAMILY        S" AVR" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" AVR" ;
8 CONSTANT BITS
20000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x01 CONSTANT R1  \ 
0x02 CONSTANT R2  \ 
0x10 CONSTANT R16  \ 
0x11 CONSTANT R17  \ 
0x1A CONSTANT R26  \ XL
0x1B CONSTANT R27  \ XH
0x1C CONSTANT R28  \ YL
0x1D CONSTANT R29  \ YH
0x1E CONSTANT R30  \ ZL
0x1F CONSTANT R31  \ ZH
0x5D CONSTANT SPL  \ Stack Pointer Low
0x5E CONSTANT SPH  \ Stack Pointer High
0x5F CONSTANT SREG  \ Status Register

\ 内存段定义
0x0000 CONSTANT FLASH-START
0x03FF CONSTANT FLASH-END
1024 CONSTANT FLASH-SIZE  \ 
0x0060 CONSTANT SRAM-START
0x009F CONSTANT SRAM-END
64 CONSTANT SRAM-SIZE  \ 
0x0000 CONSTANT EEPROM-START
0x003F CONSTANT EEPROM-END
64 CONSTANT EEPROM-SIZE  \ 
0x00 CONSTANT IO-START
0x1F CONSTANT IO-END
32 CONSTANT IO-SIZE  \ 
0x20 CONSTANT EXTIO-START
0x5F CONSTANT EXTIO-END
64 CONSTANT EXTIO-SIZE  \ 

\ 外设定义
\ Port B (only port)
0x18 CONSTANT PORTB-BASE
0x17 CONSTANT PORTB-DDRB
0x18 CONSTANT PORTB-PORTB
0x19 CONSTANT PORTB-PINB
\ 8-bit Timer/Counter0
0x33 CONSTANT TIMER0-BASE
0x33 CONSTANT TIMER0-TCCR0A
0x33 CONSTANT TIMER0-TCCR0B
0x32 CONSTANT TIMER0-TCNT0
0x36 CONSTANT TIMER0-OCR0A
0x35 CONSTANT TIMER0-OCR0B
0x39 CONSTANT TIMER0-TIMSK0
0x38 CONSTANT TIMER0-TIFR0
\ Analog-to-Digital
0x04 CONSTANT ADC-BASE
0x07 CONSTANT ADC-ADMUX
0x06 CONSTANT ADC-ADCSRA
0x04 CONSTANT ADC-ADCL
0x05 CONSTANT ADC-ADCH

\ 中断向量定义
1 CONSTANT INT-RESET  \ 
2 CONSTANT INT-INT0  \ External Interrupt 0
3 CONSTANT INT-PCINT0  \ Pin Change Interrupt
4 CONSTANT INT-TIM0_OVF  \ Timer0 Overflow
5 CONSTANT INT-TIM0_COMPA  \ Timer0 Compare A
6 CONSTANT INT-WDT  \ Watchdog Timeout
7 CONSTANT INT-ADC  \ ADC Conversion Complete

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

: R16@ ( -- n ) R16 C@ ;
: R16! ( n -- ) R16 C! ;

: R17@ ( -- n ) R17 C@ ;
: R17! ( n -- ) R17 C! ;

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
: PORTB-DDRB@ ( -- n ) PORTB-DDRB C@ ;
: PORTB-DDRB! ( n -- ) PORTB-DDRB C! ;
: PORTB-PORTB@ ( -- n ) PORTB-PORTB C@ ;
: PORTB-PORTB! ( n -- ) PORTB-PORTB C! ;
: PORTB-PINB@ ( -- n ) PORTB-PINB C@ ;
: PORTB-PINB! ( n -- ) PORTB-PINB C! ;

\ TIMER0外设
: TIMER0-TCCR0A@ ( -- n ) TIMER0-TCCR0A C@ ;
: TIMER0-TCCR0A! ( n -- ) TIMER0-TCCR0A C! ;
: TIMER0-TCCR0B@ ( -- n ) TIMER0-TCCR0B C@ ;
: TIMER0-TCCR0B! ( n -- ) TIMER0-TCCR0B C! ;
: TIMER0-TCNT0@ ( -- n ) TIMER0-TCNT0 C@ ;
: TIMER0-TCNT0! ( n -- ) TIMER0-TCNT0 C! ;
: TIMER0-OCR0A@ ( -- n ) TIMER0-OCR0A C@ ;
: TIMER0-OCR0A! ( n -- ) TIMER0-OCR0A C! ;
: TIMER0-OCR0B@ ( -- n ) TIMER0-OCR0B C@ ;
: TIMER0-OCR0B! ( n -- ) TIMER0-OCR0B C! ;
: TIMER0-TIMSK0@ ( -- n ) TIMER0-TIMSK0 C@ ;
: TIMER0-TIMSK0! ( n -- ) TIMER0-TIMSK0 C! ;
: TIMER0-TIFR0@ ( -- n ) TIMER0-TIFR0 C@ ;
: TIMER0-TIFR0! ( n -- ) TIMER0-TIFR0 C! ;

\ ADC外设
: ADC-ADMUX@ ( -- n ) ADC-ADMUX C@ ;
: ADC-ADMUX! ( n -- ) ADC-ADMUX C! ;
: ADC-ADCSRA@ ( -- n ) ADC-ADCSRA C@ ;
: ADC-ADCSRA! ( n -- ) ADC-ADCSRA C! ;
: ADC-ADCL@ ( -- n ) ADC-ADCL C@ ;
: ADC-ADCL! ( n -- ) ADC-ADCL C! ;
: ADC-ADCH@ ( -- n ) ADC-ADCH C@ ;
: ADC-ADCH! ( n -- ) ADC-ADCH C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ATTINY13-INIT ( -- )
  \ 初始化ATtiny13设备
  ." 初始化ATtiny13..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R16!  \ 
  0 R17!  \ 
  0 R26!  \ XL
  0 R27!  \ XH
  0 R28!  \ YL
  0 R29!  \ YH
  0 R30!  \ ZL
  0 R31!  \ ZH
  0 SPL!  \ Stack Pointer Low
  0 SPH!  \ Stack Pointer High
  0 SREG!  \ Status Register

  \ 初始化外设
  \ 初始化PORTB
  0 PORTB-DDRB!  \ DDRB寄存器
  0 PORTB-PORTB!  \ PORTB寄存器
  0 PORTB-PINB!  \ PINB寄存器
  \ 初始化TIMER0
  0 TIMER0-TCCR0A!  \ TCCR0A寄存器
  0 TIMER0-TCCR0B!  \ TCCR0B寄存器
  0 TIMER0-TCNT0!  \ TCNT0寄存器
  0 TIMER0-OCR0A!  \ OCR0A寄存器
  0 TIMER0-OCR0B!  \ OCR0B寄存器
  0 TIMER0-TIMSK0!  \ TIMSK0寄存器
  0 TIMER0-TIFR0!  \ TIFR0寄存器
  \ 初始化ADC
  0 ADC-ADMUX!  \ ADMUX寄存器
  0 ADC-ADCSRA!  \ ADCSRA寄存器
  0 ADC-ADCL!  \ ADCL寄存器
  0 ADC-ADCH!  \ ADCH寄存器

  ." ATtiny13初始化完成" CR
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
  R16@ R16 .R 8 .R SPACE ."  R16: " R16@ .
  R17@ R17 .R 8 .R SPACE ."  R17: " R17@ .
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

\ 
: INT-RESET-HANDLER ( -- )
  ." Reset中断处理" CR
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

\ Pin Change Interrupt
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

\ Timer0 Overflow
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

\ Timer0 Compare A
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
  ATTINY13-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
