#ifndef CCS811_HPP
#define CCS811_HPP

// CCS811寄存器定义
// 生成自: AMS/ScioSense/Sensor/CCS811
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

// 外设定义
// CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
#define CCS811_BASE 0x5A
#define CCS811_STATUS (*(volatile uint8_t*)0x0000005A)
#define CCS811_MEAS_MODE (*(volatile uint8_t*)0x0000005B)
#define CCS811_ALG_RESULT (*(volatile uint32_t*)0x0000005C)
#define CCS811_ECO2 (*(volatile uint16_t*)0x0000005C)
#define CCS811_TVOC (*(volatile uint16_t*)0x0000005E)
#define CCS811_RAW_DATA (*(volatile uint16_t*)0x00000060)
#define CCS811_BASELINE (*(volatile uint16_t*)0x00000065)
#define CCS811_HW_ID (*(volatile uint8_t*)0x0000007A)
#define CCS811_ERROR_ID (*(volatile uint8_t*)0x0000013A)
#define CCS811_APP_START (*(volatile uint8_t*)0x0000014E)
#define CCS811_SW_RESET (*(volatile uint32_t*)0x00000159)

// 中断向量定义
#define INT_VECTOR 0  // Data ready / interrupt pin

// 引脚定义
#define PIN_WAKE 1  // Wake pin (active low)

void ccs811_init(void);

#ifdef __cplusplus
}
#endif

#endif // CCS811_HPP
