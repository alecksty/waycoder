package a4988

/**
 * 设备寄存器定义
 * 设备: A4988
 * 生成自: Allegro/Motor/A4988
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
 */

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// A4988: A4988 Stepper Motor Driver (3.3V/5V logic)

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
