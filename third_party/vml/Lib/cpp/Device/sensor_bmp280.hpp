#ifndef BMP280_HPP
#define BMP280_HPP

// BMP280寄存器定义
// 生成自: Bosch/Sensor/BMP280
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 3400000 Hz

// 内存段定义
// LGA-8 (2.0x2.5x0.95mm)
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 8

// 外设定义
// BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
#define BMP280_BASE 0x76
#define BMP280_TEMP_XLSB (*(volatile uint8_t*)0x00000172)
#define BMP280_TEMP_LSB (*(volatile uint8_t*)0x00000171)
#define BMP280_TEMP_MSB (*(volatile uint8_t*)0x00000170)
#define BMP280_PRESS_XLSB (*(volatile uint8_t*)0x0000016F)
#define BMP280_PRESS_LSB (*(volatile uint8_t*)0x0000016E)
#define BMP280_PRESS_MSB (*(volatile uint8_t*)0x0000016D)
#define BMP280_CONFIG (*(volatile uint8_t*)0x0000016B)
#define BMP280_CONFIG_T_SB 5  // Standby time in normal mode
#define BMP280_CONFIG_FILTER 2  // Filter coefficient
#define BMP280_CONFIG_SPI3W_EN 0  // Enable 3-wire SPI
#define BMP280_CTRL_MEAS (*(volatile uint8_t*)0x0000016A)
#define BMP280_CTRL_MEAS_MODE 0  // 0=sleep, 1/2=forced, 3=normal
#define BMP280_CTRL_MEAS_OSRS_P 2  // Pressure oversampling
#define BMP280_CTRL_MEAS_OSRS_T 5  // Temperature oversampling
#define BMP280_STATUS (*(volatile uint8_t*)0x00000169)
#define BMP280_STATUS_IM_UPDATE 0  // 1=Image register update in progress
#define BMP280_STATUS_MEASURING 3  // 1=Conversion is running
#define BMP280_CHIP_ID (*(volatile uint8_t*)0x00000146)
#define BMP280_RESET (*(volatile uint8_t*)0x00000156)

void bmp280_init(void);

#ifdef __cplusplus
}
#endif

#endif // BMP280_HPP
