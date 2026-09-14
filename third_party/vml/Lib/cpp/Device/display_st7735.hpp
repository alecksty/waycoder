#ifndef ST7735_HPP
#define ST7735_HPP

// ST7735寄存器定义
// 生成自: Sitronix/Display/ST7735
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Display
// 位宽: 16位
// 时钟频率: 16000000 Hz

// 内存段定义
// Graphics RAM (128x160x16bit)
#define GRAM_START 0x00
#define GRAM_END 0x4FFF
#define GRAM_SIZE 20480

// 外设定义
// ST7735 128x160 TFT (SPI, 3.3V-5V)
#define ST7735_BASE 0x00
#define ST7735_CMD (*(volatile uint8_t*)0x00000000)
#define ST7735_DATA (*(volatile uint8_t*)0x00000001)
#define ST7735_COL_START (*(volatile uint16_t*)0x0000002A)
#define ST7735_ROW_START (*(volatile uint16_t*)0x0000002B)
#define ST7735_WRITE_RAM (*(volatile uint16_t*)0x0000002C)
#define ST7735_MADCTL (*(volatile uint8_t*)0x00000036)
#define ST7735_COLMOD (*(volatile uint8_t*)0x0000003A)
#define ST7735_INVON (*(volatile uint0_t*)0x00000021)
#define ST7735_SLEEP_OUT (*(volatile uint0_t*)0x00000011)
#define ST7735_DISP_ON (*(volatile uint0_t*)0x00000029)

void st7735_init(void);

#ifdef __cplusplus
}
#endif

#endif // ST7735_HPP
