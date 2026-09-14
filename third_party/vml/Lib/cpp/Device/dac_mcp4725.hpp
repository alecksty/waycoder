#ifndef MCP4725_HPP
#define MCP4725_HPP

// MCP4725寄存器定义
// 生成自: Microchip/DAC/MCP4725
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: DAC
// 位宽: 12位
// 时钟频率: 400000 Hz

// 内存段定义
// Power-on default DAC value
#define EEPROM_START 0x00
#define EEPROM_END 0x01
#define EEPROM_SIZE 2

// 外设定义
// MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
#define MCP4725_BASE 0x60
#define MCP4725_DAC_VALUE (*(volatile uint16_t*)0x00000060)
#define MCP4725_DAC_VALUE_PD 12  // Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
#define MCP4725_WRITE_EEPROM (*(volatile uint16_t*)0x000000C0)

void mcp4725_init(void);

#ifdef __cplusplus
}
#endif

#endif // MCP4725_HPP
