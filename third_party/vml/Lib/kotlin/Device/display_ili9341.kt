package ili9341

/**
 * 设备寄存器定义
 * 设备: ILI9341
 * 生成自: Ilitek/Display/ILI9341
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
 */

// CPU架构: Display
// 位宽: 18位
// 时钟频率: 20000000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// ILI9341: ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)

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
