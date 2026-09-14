package vml.device.nordicsemiconductor.nrf52840;

/**
 * nRF52840 寄存器定义
 * 生成自: Nordic Semiconductor/nRF52/nRF52840
 * 版本: 1.0
 */
public final class nRF52840 {
    private nRF52840() {} // 工具类
    // CPU架构: ARM-Cortex-M4F, 32位, 32000000 Hz

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
    public static final int XPSR_ICI1 = 0;  // Interrupt Continue State
    public static final int XPSR_GE = 0;  // Greater than or Equal
    public static final int XPSR_IT = 0;  // If-Then execution state
    public static final int XPSR_T = 24;  // Thumb bit
    public static final int XPSR_IPSR = 0;  // Exception number

    // Priority Mask Register
    public static final int PRIMASK_ADDR = (int)0xE0000E20;

    // Base Priority Register
    public static final int BASEPRI_ADDR = (int)0xE0000E24;

    // Fault Mask Register
    public static final int FAULTMASK_ADDR = (int)0xE0000E28;

    // Control Register
    public static final int CONTROL_ADDR = (int)0xE0000E2C;

    // FPU Status Control
    public static final int FPSCR_ADDR = (int)0xE0000EF34;

    // FPU Register S0
    public static final int S0_ADDR = (int)0xE0000EF00;

    // FPU Register S1
    public static final int S1_ADDR = (int)0xE0000EF04;

    // FPU Register S2
    public static final int S2_ADDR = (int)0xE0000EF08;

    // FPU Register S3
    public static final int S3_ADDR = (int)0xE0000EF0C;

    // FPU Register S4
    public static final int S4_ADDR = (int)0xE0000EF10;

    // FPU Register S5
    public static final int S5_ADDR = (int)0xE0000EF14;

    // FPU Register S6
    public static final int S6_ADDR = (int)0xE0000EF18;

    // FPU Register S7
    public static final int S7_ADDR = (int)0xE0000EF1C;

    // FPU Register S8
    public static final int S8_ADDR = (int)0xE0000EF20;

    // FPU Register S9
    public static final int S9_ADDR = (int)0xE0000EF24;

    // FPU Register S10
    public static final int S10_ADDR = (int)0xE0000EF28;

    // FPU Register S11
    public static final int S11_ADDR = (int)0xE0000EF2C;

    // FPU Register S12
    public static final int S12_ADDR = (int)0xE0000EF30;

    // FPU Register S13
    public static final int S13_ADDR = (int)0xE0000EF34;

    // FPU Register S14
    public static final int S14_ADDR = (int)0xE0000EF38;

    // FPU Register S15
    public static final int S15_ADDR = (int)0xE0000EF3C;

    // FPU Register S16
    public static final int S16_ADDR = (int)0xE0000EF40;

    // FPU Register S17
    public static final int S17_ADDR = (int)0xE0000EF44;

    // FPU Register S18
    public static final int S18_ADDR = (int)0xE0000EF48;

    // FPU Register S19
    public static final int S19_ADDR = (int)0xE0000EF4C;

    // FPU Register S20
    public static final int S20_ADDR = (int)0xE0000EF50;

    // FPU Register S21
    public static final int S21_ADDR = (int)0xE0000EF54;

    // FPU Register S22
    public static final int S22_ADDR = (int)0xE0000EF58;

    // FPU Register S23
    public static final int S23_ADDR = (int)0xE0000EF5C;

    // FPU Register S24
    public static final int S24_ADDR = (int)0xE0000EF60;

    // FPU Register S25
    public static final int S25_ADDR = (int)0xE0000EF64;

    // FPU Register S26
    public static final int S26_ADDR = (int)0xE0000EF68;

    // FPU Register S27
    public static final int S27_ADDR = (int)0xE0000EF6C;

    // FPU Register S28
    public static final int S28_ADDR = (int)0xE0000EF70;

    // FPU Register S29
    public static final int S29_ADDR = (int)0xE0000EF74;

    // FPU Register S30
    public static final int S30_ADDR = (int)0xE0000EF78;

    // FPU Register S31
    public static final int S31_ADDR = (int)0xE0000EF7C;

    // 内存段定义
    // Flash (1MB)
    public static final int FLASH_START = (int)0x00000000;
    public static final int FLASH_END = (int)0x0FFFFF;
    public static final int FLASH_SIZE = 1048576;

    // SRAM (256KB)
    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x2003FFFF;
    public static final int SRAM_SIZE = 262144;

    // SRAM Low (128KB)
    public static final int SRAM_LOW_START = (int)0x20000000;
    public static final int SRAM_LOW_END = (int)0x2001FFFF;
    public static final int SRAM_LOW_SIZE = 131072;

    // SRAM High (128KB)
    public static final int SRAM_HIGH_START = (int)0x20020000;
    public static final int SRAM_HIGH_END = (int)0x2003FFFF;
    public static final int SRAM_HIGH_SIZE = 131072;

    // Factory Information Configuration
    public static final int FICR_START = (int)0x10000000;
    public static final int FICR_END = (int)0x10001000;
    public static final int FICR_SIZE = 4096;

    // User Information Configuration
    public static final int UICR_START = (int)0x10001000;
    public static final int UICR_END = (int)0x10001000;
    public static final int UICR_SIZE = 4096;

    // Peripheral Space
    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x50000000;
    public static final int PERIPHERAL_SIZE = 268435456;

    // 外设定义
    // GPIO
    public static final int GPIO_BASE = (int)0x50000000;
    public static final int GPIO_OUT = (int)0x50000000;
    public static final int GPIO_OUTSET = (int)0x50000004;
    public static final int GPIO_OUTCLR = (int)0x50000008;
    public static final int GPIO_IN = (int)0x5000000C;
    public static final int GPIO_DIR = (int)0x50000010;
    public static final int GPIO_DIRSET = (int)0x50000014;
    public static final int GPIO_DIRCLR = (int)0x50000018;
    public static final int GPIO_PIN_CNF0 = (int)0x50000300;
    public static final int GPIO_PIN_CNF1 = (int)0x50000304;
    public static final int GPIO_PIN_CNF2 = (int)0x50000308;
    public static final int GPIO_PIN_CNF3 = (int)0x5000030C;
    public static final int GPIO_PIN_CNF4 = (int)0x50000310;
    public static final int GPIO_PIN_CNF5 = (int)0x50000314;
    public static final int GPIO_PIN_CNF6 = (int)0x50000318;
    public static final int GPIO_PIN_CNF7 = (int)0x5000031C;
    public static final int GPIO_PIN_CNF8 = (int)0x50000320;
    public static final int GPIO_PIN_CNF9 = (int)0x50000324;
    public static final int GPIO_PIN_CNF10 = (int)0x50000328;
    public static final int GPIO_PIN_CNF11 = (int)0x5000032C;
    public static final int GPIO_PIN_CNF12 = (int)0x50000330;
    public static final int GPIO_PIN_CNF13 = (int)0x50000334;
    public static final int GPIO_PIN_CNF14 = (int)0x50000338;
    public static final int GPIO_PIN_CNF15 = (int)0x5000033C;
    public static final int GPIO_PIN_CNF16 = (int)0x50000340;
    public static final int GPIO_PIN_CNF17 = (int)0x50000344;
    public static final int GPIO_PIN_CNF18 = (int)0x50000348;
    public static final int GPIO_PIN_CNF19 = (int)0x5000034C;
    public static final int GPIO_PIN_CNF20 = (int)0x50000350;
    public static final int GPIO_PIN_CNF21 = (int)0x50000354;
    public static final int GPIO_PIN_CNF22 = (int)0x50000358;
    public static final int GPIO_PIN_CNF23 = (int)0x5000035C;
    public static final int GPIO_PIN_CNF24 = (int)0x50000360;
    public static final int GPIO_PIN_CNF25 = (int)0x50000364;
    public static final int GPIO_PIN_CNF26 = (int)0x50000368;
    public static final int GPIO_PIN_CNF27 = (int)0x5000036C;
    public static final int GPIO_PIN_CNF28 = (int)0x50000370;
    public static final int GPIO_PIN_CNF29 = (int)0x50000374;
    public static final int GPIO_PIN_CNF30 = (int)0x50000378;
    public static final int GPIO_PIN_CNF31 = (int)0x5000037C;

    // UART0
    public static final int UART0_BASE = (int)0x40002000;
    public static final int UART0_TASKS_STARTRX = (int)0x40002000;
    public static final int UART0_TASKS_STARTTX = (int)0x40002004;
    public static final int UART0_TASKS_STOPRX = (int)0x40002008;
    public static final int UART0_TASKS_STOPTX = (int)0x4000200C;
    public static final int UART0_SUBSCRIBED_STARTRX = (int)0x40002010;
    public static final int UART0_SUBSCRIBED_STARTTX = (int)0x40002014;
    public static final int UART0_EVENTS_CTS = (int)0x40002100;
    public static final int UART0_EVENTS_NCTS = (int)0x40002104;
    public static final int UART0_EVENTS_RXDRDY = (int)0x40002108;
    public static final int UART0_EVENTS_TXDRDY = (int)0x4000210C;
    public static final int UART0_EVENTS_ERROR = (int)0x40002110;
    public static final int UART0_RXD = (int)0x40002518;
    public static final int UART0_TXD = (int)0x4000251C;
    public static final int UART0_BAUDRATE = (int)0x40002524;
    public static final int UART0_CONFIG = (int)0x4000256C;
    public static final int UART0_INTEN = (int)0x40002700;
    public static final int UART0_INTENSET = (int)0x40002704;
    public static final int UART0_INTENCLR = (int)0x40002708;

    // UART1
    public static final int UART1_BASE = (int)0x40003000;
    public static final int UART1_TASKS_STARTRX = (int)0x40003000;
    public static final int UART1_TASKS_STARTTX = (int)0x40003004;
    public static final int UART1_EVENTS_RXDRDY = (int)0x40003108;
    public static final int UART1_EVENTS_TXDRDY = (int)0x4000310C;
    public static final int UART1_RXD = (int)0x40003518;
    public static final int UART1_TXD = (int)0x4000351C;
    public static final int UART1_BAUDRATE = (int)0x40003524;

    // SPI0
    public static final int SPI0_BASE = (int)0x40003000;
    public static final int SPI0_EVENTS_READY = (int)0x40003108;
    public static final int SPI0_RXD = (int)0x40003518;
    public static final int SPI0_TXD = (int)0x4000351C;
    public static final int SPI0_FREQUENCY = (int)0x40003524;
    public static final int SPI0_CONFIG = (int)0x4000356C;
    public static final int SPI0_INTEN = (int)0x40003700;

    // SPI1
    public static final int SPI1_BASE = (int)0x40004000;
    public static final int SPI1_EVENTS_READY = (int)0x40004108;
    public static final int SPI1_RXD = (int)0x40004518;
    public static final int SPI1_TXD = (int)0x4000451C;
    public static final int SPI1_FREQUENCY = (int)0x40004524;
    public static final int SPI1_CONFIG = (int)0x4000456C;

    // SPI2
    public static final int SPI2_BASE = (int)0x40005000;
    public static final int SPI2_EVENTS_READY = (int)0x40005108;
    public static final int SPI2_RXD = (int)0x40005518;
    public static final int SPI2_TXD = (int)0x4000551C;
    public static final int SPI2_FREQUENCY = (int)0x40005524;

    // I2C0
    public static final int I2C0_BASE = (int)0x40003000;
    public static final int I2C0_TASKS_STARTRX = (int)0x40003000;
    public static final int I2C0_TASKS_STARTTX = (int)0x40003008;
    public static final int I2C0_TASKS_STOP = (int)0x40003014;
    public static final int I2C0_EVENTS_DONE = (int)0x40003108;
    public static final int I2C0_EVENTS_TXDSENT = (int)0x4000310C;
    public static final int I2C0_EVENTS_ERROR = (int)0x40003110;
    public static final int I2C0_RXD = (int)0x40003518;
    public static final int I2C0_TXD = (int)0x4000351C;
    public static final int I2C0_ADDRESS = (int)0x40003524;

    // I2C1
    public static final int I2C1_BASE = (int)0x40004000;
    public static final int I2C1_TASKS_STARTRX = (int)0x40004000;
    public static final int I2C1_TASKS_STARTTX = (int)0x40004008;
    public static final int I2C1_TASKS_STOP = (int)0x40004014;
    public static final int I2C1_EVENTS_DONE = (int)0x40004108;
    public static final int I2C1_RXD = (int)0x40004518;
    public static final int I2C1_TXD = (int)0x4000451C;
    public static final int I2C1_ADDRESS = (int)0x40004524;

    // Timer0
    public static final int TIMER0_BASE = (int)0x40008000;
    public static final int TIMER0_TASKS_START = (int)0x40008000;
    public static final int TIMER0_TASKS_STOP = (int)0x40008004;
    public static final int TIMER0_TASKS_COUNT = (int)0x40008008;
    public static final int TIMER0_TASKS_CLEAR = (int)0x4000800C;
    public static final int TIMER0_CC0 = (int)0x40008400;
    public static final int TIMER0_CC1 = (int)0x40008404;
    public static final int TIMER0_CC2 = (int)0x40008408;
    public static final int TIMER0_CC3 = (int)0x4000840C;
    public static final int TIMER0_SHORTS = (int)0x40008200;
    public static final int TIMER0_INTEN = (int)0x40008700;
    public static final int TIMER0_MODE = (int)0x40008510;
    public static final int TIMER0_BITMODE = (int)0x40008514;

    // Timer1
    public static final int TIMER1_BASE = (int)0x40009000;
    public static final int TIMER1_TASKS_START = (int)0x40009000;
    public static final int TIMER1_TASKS_STOP = (int)0x40009004;
    public static final int TIMER1_TASKS_COUNT = (int)0x40009008;
    public static final int TIMER1_TASKS_CLEAR = (int)0x4000900C;
    public static final int TIMER1_CC0 = (int)0x40009400;
    public static final int TIMER1_CC1 = (int)0x40009404;
    public static final int TIMER1_CC2 = (int)0x40009408;
    public static final int TIMER1_CC3 = (int)0x4000940C;
    public static final int TIMER1_SHORTS = (int)0x40009200;

    // Timer2
    public static final int TIMER2_BASE = (int)0x4000A000;
    public static final int TIMER2_TASKS_START = (int)0x4000A000;
    public static final int TIMER2_TASKS_STOP = (int)0x4000A004;
    public static final int TIMER2_TASKS_COUNT = (int)0x4000A008;
    public static final int TIMER2_TASKS_CLEAR = (int)0x4000A00C;
    public static final int TIMER2_CC0 = (int)0x4000A400;
    public static final int TIMER2_CC1 = (int)0x4000A404;
    public static final int TIMER2_CC2 = (int)0x4000A408;
    public static final int TIMER2_CC3 = (int)0x4000A40C;

    // Timer3
    public static final int TIMER3_BASE = (int)0x4000B000;
    public static final int TIMER3_TASKS_START = (int)0x4000B000;
    public static final int TIMER3_TASKS_STOP = (int)0x4000B004;
    public static final int TIMER3_TASKS_COUNT = (int)0x4000B008;
    public static final int TIMER3_TASKS_CLEAR = (int)0x4000B00C;
    public static final int TIMER3_CC0 = (int)0x4000B400;
    public static final int TIMER3_CC1 = (int)0x4000B404;
    public static final int TIMER3_CC2 = (int)0x4000B408;
    public static final int TIMER3_CC3 = (int)0x4000B40C;

    // Timer4
    public static final int TIMER4_BASE = (int)0x4000C000;
    public static final int TIMER4_TASKS_START = (int)0x4000C000;
    public static final int TIMER4_TASKS_STOP = (int)0x4000C004;
    public static final int TIMER4_TASKS_COUNT = (int)0x4000C008;
    public static final int TIMER4_TASKS_CLEAR = (int)0x4000C00C;
    public static final int TIMER4_CC0 = (int)0x4000C400;
    public static final int TIMER4_CC1 = (int)0x4000C404;
    public static final int TIMER4_CC2 = (int)0x4000C408;
    public static final int TIMER4_CC3 = (int)0x4000C40C;

    // RTC0
    public static final int RTC0_BASE = (int)0x4000B000;
    public static final int RTC0_TASKS_START = (int)0x4000B000;
    public static final int RTC0_TASKS_STOP = (int)0x4000B004;
    public static final int RTC0_TASKS_TRIGOVRFLW = (int)0x4000B010;
    public static final int RTC0_EVENTS_TICK = (int)0x4000B100;
    public static final int RTC0_EVENTS_OVRFLW = (int)0x4000B104;
    public static final int RTC0_EVENTS_COMPARE0 = (int)0x4000B140;
    public static final int RTC0_EVENTS_COMPARE1 = (int)0x4000B144;
    public static final int RTC0_EVENTS_COMPARE2 = (int)0x4000B148;
    public static final int RTC0_EVENTS_COMPARE3 = (int)0x4000B14C;
    public static final int RTC0_CC0 = (int)0x4000B400;
    public static final int RTC0_CC1 = (int)0x4000B404;
    public static final int RTC0_CC2 = (int)0x4000B408;
    public static final int RTC0_CC3 = (int)0x4000B40C;
    public static final int RTC0_CNT = (int)0x4000B500;
    public static final int RTC0_PRESCALER = (int)0x4000B504;
    public static final int RTC0_TICK = (int)0x4000B510;

    // RTC1
    public static final int RTC1_BASE = (int)0x4000D000;
    public static final int RTC1_TASKS_START = (int)0x4000D000;
    public static final int RTC1_TASKS_STOP = (int)0x4000D004;
    public static final int RTC1_EVENTS_TICK = (int)0x4000D100;
    public static final int RTC1_EVENTS_OVRFLW = (int)0x4000D104;
    public static final int RTC1_EVENTS_COMPARE0 = (int)0x4000D140;
    public static final int RTC1_EVENTS_COMPARE1 = (int)0x4000D144;
    public static final int RTC1_CC0 = (int)0x4000D400;
    public static final int RTC1_CC1 = (int)0x4000D404;
    public static final int RTC1_CNT = (int)0x4000D500;
    public static final int RTC1_PRESCALER = (int)0x4000D504;

    // PWM0
    public static final int PWM0_BASE = (int)0x4001C000;
    public static final int PWM0_TASKS_START = (int)0x4001C000;
    public static final int PWM0_TASKS_STOP = (int)0x4001C004;
    public static final int PWM0_TASKS_SEQSTART0 = (int)0x4001C008;
    public static final int PWM0_TASKS_SEQSTART1 = (int)0x4001C00C;
    public static final int PWM0_TASKS_NEXTSTEP = (int)0x4001C010;
    public static final int PWM0_EVENTS_PWMPERIODEND = (int)0x4001C108;
    public static final int PWM0_EVENTS_LOOPEND = (int)0x4001C10C;
    public static final int PWM0_EVENTS_SEQEND0 = (int)0x4001C110;
    public static final int PWM0_EVENTS_SEQEND1 = (int)0x4001C114;
    public static final int PWM0_SEQ0_PTR = (int)0x4001C510;
    public static final int PWM0_SEQ1_PTR = (int)0x4001C514;
    public static final int PWM0_SEQ0_CNT = (int)0x4001C528;
    public static final int PWM0_SEQ1_CNT = (int)0x4001C52C;
    public static final int PWM0_SEQ0_REFRESH = (int)0x4001C530;
    public static final int PWM0_SEQ1_REFRESH = (int)0x4001C534;
    public static final int PWM0_DECODER = (int)0x4001C540;
    public static final int PWM0_LOOP = (int)0x4001C544;
    public static final int PWM0_MODE = (int)0x4001C500;
    public static final int PWM0_CLKEN = (int)0x4001C504;
    public static final int PWM0_CNT = (int)0x4001C548;

    // PWM1
    public static final int PWM1_BASE = (int)0x4001D000;
    public static final int PWM1_TASKS_START = (int)0x4001D000;
    public static final int PWM1_TASKS_STOP = (int)0x4001D004;
    public static final int PWM1_SEQ0_PTR = (int)0x4001D510;
    public static final int PWM1_SEQ1_PTR = (int)0x4001D514;
    public static final int PWM1_MODE = (int)0x4001D500;
    public static final int PWM1_CNT = (int)0x4001D548;

    // PWM2
    public static final int PWM2_BASE = (int)0x4001E000;
    public static final int PWM2_TASKS_START = (int)0x4001E000;
    public static final int PWM2_SEQ0_PTR = (int)0x4001E510;
    public static final int PWM2_MODE = (int)0x4001E500;

    // PWM3
    public static final int PWM3_BASE = (int)0x4001F000;
    public static final int PWM3_TASKS_START = (int)0x4001F000;
    public static final int PWM3_SEQ0_PTR = (int)0x4001F510;
    public static final int PWM3_MODE = (int)0x4001F500;

    // ADC
    public static final int ADC_BASE = (int)0x40012000;
    public static final int ADC_TASKS_START = (int)0x40012000;
    public static final int ADC_TASKS_STOP = (int)0x40012004;
    public static final int ADC_EVENTS_DONE = (int)0x40012108;
    public static final int ADC_EVENTS_RESULTDONE = (int)0x4001210C;
    public static final int ADC_EVENTS_CALIBRATEDONE = (int)0x40012110;
    public static final int ADC_EVENTS_CH_LIMITH = (int)0x40012114;
    public static final int ADC_EVENTS_CH_LIMITL = (int)0x40012118;
    public static final int ADC_RESULT = (int)0x40012400;
    public static final int ADC_CH0_CONFIG = (int)0x40012510;
    public static final int ADC_CH1_CONFIG = (int)0x40012514;
    public static final int ADC_CH2_CONFIG = (int)0x40012518;
    public static final int ADC_CH3_CONFIG = (int)0x4001251C;
    public static final int ADC_CH4_CONFIG = (int)0x40012520;
    public static final int ADC_CH5_CONFIG = (int)0x40012524;
    public static final int ADC_CH6_CONFIG = (int)0x40012528;
    public static final int ADC_CH7_CONFIG = (int)0x4001252C;
    public static final int ADC_CONFIG = (int)0x40012530;
    public static final int ADC_TASKS_CALIBRATELOAD = (int)0x40012034;
    public static final int ADC_INTEN = (int)0x40012700;

    // DAC
    public static final int DAC_BASE = (int)0x40013000;
    public static final int DAC_TASKS_START = (int)0x40013000;
    public static final int DAC_TASKS_STOP = (int)0x40013004;
    public static final int DAC_EVENTS_DONE = (int)0x40013108;
    public static final int DAC_VALUE = (int)0x40013400;
    public static final int DAC_CEN = (int)0x40013504;

    // Analog Comparator
    public static final int COMP_BASE = (int)0x40013000;
    public static final int COMP_TASKS_START = (int)0x40013000;
    public static final int COMP_TASKS_STOP = (int)0x40013004;
    public static final int COMP_TASKS_SETTLE = (int)0x40013010;
    public static final int COMP_EVENTS_READY = (int)0x40013108;
    public static final int COMP_EVENTS_DOWN = (int)0x4001310C;
    public static final int COMP_EVENTS_UP = (int)0x40013110;
    public static final int COMP_EVENTS_CROSS = (int)0x40013114;
    public static final int COMP_RESULT = (int)0x40013400;
    public static final int COMP_EN = (int)0x40013500;
    public static final int COMP_TASK_MODE = (int)0x40013504;
    public static final int COMP_REFSEL = (int)0x40013508;
    public static final int COMP_EXTREFSEL = (int)0x4001350C;
    public static final int COMP_THD = (int)0x40013510;
    public static final int COMP_HYST = (int)0x40013514;
    public static final int COMP_SPEED = (int)0x40013518;
    public static final int COMP_ISOURCE = (int)0x4001351C;
    public static final int COMP_PSEL = (int)0x40013520;
    public static final int COMP_NOREF = (int)0x40013524;
    public static final int COMP_INTEN = (int)0x40013700;

    // Quadrature Decoder
    public static final int QDEC_BASE = (int)0x40014000;
    public static final int QDEC_TASKS_START = (int)0x40014000;
    public static final int QDEC_TASKS_STOP = (int)0x40014004;
    public static final int QDEC_TASKS_RDCLRACC = (int)0x40014008;
    public static final int QDEC_TASKS_RDCLRDBL = (int)0x4001400C;
    public static final int QDEC_TASKS_RDCLRPH = (int)0x40014010;
    public static final int QDEC_EVENTS_READY = (int)0x40014108;
    public static final int QDEC_EVENTS_DBLRDY = (int)0x4001410C;
    public static final int QDEC_EVENTS_QCLR = (int)0x40014110;
    public static final int QDEC_ACC = (int)0x40014404;
    public static final int QDEC_ACCREAD = (int)0x40014408;
    public static final int QDEC_DBLINC = (int)0x40014410;
    public static final int QDEC_DBL = (int)0x40014418;
    public static final int QDEC_DBLREAD = (int)0x4001441C;
    public static final int QDEC_PHASE = (int)0x40014420;
    public static final int QDEC_PHASEREAD = (int)0x40014424;
    public static final int QDEC_LEFLL = (int)0x40014428;
    public static final int QDEC_INTEN = (int)0x40014700;

    // Event Generators Unit 0
    public static final int EGU0_BASE = (int)0x40014000;
    public static final int EGU0_TASKS_TRIGGER0 = (int)0x40014000;
    public static final int EGU0_TASKS_TRIGGER1 = (int)0x40014004;
    public static final int EGU0_TASKS_TRIGGER2 = (int)0x40014008;
    public static final int EGU0_TASKS_TRIGGER3 = (int)0x4001400C;
    public static final int EGU0_TASKS_TRIGGER4 = (int)0x40014010;
    public static final int EGU0_TASKS_TRIGGER5 = (int)0x40014014;
    public static final int EGU0_TASKS_TRIGGER6 = (int)0x40014018;
    public static final int EGU0_TASKS_TRIGGER7 = (int)0x4001401C;
    public static final int EGU0_TASKS_TRIGGER8 = (int)0x40014020;
    public static final int EGU0_TASKS_TRIGGER9 = (int)0x40014024;
    public static final int EGU0_TASKS_TRIGGER10 = (int)0x40014028;
    public static final int EGU0_TASKS_TRIGGER11 = (int)0x4001402C;
    public static final int EGU0_TASKS_TRIGGER12 = (int)0x40014030;
    public static final int EGU0_TASKS_TRIGGER13 = (int)0x40014034;
    public static final int EGU0_TASKS_TRIGGER14 = (int)0x40014038;
    public static final int EGU0_TASKS_TRIGGER15 = (int)0x4001403C;
    public static final int EGU0_EVENTS_EVENT0 = (int)0x40014100;
    public static final int EGU0_EVENTS_EVENT1 = (int)0x40014104;
    public static final int EGU0_EVENTS_EVENT2 = (int)0x40014108;
    public static final int EGU0_EVENTS_EVENT3 = (int)0x4001410C;
    public static final int EGU0_EVENTS_EVENT4 = (int)0x40014110;
    public static final int EGU0_EVENTS_EVENT5 = (int)0x40014114;
    public static final int EGU0_INTEN = (int)0x40014700;

    // Random Number Generator
    public static final int RNG_BASE = (int)0x40006000;
    public static final int RNG_TASKS_START = (int)0x40006000;
    public static final int RNG_TASKS_STOP = (int)0x40006004;
    public static final int RNG_EVENTS_VALRDY = (int)0x40006108;
    public static final int RNG_VALUE = (int)0x40006400;
    public static final int RNG_CONFIG = (int)0x40006504;
    public static final int RNG_INTEN = (int)0x40006700;

    // AES ECB
    public static final int AES_BASE = (int)0x40005000;
    public static final int AES_TASKS_START = (int)0x40005000;
    public static final int AES_TASKS_STOP = (int)0x40005004;
    public static final int AES_EVENTS_END = (int)0x40005108;
    public static final int AES_EVENTS_ERROR = (int)0x4000510C;
    public static final int AES_CRYPTCNTXT = (int)0x40005400;
    public static final int AES_CRYPTCNTCPY = (int)0x40005404;
    public static final int AES_CRYPTCMD = (int)0x40005500;
    public static final int AES_INTEN = (int)0x40005700;

    // Cryptocell
    public static final int CRYPTO_BASE = (int)0x4000E000;
    public static final int CRYPTO_TASKS_START = (int)0x4000E000;
    public static final int CRYPTO_TASKS_STOP = (int)0x4000E004;
    public static final int CRYPTO_EVENTS_DONE = (int)0x4000E108;
    public static final int CRYPTO_EVENTS_ERROR = (int)0x4000E10C;
    public static final int CRYPTO_DMA = (int)0x4000E400;
    public static final int CRYPTO_CMDS = (int)0x4000E404;
    public static final int CRYPTO_CMDS_AMOUNT = (int)0x4000E408;
    public static final int CRYPTO_INTENSET = (int)0x4000E704;
    public static final int CRYPTO_INTENCLR = (int)0x4000E708;
    public static final int CRYPTO_INTCONTEXT = (int)0x4000E710;

    // USB
    public static final int USB_BASE = (int)0x40027000;
    public static final int USB_TASKS_STARTUP = (int)0x40027000;
    public static final int USB_TASKS_SUSPEND = (int)0x40027004;
    public static final int USB_TASKS_RESUME = (int)0x40027008;
    public static final int USB_EVENTS_ENDRDY = (int)0x40027108;
    public static final int USB_EVENTS_SUSPENDED = (int)0x4002710C;
    public static final int USB_EVENTS_RESUMED = (int)0x40027110;
    public static final int USB_EVENTS_SOF = (int)0x40027114;
    public static final int USB_EVENTS_EPOF = (int)0x40027118;
    public static final int USB_EVENTS_DATA = (int)0x4002711C;
    public static final int USB_EVENTS_EP0DATADONE = (int)0x40027120;
    public static final int USB_EVENTS_EP0SETUP = (int)0x40027124;
    public static final int USB_EVENTS_EP0HALTD = (int)0x40027128;
    public static final int USB_EVENTS_EP1DMA = (int)0x40027134;
    public static final int USB_EVENTS_EP2DMA = (int)0x40027138;
    public static final int USB_EVENTS_EP3DMA = (int)0x4002713C;
    public static final int USB_EVENTS_EP4DMA = (int)0x40027140;
    public static final int USB_EVENTS_EP1 = (int)0x40027158;
    public static final int USB_EVENTS_EP2 = (int)0x4002715C;
    public static final int USB_EVENTS_EP3 = (int)0x40027160;
    public static final int USB_EVENTS_EP4 = (int)0x40027164;
    public static final int USB_EVENTS_EP5 = (int)0x40027168;
    public static final int USB_EVENTS_EP6 = (int)0x4002716C;
    public static final int USB_EVENTS_EP7 = (int)0x40027170;
    public static final int USB_EVENTS_EP8 = (int)0x40027174;
    public static final int USB_EVENTS_EP9 = (int)0x40027178;
    public static final int USB_EVENTS_EP10 = (int)0x4002717C;
    public static final int USB_EVENTS_EP11 = (int)0x40027180;
    public static final int USB_EVENTS_EP12 = (int)0x40027184;
    public static final int USB_EVENTS_EP13 = (int)0x40027188;
    public static final int USB_EVENTS_EP14 = (int)0x4002718C;
    public static final int USB_EVENTS_EP15 = (int)0x40027190;
    public static final int USB_USBADDR = (int)0x40027500;
    public static final int USB_USBREQ = (int)0x40027504;
    public static final int USB_USBVAL = (int)0x40027508;
    public static final int USB_USBINDEX = (int)0x4002750C;
    public static final int USB_USBCONFIG = (int)0x40027510;
    public static final int USB_EPIN = (int)0x40027514;
    public static final int USB_EPOUT = (int)0x40027518;
    public static final int USB_EPLEN = (int)0x40027520;
    public static final int USB_EPSIZE = (int)0x40027524;
    public static final int USB_EPDMA = (int)0x40027500;
    public static final int USB_EPDMA = (int)0x40027504;
    public static final int USB_INTEN = (int)0x40027700;
    public static final int USB_INTENSET = (int)0x40027704;
    public static final int USB_INTENCLR = (int)0x40027708;

    // Watchdog Timer
    public static final int WDT_BASE = (int)0x40011000;
    public static final int WDT_TASKS_START = (int)0x40011000;
    public static final int WDT_TASKS_KEEP = (int)0x40011004;
    public static final int WDT_TASKS_STOP = (int)0x40011008;
    public static final int WDT_EVENTS_TIMEOUT = (int)0x40011108;
    public static final int WDT_RUNSTATUS = (int)0x40011404;
    public static final int WDT_REQSTATUS = (int)0x40011408;
    public static final int WDT_CRV = (int)0x40011504;
    public static final int WDT_RCV = (int)0x40011508;
    public static final int WDT_CONFIG = (int)0x4001150C;
    public static final int WDT_INTEN = (int)0x40011700;

    // Reset
    public static final int NRF_RESET_BASE = (int)0x40000000;
    public static final int NRF_RESET_RESET = (int)0x40000000;
    public static final int NRF_RESET_RESET_FAC = (int)0x40000400;
    public static final int NRF_RESET_RESET_NFAC = (int)0x40000500;

    // Clock
    public static final int CLOCK_BASE = (int)0x40000000;
    public static final int CLOCK_TASKS_HFCLKSTART = (int)0x40000000;
    public static final int CLOCK_TASKS_HFCLKSTOP = (int)0x40000004;
    public static final int CLOCK_TASKS_LFCLKSTART = (int)0x40000008;
    public static final int CLOCK_TASKS_LFCLKSTOP = (int)0x4000000C;
    public static final int CLOCK_TASKS_CAL = (int)0x40000010;
    public static final int CLOCK_TASKS_CTTO = (int)0x40000010;
    public static final int CLOCK_EVENTS_HFCLKSTATED = (int)0x40000100;
    public static final int CLOCK_EVENTS_LFCLKSTATED = (int)0x40000104;
    public static final int CLOCK_EVENTS_DONE = (int)0x40000108;
    public static final int CLOCK_EVENTS_CTTO = (int)0x4000010C;
    public static final int CLOCK_HFCLKSTAT = (int)0x40000400;
    public static final int CLOCK_LFCLKSTAT = (int)0x40000404;
    public static final int CLOCK_LFCLKSRC = (int)0x40000508;
    public static final int CLOCK_CTIV = (int)0x4000050C;
    public static final int CLOCK_INTEN = (int)0x40000700;

    // Power
    public static final int POWER_BASE = (int)0x40000000;
    public static final int POWER_TASKS_CONSTLAT = (int)0x40000000;
    public static final int POWER_TASKS_LOWPWR = (int)0x40000004;
    public static final int POWER_EVENTS_POWERDEBUG = (int)0x40000100;
    public static final int POWER_EVENTS_SLEEPDEBUG = (int)0x40000104;
    public static final int POWER_INTEN = (int)0x40000700;

    // GPIO Tasks and Events
    public static final int GPIOTE_BASE = (int)0x40006000;
    public static final int GPIOTE_TASKS_SET0 = (int)0x40006000;
    public static final int GPIOTE_TASKS_SET1 = (int)0x40006004;
    public static final int GPIOTE_TASKS_SET2 = (int)0x40006008;
    public static final int GPIOTE_TASKS_SET3 = (int)0x4000600C;
    public static final int GPIOTE_TASKS_CLR0 = (int)0x40006010;
    public static final int GPIOTE_TASKS_CLR1 = (int)0x40006014;
    public static final int GPIOTE_TASKS_CLR2 = (int)0x40006018;
    public static final int GPIOTE_TASKS_CLR3 = (int)0x4000601C;
    public static final int GPIOTE_EVENTS_IN0 = (int)0x40006100;
    public static final int GPIOTE_EVENTS_IN1 = (int)0x40006104;
    public static final int GPIOTE_EVENTS_IN2 = (int)0x40006108;
    public static final int GPIOTE_EVENTS_IN3 = (int)0x4000610C;
    public static final int GPIOTE_EVENTS_IN4 = (int)0x40006110;
    public static final int GPIOTE_EVENTS_IN5 = (int)0x40006114;
    public static final int GPIOTE_EVENTS_IN6 = (int)0x40006118;
    public static final int GPIOTE_EVENTS_IN7 = (int)0x4000611C;
    public static final int GPIOTE_EVENTS_TOUCH = (int)0x40006140;
    public static final int GPIOTE_EVENTS_LISR = (int)0x40006144;
    public static final int GPIOTE_EVENTS_LISF = (int)0x40006148;
    public static final int GPIOTE_EVENTS_COUNT = (int)0x4000614C;
    public static final int GPIOTE_CONFIG0 = (int)0x40006510;
    public static final int GPIOTE_CONFIG1 = (int)0x40006514;
    public static final int GPIOTE_CONFIG2 = (int)0x40006518;
    public static final int GPIOTE_CONFIG3 = (int)0x4000651C;
    public static final int GPIOTE_CONFIG4 = (int)0x40006520;
    public static final int GPIOTE_CONFIG5 = (int)0x40006524;
    public static final int GPIOTE_CONFIG6 = (int)0x40006528;
    public static final int GPIOTE_CONFIG7 = (int)0x4000652C;
    public static final int GPIOTE_INTEN = (int)0x40006700;

    // Real Time Timer
    public static final int RTT_BASE = (int)0x40009000;
    public static final int RTT_TASKS_START = (int)0x40009000;
    public static final int RTT_TASKS_STOP = (int)0x40009004;
    public static final int RTT_TASKS_TRIGOVRFLW = (int)0x40009010;
    public static final int RTT_EVENTS_TICK = (int)0x40009100;
    public static final int RTT_EVENTS_OVRFLW = (int)0x40009104;
    public static final int RTT_EVENTS_COMPARE0 = (int)0x40009140;
    public static final int RTT_CC0 = (int)0x40009400;
    public static final int RTT_CC1 = (int)0x40009404;
    public static final int RTT_CC2 = (int)0x40009408;
    public static final int RTT_CC3 = (int)0x4000940C;
    public static final int RTT_CNT = (int)0x40009500;
    public static final int RTT_PRESCALER = (int)0x40009504;

    // Inter-Process Communication
    public static final int IPC_BASE = (int)0x40014000;
    public static final int IPC_TASKS_SEND0 = (int)0x40014000;
    public static final int IPC_TASKS_SEND1 = (int)0x40014004;
    public static final int IPC_TASKS_SEND2 = (int)0x40014008;
    public static final int IPC_TASKS_SEND3 = (int)0x4001400C;
    public static final int IPC_TASKS_SEND4 = (int)0x40014010;
    public static final int IPC_TASKS_SEND5 = (int)0x40014014;
    public static final int IPC_TASKS_SEND6 = (int)0x40014018;
    public static final int IPC_TASKS_SEND7 = (int)0x4001401C;
    public static final int IPC_TASKS_RECEIVE0 = (int)0x40014080;
    public static final int IPC_TASKS_RECEIVE1 = (int)0x40014084;
    public static final int IPC_TASKS_RECEIVE2 = (int)0x40014088;
    public static final int IPC_TASKS_RECEIVE3 = (int)0x4001408C;
    public static final int IPC_TASKS_RECEIVE4 = (int)0x40014090;
    public static final int IPC_TASKS_RECEIVE5 = (int)0x40014094;
    public static final int IPC_TASKS_RECEIVE6 = (int)0x40014098;
    public static final int IPC_TASKS_RECEIVE7 = (int)0x4001409C;
    public static final int IPC_EVENTS_SENT0 = (int)0x40014100;
    public static final int IPC_EVENTS_SENT1 = (int)0x40014104;
    public static final int IPC_EVENTS_SENT2 = (int)0x40014108;
    public static final int IPC_EVENTS_SENT3 = (int)0x4001410C;
    public static final int IPC_EVENTS_SENT4 = (int)0x40014110;
    public static final int IPC_EVENTS_SENT5 = (int)0x40014114;
    public static final int IPC_EVENTS_SENT6 = (int)0x40014118;
    public static final int IPC_EVENTS_SENT7 = (int)0x4001411C;
    public static final int IPC_EVENTS_RECEIVE0 = (int)0x40014180;
    public static final int IPC_EVENTS_RECEIVE1 = (int)0x40014184;
    public static final int IPC_EVENTS_RECEIVE2 = (int)0x40014188;
    public static final int IPC_EVENTS_RECEIVE3 = (int)0x4001418C;
    public static final int IPC_EVENTS_RECEIVE4 = (int)0x40014190;
    public static final int IPC_EVENTS_RECEIVE5 = (int)0x40014194;
    public static final int IPC_EVENTS_RECEIVE6 = (int)0x40014198;
    public static final int IPC_EVENTS_RECEIVE7 = (int)0x4001419C;
    public static final int IPC_CH0 = (int)0x40014500;
    public static final int IPC_CH1 = (int)0x40014504;
    public static final int IPC_CH2 = (int)0x40014508;
    public static final int IPC_CH3 = (int)0x4001450C;
    public static final int IPC_CH4 = (int)0x40014510;
    public static final int IPC_CH5 = (int)0x40014514;
    public static final int IPC_CH6 = (int)0x40014518;
    public static final int IPC_CH7 = (int)0x4001451C;
    public static final int IPC_INTEN = (int)0x40014700;

    // 中断向量定义
    public static final int IRQ_POWER = 0;  // Power
    public static final int IRQ_RADIO = 1;  // RADIO
    public static final int IRQ_UART0 = 2;  // UART0
    public static final int IRQ_UART1 = 3;  // UART1
    public static final int IRQ_SPI0 = 4;  // SPI0
    public static final int IRQ_SPI1 = 5;  // SPI1
    public static final int IRQ_SPI2 = 6;  // SPI2
    public static final int IRQ_GPIOTE = 7;  // GPIOTE
    public static final int IRQ_ADC = 8;  // ADC
    public static final int IRQ_TIMER0 = 9;  // TIMER0
    public static final int IRQ_TIMER1 = 10;  // TIMER1
    public static final int IRQ_TIMER2 = 11;  // TIMER2
    public static final int IRQ_TIMER3 = 12;  // TIMER3
    public static final int IRQ_TIMER4 = 13;  // TIMER4
    public static final int IRQ_RTC0 = 14;  // RTC0
    public static final int IRQ_RTC1 = 15;  // RTC1
    public static final int IRQ_TEMP = 16;  // TEMP
    public static final int IRQ_RNG = 17;  // RNG
    public static final int IRQ_WDT = 18;  // WDT
    public static final int IRQ_IPC = 19;  // IPC
    public static final int IRQ_PWM0 = 20;  // PWM0
    public static final int IRQ_PWM1 = 21;  // PWM1
    public static final int IRQ_PWM2 = 22;  // PWM2
    public static final int IRQ_PWM3 = 23;  // PWM3
    public static final int IRQ_ZAR = 24;  // RESERVED
    public static final int IRQ_EGU0 = 25;  // EGU0
    public static final int IRQ_EGU1 = 26;  // EGU1
    public static final int IRQ_EGU2 = 27;  // EGU2
    public static final int IRQ_EGU3 = 28;  // EGU3
    public static final int IRQ_EGU4 = 29;  // EGU4
    public static final int IRQ_EGU5 = 30;  // EGU5
    public static final int IRQ_RESERVED = 31;  // RESERVED
    public static final int IRQ_SPIM0 = 32;  // SPIM0
    public static final int IRQ_SPIM1 = 33;  // SPIM1
    public static final int IRQ_SPIM2 = 34;  // SPIM2
    public static final int IRQ_RESERVED = 35;  // RESERVED
    public static final int IRQ_RESERVED = 36;  // RESERVED
    public static final int IRQ_USB = 37;  // USB
    public static final int IRQ_RESERVED = 38;  // RESERVED
    public static final int IRQ_RESERVED = 39;  // RESERVED
    public static final int IRQ_RESERVED = 40;  // RESERVED
    public static final int IRQ_RESERVED = 41;  // RESERVED
    public static final int IRQ_RESERVED = 42;  // RESERVED
    public static final int IRQ_CRYPTOCELL = 43;  // CRYPTOCELL
    public static final int IRQ_RESERVED = 44;  // RESERVED
    public static final int IRQ_RESERVED = 45;  // RESERVED
    public static final int IRQ_RESERVED = 46;  // RESERVED
    public static final int IRQ_RESERVED = 47;  // RESERVED

    // 引脚定义
    public static final int PIN_VDD = 1;  // 3.3V power supply
    public static final int PIN_VDD = 2;  // 3.3V power supply
    public static final int PIN_DEC4 = 3;  // Decoupling 4
    public static final int PIN_DEC5 = 4;  // Decoupling 5
    public static final int PIN_P0_01 = 5;  // GPIO Port 0.01
    public static final int PIN_P0_02 = 6;  // GPIO Port 0.02
    public static final int PIN_P0_03 = 7;  // GPIO Port 0.03
    public static final int PIN_P0_04 = 8;  // GPIO Port 0.04
    public static final int PIN_P0_05 = 9;  // GPIO Port 0.05
    public static final int PIN_P0_06 = 10;  // GPIO Port 0.06
    public static final int PIN_P0_07 = 11;  // GPIO Port 0.07
    public static final int PIN_P0_08 = 12;  // GPIO Port 0.08
    public static final int PIN_P0_09 = 13;  // GPIO Port 0.09
    public static final int PIN_P0_10 = 14;  // GPIO Port 0.10
    public static final int PIN_P0_11 = 15;  // GPIO Port 0.11
    public static final int PIN_P0_12 = 16;  // GPIO Port 0.12
    public static final int PIN_P0_13 = 17;  // GPIO Port 0.13
    public static final int PIN_P0_14 = 18;  // GPIO Port 0.14
    public static final int PIN_P0_15 = 19;  // GPIO Port 0.15
    public static final int PIN_P0_16 = 20;  // GPIO Port 0.16
    public static final int PIN_P0_17 = 21;  // GPIO Port 0.17
    public static final int PIN_P0_18 = 22;  // GPIO Port 0.18
    public static final int PIN_P0_19 = 23;  // GPIO Port 0.19
    public static final int PIN_P0_20 = 24;  // GPIO Port 0.20
    public static final int PIN_P0_21 = 25;  // GPIO Port 0.21
    public static final int PIN_P0_22 = 26;  // GPIO Port 0.22
    public static final int PIN_P0_23 = 27;  // GPIO Port 0.23
    public static final int PIN_P0_24 = 28;  // GPIO Port 0.24
    public static final int PIN_P0_25 = 29;  // GPIO Port 0.25
    public static final int PIN_P0_26 = 30;  // GPIO Port 0.26
    public static final int PIN_P0_27 = 31;  // GPIO Port 0.27
    public static final int PIN_P0_28 = 32;  // GPIO Port 0.28
    public static final int PIN_P0_29 = 33;  // GPIO Port 0.29
    public static final int PIN_P0_30 = 34;  // GPIO Port 0.30
    public static final int PIN_P0_31 = 35;  // GPIO Port 0.31
    public static final int PIN_P1_00 = 36;  // GPIO Port 1.00
    public static final int PIN_P1_01 = 37;  // GPIO Port 1.01
    public static final int PIN_P1_02 = 38;  // GPIO Port 1.02
    public static final int PIN_P1_03 = 39;  // GPIO Port 1.03
    public static final int PIN_P1_04 = 40;  // GPIO Port 1.04
    public static final int PIN_P1_05 = 41;  // GPIO Port 1.05
    public static final int PIN_P1_06 = 42;  // GPIO Port 1.06
    public static final int PIN_P1_07 = 43;  // GPIO Port 1.07
    public static final int PIN_P1_08 = 44;  // GPIO Port 1.08
    public static final int PIN_P1_09 = 45;  // GPIO Port 1.09
    public static final int PIN_P1_10 = 46;  // GPIO Port 1.10
    public static final int PIN_P1_11 = 47;  // GPIO Port 1.11
    public static final int PIN_P1_12 = 48;  // GPIO Port 1.12
    public static final int PIN_P1_13 = 49;  // GPIO Port 1.13
    public static final int PIN_P1_14 = 50;  // GPIO Port 1.14
    public static final int PIN_P1_15 = 51;  // GPIO Port 1.15
    public static final int PIN_VDD = 52;  // 3.3V power supply
    public static final int PIN_VDD = 53;  // 3.3V power supply
    public static final int PIN_DEC1 = 54;  // Decoupling 1
    public static final int PIN_DEC2 = 55;  // Decoupling 2
    public static final int PIN_DEC3 = 56;  // Decoupling 3
    public static final int PIN_NFC1 = 57;  // NFC 1
    public static final int PIN_NFC2 = 58;  // NFC 2
    public static final int PIN_P0_16 = 59;  // GPIO Port 0.16
    public static final int PIN_SWDIO = 60;  // SWD I/O
    public static final int PIN_SWDCLK = 61;  // SWD Clock
    public static final int PIN_RESET = 62;  // Reset

    public static native void nrf52840_init();
}
