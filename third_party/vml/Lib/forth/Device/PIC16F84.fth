\ PIC16F84设备定义 - Forth文件
\ 生成自: Microchip Technology/PIC16/PIC16F84
\ 版本: 
\ 日期: 
\ 作者: 
\ 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
\ CPU架构: PIC16
\ 位宽: 0位
\ 时钟频率: 0 Hz

\ =========================================
\ PIC16F84设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PIC16F84" ;
: MANUFACTURER  S" Microchip Technology" ;
: FAMILY        S" PIC16" ;
: VERSION       S" " ;
: ARCHITECTURE  S" PIC16" ;
0 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ 8-bit timer/counter with prescaler
 CONSTANT TIMER0-BASE
0x01 CONSTANT TIMER0-TMR0
\ 16-bit timer/counter with prescaler
 CONSTANT TIMER1-BASE
0x0E CONSTANT TIMER1-TMR1L
0x0F CONSTANT TIMER1-TMR1H
0x10 CONSTANT TIMER1-T1CON
0 CONSTANT TIMER1-T1CON-TMR1ON  \ Timer1 On
1 CONSTANT TIMER1-T1CON-TMR1CS  \ Timer1 Clock Source
2 CONSTANT TIMER1-T1CON-T1SYNC  \ Timer1 External Clock Input Synchronization
3 CONSTANT TIMER1-T1CON-T1OSCEN  \ Timer1 Oscillator Enable
4 CONSTANT TIMER1-T1CON-T1CKPS0  \ Timer1 Input Clock Prescale Select bit 0
5 CONSTANT TIMER1-T1CON-T1CKPS1  \ Timer1 Input Clock Prescale Select bit 1
\ Watchdog Timer
 CONSTANT WATCHDOG-BASE
0x07 CONSTANT WATCHDOG-WDTCON
0 CONSTANT WATCHDOG-WDTCON-SWDTEN  \ Software Watchdog Timer Enable
\ 64-byte EEPROM data memory
 CONSTANT EEPROM-BASE
0x08 CONSTANT EEPROM-EEDATA
0x09 CONSTANT EEPROM-EEADR
0x88 CONSTANT EEPROM-EECON1
0x89 CONSTANT EEPROM-EECON2
\ General Purpose I/O
 CONSTANT GPIO-BASE
0x05 CONSTANT GPIO-PORTA
0x06 CONSTANT GPIO-PORTB
0x85 CONSTANT GPIO-TRISA
0x86 CONSTANT GPIO-TRISB

\ 中断向量定义
4 CONSTANT INT-INT  \ External interrupt on RB0/INT pin
4 CONSTANT INT-TMR0  \ Timer0 overflow interrupt
4 CONSTANT INT-PORTB  \ PORTB change interrupt (RB4-RB7)
4 CONSTANT INT-EEPROM  \ EEPROM write complete interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ Timer0外设
: TIMER0-TMR0@ ( -- n ) TIMER0-TMR0 XL@ ;
: TIMER0-TMR0! ( n -- ) TIMER0-TMR0 XL! ;

\ Timer1外设
: TIMER1-TMR1L@ ( -- n ) TIMER1-TMR1L XL@ ;
: TIMER1-TMR1L! ( n -- ) TIMER1-TMR1L XL! ;
: TIMER1-TMR1H@ ( -- n ) TIMER1-TMR1H XL@ ;
: TIMER1-TMR1H! ( n -- ) TIMER1-TMR1H XL! ;
: TIMER1-T1CON@ ( -- n ) TIMER1-T1CON XL@ ;
: TIMER1-T1CON! ( n -- ) TIMER1-T1CON XL! ;
: TIMER1-T1CON-TMR1ON@ ( -- flag ) TIMER1-T1CON@ 0 BIT@ ;
: TIMER1-T1CON-TMR1ON! ( flag -- ) TIMER1-T1CON@ 0 BIT! TIMER1-T1CON! ;
: TIMER1-T1CON-TMR1CS@ ( -- flag ) TIMER1-T1CON@ 1 BIT@ ;
: TIMER1-T1CON-TMR1CS! ( flag -- ) TIMER1-T1CON@ 1 BIT! TIMER1-T1CON! ;
: TIMER1-T1CON-T1SYNC@ ( -- flag ) TIMER1-T1CON@ 2 BIT@ ;
: TIMER1-T1CON-T1SYNC! ( flag -- ) TIMER1-T1CON@ 2 BIT! TIMER1-T1CON! ;
: TIMER1-T1CON-T1OSCEN@ ( -- flag ) TIMER1-T1CON@ 3 BIT@ ;
: TIMER1-T1CON-T1OSCEN! ( flag -- ) TIMER1-T1CON@ 3 BIT! TIMER1-T1CON! ;
: TIMER1-T1CON-T1CKPS0@ ( -- flag ) TIMER1-T1CON@ 4 BIT@ ;
: TIMER1-T1CON-T1CKPS0! ( flag -- ) TIMER1-T1CON@ 4 BIT! TIMER1-T1CON! ;
: TIMER1-T1CON-T1CKPS1@ ( -- flag ) TIMER1-T1CON@ 5 BIT@ ;
: TIMER1-T1CON-T1CKPS1! ( flag -- ) TIMER1-T1CON@ 5 BIT! TIMER1-T1CON! ;

\ Watchdog外设
: WATCHDOG-WDTCON@ ( -- n ) WATCHDOG-WDTCON XL@ ;
: WATCHDOG-WDTCON! ( n -- ) WATCHDOG-WDTCON XL! ;
: WATCHDOG-WDTCON-SWDTEN@ ( -- flag ) WATCHDOG-WDTCON@ 0 BIT@ ;
: WATCHDOG-WDTCON-SWDTEN! ( flag -- ) WATCHDOG-WDTCON@ 0 BIT! WATCHDOG-WDTCON! ;

\ EEPROM外设
: EEPROM-EEDATA@ ( -- n ) EEPROM-EEDATA XL@ ;
: EEPROM-EEDATA! ( n -- ) EEPROM-EEDATA XL! ;
: EEPROM-EEADR@ ( -- n ) EEPROM-EEADR XL@ ;
: EEPROM-EEADR! ( n -- ) EEPROM-EEADR XL! ;
: EEPROM-EECON1@ ( -- n ) EEPROM-EECON1 XL@ ;
: EEPROM-EECON1! ( n -- ) EEPROM-EECON1 XL! ;
: EEPROM-EECON2@ ( -- n ) EEPROM-EECON2 XL@ ;
: EEPROM-EECON2! ( n -- ) EEPROM-EECON2 XL! ;

\ GPIO外设
: GPIO-PORTA@ ( -- n ) GPIO-PORTA XL@ ;
: GPIO-PORTA! ( n -- ) GPIO-PORTA XL! ;
: GPIO-PORTB@ ( -- n ) GPIO-PORTB XL@ ;
: GPIO-PORTB! ( n -- ) GPIO-PORTB XL! ;
: GPIO-TRISA@ ( -- n ) GPIO-TRISA XL@ ;
: GPIO-TRISA! ( n -- ) GPIO-TRISA XL! ;
: GPIO-TRISB@ ( -- n ) GPIO-TRISB XL@ ;
: GPIO-TRISB! ( n -- ) GPIO-TRISB XL! ;

\ =========================================
\ 设备初始化
\ =========================================

: PIC16F84-INIT ( -- )
  \ 初始化PIC16F84设备
  ." 初始化PIC16F84..." CR


  \ 初始化外设
  \ 初始化Timer0
  0 TIMER0-TMR0!  \ TMR0寄存器
  \ 初始化Timer1
  0 TIMER1-TMR1L!  \ TMR1L寄存器
  0 TIMER1-TMR1H!  \ TMR1H寄存器
  0 TIMER1-T1CON!  \ T1CON寄存器
  \ 初始化Watchdog
  0 WATCHDOG-WDTCON!  \ WDTCON寄存器
  \ 初始化EEPROM
  0 EEPROM-EEDATA!  \ EEDATA寄存器
  0 EEPROM-EEADR!  \ EEADR寄存器
  0 EEPROM-EECON1!  \ EECON1寄存器
  0 EEPROM-EECON2!  \ EECON2寄存器
  \ 初始化GPIO
  0 GPIO-PORTA!  \ PORTA寄存器
  0 GPIO-PORTB!  \ PORTB寄存器
  0 GPIO-TRISA!  \ TRISA寄存器
  0 GPIO-TRISB!  \ TRISB寄存器

  ." PIC16F84初始化完成" CR
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

\ =========================================
\ 中断处理
\ =========================================

\ External interrupt on RB0/INT pin
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

\ Timer0 overflow interrupt
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

\ PORTB change interrupt (RB4-RB7)
: INT-PORTB-HANDLER ( -- )
  ." PORTB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PORTB-ENABLE ( -- )
  INT-PORTB INT-ENABLE
;

: INT-PORTB-DISABLE ( -- )
  INT-PORTB INT-DISABLE
;

\ EEPROM write complete interrupt
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
  PIC16F84-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
