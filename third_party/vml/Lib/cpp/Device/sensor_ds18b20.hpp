#ifndef DS18B20_HPP
#define DS18B20_HPP

// DS18B20寄存器定义
// 生成自: Maxim/Dallas/Sensor/DS18B20
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 100000 Hz

// 内存段定义
// Scratchpad memory (9 bytes)
#define SCRATCHPAD_START 0x00
#define SCRATCHPAD_END 0x08
#define SCRATCHPAD_SIZE 9

// EEPROM (TH, TL, config bytes)
#define EEPROM_START 0x00
#define EEPROM_END 0x02
#define EEPROM_SIZE 3

// 外设定义
// DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
#define DS18B20_BASE 0x00
#define DS18B20_TEMP_LSB (*(volatile uint8_t*)0x00000000)
#define DS18B20_TEMP_MSB (*(volatile uint8_t*)0x00000001)
#define DS18B20_TH_REG (*(volatile uint8_t*)0x00000002)
#define DS18B20_TL_REG (*(volatile uint8_t*)0x00000003)
#define DS18B20_CONFIG (*(volatile uint8_t*)0x00000004)
#define DS18B20_CONFIG_R0 5  // Resolution select bit 0
#define DS18B20_CONFIG_R1 6  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
#define DS18B20_COUNT_REMAIN (*(volatile uint8_t*)0x00000006)
#define DS18B20_COUNT_PER_C (*(volatile uint8_t*)0x00000007)
#define DS18B20_CRC (*(volatile uint8_t*)0x00000008)

void ds18b20_init(void);

#ifdef __cplusplus
}
#endif

#endif // DS18B20_HPP
