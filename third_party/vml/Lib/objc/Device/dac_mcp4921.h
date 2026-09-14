// MCP4921 设备定义 - Objective-C 头文件
// 生成自: Microchip/DAC/MCP4921
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP4921 12-bit SPI DAC (single channel, 2x buffered output)
// CPU架构: DAC
// 位宽: 12位
// 时钟频率: 20000000 Hz

#ifndef MCP4921_DEVICE_H
#define MCP4921_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
#define MCP4921_BASE 0x00
#define MCP4921_DAC_VALUE_ADDR 0x00
#define MCP4921_DAC_VALUE_BUF_BIT 14  // VREF buffer (0=unbuffered, 1=buffered)
#define MCP4921_DAC_VALUE_GA_BIT 13  // Gain (0=2x, 1=1x)
#define MCP4921_DAC_VALUE_SHDN_BIT 12  // Shutdown (0=shutdown, 1=active)
#define MCP4921_VREF_ADDR 0x02

#endif /* MCP4921_DEVICE_H */
