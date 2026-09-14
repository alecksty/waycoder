package atmega2560

/**
 * 设备寄存器定义
 * 设备: ATmega2560
 * 生成自: Atmel/AVR/ATmega2560
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
 */

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

import kotlinx.cinterop.*

// 寄存器定义






// 内存段定义
// 外设定义
// PORTA: Port A

// PORTB: Port B

// PORTC: Port C

// PORTD: Port D

// PORTE: Port E

// PORTF: Port F

// PORTG: Port G

// USART0: USART 0

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(1),
    // 
    INT0(2),
    // 
    INT1(3),
    // 
    INT2(4),
    // 
    INT3(5),
    // 
    INT4(6),
    // 
    INT5(7),
    // 
    INT6(8),
    // 
    INT7(9),
    // 
    PCINT0(10),
    // 
    PCINT1(11),
    // 
    PCINT2(12),
    // 
    WDT(13),
    // 
    TIM2_COMPA(14),
    // 
    TIM2_COMPB(15),
    // 
    TIM2_OVF(16),
    // 
    TIM1_CAPT(17),
    // 
    TIM1_COMPA(18),
    // 
    TIM1_COMPB(19),
    // 
    TIM1_OVF(20),
    // 
    TIM0_COMPA(21),
    // 
    TIM0_COMPB(22),
    // 
    TIM0_OVF(23),
    // 
    SPI_STC(24),
    // 
    USART0_RX(25),
    // 
    USART0_UDRE(26),
    // 
    USART0_TX(27),
    // 
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
