// MCP23017 设备定义 - Objective-C 头文件
// 生成自: Microchip/GPIO/MCP23017
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

#ifndef MCP23017_DEVICE_H
#define MCP23017_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
#define MCP23017_BASE 0x20
#define MCP23017_IODIRA_ADDR 0x00
#define MCP23017_IODIRB_ADDR 0x01
#define MCP23017_GPIOA_ADDR 0x12
#define MCP23017_GPIOB_ADDR 0x13
#define MCP23017_GPINTENA_ADDR 0x04
#define MCP23017_GPINTENB_ADDR 0x05
#define MCP23017_INTCONA_ADDR 0x08
#define MCP23017_IOCON_ADDR 0x0A
#define MCP23017_GPPUA_ADDR 0x0C
#define MCP23017_GPPUB_ADDR 0x0D

// 中断向量定义
#define INT_INTA 0  // Port A interrupt
#define INT_INTB 1  // Port B interrupt

#endif /* MCP23017_DEVICE_H */
