#ifndef NRF52840_HPP
#define NRF52840_HPP

// nRF52840寄存器定义
// 生成自: Nordic Semiconductor/nRF52/nRF52840
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 32000000 Hz

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
#define XPSR (*(volatile uint32_t*)0x00000040)
#define XPSR_N 31  // Negative Flag
#define XPSR_Z 30  // Zero Flag
#define XPSR_C 29  // Carry Flag
#define XPSR_V 28  // Overflow Flag
#define XPSR_Q 27  // Saturation Flag
#define XPSR_ICI1 0  // Interrupt Continue State
#define XPSR_GE 0  // Greater than or Equal
#define XPSR_IT 0  // If-Then execution state
#define XPSR_T 24  // Thumb bit
#define XPSR_IPSR 0  // Exception number

// Priority Mask Register
#define PRIMASK (*(volatile uint32_t*)0xE0000E20)

// Base Priority Register
#define BASEPRI (*(volatile uint32_t*)0xE0000E24)

// Fault Mask Register
#define FAULTMASK (*(volatile uint32_t*)0xE0000E28)

// Control Register
#define CONTROL (*(volatile uint32_t*)0xE0000E2C)

// FPU Status Control
#define FPSCR (*(volatile uint32_t*)0xE0000EF34)

// FPU Register S0
#define S0 (*(volatile uint32_t*)0xE0000EF00)

// FPU Register S1
#define S1 (*(volatile uint32_t*)0xE0000EF04)

// FPU Register S2
#define S2 (*(volatile uint32_t*)0xE0000EF08)

// FPU Register S3
#define S3 (*(volatile uint32_t*)0xE0000EF0C)

// FPU Register S4
#define S4 (*(volatile uint32_t*)0xE0000EF10)

// FPU Register S5
#define S5 (*(volatile uint32_t*)0xE0000EF14)

// FPU Register S6
#define S6 (*(volatile uint32_t*)0xE0000EF18)

// FPU Register S7
#define S7 (*(volatile uint32_t*)0xE0000EF1C)

// FPU Register S8
#define S8 (*(volatile uint32_t*)0xE0000EF20)

// FPU Register S9
#define S9 (*(volatile uint32_t*)0xE0000EF24)

// FPU Register S10
#define S10 (*(volatile uint32_t*)0xE0000EF28)

// FPU Register S11
#define S11 (*(volatile uint32_t*)0xE0000EF2C)

// FPU Register S12
#define S12 (*(volatile uint32_t*)0xE0000EF30)

// FPU Register S13
#define S13 (*(volatile uint32_t*)0xE0000EF34)

// FPU Register S14
#define S14 (*(volatile uint32_t*)0xE0000EF38)

// FPU Register S15
#define S15 (*(volatile uint32_t*)0xE0000EF3C)

// FPU Register S16
#define S16 (*(volatile uint32_t*)0xE0000EF40)

// FPU Register S17
#define S17 (*(volatile uint32_t*)0xE0000EF44)

// FPU Register S18
#define S18 (*(volatile uint32_t*)0xE0000EF48)

// FPU Register S19
#define S19 (*(volatile uint32_t*)0xE0000EF4C)

// FPU Register S20
#define S20 (*(volatile uint32_t*)0xE0000EF50)

// FPU Register S21
#define S21 (*(volatile uint32_t*)0xE0000EF54)

// FPU Register S22
#define S22 (*(volatile uint32_t*)0xE0000EF58)

// FPU Register S23
#define S23 (*(volatile uint32_t*)0xE0000EF5C)

// FPU Register S24
#define S24 (*(volatile uint32_t*)0xE0000EF60)

// FPU Register S25
#define S25 (*(volatile uint32_t*)0xE0000EF64)

// FPU Register S26
#define S26 (*(volatile uint32_t*)0xE0000EF68)

// FPU Register S27
#define S27 (*(volatile uint32_t*)0xE0000EF6C)

// FPU Register S28
#define S28 (*(volatile uint32_t*)0xE0000EF70)

// FPU Register S29
#define S29 (*(volatile uint32_t*)0xE0000EF74)

// FPU Register S30
#define S30 (*(volatile uint32_t*)0xE0000EF78)

// FPU Register S31
#define S31 (*(volatile uint32_t*)0xE0000EF7C)

// 内存段定义
// Flash (1MB)
#define FLASH_START 0x00000000
#define FLASH_END 0x0FFFFF
#define FLASH_SIZE 1048576

// SRAM (256KB)
#define SRAM_START 0x20000000
#define SRAM_END 0x2003FFFF
#define SRAM_SIZE 262144

// SRAM Low (128KB)
#define SRAM_LOW_START 0x20000000
#define SRAM_LOW_END 0x2001FFFF
#define SRAM_LOW_SIZE 131072

// SRAM High (128KB)
#define SRAM_HIGH_START 0x20020000
#define SRAM_HIGH_END 0x2003FFFF
#define SRAM_HIGH_SIZE 131072

// Factory Information Configuration
#define FICR_START 0x10000000
#define FICR_END 0x10001000
#define FICR_SIZE 4096

// User Information Configuration
#define UICR_START 0x10001000
#define UICR_END 0x10001000
#define UICR_SIZE 4096

// Peripheral Space
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x50000000
#define PERIPHERAL_SIZE 268435456

// 外设定义
// GPIO
#define GPIO_BASE 0x50000000
#define GPIO_OUT (*(volatile uint32_t*)0x50000000)
#define GPIO_OUTSET (*(volatile uint32_t*)0x50000004)
#define GPIO_OUTCLR (*(volatile uint32_t*)0x50000008)
#define GPIO_IN (*(volatile uint32_t*)0x5000000C)
#define GPIO_DIR (*(volatile uint32_t*)0x50000010)
#define GPIO_DIRSET (*(volatile uint32_t*)0x50000014)
#define GPIO_DIRCLR (*(volatile uint32_t*)0x50000018)
#define GPIO_PIN_CNF0 (*(volatile uint32_t*)0x50000300)
#define GPIO_PIN_CNF1 (*(volatile uint32_t*)0x50000304)
#define GPIO_PIN_CNF2 (*(volatile uint32_t*)0x50000308)
#define GPIO_PIN_CNF3 (*(volatile uint32_t*)0x5000030C)
#define GPIO_PIN_CNF4 (*(volatile uint32_t*)0x50000310)
#define GPIO_PIN_CNF5 (*(volatile uint32_t*)0x50000314)
#define GPIO_PIN_CNF6 (*(volatile uint32_t*)0x50000318)
#define GPIO_PIN_CNF7 (*(volatile uint32_t*)0x5000031C)
#define GPIO_PIN_CNF8 (*(volatile uint32_t*)0x50000320)
#define GPIO_PIN_CNF9 (*(volatile uint32_t*)0x50000324)
#define GPIO_PIN_CNF10 (*(volatile uint32_t*)0x50000328)
#define GPIO_PIN_CNF11 (*(volatile uint32_t*)0x5000032C)
#define GPIO_PIN_CNF12 (*(volatile uint32_t*)0x50000330)
#define GPIO_PIN_CNF13 (*(volatile uint32_t*)0x50000334)
#define GPIO_PIN_CNF14 (*(volatile uint32_t*)0x50000338)
#define GPIO_PIN_CNF15 (*(volatile uint32_t*)0x5000033C)
#define GPIO_PIN_CNF16 (*(volatile uint32_t*)0x50000340)
#define GPIO_PIN_CNF17 (*(volatile uint32_t*)0x50000344)
#define GPIO_PIN_CNF18 (*(volatile uint32_t*)0x50000348)
#define GPIO_PIN_CNF19 (*(volatile uint32_t*)0x5000034C)
#define GPIO_PIN_CNF20 (*(volatile uint32_t*)0x50000350)
#define GPIO_PIN_CNF21 (*(volatile uint32_t*)0x50000354)
#define GPIO_PIN_CNF22 (*(volatile uint32_t*)0x50000358)
#define GPIO_PIN_CNF23 (*(volatile uint32_t*)0x5000035C)
#define GPIO_PIN_CNF24 (*(volatile uint32_t*)0x50000360)
#define GPIO_PIN_CNF25 (*(volatile uint32_t*)0x50000364)
#define GPIO_PIN_CNF26 (*(volatile uint32_t*)0x50000368)
#define GPIO_PIN_CNF27 (*(volatile uint32_t*)0x5000036C)
#define GPIO_PIN_CNF28 (*(volatile uint32_t*)0x50000370)
#define GPIO_PIN_CNF29 (*(volatile uint32_t*)0x50000374)
#define GPIO_PIN_CNF30 (*(volatile uint32_t*)0x50000378)
#define GPIO_PIN_CNF31 (*(volatile uint32_t*)0x5000037C)

// UART0
#define UART0_BASE 0x40002000
#define UART0_TASKS_STARTRX (*(volatile uint32_t*)0x40002000)
#define UART0_TASKS_STARTTX (*(volatile uint32_t*)0x40002004)
#define UART0_TASKS_STOPRX (*(volatile uint32_t*)0x40002008)
#define UART0_TASKS_STOPTX (*(volatile uint32_t*)0x4000200C)
#define UART0_SUBSCRIBED_STARTRX (*(volatile uint32_t*)0x40002010)
#define UART0_SUBSCRIBED_STARTTX (*(volatile uint32_t*)0x40002014)
#define UART0_EVENTS_CTS (*(volatile uint32_t*)0x40002100)
#define UART0_EVENTS_NCTS (*(volatile uint32_t*)0x40002104)
#define UART0_EVENTS_RXDRDY (*(volatile uint32_t*)0x40002108)
#define UART0_EVENTS_TXDRDY (*(volatile uint32_t*)0x4000210C)
#define UART0_EVENTS_ERROR (*(volatile uint32_t*)0x40002110)
#define UART0_RXD (*(volatile uint32_t*)0x40002518)
#define UART0_TXD (*(volatile uint32_t*)0x4000251C)
#define UART0_BAUDRATE (*(volatile uint32_t*)0x40002524)
#define UART0_CONFIG (*(volatile uint32_t*)0x4000256C)
#define UART0_INTEN (*(volatile uint32_t*)0x40002700)
#define UART0_INTENSET (*(volatile uint32_t*)0x40002704)
#define UART0_INTENCLR (*(volatile uint32_t*)0x40002708)

// UART1
#define UART1_BASE 0x40003000
#define UART1_TASKS_STARTRX (*(volatile uint32_t*)0x40003000)
#define UART1_TASKS_STARTTX (*(volatile uint32_t*)0x40003004)
#define UART1_EVENTS_RXDRDY (*(volatile uint32_t*)0x40003108)
#define UART1_EVENTS_TXDRDY (*(volatile uint32_t*)0x4000310C)
#define UART1_RXD (*(volatile uint32_t*)0x40003518)
#define UART1_TXD (*(volatile uint32_t*)0x4000351C)
#define UART1_BAUDRATE (*(volatile uint32_t*)0x40003524)

// SPI0
#define SPI0_BASE 0x40003000
#define SPI0_EVENTS_READY (*(volatile uint32_t*)0x40003108)
#define SPI0_RXD (*(volatile uint32_t*)0x40003518)
#define SPI0_TXD (*(volatile uint32_t*)0x4000351C)
#define SPI0_FREQUENCY (*(volatile uint32_t*)0x40003524)
#define SPI0_CONFIG (*(volatile uint32_t*)0x4000356C)
#define SPI0_INTEN (*(volatile uint32_t*)0x40003700)

// SPI1
#define SPI1_BASE 0x40004000
#define SPI1_EVENTS_READY (*(volatile uint32_t*)0x40004108)
#define SPI1_RXD (*(volatile uint32_t*)0x40004518)
#define SPI1_TXD (*(volatile uint32_t*)0x4000451C)
#define SPI1_FREQUENCY (*(volatile uint32_t*)0x40004524)
#define SPI1_CONFIG (*(volatile uint32_t*)0x4000456C)

// SPI2
#define SPI2_BASE 0x40005000
#define SPI2_EVENTS_READY (*(volatile uint32_t*)0x40005108)
#define SPI2_RXD (*(volatile uint32_t*)0x40005518)
#define SPI2_TXD (*(volatile uint32_t*)0x4000551C)
#define SPI2_FREQUENCY (*(volatile uint32_t*)0x40005524)

// I2C0
#define I2C0_BASE 0x40003000
#define I2C0_TASKS_STARTRX (*(volatile uint32_t*)0x40003000)
#define I2C0_TASKS_STARTTX (*(volatile uint32_t*)0x40003008)
#define I2C0_TASKS_STOP (*(volatile uint32_t*)0x40003014)
#define I2C0_EVENTS_DONE (*(volatile uint32_t*)0x40003108)
#define I2C0_EVENTS_TXDSENT (*(volatile uint32_t*)0x4000310C)
#define I2C0_EVENTS_ERROR (*(volatile uint32_t*)0x40003110)
#define I2C0_RXD (*(volatile uint32_t*)0x40003518)
#define I2C0_TXD (*(volatile uint32_t*)0x4000351C)
#define I2C0_ADDRESS (*(volatile uint32_t*)0x40003524)

// I2C1
#define I2C1_BASE 0x40004000
#define I2C1_TASKS_STARTRX (*(volatile uint32_t*)0x40004000)
#define I2C1_TASKS_STARTTX (*(volatile uint32_t*)0x40004008)
#define I2C1_TASKS_STOP (*(volatile uint32_t*)0x40004014)
#define I2C1_EVENTS_DONE (*(volatile uint32_t*)0x40004108)
#define I2C1_RXD (*(volatile uint32_t*)0x40004518)
#define I2C1_TXD (*(volatile uint32_t*)0x4000451C)
#define I2C1_ADDRESS (*(volatile uint32_t*)0x40004524)

// Timer0
#define TIMER0_BASE 0x40008000
#define TIMER0_TASKS_START (*(volatile uint32_t*)0x40008000)
#define TIMER0_TASKS_STOP (*(volatile uint32_t*)0x40008004)
#define TIMER0_TASKS_COUNT (*(volatile uint32_t*)0x40008008)
#define TIMER0_TASKS_CLEAR (*(volatile uint32_t*)0x4000800C)
#define TIMER0_CC0 (*(volatile uint32_t*)0x40008400)
#define TIMER0_CC1 (*(volatile uint32_t*)0x40008404)
#define TIMER0_CC2 (*(volatile uint32_t*)0x40008408)
#define TIMER0_CC3 (*(volatile uint32_t*)0x4000840C)
#define TIMER0_SHORTS (*(volatile uint32_t*)0x40008200)
#define TIMER0_INTEN (*(volatile uint32_t*)0x40008700)
#define TIMER0_MODE (*(volatile uint32_t*)0x40008510)
#define TIMER0_BITMODE (*(volatile uint32_t*)0x40008514)

// Timer1
#define TIMER1_BASE 0x40009000
#define TIMER1_TASKS_START (*(volatile uint32_t*)0x40009000)
#define TIMER1_TASKS_STOP (*(volatile uint32_t*)0x40009004)
#define TIMER1_TASKS_COUNT (*(volatile uint32_t*)0x40009008)
#define TIMER1_TASKS_CLEAR (*(volatile uint32_t*)0x4000900C)
#define TIMER1_CC0 (*(volatile uint32_t*)0x40009400)
#define TIMER1_CC1 (*(volatile uint32_t*)0x40009404)
#define TIMER1_CC2 (*(volatile uint32_t*)0x40009408)
#define TIMER1_CC3 (*(volatile uint32_t*)0x4000940C)
#define TIMER1_SHORTS (*(volatile uint32_t*)0x40009200)

// Timer2
#define TIMER2_BASE 0x4000A000
#define TIMER2_TASKS_START (*(volatile uint32_t*)0x4000A000)
#define TIMER2_TASKS_STOP (*(volatile uint32_t*)0x4000A004)
#define TIMER2_TASKS_COUNT (*(volatile uint32_t*)0x4000A008)
#define TIMER2_TASKS_CLEAR (*(volatile uint32_t*)0x4000A00C)
#define TIMER2_CC0 (*(volatile uint32_t*)0x4000A400)
#define TIMER2_CC1 (*(volatile uint32_t*)0x4000A404)
#define TIMER2_CC2 (*(volatile uint32_t*)0x4000A408)
#define TIMER2_CC3 (*(volatile uint32_t*)0x4000A40C)

// Timer3
#define TIMER3_BASE 0x4000B000
#define TIMER3_TASKS_START (*(volatile uint32_t*)0x4000B000)
#define TIMER3_TASKS_STOP (*(volatile uint32_t*)0x4000B004)
#define TIMER3_TASKS_COUNT (*(volatile uint32_t*)0x4000B008)
#define TIMER3_TASKS_CLEAR (*(volatile uint32_t*)0x4000B00C)
#define TIMER3_CC0 (*(volatile uint32_t*)0x4000B400)
#define TIMER3_CC1 (*(volatile uint32_t*)0x4000B404)
#define TIMER3_CC2 (*(volatile uint32_t*)0x4000B408)
#define TIMER3_CC3 (*(volatile uint32_t*)0x4000B40C)

// Timer4
#define TIMER4_BASE 0x4000C000
#define TIMER4_TASKS_START (*(volatile uint32_t*)0x4000C000)
#define TIMER4_TASKS_STOP (*(volatile uint32_t*)0x4000C004)
#define TIMER4_TASKS_COUNT (*(volatile uint32_t*)0x4000C008)
#define TIMER4_TASKS_CLEAR (*(volatile uint32_t*)0x4000C00C)
#define TIMER4_CC0 (*(volatile uint32_t*)0x4000C400)
#define TIMER4_CC1 (*(volatile uint32_t*)0x4000C404)
#define TIMER4_CC2 (*(volatile uint32_t*)0x4000C408)
#define TIMER4_CC3 (*(volatile uint32_t*)0x4000C40C)

// RTC0
#define RTC0_BASE 0x4000B000
#define RTC0_TASKS_START (*(volatile uint32_t*)0x4000B000)
#define RTC0_TASKS_STOP (*(volatile uint32_t*)0x4000B004)
#define RTC0_TASKS_TRIGOVRFLW (*(volatile uint32_t*)0x4000B010)
#define RTC0_EVENTS_TICK (*(volatile uint32_t*)0x4000B100)
#define RTC0_EVENTS_OVRFLW (*(volatile uint32_t*)0x4000B104)
#define RTC0_EVENTS_COMPARE0 (*(volatile uint32_t*)0x4000B140)
#define RTC0_EVENTS_COMPARE1 (*(volatile uint32_t*)0x4000B144)
#define RTC0_EVENTS_COMPARE2 (*(volatile uint32_t*)0x4000B148)
#define RTC0_EVENTS_COMPARE3 (*(volatile uint32_t*)0x4000B14C)
#define RTC0_CC0 (*(volatile uint32_t*)0x4000B400)
#define RTC0_CC1 (*(volatile uint32_t*)0x4000B404)
#define RTC0_CC2 (*(volatile uint32_t*)0x4000B408)
#define RTC0_CC3 (*(volatile uint32_t*)0x4000B40C)
#define RTC0_CNT (*(volatile uint32_t*)0x4000B500)
#define RTC0_PRESCALER (*(volatile uint32_t*)0x4000B504)
#define RTC0_TICK (*(volatile uint32_t*)0x4000B510)

// RTC1
#define RTC1_BASE 0x4000D000
#define RTC1_TASKS_START (*(volatile uint32_t*)0x4000D000)
#define RTC1_TASKS_STOP (*(volatile uint32_t*)0x4000D004)
#define RTC1_EVENTS_TICK (*(volatile uint32_t*)0x4000D100)
#define RTC1_EVENTS_OVRFLW (*(volatile uint32_t*)0x4000D104)
#define RTC1_EVENTS_COMPARE0 (*(volatile uint32_t*)0x4000D140)
#define RTC1_EVENTS_COMPARE1 (*(volatile uint32_t*)0x4000D144)
#define RTC1_CC0 (*(volatile uint32_t*)0x4000D400)
#define RTC1_CC1 (*(volatile uint32_t*)0x4000D404)
#define RTC1_CNT (*(volatile uint32_t*)0x4000D500)
#define RTC1_PRESCALER (*(volatile uint32_t*)0x4000D504)

// PWM0
#define PWM0_BASE 0x4001C000
#define PWM0_TASKS_START (*(volatile uint32_t*)0x4001C000)
#define PWM0_TASKS_STOP (*(volatile uint32_t*)0x4001C004)
#define PWM0_TASKS_SEQSTART0 (*(volatile uint32_t*)0x4001C008)
#define PWM0_TASKS_SEQSTART1 (*(volatile uint32_t*)0x4001C00C)
#define PWM0_TASKS_NEXTSTEP (*(volatile uint32_t*)0x4001C010)
#define PWM0_EVENTS_PWMPERIODEND (*(volatile uint32_t*)0x4001C108)
#define PWM0_EVENTS_LOOPEND (*(volatile uint32_t*)0x4001C10C)
#define PWM0_EVENTS_SEQEND0 (*(volatile uint32_t*)0x4001C110)
#define PWM0_EVENTS_SEQEND1 (*(volatile uint32_t*)0x4001C114)
#define PWM0_SEQ0_PTR (*(volatile uint32_t*)0x4001C510)
#define PWM0_SEQ1_PTR (*(volatile uint32_t*)0x4001C514)
#define PWM0_SEQ0_CNT (*(volatile uint32_t*)0x4001C528)
#define PWM0_SEQ1_CNT (*(volatile uint32_t*)0x4001C52C)
#define PWM0_SEQ0_REFRESH (*(volatile uint32_t*)0x4001C530)
#define PWM0_SEQ1_REFRESH (*(volatile uint32_t*)0x4001C534)
#define PWM0_DECODER (*(volatile uint32_t*)0x4001C540)
#define PWM0_LOOP (*(volatile uint32_t*)0x4001C544)
#define PWM0_MODE (*(volatile uint32_t*)0x4001C500)
#define PWM0_CLKEN (*(volatile uint32_t*)0x4001C504)
#define PWM0_CNT (*(volatile uint32_t*)0x4001C548)

// PWM1
#define PWM1_BASE 0x4001D000
#define PWM1_TASKS_START (*(volatile uint32_t*)0x4001D000)
#define PWM1_TASKS_STOP (*(volatile uint32_t*)0x4001D004)
#define PWM1_SEQ0_PTR (*(volatile uint32_t*)0x4001D510)
#define PWM1_SEQ1_PTR (*(volatile uint32_t*)0x4001D514)
#define PWM1_MODE (*(volatile uint32_t*)0x4001D500)
#define PWM1_CNT (*(volatile uint32_t*)0x4001D548)

// PWM2
#define PWM2_BASE 0x4001E000
#define PWM2_TASKS_START (*(volatile uint32_t*)0x4001E000)
#define PWM2_SEQ0_PTR (*(volatile uint32_t*)0x4001E510)
#define PWM2_MODE (*(volatile uint32_t*)0x4001E500)

// PWM3
#define PWM3_BASE 0x4001F000
#define PWM3_TASKS_START (*(volatile uint32_t*)0x4001F000)
#define PWM3_SEQ0_PTR (*(volatile uint32_t*)0x4001F510)
#define PWM3_MODE (*(volatile uint32_t*)0x4001F500)

// ADC
#define ADC_BASE 0x40012000
#define ADC_TASKS_START (*(volatile uint32_t*)0x40012000)
#define ADC_TASKS_STOP (*(volatile uint32_t*)0x40012004)
#define ADC_EVENTS_DONE (*(volatile uint32_t*)0x40012108)
#define ADC_EVENTS_RESULTDONE (*(volatile uint32_t*)0x4001210C)
#define ADC_EVENTS_CALIBRATEDONE (*(volatile uint32_t*)0x40012110)
#define ADC_EVENTS_CH_LIMITH (*(volatile uint32_t*)0x40012114)
#define ADC_EVENTS_CH_LIMITL (*(volatile uint32_t*)0x40012118)
#define ADC_RESULT (*(volatile uint32_t*)0x40012400)
#define ADC_CH0_CONFIG (*(volatile uint32_t*)0x40012510)
#define ADC_CH1_CONFIG (*(volatile uint32_t*)0x40012514)
#define ADC_CH2_CONFIG (*(volatile uint32_t*)0x40012518)
#define ADC_CH3_CONFIG (*(volatile uint32_t*)0x4001251C)
#define ADC_CH4_CONFIG (*(volatile uint32_t*)0x40012520)
#define ADC_CH5_CONFIG (*(volatile uint32_t*)0x40012524)
#define ADC_CH6_CONFIG (*(volatile uint32_t*)0x40012528)
#define ADC_CH7_CONFIG (*(volatile uint32_t*)0x4001252C)
#define ADC_CONFIG (*(volatile uint32_t*)0x40012530)
#define ADC_TASKS_CALIBRATELOAD (*(volatile uint32_t*)0x40012034)
#define ADC_INTEN (*(volatile uint32_t*)0x40012700)

// DAC
#define DAC_BASE 0x40013000
#define DAC_TASKS_START (*(volatile uint32_t*)0x40013000)
#define DAC_TASKS_STOP (*(volatile uint32_t*)0x40013004)
#define DAC_EVENTS_DONE (*(volatile uint32_t*)0x40013108)
#define DAC_VALUE (*(volatile uint32_t*)0x40013400)
#define DAC_CEN (*(volatile uint32_t*)0x40013504)

// Analog Comparator
#define COMP_BASE 0x40013000
#define COMP_TASKS_START (*(volatile uint32_t*)0x40013000)
#define COMP_TASKS_STOP (*(volatile uint32_t*)0x40013004)
#define COMP_TASKS_SETTLE (*(volatile uint32_t*)0x40013010)
#define COMP_EVENTS_READY (*(volatile uint32_t*)0x40013108)
#define COMP_EVENTS_DOWN (*(volatile uint32_t*)0x4001310C)
#define COMP_EVENTS_UP (*(volatile uint32_t*)0x40013110)
#define COMP_EVENTS_CROSS (*(volatile uint32_t*)0x40013114)
#define COMP_RESULT (*(volatile uint32_t*)0x40013400)
#define COMP_EN (*(volatile uint32_t*)0x40013500)
#define COMP_TASK_MODE (*(volatile uint32_t*)0x40013504)
#define COMP_REFSEL (*(volatile uint32_t*)0x40013508)
#define COMP_EXTREFSEL (*(volatile uint32_t*)0x4001350C)
#define COMP_THD (*(volatile uint32_t*)0x40013510)
#define COMP_HYST (*(volatile uint32_t*)0x40013514)
#define COMP_SPEED (*(volatile uint32_t*)0x40013518)
#define COMP_ISOURCE (*(volatile uint32_t*)0x4001351C)
#define COMP_PSEL (*(volatile uint32_t*)0x40013520)
#define COMP_NOREF (*(volatile uint32_t*)0x40013524)
#define COMP_INTEN (*(volatile uint32_t*)0x40013700)

// Quadrature Decoder
#define QDEC_BASE 0x40014000
#define QDEC_TASKS_START (*(volatile uint32_t*)0x40014000)
#define QDEC_TASKS_STOP (*(volatile uint32_t*)0x40014004)
#define QDEC_TASKS_RDCLRACC (*(volatile uint32_t*)0x40014008)
#define QDEC_TASKS_RDCLRDBL (*(volatile uint32_t*)0x4001400C)
#define QDEC_TASKS_RDCLRPH (*(volatile uint32_t*)0x40014010)
#define QDEC_EVENTS_READY (*(volatile uint32_t*)0x40014108)
#define QDEC_EVENTS_DBLRDY (*(volatile uint32_t*)0x4001410C)
#define QDEC_EVENTS_QCLR (*(volatile uint32_t*)0x40014110)
#define QDEC_ACC (*(volatile uint32_t*)0x40014404)
#define QDEC_ACCREAD (*(volatile uint32_t*)0x40014408)
#define QDEC_DBLINC (*(volatile uint32_t*)0x40014410)
#define QDEC_DBL (*(volatile uint32_t*)0x40014418)
#define QDEC_DBLREAD (*(volatile uint32_t*)0x4001441C)
#define QDEC_PHASE (*(volatile uint32_t*)0x40014420)
#define QDEC_PHASEREAD (*(volatile uint32_t*)0x40014424)
#define QDEC_LEFLL (*(volatile uint32_t*)0x40014428)
#define QDEC_INTEN (*(volatile uint32_t*)0x40014700)

// Event Generators Unit 0
#define EGU0_BASE 0x40014000
#define EGU0_TASKS_TRIGGER0 (*(volatile uint32_t*)0x40014000)
#define EGU0_TASKS_TRIGGER1 (*(volatile uint32_t*)0x40014004)
#define EGU0_TASKS_TRIGGER2 (*(volatile uint32_t*)0x40014008)
#define EGU0_TASKS_TRIGGER3 (*(volatile uint32_t*)0x4001400C)
#define EGU0_TASKS_TRIGGER4 (*(volatile uint32_t*)0x40014010)
#define EGU0_TASKS_TRIGGER5 (*(volatile uint32_t*)0x40014014)
#define EGU0_TASKS_TRIGGER6 (*(volatile uint32_t*)0x40014018)
#define EGU0_TASKS_TRIGGER7 (*(volatile uint32_t*)0x4001401C)
#define EGU0_TASKS_TRIGGER8 (*(volatile uint32_t*)0x40014020)
#define EGU0_TASKS_TRIGGER9 (*(volatile uint32_t*)0x40014024)
#define EGU0_TASKS_TRIGGER10 (*(volatile uint32_t*)0x40014028)
#define EGU0_TASKS_TRIGGER11 (*(volatile uint32_t*)0x4001402C)
#define EGU0_TASKS_TRIGGER12 (*(volatile uint32_t*)0x40014030)
#define EGU0_TASKS_TRIGGER13 (*(volatile uint32_t*)0x40014034)
#define EGU0_TASKS_TRIGGER14 (*(volatile uint32_t*)0x40014038)
#define EGU0_TASKS_TRIGGER15 (*(volatile uint32_t*)0x4001403C)
#define EGU0_EVENTS_EVENT0 (*(volatile uint32_t*)0x40014100)
#define EGU0_EVENTS_EVENT1 (*(volatile uint32_t*)0x40014104)
#define EGU0_EVENTS_EVENT2 (*(volatile uint32_t*)0x40014108)
#define EGU0_EVENTS_EVENT3 (*(volatile uint32_t*)0x4001410C)
#define EGU0_EVENTS_EVENT4 (*(volatile uint32_t*)0x40014110)
#define EGU0_EVENTS_EVENT5 (*(volatile uint32_t*)0x40014114)
#define EGU0_INTEN (*(volatile uint32_t*)0x40014700)

// Random Number Generator
#define RNG_BASE 0x40006000
#define RNG_TASKS_START (*(volatile uint32_t*)0x40006000)
#define RNG_TASKS_STOP (*(volatile uint32_t*)0x40006004)
#define RNG_EVENTS_VALRDY (*(volatile uint32_t*)0x40006108)
#define RNG_VALUE (*(volatile uint32_t*)0x40006400)
#define RNG_CONFIG (*(volatile uint32_t*)0x40006504)
#define RNG_INTEN (*(volatile uint32_t*)0x40006700)

// AES ECB
#define AES_BASE 0x40005000
#define AES_TASKS_START (*(volatile uint32_t*)0x40005000)
#define AES_TASKS_STOP (*(volatile uint32_t*)0x40005004)
#define AES_EVENTS_END (*(volatile uint32_t*)0x40005108)
#define AES_EVENTS_ERROR (*(volatile uint32_t*)0x4000510C)
#define AES_CRYPTCNTXT (*(volatile uint32_t*)0x40005400)
#define AES_CRYPTCNTCPY (*(volatile uint32_t*)0x40005404)
#define AES_CRYPTCMD (*(volatile uint32_t*)0x40005500)
#define AES_INTEN (*(volatile uint32_t*)0x40005700)

// Cryptocell
#define CRYPTO_BASE 0x4000E000
#define CRYPTO_TASKS_START (*(volatile uint32_t*)0x4000E000)
#define CRYPTO_TASKS_STOP (*(volatile uint32_t*)0x4000E004)
#define CRYPTO_EVENTS_DONE (*(volatile uint32_t*)0x4000E108)
#define CRYPTO_EVENTS_ERROR (*(volatile uint32_t*)0x4000E10C)
#define CRYPTO_DMA (*(volatile uint32_t*)0x4000E400)
#define CRYPTO_CMDS (*(volatile uint32_t*)0x4000E404)
#define CRYPTO_CMDS_AMOUNT (*(volatile uint32_t*)0x4000E408)
#define CRYPTO_INTENSET (*(volatile uint32_t*)0x4000E704)
#define CRYPTO_INTENCLR (*(volatile uint32_t*)0x4000E708)
#define CRYPTO_INTCONTEXT (*(volatile uint32_t*)0x4000E710)

// USB
#define USB_BASE 0x40027000
#define USB_TASKS_STARTUP (*(volatile uint32_t*)0x40027000)
#define USB_TASKS_SUSPEND (*(volatile uint32_t*)0x40027004)
#define USB_TASKS_RESUME (*(volatile uint32_t*)0x40027008)
#define USB_EVENTS_ENDRDY (*(volatile uint32_t*)0x40027108)
#define USB_EVENTS_SUSPENDED (*(volatile uint32_t*)0x4002710C)
#define USB_EVENTS_RESUMED (*(volatile uint32_t*)0x40027110)
#define USB_EVENTS_SOF (*(volatile uint32_t*)0x40027114)
#define USB_EVENTS_EPOF (*(volatile uint32_t*)0x40027118)
#define USB_EVENTS_DATA (*(volatile uint32_t*)0x4002711C)
#define USB_EVENTS_EP0DATADONE (*(volatile uint32_t*)0x40027120)
#define USB_EVENTS_EP0SETUP (*(volatile uint32_t*)0x40027124)
#define USB_EVENTS_EP0HALTD (*(volatile uint32_t*)0x40027128)
#define USB_EVENTS_EP1DMA (*(volatile uint32_t*)0x40027134)
#define USB_EVENTS_EP2DMA (*(volatile uint32_t*)0x40027138)
#define USB_EVENTS_EP3DMA (*(volatile uint32_t*)0x4002713C)
#define USB_EVENTS_EP4DMA (*(volatile uint32_t*)0x40027140)
#define USB_EVENTS_EP1 (*(volatile uint32_t*)0x40027158)
#define USB_EVENTS_EP2 (*(volatile uint32_t*)0x4002715C)
#define USB_EVENTS_EP3 (*(volatile uint32_t*)0x40027160)
#define USB_EVENTS_EP4 (*(volatile uint32_t*)0x40027164)
#define USB_EVENTS_EP5 (*(volatile uint32_t*)0x40027168)
#define USB_EVENTS_EP6 (*(volatile uint32_t*)0x4002716C)
#define USB_EVENTS_EP7 (*(volatile uint32_t*)0x40027170)
#define USB_EVENTS_EP8 (*(volatile uint32_t*)0x40027174)
#define USB_EVENTS_EP9 (*(volatile uint32_t*)0x40027178)
#define USB_EVENTS_EP10 (*(volatile uint32_t*)0x4002717C)
#define USB_EVENTS_EP11 (*(volatile uint32_t*)0x40027180)
#define USB_EVENTS_EP12 (*(volatile uint32_t*)0x40027184)
#define USB_EVENTS_EP13 (*(volatile uint32_t*)0x40027188)
#define USB_EVENTS_EP14 (*(volatile uint32_t*)0x4002718C)
#define USB_EVENTS_EP15 (*(volatile uint32_t*)0x40027190)
#define USB_USBADDR (*(volatile uint32_t*)0x40027500)
#define USB_USBREQ (*(volatile uint32_t*)0x40027504)
#define USB_USBVAL (*(volatile uint32_t*)0x40027508)
#define USB_USBINDEX (*(volatile uint32_t*)0x4002750C)
#define USB_USBCONFIG (*(volatile uint32_t*)0x40027510)
#define USB_EPIN (*(volatile uint32_t*)0x40027514)
#define USB_EPOUT (*(volatile uint32_t*)0x40027518)
#define USB_EPLEN (*(volatile uint32_t*)0x40027520)
#define USB_EPSIZE (*(volatile uint32_t*)0x40027524)
#define USB_EPDMA (*(volatile uint32_t*)0x40027500)
#define USB_EPDMA (*(volatile uint32_t*)0x40027504)
#define USB_INTEN (*(volatile uint32_t*)0x40027700)
#define USB_INTENSET (*(volatile uint32_t*)0x40027704)
#define USB_INTENCLR (*(volatile uint32_t*)0x40027708)

// Watchdog Timer
#define WDT_BASE 0x40011000
#define WDT_TASKS_START (*(volatile uint32_t*)0x40011000)
#define WDT_TASKS_KEEP (*(volatile uint32_t*)0x40011004)
#define WDT_TASKS_STOP (*(volatile uint32_t*)0x40011008)
#define WDT_EVENTS_TIMEOUT (*(volatile uint32_t*)0x40011108)
#define WDT_RUNSTATUS (*(volatile uint32_t*)0x40011404)
#define WDT_REQSTATUS (*(volatile uint32_t*)0x40011408)
#define WDT_CRV (*(volatile uint32_t*)0x40011504)
#define WDT_RCV (*(volatile uint32_t*)0x40011508)
#define WDT_CONFIG (*(volatile uint32_t*)0x4001150C)
#define WDT_INTEN (*(volatile uint32_t*)0x40011700)

// Reset
#define NRF_RESET_BASE 0x40000000
#define NRF_RESET_RESET (*(volatile uint32_t*)0x40000000)
#define NRF_RESET_RESET_FAC (*(volatile uint32_t*)0x40000400)
#define NRF_RESET_RESET_NFAC (*(volatile uint32_t*)0x40000500)

// Clock
#define CLOCK_BASE 0x40000000
#define CLOCK_TASKS_HFCLKSTART (*(volatile uint32_t*)0x40000000)
#define CLOCK_TASKS_HFCLKSTOP (*(volatile uint32_t*)0x40000004)
#define CLOCK_TASKS_LFCLKSTART (*(volatile uint32_t*)0x40000008)
#define CLOCK_TASKS_LFCLKSTOP (*(volatile uint32_t*)0x4000000C)
#define CLOCK_TASKS_CAL (*(volatile uint32_t*)0x40000010)
#define CLOCK_TASKS_CTTO (*(volatile uint32_t*)0x40000010)
#define CLOCK_EVENTS_HFCLKSTATED (*(volatile uint32_t*)0x40000100)
#define CLOCK_EVENTS_LFCLKSTATED (*(volatile uint32_t*)0x40000104)
#define CLOCK_EVENTS_DONE (*(volatile uint32_t*)0x40000108)
#define CLOCK_EVENTS_CTTO (*(volatile uint32_t*)0x4000010C)
#define CLOCK_HFCLKSTAT (*(volatile uint32_t*)0x40000400)
#define CLOCK_LFCLKSTAT (*(volatile uint32_t*)0x40000404)
#define CLOCK_LFCLKSRC (*(volatile uint32_t*)0x40000508)
#define CLOCK_CTIV (*(volatile uint32_t*)0x4000050C)
#define CLOCK_INTEN (*(volatile uint32_t*)0x40000700)

// Power
#define POWER_BASE 0x40000000
#define POWER_TASKS_CONSTLAT (*(volatile uint32_t*)0x40000000)
#define POWER_TASKS_LOWPWR (*(volatile uint32_t*)0x40000004)
#define POWER_EVENTS_POWERDEBUG (*(volatile uint32_t*)0x40000100)
#define POWER_EVENTS_SLEEPDEBUG (*(volatile uint32_t*)0x40000104)
#define POWER_INTEN (*(volatile uint32_t*)0x40000700)

// GPIO Tasks and Events
#define GPIOTE_BASE 0x40006000
#define GPIOTE_TASKS_SET0 (*(volatile uint32_t*)0x40006000)
#define GPIOTE_TASKS_SET1 (*(volatile uint32_t*)0x40006004)
#define GPIOTE_TASKS_SET2 (*(volatile uint32_t*)0x40006008)
#define GPIOTE_TASKS_SET3 (*(volatile uint32_t*)0x4000600C)
#define GPIOTE_TASKS_CLR0 (*(volatile uint32_t*)0x40006010)
#define GPIOTE_TASKS_CLR1 (*(volatile uint32_t*)0x40006014)
#define GPIOTE_TASKS_CLR2 (*(volatile uint32_t*)0x40006018)
#define GPIOTE_TASKS_CLR3 (*(volatile uint32_t*)0x4000601C)
#define GPIOTE_EVENTS_IN0 (*(volatile uint32_t*)0x40006100)
#define GPIOTE_EVENTS_IN1 (*(volatile uint32_t*)0x40006104)
#define GPIOTE_EVENTS_IN2 (*(volatile uint32_t*)0x40006108)
#define GPIOTE_EVENTS_IN3 (*(volatile uint32_t*)0x4000610C)
#define GPIOTE_EVENTS_IN4 (*(volatile uint32_t*)0x40006110)
#define GPIOTE_EVENTS_IN5 (*(volatile uint32_t*)0x40006114)
#define GPIOTE_EVENTS_IN6 (*(volatile uint32_t*)0x40006118)
#define GPIOTE_EVENTS_IN7 (*(volatile uint32_t*)0x4000611C)
#define GPIOTE_EVENTS_TOUCH (*(volatile uint32_t*)0x40006140)
#define GPIOTE_EVENTS_LISR (*(volatile uint32_t*)0x40006144)
#define GPIOTE_EVENTS_LISF (*(volatile uint32_t*)0x40006148)
#define GPIOTE_EVENTS_COUNT (*(volatile uint32_t*)0x4000614C)
#define GPIOTE_CONFIG0 (*(volatile uint32_t*)0x40006510)
#define GPIOTE_CONFIG1 (*(volatile uint32_t*)0x40006514)
#define GPIOTE_CONFIG2 (*(volatile uint32_t*)0x40006518)
#define GPIOTE_CONFIG3 (*(volatile uint32_t*)0x4000651C)
#define GPIOTE_CONFIG4 (*(volatile uint32_t*)0x40006520)
#define GPIOTE_CONFIG5 (*(volatile uint32_t*)0x40006524)
#define GPIOTE_CONFIG6 (*(volatile uint32_t*)0x40006528)
#define GPIOTE_CONFIG7 (*(volatile uint32_t*)0x4000652C)
#define GPIOTE_INTEN (*(volatile uint32_t*)0x40006700)

// Real Time Timer
#define RTT_BASE 0x40009000
#define RTT_TASKS_START (*(volatile uint32_t*)0x40009000)
#define RTT_TASKS_STOP (*(volatile uint32_t*)0x40009004)
#define RTT_TASKS_TRIGOVRFLW (*(volatile uint32_t*)0x40009010)
#define RTT_EVENTS_TICK (*(volatile uint32_t*)0x40009100)
#define RTT_EVENTS_OVRFLW (*(volatile uint32_t*)0x40009104)
#define RTT_EVENTS_COMPARE0 (*(volatile uint32_t*)0x40009140)
#define RTT_CC0 (*(volatile uint32_t*)0x40009400)
#define RTT_CC1 (*(volatile uint32_t*)0x40009404)
#define RTT_CC2 (*(volatile uint32_t*)0x40009408)
#define RTT_CC3 (*(volatile uint32_t*)0x4000940C)
#define RTT_CNT (*(volatile uint32_t*)0x40009500)
#define RTT_PRESCALER (*(volatile uint32_t*)0x40009504)

// Inter-Process Communication
#define IPC_BASE 0x40014000
#define IPC_TASKS_SEND0 (*(volatile uint32_t*)0x40014000)
#define IPC_TASKS_SEND1 (*(volatile uint32_t*)0x40014004)
#define IPC_TASKS_SEND2 (*(volatile uint32_t*)0x40014008)
#define IPC_TASKS_SEND3 (*(volatile uint32_t*)0x4001400C)
#define IPC_TASKS_SEND4 (*(volatile uint32_t*)0x40014010)
#define IPC_TASKS_SEND5 (*(volatile uint32_t*)0x40014014)
#define IPC_TASKS_SEND6 (*(volatile uint32_t*)0x40014018)
#define IPC_TASKS_SEND7 (*(volatile uint32_t*)0x4001401C)
#define IPC_TASKS_RECEIVE0 (*(volatile uint32_t*)0x40014080)
#define IPC_TASKS_RECEIVE1 (*(volatile uint32_t*)0x40014084)
#define IPC_TASKS_RECEIVE2 (*(volatile uint32_t*)0x40014088)
#define IPC_TASKS_RECEIVE3 (*(volatile uint32_t*)0x4001408C)
#define IPC_TASKS_RECEIVE4 (*(volatile uint32_t*)0x40014090)
#define IPC_TASKS_RECEIVE5 (*(volatile uint32_t*)0x40014094)
#define IPC_TASKS_RECEIVE6 (*(volatile uint32_t*)0x40014098)
#define IPC_TASKS_RECEIVE7 (*(volatile uint32_t*)0x4001409C)
#define IPC_EVENTS_SENT0 (*(volatile uint32_t*)0x40014100)
#define IPC_EVENTS_SENT1 (*(volatile uint32_t*)0x40014104)
#define IPC_EVENTS_SENT2 (*(volatile uint32_t*)0x40014108)
#define IPC_EVENTS_SENT3 (*(volatile uint32_t*)0x4001410C)
#define IPC_EVENTS_SENT4 (*(volatile uint32_t*)0x40014110)
#define IPC_EVENTS_SENT5 (*(volatile uint32_t*)0x40014114)
#define IPC_EVENTS_SENT6 (*(volatile uint32_t*)0x40014118)
#define IPC_EVENTS_SENT7 (*(volatile uint32_t*)0x4001411C)
#define IPC_EVENTS_RECEIVE0 (*(volatile uint32_t*)0x40014180)
#define IPC_EVENTS_RECEIVE1 (*(volatile uint32_t*)0x40014184)
#define IPC_EVENTS_RECEIVE2 (*(volatile uint32_t*)0x40014188)
#define IPC_EVENTS_RECEIVE3 (*(volatile uint32_t*)0x4001418C)
#define IPC_EVENTS_RECEIVE4 (*(volatile uint32_t*)0x40014190)
#define IPC_EVENTS_RECEIVE5 (*(volatile uint32_t*)0x40014194)
#define IPC_EVENTS_RECEIVE6 (*(volatile uint32_t*)0x40014198)
#define IPC_EVENTS_RECEIVE7 (*(volatile uint32_t*)0x4001419C)
#define IPC_CH0 (*(volatile uint32_t*)0x40014500)
#define IPC_CH1 (*(volatile uint32_t*)0x40014504)
#define IPC_CH2 (*(volatile uint32_t*)0x40014508)
#define IPC_CH3 (*(volatile uint32_t*)0x4001450C)
#define IPC_CH4 (*(volatile uint32_t*)0x40014510)
#define IPC_CH5 (*(volatile uint32_t*)0x40014514)
#define IPC_CH6 (*(volatile uint32_t*)0x40014518)
#define IPC_CH7 (*(volatile uint32_t*)0x4001451C)
#define IPC_INTEN (*(volatile uint32_t*)0x40014700)

// 中断向量定义
#define POWER_VECTOR 0  // Power
#define RADIO_VECTOR 1  // RADIO
#define UART0_VECTOR 2  // UART0
#define UART1_VECTOR 3  // UART1
#define SPI0_VECTOR 4  // SPI0
#define SPI1_VECTOR 5  // SPI1
#define SPI2_VECTOR 6  // SPI2
#define GPIOTE_VECTOR 7  // GPIOTE
#define ADC_VECTOR 8  // ADC
#define TIMER0_VECTOR 9  // TIMER0
#define TIMER1_VECTOR 10  // TIMER1
#define TIMER2_VECTOR 11  // TIMER2
#define TIMER3_VECTOR 12  // TIMER3
#define TIMER4_VECTOR 13  // TIMER4
#define RTC0_VECTOR 14  // RTC0
#define RTC1_VECTOR 15  // RTC1
#define TEMP_VECTOR 16  // TEMP
#define RNG_VECTOR 17  // RNG
#define WDT_VECTOR 18  // WDT
#define IPC_VECTOR 19  // IPC
#define PWM0_VECTOR 20  // PWM0
#define PWM1_VECTOR 21  // PWM1
#define PWM2_VECTOR 22  // PWM2
#define PWM3_VECTOR 23  // PWM3
#define ZAR_VECTOR 24  // RESERVED
#define EGU0_VECTOR 25  // EGU0
#define EGU1_VECTOR 26  // EGU1
#define EGU2_VECTOR 27  // EGU2
#define EGU3_VECTOR 28  // EGU3
#define EGU4_VECTOR 29  // EGU4
#define EGU5_VECTOR 30  // EGU5
#define RESERVED_VECTOR 31  // RESERVED
#define SPIM0_VECTOR 32  // SPIM0
#define SPIM1_VECTOR 33  // SPIM1
#define SPIM2_VECTOR 34  // SPIM2
#define RESERVED_VECTOR 35  // RESERVED
#define RESERVED_VECTOR 36  // RESERVED
#define USB_VECTOR 37  // USB
#define RESERVED_VECTOR 38  // RESERVED
#define RESERVED_VECTOR 39  // RESERVED
#define RESERVED_VECTOR 40  // RESERVED
#define RESERVED_VECTOR 41  // RESERVED
#define RESERVED_VECTOR 42  // RESERVED
#define CRYPTOCELL_VECTOR 43  // CRYPTOCELL
#define RESERVED_VECTOR 44  // RESERVED
#define RESERVED_VECTOR 45  // RESERVED
#define RESERVED_VECTOR 46  // RESERVED
#define RESERVED_VECTOR 47  // RESERVED

// 引脚定义
#define PIN_VDD 1  // 3.3V power supply
#define PIN_VDD 2  // 3.3V power supply
#define PIN_DEC4 3  // Decoupling 4
#define PIN_DEC5 4  // Decoupling 5
#define PIN_P0_01 5  // GPIO Port 0.01
#define PIN_P0_02 6  // GPIO Port 0.02
#define PIN_P0_03 7  // GPIO Port 0.03
#define PIN_P0_04 8  // GPIO Port 0.04
#define PIN_P0_05 9  // GPIO Port 0.05
#define PIN_P0_06 10  // GPIO Port 0.06
#define PIN_P0_07 11  // GPIO Port 0.07
#define PIN_P0_08 12  // GPIO Port 0.08
#define PIN_P0_09 13  // GPIO Port 0.09
#define PIN_P0_10 14  // GPIO Port 0.10
#define PIN_P0_11 15  // GPIO Port 0.11
#define PIN_P0_12 16  // GPIO Port 0.12
#define PIN_P0_13 17  // GPIO Port 0.13
#define PIN_P0_14 18  // GPIO Port 0.14
#define PIN_P0_15 19  // GPIO Port 0.15
#define PIN_P0_16 20  // GPIO Port 0.16
#define PIN_P0_17 21  // GPIO Port 0.17
#define PIN_P0_18 22  // GPIO Port 0.18
#define PIN_P0_19 23  // GPIO Port 0.19
#define PIN_P0_20 24  // GPIO Port 0.20
#define PIN_P0_21 25  // GPIO Port 0.21
#define PIN_P0_22 26  // GPIO Port 0.22
#define PIN_P0_23 27  // GPIO Port 0.23
#define PIN_P0_24 28  // GPIO Port 0.24
#define PIN_P0_25 29  // GPIO Port 0.25
#define PIN_P0_26 30  // GPIO Port 0.26
#define PIN_P0_27 31  // GPIO Port 0.27
#define PIN_P0_28 32  // GPIO Port 0.28
#define PIN_P0_29 33  // GPIO Port 0.29
#define PIN_P0_30 34  // GPIO Port 0.30
#define PIN_P0_31 35  // GPIO Port 0.31
#define PIN_P1_00 36  // GPIO Port 1.00
#define PIN_P1_01 37  // GPIO Port 1.01
#define PIN_P1_02 38  // GPIO Port 1.02
#define PIN_P1_03 39  // GPIO Port 1.03
#define PIN_P1_04 40  // GPIO Port 1.04
#define PIN_P1_05 41  // GPIO Port 1.05
#define PIN_P1_06 42  // GPIO Port 1.06
#define PIN_P1_07 43  // GPIO Port 1.07
#define PIN_P1_08 44  // GPIO Port 1.08
#define PIN_P1_09 45  // GPIO Port 1.09
#define PIN_P1_10 46  // GPIO Port 1.10
#define PIN_P1_11 47  // GPIO Port 1.11
#define PIN_P1_12 48  // GPIO Port 1.12
#define PIN_P1_13 49  // GPIO Port 1.13
#define PIN_P1_14 50  // GPIO Port 1.14
#define PIN_P1_15 51  // GPIO Port 1.15
#define PIN_VDD 52  // 3.3V power supply
#define PIN_VDD 53  // 3.3V power supply
#define PIN_DEC1 54  // Decoupling 1
#define PIN_DEC2 55  // Decoupling 2
#define PIN_DEC3 56  // Decoupling 3
#define PIN_NFC1 57  // NFC 1
#define PIN_NFC2 58  // NFC 2
#define PIN_P0_16 59  // GPIO Port 0.16
#define PIN_SWDIO 60  // SWD I/O
#define PIN_SWDCLK 61  // SWD Clock
#define PIN_RESET 62  // Reset

void nrf52840_init(void);

#ifdef __cplusplus
}
#endif

#endif // NRF52840_HPP
