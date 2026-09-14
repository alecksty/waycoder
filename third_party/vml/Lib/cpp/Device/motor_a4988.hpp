#ifndef A4988_HPP
#define A4988_HPP

// A4988寄存器定义
// 生成自: Allegro/Motor/A4988
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

// 外设定义
// A4988 Stepper Motor Driver (3.3V/5V logic)
#define A4988_BASE 0x00
#define A4988_CTRL (*(volatile uint8_t*)0x00000000)
#define A4988_CTRL_STEP 0  // Step pulse (rising edge)
#define A4988_CTRL_DIR 1  // Direction (0=CW, 1=CCW)
#define A4988_CTRL_ENABLE 2  // Enable (active low)
#define A4988_CTRL_SLEEP 3  // Sleep mode (active low)
#define A4988_CTRL_RESET 4  // Reset (active low)
#define A4988_MICROSTEP (*(volatile uint8_t*)0x00000001)
#define A4988_MICROSTEP_MS1 0  // Microstep select 1
#define A4988_MICROSTEP_MS2 1  // Microstep select 2
#define A4988_MICROSTEP_MS3 2  // Microstep select 3
#define A4988_STEPS (*(volatile uint32_t*)0x00000002)
#define A4988_DELAY_US (*(volatile uint16_t*)0x00000006)

void a4988_init(void);

#ifdef __cplusplus
}
#endif

#endif // A4988_HPP
