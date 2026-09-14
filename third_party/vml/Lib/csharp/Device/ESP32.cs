using System;

namespace VML.Device.Espressif.ESP32_WROOM_32
{
    /// <summary>
    /// ESP32-WROOM-32 寄存器定义
    /// 生成自: Espressif/ESP32/ESP32-WROOM-32
    /// 版本: 1.0
    /// </summary>
    public static class ESP32_WROOM_32
    {
        // CPU架构: Xtensa-LX6, 32位, 160000000 Hz

        // 寄存器定义
        // Program Counter
        public const int PC_ADDR = 0x00000000;
        public static unsafe uint* PC => (uint*)0x00000000;

        // General Purpose Register 0
        public const int A0_ADDR = 0x00000004;
        public static unsafe uint* A0 => (uint*)0x00000004;

        // Stack Pointer
        public const int A1_ADDR = 0x00000008;
        public static unsafe uint* A1 => (uint*)0x00000008;

        // General Purpose Register 2
        public const int A2_ADDR = 0x0000000C;
        public static unsafe uint* A2 => (uint*)0x0000000C;

        // General Purpose Register 3
        public const int A3_ADDR = 0x00000010;
        public static unsafe uint* A3 => (uint*)0x00000010;

        // General Purpose Register 4
        public const int A4_ADDR = 0x00000014;
        public static unsafe uint* A4 => (uint*)0x00000014;

        // General Purpose Register 5
        public const int A5_ADDR = 0x00000018;
        public static unsafe uint* A5 => (uint*)0x00000018;

        // General Purpose Register 6
        public const int A6_ADDR = 0x0000001C;
        public static unsafe uint* A6 => (uint*)0x0000001C;

        // General Purpose Register 7
        public const int A7_ADDR = 0x00000020;
        public static unsafe uint* A7 => (uint*)0x00000020;

        // General Purpose Register 8
        public const int A8_ADDR = 0x00000024;
        public static unsafe uint* A8 => (uint*)0x00000024;

        // General Purpose Register 9
        public const int A9_ADDR = 0x00000028;
        public static unsafe uint* A9 => (uint*)0x00000028;

        // General Purpose Register 10
        public const int A10_ADDR = 0x0000002C;
        public static unsafe uint* A10 => (uint*)0x0000002C;

        // General Purpose Register 11
        public const int A11_ADDR = 0x00000030;
        public static unsafe uint* A11 => (uint*)0x00000030;

        // General Purpose Register 12
        public const int A12_ADDR = 0x00000034;
        public static unsafe uint* A12 => (uint*)0x00000034;

        // General Purpose Register 13
        public const int A13_ADDR = 0x00000038;
        public static unsafe uint* A13 => (uint*)0x00000038;

        // General Purpose Register 14
        public const int A14_ADDR = 0x0000003C;
        public static unsafe uint* A14 => (uint*)0x0000003C;

        // General Purpose Register 15
        public const int A15_ADDR = 0x00000040;
        public static unsafe uint* A15 => (uint*)0x00000040;

        // Special Address Register 1
        public const int SAREG1_ADDR = 0x00000044;
        public static unsafe uint* SAREG1 => (uint*)0x00000044;

        // Special Address Register 2
        public const int SAREG2_ADDR = 0x00000048;
        public static unsafe uint* SAREG2 => (uint*)0x00000048;

        // Loop Beginning
        public const int LBEG_ADDR = 0x0000004C;
        public static unsafe uint* LBEG => (uint*)0x0000004C;

        // Loop End
        public const int LEND_ADDR = 0x00000050;
        public static unsafe uint* LEND => (uint*)0x00000050;

        // Loop Counter
        public const int LCOUNT_ADDR = 0x00000054;
        public static unsafe uint* LCOUNT => (uint*)0x00000054;

        // Processor Status
        public const int PS_ADDR = 0x00000058;
        public static unsafe uint* PS => (uint*)0x00000058;

        // Window Base
        public const int WINDOWBASE_ADDR = 0x0000005C;
        public static unsafe uint* WINDOWBASE => (uint*)0x0000005C;

        // Window Start
        public const int WINDOWSTART_ADDR = 0x00000060;
        public static unsafe uint* WINDOWSTART => (uint*)0x00000060;

        // Page Table Base
        public const int PTEBASE_ADDR = 0x00000064;
        public static unsafe uint* PTEBASE => (uint*)0x00000064;

        // Page Table Entry Size
        public const int PTESIZE_ADDR = 0x00000068;
        public static unsafe uint* PTESIZE => (uint*)0x00000068;

        // Special Compare 1
        public const int SCOMPARE1_ADDR = 0x0000006C;
        public static unsafe uint* SCOMPARE1 => (uint*)0x0000006C;

        // Atomic Operation Control
        public const int ATOMCTL_ADDR = 0x00000070;
        public static unsafe uint* ATOMCTL => (uint*)0x00000070;

        // Data Destination Register
        public const int DDR_ADDR = 0x00000074;
        public static unsafe uint* DDR => (uint*)0x00000074;

        // 内存段定义
        // ROM (224KB)
        public const int ROM_START = 0x40000000;
        public const int ROM_END = 0x4003FFFF;
        public const int ROM_SIZE = 262144;

        // SRAM (320KB total)
        public const int SRAM_START = 0x3FF00000;
        public const int SRAM_END = 0x3FF7FFFF;
        public const int SRAM_SIZE = 524288;

        // DRAM0 (128KB)
        public const int DRAM0_START = 0x3FF80000;
        public const int DRAM0_END = 0x3FF9FFFF;
        public const int DRAM0_SIZE = 131072;

        // IRAM0
        public const int IRAM0_START = 0x40000000;
        public const int IRAM0_END = 0x401FFFFF;
        public const int IRAM0_SIZE = 2097152;

        // External Flash (4MB)
        public const int FLASH_START = 0x40200000;
        public const int FLASH_END = 0x405FFFFF;
        public const int FLASH_SIZE = 4194304;

        // Peripheral Registers
        public const int PERIPHERAL_START = 0x3FF00000;
        public const int PERIPHERAL_END = 0x3FFBFFFF;
        public const int PERIPHERAL_SIZE = 786432;

        // GPIO
        public const int GPIO_START = 0x3FF44000;
        public const int GPIO_END = 0x3FF44FFF;
        public const int GPIO_SIZE = 4096;

        // 外设定义
        // GPIO
        public const int GPIO_BASE = 0x3FF44000;
        public static unsafe uint* GPIO_OUT => (uint*)0x3FF44000;
        public static unsafe uint* GPIO_OUT_W1TS => (uint*)0x3FF44008;
        public static unsafe uint* GPIO_OUT_W1TC => (uint*)0x3FF4400C;
        public static unsafe uint* GPIO_IN => (uint*)0x3FF4403C;
        public static unsafe uint* GPIO_STATUS => (uint*)0x3FF44024;
        public static unsafe uint* GPIO_STATUS_W1TS => (uint*)0x3FF44028;
        public static unsafe uint* GPIO_STATUS_W1TC => (uint*)0x3FF4402C;
        public static unsafe uint* GPIO_PIN => (uint*)0x3FF44040;
        public static unsafe uint* GPIO_ENABLE => (uint*)0x3FF44020;
        public static unsafe uint* GPIO_STRAP => (uint*)0x3FF44038;
        public static unsafe uint* GPIO_IN_NEXT => (uint*)0x3FF44044;

        // RTC GPIO
        public const int RTC_GPIO_BASE = 0x3FF48000;
        public static unsafe uint* RTC_GPIO_OUT => (uint*)0x3FF48000;
        public static unsafe uint* RTC_GPIO_OUT_W1TS => (uint*)0x3FF48008;
        public static unsafe uint* RTC_GPIO_OUT_W1TC => (uint*)0x3FF4800C;
        public static unsafe uint* RTC_GPIO_IN => (uint*)0x3FF48044;
        public static unsafe uint* RTC_GPIO_STATUS => (uint*)0x3FF48024;
        public static unsafe uint* RTC_GPIO_PIN => (uint*)0x3FF48048;
        public static unsafe uint* RTC_GPIO_ENABLE => (uint*)0x3FF48020;

        // IO MUX
        public const int IO_MUX_BASE = 0x3FF49000;
        public static unsafe uint* IO_MUX_GPIO0 => (uint*)0x3FF49000;
        public static unsafe uint* IO_MUX_GPIO1 => (uint*)0x3FF49004;
        public static unsafe uint* IO_MUX_GPIO2 => (uint*)0x3FF49008;
        public static unsafe uint* IO_MUX_GPIO3 => (uint*)0x3FF4900C;
        public static unsafe uint* IO_MUX_GPIO4 => (uint*)0x3FF49010;
        public static unsafe uint* IO_MUX_GPIO5 => (uint*)0x3FF49014;
        public static unsafe uint* IO_MUX_GPIO6 => (uint*)0x3FF49018;
        public static unsafe uint* IO_MUX_GPIO7 => (uint*)0x3FF4901C;
        public static unsafe uint* IO_MUX_GPIO8 => (uint*)0x3FF49020;
        public static unsafe uint* IO_MUX_GPIO9 => (uint*)0x3FF49024;
        public static unsafe uint* IO_MUX_GPIO10 => (uint*)0x3FF49028;
        public static unsafe uint* IO_MUX_GPIO11 => (uint*)0x3FF4902C;
        public static unsafe uint* IO_MUX_GPIO12 => (uint*)0x3FF49030;
        public static unsafe uint* IO_MUX_GPIO13 => (uint*)0x3FF49034;
        public static unsafe uint* IO_MUX_GPIO14 => (uint*)0x3FF49038;
        public static unsafe uint* IO_MUX_GPIO15 => (uint*)0x3FF4903C;
        public static unsafe uint* IO_MUX_GPIO16 => (uint*)0x3FF49040;
        public static unsafe uint* IO_MUX_GPIO17 => (uint*)0x3FF49044;
        public static unsafe uint* IO_MUX_GPIO18 => (uint*)0x3FF49048;
        public static unsafe uint* IO_MUX_GPIO19 => (uint*)0x3FF4904C;
        public static unsafe uint* IO_MUX_GPIO20 => (uint*)0x3FF49050;
        public static unsafe uint* IO_MUX_GPIO21 => (uint*)0x3FF49054;
        public static unsafe uint* IO_MUX_GPIO22 => (uint*)0x3FF49058;
        public static unsafe uint* IO_MUX_GPIO23 => (uint*)0x3FF4905C;
        public static unsafe uint* IO_MUX_GPIO24 => (uint*)0x3FF49060;
        public static unsafe uint* IO_MUX_GPIO25 => (uint*)0x3FF49064;
        public static unsafe uint* IO_MUX_GPIO26 => (uint*)0x3FF49068;
        public static unsafe uint* IO_MUX_GPIO27 => (uint*)0x3FF4906C;

        // UART 0
        public const int UART0_BASE = 0x3FF40000;
        public static unsafe uint* UART0_FIFO => (uint*)0x3FF40000;
        public static unsafe uint* UART0_INT_RAW => (uint*)0x3FF40004;
        public static unsafe uint* UART0_INT_ST => (uint*)0x3FF40008;
        public static unsafe uint* UART0_INT_ENA => (uint*)0x3FF4000C;
        public static unsafe uint* UART0_INT_CLR => (uint*)0x3FF40010;
        public static unsafe uint* UART0_CONF0 => (uint*)0x3FF40020;
        public static unsafe uint* UART0_CONF1 => (uint*)0x3FF40024;
        public static unsafe uint* UART0_LOWPULSE => (uint*)0x3FF40028;
        public static unsafe uint* UART0_HIGHPULSE => (uint*)0x3FF4002C;
        public static unsafe uint* UART0_PULSE_CNT => (uint*)0x3FF40030;
        public static unsafe uint* UART0_DATE => (uint*)0x3FF40078;
        public static unsafe uint* UART0_AHB_BIT => (uint*)0x3FF4007C;

        // UART 1
        public const int UART1_BASE = 0x3FF50000;
        public static unsafe uint* UART1_FIFO => (uint*)0x3FF50000;
        public static unsafe uint* UART1_INT_RAW => (uint*)0x3FF50004;
        public static unsafe uint* UART1_INT_ST => (uint*)0x3FF50008;
        public static unsafe uint* UART1_INT_ENA => (uint*)0x3FF5000C;
        public static unsafe uint* UART1_INT_CLR => (uint*)0x3FF50010;
        public static unsafe uint* UART1_CONF0 => (uint*)0x3FF50020;
        public static unsafe uint* UART1_CONF1 => (uint*)0x3FF50024;

        // UART 2
        public const int UART2_BASE = 0x3FF6E000;
        public static unsafe uint* UART2_FIFO => (uint*)0x3FF6E000;
        public static unsafe uint* UART2_INT_RAW => (uint*)0x3FF6E004;
        public static unsafe uint* UART2_INT_ST => (uint*)0x3FF6E008;
        public static unsafe uint* UART2_INT_ENA => (uint*)0x3FF6E00C;
        public static unsafe uint* UART2_INT_CLR => (uint*)0x3FF6E010;
        public static unsafe uint* UART2_CONF0 => (uint*)0x3FF6E020;
        public static unsafe uint* UART2_CONF1 => (uint*)0x3FF6E024;

        // SPI0 (Flash)
        public const int SPI0_BASE = 0x3FF42000;
        public static unsafe uint* SPI0_CMD => (uint*)0x3FF42000;
        public static unsafe uint* SPI0_ADDR => (uint*)0x3FF42004;
        public static unsafe uint* SPI0_CONTROL => (uint*)0x3FF42008;
        public static unsafe uint* SPI0_CONTROL1 => (uint*)0x3FF4200C;
        public static unsafe uint* SPI0_STATUS => (uint*)0x3FF42010;
        public static unsafe uint* SPI0_STATUS1 => (uint*)0x3FF42014;
        public static unsafe uint* SPI0_DATA => (uint*)0x3FF42020;
        public static unsafe uint* SPI0_USER => (uint*)0x3FF4203C;
        public static unsafe uint* SPI0_USER1 => (uint*)0x3FF42040;
        public static unsafe uint* SPI0_USER2 => (uint*)0x3FF42044;
        public static unsafe uint* SPI0_PIN => (uint*)0x3FF42048;
        public static unsafe uint* SPI0_SLAVE => (uint*)0x3FF4204C;
        public static unsafe uint* SPI0_CACHE_FLASH => (uint*)0x3FF42050;
        public static unsafe uint* SPI0_CLOCK => (uint*)0x3FF42058;
        public static unsafe uint* SPI0_FIFO => (uint*)0x3FF42060;

        // SPI1
        public const int SPI1_BASE = 0x3FF43000;
        public static unsafe uint* SPI1_CMD => (uint*)0x3FF43000;
        public static unsafe uint* SPI1_ADDR => (uint*)0x3FF43004;
        public static unsafe uint* SPI1_CONTROL => (uint*)0x3FF43008;
        public static unsafe uint* SPI1_STATUS => (uint*)0x3FF43010;
        public static unsafe uint* SPI1_DATA => (uint*)0x3FF43020;
        public static unsafe uint* SPI1_USER => (uint*)0x3FF4303C;
        public static unsafe uint* SPI1_CLOCK => (uint*)0x3FF43058;

        // SPI2 (HSPI)
        public const int SPI2_BASE = 0x3FF64000;
        public static unsafe uint* SPI2_CMD => (uint*)0x3FF64000;
        public static unsafe uint* SPI2_ADDR => (uint*)0x3FF64004;
        public static unsafe uint* SPI2_CONTROL => (uint*)0x3FF64008;
        public static unsafe uint* SPI2_STATUS => (uint*)0x3FF64010;
        public static unsafe uint* SPI2_DATA => (uint*)0x3FF64020;
        public static unsafe uint* SPI2_USER => (uint*)0x3FF6403C;
        public static unsafe uint* SPI2_CLOCK => (uint*)0x3FF64058;
        public static unsafe uint* SPI2_FIFO => (uint*)0x3FF64060;

        // I2C 0
        public const int I2C0_BASE = 0x3FF53000;
        public static unsafe uint* I2C0_SCL_START => (uint*)0x3FF53000;
        public static unsafe uint* I2C0_SCL_LOW => (uint*)0x3FF53004;
        public static unsafe uint* I2C0_SDA_START => (uint*)0x3FF53008;
        public static unsafe uint* I2C0_SDA_LOW => (uint*)0x3FF5300C;
        public static unsafe uint* I2C0_INT_ENA => (uint*)0x3FF53010;
        public static unsafe uint* I2C0_INT_CLR => (uint*)0x3FF53014;
        public static unsafe uint* I2C0_INT_RAW => (uint*)0x3FF53018;
        public static unsafe uint* I2C0_INT_STATUS => (uint*)0x3FF5301C;
        public static unsafe uint* I2C0_SCL_HIGH_PERIOD => (uint*)0x3FF53020;
        public static unsafe uint* I2C0_SCL_HIGH_PERIOD_S => (uint*)0x3FF53024;
        public static unsafe uint* I2C0_SCL_START_HOLD => (uint*)0x3FF53028;
        public static unsafe uint* I2C0_SDA_START_HOLD => (uint*)0x3FF5302C;
        public static unsafe uint* I2C0_SCL_LAST_HOLD => (uint*)0x3FF53030;
        public static unsafe uint* I2C0_SCL_WAIT_PERIOD => (uint*)0x3FF53034;
        public static unsafe uint* I2C0_CTR => (uint*)0x3FF53050;
        public static unsafe uint* I2C0_STATUS => (uint*)0x3FF53054;
        public static unsafe uint* I2C0_FINISH_INT_ENA => (uint*)0x3FF53058;
        public static unsafe uint* I2C0_COMMAND0 => (uint*)0x3FF53060;
        public static unsafe uint* I2C0_COMMAND1 => (uint*)0x3FF53064;
        public static unsafe uint* I2C0_COMMAND2 => (uint*)0x3FF53068;
        public static unsafe uint* I2C0_COMMAND3 => (uint*)0x3FF5306C;
        public static unsafe uint* I2C0_DATA => (uint*)0x3FF53080;

        // I2C 1
        public const int I2C1_BASE = 0x3FF67000;
        public static unsafe uint* I2C1_CTR => (uint*)0x3FF67050;
        public static unsafe uint* I2C1_DATA => (uint*)0x3FF67080;
        public static unsafe uint* I2C1_COMMAND0 => (uint*)0x3FF67060;
        public static unsafe uint* I2C1_COMMAND1 => (uint*)0x3FF67064;

        // Timer Group 0
        public const int TIMG0_BASE = 0x3FF5F000;
        public static unsafe uint* TIMG0_T0CONFIG => (uint*)0x3FF5F000;
        public static unsafe uint* TIMG0_T0LO => (uint*)0x3FF5F004;
        public static unsafe uint* TIMG0_T0HI => (uint*)0x3FF5F008;
        public static unsafe uint* TIMG0_T0UPDATE => (uint*)0x3FF5F00C;
        public static unsafe uint* TIMG0_T0ALARM => (uint*)0x3FF5F010;
        public static unsafe uint* TIMG0_T0LOAD => (uint*)0x3FF5F014;
        public static unsafe uint* TIMG0_T0LOAD_REG => (uint*)0x3FF5F018;

        // Timer Group 1
        public const int TIMG1_BASE = 0x3FF60000;
        public static unsafe uint* TIMG1_T0CONFIG => (uint*)0x3FF60000;
        public static unsafe uint* TIMG1_T0LO => (uint*)0x3FF60004;
        public static unsafe uint* TIMG1_T0HI => (uint*)0x3FF60008;
        public static unsafe uint* TIMG1_T0ALARM => (uint*)0x3FF60010;
        public static unsafe uint* TIMG1_T0LOAD => (uint*)0x3FF60014;

        // Motor Control PWM 0
        public const int PWM0_BASE = 0x3FF59000;
        public static unsafe uint* PWM0_CNT => (uint*)0x3FF59000;
        public static unsafe uint* PWM0_PERIOD => (uint*)0x3FF59004;
        public static unsafe uint* PWM0_DUTY => (uint*)0x3FF59008;
        public static unsafe uint* PWM0_CONFIG0 => (uint*)0x3FF59010;
        public static unsafe uint* PWM0_CONFIG1 => (uint*)0x3FF59014;
        public static unsafe uint* PWM0_CONFIG2 => (uint*)0x3FF59018;
        public static unsafe uint* PWM0_UPDATE => (uint*)0x3FF59020;

        // Motor Control PWM 1
        public const int PWM1_BASE = 0x3FF5A000;
        public static unsafe uint* PWM1_CNT => (uint*)0x3FF5A000;
        public static unsafe uint* PWM1_PERIOD => (uint*)0x3FF5A004;
        public static unsafe uint* PWM1_DUTY => (uint*)0x3FF5A008;

        // LED PWM Controller
        public const int LEDC_BASE = 0x3FF59000;
        public static unsafe uint* LEDC_CONFIG0 => (uint*)0x3FF59000;
        public static unsafe uint* LEDC_HPOINT0 => (uint*)0x3FF59018;
        public static unsafe uint* LEDC_DUTY0 => (uint*)0x3FF5901C;
        public static unsafe uint* LEDC_HPOINT1 => (uint*)0x3FF59028;
        public static unsafe uint* LEDC_DUTY1 => (uint*)0x3FF5902C;
        public static unsafe uint* LEDC_HPOINT2 => (uint*)0x3FF59038;
        public static unsafe uint* LEDC_DUTY2 => (uint*)0x3FF5903C;
        public static unsafe uint* LEDC_HPOINT3 => (uint*)0x3FF59048;
        public static unsafe uint* LEDC_DUTY3 => (uint*)0x3FF5904C;
        public static unsafe uint* LEDC_TIMER0_CONF => (uint*)0x3FF59000;
        public static unsafe uint* LEDC_TIMER0_LOAD => (uint*)0x3FF59004;

        // RTC Controller
        public const int RTC_BASE = 0x3FF48000;
        public static unsafe uint* RTC_RTC_CNTL => (uint*)0x3FF48000;
        public static unsafe uint* RTC_RTC_TIMER => (uint*)0x3FF4800C;
        public static unsafe uint* RTC_RTC_UPDATE => (uint*)0x3FF48010;
        public static unsafe uint* RTC_RTC_STATE0 => (uint*)0x3FF48080;

        // Wi-Fi
        public const int WIFI_BASE = 0x3FFAE000;
        public static unsafe uint* WIFI_MAC => (uint*)0x3FFAE000;
        public static unsafe uint* WIFI_CONFIG => (uint*)0x3FFAE100;

        // Bluetooth/BLE
        public const int BT_BASE = 0x3FFB0000;
        public static unsafe uint* BT_CONFIG => (uint*)0x3FFB0000;

        // SHA Hardware Accelerator
        public const int SHA_BASE = 0x3FF67000;
        public static unsafe uint* SHA_MODE => (uint*)0x3FF67000;
        public static unsafe uint* SHA_DATA => (uint*)0x3FF67004;
        public static unsafe uint* SHA_HASH => (uint*)0x3FF67008;

        // AES Hardware Accelerator
        public const int AES_BASE = 0x3FF68000;
        public static unsafe uint* AES_KEY => (uint*)0x3FF68000;
        public static unsafe uint* AES_DATA_IN => (uint*)0x3FF68004;
        public static unsafe uint* AES_DATA_OUT => (uint*)0x3FF68008;
        public static unsafe uint* AES_MODE => (uint*)0x3FF6800C;

        // Random Number Generator
        public const int RNG_BASE = 0x3FF75000;
        public static unsafe uint* RNG_DATA => (uint*)0x3FF75000;

        // eFuse Controller
        public const int EFUSE_BASE = 0x3FF5A000;
        public static unsafe uint* EFUSE_DATA0 => (uint*)0x3FF5A000;
        public static unsafe uint* EFUSE_DATA1 => (uint*)0x3FF5A004;
        public static unsafe uint* EFUSE_DATA2 => (uint*)0x3FF5A008;
        public static unsafe uint* EFUSE_DATA3 => (uint*)0x3FF5A00C;

        // 中断向量定义
        public const int IRQ_NMI = 0;  // Non-maskable interrupt
        public const int IRQ_SYS_SOFT = 1;  // Software interrupt
        public const int IRQ_TIMER_INTR0 = 2;  // Hardware timer 0
        public const int IRQ_TIMER_INTR1 = 3;  // Hardware timer 1
        public const int IRQ_TIMER_INTR2 = 4;  // Hardware timer 2
        public const int IRQ_TIMER_GROUP0 = 5;  // TG0 interrupt
        public const int IRQ_TIMER_GROUP1 = 6;  // TG1 interrupt
        public const int IRQ_GPIO = 7;  // GPIO interrupt
        public const int IRQ_GPIO_NMI = 8;  // GPIO NMI interrupt
        public const int IRQ_SPI0 = 9;  // SPI0 interrupt
        public const int IRQ_SPI1 = 10;  // SPI1 interrupt
        public const int IRQ_SPI2 = 11;  // SPI2 interrupt
        public const int IRQ_I2C0 = 12;  // I2C0 interrupt
        public const int IRQ_I2C1 = 13;  // I2C1 interrupt
        public const int IRQ_UART0 = 14;  // UART0 interrupt
        public const int IRQ_UART1 = 15;  // UART1 interrupt
        public const int IRQ_UART2 = 16;  // UART2 interrupt
        public const int IRQ_WDT = 17;  // Watchdog interrupt
        public const int IRQ_RTC = 18;  // RTC interrupt
        public const int IRQ_PWM0 = 19;  // PWM0 interrupt
        public const int IRQ_PWM1 = 20;  // PWM1 interrupt
        public const int IRQ_LEDC = 21;  // LEDC interrupt
        public const int IRQ_TOUCH = 22;  // Touch sensor interrupt
        public const int IRQ_SARADC = 23;  // SARADC interrupt
        public const int IRQ_MAX = 24;  // No. of CPU interrupts
        public const int IRQ_CORE_INTR0 = 25;  // Core 0 interrupt 0
        public const int IRQ_CORE_INTR1 = 26;  // Core 0 interrupt 1
        public const int IRQ_CORE_INTR2 = 27;  // Core 0 interrupt 2
        public const int IRQ_CORE_INTR3 = 28;  // Core 0 interrupt 3
        public const int IRQ_CORE_INTR4 = 29;  // Core 0 interrupt 4
        public const int IRQ_CORE_INTR5 = 30;  // Core 0 interrupt 5
        public const int IRQ_CORE_INTR6 = 31;  // Core 0 interrupt 6
        public const int IRQ_GPIO_INTERRUPT = 32;  // GPIO interrupt
        public const int IRQ_GPIO_INTERRUPT_NMI = 33;  // GPIO NMI interrupt

        // 引脚定义
        public const int PIN_VDD = 1;  // 3.3V Power Supply
        public const int PIN_EN = 2;  // Enable ( CHIP_PU )
        public const int PIN_SENSOR_VP = 3;  // GPIO36 - ADC1_CH0 - SENSOR_VP
        public const int PIN_SENSOR_VN = 4;  // GPIO37 - ADC1_CH1 - SENSOR_VN
        public const int PIN_IO34 = 5;  // GPIO34 - ADC1_CH6
        public const int PIN_IO35 = 6;  // GPIO35 - ADC1_CH7
        public const int PIN_IO32 = 7;  // GPIO32 - ADC1_CH4 - TOUCH_CH9
        public const int PIN_IO33 = 8;  // GPIO33 - ADC1_CH5 - TOUCH_CH8
        public const int PIN_IO25 = 9;  // GPIO25 - DAC1 - ADC2_CH8
        public const int PIN_IO26 = 10;  // GPIO26 - DAC2 - ADC2_CH9
        public const int PIN_IO27 = 11;  // GPIO27 - TOUCH_CH7 - ADC2_CH7
        public const int PIN_IO14 = 12;  // GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
        public const int PIN_IO12 = 13;  // GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
        public const int PIN_GND = 14;  // Ground
        public const int PIN_IO13 = 15;  // GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
        public const int PIN_SD2 = 16;  // GPIO9 - SD_DATA2
        public const int PIN_SD3 = 17;  // GPIO10 - SD_DATA3
        public const int PIN_CMD = 18;  // GPIO11 - SD_CMD
        public const int PIN_CLK = 19;  // GPIO6 - SD_CLK
        public const int PIN_SD0 = 20;  // GPIO7 - SD_DATA0
        public const int PIN_SD1 = 21;  // GPIO8 - SD_DATA1
        public const int PIN_IO15 = 22;  // GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
        public const int PIN_IO2 = 23;  // GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
        public const int PIN_IO0 = 24;  // GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
        public const int PIN_IO4 = 25;  // GPIO4 - ADC2_CH0 - TOUCH_CH1
        public const int PIN_IO16 = 26;  // GPIO16 - HSPI WP
        public const int PIN_IO17 = 27;  // GPIO17 - HSPI HD
        public const int PIN_IO5 = 28;  // GPIO5 - HSPI CS0
        public const int PIN_IO18 = 29;  // GPIO18 - VSPICLK
        public const int PIN_IO19 = 30;  // GPIO19 - VSPIQ
        public const int PIN_NC = 31;  // Not Connected
        public const int PIN_IO21 = 32;  // GPIO21
        public const int PIN_RXD0 = 33;  // GPIO3 - U0RXD
        public const int PIN_TXD0 = 34;  // GPIO1 - U0TXD
        public const int PIN_IO22 = 35;  // GPIO22
        public const int PIN_IO23 = 36;  // GPIO23 - VSPID
        public const int PIN_GND = 37;  // Ground
        public const int PIN_GND = 38;  // Ground

        public static void esp32_wroom_32_init()
        {
            // 硬件初始化代码
        }
    }
}
