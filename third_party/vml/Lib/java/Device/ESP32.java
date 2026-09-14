package vml.device.espressif.esp32_wroom_32;

/**
 * ESP32-WROOM-32 寄存器定义
 * 生成自: Espressif/ESP32/ESP32-WROOM-32
 * 版本: 1.0
 */
public final class ESP32_WROOM_32 {
    private ESP32_WROOM_32() {} // 工具类
    // CPU架构: Xtensa-LX6, 32位, 160000000 Hz

    // 寄存器定义
    // Program Counter
    public static final int PC_ADDR = (int)0x00000000;

    // General Purpose Register 0
    public static final int A0_ADDR = (int)0x00000004;

    // Stack Pointer
    public static final int A1_ADDR = (int)0x00000008;

    // General Purpose Register 2
    public static final int A2_ADDR = (int)0x0000000C;

    // General Purpose Register 3
    public static final int A3_ADDR = (int)0x00000010;

    // General Purpose Register 4
    public static final int A4_ADDR = (int)0x00000014;

    // General Purpose Register 5
    public static final int A5_ADDR = (int)0x00000018;

    // General Purpose Register 6
    public static final int A6_ADDR = (int)0x0000001C;

    // General Purpose Register 7
    public static final int A7_ADDR = (int)0x00000020;

    // General Purpose Register 8
    public static final int A8_ADDR = (int)0x00000024;

    // General Purpose Register 9
    public static final int A9_ADDR = (int)0x00000028;

    // General Purpose Register 10
    public static final int A10_ADDR = (int)0x0000002C;

    // General Purpose Register 11
    public static final int A11_ADDR = (int)0x00000030;

    // General Purpose Register 12
    public static final int A12_ADDR = (int)0x00000034;

    // General Purpose Register 13
    public static final int A13_ADDR = (int)0x00000038;

    // General Purpose Register 14
    public static final int A14_ADDR = (int)0x0000003C;

    // General Purpose Register 15
    public static final int A15_ADDR = (int)0x00000040;

    // Special Address Register 1
    public static final int SAREG1_ADDR = (int)0x00000044;

    // Special Address Register 2
    public static final int SAREG2_ADDR = (int)0x00000048;

    // Loop Beginning
    public static final int LBEG_ADDR = (int)0x0000004C;

    // Loop End
    public static final int LEND_ADDR = (int)0x00000050;

    // Loop Counter
    public static final int LCOUNT_ADDR = (int)0x00000054;

    // Processor Status
    public static final int PS_ADDR = (int)0x00000058;

    // Window Base
    public static final int WINDOWBASE_ADDR = (int)0x0000005C;

    // Window Start
    public static final int WINDOWSTART_ADDR = (int)0x00000060;

    // Page Table Base
    public static final int PTEBASE_ADDR = (int)0x00000064;

    // Page Table Entry Size
    public static final int PTESIZE_ADDR = (int)0x00000068;

    // Special Compare 1
    public static final int SCOMPARE1_ADDR = (int)0x0000006C;

    // Atomic Operation Control
    public static final int ATOMCTL_ADDR = (int)0x00000070;

    // Data Destination Register
    public static final int DDR_ADDR = (int)0x00000074;

    // 内存段定义
    // ROM (224KB)
    public static final int ROM_START = (int)0x40000000;
    public static final int ROM_END = (int)0x4003FFFF;
    public static final int ROM_SIZE = 262144;

    // SRAM (320KB total)
    public static final int SRAM_START = (int)0x3FF00000;
    public static final int SRAM_END = (int)0x3FF7FFFF;
    public static final int SRAM_SIZE = 524288;

    // DRAM0 (128KB)
    public static final int DRAM0_START = (int)0x3FF80000;
    public static final int DRAM0_END = (int)0x3FF9FFFF;
    public static final int DRAM0_SIZE = 131072;

    // IRAM0
    public static final int IRAM0_START = (int)0x40000000;
    public static final int IRAM0_END = (int)0x401FFFFF;
    public static final int IRAM0_SIZE = 2097152;

    // External Flash (4MB)
    public static final int FLASH_START = (int)0x40200000;
    public static final int FLASH_END = (int)0x405FFFFF;
    public static final int FLASH_SIZE = 4194304;

    // Peripheral Registers
    public static final int PERIPHERAL_START = (int)0x3FF00000;
    public static final int PERIPHERAL_END = (int)0x3FFBFFFF;
    public static final int PERIPHERAL_SIZE = 786432;

    // GPIO
    public static final int GPIO_START = (int)0x3FF44000;
    public static final int GPIO_END = (int)0x3FF44FFF;
    public static final int GPIO_SIZE = 4096;

    // 外设定义
    // GPIO
    public static final int GPIO_BASE = (int)0x3FF44000;
    public static final int GPIO_OUT = (int)0x3FF44000;
    public static final int GPIO_OUT_W1TS = (int)0x3FF44008;
    public static final int GPIO_OUT_W1TC = (int)0x3FF4400C;
    public static final int GPIO_IN = (int)0x3FF4403C;
    public static final int GPIO_STATUS = (int)0x3FF44024;
    public static final int GPIO_STATUS_W1TS = (int)0x3FF44028;
    public static final int GPIO_STATUS_W1TC = (int)0x3FF4402C;
    public static final int GPIO_PIN = (int)0x3FF44040;
    public static final int GPIO_ENABLE = (int)0x3FF44020;
    public static final int GPIO_STRAP = (int)0x3FF44038;
    public static final int GPIO_IN_NEXT = (int)0x3FF44044;

    // RTC GPIO
    public static final int RTC_GPIO_BASE = (int)0x3FF48000;
    public static final int RTC_GPIO_OUT = (int)0x3FF48000;
    public static final int RTC_GPIO_OUT_W1TS = (int)0x3FF48008;
    public static final int RTC_GPIO_OUT_W1TC = (int)0x3FF4800C;
    public static final int RTC_GPIO_IN = (int)0x3FF48044;
    public static final int RTC_GPIO_STATUS = (int)0x3FF48024;
    public static final int RTC_GPIO_PIN = (int)0x3FF48048;
    public static final int RTC_GPIO_ENABLE = (int)0x3FF48020;

    // IO MUX
    public static final int IO_MUX_BASE = (int)0x3FF49000;
    public static final int IO_MUX_GPIO0 = (int)0x3FF49000;
    public static final int IO_MUX_GPIO1 = (int)0x3FF49004;
    public static final int IO_MUX_GPIO2 = (int)0x3FF49008;
    public static final int IO_MUX_GPIO3 = (int)0x3FF4900C;
    public static final int IO_MUX_GPIO4 = (int)0x3FF49010;
    public static final int IO_MUX_GPIO5 = (int)0x3FF49014;
    public static final int IO_MUX_GPIO6 = (int)0x3FF49018;
    public static final int IO_MUX_GPIO7 = (int)0x3FF4901C;
    public static final int IO_MUX_GPIO8 = (int)0x3FF49020;
    public static final int IO_MUX_GPIO9 = (int)0x3FF49024;
    public static final int IO_MUX_GPIO10 = (int)0x3FF49028;
    public static final int IO_MUX_GPIO11 = (int)0x3FF4902C;
    public static final int IO_MUX_GPIO12 = (int)0x3FF49030;
    public static final int IO_MUX_GPIO13 = (int)0x3FF49034;
    public static final int IO_MUX_GPIO14 = (int)0x3FF49038;
    public static final int IO_MUX_GPIO15 = (int)0x3FF4903C;
    public static final int IO_MUX_GPIO16 = (int)0x3FF49040;
    public static final int IO_MUX_GPIO17 = (int)0x3FF49044;
    public static final int IO_MUX_GPIO18 = (int)0x3FF49048;
    public static final int IO_MUX_GPIO19 = (int)0x3FF4904C;
    public static final int IO_MUX_GPIO20 = (int)0x3FF49050;
    public static final int IO_MUX_GPIO21 = (int)0x3FF49054;
    public static final int IO_MUX_GPIO22 = (int)0x3FF49058;
    public static final int IO_MUX_GPIO23 = (int)0x3FF4905C;
    public static final int IO_MUX_GPIO24 = (int)0x3FF49060;
    public static final int IO_MUX_GPIO25 = (int)0x3FF49064;
    public static final int IO_MUX_GPIO26 = (int)0x3FF49068;
    public static final int IO_MUX_GPIO27 = (int)0x3FF4906C;

    // UART 0
    public static final int UART0_BASE = (int)0x3FF40000;
    public static final int UART0_FIFO = (int)0x3FF40000;
    public static final int UART0_INT_RAW = (int)0x3FF40004;
    public static final int UART0_INT_ST = (int)0x3FF40008;
    public static final int UART0_INT_ENA = (int)0x3FF4000C;
    public static final int UART0_INT_CLR = (int)0x3FF40010;
    public static final int UART0_CONF0 = (int)0x3FF40020;
    public static final int UART0_CONF1 = (int)0x3FF40024;
    public static final int UART0_LOWPULSE = (int)0x3FF40028;
    public static final int UART0_HIGHPULSE = (int)0x3FF4002C;
    public static final int UART0_PULSE_CNT = (int)0x3FF40030;
    public static final int UART0_DATE = (int)0x3FF40078;
    public static final int UART0_AHB_BIT = (int)0x3FF4007C;

    // UART 1
    public static final int UART1_BASE = (int)0x3FF50000;
    public static final int UART1_FIFO = (int)0x3FF50000;
    public static final int UART1_INT_RAW = (int)0x3FF50004;
    public static final int UART1_INT_ST = (int)0x3FF50008;
    public static final int UART1_INT_ENA = (int)0x3FF5000C;
    public static final int UART1_INT_CLR = (int)0x3FF50010;
    public static final int UART1_CONF0 = (int)0x3FF50020;
    public static final int UART1_CONF1 = (int)0x3FF50024;

    // UART 2
    public static final int UART2_BASE = (int)0x3FF6E000;
    public static final int UART2_FIFO = (int)0x3FF6E000;
    public static final int UART2_INT_RAW = (int)0x3FF6E004;
    public static final int UART2_INT_ST = (int)0x3FF6E008;
    public static final int UART2_INT_ENA = (int)0x3FF6E00C;
    public static final int UART2_INT_CLR = (int)0x3FF6E010;
    public static final int UART2_CONF0 = (int)0x3FF6E020;
    public static final int UART2_CONF1 = (int)0x3FF6E024;

    // SPI0 (Flash)
    public static final int SPI0_BASE = (int)0x3FF42000;
    public static final int SPI0_CMD = (int)0x3FF42000;
    public static final int SPI0_ADDR = (int)0x3FF42004;
    public static final int SPI0_CONTROL = (int)0x3FF42008;
    public static final int SPI0_CONTROL1 = (int)0x3FF4200C;
    public static final int SPI0_STATUS = (int)0x3FF42010;
    public static final int SPI0_STATUS1 = (int)0x3FF42014;
    public static final int SPI0_DATA = (int)0x3FF42020;
    public static final int SPI0_USER = (int)0x3FF4203C;
    public static final int SPI0_USER1 = (int)0x3FF42040;
    public static final int SPI0_USER2 = (int)0x3FF42044;
    public static final int SPI0_PIN = (int)0x3FF42048;
    public static final int SPI0_SLAVE = (int)0x3FF4204C;
    public static final int SPI0_CACHE_FLASH = (int)0x3FF42050;
    public static final int SPI0_CLOCK = (int)0x3FF42058;
    public static final int SPI0_FIFO = (int)0x3FF42060;

    // SPI1
    public static final int SPI1_BASE = (int)0x3FF43000;
    public static final int SPI1_CMD = (int)0x3FF43000;
    public static final int SPI1_ADDR = (int)0x3FF43004;
    public static final int SPI1_CONTROL = (int)0x3FF43008;
    public static final int SPI1_STATUS = (int)0x3FF43010;
    public static final int SPI1_DATA = (int)0x3FF43020;
    public static final int SPI1_USER = (int)0x3FF4303C;
    public static final int SPI1_CLOCK = (int)0x3FF43058;

    // SPI2 (HSPI)
    public static final int SPI2_BASE = (int)0x3FF64000;
    public static final int SPI2_CMD = (int)0x3FF64000;
    public static final int SPI2_ADDR = (int)0x3FF64004;
    public static final int SPI2_CONTROL = (int)0x3FF64008;
    public static final int SPI2_STATUS = (int)0x3FF64010;
    public static final int SPI2_DATA = (int)0x3FF64020;
    public static final int SPI2_USER = (int)0x3FF6403C;
    public static final int SPI2_CLOCK = (int)0x3FF64058;
    public static final int SPI2_FIFO = (int)0x3FF64060;

    // I2C 0
    public static final int I2C0_BASE = (int)0x3FF53000;
    public static final int I2C0_SCL_START = (int)0x3FF53000;
    public static final int I2C0_SCL_LOW = (int)0x3FF53004;
    public static final int I2C0_SDA_START = (int)0x3FF53008;
    public static final int I2C0_SDA_LOW = (int)0x3FF5300C;
    public static final int I2C0_INT_ENA = (int)0x3FF53010;
    public static final int I2C0_INT_CLR = (int)0x3FF53014;
    public static final int I2C0_INT_RAW = (int)0x3FF53018;
    public static final int I2C0_INT_STATUS = (int)0x3FF5301C;
    public static final int I2C0_SCL_HIGH_PERIOD = (int)0x3FF53020;
    public static final int I2C0_SCL_HIGH_PERIOD_S = (int)0x3FF53024;
    public static final int I2C0_SCL_START_HOLD = (int)0x3FF53028;
    public static final int I2C0_SDA_START_HOLD = (int)0x3FF5302C;
    public static final int I2C0_SCL_LAST_HOLD = (int)0x3FF53030;
    public static final int I2C0_SCL_WAIT_PERIOD = (int)0x3FF53034;
    public static final int I2C0_CTR = (int)0x3FF53050;
    public static final int I2C0_STATUS = (int)0x3FF53054;
    public static final int I2C0_FINISH_INT_ENA = (int)0x3FF53058;
    public static final int I2C0_COMMAND0 = (int)0x3FF53060;
    public static final int I2C0_COMMAND1 = (int)0x3FF53064;
    public static final int I2C0_COMMAND2 = (int)0x3FF53068;
    public static final int I2C0_COMMAND3 = (int)0x3FF5306C;
    public static final int I2C0_DATA = (int)0x3FF53080;

    // I2C 1
    public static final int I2C1_BASE = (int)0x3FF67000;
    public static final int I2C1_CTR = (int)0x3FF67050;
    public static final int I2C1_DATA = (int)0x3FF67080;
    public static final int I2C1_COMMAND0 = (int)0x3FF67060;
    public static final int I2C1_COMMAND1 = (int)0x3FF67064;

    // Timer Group 0
    public static final int TIMG0_BASE = (int)0x3FF5F000;
    public static final int TIMG0_T0CONFIG = (int)0x3FF5F000;
    public static final int TIMG0_T0LO = (int)0x3FF5F004;
    public static final int TIMG0_T0HI = (int)0x3FF5F008;
    public static final int TIMG0_T0UPDATE = (int)0x3FF5F00C;
    public static final int TIMG0_T0ALARM = (int)0x3FF5F010;
    public static final int TIMG0_T0LOAD = (int)0x3FF5F014;
    public static final int TIMG0_T0LOAD_REG = (int)0x3FF5F018;

    // Timer Group 1
    public static final int TIMG1_BASE = (int)0x3FF60000;
    public static final int TIMG1_T0CONFIG = (int)0x3FF60000;
    public static final int TIMG1_T0LO = (int)0x3FF60004;
    public static final int TIMG1_T0HI = (int)0x3FF60008;
    public static final int TIMG1_T0ALARM = (int)0x3FF60010;
    public static final int TIMG1_T0LOAD = (int)0x3FF60014;

    // Motor Control PWM 0
    public static final int PWM0_BASE = (int)0x3FF59000;
    public static final int PWM0_CNT = (int)0x3FF59000;
    public static final int PWM0_PERIOD = (int)0x3FF59004;
    public static final int PWM0_DUTY = (int)0x3FF59008;
    public static final int PWM0_CONFIG0 = (int)0x3FF59010;
    public static final int PWM0_CONFIG1 = (int)0x3FF59014;
    public static final int PWM0_CONFIG2 = (int)0x3FF59018;
    public static final int PWM0_UPDATE = (int)0x3FF59020;

    // Motor Control PWM 1
    public static final int PWM1_BASE = (int)0x3FF5A000;
    public static final int PWM1_CNT = (int)0x3FF5A000;
    public static final int PWM1_PERIOD = (int)0x3FF5A004;
    public static final int PWM1_DUTY = (int)0x3FF5A008;

    // LED PWM Controller
    public static final int LEDC_BASE = (int)0x3FF59000;
    public static final int LEDC_CONFIG0 = (int)0x3FF59000;
    public static final int LEDC_HPOINT0 = (int)0x3FF59018;
    public static final int LEDC_DUTY0 = (int)0x3FF5901C;
    public static final int LEDC_HPOINT1 = (int)0x3FF59028;
    public static final int LEDC_DUTY1 = (int)0x3FF5902C;
    public static final int LEDC_HPOINT2 = (int)0x3FF59038;
    public static final int LEDC_DUTY2 = (int)0x3FF5903C;
    public static final int LEDC_HPOINT3 = (int)0x3FF59048;
    public static final int LEDC_DUTY3 = (int)0x3FF5904C;
    public static final int LEDC_TIMER0_CONF = (int)0x3FF59000;
    public static final int LEDC_TIMER0_LOAD = (int)0x3FF59004;

    // RTC Controller
    public static final int RTC_BASE = (int)0x3FF48000;
    public static final int RTC_RTC_CNTL = (int)0x3FF48000;
    public static final int RTC_RTC_TIMER = (int)0x3FF4800C;
    public static final int RTC_RTC_UPDATE = (int)0x3FF48010;
    public static final int RTC_RTC_STATE0 = (int)0x3FF48080;

    // Wi-Fi
    public static final int WIFI_BASE = (int)0x3FFAE000;
    public static final int WIFI_MAC = (int)0x3FFAE000;
    public static final int WIFI_CONFIG = (int)0x3FFAE100;

    // Bluetooth/BLE
    public static final int BT_BASE = (int)0x3FFB0000;
    public static final int BT_CONFIG = (int)0x3FFB0000;

    // SHA Hardware Accelerator
    public static final int SHA_BASE = (int)0x3FF67000;
    public static final int SHA_MODE = (int)0x3FF67000;
    public static final int SHA_DATA = (int)0x3FF67004;
    public static final int SHA_HASH = (int)0x3FF67008;

    // AES Hardware Accelerator
    public static final int AES_BASE = (int)0x3FF68000;
    public static final int AES_KEY = (int)0x3FF68000;
    public static final int AES_DATA_IN = (int)0x3FF68004;
    public static final int AES_DATA_OUT = (int)0x3FF68008;
    public static final int AES_MODE = (int)0x3FF6800C;

    // Random Number Generator
    public static final int RNG_BASE = (int)0x3FF75000;
    public static final int RNG_DATA = (int)0x3FF75000;

    // eFuse Controller
    public static final int EFUSE_BASE = (int)0x3FF5A000;
    public static final int EFUSE_DATA0 = (int)0x3FF5A000;
    public static final int EFUSE_DATA1 = (int)0x3FF5A004;
    public static final int EFUSE_DATA2 = (int)0x3FF5A008;
    public static final int EFUSE_DATA3 = (int)0x3FF5A00C;

    // 中断向量定义
    public static final int IRQ_NMI = 0;  // Non-maskable interrupt
    public static final int IRQ_SYS_SOFT = 1;  // Software interrupt
    public static final int IRQ_TIMER_INTR0 = 2;  // Hardware timer 0
    public static final int IRQ_TIMER_INTR1 = 3;  // Hardware timer 1
    public static final int IRQ_TIMER_INTR2 = 4;  // Hardware timer 2
    public static final int IRQ_TIMER_GROUP0 = 5;  // TG0 interrupt
    public static final int IRQ_TIMER_GROUP1 = 6;  // TG1 interrupt
    public static final int IRQ_GPIO = 7;  // GPIO interrupt
    public static final int IRQ_GPIO_NMI = 8;  // GPIO NMI interrupt
    public static final int IRQ_SPI0 = 9;  // SPI0 interrupt
    public static final int IRQ_SPI1 = 10;  // SPI1 interrupt
    public static final int IRQ_SPI2 = 11;  // SPI2 interrupt
    public static final int IRQ_I2C0 = 12;  // I2C0 interrupt
    public static final int IRQ_I2C1 = 13;  // I2C1 interrupt
    public static final int IRQ_UART0 = 14;  // UART0 interrupt
    public static final int IRQ_UART1 = 15;  // UART1 interrupt
    public static final int IRQ_UART2 = 16;  // UART2 interrupt
    public static final int IRQ_WDT = 17;  // Watchdog interrupt
    public static final int IRQ_RTC = 18;  // RTC interrupt
    public static final int IRQ_PWM0 = 19;  // PWM0 interrupt
    public static final int IRQ_PWM1 = 20;  // PWM1 interrupt
    public static final int IRQ_LEDC = 21;  // LEDC interrupt
    public static final int IRQ_TOUCH = 22;  // Touch sensor interrupt
    public static final int IRQ_SARADC = 23;  // SARADC interrupt
    public static final int IRQ_MAX = 24;  // No. of CPU interrupts
    public static final int IRQ_CORE_INTR0 = 25;  // Core 0 interrupt 0
    public static final int IRQ_CORE_INTR1 = 26;  // Core 0 interrupt 1
    public static final int IRQ_CORE_INTR2 = 27;  // Core 0 interrupt 2
    public static final int IRQ_CORE_INTR3 = 28;  // Core 0 interrupt 3
    public static final int IRQ_CORE_INTR4 = 29;  // Core 0 interrupt 4
    public static final int IRQ_CORE_INTR5 = 30;  // Core 0 interrupt 5
    public static final int IRQ_CORE_INTR6 = 31;  // Core 0 interrupt 6
    public static final int IRQ_GPIO_INTERRUPT = 32;  // GPIO interrupt
    public static final int IRQ_GPIO_INTERRUPT_NMI = 33;  // GPIO NMI interrupt

    // 引脚定义
    public static final int PIN_VDD = 1;  // 3.3V Power Supply
    public static final int PIN_EN = 2;  // Enable ( CHIP_PU )
    public static final int PIN_SENSOR_VP = 3;  // GPIO36 - ADC1_CH0 - SENSOR_VP
    public static final int PIN_SENSOR_VN = 4;  // GPIO37 - ADC1_CH1 - SENSOR_VN
    public static final int PIN_IO34 = 5;  // GPIO34 - ADC1_CH6
    public static final int PIN_IO35 = 6;  // GPIO35 - ADC1_CH7
    public static final int PIN_IO32 = 7;  // GPIO32 - ADC1_CH4 - TOUCH_CH9
    public static final int PIN_IO33 = 8;  // GPIO33 - ADC1_CH5 - TOUCH_CH8
    public static final int PIN_IO25 = 9;  // GPIO25 - DAC1 - ADC2_CH8
    public static final int PIN_IO26 = 10;  // GPIO26 - DAC2 - ADC2_CH9
    public static final int PIN_IO27 = 11;  // GPIO27 - TOUCH_CH7 - ADC2_CH7
    public static final int PIN_IO14 = 12;  // GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
    public static final int PIN_IO12 = 13;  // GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
    public static final int PIN_GND = 14;  // Ground
    public static final int PIN_IO13 = 15;  // GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
    public static final int PIN_SD2 = 16;  // GPIO9 - SD_DATA2
    public static final int PIN_SD3 = 17;  // GPIO10 - SD_DATA3
    public static final int PIN_CMD = 18;  // GPIO11 - SD_CMD
    public static final int PIN_CLK = 19;  // GPIO6 - SD_CLK
    public static final int PIN_SD0 = 20;  // GPIO7 - SD_DATA0
    public static final int PIN_SD1 = 21;  // GPIO8 - SD_DATA1
    public static final int PIN_IO15 = 22;  // GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
    public static final int PIN_IO2 = 23;  // GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
    public static final int PIN_IO0 = 24;  // GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
    public static final int PIN_IO4 = 25;  // GPIO4 - ADC2_CH0 - TOUCH_CH1
    public static final int PIN_IO16 = 26;  // GPIO16 - HSPI WP
    public static final int PIN_IO17 = 27;  // GPIO17 - HSPI HD
    public static final int PIN_IO5 = 28;  // GPIO5 - HSPI CS0
    public static final int PIN_IO18 = 29;  // GPIO18 - VSPICLK
    public static final int PIN_IO19 = 30;  // GPIO19 - VSPIQ
    public static final int PIN_NC = 31;  // Not Connected
    public static final int PIN_IO21 = 32;  // GPIO21
    public static final int PIN_RXD0 = 33;  // GPIO3 - U0RXD
    public static final int PIN_TXD0 = 34;  // GPIO1 - U0TXD
    public static final int PIN_IO22 = 35;  // GPIO22
    public static final int PIN_IO23 = 36;  // GPIO23 - VSPID
    public static final int PIN_GND = 37;  // Ground
    public static final int PIN_GND = 38;  // Ground

    public static native void esp32_wroom_32_init();
}
