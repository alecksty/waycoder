# BMP280 设备定义 - Ruby 模块
# 生成自: Bosch/Sensor/BMP280
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
# CPU架构: Sensor
# 位宽: 8位
# 时钟频率: 3400000 Hz

module BMP280

  # 内存段定义
  PACKAGE_START = 0x00
  PACKAGE_END = 0x00
  PACKAGE_SIZE = 8  # LGA-8 (2.0x2.5x0.95mm)

  # 外设定义
  # BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
  BMP280_BASE = 0x76
  BMP280_TEMP_XLSB_ADDR = 0xFC
  BMP280_TEMP_LSB_ADDR = 0xFB
  BMP280_TEMP_MSB_ADDR = 0xFA
  BMP280_PRESS_XLSB_ADDR = 0xF9
  BMP280_PRESS_LSB_ADDR = 0xF8
  BMP280_PRESS_MSB_ADDR = 0xF7
  BMP280_CONFIG_ADDR = 0xF5
  BMP280_CONFIG_T_SB_BIT = 5  # Standby time in normal mode
  BMP280_CONFIG_FILTER_BIT = 2  # Filter coefficient
  BMP280_CONFIG_SPI3W_EN_BIT = 0  # Enable 3-wire SPI
  BMP280_CTRL_MEAS_ADDR = 0xF4
  BMP280_CTRL_MEAS_MODE_BIT = 0  # 0=sleep, 1/2=forced, 3=normal
  BMP280_CTRL_MEAS_OSRS_P_BIT = 2  # Pressure oversampling
  BMP280_CTRL_MEAS_OSRS_T_BIT = 5  # Temperature oversampling
  BMP280_STATUS_ADDR = 0xF3
  BMP280_STATUS_IM_UPDATE_BIT = 0  # 1=Image register update in progress
  BMP280_STATUS_MEASURING_BIT = 3  # 1=Conversion is running
  BMP280_CHIP_ID_ADDR = 0xD0
  BMP280_RESET_ADDR = 0xE0

end
