package pic32mx170f256b

/**
 * 设备寄存器定义
 * 设备: PIC32MX170F256B
 * 生成自: Microchip/PIC32/PIC32MX170F256B
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
 */

// CPU架构: MIPS32-M4K
// 位宽: 32位
// 时钟频率: 50000000 Hz

import kotlinx.cinterop.*

// 寄存器定义









// 内存段定义
// 外设定义
// PORTA: General Purpose I/O Port A

// PORTB: General Purpose I/O Port B

// UART1: UART1

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // 
    UART1(8),
    // UART1 Interrupt
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
