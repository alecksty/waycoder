// ST7735 设备定义 - Objective-C 头文件
// 生成自: Sitronix/Display/ST7735
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: ST7735 1.8" 128x160 TFT LCD Display (SPI, 16-bit color)
// CPU架构: Display
// 位宽: 16位
// 时钟频率: 16000000 Hz

#ifndef ST7735_DEVICE_H
#define ST7735_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define GRAM_START 0x00
#define GRAM_END 0x4FFF
#define GRAM_SIZE 20480  // Graphics RAM (128x160x16bit)

// 外设定义
// ST7735 128x160 TFT (SPI, 3.3V-5V)
#define ST7735_BASE 0x00
#define ST7735_CMD_ADDR 0x00
#define ST7735_DATA_ADDR 0x01
#define ST7735_COL_START_ADDR 0x2A
#define ST7735_ROW_START_ADDR 0x2B
#define ST7735_WRITE_RAM_ADDR 0x2C
#define ST7735_MADCTL_ADDR 0x36
#define ST7735_COLMOD_ADDR 0x3A
#define ST7735_INVON_ADDR 0x21
#define ST7735_SLEEP_OUT_ADDR 0x11
#define ST7735_DISP_ON_ADDR 0x29

#endif /* ST7735_DEVICE_H */
