(*
 * PIC18F4550梯形图定义
 * 生成自: Microchip/PIC18/PIC18F4550
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
 * CPU架构: PIC18
 * 位宽: 8位
 * 时钟频率: 20000000 Hz
 *)

PROGRAM PIC18F4550
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    W AT %M0x0E : BYTE; (* Working Register *)
    STATUS AT %M0xFD8 : BYTE; (* Status Register *)
    STATUS_C AT %M0xFD8.0 : BOOL; (* Carry Flag *)
    STATUS_DC AT %M0xFD8.1 : BOOL; (* Digit Carry Flag *)
    STATUS_Z AT %M0xFD8.2 : BOOL; (* Zero Flag *)
    STATUS_PD AT %M0xFD8.3 : BOOL; (* Power-Down Flag *)
    STATUS_TO AT %M0xFD8.4 : BOOL; (* Time-out Flag *)
    STATUS_RP AT %M0xFD8.0 : BOOL; (* Register Bank Select *)
    STATUS_IRP AT %M0xFD8.7 : BOOL; (* Indirect Register Bank Select *)
    BSR AT %M0xFE0 : BYTE; (* Bank Select Register *)
    PORTA AT %M0xF80 : BYTE; (* Port A *)
    PORTB AT %M0xF81 : BYTE; (* Port B *)
    PORTC AT %M0xF82 : BYTE; (* Port C *)
    PORTD AT %M0xF83 : BYTE; (* Port D *)
    PORTE AT %M0xF84 : BYTE; (* Port E *)
    TRISA AT %M0xF92 : BYTE; (* Tri-state Port A *)
    TRISB AT %M0xF93 : BYTE; (* Tri-state Port B *)
    TRISC AT %M0xF94 : BYTE; (* Tri-state Port C *)
    TRISD AT %M0xF95 : BYTE; (* Tri-state Port D *)
    TRISE AT %M0xF96 : BYTE; (* Tri-state Port E *)
    LATA AT %M0xF89 : BYTE; (* Latch Port A *)
    LATB AT %M0xF8A : BYTE; (* Latch Port B *)
    LATC AT %M0xF8B : BYTE; (* Latch Port C *)
    LATD AT %M0xF8C : BYTE; (* Latch Port D *)
    LATE AT %M0xF8D : BYTE; (* Latch Port E *)
    INTCON AT %M0xFF2 : BYTE; (* Interrupt Control *)
    INTCON_RBIF AT %M0xFF2.0 : BOOL; (* Port B Interrupt Flag *)
    INTCON_INT0IF AT %M0xFF2.1 : BOOL; (* INT0 Interrupt Flag *)
    INTCON_TMR0IF AT %M0xFF2.2 : BOOL; (* Timer 0 Interrupt Flag *)
    INTCON_RBIE AT %M0xFF2.3 : BOOL; (* Port B Interrupt Enable *)
    INTCON_INT0IE AT %M0xFF2.4 : BOOL; (* INT0 Interrupt Enable *)
    INTCON_TMR0IE AT %M0xFF2.5 : BOOL; (* Timer 0 Interrupt Enable *)
    INTCON_PEIE AT %M0xFF2.6 : BOOL; (* Peripheral Interrupt Enable *)
    INTCON_GIE AT %M0xFF2.7 : BOOL; (* Global Interrupt Enable *)
    PIR1 AT %M0xF9E : BYTE; (* Peripheral Interrupt 1 *)
    PIR2 AT %M0xF9F : BYTE; (* Peripheral Interrupt 2 *)
    PIE1 AT %M0xF9D : BYTE; (* Peripheral Interrupt Enable 1 *)
    PIE2 AT %M0xF9C : BYTE; (* Peripheral Interrupt Enable 2 *)
    IPR1 AT %M0xF9B : BYTE; (* Interrupt Priority 1 *)
    IPR2 AT %M0xF9A : BYTE; (* Interrupt Priority 2 *)
    RCON AT %M0xFD0 : BYTE; (* Reset Control *)
    RCON_NOT_TO AT %M0xFD0.3 : BOOL; (* Time-out Flag *)
    RCON_NOT_PD AT %M0xFD0.4 : BOOL; (* Power-Down Flag *)
    RCON_NOT_RI AT %M0xFD0.5 : BOOL; (* RESET Flag *)
    RCON_NOT_POR AT %M0xFD0.6 : BOOL; (* Power-on Reset Flag *)
    RCON_NOT_BOR AT %M0xFD0.7 : BOOL; (* Brown-out Reset Flag *)
    T0CON AT %M0xFD1 : BYTE; (* Timer 0 Control *)
    TMR0 AT %M0xFD6 : BYTE; (* Timer 0 Register *)
    T1CON AT %M0xFCD : BYTE; (* Timer 1 Control *)
    TMR1 AT %M0xFCE : BYTE; (* Timer 1 Register High *)
    TMR1L AT %M0xFCF : BYTE; (* Timer 1 Register Low *)
    T2CON AT %M0xFCA : BYTE; (* Timer 2 Control *)
    TMR2 AT %M0xFCB : BYTE; (* Timer 2 Register *)
    T3CON AT %M0xFB1 : BYTE; (* Timer 3 Control *)
    TMR3 AT %M0xFB3 : BYTE; (* Timer 3 Register High *)
    TMR3L AT %M0xFB2 : BYTE; (* Timer 3 Register Low *)
    SSPCON1 AT %M0xFC6 : BYTE; (* SSP Control 1 *)
    SSPCON2 AT %M0xFC5 : BYTE; (* SSP Control 2 *)
    SSPSTAT AT %M0xFC7 : BYTE; (* SSP Status *)
    SSPBUF AT %M0xFC9 : BYTE; (* SSP Buffer *)
    SSPOR AT %M0xFC8 : BYTE; (* SSP Shift Register *)
    ADCON0 AT %M0xFC2 : BYTE; (* A/D Control 0 *)
    ADCON1 AT %M0xFC1 : BYTE; (* A/D Control 1 *)
    ADCON2 AT %M0xFC0 : BYTE; (* A/D Control 2 *)
    ADRES AT %M0xFC3 : BYTE; (* A/D Result *)
    ADRESL AT %M0xFC4 : BYTE; (* A/D Result Low *)
    CCP1CON AT %M0xFD4 : BYTE; (* CCP 1 Control *)
    CCPR1 AT %M0xFD6 : BYTE; (* CCP 1 Register High *)
    CCPR1L AT %M0xFD5 : BYTE; (* CCP 1 Register Low *)
    CCP2CON AT %M0xFBA : BYTE; (* CCP 2 Control *)
    CCPR2 AT %M0xFBB : BYTE; (* CCP 2 Register High *)
    CCPR2L AT %M0xFBC : BYTE; (* CCP 2 Register Low *)
    USBCON AT %M0xF75 : BYTE; (* USB Control *)
    USBSTAT AT %M0xF74 : BYTE; (* USB Status *)
    UIE AT %M0xF73 : BYTE; (* USB Interrupt Enable *)
    UIR AT %M0xF72 : BYTE; (* USB Interrupt Flag *)
    UCON AT %M0xF71 : BYTE; (* USB Control *)
    USTAT AT %M0xF70 : BYTE; (* USB Status *)
    UEP0 AT %M0xF60 : BYTE; (* USB Endpoint 0 *)
    UEP1 AT %M0xF61 : BYTE; (* USB Endpoint 1 *)
    UEP2 AT %M0xF62 : BYTE; (* USB Endpoint 2 *)
    UEP3 AT %M0xF63 : BYTE; (* USB Endpoint 3 *)
    UEP4 AT %M0xF64 : BYTE; (* USB Endpoint 4 *)

    (* 外设变量 *)
    (* Port A *)
    PORTA_BASE AT %MW0xF80 : WORD;
    PORTA_PORT AT %MW0xF80 : BYTE;
    PORTA_TRIS AT %MW0xF92 : BYTE;
    PORTA_LAT AT %MW0xF89 : BYTE;
    (* Port B *)
    PORTB_BASE AT %MW0xF81 : WORD;
    PORTB_PORT AT %MW0xF81 : BYTE;
    PORTB_TRIS AT %MW0xF93 : BYTE;
    PORTB_LAT AT %MW0xF8A : BYTE;
    (* Port C *)
    PORTC_BASE AT %MW0xF82 : WORD;
    PORTC_PORT AT %MW0xF82 : BYTE;
    PORTC_TRIS AT %MW0xF94 : BYTE;
    PORTC_LAT AT %MW0xF8B : BYTE;
    (* Port D *)
    PORTD_BASE AT %MW0xF83 : WORD;
    PORTD_PORT AT %MW0xF83 : BYTE;
    PORTD_TRIS AT %MW0xF95 : BYTE;
    PORTD_LAT AT %MW0xF8C : BYTE;
    (* Port E *)
    PORTE_BASE AT %MW0xF84 : WORD;
    PORTE_PORT AT %MW0xF84 : BYTE;
    PORTE_TRIS AT %MW0xF96 : BYTE;
    PORTE_LAT AT %MW0xF8D : BYTE;
    (* Timer 0 *)
    TIMER0_BASE AT %MW0xFD1 : WORD;
    TIMER0_T0CON AT %MW0xFD1 : BYTE;
    TIMER0_TMR0 AT %MW0xFD6 : BYTE;
    (* Timer 1 *)
    TIMER1_BASE AT %MW0xFCD : WORD;
    TIMER1_T1CON AT %MW0xFCD : BYTE;
    TIMER1_TMR1 AT %MW0xFCF : BYTE;
    TIMER1_TMR1L AT %MW0xFCE : BYTE;
    (* Timer 2 *)
    TIMER2_BASE AT %MW0xFCA : WORD;
    TIMER2_T2CON AT %MW0xFCA : BYTE;
    TIMER2_TMR2 AT %MW0xFCB : BYTE;
    (* Timer 3 *)
    TIMER3_BASE AT %MW0xFB0 : WORD;
    TIMER3_T3CON AT %MW0xFB0 : BYTE;
    TIMER3_TMR3 AT %MW0xFB2 : BYTE;
    (* A/D Converter *)
    ADC_BASE AT %MW0xFC2 : WORD;
    ADC_ADCON0 AT %MW0xFC2 : BYTE;
    ADC_ADCON1 AT %MW0xFC1 : BYTE;
    ADC_ADCON2 AT %MW0xFC0 : BYTE;
    ADC_ADRES AT %MW0xFC3 : BYTE;
    ADC_ADRESL AT %MW0xFC4 : BYTE;
    (* CCP 1 *)
    CCP1_BASE AT %MW0xFD4 : WORD;
    CCP1_CCP1CON AT %MW0xFD4 : BYTE;
    CCP1_CCPR1 AT %MW0xFD6 : BYTE;
    CCP1_CCPR1L AT %MW0xFD5 : BYTE;
    (* CCP 2 *)
    CCP2_BASE AT %MW0xFBA : WORD;
    CCP2_CCP2CON AT %MW0xFBA : BYTE;
    CCP2_CCPR2 AT %MW0xFBB : BYTE;
    CCP2_CCPR2L AT %MW0xFBC : BYTE;
    (* SSP (I2C/SPI) *)
    SSP_BASE AT %MW0xFC6 : WORD;
    SSP_SSPCON1 AT %MW0xFC6 : BYTE;
    SSP_SSPCON2 AT %MW0xFC5 : BYTE;
    SSP_SSPSTAT AT %MW0xFC7 : BYTE;
    SSP_SSPBUF AT %MW0xFC9 : BYTE;
    SSP_SSPOV AT %MW0xFC8 : BYTE;
    (* EUSART *)
    EUSART_BASE AT %MW0xF15 : WORD;
    EUSART_TXSTA AT %MW0xFE2 : BYTE;
    EUSART_RCSTA AT %MW0xFE3 : BYTE;
    EUSART_TXREG AT %MW0xFAD : BYTE;
    EUSART_RCREG AT %MW0xFAE : BYTE;
    EUSART_SPBRG AT %MW0xFAF : BYTE;
    EUSART_SPBRGH AT %MW0xFB0 : BYTE;
    EUSART_BAUDCON AT %MW0xFB8 : BYTE;
    (* Comparators *)
    COMPARATOR_BASE AT %MW0xFB4 : WORD;
    COMPARATOR_CMCON AT %MW0xFB4 : BYTE;
    COMPARATOR_CVRCON AT %MW0xFB5 : BYTE;
    (* USB Module *)
    USB_BASE AT %MW0xF70 : WORD;
    USB_UCON AT %MW0xF71 : BYTE;
    USB_USTAT AT %MW0xF72 : BYTE;
    USB_UIR AT %MW0xF73 : BYTE;
    USB_UIE AT %MW0xF74 : BYTE;
    USB_UEP0 AT %MW0xF80 : BYTE;
    USB_UEP1 AT %MW0xF81 : BYTE;
    USB_UEP2 AT %MW0xF82 : BYTE;
    USB_UEP3 AT %MW0xF83 : BYTE;
    USB_BD0 AT %MW0xF00 : BYTE;
    USB_BD1 AT %MW0xF08 : BYTE;
    USB_BD2 AT %MW0xF10 : BYTE;
    USB_BD3 AT %MW0xF18 : BYTE;
    (* Oscillator *)
    OSCCON_BASE AT %MW0xFD3 : WORD;
    OSCCON_OSCCON AT %MW0xFD3 : BYTE;
    OSCCON_OSCTUNE AT %MW0xFD9 : BYTE;
    (* Watchdog Timer *)
    WDTCON_BASE AT %MW0xFD1 : WORD;
    WDTCON_WDTCON AT %MW0xFD1 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* RESET *)
    INT_INT0 : BOOL; (* External Interrupt 0 *)
    INT_INT1 : BOOL; (* External Interrupt 1 *)
    INT_INT2 : BOOL; (* External Interrupt 2 *)
    INT_TMR0 : BOOL; (* Timer 0 Overflow *)
    INT_TMR1 : BOOL; (* Timer 1 Overflow *)
    INT_TMR2 : BOOL; (* Timer 2 Match *)
    INT_TMR3 : BOOL; (* Timer 3 Overflow *)
    INT_CCP1 : BOOL; (* CCP 1 *)
    INT_CCP2 : BOOL; (* CCP 2 *)
    INT_SSP : BOOL; (* SSP *)
    INT_TX : BOOL; (* USART TX *)
    INT_RC : BOOL; (* USART RX *)
    INT_ADC : BOOL; (* A/D *)
    INT_RBO : BOOL; (* Port B Change *)
    INT_EXT : BOOL; (* External *)

END_VAR

BEGIN
    (* PIC18F4550初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|W>0|--(监控灯)--
    --|STATUS>0|--(监控灯)--
    --|BSR>0|--(监控灯)--
    --|PORTA>0|--(监控灯)--
    --|PORTB>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_INT0|--[中断处理]--
    --|INT_INT1|--[中断处理]--
    --|INT_INT2|--[中断处理]--
    --|INT_TMR0|--[中断处理]--
    --|INT_TMR1|--[中断处理]--
    --|INT_TMR2|--[中断处理]--
    --|INT_TMR3|--[中断处理]--
    --|INT_CCP1|--[中断处理]--
    --|INT_CCP2|--[中断处理]--
    --|INT_SSP|--[中断处理]--
    --|INT_TX|--[中断处理]--
    --|INT_RC|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_RBO|--[中断处理]--
    --|INT_EXT|--[中断处理]--

END_PROGRAM
