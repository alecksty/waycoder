#ifndef NRF24L01_HPP
#define NRF24L01_HPP

// NRF24L01寄存器定义
// 生成自: Nordic/RF/NRF24L01
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RF
// 位宽: 8位
// 时钟频率: 10000000 Hz

// 外设定义
// nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
#define NRF24L01_BASE 0x00
#define NRF24L01_CONFIG (*(volatile uint8_t*)0x00000000)
#define NRF24L01_CONFIG_PWR_UP 1  // Power up (1=on)
#define NRF24L01_CONFIG_PRIM_RX 0  // Primary RX mode (1=RX, 0=TX)
#define NRF24L01_EN_AA (*(volatile uint8_t*)0x00000001)
#define NRF24L01_EN_RXADDR (*(volatile uint8_t*)0x00000002)
#define NRF24L01_SETUP_AW (*(volatile uint8_t*)0x00000003)
#define NRF24L01_SETUP_RETR (*(volatile uint8_t*)0x00000004)
#define NRF24L01_RF_CH (*(volatile uint8_t*)0x00000005)
#define NRF24L01_RF_SETUP (*(volatile uint8_t*)0x00000006)
#define NRF24L01_RF_SETUP_RF_PWR 1  // TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
#define NRF24L01_RF_SETUP_RF_DR 3  // Data rate: 0=1Mbps,1=2Mbps
#define NRF24L01_STATUS (*(volatile uint8_t*)0x00000007)
#define NRF24L01_RX_PW_P0 (*(volatile uint8_t*)0x00000011)
#define NRF24L01_FIFO_STATUS (*(volatile uint8_t*)0x00000017)
#define NRF24L01_TX_PAYLOAD (*(volatile uint256_t*)0x000000A0)
#define NRF24L01_RX_PAYLOAD (*(volatile uint256_t*)0x00000061)

void nrf24l01_init(void);

#ifdef __cplusplus
}
#endif

#endif // NRF24L01_HPP
