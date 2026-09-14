//
// LPC1768 Register Definitions
// Generated from: ARM Cortex-M3 up to 100MHz with 512KB Flash, 64KB SRAM
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - GPIO (GPIO)
let GPIO_FIODIR: UInt32 = 0x0x0000
let GPIO_FIOMASK: UInt32 = 0x0x0004
let GPIO_FIOPIN: UInt32 = 0x0x0008
let GPIO_FIOSET: UInt32 = 0x0x000C
let GPIO_FIOCLR: UInt32 = 0x0x0010
let GPIO_P0: UInt32 = 0x0x0014
let GPIO_P1: UInt32 = 0x0x0018
let GPIO_P2: UInt32 = 0x0x001C
let GPIO_P3: UInt32 = 0x0x0020
let GPIO_P4: UInt32 = 0x0x0024

// MARK: - UART0 (UART0)
let UART0_RBR: UInt32 = 0x0x0000
let UART0_THR: UInt32 = 0x0x0000
let UART0_DLL: UInt32 = 0x0x0000
let UART0_DLM: UInt32 = 0x0x0004
let UART0_IER: UInt32 = 0x0x0004
let UART0_IIR: UInt32 = 0x0x0008
let UART0_FCR: UInt32 = 0x0x0008
let UART0_LCR: UInt32 = 0x0x000C
let UART0_LSR: UInt32 = 0x0x0014
let UART0_SCR: UInt32 = 0x0x001C
let UART0_ACR: UInt32 = 0x0x0020
let UART0_ICR: UInt32 = 0x0x0024
let UART0_FDR: UInt32 = 0x0x0028
let UART0_TER: UInt32 = 0x0x0030

// MARK: - UART1 (UART1)
let UART1_RBR: UInt32 = 0x0x0000
let UART1_THR: UInt32 = 0x0x0000
let UART1_DLL: UInt32 = 0x0x0000
let UART1_DLM: UInt32 = 0x0x0004
let UART1_IER: UInt32 = 0x0x0004
let UART1_IIR: UInt32 = 0x0x0008
let UART1_LCR: UInt32 = 0x0x000C
let UART1_LSR: UInt32 = 0x0x0014
let UART1_SCR: UInt32 = 0x0x001C
let UART1_MSR: UInt32 = 0x0x0020
let UART1_SCR: UInt32 = 0x0x0024

// MARK: - UART2 (UART2)
let UART2_RBR: UInt32 = 0x0x0000
let UART2_THR: UInt32 = 0x0x0000
let UART2_DLL: UInt32 = 0x0x0000
let UART2_DLM: UInt32 = 0x0x0004
let UART2_IER: UInt32 = 0x0x0004
let UART2_IIR: UInt32 = 0x0x0008
let UART2_LCR: UInt32 = 0x0x000C
let UART2_LSR: UInt32 = 0x0x0014

// MARK: - UART3 (UART3)
let UART3_RBR: UInt32 = 0x0x0000
let UART3_THR: UInt32 = 0x0x0000
let UART3_DLL: UInt32 = 0x0x0000
let UART3_DLM: UInt32 = 0x0x0004
let UART3_IER: UInt32 = 0x0x0004
let UART3_IIR: UInt32 = 0x0x0008
let UART3_LCR: UInt32 = 0x0x000C
let UART3_LSR: UInt32 = 0x0x0014

// MARK: - SPI0 (SPI0)
let SPI0_CR0: UInt32 = 0x0x0000
let SPI0_CR1: UInt32 = 0x0x0004
let SPI0_DR: UInt32 = 0x0x0008
let SPI0_SR: UInt32 = 0x0x000C
let SPI0_CPSR: UInt32 = 0x0x0010
let SPI0_IMSC: UInt32 = 0x0x0014
let SPI0_RIS: UInt32 = 0x0x0018
let SPI0_MIS: UInt32 = 0x0x001C
let SPI0_ICR: UInt32 = 0x0x0020

// MARK: - SPI1 (SPI1)
let SPI1_CR0: UInt32 = 0x0x0000
let SPI1_CR1: UInt32 = 0x0x0004
let SPI1_DR: UInt32 = 0x0x0008
let SPI1_SR: UInt32 = 0x0x000C
let SPI1_CPSR: UInt32 = 0x0x0010

// MARK: - I2C0 (I2C0)
let I2C0_CON: UInt32 = 0x0x0000
let I2C0_TAR: UInt32 = 0x0x0004
let I2C0_DAT: UInt32 = 0x0x0008
let I2C0_SSHC: UInt32 = 0x0x000C
let I2C0_HSH: UInt32 = 0x0x0010
let I2C0_INTX: UInt32 = 0x0x0014
let I2C0_INTM: UInt32 = 0x0x0018
let I2C0_AR: UInt32 = 0x0x001C
let I2C0_SR: UInt32 = 0x0x0020
let I2C0_TXFL: UInt32 = 0x0x0024
let I2C0_RXFL: UInt32 = 0x0x0028
let I2C0_COMP: UInt32 = 0x0x002C
let I2C0_RXFI: UInt32 = 0x0x0030
let I2C0_RXFT: UInt32 = 0x0x0030

// MARK: - I2C1 (I2C1)
let I2C1_CON: UInt32 = 0x0x0000
let I2C1_TAR: UInt32 = 0x0x0004
let I2C1_DAT: UInt32 = 0x0x0008
let I2C1_SR: UInt32 = 0x0x0020

// MARK: - TIMER0 (Timer0)
let TIMER0_IR: UInt32 = 0x0x0000
let TIMER0_TCR: UInt32 = 0x0x0004
let TIMER0_TC: UInt32 = 0x0x0008
let TIMER0_PR: UInt32 = 0x0x000C
let TIMER0_PC: UInt32 = 0x0x0010
let TIMER0_MCR: UInt32 = 0x0x0014
let TIMER0_MR0: UInt32 = 0x0x0018
let TIMER0_MR1: UInt32 = 0x0x001C
let TIMER0_MR2: UInt32 = 0x0x0020
let TIMER0_MR3: UInt32 = 0x0x0024
let TIMER0_CCR: UInt32 = 0x0x0028
let TIMER0_CR0: UInt32 = 0x0x002C
let TIMER0_CR1: UInt32 = 0x0x0030
let TIMER0_CR2: UInt32 = 0x0x0034
let TIMER0_CR3: UInt32 = 0x0x0038
let TIMER0_EMR: UInt32 = 0x0x003C
let TIMER0_CTCR: UInt32 = 0x0x0070
let TIMER0_EW: UInt32 = 0x0x0074

// MARK: - TIMER1 (Timer1)
let TIMER1_IR: UInt32 = 0x0x0000
let TIMER1_TCR: UInt32 = 0x0x0004
let TIMER1_TC: UInt32 = 0x0x0008
let TIMER1_PR: UInt32 = 0x0x000C
let TIMER1_MCR: UInt32 = 0x0x0014
let TIMER1_MR0: UInt32 = 0x0x0018
let TIMER1_MR1: UInt32 = 0x0x001C
let TIMER1_MR2: UInt32 = 0x0x0020
let TIMER1_MR3: UInt32 = 0x0x0024
let TIMER1_CCR: UInt32 = 0x0x0028
let TIMER1_CR0: UInt32 = 0x0x002C
let TIMER1_CR1: UInt32 = 0x0x0030
let TIMER1_EMR: UInt32 = 0x0x003C

// MARK: - TIMER2 (Timer2)
let TIMER2_IR: UInt32 = 0x0x0000
let TIMER2_TCR: UInt32 = 0x0x0004
let TIMER2_TC: UInt32 = 0x0x0008
let TIMER2_PR: UInt32 = 0x0x000C
let TIMER2_MCR: UInt32 = 0x0x0014
let TIMER2_MR0: UInt32 = 0x0x0018
let TIMER2_CCR: UInt32 = 0x0x0028
let TIMER2_CR0: UInt32 = 0x0x002C

// MARK: - TIMER3 (Timer3)
let TIMER3_IR: UInt32 = 0x0x0000
let TIMER3_TCR: UInt32 = 0x0x0004
let TIMER3_TC: UInt32 = 0x0x0008
let TIMER3_PR: UInt32 = 0x0x000C
let TIMER3_MCR: UInt32 = 0x0x0014
let TIMER3_MR0: UInt32 = 0x0x0018
let TIMER3_CCR: UInt32 = 0x0x0028

// MARK: - PWM0 (PWM0)
let PWM0_IR: UInt32 = 0x0x0000
let PWM0_TCR: UInt32 = 0x0x0004
let PWM0_TC: UInt32 = 0x0x0008
let PWM0_PR: UInt32 = 0x0x000C
let PWM0_PC: UInt32 = 0x0x0010
let PWM0_MCR: UInt32 = 0x0x0014
let PWM0_MR0: UInt32 = 0x0x0018
let PWM0_MR1: UInt32 = 0x0x001C
let PWM0_MR2: UInt32 = 0x0x0020
let PWM0_MR3: UInt32 = 0x0x0024
let PWM0_MR4: UInt32 = 0x0x0040
let PWM0_MR5: UInt32 = 0x0x0044
let PWM0_MR6: UInt32 = 0x0x0048
let PWM0_CCR: UInt32 = 0x0x0028
let PWM0_CR0: UInt32 = 0x0x002C
let PWM0_PCR: UInt32 = 0x0x004C
let PWM0_LER: UInt32 = 0x0x0050
let PWM0_CTCR: UInt32 = 0x0x0070

// MARK: - ADC (ADC)
let ADC_CR: UInt32 = 0x0x0000
let ADC_GDR: UInt32 = 0x0x0004
let ADC_INTEN: UInt32 = 0x0x000C
let ADC_STATUS: UInt32 = 0x0x0010
let ADC_TR: UInt32 = 0x0x0014

// MARK: - DAC (DAC)
let DAC_CR: UInt32 = 0x0x0000
let DAC_CTRL: UInt32 = 0x0x0004

// MARK: - ETH (Ethernet)
let ETH_MAC1: UInt32 = 0x0x0000
let ETH_MAC2: UInt32 = 0x0x0004
let ETH_IPGT: UInt32 = 0x0x0008
let ETH_IPGR: UInt32 = 0x0x000C
let ETH_CLRT: UInt32 = 0x0x0010
let ETH_MAXF: UInt32 = 0x0x0014
let ETH_SUPP: UInt32 = 0x0x0018
let ETH_TEST: UInt32 = 0x0x001C
let ETH_MCFG: UInt32 = 0x0x0020
let ETH_MCMD: UInt32 = 0x0x0024
let ETH_MADR: UInt32 = 0x0x0028
let ETH_MWTD: UInt32 = 0x0x002C
let ETH_MRDD: UInt32 = 0x0x0030
let ETH_IND: UInt32 = 0x0x0034

// MARK: - USB (USB Controller)
let USB_HCCHAR: UInt32 = 0x
let USB_HCINT: UInt32 = 0x
let USB_HCINTMSK: UInt32 = 0x
let USB_HCTSIZ: UInt32 = 0x
let USB_HCDMA: UInt32 = 0x
let USB_HCDMAB: UInt32 = 0x
let USB_OTGIntSt: UInt32 = 0x
let USB_OTGIntEn: UInt32 = 0x
let USB_OTGIntSel: UInt32 = 0x

// MARK: - DMA (DMA Controller)
let DMA_IntStat: UInt32 = 0x0x0000
let DMA_IntTCStat: UInt32 = 0x0x0004
let DMA_IntTCClear: UInt32 = 0x0x0008
let DMA_IntErrStat: UInt32 = 0x0x000C
let DMA_IntErrClr: UInt32 = 0x0x0010
let DMA_RawIntStat: UInt32 = 0x0x0014
let DMA_RawIntTCStat: UInt32 = 0x0x0018
let DMA_EnbldChns: UInt32 = 0x0x001C
let DMA_SoftBReq: UInt32 = 0x0x0020
let DMA_SoftSReq: UInt32 = 0x0x0024
let DMA_Config: UInt32 = 0x0x0028
let DMA_Sync: UInt32 = 0x0x002C

// MARK: - WDT (Watchdog Timer)
let WDT_WDMOD: UInt32 = 0x0x0000
let WDT_WDTC: UInt32 = 0x0x0004
let WDT_WDFEED: UInt32 = 0x0x0008
let WDT_WDTV: UInt32 = 0x0x000C

// MARK: - RTC (RTC)
let RTC_ILR: UInt32 = 0x0x0000
let RTC_CCR: UInt32 = 0x0x0004
let RTC_CIIR: UInt32 = 0x0x0008
let RTC_CWR: UInt32 = 0x0x000C
let RTC_PREINT: UInt32 = 0x0x0010
let RTC_PREFRAC: UInt32 = 0x0x0014
let RTC_CRT: UInt32 = 0x0x0018
let RTC_SEC: UInt32 = 0x0x001C
let RTC_MIN: UInt32 = 0x0x0020
let RTC_HOUR: UInt32 = 0x0x0024
let RTC_DOM: UInt32 = 0x0x0028
let RTC_DOW: UInt32 = 0x0x002C
let RTC_DOY: UInt32 = 0x0x0030
let RTC_MONTH: UInt32 = 0x0x0034
let RTC_YEAR: UInt32 = 0x0x0038

// MARK: - SC (System Control)
let SC_PLL0CON: UInt32 = 0x0x0000
let SC_PLL0CFG: UInt32 = 0x0x0004
let SC_PLL0STAT: UInt32 = 0x0x0008
let SC_PLL0FEED: UInt32 = 0x0x000C
let SC_PLL1CON: UInt32 = 0x0x0010
let SC_PLL1CFG: UInt32 = 0x0x0014
let SC_PLL1STAT: UInt32 = 0x0x0018
let SC_PLL1FEED: UInt32 = 0x0x001C
let SC_CCLKCFG: UInt32 = 0x0x0020
let SC_USBCLKCFG: UInt32 = 0x0x0024
let SC_CLKSRC: UInt32 = 0x0x0028
let SC_PCLKSEL0: UInt32 = 0x0x002C
let SC_PCLKSEL1: UInt32 = 0x0x0030
let SC_BOSC: UInt32 = 0x0x0050
let SC_EXTINT: UInt32 = 0x0x0054
let SC_EXTMODE: UInt32 = 0x0x0058
let SC_EXTPOL: UInt32 = 0x0x005C

// MARK: - PinConnectBlock (Pin Connect Block)
let PinConnectBlock_PINSEL0: UInt32 = 0x0x0000
let PinConnectBlock_PINSEL1: UInt32 = 0x0x0004
let PinConnectBlock_PINSEL2: UInt32 = 0x0x0008
let PinConnectBlock_PINSEL3: UInt32 = 0x0x000C
let PinConnectBlock_PINSEL4: UInt32 = 0x0x0010
let PinConnectBlock_PINSEL5: UInt32 = 0x0x0014
let PinConnectBlock_PINSEL6: UInt32 = 0x0x0018
let PinConnectBlock_PINSEL7: UInt32 = 0x0x001C
let PinConnectBlock_PINSEL8: UInt32 = 0x0x0020
let PinConnectBlock_PINSEL9: UInt32 = 0x0x0024
let PinConnectBlock_PINMODE0: UInt32 = 0x0x0040
let PinConnectBlock_PINMODE1: UInt32 = 0x0x0044
let PinConnectBlock_PINMODE2: UInt32 = 0x0x0048
let PinConnectBlock_PINMODE3: UInt32 = 0x0x004C
let PinConnectBlock_PINMODE4: UInt32 = 0x0x0050
let PinConnectBlock_PINMODE5: UInt32 = 0x0x0054
let PinConnectBlock_PINMODE6: UInt32 = 0x0x0058
let PinConnectBlock_PINMODE7: UInt32 = 0x0x005C
let PinConnectBlock_PINMODE8: UInt32 = 0x0x0060
let PinConnectBlock_PINMODE9: UInt32 = 0x0x0064
let PinConnectBlock_PINOD0: UInt32 = 0x0x0080
let PinConnectBlock_PINOD1: UInt32 = 0x0x0084
let PinConnectBlock_PINOD2: UInt32 = 0x0x0088
let PinConnectBlock_PINOD3: UInt32 = 0x0x008C

// MARK: - Interrupt Vectors
let IRQ_WDT: Int = 0
let IRQ_RESERVED: Int = 1
let IRQ_DEBUG_MON: Int = 2
let IRQ_RESERVED: Int = 3
let IRQ_TIMER0: Int = 4
let IRQ_TIMER1: Int = 5
let IRQ_PWM0: Int = 6
let IRQ_UART0: Int = 7
let IRQ_UART1: Int = 8
let IRQ_PWM1: Int = 9
let IRQ_I2C0: Int = 10
let IRQ_I2C1: Int = 11
let IRQ_SPI0: Int = 12
let IRQ_SPI1: Int = 13
let IRQ_RTC: Int = 14
let IRQ_EINT0: Int = 15
let IRQ_EINT1: Int = 16
let IRQ_EINT2: Int = 17
let IRQ_EINT3: Int = 18
let IRQ_RESERVED: Int = 19
let IRQ_ADC: Int = 20
let IRQ_BOD: Int = 21
let IRQ_USB: Int = 22
let IRQ_CAN: Int = 23
let IRQ_GP: Int = 24
let IRQ_I2S: Int = 25
let IRQ_ETHERNET: Int = 26
let IRQ_RIT: Int = 27
let IRQ_QM: Int = 28
let IRQ_RESERVED: Int = 29
let IRQ_RESERVED: Int = 30

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x00000000, 524288)
let MEM_flash_boot: (start: UInt32, size: UInt32) = (0x0x00080000, 32768)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x10000000, 65536)
let MEM_ahb1: (start: UInt32, size: UInt32) = (0x0x20000000, 1048576)
let MEM_apb0: (start: UInt32, size: UInt32) = (0x0x40000000, 1048576)
let MEM_apb1: (start: UInt32, size: UInt32) = (0x0x50000000, 1048576)

// MARK: - Device Functions
func lpc1768_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
