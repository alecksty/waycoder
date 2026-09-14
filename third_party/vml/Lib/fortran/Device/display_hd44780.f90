! HD44780 设备定义 - Fortran 模块
! 生成自: Hitachi/Display/HD44780
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
! CPU架构: Display
! 位宽: 8位
! 时钟频率: 0 Hz

module hd44780_device
  implicit none

  ! 内存段定义
  integer, parameter :: DDRAM_START = 0x00
  integer, parameter :: DDRAM_END = 0x4F
  integer, parameter :: DDRAM_SIZE = 80  ! Display Data RAM (80 bytes, 2 lines)
  integer, parameter :: CGRAM_START = 0x00
  integer, parameter :: CGRAM_END = 0x3F
  integer, parameter :: CGRAM_SIZE = 64  ! Character Generator RAM (8 custom chars x 8 bytes)

  ! 外设定义
  ! HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
  integer, parameter :: HD44780_BASE = 0x27
  integer, parameter :: HD44780_CMD_ADDR = 0x00
  integer, parameter :: HD44780_DATA_ADDR = 0x01
  integer, parameter :: HD44780_CTRL_RS_ADDR = 0x00
  integer, parameter :: HD44780_CTRL_RW_ADDR = 0x01
  integer, parameter :: HD44780_CTRL_EN_ADDR = 0x02
  integer, parameter :: HD44780_CTRL_BL_ADDR = 0x03
  integer, parameter :: HD44780_ADDR_DDRAM_ADDR = 0x80
  integer, parameter :: HD44780_ADDR_CGRAM_ADDR = 0x40

end module hd44780_device
