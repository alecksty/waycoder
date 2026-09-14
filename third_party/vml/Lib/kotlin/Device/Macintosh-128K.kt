package macintosh_128k

/**
 * 设备寄存器定义
 * 设备: Macintosh-128K
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
 */

// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7998000 Hz

import kotlinx.cinterop.*

// 寄存器定义
val D0: UInt = 0u
    // 地址: 0x0, Data Register 0

val D1: UInt = 0u
    // 地址: 0x0, Data Register 1

val D2: UInt = 0u
    // 地址: 0x0, Data Register 2

val D3: UInt = 0u
    // 地址: 0x0, Data Register 3

val D4: UInt = 0u
    // 地址: 0x0, Data Register 4

val D5: UInt = 0u
    // 地址: 0x0, Data Register 5

val D6: UInt = 0u
    // 地址: 0x0, Data Register 6

val D7: UInt = 0u
    // 地址: 0x0, Data Register 7

val A0: UInt = 0u
    // 地址: 0x0, Address Register 0

val A1: UInt = 0u
    // 地址: 0x0, Address Register 1

val A2: UInt = 0u
    // 地址: 0x0, Address Register 2

val A3: UInt = 0u
    // 地址: 0x0, Address Register 3

val A4: UInt = 0u
    // 地址: 0x0, Address Register 4

val A5: UInt = 0u
    // 地址: 0x0, Address Register 5

val A6: UInt = 0u
    // 地址: 0x0, Address Register 6

val A7: UInt = 0u
    // 地址: 0x0, Address Register 7 (SP)

val PC: UInt = 0u
    // 地址: 0x0, Program Counter

val SR: UShort = 0u
    // 地址: 0x0, Status Register

// 外设定义
// VIA: Versatile Interface Adapter (6522)

// IWM: Integrated Woz Machine (floppy controller)

// SCC: Zilog 8530 Serial Communications Controller

// SOUND: Built-in speaker

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET_SP(0),
    // Reset (Initial SP)
    RESET_PC(4),
    // Reset (Initial PC)
    AUTOVECTOR1(24),
    // Auto vector 1
    AUTOVECTOR2(25),
    // Auto vector 2
    AUTOVECTOR3(26),
    // Auto vector 3
    AUTOVECTOR4(27),
    // Auto vector 4
    AUTOVECTOR5(28),
    // Auto vector 5
    AUTOVECTOR6(29),
    // Auto vector 6
    AUTOVECTOR7(30),
    // Auto vector 7
    SPURIOUS(31),
    // Spurious interrupt
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
