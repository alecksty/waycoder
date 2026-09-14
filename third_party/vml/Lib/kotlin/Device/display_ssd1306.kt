package ssd1306

/**
 * 设备寄存器定义
 * 设备: SSD1306
 * 生成自: Solomon Systech/Display/SSD1306
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: SSD1306 128x64 OLED Display Controller (I2C/SPI)
 */

// CPU架构: Display
// 位宽: 8位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// SSD1306: SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)

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
