/**
 * SG90 寄存器定义
 * 生成自: Tower Pro/Motor/SG90
 * 版本: 1.0
 */
export const sg90 = {
  // CPU: Motor, 8位, 0 Hz

  // 外设定义
  // SG90 Micro Servo (500-2500us pulse, 50Hz)
  SG90_BASE: 0x00,
  SG90_ANGLE: 0x00000000,
  SG90_PULSE_MIN: 0x00000001,
  SG90_PULSE_MAX: 0x00000003,
  SG90_CURRENT_ANGLE: 0x00000005,
  SG90_SPEED: 0x00000006,

  init: function() {
    // 硬件初始化
  }
};
