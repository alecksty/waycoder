package _8086

/**
 * 设备寄存器定义
 * 设备: 8086
 * 生成自: Intel/x86/8086
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: 16-bit microprocessor, first x86 processor
 */

// CPU架构: x86
// 位宽: 16位
// 时钟频率: 5000000 Hz

import kotlinx.cinterop.*

// 寄存器定义
val AX: UShort = 0u
    // 地址: 0x0, Accumulator
const val AX_AH: Int = 8
    // High byte of AX
const val AX_AL: Int = 0
    // Low byte of AX

val BX: UShort = 1u
    // 地址: 0x1, Base
const val BX_BH: Int = 8
    // High byte of BX
const val BX_BL: Int = 0
    // Low byte of BX

val CX: UShort = 2u
    // 地址: 0x2, Counter
const val CX_CH: Int = 8
    // High byte of CX
const val CX_CL: Int = 0
    // Low byte of CX

val DX: UShort = 3u
    // 地址: 0x3, Data
const val DX_DH: Int = 8
    // High byte of DX
const val DX_DL: Int = 0
    // Low byte of DX

val SI: UShort = 4u
    // 地址: 0x4, Source Index

val DI: UShort = 5u
    // 地址: 0x5, Destination Index

val BP: UShort = 6u
    // 地址: 0x6, Base Pointer

val SP: UShort = 7u
    // 地址: 0x7, Stack Pointer

val IP: UShort = 8u
    // 地址: 0x8, Instruction Pointer

val CS: UShort = 9u
    // 地址: 0x9, Code Segment

val DS: UShort = 16u
    // 地址: 0x10, Data Segment

val ES: UShort = 17u
    // 地址: 0x11, Extra Segment

val SS: UShort = 18u
    // 地址: 0x12, Stack Segment

val FLAGS: UShort = 19u
    // 地址: 0x13, Flags Register
const val FLAGS_CF: Int = 0
    // Carry Flag
const val FLAGS_PF: Int = 2
    // Parity Flag
const val FLAGS_AF: Int = 4
    // Auxiliary Flag
const val FLAGS_ZF: Int = 6
    // Zero Flag
const val FLAGS_SF: Int = 7
    // Sign Flag
const val FLAGS_TF: Int = 8
    // Trap Flag
const val FLAGS_IF: Int = 9
    // Interrupt Enable Flag
const val FLAGS_DF: Int = 10
    // Direction Flag
const val FLAGS_OF: Int = 11
    // Overflow Flag

// 内存段定义
// 外设定义
// PIC: Programmable Interrupt Controller

// PIT: Programmable Interval Timer

// PPI: Programmable Peripheral Interface

// 中断向量定义
enum class Irq(val vector: Int) {
    DIVIDE_ERROR(0),
    // Divide by zero
    DEBUG(1),
    // Single step
    NMI(2),
    // Non-maskable interrupt
    BREAKPOINT(3),
    // Breakpoint
    OVERFLOW(4),
    // INTO detected overflow
    IRQ0(8),
    // Timer interrupt
    IRQ1(9),
    // Keyboard interrupt
    IRQ2(10),
    // Cascade
    IRQ3(11),
    // COM2
    IRQ4(12),
    // COM1
    IRQ5(13),
    // LPT2
    IRQ6(14),
    // Floppy disk
    IRQ7(15),
    // LPT1
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
