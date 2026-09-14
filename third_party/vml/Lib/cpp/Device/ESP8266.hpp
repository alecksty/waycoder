#ifndef ESP8266_HPP
#define ESP8266_HPP

// ESP8266寄存器定义
// 生成自: Espressif Systems/ESP8266/ESP8266
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Xtensa LX106
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// Wi-Fi 802.11 b/g/n
#define WIFI_BASE 
#define WIFI_WIFI_MAC (*(volatile uint256_t*)0x60000800)
#define WIFI_WIFI_MODE (*(volatile uint256_t*)0x60000804)
#define WIFI_WIFI_CHANNEL (*(volatile uint256_t*)0x60000808)
#define WIFI_WIFI_RATE (*(volatile uint256_t*)0x6000080C)

// Universal Asynchronous Receiver/Transmitter 0
#define UART0_BASE 
#define UART0_UART0_FIFO (*(volatile uint256_t*)0x60000000)
#define UART0_UART0_INT_RAW (*(volatile uint256_t*)0x60000004)
#define UART0_UART0_INT_ST (*(volatile uint256_t*)0x60000008)
#define UART0_UART0_INT_ENA (*(volatile uint256_t*)0x6000000C)
#define UART0_UART0_INT_CLR (*(volatile uint256_t*)0x60000010)
#define UART0_UART0_CLKDIV (*(volatile uint256_t*)0x60000014)
#define UART0_UART0_AUTOBAUD (*(volatile uint256_t*)0x60000018)
#define UART0_UART0_STATUS (*(volatile uint256_t*)0x6000001C)
#define UART0_UART0_CONF0 (*(volatile uint256_t*)0x60000020)
#define UART0_UART0_CONF1 (*(volatile uint256_t*)0x60000024)
#define UART0_UART0_LOWPULSE (*(volatile uint256_t*)0x60000028)
#define UART0_UART0_HIGHPULSE (*(volatile uint256_t*)0x6000002C)
#define UART0_UART0_RXD_CNT (*(volatile uint256_t*)0x60000030)

// Serial Peripheral Interface
#define SPI_BASE 
#define SPI_SPI_CMD (*(volatile uint256_t*)0x60000200)
#define SPI_SPI_ADDR (*(volatile uint256_t*)0x60000204)
#define SPI_SPI_CTRL (*(volatile uint256_t*)0x60000208)
#define SPI_SPI_RD_STATUS (*(volatile uint256_t*)0x6000020C)
#define SPI_SPI_CTRL2 (*(volatile uint256_t*)0x60000210)
#define SPI_SPI_CLOCK (*(volatile uint256_t*)0x60000214)
#define SPI_SPI_USER (*(volatile uint256_t*)0x60000218)
#define SPI_SPI_USER1 (*(volatile uint256_t*)0x6000021C)
#define SPI_SPI_USER2 (*(volatile uint256_t*)0x60000220)
#define SPI_SPI_W0 (*(volatile uint256_t*)0x60000280)

// Inter-Integrated Circuit
#define I2C_BASE 
#define I2C_I2C_SCL_LOW (*(volatile uint256_t*)0x60000C00)
#define I2C_I2C_SCL_HIGH (*(volatile uint256_t*)0x60000C04)
#define I2C_I2C_SDA_HOLD (*(volatile uint256_t*)0x60000C08)
#define I2C_I2C_SCL_START_HOLD (*(volatile uint256_t*)0x60000C0C)
#define I2C_I2C_SCL_STOP_HOLD (*(volatile uint256_t*)0x60000C10)
#define I2C_I2C_INT_RAW (*(volatile uint256_t*)0x60000C14)
#define I2C_I2C_INT_ST (*(volatile uint256_t*)0x60000C18)
#define I2C_I2C_INT_ENA (*(volatile uint256_t*)0x60000C1C)
#define I2C_I2C_INT_CLR (*(volatile uint256_t*)0x60000C20)
#define I2C_I2C_CMD (*(volatile uint256_t*)0x60000C24)
#define I2C_I2C_FIFO_DATA (*(volatile uint256_t*)0x60000C28)
#define I2C_I2C_FIFO_CNT (*(volatile uint256_t*)0x60000C2C)

// General Purpose I/O
#define GPIO_BASE 
#define GPIO_GPIO_OUT (*(volatile uint256_t*)0x60000300)
#define GPIO_GPIO_OUT_W1TS (*(volatile uint256_t*)0x60000304)
#define GPIO_GPIO_OUT_W1TC (*(volatile uint256_t*)0x60000308)
#define GPIO_GPIO_ENABLE (*(volatile uint256_t*)0x6000030C)
#define GPIO_GPIO_ENABLE_W1TS (*(volatile uint256_t*)0x60000310)
#define GPIO_GPIO_ENABLE_W1TC (*(volatile uint256_t*)0x60000314)
#define GPIO_GPIO_IN (*(volatile uint256_t*)0x60000318)
#define GPIO_GPIO_STATUS (*(volatile uint256_t*)0x6000031C)
#define GPIO_GPIO_STATUS_W1TS (*(volatile uint256_t*)0x60000320)
#define GPIO_GPIO_STATUS_W1TC (*(volatile uint256_t*)0x60000324)
#define GPIO_GPIO_PIN (*(volatile uint256_t*)0x60000328)

// Hardware Timer
#define TIMER_BASE 
#define TIMER_TIMER_LOAD (*(volatile uint256_t*)0x60000600)
#define TIMER_TIMER_COUNT (*(volatile uint256_t*)0x60000604)
#define TIMER_TIMER_CTRL (*(volatile uint256_t*)0x60000608)
#define TIMER_TIMER_INT (*(volatile uint256_t*)0x6000060C)
#define TIMER_TIMER_ALARM (*(volatile uint256_t*)0x60000610)

// Analog-to-Digital Converter
#define ADC_BASE 
#define ADC_ADC_CTRL (*(volatile uint256_t*)0x60000E00)
#define ADC_ADC_DATA (*(volatile uint256_t*)0x60000E04)

// Pulse Width Modulation
#define PWM_BASE 
#define PWM_PWM_CTRL (*(volatile uint256_t*)0x60000F00)
#define PWM_PWM_PERIOD (*(volatile uint256_t*)0x60000F04)
#define PWM_PWM_DUTY (*(volatile uint256_t*)0x60000F08)

// 中断向量定义
#define NMI_VECTOR 1  // Non-maskable interrupt
#define LEVEL1_VECTOR 3  // Level 1 interrupt
#define LEVEL2_VECTOR 4  // Level 2 interrupt
#define LEVEL3_VECTOR 5  // Level 3 interrupt
#define LEVEL4_VECTOR 6  // Level 4 interrupt
#define LEVEL5_VECTOR 7  // Level 5 interrupt
#define TIMER0_VECTOR 8  // Timer 0 interrupt
#define TIMER1_VECTOR 9  // Timer 1 interrupt
#define UART0_VECTOR 10  // UART0 interrupt
#define UART1_VECTOR 11  // UART1 interrupt
#define GPIO_VECTOR 12  // GPIO interrupt
#define PWM_VECTOR 13  // PWM interrupt
#define I2C_VECTOR 14  // I2C interrupt
#define SPI_VECTOR 15  // SPI interrupt
#define ADC_VECTOR 16  // ADC interrupt
#define WIFI_VECTOR 17  // Wi-Fi interrupt
#define RTC_VECTOR 18  // RTC interrupt

void esp8266_init(void);

#ifdef __cplusplus
}
#endif

#endif // ESP8266_HPP
