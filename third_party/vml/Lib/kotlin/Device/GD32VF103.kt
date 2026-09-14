package gd32vf103

/**
 * 设备寄存器定义
 * 设备: GD32VF103
 * 生成自: GigaDevice/GD32/GD32VF103
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
 */

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 108000000 Hz

import kotlinx.cinterop.*

// 寄存器定义







// 内存段定义
// 外设定义
// RCU: Reset and Clock Control

// GPIOA: General Purpose I/O Port A

// GPIOB: General Purpose I/O Port B

// GPIOC: General Purpose I/O Port C

// USART0: USART0

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
    USART0(25),
    // USART0 Global Interrupt
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
