package ads1115

/**
 * 设备寄存器定义
 * 设备: ADS1115
 * 生成自: Texas Instruments/ADC/ADS1115
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
 */

// CPU架构: ADC
// 位宽: 16位
// 时钟频率: 400000 Hz

import kotlinx.cinterop.*

// 外设定义
// ADS1115: ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)

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
