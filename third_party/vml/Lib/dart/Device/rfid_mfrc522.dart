// MFRC522 设备定义 - Dart 库
// 生成自: NXP/RFID/MFRC522
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
// CPU架构: RFID
// 位宽: 8位
// 时钟频率: 10000000 Hz

class MFRC522Device {
  static const String deviceName = "MFRC522";
  static const String manufacturer = "NXP";
  static const String family = "RFID";
  static const String version = "1.0";
  static const String architecture = "RFID";
  static const int bits = 8;
  static const int clockFrequency = 10000000;

  // 内存段定义
  static const int FIFO_START = 0x00;
  static const int FIFO_END = 0x3F;
  static const int FIFO_SIZE = 64;  // 64-byte FIFO buffer

  // 外设定义
  // MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
  static const int MFRC522_BASE = 0x00;
  static const int MFRC522_CMD_ADDR = 0x01;
  static const int MFRC522_COM_IRQ_ADDR = 0x04;
  static const int MFRC522_COM_IRQ_TX_IRQ_BIT = 6;  // Transmitter interrupt
  static const int MFRC522_COM_IRQ_RX_IRQ_BIT = 5;  // Receiver interrupt
  static const int MFRC522_COM_IRQ_IDLE_IRQ_BIT = 4;  // Idle interrupt
  static const int MFRC522_COM_IRQ_TIMER_IRQ_BIT = 0;  // Timer interrupt
  static const int MFRC522_COM_IRQ_EN_ADDR = 0x05;
  static const int MFRC522_ERROR_ADDR = 0x06;
  static const int MFRC522_STATUS2_ADDR = 0x08;
  static const int MFRC522_FIFO_DATA_ADDR = 0x09;
  static const int MFRC522_FIFO_LEVEL_ADDR = 0x0A;
  static const int MFRC522_TX_CTRL_ADDR = 0x14;
  static const int MFRC522_TX_ASK_ADDR = 0x15;
  static const int MFRC522_MODE_ADDR = 0x11;
  static const int MFRC522_VERSION_ADDR = 0x37;

}
