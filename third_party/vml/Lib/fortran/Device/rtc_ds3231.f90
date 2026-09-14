! DS3231 设备定义 - Fortran 模块
! 生成自: Maxim/Dallas/RTC/DS3231
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
! CPU架构: RTC
! 位宽: 8位
! 时钟频率: 400000 Hz

module ds3231_device
  implicit none

  ! 内存段定义
  integer, parameter :: EEPROM_START = 0x14
  integer, parameter :: EEPROM_END = 0xFF
  integer, parameter :: EEPROM_SIZE = 236  ! AT24C32 EEPROM (32Kbit)

  ! 外设定义
  ! DS3231 Precision RTC (0x68, 3.3V-5.5V)
  integer, parameter :: DS3231_BASE = 0x68
  integer, parameter :: DS3231_SEC_ADDR = 0x00
  integer, parameter :: DS3231_MIN_ADDR = 0x01
  integer, parameter :: DS3231_HOUR_ADDR = 0x02
  integer, parameter :: DS3231_DAY_ADDR = 0x03
  integer, parameter :: DS3231_DATE_ADDR = 0x04
  integer, parameter :: DS3231_MONTH_CENT_ADDR = 0x05
  integer, parameter :: DS3231_YEAR_ADDR = 0x06
  integer, parameter :: DS3231_ALARM1_SEC_ADDR = 0x07
  integer, parameter :: DS3231_ALARM1_MIN_ADDR = 0x08
  integer, parameter :: DS3231_ALARM1_HOUR_ADDR = 0x09
  integer, parameter :: DS3231_ALARM2_MIN_ADDR = 0x0B
  integer, parameter :: DS3231_ALARM2_HOUR_ADDR = 0x0C
  integer, parameter :: DS3231_CTRL_ADDR = 0x0E
  integer, parameter :: DS3231_CTRL_STATUS_ADDR = 0x0F
  integer, parameter :: DS3231_TEMP_MSB_ADDR = 0x11
  integer, parameter :: DS3231_TEMP_LSB_ADDR = 0x12

end module ds3231_device
