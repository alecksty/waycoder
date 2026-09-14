! DS1307 设备定义 - Fortran 模块
! 生成自: Maxim/Dallas/RTC/DS1307
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: DS1307 I2C Real-Time Clock (56-byte NVRAM, battery backup)
! CPU架构: RTC
! 位宽: 8位
! 时钟频率: 100000 Hz

module ds1307_device
  implicit none

  ! 内存段定义
  integer, parameter :: NVRAM_START = 0x08
  integer, parameter :: NVRAM_END = 0x3F
  integer, parameter :: NVRAM_SIZE = 56  ! Non-volatile RAM (56 bytes)

  ! 外设定义
  ! DS1307 RTC (0x68, 5V, DIP-8)
  integer, parameter :: DS1307_BASE = 0x68
  integer, parameter :: DS1307_SEC_ADDR = 0x00
  integer, parameter :: DS1307_MIN_ADDR = 0x01
  integer, parameter :: DS1307_HOUR_ADDR = 0x02
  integer, parameter :: DS1307_DAY_ADDR = 0x03
  integer, parameter :: DS1307_DATE_ADDR = 0x04
  integer, parameter :: DS1307_MONTH_ADDR = 0x05
  integer, parameter :: DS1307_YEAR_ADDR = 0x06
  integer, parameter :: DS1307_CTRL_ADDR = 0x07

end module ds1307_device
