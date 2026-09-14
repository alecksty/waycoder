package tms320f280049

/**
 * 设备寄存器定义
 * 设备: TMS320F280049
 * 生成自: Texas Instruments/C2000/TMS320F280049
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
 */

// CPU架构: C28x-DSP
// 位宽: 32位
// 时钟频率: 100000000 Hz

import kotlinx.cinterop.*

// 寄存器定义











// 内存段定义
// 外设定义
// PLL: PLL Clock Control

// GPIO_CTRL: GPIO Control Registers

// GPIO_DATA: GPIO Data Registers

// GPIO_B_CTRL: GPIO B Control

// SCI_A: SCI-A UART

// 中断向量定义
enum class Irq(val vector: Int) {
    RESET(1),
    // 
    SCIA_RX(8),
    // SCI-A Receive Interrupt
    SCIA_TX(9),
    // SCI-A Transmit Interrupt
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
