#ifndef RP2040_HPP
#define RP2040_HPP

// RP2040寄存器定义
// 生成自: Raspberry Pi/RP/RP2040
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M0+
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
#define XPSR (*(volatile uint32_t*)0x00000040)
#define XPSR_N 31  // Negative Flag
#define XPSR_Z 30  // Zero Flag
#define XPSR_C 29  // Carry Flag
#define XPSR_V 28  // Overflow Flag
#define XPSR_Q 27  // Saturation Flag
#define XPSR_ICI 0  // ICI execution state
#define XPSR_IT 0  // If-Then execution state
#define XPSR_T 24  // Thumb bit
#define XPSR_IPSR 0  // Exception number

// Priority Mask Register
#define PRIMASK (*(volatile uint32_t*)0xE0000E20)

// Control Register
#define CONTROL (*(volatile uint32_t*)0xE0000E24)

// Fault Mask Register
#define FAULTMASK (*(volatile uint32_t*)0xE0000E28)

// 内存段定义
// ROM (bootloader)
#define ROM_START 0x00000000
#define ROM_END 0x00001000
#define ROM_SIZE 4096

// SRAM0 (16KB)
#define SRAM0_START 0x20000000
#define SRAM0_END 0x20003FFF
#define SRAM0_SIZE 16384

// SRAM1 (16KB)
#define SRAM1_START 0x20004000
#define SRAM1_END 0x20007FFF
#define SRAM1_SIZE 16384

// SRAM2 (16KB)
#define SRAM2_START 0x20008000
#define SRAM2_END 0x2000BFFF
#define SRAM2_SIZE 16384

// SRAM3 (16KB)
#define SRAM3_START 0x2000C000
#define SRAM3_END 0x2000FFFF
#define SRAM3_SIZE 16384

// SRAM4 (16KB)
#define SRAM4_START 0x20010000
#define SRAM4_END 0x20013FFF
#define SRAM4_SIZE 16384

// APB Peripherals
#define APB_START 0x40000000
#define APB_END 0x400FFFFF
#define APB_SIZE 1048576

// AHB Peripherals
#define AHB_START 0x50000000
#define AHB_END 0x500FFFFF
#define AHB_SIZE 1048576

// 外设定义
// IO Bank 0
#define IO_BANK0_BASE 0x40014000
#define IO_BANK0_GPIO0_STATUS (*(volatile uint32_t*)0x40014000)
#define IO_BANK0_GPIO0_CTRL (*(volatile uint32_t*)0x40014004)
#define IO_BANK0_GPIO1_STATUS (*(volatile uint32_t*)0x40014008)
#define IO_BANK0_GPIO1_CTRL (*(volatile uint32_t*)0x4001400C)
#define IO_BANK0_GPIO2_STATUS (*(volatile uint32_t*)0x40014010)
#define IO_BANK0_GPIO2_CTRL (*(volatile uint32_t*)0x40014014)
#define IO_BANK0_GPIO3_STATUS (*(volatile uint32_t*)0x40014018)
#define IO_BANK0_GPIO3_CTRL (*(volatile uint32_t*)0x4001401C)
#define IO_BANK0_GPIO4_STATUS (*(volatile uint32_t*)0x40014020)
#define IO_BANK0_GPIO4_CTRL (*(volatile uint32_t*)0x40014024)
#define IO_BANK0_GPIO5_STATUS (*(volatile uint32_t*)0x40014028)
#define IO_BANK0_GPIO5_CTRL (*(volatile uint32_t*)0x4001402C)
#define IO_BANK0_GPIO6_STATUS (*(volatile uint32_t*)0x40014030)
#define IO_BANK0_GPIO6_CTRL (*(volatile uint32_t*)0x40014034)
#define IO_BANK0_GPIO7_STATUS (*(volatile uint32_t*)0x40014038)
#define IO_BANK0_GPIO7_CTRL (*(volatile uint32_t*)0x4001403C)
#define IO_BANK0_GPIO8_STATUS (*(volatile uint32_t*)0x40014040)
#define IO_BANK0_GPIO8_CTRL (*(volatile uint32_t*)0x40014044)
#define IO_BANK0_GPIO9_STATUS (*(volatile uint32_t*)0x40014048)
#define IO_BANK0_GPIO9_CTRL (*(volatile uint32_t*)0x4001404C)
#define IO_BANK0_GPIO10_STATUS (*(volatile uint32_t*)0x40014050)
#define IO_BANK0_GPIO10_CTRL (*(volatile uint32_t*)0x40014054)
#define IO_BANK0_GPIO11_STATUS (*(volatile uint32_t*)0x40014058)
#define IO_BANK0_GPIO11_CTRL (*(volatile uint32_t*)0x4001405C)
#define IO_BANK0_GPIO12_STATUS (*(volatile uint32_t*)0x40014060)
#define IO_BANK0_GPIO12_CTRL (*(volatile uint32_t*)0x40014064)
#define IO_BANK0_GPIO13_STATUS (*(volatile uint32_t*)0x40014068)
#define IO_BANK0_GPIO13_CTRL (*(volatile uint32_t*)0x4001406C)
#define IO_BANK0_GPIO14_STATUS (*(volatile uint32_t*)0x40014070)
#define IO_BANK0_GPIO14_CTRL (*(volatile uint32_t*)0x40014074)
#define IO_BANK0_GPIO15_STATUS (*(volatile uint32_t*)0x40014078)
#define IO_BANK0_GPIO15_CTRL (*(volatile uint32_t*)0x4001407C)
#define IO_BANK0_GPIO16_STATUS (*(volatile uint32_t*)0x40014080)
#define IO_BANK0_GPIO16_CTRL (*(volatile uint32_t*)0x40014084)
#define IO_BANK0_GPIO17_STATUS (*(volatile uint32_t*)0x40014088)
#define IO_BANK0_GPIO17_CTRL (*(volatile uint32_t*)0x4001408C)
#define IO_BANK0_GPIO18_STATUS (*(volatile uint32_t*)0x40014090)
#define IO_BANK0_GPIO18_CTRL (*(volatile uint32_t*)0x40014094)
#define IO_BANK0_GPIO19_STATUS (*(volatile uint32_t*)0x40014098)
#define IO_BANK0_GPIO19_CTRL (*(volatile uint32_t*)0x4001409C)
#define IO_BANK0_GPIO20_STATUS (*(volatile uint32_t*)0x400140A0)
#define IO_BANK0_GPIO20_CTRL (*(volatile uint32_t*)0x400140A4)
#define IO_BANK0_GPIO21_STATUS (*(volatile uint32_t*)0x400140A8)
#define IO_BANK0_GPIO21_CTRL (*(volatile uint32_t*)0x400140AC)
#define IO_BANK0_GPIO22_STATUS (*(volatile uint32_t*)0x400140B0)
#define IO_BANK0_GPIO22_CTRL (*(volatile uint32_t*)0x400140B4)
#define IO_BANK0_GPIO23_STATUS (*(volatile uint32_t*)0x400140B8)
#define IO_BANK0_GPIO23_CTRL (*(volatile uint32_t*)0x400140BC)
#define IO_BANK0_GPIO24_STATUS (*(volatile uint32_t*)0x400140C0)
#define IO_BANK0_GPIO24_CTRL (*(volatile uint32_t*)0x400140C4)
#define IO_BANK0_GPIO25_STATUS (*(volatile uint32_t*)0x400140C8)
#define IO_BANK0_GPIO25_CTRL (*(volatile uint32_t*)0x400140CC)
#define IO_BANK0_GPIO26_STATUS (*(volatile uint32_t*)0x400140D0)
#define IO_BANK0_GPIO26_CTRL (*(volatile uint32_t*)0x400140D4)
#define IO_BANK0_GPIO27_STATUS (*(volatile uint32_t*)0x400140D8)
#define IO_BANK0_GPIO27_CTRL (*(volatile uint32_t*)0x400140DC)
#define IO_BANK0_GPIO28_STATUS (*(volatile uint32_t*)0x400140E0)
#define IO_BANK0_GPIO28_CTRL (*(volatile uint32_t*)0x400140E4)
#define IO_BANK0_GPIO29_STATUS (*(volatile uint32_t*)0x400140E8)
#define IO_BANK0_GPIO29_CTRL (*(volatile uint32_t*)0x400140EC)
#define IO_BANK0_INTR (*(volatile uint32_t*)0x400140F0)
#define IO_BANK0_PROC0_INTE (*(volatile uint32_t*)0x400140F4)
#define IO_BANK0_PROC1_INTE (*(volatile uint32_t*)0x400140F8)
#define IO_BANK0_PROC0_INTF (*(volatile uint32_t*)0x400140FC)
#define IO_BANK0_PROC1_INTF (*(volatile uint32_t*)0x40014100)
#define IO_BANK0_PROC0_INTS (*(volatile uint32_t*)0x40014104)
#define IO_BANK0_PROC1_INTS (*(volatile uint32_t*)0x40014108)
#define IO_BANK0_DORMANT_WAKE_INTE (*(volatile uint32_t*)0x4001410C)
#define IO_BANK0_DORMANT_WAKE_INTF (*(volatile uint32_t*)0x40014110)
#define IO_BANK0_DORMANT_WAKE_INTS (*(volatile uint32_t*)0x40014114)

// Pads
#define PADS_BASE 0x4001E000
#define PADS_GPIO_VOLT (*(volatile uint32_t*)0x4001E0E0)

// SIO (Single-cycle I/O)
#define SIO_BASE 0xD0000000
#define SIO_CPUID (*(volatile uint32_t*)0xD0000000)
#define SIO_GPIO_OUT (*(volatile uint32_t*)0xD0000004)
#define SIO_GPIO_OUT_SET (*(volatile uint32_t*)0xD0000008)
#define SIO_GPIO_OUT_CLR (*(volatile uint32_t*)0xD000000C)
#define SIO_GPIO_OUT_XOR (*(volatile uint32_t*)0xD0000010)
#define SIO_GPIO_OE (*(volatile uint32_t*)0xD0000014)
#define SIO_GPIO_OE_SET (*(volatile uint32_t*)0xD0000018)
#define SIO_GPIO_OE_CLR (*(volatile uint32_t*)0xD000001C)
#define SIO_GPIO_OE_XOR (*(volatile uint32_t*)0xD0000020)
#define SIO_GPIO_IN (*(volatile uint32_t*)0xD0000024)
#define SIO_FIFO_ST (*(volatile uint32_t*)0xD0000040)
#define SIO_FIFO_WR (*(volatile uint32_t*)0xD0000044)
#define SIO_FIFO_RD (*(volatile uint32_t*)0xD0000048)
#define SIO_SPINLOCK_ST (*(volatile uint32_t*)0xD000004C)
#define SIO_INTERRUPT_ST (*(volatile uint32_t*)0xD0000050)

// UART0
#define UART0_BASE 0x40034000
#define UART0_UARTDR (*(volatile uint32_t*)0x40034000)
#define UART0_UARTRSR (*(volatile uint32_t*)0x40034004)
#define UART0_UARTECR (*(volatile uint32_t*)0x40034004)
#define UART0_UARTFR (*(volatile uint32_t*)0x40034018)
#define UART0_UARTILPR (*(volatile uint32_t*)0x40034020)
#define UART0_UARTIBRD (*(volatile uint32_t*)0x40034024)
#define UART0_UARTFBRD (*(volatile uint32_t*)0x40034028)
#define UART0_UARTLCR_H (*(volatile uint32_t*)0x4003402C)
#define UART0_UARTCR (*(volatile uint32_t*)0x40034030)
#define UART0_UARTIFLS (*(volatile uint32_t*)0x40034034)
#define UART0_UARTIMSC (*(volatile uint32_t*)0x40034038)
#define UART0_UARTRIS (*(volatile uint32_t*)0x4003403C)
#define UART0_UARTMIS (*(volatile uint32_t*)0x40034040)
#define UART0_UARTICR (*(volatile uint32_t*)0x40034044)
#define UART0_UARTDMACR (*(volatile uint32_t*)0x40034048)

// UART1
#define UART1_BASE 0x40038000
#define UART1_UARTDR (*(volatile uint32_t*)0x40038000)
#define UART1_UARTRSR (*(volatile uint32_t*)0x40038004)
#define UART1_UARTFR (*(volatile uint32_t*)0x40038018)
#define UART1_UARTIBRD (*(volatile uint32_t*)0x40038024)
#define UART1_UARTFBRD (*(volatile uint32_t*)0x40038028)
#define UART1_UARTLCR_H (*(volatile uint32_t*)0x4003802C)
#define UART1_UARTCR (*(volatile uint32_t*)0x40038030)
#define UART1_UARTIFLS (*(volatile uint32_t*)0x40038034)
#define UART1_UARTIMSC (*(volatile uint32_t*)0x40038038)
#define UART1_UARTICR (*(volatile uint32_t*)0x40038044)

// SPI0
#define SPI0_BASE 0x4003C000
#define SPI0_SSPCR0 (*(volatile uint32_t*)0x4003C000)
#define SPI0_SSPCR1 (*(volatile uint32_t*)0x4003C004)
#define SPI0_SSPDR (*(volatile uint32_t*)0x4003C008)
#define SPI0_SSPSR (*(volatile uint32_t*)0x4003C00C)
#define SPI0_SSPCPSR (*(volatile uint32_t*)0x4003C010)
#define SPI0_SSPIMSC (*(volatile uint32_t*)0x4003C014)
#define SPI0_SSPRIS (*(volatile uint32_t*)0x4003C018)
#define SPI0_SSPMIS (*(volatile uint32_t*)0x4003C01C)
#define SPI0_SSPICR (*(volatile uint32_t*)0x4003C020)
#define SPI0_SSPDMACR (*(volatile uint32_t*)0x4003C024)

// SPI1
#define SPI1_BASE 0x4003C000
#define SPI1_SSPCR0 (*(volatile uint32_t*)0x4003C000)
#define SPI1_SSPCR1 (*(volatile uint32_t*)0x4003C004)
#define SPI1_SSPDR (*(volatile uint32_t*)0x4003C008)
#define SPI1_SSPSR (*(volatile uint32_t*)0x4003C00C)
#define SPI1_SSPCPSR (*(volatile uint32_t*)0x4003C010)
#define SPI1_SSPIMSC (*(volatile uint32_t*)0x4003C014)

// I2C0
#define I2C0_BASE 0x40044000
#define I2C0_IC_CON (*(volatile uint32_t*)0x40044000)
#define I2C0_IC_TAR (*(volatile uint32_t*)0x40044004)
#define I2C0_IC_SAR (*(volatile uint32_t*)0x40044008)
#define I2C0_IC_DATA_CMD (*(volatile uint32_t*)0x40044010)
#define I2C0_IC_SS_SCL_HCNT (*(volatile uint32_t*)0x40044014)
#define I2C0_IC_SS_SCL_LCNT (*(volatile uint32_t*)0x40044018)
#define I2C0_IC_FS_SCL_HCNT (*(volatile uint32_t*)0x4004401C)
#define I2C0_IC_FS_SCL_LCNT (*(volatile uint32_t*)0x40044020)
#define I2C0_IC_RAW_INTR_STAT (*(volatile uint32_t*)0x40044024)
#define I2C0_IC_ENABLE (*(volatile uint32_t*)0x4004402C)
#define I2C0_IC_STATUS (*(volatile uint32_t*)0x40044030)
#define I2C0_IC_TXFLR (*(volatile uint32_t*)0x40044034)
#define I2C0_IC_RXFLR (*(volatile uint32_t*)0x40044038)
#define I2C0_IC_TX_ABRT (*(volatile uint32_t*)0x4004403C)
#define I2C0_IC_DMA_CR (*(volatile uint32_t*)0x40044040)
#define I2C0_IC_DMA_TDLR (*(volatile uint32_t*)0x40044044)
#define I2C0_IC_DMA_RDLR (*(volatile uint32_t*)0x40044048)

// I2C1
#define I2C1_BASE 0x40048000
#define I2C1_IC_CON (*(volatile uint32_t*)0x40048000)
#define I2C1_IC_TAR (*(volatile uint32_t*)0x40048004)
#define I2C1_IC_ENABLE (*(volatile uint32_t*)0x4004802C)
#define I2C1_IC_STATUS (*(volatile uint32_t*)0x40048030)
#define I2C1_IC_TX_ABRT (*(volatile uint32_t*)0x4004803C)

// PWM0
#define PWM0_BASE 0x40050000
#define PWM0_CS (*(volatile uint32_t*)0x40050000)
#define PWM0_CMPR0 (*(volatile uint32_t*)0x40050004)
#define PWM0_CMPR1 (*(volatile uint32_t*)0x40050008)
#define PWM0_CMPR2 (*(volatile uint32_t*)0x4005000C)
#define PWM0_CMPR3 (*(volatile uint32_t*)0x40050010)
#define PWM0_CC (*(volatile uint32_t*)0x40050014)
#define PWM0_TOP (*(volatile uint32_t*)0x40050018)
#define PWM0_INTR (*(volatile uint32_t*)0x4005001C)
#define PWM0_INTE (*(volatile uint32_t*)0x40050020)
#define PWM0_INTF (*(volatile uint32_t*)0x40050024)
#define PWM0_INTS (*(volatile uint32_t*)0x40050028)
#define PWM0_PHS0 (*(volatile uint32_t*)0x40050034)
#define PWM0_PHS1 (*(volatile uint32_t*)0x40050038)
#define PWM0_PHS2 (*(volatile uint32_t*)0x4005003C)
#define PWM0_PHS3 (*(volatile uint32_t*)0x40050040)
#define PWM0_DIV (*(volatile uint32_t*)0x40050044)
#define PWM0_PHASE (*(volatile uint32_t*)0x40050048)

// PWM1
#define PWM1_BASE 0x40051000
#define PWM1_CS (*(volatile uint32_t*)0x40051000)
#define PWM1_CMPR0 (*(volatile uint32_t*)0x40051004)
#define PWM1_CMPR1 (*(volatile uint32_t*)0x40051008)
#define PWM1_CMPR2 (*(volatile uint32_t*)0x4005100C)
#define PWM1_CMPR3 (*(volatile uint32_t*)0x40051010)
#define PWM1_CC (*(volatile uint32_t*)0x40051014)
#define PWM1_TOP (*(volatile uint32_t*)0x40051018)
#define PWM1_DIV (*(volatile uint32_t*)0x40051044)

// ADC
#define ADC_BASE 0x4004C000
#define ADC_ADC_CS (*(volatile uint32_t*)0x4004C000)
#define ADC_ADC_RESULT (*(volatile uint32_t*)0x4004C004)
#define ADC_ADC_FCS (*(volatile uint32_t*)0x4004C008)
#define ADC_ADC_FIFO (*(volatile uint32_t*)0x4004C00C)
#define ADC_ADC_TS (*(volatile uint32_t*)0x4004C010)
#define ADC_ADC_OFFSET (*(volatile uint32_t*)0x4004C014)
#define ADC_ADC_TRIG (*(volatile uint32_t*)0x4004C018)

// Timer0
#define TIMER0_BASE 0x40054000
#define TIMER0_TIMEHW (*(volatile uint32_t*)0x40054000)
#define TIMER0_TIMELW (*(volatile uint32_t*)0x40054004)
#define TIMER0_TIMEHA (*(volatile uint32_t*)0x40054008)
#define TIMER0_TIMELA (*(volatile uint32_t*)0x4005400C)
#define TIMER0_TIMERA (*(volatile uint32_t*)0x40054010)
#define TIMER0_TIMERIQ (*(volatile uint32_t*)0x40054014)
#define TIMER0_TIMEREAD (*(volatile uint32_t*)0x40054018)

// Timer1
#define TIMER1_BASE 0x40058000
#define TIMER1_TIMEHW (*(volatile uint32_t*)0x40058000)
#define TIMER1_TIMELW (*(volatile uint32_t*)0x40058004)
#define TIMER1_TIMEHA (*(volatile uint32_t*)0x40058008)
#define TIMER1_TIMELA (*(volatile uint32_t*)0x4005800C)
#define TIMER1_TIMERA (*(volatile uint32_t*)0x40058010)

// RTC
#define RTC_BASE 0x4005C000
#define RTC_RTC_CLKS (*(volatile uint32_t*)0x4005C000)
#define RTC_RTC_SET (*(volatile uint32_t*)0x4005C004)
#define RTC_RTC_WR (*(volatile uint32_t*)0x4005C008)
#define RTC_RTC_DATE (*(volatile uint32_t*)0x4005C00C)
#define RTC_RTC_TOTAL (*(volatile uint32_t*)0x4005C010)
#define RTC_RTC_HASH (*(volatile uint32_t*)0x4005C014)
#define RTC_RTC_RTC (*(volatile uint32_t*)0x4005C018)
#define RTC_INTR (*(volatile uint32_t*)0x4005C01C)
#define RTC_INTE (*(volatile uint32_t*)0x4005C020)
#define RTC_INTF (*(volatile uint32_t*)0x4005C024)
#define RTC_INTS (*(volatile uint32_t*)0x4005C028)

// Watchdog
#define WATCHDOG_BASE 0x40060000
#define WATCHDOG_WATCHDOG_CTL (*(volatile uint32_t*)0x40060000)
#define WATCHDOG_WATCHDOG_MOD (*(volatile uint32_t*)0x40060004)
#define WATCHDOG_WATCHDOG_FR (*(volatile uint32_t*)0x40060008)
#define WATCHDOG_WATCHDOG_LOAD (*(volatile uint32_t*)0x4006000C)

// USB
#define USB_BASE 0x50100000
#define USB_USB_CTRL (*(volatile uint32_t*)0x50100000)
#define USB_USB_ADDR (*(volatile uint32_t*)0x50100004)
#define USB_USB_PWR (*(volatile uint32_t*)0x50100008)
#define USB_USB_TXFIFO (*(volatile uint32_t*)0x50100010)
#define USB_USB_RXFIFO (*(volatile uint32_t*)0x50100014)
#define USB_USB_TXIE (*(volatile uint32_t*)0x50100018)
#define USB_USB_RXIE (*(volatile uint32_t*)0x5010001C)
#define USB_USB_IS (*(volatile uint32_t*)0x50100020)
#define USB_USB_IM (*(volatile uint32_t*)0x50100024)
#define USB_USB_IE (*(volatile uint32_t*)0x50100028)
#define USB_USB_REVO (*(volatile uint32_t*)0x5010002C)
#define USB_USB_EP (*(volatile uint32_t*)0x50100030)
#define USB_USB_BUFF (*(volatile uint32_t*)0x50100034)
#define USB_USB_MPS (*(volatile uint32_t*)0x50100038)

// PIO0
#define PIO0_BASE 0x50200000
#define PIO0_CTRL (*(volatile uint32_t*)0x50200000)
#define PIO0_FSTAT (*(volatile uint32_t*)0x50200004)
#define PIO0_FDEBUG (*(volatile uint32_t*)0x50200008)
#define PIO0_FCTRL (*(volatile uint32_t*)0x5020000C)
#define PIO0_RXF0 (*(volatile uint32_t*)0x50200010)
#define PIO0_RXF1 (*(volatile uint32_t*)0x50200014)
#define PIO0_RXF2 (*(volatile uint32_t*)0x50200018)
#define PIO0_RXF3 (*(volatile uint32_t*)0x5020001C)
#define PIO0_TXF0 (*(volatile uint32_t*)0x50200020)
#define PIO0_TXF1 (*(volatile uint32_t*)0x50200024)
#define PIO0_TXF2 (*(volatile uint32_t*)0x50200028)
#define PIO0_TXF3 (*(volatile uint32_t*)0x5020002C)
#define PIO0_IRQ (*(volatile uint32_t*)0x50200030)
#define PIO0_IRQ_FORCE (*(volatile uint32_t*)0x50200034)
#define PIO0_IRQ_INTF (*(volatile uint32_t*)0x50200038)
#define PIO0_IRQ_INTS (*(volatile uint32_t*)0x5020003C)
#define PIO0_SM0_CLKDIV (*(volatile uint32_t*)0x502000C8)
#define PIO0_SM0_EXECCTRL (*(volatile uint32_t*)0x502000CC)
#define PIO0_SM0_SHIFTCTRL (*(volatile uint32_t*)0x502000D0)
#define PIO0_SM0_ADDR (*(volatile uint32_t*)0x502000D4)
#define PIO0_SM0_INSTR (*(volatile uint32_t*)0x502000D8)
#define PIO0_SM0_PINCTRL (*(volatile uint32_t*)0x502000DC)

// PIO1
#define PIO1_BASE 0x50201000
#define PIO1_CTRL (*(volatile uint32_t*)0x50201000)
#define PIO1_FSTAT (*(volatile uint32_t*)0x50201004)
#define PIO1_IRQ (*(volatile uint32_t*)0x50201030)
#define PIO1_SM0_CLKDIV (*(volatile uint32_t*)0x502010C8)
#define PIO1_SM0_EXECCTRL (*(volatile uint32_t*)0x502010CC)
#define PIO1_SM0_SHIFTCTRL (*(volatile uint32_t*)0x502010D0)
#define PIO1_SM0_ADDR (*(volatile uint32_t*)0x502010D4)
#define PIO1_SM0_INSTR (*(volatile uint32_t*)0x502010D8)

// Clock Manager
#define CLOCKS_BASE 0x40008000
#define CLOCKS_CLK_GP0DIV (*(volatile uint32_t*)0x40008000)
#define CLOCKS_CLK_GP0CTRL (*(volatile uint32_t*)0x40008004)
#define CLOCKS_CLK_GP1DIV (*(volatile uint32_t*)0x40008008)
#define CLOCKS_CLK_GP1CTRL (*(volatile uint32_t*)0x4000800C)
#define CLOCKS_CLK_GP2DIV (*(volatile uint32_t*)0x40008010)
#define CLOCKS_CLK_GP2CTRL (*(volatile uint32_t*)0x40008014)
#define CLOCKS_CLK_REF (*(volatile uint32_t*)0x4000801C)
#define CLOCKS_CLK_SYS (*(volatile uint32_t*)0x40008020)
#define CLOCKS_CLK_PERI (*(volatile uint32_t*)0x40008024)

// Crystal Oscillator
#define XOSC_BASE 0x40020000
#define XOSC_XOSC_CTRL (*(volatile uint32_t*)0x40020000)
#define XOSC_XOSC_STATUS (*(volatile uint32_t*)0x40020004)
#define XOSC_XOSC_COUNT (*(volatile uint32_t*)0x40020008)

// Ring Oscillator
#define ROSC_BASE 0x40010000
#define ROSC_ROSC_CTRL (*(volatile uint32_t*)0x40010000)
#define ROSC_ROSC_FREQA (*(volatile uint32_t*)0x40010004)
#define ROSC_ROSC_FREQB (*(volatile uint32_t*)0x40010008)
#define ROSC_ROSC_FREQC (*(volatile uint32_t*)0x4001000C)
#define ROSC_ROSC_FREQD (*(volatile uint32_t*)0x40010010)
#define ROSC_ROSC_STATUS (*(volatile uint32_t*)0x40010014)
#define ROSC_ROSC_DR (*(volatile uint32_t*)0x40010018)

// System PLL
#define PLL_SYS_BASE 0x40028000
#define PLL_SYS_PLL_CS (*(volatile uint32_t*)0x40028000)
#define PLL_SYS_PLL_PWR (*(volatile uint32_t*)0x40028004)
#define PLL_SYS_PLL_FBDIV (*(volatile uint32_t*)0x40028008)
#define PLL_SYS_PLL_PRIMARY (*(volatile uint32_t*)0x4002800C)
#define PLL_SYS_PLL_POSTDIV1 (*(volatile uint32_t*)0x40028010)
#define PLL_SYS_PLL_POSTDIV2 (*(volatile uint32_t*)0x40028014)

// USB PLL
#define PLL_USB_BASE 0x4002C000
#define PLL_USB_PLL_CS (*(volatile uint32_t*)0x4002C000)
#define PLL_USB_PLL_PWR (*(volatile uint32_t*)0x4002C004)
#define PLL_USB_PLL_FBDIV (*(volatile uint32_t*)0x4002C008)
#define PLL_USB_PLL_PRIMARY (*(volatile uint32_t*)0x4002C00C)

// Resets
#define RESETS_BASE 0x4000C000
#define RESETS_RESET (*(volatile uint32_t*)0x4000C000)
#define RESETS_RESET_DONE (*(volatile uint32_t*)0x4000C004)
#define RESETS_WD_RESET (*(volatile uint32_t*)0x4000C008)

// 中断向量定义
#define RESERVED_VECTOR 0  // Reserved
#define TIMER0_IRQ_0_VECTOR 1  // Timer 0 IRQ 0
#define TIMER0_IRQ_1_VECTOR 2  // Timer 0 IRQ 1
#define TIMER1_IRQ_0_VECTOR 3  // Timer 1 IRQ 0
#define TIMER1_IRQ_1_VECTOR 4  // Timer 1 IRQ 1
#define TIMER2_IRQ_0_VECTOR 5  // Timer 2 IRQ 0
#define TIMER2_IRQ_1_VECTOR 6  // Timer 2 IRQ 1
#define TIMER3_IRQ_0_VECTOR 7  // Timer 3 IRQ 0
#define TIMER3_IRQ_1_VECTOR 8  // Timer 3 IRQ 1
#define PWM_IRQ_WRAP_VECTOR 9  // PWM IRQ wrap
#define USB_CTRL_IRQ_VECTOR 10  // USB ctrl IRQ
#define USB_DMA_IRQ_VECTOR 11  // USB dma IRQ
#define USB_VBUS_DETECT_VECTOR 12  // USB VBUS detect IRQ
#define USB_RESUME_IRQ_VECTOR 13  // USB resume IRQ
#define ADC_IRQ_FIFO_VECTOR 14  // ADC IRQ FIFO
#define ADC_IRQ_TRIGGER_VECTOR 15  // ADC IRQ trigger
#define I2C0_IRQ_VECTOR 16  // I2C 0 IRQ
#define I2C1_IRQ_VECTOR 17  // I2C 1 IRQ
#define SPI0_IRQ_VECTOR 18  // SPI 0 IRQ
#define SPI1_IRQ_VECTOR 19  // SPI 1 IRQ
#define UART0_IRQ_VECTOR 20  // UART 0 IRQ
#define UART0_IRQ_TX_VECTOR 21  // UART 0 IRQ TX
#define UART1_IRQ_VECTOR 22  // UART 1 IRQ
#define UART1_IRQ_TX_VECTOR 23  // UART 1 IRQ TX
#define PIO0_IRQ_0_VECTOR 24  // PIO 0 IRQ 0
#define PIO0_IRQ_1_VECTOR 25  // PIO 0 IRQ 1
#define PIO1_IRQ_0_VECTOR 26  // PIO 1 IRQ 0
#define PIO1_IRQ_1_VECTOR 27  // PIO 1 IRQ 1
#define RTC_IRQ_VECTOR 28  // RTC IRQ

// 引脚定义
#define PIN_GP0 1  // UART0 TX / GP0
#define PIN_GP1 2  // UART0 RX / GP1
#define PIN_GP2 3  // SPI0 TX / GP2
#define PIN_GP3 4  // SPI0 RX / GP3
#define PIN_GP4 5  // SPI0 CSn / GP4
#define PIN_GP5 6  // SPI0 SCK / GP5
#define PIN_GP6 7  // PWM6 / GP6
#define PIN_GP7 8  // PWM7 / GP7
#define PIN_GP8 9  // PWM8 / GP8
#define PIN_GP9 10  // PWM9 / GP9
#define PIN_GP10 11  // SPI1 TX / GP10
#define PIN_GP11 12  // SPI1 RX / GP11
#define PIN_GP12 13  // SPI1 CSn / GP12
#define PIN_GP13 14  // SPI1 SCK / GP13
#define PIN_GP14 15  // PWM14 / GP14
#define PIN_GP15 16  // PWM15 / GP15
#define PIN_GP16 17  // UART1 TX / GP16
#define PIN_GP17 18  // UART1 RX / GP17
#define PIN_GP18 19  // I2C0 SDA / GP18
#define PIN_GP19 20  // I2C0 SCL / GP19
#define PIN_GP20 21  // I2C1 SDA / GP20
#define PIN_GP21 22  // I2C1 SCL / GP21
#define PIN_GP22 23  // GP22
#define PIN_RUN 24  // Run enable
#define PIN_AGND 25  // Analog ground
#define PIN_GP26 26  // ADC0 / GP26
#define PIN_GP27 27  // ADC1 / GP27
#define PIN_GP28 28  // ADC2 / GP28
#define PIN_ADC_VREF 29  // ADC voltage reference
#define PIN_GP35 30  // GP35
#define PIN_GP34 31  // GP34
#define PIN_GP33 32  // GP33
#define PIN_GP36 37  // GP36
#define PIN_GP37 38  // GP37
#define PIN_GP38 39  // GP38
#define PIN_GP39 40  // GP39
#define PIN_GP40 41  // GP40
#define PIN_GP41 42  // GP41
#define PIN_SWCLK 43  // SWD Clock
#define PIN_SWDIO 44  // SWD Data I/O

void rp2040_init(void);

#ifdef __cplusplus
}
#endif

#endif // RP2040_HPP
