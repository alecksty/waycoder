using System;

namespace VML.Device.NordicSemiconductor.nRF52840
{
    /// <summary>
    /// nRF52840 寄存器定义
    /// 生成自: Nordic Semiconductor/nRF52/nRF52840
    /// 版本: 1.0
    /// </summary>
    public static class nRF52840
    {
        // CPU架构: ARM-Cortex-M4F, 32位, 32000000 Hz

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
        public const int XPSR_ICI1 = 0;  // Interrupt Continue State
        public const int XPSR_GE = 0;  // Greater than or Equal
        public const int XPSR_IT = 0;  // If-Then execution state
        public const int XPSR_T = 24;  // Thumb bit
        public const int XPSR_IPSR = 0;  // Exception number

        // Priority Mask Register
        public const int PRIMASK_ADDR = 0xE0000E20;
        public static unsafe uint* PRIMASK => (uint*)0xE0000E20;

        // Base Priority Register
        public const int BASEPRI_ADDR = 0xE0000E24;
        public static unsafe uint* BASEPRI => (uint*)0xE0000E24;

        // Fault Mask Register
        public const int FAULTMASK_ADDR = 0xE0000E28;
        public static unsafe uint* FAULTMASK => (uint*)0xE0000E28;

        // Control Register
        public const int CONTROL_ADDR = 0xE0000E2C;
        public static unsafe uint* CONTROL => (uint*)0xE0000E2C;

        // FPU Status Control
        public const int FPSCR_ADDR = 0xE0000EF34;
        public static unsafe uint* FPSCR => (uint*)0xE0000EF34;

        // FPU Register S0
        public const int S0_ADDR = 0xE0000EF00;
        public static unsafe uint* S0 => (uint*)0xE0000EF00;

        // FPU Register S1
        public const int S1_ADDR = 0xE0000EF04;
        public static unsafe uint* S1 => (uint*)0xE0000EF04;

        // FPU Register S2
        public const int S2_ADDR = 0xE0000EF08;
        public static unsafe uint* S2 => (uint*)0xE0000EF08;

        // FPU Register S3
        public const int S3_ADDR = 0xE0000EF0C;
        public static unsafe uint* S3 => (uint*)0xE0000EF0C;

        // FPU Register S4
        public const int S4_ADDR = 0xE0000EF10;
        public static unsafe uint* S4 => (uint*)0xE0000EF10;

        // FPU Register S5
        public const int S5_ADDR = 0xE0000EF14;
        public static unsafe uint* S5 => (uint*)0xE0000EF14;

        // FPU Register S6
        public const int S6_ADDR = 0xE0000EF18;
        public static unsafe uint* S6 => (uint*)0xE0000EF18;

        // FPU Register S7
        public const int S7_ADDR = 0xE0000EF1C;
        public static unsafe uint* S7 => (uint*)0xE0000EF1C;

        // FPU Register S8
        public const int S8_ADDR = 0xE0000EF20;
        public static unsafe uint* S8 => (uint*)0xE0000EF20;

        // FPU Register S9
        public const int S9_ADDR = 0xE0000EF24;
        public static unsafe uint* S9 => (uint*)0xE0000EF24;

        // FPU Register S10
        public const int S10_ADDR = 0xE0000EF28;
        public static unsafe uint* S10 => (uint*)0xE0000EF28;

        // FPU Register S11
        public const int S11_ADDR = 0xE0000EF2C;
        public static unsafe uint* S11 => (uint*)0xE0000EF2C;

        // FPU Register S12
        public const int S12_ADDR = 0xE0000EF30;
        public static unsafe uint* S12 => (uint*)0xE0000EF30;

        // FPU Register S13
        public const int S13_ADDR = 0xE0000EF34;
        public static unsafe uint* S13 => (uint*)0xE0000EF34;

        // FPU Register S14
        public const int S14_ADDR = 0xE0000EF38;
        public static unsafe uint* S14 => (uint*)0xE0000EF38;

        // FPU Register S15
        public const int S15_ADDR = 0xE0000EF3C;
        public static unsafe uint* S15 => (uint*)0xE0000EF3C;

        // FPU Register S16
        public const int S16_ADDR = 0xE0000EF40;
        public static unsafe uint* S16 => (uint*)0xE0000EF40;

        // FPU Register S17
        public const int S17_ADDR = 0xE0000EF44;
        public static unsafe uint* S17 => (uint*)0xE0000EF44;

        // FPU Register S18
        public const int S18_ADDR = 0xE0000EF48;
        public static unsafe uint* S18 => (uint*)0xE0000EF48;

        // FPU Register S19
        public const int S19_ADDR = 0xE0000EF4C;
        public static unsafe uint* S19 => (uint*)0xE0000EF4C;

        // FPU Register S20
        public const int S20_ADDR = 0xE0000EF50;
        public static unsafe uint* S20 => (uint*)0xE0000EF50;

        // FPU Register S21
        public const int S21_ADDR = 0xE0000EF54;
        public static unsafe uint* S21 => (uint*)0xE0000EF54;

        // FPU Register S22
        public const int S22_ADDR = 0xE0000EF58;
        public static unsafe uint* S22 => (uint*)0xE0000EF58;

        // FPU Register S23
        public const int S23_ADDR = 0xE0000EF5C;
        public static unsafe uint* S23 => (uint*)0xE0000EF5C;

        // FPU Register S24
        public const int S24_ADDR = 0xE0000EF60;
        public static unsafe uint* S24 => (uint*)0xE0000EF60;

        // FPU Register S25
        public const int S25_ADDR = 0xE0000EF64;
        public static unsafe uint* S25 => (uint*)0xE0000EF64;

        // FPU Register S26
        public const int S26_ADDR = 0xE0000EF68;
        public static unsafe uint* S26 => (uint*)0xE0000EF68;

        // FPU Register S27
        public const int S27_ADDR = 0xE0000EF6C;
        public static unsafe uint* S27 => (uint*)0xE0000EF6C;

        // FPU Register S28
        public const int S28_ADDR = 0xE0000EF70;
        public static unsafe uint* S28 => (uint*)0xE0000EF70;

        // FPU Register S29
        public const int S29_ADDR = 0xE0000EF74;
        public static unsafe uint* S29 => (uint*)0xE0000EF74;

        // FPU Register S30
        public const int S30_ADDR = 0xE0000EF78;
        public static unsafe uint* S30 => (uint*)0xE0000EF78;

        // FPU Register S31
        public const int S31_ADDR = 0xE0000EF7C;
        public static unsafe uint* S31 => (uint*)0xE0000EF7C;

        // 内存段定义
        // Flash (1MB)
        public const int FLASH_START = 0x00000000;
        public const int FLASH_END = 0x0FFFFF;
        public const int FLASH_SIZE = 1048576;

        // SRAM (256KB)
        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x2003FFFF;
        public const int SRAM_SIZE = 262144;

        // SRAM Low (128KB)
        public const int SRAM_LOW_START = 0x20000000;
        public const int SRAM_LOW_END = 0x2001FFFF;
        public const int SRAM_LOW_SIZE = 131072;

        // SRAM High (128KB)
        public const int SRAM_HIGH_START = 0x20020000;
        public const int SRAM_HIGH_END = 0x2003FFFF;
        public const int SRAM_HIGH_SIZE = 131072;

        // Factory Information Configuration
        public const int FICR_START = 0x10000000;
        public const int FICR_END = 0x10001000;
        public const int FICR_SIZE = 4096;

        // User Information Configuration
        public const int UICR_START = 0x10001000;
        public const int UICR_END = 0x10001000;
        public const int UICR_SIZE = 4096;

        // Peripheral Space
        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x50000000;
        public const int PERIPHERAL_SIZE = 268435456;

        // 外设定义
        // GPIO
        public const int GPIO_BASE = 0x50000000;
        public static unsafe uint* GPIO_OUT => (uint*)0x50000000;
        public static unsafe uint* GPIO_OUTSET => (uint*)0x50000004;
        public static unsafe uint* GPIO_OUTCLR => (uint*)0x50000008;
        public static unsafe uint* GPIO_IN => (uint*)0x5000000C;
        public static unsafe uint* GPIO_DIR => (uint*)0x50000010;
        public static unsafe uint* GPIO_DIRSET => (uint*)0x50000014;
        public static unsafe uint* GPIO_DIRCLR => (uint*)0x50000018;
        public static unsafe uint* GPIO_PIN_CNF0 => (uint*)0x50000300;
        public static unsafe uint* GPIO_PIN_CNF1 => (uint*)0x50000304;
        public static unsafe uint* GPIO_PIN_CNF2 => (uint*)0x50000308;
        public static unsafe uint* GPIO_PIN_CNF3 => (uint*)0x5000030C;
        public static unsafe uint* GPIO_PIN_CNF4 => (uint*)0x50000310;
        public static unsafe uint* GPIO_PIN_CNF5 => (uint*)0x50000314;
        public static unsafe uint* GPIO_PIN_CNF6 => (uint*)0x50000318;
        public static unsafe uint* GPIO_PIN_CNF7 => (uint*)0x5000031C;
        public static unsafe uint* GPIO_PIN_CNF8 => (uint*)0x50000320;
        public static unsafe uint* GPIO_PIN_CNF9 => (uint*)0x50000324;
        public static unsafe uint* GPIO_PIN_CNF10 => (uint*)0x50000328;
        public static unsafe uint* GPIO_PIN_CNF11 => (uint*)0x5000032C;
        public static unsafe uint* GPIO_PIN_CNF12 => (uint*)0x50000330;
        public static unsafe uint* GPIO_PIN_CNF13 => (uint*)0x50000334;
        public static unsafe uint* GPIO_PIN_CNF14 => (uint*)0x50000338;
        public static unsafe uint* GPIO_PIN_CNF15 => (uint*)0x5000033C;
        public static unsafe uint* GPIO_PIN_CNF16 => (uint*)0x50000340;
        public static unsafe uint* GPIO_PIN_CNF17 => (uint*)0x50000344;
        public static unsafe uint* GPIO_PIN_CNF18 => (uint*)0x50000348;
        public static unsafe uint* GPIO_PIN_CNF19 => (uint*)0x5000034C;
        public static unsafe uint* GPIO_PIN_CNF20 => (uint*)0x50000350;
        public static unsafe uint* GPIO_PIN_CNF21 => (uint*)0x50000354;
        public static unsafe uint* GPIO_PIN_CNF22 => (uint*)0x50000358;
        public static unsafe uint* GPIO_PIN_CNF23 => (uint*)0x5000035C;
        public static unsafe uint* GPIO_PIN_CNF24 => (uint*)0x50000360;
        public static unsafe uint* GPIO_PIN_CNF25 => (uint*)0x50000364;
        public static unsafe uint* GPIO_PIN_CNF26 => (uint*)0x50000368;
        public static unsafe uint* GPIO_PIN_CNF27 => (uint*)0x5000036C;
        public static unsafe uint* GPIO_PIN_CNF28 => (uint*)0x50000370;
        public static unsafe uint* GPIO_PIN_CNF29 => (uint*)0x50000374;
        public static unsafe uint* GPIO_PIN_CNF30 => (uint*)0x50000378;
        public static unsafe uint* GPIO_PIN_CNF31 => (uint*)0x5000037C;

        // UART0
        public const int UART0_BASE = 0x40002000;
        public static unsafe uint* UART0_TASKS_STARTRX => (uint*)0x40002000;
        public static unsafe uint* UART0_TASKS_STARTTX => (uint*)0x40002004;
        public static unsafe uint* UART0_TASKS_STOPRX => (uint*)0x40002008;
        public static unsafe uint* UART0_TASKS_STOPTX => (uint*)0x4000200C;
        public static unsafe uint* UART0_SUBSCRIBED_STARTRX => (uint*)0x40002010;
        public static unsafe uint* UART0_SUBSCRIBED_STARTTX => (uint*)0x40002014;
        public static unsafe uint* UART0_EVENTS_CTS => (uint*)0x40002100;
        public static unsafe uint* UART0_EVENTS_NCTS => (uint*)0x40002104;
        public static unsafe uint* UART0_EVENTS_RXDRDY => (uint*)0x40002108;
        public static unsafe uint* UART0_EVENTS_TXDRDY => (uint*)0x4000210C;
        public static unsafe uint* UART0_EVENTS_ERROR => (uint*)0x40002110;
        public static unsafe uint* UART0_RXD => (uint*)0x40002518;
        public static unsafe uint* UART0_TXD => (uint*)0x4000251C;
        public static unsafe uint* UART0_BAUDRATE => (uint*)0x40002524;
        public static unsafe uint* UART0_CONFIG => (uint*)0x4000256C;
        public static unsafe uint* UART0_INTEN => (uint*)0x40002700;
        public static unsafe uint* UART0_INTENSET => (uint*)0x40002704;
        public static unsafe uint* UART0_INTENCLR => (uint*)0x40002708;

        // UART1
        public const int UART1_BASE = 0x40003000;
        public static unsafe uint* UART1_TASKS_STARTRX => (uint*)0x40003000;
        public static unsafe uint* UART1_TASKS_STARTTX => (uint*)0x40003004;
        public static unsafe uint* UART1_EVENTS_RXDRDY => (uint*)0x40003108;
        public static unsafe uint* UART1_EVENTS_TXDRDY => (uint*)0x4000310C;
        public static unsafe uint* UART1_RXD => (uint*)0x40003518;
        public static unsafe uint* UART1_TXD => (uint*)0x4000351C;
        public static unsafe uint* UART1_BAUDRATE => (uint*)0x40003524;

        // SPI0
        public const int SPI0_BASE = 0x40003000;
        public static unsafe uint* SPI0_EVENTS_READY => (uint*)0x40003108;
        public static unsafe uint* SPI0_RXD => (uint*)0x40003518;
        public static unsafe uint* SPI0_TXD => (uint*)0x4000351C;
        public static unsafe uint* SPI0_FREQUENCY => (uint*)0x40003524;
        public static unsafe uint* SPI0_CONFIG => (uint*)0x4000356C;
        public static unsafe uint* SPI0_INTEN => (uint*)0x40003700;

        // SPI1
        public const int SPI1_BASE = 0x40004000;
        public static unsafe uint* SPI1_EVENTS_READY => (uint*)0x40004108;
        public static unsafe uint* SPI1_RXD => (uint*)0x40004518;
        public static unsafe uint* SPI1_TXD => (uint*)0x4000451C;
        public static unsafe uint* SPI1_FREQUENCY => (uint*)0x40004524;
        public static unsafe uint* SPI1_CONFIG => (uint*)0x4000456C;

        // SPI2
        public const int SPI2_BASE = 0x40005000;
        public static unsafe uint* SPI2_EVENTS_READY => (uint*)0x40005108;
        public static unsafe uint* SPI2_RXD => (uint*)0x40005518;
        public static unsafe uint* SPI2_TXD => (uint*)0x4000551C;
        public static unsafe uint* SPI2_FREQUENCY => (uint*)0x40005524;

        // I2C0
        public const int I2C0_BASE = 0x40003000;
        public static unsafe uint* I2C0_TASKS_STARTRX => (uint*)0x40003000;
        public static unsafe uint* I2C0_TASKS_STARTTX => (uint*)0x40003008;
        public static unsafe uint* I2C0_TASKS_STOP => (uint*)0x40003014;
        public static unsafe uint* I2C0_EVENTS_DONE => (uint*)0x40003108;
        public static unsafe uint* I2C0_EVENTS_TXDSENT => (uint*)0x4000310C;
        public static unsafe uint* I2C0_EVENTS_ERROR => (uint*)0x40003110;
        public static unsafe uint* I2C0_RXD => (uint*)0x40003518;
        public static unsafe uint* I2C0_TXD => (uint*)0x4000351C;
        public static unsafe uint* I2C0_ADDRESS => (uint*)0x40003524;

        // I2C1
        public const int I2C1_BASE = 0x40004000;
        public static unsafe uint* I2C1_TASKS_STARTRX => (uint*)0x40004000;
        public static unsafe uint* I2C1_TASKS_STARTTX => (uint*)0x40004008;
        public static unsafe uint* I2C1_TASKS_STOP => (uint*)0x40004014;
        public static unsafe uint* I2C1_EVENTS_DONE => (uint*)0x40004108;
        public static unsafe uint* I2C1_RXD => (uint*)0x40004518;
        public static unsafe uint* I2C1_TXD => (uint*)0x4000451C;
        public static unsafe uint* I2C1_ADDRESS => (uint*)0x40004524;

        // Timer0
        public const int TIMER0_BASE = 0x40008000;
        public static unsafe uint* TIMER0_TASKS_START => (uint*)0x40008000;
        public static unsafe uint* TIMER0_TASKS_STOP => (uint*)0x40008004;
        public static unsafe uint* TIMER0_TASKS_COUNT => (uint*)0x40008008;
        public static unsafe uint* TIMER0_TASKS_CLEAR => (uint*)0x4000800C;
        public static unsafe uint* TIMER0_CC0 => (uint*)0x40008400;
        public static unsafe uint* TIMER0_CC1 => (uint*)0x40008404;
        public static unsafe uint* TIMER0_CC2 => (uint*)0x40008408;
        public static unsafe uint* TIMER0_CC3 => (uint*)0x4000840C;
        public static unsafe uint* TIMER0_SHORTS => (uint*)0x40008200;
        public static unsafe uint* TIMER0_INTEN => (uint*)0x40008700;
        public static unsafe uint* TIMER0_MODE => (uint*)0x40008510;
        public static unsafe uint* TIMER0_BITMODE => (uint*)0x40008514;

        // Timer1
        public const int TIMER1_BASE = 0x40009000;
        public static unsafe uint* TIMER1_TASKS_START => (uint*)0x40009000;
        public static unsafe uint* TIMER1_TASKS_STOP => (uint*)0x40009004;
        public static unsafe uint* TIMER1_TASKS_COUNT => (uint*)0x40009008;
        public static unsafe uint* TIMER1_TASKS_CLEAR => (uint*)0x4000900C;
        public static unsafe uint* TIMER1_CC0 => (uint*)0x40009400;
        public static unsafe uint* TIMER1_CC1 => (uint*)0x40009404;
        public static unsafe uint* TIMER1_CC2 => (uint*)0x40009408;
        public static unsafe uint* TIMER1_CC3 => (uint*)0x4000940C;
        public static unsafe uint* TIMER1_SHORTS => (uint*)0x40009200;

        // Timer2
        public const int TIMER2_BASE = 0x4000A000;
        public static unsafe uint* TIMER2_TASKS_START => (uint*)0x4000A000;
        public static unsafe uint* TIMER2_TASKS_STOP => (uint*)0x4000A004;
        public static unsafe uint* TIMER2_TASKS_COUNT => (uint*)0x4000A008;
        public static unsafe uint* TIMER2_TASKS_CLEAR => (uint*)0x4000A00C;
        public static unsafe uint* TIMER2_CC0 => (uint*)0x4000A400;
        public static unsafe uint* TIMER2_CC1 => (uint*)0x4000A404;
        public static unsafe uint* TIMER2_CC2 => (uint*)0x4000A408;
        public static unsafe uint* TIMER2_CC3 => (uint*)0x4000A40C;

        // Timer3
        public const int TIMER3_BASE = 0x4000B000;
        public static unsafe uint* TIMER3_TASKS_START => (uint*)0x4000B000;
        public static unsafe uint* TIMER3_TASKS_STOP => (uint*)0x4000B004;
        public static unsafe uint* TIMER3_TASKS_COUNT => (uint*)0x4000B008;
        public static unsafe uint* TIMER3_TASKS_CLEAR => (uint*)0x4000B00C;
        public static unsafe uint* TIMER3_CC0 => (uint*)0x4000B400;
        public static unsafe uint* TIMER3_CC1 => (uint*)0x4000B404;
        public static unsafe uint* TIMER3_CC2 => (uint*)0x4000B408;
        public static unsafe uint* TIMER3_CC3 => (uint*)0x4000B40C;

        // Timer4
        public const int TIMER4_BASE = 0x4000C000;
        public static unsafe uint* TIMER4_TASKS_START => (uint*)0x4000C000;
        public static unsafe uint* TIMER4_TASKS_STOP => (uint*)0x4000C004;
        public static unsafe uint* TIMER4_TASKS_COUNT => (uint*)0x4000C008;
        public static unsafe uint* TIMER4_TASKS_CLEAR => (uint*)0x4000C00C;
        public static unsafe uint* TIMER4_CC0 => (uint*)0x4000C400;
        public static unsafe uint* TIMER4_CC1 => (uint*)0x4000C404;
        public static unsafe uint* TIMER4_CC2 => (uint*)0x4000C408;
        public static unsafe uint* TIMER4_CC3 => (uint*)0x4000C40C;

        // RTC0
        public const int RTC0_BASE = 0x4000B000;
        public static unsafe uint* RTC0_TASKS_START => (uint*)0x4000B000;
        public static unsafe uint* RTC0_TASKS_STOP => (uint*)0x4000B004;
        public static unsafe uint* RTC0_TASKS_TRIGOVRFLW => (uint*)0x4000B010;
        public static unsafe uint* RTC0_EVENTS_TICK => (uint*)0x4000B100;
        public static unsafe uint* RTC0_EVENTS_OVRFLW => (uint*)0x4000B104;
        public static unsafe uint* RTC0_EVENTS_COMPARE0 => (uint*)0x4000B140;
        public static unsafe uint* RTC0_EVENTS_COMPARE1 => (uint*)0x4000B144;
        public static unsafe uint* RTC0_EVENTS_COMPARE2 => (uint*)0x4000B148;
        public static unsafe uint* RTC0_EVENTS_COMPARE3 => (uint*)0x4000B14C;
        public static unsafe uint* RTC0_CC0 => (uint*)0x4000B400;
        public static unsafe uint* RTC0_CC1 => (uint*)0x4000B404;
        public static unsafe uint* RTC0_CC2 => (uint*)0x4000B408;
        public static unsafe uint* RTC0_CC3 => (uint*)0x4000B40C;
        public static unsafe uint* RTC0_CNT => (uint*)0x4000B500;
        public static unsafe uint* RTC0_PRESCALER => (uint*)0x4000B504;
        public static unsafe uint* RTC0_TICK => (uint*)0x4000B510;

        // RTC1
        public const int RTC1_BASE = 0x4000D000;
        public static unsafe uint* RTC1_TASKS_START => (uint*)0x4000D000;
        public static unsafe uint* RTC1_TASKS_STOP => (uint*)0x4000D004;
        public static unsafe uint* RTC1_EVENTS_TICK => (uint*)0x4000D100;
        public static unsafe uint* RTC1_EVENTS_OVRFLW => (uint*)0x4000D104;
        public static unsafe uint* RTC1_EVENTS_COMPARE0 => (uint*)0x4000D140;
        public static unsafe uint* RTC1_EVENTS_COMPARE1 => (uint*)0x4000D144;
        public static unsafe uint* RTC1_CC0 => (uint*)0x4000D400;
        public static unsafe uint* RTC1_CC1 => (uint*)0x4000D404;
        public static unsafe uint* RTC1_CNT => (uint*)0x4000D500;
        public static unsafe uint* RTC1_PRESCALER => (uint*)0x4000D504;

        // PWM0
        public const int PWM0_BASE = 0x4001C000;
        public static unsafe uint* PWM0_TASKS_START => (uint*)0x4001C000;
        public static unsafe uint* PWM0_TASKS_STOP => (uint*)0x4001C004;
        public static unsafe uint* PWM0_TASKS_SEQSTART0 => (uint*)0x4001C008;
        public static unsafe uint* PWM0_TASKS_SEQSTART1 => (uint*)0x4001C00C;
        public static unsafe uint* PWM0_TASKS_NEXTSTEP => (uint*)0x4001C010;
        public static unsafe uint* PWM0_EVENTS_PWMPERIODEND => (uint*)0x4001C108;
        public static unsafe uint* PWM0_EVENTS_LOOPEND => (uint*)0x4001C10C;
        public static unsafe uint* PWM0_EVENTS_SEQEND0 => (uint*)0x4001C110;
        public static unsafe uint* PWM0_EVENTS_SEQEND1 => (uint*)0x4001C114;
        public static unsafe uint* PWM0_SEQ0_PTR => (uint*)0x4001C510;
        public static unsafe uint* PWM0_SEQ1_PTR => (uint*)0x4001C514;
        public static unsafe uint* PWM0_SEQ0_CNT => (uint*)0x4001C528;
        public static unsafe uint* PWM0_SEQ1_CNT => (uint*)0x4001C52C;
        public static unsafe uint* PWM0_SEQ0_REFRESH => (uint*)0x4001C530;
        public static unsafe uint* PWM0_SEQ1_REFRESH => (uint*)0x4001C534;
        public static unsafe uint* PWM0_DECODER => (uint*)0x4001C540;
        public static unsafe uint* PWM0_LOOP => (uint*)0x4001C544;
        public static unsafe uint* PWM0_MODE => (uint*)0x4001C500;
        public static unsafe uint* PWM0_CLKEN => (uint*)0x4001C504;
        public static unsafe uint* PWM0_CNT => (uint*)0x4001C548;

        // PWM1
        public const int PWM1_BASE = 0x4001D000;
        public static unsafe uint* PWM1_TASKS_START => (uint*)0x4001D000;
        public static unsafe uint* PWM1_TASKS_STOP => (uint*)0x4001D004;
        public static unsafe uint* PWM1_SEQ0_PTR => (uint*)0x4001D510;
        public static unsafe uint* PWM1_SEQ1_PTR => (uint*)0x4001D514;
        public static unsafe uint* PWM1_MODE => (uint*)0x4001D500;
        public static unsafe uint* PWM1_CNT => (uint*)0x4001D548;

        // PWM2
        public const int PWM2_BASE = 0x4001E000;
        public static unsafe uint* PWM2_TASKS_START => (uint*)0x4001E000;
        public static unsafe uint* PWM2_SEQ0_PTR => (uint*)0x4001E510;
        public static unsafe uint* PWM2_MODE => (uint*)0x4001E500;

        // PWM3
        public const int PWM3_BASE = 0x4001F000;
        public static unsafe uint* PWM3_TASKS_START => (uint*)0x4001F000;
        public static unsafe uint* PWM3_SEQ0_PTR => (uint*)0x4001F510;
        public static unsafe uint* PWM3_MODE => (uint*)0x4001F500;

        // ADC
        public const int ADC_BASE = 0x40012000;
        public static unsafe uint* ADC_TASKS_START => (uint*)0x40012000;
        public static unsafe uint* ADC_TASKS_STOP => (uint*)0x40012004;
        public static unsafe uint* ADC_EVENTS_DONE => (uint*)0x40012108;
        public static unsafe uint* ADC_EVENTS_RESULTDONE => (uint*)0x4001210C;
        public static unsafe uint* ADC_EVENTS_CALIBRATEDONE => (uint*)0x40012110;
        public static unsafe uint* ADC_EVENTS_CH_LIMITH => (uint*)0x40012114;
        public static unsafe uint* ADC_EVENTS_CH_LIMITL => (uint*)0x40012118;
        public static unsafe uint* ADC_RESULT => (uint*)0x40012400;
        public static unsafe uint* ADC_CH0_CONFIG => (uint*)0x40012510;
        public static unsafe uint* ADC_CH1_CONFIG => (uint*)0x40012514;
        public static unsafe uint* ADC_CH2_CONFIG => (uint*)0x40012518;
        public static unsafe uint* ADC_CH3_CONFIG => (uint*)0x4001251C;
        public static unsafe uint* ADC_CH4_CONFIG => (uint*)0x40012520;
        public static unsafe uint* ADC_CH5_CONFIG => (uint*)0x40012524;
        public static unsafe uint* ADC_CH6_CONFIG => (uint*)0x40012528;
        public static unsafe uint* ADC_CH7_CONFIG => (uint*)0x4001252C;
        public static unsafe uint* ADC_CONFIG => (uint*)0x40012530;
        public static unsafe uint* ADC_TASKS_CALIBRATELOAD => (uint*)0x40012034;
        public static unsafe uint* ADC_INTEN => (uint*)0x40012700;

        // DAC
        public const int DAC_BASE = 0x40013000;
        public static unsafe uint* DAC_TASKS_START => (uint*)0x40013000;
        public static unsafe uint* DAC_TASKS_STOP => (uint*)0x40013004;
        public static unsafe uint* DAC_EVENTS_DONE => (uint*)0x40013108;
        public static unsafe uint* DAC_VALUE => (uint*)0x40013400;
        public static unsafe uint* DAC_CEN => (uint*)0x40013504;

        // Analog Comparator
        public const int COMP_BASE = 0x40013000;
        public static unsafe uint* COMP_TASKS_START => (uint*)0x40013000;
        public static unsafe uint* COMP_TASKS_STOP => (uint*)0x40013004;
        public static unsafe uint* COMP_TASKS_SETTLE => (uint*)0x40013010;
        public static unsafe uint* COMP_EVENTS_READY => (uint*)0x40013108;
        public static unsafe uint* COMP_EVENTS_DOWN => (uint*)0x4001310C;
        public static unsafe uint* COMP_EVENTS_UP => (uint*)0x40013110;
        public static unsafe uint* COMP_EVENTS_CROSS => (uint*)0x40013114;
        public static unsafe uint* COMP_RESULT => (uint*)0x40013400;
        public static unsafe uint* COMP_EN => (uint*)0x40013500;
        public static unsafe uint* COMP_TASK_MODE => (uint*)0x40013504;
        public static unsafe uint* COMP_REFSEL => (uint*)0x40013508;
        public static unsafe uint* COMP_EXTREFSEL => (uint*)0x4001350C;
        public static unsafe uint* COMP_THD => (uint*)0x40013510;
        public static unsafe uint* COMP_HYST => (uint*)0x40013514;
        public static unsafe uint* COMP_SPEED => (uint*)0x40013518;
        public static unsafe uint* COMP_ISOURCE => (uint*)0x4001351C;
        public static unsafe uint* COMP_PSEL => (uint*)0x40013520;
        public static unsafe uint* COMP_NOREF => (uint*)0x40013524;
        public static unsafe uint* COMP_INTEN => (uint*)0x40013700;

        // Quadrature Decoder
        public const int QDEC_BASE = 0x40014000;
        public static unsafe uint* QDEC_TASKS_START => (uint*)0x40014000;
        public static unsafe uint* QDEC_TASKS_STOP => (uint*)0x40014004;
        public static unsafe uint* QDEC_TASKS_RDCLRACC => (uint*)0x40014008;
        public static unsafe uint* QDEC_TASKS_RDCLRDBL => (uint*)0x4001400C;
        public static unsafe uint* QDEC_TASKS_RDCLRPH => (uint*)0x40014010;
        public static unsafe uint* QDEC_EVENTS_READY => (uint*)0x40014108;
        public static unsafe uint* QDEC_EVENTS_DBLRDY => (uint*)0x4001410C;
        public static unsafe uint* QDEC_EVENTS_QCLR => (uint*)0x40014110;
        public static unsafe uint* QDEC_ACC => (uint*)0x40014404;
        public static unsafe uint* QDEC_ACCREAD => (uint*)0x40014408;
        public static unsafe uint* QDEC_DBLINC => (uint*)0x40014410;
        public static unsafe uint* QDEC_DBL => (uint*)0x40014418;
        public static unsafe uint* QDEC_DBLREAD => (uint*)0x4001441C;
        public static unsafe uint* QDEC_PHASE => (uint*)0x40014420;
        public static unsafe uint* QDEC_PHASEREAD => (uint*)0x40014424;
        public static unsafe uint* QDEC_LEFLL => (uint*)0x40014428;
        public static unsafe uint* QDEC_INTEN => (uint*)0x40014700;

        // Event Generators Unit 0
        public const int EGU0_BASE = 0x40014000;
        public static unsafe uint* EGU0_TASKS_TRIGGER0 => (uint*)0x40014000;
        public static unsafe uint* EGU0_TASKS_TRIGGER1 => (uint*)0x40014004;
        public static unsafe uint* EGU0_TASKS_TRIGGER2 => (uint*)0x40014008;
        public static unsafe uint* EGU0_TASKS_TRIGGER3 => (uint*)0x4001400C;
        public static unsafe uint* EGU0_TASKS_TRIGGER4 => (uint*)0x40014010;
        public static unsafe uint* EGU0_TASKS_TRIGGER5 => (uint*)0x40014014;
        public static unsafe uint* EGU0_TASKS_TRIGGER6 => (uint*)0x40014018;
        public static unsafe uint* EGU0_TASKS_TRIGGER7 => (uint*)0x4001401C;
        public static unsafe uint* EGU0_TASKS_TRIGGER8 => (uint*)0x40014020;
        public static unsafe uint* EGU0_TASKS_TRIGGER9 => (uint*)0x40014024;
        public static unsafe uint* EGU0_TASKS_TRIGGER10 => (uint*)0x40014028;
        public static unsafe uint* EGU0_TASKS_TRIGGER11 => (uint*)0x4001402C;
        public static unsafe uint* EGU0_TASKS_TRIGGER12 => (uint*)0x40014030;
        public static unsafe uint* EGU0_TASKS_TRIGGER13 => (uint*)0x40014034;
        public static unsafe uint* EGU0_TASKS_TRIGGER14 => (uint*)0x40014038;
        public static unsafe uint* EGU0_TASKS_TRIGGER15 => (uint*)0x4001403C;
        public static unsafe uint* EGU0_EVENTS_EVENT0 => (uint*)0x40014100;
        public static unsafe uint* EGU0_EVENTS_EVENT1 => (uint*)0x40014104;
        public static unsafe uint* EGU0_EVENTS_EVENT2 => (uint*)0x40014108;
        public static unsafe uint* EGU0_EVENTS_EVENT3 => (uint*)0x4001410C;
        public static unsafe uint* EGU0_EVENTS_EVENT4 => (uint*)0x40014110;
        public static unsafe uint* EGU0_EVENTS_EVENT5 => (uint*)0x40014114;
        public static unsafe uint* EGU0_INTEN => (uint*)0x40014700;

        // Random Number Generator
        public const int RNG_BASE = 0x40006000;
        public static unsafe uint* RNG_TASKS_START => (uint*)0x40006000;
        public static unsafe uint* RNG_TASKS_STOP => (uint*)0x40006004;
        public static unsafe uint* RNG_EVENTS_VALRDY => (uint*)0x40006108;
        public static unsafe uint* RNG_VALUE => (uint*)0x40006400;
        public static unsafe uint* RNG_CONFIG => (uint*)0x40006504;
        public static unsafe uint* RNG_INTEN => (uint*)0x40006700;

        // AES ECB
        public const int AES_BASE = 0x40005000;
        public static unsafe uint* AES_TASKS_START => (uint*)0x40005000;
        public static unsafe uint* AES_TASKS_STOP => (uint*)0x40005004;
        public static unsafe uint* AES_EVENTS_END => (uint*)0x40005108;
        public static unsafe uint* AES_EVENTS_ERROR => (uint*)0x4000510C;
        public static unsafe uint* AES_CRYPTCNTXT => (uint*)0x40005400;
        public static unsafe uint* AES_CRYPTCNTCPY => (uint*)0x40005404;
        public static unsafe uint* AES_CRYPTCMD => (uint*)0x40005500;
        public static unsafe uint* AES_INTEN => (uint*)0x40005700;

        // Cryptocell
        public const int CRYPTO_BASE = 0x4000E000;
        public static unsafe uint* CRYPTO_TASKS_START => (uint*)0x4000E000;
        public static unsafe uint* CRYPTO_TASKS_STOP => (uint*)0x4000E004;
        public static unsafe uint* CRYPTO_EVENTS_DONE => (uint*)0x4000E108;
        public static unsafe uint* CRYPTO_EVENTS_ERROR => (uint*)0x4000E10C;
        public static unsafe uint* CRYPTO_DMA => (uint*)0x4000E400;
        public static unsafe uint* CRYPTO_CMDS => (uint*)0x4000E404;
        public static unsafe uint* CRYPTO_CMDS_AMOUNT => (uint*)0x4000E408;
        public static unsafe uint* CRYPTO_INTENSET => (uint*)0x4000E704;
        public static unsafe uint* CRYPTO_INTENCLR => (uint*)0x4000E708;
        public static unsafe uint* CRYPTO_INTCONTEXT => (uint*)0x4000E710;

        // USB
        public const int USB_BASE = 0x40027000;
        public static unsafe uint* USB_TASKS_STARTUP => (uint*)0x40027000;
        public static unsafe uint* USB_TASKS_SUSPEND => (uint*)0x40027004;
        public static unsafe uint* USB_TASKS_RESUME => (uint*)0x40027008;
        public static unsafe uint* USB_EVENTS_ENDRDY => (uint*)0x40027108;
        public static unsafe uint* USB_EVENTS_SUSPENDED => (uint*)0x4002710C;
        public static unsafe uint* USB_EVENTS_RESUMED => (uint*)0x40027110;
        public static unsafe uint* USB_EVENTS_SOF => (uint*)0x40027114;
        public static unsafe uint* USB_EVENTS_EPOF => (uint*)0x40027118;
        public static unsafe uint* USB_EVENTS_DATA => (uint*)0x4002711C;
        public static unsafe uint* USB_EVENTS_EP0DATADONE => (uint*)0x40027120;
        public static unsafe uint* USB_EVENTS_EP0SETUP => (uint*)0x40027124;
        public static unsafe uint* USB_EVENTS_EP0HALTD => (uint*)0x40027128;
        public static unsafe uint* USB_EVENTS_EP1DMA => (uint*)0x40027134;
        public static unsafe uint* USB_EVENTS_EP2DMA => (uint*)0x40027138;
        public static unsafe uint* USB_EVENTS_EP3DMA => (uint*)0x4002713C;
        public static unsafe uint* USB_EVENTS_EP4DMA => (uint*)0x40027140;
        public static unsafe uint* USB_EVENTS_EP1 => (uint*)0x40027158;
        public static unsafe uint* USB_EVENTS_EP2 => (uint*)0x4002715C;
        public static unsafe uint* USB_EVENTS_EP3 => (uint*)0x40027160;
        public static unsafe uint* USB_EVENTS_EP4 => (uint*)0x40027164;
        public static unsafe uint* USB_EVENTS_EP5 => (uint*)0x40027168;
        public static unsafe uint* USB_EVENTS_EP6 => (uint*)0x4002716C;
        public static unsafe uint* USB_EVENTS_EP7 => (uint*)0x40027170;
        public static unsafe uint* USB_EVENTS_EP8 => (uint*)0x40027174;
        public static unsafe uint* USB_EVENTS_EP9 => (uint*)0x40027178;
        public static unsafe uint* USB_EVENTS_EP10 => (uint*)0x4002717C;
        public static unsafe uint* USB_EVENTS_EP11 => (uint*)0x40027180;
        public static unsafe uint* USB_EVENTS_EP12 => (uint*)0x40027184;
        public static unsafe uint* USB_EVENTS_EP13 => (uint*)0x40027188;
        public static unsafe uint* USB_EVENTS_EP14 => (uint*)0x4002718C;
        public static unsafe uint* USB_EVENTS_EP15 => (uint*)0x40027190;
        public static unsafe uint* USB_USBADDR => (uint*)0x40027500;
        public static unsafe uint* USB_USBREQ => (uint*)0x40027504;
        public static unsafe uint* USB_USBVAL => (uint*)0x40027508;
        public static unsafe uint* USB_USBINDEX => (uint*)0x4002750C;
        public static unsafe uint* USB_USBCONFIG => (uint*)0x40027510;
        public static unsafe uint* USB_EPIN => (uint*)0x40027514;
        public static unsafe uint* USB_EPOUT => (uint*)0x40027518;
        public static unsafe uint* USB_EPLEN => (uint*)0x40027520;
        public static unsafe uint* USB_EPSIZE => (uint*)0x40027524;
        public static unsafe uint* USB_EPDMA => (uint*)0x40027500;
        public static unsafe uint* USB_EPDMA => (uint*)0x40027504;
        public static unsafe uint* USB_INTEN => (uint*)0x40027700;
        public static unsafe uint* USB_INTENSET => (uint*)0x40027704;
        public static unsafe uint* USB_INTENCLR => (uint*)0x40027708;

        // Watchdog Timer
        public const int WDT_BASE = 0x40011000;
        public static unsafe uint* WDT_TASKS_START => (uint*)0x40011000;
        public static unsafe uint* WDT_TASKS_KEEP => (uint*)0x40011004;
        public static unsafe uint* WDT_TASKS_STOP => (uint*)0x40011008;
        public static unsafe uint* WDT_EVENTS_TIMEOUT => (uint*)0x40011108;
        public static unsafe uint* WDT_RUNSTATUS => (uint*)0x40011404;
        public static unsafe uint* WDT_REQSTATUS => (uint*)0x40011408;
        public static unsafe uint* WDT_CRV => (uint*)0x40011504;
        public static unsafe uint* WDT_RCV => (uint*)0x40011508;
        public static unsafe uint* WDT_CONFIG => (uint*)0x4001150C;
        public static unsafe uint* WDT_INTEN => (uint*)0x40011700;

        // Reset
        public const int NRF_RESET_BASE = 0x40000000;
        public static unsafe uint* NRF_RESET_RESET => (uint*)0x40000000;
        public static unsafe uint* NRF_RESET_RESET_FAC => (uint*)0x40000400;
        public static unsafe uint* NRF_RESET_RESET_NFAC => (uint*)0x40000500;

        // Clock
        public const int CLOCK_BASE = 0x40000000;
        public static unsafe uint* CLOCK_TASKS_HFCLKSTART => (uint*)0x40000000;
        public static unsafe uint* CLOCK_TASKS_HFCLKSTOP => (uint*)0x40000004;
        public static unsafe uint* CLOCK_TASKS_LFCLKSTART => (uint*)0x40000008;
        public static unsafe uint* CLOCK_TASKS_LFCLKSTOP => (uint*)0x4000000C;
        public static unsafe uint* CLOCK_TASKS_CAL => (uint*)0x40000010;
        public static unsafe uint* CLOCK_TASKS_CTTO => (uint*)0x40000010;
        public static unsafe uint* CLOCK_EVENTS_HFCLKSTATED => (uint*)0x40000100;
        public static unsafe uint* CLOCK_EVENTS_LFCLKSTATED => (uint*)0x40000104;
        public static unsafe uint* CLOCK_EVENTS_DONE => (uint*)0x40000108;
        public static unsafe uint* CLOCK_EVENTS_CTTO => (uint*)0x4000010C;
        public static unsafe uint* CLOCK_HFCLKSTAT => (uint*)0x40000400;
        public static unsafe uint* CLOCK_LFCLKSTAT => (uint*)0x40000404;
        public static unsafe uint* CLOCK_LFCLKSRC => (uint*)0x40000508;
        public static unsafe uint* CLOCK_CTIV => (uint*)0x4000050C;
        public static unsafe uint* CLOCK_INTEN => (uint*)0x40000700;

        // Power
        public const int POWER_BASE = 0x40000000;
        public static unsafe uint* POWER_TASKS_CONSTLAT => (uint*)0x40000000;
        public static unsafe uint* POWER_TASKS_LOWPWR => (uint*)0x40000004;
        public static unsafe uint* POWER_EVENTS_POWERDEBUG => (uint*)0x40000100;
        public static unsafe uint* POWER_EVENTS_SLEEPDEBUG => (uint*)0x40000104;
        public static unsafe uint* POWER_INTEN => (uint*)0x40000700;

        // GPIO Tasks and Events
        public const int GPIOTE_BASE = 0x40006000;
        public static unsafe uint* GPIOTE_TASKS_SET0 => (uint*)0x40006000;
        public static unsafe uint* GPIOTE_TASKS_SET1 => (uint*)0x40006004;
        public static unsafe uint* GPIOTE_TASKS_SET2 => (uint*)0x40006008;
        public static unsafe uint* GPIOTE_TASKS_SET3 => (uint*)0x4000600C;
        public static unsafe uint* GPIOTE_TASKS_CLR0 => (uint*)0x40006010;
        public static unsafe uint* GPIOTE_TASKS_CLR1 => (uint*)0x40006014;
        public static unsafe uint* GPIOTE_TASKS_CLR2 => (uint*)0x40006018;
        public static unsafe uint* GPIOTE_TASKS_CLR3 => (uint*)0x4000601C;
        public static unsafe uint* GPIOTE_EVENTS_IN0 => (uint*)0x40006100;
        public static unsafe uint* GPIOTE_EVENTS_IN1 => (uint*)0x40006104;
        public static unsafe uint* GPIOTE_EVENTS_IN2 => (uint*)0x40006108;
        public static unsafe uint* GPIOTE_EVENTS_IN3 => (uint*)0x4000610C;
        public static unsafe uint* GPIOTE_EVENTS_IN4 => (uint*)0x40006110;
        public static unsafe uint* GPIOTE_EVENTS_IN5 => (uint*)0x40006114;
        public static unsafe uint* GPIOTE_EVENTS_IN6 => (uint*)0x40006118;
        public static unsafe uint* GPIOTE_EVENTS_IN7 => (uint*)0x4000611C;
        public static unsafe uint* GPIOTE_EVENTS_TOUCH => (uint*)0x40006140;
        public static unsafe uint* GPIOTE_EVENTS_LISR => (uint*)0x40006144;
        public static unsafe uint* GPIOTE_EVENTS_LISF => (uint*)0x40006148;
        public static unsafe uint* GPIOTE_EVENTS_COUNT => (uint*)0x4000614C;
        public static unsafe uint* GPIOTE_CONFIG0 => (uint*)0x40006510;
        public static unsafe uint* GPIOTE_CONFIG1 => (uint*)0x40006514;
        public static unsafe uint* GPIOTE_CONFIG2 => (uint*)0x40006518;
        public static unsafe uint* GPIOTE_CONFIG3 => (uint*)0x4000651C;
        public static unsafe uint* GPIOTE_CONFIG4 => (uint*)0x40006520;
        public static unsafe uint* GPIOTE_CONFIG5 => (uint*)0x40006524;
        public static unsafe uint* GPIOTE_CONFIG6 => (uint*)0x40006528;
        public static unsafe uint* GPIOTE_CONFIG7 => (uint*)0x4000652C;
        public static unsafe uint* GPIOTE_INTEN => (uint*)0x40006700;

        // Real Time Timer
        public const int RTT_BASE = 0x40009000;
        public static unsafe uint* RTT_TASKS_START => (uint*)0x40009000;
        public static unsafe uint* RTT_TASKS_STOP => (uint*)0x40009004;
        public static unsafe uint* RTT_TASKS_TRIGOVRFLW => (uint*)0x40009010;
        public static unsafe uint* RTT_EVENTS_TICK => (uint*)0x40009100;
        public static unsafe uint* RTT_EVENTS_OVRFLW => (uint*)0x40009104;
        public static unsafe uint* RTT_EVENTS_COMPARE0 => (uint*)0x40009140;
        public static unsafe uint* RTT_CC0 => (uint*)0x40009400;
        public static unsafe uint* RTT_CC1 => (uint*)0x40009404;
        public static unsafe uint* RTT_CC2 => (uint*)0x40009408;
        public static unsafe uint* RTT_CC3 => (uint*)0x4000940C;
        public static unsafe uint* RTT_CNT => (uint*)0x40009500;
        public static unsafe uint* RTT_PRESCALER => (uint*)0x40009504;

        // Inter-Process Communication
        public const int IPC_BASE = 0x40014000;
        public static unsafe uint* IPC_TASKS_SEND0 => (uint*)0x40014000;
        public static unsafe uint* IPC_TASKS_SEND1 => (uint*)0x40014004;
        public static unsafe uint* IPC_TASKS_SEND2 => (uint*)0x40014008;
        public static unsafe uint* IPC_TASKS_SEND3 => (uint*)0x4001400C;
        public static unsafe uint* IPC_TASKS_SEND4 => (uint*)0x40014010;
        public static unsafe uint* IPC_TASKS_SEND5 => (uint*)0x40014014;
        public static unsafe uint* IPC_TASKS_SEND6 => (uint*)0x40014018;
        public static unsafe uint* IPC_TASKS_SEND7 => (uint*)0x4001401C;
        public static unsafe uint* IPC_TASKS_RECEIVE0 => (uint*)0x40014080;
        public static unsafe uint* IPC_TASKS_RECEIVE1 => (uint*)0x40014084;
        public static unsafe uint* IPC_TASKS_RECEIVE2 => (uint*)0x40014088;
        public static unsafe uint* IPC_TASKS_RECEIVE3 => (uint*)0x4001408C;
        public static unsafe uint* IPC_TASKS_RECEIVE4 => (uint*)0x40014090;
        public static unsafe uint* IPC_TASKS_RECEIVE5 => (uint*)0x40014094;
        public static unsafe uint* IPC_TASKS_RECEIVE6 => (uint*)0x40014098;
        public static unsafe uint* IPC_TASKS_RECEIVE7 => (uint*)0x4001409C;
        public static unsafe uint* IPC_EVENTS_SENT0 => (uint*)0x40014100;
        public static unsafe uint* IPC_EVENTS_SENT1 => (uint*)0x40014104;
        public static unsafe uint* IPC_EVENTS_SENT2 => (uint*)0x40014108;
        public static unsafe uint* IPC_EVENTS_SENT3 => (uint*)0x4001410C;
        public static unsafe uint* IPC_EVENTS_SENT4 => (uint*)0x40014110;
        public static unsafe uint* IPC_EVENTS_SENT5 => (uint*)0x40014114;
        public static unsafe uint* IPC_EVENTS_SENT6 => (uint*)0x40014118;
        public static unsafe uint* IPC_EVENTS_SENT7 => (uint*)0x4001411C;
        public static unsafe uint* IPC_EVENTS_RECEIVE0 => (uint*)0x40014180;
        public static unsafe uint* IPC_EVENTS_RECEIVE1 => (uint*)0x40014184;
        public static unsafe uint* IPC_EVENTS_RECEIVE2 => (uint*)0x40014188;
        public static unsafe uint* IPC_EVENTS_RECEIVE3 => (uint*)0x4001418C;
        public static unsafe uint* IPC_EVENTS_RECEIVE4 => (uint*)0x40014190;
        public static unsafe uint* IPC_EVENTS_RECEIVE5 => (uint*)0x40014194;
        public static unsafe uint* IPC_EVENTS_RECEIVE6 => (uint*)0x40014198;
        public static unsafe uint* IPC_EVENTS_RECEIVE7 => (uint*)0x4001419C;
        public static unsafe uint* IPC_CH0 => (uint*)0x40014500;
        public static unsafe uint* IPC_CH1 => (uint*)0x40014504;
        public static unsafe uint* IPC_CH2 => (uint*)0x40014508;
        public static unsafe uint* IPC_CH3 => (uint*)0x4001450C;
        public static unsafe uint* IPC_CH4 => (uint*)0x40014510;
        public static unsafe uint* IPC_CH5 => (uint*)0x40014514;
        public static unsafe uint* IPC_CH6 => (uint*)0x40014518;
        public static unsafe uint* IPC_CH7 => (uint*)0x4001451C;
        public static unsafe uint* IPC_INTEN => (uint*)0x40014700;

        // 中断向量定义
        public const int IRQ_POWER = 0;  // Power
        public const int IRQ_RADIO = 1;  // RADIO
        public const int IRQ_UART0 = 2;  // UART0
        public const int IRQ_UART1 = 3;  // UART1
        public const int IRQ_SPI0 = 4;  // SPI0
        public const int IRQ_SPI1 = 5;  // SPI1
        public const int IRQ_SPI2 = 6;  // SPI2
        public const int IRQ_GPIOTE = 7;  // GPIOTE
        public const int IRQ_ADC = 8;  // ADC
        public const int IRQ_TIMER0 = 9;  // TIMER0
        public const int IRQ_TIMER1 = 10;  // TIMER1
        public const int IRQ_TIMER2 = 11;  // TIMER2
        public const int IRQ_TIMER3 = 12;  // TIMER3
        public const int IRQ_TIMER4 = 13;  // TIMER4
        public const int IRQ_RTC0 = 14;  // RTC0
        public const int IRQ_RTC1 = 15;  // RTC1
        public const int IRQ_TEMP = 16;  // TEMP
        public const int IRQ_RNG = 17;  // RNG
        public const int IRQ_WDT = 18;  // WDT
        public const int IRQ_IPC = 19;  // IPC
        public const int IRQ_PWM0 = 20;  // PWM0
        public const int IRQ_PWM1 = 21;  // PWM1
        public const int IRQ_PWM2 = 22;  // PWM2
        public const int IRQ_PWM3 = 23;  // PWM3
        public const int IRQ_ZAR = 24;  // RESERVED
        public const int IRQ_EGU0 = 25;  // EGU0
        public const int IRQ_EGU1 = 26;  // EGU1
        public const int IRQ_EGU2 = 27;  // EGU2
        public const int IRQ_EGU3 = 28;  // EGU3
        public const int IRQ_EGU4 = 29;  // EGU4
        public const int IRQ_EGU5 = 30;  // EGU5
        public const int IRQ_RESERVED = 31;  // RESERVED
        public const int IRQ_SPIM0 = 32;  // SPIM0
        public const int IRQ_SPIM1 = 33;  // SPIM1
        public const int IRQ_SPIM2 = 34;  // SPIM2
        public const int IRQ_RESERVED = 35;  // RESERVED
        public const int IRQ_RESERVED = 36;  // RESERVED
        public const int IRQ_USB = 37;  // USB
        public const int IRQ_RESERVED = 38;  // RESERVED
        public const int IRQ_RESERVED = 39;  // RESERVED
        public const int IRQ_RESERVED = 40;  // RESERVED
        public const int IRQ_RESERVED = 41;  // RESERVED
        public const int IRQ_RESERVED = 42;  // RESERVED
        public const int IRQ_CRYPTOCELL = 43;  // CRYPTOCELL
        public const int IRQ_RESERVED = 44;  // RESERVED
        public const int IRQ_RESERVED = 45;  // RESERVED
        public const int IRQ_RESERVED = 46;  // RESERVED
        public const int IRQ_RESERVED = 47;  // RESERVED

        // 引脚定义
        public const int PIN_VDD = 1;  // 3.3V power supply
        public const int PIN_VDD = 2;  // 3.3V power supply
        public const int PIN_DEC4 = 3;  // Decoupling 4
        public const int PIN_DEC5 = 4;  // Decoupling 5
        public const int PIN_P0_01 = 5;  // GPIO Port 0.01
        public const int PIN_P0_02 = 6;  // GPIO Port 0.02
        public const int PIN_P0_03 = 7;  // GPIO Port 0.03
        public const int PIN_P0_04 = 8;  // GPIO Port 0.04
        public const int PIN_P0_05 = 9;  // GPIO Port 0.05
        public const int PIN_P0_06 = 10;  // GPIO Port 0.06
        public const int PIN_P0_07 = 11;  // GPIO Port 0.07
        public const int PIN_P0_08 = 12;  // GPIO Port 0.08
        public const int PIN_P0_09 = 13;  // GPIO Port 0.09
        public const int PIN_P0_10 = 14;  // GPIO Port 0.10
        public const int PIN_P0_11 = 15;  // GPIO Port 0.11
        public const int PIN_P0_12 = 16;  // GPIO Port 0.12
        public const int PIN_P0_13 = 17;  // GPIO Port 0.13
        public const int PIN_P0_14 = 18;  // GPIO Port 0.14
        public const int PIN_P0_15 = 19;  // GPIO Port 0.15
        public const int PIN_P0_16 = 20;  // GPIO Port 0.16
        public const int PIN_P0_17 = 21;  // GPIO Port 0.17
        public const int PIN_P0_18 = 22;  // GPIO Port 0.18
        public const int PIN_P0_19 = 23;  // GPIO Port 0.19
        public const int PIN_P0_20 = 24;  // GPIO Port 0.20
        public const int PIN_P0_21 = 25;  // GPIO Port 0.21
        public const int PIN_P0_22 = 26;  // GPIO Port 0.22
        public const int PIN_P0_23 = 27;  // GPIO Port 0.23
        public const int PIN_P0_24 = 28;  // GPIO Port 0.24
        public const int PIN_P0_25 = 29;  // GPIO Port 0.25
        public const int PIN_P0_26 = 30;  // GPIO Port 0.26
        public const int PIN_P0_27 = 31;  // GPIO Port 0.27
        public const int PIN_P0_28 = 32;  // GPIO Port 0.28
        public const int PIN_P0_29 = 33;  // GPIO Port 0.29
        public const int PIN_P0_30 = 34;  // GPIO Port 0.30
        public const int PIN_P0_31 = 35;  // GPIO Port 0.31
        public const int PIN_P1_00 = 36;  // GPIO Port 1.00
        public const int PIN_P1_01 = 37;  // GPIO Port 1.01
        public const int PIN_P1_02 = 38;  // GPIO Port 1.02
        public const int PIN_P1_03 = 39;  // GPIO Port 1.03
        public const int PIN_P1_04 = 40;  // GPIO Port 1.04
        public const int PIN_P1_05 = 41;  // GPIO Port 1.05
        public const int PIN_P1_06 = 42;  // GPIO Port 1.06
        public const int PIN_P1_07 = 43;  // GPIO Port 1.07
        public const int PIN_P1_08 = 44;  // GPIO Port 1.08
        public const int PIN_P1_09 = 45;  // GPIO Port 1.09
        public const int PIN_P1_10 = 46;  // GPIO Port 1.10
        public const int PIN_P1_11 = 47;  // GPIO Port 1.11
        public const int PIN_P1_12 = 48;  // GPIO Port 1.12
        public const int PIN_P1_13 = 49;  // GPIO Port 1.13
        public const int PIN_P1_14 = 50;  // GPIO Port 1.14
        public const int PIN_P1_15 = 51;  // GPIO Port 1.15
        public const int PIN_VDD = 52;  // 3.3V power supply
        public const int PIN_VDD = 53;  // 3.3V power supply
        public const int PIN_DEC1 = 54;  // Decoupling 1
        public const int PIN_DEC2 = 55;  // Decoupling 2
        public const int PIN_DEC3 = 56;  // Decoupling 3
        public const int PIN_NFC1 = 57;  // NFC 1
        public const int PIN_NFC2 = 58;  // NFC 2
        public const int PIN_P0_16 = 59;  // GPIO Port 0.16
        public const int PIN_SWDIO = 60;  // SWD I/O
        public const int PIN_SWDCLK = 61;  // SWD Clock
        public const int PIN_RESET = 62;  // Reset

        public static void nrf52840_init()
        {
            // 硬件初始化代码
        }
    }
}
