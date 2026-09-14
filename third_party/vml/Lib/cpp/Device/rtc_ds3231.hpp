#ifndef DS3231_HPP
#define DS3231_HPP

// DS3231寄存器定义
// 生成自: Maxim/Dallas/RTC/DS3231
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 400000 Hz

// 内存段定义
// AT24C32 EEPROM (32Kbit)
#define EEPROM_START 0x14
#define EEPROM_END 0xFF
#define EEPROM_SIZE 236

// 外设定义
// DS3231 Precision RTC (0x68, 3.3V-5.5V)
#define DS3231_BASE 0x68
#define DS3231_SEC (*(volatile uint8_t*)0x00000068)
#define DS3231_MIN (*(volatile uint8_t*)0x00000069)
#define DS3231_HOUR (*(volatile uint8_t*)0x0000006A)
#define DS3231_DAY (*(volatile uint8_t*)0x0000006B)
#define DS3231_DATE (*(volatile uint8_t*)0x0000006C)
#define DS3231_MONTH_CENT (*(volatile uint8_t*)0x0000006D)
#define DS3231_YEAR (*(volatile uint8_t*)0x0000006E)
#define DS3231_ALARM1_SEC (*(volatile uint8_t*)0x0000006F)
#define DS3231_ALARM1_MIN (*(volatile uint8_t*)0x00000070)
#define DS3231_ALARM1_HOUR (*(volatile uint8_t*)0x00000071)
#define DS3231_ALARM2_MIN (*(volatile uint8_t*)0x00000073)
#define DS3231_ALARM2_HOUR (*(volatile uint8_t*)0x00000074)
#define DS3231_CTRL (*(volatile uint8_t*)0x00000076)
#define DS3231_CTRL_STATUS (*(volatile uint8_t*)0x00000077)
#define DS3231_TEMP_MSB (*(volatile uint8_t*)0x00000079)
#define DS3231_TEMP_LSB (*(volatile uint8_t*)0x0000007A)

void ds3231_init(void);

#ifdef __cplusplus
}
#endif

#endif // DS3231_HPP
