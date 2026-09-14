#ifndef DHT11_HPP
#define DHT11_HPP

// DHT11寄存器定义
// 生成自: Aosong/Sensor/DHT11
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 500000 Hz

// 内存段定义
// DIP-4/SMD-4
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 4

// 外设定义
// DHT11 1-Wire Sensor (3.0V-5.5V)
#define DHT11_BASE 0x00
#define DHT11_HUMIDITY_INT (*(volatile uint8_t*)0x00000000)
#define DHT11_HUMIDITY_DEC (*(volatile uint8_t*)0x00000001)
#define DHT11_TEMP_INT (*(volatile uint8_t*)0x00000002)
#define DHT11_TEMP_DEC (*(volatile uint8_t*)0x00000003)
#define DHT11_CHECKSUM (*(volatile uint8_t*)0x00000004)

void dht11_init(void);

#ifdef __cplusplus
}
#endif

#endif // DHT11_HPP
