#ifndef HD44780_HPP
#define HD44780_HPP

// HD44780寄存器定义
// 生成自: Hitachi/Display/HD44780
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Display
// 位宽: 8位
// 时钟频率: 0 Hz

// 内存段定义
// Display Data RAM (80 bytes, 2 lines)
#define DDRAM_START 0x00
#define DDRAM_END 0x4F
#define DDRAM_SIZE 80

// Character Generator RAM (8 custom chars x 8 bytes)
#define CGRAM_START 0x00
#define CGRAM_END 0x3F
#define CGRAM_SIZE 64

// 外设定义
// HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
#define HD44780_BASE 0x27
#define HD44780_CMD (*(volatile uint8_t*)0x00000027)
#define HD44780_DATA (*(volatile uint8_t*)0x00000028)
#define HD44780_CTRL_RS (*(volatile uint8_t*)0x00000027)
#define HD44780_CTRL_RW (*(volatile uint8_t*)0x00000028)
#define HD44780_CTRL_EN (*(volatile uint8_t*)0x00000029)
#define HD44780_CTRL_BL (*(volatile uint8_t*)0x0000002A)
#define HD44780_ADDR_DDRAM (*(volatile uint8_t*)0x000000A7)
#define HD44780_ADDR_CGRAM (*(volatile uint8_t*)0x00000067)

void hd44780_init(void);

#ifdef __cplusplus
}
#endif

#endif // HD44780_HPP
