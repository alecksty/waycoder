! VL53L0X 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/Sensor/VL53L0X
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
! CPU架构: Sensor
! 位宽: 16位
! 时钟频率: 400000 Hz

module vl53l0x_device
  implicit none

  ! 外设定义
  ! VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
  integer, parameter :: VL53L0X_BASE = 0x29
  integer, parameter :: VL53L0X_DISTANCE_ADDR = 0x00
  integer, parameter :: VL53L0X_SIGNAL_RATE_ADDR = 0x02
  integer, parameter :: VL53L0X_AMBIENT_RATE_ADDR = 0x04
  integer, parameter :: VL53L0X_SPAD_COUNT_ADDR = 0x06
  integer, parameter :: VL53L0X_RANGE_STATUS_ADDR = 0x08
  integer, parameter :: VL53L0X_TIMING_BUDGET_ADDR = 0x09
  integer, parameter :: VL53L0X_INTER_MEAS_ADDR = 0x0D
  integer, parameter :: VL53L0X_MODE_ADDR = 0x0E

  ! 引脚定义
  integer, parameter :: PIN_XSHUT = 1  ! Shutdown pin (active low)
  integer, parameter :: PIN_INT = 2  ! Interrupt (open-drain)

end module vl53l0x_device
