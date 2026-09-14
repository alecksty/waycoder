package samd21

/**
 * 设备寄存器定义
 * 设备: SAMD21
 * 生成自: Atmel (Microchip)/SAM D/SAMD21
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
 */

// CPU架构: ARM Cortex-M0+
// 位宽: 0位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// PM: Power Manager

// SYSCTRL: System Controller

// GCLK: Generic Clock Generator

// WDT: Watchdog Timer

// RTC: Real-Time Clock

// EIC: External Interrupt Controller

// SERCOM0: Serial Communication Interface 0

// ADC: Analog-to-Digital Converter

// DAC: Digital-to-Analog Converter

// PORT: General Purpose I/O

// TC0: Timer/Counter 0

// USB: USB Device Controller

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // Reset vector
    NONMASKABLEINT(1),
    // Non-maskable interrupt
    HARDFAULT(2),
    // Hard fault
    SVCALL(3),
    // Supervisor call
    PENDSV(4),
    // Pendable service call
    SYSTICK(5),
    // System tick timer
    PM(6),
    // Power Manager
    SYSCTRL(7),
    // System Controller
    WDT(8),
    // Watchdog Timer
    RTC(9),
    // Real-Time Clock
    EIC(10),
    // External Interrupt Controller
    NVMCTRL(11),
    // Non-Volatile Memory Controller
    DMAC(12),
    // Direct Memory Access Controller
    USB(13),
    // USB Device Controller
    EVSYS(14),
    // Event System
    SERCOM0(15),
    // Serial Communication Interface 0
    SERCOM1(16),
    // Serial Communication Interface 1
    SERCOM2(17),
    // Serial Communication Interface 2
    SERCOM3(18),
    // Serial Communication Interface 3
    SERCOM4(19),
    // Serial Communication Interface 4
    SERCOM5(20),
    // Serial Communication Interface 5
    TCC0(21),
    // Timer/Counter for Control 0
    TCC1(22),
    // Timer/Counter for Control 1
    TCC2(23),
    // Timer/Counter for Control 2
    TC3(24),
    // Timer/Counter 3
    TC4(25),
    // Timer/Counter 4
    TC5(26),
    // Timer/Counter 5
    TC6(27),
    // Timer/Counter 6
    TC7(28),
    // Timer/Counter 7
    ADC(29),
    // Analog-to-Digital Converter
    AC(30),
    // Analog Comparator
    DAC(31),
    // Digital-to-Analog Converter
    PTC(32),
    // Peripheral Touch Controller
    I2S(33),
    // Inter-IC Sound Interface
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
