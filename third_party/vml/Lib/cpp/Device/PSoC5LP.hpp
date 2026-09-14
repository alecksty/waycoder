#ifndef CY8C5888LTI_LP097_HPP
#define CY8C5888LTI_LP097_HPP

// CY8C5888LTI-LP097寄存器定义
// 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 80000000 Hz

// 外设定义
// SCB UART (可编程)
#define UART_BASE 0x40050000
#define UART_CTRL (*(volatile uint32_t*)0x40050000)
#define UART_STATUS (*(volatile uint32_t*)0x40050004)
#define UART_TX_DATA (*(volatile uint32_t*)0x40050008)
#define UART_RX_DATA (*(volatile uint32_t*)0x4005000C)

// SCB I2C
#define I2C_BASE 0x40051000
#define I2C_CTRL (*(volatile uint32_t*)0x40051000)
#define I2C_STATUS (*(volatile uint32_t*)0x40051004)
#define I2C_TX_DATA (*(volatile uint32_t*)0x40051008)
#define I2C_RX_DATA (*(volatile uint32_t*)0x4005100C)

// TCPWM 定时器
#define TIMER_BASE 0x40060000
#define TIMER_CTRL (*(volatile uint32_t*)0x40060000)
#define TIMER_STATUS (*(volatile uint32_t*)0x40060004)
#define TIMER_CNT (*(volatile uint32_t*)0x40060008)
#define TIMER_PERIOD (*(volatile uint32_t*)0x4006000C)
#define TIMER_CC (*(volatile uint32_t*)0x40060010)

// DelSig ADC 20-bit
#define ADC_BASE 0x40100000
#define ADC_CTRL (*(volatile uint32_t*)0x40100000)
#define ADC_STATUS (*(volatile uint32_t*)0x40100004)
#define ADC_DATA (*(volatile uint32_t*)0x40100008)
#define ADC_CLOCK (*(volatile uint32_t*)0x40100010)

// GPIO 端口
#define GPIO_BASE 0x40040000
#define GPIO_DR (*(volatile uint32_t*)0x40040000)
#define GPIO_PS (*(volatile uint32_t*)0x40040004)
#define GPIO_IE (*(volatile uint32_t*)0x40040008)
#define GPIO_DM (*(volatile uint32_t*)0x4004000C)

// USB 控制器
#define USB_BASE 0x40080000
#define USB_CR0 (*(volatile uint32_t*)0x40080000)
#define USB_CR1 (*(volatile uint32_t*)0x40080004)
#define USB_STAT (*(volatile uint32_t*)0x40080008)

void cy8c5888lti_lp097_init(void);

#ifdef __cplusplus
}
#endif

#endif // CY8C5888LTI_LP097_HPP
