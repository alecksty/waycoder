! MLX90614 设备定义 - Fortran 模块
! 生成自: Melexis/Sensor/MLX90614
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
! CPU架构: Sensor
! 位宽: 17位
! 时钟频率: 100000 Hz

module mlx90614_device
  implicit none

  ! 内存段定义
  integer, parameter :: EEPROM_START = 0x00
  integer, parameter :: EEPROM_END = 0x1F
  integer, parameter :: EEPROM_SIZE = 32  ! Internal EEPROM (calibration data)

  ! 外设定义
  ! MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
  integer, parameter :: MLX90614_BASE = 0x5A
  integer, parameter :: MLX90614_T_AMBIENT_ADDR = 0x06
  integer, parameter :: MLX90614_T_OBJECT1_ADDR = 0x07
  integer, parameter :: MLX90614_T_OBJECT2_ADDR = 0x08
  integer, parameter :: MLX90614_RAW_IR1_ADDR = 0x04
  integer, parameter :: MLX90614_RAW_IR2_ADDR = 0x05
  integer, parameter :: MLX90614_EMISSIVITY_ADDR = 0x04

end module mlx90614_device
