package atmega328p

/**
 * 设备寄存器定义
 * 设备: ATmega328P
 * 生成自: Atmel/AVR/ATmega328P
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
 */

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

import kotlinx.cinterop.*

// 寄存器定义



































// 内存段定义
// 外设定义
// PORTB: Port B Data Register

// PORTC: Port C Data Register

// PORTD: Port D Data Register

// TIMER0: 8-bit Timer/Counter0

// USART0: Universal Synchronous/Asynchronous Receiver/Transmitter

// ADC: Analog-to-Digital Converter

// 中断向量定义
enum class Irq(val vector: Int) {
    INT0(1),
    // External Interrupt Request 0
    INT1(2),
    // External Interrupt Request 1
    PCINT0(3),
    // Pin Change Interrupt Request 0
    PCINT1(4),
    // Pin Change Interrupt Request 1
    PCINT2(5),
    // Pin Change Interrupt Request 2
    WDT(6),
    // Watchdog Time-out Interrupt
    TIMER2_COMPA(7),
    // Timer/Counter2 Compare Match A
    TIMER2_COMPB(8),
    // Timer/Counter2 Compare Match B
    TIMER2_OVF(9),
    // Timer/Counter2 Overflow
    TIMER1_CAPT(10),
    // Timer/Counter1 Capture Event
    TIMER1_COMPA(11),
    // Timer/Counter1 Compare Match A
    TIMER1_COMPB(12),
    // Timer/Counter1 Compare Match B
    TIMER1_OVF(13),
    // Timer/Counter1 Overflow
    TIMER0_COMPA(14),
    // Timer/Counter0 Compare Match A
    TIMER0_COMPB(15),
    // Timer/Counter0 Compare Match B
    TIMER0_OVF(16),
    // Timer/Counter0 Overflow
    SPI_STC(17),
    // SPI Serial Transfer Complete
    USART_RX(18),
    // USART Rx Complete
    USART_UDRE(19),
    // USART Data Register Empty
    USART_TX(20),
    // USART Tx Complete
    ADC(21),
    // ADC Conversion Complete
    EE_READY(22),
    // EEPROM Ready
    ANALOG_COMP(23),
    // Analog Comparator
    TWI(24),
    // Two-wire Serial Interface
    SPM_READY(25),
    // Store Program Memory Ready
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
