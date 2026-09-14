package nrf24l01

/**
 * 设备寄存器定义
 * 设备: NRF24L01
 * 生成自: Nordic/RF/NRF24L01
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
 */

// CPU架构: RF
// 位宽: 8位
// 时钟频率: 10000000 Hz

import kotlinx.cinterop.*

// 外设定义
// NRF24L01: nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)

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
