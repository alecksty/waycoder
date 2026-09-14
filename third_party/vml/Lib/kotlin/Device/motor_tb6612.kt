package tb6612

/**
 * 设备寄存器定义
 * 设备: TB6612
 * 生成自: Toshiba/Motor/TB6612
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
 */

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 100000 Hz

import kotlinx.cinterop.*

// 外设定义
// TB6612: TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)

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
