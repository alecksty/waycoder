/**
 * A4988 寄存器定义
 * 生成自: Allegro/Motor/A4988
 * 版本: 1.0
 */
export const a4988 = {
  // CPU: Motor, 8位, 0 Hz

  // 外设定义
  // A4988 Stepper Motor Driver (3.3V/5V logic)
  A4988_BASE: 0x00,
  A4988_CTRL: 0x00000000,
  A4988_CTRL_STEP: 0,  // Step pulse (rising edge)
  A4988_CTRL_DIR: 1,  // Direction (0=CW, 1=CCW)
  A4988_CTRL_ENABLE: 2,  // Enable (active low)
  A4988_CTRL_SLEEP: 3,  // Sleep mode (active low)
  A4988_CTRL_RESET: 4,  // Reset (active low)
  A4988_MICROSTEP: 0x00000001,
  A4988_MICROSTEP_MS1: 0,  // Microstep select 1
  A4988_MICROSTEP_MS2: 1,  // Microstep select 2
  A4988_MICROSTEP_MS3: 2,  // Microstep select 3
  A4988_STEPS: 0x00000002,
  A4988_DELAY_US: 0x00000006,

  init: function() {
    // 硬件初始化
  }
};
