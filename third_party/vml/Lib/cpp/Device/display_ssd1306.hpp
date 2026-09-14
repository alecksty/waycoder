#ifndef SSD1306_HPP
#define SSD1306_HPP

// SSD1306寄存器定义
// 生成自: Solomon Systech/Display/SSD1306
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Display
// 位宽: 8位
// 时钟频率: 400000 Hz

// 内存段定义
// Graphic Display Data RAM (128x64 = 1024 bytes)
#define GDDRAM_START 0x00
#define GDDRAM_END 0x3FF
#define GDDRAM_SIZE 1024

// 外设定义
// SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
#define SSD1306_BASE 0x3C
#define SSD1306_CMD (*(volatile uint8_t*)0x0000003C)
#define SSD1306_DATA (*(volatile uint8_t*)0x0000007C)
#define SSD1306_DISPLAY_OFF (*(volatile uint8_t*)0x000000EA)
#define SSD1306_DISPLAY_ON (*(volatile uint8_t*)0x000000EB)
#define SSD1306_CONTRAST (*(volatile uint8_t*)0x000000BD)
#define SSD1306_SEG_REMAP (*(volatile uint8_t*)0x000000DD)
#define SSD1306_COM_SCAN (*(volatile uint8_t*)0x00000104)
#define SSD1306_ADDR_MODE (*(volatile uint8_t*)0x0000005C)
#define SSD1306_COL_START (*(volatile uint8_t*)0x0000005D)
#define SSD1306_PAGE_START (*(volatile uint8_t*)0x0000005E)

void ssd1306_init(void);

#ifdef __cplusplus
}
#endif

#endif // SSD1306_HPP
