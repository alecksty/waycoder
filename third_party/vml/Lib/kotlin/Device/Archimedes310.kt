package acorn_archimedes_a310

/**
 * 设备寄存器定义
 * 设备: Acorn-Archimedes-A310
 * 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
 */

// CPU架构: ARM250
// 位宽: 32位
// 时钟频率: 26000000 Hz

import kotlinx.cinterop.*

// 寄存器定义

















// 内存段定义
// 外设定义
// IOC: I/O Controller (IOC) - Interrupt/Keyboard/RTC

// MEMC: Memory Controller (MEMC1)

// VIDC: Video Controller - VIDC1

// FDC: Intel 82710 Floppy Disk Controller

// SERIAL: Serial Port (via IOC)

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(0),
    // Reset
    UND(1),
    // Undefined instruction
    SWI(2),
    // Software Interrupt (SWI/SVC)
    PABORT(3),
    // Prefetch Abort
    DABORT(4),
    // Data Abort
    ADDRESS(5),
    // Address Exception
    IRQ(6),
    // IRQ interrupt (IOC)
    FIQ(7),
    // FIQ interrupt (VIDC)
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
