// MFRC522 设备定义 - Objective-C 头文件
// 生成自: NXP/RFID/MFRC522
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
// CPU架构: RFID
// 位宽: 8位
// 时钟频率: 10000000 Hz

#ifndef MFRC522_DEVICE_H
#define MFRC522_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define FIFO_START 0x00
#define FIFO_END 0x3F
#define FIFO_SIZE 64  // 64-byte FIFO buffer

// 外设定义
// MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
#define MFRC522_BASE 0x00
#define MFRC522_CMD_ADDR 0x01
#define MFRC522_COM_IRQ_ADDR 0x04
#define MFRC522_COM_IRQ_TX_IRQ_BIT 6  // Transmitter interrupt
#define MFRC522_COM_IRQ_RX_IRQ_BIT 5  // Receiver interrupt
#define MFRC522_COM_IRQ_IDLE_IRQ_BIT 4  // Idle interrupt
#define MFRC522_COM_IRQ_TIMER_IRQ_BIT 0  // Timer interrupt
#define MFRC522_COM_IRQ_EN_ADDR 0x05
#define MFRC522_ERROR_ADDR 0x06
#define MFRC522_STATUS2_ADDR 0x08
#define MFRC522_FIFO_DATA_ADDR 0x09
#define MFRC522_FIFO_LEVEL_ADDR 0x0A
#define MFRC522_TX_CTRL_ADDR 0x14
#define MFRC522_TX_ASK_ADDR 0x15
#define MFRC522_MODE_ADDR 0x11
#define MFRC522_VERSION_ADDR 0x37

#endif /* MFRC522_DEVICE_H */
