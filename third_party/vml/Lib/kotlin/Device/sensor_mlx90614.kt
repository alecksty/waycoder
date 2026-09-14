package mlx90614

/**
 * 设备寄存器定义
 * 设备: MLX90614
 * 生成自: Melexis/Sensor/MLX90614
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
 */

// CPU架构: Sensor
// 位宽: 17位
// 时钟频率: 100000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// MLX90614: MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)

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
