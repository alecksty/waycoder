package vml.device.raspberrypi.rp2040;

/**
 * RP2040 寄存器定义
 * 生成自: Raspberry Pi/RP/RP2040
 * 版本: 1.0
 */
public final class RP2040 {
    private RP2040() {} // 工具类
    // CPU架构: ARM-Cortex-M0+, 32位, 12000000 Hz

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
    public static final int XPSR_ADDR = (int)0x00000040;
    public static final int XPSR_N = 31;  // Negative Flag
    public static final int XPSR_Z = 30;  // Zero Flag
    public static final int XPSR_C = 29;  // Carry Flag
    public static final int XPSR_V = 28;  // Overflow Flag
    public static final int XPSR_Q = 27;  // Saturation Flag
    public static final int XPSR_ICI = 0;  // ICI execution state
    public static final int XPSR_IT = 0;  // If-Then execution state
    public static final int XPSR_T = 24;  // Thumb bit
    public static final int XPSR_IPSR = 0;  // Exception number

    // Priority Mask Register
    public static final int PRIMASK_ADDR = (int)0xE0000E20;

    // Control Register
    public static final int CONTROL_ADDR = (int)0xE0000E24;

    // Fault Mask Register
    public static final int FAULTMASK_ADDR = (int)0xE0000E28;

    // 内存段定义
    // ROM (bootloader)
    public static final int ROM_START = (int)0x00000000;
    public static final int ROM_END = (int)0x00001000;
    public static final int ROM_SIZE = 4096;

    // SRAM0 (16KB)
    public static final int SRAM0_START = (int)0x20000000;
    public static final int SRAM0_END = (int)0x20003FFF;
    public static final int SRAM0_SIZE = 16384;

    // SRAM1 (16KB)
    public static final int SRAM1_START = (int)0x20004000;
    public static final int SRAM1_END = (int)0x20007FFF;
    public static final int SRAM1_SIZE = 16384;

    // SRAM2 (16KB)
    public static final int SRAM2_START = (int)0x20008000;
    public static final int SRAM2_END = (int)0x2000BFFF;
    public static final int SRAM2_SIZE = 16384;

    // SRAM3 (16KB)
    public static final int SRAM3_START = (int)0x2000C000;
    public static final int SRAM3_END = (int)0x2000FFFF;
    public static final int SRAM3_SIZE = 16384;

    // SRAM4 (16KB)
    public static final int SRAM4_START = (int)0x20010000;
    public static final int SRAM4_END = (int)0x20013FFF;
    public static final int SRAM4_SIZE = 16384;

    // APB Peripherals
    public static final int APB_START = (int)0x40000000;
    public static final int APB_END = (int)0x400FFFFF;
    public static final int APB_SIZE = 1048576;

    // AHB Peripherals
    public static final int AHB_START = (int)0x50000000;
    public static final int AHB_END = (int)0x500FFFFF;
    public static final int AHB_SIZE = 1048576;

    // 外设定义
    // IO Bank 0
    public static final int IO_BANK0_BASE = (int)0x40014000;
    public static final int IO_BANK0_GPIO0_STATUS = (int)0x40014000;
    public static final int IO_BANK0_GPIO0_CTRL = (int)0x40014004;
    public static final int IO_BANK0_GPIO1_STATUS = (int)0x40014008;
    public static final int IO_BANK0_GPIO1_CTRL = (int)0x4001400C;
    public static final int IO_BANK0_GPIO2_STATUS = (int)0x40014010;
    public static final int IO_BANK0_GPIO2_CTRL = (int)0x40014014;
    public static final int IO_BANK0_GPIO3_STATUS = (int)0x40014018;
    public static final int IO_BANK0_GPIO3_CTRL = (int)0x4001401C;
    public static final int IO_BANK0_GPIO4_STATUS = (int)0x40014020;
    public static final int IO_BANK0_GPIO4_CTRL = (int)0x40014024;
    public static final int IO_BANK0_GPIO5_STATUS = (int)0x40014028;
    public static final int IO_BANK0_GPIO5_CTRL = (int)0x4001402C;
    public static final int IO_BANK0_GPIO6_STATUS = (int)0x40014030;
    public static final int IO_BANK0_GPIO6_CTRL = (int)0x40014034;
    public static final int IO_BANK0_GPIO7_STATUS = (int)0x40014038;
    public static final int IO_BANK0_GPIO7_CTRL = (int)0x4001403C;
    public static final int IO_BANK0_GPIO8_STATUS = (int)0x40014040;
    public static final int IO_BANK0_GPIO8_CTRL = (int)0x40014044;
    public static final int IO_BANK0_GPIO9_STATUS = (int)0x40014048;
    public static final int IO_BANK0_GPIO9_CTRL = (int)0x4001404C;
    public static final int IO_BANK0_GPIO10_STATUS = (int)0x40014050;
    public static final int IO_BANK0_GPIO10_CTRL = (int)0x40014054;
    public static final int IO_BANK0_GPIO11_STATUS = (int)0x40014058;
    public static final int IO_BANK0_GPIO11_CTRL = (int)0x4001405C;
    public static final int IO_BANK0_GPIO12_STATUS = (int)0x40014060;
    public static final int IO_BANK0_GPIO12_CTRL = (int)0x40014064;
    public static final int IO_BANK0_GPIO13_STATUS = (int)0x40014068;
    public static final int IO_BANK0_GPIO13_CTRL = (int)0x4001406C;
    public static final int IO_BANK0_GPIO14_STATUS = (int)0x40014070;
    public static final int IO_BANK0_GPIO14_CTRL = (int)0x40014074;
    public static final int IO_BANK0_GPIO15_STATUS = (int)0x40014078;
    public static final int IO_BANK0_GPIO15_CTRL = (int)0x4001407C;
    public static final int IO_BANK0_GPIO16_STATUS = (int)0x40014080;
    public static final int IO_BANK0_GPIO16_CTRL = (int)0x40014084;
    public static final int IO_BANK0_GPIO17_STATUS = (int)0x40014088;
    public static final int IO_BANK0_GPIO17_CTRL = (int)0x4001408C;
    public static final int IO_BANK0_GPIO18_STATUS = (int)0x40014090;
    public static final int IO_BANK0_GPIO18_CTRL = (int)0x40014094;
    public static final int IO_BANK0_GPIO19_STATUS = (int)0x40014098;
    public static final int IO_BANK0_GPIO19_CTRL = (int)0x4001409C;
    public static final int IO_BANK0_GPIO20_STATUS = (int)0x400140A0;
    public static final int IO_BANK0_GPIO20_CTRL = (int)0x400140A4;
    public static final int IO_BANK0_GPIO21_STATUS = (int)0x400140A8;
    public static final int IO_BANK0_GPIO21_CTRL = (int)0x400140AC;
    public static final int IO_BANK0_GPIO22_STATUS = (int)0x400140B0;
    public static final int IO_BANK0_GPIO22_CTRL = (int)0x400140B4;
    public static final int IO_BANK0_GPIO23_STATUS = (int)0x400140B8;
    public static final int IO_BANK0_GPIO23_CTRL = (int)0x400140BC;
    public static final int IO_BANK0_GPIO24_STATUS = (int)0x400140C0;
    public static final int IO_BANK0_GPIO24_CTRL = (int)0x400140C4;
    public static final int IO_BANK0_GPIO25_STATUS = (int)0x400140C8;
    public static final int IO_BANK0_GPIO25_CTRL = (int)0x400140CC;
    public static final int IO_BANK0_GPIO26_STATUS = (int)0x400140D0;
    public static final int IO_BANK0_GPIO26_CTRL = (int)0x400140D4;
    public static final int IO_BANK0_GPIO27_STATUS = (int)0x400140D8;
    public static final int IO_BANK0_GPIO27_CTRL = (int)0x400140DC;
    public static final int IO_BANK0_GPIO28_STATUS = (int)0x400140E0;
    public static final int IO_BANK0_GPIO28_CTRL = (int)0x400140E4;
    public static final int IO_BANK0_GPIO29_STATUS = (int)0x400140E8;
    public static final int IO_BANK0_GPIO29_CTRL = (int)0x400140EC;
    public static final int IO_BANK0_INTR = (int)0x400140F0;
    public static final int IO_BANK0_PROC0_INTE = (int)0x400140F4;
    public static final int IO_BANK0_PROC1_INTE = (int)0x400140F8;
    public static final int IO_BANK0_PROC0_INTF = (int)0x400140FC;
    public static final int IO_BANK0_PROC1_INTF = (int)0x40014100;
    public static final int IO_BANK0_PROC0_INTS = (int)0x40014104;
    public static final int IO_BANK0_PROC1_INTS = (int)0x40014108;
    public static final int IO_BANK0_DORMANT_WAKE_INTE = (int)0x4001410C;
    public static final int IO_BANK0_DORMANT_WAKE_INTF = (int)0x40014110;
    public static final int IO_BANK0_DORMANT_WAKE_INTS = (int)0x40014114;

    // Pads
    public static final int PADS_BASE = (int)0x4001E000;
    public static final int PADS_GPIO_VOLT = (int)0x4001E0E0;

    // SIO (Single-cycle I/O)
    public static final int SIO_BASE = (int)0xD0000000;
    public static final int SIO_CPUID = (int)0xD0000000;
    public static final int SIO_GPIO_OUT = (int)0xD0000004;
    public static final int SIO_GPIO_OUT_SET = (int)0xD0000008;
    public static final int SIO_GPIO_OUT_CLR = (int)0xD000000C;
    public static final int SIO_GPIO_OUT_XOR = (int)0xD0000010;
    public static final int SIO_GPIO_OE = (int)0xD0000014;
    public static final int SIO_GPIO_OE_SET = (int)0xD0000018;
    public static final int SIO_GPIO_OE_CLR = (int)0xD000001C;
    public static final int SIO_GPIO_OE_XOR = (int)0xD0000020;
    public static final int SIO_GPIO_IN = (int)0xD0000024;
    public static final int SIO_FIFO_ST = (int)0xD0000040;
    public static final int SIO_FIFO_WR = (int)0xD0000044;
    public static final int SIO_FIFO_RD = (int)0xD0000048;
    public static final int SIO_SPINLOCK_ST = (int)0xD000004C;
    public static final int SIO_INTERRUPT_ST = (int)0xD0000050;

    // UART0
    public static final int UART0_BASE = (int)0x40034000;
    public static final int UART0_UARTDR = (int)0x40034000;
    public static final int UART0_UARTRSR = (int)0x40034004;
    public static final int UART0_UARTECR = (int)0x40034004;
    public static final int UART0_UARTFR = (int)0x40034018;
    public static final int UART0_UARTILPR = (int)0x40034020;
    public static final int UART0_UARTIBRD = (int)0x40034024;
    public static final int UART0_UARTFBRD = (int)0x40034028;
    public static final int UART0_UARTLCR_H = (int)0x4003402C;
    public static final int UART0_UARTCR = (int)0x40034030;
    public static final int UART0_UARTIFLS = (int)0x40034034;
    public static final int UART0_UARTIMSC = (int)0x40034038;
    public static final int UART0_UARTRIS = (int)0x4003403C;
    public static final int UART0_UARTMIS = (int)0x40034040;
    public static final int UART0_UARTICR = (int)0x40034044;
    public static final int UART0_UARTDMACR = (int)0x40034048;

    // UART1
    public static final int UART1_BASE = (int)0x40038000;
    public static final int UART1_UARTDR = (int)0x40038000;
    public static final int UART1_UARTRSR = (int)0x40038004;
    public static final int UART1_UARTFR = (int)0x40038018;
    public static final int UART1_UARTIBRD = (int)0x40038024;
    public static final int UART1_UARTFBRD = (int)0x40038028;
    public static final int UART1_UARTLCR_H = (int)0x4003802C;
    public static final int UART1_UARTCR = (int)0x40038030;
    public static final int UART1_UARTIFLS = (int)0x40038034;
    public static final int UART1_UARTIMSC = (int)0x40038038;
    public static final int UART1_UARTICR = (int)0x40038044;

    // SPI0
    public static final int SPI0_BASE = (int)0x4003C000;
    public static final int SPI0_SSPCR0 = (int)0x4003C000;
    public static final int SPI0_SSPCR1 = (int)0x4003C004;
    public static final int SPI0_SSPDR = (int)0x4003C008;
    public static final int SPI0_SSPSR = (int)0x4003C00C;
    public static final int SPI0_SSPCPSR = (int)0x4003C010;
    public static final int SPI0_SSPIMSC = (int)0x4003C014;
    public static final int SPI0_SSPRIS = (int)0x4003C018;
    public static final int SPI0_SSPMIS = (int)0x4003C01C;
    public static final int SPI0_SSPICR = (int)0x4003C020;
    public static final int SPI0_SSPDMACR = (int)0x4003C024;

    // SPI1
    public static final int SPI1_BASE = (int)0x4003C000;
    public static final int SPI1_SSPCR0 = (int)0x4003C000;
    public static final int SPI1_SSPCR1 = (int)0x4003C004;
    public static final int SPI1_SSPDR = (int)0x4003C008;
    public static final int SPI1_SSPSR = (int)0x4003C00C;
    public static final int SPI1_SSPCPSR = (int)0x4003C010;
    public static final int SPI1_SSPIMSC = (int)0x4003C014;

    // I2C0
    public static final int I2C0_BASE = (int)0x40044000;
    public static final int I2C0_IC_CON = (int)0x40044000;
    public static final int I2C0_IC_TAR = (int)0x40044004;
    public static final int I2C0_IC_SAR = (int)0x40044008;
    public static final int I2C0_IC_DATA_CMD = (int)0x40044010;
    public static final int I2C0_IC_SS_SCL_HCNT = (int)0x40044014;
    public static final int I2C0_IC_SS_SCL_LCNT = (int)0x40044018;
    public static final int I2C0_IC_FS_SCL_HCNT = (int)0x4004401C;
    public static final int I2C0_IC_FS_SCL_LCNT = (int)0x40044020;
    public static final int I2C0_IC_RAW_INTR_STAT = (int)0x40044024;
    public static final int I2C0_IC_ENABLE = (int)0x4004402C;
    public static final int I2C0_IC_STATUS = (int)0x40044030;
    public static final int I2C0_IC_TXFLR = (int)0x40044034;
    public static final int I2C0_IC_RXFLR = (int)0x40044038;
    public static final int I2C0_IC_TX_ABRT = (int)0x4004403C;
    public static final int I2C0_IC_DMA_CR = (int)0x40044040;
    public static final int I2C0_IC_DMA_TDLR = (int)0x40044044;
    public static final int I2C0_IC_DMA_RDLR = (int)0x40044048;

    // I2C1
    public static final int I2C1_BASE = (int)0x40048000;
    public static final int I2C1_IC_CON = (int)0x40048000;
    public static final int I2C1_IC_TAR = (int)0x40048004;
    public static final int I2C1_IC_ENABLE = (int)0x4004802C;
    public static final int I2C1_IC_STATUS = (int)0x40048030;
    public static final int I2C1_IC_TX_ABRT = (int)0x4004803C;

    // PWM0
    public static final int PWM0_BASE = (int)0x40050000;
    public static final int PWM0_CS = (int)0x40050000;
    public static final int PWM0_CMPR0 = (int)0x40050004;
    public static final int PWM0_CMPR1 = (int)0x40050008;
    public static final int PWM0_CMPR2 = (int)0x4005000C;
    public static final int PWM0_CMPR3 = (int)0x40050010;
    public static final int PWM0_CC = (int)0x40050014;
    public static final int PWM0_TOP = (int)0x40050018;
    public static final int PWM0_INTR = (int)0x4005001C;
    public static final int PWM0_INTE = (int)0x40050020;
    public static final int PWM0_INTF = (int)0x40050024;
    public static final int PWM0_INTS = (int)0x40050028;
    public static final int PWM0_PHS0 = (int)0x40050034;
    public static final int PWM0_PHS1 = (int)0x40050038;
    public static final int PWM0_PHS2 = (int)0x4005003C;
    public static final int PWM0_PHS3 = (int)0x40050040;
    public static final int PWM0_DIV = (int)0x40050044;
    public static final int PWM0_PHASE = (int)0x40050048;

    // PWM1
    public static final int PWM1_BASE = (int)0x40051000;
    public static final int PWM1_CS = (int)0x40051000;
    public static final int PWM1_CMPR0 = (int)0x40051004;
    public static final int PWM1_CMPR1 = (int)0x40051008;
    public static final int PWM1_CMPR2 = (int)0x4005100C;
    public static final int PWM1_CMPR3 = (int)0x40051010;
    public static final int PWM1_CC = (int)0x40051014;
    public static final int PWM1_TOP = (int)0x40051018;
    public static final int PWM1_DIV = (int)0x40051044;

    // ADC
    public static final int ADC_BASE = (int)0x4004C000;
    public static final int ADC_ADC_CS = (int)0x4004C000;
    public static final int ADC_ADC_RESULT = (int)0x4004C004;
    public static final int ADC_ADC_FCS = (int)0x4004C008;
    public static final int ADC_ADC_FIFO = (int)0x4004C00C;
    public static final int ADC_ADC_TS = (int)0x4004C010;
    public static final int ADC_ADC_OFFSET = (int)0x4004C014;
    public static final int ADC_ADC_TRIG = (int)0x4004C018;

    // Timer0
    public static final int TIMER0_BASE = (int)0x40054000;
    public static final int TIMER0_TIMEHW = (int)0x40054000;
    public static final int TIMER0_TIMELW = (int)0x40054004;
    public static final int TIMER0_TIMEHA = (int)0x40054008;
    public static final int TIMER0_TIMELA = (int)0x4005400C;
    public static final int TIMER0_TIMERA = (int)0x40054010;
    public static final int TIMER0_TIMERIQ = (int)0x40054014;
    public static final int TIMER0_TIMEREAD = (int)0x40054018;

    // Timer1
    public static final int TIMER1_BASE = (int)0x40058000;
    public static final int TIMER1_TIMEHW = (int)0x40058000;
    public static final int TIMER1_TIMELW = (int)0x40058004;
    public static final int TIMER1_TIMEHA = (int)0x40058008;
    public static final int TIMER1_TIMELA = (int)0x4005800C;
    public static final int TIMER1_TIMERA = (int)0x40058010;

    // RTC
    public static final int RTC_BASE = (int)0x4005C000;
    public static final int RTC_RTC_CLKS = (int)0x4005C000;
    public static final int RTC_RTC_SET = (int)0x4005C004;
    public static final int RTC_RTC_WR = (int)0x4005C008;
    public static final int RTC_RTC_DATE = (int)0x4005C00C;
    public static final int RTC_RTC_TOTAL = (int)0x4005C010;
    public static final int RTC_RTC_HASH = (int)0x4005C014;
    public static final int RTC_RTC_RTC = (int)0x4005C018;
    public static final int RTC_INTR = (int)0x4005C01C;
    public static final int RTC_INTE = (int)0x4005C020;
    public static final int RTC_INTF = (int)0x4005C024;
    public static final int RTC_INTS = (int)0x4005C028;

    // Watchdog
    public static final int WATCHDOG_BASE = (int)0x40060000;
    public static final int WATCHDOG_WATCHDOG_CTL = (int)0x40060000;
    public static final int WATCHDOG_WATCHDOG_MOD = (int)0x40060004;
    public static final int WATCHDOG_WATCHDOG_FR = (int)0x40060008;
    public static final int WATCHDOG_WATCHDOG_LOAD = (int)0x4006000C;

    // USB
    public static final int USB_BASE = (int)0x50100000;
    public static final int USB_USB_CTRL = (int)0x50100000;
    public static final int USB_USB_ADDR = (int)0x50100004;
    public static final int USB_USB_PWR = (int)0x50100008;
    public static final int USB_USB_TXFIFO = (int)0x50100010;
    public static final int USB_USB_RXFIFO = (int)0x50100014;
    public static final int USB_USB_TXIE = (int)0x50100018;
    public static final int USB_USB_RXIE = (int)0x5010001C;
    public static final int USB_USB_IS = (int)0x50100020;
    public static final int USB_USB_IM = (int)0x50100024;
    public static final int USB_USB_IE = (int)0x50100028;
    public static final int USB_USB_REVO = (int)0x5010002C;
    public static final int USB_USB_EP = (int)0x50100030;
    public static final int USB_USB_BUFF = (int)0x50100034;
    public static final int USB_USB_MPS = (int)0x50100038;

    // PIO0
    public static final int PIO0_BASE = (int)0x50200000;
    public static final int PIO0_CTRL = (int)0x50200000;
    public static final int PIO0_FSTAT = (int)0x50200004;
    public static final int PIO0_FDEBUG = (int)0x50200008;
    public static final int PIO0_FCTRL = (int)0x5020000C;
    public static final int PIO0_RXF0 = (int)0x50200010;
    public static final int PIO0_RXF1 = (int)0x50200014;
    public static final int PIO0_RXF2 = (int)0x50200018;
    public static final int PIO0_RXF3 = (int)0x5020001C;
    public static final int PIO0_TXF0 = (int)0x50200020;
    public static final int PIO0_TXF1 = (int)0x50200024;
    public static final int PIO0_TXF2 = (int)0x50200028;
    public static final int PIO0_TXF3 = (int)0x5020002C;
    public static final int PIO0_IRQ = (int)0x50200030;
    public static final int PIO0_IRQ_FORCE = (int)0x50200034;
    public static final int PIO0_IRQ_INTF = (int)0x50200038;
    public static final int PIO0_IRQ_INTS = (int)0x5020003C;
    public static final int PIO0_SM0_CLKDIV = (int)0x502000C8;
    public static final int PIO0_SM0_EXECCTRL = (int)0x502000CC;
    public static final int PIO0_SM0_SHIFTCTRL = (int)0x502000D0;
    public static final int PIO0_SM0_ADDR = (int)0x502000D4;
    public static final int PIO0_SM0_INSTR = (int)0x502000D8;
    public static final int PIO0_SM0_PINCTRL = (int)0x502000DC;

    // PIO1
    public static final int PIO1_BASE = (int)0x50201000;
    public static final int PIO1_CTRL = (int)0x50201000;
    public static final int PIO1_FSTAT = (int)0x50201004;
    public static final int PIO1_IRQ = (int)0x50201030;
    public static final int PIO1_SM0_CLKDIV = (int)0x502010C8;
    public static final int PIO1_SM0_EXECCTRL = (int)0x502010CC;
    public static final int PIO1_SM0_SHIFTCTRL = (int)0x502010D0;
    public static final int PIO1_SM0_ADDR = (int)0x502010D4;
    public static final int PIO1_SM0_INSTR = (int)0x502010D8;

    // Clock Manager
    public static final int CLOCKS_BASE = (int)0x40008000;
    public static final int CLOCKS_CLK_GP0DIV = (int)0x40008000;
    public static final int CLOCKS_CLK_GP0CTRL = (int)0x40008004;
    public static final int CLOCKS_CLK_GP1DIV = (int)0x40008008;
    public static final int CLOCKS_CLK_GP1CTRL = (int)0x4000800C;
    public static final int CLOCKS_CLK_GP2DIV = (int)0x40008010;
    public static final int CLOCKS_CLK_GP2CTRL = (int)0x40008014;
    public static final int CLOCKS_CLK_REF = (int)0x4000801C;
    public static final int CLOCKS_CLK_SYS = (int)0x40008020;
    public static final int CLOCKS_CLK_PERI = (int)0x40008024;

    // Crystal Oscillator
    public static final int XOSC_BASE = (int)0x40020000;
    public static final int XOSC_XOSC_CTRL = (int)0x40020000;
    public static final int XOSC_XOSC_STATUS = (int)0x40020004;
    public static final int XOSC_XOSC_COUNT = (int)0x40020008;

    // Ring Oscillator
    public static final int ROSC_BASE = (int)0x40010000;
    public static final int ROSC_ROSC_CTRL = (int)0x40010000;
    public static final int ROSC_ROSC_FREQA = (int)0x40010004;
    public static final int ROSC_ROSC_FREQB = (int)0x40010008;
    public static final int ROSC_ROSC_FREQC = (int)0x4001000C;
    public static final int ROSC_ROSC_FREQD = (int)0x40010010;
    public static final int ROSC_ROSC_STATUS = (int)0x40010014;
    public static final int ROSC_ROSC_DR = (int)0x40010018;

    // System PLL
    public static final int PLL_SYS_BASE = (int)0x40028000;
    public static final int PLL_SYS_PLL_CS = (int)0x40028000;
    public static final int PLL_SYS_PLL_PWR = (int)0x40028004;
    public static final int PLL_SYS_PLL_FBDIV = (int)0x40028008;
    public static final int PLL_SYS_PLL_PRIMARY = (int)0x4002800C;
    public static final int PLL_SYS_PLL_POSTDIV1 = (int)0x40028010;
    public static final int PLL_SYS_PLL_POSTDIV2 = (int)0x40028014;

    // USB PLL
    public static final int PLL_USB_BASE = (int)0x4002C000;
    public static final int PLL_USB_PLL_CS = (int)0x4002C000;
    public static final int PLL_USB_PLL_PWR = (int)0x4002C004;
    public static final int PLL_USB_PLL_FBDIV = (int)0x4002C008;
    public static final int PLL_USB_PLL_PRIMARY = (int)0x4002C00C;

    // Resets
    public static final int RESETS_BASE = (int)0x4000C000;
    public static final int RESETS_RESET = (int)0x4000C000;
    public static final int RESETS_RESET_DONE = (int)0x4000C004;
    public static final int RESETS_WD_RESET = (int)0x4000C008;

    // 中断向量定义
    public static final int IRQ_RESERVED = 0;  // Reserved
    public static final int IRQ_TIMER0_IRQ_0 = 1;  // Timer 0 IRQ 0
    public static final int IRQ_TIMER0_IRQ_1 = 2;  // Timer 0 IRQ 1
    public static final int IRQ_TIMER1_IRQ_0 = 3;  // Timer 1 IRQ 0
    public static final int IRQ_TIMER1_IRQ_1 = 4;  // Timer 1 IRQ 1
    public static final int IRQ_TIMER2_IRQ_0 = 5;  // Timer 2 IRQ 0
    public static final int IRQ_TIMER2_IRQ_1 = 6;  // Timer 2 IRQ 1
    public static final int IRQ_TIMER3_IRQ_0 = 7;  // Timer 3 IRQ 0
    public static final int IRQ_TIMER3_IRQ_1 = 8;  // Timer 3 IRQ 1
    public static final int IRQ_PWM_IRQ_WRAP = 9;  // PWM IRQ wrap
    public static final int IRQ_USB_CTRL_IRQ = 10;  // USB ctrl IRQ
    public static final int IRQ_USB_DMA_IRQ = 11;  // USB dma IRQ
    public static final int IRQ_USB_VBUS_DETECT = 12;  // USB VBUS detect IRQ
    public static final int IRQ_USB_RESUME_IRQ = 13;  // USB resume IRQ
    public static final int IRQ_ADC_IRQ_FIFO = 14;  // ADC IRQ FIFO
    public static final int IRQ_ADC_IRQ_TRIGGER = 15;  // ADC IRQ trigger
    public static final int IRQ_I2C0_IRQ = 16;  // I2C 0 IRQ
    public static final int IRQ_I2C1_IRQ = 17;  // I2C 1 IRQ
    public static final int IRQ_SPI0_IRQ = 18;  // SPI 0 IRQ
    public static final int IRQ_SPI1_IRQ = 19;  // SPI 1 IRQ
    public static final int IRQ_UART0_IRQ = 20;  // UART 0 IRQ
    public static final int IRQ_UART0_IRQ_TX = 21;  // UART 0 IRQ TX
    public static final int IRQ_UART1_IRQ = 22;  // UART 1 IRQ
    public static final int IRQ_UART1_IRQ_TX = 23;  // UART 1 IRQ TX
    public static final int IRQ_PIO0_IRQ_0 = 24;  // PIO 0 IRQ 0
    public static final int IRQ_PIO0_IRQ_1 = 25;  // PIO 0 IRQ 1
    public static final int IRQ_PIO1_IRQ_0 = 26;  // PIO 1 IRQ 0
    public static final int IRQ_PIO1_IRQ_1 = 27;  // PIO 1 IRQ 1
    public static final int IRQ_RTC_IRQ = 28;  // RTC IRQ

    // 引脚定义
    public static final int PIN_GP0 = 1;  // UART0 TX / GP0
    public static final int PIN_GP1 = 2;  // UART0 RX / GP1
    public static final int PIN_GP2 = 3;  // SPI0 TX / GP2
    public static final int PIN_GP3 = 4;  // SPI0 RX / GP3
    public static final int PIN_GP4 = 5;  // SPI0 CSn / GP4
    public static final int PIN_GP5 = 6;  // SPI0 SCK / GP5
    public static final int PIN_GP6 = 7;  // PWM6 / GP6
    public static final int PIN_GP7 = 8;  // PWM7 / GP7
    public static final int PIN_GP8 = 9;  // PWM8 / GP8
    public static final int PIN_GP9 = 10;  // PWM9 / GP9
    public static final int PIN_GP10 = 11;  // SPI1 TX / GP10
    public static final int PIN_GP11 = 12;  // SPI1 RX / GP11
    public static final int PIN_GP12 = 13;  // SPI1 CSn / GP12
    public static final int PIN_GP13 = 14;  // SPI1 SCK / GP13
    public static final int PIN_GP14 = 15;  // PWM14 / GP14
    public static final int PIN_GP15 = 16;  // PWM15 / GP15
    public static final int PIN_GP16 = 17;  // UART1 TX / GP16
    public static final int PIN_GP17 = 18;  // UART1 RX / GP17
    public static final int PIN_GP18 = 19;  // I2C0 SDA / GP18
    public static final int PIN_GP19 = 20;  // I2C0 SCL / GP19
    public static final int PIN_GP20 = 21;  // I2C1 SDA / GP20
    public static final int PIN_GP21 = 22;  // I2C1 SCL / GP21
    public static final int PIN_GP22 = 23;  // GP22
    public static final int PIN_RUN = 24;  // Run enable
    public static final int PIN_AGND = 25;  // Analog ground
    public static final int PIN_GP26 = 26;  // ADC0 / GP26
    public static final int PIN_GP27 = 27;  // ADC1 / GP27
    public static final int PIN_GP28 = 28;  // ADC2 / GP28
    public static final int PIN_ADC_VREF = 29;  // ADC voltage reference
    public static final int PIN_GP35 = 30;  // GP35
    public static final int PIN_GP34 = 31;  // GP34
    public static final int PIN_GP33 = 32;  // GP33
    public static final int PIN_GP36 = 37;  // GP36
    public static final int PIN_GP37 = 38;  // GP37
    public static final int PIN_GP38 = 39;  // GP38
    public static final int PIN_GP39 = 40;  // GP39
    public static final int PIN_GP40 = 41;  // GP40
    public static final int PIN_GP41 = 42;  // GP41
    public static final int PIN_SWCLK = 43;  // SWD Clock
    public static final int PIN_SWDIO = 44;  // SWD Data I/O

    public static native void rp2040_init();
}
