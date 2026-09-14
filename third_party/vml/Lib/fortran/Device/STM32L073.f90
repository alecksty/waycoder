! STM32L073 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32L073
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M0+ Ultra-Low-Power MCU with 192KB Flash, 20KB RAM, 32MHz
! CPU架构: ARM-Cortex-M0+
! 位宽: 32位
! 时钟频率: 32000000 Hz

module stm32l073_device
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
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x0802FFFF
  integer, parameter :: FLASH_SIZE = 196608  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20004FFF
  integer, parameter :: SRAM_SIZE = 20480  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4002FFFF
  integer, parameter :: PERIPHERAL_SIZE = 196608  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40020000
  integer, parameter :: RCC_CR_ADDR = 0x00
  integer, parameter :: RCC_CFGR_ADDR = 0x04
  integer, parameter :: RCC_AHBENR_ADDR = 0x1C
  integer, parameter :: RCC_AHBENR_GPIOAEN_BIT = 17  ! GPIOA clock enable
  integer, parameter :: RCC_AHBENR_GPIOBEN_BIT = 18  ! GPIOB clock enable
  integer, parameter :: RCC_AHBENR_GPIOCEN_BIT = 19  ! GPIOC clock enable
  integer, parameter :: RCC_APB1ENR_ADDR = 0x20
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x50000000
  integer, parameter :: GPIOA_MODER_ADDR = 0x00
  integer, parameter :: GPIOA_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOA_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOA_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOA_IDR_ADDR = 0x10
  integer, parameter :: GPIOA_ODR_ADDR = 0x14
  integer, parameter :: GPIOA_BSRR_ADDR = 0x18
  integer, parameter :: GPIOA_BRR_ADDR = 0x28
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x50000400
  integer, parameter :: GPIOB_MODER_ADDR = 0x00
  integer, parameter :: GPIOB_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOB_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOB_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOB_IDR_ADDR = 0x10
  integer, parameter :: GPIOB_ODR_ADDR = 0x14
  integer, parameter :: GPIOB_BSRR_ADDR = 0x18
  integer, parameter :: GPIOB_BRR_ADDR = 0x28
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x50000800
  integer, parameter :: GPIOC_MODER_ADDR = 0x00
  integer, parameter :: GPIOC_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOC_IDR_ADDR = 0x10
  integer, parameter :: GPIOC_ODR_ADDR = 0x14
  integer, parameter :: GPIOC_BSRR_ADDR = 0x18
  ! General Purpose I/O Port D
  integer, parameter :: GPIOD_BASE = 0x50000C00
  integer, parameter :: GPIOD_MODER_ADDR = 0x00
  integer, parameter :: GPIOD_IDR_ADDR = 0x10
  integer, parameter :: GPIOD_ODR_ADDR = 0x14
  integer, parameter :: GPIOD_BSRR_ADDR = 0x18
  ! General Purpose I/O Port E
  integer, parameter :: GPIOE_BASE = 0x50001000
  integer, parameter :: GPIOE_MODER_ADDR = 0x00
  integer, parameter :: GPIOE_IDR_ADDR = 0x10
  integer, parameter :: GPIOE_ODR_ADDR = 0x14
  integer, parameter :: GPIOE_BSRR_ADDR = 0x18

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 

end module stm32l073_device
