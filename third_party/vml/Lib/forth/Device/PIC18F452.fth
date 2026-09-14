\ PIC18F452设备定义 - Forth文件
\ 生成自: Microchip Technology/PIC18/PIC18F452
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
\ CPU架构: PIC18
\ 位宽: 8位
\ 时钟频率: 20000000 Hz

\ =========================================
\ PIC18F452设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PIC18F452" ;
: MANUFACTURER  S" Microchip Technology" ;
: FAMILY        S" PIC18" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" PIC18" ;
8 CONSTANT BITS
20000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0xFE8 CONSTANT WREG  \ Working Register
0xFD8 CONSTANT STATUS  \ Status Register
0xFE0 CONSTANT BSR  \ Bank Select Register
0xFF9 CONSTANT PCL  \ Program Counter Low
0xFFA CONSTANT PCLATH  \ Program Counter Latch High
0xFFB CONSTANT PCLATU  \ Program Counter Latch Upper
0xFFF CONSTANT TOSU  \ Top of Stack Upper
0xFFE CONSTANT TOSH  \ Top of Stack High
0xFFD CONSTANT TOSL  \ Top of Stack Low

\ 外设定义
\ Port A
 CONSTANT PORTA-BASE
0xF80 CONSTANT PORTA-PORTA
0xF92 CONSTANT PORTA-TRISA
0xF89 CONSTANT PORTA-LATA
\ Port B
 CONSTANT PORTB-BASE
0xF81 CONSTANT PORTB-PORTB
0xF93 CONSTANT PORTB-TRISB
0xF8A CONSTANT PORTB-LATB
\ Port C
 CONSTANT PORTC-BASE
0xF82 CONSTANT PORTC-PORTC
0xF94 CONSTANT PORTC-TRISC
0xF8B CONSTANT PORTC-LATC
\ Port D
 CONSTANT PORTD-BASE
0xF83 CONSTANT PORTD-PORTD
0xF95 CONSTANT PORTD-TRISD
0xF8C CONSTANT PORTD-LATD
\ Port E
 CONSTANT PORTE-BASE
0xF84 CONSTANT PORTE-PORTE
0xF96 CONSTANT PORTE-TRISE
0xF8D CONSTANT PORTE-LATE
\ Timer0
 CONSTANT TMR0-BASE
0xFD6 CONSTANT TMR0-TMR0L
0xFD7 CONSTANT TMR0-TMR0H
0xFD5 CONSTANT TMR0-T0CON
\ Timer1
 CONSTANT TMR1-BASE
0xFCE CONSTANT TMR1-TMR1L
0xFCF CONSTANT TMR1-TMR1H
0xFCD CONSTANT TMR1-T1CON
\ Timer2
 CONSTANT TMR2-BASE
0xFCC CONSTANT TMR2-TMR2
0xFCB CONSTANT TMR2-PR2
0xFCA CONSTANT TMR2-T2CON
\ Timer3
 CONSTANT TMR3-BASE
0xFB2 CONSTANT TMR3-TMR3L
0xFB3 CONSTANT TMR3-TMR3H
0xFB1 CONSTANT TMR3-T3CON
\ Analog-to-Digital Converter
 CONSTANT ADC-BASE
0xFC3 CONSTANT ADC-ADRESL
0xFC4 CONSTANT ADC-ADRESH
0xFC2 CONSTANT ADC-ADCON0
0xFC1 CONSTANT ADC-ADCON1
\ Universal Synchronous Asynchronous Receiver Transmitter
 CONSTANT USART-BASE
0xFAC CONSTANT USART-TXREG
0xFAB CONSTANT USART-RCREG
0xFAF CONSTANT USART-SPBRG
0xFAD CONSTANT USART-TXSTA
0xFAE CONSTANT USART-RCSTA
\ Synchronous Serial Port
 CONSTANT SSP-BASE
0xFC9 CONSTANT SSP-SSPBUF
0xFC8 CONSTANT SSP-SSPADD
0xFC7 CONSTANT SSP-SSPSTAT
0xFC6 CONSTANT SSP-SSPCON1
0xFC5 CONSTANT SSP-SSPCON2
\ Capture/Compare/PWM 1
 CONSTANT CCP1-BASE
0xFBE CONSTANT CCP1-CCPR1L
0xFBF CONSTANT CCP1-CCPR1H
0xFBD CONSTANT CCP1-CCP1CON
\ Capture/Compare/PWM 2
 CONSTANT CCP2-BASE
0xFBA CONSTANT CCP2-CCPR2L
0xFBB CONSTANT CCP2-CCPR2H
0xFB9 CONSTANT CCP2-CCP2CON

\ 中断向量定义
8 CONSTANT INT-HIGH_PRIORITY  \ High priority interrupt
24 CONSTANT INT-LOW_PRIORITY  \ Low priority interrupt
0 CONSTANT INT-RESET  \ Reset vector

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: WREG@ ( -- n ) WREG C@ ;
: WREG! ( n -- ) WREG C! ;

: STATUS@ ( -- n ) STATUS C@ ;
: STATUS! ( n -- ) STATUS C! ;

: BSR@ ( -- n ) BSR C@ ;
: BSR! ( n -- ) BSR C! ;

: PCL@ ( -- n ) PCL C@ ;
: PCL! ( n -- ) PCL C! ;

: PCLATH@ ( -- n ) PCLATH C@ ;
: PCLATH! ( n -- ) PCLATH C! ;

: PCLATU@ ( -- n ) PCLATU C@ ;
: PCLATU! ( n -- ) PCLATU C! ;

: TOSU@ ( -- n ) TOSU C@ ;
: TOSU! ( n -- ) TOSU C! ;

: TOSH@ ( -- n ) TOSH C@ ;
: TOSH! ( n -- ) TOSH C! ;

: TOSL@ ( -- n ) TOSL C@ ;
: TOSL! ( n -- ) TOSL C! ;

\ 外设访问
\ PORTA外设
: PORTA-PORTA@ ( -- n ) PORTA-PORTA C@ ;
: PORTA-PORTA! ( n -- ) PORTA-PORTA C! ;
: PORTA-TRISA@ ( -- n ) PORTA-TRISA C@ ;
: PORTA-TRISA! ( n -- ) PORTA-TRISA C! ;
: PORTA-LATA@ ( -- n ) PORTA-LATA C@ ;
: PORTA-LATA! ( n -- ) PORTA-LATA C! ;

\ PORTB外设
: PORTB-PORTB@ ( -- n ) PORTB-PORTB C@ ;
: PORTB-PORTB! ( n -- ) PORTB-PORTB C! ;
: PORTB-TRISB@ ( -- n ) PORTB-TRISB C@ ;
: PORTB-TRISB! ( n -- ) PORTB-TRISB C! ;
: PORTB-LATB@ ( -- n ) PORTB-LATB C@ ;
: PORTB-LATB! ( n -- ) PORTB-LATB C! ;

\ PORTC外设
: PORTC-PORTC@ ( -- n ) PORTC-PORTC C@ ;
: PORTC-PORTC! ( n -- ) PORTC-PORTC C! ;
: PORTC-TRISC@ ( -- n ) PORTC-TRISC C@ ;
: PORTC-TRISC! ( n -- ) PORTC-TRISC C! ;
: PORTC-LATC@ ( -- n ) PORTC-LATC C@ ;
: PORTC-LATC! ( n -- ) PORTC-LATC C! ;

\ PORTD外设
: PORTD-PORTD@ ( -- n ) PORTD-PORTD C@ ;
: PORTD-PORTD! ( n -- ) PORTD-PORTD C! ;
: PORTD-TRISD@ ( -- n ) PORTD-TRISD C@ ;
: PORTD-TRISD! ( n -- ) PORTD-TRISD C! ;
: PORTD-LATD@ ( -- n ) PORTD-LATD C@ ;
: PORTD-LATD! ( n -- ) PORTD-LATD C! ;

\ PORTE外设
: PORTE-PORTE@ ( -- n ) PORTE-PORTE C@ ;
: PORTE-PORTE! ( n -- ) PORTE-PORTE C! ;
: PORTE-TRISE@ ( -- n ) PORTE-TRISE C@ ;
: PORTE-TRISE! ( n -- ) PORTE-TRISE C! ;
: PORTE-LATE@ ( -- n ) PORTE-LATE C@ ;
: PORTE-LATE! ( n -- ) PORTE-LATE C! ;

\ TMR0外设
: TMR0-TMR0L@ ( -- n ) TMR0-TMR0L C@ ;
: TMR0-TMR0L! ( n -- ) TMR0-TMR0L C! ;
: TMR0-TMR0H@ ( -- n ) TMR0-TMR0H C@ ;
: TMR0-TMR0H! ( n -- ) TMR0-TMR0H C! ;
: TMR0-T0CON@ ( -- n ) TMR0-T0CON C@ ;
: TMR0-T0CON! ( n -- ) TMR0-T0CON C! ;

\ TMR1外设
: TMR1-TMR1L@ ( -- n ) TMR1-TMR1L C@ ;
: TMR1-TMR1L! ( n -- ) TMR1-TMR1L C! ;
: TMR1-TMR1H@ ( -- n ) TMR1-TMR1H C@ ;
: TMR1-TMR1H! ( n -- ) TMR1-TMR1H C! ;
: TMR1-T1CON@ ( -- n ) TMR1-T1CON C@ ;
: TMR1-T1CON! ( n -- ) TMR1-T1CON C! ;

\ TMR2外设
: TMR2-TMR2@ ( -- n ) TMR2-TMR2 C@ ;
: TMR2-TMR2! ( n -- ) TMR2-TMR2 C! ;
: TMR2-PR2@ ( -- n ) TMR2-PR2 C@ ;
: TMR2-PR2! ( n -- ) TMR2-PR2 C! ;
: TMR2-T2CON@ ( -- n ) TMR2-T2CON C@ ;
: TMR2-T2CON! ( n -- ) TMR2-T2CON C! ;

\ TMR3外设
: TMR3-TMR3L@ ( -- n ) TMR3-TMR3L C@ ;
: TMR3-TMR3L! ( n -- ) TMR3-TMR3L C! ;
: TMR3-TMR3H@ ( -- n ) TMR3-TMR3H C@ ;
: TMR3-TMR3H! ( n -- ) TMR3-TMR3H C! ;
: TMR3-T3CON@ ( -- n ) TMR3-T3CON C@ ;
: TMR3-T3CON! ( n -- ) TMR3-T3CON C! ;

\ ADC外设
: ADC-ADRESL@ ( -- n ) ADC-ADRESL C@ ;
: ADC-ADRESL! ( n -- ) ADC-ADRESL C! ;
: ADC-ADRESH@ ( -- n ) ADC-ADRESH C@ ;
: ADC-ADRESH! ( n -- ) ADC-ADRESH C! ;
: ADC-ADCON0@ ( -- n ) ADC-ADCON0 C@ ;
: ADC-ADCON0! ( n -- ) ADC-ADCON0 C! ;
: ADC-ADCON1@ ( -- n ) ADC-ADCON1 C@ ;
: ADC-ADCON1! ( n -- ) ADC-ADCON1 C! ;

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

\ SSP外设
: SSP-SSPBUF@ ( -- n ) SSP-SSPBUF C@ ;
: SSP-SSPBUF! ( n -- ) SSP-SSPBUF C! ;
: SSP-SSPADD@ ( -- n ) SSP-SSPADD C@ ;
: SSP-SSPADD! ( n -- ) SSP-SSPADD C! ;
: SSP-SSPSTAT@ ( -- n ) SSP-SSPSTAT C@ ;
: SSP-SSPSTAT! ( n -- ) SSP-SSPSTAT C! ;
: SSP-SSPCON1@ ( -- n ) SSP-SSPCON1 C@ ;
: SSP-SSPCON1! ( n -- ) SSP-SSPCON1 C! ;
: SSP-SSPCON2@ ( -- n ) SSP-SSPCON2 C@ ;
: SSP-SSPCON2! ( n -- ) SSP-SSPCON2 C! ;

\ CCP1外设
: CCP1-CCPR1L@ ( -- n ) CCP1-CCPR1L C@ ;
: CCP1-CCPR1L! ( n -- ) CCP1-CCPR1L C! ;
: CCP1-CCPR1H@ ( -- n ) CCP1-CCPR1H C@ ;
: CCP1-CCPR1H! ( n -- ) CCP1-CCPR1H C! ;
: CCP1-CCP1CON@ ( -- n ) CCP1-CCP1CON C@ ;
: CCP1-CCP1CON! ( n -- ) CCP1-CCP1CON C! ;

\ CCP2外设
: CCP2-CCPR2L@ ( -- n ) CCP2-CCPR2L C@ ;
: CCP2-CCPR2L! ( n -- ) CCP2-CCPR2L C! ;
: CCP2-CCPR2H@ ( -- n ) CCP2-CCPR2H C@ ;
: CCP2-CCPR2H! ( n -- ) CCP2-CCPR2H C! ;
: CCP2-CCP2CON@ ( -- n ) CCP2-CCP2CON C@ ;
: CCP2-CCP2CON! ( n -- ) CCP2-CCP2CON C! ;

\ =========================================
\ 设备初始化
\ =========================================

: PIC18F452-INIT ( -- )
  \ 初始化PIC18F452设备
  ." 初始化PIC18F452..." CR

  \ 初始化寄存器
  0 WREG!  \ Working Register
  0 STATUS!  \ Status Register
  0 BSR!  \ Bank Select Register
  0 PCL!  \ Program Counter Low
  0 PCLATH!  \ Program Counter Latch High
  0 PCLATU!  \ Program Counter Latch Upper
  0 TOSU!  \ Top of Stack Upper
  0 TOSH!  \ Top of Stack High
  0 TOSL!  \ Top of Stack Low

  \ 初始化外设
  \ 初始化PORTA
  0 PORTA-PORTA!  \ PORTA寄存器
  0 PORTA-TRISA!  \ TRISA寄存器
  0 PORTA-LATA!  \ LATA寄存器
  \ 初始化PORTB
  0 PORTB-PORTB!  \ PORTB寄存器
  0 PORTB-TRISB!  \ TRISB寄存器
  0 PORTB-LATB!  \ LATB寄存器
  \ 初始化PORTC
  0 PORTC-PORTC!  \ PORTC寄存器
  0 PORTC-TRISC!  \ TRISC寄存器
  0 PORTC-LATC!  \ LATC寄存器
  \ 初始化PORTD
  0 PORTD-PORTD!  \ PORTD寄存器
  0 PORTD-TRISD!  \ TRISD寄存器
  0 PORTD-LATD!  \ LATD寄存器
  \ 初始化PORTE
  0 PORTE-PORTE!  \ PORTE寄存器
  0 PORTE-TRISE!  \ TRISE寄存器
  0 PORTE-LATE!  \ LATE寄存器
  \ 初始化TMR0
  0 TMR0-TMR0L!  \ TMR0L寄存器
  0 TMR0-TMR0H!  \ TMR0H寄存器
  0 TMR0-T0CON!  \ T0CON寄存器
  \ 初始化TMR1
  0 TMR1-TMR1L!  \ TMR1L寄存器
  0 TMR1-TMR1H!  \ TMR1H寄存器
  0 TMR1-T1CON!  \ T1CON寄存器
  \ 初始化TMR2
  0 TMR2-TMR2!  \ TMR2寄存器
  0 TMR2-PR2!  \ PR2寄存器
  0 TMR2-T2CON!  \ T2CON寄存器
  \ 初始化TMR3
  0 TMR3-TMR3L!  \ TMR3L寄存器
  0 TMR3-TMR3H!  \ TMR3H寄存器
  0 TMR3-T3CON!  \ T3CON寄存器
  \ 初始化ADC
  0 ADC-ADRESL!  \ ADRESL寄存器
  0 ADC-ADRESH!  \ ADRESH寄存器
  0 ADC-ADCON0!  \ ADCON0寄存器
  0 ADC-ADCON1!  \ ADCON1寄存器
  \ 初始化USART
  0 USART-TXREG!  \ TXREG寄存器
  0 USART-RCREG!  \ RCREG寄存器
  0 USART-SPBRG!  \ SPBRG寄存器
  0 USART-TXSTA!  \ TXSTA寄存器
  0 USART-RCSTA!  \ RCSTA寄存器
  \ 初始化SSP
  0 SSP-SSPBUF!  \ SSPBUF寄存器
  0 SSP-SSPADD!  \ SSPADD寄存器
  0 SSP-SSPSTAT!  \ SSPSTAT寄存器
  0 SSP-SSPCON1!  \ SSPCON1寄存器
  0 SSP-SSPCON2!  \ SSPCON2寄存器
  \ 初始化CCP1
  0 CCP1-CCPR1L!  \ CCPR1L寄存器
  0 CCP1-CCPR1H!  \ CCPR1H寄存器
  0 CCP1-CCP1CON!  \ CCP1CON寄存器
  \ 初始化CCP2
  0 CCP2-CCPR2L!  \ CCPR2L寄存器
  0 CCP2-CCPR2H!  \ CCPR2H寄存器
  0 CCP2-CCP2CON!  \ CCP2CON寄存器

  ." PIC18F452初始化完成" CR
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
  WREG@ WREG .R 8 .R SPACE ."  WREG: " WREG@ .
  STATUS@ STATUS .R 8 .R SPACE ."  STATUS: " STATUS@ .
  BSR@ BSR .R 8 .R SPACE ."  BSR: " BSR@ .
  PCL@ PCL .R 8 .R SPACE ."  PCL: " PCL@ .
  PCLATH@ PCLATH .R 8 .R SPACE ."  PCLATH: " PCLATH@ .
  PCLATU@ PCLATU .R 8 .R SPACE ."  PCLATU: " PCLATU@ .
  TOSU@ TOSU .R 8 .R SPACE ."  TOSU: " TOSU@ .
  TOSH@ TOSH .R 8 .R SPACE ."  TOSH: " TOSH@ .
  TOSL@ TOSL .R 8 .R SPACE ."  TOSL: " TOSL@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ High priority interrupt
: INT-HIGH_PRIORITY-HANDLER ( -- )
  ." HIGH_PRIORITY中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-HIGH_PRIORITY-ENABLE ( -- )
  INT-HIGH_PRIORITY INT-ENABLE
;

: INT-HIGH_PRIORITY-DISABLE ( -- )
  INT-HIGH_PRIORITY INT-DISABLE
;

\ Low priority interrupt
: INT-LOW_PRIORITY-HANDLER ( -- )
  ." LOW_PRIORITY中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-LOW_PRIORITY-ENABLE ( -- )
  INT-LOW_PRIORITY INT-ENABLE
;

: INT-LOW_PRIORITY-DISABLE ( -- )
  INT-LOW_PRIORITY INT-DISABLE
;

\ Reset vector
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  PIC18F452-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
