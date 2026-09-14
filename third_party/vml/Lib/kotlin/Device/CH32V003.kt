package ch32v003

/**
 * 设备寄存器定义
 * 设备: CH32V003
 * 生成自: WCH/CH32V0/CH32V003
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
 */

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 48000000 Hz

import kotlinx.cinterop.*

// 寄存器定义




// 内存段定义
// 外设定义
// RCC: Reset and Clock Control

// GPIOA: General Purpose I/O Port A

// GPIOC: General Purpose I/O Port C

// GPIOD: General Purpose I/O Port D

// USART1: USART1

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(1),
    // 
    MACHINESOFTWARE(3),
    // 
    MACHINETIMER(7),
    // 
    MACHINEEXTERNAL(11),
    // 
    USART1(25),
    // USART1 Global Interrupt
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
