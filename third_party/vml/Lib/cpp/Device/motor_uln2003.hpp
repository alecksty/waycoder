#ifndef ULN2003_HPP
#define ULN2003_HPP

// ULN2003寄存器定义
// 生成自: ST/TI/Motor/ULN2003
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

// 外设定义
// ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
#define ULN2003_BASE 0x00
#define ULN2003_STEPPER (*(volatile uint8_t*)0x00000000)
#define ULN2003_STEP_MODE (*(volatile uint8_t*)0x00000001)
#define ULN2003_STEPS (*(volatile uint16_t*)0x00000002)
#define ULN2003_DELAY_MS (*(volatile uint8_t*)0x00000004)
#define ULN2003_POSITION (*(volatile uint16_t*)0x00000005)
#define ULN2003_DIRECTION (*(volatile uint8_t*)0x00000007)

void uln2003_init(void);

#ifdef __cplusplus
}
#endif

#endif // ULN2003_HPP
