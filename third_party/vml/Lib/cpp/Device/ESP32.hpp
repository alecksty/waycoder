#ifndef ESP32_WROOM_32_HPP
#define ESP32_WROOM_32_HPP

// ESP32-WROOM-32寄存器定义
// 生成自: Espressif/ESP32/ESP32-WROOM-32
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Xtensa-LX6
// 位宽: 32位
// 时钟频率: 160000000 Hz

// 寄存器定义
// Program Counter
#define PC (*(volatile uint32_t*)0x00000000)

// General Purpose Register 0
#define A0 (*(volatile uint32_t*)0x00000004)

// Stack Pointer
#define A1 (*(volatile uint32_t*)0x00000008)

// General Purpose Register 2
#define A2 (*(volatile uint32_t*)0x0000000C)

// General Purpose Register 3
#define A3 (*(volatile uint32_t*)0x00000010)

// General Purpose Register 4
#define A4 (*(volatile uint32_t*)0x00000014)

// General Purpose Register 5
#define A5 (*(volatile uint32_t*)0x00000018)

// General Purpose Register 6
#define A6 (*(volatile uint32_t*)0x0000001C)

// General Purpose Register 7
#define A7 (*(volatile uint32_t*)0x00000020)

// General Purpose Register 8
#define A8 (*(volatile uint32_t*)0x00000024)

// General Purpose Register 9
#define A9 (*(volatile uint32_t*)0x00000028)

// General Purpose Register 10
#define A10 (*(volatile uint32_t*)0x0000002C)

// General Purpose Register 11
#define A11 (*(volatile uint32_t*)0x00000030)

// General Purpose Register 12
#define A12 (*(volatile uint32_t*)0x00000034)

// General Purpose Register 13
#define A13 (*(volatile uint32_t*)0x00000038)

// General Purpose Register 14
#define A14 (*(volatile uint32_t*)0x0000003C)

// General Purpose Register 15
#define A15 (*(volatile uint32_t*)0x00000040)

// Special Address Register 1
#define SAREG1 (*(volatile uint32_t*)0x00000044)

// Special Address Register 2
#define SAREG2 (*(volatile uint32_t*)0x00000048)

// Loop Beginning
#define LBEG (*(volatile uint32_t*)0x0000004C)

// Loop End
#define LEND (*(volatile uint32_t*)0x00000050)

// Loop Counter
#define LCOUNT (*(volatile uint32_t*)0x00000054)

// Processor Status
#define PS (*(volatile uint32_t*)0x00000058)

// Window Base
#define WINDOWBASE (*(volatile uint32_t*)0x0000005C)

// Window Start
#define WINDOWSTART (*(volatile uint32_t*)0x00000060)

// Page Table Base
#define PTEBASE (*(volatile uint32_t*)0x00000064)

// Page Table Entry Size
#define PTESIZE (*(volatile uint32_t*)0x00000068)

// Special Compare 1
#define SCOMPARE1 (*(volatile uint32_t*)0x0000006C)

// Atomic Operation Control
#define ATOMCTL (*(volatile uint32_t*)0x00000070)

// Data Destination Register
#define DDR (*(volatile uint32_t*)0x00000074)

// 内存段定义
// ROM (224KB)
#define ROM_START 0x40000000
#define ROM_END 0x4003FFFF
#define ROM_SIZE 262144

// SRAM (320KB total)
#define SRAM_START 0x3FF00000
#define SRAM_END 0x3FF7FFFF
#define SRAM_SIZE 524288

// DRAM0 (128KB)
#define DRAM0_START 0x3FF80000
#define DRAM0_END 0x3FF9FFFF
#define DRAM0_SIZE 131072

// IRAM0
#define IRAM0_START 0x40000000
#define IRAM0_END 0x401FFFFF
#define IRAM0_SIZE 2097152

// External Flash (4MB)
#define FLASH_START 0x40200000
#define FLASH_END 0x405FFFFF
#define FLASH_SIZE 4194304

// Peripheral Registers
#define PERIPHERAL_START 0x3FF00000
#define PERIPHERAL_END 0x3FFBFFFF
#define PERIPHERAL_SIZE 786432

// GPIO
#define GPIO_START 0x3FF44000
#define GPIO_END 0x3FF44FFF
#define GPIO_SIZE 4096

// 外设定义
// GPIO
#define GPIO_BASE 0x3FF44000
#define GPIO_OUT (*(volatile uint32_t*)0x3FF44000)
#define GPIO_OUT_W1TS (*(volatile uint32_t*)0x3FF44008)
#define GPIO_OUT_W1TC (*(volatile uint32_t*)0x3FF4400C)
#define GPIO_IN (*(volatile uint32_t*)0x3FF4403C)
#define GPIO_STATUS (*(volatile uint32_t*)0x3FF44024)
#define GPIO_STATUS_W1TS (*(volatile uint32_t*)0x3FF44028)
#define GPIO_STATUS_W1TC (*(volatile uint32_t*)0x3FF4402C)
#define GPIO_PIN (*(volatile uint32_t*)0x3FF44040)
#define GPIO_ENABLE (*(volatile uint32_t*)0x3FF44020)
#define GPIO_STRAP (*(volatile uint32_t*)0x3FF44038)
#define GPIO_IN_NEXT (*(volatile uint32_t*)0x3FF44044)

// RTC GPIO
#define RTC_GPIO_BASE 0x3FF48000
#define RTC_GPIO_OUT (*(volatile uint32_t*)0x3FF48000)
#define RTC_GPIO_OUT_W1TS (*(volatile uint32_t*)0x3FF48008)
#define RTC_GPIO_OUT_W1TC (*(volatile uint32_t*)0x3FF4800C)
#define RTC_GPIO_IN (*(volatile uint32_t*)0x3FF48044)
#define RTC_GPIO_STATUS (*(volatile uint32_t*)0x3FF48024)
#define RTC_GPIO_PIN (*(volatile uint32_t*)0x3FF48048)
#define RTC_GPIO_ENABLE (*(volatile uint32_t*)0x3FF48020)

// IO MUX
#define IO_MUX_BASE 0x3FF49000
#define IO_MUX_GPIO0 (*(volatile uint32_t*)0x3FF49000)
#define IO_MUX_GPIO1 (*(volatile uint32_t*)0x3FF49004)
#define IO_MUX_GPIO2 (*(volatile uint32_t*)0x3FF49008)
#define IO_MUX_GPIO3 (*(volatile uint32_t*)0x3FF4900C)
#define IO_MUX_GPIO4 (*(volatile uint32_t*)0x3FF49010)
#define IO_MUX_GPIO5 (*(volatile uint32_t*)0x3FF49014)
#define IO_MUX_GPIO6 (*(volatile uint32_t*)0x3FF49018)
#define IO_MUX_GPIO7 (*(volatile uint32_t*)0x3FF4901C)
#define IO_MUX_GPIO8 (*(volatile uint32_t*)0x3FF49020)
#define IO_MUX_GPIO9 (*(volatile uint32_t*)0x3FF49024)
#define IO_MUX_GPIO10 (*(volatile uint32_t*)0x3FF49028)
#define IO_MUX_GPIO11 (*(volatile uint32_t*)0x3FF4902C)
#define IO_MUX_GPIO12 (*(volatile uint32_t*)0x3FF49030)
#define IO_MUX_GPIO13 (*(volatile uint32_t*)0x3FF49034)
#define IO_MUX_GPIO14 (*(volatile uint32_t*)0x3FF49038)
#define IO_MUX_GPIO15 (*(volatile uint32_t*)0x3FF4903C)
#define IO_MUX_GPIO16 (*(volatile uint32_t*)0x3FF49040)
#define IO_MUX_GPIO17 (*(volatile uint32_t*)0x3FF49044)
#define IO_MUX_GPIO18 (*(volatile uint32_t*)0x3FF49048)
#define IO_MUX_GPIO19 (*(volatile uint32_t*)0x3FF4904C)
#define IO_MUX_GPIO20 (*(volatile uint32_t*)0x3FF49050)
#define IO_MUX_GPIO21 (*(volatile uint32_t*)0x3FF49054)
#define IO_MUX_GPIO22 (*(volatile uint32_t*)0x3FF49058)
#define IO_MUX_GPIO23 (*(volatile uint32_t*)0x3FF4905C)
#define IO_MUX_GPIO24 (*(volatile uint32_t*)0x3FF49060)
#define IO_MUX_GPIO25 (*(volatile uint32_t*)0x3FF49064)
#define IO_MUX_GPIO26 (*(volatile uint32_t*)0x3FF49068)
#define IO_MUX_GPIO27 (*(volatile uint32_t*)0x3FF4906C)

// UART 0
#define UART0_BASE 0x3FF40000
#define UART0_FIFO (*(volatile uint32_t*)0x3FF40000)
#define UART0_INT_RAW (*(volatile uint32_t*)0x3FF40004)
#define UART0_INT_ST (*(volatile uint32_t*)0x3FF40008)
#define UART0_INT_ENA (*(volatile uint32_t*)0x3FF4000C)
#define UART0_INT_CLR (*(volatile uint32_t*)0x3FF40010)
#define UART0_CONF0 (*(volatile uint32_t*)0x3FF40020)
#define UART0_CONF1 (*(volatile uint32_t*)0x3FF40024)
#define UART0_LOWPULSE (*(volatile uint32_t*)0x3FF40028)
#define UART0_HIGHPULSE (*(volatile uint32_t*)0x3FF4002C)
#define UART0_PULSE_CNT (*(volatile uint32_t*)0x3FF40030)
#define UART0_DATE (*(volatile uint32_t*)0x3FF40078)
#define UART0_AHB_BIT (*(volatile uint32_t*)0x3FF4007C)

// UART 1
#define UART1_BASE 0x3FF50000
#define UART1_FIFO (*(volatile uint32_t*)0x3FF50000)
#define UART1_INT_RAW (*(volatile uint32_t*)0x3FF50004)
#define UART1_INT_ST (*(volatile uint32_t*)0x3FF50008)
#define UART1_INT_ENA (*(volatile uint32_t*)0x3FF5000C)
#define UART1_INT_CLR (*(volatile uint32_t*)0x3FF50010)
#define UART1_CONF0 (*(volatile uint32_t*)0x3FF50020)
#define UART1_CONF1 (*(volatile uint32_t*)0x3FF50024)

// UART 2
#define UART2_BASE 0x3FF6E000
#define UART2_FIFO (*(volatile uint32_t*)0x3FF6E000)
#define UART2_INT_RAW (*(volatile uint32_t*)0x3FF6E004)
#define UART2_INT_ST (*(volatile uint32_t*)0x3FF6E008)
#define UART2_INT_ENA (*(volatile uint32_t*)0x3FF6E00C)
#define UART2_INT_CLR (*(volatile uint32_t*)0x3FF6E010)
#define UART2_CONF0 (*(volatile uint32_t*)0x3FF6E020)
#define UART2_CONF1 (*(volatile uint32_t*)0x3FF6E024)

// SPI0 (Flash)
#define SPI0_BASE 0x3FF42000
#define SPI0_CMD (*(volatile uint32_t*)0x3FF42000)
#define SPI0_ADDR (*(volatile uint32_t*)0x3FF42004)
#define SPI0_CONTROL (*(volatile uint32_t*)0x3FF42008)
#define SPI0_CONTROL1 (*(volatile uint32_t*)0x3FF4200C)
#define SPI0_STATUS (*(volatile uint32_t*)0x3FF42010)
#define SPI0_STATUS1 (*(volatile uint32_t*)0x3FF42014)
#define SPI0_DATA (*(volatile uint32_t*)0x3FF42020)
#define SPI0_USER (*(volatile uint32_t*)0x3FF4203C)
#define SPI0_USER1 (*(volatile uint32_t*)0x3FF42040)
#define SPI0_USER2 (*(volatile uint32_t*)0x3FF42044)
#define SPI0_PIN (*(volatile uint32_t*)0x3FF42048)
#define SPI0_SLAVE (*(volatile uint32_t*)0x3FF4204C)
#define SPI0_CACHE_FLASH (*(volatile uint32_t*)0x3FF42050)
#define SPI0_CLOCK (*(volatile uint32_t*)0x3FF42058)
#define SPI0_FIFO (*(volatile uint32_t*)0x3FF42060)

// SPI1
#define SPI1_BASE 0x3FF43000
#define SPI1_CMD (*(volatile uint32_t*)0x3FF43000)
#define SPI1_ADDR (*(volatile uint32_t*)0x3FF43004)
#define SPI1_CONTROL (*(volatile uint32_t*)0x3FF43008)
#define SPI1_STATUS (*(volatile uint32_t*)0x3FF43010)
#define SPI1_DATA (*(volatile uint32_t*)0x3FF43020)
#define SPI1_USER (*(volatile uint32_t*)0x3FF4303C)
#define SPI1_CLOCK (*(volatile uint32_t*)0x3FF43058)

// SPI2 (HSPI)
#define SPI2_BASE 0x3FF64000
#define SPI2_CMD (*(volatile uint32_t*)0x3FF64000)
#define SPI2_ADDR (*(volatile uint32_t*)0x3FF64004)
#define SPI2_CONTROL (*(volatile uint32_t*)0x3FF64008)
#define SPI2_STATUS (*(volatile uint32_t*)0x3FF64010)
#define SPI2_DATA (*(volatile uint32_t*)0x3FF64020)
#define SPI2_USER (*(volatile uint32_t*)0x3FF6403C)
#define SPI2_CLOCK (*(volatile uint32_t*)0x3FF64058)
#define SPI2_FIFO (*(volatile uint32_t*)0x3FF64060)

// I2C 0
#define I2C0_BASE 0x3FF53000
#define I2C0_SCL_START (*(volatile uint32_t*)0x3FF53000)
#define I2C0_SCL_LOW (*(volatile uint32_t*)0x3FF53004)
#define I2C0_SDA_START (*(volatile uint32_t*)0x3FF53008)
#define I2C0_SDA_LOW (*(volatile uint32_t*)0x3FF5300C)
#define I2C0_INT_ENA (*(volatile uint32_t*)0x3FF53010)
#define I2C0_INT_CLR (*(volatile uint32_t*)0x3FF53014)
#define I2C0_INT_RAW (*(volatile uint32_t*)0x3FF53018)
#define I2C0_INT_STATUS (*(volatile uint32_t*)0x3FF5301C)
#define I2C0_SCL_HIGH_PERIOD (*(volatile uint32_t*)0x3FF53020)
#define I2C0_SCL_HIGH_PERIOD_S (*(volatile uint32_t*)0x3FF53024)
#define I2C0_SCL_START_HOLD (*(volatile uint32_t*)0x3FF53028)
#define I2C0_SDA_START_HOLD (*(volatile uint32_t*)0x3FF5302C)
#define I2C0_SCL_LAST_HOLD (*(volatile uint32_t*)0x3FF53030)
#define I2C0_SCL_WAIT_PERIOD (*(volatile uint32_t*)0x3FF53034)
#define I2C0_CTR (*(volatile uint32_t*)0x3FF53050)
#define I2C0_STATUS (*(volatile uint32_t*)0x3FF53054)
#define I2C0_FINISH_INT_ENA (*(volatile uint32_t*)0x3FF53058)
#define I2C0_COMMAND0 (*(volatile uint32_t*)0x3FF53060)
#define I2C0_COMMAND1 (*(volatile uint32_t*)0x3FF53064)
#define I2C0_COMMAND2 (*(volatile uint32_t*)0x3FF53068)
#define I2C0_COMMAND3 (*(volatile uint32_t*)0x3FF5306C)
#define I2C0_DATA (*(volatile uint32_t*)0x3FF53080)

// I2C 1
#define I2C1_BASE 0x3FF67000
#define I2C1_CTR (*(volatile uint32_t*)0x3FF67050)
#define I2C1_DATA (*(volatile uint32_t*)0x3FF67080)
#define I2C1_COMMAND0 (*(volatile uint32_t*)0x3FF67060)
#define I2C1_COMMAND1 (*(volatile uint32_t*)0x3FF67064)

// Timer Group 0
#define TIMG0_BASE 0x3FF5F000
#define TIMG0_T0CONFIG (*(volatile uint32_t*)0x3FF5F000)
#define TIMG0_T0LO (*(volatile uint32_t*)0x3FF5F004)
#define TIMG0_T0HI (*(volatile uint32_t*)0x3FF5F008)
#define TIMG0_T0UPDATE (*(volatile uint32_t*)0x3FF5F00C)
#define TIMG0_T0ALARM (*(volatile uint32_t*)0x3FF5F010)
#define TIMG0_T0LOAD (*(volatile uint32_t*)0x3FF5F014)
#define TIMG0_T0LOAD_REG (*(volatile uint32_t*)0x3FF5F018)

// Timer Group 1
#define TIMG1_BASE 0x3FF60000
#define TIMG1_T0CONFIG (*(volatile uint32_t*)0x3FF60000)
#define TIMG1_T0LO (*(volatile uint32_t*)0x3FF60004)
#define TIMG1_T0HI (*(volatile uint32_t*)0x3FF60008)
#define TIMG1_T0ALARM (*(volatile uint32_t*)0x3FF60010)
#define TIMG1_T0LOAD (*(volatile uint32_t*)0x3FF60014)

// Motor Control PWM 0
#define PWM0_BASE 0x3FF59000
#define PWM0_CNT (*(volatile uint32_t*)0x3FF59000)
#define PWM0_PERIOD (*(volatile uint32_t*)0x3FF59004)
#define PWM0_DUTY (*(volatile uint32_t*)0x3FF59008)
#define PWM0_CONFIG0 (*(volatile uint32_t*)0x3FF59010)
#define PWM0_CONFIG1 (*(volatile uint32_t*)0x3FF59014)
#define PWM0_CONFIG2 (*(volatile uint32_t*)0x3FF59018)
#define PWM0_UPDATE (*(volatile uint32_t*)0x3FF59020)

// Motor Control PWM 1
#define PWM1_BASE 0x3FF5A000
#define PWM1_CNT (*(volatile uint32_t*)0x3FF5A000)
#define PWM1_PERIOD (*(volatile uint32_t*)0x3FF5A004)
#define PWM1_DUTY (*(volatile uint32_t*)0x3FF5A008)

// LED PWM Controller
#define LEDC_BASE 0x3FF59000
#define LEDC_CONFIG0 (*(volatile uint32_t*)0x3FF59000)
#define LEDC_HPOINT0 (*(volatile uint32_t*)0x3FF59018)
#define LEDC_DUTY0 (*(volatile uint32_t*)0x3FF5901C)
#define LEDC_HPOINT1 (*(volatile uint32_t*)0x3FF59028)
#define LEDC_DUTY1 (*(volatile uint32_t*)0x3FF5902C)
#define LEDC_HPOINT2 (*(volatile uint32_t*)0x3FF59038)
#define LEDC_DUTY2 (*(volatile uint32_t*)0x3FF5903C)
#define LEDC_HPOINT3 (*(volatile uint32_t*)0x3FF59048)
#define LEDC_DUTY3 (*(volatile uint32_t*)0x3FF5904C)
#define LEDC_TIMER0_CONF (*(volatile uint32_t*)0x3FF59000)
#define LEDC_TIMER0_LOAD (*(volatile uint32_t*)0x3FF59004)

// RTC Controller
#define RTC_BASE 0x3FF48000
#define RTC_RTC_CNTL (*(volatile uint32_t*)0x3FF48000)
#define RTC_RTC_TIMER (*(volatile uint32_t*)0x3FF4800C)
#define RTC_RTC_UPDATE (*(volatile uint32_t*)0x3FF48010)
#define RTC_RTC_STATE0 (*(volatile uint32_t*)0x3FF48080)

// Wi-Fi
#define WIFI_BASE 0x3FFAE000
#define WIFI_MAC (*(volatile uint32_t*)0x3FFAE000)
#define WIFI_CONFIG (*(volatile uint32_t*)0x3FFAE100)

// Bluetooth/BLE
#define BT_BASE 0x3FFB0000
#define BT_CONFIG (*(volatile uint32_t*)0x3FFB0000)

// SHA Hardware Accelerator
#define SHA_BASE 0x3FF67000
#define SHA_MODE (*(volatile uint32_t*)0x3FF67000)
#define SHA_DATA (*(volatile uint32_t*)0x3FF67004)
#define SHA_HASH (*(volatile uint32_t*)0x3FF67008)

// AES Hardware Accelerator
#define AES_BASE 0x3FF68000
#define AES_KEY (*(volatile uint32_t*)0x3FF68000)
#define AES_DATA_IN (*(volatile uint32_t*)0x3FF68004)
#define AES_DATA_OUT (*(volatile uint32_t*)0x3FF68008)
#define AES_MODE (*(volatile uint32_t*)0x3FF6800C)

// Random Number Generator
#define RNG_BASE 0x3FF75000
#define RNG_DATA (*(volatile uint32_t*)0x3FF75000)

// eFuse Controller
#define EFUSE_BASE 0x3FF5A000
#define EFUSE_DATA0 (*(volatile uint32_t*)0x3FF5A000)
#define EFUSE_DATA1 (*(volatile uint32_t*)0x3FF5A004)
#define EFUSE_DATA2 (*(volatile uint32_t*)0x3FF5A008)
#define EFUSE_DATA3 (*(volatile uint32_t*)0x3FF5A00C)

// 中断向量定义
#define NMI_VECTOR 0  // Non-maskable interrupt
#define SYS_SOFT_VECTOR 1  // Software interrupt
#define TIMER_INTR0_VECTOR 2  // Hardware timer 0
#define TIMER_INTR1_VECTOR 3  // Hardware timer 1
#define TIMER_INTR2_VECTOR 4  // Hardware timer 2
#define TIMER_GROUP0_VECTOR 5  // TG0 interrupt
#define TIMER_GROUP1_VECTOR 6  // TG1 interrupt
#define GPIO_VECTOR 7  // GPIO interrupt
#define GPIO_NMI_VECTOR 8  // GPIO NMI interrupt
#define SPI0_VECTOR 9  // SPI0 interrupt
#define SPI1_VECTOR 10  // SPI1 interrupt
#define SPI2_VECTOR 11  // SPI2 interrupt
#define I2C0_VECTOR 12  // I2C0 interrupt
#define I2C1_VECTOR 13  // I2C1 interrupt
#define UART0_VECTOR 14  // UART0 interrupt
#define UART1_VECTOR 15  // UART1 interrupt
#define UART2_VECTOR 16  // UART2 interrupt
#define WDT_VECTOR 17  // Watchdog interrupt
#define RTC_VECTOR 18  // RTC interrupt
#define PWM0_VECTOR 19  // PWM0 interrupt
#define PWM1_VECTOR 20  // PWM1 interrupt
#define LEDC_VECTOR 21  // LEDC interrupt
#define TOUCH_VECTOR 22  // Touch sensor interrupt
#define SARADC_VECTOR 23  // SARADC interrupt
#define MAX_VECTOR 24  // No. of CPU interrupts
#define CORE_INTR0_VECTOR 25  // Core 0 interrupt 0
#define CORE_INTR1_VECTOR 26  // Core 0 interrupt 1
#define CORE_INTR2_VECTOR 27  // Core 0 interrupt 2
#define CORE_INTR3_VECTOR 28  // Core 0 interrupt 3
#define CORE_INTR4_VECTOR 29  // Core 0 interrupt 4
#define CORE_INTR5_VECTOR 30  // Core 0 interrupt 5
#define CORE_INTR6_VECTOR 31  // Core 0 interrupt 6
#define GPIO_INTERRUPT_VECTOR 32  // GPIO interrupt
#define GPIO_INTERRUPT_NMI_VECTOR 33  // GPIO NMI interrupt

// 引脚定义
#define PIN_VDD 1  // 3.3V Power Supply
#define PIN_EN 2  // Enable ( CHIP_PU )
#define PIN_SENSOR_VP 3  // GPIO36 - ADC1_CH0 - SENSOR_VP
#define PIN_SENSOR_VN 4  // GPIO37 - ADC1_CH1 - SENSOR_VN
#define PIN_IO34 5  // GPIO34 - ADC1_CH6
#define PIN_IO35 6  // GPIO35 - ADC1_CH7
#define PIN_IO32 7  // GPIO32 - ADC1_CH4 - TOUCH_CH9
#define PIN_IO33 8  // GPIO33 - ADC1_CH5 - TOUCH_CH8
#define PIN_IO25 9  // GPIO25 - DAC1 - ADC2_CH8
#define PIN_IO26 10  // GPIO26 - DAC2 - ADC2_CH9
#define PIN_IO27 11  // GPIO27 - TOUCH_CH7 - ADC2_CH7
#define PIN_IO14 12  // GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
#define PIN_IO12 13  // GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
#define PIN_GND 14  // Ground
#define PIN_IO13 15  // GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
#define PIN_SD2 16  // GPIO9 - SD_DATA2
#define PIN_SD3 17  // GPIO10 - SD_DATA3
#define PIN_CMD 18  // GPIO11 - SD_CMD
#define PIN_CLK 19  // GPIO6 - SD_CLK
#define PIN_SD0 20  // GPIO7 - SD_DATA0
#define PIN_SD1 21  // GPIO8 - SD_DATA1
#define PIN_IO15 22  // GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
#define PIN_IO2 23  // GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
#define PIN_IO0 24  // GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
#define PIN_IO4 25  // GPIO4 - ADC2_CH0 - TOUCH_CH1
#define PIN_IO16 26  // GPIO16 - HSPI WP
#define PIN_IO17 27  // GPIO17 - HSPI HD
#define PIN_IO5 28  // GPIO5 - HSPI CS0
#define PIN_IO18 29  // GPIO18 - VSPICLK
#define PIN_IO19 30  // GPIO19 - VSPIQ
#define PIN_NC 31  // Not Connected
#define PIN_IO21 32  // GPIO21
#define PIN_RXD0 33  // GPIO3 - U0RXD
#define PIN_TXD0 34  // GPIO1 - U0TXD
#define PIN_IO22 35  // GPIO22
#define PIN_IO23 36  // GPIO23 - VSPID
#define PIN_GND 37  // Ground
#define PIN_GND 38  // Ground

void esp32_wroom_32_init(void);

#ifdef __cplusplus
}
#endif

#endif // ESP32_WROOM_32_HPP
