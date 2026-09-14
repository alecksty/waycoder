package mcp3008

/**
 * 设备寄存器定义
 * 设备: MCP3008
 * 生成自: Microchip/ADC/MCP3008
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP3008 10-bit SPI ADC (8-channel, 200ksps)
 */

// CPU架构: ADC
// 位宽: 10位
// 时钟频率: 1350000 Hz

import kotlinx.cinterop.*

// 外设定义
// MCP3008: MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)

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
