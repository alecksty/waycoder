#ifndef MSP432P401R_HPP
#define MSP432P401R_HPP

// MSP432P401R寄存器定义
// 生成自: Texas Instruments/MSP432/MSP432P401R
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 48000000 Hz

// 外设定义
// eUSCI_A0 UART
#define UART0_BASE 0x40001000
#define UART0_CTLW0 (*(volatile uint16_t*)0x40001000)
#define UART0_BRW (*(volatile uint16_t*)0x40001006)
#define UART0_UCA0TXBUF (*(volatile uint16_t*)0x40001008)
#define UART0_UCA0RXBUF (*(volatile uint16_t*)0x4000100A)
#define UART0_IFG (*(volatile uint16_t*)0x4000100C)
#define UART0_IE (*(volatile uint16_t*)0x4000100E)

// eUSCI_A1 UART
#define UART1_BASE 0x40002000
#define UART1_CTLW0 (*(volatile uint16_t*)0x40002000)
#define UART1_BRW (*(volatile uint16_t*)0x40002006)
#define UART1_TXBUF (*(volatile uint16_t*)0x40002008)
#define UART1_RXBUF (*(volatile uint16_t*)0x4000200A)
#define UART1_IFG (*(volatile uint16_t*)0x4000200C)
#define UART1_IE (*(volatile uint16_t*)0x4000200E)

// Timer_A0 16bit
#define TIMER0_BASE 0x40003000
#define TIMER0_CTL (*(volatile uint16_t*)0x40003000)
#define TIMER0_R (*(volatile uint16_t*)0x40003010)
#define TIMER0_CCR0 (*(volatile uint16_t*)0x40003012)
#define TIMER0_CCR1 (*(volatile uint16_t*)0x40003014)
#define TIMER0_CCR2 (*(volatile uint16_t*)0x40003016)
#define TIMER0_EX0 (*(volatile uint16_t*)0x40003020)

// ADC14 14-bit
#define ADC14_BASE 0x40006000
#define ADC14_CTL0 (*(volatile uint16_t*)0x40006000)
#define ADC14_CTL1 (*(volatile uint16_t*)0x40006002)
#define ADC14_LO (*(volatile uint16_t*)0x40006004)
#define ADC14_HI (*(volatile uint16_t*)0x40006006)
#define ADC14_MCTL0 (*(volatile uint16_t*)0x40006008)
#define ADC14_MEM0 (*(volatile uint16_t*)0x40006020)

void msp432p401r_init(void);

#ifdef __cplusplus
}
#endif

#endif // MSP432P401R_HPP
