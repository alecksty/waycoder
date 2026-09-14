using System;

namespace VML.Device.NXP.LPC1768
{
    /// <summary>
    /// LPC1768 寄存器定义
    /// 生成自: NXP/LPC17xx/LPC1768
    /// 版本: 1.0
    /// </summary>
    public static class LPC1768
    {
        // CPU架构: ARM-Cortex-M3, 32位, 12000000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00000000;
        public static unsafe uint* R0 => (uint*)0x00000000;

        // General Purpose Register 1
        public const int R1_ADDR = 0x00000004;
        public static unsafe uint* R1 => (uint*)0x00000004;

        // General Purpose Register 2
        public const int R2_ADDR = 0x00000008;
        public static unsafe uint* R2 => (uint*)0x00000008;

        // General Purpose Register 3
        public const int R3_ADDR = 0x0000000C;
        public static unsafe uint* R3 => (uint*)0x0000000C;

        // General Purpose Register 4
        public const int R4_ADDR = 0x00000010;
        public static unsafe uint* R4 => (uint*)0x00000010;

        // General Purpose Register 5
        public const int R5_ADDR = 0x00000014;
        public static unsafe uint* R5 => (uint*)0x00000014;

        // General Purpose Register 6
        public const int R6_ADDR = 0x00000018;
        public static unsafe uint* R6 => (uint*)0x00000018;

        // General Purpose Register 7
        public const int R7_ADDR = 0x0000001C;
        public static unsafe uint* R7 => (uint*)0x0000001C;

        // General Purpose Register 8
        public const int R8_ADDR = 0x00000020;
        public static unsafe uint* R8 => (uint*)0x00000020;

        // General Purpose Register 9
        public const int R9_ADDR = 0x00000024;
        public static unsafe uint* R9 => (uint*)0x00000024;

        // General Purpose Register 10
        public const int R10_ADDR = 0x00000028;
        public static unsafe uint* R10 => (uint*)0x00000028;

        // General Purpose Register 11
        public const int R11_ADDR = 0x0000002C;
        public static unsafe uint* R11 => (uint*)0x0000002C;

        // General Purpose Register 12
        public const int R12_ADDR = 0x00000030;
        public static unsafe uint* R12 => (uint*)0x00000030;

        // Stack Pointer
        public const int SP_ADDR = 0x00000034;
        public static unsafe uint* SP => (uint*)0x00000034;

        // Link Register
        public const int LR_ADDR = 0x00000038;
        public static unsafe uint* LR => (uint*)0x00000038;

        // Program Counter
        public const int PC_ADDR = 0x0000003C;
        public static unsafe uint* PC => (uint*)0x0000003C;

        // Program Status Register
        public const int PSR_ADDR = 0x00000040;
        public static unsafe uint* PSR => (uint*)0x00000040;
        public const int PSR_N = 31;  // Negative Flag
        public const int PSR_Z = 30;  // Zero Flag
        public const int PSR_C = 29;  // Carry Flag
        public const int PSR_V = 28;  // Overflow Flag
        public const int PSR_Q = 27;  // Saturation Flag
        public const int PSR_ICI1 = 0;  // Interrupt Continue State
        public const int PSR_GE = 0;  // Greater than or Equal
        public const int PSR_IT = 0;  // If-Then execution state
        public const int PSR_APSR = 0;  // Application Program Status

        // Priority Mask Register
        public const int PRIMASK_ADDR = 0xE0000E20;
        public static unsafe uint* PRIMASK => (uint*)0xE0000E20;

        // Fault Mask Register
        public const int FAULTMASK_ADDR = 0xE0000E28;
        public static unsafe uint* FAULTMASK => (uint*)0xE0000E28;

        // Base Priority Register
        public const int BASEPRI_ADDR = 0xE0000E24;
        public static unsafe uint* BASEPRI => (uint*)0xE0000E24;

        // Control Register
        public const int CONTROL_ADDR = 0xE0000E2C;
        public static unsafe uint* CONTROL => (uint*)0xE0000E2C;

        // 内存段定义
        // Main Flash (512KB)
        public const int FLASH_START = 0x00000000;
        public const int FLASH_END = 0x0007FFFF;
        public const int FLASH_SIZE = 524288;

        // Boot Flash (32KB)
        public const int FLASH_BOOT_START = 0x00080000;
        public const int FLASH_BOOT_END = 0x0007FFFF;
        public const int FLASH_BOOT_SIZE = 32768;

        // SRAM (64KB)
        public const int SRAM_START = 0x10000000;
        public const int SRAM_END = 0x1000FFFF;
        public const int SRAM_SIZE = 65536;

        // AHB1 Peripherals
        public const int AHB1_START = 0x20000000;
        public const int AHB1_END = 0x200FFFFF;
        public const int AHB1_SIZE = 1048576;

        // APB0 Peripherals
        public const int APB0_START = 0x40000000;
        public const int APB0_END = 0x400FFFFF;
        public const int APB0_SIZE = 1048576;

        // APB1 Peripherals
        public const int APB1_START = 0x50000000;
        public const int APB1_END = 0x500FFFFF;
        public const int APB1_SIZE = 1048576;

        // 外设定义
        // GPIO
        public const int GPIO_BASE = 0x2009C000;
        public static unsafe uint* GPIO_FIODIR => (uint*)0x2009C000;
        public static unsafe uint* GPIO_FIOMASK => (uint*)0x2009C004;
        public static unsafe uint* GPIO_FIOPIN => (uint*)0x2009C008;
        public static unsafe uint* GPIO_FIOSET => (uint*)0x2009C00C;
        public static unsafe uint* GPIO_FIOCLR => (uint*)0x2009C010;
        public static unsafe uint* GPIO_P0 => (uint*)0x2009C014;
        public static unsafe uint* GPIO_P1 => (uint*)0x2009C018;
        public static unsafe uint* GPIO_P2 => (uint*)0x2009C01C;
        public static unsafe uint* GPIO_P3 => (uint*)0x2009C020;
        public static unsafe uint* GPIO_P4 => (uint*)0x2009C024;

        // UART0
        public const int UART0_BASE = 0x4000C000;
        public static unsafe uint* UART0_RBR => (uint*)0x4000C000;
        public static unsafe uint* UART0_THR => (uint*)0x4000C000;
        public static unsafe uint* UART0_DLL => (uint*)0x4000C000;
        public static unsafe uint* UART0_DLM => (uint*)0x4000C004;
        public static unsafe uint* UART0_IER => (uint*)0x4000C004;
        public static unsafe uint* UART0_IIR => (uint*)0x4000C008;
        public static unsafe uint* UART0_FCR => (uint*)0x4000C008;
        public static unsafe uint* UART0_LCR => (uint*)0x4000C00C;
        public static unsafe uint* UART0_LSR => (uint*)0x4000C014;
        public static unsafe uint* UART0_SCR => (uint*)0x4000C01C;
        public static unsafe uint* UART0_ACR => (uint*)0x4000C020;
        public static unsafe uint* UART0_ICR => (uint*)0x4000C024;
        public static unsafe uint* UART0_FDR => (uint*)0x4000C028;
        public static unsafe uint* UART0_TER => (uint*)0x4000C030;

        // UART1
        public const int UART1_BASE = 0x4000D000;
        public static unsafe uint* UART1_RBR => (uint*)0x4000D000;
        public static unsafe uint* UART1_THR => (uint*)0x4000D000;
        public static unsafe uint* UART1_DLL => (uint*)0x4000D000;
        public static unsafe uint* UART1_DLM => (uint*)0x4000D004;
        public static unsafe uint* UART1_IER => (uint*)0x4000D004;
        public static unsafe uint* UART1_IIR => (uint*)0x4000D008;
        public static unsafe uint* UART1_LCR => (uint*)0x4000D00C;
        public static unsafe uint* UART1_LSR => (uint*)0x4000D014;
        public static unsafe uint* UART1_SCR => (uint*)0x4000D01C;
        public static unsafe uint* UART1_MSR => (uint*)0x4000D020;
        public static unsafe uint* UART1_SCR => (uint*)0x4000D024;

        // UART2
        public const int UART2_BASE = 0x40098000;
        public static unsafe uint* UART2_RBR => (uint*)0x40098000;
        public static unsafe uint* UART2_THR => (uint*)0x40098000;
        public static unsafe uint* UART2_DLL => (uint*)0x40098000;
        public static unsafe uint* UART2_DLM => (uint*)0x40098004;
        public static unsafe uint* UART2_IER => (uint*)0x40098004;
        public static unsafe uint* UART2_IIR => (uint*)0x40098008;
        public static unsafe uint* UART2_LCR => (uint*)0x4009800C;
        public static unsafe uint* UART2_LSR => (uint*)0x40098014;

        // UART3
        public const int UART3_BASE = 0x4009C000;
        public static unsafe uint* UART3_RBR => (uint*)0x4009C000;
        public static unsafe uint* UART3_THR => (uint*)0x4009C000;
        public static unsafe uint* UART3_DLL => (uint*)0x4009C000;
        public static unsafe uint* UART3_DLM => (uint*)0x4009C004;
        public static unsafe uint* UART3_IER => (uint*)0x4009C004;
        public static unsafe uint* UART3_IIR => (uint*)0x4009C008;
        public static unsafe uint* UART3_LCR => (uint*)0x4009C00C;
        public static unsafe uint* UART3_LSR => (uint*)0x4009C014;

        // SPI0
        public const int SPI0_BASE = 0x40088000;
        public static unsafe uint* SPI0_CR0 => (uint*)0x40088000;
        public static unsafe uint* SPI0_CR1 => (uint*)0x40088004;
        public static unsafe uint* SPI0_DR => (uint*)0x40088008;
        public static unsafe uint* SPI0_SR => (uint*)0x4008800C;
        public static unsafe uint* SPI0_CPSR => (uint*)0x40088010;
        public static unsafe uint* SPI0_IMSC => (uint*)0x40088014;
        public static unsafe uint* SPI0_RIS => (uint*)0x40088018;
        public static unsafe uint* SPI0_MIS => (uint*)0x4008801C;
        public static unsafe uint* SPI0_ICR => (uint*)0x40088020;

        // SPI1
        public const int SPI1_BASE = 0x4008C000;
        public static unsafe uint* SPI1_CR0 => (uint*)0x4008C000;
        public static unsafe uint* SPI1_CR1 => (uint*)0x4008C004;
        public static unsafe uint* SPI1_DR => (uint*)0x4008C008;
        public static unsafe uint* SPI1_SR => (uint*)0x4008C00C;
        public static unsafe uint* SPI1_CPSR => (uint*)0x4008C010;

        // I2C0
        public const int I2C0_BASE = 0x4001C000;
        public static unsafe uint* I2C0_CON => (uint*)0x4001C000;
        public static unsafe uint* I2C0_TAR => (uint*)0x4001C004;
        public static unsafe uint* I2C0_DAT => (uint*)0x4001C008;
        public static unsafe uint* I2C0_SSHC => (uint*)0x4001C00C;
        public static unsafe uint* I2C0_HSH => (uint*)0x4001C010;
        public static unsafe uint* I2C0_INTX => (uint*)0x4001C014;
        public static unsafe uint* I2C0_INTM => (uint*)0x4001C018;
        public static unsafe uint* I2C0_AR => (uint*)0x4001C01C;
        public static unsafe uint* I2C0_SR => (uint*)0x4001C020;
        public static unsafe uint* I2C0_TXFL => (uint*)0x4001C024;
        public static unsafe uint* I2C0_RXFL => (uint*)0x4001C028;
        public static unsafe uint* I2C0_COMP => (uint*)0x4001C02C;
        public static unsafe uint* I2C0_RXFI => (uint*)0x4001C030;
        public static unsafe uint* I2C0_RXFT => (uint*)0x4001C030;

        // I2C1
        public const int I2C1_BASE = 0x4001C000;
        public static unsafe uint* I2C1_CON => (uint*)0x4001C000;
        public static unsafe uint* I2C1_TAR => (uint*)0x4001C004;
        public static unsafe uint* I2C1_DAT => (uint*)0x4001C008;
        public static unsafe uint* I2C1_SR => (uint*)0x4001C020;

        // Timer0
        public const int TIMER0_BASE = 0x40004000;
        public static unsafe uint* TIMER0_IR => (uint*)0x40004000;
        public static unsafe uint* TIMER0_TCR => (uint*)0x40004004;
        public static unsafe uint* TIMER0_TC => (uint*)0x40004008;
        public static unsafe uint* TIMER0_PR => (uint*)0x4000400C;
        public static unsafe uint* TIMER0_PC => (uint*)0x40004010;
        public static unsafe uint* TIMER0_MCR => (uint*)0x40004014;
        public static unsafe uint* TIMER0_MR0 => (uint*)0x40004018;
        public static unsafe uint* TIMER0_MR1 => (uint*)0x4000401C;
        public static unsafe uint* TIMER0_MR2 => (uint*)0x40004020;
        public static unsafe uint* TIMER0_MR3 => (uint*)0x40004024;
        public static unsafe uint* TIMER0_CCR => (uint*)0x40004028;
        public static unsafe uint* TIMER0_CR0 => (uint*)0x4000402C;
        public static unsafe uint* TIMER0_CR1 => (uint*)0x40004030;
        public static unsafe uint* TIMER0_CR2 => (uint*)0x40004034;
        public static unsafe uint* TIMER0_CR3 => (uint*)0x40004038;
        public static unsafe uint* TIMER0_EMR => (uint*)0x4000403C;
        public static unsafe uint* TIMER0_CTCR => (uint*)0x40004070;
        public static unsafe uint* TIMER0_EW => (uint*)0x40004074;

        // Timer1
        public const int TIMER1_BASE = 0x40008000;
        public static unsafe uint* TIMER1_IR => (uint*)0x40008000;
        public static unsafe uint* TIMER1_TCR => (uint*)0x40008004;
        public static unsafe uint* TIMER1_TC => (uint*)0x40008008;
        public static unsafe uint* TIMER1_PR => (uint*)0x4000800C;
        public static unsafe uint* TIMER1_MCR => (uint*)0x40008014;
        public static unsafe uint* TIMER1_MR0 => (uint*)0x40008018;
        public static unsafe uint* TIMER1_MR1 => (uint*)0x4000801C;
        public static unsafe uint* TIMER1_MR2 => (uint*)0x40008020;
        public static unsafe uint* TIMER1_MR3 => (uint*)0x40008024;
        public static unsafe uint* TIMER1_CCR => (uint*)0x40008028;
        public static unsafe uint* TIMER1_CR0 => (uint*)0x4000802C;
        public static unsafe uint* TIMER1_CR1 => (uint*)0x40008030;
        public static unsafe uint* TIMER1_EMR => (uint*)0x4000803C;

        // Timer2
        public const int TIMER2_BASE = 0x400A4000;
        public static unsafe uint* TIMER2_IR => (uint*)0x400A4000;
        public static unsafe uint* TIMER2_TCR => (uint*)0x400A4004;
        public static unsafe uint* TIMER2_TC => (uint*)0x400A4008;
        public static unsafe uint* TIMER2_PR => (uint*)0x400A400C;
        public static unsafe uint* TIMER2_MCR => (uint*)0x400A4014;
        public static unsafe uint* TIMER2_MR0 => (uint*)0x400A4018;
        public static unsafe uint* TIMER2_CCR => (uint*)0x400A4028;
        public static unsafe uint* TIMER2_CR0 => (uint*)0x400A402C;

        // Timer3
        public const int TIMER3_BASE = 0x400A8000;
        public static unsafe uint* TIMER3_IR => (uint*)0x400A8000;
        public static unsafe uint* TIMER3_TCR => (uint*)0x400A8004;
        public static unsafe uint* TIMER3_TC => (uint*)0x400A8008;
        public static unsafe uint* TIMER3_PR => (uint*)0x400A800C;
        public static unsafe uint* TIMER3_MCR => (uint*)0x400A8014;
        public static unsafe uint* TIMER3_MR0 => (uint*)0x400A8018;
        public static unsafe uint* TIMER3_CCR => (uint*)0x400A8028;

        // PWM0
        public const int PWM0_BASE = 0x40014000;
        public static unsafe uint* PWM0_IR => (uint*)0x40014000;
        public static unsafe uint* PWM0_TCR => (uint*)0x40014004;
        public static unsafe uint* PWM0_TC => (uint*)0x40014008;
        public static unsafe uint* PWM0_PR => (uint*)0x4001400C;
        public static unsafe uint* PWM0_PC => (uint*)0x40014010;
        public static unsafe uint* PWM0_MCR => (uint*)0x40014014;
        public static unsafe uint* PWM0_MR0 => (uint*)0x40014018;
        public static unsafe uint* PWM0_MR1 => (uint*)0x4001401C;
        public static unsafe uint* PWM0_MR2 => (uint*)0x40014020;
        public static unsafe uint* PWM0_MR3 => (uint*)0x40014024;
        public static unsafe uint* PWM0_MR4 => (uint*)0x40014040;
        public static unsafe uint* PWM0_MR5 => (uint*)0x40014044;
        public static unsafe uint* PWM0_MR6 => (uint*)0x40014048;
        public static unsafe uint* PWM0_CCR => (uint*)0x40014028;
        public static unsafe uint* PWM0_CR0 => (uint*)0x4001402C;
        public static unsafe uint* PWM0_PCR => (uint*)0x4001404C;
        public static unsafe uint* PWM0_LER => (uint*)0x40014050;
        public static unsafe uint* PWM0_CTCR => (uint*)0x40014070;

        // ADC
        public const int ADC_BASE = 0x400E4000;
        public static unsafe uint* ADC_CR => (uint*)0x400E4000;
        public static unsafe uint* ADC_GDR => (uint*)0x400E4004;
        public static unsafe uint* ADC_INTEN => (uint*)0x400E400C;
        public static unsafe uint* ADC_STATUS => (uint*)0x400E4010;
        public static unsafe uint* ADC_TR => (uint*)0x400E4014;

        // DAC
        public const int DAC_BASE = 0x400E5000;
        public static unsafe uint* DAC_CR => (uint*)0x400E5000;
        public static unsafe uint* DAC_CTRL => (uint*)0x400E5004;

        // Ethernet
        public const int ETH_BASE = 0x50000000;
        public static unsafe uint* ETH_MAC1 => (uint*)0x50000000;
        public static unsafe uint* ETH_MAC2 => (uint*)0x50000004;
        public static unsafe uint* ETH_IPGT => (uint*)0x50000008;
        public static unsafe uint* ETH_IPGR => (uint*)0x5000000C;
        public static unsafe uint* ETH_CLRT => (uint*)0x50000010;
        public static unsafe uint* ETH_MAXF => (uint*)0x50000014;
        public static unsafe uint* ETH_SUPP => (uint*)0x50000018;
        public static unsafe uint* ETH_TEST => (uint*)0x5000001C;
        public static unsafe uint* ETH_MCFG => (uint*)0x50000020;
        public static unsafe uint* ETH_MCMD => (uint*)0x50000024;
        public static unsafe uint* ETH_MADR => (uint*)0x50000028;
        public static unsafe uint* ETH_MWTD => (uint*)0x5000002C;
        public static unsafe uint* ETH_MRDD => (uint*)0x50000030;
        public static unsafe uint* ETH_IND => (uint*)0x50000034;

        // USB Controller
        public const int USB_BASE = 0x50000000;
        public static unsafe uint* USB_HCCHAR => (uint*)0x50000000;
        public static unsafe uint* USB_HCINT => (uint*)0x50000000;
        public static unsafe uint* USB_HCINTMSK => (uint*)0x50000000;
        public static unsafe uint* USB_HCTSIZ => (uint*)0x50000000;
        public static unsafe uint* USB_HCDMA => (uint*)0x50000000;
        public static unsafe uint* USB_HCDMAB => (uint*)0x50000000;
        public static unsafe uint* USB_OTGINTST => (uint*)0x50000000;
        public static unsafe uint* USB_OTGINTEN => (uint*)0x50000000;
        public static unsafe uint* USB_OTGINTSEL => (uint*)0x50000000;

        // DMA Controller
        public const int DMA_BASE = 0x50004000;
        public static unsafe uint* DMA_INTSTAT => (uint*)0x50004000;
        public static unsafe uint* DMA_INTTCSTAT => (uint*)0x50004004;
        public static unsafe uint* DMA_INTTCCLEAR => (uint*)0x50004008;
        public static unsafe uint* DMA_INTERRSTAT => (uint*)0x5000400C;
        public static unsafe uint* DMA_INTERRCLR => (uint*)0x50004010;
        public static unsafe uint* DMA_RAWINTSTAT => (uint*)0x50004014;
        public static unsafe uint* DMA_RAWINTTCSTAT => (uint*)0x50004018;
        public static unsafe uint* DMA_ENBLDCHNS => (uint*)0x5000401C;
        public static unsafe uint* DMA_SOFTBREQ => (uint*)0x50004020;
        public static unsafe uint* DMA_SOFTSREQ => (uint*)0x50004024;
        public static unsafe uint* DMA_CONFIG => (uint*)0x50004028;
        public static unsafe uint* DMA_SYNC => (uint*)0x5000402C;

        // Watchdog Timer
        public const int WDT_BASE = 0x40000000;
        public static unsafe uint* WDT_WDMOD => (uint*)0x40000000;
        public static unsafe uint* WDT_WDTC => (uint*)0x40000004;
        public static unsafe uint* WDT_WDFEED => (uint*)0x40000008;
        public static unsafe uint* WDT_WDTV => (uint*)0x4000000C;

        // RTC
        public const int RTC_BASE = 0x40024000;
        public static unsafe uint* RTC_ILR => (uint*)0x40024000;
        public static unsafe uint* RTC_CCR => (uint*)0x40024004;
        public static unsafe uint* RTC_CIIR => (uint*)0x40024008;
        public static unsafe uint* RTC_CWR => (uint*)0x4002400C;
        public static unsafe uint* RTC_PREINT => (uint*)0x40024010;
        public static unsafe uint* RTC_PREFRAC => (uint*)0x40024014;
        public static unsafe uint* RTC_CRT => (uint*)0x40024018;
        public static unsafe uint* RTC_SEC => (uint*)0x4002401C;
        public static unsafe uint* RTC_MIN => (uint*)0x40024020;
        public static unsafe uint* RTC_HOUR => (uint*)0x40024024;
        public static unsafe uint* RTC_DOM => (uint*)0x40024028;
        public static unsafe uint* RTC_DOW => (uint*)0x4002402C;
        public static unsafe uint* RTC_DOY => (uint*)0x40024030;
        public static unsafe uint* RTC_MONTH => (uint*)0x40024034;
        public static unsafe uint* RTC_YEAR => (uint*)0x40024038;

        // System Control
        public const int SC_BASE = 0x400FC000;
        public static unsafe uint* SC_PLL0CON => (uint*)0x400FC000;
        public static unsafe uint* SC_PLL0CFG => (uint*)0x400FC004;
        public static unsafe uint* SC_PLL0STAT => (uint*)0x400FC008;
        public static unsafe uint* SC_PLL0FEED => (uint*)0x400FC00C;
        public static unsafe uint* SC_PLL1CON => (uint*)0x400FC010;
        public static unsafe uint* SC_PLL1CFG => (uint*)0x400FC014;
        public static unsafe uint* SC_PLL1STAT => (uint*)0x400FC018;
        public static unsafe uint* SC_PLL1FEED => (uint*)0x400FC01C;
        public static unsafe uint* SC_CCLKCFG => (uint*)0x400FC020;
        public static unsafe uint* SC_USBCLKCFG => (uint*)0x400FC024;
        public static unsafe uint* SC_CLKSRC => (uint*)0x400FC028;
        public static unsafe uint* SC_PCLKSEL0 => (uint*)0x400FC02C;
        public static unsafe uint* SC_PCLKSEL1 => (uint*)0x400FC030;
        public static unsafe uint* SC_BOSC => (uint*)0x400FC050;
        public static unsafe uint* SC_EXTINT => (uint*)0x400FC054;
        public static unsafe uint* SC_EXTMODE => (uint*)0x400FC058;
        public static unsafe uint* SC_EXTPOL => (uint*)0x400FC05C;

        // Pin Connect Block
        public const int PINCONNECTBLOCK_BASE = 0x4002C000;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL0 => (uint*)0x4002C000;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL1 => (uint*)0x4002C004;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL2 => (uint*)0x4002C008;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL3 => (uint*)0x4002C00C;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL4 => (uint*)0x4002C010;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL5 => (uint*)0x4002C014;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL6 => (uint*)0x4002C018;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL7 => (uint*)0x4002C01C;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL8 => (uint*)0x4002C020;
        public static unsafe uint* PINCONNECTBLOCK_PINSEL9 => (uint*)0x4002C024;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE0 => (uint*)0x4002C040;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE1 => (uint*)0x4002C044;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE2 => (uint*)0x4002C048;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE3 => (uint*)0x4002C04C;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE4 => (uint*)0x4002C050;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE5 => (uint*)0x4002C054;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE6 => (uint*)0x4002C058;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE7 => (uint*)0x4002C05C;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE8 => (uint*)0x4002C060;
        public static unsafe uint* PINCONNECTBLOCK_PINMODE9 => (uint*)0x4002C064;
        public static unsafe uint* PINCONNECTBLOCK_PINOD0 => (uint*)0x4002C080;
        public static unsafe uint* PINCONNECTBLOCK_PINOD1 => (uint*)0x4002C084;
        public static unsafe uint* PINCONNECTBLOCK_PINOD2 => (uint*)0x4002C088;
        public static unsafe uint* PINCONNECTBLOCK_PINOD3 => (uint*)0x4002C08C;

        // 中断向量定义
        public const int IRQ_WDT = 0;  // Watchdog Timer
        public const int IRQ_RESERVED = 1;  // Reserved
        public const int IRQ_DEBUG_MON = 2;  // ARM Debug Mon
        public const int IRQ_RESERVED = 3;  // Reserved
        public const int IRQ_TIMER0 = 4;  // Timer 0
        public const int IRQ_TIMER1 = 5;  // Timer 1
        public const int IRQ_PWM0 = 6;  // PWM 0
        public const int IRQ_UART0 = 7;  // UART 0
        public const int IRQ_UART1 = 8;  // UART 1
        public const int IRQ_PWM1 = 9;  // PWM 1
        public const int IRQ_I2C0 = 10;  // I2C 0
        public const int IRQ_I2C1 = 11;  // I2C 1
        public const int IRQ_SPI0 = 12;  // SPI 0
        public const int IRQ_SPI1 = 13;  // SPI 1
        public const int IRQ_RTC = 14;  // RTC
        public const int IRQ_EINT0 = 15;  // External Interrupt 0
        public const int IRQ_EINT1 = 16;  // External Interrupt 1
        public const int IRQ_EINT2 = 17;  // External Interrupt 2
        public const int IRQ_EINT3 = 18;  // External Interrupt 3
        public const int IRQ_RESERVED = 19;  // Reserved
        public const int IRQ_ADC = 20;  // A/D Converter
        public const int IRQ_BOD = 21;  // Brown-Out Detect
        public const int IRQ_USB = 22;  // USB
        public const int IRQ_CAN = 23;  // CAN
        public const int IRQ_GP = 24;  // General Purpose DMA
        public const int IRQ_I2S = 25;  // I2S
        public const int IRQ_ETHERNET = 26;  // Ethernet
        public const int IRQ_RIT = 27;  // Repetitive Interrupt Timer
        public const int IRQ_QM = 28;  // Quadrature Encoder
        public const int IRQ_RESERVED = 29;  // Reserved
        public const int IRQ_RESERVED = 30;  // Reserved

        // 引脚定义
        public const int PIN_RESET = 1;  // External Reset
        public const int PIN_P0_0 = 2;  // GPIO Port 0.0
        public const int PIN_P0_1 = 3;  // GPIO Port 0.1
        public const int PIN_VSSA = 4;  // Analog Ground
        public const int PIN_VDDA = 5;  // Analog 3.3V
        public const int PIN_P0_2 = 6;  // GPIO Port 0.2
        public const int PIN_P0_3 = 7;  // GPIO Port 0.3
        public const int PIN_P0_4 = 8;  // GPIO Port 0.4
        public const int PIN_P0_5 = 9;  // GPIO Port 0.5
        public const int PIN_P0_6 = 10;  // GPIO Port 0.6
        public const int PIN_P0_7 = 11;  // GPIO Port 0.7
        public const int PIN_P0_8 = 12;  // GPIO Port 0.8
        public const int PIN_P0_9 = 13;  // GPIO Port 0.9
        public const int PIN_P0_10 = 14;  // GPIO Port 0.10
        public const int PIN_VSS = 15;  // Ground
        public const int PIN_VDD = 16;  // 3.3V
        public const int PIN_P0_11 = 17;  // GPIO Port 0.11
        public const int PIN_P0_12 = 18;  // GPIO Port 0.12
        public const int PIN_P0_13 = 19;  // GPIO Port 0.13
        public const int PIN_P0_14 = 20;  // GPIO Port 0.14
        public const int PIN_P0_15 = 21;  // GPIO Port 0.15
        public const int PIN_P0_16 = 22;  // GPIO Port 0.16
        public const int PIN_P0_17 = 23;  // GPIO Port 0.17
        public const int PIN_P0_18 = 24;  // GPIO Port 0.18
        public const int PIN_P0_19 = 25;  // GPIO Port 0.19
        public const int PIN_P0_20 = 26;  // GPIO Port 0.20
        public const int PIN_P0_21 = 27;  // GPIO Port 0.21
        public const int PIN_P0_22 = 28;  // GPIO Port 0.22
        public const int PIN_P0_23 = 29;  // GPIO Port 0.23
        public const int PIN_VSS = 30;  // Ground
        public const int PIN_VDD = 31;  // 3.3V
        public const int PIN_RTCX1 = 32;  // RTC Crystal Input
        public const int PIN_RTCX2 = 33;  // RTC Crystal Output
        public const int PIN_P1_0 = 34;  // GPIO Port 1.0
        public const int PIN_P1_1 = 35;  // GPIO Port 1.1
        public const int PIN_P1_2 = 36;  // GPIO Port 1.2
        public const int PIN_P1_3 = 37;  // GPIO Port 1.3
        public const int PIN_P1_4 = 38;  // GPIO Port 1.4
        public const int PIN_P1_5 = 39;  // GPIO Port 1.5
        public const int PIN_P1_6 = 40;  // GPIO Port 1.6
        public const int PIN_P1_7 = 41;  // GPIO Port 1.7
        public const int PIN_P1_8 = 42;  // GPIO Port 1.8
        public const int PIN_P1_9 = 43;  // GPIO Port 1.9
        public const int PIN_P1_10 = 44;  // GPIO Port 1.10
        public const int PIN_P1_11 = 45;  // GPIO Port 1.11
        public const int PIN_P1_12 = 46;  // GPIO Port 1.12
        public const int PIN_P1_13 = 47;  // GPIO Port 1.13
        public const int PIN_P1_14 = 48;  // GPIO Port 1.14
        public const int PIN_P1_15 = 49;  // GPIO Port 1.15
        public const int PIN_P1_16 = 50;  // GPIO Port 1.16
        public const int PIN_P1_17 = 51;  // GPIO Port 1.17
        public const int PIN_P1_18 = 52;  // GPIO Port 1.18
        public const int PIN_P1_19 = 53;  // GPIO Port 1.19
        public const int PIN_P1_20 = 54;  // GPIO Port 1.20
        public const int PIN_P1_21 = 55;  // GPIO Port 1.21
        public const int PIN_P1_22 = 56;  // GPIO Port 1.22
        public const int PIN_P1_23 = 57;  // GPIO Port 1.23
        public const int PIN_P1_24 = 58;  // GPIO Port 1.24
        public const int PIN_P1_25 = 59;  // GPIO Port 1.25
        public const int PIN_P1_26 = 60;  // GPIO Port 1.26
        public const int PIN_P1_27 = 61;  // GPIO Port 1.27
        public const int PIN_P1_28 = 62;  // GPIO Port 1.28
        public const int PIN_P1_29 = 63;  // GPIO Port 1.29
        public const int PIN_P1_30 = 64;  // GPIO Port 1.30
        public const int PIN_P1_31 = 65;  // GPIO Port 1.31
        public const int PIN_P2_0 = 66;  // GPIO Port 2.0
        public const int PIN_P2_1 = 67;  // GPIO Port 2.1
        public const int PIN_P2_2 = 68;  // GPIO Port 2.2
        public const int PIN_P2_3 = 69;  // GPIO Port 2.3
        public const int PIN_P2_4 = 70;  // GPIO Port 2.4
        public const int PIN_P2_5 = 71;  // GPIO Port 2.5
        public const int PIN_P2_6 = 72;  // GPIO Port 2.6
        public const int PIN_P2_7 = 73;  // GPIO Port 2.7
        public const int PIN_P2_8 = 74;  // GPIO Port 2.8
        public const int PIN_P2_9 = 75;  // GPIO Port 2.9
        public const int PIN_P2_10 = 76;  // GPIO Port 2.10
        public const int PIN_P2_11 = 77;  // GPIO Port 2.11
        public const int PIN_P2_12 = 78;  // GPIO Port 2.12
        public const int PIN_P2_13 = 79;  // GPIO Port 2.13
        public const int PIN_P2_14 = 80;  // GPIO Port 2.14
        public const int PIN_P2_15 = 81;  // GPIO Port 2.15
        public const int PIN_VSS = 82;  // Ground
        public const int PIN_VDD = 83;  // 3.3V

        public static void lpc1768_init()
        {
            // 硬件初始化代码
        }
    }
}
