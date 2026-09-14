#ifndef _24C02_HPP
#define _24C02_HPP

// 24C02寄存器定义
// 生成自: Generic/Memory/24C02
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

// 内存段定义
// EEPROM main memory array (256 bytes, 8-byte page write)
#define EEPROM_START 0x00
#define EEPROM_END 0xFF
#define EEPROM_SIZE 256

// 外设定义
// 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
#define _24C02_BASE 0x50
#define _24C02_STATUS (*(volatile uint8_t*)0x0000014F)
#define _24C02_STATUS_BUSY 0  // 1=Write in progress
#define _24C02_PAGE_SIZE (*(volatile uint8_t*)0x0000014E)
#define _24C02_SIZE (*(volatile uint16_t*)0x0000014D)

void _24c02_init(void);

#ifdef __cplusplus
}
#endif

#endif // _24C02_HPP
