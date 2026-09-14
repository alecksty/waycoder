! 24C64 设备定义 - Fortran 模块
! 生成自: Generic/Memory/24C64
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
! CPU架构: Memory
! 位宽: 8位
! 时钟频率: 400000 Hz

module 24c64_device
  implicit none

  ! 内存段定义
  integer, parameter :: EEPROM_START = 0x00
  integer, parameter :: EEPROM_END = 0x1FFF
  integer, parameter :: EEPROM_SIZE = 8192  ! EEPROM main memory array (8KB, 32-byte page write)

  ! 外设定义
  ! 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
  integer, parameter :: _24C64_BASE = 0x50
  integer, parameter :: _24C64_ADDR_H_ADDR = 0x00
  integer, parameter :: _24C64_ADDR_L_ADDR = 0x01
  integer, parameter :: _24C64_DATA_ADDR = 0x02
  integer, parameter :: _24C64_PAGE_SIZE_ADDR = 0xFE
  integer, parameter :: _24C64_SIZE_ADDR = 0xFD

end module 24c64_device
