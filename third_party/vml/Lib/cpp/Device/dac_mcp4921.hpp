#ifndef MCP4921_HPP
#define MCP4921_HPP

// MCP4921寄存器定义
// 生成自: Microchip/DAC/MCP4921
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: DAC
// 位宽: 12位
// 时钟频率: 20000000 Hz

// 外设定义
// MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
#define MCP4921_BASE 0x00
#define MCP4921_DAC_VALUE (*(volatile uint16_t*)0x00000000)
#define MCP4921_DAC_VALUE_BUF 14  // VREF buffer (0=unbuffered, 1=buffered)
#define MCP4921_DAC_VALUE_GA 13  // Gain (0=2x, 1=1x)
#define MCP4921_DAC_VALUE_SHDN 12  // Shutdown (0=shutdown, 1=active)
#define MCP4921_VREF (*(volatile uint16_t*)0x00000002)

void mcp4921_init(void);

#ifdef __cplusplus
}
#endif

#endif // MCP4921_HPP
