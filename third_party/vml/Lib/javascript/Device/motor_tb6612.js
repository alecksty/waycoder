/**
 * TB6612 寄存器定义
 * 生成自: Toshiba/Motor/TB6612
 * 版本: 1.0
 */
export const tb6612 = {
  // CPU: Motor, 8位, 100000 Hz

  // 外设定义
  // TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
  TB6612_BASE: 0x00,
  TB6612_MOTOR_A: 0x00000000,
  TB6612_MOTOR_A_AIN1: 0,  // Motor A input 1
  TB6612_MOTOR_A_AIN2: 1,  // Motor A input 2
  TB6612_MOTOR_A_PWMA: 2,  // Motor A PWM enable
  TB6612_MOTOR_B: 0x00000001,
  TB6612_MOTOR_B_BIN1: 0,  // Motor B input 1
  TB6612_MOTOR_B_BIN2: 1,  // Motor B input 2
  TB6612_MOTOR_B_PWMB: 2,  // Motor B PWM enable
  TB6612_SPEED_A: 0x00000002,
  TB6612_SPEED_B: 0x00000004,
  TB6612_STBY: 0x00000006,

  init: function() {
    // 硬件初始化
  }
};
