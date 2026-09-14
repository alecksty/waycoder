// MCP4725 设备定义 - Objective-C 头文件
// 生成自: Microchip/DAC/MCP4725
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
// CPU架构: DAC
// 位宽: 12位
// 时钟频率: 400000 Hz

#ifndef MCP4725_DEVICE_H
#define MCP4725_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define EEPROM_START 0x00
#define EEPROM_END 0x01
#define EEPROM_SIZE 2  // Power-on default DAC value

// 外设定义
// MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
#define MCP4725_BASE 0x60
#define MCP4725_DAC_VALUE_ADDR 0x00
#define MCP4725_DAC_VALUE_PD_BIT 12  // Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
#define MCP4725_WRITE_EEPROM_ADDR 0x60

#endif /* MCP4725_DEVICE_H */
