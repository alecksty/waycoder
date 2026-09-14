// NRF24L01 设备定义 - Dart 库
// 生成自: Nordic/RF/NRF24L01
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
// CPU架构: RF
// 位宽: 8位
// 时钟频率: 10000000 Hz

class NRF24L01Device {
  static const String deviceName = "NRF24L01";
  static const String manufacturer = "Nordic";
  static const String family = "RF";
  static const String version = "1.0";
  static const String architecture = "RF";
  static const int bits = 8;
  static const int clockFrequency = 10000000;

  // 外设定义
  // nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
  static const int NRF24L01_BASE = 0x00;
  static const int NRF24L01_CONFIG_ADDR = 0x00;
  static const int NRF24L01_CONFIG_PWR_UP_BIT = 1;  // Power up (1=on)
  static const int NRF24L01_CONFIG_PRIM_RX_BIT = 0;  // Primary RX mode (1=RX, 0=TX)
  static const int NRF24L01_EN_AA_ADDR = 0x01;
  static const int NRF24L01_EN_RXADDR_ADDR = 0x02;
  static const int NRF24L01_SETUP_AW_ADDR = 0x03;
  static const int NRF24L01_SETUP_RETR_ADDR = 0x04;
  static const int NRF24L01_RF_CH_ADDR = 0x05;
  static const int NRF24L01_RF_SETUP_ADDR = 0x06;
  static const int NRF24L01_RF_SETUP_RF_PWR_BIT = 1;  // TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
  static const int NRF24L01_RF_SETUP_RF_DR_BIT = 3;  // Data rate: 0=1Mbps,1=2Mbps
  static const int NRF24L01_STATUS_ADDR = 0x07;
  static const int NRF24L01_RX_PW_P0_ADDR = 0x11;
  static const int NRF24L01_FIFO_STATUS_ADDR = 0x17;
  static const int NRF24L01_TX_PAYLOAD_ADDR = 0xA0;
  static const int NRF24L01_RX_PAYLOAD_ADDR = 0x61;

}
