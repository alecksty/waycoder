! MAX7219 设备定义 - Fortran 模块
! 生成自: Maxim/LED/MAX7219
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
! CPU架构: LED
! 位宽: 8位
! 时钟频率: 10000000 Hz

module max7219_device
  implicit none

  ! 外设定义
  ! MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
  integer, parameter :: MAX7219_BASE = 0x00
  integer, parameter :: MAX7219_DIGIT0_ADDR = 0x01
  integer, parameter :: MAX7219_DIGIT1_ADDR = 0x02
  integer, parameter :: MAX7219_DIGIT2_ADDR = 0x03
  integer, parameter :: MAX7219_DIGIT3_ADDR = 0x04
  integer, parameter :: MAX7219_DIGIT4_ADDR = 0x05
  integer, parameter :: MAX7219_DIGIT5_ADDR = 0x06
  integer, parameter :: MAX7219_DIGIT6_ADDR = 0x07
  integer, parameter :: MAX7219_DIGIT7_ADDR = 0x08
  integer, parameter :: MAX7219_DECODE_ADDR = 0x09
  integer, parameter :: MAX7219_INTENSITY_ADDR = 0x0A
  integer, parameter :: MAX7219_SCAN_LIMIT_ADDR = 0x0B
  integer, parameter :: MAX7219_SHUTDOWN_ADDR = 0x0C
  integer, parameter :: MAX7219_TEST_ADDR = 0x0F

end module max7219_device
