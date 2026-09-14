! CH32V003 设备定义 - Fortran 模块
! 生成自: WCH/CH32V0/CH32V003
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
! CPU架构: RISC-V
! 位宽: 32位
! 时钟频率: 48000000 Hz

module ch32v003_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: X1_ADDR = 0x04  ! Return Address
  integer, parameter :: X2_ADDR = 0x08  ! Stack Pointer (SP)
  integer, parameter :: X3_ADDR = 0x0C  ! Global Pointer (GP)
  integer, parameter :: PC_ADDR = 0x3C  ! Program Counter

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x08003FFF
  integer, parameter :: FLASH_SIZE = 16384  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x200007FF
  integer, parameter :: SRAM_SIZE = 2048  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x40003FFF
  integer, parameter :: PERIPHERAL_SIZE = 16384  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40021000
  integer, parameter :: RCC_CTLR_ADDR = 0x00
  integer, parameter :: RCC_CFGR0_ADDR = 0x04
  integer, parameter :: RCC_APB2PCENR_ADDR = 0x18
  integer, parameter :: RCC_APB2PCENR_IOPAEN_BIT = 2  ! GPIOA clock enable
  integer, parameter :: RCC_APB2PCENR_IOPCEN_BIT = 4  ! GPIOC clock enable
  integer, parameter :: RCC_APB2PCENR_IOPDEN_BIT = 5  ! GPIOD clock enable
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x40010800
  integer, parameter :: GPIOA_CFGLR_ADDR = 0x00
  integer, parameter :: GPIOA_CFGHR_ADDR = 0x04
  integer, parameter :: GPIOA_INDR_ADDR = 0x08
  integer, parameter :: GPIOA_OUTDR_ADDR = 0x0C
  integer, parameter :: GPIOA_BSHR_ADDR = 0x10
  integer, parameter :: GPIOA_BCR_ADDR = 0x14
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x40011000
  integer, parameter :: GPIOC_CFGLR_ADDR = 0x00
  integer, parameter :: GPIOC_CFGHR_ADDR = 0x04
  integer, parameter :: GPIOC_INDR_ADDR = 0x08
  integer, parameter :: GPIOC_OUTDR_ADDR = 0x0C
  integer, parameter :: GPIOC_BSHR_ADDR = 0x10
  integer, parameter :: GPIOC_BCR_ADDR = 0x14
  ! General Purpose I/O Port D
  integer, parameter :: GPIOD_BASE = 0x40011400
  integer, parameter :: GPIOD_CFGLR_ADDR = 0x00
  integer, parameter :: GPIOD_CFGHR_ADDR = 0x04
  integer, parameter :: GPIOD_INDR_ADDR = 0x08
  integer, parameter :: GPIOD_OUTDR_ADDR = 0x0C
  integer, parameter :: GPIOD_BSHR_ADDR = 0x10
  integer, parameter :: GPIOD_BCR_ADDR = 0x14
  ! USART1
  integer, parameter :: USART1_BASE = 0x40013800
  integer, parameter :: USART1_STATR_ADDR = 0x00
  integer, parameter :: USART1_DATAR_ADDR = 0x04
  integer, parameter :: USART1_BRR_ADDR = 0x08
  integer, parameter :: USART1_CTLR1_ADDR = 0x0C

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_MACHINESOFTWARE = 3  ! 
  integer, parameter :: INT_MACHINETIMER = 7  ! 
  integer, parameter :: INT_MACHINEEXTERNAL = 11  ! 
  integer, parameter :: INT_USART1 = 25  ! USART1 Global Interrupt

end module ch32v003_device
