// ULN2003 设备定义 - Objective-C 头文件
// 生成自: ST/TI/Motor/ULN2003
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

#ifndef ULN2003_DEVICE_H
#define ULN2003_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
#define ULN2003_BASE 0x00
#define ULN2003_STEPPER_ADDR 0x00
#define ULN2003_STEP_MODE_ADDR 0x01
#define ULN2003_STEPS_ADDR 0x02
#define ULN2003_DELAY_MS_ADDR 0x04
#define ULN2003_POSITION_ADDR 0x05
#define ULN2003_DIRECTION_ADDR 0x07

#endif /* ULN2003_DEVICE_H */
