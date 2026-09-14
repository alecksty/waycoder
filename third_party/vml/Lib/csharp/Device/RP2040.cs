using System;

namespace VML.Device.RaspberryPi.RP2040
{
    /// <summary>
    /// RP2040 寄存器定义
    /// 生成自: Raspberry Pi/RP/RP2040
    /// 版本: 1.0
    /// </summary>
    public static class RP2040
    {
        // CPU架构: ARM-Cortex-M0+, 32位, 12000000 Hz

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
        public const int XPSR_ADDR = 0x00000040;
        public static unsafe uint* xPSR => (uint*)0x00000040;
        public const int XPSR_N = 31;  // Negative Flag
        public const int XPSR_Z = 30;  // Zero Flag
        public const int XPSR_C = 29;  // Carry Flag
        public const int XPSR_V = 28;  // Overflow Flag
        public const int XPSR_Q = 27;  // Saturation Flag
        public const int XPSR_ICI = 0;  // ICI execution state
        public const int XPSR_IT = 0;  // If-Then execution state
        public const int XPSR_T = 24;  // Thumb bit
        public const int XPSR_IPSR = 0;  // Exception number

        // Priority Mask Register
        public const int PRIMASK_ADDR = 0xE0000E20;
        public static unsafe uint* PRIMASK => (uint*)0xE0000E20;

        // Control Register
        public const int CONTROL_ADDR = 0xE0000E24;
        public static unsafe uint* CONTROL => (uint*)0xE0000E24;

        // Fault Mask Register
        public const int FAULTMASK_ADDR = 0xE0000E28;
        public static unsafe uint* FAULTMASK => (uint*)0xE0000E28;

        // 内存段定义
        // ROM (bootloader)
        public const int ROM_START = 0x00000000;
        public const int ROM_END = 0x00001000;
        public const int ROM_SIZE = 4096;

        // SRAM0 (16KB)
        public const int SRAM0_START = 0x20000000;
        public const int SRAM0_END = 0x20003FFF;
        public const int SRAM0_SIZE = 16384;

        // SRAM1 (16KB)
        public const int SRAM1_START = 0x20004000;
        public const int SRAM1_END = 0x20007FFF;
        public const int SRAM1_SIZE = 16384;

        // SRAM2 (16KB)
        public const int SRAM2_START = 0x20008000;
        public const int SRAM2_END = 0x2000BFFF;
        public const int SRAM2_SIZE = 16384;

        // SRAM3 (16KB)
        public const int SRAM3_START = 0x2000C000;
        public const int SRAM3_END = 0x2000FFFF;
        public const int SRAM3_SIZE = 16384;

        // SRAM4 (16KB)
        public const int SRAM4_START = 0x20010000;
        public const int SRAM4_END = 0x20013FFF;
        public const int SRAM4_SIZE = 16384;

        // APB Peripherals
        public const int APB_START = 0x40000000;
        public const int APB_END = 0x400FFFFF;
        public const int APB_SIZE = 1048576;

        // AHB Peripherals
        public const int AHB_START = 0x50000000;
        public const int AHB_END = 0x500FFFFF;
        public const int AHB_SIZE = 1048576;

        // 外设定义
        // IO Bank 0
        public const int IO_BANK0_BASE = 0x40014000;
        public static unsafe uint* IO_BANK0_GPIO0_STATUS => (uint*)0x40014000;
        public static unsafe uint* IO_BANK0_GPIO0_CTRL => (uint*)0x40014004;
        public static unsafe uint* IO_BANK0_GPIO1_STATUS => (uint*)0x40014008;
        public static unsafe uint* IO_BANK0_GPIO1_CTRL => (uint*)0x4001400C;
        public static unsafe uint* IO_BANK0_GPIO2_STATUS => (uint*)0x40014010;
        public static unsafe uint* IO_BANK0_GPIO2_CTRL => (uint*)0x40014014;
        public static unsafe uint* IO_BANK0_GPIO3_STATUS => (uint*)0x40014018;
        public static unsafe uint* IO_BANK0_GPIO3_CTRL => (uint*)0x4001401C;
        public static unsafe uint* IO_BANK0_GPIO4_STATUS => (uint*)0x40014020;
        public static unsafe uint* IO_BANK0_GPIO4_CTRL => (uint*)0x40014024;
        public static unsafe uint* IO_BANK0_GPIO5_STATUS => (uint*)0x40014028;
        public static unsafe uint* IO_BANK0_GPIO5_CTRL => (uint*)0x4001402C;
        public static unsafe uint* IO_BANK0_GPIO6_STATUS => (uint*)0x40014030;
        public static unsafe uint* IO_BANK0_GPIO6_CTRL => (uint*)0x40014034;
        public static unsafe uint* IO_BANK0_GPIO7_STATUS => (uint*)0x40014038;
        public static unsafe uint* IO_BANK0_GPIO7_CTRL => (uint*)0x4001403C;
        public static unsafe uint* IO_BANK0_GPIO8_STATUS => (uint*)0x40014040;
        public static unsafe uint* IO_BANK0_GPIO8_CTRL => (uint*)0x40014044;
        public static unsafe uint* IO_BANK0_GPIO9_STATUS => (uint*)0x40014048;
        public static unsafe uint* IO_BANK0_GPIO9_CTRL => (uint*)0x4001404C;
        public static unsafe uint* IO_BANK0_GPIO10_STATUS => (uint*)0x40014050;
        public static unsafe uint* IO_BANK0_GPIO10_CTRL => (uint*)0x40014054;
        public static unsafe uint* IO_BANK0_GPIO11_STATUS => (uint*)0x40014058;
        public static unsafe uint* IO_BANK0_GPIO11_CTRL => (uint*)0x4001405C;
        public static unsafe uint* IO_BANK0_GPIO12_STATUS => (uint*)0x40014060;
        public static unsafe uint* IO_BANK0_GPIO12_CTRL => (uint*)0x40014064;
        public static unsafe uint* IO_BANK0_GPIO13_STATUS => (uint*)0x40014068;
        public static unsafe uint* IO_BANK0_GPIO13_CTRL => (uint*)0x4001406C;
        public static unsafe uint* IO_BANK0_GPIO14_STATUS => (uint*)0x40014070;
        public static unsafe uint* IO_BANK0_GPIO14_CTRL => (uint*)0x40014074;
        public static unsafe uint* IO_BANK0_GPIO15_STATUS => (uint*)0x40014078;
        public static unsafe uint* IO_BANK0_GPIO15_CTRL => (uint*)0x4001407C;
        public static unsafe uint* IO_BANK0_GPIO16_STATUS => (uint*)0x40014080;
        public static unsafe uint* IO_BANK0_GPIO16_CTRL => (uint*)0x40014084;
        public static unsafe uint* IO_BANK0_GPIO17_STATUS => (uint*)0x40014088;
        public static unsafe uint* IO_BANK0_GPIO17_CTRL => (uint*)0x4001408C;
        public static unsafe uint* IO_BANK0_GPIO18_STATUS => (uint*)0x40014090;
        public static unsafe uint* IO_BANK0_GPIO18_CTRL => (uint*)0x40014094;
        public static unsafe uint* IO_BANK0_GPIO19_STATUS => (uint*)0x40014098;
        public static unsafe uint* IO_BANK0_GPIO19_CTRL => (uint*)0x4001409C;
        public static unsafe uint* IO_BANK0_GPIO20_STATUS => (uint*)0x400140A0;
        public static unsafe uint* IO_BANK0_GPIO20_CTRL => (uint*)0x400140A4;
        public static unsafe uint* IO_BANK0_GPIO21_STATUS => (uint*)0x400140A8;
        public static unsafe uint* IO_BANK0_GPIO21_CTRL => (uint*)0x400140AC;
        public static unsafe uint* IO_BANK0_GPIO22_STATUS => (uint*)0x400140B0;
        public static unsafe uint* IO_BANK0_GPIO22_CTRL => (uint*)0x400140B4;
        public static unsafe uint* IO_BANK0_GPIO23_STATUS => (uint*)0x400140B8;
        public static unsafe uint* IO_BANK0_GPIO23_CTRL => (uint*)0x400140BC;
        public static unsafe uint* IO_BANK0_GPIO24_STATUS => (uint*)0x400140C0;
        public static unsafe uint* IO_BANK0_GPIO24_CTRL => (uint*)0x400140C4;
        public static unsafe uint* IO_BANK0_GPIO25_STATUS => (uint*)0x400140C8;
        public static unsafe uint* IO_BANK0_GPIO25_CTRL => (uint*)0x400140CC;
        public static unsafe uint* IO_BANK0_GPIO26_STATUS => (uint*)0x400140D0;
        public static unsafe uint* IO_BANK0_GPIO26_CTRL => (uint*)0x400140D4;
        public static unsafe uint* IO_BANK0_GPIO27_STATUS => (uint*)0x400140D8;
        public static unsafe uint* IO_BANK0_GPIO27_CTRL => (uint*)0x400140DC;
        public static unsafe uint* IO_BANK0_GPIO28_STATUS => (uint*)0x400140E0;
        public static unsafe uint* IO_BANK0_GPIO28_CTRL => (uint*)0x400140E4;
        public static unsafe uint* IO_BANK0_GPIO29_STATUS => (uint*)0x400140E8;
        public static unsafe uint* IO_BANK0_GPIO29_CTRL => (uint*)0x400140EC;
        public static unsafe uint* IO_BANK0_INTR => (uint*)0x400140F0;
        public static unsafe uint* IO_BANK0_PROC0_INTE => (uint*)0x400140F4;
        public static unsafe uint* IO_BANK0_PROC1_INTE => (uint*)0x400140F8;
        public static unsafe uint* IO_BANK0_PROC0_INTF => (uint*)0x400140FC;
        public static unsafe uint* IO_BANK0_PROC1_INTF => (uint*)0x40014100;
        public static unsafe uint* IO_BANK0_PROC0_INTS => (uint*)0x40014104;
        public static unsafe uint* IO_BANK0_PROC1_INTS => (uint*)0x40014108;
        public static unsafe uint* IO_BANK0_DORMANT_WAKE_INTE => (uint*)0x4001410C;
        public static unsafe uint* IO_BANK0_DORMANT_WAKE_INTF => (uint*)0x40014110;
        public static unsafe uint* IO_BANK0_DORMANT_WAKE_INTS => (uint*)0x40014114;

        // Pads
        public const int PADS_BASE = 0x4001E000;
        public static unsafe uint* PADS_GPIO_VOLT => (uint*)0x4001E0E0;

        // SIO (Single-cycle I/O)
        public const int SIO_BASE = 0xD0000000;
        public static unsafe uint* SIO_CPUID => (uint*)0xD0000000;
        public static unsafe uint* SIO_GPIO_OUT => (uint*)0xD0000004;
        public static unsafe uint* SIO_GPIO_OUT_SET => (uint*)0xD0000008;
        public static unsafe uint* SIO_GPIO_OUT_CLR => (uint*)0xD000000C;
        public static unsafe uint* SIO_GPIO_OUT_XOR => (uint*)0xD0000010;
        public static unsafe uint* SIO_GPIO_OE => (uint*)0xD0000014;
        public static unsafe uint* SIO_GPIO_OE_SET => (uint*)0xD0000018;
        public static unsafe uint* SIO_GPIO_OE_CLR => (uint*)0xD000001C;
        public static unsafe uint* SIO_GPIO_OE_XOR => (uint*)0xD0000020;
        public static unsafe uint* SIO_GPIO_IN => (uint*)0xD0000024;
        public static unsafe uint* SIO_FIFO_ST => (uint*)0xD0000040;
        public static unsafe uint* SIO_FIFO_WR => (uint*)0xD0000044;
        public static unsafe uint* SIO_FIFO_RD => (uint*)0xD0000048;
        public static unsafe uint* SIO_SPINLOCK_ST => (uint*)0xD000004C;
        public static unsafe uint* SIO_INTERRUPT_ST => (uint*)0xD0000050;

        // UART0
        public const int UART0_BASE = 0x40034000;
        public static unsafe uint* UART0_UARTDR => (uint*)0x40034000;
        public static unsafe uint* UART0_UARTRSR => (uint*)0x40034004;
        public static unsafe uint* UART0_UARTECR => (uint*)0x40034004;
        public static unsafe uint* UART0_UARTFR => (uint*)0x40034018;
        public static unsafe uint* UART0_UARTILPR => (uint*)0x40034020;
        public static unsafe uint* UART0_UARTIBRD => (uint*)0x40034024;
        public static unsafe uint* UART0_UARTFBRD => (uint*)0x40034028;
        public static unsafe uint* UART0_UARTLCR_H => (uint*)0x4003402C;
        public static unsafe uint* UART0_UARTCR => (uint*)0x40034030;
        public static unsafe uint* UART0_UARTIFLS => (uint*)0x40034034;
        public static unsafe uint* UART0_UARTIMSC => (uint*)0x40034038;
        public static unsafe uint* UART0_UARTRIS => (uint*)0x4003403C;
        public static unsafe uint* UART0_UARTMIS => (uint*)0x40034040;
        public static unsafe uint* UART0_UARTICR => (uint*)0x40034044;
        public static unsafe uint* UART0_UARTDMACR => (uint*)0x40034048;

        // UART1
        public const int UART1_BASE = 0x40038000;
        public static unsafe uint* UART1_UARTDR => (uint*)0x40038000;
        public static unsafe uint* UART1_UARTRSR => (uint*)0x40038004;
        public static unsafe uint* UART1_UARTFR => (uint*)0x40038018;
        public static unsafe uint* UART1_UARTIBRD => (uint*)0x40038024;
        public static unsafe uint* UART1_UARTFBRD => (uint*)0x40038028;
        public static unsafe uint* UART1_UARTLCR_H => (uint*)0x4003802C;
        public static unsafe uint* UART1_UARTCR => (uint*)0x40038030;
        public static unsafe uint* UART1_UARTIFLS => (uint*)0x40038034;
        public static unsafe uint* UART1_UARTIMSC => (uint*)0x40038038;
        public static unsafe uint* UART1_UARTICR => (uint*)0x40038044;

        // SPI0
        public const int SPI0_BASE = 0x4003C000;
        public static unsafe uint* SPI0_SSPCR0 => (uint*)0x4003C000;
        public static unsafe uint* SPI0_SSPCR1 => (uint*)0x4003C004;
        public static unsafe uint* SPI0_SSPDR => (uint*)0x4003C008;
        public static unsafe uint* SPI0_SSPSR => (uint*)0x4003C00C;
        public static unsafe uint* SPI0_SSPCPSR => (uint*)0x4003C010;
        public static unsafe uint* SPI0_SSPIMSC => (uint*)0x4003C014;
        public static unsafe uint* SPI0_SSPRIS => (uint*)0x4003C018;
        public static unsafe uint* SPI0_SSPMIS => (uint*)0x4003C01C;
        public static unsafe uint* SPI0_SSPICR => (uint*)0x4003C020;
        public static unsafe uint* SPI0_SSPDMACR => (uint*)0x4003C024;

        // SPI1
        public const int SPI1_BASE = 0x4003C000;
        public static unsafe uint* SPI1_SSPCR0 => (uint*)0x4003C000;
        public static unsafe uint* SPI1_SSPCR1 => (uint*)0x4003C004;
        public static unsafe uint* SPI1_SSPDR => (uint*)0x4003C008;
        public static unsafe uint* SPI1_SSPSR => (uint*)0x4003C00C;
        public static unsafe uint* SPI1_SSPCPSR => (uint*)0x4003C010;
        public static unsafe uint* SPI1_SSPIMSC => (uint*)0x4003C014;

        // I2C0
        public const int I2C0_BASE = 0x40044000;
        public static unsafe uint* I2C0_IC_CON => (uint*)0x40044000;
        public static unsafe uint* I2C0_IC_TAR => (uint*)0x40044004;
        public static unsafe uint* I2C0_IC_SAR => (uint*)0x40044008;
        public static unsafe uint* I2C0_IC_DATA_CMD => (uint*)0x40044010;
        public static unsafe uint* I2C0_IC_SS_SCL_HCNT => (uint*)0x40044014;
        public static unsafe uint* I2C0_IC_SS_SCL_LCNT => (uint*)0x40044018;
        public static unsafe uint* I2C0_IC_FS_SCL_HCNT => (uint*)0x4004401C;
        public static unsafe uint* I2C0_IC_FS_SCL_LCNT => (uint*)0x40044020;
        public static unsafe uint* I2C0_IC_RAW_INTR_STAT => (uint*)0x40044024;
        public static unsafe uint* I2C0_IC_ENABLE => (uint*)0x4004402C;
        public static unsafe uint* I2C0_IC_STATUS => (uint*)0x40044030;
        public static unsafe uint* I2C0_IC_TXFLR => (uint*)0x40044034;
        public static unsafe uint* I2C0_IC_RXFLR => (uint*)0x40044038;
        public static unsafe uint* I2C0_IC_TX_ABRT => (uint*)0x4004403C;
        public static unsafe uint* I2C0_IC_DMA_CR => (uint*)0x40044040;
        public static unsafe uint* I2C0_IC_DMA_TDLR => (uint*)0x40044044;
        public static unsafe uint* I2C0_IC_DMA_RDLR => (uint*)0x40044048;

        // I2C1
        public const int I2C1_BASE = 0x40048000;
        public static unsafe uint* I2C1_IC_CON => (uint*)0x40048000;
        public static unsafe uint* I2C1_IC_TAR => (uint*)0x40048004;
        public static unsafe uint* I2C1_IC_ENABLE => (uint*)0x4004802C;
        public static unsafe uint* I2C1_IC_STATUS => (uint*)0x40048030;
        public static unsafe uint* I2C1_IC_TX_ABRT => (uint*)0x4004803C;

        // PWM0
        public const int PWM0_BASE = 0x40050000;
        public static unsafe uint* PWM0_CS => (uint*)0x40050000;
        public static unsafe uint* PWM0_CMPR0 => (uint*)0x40050004;
        public static unsafe uint* PWM0_CMPR1 => (uint*)0x40050008;
        public static unsafe uint* PWM0_CMPR2 => (uint*)0x4005000C;
        public static unsafe uint* PWM0_CMPR3 => (uint*)0x40050010;
        public static unsafe uint* PWM0_CC => (uint*)0x40050014;
        public static unsafe uint* PWM0_TOP => (uint*)0x40050018;
        public static unsafe uint* PWM0_INTR => (uint*)0x4005001C;
        public static unsafe uint* PWM0_INTE => (uint*)0x40050020;
        public static unsafe uint* PWM0_INTF => (uint*)0x40050024;
        public static unsafe uint* PWM0_INTS => (uint*)0x40050028;
        public static unsafe uint* PWM0_PHS0 => (uint*)0x40050034;
        public static unsafe uint* PWM0_PHS1 => (uint*)0x40050038;
        public static unsafe uint* PWM0_PHS2 => (uint*)0x4005003C;
        public static unsafe uint* PWM0_PHS3 => (uint*)0x40050040;
        public static unsafe uint* PWM0_DIV => (uint*)0x40050044;
        public static unsafe uint* PWM0_PHASE => (uint*)0x40050048;

        // PWM1
        public const int PWM1_BASE = 0x40051000;
        public static unsafe uint* PWM1_CS => (uint*)0x40051000;
        public static unsafe uint* PWM1_CMPR0 => (uint*)0x40051004;
        public static unsafe uint* PWM1_CMPR1 => (uint*)0x40051008;
        public static unsafe uint* PWM1_CMPR2 => (uint*)0x4005100C;
        public static unsafe uint* PWM1_CMPR3 => (uint*)0x40051010;
        public static unsafe uint* PWM1_CC => (uint*)0x40051014;
        public static unsafe uint* PWM1_TOP => (uint*)0x40051018;
        public static unsafe uint* PWM1_DIV => (uint*)0x40051044;

        // ADC
        public const int ADC_BASE = 0x4004C000;
        public static unsafe uint* ADC_ADC_CS => (uint*)0x4004C000;
        public static unsafe uint* ADC_ADC_RESULT => (uint*)0x4004C004;
        public static unsafe uint* ADC_ADC_FCS => (uint*)0x4004C008;
        public static unsafe uint* ADC_ADC_FIFO => (uint*)0x4004C00C;
        public static unsafe uint* ADC_ADC_TS => (uint*)0x4004C010;
        public static unsafe uint* ADC_ADC_OFFSET => (uint*)0x4004C014;
        public static unsafe uint* ADC_ADC_TRIG => (uint*)0x4004C018;

        // Timer0
        public const int TIMER0_BASE = 0x40054000;
        public static unsafe uint* TIMER0_TIMEHW => (uint*)0x40054000;
        public static unsafe uint* TIMER0_TIMELW => (uint*)0x40054004;
        public static unsafe uint* TIMER0_TIMEHA => (uint*)0x40054008;
        public static unsafe uint* TIMER0_TIMELA => (uint*)0x4005400C;
        public static unsafe uint* TIMER0_TIMERA => (uint*)0x40054010;
        public static unsafe uint* TIMER0_TIMERIQ => (uint*)0x40054014;
        public static unsafe uint* TIMER0_TIMEREAD => (uint*)0x40054018;

        // Timer1
        public const int TIMER1_BASE = 0x40058000;
        public static unsafe uint* TIMER1_TIMEHW => (uint*)0x40058000;
        public static unsafe uint* TIMER1_TIMELW => (uint*)0x40058004;
        public static unsafe uint* TIMER1_TIMEHA => (uint*)0x40058008;
        public static unsafe uint* TIMER1_TIMELA => (uint*)0x4005800C;
        public static unsafe uint* TIMER1_TIMERA => (uint*)0x40058010;

        // RTC
        public const int RTC_BASE = 0x4005C000;
        public static unsafe uint* RTC_RTC_CLKS => (uint*)0x4005C000;
        public static unsafe uint* RTC_RTC_SET => (uint*)0x4005C004;
        public static unsafe uint* RTC_RTC_WR => (uint*)0x4005C008;
        public static unsafe uint* RTC_RTC_DATE => (uint*)0x4005C00C;
        public static unsafe uint* RTC_RTC_TOTAL => (uint*)0x4005C010;
        public static unsafe uint* RTC_RTC_HASH => (uint*)0x4005C014;
        public static unsafe uint* RTC_RTC_RTC => (uint*)0x4005C018;
        public static unsafe uint* RTC_INTR => (uint*)0x4005C01C;
        public static unsafe uint* RTC_INTE => (uint*)0x4005C020;
        public static unsafe uint* RTC_INTF => (uint*)0x4005C024;
        public static unsafe uint* RTC_INTS => (uint*)0x4005C028;

        // Watchdog
        public const int WATCHDOG_BASE = 0x40060000;
        public static unsafe uint* WATCHDOG_WATCHDOG_CTL => (uint*)0x40060000;
        public static unsafe uint* WATCHDOG_WATCHDOG_MOD => (uint*)0x40060004;
        public static unsafe uint* WATCHDOG_WATCHDOG_FR => (uint*)0x40060008;
        public static unsafe uint* WATCHDOG_WATCHDOG_LOAD => (uint*)0x4006000C;

        // USB
        public const int USB_BASE = 0x50100000;
        public static unsafe uint* USB_USB_CTRL => (uint*)0x50100000;
        public static unsafe uint* USB_USB_ADDR => (uint*)0x50100004;
        public static unsafe uint* USB_USB_PWR => (uint*)0x50100008;
        public static unsafe uint* USB_USB_TXFIFO => (uint*)0x50100010;
        public static unsafe uint* USB_USB_RXFIFO => (uint*)0x50100014;
        public static unsafe uint* USB_USB_TXIE => (uint*)0x50100018;
        public static unsafe uint* USB_USB_RXIE => (uint*)0x5010001C;
        public static unsafe uint* USB_USB_IS => (uint*)0x50100020;
        public static unsafe uint* USB_USB_IM => (uint*)0x50100024;
        public static unsafe uint* USB_USB_IE => (uint*)0x50100028;
        public static unsafe uint* USB_USB_REVO => (uint*)0x5010002C;
        public static unsafe uint* USB_USB_EP => (uint*)0x50100030;
        public static unsafe uint* USB_USB_BUFF => (uint*)0x50100034;
        public static unsafe uint* USB_USB_MPS => (uint*)0x50100038;

        // PIO0
        public const int PIO0_BASE = 0x50200000;
        public static unsafe uint* PIO0_CTRL => (uint*)0x50200000;
        public static unsafe uint* PIO0_FSTAT => (uint*)0x50200004;
        public static unsafe uint* PIO0_FDEBUG => (uint*)0x50200008;
        public static unsafe uint* PIO0_FCTRL => (uint*)0x5020000C;
        public static unsafe uint* PIO0_RXF0 => (uint*)0x50200010;
        public static unsafe uint* PIO0_RXF1 => (uint*)0x50200014;
        public static unsafe uint* PIO0_RXF2 => (uint*)0x50200018;
        public static unsafe uint* PIO0_RXF3 => (uint*)0x5020001C;
        public static unsafe uint* PIO0_TXF0 => (uint*)0x50200020;
        public static unsafe uint* PIO0_TXF1 => (uint*)0x50200024;
        public static unsafe uint* PIO0_TXF2 => (uint*)0x50200028;
        public static unsafe uint* PIO0_TXF3 => (uint*)0x5020002C;
        public static unsafe uint* PIO0_IRQ => (uint*)0x50200030;
        public static unsafe uint* PIO0_IRQ_FORCE => (uint*)0x50200034;
        public static unsafe uint* PIO0_IRQ_INTF => (uint*)0x50200038;
        public static unsafe uint* PIO0_IRQ_INTS => (uint*)0x5020003C;
        public static unsafe uint* PIO0_SM0_CLKDIV => (uint*)0x502000C8;
        public static unsafe uint* PIO0_SM0_EXECCTRL => (uint*)0x502000CC;
        public static unsafe uint* PIO0_SM0_SHIFTCTRL => (uint*)0x502000D0;
        public static unsafe uint* PIO0_SM0_ADDR => (uint*)0x502000D4;
        public static unsafe uint* PIO0_SM0_INSTR => (uint*)0x502000D8;
        public static unsafe uint* PIO0_SM0_PINCTRL => (uint*)0x502000DC;

        // PIO1
        public const int PIO1_BASE = 0x50201000;
        public static unsafe uint* PIO1_CTRL => (uint*)0x50201000;
        public static unsafe uint* PIO1_FSTAT => (uint*)0x50201004;
        public static unsafe uint* PIO1_IRQ => (uint*)0x50201030;
        public static unsafe uint* PIO1_SM0_CLKDIV => (uint*)0x502010C8;
        public static unsafe uint* PIO1_SM0_EXECCTRL => (uint*)0x502010CC;
        public static unsafe uint* PIO1_SM0_SHIFTCTRL => (uint*)0x502010D0;
        public static unsafe uint* PIO1_SM0_ADDR => (uint*)0x502010D4;
        public static unsafe uint* PIO1_SM0_INSTR => (uint*)0x502010D8;

        // Clock Manager
        public const int CLOCKS_BASE = 0x40008000;
        public static unsafe uint* CLOCKS_CLK_GP0DIV => (uint*)0x40008000;
        public static unsafe uint* CLOCKS_CLK_GP0CTRL => (uint*)0x40008004;
        public static unsafe uint* CLOCKS_CLK_GP1DIV => (uint*)0x40008008;
        public static unsafe uint* CLOCKS_CLK_GP1CTRL => (uint*)0x4000800C;
        public static unsafe uint* CLOCKS_CLK_GP2DIV => (uint*)0x40008010;
        public static unsafe uint* CLOCKS_CLK_GP2CTRL => (uint*)0x40008014;
        public static unsafe uint* CLOCKS_CLK_REF => (uint*)0x4000801C;
        public static unsafe uint* CLOCKS_CLK_SYS => (uint*)0x40008020;
        public static unsafe uint* CLOCKS_CLK_PERI => (uint*)0x40008024;

        // Crystal Oscillator
        public const int XOSC_BASE = 0x40020000;
        public static unsafe uint* XOSC_XOSC_CTRL => (uint*)0x40020000;
        public static unsafe uint* XOSC_XOSC_STATUS => (uint*)0x40020004;
        public static unsafe uint* XOSC_XOSC_COUNT => (uint*)0x40020008;

        // Ring Oscillator
        public const int ROSC_BASE = 0x40010000;
        public static unsafe uint* ROSC_ROSC_CTRL => (uint*)0x40010000;
        public static unsafe uint* ROSC_ROSC_FREQA => (uint*)0x40010004;
        public static unsafe uint* ROSC_ROSC_FREQB => (uint*)0x40010008;
        public static unsafe uint* ROSC_ROSC_FREQC => (uint*)0x4001000C;
        public static unsafe uint* ROSC_ROSC_FREQD => (uint*)0x40010010;
        public static unsafe uint* ROSC_ROSC_STATUS => (uint*)0x40010014;
        public static unsafe uint* ROSC_ROSC_DR => (uint*)0x40010018;

        // System PLL
        public const int PLL_SYS_BASE = 0x40028000;
        public static unsafe uint* PLL_SYS_PLL_CS => (uint*)0x40028000;
        public static unsafe uint* PLL_SYS_PLL_PWR => (uint*)0x40028004;
        public static unsafe uint* PLL_SYS_PLL_FBDIV => (uint*)0x40028008;
        public static unsafe uint* PLL_SYS_PLL_PRIMARY => (uint*)0x4002800C;
        public static unsafe uint* PLL_SYS_PLL_POSTDIV1 => (uint*)0x40028010;
        public static unsafe uint* PLL_SYS_PLL_POSTDIV2 => (uint*)0x40028014;

        // USB PLL
        public const int PLL_USB_BASE = 0x4002C000;
        public static unsafe uint* PLL_USB_PLL_CS => (uint*)0x4002C000;
        public static unsafe uint* PLL_USB_PLL_PWR => (uint*)0x4002C004;
        public static unsafe uint* PLL_USB_PLL_FBDIV => (uint*)0x4002C008;
        public static unsafe uint* PLL_USB_PLL_PRIMARY => (uint*)0x4002C00C;

        // Resets
        public const int RESETS_BASE = 0x4000C000;
        public static unsafe uint* RESETS_RESET => (uint*)0x4000C000;
        public static unsafe uint* RESETS_RESET_DONE => (uint*)0x4000C004;
        public static unsafe uint* RESETS_WD_RESET => (uint*)0x4000C008;

        // 中断向量定义
        public const int IRQ_RESERVED = 0;  // Reserved
        public const int IRQ_TIMER0_IRQ_0 = 1;  // Timer 0 IRQ 0
        public const int IRQ_TIMER0_IRQ_1 = 2;  // Timer 0 IRQ 1
        public const int IRQ_TIMER1_IRQ_0 = 3;  // Timer 1 IRQ 0
        public const int IRQ_TIMER1_IRQ_1 = 4;  // Timer 1 IRQ 1
        public const int IRQ_TIMER2_IRQ_0 = 5;  // Timer 2 IRQ 0
        public const int IRQ_TIMER2_IRQ_1 = 6;  // Timer 2 IRQ 1
        public const int IRQ_TIMER3_IRQ_0 = 7;  // Timer 3 IRQ 0
        public const int IRQ_TIMER3_IRQ_1 = 8;  // Timer 3 IRQ 1
        public const int IRQ_PWM_IRQ_WRAP = 9;  // PWM IRQ wrap
        public const int IRQ_USB_CTRL_IRQ = 10;  // USB ctrl IRQ
        public const int IRQ_USB_DMA_IRQ = 11;  // USB dma IRQ
        public const int IRQ_USB_VBUS_DETECT = 12;  // USB VBUS detect IRQ
        public const int IRQ_USB_RESUME_IRQ = 13;  // USB resume IRQ
        public const int IRQ_ADC_IRQ_FIFO = 14;  // ADC IRQ FIFO
        public const int IRQ_ADC_IRQ_TRIGGER = 15;  // ADC IRQ trigger
        public const int IRQ_I2C0_IRQ = 16;  // I2C 0 IRQ
        public const int IRQ_I2C1_IRQ = 17;  // I2C 1 IRQ
        public const int IRQ_SPI0_IRQ = 18;  // SPI 0 IRQ
        public const int IRQ_SPI1_IRQ = 19;  // SPI 1 IRQ
        public const int IRQ_UART0_IRQ = 20;  // UART 0 IRQ
        public const int IRQ_UART0_IRQ_TX = 21;  // UART 0 IRQ TX
        public const int IRQ_UART1_IRQ = 22;  // UART 1 IRQ
        public const int IRQ_UART1_IRQ_TX = 23;  // UART 1 IRQ TX
        public const int IRQ_PIO0_IRQ_0 = 24;  // PIO 0 IRQ 0
        public const int IRQ_PIO0_IRQ_1 = 25;  // PIO 0 IRQ 1
        public const int IRQ_PIO1_IRQ_0 = 26;  // PIO 1 IRQ 0
        public const int IRQ_PIO1_IRQ_1 = 27;  // PIO 1 IRQ 1
        public const int IRQ_RTC_IRQ = 28;  // RTC IRQ

        // 引脚定义
        public const int PIN_GP0 = 1;  // UART0 TX / GP0
        public const int PIN_GP1 = 2;  // UART0 RX / GP1
        public const int PIN_GP2 = 3;  // SPI0 TX / GP2
        public const int PIN_GP3 = 4;  // SPI0 RX / GP3
        public const int PIN_GP4 = 5;  // SPI0 CSn / GP4
        public const int PIN_GP5 = 6;  // SPI0 SCK / GP5
        public const int PIN_GP6 = 7;  // PWM6 / GP6
        public const int PIN_GP7 = 8;  // PWM7 / GP7
        public const int PIN_GP8 = 9;  // PWM8 / GP8
        public const int PIN_GP9 = 10;  // PWM9 / GP9
        public const int PIN_GP10 = 11;  // SPI1 TX / GP10
        public const int PIN_GP11 = 12;  // SPI1 RX / GP11
        public const int PIN_GP12 = 13;  // SPI1 CSn / GP12
        public const int PIN_GP13 = 14;  // SPI1 SCK / GP13
        public const int PIN_GP14 = 15;  // PWM14 / GP14
        public const int PIN_GP15 = 16;  // PWM15 / GP15
        public const int PIN_GP16 = 17;  // UART1 TX / GP16
        public const int PIN_GP17 = 18;  // UART1 RX / GP17
        public const int PIN_GP18 = 19;  // I2C0 SDA / GP18
        public const int PIN_GP19 = 20;  // I2C0 SCL / GP19
        public const int PIN_GP20 = 21;  // I2C1 SDA / GP20
        public const int PIN_GP21 = 22;  // I2C1 SCL / GP21
        public const int PIN_GP22 = 23;  // GP22
        public const int PIN_RUN = 24;  // Run enable
        public const int PIN_AGND = 25;  // Analog ground
        public const int PIN_GP26 = 26;  // ADC0 / GP26
        public const int PIN_GP27 = 27;  // ADC1 / GP27
        public const int PIN_GP28 = 28;  // ADC2 / GP28
        public const int PIN_ADC_VREF = 29;  // ADC voltage reference
        public const int PIN_GP35 = 30;  // GP35
        public const int PIN_GP34 = 31;  // GP34
        public const int PIN_GP33 = 32;  // GP33
        public const int PIN_GP36 = 37;  // GP36
        public const int PIN_GP37 = 38;  // GP37
        public const int PIN_GP38 = 39;  // GP38
        public const int PIN_GP39 = 40;  // GP39
        public const int PIN_GP40 = 41;  // GP40
        public const int PIN_GP41 = 42;  // GP41
        public const int PIN_SWCLK = 43;  // SWD Clock
        public const int PIN_SWDIO = 44;  // SWD Data I/O

        public static void rp2040_init()
        {
            // 硬件初始化代码
        }
    }
}
