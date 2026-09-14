package _74hc595

/**
 * 设备寄存器定义
 * 设备: 74HC595
 * 生成自: TI/NXP/GPIO/74HC595
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
 */

// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 10000000 Hz

import kotlinx.cinterop.*

// 外设定义
// _74HC595: 74HC595 8-bit Shift Register (2V-6V, DIP-16)

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
