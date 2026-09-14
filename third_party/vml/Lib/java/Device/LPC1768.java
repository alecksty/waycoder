package vml.device.nxp.lpc1768;

/**
 * LPC1768 寄存器定义
 * 生成自: NXP/LPC17xx/LPC1768
 * 版本: 1.0
 */
public final class LPC1768 {
    private LPC1768() {} // 工具类
    // CPU架构: ARM-Cortex-M3, 32位, 12000000 Hz

    // 寄存器定义
    // General Purpose Register 0
    public static final int R0_ADDR = (int)0x00000000;

    // General Purpose Register 1
    public static final int R1_ADDR = (int)0x00000004;

    // General Purpose Register 2
    public static final int R2_ADDR = (int)0x00000008;

    // General Purpose Register 3
    public static final int R3_ADDR = (int)0x0000000C;

    // General Purpose Register 4
    public static final int R4_ADDR = (int)0x00000010;

    // General Purpose Register 5
    public static final int R5_ADDR = (int)0x00000014;

    // General Purpose Register 6
    public static final int R6_ADDR = (int)0x00000018;

    // General Purpose Register 7
    public static final int R7_ADDR = (int)0x0000001C;

    // General Purpose Register 8
    public static final int R8_ADDR = (int)0x00000020;

    // General Purpose Register 9
    public static final int R9_ADDR = (int)0x00000024;

    // General Purpose Register 10
    public static final int R10_ADDR = (int)0x00000028;

    // General Purpose Register 11
    public static final int R11_ADDR = (int)0x0000002C;

    // General Purpose Register 12
    public static final int R12_ADDR = (int)0x00000030;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x00000034;

    // Link Register
    public static final int LR_ADDR = (int)0x00000038;

    // Program Counter
    public static final int PC_ADDR = (int)0x0000003C;

    // Program Status Register
    public static final int PSR_ADDR = (int)0x00000040;
    public static final int PSR_N = 31;  // Negative Flag
    public static final int PSR_Z = 30;  // Zero Flag
    public static final int PSR_C = 29;  // Carry Flag
    public static final int PSR_V = 28;  // Overflow Flag
    public static final int PSR_Q = 27;  // Saturation Flag
    public static final int PSR_ICI1 = 0;  // Interrupt Continue State
    public static final int PSR_GE = 0;  // Greater than or Equal
    public static final int PSR_IT = 0;  // If-Then execution state
    public static final int PSR_APSR = 0;  // Application Program Status

    // Priority Mask Register
    public static final int PRIMASK_ADDR = (int)0xE0000E20;

    // Fault Mask Register
    public static final int FAULTMASK_ADDR = (int)0xE0000E28;

    // Base Priority Register
    public static final int BASEPRI_ADDR = (int)0xE0000E24;

    // Control Register
    public static final int CONTROL_ADDR = (int)0xE0000E2C;

    // 内存段定义
    // Main Flash (512KB)
    public static final int FLASH_START = (int)0x00000000;
    public static final int FLASH_END = (int)0x0007FFFF;
    public static final int FLASH_SIZE = 524288;

    // Boot Flash (32KB)
    public static final int FLASH_BOOT_START = (int)0x00080000;
    public static final int FLASH_BOOT_END = (int)0x0007FFFF;
    public static final int FLASH_BOOT_SIZE = 32768;

    // SRAM (64KB)
    public static final int SRAM_START = (int)0x10000000;
    public static final int SRAM_END = (int)0x1000FFFF;
    public static final int SRAM_SIZE = 65536;

    // AHB1 Peripherals
    public static final int AHB1_START = (int)0x20000000;
    public static final int AHB1_END = (int)0x200FFFFF;
    public static final int AHB1_SIZE = 1048576;

    // APB0 Peripherals
    public static final int APB0_START = (int)0x40000000;
    public static final int APB0_END = (int)0x400FFFFF;
    public static final int APB0_SIZE = 1048576;

    // APB1 Peripherals
    public static final int APB1_START = (int)0x50000000;
    public static final int APB1_END = (int)0x500FFFFF;
    public static final int APB1_SIZE = 1048576;

    // 外设定义
    // GPIO
    public static final int GPIO_BASE = (int)0x2009C000;
    public static final int GPIO_FIODIR = (int)0x2009C000;
    public static final int GPIO_FIOMASK = (int)0x2009C004;
    public static final int GPIO_FIOPIN = (int)0x2009C008;
    public static final int GPIO_FIOSET = (int)0x2009C00C;
    public static final int GPIO_FIOCLR = (int)0x2009C010;
    public static final int GPIO_P0 = (int)0x2009C014;
    public static final int GPIO_P1 = (int)0x2009C018;
    public static final int GPIO_P2 = (int)0x2009C01C;
    public static final int GPIO_P3 = (int)0x2009C020;
    public static final int GPIO_P4 = (int)0x2009C024;

    // UART0
    public static final int UART0_BASE = (int)0x4000C000;
    public static final int UART0_RBR = (int)0x4000C000;
    public static final int UART0_THR = (int)0x4000C000;
    public static final int UART0_DLL = (int)0x4000C000;
    public static final int UART0_DLM = (int)0x4000C004;
    public static final int UART0_IER = (int)0x4000C004;
    public static final int UART0_IIR = (int)0x4000C008;
    public static final int UART0_FCR = (int)0x4000C008;
    public static final int UART0_LCR = (int)0x4000C00C;
    public static final int UART0_LSR = (int)0x4000C014;
    public static final int UART0_SCR = (int)0x4000C01C;
    public static final int UART0_ACR = (int)0x4000C020;
    public static final int UART0_ICR = (int)0x4000C024;
    public static final int UART0_FDR = (int)0x4000C028;
    public static final int UART0_TER = (int)0x4000C030;

    // UART1
    public static final int UART1_BASE = (int)0x4000D000;
    public static final int UART1_RBR = (int)0x4000D000;
    public static final int UART1_THR = (int)0x4000D000;
    public static final int UART1_DLL = (int)0x4000D000;
    public static final int UART1_DLM = (int)0x4000D004;
    public static final int UART1_IER = (int)0x4000D004;
    public static final int UART1_IIR = (int)0x4000D008;
    public static final int UART1_LCR = (int)0x4000D00C;
    public static final int UART1_LSR = (int)0x4000D014;
    public static final int UART1_SCR = (int)0x4000D01C;
    public static final int UART1_MSR = (int)0x4000D020;
    public static final int UART1_SCR = (int)0x4000D024;

    // UART2
    public static final int UART2_BASE = (int)0x40098000;
    public static final int UART2_RBR = (int)0x40098000;
    public static final int UART2_THR = (int)0x40098000;
    public static final int UART2_DLL = (int)0x40098000;
    public static final int UART2_DLM = (int)0x40098004;
    public static final int UART2_IER = (int)0x40098004;
    public static final int UART2_IIR = (int)0x40098008;
    public static final int UART2_LCR = (int)0x4009800C;
    public static final int UART2_LSR = (int)0x40098014;

    // UART3
    public static final int UART3_BASE = (int)0x4009C000;
    public static final int UART3_RBR = (int)0x4009C000;
    public static final int UART3_THR = (int)0x4009C000;
    public static final int UART3_DLL = (int)0x4009C000;
    public static final int UART3_DLM = (int)0x4009C004;
    public static final int UART3_IER = (int)0x4009C004;
    public static final int UART3_IIR = (int)0x4009C008;
    public static final int UART3_LCR = (int)0x4009C00C;
    public static final int UART3_LSR = (int)0x4009C014;

    // SPI0
    public static final int SPI0_BASE = (int)0x40088000;
    public static final int SPI0_CR0 = (int)0x40088000;
    public static final int SPI0_CR1 = (int)0x40088004;
    public static final int SPI0_DR = (int)0x40088008;
    public static final int SPI0_SR = (int)0x4008800C;
    public static final int SPI0_CPSR = (int)0x40088010;
    public static final int SPI0_IMSC = (int)0x40088014;
    public static final int SPI0_RIS = (int)0x40088018;
    public static final int SPI0_MIS = (int)0x4008801C;
    public static final int SPI0_ICR = (int)0x40088020;

    // SPI1
    public static final int SPI1_BASE = (int)0x4008C000;
    public static final int SPI1_CR0 = (int)0x4008C000;
    public static final int SPI1_CR1 = (int)0x4008C004;
    public static final int SPI1_DR = (int)0x4008C008;
    public static final int SPI1_SR = (int)0x4008C00C;
    public static final int SPI1_CPSR = (int)0x4008C010;

    // I2C0
    public static final int I2C0_BASE = (int)0x4001C000;
    public static final int I2C0_CON = (int)0x4001C000;
    public static final int I2C0_TAR = (int)0x4001C004;
    public static final int I2C0_DAT = (int)0x4001C008;
    public static final int I2C0_SSHC = (int)0x4001C00C;
    public static final int I2C0_HSH = (int)0x4001C010;
    public static final int I2C0_INTX = (int)0x4001C014;
    public static final int I2C0_INTM = (int)0x4001C018;
    public static final int I2C0_AR = (int)0x4001C01C;
    public static final int I2C0_SR = (int)0x4001C020;
    public static final int I2C0_TXFL = (int)0x4001C024;
    public static final int I2C0_RXFL = (int)0x4001C028;
    public static final int I2C0_COMP = (int)0x4001C02C;
    public static final int I2C0_RXFI = (int)0x4001C030;
    public static final int I2C0_RXFT = (int)0x4001C030;

    // I2C1
    public static final int I2C1_BASE = (int)0x4001C000;
    public static final int I2C1_CON = (int)0x4001C000;
    public static final int I2C1_TAR = (int)0x4001C004;
    public static final int I2C1_DAT = (int)0x4001C008;
    public static final int I2C1_SR = (int)0x4001C020;

    // Timer0
    public static final int TIMER0_BASE = (int)0x40004000;
    public static final int TIMER0_IR = (int)0x40004000;
    public static final int TIMER0_TCR = (int)0x40004004;
    public static final int TIMER0_TC = (int)0x40004008;
    public static final int TIMER0_PR = (int)0x4000400C;
    public static final int TIMER0_PC = (int)0x40004010;
    public static final int TIMER0_MCR = (int)0x40004014;
    public static final int TIMER0_MR0 = (int)0x40004018;
    public static final int TIMER0_MR1 = (int)0x4000401C;
    public static final int TIMER0_MR2 = (int)0x40004020;
    public static final int TIMER0_MR3 = (int)0x40004024;
    public static final int TIMER0_CCR = (int)0x40004028;
    public static final int TIMER0_CR0 = (int)0x4000402C;
    public static final int TIMER0_CR1 = (int)0x40004030;
    public static final int TIMER0_CR2 = (int)0x40004034;
    public static final int TIMER0_CR3 = (int)0x40004038;
    public static final int TIMER0_EMR = (int)0x4000403C;
    public static final int TIMER0_CTCR = (int)0x40004070;
    public static final int TIMER0_EW = (int)0x40004074;

    // Timer1
    public static final int TIMER1_BASE = (int)0x40008000;
    public static final int TIMER1_IR = (int)0x40008000;
    public static final int TIMER1_TCR = (int)0x40008004;
    public static final int TIMER1_TC = (int)0x40008008;
    public static final int TIMER1_PR = (int)0x4000800C;
    public static final int TIMER1_MCR = (int)0x40008014;
    public static final int TIMER1_MR0 = (int)0x40008018;
    public static final int TIMER1_MR1 = (int)0x4000801C;
    public static final int TIMER1_MR2 = (int)0x40008020;
    public static final int TIMER1_MR3 = (int)0x40008024;
    public static final int TIMER1_CCR = (int)0x40008028;
    public static final int TIMER1_CR0 = (int)0x4000802C;
    public static final int TIMER1_CR1 = (int)0x40008030;
    public static final int TIMER1_EMR = (int)0x4000803C;

    // Timer2
    public static final int TIMER2_BASE = (int)0x400A4000;
    public static final int TIMER2_IR = (int)0x400A4000;
    public static final int TIMER2_TCR = (int)0x400A4004;
    public static final int TIMER2_TC = (int)0x400A4008;
    public static final int TIMER2_PR = (int)0x400A400C;
    public static final int TIMER2_MCR = (int)0x400A4014;
    public static final int TIMER2_MR0 = (int)0x400A4018;
    public static final int TIMER2_CCR = (int)0x400A4028;
    public static final int TIMER2_CR0 = (int)0x400A402C;

    // Timer3
    public static final int TIMER3_BASE = (int)0x400A8000;
    public static final int TIMER3_IR = (int)0x400A8000;
    public static final int TIMER3_TCR = (int)0x400A8004;
    public static final int TIMER3_TC = (int)0x400A8008;
    public static final int TIMER3_PR = (int)0x400A800C;
    public static final int TIMER3_MCR = (int)0x400A8014;
    public static final int TIMER3_MR0 = (int)0x400A8018;
    public static final int TIMER3_CCR = (int)0x400A8028;

    // PWM0
    public static final int PWM0_BASE = (int)0x40014000;
    public static final int PWM0_IR = (int)0x40014000;
    public static final int PWM0_TCR = (int)0x40014004;
    public static final int PWM0_TC = (int)0x40014008;
    public static final int PWM0_PR = (int)0x4001400C;
    public static final int PWM0_PC = (int)0x40014010;
    public static final int PWM0_MCR = (int)0x40014014;
    public static final int PWM0_MR0 = (int)0x40014018;
    public static final int PWM0_MR1 = (int)0x4001401C;
    public static final int PWM0_MR2 = (int)0x40014020;
    public static final int PWM0_MR3 = (int)0x40014024;
    public static final int PWM0_MR4 = (int)0x40014040;
    public static final int PWM0_MR5 = (int)0x40014044;
    public static final int PWM0_MR6 = (int)0x40014048;
    public static final int PWM0_CCR = (int)0x40014028;
    public static final int PWM0_CR0 = (int)0x4001402C;
    public static final int PWM0_PCR = (int)0x4001404C;
    public static final int PWM0_LER = (int)0x40014050;
    public static final int PWM0_CTCR = (int)0x40014070;

    // ADC
    public static final int ADC_BASE = (int)0x400E4000;
    public static final int ADC_CR = (int)0x400E4000;
    public static final int ADC_GDR = (int)0x400E4004;
    public static final int ADC_INTEN = (int)0x400E400C;
    public static final int ADC_STATUS = (int)0x400E4010;
    public static final int ADC_TR = (int)0x400E4014;

    // DAC
    public static final int DAC_BASE = (int)0x400E5000;
    public static final int DAC_CR = (int)0x400E5000;
    public static final int DAC_CTRL = (int)0x400E5004;

    // Ethernet
    public static final int ETH_BASE = (int)0x50000000;
    public static final int ETH_MAC1 = (int)0x50000000;
    public static final int ETH_MAC2 = (int)0x50000004;
    public static final int ETH_IPGT = (int)0x50000008;
    public static final int ETH_IPGR = (int)0x5000000C;
    public static final int ETH_CLRT = (int)0x50000010;
    public static final int ETH_MAXF = (int)0x50000014;
    public static final int ETH_SUPP = (int)0x50000018;
    public static final int ETH_TEST = (int)0x5000001C;
    public static final int ETH_MCFG = (int)0x50000020;
    public static final int ETH_MCMD = (int)0x50000024;
    public static final int ETH_MADR = (int)0x50000028;
    public static final int ETH_MWTD = (int)0x5000002C;
    public static final int ETH_MRDD = (int)0x50000030;
    public static final int ETH_IND = (int)0x50000034;

    // USB Controller
    public static final int USB_BASE = (int)0x50000000;
    public static final int USB_HCCHAR = (int)0x50000000;
    public static final int USB_HCINT = (int)0x50000000;
    public static final int USB_HCINTMSK = (int)0x50000000;
    public static final int USB_HCTSIZ = (int)0x50000000;
    public static final int USB_HCDMA = (int)0x50000000;
    public static final int USB_HCDMAB = (int)0x50000000;
    public static final int USB_OTGINTST = (int)0x50000000;
    public static final int USB_OTGINTEN = (int)0x50000000;
    public static final int USB_OTGINTSEL = (int)0x50000000;

    // DMA Controller
    public static final int DMA_BASE = (int)0x50004000;
    public static final int DMA_INTSTAT = (int)0x50004000;
    public static final int DMA_INTTCSTAT = (int)0x50004004;
    public static final int DMA_INTTCCLEAR = (int)0x50004008;
    public static final int DMA_INTERRSTAT = (int)0x5000400C;
    public static final int DMA_INTERRCLR = (int)0x50004010;
    public static final int DMA_RAWINTSTAT = (int)0x50004014;
    public static final int DMA_RAWINTTCSTAT = (int)0x50004018;
    public static final int DMA_ENBLDCHNS = (int)0x5000401C;
    public static final int DMA_SOFTBREQ = (int)0x50004020;
    public static final int DMA_SOFTSREQ = (int)0x50004024;
    public static final int DMA_CONFIG = (int)0x50004028;
    public static final int DMA_SYNC = (int)0x5000402C;

    // Watchdog Timer
    public static final int WDT_BASE = (int)0x40000000;
    public static final int WDT_WDMOD = (int)0x40000000;
    public static final int WDT_WDTC = (int)0x40000004;
    public static final int WDT_WDFEED = (int)0x40000008;
    public static final int WDT_WDTV = (int)0x4000000C;

    // RTC
    public static final int RTC_BASE = (int)0x40024000;
    public static final int RTC_ILR = (int)0x40024000;
    public static final int RTC_CCR = (int)0x40024004;
    public static final int RTC_CIIR = (int)0x40024008;
    public static final int RTC_CWR = (int)0x4002400C;
    public static final int RTC_PREINT = (int)0x40024010;
    public static final int RTC_PREFRAC = (int)0x40024014;
    public static final int RTC_CRT = (int)0x40024018;
    public static final int RTC_SEC = (int)0x4002401C;
    public static final int RTC_MIN = (int)0x40024020;
    public static final int RTC_HOUR = (int)0x40024024;
    public static final int RTC_DOM = (int)0x40024028;
    public static final int RTC_DOW = (int)0x4002402C;
    public static final int RTC_DOY = (int)0x40024030;
    public static final int RTC_MONTH = (int)0x40024034;
    public static final int RTC_YEAR = (int)0x40024038;

    // System Control
    public static final int SC_BASE = (int)0x400FC000;
    public static final int SC_PLL0CON = (int)0x400FC000;
    public static final int SC_PLL0CFG = (int)0x400FC004;
    public static final int SC_PLL0STAT = (int)0x400FC008;
    public static final int SC_PLL0FEED = (int)0x400FC00C;
    public static final int SC_PLL1CON = (int)0x400FC010;
    public static final int SC_PLL1CFG = (int)0x400FC014;
    public static final int SC_PLL1STAT = (int)0x400FC018;
    public static final int SC_PLL1FEED = (int)0x400FC01C;
    public static final int SC_CCLKCFG = (int)0x400FC020;
    public static final int SC_USBCLKCFG = (int)0x400FC024;
    public static final int SC_CLKSRC = (int)0x400FC028;
    public static final int SC_PCLKSEL0 = (int)0x400FC02C;
    public static final int SC_PCLKSEL1 = (int)0x400FC030;
    public static final int SC_BOSC = (int)0x400FC050;
    public static final int SC_EXTINT = (int)0x400FC054;
    public static final int SC_EXTMODE = (int)0x400FC058;
    public static final int SC_EXTPOL = (int)0x400FC05C;

    // Pin Connect Block
    public static final int PINCONNECTBLOCK_BASE = (int)0x4002C000;
    public static final int PINCONNECTBLOCK_PINSEL0 = (int)0x4002C000;
    public static final int PINCONNECTBLOCK_PINSEL1 = (int)0x4002C004;
    public static final int PINCONNECTBLOCK_PINSEL2 = (int)0x4002C008;
    public static final int PINCONNECTBLOCK_PINSEL3 = (int)0x4002C00C;
    public static final int PINCONNECTBLOCK_PINSEL4 = (int)0x4002C010;
    public static final int PINCONNECTBLOCK_PINSEL5 = (int)0x4002C014;
    public static final int PINCONNECTBLOCK_PINSEL6 = (int)0x4002C018;
    public static final int PINCONNECTBLOCK_PINSEL7 = (int)0x4002C01C;
    public static final int PINCONNECTBLOCK_PINSEL8 = (int)0x4002C020;
    public static final int PINCONNECTBLOCK_PINSEL9 = (int)0x4002C024;
    public static final int PINCONNECTBLOCK_PINMODE0 = (int)0x4002C040;
    public static final int PINCONNECTBLOCK_PINMODE1 = (int)0x4002C044;
    public static final int PINCONNECTBLOCK_PINMODE2 = (int)0x4002C048;
    public static final int PINCONNECTBLOCK_PINMODE3 = (int)0x4002C04C;
    public static final int PINCONNECTBLOCK_PINMODE4 = (int)0x4002C050;
    public static final int PINCONNECTBLOCK_PINMODE5 = (int)0x4002C054;
    public static final int PINCONNECTBLOCK_PINMODE6 = (int)0x4002C058;
    public static final int PINCONNECTBLOCK_PINMODE7 = (int)0x4002C05C;
    public static final int PINCONNECTBLOCK_PINMODE8 = (int)0x4002C060;
    public static final int PINCONNECTBLOCK_PINMODE9 = (int)0x4002C064;
    public static final int PINCONNECTBLOCK_PINOD0 = (int)0x4002C080;
    public static final int PINCONNECTBLOCK_PINOD1 = (int)0x4002C084;
    public static final int PINCONNECTBLOCK_PINOD2 = (int)0x4002C088;
    public static final int PINCONNECTBLOCK_PINOD3 = (int)0x4002C08C;

    // 中断向量定义
    public static final int IRQ_WDT = 0;  // Watchdog Timer
    public static final int IRQ_RESERVED = 1;  // Reserved
    public static final int IRQ_DEBUG_MON = 2;  // ARM Debug Mon
    public static final int IRQ_RESERVED = 3;  // Reserved
    public static final int IRQ_TIMER0 = 4;  // Timer 0
    public static final int IRQ_TIMER1 = 5;  // Timer 1
    public static final int IRQ_PWM0 = 6;  // PWM 0
    public static final int IRQ_UART0 = 7;  // UART 0
    public static final int IRQ_UART1 = 8;  // UART 1
    public static final int IRQ_PWM1 = 9;  // PWM 1
    public static final int IRQ_I2C0 = 10;  // I2C 0
    public static final int IRQ_I2C1 = 11;  // I2C 1
    public static final int IRQ_SPI0 = 12;  // SPI 0
    public static final int IRQ_SPI1 = 13;  // SPI 1
    public static final int IRQ_RTC = 14;  // RTC
    public static final int IRQ_EINT0 = 15;  // External Interrupt 0
    public static final int IRQ_EINT1 = 16;  // External Interrupt 1
    public static final int IRQ_EINT2 = 17;  // External Interrupt 2
    public static final int IRQ_EINT3 = 18;  // External Interrupt 3
    public static final int IRQ_RESERVED = 19;  // Reserved
    public static final int IRQ_ADC = 20;  // A/D Converter
    public static final int IRQ_BOD = 21;  // Brown-Out Detect
    public static final int IRQ_USB = 22;  // USB
    public static final int IRQ_CAN = 23;  // CAN
    public static final int IRQ_GP = 24;  // General Purpose DMA
    public static final int IRQ_I2S = 25;  // I2S
    public static final int IRQ_ETHERNET = 26;  // Ethernet
    public static final int IRQ_RIT = 27;  // Repetitive Interrupt Timer
    public static final int IRQ_QM = 28;  // Quadrature Encoder
    public static final int IRQ_RESERVED = 29;  // Reserved
    public static final int IRQ_RESERVED = 30;  // Reserved

    // 引脚定义
    public static final int PIN_RESET = 1;  // External Reset
    public static final int PIN_P0_0 = 2;  // GPIO Port 0.0
    public static final int PIN_P0_1 = 3;  // GPIO Port 0.1
    public static final int PIN_VSSA = 4;  // Analog Ground
    public static final int PIN_VDDA = 5;  // Analog 3.3V
    public static final int PIN_P0_2 = 6;  // GPIO Port 0.2
    public static final int PIN_P0_3 = 7;  // GPIO Port 0.3
    public static final int PIN_P0_4 = 8;  // GPIO Port 0.4
    public static final int PIN_P0_5 = 9;  // GPIO Port 0.5
    public static final int PIN_P0_6 = 10;  // GPIO Port 0.6
    public static final int PIN_P0_7 = 11;  // GPIO Port 0.7
    public static final int PIN_P0_8 = 12;  // GPIO Port 0.8
    public static final int PIN_P0_9 = 13;  // GPIO Port 0.9
    public static final int PIN_P0_10 = 14;  // GPIO Port 0.10
    public static final int PIN_VSS = 15;  // Ground
    public static final int PIN_VDD = 16;  // 3.3V
    public static final int PIN_P0_11 = 17;  // GPIO Port 0.11
    public static final int PIN_P0_12 = 18;  // GPIO Port 0.12
    public static final int PIN_P0_13 = 19;  // GPIO Port 0.13
    public static final int PIN_P0_14 = 20;  // GPIO Port 0.14
    public static final int PIN_P0_15 = 21;  // GPIO Port 0.15
    public static final int PIN_P0_16 = 22;  // GPIO Port 0.16
    public static final int PIN_P0_17 = 23;  // GPIO Port 0.17
    public static final int PIN_P0_18 = 24;  // GPIO Port 0.18
    public static final int PIN_P0_19 = 25;  // GPIO Port 0.19
    public static final int PIN_P0_20 = 26;  // GPIO Port 0.20
    public static final int PIN_P0_21 = 27;  // GPIO Port 0.21
    public static final int PIN_P0_22 = 28;  // GPIO Port 0.22
    public static final int PIN_P0_23 = 29;  // GPIO Port 0.23
    public static final int PIN_VSS = 30;  // Ground
    public static final int PIN_VDD = 31;  // 3.3V
    public static final int PIN_RTCX1 = 32;  // RTC Crystal Input
    public static final int PIN_RTCX2 = 33;  // RTC Crystal Output
    public static final int PIN_P1_0 = 34;  // GPIO Port 1.0
    public static final int PIN_P1_1 = 35;  // GPIO Port 1.1
    public static final int PIN_P1_2 = 36;  // GPIO Port 1.2
    public static final int PIN_P1_3 = 37;  // GPIO Port 1.3
    public static final int PIN_P1_4 = 38;  // GPIO Port 1.4
    public static final int PIN_P1_5 = 39;  // GPIO Port 1.5
    public static final int PIN_P1_6 = 40;  // GPIO Port 1.6
    public static final int PIN_P1_7 = 41;  // GPIO Port 1.7
    public static final int PIN_P1_8 = 42;  // GPIO Port 1.8
    public static final int PIN_P1_9 = 43;  // GPIO Port 1.9
    public static final int PIN_P1_10 = 44;  // GPIO Port 1.10
    public static final int PIN_P1_11 = 45;  // GPIO Port 1.11
    public static final int PIN_P1_12 = 46;  // GPIO Port 1.12
    public static final int PIN_P1_13 = 47;  // GPIO Port 1.13
    public static final int PIN_P1_14 = 48;  // GPIO Port 1.14
    public static final int PIN_P1_15 = 49;  // GPIO Port 1.15
    public static final int PIN_P1_16 = 50;  // GPIO Port 1.16
    public static final int PIN_P1_17 = 51;  // GPIO Port 1.17
    public static final int PIN_P1_18 = 52;  // GPIO Port 1.18
    public static final int PIN_P1_19 = 53;  // GPIO Port 1.19
    public static final int PIN_P1_20 = 54;  // GPIO Port 1.20
    public static final int PIN_P1_21 = 55;  // GPIO Port 1.21
    public static final int PIN_P1_22 = 56;  // GPIO Port 1.22
    public static final int PIN_P1_23 = 57;  // GPIO Port 1.23
    public static final int PIN_P1_24 = 58;  // GPIO Port 1.24
    public static final int PIN_P1_25 = 59;  // GPIO Port 1.25
    public static final int PIN_P1_26 = 60;  // GPIO Port 1.26
    public static final int PIN_P1_27 = 61;  // GPIO Port 1.27
    public static final int PIN_P1_28 = 62;  // GPIO Port 1.28
    public static final int PIN_P1_29 = 63;  // GPIO Port 1.29
    public static final int PIN_P1_30 = 64;  // GPIO Port 1.30
    public static final int PIN_P1_31 = 65;  // GPIO Port 1.31
    public static final int PIN_P2_0 = 66;  // GPIO Port 2.0
    public static final int PIN_P2_1 = 67;  // GPIO Port 2.1
    public static final int PIN_P2_2 = 68;  // GPIO Port 2.2
    public static final int PIN_P2_3 = 69;  // GPIO Port 2.3
    public static final int PIN_P2_4 = 70;  // GPIO Port 2.4
    public static final int PIN_P2_5 = 71;  // GPIO Port 2.5
    public static final int PIN_P2_6 = 72;  // GPIO Port 2.6
    public static final int PIN_P2_7 = 73;  // GPIO Port 2.7
    public static final int PIN_P2_8 = 74;  // GPIO Port 2.8
    public static final int PIN_P2_9 = 75;  // GPIO Port 2.9
    public static final int PIN_P2_10 = 76;  // GPIO Port 2.10
    public static final int PIN_P2_11 = 77;  // GPIO Port 2.11
    public static final int PIN_P2_12 = 78;  // GPIO Port 2.12
    public static final int PIN_P2_13 = 79;  // GPIO Port 2.13
    public static final int PIN_P2_14 = 80;  // GPIO Port 2.14
    public static final int PIN_P2_15 = 81;  // GPIO Port 2.15
    public static final int PIN_VSS = 82;  // Ground
    public static final int PIN_VDD = 83;  // 3.3V

    public static native void lpc1768_init();
}
