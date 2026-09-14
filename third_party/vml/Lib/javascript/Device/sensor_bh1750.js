/**
 * BH1750 寄存器定义
 * 生成自: ROHM/Sensor/BH1750
 * 版本: 1.0
 */
export const bh1750 = {
  // CPU: Sensor, 16位, 400000 Hz

  // 外设定义
  // BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
  BH1750_BASE: 0x23,
  BH1750_LUX: 0x00000023,
  BH1750_MODE: 0x00000024,
  BH1750_MODE_CONT_H: 0,  // Continuous High Res (1lx, 120ms)
  BH1750_MODE_CONT_H2: 1,  // Continuous High Res 2 (0.5lx, 120ms)
  BH1750_MODE_CONT_L: 2,  // Continuous Low Res (4lx, 16ms)
  BH1750_MODE_ONCE_H: 3,  // One-time High Res (1lx, 120ms)
  BH1750_MODE_ONCE_H2: 4,  // One-time High Res 2 (0.5lx, 120ms)
  BH1750_MODE_ONCE_L: 5,  // One-time Low Res (4lx, 16ms)
  BH1750_CMD_POWER_ON: 0x00000024,
  BH1750_CMD_POWER_OFF: 0x00000023,
  BH1750_CMD_RESET: 0x0000002A,

  init: function() {
    // 硬件初始化
  }
};
