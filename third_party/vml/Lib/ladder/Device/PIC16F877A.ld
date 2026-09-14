(*
 * PIC16F877A梯形图定义
 * 生成自: Microchip/PIC/PIC16F877A
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
 * CPU架构: PIC16
 * 位宽: 8位
 * 时钟频率: 4000000 Hz
 *)

PROGRAM PIC16F877A
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    W AT %M0x00 : BYTE; (* Working Register *)
    STATUS AT %M0x03 : BYTE; (* Status Register *)
    STATUS_C AT %M0x03.0 : BOOL; (* Carry flag *)
    STATUS_DC AT %M0x03.1 : BOOL; (* Digit carry flag *)
    STATUS_Z AT %M0x03.2 : BOOL; (* Zero flag *)
    STATUS_PD AT %M0x03.3 : BOOL; (* Power-down flag *)
    STATUS_TO AT %M0x03.4 : BOOL; (* Time-out flag *)
    STATUS_RP AT %M0x03.5 : BOOL; (* Register bank select *)
    STATUS_IRP AT %M0x03.7 : BOOL; (* Indirect register bank select *)
    INTCON AT %M0x0B : BYTE; (* Interrupt Control Register *)
    INTCON_RBIF AT %M0x0B.0 : BOOL; (* PORTB change interrupt flag *)
    INTCON_INTF AT %M0x0B.1 : BOOL; (* External interrupt flag *)
    INTCON_TMR0IF AT %M0x0B.2 : BOOL; (* TMR0 overflow interrupt flag *)
    INTCON_RBIE AT %M0x0B.3 : BOOL; (* PORTB change interrupt enable *)
    INTCON_INTE AT %M0x0B.4 : BOOL; (* External interrupt enable *)
    INTCON_TMR0IE AT %M0x0B.5 : BOOL; (* TMR0 overflow interrupt enable *)
    INTCON_PEIE AT %M0x0B.6 : BOOL; (* Peripheral interrupt enable *)
    INTCON_GIE AT %M0x0B.7 : BOOL; (* Global interrupt enable *)
    PORTB AT %M0x06 : BYTE; (* PORT B *)
    TRISB AT %M0x86 : BYTE; (* TRIS B *)
    PORTC AT %M0x07 : BYTE; (* PORT C *)
    TRISC AT %M0x87 : BYTE; (* TRIS C *)
    PORTD AT %M0x08 : BYTE; (* PORT D *)
    TRISD AT %M0x88 : BYTE; (* TRIS D *)
    PORTE AT %M0x09 : BYTE; (* PORT E *)
    TRISE AT %M0x89 : BYTE; (* TRIS E *)
    TMR0 AT %M0x01 : BYTE; (* Timer 0 *)
    OPTION_REG AT %M0x81 : BYTE; (* Option Register *)
    PCL AT %M0x02 : BYTE; (* Program Counter Low *)
    PCLATH AT %M0x0A : BYTE; (* Program Counter Latch High *)
    FSR AT %M0x04 : BYTE; (* File Select Register *)
    EEDATA AT %M0x10C : BYTE; (* EEPROM Data *)
    EEADR AT %M0x10D : BYTE; (* EEPROM Address *)
    EECON1 AT %M0x18C : BYTE; (* EEPROM Control 1 *)
    EECON1_RD AT %M0x18C.0 : BOOL; (* Read control *)
    EECON1_WR AT %M0x18C.1 : BOOL; (* Write control *)
    EECON1_WREN AT %M0x18C.2 : BOOL; (* Write enable *)
    EECON1_WRERR AT %M0x18C.3 : BOOL; (* Write error flag *)
    EECON1_EEPGD AT %M0x18C.7 : BOOL; (* EEPROM program/data select *)
    EECON2 AT %M0x18D : BYTE; (* EEPROM Control 2 *)
    ADRESH AT %M0x1E : BYTE; (* A/D Result High *)
    ADRESL AT %M0x1F : BYTE; (* A/D Result Low *)
    ADCON0 AT %M0x1F : BYTE; (* A/D Control 0 *)
    ADCON0_ADON AT %M0x1F.0 : BOOL; (* A/D enable *)
    ADCON0_GO_DONE AT %M0x1F.2 : BOOL; (* A/D conversion status *)
    ADCON0_CHS AT %M0x1F.3 : BOOL; (* Channel select *)
    ADCON1 AT %M0x9F : BYTE; (* A/D Control 1 *)
    SSPSTAT AT %M0x94 : BYTE; (* MSSP Status *)
    SSPCON AT %M0x14 : BYTE; (* MSSP Control *)
    SSPBUF AT %M0x13 : BYTE; (* SSP Buffer *)
    TXREG AT %M0x19 : BYTE; (* USART Transmit Register *)
    RCREG AT %M0x1A : BYTE; (* USART Receive Register *)
    SPBRG AT %M0x99 : BYTE; (* Baud Rate Generator *)
    TXSTA AT %M0x98 : BYTE; (* TX Status and Control *)
    RCSTA AT %M0x18 : BYTE; (* RX Status and Control *)
    CCP1CON AT %M0x17 : BYTE; (* CCP1 Control *)
    CCPR1L AT %M0x15 : BYTE; (* CCP1 Low *)
    CCPR1H AT %M0x16 : BYTE; (* CCP1 High *)
    CCP2CON AT %M0x1D : BYTE; (* CCP2 Control *)
    CCPR2L AT %M0x1B : BYTE; (* CCP2 Low *)
    CCPR2H AT %M0x1C : BYTE; (* CCP2 High *)
    T1CON AT %M0x10 : BYTE; (* Timer 1 Control *)
    TMR1L AT %M0x0E : BYTE; (* Timer 1 Low *)
    TMR1H AT %M0x0F : BYTE; (* Timer 1 High *)
    T2CON AT %M0x12 : BYTE; (* Timer 2 Control *)
    TMR2 AT %M0x11 : BYTE; (* Timer 2 *)
    PR2 AT %M0x92 : BYTE; (* Timer 2 Period *)

    (* 外设变量 *)
    (* Port B *)
    GPIO_PORTB_BASE AT %MW0x06 : WORD;
    GPIO_PORTB_PORTB AT %MW0x06 : BYTE;
    GPIO_PORTB_TRISB AT %MW0x86 : BYTE;
    (* Port C *)
    GPIO_PORTC_BASE AT %MW0x07 : WORD;
    GPIO_PORTC_PORTC AT %MW0x07 : BYTE;
    GPIO_PORTC_TRISC AT %MW0x87 : BYTE;
    (* Port D *)
    GPIO_PORTD_BASE AT %MW0x08 : WORD;
    GPIO_PORTD_PORTD AT %MW0x08 : BYTE;
    GPIO_PORTD_TRISD AT %MW0x88 : BYTE;
    (* Timer 0 *)
    TIMER0_BASE AT %MW0x01 : WORD;
    TIMER0_TMR0 AT %MW0x01 : BYTE;
    TIMER0_OPTION_REG AT %MW0x81 : BYTE;
    (* Timer 1 *)
    TIMER1_BASE AT %MW0x0E : WORD;
    TIMER1_T1CON AT %MW0x10 : BYTE;
    TIMER1_TMR1L AT %MW0x0E : BYTE;
    TIMER1_TMR1H AT %MW0x0F : BYTE;
    (* Timer 2 *)
    TIMER2_BASE AT %MW0x11 : WORD;
    TIMER2_T2CON AT %MW0x12 : BYTE;
    TIMER2_TMR2 AT %MW0x11 : BYTE;
    TIMER2_PR2 AT %MW0x92 : BYTE;
    (* A/D Converter *)
    ADC_BASE AT %MW0x1E : WORD;
    ADC_ADRESH AT %MW0x1E : BYTE;
    ADC_ADRESL AT %MW0x9F : BYTE;
    ADC_ADCON0 AT %MW0x1F : BYTE;
    ADC_ADCON1 AT %MW0x9F : BYTE;
    (* Master Synchronous Serial Port *)
    MSSP_BASE AT %MW0x13 : WORD;
    MSSP_SSPSTAT AT %MW0x94 : BYTE;
    MSSP_SSPCON AT %MW0x14 : BYTE;
    MSSP_SSPBUF AT %MW0x13 : BYTE;
    (* USART *)
    USART_BASE AT %MW0x19 : WORD;
    USART_TXREG AT %MW0x19 : BYTE;
    USART_RCREG AT %MW0x1A : BYTE;
    USART_SPBRG AT %MW0x99 : BYTE;
    USART_TXSTA AT %MW0x98 : BYTE;
    USART_RCSTA AT %MW0x18 : BYTE;
    (* Capture/Compare/PWM 1 *)
    CCP1_BASE AT %MW0x15 : WORD;
    CCP1_CCP1CON AT %MW0x17 : BYTE;
    CCP1_CCPR1L AT %MW0x15 : BYTE;
    CCP1_CCPR1H AT %MW0x16 : BYTE;
    (* Capture/Compare/PWM 2 *)
    CCP2_BASE AT %MW0x1B : WORD;
    CCP2_CCP2CON AT %MW0x1D : BYTE;
    CCP2_CCPR2L AT %MW0x1B : BYTE;
    CCP2_CCPR2H AT %MW0x1C : BYTE;

    (* 中断变量 *)
    INT_INT : BOOL; (* External Interrupt *)
    INT_TMR0 : BOOL; (* Timer 0 Overflow *)
    INT_RB : BOOL; (* PORTB Change *)
    INT_CCP1 : BOOL; (* CCP1 *)
    INT_CCP2 : BOOL; (* CCP2 *)
    INT_TMR1 : BOOL; (* Timer 1 Overflow *)
    INT_TMR2 : BOOL; (* Timer 2 Overflow *)
    INT_SPI : BOOL; (* SPI/I2C *)
    INT_SCI : BOOL; (* USART Receive *)
    INT_SCI : BOOL; (* USART Transmit *)
    INT_ADC : BOOL; (* A/D Converter *)
    INT_EEPROM : BOOL; (* EEPROM Write Complete *)

END_VAR

BEGIN
    (* PIC16F877A初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|W>0|--(监控灯)--
    --|STATUS>0|--(监控灯)--
    --|INTCON>0|--(监控灯)--
    --|PORTB>0|--(监控灯)--
    --|TRISB>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_INT|--[中断处理]--
    --|INT_TMR0|--[中断处理]--
    --|INT_RB|--[中断处理]--
    --|INT_CCP1|--[中断处理]--
    --|INT_CCP2|--[中断处理]--
    --|INT_TMR1|--[中断处理]--
    --|INT_TMR2|--[中断处理]--
    --|INT_SPI|--[中断处理]--
    --|INT_SCI|--[中断处理]--
    --|INT_SCI|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_EEPROM|--[中断处理]--

END_PROGRAM
