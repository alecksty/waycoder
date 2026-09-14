#ifndef VL53L0X_HPP
#define VL53L0X_HPP

// VL53L0X寄存器定义
// 生成自: STMicroelectronics/Sensor/VL53L0X
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

// 外设定义
// VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
#define VL53L0X_BASE 0x29
#define VL53L0X_DISTANCE (*(volatile uint16_t*)0x00000029)
#define VL53L0X_SIGNAL_RATE (*(volatile uint16_t*)0x0000002B)
#define VL53L0X_AMBIENT_RATE (*(volatile uint16_t*)0x0000002D)
#define VL53L0X_SPAD_COUNT (*(volatile uint16_t*)0x0000002F)
#define VL53L0X_RANGE_STATUS (*(volatile uint8_t*)0x00000031)
#define VL53L0X_TIMING_BUDGET (*(volatile uint32_t*)0x00000032)
#define VL53L0X_INTER_MEAS (*(volatile uint32_t*)0x00000036)
#define VL53L0X_MODE (*(volatile uint8_t*)0x00000037)

// 引脚定义
#define PIN_XSHUT 1  // Shutdown pin (active low)
#define PIN_INT 2  // Interrupt (open-drain)

void vl53l0x_init(void);

#ifdef __cplusplus
}
#endif

#endif // VL53L0X_HPP
