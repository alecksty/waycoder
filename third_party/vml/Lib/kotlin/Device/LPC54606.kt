package lpc54606

/**
 * 设备寄存器定义
 * 设备: LPC54606
 * 生成自: NXP/LPC/LPC54606
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
 */

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 180000000 Hz

import kotlinx.cinterop.*

// 寄存器定义









// 内存段定义
// 外设定义
// SYSCON: System Control

// GPIO: General Purpose I/O

// USART0: USART0

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // 
    SVCALL(11),
    // 
    USART0(24),
    // USART0 Interrupt
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
