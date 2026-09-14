// ESP32-WROOM-32 设备定义 - Dart 库
// 生成自: Espressif/ESP32/ESP32-WROOM-32
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Dual-core Xtensa LX6 Wi-Fi and Bluetooth/BLE SoC with 4MB Flash
// CPU架构: Xtensa-LX6
// 位宽: 32位
// 时钟频率: 160000000 Hz

class ESP32_WROOM_32Device {
  static const String deviceName = "ESP32-WROOM-32";
  static const String manufacturer = "Espressif";
  static const String family = "ESP32";
  static const String version = "1.0";
  static const String architecture = "Xtensa-LX6";
  static const int bits = 32;
  static const int clockFrequency = 160000000;

  // 寄存器地址定义
  static const int PC_ADDR = 0x00000000;  // Program Counter
  static const int A0_ADDR = 0x00000004;  // General Purpose Register 0
  static const int A1_ADDR = 0x00000008;  // Stack Pointer
  static const int A2_ADDR = 0x0000000C;  // General Purpose Register 2
  static const int A3_ADDR = 0x00000010;  // General Purpose Register 3
  static const int A4_ADDR = 0x00000014;  // General Purpose Register 4
  static const int A5_ADDR = 0x00000018;  // General Purpose Register 5
  static const int A6_ADDR = 0x0000001C;  // General Purpose Register 6
  static const int A7_ADDR = 0x00000020;  // General Purpose Register 7
  static const int A8_ADDR = 0x00000024;  // General Purpose Register 8
  static const int A9_ADDR = 0x00000028;  // General Purpose Register 9
  static const int A10_ADDR = 0x0000002C;  // General Purpose Register 10
  static const int A11_ADDR = 0x00000030;  // General Purpose Register 11
  static const int A12_ADDR = 0x00000034;  // General Purpose Register 12
  static const int A13_ADDR = 0x00000038;  // General Purpose Register 13
  static const int A14_ADDR = 0x0000003C;  // General Purpose Register 14
  static const int A15_ADDR = 0x00000040;  // General Purpose Register 15
  static const int SAREG1_ADDR = 0x00000044;  // Special Address Register 1
  static const int SAREG2_ADDR = 0x00000048;  // Special Address Register 2
  static const int LBEG_ADDR = 0x0000004C;  // Loop Beginning
  static const int LEND_ADDR = 0x00000050;  // Loop End
  static const int LCOUNT_ADDR = 0x00000054;  // Loop Counter
  static const int PS_ADDR = 0x00000058;  // Processor Status
  static const int WINDOWBASE_ADDR = 0x0000005C;  // Window Base
  static const int WINDOWSTART_ADDR = 0x00000060;  // Window Start
  static const int PTEBASE_ADDR = 0x00000064;  // Page Table Base
  static const int PTESIZE_ADDR = 0x00000068;  // Page Table Entry Size
  static const int SCOMPARE1_ADDR = 0x0000006C;  // Special Compare 1
  static const int ATOMCTL_ADDR = 0x00000070;  // Atomic Operation Control
  static const int DDR_ADDR = 0x00000074;  // Data Destination Register

  // 内存段定义
  static const int ROM_START = 0x40000000;
  static const int ROM_END = 0x4003FFFF;
  static const int ROM_SIZE = 262144;  // ROM (224KB)
  static const int SRAM_START = 0x3FF00000;
  static const int SRAM_END = 0x3FF7FFFF;
  static const int SRAM_SIZE = 524288;  // SRAM (320KB total)
  static const int DRAM0_START = 0x3FF80000;
  static const int DRAM0_END = 0x3FF9FFFF;
  static const int DRAM0_SIZE = 131072;  // DRAM0 (128KB)
  static const int IRAM0_START = 0x40000000;
  static const int IRAM0_END = 0x401FFFFF;
  static const int IRAM0_SIZE = 2097152;  // IRAM0
  static const int FLASH_START = 0x40200000;
  static const int FLASH_END = 0x405FFFFF;
  static const int FLASH_SIZE = 4194304;  // External Flash (4MB)
  static const int PERIPHERAL_START = 0x3FF00000;
  static const int PERIPHERAL_END = 0x3FFBFFFF;
  static const int PERIPHERAL_SIZE = 786432;  // Peripheral Registers
  static const int GPIO_START = 0x3FF44000;
  static const int GPIO_END = 0x3FF44FFF;
  static const int GPIO_SIZE = 4096;  // GPIO

  // 外设定义
  // GPIO
  static const int GPIO_BASE = 0x3FF44000;
  static const int GPIO_OUT_ADDR = 0x0000;
  static const int GPIO_OUT_W1TS_ADDR = 0x0008;
  static const int GPIO_OUT_W1TC_ADDR = 0x000C;
  static const int GPIO_IN_ADDR = 0x003C;
  static const int GPIO_STATUS_ADDR = 0x0024;
  static const int GPIO_STATUS_W1TS_ADDR = 0x0028;
  static const int GPIO_STATUS_W1TC_ADDR = 0x002C;
  static const int GPIO_PIN_ADDR = 0x0040;
  static const int GPIO_ENABLE_ADDR = 0x0020;
  static const int GPIO_STRAP_ADDR = 0x0038;
  static const int GPIO_IN_NEXT_ADDR = 0x0044;
  // RTC GPIO
  static const int RTC_GPIO_BASE = 0x3FF48000;
  static const int RTC_GPIO_OUT_ADDR = 0x0000;
  static const int RTC_GPIO_OUT_W1TS_ADDR = 0x0008;
  static const int RTC_GPIO_OUT_W1TC_ADDR = 0x000C;
  static const int RTC_GPIO_IN_ADDR = 0x0044;
  static const int RTC_GPIO_STATUS_ADDR = 0x0024;
  static const int RTC_GPIO_PIN_ADDR = 0x0048;
  static const int RTC_GPIO_ENABLE_ADDR = 0x0020;
  // IO MUX
  static const int IO_MUX_BASE = 0x3FF49000;
  static const int IO_MUX_GPIO0_ADDR = 0x0000;
  static const int IO_MUX_GPIO1_ADDR = 0x0004;
  static const int IO_MUX_GPIO2_ADDR = 0x0008;
  static const int IO_MUX_GPIO3_ADDR = 0x000C;
  static const int IO_MUX_GPIO4_ADDR = 0x0010;
  static const int IO_MUX_GPIO5_ADDR = 0x0014;
  static const int IO_MUX_GPIO6_ADDR = 0x0018;
  static const int IO_MUX_GPIO7_ADDR = 0x001C;
  static const int IO_MUX_GPIO8_ADDR = 0x0020;
  static const int IO_MUX_GPIO9_ADDR = 0x0024;
  static const int IO_MUX_GPIO10_ADDR = 0x0028;
  static const int IO_MUX_GPIO11_ADDR = 0x002C;
  static const int IO_MUX_GPIO12_ADDR = 0x0030;
  static const int IO_MUX_GPIO13_ADDR = 0x0034;
  static const int IO_MUX_GPIO14_ADDR = 0x0038;
  static const int IO_MUX_GPIO15_ADDR = 0x003C;
  static const int IO_MUX_GPIO16_ADDR = 0x0040;
  static const int IO_MUX_GPIO17_ADDR = 0x0044;
  static const int IO_MUX_GPIO18_ADDR = 0x0048;
  static const int IO_MUX_GPIO19_ADDR = 0x004C;
  static const int IO_MUX_GPIO20_ADDR = 0x0050;
  static const int IO_MUX_GPIO21_ADDR = 0x0054;
  static const int IO_MUX_GPIO22_ADDR = 0x0058;
  static const int IO_MUX_GPIO23_ADDR = 0x005C;
  static const int IO_MUX_GPIO24_ADDR = 0x0060;
  static const int IO_MUX_GPIO25_ADDR = 0x0064;
  static const int IO_MUX_GPIO26_ADDR = 0x0068;
  static const int IO_MUX_GPIO27_ADDR = 0x006C;
  // UART 0
  static const int UART0_BASE = 0x3FF40000;
  static const int UART0_FIFO_ADDR = 0x0000;
  static const int UART0_INT_RAW_ADDR = 0x0004;
  static const int UART0_INT_ST_ADDR = 0x0008;
  static const int UART0_INT_ENA_ADDR = 0x000C;
  static const int UART0_INT_CLR_ADDR = 0x0010;
  static const int UART0_CONF0_ADDR = 0x0020;
  static const int UART0_CONF1_ADDR = 0x0024;
  static const int UART0_LOWPULSE_ADDR = 0x0028;
  static const int UART0_HIGHPULSE_ADDR = 0x002C;
  static const int UART0_PULSE_CNT_ADDR = 0x0030;
  static const int UART0_DATE_ADDR = 0x0078;
  static const int UART0_AHB_BIT_ADDR = 0x007C;
  // UART 1
  static const int UART1_BASE = 0x3FF50000;
  static const int UART1_FIFO_ADDR = 0x0000;
  static const int UART1_INT_RAW_ADDR = 0x0004;
  static const int UART1_INT_ST_ADDR = 0x0008;
  static const int UART1_INT_ENA_ADDR = 0x000C;
  static const int UART1_INT_CLR_ADDR = 0x0010;
  static const int UART1_CONF0_ADDR = 0x0020;
  static const int UART1_CONF1_ADDR = 0x0024;
  // UART 2
  static const int UART2_BASE = 0x3FF6E000;
  static const int UART2_FIFO_ADDR = 0x0000;
  static const int UART2_INT_RAW_ADDR = 0x0004;
  static const int UART2_INT_ST_ADDR = 0x0008;
  static const int UART2_INT_ENA_ADDR = 0x000C;
  static const int UART2_INT_CLR_ADDR = 0x0010;
  static const int UART2_CONF0_ADDR = 0x0020;
  static const int UART2_CONF1_ADDR = 0x0024;
  // SPI0 (Flash)
  static const int SPI0_BASE = 0x3FF42000;
  static const int SPI0_CMD_ADDR = 0x0000;
  static const int SPI0_ADDR_ADDR = 0x0004;
  static const int SPI0_CONTROL_ADDR = 0x0008;
  static const int SPI0_CONTROL1_ADDR = 0x000C;
  static const int SPI0_STATUS_ADDR = 0x0010;
  static const int SPI0_STATUS1_ADDR = 0x0014;
  static const int SPI0_DATA_ADDR = 0x0020;
  static const int SPI0_USER_ADDR = 0x003C;
  static const int SPI0_USER1_ADDR = 0x0040;
  static const int SPI0_USER2_ADDR = 0x0044;
  static const int SPI0_PIN_ADDR = 0x0048;
  static const int SPI0_SLAVE_ADDR = 0x004C;
  static const int SPI0_CACHE_FLASH_ADDR = 0x0050;
  static const int SPI0_CLOCK_ADDR = 0x0058;
  static const int SPI0_FIFO_ADDR = 0x0060;
  // SPI1
  static const int SPI1_BASE = 0x3FF43000;
  static const int SPI1_CMD_ADDR = 0x0000;
  static const int SPI1_ADDR_ADDR = 0x0004;
  static const int SPI1_CONTROL_ADDR = 0x0008;
  static const int SPI1_STATUS_ADDR = 0x0010;
  static const int SPI1_DATA_ADDR = 0x0020;
  static const int SPI1_USER_ADDR = 0x003C;
  static const int SPI1_CLOCK_ADDR = 0x0058;
  // SPI2 (HSPI)
  static const int SPI2_BASE = 0x3FF64000;
  static const int SPI2_CMD_ADDR = 0x0000;
  static const int SPI2_ADDR_ADDR = 0x0004;
  static const int SPI2_CONTROL_ADDR = 0x0008;
  static const int SPI2_STATUS_ADDR = 0x0010;
  static const int SPI2_DATA_ADDR = 0x0020;
  static const int SPI2_USER_ADDR = 0x003C;
  static const int SPI2_CLOCK_ADDR = 0x0058;
  static const int SPI2_FIFO_ADDR = 0x0060;
  // I2C 0
  static const int I2C0_BASE = 0x3FF53000;
  static const int I2C0_SCL_START_ADDR = 0x0000;
  static const int I2C0_SCL_LOW_ADDR = 0x0004;
  static const int I2C0_SDA_START_ADDR = 0x0008;
  static const int I2C0_SDA_LOW_ADDR = 0x000C;
  static const int I2C0_INT_ENA_ADDR = 0x0010;
  static const int I2C0_INT_CLR_ADDR = 0x0014;
  static const int I2C0_INT_RAW_ADDR = 0x0018;
  static const int I2C0_INT_STATUS_ADDR = 0x001C;
  static const int I2C0_SCL_HIGH_PERIOD_ADDR = 0x0020;
  static const int I2C0_SCL_HIGH_PERIOD_S_ADDR = 0x0024;
  static const int I2C0_SCL_START_HOLD_ADDR = 0x0028;
  static const int I2C0_SDA_START_HOLD_ADDR = 0x002C;
  static const int I2C0_SCL_LAST_HOLD_ADDR = 0x0030;
  static const int I2C0_SCL_WAIT_PERIOD_ADDR = 0x0034;
  static const int I2C0_CTR_ADDR = 0x0050;
  static const int I2C0_STATUS_ADDR = 0x0054;
  static const int I2C0_FINISH_INT_ENA_ADDR = 0x0058;
  static const int I2C0_COMMAND0_ADDR = 0x0060;
  static const int I2C0_COMMAND1_ADDR = 0x0064;
  static const int I2C0_COMMAND2_ADDR = 0x0068;
  static const int I2C0_COMMAND3_ADDR = 0x006C;
  static const int I2C0_DATA_ADDR = 0x0080;
  // I2C 1
  static const int I2C1_BASE = 0x3FF67000;
  static const int I2C1_CTR_ADDR = 0x0050;
  static const int I2C1_DATA_ADDR = 0x0080;
  static const int I2C1_COMMAND0_ADDR = 0x0060;
  static const int I2C1_COMMAND1_ADDR = 0x0064;
  // Timer Group 0
  static const int TIMG0_BASE = 0x3FF5F000;
  static const int TIMG0_T0CONFIG_ADDR = 0x0000;
  static const int TIMG0_T0LO_ADDR = 0x0004;
  static const int TIMG0_T0HI_ADDR = 0x0008;
  static const int TIMG0_T0UPDATE_ADDR = 0x000C;
  static const int TIMG0_T0ALARM_ADDR = 0x0010;
  static const int TIMG0_T0LOAD_ADDR = 0x0014;
  static const int TIMG0_T0LOAD_REG_ADDR = 0x0018;
  // Timer Group 1
  static const int TIMG1_BASE = 0x3FF60000;
  static const int TIMG1_T0CONFIG_ADDR = 0x0000;
  static const int TIMG1_T0LO_ADDR = 0x0004;
  static const int TIMG1_T0HI_ADDR = 0x0008;
  static const int TIMG1_T0ALARM_ADDR = 0x0010;
  static const int TIMG1_T0LOAD_ADDR = 0x0014;
  // Motor Control PWM 0
  static const int PWM0_BASE = 0x3FF59000;
  static const int PWM0_CNT_ADDR = 0x0000;
  static const int PWM0_PERIOD_ADDR = 0x0004;
  static const int PWM0_DUTY_ADDR = 0x0008;
  static const int PWM0_CONFIG0_ADDR = 0x0010;
  static const int PWM0_CONFIG1_ADDR = 0x0014;
  static const int PWM0_CONFIG2_ADDR = 0x0018;
  static const int PWM0_UPDATE_ADDR = 0x0020;
  // Motor Control PWM 1
  static const int PWM1_BASE = 0x3FF5A000;
  static const int PWM1_CNT_ADDR = 0x0000;
  static const int PWM1_PERIOD_ADDR = 0x0004;
  static const int PWM1_DUTY_ADDR = 0x0008;
  // LED PWM Controller
  static const int LEDC_BASE = 0x3FF59000;
  static const int LEDC_CONFIG0_ADDR = 0x0000;
  static const int LEDC_HPOINT0_ADDR = 0x0018;
  static const int LEDC_DUTY0_ADDR = 0x001C;
  static const int LEDC_HPOINT1_ADDR = 0x0028;
  static const int LEDC_DUTY1_ADDR = 0x002C;
  static const int LEDC_HPOINT2_ADDR = 0x0038;
  static const int LEDC_DUTY2_ADDR = 0x003C;
  static const int LEDC_HPOINT3_ADDR = 0x0048;
  static const int LEDC_DUTY3_ADDR = 0x004C;
  static const int LEDC_TIMER0_CONF_ADDR = 0x0000;
  static const int LEDC_TIMER0_LOAD_ADDR = 0x0004;
  // RTC Controller
  static const int RTC_BASE = 0x3FF48000;
  static const int RTC_RTC_CNTL_ADDR = 0x0000;
  static const int RTC_RTC_TIMER_ADDR = 0x000C;
  static const int RTC_RTC_UPDATE_ADDR = 0x0010;
  static const int RTC_RTC_STATE0_ADDR = 0x0080;
  // Wi-Fi
  static const int WIFI_BASE = 0x3FFAE000;
  static const int WIFI_MAC_ADDR = 0x0000;
  static const int WIFI_CONFIG_ADDR = 0x0100;
  // Bluetooth/BLE
  static const int BT_BASE = 0x3FFB0000;
  static const int BT_CONFIG_ADDR = 0x0000;
  // SHA Hardware Accelerator
  static const int SHA_BASE = 0x3FF67000;
  static const int SHA_MODE_ADDR = 0x0000;
  static const int SHA_DATA_ADDR = 0x0004;
  static const int SHA_HASH_ADDR = 0x0008;
  // AES Hardware Accelerator
  static const int AES_BASE = 0x3FF68000;
  static const int AES_KEY_ADDR = 0x0000;
  static const int AES_DATA_IN_ADDR = 0x0004;
  static const int AES_DATA_OUT_ADDR = 0x0008;
  static const int AES_MODE_ADDR = 0x000C;
  // Random Number Generator
  static const int RNG_BASE = 0x3FF75000;
  static const int RNG_DATA_ADDR = 0x0000;
  // eFuse Controller
  static const int EFUSE_BASE = 0x3FF5A000;
  static const int EFUSE_DATA0_ADDR = 0x0000;
  static const int EFUSE_DATA1_ADDR = 0x0004;
  static const int EFUSE_DATA2_ADDR = 0x0008;
  static const int EFUSE_DATA3_ADDR = 0x000C;

  // 中断向量定义
  static const int INT_NMI = 0;  // Non-maskable interrupt
  static const int INT_SYS_SOFT = 1;  // Software interrupt
  static const int INT_TIMER_INTR0 = 2;  // Hardware timer 0
  static const int INT_TIMER_INTR1 = 3;  // Hardware timer 1
  static const int INT_TIMER_INTR2 = 4;  // Hardware timer 2
  static const int INT_TIMER_GROUP0 = 5;  // TG0 interrupt
  static const int INT_TIMER_GROUP1 = 6;  // TG1 interrupt
  static const int INT_GPIO = 7;  // GPIO interrupt
  static const int INT_GPIO_NMI = 8;  // GPIO NMI interrupt
  static const int INT_SPI0 = 9;  // SPI0 interrupt
  static const int INT_SPI1 = 10;  // SPI1 interrupt
  static const int INT_SPI2 = 11;  // SPI2 interrupt
  static const int INT_I2C0 = 12;  // I2C0 interrupt
  static const int INT_I2C1 = 13;  // I2C1 interrupt
  static const int INT_UART0 = 14;  // UART0 interrupt
  static const int INT_UART1 = 15;  // UART1 interrupt
  static const int INT_UART2 = 16;  // UART2 interrupt
  static const int INT_WDT = 17;  // Watchdog interrupt
  static const int INT_RTC = 18;  // RTC interrupt
  static const int INT_PWM0 = 19;  // PWM0 interrupt
  static const int INT_PWM1 = 20;  // PWM1 interrupt
  static const int INT_LEDC = 21;  // LEDC interrupt
  static const int INT_TOUCH = 22;  // Touch sensor interrupt
  static const int INT_SARADC = 23;  // SARADC interrupt
  static const int INT_MAX = 24;  // No. of CPU interrupts
  static const int INT_CORE_INTR0 = 25;  // Core 0 interrupt 0
  static const int INT_CORE_INTR1 = 26;  // Core 0 interrupt 1
  static const int INT_CORE_INTR2 = 27;  // Core 0 interrupt 2
  static const int INT_CORE_INTR3 = 28;  // Core 0 interrupt 3
  static const int INT_CORE_INTR4 = 29;  // Core 0 interrupt 4
  static const int INT_CORE_INTR5 = 30;  // Core 0 interrupt 5
  static const int INT_CORE_INTR6 = 31;  // Core 0 interrupt 6
  static const int INT_GPIO_INTERRUPT = 32;  // GPIO interrupt
  static const int INT_GPIO_INTERRUPT_NMI = 33;  // GPIO NMI interrupt

  // 引脚定义
  static const int PIN_VDD = 1;  // 3.3V Power Supply
  static const int PIN_EN = 2;  // Enable ( CHIP_PU )
  static const int PIN_SENSOR_VP = 3;  // GPIO36 - ADC1_CH0 - SENSOR_VP
  static const int PIN_SENSOR_VN = 4;  // GPIO37 - ADC1_CH1 - SENSOR_VN
  static const int PIN_IO34 = 5;  // GPIO34 - ADC1_CH6
  static const int PIN_IO35 = 6;  // GPIO35 - ADC1_CH7
  static const int PIN_IO32 = 7;  // GPIO32 - ADC1_CH4 - TOUCH_CH9
  static const int PIN_IO33 = 8;  // GPIO33 - ADC1_CH5 - TOUCH_CH8
  static const int PIN_IO25 = 9;  // GPIO25 - DAC1 - ADC2_CH8
  static const int PIN_IO26 = 10;  // GPIO26 - DAC2 - ADC2_CH9
  static const int PIN_IO27 = 11;  // GPIO27 - TOUCH_CH7 - ADC2_CH7
  static const int PIN_IO14 = 12;  // GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
  static const int PIN_IO12 = 13;  // GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
  static const int PIN_GND = 14;  // Ground
  static const int PIN_IO13 = 15;  // GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
  static const int PIN_SD2 = 16;  // GPIO9 - SD_DATA2
  static const int PIN_SD3 = 17;  // GPIO10 - SD_DATA3
  static const int PIN_CMD = 18;  // GPIO11 - SD_CMD
  static const int PIN_CLK = 19;  // GPIO6 - SD_CLK
  static const int PIN_SD0 = 20;  // GPIO7 - SD_DATA0
  static const int PIN_SD1 = 21;  // GPIO8 - SD_DATA1
  static const int PIN_IO15 = 22;  // GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
  static const int PIN_IO2 = 23;  // GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
  static const int PIN_IO0 = 24;  // GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
  static const int PIN_IO4 = 25;  // GPIO4 - ADC2_CH0 - TOUCH_CH1
  static const int PIN_IO16 = 26;  // GPIO16 - HSPI WP
  static const int PIN_IO17 = 27;  // GPIO17 - HSPI HD
  static const int PIN_IO5 = 28;  // GPIO5 - HSPI CS0
  static const int PIN_IO18 = 29;  // GPIO18 - VSPICLK
  static const int PIN_IO19 = 30;  // GPIO19 - VSPIQ
  static const int PIN_NC = 31;  // Not Connected
  static const int PIN_IO21 = 32;  // GPIO21
  static const int PIN_RXD0 = 33;  // GPIO3 - U0RXD
  static const int PIN_TXD0 = 34;  // GPIO1 - U0TXD
  static const int PIN_IO22 = 35;  // GPIO22
  static const int PIN_IO23 = 36;  // GPIO23 - VSPID
  static const int PIN_GND = 37;  // Ground
  static const int PIN_GND = 38;  // Ground

}
