package l298n

/**
 * 设备寄存器定义
 * 设备: L298N
 * 生成自: STMicroelectronics/Motor/L298N
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
 */

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// L298N: L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)

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
