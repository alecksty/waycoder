! CCS811 设备定义 - Fortran 模块
! 生成自: AMS/ScioSense/Sensor/CCS811
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
! CPU架构: Sensor
! 位宽: 16位
! 时钟频率: 400000 Hz

module ccs811_device
  implicit none

  ! 外设定义
  ! CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
  integer, parameter :: CCS811_BASE = 0x5A
  integer, parameter :: CCS811_STATUS_ADDR = 0x00
  integer, parameter :: CCS811_MEAS_MODE_ADDR = 0x01
  integer, parameter :: CCS811_ALG_RESULT_ADDR = 0x02
  integer, parameter :: CCS811_ECO2_ADDR = 0x02
  integer, parameter :: CCS811_TVOC_ADDR = 0x04
  integer, parameter :: CCS811_RAW_DATA_ADDR = 0x06
  integer, parameter :: CCS811_BASELINE_ADDR = 0x0B
  integer, parameter :: CCS811_HW_ID_ADDR = 0x20
  integer, parameter :: CCS811_ERROR_ID_ADDR = 0xE0
  integer, parameter :: CCS811_APP_START_ADDR = 0xF4
  integer, parameter :: CCS811_SW_RESET_ADDR = 0xFF

  ! 中断向量定义
  integer, parameter :: INT_INT = 0  ! Data ready / interrupt pin

  ! 引脚定义
  integer, parameter :: PIN_WAKE = 1  ! Wake pin (active low)

end module ccs811_device
