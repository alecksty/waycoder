package stm32f303cct6

/**
 * 设备寄存器定义
 * 设备: STM32F303CCT6
 * 生成自: STMicroelectronics/STM32/STM32F303CCT6
 * 版本: 1.0
 * 日期: 2026-04-29
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
 */

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 72000000 Hz

import kotlinx.cinterop.*

// 外设定义
// USART1: USART 1

// USART2: USART 2

// USART3: USART 3

// GPIOA: GPIO Port A

// TIM1: 高级定时器 1

// ADC1: ADC 1

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
