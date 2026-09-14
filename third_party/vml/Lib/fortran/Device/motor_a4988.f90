! A4988 设备定义 - Fortran 模块
! 生成自: Allegro/Motor/A4988
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
! CPU架构: Motor
! 位宽: 8位
! 时钟频率: 0 Hz

module a4988_device
  implicit none

  ! 外设定义
  ! A4988 Stepper Motor Driver (3.3V/5V logic)
  integer, parameter :: A4988_BASE = 0x00
  integer, parameter :: A4988_CTRL_ADDR = 0x00
  integer, parameter :: A4988_CTRL_STEP_BIT = 0  ! Step pulse (rising edge)
  integer, parameter :: A4988_CTRL_DIR_BIT = 1  ! Direction (0=CW, 1=CCW)
  integer, parameter :: A4988_CTRL_ENABLE_BIT = 2  ! Enable (active low)
  integer, parameter :: A4988_CTRL_SLEEP_BIT = 3  ! Sleep mode (active low)
  integer, parameter :: A4988_CTRL_RESET_BIT = 4  ! Reset (active low)
  integer, parameter :: A4988_MICROSTEP_ADDR = 0x01
  integer, parameter :: A4988_MICROSTEP_MS1_BIT = 0  ! Microstep select 1
  integer, parameter :: A4988_MICROSTEP_MS2_BIT = 1  ! Microstep select 2
  integer, parameter :: A4988_MICROSTEP_MS3_BIT = 2  ! Microstep select 3
  integer, parameter :: A4988_STEPS_ADDR = 0x02
  integer, parameter :: A4988_DELAY_US_ADDR = 0x06

end module a4988_device
