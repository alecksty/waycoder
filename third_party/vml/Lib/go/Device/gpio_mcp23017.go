package device_mcp23017

import (
    "unsafe"
)

// MCP23017寄存器定义
// 生成自: Microchip/GPIO/MCP23017
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)

// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

// 外设定义
// MCP23017: MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)

// 中断向量定义
const (
    IRQ_INTA = 0
    // Port A interrupt
    IRQ_INTB = 1
    // Port B interrupt
)

// 初始化设备寄存器映射
func InitMCP23017() {
    // 这里应该实现实际的硬件映射
    // 例如: AX = (*uint16)(unsafe.Pointer(uintptr(0xE000)))
}
