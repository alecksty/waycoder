! LPC54606 设备定义 - Fortran 模块
! 生成自: NXP/LPC/LPC54606
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
! CPU架构: ARM-Cortex-M4
! 位宽: 32位
! 时钟频率: 180000000 Hz

module lpc54606_device
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
  integer, parameter :: FLASH_START = 0x00000000
  integer, parameter :: FLASH_END = 0x0003FFFF
  integer, parameter :: FLASH_SIZE = 262144  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20021FFF
  integer, parameter :: SRAM_SIZE = 139264  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x401FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 2097152  ! 

  ! 外设定义
  ! System Control
  integer, parameter :: SYSCON_BASE = 0x40000000
  integer, parameter :: SYSCON_SYSAHBCLKCTRL_ADDR = 0x80
  integer, parameter :: SYSCON_MAINCLKSEL_ADDR = 0x04
  integer, parameter :: SYSCON_MAINCLKUEN_ADDR = 0x08
  integer, parameter :: SYSCON_SYSPLLCTRL_ADDR = 0x0C
  ! General Purpose I/O
  integer, parameter :: GPIO_BASE = 0x400F4000
  integer, parameter :: GPIO_DIR0_ADDR = 0x0000
  integer, parameter :: GPIO_PIN0_ADDR = 0x1000
  integer, parameter :: GPIO_SET0_ADDR = 0x2000
  integer, parameter :: GPIO_CLR0_ADDR = 0x3000
  integer, parameter :: GPIO_NOT0_ADDR = 0x4000
  integer, parameter :: GPIO_DIR1_ADDR = 0x0004
  integer, parameter :: GPIO_PIN1_ADDR = 0x1004
  integer, parameter :: GPIO_SET1_ADDR = 0x2004
  integer, parameter :: GPIO_CLR1_ADDR = 0x3004
  integer, parameter :: GPIO_NOT1_ADDR = 0x4004
  ! USART0
  integer, parameter :: USART0_BASE = 0x40086000
  integer, parameter :: USART0_CFG_ADDR = 0x00
  integer, parameter :: USART0_CTRL_ADDR = 0x04
  integer, parameter :: USART0_STAT_ADDR = 0x08
  integer, parameter :: USART0_TXDAT_ADDR = 0x10
  integer, parameter :: USART0_RXDAT_ADDR = 0x14
  integer, parameter :: USART0_BRG_ADDR = 0x20

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USART0 = 24  ! USART0 Interrupt

end module lpc54606_device
