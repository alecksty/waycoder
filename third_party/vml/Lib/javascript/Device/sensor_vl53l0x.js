/**
 * VL53L0X 寄存器定义
 * 生成自: STMicroelectronics/Sensor/VL53L0X
 * 版本: 1.0
 */
export const vl53l0x = {
  // CPU: Sensor, 16位, 400000 Hz

  // 外设定义
  // VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
  VL53L0X_BASE: 0x29,
  VL53L0X_DISTANCE: 0x00000029,
  VL53L0X_SIGNAL_RATE: 0x0000002B,
  VL53L0X_AMBIENT_RATE: 0x0000002D,
  VL53L0X_SPAD_COUNT: 0x0000002F,
  VL53L0X_RANGE_STATUS: 0x00000031,
  VL53L0X_TIMING_BUDGET: 0x00000032,
  VL53L0X_INTER_MEAS: 0x00000036,
  VL53L0X_MODE: 0x00000037,

  // 引脚定义
  PIN_XSHUT: 1,  // Shutdown pin (active low)
  PIN_INT: 2,  // Interrupt (open-drain)

  init: function() {
    // 硬件初始化
  }
};
