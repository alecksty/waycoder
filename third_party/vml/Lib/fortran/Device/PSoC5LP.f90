! CY8C5888LTI-LP097 设备定义 - Fortran 模块
! 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
! CPU架构: ARM-Cortex-M3
! 位宽: 32位
! 时钟频率: 80000000 Hz

module cy8c5888lti_lp097_device
  implicit none

  ! 外设定义
  ! SCB UART (可编程)
  integer, parameter :: UART_BASE = 0x40050000
  integer, parameter :: UART_CTRL_ADDR = 0x00
  integer, parameter :: UART_STATUS_ADDR = 0x04
  integer, parameter :: UART_TX_DATA_ADDR = 0x08
  integer, parameter :: UART_RX_DATA_ADDR = 0x0C
  ! SCB I2C
  integer, parameter :: I2C_BASE = 0x40051000
  integer, parameter :: I2C_CTRL_ADDR = 0x00
  integer, parameter :: I2C_STATUS_ADDR = 0x04
  integer, parameter :: I2C_TX_DATA_ADDR = 0x08
  integer, parameter :: I2C_RX_DATA_ADDR = 0x0C
  ! TCPWM 定时器
  integer, parameter :: TIMER_BASE = 0x40060000
  integer, parameter :: TIMER_CTRL_ADDR = 0x00
  integer, parameter :: TIMER_STATUS_ADDR = 0x04
  integer, parameter :: TIMER_CNT_ADDR = 0x08
  integer, parameter :: TIMER_PERIOD_ADDR = 0x0C
  integer, parameter :: TIMER_CC_ADDR = 0x10
  ! DelSig ADC 20-bit
  integer, parameter :: ADC_BASE = 0x40100000
  integer, parameter :: ADC_CTRL_ADDR = 0x00
  integer, parameter :: ADC_STATUS_ADDR = 0x04
  integer, parameter :: ADC_DATA_ADDR = 0x08
  integer, parameter :: ADC_CLOCK_ADDR = 0x10
  ! GPIO 端口
  integer, parameter :: GPIO_BASE = 0x40040000
  integer, parameter :: GPIO_DR_ADDR = 0x00
  integer, parameter :: GPIO_PS_ADDR = 0x04
  integer, parameter :: GPIO_IE_ADDR = 0x08
  integer, parameter :: GPIO_DM_ADDR = 0x0C
  ! USB 控制器
  integer, parameter :: USB_BASE = 0x40080000
  integer, parameter :: USB_CR0_ADDR = 0x00
  integer, parameter :: USB_CR1_ADDR = 0x04
  integer, parameter :: USB_STAT_ADDR = 0x08

end module cy8c5888lti_lp097_device
