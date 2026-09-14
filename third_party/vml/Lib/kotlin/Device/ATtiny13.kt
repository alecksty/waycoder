package attiny13

/**
 * 设备寄存器定义
 * 设备: ATtiny13
 * 生成自: Atmel/AVR/ATtiny13
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
 */

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 20000000 Hz

import kotlinx.cinterop.*

// 寄存器定义














// 内存段定义
// 外设定义
// PORTB: Port B (only port)

// TIMER0: 8-bit Timer/Counter0

// ADC: Analog-to-Digital

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(1),
    // 
    INT0(2),
    // External Interrupt 0
    PCINT0(3),
    // Pin Change Interrupt
    TIM0_OVF(4),
    // Timer0 Overflow
    TIM0_COMPA(5),
    // Timer0 Compare A
    WDT(6),
    // Watchdog Timeout
    ADC(7),
    // ADC Conversion Complete
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
