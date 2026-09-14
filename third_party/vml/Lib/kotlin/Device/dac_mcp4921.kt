package mcp4921

/**
 * 设备寄存器定义
 * 设备: MCP4921
 * 生成自: Microchip/DAC/MCP4921
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP4921 12-bit SPI DAC (single channel, 2x buffered output)
 */

// CPU架构: DAC
// 位宽: 12位
// 时钟频率: 20000000 Hz

import kotlinx.cinterop.*

// 外设定义
// MCP4921: MCP4921 12-bit DAC (SPI, 2.7V-5.5V)

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
