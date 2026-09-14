package max7219

/**
 * 设备寄存器定义
 * 设备: MAX7219
 * 生成自: Maxim/LED/MAX7219
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
 */

// CPU架构: LED
// 位宽: 8位
// 时钟频率: 10000000 Hz

import kotlinx.cinterop.*

// 外设定义
// MAX7219: MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)

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
