package neo6m

/**
 * 设备寄存器定义
 * 设备: NEO6M
 * 生成自: u-blox/GPS/NEO6M
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
 */

// CPU架构: GPS
// 位宽: 8位
// 时钟频率: 9600 Hz

import kotlinx.cinterop.*

// 外设定义
// NEO6M: NEO-6M GPS Module (UART 9600bps, 3.3V-5V)

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
