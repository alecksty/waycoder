! TB6612 设备定义 - Fortran 模块
! 生成自: Toshiba/Motor/TB6612
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
! CPU架构: Motor
! 位宽: 8位
! 时钟频率: 100000 Hz

module tb6612_device
  implicit none

  ! 外设定义
  ! TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
  integer, parameter :: TB6612_BASE = 0x00
  integer, parameter :: TB6612_MOTOR_A_ADDR = 0x00
  integer, parameter :: TB6612_MOTOR_A_AIN1_BIT = 0  ! Motor A input 1
  integer, parameter :: TB6612_MOTOR_A_AIN2_BIT = 1  ! Motor A input 2
  integer, parameter :: TB6612_MOTOR_A_PWMA_BIT = 2  ! Motor A PWM enable
  integer, parameter :: TB6612_MOTOR_B_ADDR = 0x01
  integer, parameter :: TB6612_MOTOR_B_BIN1_BIT = 0  ! Motor B input 1
  integer, parameter :: TB6612_MOTOR_B_BIN2_BIT = 1  ! Motor B input 2
  integer, parameter :: TB6612_MOTOR_B_PWMB_BIT = 2  ! Motor B PWM enable
  integer, parameter :: TB6612_SPEED_A_ADDR = 0x02
  integer, parameter :: TB6612_SPEED_B_ADDR = 0x04
  integer, parameter :: TB6612_STBY_ADDR = 0x06

end module tb6612_device
