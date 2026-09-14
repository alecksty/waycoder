/**
 * L298N 寄存器定义
 * 生成自: STMicroelectronics/Motor/L298N
 * 版本: 1.0
 */
export const l298n = {
  // CPU: Motor, 8位, 0 Hz

  // 外设定义
  // L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
  L298N_BASE: 0x00,
  L298N_MOTOR_A: 0x00000000,
  L298N_MOTOR_A_IN1: 0,  // Motor A Input 1
  L298N_MOTOR_A_IN2: 1,  // Motor A Input 2
  L298N_MOTOR_A_ENA: 2,  // Motor A Enable/PWM
  L298N_MOTOR_B: 0x00000001,
  L298N_MOTOR_B_IN3: 0,  // Motor B Input 3
  L298N_MOTOR_B_IN4: 1,  // Motor B Input 4
  L298N_MOTOR_B_ENB: 2,  // Motor B Enable/PWM
  L298N_SPEED_A: 0x00000002,
  L298N_SPEED_B: 0x00000003,
  L298N_STATUS: 0x00000004,

  init: function() {
    // 硬件初始化
  }
};
