#ifndef _24C64_HPP
#define _24C64_HPP

// 24C64寄存器定义
// 生成自: Generic/Memory/24C64
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

// 内存段定义
// EEPROM main memory array (8KB, 32-byte page write)
#define EEPROM_START 0x00
#define EEPROM_END 0x1FFF
#define EEPROM_SIZE 8192

// 外设定义
// 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
#define _24C64_BASE 0x50
#define _24C64_ADDR_H (*(volatile uint8_t*)0x00000050)
#define _24C64_ADDR_L (*(volatile uint8_t*)0x00000051)
#define _24C64_DATA (*(volatile uint8_t*)0x00000052)
#define _24C64_PAGE_SIZE (*(volatile uint8_t*)0x0000014E)
#define _24C64_SIZE (*(volatile uint16_t*)0x0000014D)

void _24c64_init(void);

#ifdef __cplusplus
}
#endif

#endif // _24C64_HPP
