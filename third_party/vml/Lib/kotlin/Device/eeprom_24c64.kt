package _24c64

/**
 * 设备寄存器定义
 * 设备: 24C64
 * 生成自: Generic/Memory/24C64
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
 */

// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// _24C64: 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)

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
