package _24c02

/**
 * 设备寄存器定义
 * 设备: 24C02
 * 生成自: Generic/Memory/24C02
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)
 */

// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// _24C02: 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)

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
