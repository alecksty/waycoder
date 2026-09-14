#ifndef MCP23017_HPP
#define MCP23017_HPP

// MCP23017寄存器定义
// 生成自: Microchip/GPIO/MCP23017
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

// 外设定义
// MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
#define MCP23017_BASE 0x20
#define MCP23017_IODIRA (*(volatile uint8_t*)0x00000020)
#define MCP23017_IODIRB (*(volatile uint8_t*)0x00000021)
#define MCP23017_GPIOA (*(volatile uint8_t*)0x00000032)
#define MCP23017_GPIOB (*(volatile uint8_t*)0x00000033)
#define MCP23017_GPINTENA (*(volatile uint8_t*)0x00000024)
#define MCP23017_GPINTENB (*(volatile uint8_t*)0x00000025)
#define MCP23017_INTCONA (*(volatile uint8_t*)0x00000028)
#define MCP23017_IOCON (*(volatile uint8_t*)0x0000002A)
#define MCP23017_GPPUA (*(volatile uint8_t*)0x0000002C)
#define MCP23017_GPPUB (*(volatile uint8_t*)0x0000002D)

// 中断向量定义
#define INTA_VECTOR 0  // Port A interrupt
#define INTB_VECTOR 1  // Port B interrupt

void mcp23017_init(void);

#ifdef __cplusplus
}
#endif

#endif // MCP23017_HPP
