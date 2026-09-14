package ibm_pc_at

/**
 * 设备寄存器定义
 * 设备: IBM PC/AT
 * 生成自: IBM/IBM PC/IBM PC/AT
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: IBM Personal Computer/Advanced Technology (Model 5170)
 */

// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

import kotlinx.cinterop.*

// 外设定义
// _8259A: Programmable Interrupt Controller

// _8253: Programmable Interval Timer

// _8237: Direct Memory Access Controller

// _8042: Keyboard Controller

// CMOS: Real-Time Clock with CMOS RAM

// FDC: Floppy Disk Controller

// HDC: Hard Disk Controller (ST-506/412)

// CGA: Color Graphics Adapter

// EGA: Enhanced Graphics Adapter

// VGA: Video Graphics Array

// GAMEPORT: Game Port

// PARALLELPORT: Parallel Printer Port

// SERIALPORT: Serial Communications Port

// SPEAKER: PC Speaker

// 中断向量定义
enum class Irq(val vector: Int) {
    DIVIDE_ERROR(0),
    // Division by zero
    SINGLE_STEP(1),
    // Debug single step
    NMI(2),
    // Non-maskable interrupt
    BREAKPOINT(3),
    // INT 3 instruction
    OVERFLOW(4),
    // INTO instruction
    PRINT_SCREEN(5),
    // Print screen key
    IRQ0(8),
    // Timer interrupt
    IRQ1(9),
    // Keyboard interrupt
    IRQ2(10),
    // Cascade to IRQ8-15
    IRQ3(11),
    // COM2 interrupt
    IRQ4(12),
    // COM1 interrupt
    IRQ5(13),
    // LPT2 interrupt
    IRQ6(14),
    // Floppy disk interrupt
    IRQ7(15),
    // LPT1 interrupt
    IRQ8(112),
    // Real-time clock interrupt
    IRQ9(113),
    // Redirected IRQ2
    IRQ10(114),
    // Reserved
    IRQ11(115),
    // Reserved
    IRQ12(116),
    // PS/2 mouse interrupt
    IRQ13(117),
    // Coprocessor interrupt
    IRQ14(118),
    // Primary IDE interrupt
    IRQ15(119),
    // Secondary IDE interrupt
    VIDEO_SERVICES(16),
    // Video BIOS services
    DISK_SERVICES(19),
    // Disk BIOS services
    DOS_SERVICES(21),
    // DOS function calls
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
