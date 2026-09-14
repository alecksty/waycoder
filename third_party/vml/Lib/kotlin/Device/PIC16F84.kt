package pic16f84

/**
 * 设备寄存器定义
 * 设备: PIC16F84
 * 生成自: Microchip Technology/PIC16/PIC16F84
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
 */

// CPU架构: PIC16
// 位宽: 0位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// TIMER0: 8-bit timer/counter with prescaler

// TIMER1: 16-bit timer/counter with prescaler

// WATCHDOG: Watchdog Timer

// EEPROM: 64-byte EEPROM data memory

// GPIO: General Purpose I/O

// 中断向量定义
enum class Irq(val vector: Int) {
    INT(4),
    // External interrupt on RB0/INT pin
    TMR0(4),
    // Timer0 overflow interrupt
    PORTB(4),
    // PORTB change interrupt (RB4-RB7)
    EEPROM(4),
    // EEPROM write complete interrupt
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
