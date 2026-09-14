#ifndef BH1750_HPP
#define BH1750_HPP

// BH1750寄存器定义
// 生成自: ROHM/Sensor/BH1750
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

// 外设定义
// BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
#define BH1750_BASE 0x23
#define BH1750_LUX (*(volatile uint16_t*)0x00000023)
#define BH1750_MODE (*(volatile uint8_t*)0x00000024)
#define BH1750_MODE_CONT_H 0  // Continuous High Res (1lx, 120ms)
#define BH1750_MODE_CONT_H2 1  // Continuous High Res 2 (0.5lx, 120ms)
#define BH1750_MODE_CONT_L 2  // Continuous Low Res (4lx, 16ms)
#define BH1750_MODE_ONCE_H 3  // One-time High Res (1lx, 120ms)
#define BH1750_MODE_ONCE_H2 4  // One-time High Res 2 (0.5lx, 120ms)
#define BH1750_MODE_ONCE_L 5  // One-time Low Res (4lx, 16ms)
#define BH1750_CMD_POWER_ON (*(volatile uint8_t*)0x00000024)
#define BH1750_CMD_POWER_OFF (*(volatile uint8_t*)0x00000023)
#define BH1750_CMD_RESET (*(volatile uint8_t*)0x0000002A)

void bh1750_init(void);

#ifdef __cplusplus
}
#endif

#endif // BH1750_HPP
