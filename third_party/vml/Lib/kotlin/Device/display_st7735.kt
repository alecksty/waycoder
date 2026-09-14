package st7735

/**
 * 设备寄存器定义
 * 设备: ST7735
 * 生成自: Sitronix/Display/ST7735
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ST7735 1.8" 128x160 TFT LCD Display (SPI, 16-bit color)
 */

// CPU架构: Display
// 位宽: 16位
// 时钟频率: 16000000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// ST7735: ST7735 128x160 TFT (SPI, 3.3V-5V)

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
