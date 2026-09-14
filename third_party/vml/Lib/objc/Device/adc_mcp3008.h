// MCP3008 设备定义 - Objective-C 头文件
// 生成自: Microchip/ADC/MCP3008
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP3008 10-bit SPI ADC (8-channel, 200ksps)
// CPU架构: ADC
// 位宽: 10位
// 时钟频率: 1350000 Hz

#ifndef MCP3008_DEVICE_H
#define MCP3008_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)
#define MCP3008_BASE 0x00
#define MCP3008_CH0_ADDR 0x00
#define MCP3008_CH1_ADDR 0x01
#define MCP3008_CH2_ADDR 0x02
#define MCP3008_CH3_ADDR 0x03
#define MCP3008_CH4_ADDR 0x04
#define MCP3008_CH5_ADDR 0x05
#define MCP3008_CH6_ADDR 0x06
#define MCP3008_CH7_ADDR 0x07
#define MCP3008_DIFF_01_ADDR 0x08
#define MCP3008_DIFF_23_ADDR 0x09

#endif /* MCP3008_DEVICE_H */
