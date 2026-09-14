package intel_80286

/**
 * 设备寄存器定义
 * 设备: Intel 80286
 * 生成自: Intel/x86/Intel 80286
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Intel 80286 16-bit microprocessor with memory management and protection
 */

// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// _8259A: Programmable Interrupt Controller

// _8253: Programmable Interval Timer

// _8255: Programmable Peripheral Interface

// _8237: Direct Memory Access Controller

// _8042: Keyboard Controller

// 中断向量定义
enum class Irq(val vector: Int) {
    DIVIDE_ERROR(0),
    // Division by zero or overflow
    DEBUG_EXCEPTION(1),
    // Single-step or debug register access
    NMI(2),
    // Non-maskable interrupt
    BREAKPOINT(3),
    // INT 3 instruction
    OVERFLOW(4),
    // INTO instruction with OF=1
    BOUNDS_CHECK(5),
    // BOUND instruction
    INVALID_OPCODE(6),
    // Undefined opcode
    COPROCESSOR_NOT_AVAILABLE(7),
    // No math coprocessor
    DOUBLE_FAULT(8),
    // Two exceptions in handler
    COPROCESSOR_SEGMENT_OVERRUN(9),
    // Coprocessor operand beyond segment
    INVALID_TSS(10),
    // Invalid Task State Segment
    SEGMENT_NOT_PRESENT(11),
    // Segment not present
    STACK_FAULT(12),
    // Stack segment limit violation
    GENERAL_PROTECTION(13),
    // Memory access violation
    PAGE_FAULT(14),
    // Page not present (386+)
    COPROCESSOR_ERROR(16),
    // Math coprocessor error
    IRQ0(32),
    // Timer interrupt
    IRQ1(33),
    // Keyboard interrupt
    IRQ2(34),
    // Cascade to IRQ8-15
    IRQ3(35),
    // COM2 interrupt
    IRQ4(36),
    // COM1 interrupt
    IRQ5(37),
    // LPT2 interrupt
    IRQ6(38),
    // Floppy disk interrupt
    IRQ7(39),
    // LPT1 interrupt
    IRQ8(40),
    // Real-time clock interrupt
    IRQ9(41),
    // Redirected IRQ2
    IRQ10(42),
    // Reserved
    IRQ11(43),
    // Reserved
    IRQ12(44),
    // PS/2 mouse interrupt
    IRQ13(45),
    // Coprocessor interrupt
    IRQ14(46),
    // Primary IDE interrupt
    IRQ15(47),
    // Secondary IDE interrupt
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
