using System;

namespace VML.Device.MicrochipTechnology.PIC18F452
{
    /// <summary>
    /// PIC18F452 寄存器定义
    /// 生成自: Microchip Technology/PIC18/PIC18F452
    /// 版本: 1.0
    /// </summary>
    public static class PIC18F452
    {
        // CPU架构: PIC18, 8位, 20000000 Hz

        // 寄存器定义
        // Working Register
        public const int WREG_ADDR = 0xFE8;
        public static unsafe byte* WREG => (byte*)0xFE8;

        // Status Register
        public const int STATUS_ADDR = 0xFD8;
        public static unsafe byte* STATUS => (byte*)0xFD8;

        // Bank Select Register
        public const int BSR_ADDR = 0xFE0;
        public static unsafe byte* BSR => (byte*)0xFE0;

        // Program Counter Low
        public const int PCL_ADDR = 0xFF9;
        public static unsafe byte* PCL => (byte*)0xFF9;

        // Program Counter Latch High
        public const int PCLATH_ADDR = 0xFFA;
        public static unsafe byte* PCLATH => (byte*)0xFFA;

        // Program Counter Latch Upper
        public const int PCLATU_ADDR = 0xFFB;
        public static unsafe byte* PCLATU => (byte*)0xFFB;

        // Top of Stack Upper
        public const int TOSU_ADDR = 0xFFF;
        public static unsafe byte* TOSU => (byte*)0xFFF;

        // Top of Stack High
        public const int TOSH_ADDR = 0xFFE;
        public static unsafe byte* TOSH => (byte*)0xFFE;

        // Top of Stack Low
        public const int TOSL_ADDR = 0xFFD;
        public static unsafe byte* TOSL => (byte*)0xFFD;

        // 外设定义
        // Port A
        public const int PORTA_BASE = ;
        public static unsafe byte* PORTA_PORTA => (byte*)0x00000F80;
        public static unsafe byte* PORTA_TRISA => (byte*)0x00000F92;
        public static unsafe byte* PORTA_LATA => (byte*)0x00000F89;

        // Port B
        public const int PORTB_BASE = ;
        public static unsafe byte* PORTB_PORTB => (byte*)0x00000F81;
        public static unsafe byte* PORTB_TRISB => (byte*)0x00000F93;
        public static unsafe byte* PORTB_LATB => (byte*)0x00000F8A;

        // Port C
        public const int PORTC_BASE = ;
        public static unsafe byte* PORTC_PORTC => (byte*)0x00000F82;
        public static unsafe byte* PORTC_TRISC => (byte*)0x00000F94;
        public static unsafe byte* PORTC_LATC => (byte*)0x00000F8B;

        // Port D
        public const int PORTD_BASE = ;
        public static unsafe byte* PORTD_PORTD => (byte*)0x00000F83;
        public static unsafe byte* PORTD_TRISD => (byte*)0x00000F95;
        public static unsafe byte* PORTD_LATD => (byte*)0x00000F8C;

        // Port E
        public const int PORTE_BASE = ;
        public static unsafe byte* PORTE_PORTE => (byte*)0x00000F84;
        public static unsafe byte* PORTE_TRISE => (byte*)0x00000F96;
        public static unsafe byte* PORTE_LATE => (byte*)0x00000F8D;

        // Timer0
        public const int TMR0_BASE = ;
        public static unsafe byte* TMR0_TMR0L => (byte*)0x00000FD6;
        public static unsafe byte* TMR0_TMR0H => (byte*)0x00000FD7;
        public static unsafe byte* TMR0_T0CON => (byte*)0x00000FD5;

        // Timer1
        public const int TMR1_BASE = ;
        public static unsafe byte* TMR1_TMR1L => (byte*)0x00000FCE;
        public static unsafe byte* TMR1_TMR1H => (byte*)0x00000FCF;
        public static unsafe byte* TMR1_T1CON => (byte*)0x00000FCD;

        // Timer2
        public const int TMR2_BASE = ;
        public static unsafe byte* TMR2_TMR2 => (byte*)0x00000FCC;
        public static unsafe byte* TMR2_PR2 => (byte*)0x00000FCB;
        public static unsafe byte* TMR2_T2CON => (byte*)0x00000FCA;

        // Timer3
        public const int TMR3_BASE = ;
        public static unsafe byte* TMR3_TMR3L => (byte*)0x00000FB2;
        public static unsafe byte* TMR3_TMR3H => (byte*)0x00000FB3;
        public static unsafe byte* TMR3_T3CON => (byte*)0x00000FB1;

        // Analog-to-Digital Converter
        public const int ADC_BASE = ;
        public static unsafe byte* ADC_ADRESL => (byte*)0x00000FC3;
        public static unsafe byte* ADC_ADRESH => (byte*)0x00000FC4;
        public static unsafe byte* ADC_ADCON0 => (byte*)0x00000FC2;
        public static unsafe byte* ADC_ADCON1 => (byte*)0x00000FC1;

        // Universal Synchronous Asynchronous Receiver Transmitter
        public const int USART_BASE = ;
        public static unsafe byte* USART_TXREG => (byte*)0x00000FAC;
        public static unsafe byte* USART_RCREG => (byte*)0x00000FAB;
        public static unsafe byte* USART_SPBRG => (byte*)0x00000FAF;
        public static unsafe byte* USART_TXSTA => (byte*)0x00000FAD;
        public static unsafe byte* USART_RCSTA => (byte*)0x00000FAE;

        // Synchronous Serial Port
        public const int SSP_BASE = ;
        public static unsafe byte* SSP_SSPBUF => (byte*)0x00000FC9;
        public static unsafe byte* SSP_SSPADD => (byte*)0x00000FC8;
        public static unsafe byte* SSP_SSPSTAT => (byte*)0x00000FC7;
        public static unsafe byte* SSP_SSPCON1 => (byte*)0x00000FC6;
        public static unsafe byte* SSP_SSPCON2 => (byte*)0x00000FC5;

        // Capture/Compare/PWM 1
        public const int CCP1_BASE = ;
        public static unsafe byte* CCP1_CCPR1L => (byte*)0x00000FBE;
        public static unsafe byte* CCP1_CCPR1H => (byte*)0x00000FBF;
        public static unsafe byte* CCP1_CCP1CON => (byte*)0x00000FBD;

        // Capture/Compare/PWM 2
        public const int CCP2_BASE = ;
        public static unsafe byte* CCP2_CCPR2L => (byte*)0x00000FBA;
        public static unsafe byte* CCP2_CCPR2H => (byte*)0x00000FBB;
        public static unsafe byte* CCP2_CCP2CON => (byte*)0x00000FB9;

        // 中断向量定义
        public const int IRQ_HIGH_PRIORITY = 8;  // High priority interrupt
        public const int IRQ_LOW_PRIORITY = 24;  // Low priority interrupt
        public const int IRQ_RESET = 0;  // Reset vector

        public static void pic18f452_init()
        {
            // 硬件初始化代码
        }
    }
}
