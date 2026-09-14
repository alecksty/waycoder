// MAX7219 设备定义 - Objective-C 头文件
// 生成自: Maxim/LED/MAX7219
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
// CPU架构: LED
// 位宽: 8位
// 时钟频率: 10000000 Hz

#ifndef MAX7219_DEVICE_H
#define MAX7219_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
#define MAX7219_BASE 0x00
#define MAX7219_DIGIT0_ADDR 0x01
#define MAX7219_DIGIT1_ADDR 0x02
#define MAX7219_DIGIT2_ADDR 0x03
#define MAX7219_DIGIT3_ADDR 0x04
#define MAX7219_DIGIT4_ADDR 0x05
#define MAX7219_DIGIT5_ADDR 0x06
#define MAX7219_DIGIT6_ADDR 0x07
#define MAX7219_DIGIT7_ADDR 0x08
#define MAX7219_DECODE_ADDR 0x09
#define MAX7219_INTENSITY_ADDR 0x0A
#define MAX7219_SCAN_LIMIT_ADDR 0x0B
#define MAX7219_SHUTDOWN_ADDR 0x0C
#define MAX7219_TEST_ADDR 0x0F

#endif /* MAX7219_DEVICE_H */
