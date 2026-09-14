// BMP280 设备定义 - Dart 库
// 生成自: Bosch/Sensor/BMP280
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 3400000 Hz

class BMP280Device {
  static const String deviceName = "BMP280";
  static const String manufacturer = "Bosch";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 8;
  static const int clockFrequency = 3400000;

  // 内存段定义
  static const int PACKAGE_START = 0x00;
  static const int PACKAGE_END = 0x00;
  static const int PACKAGE_SIZE = 8;  // LGA-8 (2.0x2.5x0.95mm)

  // 外设定义
  // BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
  static const int BMP280_BASE = 0x76;
  static const int BMP280_TEMP_XLSB_ADDR = 0xFC;
  static const int BMP280_TEMP_LSB_ADDR = 0xFB;
  static const int BMP280_TEMP_MSB_ADDR = 0xFA;
  static const int BMP280_PRESS_XLSB_ADDR = 0xF9;
  static const int BMP280_PRESS_LSB_ADDR = 0xF8;
  static const int BMP280_PRESS_MSB_ADDR = 0xF7;
  static const int BMP280_CONFIG_ADDR = 0xF5;
  static const int BMP280_CONFIG_T_SB_BIT = 5;  // Standby time in normal mode
  static const int BMP280_CONFIG_FILTER_BIT = 2;  // Filter coefficient
  static const int BMP280_CONFIG_SPI3W_EN_BIT = 0;  // Enable 3-wire SPI
  static const int BMP280_CTRL_MEAS_ADDR = 0xF4;
  static const int BMP280_CTRL_MEAS_MODE_BIT = 0;  // 0=sleep, 1/2=forced, 3=normal
  static const int BMP280_CTRL_MEAS_OSRS_P_BIT = 2;  // Pressure oversampling
  static const int BMP280_CTRL_MEAS_OSRS_T_BIT = 5;  // Temperature oversampling
  static const int BMP280_STATUS_ADDR = 0xF3;
  static const int BMP280_STATUS_IM_UPDATE_BIT = 0;  // 1=Image register update in progress
  static const int BMP280_STATUS_MEASURING_BIT = 3;  // 1=Conversion is running
  static const int BMP280_CHIP_ID_ADDR = 0xD0;
  static const int BMP280_RESET_ADDR = 0xE0;

}
