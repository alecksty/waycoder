/**
 * MLX90614 寄存器定义
 * 生成自: Melexis/Sensor/MLX90614
 * 版本: 1.0
 */
export const mlx90614 = {
  // CPU: Sensor, 17位, 100000 Hz

  // 内存段
  // Internal EEPROM (calibration data)
  EEPROM_START: 0x00,
  EEPROM_END: 0x1F,
  EEPROM_SIZE: 32,

  // 外设定义
  // MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
  MLX90614_BASE: 0x5A,
  MLX90614_T_AMBIENT: 0x00000060,
  MLX90614_T_OBJECT1: 0x00000061,
  MLX90614_T_OBJECT2: 0x00000062,
  MLX90614_RAW_IR1: 0x0000005E,
  MLX90614_RAW_IR2: 0x0000005F,
  MLX90614_EMISSIVITY: 0x0000005E,

  init: function() {
    // 硬件初始化
  }
};
