#ifndef BME280_HPP
#define BME280_HPP

// BME280寄存器定义
// 生成自: Bosch/Sensor/BME280
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

// 外设定义
// BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
#define BME280_BASE 0x76
#define BME280_CHIP_ID (*(volatile uint8_t*)0x00000146)
#define BME280_RESET (*(volatile uint8_t*)0x00000156)
#define BME280_CTRL_HUM (*(volatile uint8_t*)0x00000168)
#define BME280_STATUS (*(volatile uint8_t*)0x00000169)
#define BME280_CTRL_MEAS (*(volatile uint8_t*)0x0000016A)
#define BME280_CONFIG (*(volatile uint8_t*)0x0000016B)
#define BME280_PRESS (*(volatile uint32_t*)0x0000016D)
#define BME280_TEMP (*(volatile uint32_t*)0x00000170)
#define BME280_HUM (*(volatile uint16_t*)0x00000173)

void bme280_init(void);

#ifdef __cplusplus
}
#endif

#endif // BME280_HPP
