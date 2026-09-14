! i.MX RT1062 设备定义 - Fortran 模块
! 生成自: NXP/i.MX RT/i.MX RT1062
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
! CPU架构: ARM-Cortex-M7
! 位宽: 32位
! 时钟频率: 528000000 Hz

module i.mx rt1062_device
  implicit none

  ! 外设定义
  ! LPUART 1
  integer, parameter :: UART1_BASE = 0x40184000
  integer, parameter :: UART1_VERID_ADDR = 0x000
  integer, parameter :: UART1_CTRL_ADDR = 0x010
  integer, parameter :: UART1_STAT_ADDR = 0x014
  integer, parameter :: UART1_DATA_ADDR = 0x01C
  integer, parameter :: UART1_BAUD_ADDR = 0x024
  ! LPUART 2
  integer, parameter :: UART2_BASE = 0x40188000
  integer, parameter :: UART2_CTRL_ADDR = 0x010
  integer, parameter :: UART2_STAT_ADDR = 0x014
  integer, parameter :: UART2_DATA_ADDR = 0x01C
  integer, parameter :: UART2_BAUD_ADDR = 0x024
  ! GPIO 1
  integer, parameter :: GPIO1_BASE = 0x401B8000
  integer, parameter :: GPIO1_DR_ADDR = 0x000
  integer, parameter :: GPIO1_GDIR_ADDR = 0x004
  integer, parameter :: GPIO1_PSR_ADDR = 0x008
  integer, parameter :: GPIO1_ICR1_ADDR = 0x00C
  integer, parameter :: GPIO1_ICR2_ADDR = 0x010
  integer, parameter :: GPIO1_IMR_ADDR = 0x014
  integer, parameter :: GPIO1_ISR_ADDR = 0x018
  integer, parameter :: GPIO1_EDGE_SEL_ADDR = 0x01C
  ! GPT 定时器 1
  integer, parameter :: GPT1_BASE = 0x401EC000
  integer, parameter :: GPT1_CR_ADDR = 0x000
  integer, parameter :: GPT1_PR_ADDR = 0x004
  integer, parameter :: GPT1_SR_ADDR = 0x008
  integer, parameter :: GPT1_IR_ADDR = 0x00C
  integer, parameter :: GPT1_OCR1_ADDR = 0x010
  integer, parameter :: GPT1_CNT_ADDR = 0x024
  ! USB OTG 1
  integer, parameter :: USB1_BASE = 0x402E0000
  integer, parameter :: USB1_ID_ADDR = 0x000
  integer, parameter :: USB1_OTGSC_ADDR = 0x00C
  integer, parameter :: USB1_USBCMD_ADDR = 0x100
  integer, parameter :: USB1_PORTSC1_ADDR = 0x184

end module i.mx rt1062_device
