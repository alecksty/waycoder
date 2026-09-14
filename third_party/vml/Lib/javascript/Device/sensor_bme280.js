/**
 * BME280 寄存器定义
 * 生成自: Bosch/Sensor/BME280
 * 版本: 1.0
 */
export const bme280 = {
  // CPU: Sensor, 8位, 400000 Hz

  // 外设定义
  // BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
  BME280_BASE: 0x76,
  BME280_CHIP_ID: 0x00000146,
  BME280_RESET: 0x00000156,
  BME280_CTRL_HUM: 0x00000168,
  BME280_STATUS: 0x00000169,
  BME280_CTRL_MEAS: 0x0000016A,
  BME280_CONFIG: 0x0000016B,
  BME280_PRESS: 0x0000016D,
  BME280_TEMP: 0x00000170,
  BME280_HUM: 0x00000173,

  init: function() {
    // 硬件初始化
  }
};
