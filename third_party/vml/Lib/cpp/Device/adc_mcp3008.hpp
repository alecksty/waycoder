#ifndef MCP3008_HPP
#define MCP3008_HPP

// MCP3008寄存器定义
// 生成自: Microchip/ADC/MCP3008
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ADC
// 位宽: 10位
// 时钟频率: 1350000 Hz

// 外设定义
// MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)
#define MCP3008_BASE 0x00
#define MCP3008_CH0 (*(volatile uint16_t*)0x00000000)
#define MCP3008_CH1 (*(volatile uint16_t*)0x00000001)
#define MCP3008_CH2 (*(volatile uint16_t*)0x00000002)
#define MCP3008_CH3 (*(volatile uint16_t*)0x00000003)
#define MCP3008_CH4 (*(volatile uint16_t*)0x00000004)
#define MCP3008_CH5 (*(volatile uint16_t*)0x00000005)
#define MCP3008_CH6 (*(volatile uint16_t*)0x00000006)
#define MCP3008_CH7 (*(volatile uint16_t*)0x00000007)
#define MCP3008_DIFF_01 (*(volatile uint16_t*)0x00000008)
#define MCP3008_DIFF_23 (*(volatile uint16_t*)0x00000009)

void mcp3008_init(void);

#ifdef __cplusplus
}
#endif

#endif // MCP3008_HPP
