package hc_sr04

/**
 * 设备寄存器定义
 * 设备: HC_SR04
 * 生成自: Generic/Sensor/HC_SR04
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: Ultrasonic Distance Sensor (2cm-400cm)
 */

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// HC_SR04: HC-SR04 Ultrasonic Sensor (4.5V-5.5V)

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
