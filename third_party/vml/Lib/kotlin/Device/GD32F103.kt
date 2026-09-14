package gd32f103

/**
 * 设备寄存器定义
 * 设备: GD32F103
 * 生成自: GigaDevice/GD32/GD32F103
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
 */

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 108000000 Hz

import kotlinx.cinterop.*

// 寄存器定义









// 内存段定义
// 外设定义
// RCC: Reset and Clock Control

// GPIOA: General Purpose I/O Port A

// GPIOB: General Purpose I/O Port B

// GPIOC: General Purpose I/O Port C

// USART0: USART0

// USART1: USART1

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // 
    SVCALL(11),
    // 
    USART0(25),
    // USART0 Global Interrupt
    USART1(37),
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
