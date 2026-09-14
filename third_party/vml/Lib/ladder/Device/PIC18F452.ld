(*
 * PIC18F452梯形图定义
 * 生成自: Microchip Technology/PIC18/PIC18F452
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
 * CPU架构: PIC18
 * 位宽: 8位
 * 时钟频率: 20000000 Hz
 *)

PROGRAM PIC18F452
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    WREG AT %M0xFE8 : BYTE; (* Working Register *)
    STATUS AT %M0xFD8 : BYTE; (* Status Register *)
    BSR AT %M0xFE0 : BYTE; (* Bank Select Register *)
    PCL AT %M0xFF9 : BYTE; (* Program Counter Low *)
    PCLATH AT %M0xFFA : BYTE; (* Program Counter Latch High *)
    PCLATU AT %M0xFFB : BYTE; (* Program Counter Latch Upper *)
    TOSU AT %M0xFFF : BYTE; (* Top of Stack Upper *)
    TOSH AT %M0xFFE : BYTE; (* Top of Stack High *)
    TOSL AT %M0xFFD : BYTE; (* Top of Stack Low *)

    (* 外设变量 *)
    (* Port A *)
    PORTA_BASE AT %MW : WORD;
    PORTA_PORTA AT %MW0xF80 : BYTE;
    PORTA_TRISA AT %MW0xF92 : BYTE;
    PORTA_LATA AT %MW0xF89 : BYTE;
    (* Port B *)
    PORTB_BASE AT %MW : WORD;
    PORTB_PORTB AT %MW0xF81 : BYTE;
    PORTB_TRISB AT %MW0xF93 : BYTE;
    PORTB_LATB AT %MW0xF8A : BYTE;
    (* Port C *)
    PORTC_BASE AT %MW : WORD;
    PORTC_PORTC AT %MW0xF82 : BYTE;
    PORTC_TRISC AT %MW0xF94 : BYTE;
    PORTC_LATC AT %MW0xF8B : BYTE;
    (* Port D *)
    PORTD_BASE AT %MW : WORD;
    PORTD_PORTD AT %MW0xF83 : BYTE;
    PORTD_TRISD AT %MW0xF95 : BYTE;
    PORTD_LATD AT %MW0xF8C : BYTE;
    (* Port E *)
    PORTE_BASE AT %MW : WORD;
    PORTE_PORTE AT %MW0xF84 : BYTE;
    PORTE_TRISE AT %MW0xF96 : BYTE;
    PORTE_LATE AT %MW0xF8D : BYTE;
    (* Timer0 *)
    TMR0_BASE AT %MW : WORD;
    TMR0_TMR0L AT %MW0xFD6 : BYTE;
    TMR0_TMR0H AT %MW0xFD7 : BYTE;
    TMR0_T0CON AT %MW0xFD5 : BYTE;
    (* Timer1 *)
    TMR1_BASE AT %MW : WORD;
    TMR1_TMR1L AT %MW0xFCE : BYTE;
    TMR1_TMR1H AT %MW0xFCF : BYTE;
    TMR1_T1CON AT %MW0xFCD : BYTE;
    (* Timer2 *)
    TMR2_BASE AT %MW : WORD;
    TMR2_TMR2 AT %MW0xFCC : BYTE;
    TMR2_PR2 AT %MW0xFCB : BYTE;
    TMR2_T2CON AT %MW0xFCA : BYTE;
    (* Timer3 *)
    TMR3_BASE AT %MW : WORD;
    TMR3_TMR3L AT %MW0xFB2 : BYTE;
    TMR3_TMR3H AT %MW0xFB3 : BYTE;
    TMR3_T3CON AT %MW0xFB1 : BYTE;
    (* Analog-to-Digital Converter *)
    ADC_BASE AT %MW : WORD;
    ADC_ADRESL AT %MW0xFC3 : BYTE;
    ADC_ADRESH AT %MW0xFC4 : BYTE;
    ADC_ADCON0 AT %MW0xFC2 : BYTE;
    ADC_ADCON1 AT %MW0xFC1 : BYTE;
    (* Universal Synchronous Asynchronous Receiver Transmitter *)
    USART_BASE AT %MW : WORD;
    USART_TXREG AT %MW0xFAC : BYTE;
    USART_RCREG AT %MW0xFAB : BYTE;
    USART_SPBRG AT %MW0xFAF : BYTE;
    USART_TXSTA AT %MW0xFAD : BYTE;
    USART_RCSTA AT %MW0xFAE : BYTE;
    (* Synchronous Serial Port *)
    SSP_BASE AT %MW : WORD;
    SSP_SSPBUF AT %MW0xFC9 : BYTE;
    SSP_SSPADD AT %MW0xFC8 : BYTE;
    SSP_SSPSTAT AT %MW0xFC7 : BYTE;
    SSP_SSPCON1 AT %MW0xFC6 : BYTE;
    SSP_SSPCON2 AT %MW0xFC5 : BYTE;
    (* Capture/Compare/PWM 1 *)
    CCP1_BASE AT %MW : WORD;
    CCP1_CCPR1L AT %MW0xFBE : BYTE;
    CCP1_CCPR1H AT %MW0xFBF : BYTE;
    CCP1_CCP1CON AT %MW0xFBD : BYTE;
    (* Capture/Compare/PWM 2 *)
    CCP2_BASE AT %MW : WORD;
    CCP2_CCPR2L AT %MW0xFBA : BYTE;
    CCP2_CCPR2H AT %MW0xFBB : BYTE;
    CCP2_CCP2CON AT %MW0xFB9 : BYTE;

    (* 中断变量 *)
    INT_HIGH_PRIORITY : BOOL; (* High priority interrupt *)
    INT_LOW_PRIORITY : BOOL; (* Low priority interrupt *)
    INT_RESET : BOOL; (* Reset vector *)

END_VAR

BEGIN
    (* PIC18F452初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|WREG>0|--(监控灯)--
    --|STATUS>0|--(监控灯)--
    --|BSR>0|--(监控灯)--
    --|PCL>0|--(监控灯)--
    --|PCLATH>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_HIGH_PRIORITY|--[中断处理]--
    --|INT_LOW_PRIORITY|--[中断处理]--
    --|INT_RESET|--[中断处理]--

END_PROGRAM
