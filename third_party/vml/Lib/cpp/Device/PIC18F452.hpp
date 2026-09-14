#ifndef PIC18F452_HPP
#define PIC18F452_HPP

// PIC18F452寄存器定义
// 生成自: Microchip Technology/PIC18/PIC18F452
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: PIC18
// 位宽: 8位
// 时钟频率: 20000000 Hz

// 寄存器定义
// Working Register
#define WREG (*(volatile uint8_t*)0xFE8)

// Status Register
#define STATUS (*(volatile uint8_t*)0xFD8)

// Bank Select Register
#define BSR (*(volatile uint8_t*)0xFE0)

// Program Counter Low
#define PCL (*(volatile uint8_t*)0xFF9)

// Program Counter Latch High
#define PCLATH (*(volatile uint8_t*)0xFFA)

// Program Counter Latch Upper
#define PCLATU (*(volatile uint8_t*)0xFFB)

// Top of Stack Upper
#define TOSU (*(volatile uint8_t*)0xFFF)

// Top of Stack High
#define TOSH (*(volatile uint8_t*)0xFFE)

// Top of Stack Low
#define TOSL (*(volatile uint8_t*)0xFFD)

// 外设定义
// Port A
#define PORTA_BASE 
#define PORTA_PORTA (*(volatile uint8_t*)0x00000F80)
#define PORTA_TRISA (*(volatile uint8_t*)0x00000F92)
#define PORTA_LATA (*(volatile uint8_t*)0x00000F89)

// Port B
#define PORTB_BASE 
#define PORTB_PORTB (*(volatile uint8_t*)0x00000F81)
#define PORTB_TRISB (*(volatile uint8_t*)0x00000F93)
#define PORTB_LATB (*(volatile uint8_t*)0x00000F8A)

// Port C
#define PORTC_BASE 
#define PORTC_PORTC (*(volatile uint8_t*)0x00000F82)
#define PORTC_TRISC (*(volatile uint8_t*)0x00000F94)
#define PORTC_LATC (*(volatile uint8_t*)0x00000F8B)

// Port D
#define PORTD_BASE 
#define PORTD_PORTD (*(volatile uint8_t*)0x00000F83)
#define PORTD_TRISD (*(volatile uint8_t*)0x00000F95)
#define PORTD_LATD (*(volatile uint8_t*)0x00000F8C)

// Port E
#define PORTE_BASE 
#define PORTE_PORTE (*(volatile uint8_t*)0x00000F84)
#define PORTE_TRISE (*(volatile uint8_t*)0x00000F96)
#define PORTE_LATE (*(volatile uint8_t*)0x00000F8D)

// Timer0
#define TMR0_BASE 
#define TMR0_TMR0L (*(volatile uint8_t*)0x00000FD6)
#define TMR0_TMR0H (*(volatile uint8_t*)0x00000FD7)
#define TMR0_T0CON (*(volatile uint8_t*)0x00000FD5)

// Timer1
#define TMR1_BASE 
#define TMR1_TMR1L (*(volatile uint8_t*)0x00000FCE)
#define TMR1_TMR1H (*(volatile uint8_t*)0x00000FCF)
#define TMR1_T1CON (*(volatile uint8_t*)0x00000FCD)

// Timer2
#define TMR2_BASE 
#define TMR2_TMR2 (*(volatile uint8_t*)0x00000FCC)
#define TMR2_PR2 (*(volatile uint8_t*)0x00000FCB)
#define TMR2_T2CON (*(volatile uint8_t*)0x00000FCA)

// Timer3
#define TMR3_BASE 
#define TMR3_TMR3L (*(volatile uint8_t*)0x00000FB2)
#define TMR3_TMR3H (*(volatile uint8_t*)0x00000FB3)
#define TMR3_T3CON (*(volatile uint8_t*)0x00000FB1)

// Analog-to-Digital Converter
#define ADC_BASE 
#define ADC_ADRESL (*(volatile uint8_t*)0x00000FC3)
#define ADC_ADRESH (*(volatile uint8_t*)0x00000FC4)
#define ADC_ADCON0 (*(volatile uint8_t*)0x00000FC2)
#define ADC_ADCON1 (*(volatile uint8_t*)0x00000FC1)

// Universal Synchronous Asynchronous Receiver Transmitter
#define USART_BASE 
#define USART_TXREG (*(volatile uint8_t*)0x00000FAC)
#define USART_RCREG (*(volatile uint8_t*)0x00000FAB)
#define USART_SPBRG (*(volatile uint8_t*)0x00000FAF)
#define USART_TXSTA (*(volatile uint8_t*)0x00000FAD)
#define USART_RCSTA (*(volatile uint8_t*)0x00000FAE)

// Synchronous Serial Port
#define SSP_BASE 
#define SSP_SSPBUF (*(volatile uint8_t*)0x00000FC9)
#define SSP_SSPADD (*(volatile uint8_t*)0x00000FC8)
#define SSP_SSPSTAT (*(volatile uint8_t*)0x00000FC7)
#define SSP_SSPCON1 (*(volatile uint8_t*)0x00000FC6)
#define SSP_SSPCON2 (*(volatile uint8_t*)0x00000FC5)

// Capture/Compare/PWM 1
#define CCP1_BASE 
#define CCP1_CCPR1L (*(volatile uint8_t*)0x00000FBE)
#define CCP1_CCPR1H (*(volatile uint8_t*)0x00000FBF)
#define CCP1_CCP1CON (*(volatile uint8_t*)0x00000FBD)

// Capture/Compare/PWM 2
#define CCP2_BASE 
#define CCP2_CCPR2L (*(volatile uint8_t*)0x00000FBA)
#define CCP2_CCPR2H (*(volatile uint8_t*)0x00000FBB)
#define CCP2_CCP2CON (*(volatile uint8_t*)0x00000FB9)

// 中断向量定义
#define HIGH_PRIORITY_VECTOR 8  // High priority interrupt
#define LOW_PRIORITY_VECTOR 24  // Low priority interrupt
#define RESET_VECTOR 0  // Reset vector

void pic18f452_init(void);

#ifdef __cplusplus
}
#endif

#endif // PIC18F452_HPP
