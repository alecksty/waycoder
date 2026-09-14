#ifndef DS1307_HPP
#define DS1307_HPP

// DS1307寄存器定义
// 生成自: Maxim/Dallas/RTC/DS1307
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 100000 Hz

// 内存段定义
// Non-volatile RAM (56 bytes)
#define NVRAM_START 0x08
#define NVRAM_END 0x3F
#define NVRAM_SIZE 56

// 外设定义
// DS1307 RTC (0x68, 5V, DIP-8)
#define DS1307_BASE 0x68
#define DS1307_SEC (*(volatile uint8_t*)0x00000068)
#define DS1307_MIN (*(volatile uint8_t*)0x00000069)
#define DS1307_HOUR (*(volatile uint8_t*)0x0000006A)
#define DS1307_DAY (*(volatile uint8_t*)0x0000006B)
#define DS1307_DATE (*(volatile uint8_t*)0x0000006C)
#define DS1307_MONTH (*(volatile uint8_t*)0x0000006D)
#define DS1307_YEAR (*(volatile uint8_t*)0x0000006E)
#define DS1307_CTRL (*(volatile uint8_t*)0x0000006F)

void ds1307_init(void);

#ifdef __cplusplus
}
#endif

#endif // DS1307_HPP
