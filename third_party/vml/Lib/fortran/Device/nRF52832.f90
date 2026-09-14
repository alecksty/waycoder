! nRF52832 设备定义 - Fortran 模块
! 生成自: Nordic/nRF52/nRF52832
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
! CPU架构: ARM-Cortex-M4F
! 位宽: 32位
! 时钟频率: 64000000 Hz

module nrf52832_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x04  ! 
  integer, parameter :: R2_ADDR = 0x08  ! 
  integer, parameter :: R3_ADDR = 0x0C  ! 
  integer, parameter :: SP_ADDR = 0x34  ! 
  integer, parameter :: LR_ADDR = 0x38  ! 
  integer, parameter :: PC_ADDR = 0x3C  ! 

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x00000000
  integer, parameter :: FLASH_END = 0x0007FFFF
  integer, parameter :: FLASH_SIZE = 524288  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x2000FFFF
  integer, parameter :: SRAM_SIZE = 65536  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x400FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 
  integer, parameter :: FICR_START = 0x10000000
  integer, parameter :: FICR_END = 0x10000FFF
  integer, parameter :: FICR_SIZE = 4096  ! Factory Information Configuration Registers

  ! 外设定义
  ! General Purpose I/O Port 0
  integer, parameter :: GPIO_P0_BASE = 0x50000000
  integer, parameter :: GPIO_P0_OUT_ADDR = 0x504
  integer, parameter :: GPIO_P0_OUTSET_ADDR = 0x508
  integer, parameter :: GPIO_P0_OUTCLR_ADDR = 0x50C
  integer, parameter :: GPIO_P0_IN_ADDR = 0x510
  integer, parameter :: GPIO_P0_DIR_ADDR = 0x514
  integer, parameter :: GPIO_P0_DIRSET_ADDR = 0x518
  integer, parameter :: GPIO_P0_DIRCLR_ADDR = 0x51C
  ! Power Control
  integer, parameter :: POWER_BASE = 0x40000000
  integer, parameter :: POWER_DCDCEN_ADDR = 0x1C4
  integer, parameter :: POWER_RAMSTATUS_ADDR = 0x268
  ! Clock Control
  integer, parameter :: CLOCK_BASE = 0x40000000
  integer, parameter :: CLOCK_HFCLKSTART_ADDR = 0x108
  integer, parameter :: CLOCK_HFCLKSTARTED_ADDR = 0x208

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 

end module nrf52832_device
