#ifndef ILI9341_HPP
#define ILI9341_HPP

// ILI9341寄存器定义
// 生成自: Ilitek/Display/ILI9341
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Display
// 位宽: 18位
// 时钟频率: 20000000 Hz

// 内存段定义
// Graphics RAM (240x320x18bit)
#define GRAM_START 0x00
#define GRAM_END 0xBCFF
#define GRAM_SIZE 156672

// 外设定义
// ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
#define ILI9341_BASE 0x00
#define ILI9341_CMD (*(volatile uint8_t*)0x00000000)
#define ILI9341_DATA (*(volatile uint8_t*)0x00000001)
#define ILI9341_COL_START (*(volatile uint16_t*)0x0000002A)
#define ILI9341_PAGE_START (*(volatile uint16_t*)0x0000002B)
#define ILI9341_WRITE_RAM (*(volatile uint16_t*)0x0000002C)
#define ILI9341_MADCTL (*(volatile uint8_t*)0x00000036)
#define ILI9341_PIXFMT (*(volatile uint8_t*)0x0000003A)
#define ILI9341_FRMCTL (*(volatile uint16_t*)0x000000B1)
#define ILI9341_GAMMA (*(volatile uint8_t*)0x00000026)
#define ILI9341_SLEEP_OUT (*(volatile uint0_t*)0x00000011)
#define ILI9341_DISP_ON (*(volatile uint0_t*)0x00000029)

void ili9341_init(void);

#ifdef __cplusplus
}
#endif

#endif // ILI9341_HPP
