// CY8C5888LTI-LP097 设备定义 - Objective-C 头文件
// 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 80000000 Hz

#ifndef CY8C5888LTI-LP097_DEVICE_H
#define CY8C5888LTI-LP097_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// SCB UART (可编程)
#define UART_BASE 0x40050000
#define UART_CTRL_ADDR 0x00
#define UART_STATUS_ADDR 0x04
#define UART_TX_DATA_ADDR 0x08
#define UART_RX_DATA_ADDR 0x0C
// SCB I2C
#define I2C_BASE 0x40051000
#define I2C_CTRL_ADDR 0x00
#define I2C_STATUS_ADDR 0x04
#define I2C_TX_DATA_ADDR 0x08
#define I2C_RX_DATA_ADDR 0x0C
// TCPWM 定时器
#define TIMER_BASE 0x40060000
#define TIMER_CTRL_ADDR 0x00
#define TIMER_STATUS_ADDR 0x04
#define TIMER_CNT_ADDR 0x08
#define TIMER_PERIOD_ADDR 0x0C
#define TIMER_CC_ADDR 0x10
// DelSig ADC 20-bit
#define ADC_BASE 0x40100000
#define ADC_CTRL_ADDR 0x00
#define ADC_STATUS_ADDR 0x04
#define ADC_DATA_ADDR 0x08
#define ADC_CLOCK_ADDR 0x10
// GPIO 端口
#define GPIO_BASE 0x40040000
#define GPIO_DR_ADDR 0x00
#define GPIO_PS_ADDR 0x04
#define GPIO_IE_ADDR 0x08
#define GPIO_DM_ADDR 0x0C
// USB 控制器
#define USB_BASE 0x40080000
#define USB_CR0_ADDR 0x00
#define USB_CR1_ADDR 0x04
#define USB_STAT_ADDR 0x08

#endif /* CY8C5888LTI-LP097_DEVICE_H */
