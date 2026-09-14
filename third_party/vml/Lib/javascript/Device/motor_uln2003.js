/**
 * ULN2003 寄存器定义
 * 生成自: ST/TI/Motor/ULN2003
 * 版本: 1.0
 */
export const uln2003 = {
  // CPU: Motor, 8位, 0 Hz

  // 外设定义
  // ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
  ULN2003_BASE: 0x00,
  ULN2003_STEPPER: 0x00000000,
  ULN2003_STEP_MODE: 0x00000001,
  ULN2003_STEPS: 0x00000002,
  ULN2003_DELAY_MS: 0x00000004,
  ULN2003_POSITION: 0x00000005,
  ULN2003_DIRECTION: 0x00000007,

  init: function() {
    // 硬件初始化
  }
};
