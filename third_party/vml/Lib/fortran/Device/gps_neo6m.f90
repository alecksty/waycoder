! NEO6M 设备定义 - Fortran 模块
! 生成自: u-blox/GPS/NEO6M
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
! CPU架构: GPS
! 位宽: 8位
! 时钟频率: 9600 Hz

module neo6m_device
  implicit none

  ! 外设定义
  ! NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
  integer, parameter :: NEO6M_BASE = 0x00
  integer, parameter :: NEO6M_LATITUDE_ADDR = 0x00
  integer, parameter :: NEO6M_LONGITUDE_ADDR = 0x04
  integer, parameter :: NEO6M_ALTITUDE_ADDR = 0x08
  integer, parameter :: NEO6M_SPEED_ADDR = 0x0C
  integer, parameter :: NEO6M_HEADING_ADDR = 0x0E
  integer, parameter :: NEO6M_SATELLITES_ADDR = 0x10
  integer, parameter :: NEO6M_HDOP_ADDR = 0x11
  integer, parameter :: NEO6M_FIX_TYPE_ADDR = 0x13
  integer, parameter :: NEO6M_DATE_ADDR = 0x14
  integer, parameter :: NEO6M_TIME_ADDR = 0x18
  integer, parameter :: NEO6M_VALID_ADDR = 0x1C

end module neo6m_device
