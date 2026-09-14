#ifndef TB6612_HPP
#define TB6612_HPP

// TB6612寄存器定义
// 生成自: Toshiba/Motor/TB6612
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 100000 Hz

// 外设定义
// TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
#define TB6612_BASE 0x00
#define TB6612_MOTOR_A (*(volatile uint8_t*)0x00000000)
#define TB6612_MOTOR_A_AIN1 0  // Motor A input 1
#define TB6612_MOTOR_A_AIN2 1  // Motor A input 2
#define TB6612_MOTOR_A_PWMA 2  // Motor A PWM enable
#define TB6612_MOTOR_B (*(volatile uint8_t*)0x00000001)
#define TB6612_MOTOR_B_BIN1 0  // Motor B input 1
#define TB6612_MOTOR_B_BIN2 1  // Motor B input 2
#define TB6612_MOTOR_B_PWMB 2  // Motor B PWM enable
#define TB6612_SPEED_A (*(volatile uint16_t*)0x00000002)
#define TB6612_SPEED_B (*(volatile uint16_t*)0x00000004)
#define TB6612_STBY (*(volatile uint8_t*)0x00000006)

void tb6612_init(void);

#ifdef __cplusplus
}
#endif

#endif // TB6612_HPP
