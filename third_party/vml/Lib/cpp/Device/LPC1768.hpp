#ifndef LPC1768_HPP
#define LPC1768_HPP

// LPC1768寄存器定义
// 生成自: NXP/LPC17xx/LPC1768
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 12000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint32_t*)0x00000000)

// General Purpose Register 1
#define R1 (*(volatile uint32_t*)0x00000004)

// General Purpose Register 2
#define R2 (*(volatile uint32_t*)0x00000008)

// General Purpose Register 3
#define R3 (*(volatile uint32_t*)0x0000000C)

// General Purpose Register 4
#define R4 (*(volatile uint32_t*)0x00000010)

// General Purpose Register 5
#define R5 (*(volatile uint32_t*)0x00000014)

// General Purpose Register 6
#define R6 (*(volatile uint32_t*)0x00000018)

// General Purpose Register 7
#define R7 (*(volatile uint32_t*)0x0000001C)

// General Purpose Register 8
#define R8 (*(volatile uint32_t*)0x00000020)

// General Purpose Register 9
#define R9 (*(volatile uint32_t*)0x00000024)

// General Purpose Register 10
#define R10 (*(volatile uint32_t*)0x00000028)

// General Purpose Register 11
#define R11 (*(volatile uint32_t*)0x0000002C)

// General Purpose Register 12
#define R12 (*(volatile uint32_t*)0x00000030)

// Stack Pointer
#define SP (*(volatile uint32_t*)0x00000034)

// Link Register
#define LR (*(volatile uint32_t*)0x00000038)

// Program Counter
#define PC (*(volatile uint32_t*)0x0000003C)

// Program Status Register
#define PSR (*(volatile uint32_t*)0x00000040)
#define PSR_N 31  // Negative Flag
#define PSR_Z 30  // Zero Flag
#define PSR_C 29  // Carry Flag
#define PSR_V 28  // Overflow Flag
#define PSR_Q 27  // Saturation Flag
#define PSR_ICI1 0  // Interrupt Continue State
#define PSR_GE 0  // Greater than or Equal
#define PSR_IT 0  // If-Then execution state
#define PSR_APSR 0  // Application Program Status

// Priority Mask Register
#define PRIMASK (*(volatile uint32_t*)0xE0000E20)

// Fault Mask Register
#define FAULTMASK (*(volatile uint32_t*)0xE0000E28)

// Base Priority Register
#define BASEPRI (*(volatile uint32_t*)0xE0000E24)

// Control Register
#define CONTROL (*(volatile uint32_t*)0xE0000E2C)

// 内存段定义
// Main Flash (512KB)
#define FLASH_START 0x00000000
#define FLASH_END 0x0007FFFF
#define FLASH_SIZE 524288

// Boot Flash (32KB)
#define FLASH_BOOT_START 0x00080000
#define FLASH_BOOT_END 0x0007FFFF
#define FLASH_BOOT_SIZE 32768

// SRAM (64KB)
#define SRAM_START 0x10000000
#define SRAM_END 0x1000FFFF
#define SRAM_SIZE 65536

// AHB1 Peripherals
#define AHB1_START 0x20000000
#define AHB1_END 0x200FFFFF
#define AHB1_SIZE 1048576

// APB0 Peripherals
#define APB0_START 0x40000000
#define APB0_END 0x400FFFFF
#define APB0_SIZE 1048576

// APB1 Peripherals
#define APB1_START 0x50000000
#define APB1_END 0x500FFFFF
#define APB1_SIZE 1048576

// 外设定义
// GPIO
#define GPIO_BASE 0x2009C000
#define GPIO_FIODIR (*(volatile uint32_t*)0x2009C000)
#define GPIO_FIOMASK (*(volatile uint32_t*)0x2009C004)
#define GPIO_FIOPIN (*(volatile uint32_t*)0x2009C008)
#define GPIO_FIOSET (*(volatile uint32_t*)0x2009C00C)
#define GPIO_FIOCLR (*(volatile uint32_t*)0x2009C010)
#define GPIO_P0 (*(volatile uint32_t*)0x2009C014)
#define GPIO_P1 (*(volatile uint32_t*)0x2009C018)
#define GPIO_P2 (*(volatile uint32_t*)0x2009C01C)
#define GPIO_P3 (*(volatile uint32_t*)0x2009C020)
#define GPIO_P4 (*(volatile uint32_t*)0x2009C024)

// UART0
#define UART0_BASE 0x4000C000
#define UART0_RBR (*(volatile uint32_t*)0x4000C000)
#define UART0_THR (*(volatile uint32_t*)0x4000C000)
#define UART0_DLL (*(volatile uint32_t*)0x4000C000)
#define UART0_DLM (*(volatile uint32_t*)0x4000C004)
#define UART0_IER (*(volatile uint32_t*)0x4000C004)
#define UART0_IIR (*(volatile uint32_t*)0x4000C008)
#define UART0_FCR (*(volatile uint32_t*)0x4000C008)
#define UART0_LCR (*(volatile uint32_t*)0x4000C00C)
#define UART0_LSR (*(volatile uint32_t*)0x4000C014)
#define UART0_SCR (*(volatile uint32_t*)0x4000C01C)
#define UART0_ACR (*(volatile uint32_t*)0x4000C020)
#define UART0_ICR (*(volatile uint32_t*)0x4000C024)
#define UART0_FDR (*(volatile uint32_t*)0x4000C028)
#define UART0_TER (*(volatile uint32_t*)0x4000C030)

// UART1
#define UART1_BASE 0x4000D000
#define UART1_RBR (*(volatile uint32_t*)0x4000D000)
#define UART1_THR (*(volatile uint32_t*)0x4000D000)
#define UART1_DLL (*(volatile uint32_t*)0x4000D000)
#define UART1_DLM (*(volatile uint32_t*)0x4000D004)
#define UART1_IER (*(volatile uint32_t*)0x4000D004)
#define UART1_IIR (*(volatile uint32_t*)0x4000D008)
#define UART1_LCR (*(volatile uint32_t*)0x4000D00C)
#define UART1_LSR (*(volatile uint32_t*)0x4000D014)
#define UART1_SCR (*(volatile uint32_t*)0x4000D01C)
#define UART1_MSR (*(volatile uint32_t*)0x4000D020)
#define UART1_SCR (*(volatile uint32_t*)0x4000D024)

// UART2
#define UART2_BASE 0x40098000
#define UART2_RBR (*(volatile uint32_t*)0x40098000)
#define UART2_THR (*(volatile uint32_t*)0x40098000)
#define UART2_DLL (*(volatile uint32_t*)0x40098000)
#define UART2_DLM (*(volatile uint32_t*)0x40098004)
#define UART2_IER (*(volatile uint32_t*)0x40098004)
#define UART2_IIR (*(volatile uint32_t*)0x40098008)
#define UART2_LCR (*(volatile uint32_t*)0x4009800C)
#define UART2_LSR (*(volatile uint32_t*)0x40098014)

// UART3
#define UART3_BASE 0x4009C000
#define UART3_RBR (*(volatile uint32_t*)0x4009C000)
#define UART3_THR (*(volatile uint32_t*)0x4009C000)
#define UART3_DLL (*(volatile uint32_t*)0x4009C000)
#define UART3_DLM (*(volatile uint32_t*)0x4009C004)
#define UART3_IER (*(volatile uint32_t*)0x4009C004)
#define UART3_IIR (*(volatile uint32_t*)0x4009C008)
#define UART3_LCR (*(volatile uint32_t*)0x4009C00C)
#define UART3_LSR (*(volatile uint32_t*)0x4009C014)

// SPI0
#define SPI0_BASE 0x40088000
#define SPI0_CR0 (*(volatile uint32_t*)0x40088000)
#define SPI0_CR1 (*(volatile uint32_t*)0x40088004)
#define SPI0_DR (*(volatile uint32_t*)0x40088008)
#define SPI0_SR (*(volatile uint32_t*)0x4008800C)
#define SPI0_CPSR (*(volatile uint32_t*)0x40088010)
#define SPI0_IMSC (*(volatile uint32_t*)0x40088014)
#define SPI0_RIS (*(volatile uint32_t*)0x40088018)
#define SPI0_MIS (*(volatile uint32_t*)0x4008801C)
#define SPI0_ICR (*(volatile uint32_t*)0x40088020)

// SPI1
#define SPI1_BASE 0x4008C000
#define SPI1_CR0 (*(volatile uint32_t*)0x4008C000)
#define SPI1_CR1 (*(volatile uint32_t*)0x4008C004)
#define SPI1_DR (*(volatile uint32_t*)0x4008C008)
#define SPI1_SR (*(volatile uint32_t*)0x4008C00C)
#define SPI1_CPSR (*(volatile uint32_t*)0x4008C010)

// I2C0
#define I2C0_BASE 0x4001C000
#define I2C0_CON (*(volatile uint32_t*)0x4001C000)
#define I2C0_TAR (*(volatile uint32_t*)0x4001C004)
#define I2C0_DAT (*(volatile uint32_t*)0x4001C008)
#define I2C0_SSHC (*(volatile uint32_t*)0x4001C00C)
#define I2C0_HSH (*(volatile uint32_t*)0x4001C010)
#define I2C0_INTX (*(volatile uint32_t*)0x4001C014)
#define I2C0_INTM (*(volatile uint32_t*)0x4001C018)
#define I2C0_AR (*(volatile uint32_t*)0x4001C01C)
#define I2C0_SR (*(volatile uint32_t*)0x4001C020)
#define I2C0_TXFL (*(volatile uint32_t*)0x4001C024)
#define I2C0_RXFL (*(volatile uint32_t*)0x4001C028)
#define I2C0_COMP (*(volatile uint32_t*)0x4001C02C)
#define I2C0_RXFI (*(volatile uint32_t*)0x4001C030)
#define I2C0_RXFT (*(volatile uint32_t*)0x4001C030)

// I2C1
#define I2C1_BASE 0x4001C000
#define I2C1_CON (*(volatile uint32_t*)0x4001C000)
#define I2C1_TAR (*(volatile uint32_t*)0x4001C004)
#define I2C1_DAT (*(volatile uint32_t*)0x4001C008)
#define I2C1_SR (*(volatile uint32_t*)0x4001C020)

// Timer0
#define TIMER0_BASE 0x40004000
#define TIMER0_IR (*(volatile uint32_t*)0x40004000)
#define TIMER0_TCR (*(volatile uint32_t*)0x40004004)
#define TIMER0_TC (*(volatile uint32_t*)0x40004008)
#define TIMER0_PR (*(volatile uint32_t*)0x4000400C)
#define TIMER0_PC (*(volatile uint32_t*)0x40004010)
#define TIMER0_MCR (*(volatile uint32_t*)0x40004014)
#define TIMER0_MR0 (*(volatile uint32_t*)0x40004018)
#define TIMER0_MR1 (*(volatile uint32_t*)0x4000401C)
#define TIMER0_MR2 (*(volatile uint32_t*)0x40004020)
#define TIMER0_MR3 (*(volatile uint32_t*)0x40004024)
#define TIMER0_CCR (*(volatile uint32_t*)0x40004028)
#define TIMER0_CR0 (*(volatile uint32_t*)0x4000402C)
#define TIMER0_CR1 (*(volatile uint32_t*)0x40004030)
#define TIMER0_CR2 (*(volatile uint32_t*)0x40004034)
#define TIMER0_CR3 (*(volatile uint32_t*)0x40004038)
#define TIMER0_EMR (*(volatile uint32_t*)0x4000403C)
#define TIMER0_CTCR (*(volatile uint32_t*)0x40004070)
#define TIMER0_EW (*(volatile uint32_t*)0x40004074)

// Timer1
#define TIMER1_BASE 0x40008000
#define TIMER1_IR (*(volatile uint32_t*)0x40008000)
#define TIMER1_TCR (*(volatile uint32_t*)0x40008004)
#define TIMER1_TC (*(volatile uint32_t*)0x40008008)
#define TIMER1_PR (*(volatile uint32_t*)0x4000800C)
#define TIMER1_MCR (*(volatile uint32_t*)0x40008014)
#define TIMER1_MR0 (*(volatile uint32_t*)0x40008018)
#define TIMER1_MR1 (*(volatile uint32_t*)0x4000801C)
#define TIMER1_MR2 (*(volatile uint32_t*)0x40008020)
#define TIMER1_MR3 (*(volatile uint32_t*)0x40008024)
#define TIMER1_CCR (*(volatile uint32_t*)0x40008028)
#define TIMER1_CR0 (*(volatile uint32_t*)0x4000802C)
#define TIMER1_CR1 (*(volatile uint32_t*)0x40008030)
#define TIMER1_EMR (*(volatile uint32_t*)0x4000803C)

// Timer2
#define TIMER2_BASE 0x400A4000
#define TIMER2_IR (*(volatile uint32_t*)0x400A4000)
#define TIMER2_TCR (*(volatile uint32_t*)0x400A4004)
#define TIMER2_TC (*(volatile uint32_t*)0x400A4008)
#define TIMER2_PR (*(volatile uint32_t*)0x400A400C)
#define TIMER2_MCR (*(volatile uint32_t*)0x400A4014)
#define TIMER2_MR0 (*(volatile uint32_t*)0x400A4018)
#define TIMER2_CCR (*(volatile uint32_t*)0x400A4028)
#define TIMER2_CR0 (*(volatile uint32_t*)0x400A402C)

// Timer3
#define TIMER3_BASE 0x400A8000
#define TIMER3_IR (*(volatile uint32_t*)0x400A8000)
#define TIMER3_TCR (*(volatile uint32_t*)0x400A8004)
#define TIMER3_TC (*(volatile uint32_t*)0x400A8008)
#define TIMER3_PR (*(volatile uint32_t*)0x400A800C)
#define TIMER3_MCR (*(volatile uint32_t*)0x400A8014)
#define TIMER3_MR0 (*(volatile uint32_t*)0x400A8018)
#define TIMER3_CCR (*(volatile uint32_t*)0x400A8028)

// PWM0
#define PWM0_BASE 0x40014000
#define PWM0_IR (*(volatile uint32_t*)0x40014000)
#define PWM0_TCR (*(volatile uint32_t*)0x40014004)
#define PWM0_TC (*(volatile uint32_t*)0x40014008)
#define PWM0_PR (*(volatile uint32_t*)0x4001400C)
#define PWM0_PC (*(volatile uint32_t*)0x40014010)
#define PWM0_MCR (*(volatile uint32_t*)0x40014014)
#define PWM0_MR0 (*(volatile uint32_t*)0x40014018)
#define PWM0_MR1 (*(volatile uint32_t*)0x4001401C)
#define PWM0_MR2 (*(volatile uint32_t*)0x40014020)
#define PWM0_MR3 (*(volatile uint32_t*)0x40014024)
#define PWM0_MR4 (*(volatile uint32_t*)0x40014040)
#define PWM0_MR5 (*(volatile uint32_t*)0x40014044)
#define PWM0_MR6 (*(volatile uint32_t*)0x40014048)
#define PWM0_CCR (*(volatile uint32_t*)0x40014028)
#define PWM0_CR0 (*(volatile uint32_t*)0x4001402C)
#define PWM0_PCR (*(volatile uint32_t*)0x4001404C)
#define PWM0_LER (*(volatile uint32_t*)0x40014050)
#define PWM0_CTCR (*(volatile uint32_t*)0x40014070)

// ADC
#define ADC_BASE 0x400E4000
#define ADC_CR (*(volatile uint32_t*)0x400E4000)
#define ADC_GDR (*(volatile uint32_t*)0x400E4004)
#define ADC_INTEN (*(volatile uint32_t*)0x400E400C)
#define ADC_STATUS (*(volatile uint32_t*)0x400E4010)
#define ADC_TR (*(volatile uint32_t*)0x400E4014)

// DAC
#define DAC_BASE 0x400E5000
#define DAC_CR (*(volatile uint32_t*)0x400E5000)
#define DAC_CTRL (*(volatile uint32_t*)0x400E5004)

// Ethernet
#define ETH_BASE 0x50000000
#define ETH_MAC1 (*(volatile uint32_t*)0x50000000)
#define ETH_MAC2 (*(volatile uint32_t*)0x50000004)
#define ETH_IPGT (*(volatile uint32_t*)0x50000008)
#define ETH_IPGR (*(volatile uint32_t*)0x5000000C)
#define ETH_CLRT (*(volatile uint32_t*)0x50000010)
#define ETH_MAXF (*(volatile uint32_t*)0x50000014)
#define ETH_SUPP (*(volatile uint32_t*)0x50000018)
#define ETH_TEST (*(volatile uint32_t*)0x5000001C)
#define ETH_MCFG (*(volatile uint32_t*)0x50000020)
#define ETH_MCMD (*(volatile uint32_t*)0x50000024)
#define ETH_MADR (*(volatile uint32_t*)0x50000028)
#define ETH_MWTD (*(volatile uint32_t*)0x5000002C)
#define ETH_MRDD (*(volatile uint32_t*)0x50000030)
#define ETH_IND (*(volatile uint32_t*)0x50000034)

// USB Controller
#define USB_BASE 0x50000000
#define USB_HCCHAR (*(volatile uint32_t*)0x50000000)
#define USB_HCINT (*(volatile uint32_t*)0x50000000)
#define USB_HCINTMSK (*(volatile uint32_t*)0x50000000)
#define USB_HCTSIZ (*(volatile uint32_t*)0x50000000)
#define USB_HCDMA (*(volatile uint32_t*)0x50000000)
#define USB_HCDMAB (*(volatile uint32_t*)0x50000000)
#define USB_OTGINTST (*(volatile uint32_t*)0x50000000)
#define USB_OTGINTEN (*(volatile uint32_t*)0x50000000)
#define USB_OTGINTSEL (*(volatile uint32_t*)0x50000000)

// DMA Controller
#define DMA_BASE 0x50004000
#define DMA_INTSTAT (*(volatile uint32_t*)0x50004000)
#define DMA_INTTCSTAT (*(volatile uint32_t*)0x50004004)
#define DMA_INTTCCLEAR (*(volatile uint32_t*)0x50004008)
#define DMA_INTERRSTAT (*(volatile uint32_t*)0x5000400C)
#define DMA_INTERRCLR (*(volatile uint32_t*)0x50004010)
#define DMA_RAWINTSTAT (*(volatile uint32_t*)0x50004014)
#define DMA_RAWINTTCSTAT (*(volatile uint32_t*)0x50004018)
#define DMA_ENBLDCHNS (*(volatile uint32_t*)0x5000401C)
#define DMA_SOFTBREQ (*(volatile uint32_t*)0x50004020)
#define DMA_SOFTSREQ (*(volatile uint32_t*)0x50004024)
#define DMA_CONFIG (*(volatile uint32_t*)0x50004028)
#define DMA_SYNC (*(volatile uint32_t*)0x5000402C)

// Watchdog Timer
#define WDT_BASE 0x40000000
#define WDT_WDMOD (*(volatile uint32_t*)0x40000000)
#define WDT_WDTC (*(volatile uint32_t*)0x40000004)
#define WDT_WDFEED (*(volatile uint32_t*)0x40000008)
#define WDT_WDTV (*(volatile uint32_t*)0x4000000C)

// RTC
#define RTC_BASE 0x40024000
#define RTC_ILR (*(volatile uint32_t*)0x40024000)
#define RTC_CCR (*(volatile uint32_t*)0x40024004)
#define RTC_CIIR (*(volatile uint32_t*)0x40024008)
#define RTC_CWR (*(volatile uint32_t*)0x4002400C)
#define RTC_PREINT (*(volatile uint32_t*)0x40024010)
#define RTC_PREFRAC (*(volatile uint32_t*)0x40024014)
#define RTC_CRT (*(volatile uint32_t*)0x40024018)
#define RTC_SEC (*(volatile uint32_t*)0x4002401C)
#define RTC_MIN (*(volatile uint32_t*)0x40024020)
#define RTC_HOUR (*(volatile uint32_t*)0x40024024)
#define RTC_DOM (*(volatile uint32_t*)0x40024028)
#define RTC_DOW (*(volatile uint32_t*)0x4002402C)
#define RTC_DOY (*(volatile uint32_t*)0x40024030)
#define RTC_MONTH (*(volatile uint32_t*)0x40024034)
#define RTC_YEAR (*(volatile uint32_t*)0x40024038)

// System Control
#define SC_BASE 0x400FC000
#define SC_PLL0CON (*(volatile uint32_t*)0x400FC000)
#define SC_PLL0CFG (*(volatile uint32_t*)0x400FC004)
#define SC_PLL0STAT (*(volatile uint32_t*)0x400FC008)
#define SC_PLL0FEED (*(volatile uint32_t*)0x400FC00C)
#define SC_PLL1CON (*(volatile uint32_t*)0x400FC010)
#define SC_PLL1CFG (*(volatile uint32_t*)0x400FC014)
#define SC_PLL1STAT (*(volatile uint32_t*)0x400FC018)
#define SC_PLL1FEED (*(volatile uint32_t*)0x400FC01C)
#define SC_CCLKCFG (*(volatile uint32_t*)0x400FC020)
#define SC_USBCLKCFG (*(volatile uint32_t*)0x400FC024)
#define SC_CLKSRC (*(volatile uint32_t*)0x400FC028)
#define SC_PCLKSEL0 (*(volatile uint32_t*)0x400FC02C)
#define SC_PCLKSEL1 (*(volatile uint32_t*)0x400FC030)
#define SC_BOSC (*(volatile uint32_t*)0x400FC050)
#define SC_EXTINT (*(volatile uint32_t*)0x400FC054)
#define SC_EXTMODE (*(volatile uint32_t*)0x400FC058)
#define SC_EXTPOL (*(volatile uint32_t*)0x400FC05C)

// Pin Connect Block
#define PINCONNECTBLOCK_BASE 0x4002C000
#define PINCONNECTBLOCK_PINSEL0 (*(volatile uint32_t*)0x4002C000)
#define PINCONNECTBLOCK_PINSEL1 (*(volatile uint32_t*)0x4002C004)
#define PINCONNECTBLOCK_PINSEL2 (*(volatile uint32_t*)0x4002C008)
#define PINCONNECTBLOCK_PINSEL3 (*(volatile uint32_t*)0x4002C00C)
#define PINCONNECTBLOCK_PINSEL4 (*(volatile uint32_t*)0x4002C010)
#define PINCONNECTBLOCK_PINSEL5 (*(volatile uint32_t*)0x4002C014)
#define PINCONNECTBLOCK_PINSEL6 (*(volatile uint32_t*)0x4002C018)
#define PINCONNECTBLOCK_PINSEL7 (*(volatile uint32_t*)0x4002C01C)
#define PINCONNECTBLOCK_PINSEL8 (*(volatile uint32_t*)0x4002C020)
#define PINCONNECTBLOCK_PINSEL9 (*(volatile uint32_t*)0x4002C024)
#define PINCONNECTBLOCK_PINMODE0 (*(volatile uint32_t*)0x4002C040)
#define PINCONNECTBLOCK_PINMODE1 (*(volatile uint32_t*)0x4002C044)
#define PINCONNECTBLOCK_PINMODE2 (*(volatile uint32_t*)0x4002C048)
#define PINCONNECTBLOCK_PINMODE3 (*(volatile uint32_t*)0x4002C04C)
#define PINCONNECTBLOCK_PINMODE4 (*(volatile uint32_t*)0x4002C050)
#define PINCONNECTBLOCK_PINMODE5 (*(volatile uint32_t*)0x4002C054)
#define PINCONNECTBLOCK_PINMODE6 (*(volatile uint32_t*)0x4002C058)
#define PINCONNECTBLOCK_PINMODE7 (*(volatile uint32_t*)0x4002C05C)
#define PINCONNECTBLOCK_PINMODE8 (*(volatile uint32_t*)0x4002C060)
#define PINCONNECTBLOCK_PINMODE9 (*(volatile uint32_t*)0x4002C064)
#define PINCONNECTBLOCK_PINOD0 (*(volatile uint32_t*)0x4002C080)
#define PINCONNECTBLOCK_PINOD1 (*(volatile uint32_t*)0x4002C084)
#define PINCONNECTBLOCK_PINOD2 (*(volatile uint32_t*)0x4002C088)
#define PINCONNECTBLOCK_PINOD3 (*(volatile uint32_t*)0x4002C08C)

// 中断向量定义
#define WDT_VECTOR 0  // Watchdog Timer
#define RESERVED_VECTOR 1  // Reserved
#define DEBUG_MON_VECTOR 2  // ARM Debug Mon
#define RESERVED_VECTOR 3  // Reserved
#define TIMER0_VECTOR 4  // Timer 0
#define TIMER1_VECTOR 5  // Timer 1
#define PWM0_VECTOR 6  // PWM 0
#define UART0_VECTOR 7  // UART 0
#define UART1_VECTOR 8  // UART 1
#define PWM1_VECTOR 9  // PWM 1
#define I2C0_VECTOR 10  // I2C 0
#define I2C1_VECTOR 11  // I2C 1
#define SPI0_VECTOR 12  // SPI 0
#define SPI1_VECTOR 13  // SPI 1
#define RTC_VECTOR 14  // RTC
#define EINT0_VECTOR 15  // External Interrupt 0
#define EINT1_VECTOR 16  // External Interrupt 1
#define EINT2_VECTOR 17  // External Interrupt 2
#define EINT3_VECTOR 18  // External Interrupt 3
#define RESERVED_VECTOR 19  // Reserved
#define ADC_VECTOR 20  // A/D Converter
#define BOD_VECTOR 21  // Brown-Out Detect
#define USB_VECTOR 22  // USB
#define CAN_VECTOR 23  // CAN
#define GP_VECTOR 24  // General Purpose DMA
#define I2S_VECTOR 25  // I2S
#define ETHERNET_VECTOR 26  // Ethernet
#define RIT_VECTOR 27  // Repetitive Interrupt Timer
#define QM_VECTOR 28  // Quadrature Encoder
#define RESERVED_VECTOR 29  // Reserved
#define RESERVED_VECTOR 30  // Reserved

// 引脚定义
#define PIN_RESET 1  // External Reset
#define PIN_P0_0 2  // GPIO Port 0.0
#define PIN_P0_1 3  // GPIO Port 0.1
#define PIN_VSSA 4  // Analog Ground
#define PIN_VDDA 5  // Analog 3.3V
#define PIN_P0_2 6  // GPIO Port 0.2
#define PIN_P0_3 7  // GPIO Port 0.3
#define PIN_P0_4 8  // GPIO Port 0.4
#define PIN_P0_5 9  // GPIO Port 0.5
#define PIN_P0_6 10  // GPIO Port 0.6
#define PIN_P0_7 11  // GPIO Port 0.7
#define PIN_P0_8 12  // GPIO Port 0.8
#define PIN_P0_9 13  // GPIO Port 0.9
#define PIN_P0_10 14  // GPIO Port 0.10
#define PIN_VSS 15  // Ground
#define PIN_VDD 16  // 3.3V
#define PIN_P0_11 17  // GPIO Port 0.11
#define PIN_P0_12 18  // GPIO Port 0.12
#define PIN_P0_13 19  // GPIO Port 0.13
#define PIN_P0_14 20  // GPIO Port 0.14
#define PIN_P0_15 21  // GPIO Port 0.15
#define PIN_P0_16 22  // GPIO Port 0.16
#define PIN_P0_17 23  // GPIO Port 0.17
#define PIN_P0_18 24  // GPIO Port 0.18
#define PIN_P0_19 25  // GPIO Port 0.19
#define PIN_P0_20 26  // GPIO Port 0.20
#define PIN_P0_21 27  // GPIO Port 0.21
#define PIN_P0_22 28  // GPIO Port 0.22
#define PIN_P0_23 29  // GPIO Port 0.23
#define PIN_VSS 30  // Ground
#define PIN_VDD 31  // 3.3V
#define PIN_RTCX1 32  // RTC Crystal Input
#define PIN_RTCX2 33  // RTC Crystal Output
#define PIN_P1_0 34  // GPIO Port 1.0
#define PIN_P1_1 35  // GPIO Port 1.1
#define PIN_P1_2 36  // GPIO Port 1.2
#define PIN_P1_3 37  // GPIO Port 1.3
#define PIN_P1_4 38  // GPIO Port 1.4
#define PIN_P1_5 39  // GPIO Port 1.5
#define PIN_P1_6 40  // GPIO Port 1.6
#define PIN_P1_7 41  // GPIO Port 1.7
#define PIN_P1_8 42  // GPIO Port 1.8
#define PIN_P1_9 43  // GPIO Port 1.9
#define PIN_P1_10 44  // GPIO Port 1.10
#define PIN_P1_11 45  // GPIO Port 1.11
#define PIN_P1_12 46  // GPIO Port 1.12
#define PIN_P1_13 47  // GPIO Port 1.13
#define PIN_P1_14 48  // GPIO Port 1.14
#define PIN_P1_15 49  // GPIO Port 1.15
#define PIN_P1_16 50  // GPIO Port 1.16
#define PIN_P1_17 51  // GPIO Port 1.17
#define PIN_P1_18 52  // GPIO Port 1.18
#define PIN_P1_19 53  // GPIO Port 1.19
#define PIN_P1_20 54  // GPIO Port 1.20
#define PIN_P1_21 55  // GPIO Port 1.21
#define PIN_P1_22 56  // GPIO Port 1.22
#define PIN_P1_23 57  // GPIO Port 1.23
#define PIN_P1_24 58  // GPIO Port 1.24
#define PIN_P1_25 59  // GPIO Port 1.25
#define PIN_P1_26 60  // GPIO Port 1.26
#define PIN_P1_27 61  // GPIO Port 1.27
#define PIN_P1_28 62  // GPIO Port 1.28
#define PIN_P1_29 63  // GPIO Port 1.29
#define PIN_P1_30 64  // GPIO Port 1.30
#define PIN_P1_31 65  // GPIO Port 1.31
#define PIN_P2_0 66  // GPIO Port 2.0
#define PIN_P2_1 67  // GPIO Port 2.1
#define PIN_P2_2 68  // GPIO Port 2.2
#define PIN_P2_3 69  // GPIO Port 2.3
#define PIN_P2_4 70  // GPIO Port 2.4
#define PIN_P2_5 71  // GPIO Port 2.5
#define PIN_P2_6 72  // GPIO Port 2.6
#define PIN_P2_7 73  // GPIO Port 2.7
#define PIN_P2_8 74  // GPIO Port 2.8
#define PIN_P2_9 75  // GPIO Port 2.9
#define PIN_P2_10 76  // GPIO Port 2.10
#define PIN_P2_11 77  // GPIO Port 2.11
#define PIN_P2_12 78  // GPIO Port 2.12
#define PIN_P2_13 79  // GPIO Port 2.13
#define PIN_P2_14 80  // GPIO Port 2.14
#define PIN_P2_15 81  // GPIO Port 2.15
#define PIN_VSS 82  // Ground
#define PIN_VDD 83  // 3.3V

void lpc1768_init(void);

#ifdef __cplusplus
}
#endif

#endif // LPC1768_HPP
