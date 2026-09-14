package mfrc522

/**
 * 设备寄存器定义
 * 设备: MFRC522
 * 生成自: NXP/RFID/MFRC522
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
 */

// CPU架构: RFID
// 位宽: 8位
// 时钟频率: 10000000 Hz

import kotlinx.cinterop.*

// 内存段定义
// 外设定义
// MFRC522: MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)

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
