// NRF24L01 设备定义 - Objective-C 头文件
// 生成自: Nordic/RF/NRF24L01
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
// CPU架构: RF
// 位宽: 8位
// 时钟频率: 10000000 Hz

#ifndef NRF24L01_DEVICE_H
#define NRF24L01_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
#define NRF24L01_BASE 0x00
#define NRF24L01_CONFIG_ADDR 0x00
#define NRF24L01_CONFIG_PWR_UP_BIT 1  // Power up (1=on)
#define NRF24L01_CONFIG_PRIM_RX_BIT 0  // Primary RX mode (1=RX, 0=TX)
#define NRF24L01_EN_AA_ADDR 0x01
#define NRF24L01_EN_RXADDR_ADDR 0x02
#define NRF24L01_SETUP_AW_ADDR 0x03
#define NRF24L01_SETUP_RETR_ADDR 0x04
#define NRF24L01_RF_CH_ADDR 0x05
#define NRF24L01_RF_SETUP_ADDR 0x06
#define NRF24L01_RF_SETUP_RF_PWR_BIT 1  // TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
#define NRF24L01_RF_SETUP_RF_DR_BIT 3  // Data rate: 0=1Mbps,1=2Mbps
#define NRF24L01_STATUS_ADDR 0x07
#define NRF24L01_RX_PW_P0_ADDR 0x11
#define NRF24L01_FIFO_STATUS_ADDR 0x17
#define NRF24L01_TX_PAYLOAD_ADDR 0xA0
#define NRF24L01_RX_PAYLOAD_ADDR 0x61

#endif /* NRF24L01_DEVICE_H */
