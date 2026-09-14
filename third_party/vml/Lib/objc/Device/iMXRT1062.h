// i.MX RT1062 设备定义 - Objective-C 头文件
// 生成自: NXP/i.MX RT/i.MX RT1062
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 528000000 Hz

#ifndef I.MX RT1062_DEVICE_H
#define I.MX RT1062_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// LPUART 1
#define UART1_BASE 0x40184000
#define UART1_VERID_ADDR 0x000
#define UART1_CTRL_ADDR 0x010
#define UART1_STAT_ADDR 0x014
#define UART1_DATA_ADDR 0x01C
#define UART1_BAUD_ADDR 0x024
// LPUART 2
#define UART2_BASE 0x40188000
#define UART2_CTRL_ADDR 0x010
#define UART2_STAT_ADDR 0x014
#define UART2_DATA_ADDR 0x01C
#define UART2_BAUD_ADDR 0x024
// GPIO 1
#define GPIO1_BASE 0x401B8000
#define GPIO1_DR_ADDR 0x000
#define GPIO1_GDIR_ADDR 0x004
#define GPIO1_PSR_ADDR 0x008
#define GPIO1_ICR1_ADDR 0x00C
#define GPIO1_ICR2_ADDR 0x010
#define GPIO1_IMR_ADDR 0x014
#define GPIO1_ISR_ADDR 0x018
#define GPIO1_EDGE_SEL_ADDR 0x01C
// GPT 定时器 1
#define GPT1_BASE 0x401EC000
#define GPT1_CR_ADDR 0x000
#define GPT1_PR_ADDR 0x004
#define GPT1_SR_ADDR 0x008
#define GPT1_IR_ADDR 0x00C
#define GPT1_OCR1_ADDR 0x010
#define GPT1_CNT_ADDR 0x024
// USB OTG 1
#define USB1_BASE 0x402E0000
#define USB1_ID_ADDR 0x000
#define USB1_OTGSC_ADDR 0x00C
#define USB1_USBCMD_ADDR 0x100
#define USB1_PORTSC1_ADDR 0x184

#endif /* I.MX RT1062_DEVICE_H */
