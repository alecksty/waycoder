// ILI9341 设备定义 - Objective-C 头文件
// 生成自: Ilitek/Display/ILI9341
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
// CPU架构: Display
// 位宽: 18位
// 时钟频率: 20000000 Hz

#ifndef ILI9341_DEVICE_H
#define ILI9341_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define GRAM_START 0x00
#define GRAM_END 0xBCFF
#define GRAM_SIZE 156672  // Graphics RAM (240x320x18bit)

// 外设定义
// ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
#define ILI9341_BASE 0x00
#define ILI9341_CMD_ADDR 0x00
#define ILI9341_DATA_ADDR 0x01
#define ILI9341_COL_START_ADDR 0x2A
#define ILI9341_PAGE_START_ADDR 0x2B
#define ILI9341_WRITE_RAM_ADDR 0x2C
#define ILI9341_MADCTL_ADDR 0x36
#define ILI9341_PIXFMT_ADDR 0x3A
#define ILI9341_FRMCTL_ADDR 0xB1
#define ILI9341_GAMMA_ADDR 0x26
#define ILI9341_SLEEP_OUT_ADDR 0x11
#define ILI9341_DISP_ON_ADDR 0x29

#endif /* ILI9341_DEVICE_H */
