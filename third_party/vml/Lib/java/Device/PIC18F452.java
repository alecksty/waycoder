package vml.device.microchiptechnology.pic18f452;

/**
 * PIC18F452 寄存器定义
 * 生成自: Microchip Technology/PIC18/PIC18F452
 * 版本: 1.0
 */
public final class PIC18F452 {
    private PIC18F452() {} // 工具类
    // CPU架构: PIC18, 8位, 20000000 Hz

    // 寄存器定义
    // Working Register
    public static final int WREG_ADDR = (int)0xFE8;

    // Status Register
    public static final int STATUS_ADDR = (int)0xFD8;

    // Bank Select Register
    public static final int BSR_ADDR = (int)0xFE0;

    // Program Counter Low
    public static final int PCL_ADDR = (int)0xFF9;

    // Program Counter Latch High
    public static final int PCLATH_ADDR = (int)0xFFA;

    // Program Counter Latch Upper
    public static final int PCLATU_ADDR = (int)0xFFB;

    // Top of Stack Upper
    public static final int TOSU_ADDR = (int)0xFFF;

    // Top of Stack High
    public static final int TOSH_ADDR = (int)0xFFE;

    // Top of Stack Low
    public static final int TOSL_ADDR = (int)0xFFD;

    // 外设定义
    // Port A
    public static final int PORTA_BASE = (int);
    public static final int PORTA_PORTA = (int)0x00000F80;
    public static final int PORTA_TRISA = (int)0x00000F92;
    public static final int PORTA_LATA = (int)0x00000F89;

    // Port B
    public static final int PORTB_BASE = (int);
    public static final int PORTB_PORTB = (int)0x00000F81;
    public static final int PORTB_TRISB = (int)0x00000F93;
    public static final int PORTB_LATB = (int)0x00000F8A;

    // Port C
    public static final int PORTC_BASE = (int);
    public static final int PORTC_PORTC = (int)0x00000F82;
    public static final int PORTC_TRISC = (int)0x00000F94;
    public static final int PORTC_LATC = (int)0x00000F8B;

    // Port D
    public static final int PORTD_BASE = (int);
    public static final int PORTD_PORTD = (int)0x00000F83;
    public static final int PORTD_TRISD = (int)0x00000F95;
    public static final int PORTD_LATD = (int)0x00000F8C;

    // Port E
    public static final int PORTE_BASE = (int);
    public static final int PORTE_PORTE = (int)0x00000F84;
    public static final int PORTE_TRISE = (int)0x00000F96;
    public static final int PORTE_LATE = (int)0x00000F8D;

    // Timer0
    public static final int TMR0_BASE = (int);
    public static final int TMR0_TMR0L = (int)0x00000FD6;
    public static final int TMR0_TMR0H = (int)0x00000FD7;
    public static final int TMR0_T0CON = (int)0x00000FD5;

    // Timer1
    public static final int TMR1_BASE = (int);
    public static final int TMR1_TMR1L = (int)0x00000FCE;
    public static final int TMR1_TMR1H = (int)0x00000FCF;
    public static final int TMR1_T1CON = (int)0x00000FCD;

    // Timer2
    public static final int TMR2_BASE = (int);
    public static final int TMR2_TMR2 = (int)0x00000FCC;
    public static final int TMR2_PR2 = (int)0x00000FCB;
    public static final int TMR2_T2CON = (int)0x00000FCA;

    // Timer3
    public static final int TMR3_BASE = (int);
    public static final int TMR3_TMR3L = (int)0x00000FB2;
    public static final int TMR3_TMR3H = (int)0x00000FB3;
    public static final int TMR3_T3CON = (int)0x00000FB1;

    // Analog-to-Digital Converter
    public static final int ADC_BASE = (int);
    public static final int ADC_ADRESL = (int)0x00000FC3;
    public static final int ADC_ADRESH = (int)0x00000FC4;
    public static final int ADC_ADCON0 = (int)0x00000FC2;
    public static final int ADC_ADCON1 = (int)0x00000FC1;

    // Universal Synchronous Asynchronous Receiver Transmitter
    public static final int USART_BASE = (int);
    public static final int USART_TXREG = (int)0x00000FAC;
    public static final int USART_RCREG = (int)0x00000FAB;
    public static final int USART_SPBRG = (int)0x00000FAF;
    public static final int USART_TXSTA = (int)0x00000FAD;
    public static final int USART_RCSTA = (int)0x00000FAE;

    // Synchronous Serial Port
    public static final int SSP_BASE = (int);
    public static final int SSP_SSPBUF = (int)0x00000FC9;
    public static final int SSP_SSPADD = (int)0x00000FC8;
    public static final int SSP_SSPSTAT = (int)0x00000FC7;
    public static final int SSP_SSPCON1 = (int)0x00000FC6;
    public static final int SSP_SSPCON2 = (int)0x00000FC5;

    // Capture/Compare/PWM 1
    public static final int CCP1_BASE = (int);
    public static final int CCP1_CCPR1L = (int)0x00000FBE;
    public static final int CCP1_CCPR1H = (int)0x00000FBF;
    public static final int CCP1_CCP1CON = (int)0x00000FBD;

    // Capture/Compare/PWM 2
    public static final int CCP2_BASE = (int);
    public static final int CCP2_CCPR2L = (int)0x00000FBA;
    public static final int CCP2_CCPR2H = (int)0x00000FBB;
    public static final int CCP2_CCP2CON = (int)0x00000FB9;

    // 中断向量定义
    public static final int IRQ_HIGH_PRIORITY = 8;  // High priority interrupt
    public static final int IRQ_LOW_PRIORITY = 24;  // Low priority interrupt
    public static final int IRQ_RESET = 0;  // Reset vector

    public static native void pic18f452_init();
}
