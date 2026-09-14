package nuc1262se

/**
 * 设备寄存器定义
 * 设备: NUC1262SE
 * 生成自: Nuvoton/NuMicro/NUC1262SE
 * 版本: 1.0
 * 日期: 2026-04-29
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4F MCU with 512KB Flash, 96KB SRAM, 72MHz, USB
 */

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 72000000 Hz

import kotlinx.cinterop.*

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
