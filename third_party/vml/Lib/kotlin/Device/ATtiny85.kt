package attiny85

/**
 * 设备寄存器定义
 * 设备: ATtiny85
 * 生成自: Microchip/AVR/ATtiny85
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
 */

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 1000000 Hz

import kotlinx.cinterop.*

// 寄存器定义































// 内存段定义
// 外设定义
// PORTA: Port A

// PORTB: Port B

// TIPO: Timer/Counter0

// TMR1: Timer/Counter1

// ADMUX: ADC Multiplexer

// USI: Universal Serial Interface

// MCUCR: MCU Control

// WDTCR: Watchdog Timer

// EEPR: EEPROM

// GIMSK: External Interrupt

// PCMSK: Pin Change Mask

// SPMCSR: Store Program Memory

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // External Reset, Power-on Reset, Brown-out Reset
    INT0(1),
    // External Interrupt Request 0
    PCINT0(2),
    // Pin Change
    WDT(3),
    // Watchdog Timeout
    TIM1_COMPA(4),
    // Timer/Counter1 Compare Match A
    TIM1_OVF(5),
    // Timer/Counter1 Overflow
    TIM0_COMPA(6),
    // Timer/Counter0 Compare Match A
    TIM0_OVF(7),
    // Timer/Counter0 Overflow
    SPI_STC(8),
    // SPI Serial Transfer Complete
    ADC(9),
    // ADC Conversion Complete
    USI_START(10),
    // USI Start Condition
    USI_OVF(11),
    // USI Overflow
    EE_READY(12),
    // EEPROM Ready
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
