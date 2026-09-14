! RP2350 设备定义 - Fortran 模块
! 生成自: Raspberry/RP2/RP2350
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
! CPU架构: ARM-Cortex-M33
! 位宽: 32位
! 时钟频率: 150000000 Hz

module rp2350_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x04  ! 
  integer, parameter :: R2_ADDR = 0x08  ! 
  integer, parameter :: R3_ADDR = 0x0C  ! 
  integer, parameter :: R4_ADDR = 0x10  ! 
  integer, parameter :: R5_ADDR = 0x14  ! 
  integer, parameter :: SP_ADDR = 0x34  ! 
  integer, parameter :: LR_ADDR = 0x38  ! 
  integer, parameter :: PC_ADDR = 0x3C  ! 

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x10000000
  integer, parameter :: FLASH_END = 0x107FFFFF
  integer, parameter :: FLASH_SIZE = 8388608  ! XIP Flash
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20081FFF
  integer, parameter :: SRAM_SIZE = 532480  ! Total SRAM
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x5000FFFF
  integer, parameter :: PERIPHERAL_SIZE = 16777216  ! 

  ! 外设定义
  ! Single-Cycle I/O (GPIO)
  integer, parameter :: SIO_BASE = 0xD0000000
  integer, parameter :: SIO_GPIO_IN_ADDR = 0x004
  integer, parameter :: SIO_GPIO_OUT_ADDR = 0x010
  integer, parameter :: SIO_GPIO_OUT_SET_ADDR = 0x014
  integer, parameter :: SIO_GPIO_OUT_CLR_ADDR = 0x018
  integer, parameter :: SIO_GPIO_OUT_XOR_ADDR = 0x01C
  integer, parameter :: SIO_GPIO_OE_ADDR = 0x020
  integer, parameter :: SIO_GPIO_OE_SET_ADDR = 0x024
  integer, parameter :: SIO_GPIO_OE_CLR_ADDR = 0x028
  ! IO Bank 0 (GPIO control)
  integer, parameter :: IO_BANK0_BASE = 0x40028000
  integer, parameter :: IO_BANK0_GPIO0_STATUS_ADDR = 0x000
  integer, parameter :: IO_BANK0_GPIO0_CTRL_ADDR = 0x004
  integer, parameter :: IO_BANK0_GPIO1_STATUS_ADDR = 0x008
  integer, parameter :: IO_BANK0_GPIO1_CTRL_ADDR = 0x00C
  ! Pad controls for GPIO 0-29
  integer, parameter :: PADS_BANK0_BASE = 0x4002C000
  integer, parameter :: PADS_BANK0_GPIO0_ADDR = 0x000
  integer, parameter :: PADS_BANK0_GPIO1_ADDR = 0x004
  ! Reset Controller
  integer, parameter :: RESETS_BASE = 0x4000C000
  integer, parameter :: RESETS_RESET_ADDR = 0x000
  integer, parameter :: RESETS_RESET_DONE_ADDR = 0x008

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 

end module rp2350_device
