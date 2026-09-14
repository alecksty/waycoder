/**
 * BMP280 寄存器定义
 * 生成自: Bosch/Sensor/BMP280
 * 版本: 1.0
 */
export const bmp280 = {
  // CPU: Sensor, 8位, 3400000 Hz

  // 内存段
  // LGA-8 (2.0x2.5x0.95mm)
  PACKAGE_START: 0x00,
  PACKAGE_END: 0x00,
  PACKAGE_SIZE: 8,

  // 外设定义
  // BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
  BMP280_BASE: 0x76,
  BMP280_TEMP_XLSB: 0x00000172,
  BMP280_TEMP_LSB: 0x00000171,
  BMP280_TEMP_MSB: 0x00000170,
  BMP280_PRESS_XLSB: 0x0000016F,
  BMP280_PRESS_LSB: 0x0000016E,
  BMP280_PRESS_MSB: 0x0000016D,
  BMP280_CONFIG: 0x0000016B,
  BMP280_CONFIG_T_SB: 5,  // Standby time in normal mode
  BMP280_CONFIG_FILTER: 2,  // Filter coefficient
  BMP280_CONFIG_SPI3W_EN: 0,  // Enable 3-wire SPI
  BMP280_CTRL_MEAS: 0x0000016A,
  BMP280_CTRL_MEAS_MODE: 0,  // 0=sleep, 1/2=forced, 3=normal
  BMP280_CTRL_MEAS_OSRS_P: 2,  // Pressure oversampling
  BMP280_CTRL_MEAS_OSRS_T: 5,  // Temperature oversampling
  BMP280_STATUS: 0x00000169,
  BMP280_STATUS_IM_UPDATE: 0,  // 1=Image register update in progress
  BMP280_STATUS_MEASURING: 3,  // 1=Conversion is running
  BMP280_CHIP_ID: 0x00000146,
  BMP280_RESET: 0x00000156,

  init: function() {
    // 硬件初始化
  }
};
