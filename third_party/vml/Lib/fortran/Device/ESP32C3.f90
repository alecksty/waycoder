! ESP32-C3 设备定义 - Fortran 模块
! 生成自: Espressif/ESP32-C/ESP32-C3
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
! CPU架构: RISC-V
! 位宽: 32位
! 时钟频率: 160000000 Hz

module esp32_c3_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: X1_ADDR = 0x04  ! Return Address
  integer, parameter :: X2_ADDR = 0x08  ! Stack Pointer (SP)
  integer, parameter :: X3_ADDR = 0x0C  ! Global Pointer (GP)
  integer, parameter :: X8_ADDR = 0x20  ! Frame Pointer (FP)
  integer, parameter :: X10_ADDR = 0x28  ! Function Argument (A0)
  integer, parameter :: X11_ADDR = 0x2C  ! Function Argument (A1)
  integer, parameter :: PC_ADDR = 0x3C  ! Program Counter

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x42000000
  integer, parameter :: FLASH_END = 0x427FFFFF
  integer, parameter :: FLASH_SIZE = 8388608  ! Flash via Cache
  integer, parameter :: SRAM_START = 0x3FC80000
  integer, parameter :: SRAM_END = 0x3FCE3FFF
  integer, parameter :: SRAM_SIZE = 409600  ! Internal SRAM
  integer, parameter :: PERIPHERAL_START = 0x60000000
  integer, parameter :: PERIPHERAL_END = 0x600FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 

  ! 外设定义
  ! General Purpose I/O
  integer, parameter :: GPIO_BASE = 0x60004000
  integer, parameter :: GPIO_OUT_ADDR = 0x04
  integer, parameter :: GPIO_OUT_W1TS_ADDR = 0x08
  integer, parameter :: GPIO_OUT_W1TC_ADDR = 0x0C
  integer, parameter :: GPIO_IN_ADDR = 0x10
  integer, parameter :: GPIO_ENABLE_ADDR = 0x20
  integer, parameter :: GPIO_ENABLE_W1TS_ADDR = 0x24
  integer, parameter :: GPIO_ENABLE_W1TC_ADDR = 0x28
  ! I/O MUX
  integer, parameter :: IO_MUX_BASE = 0x60009000
  integer, parameter :: IO_MUX_GPIO0_ADDR = 0x00
  integer, parameter :: IO_MUX_GPIO1_ADDR = 0x04
  integer, parameter :: IO_MUX_GPIO2_ADDR = 0x08
  integer, parameter :: IO_MUX_GPIO3_ADDR = 0x0C
  ! RTC Control
  integer, parameter :: RTC_CNTL_BASE = 0x60008000
  integer, parameter :: RTC_CNTL_OPTIONS0_ADDR = 0x00
  integer, parameter :: RTC_CNTL_CLK_CONF_ADDR = 0x30

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_MACHINESOFTWARE = 3  ! 
  integer, parameter :: INT_MACHINETIMER = 7  ! 
  integer, parameter :: INT_MACHINEEXTERNAL = 11  ! 

end module esp32_c3_device
