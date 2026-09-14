package esp8266

/**
 * 设备寄存器定义
 * 设备: ESP8266
 * 生成自: Espressif Systems/ESP8266/ESP8266
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Espressif ESP8266 Wi-Fi SoC with integrated TCP/IP stack
 */

// CPU架构: Xtensa LX106
// 位宽: 0位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// WIFI: Wi-Fi 802.11 b/g/n

// UART0: Universal Asynchronous Receiver/Transmitter 0

// SPI: Serial Peripheral Interface

// I2C: Inter-Integrated Circuit

// GPIO: General Purpose I/O

// TIMER: Hardware Timer

// ADC: Analog-to-Digital Converter

// PWM: Pulse Width Modulation

// 中断向量定义
enum class Irq(val vector: Int) {
    NMI(1),
    // Non-maskable interrupt
    LEVEL1(3),
    // Level 1 interrupt
    LEVEL2(4),
    // Level 2 interrupt
    LEVEL3(5),
    // Level 3 interrupt
    LEVEL4(6),
    // Level 4 interrupt
    LEVEL5(7),
    // Level 5 interrupt
    TIMER0(8),
    // Timer 0 interrupt
    TIMER1(9),
    // Timer 1 interrupt
    UART0(10),
    // UART0 interrupt
    UART1(11),
    // UART1 interrupt
    GPIO(12),
    // GPIO interrupt
    PWM(13),
    // PWM interrupt
    I2C(14),
    // I2C interrupt
    SPI(15),
    // SPI interrupt
    ADC(16),
    // ADC interrupt
    WIFI(17),
    // Wi-Fi interrupt
    RTC(18),
    // RTC interrupt
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
