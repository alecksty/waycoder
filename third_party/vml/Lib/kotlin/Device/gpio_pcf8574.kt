package pcf8574

/**
 * 设备寄存器定义
 * 设备: PCF8574
 * 生成自: NXP/TI/GPIO/PCF8574
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
 */

// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 100000 Hz

import kotlinx.cinterop.*

// 外设定义
// PCF8574: PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)

// 中断向量定义
enum class Irq(val vector: Int) {
    INT(0),
    // Pin change interrupt (open-drain, active low)
}

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
