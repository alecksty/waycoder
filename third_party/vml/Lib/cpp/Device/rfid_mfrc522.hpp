#ifndef MFRC522_HPP
#define MFRC522_HPP

// MFRC522寄存器定义
// 生成自: NXP/RFID/MFRC522
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RFID
// 位宽: 8位
// 时钟频率: 10000000 Hz

// 内存段定义
// 64-byte FIFO buffer
#define FIFO_START 0x00
#define FIFO_END 0x3F
#define FIFO_SIZE 64

// 外设定义
// MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
#define MFRC522_BASE 0x00
#define MFRC522_CMD (*(volatile uint8_t*)0x00000001)
#define MFRC522_COM_IRQ (*(volatile uint8_t*)0x00000004)
#define MFRC522_COM_IRQ_TX_IRQ 6  // Transmitter interrupt
#define MFRC522_COM_IRQ_RX_IRQ 5  // Receiver interrupt
#define MFRC522_COM_IRQ_IDLE_IRQ 4  // Idle interrupt
#define MFRC522_COM_IRQ_TIMER_IRQ 0  // Timer interrupt
#define MFRC522_COM_IRQ_EN (*(volatile uint8_t*)0x00000005)
#define MFRC522_ERROR (*(volatile uint8_t*)0x00000006)
#define MFRC522_STATUS2 (*(volatile uint8_t*)0x00000008)
#define MFRC522_FIFO_DATA (*(volatile uint8_t*)0x00000009)
#define MFRC522_FIFO_LEVEL (*(volatile uint8_t*)0x0000000A)
#define MFRC522_TX_CTRL (*(volatile uint8_t*)0x00000014)
#define MFRC522_TX_ASK (*(volatile uint8_t*)0x00000015)
#define MFRC522_MODE (*(volatile uint8_t*)0x00000011)
#define MFRC522_VERSION (*(volatile uint8_t*)0x00000037)

void mfrc522_init(void);

#ifdef __cplusplus
}
#endif

#endif // MFRC522_HPP
