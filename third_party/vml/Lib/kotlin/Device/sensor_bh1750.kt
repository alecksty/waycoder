package bh1750

/**
 * 设备寄存器定义
 * 设备: BH1750
 * 生成自: ROHM/Sensor/BH1750
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
 */

// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 外设定义
// BH1750: BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)

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
