package mcp23017

/**
 * 设备寄存器定义
 * 设备: MCP23017
 * 生成自: Microchip/GPIO/MCP23017
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
 */

// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 外设定义
// MCP23017: MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)

// 中断向量定义
enum class Irq(val vector: Int) {
    INTA(0),
    // Port A interrupt
    INTB(1),
    // Port B interrupt
}

// 寄存器访问函数
@OptIn(ExperimentalForeignApi::class)
inline fun <reified T> readReg(addr: ULong): T {
    return memScoped {
        val ptr = addr.toCPointer<T>() ?: error("Null pointer")
        ptr.pointed.readValue()
    }
}

@OptIn(ExperimentalForeignApi::class)
inline fun <reified T> writeReg(addr: ULong, value: T) {
    memScoped {
        val ptr = addr.toCPointer<T>() ?: error("Null pointer")
        ptr.pointed.writeValue(value)
    }
}

fun initDevice() {
    // 设备初始化
    // 例如: writeReg(AX.toULong(), 0x1234u)
}
