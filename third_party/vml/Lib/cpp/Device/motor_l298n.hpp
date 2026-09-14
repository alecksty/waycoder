#ifndef L298N_HPP
#define L298N_HPP

// L298N寄存器定义
// 生成自: STMicroelectronics/Motor/L298N
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

// 外设定义
// L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
#define L298N_BASE 0x00
#define L298N_MOTOR_A (*(volatile uint8_t*)0x00000000)
#define L298N_MOTOR_A_IN1 0  // Motor A Input 1
#define L298N_MOTOR_A_IN2 1  // Motor A Input 2
#define L298N_MOTOR_A_ENA 2  // Motor A Enable/PWM
#define L298N_MOTOR_B (*(volatile uint8_t*)0x00000001)
#define L298N_MOTOR_B_IN3 0  // Motor B Input 3
#define L298N_MOTOR_B_IN4 1  // Motor B Input 4
#define L298N_MOTOR_B_ENB 2  // Motor B Enable/PWM
#define L298N_SPEED_A (*(volatile uint8_t*)0x00000002)
#define L298N_SPEED_B (*(volatile uint8_t*)0x00000003)
#define L298N_STATUS (*(volatile uint8_t*)0x00000004)

void l298n_init(void);

#ifdef __cplusplus
}
#endif

#endif // L298N_HPP
