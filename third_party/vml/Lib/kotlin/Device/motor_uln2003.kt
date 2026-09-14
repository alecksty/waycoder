package uln2003

/**
 * 设备寄存器定义
 * 设备: ULN2003
 * 生成自: ST/TI/Motor/ULN2003
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)
 */

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// ULN2003: ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)

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
