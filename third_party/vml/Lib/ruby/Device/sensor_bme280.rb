# BME280 设备定义 - Ruby 模块
# 生成自: Bosch/Sensor/BME280
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
# CPU架构: Sensor
# 位宽: 8位
# 时钟频率: 400000 Hz

module BME280

  # 外设定义
  # BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
  BME280_BASE = 0x76
  BME280_CHIP_ID_ADDR = 0xD0
  BME280_RESET_ADDR = 0xE0
  BME280_CTRL_HUM_ADDR = 0xF2
  BME280_STATUS_ADDR = 0xF3
  BME280_CTRL_MEAS_ADDR = 0xF4
  BME280_CONFIG_ADDR = 0xF5
  BME280_PRESS_ADDR = 0xF7
  BME280_TEMP_ADDR = 0xFA
  BME280_HUM_ADDR = 0xFD

end
