! BME280 设备定义 - Fortran 模块
! 生成自: Bosch/Sensor/BME280
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
! CPU架构: Sensor
! 位宽: 8位
! 时钟频率: 400000 Hz

module bme280_device
  implicit none

  ! 外设定义
  ! BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
  integer, parameter :: BME280_BASE = 0x76
  integer, parameter :: BME280_CHIP_ID_ADDR = 0xD0
  integer, parameter :: BME280_RESET_ADDR = 0xE0
  integer, parameter :: BME280_CTRL_HUM_ADDR = 0xF2
  integer, parameter :: BME280_STATUS_ADDR = 0xF3
  integer, parameter :: BME280_CTRL_MEAS_ADDR = 0xF4
  integer, parameter :: BME280_CONFIG_ADDR = 0xF5
  integer, parameter :: BME280_PRESS_ADDR = 0xF7
  integer, parameter :: BME280_TEMP_ADDR = 0xFA
  integer, parameter :: BME280_HUM_ADDR = 0xFD

end module bme280_device
