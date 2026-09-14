! PCF8574 设备定义 - Fortran 模块
! 生成自: NXP/TI/GPIO/PCF8574
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
! CPU架构: GPIO
! 位宽: 8位
! 时钟频率: 100000 Hz

module pcf8574_device
  implicit none

  ! 外设定义
  ! PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
  integer, parameter :: PCF8574_BASE = 0x20
  integer, parameter :: PCF8574_INPUT_ADDR = 0x00
  integer, parameter :: PCF8574_INPUT_P0_BIT = 0  ! Pin P0
  integer, parameter :: PCF8574_INPUT_P1_BIT = 1  ! Pin P1
  integer, parameter :: PCF8574_INPUT_P2_BIT = 2  ! Pin P2
  integer, parameter :: PCF8574_INPUT_P3_BIT = 3  ! Pin P3
  integer, parameter :: PCF8574_INPUT_P4_BIT = 4  ! Pin P4
  integer, parameter :: PCF8574_INPUT_P5_BIT = 5  ! Pin P5
  integer, parameter :: PCF8574_INPUT_P6_BIT = 6  ! Pin P6
  integer, parameter :: PCF8574_INPUT_P7_BIT = 7  ! Pin P7
  integer, parameter :: PCF8574_OUTPUT_ADDR = 0x01
  integer, parameter :: PCF8574_POLARITY_ADDR = 0x02

  ! 中断向量定义
  integer, parameter :: INT_INT = 0  ! Pin change interrupt (open-drain, active low)

end module pcf8574_device
