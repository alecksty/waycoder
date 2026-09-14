! STM32G431 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32G431
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4 MCU with 128KB Flash, 32KB RAM, 170MHz
! CPU架构: ARM-Cortex-M4
! 位宽: 32位
! 时钟频率: 170000000 Hz

module stm32g431_device
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
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x0801FFFF
  integer, parameter :: FLASH_SIZE = 131072  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20007FFF
  integer, parameter :: SRAM_SIZE = 32768  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4007FFFF
  integer, parameter :: PERIPHERAL_SIZE = 524288  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40021000
  integer, parameter :: RCC_CR_ADDR = 0x00
  integer, parameter :: RCC_CFGR_ADDR = 0x08
  integer, parameter :: RCC_PLLCFGR_ADDR = 0x0C
  integer, parameter :: RCC_AHB1ENR_ADDR = 0x38
  integer, parameter :: RCC_AHB1ENR_GPIOAEN_BIT = 0  ! GPIOA clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOBEN_BIT = 1  ! GPIOB clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOCEN_BIT = 2  ! GPIOC clock enable
  integer, parameter :: RCC_AHB1ENR_DMA1EN_BIT = 24  ! DMA1 clock enable
  integer, parameter :: RCC_AHB1ENR_DMA2EN_BIT = 25  ! DMA2 clock enable
  integer, parameter :: RCC_APB1ENR1_ADDR = 0x58
  integer, parameter :: RCC_APB2ENR_ADDR = 0x60
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x48000000
  integer, parameter :: GPIOA_MODER_ADDR = 0x00
  integer, parameter :: GPIOA_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOA_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOA_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOA_IDR_ADDR = 0x10
  integer, parameter :: GPIOA_ODR_ADDR = 0x14
  integer, parameter :: GPIOA_BSRR_ADDR = 0x18
  integer, parameter :: GPIOA_BRR_ADDR = 0x28
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x48000400
  integer, parameter :: GPIOB_MODER_ADDR = 0x00
  integer, parameter :: GPIOB_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOB_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOB_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOB_IDR_ADDR = 0x10
  integer, parameter :: GPIOB_ODR_ADDR = 0x14
  integer, parameter :: GPIOB_BSRR_ADDR = 0x18
  integer, parameter :: GPIOB_BRR_ADDR = 0x28
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x48000800
  integer, parameter :: GPIOC_MODER_ADDR = 0x00
  integer, parameter :: GPIOC_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOC_IDR_ADDR = 0x10
  integer, parameter :: GPIOC_ODR_ADDR = 0x14
  integer, parameter :: GPIOC_BSRR_ADDR = 0x18
  ! USART1
  integer, parameter :: USART1_BASE = 0x40013800
  integer, parameter :: USART1_CR1_ADDR = 0x00
  integer, parameter :: USART1_BRR_ADDR = 0x0C
  integer, parameter :: USART1_RDR_ADDR = 0x24
  integer, parameter :: USART1_TDR_ADDR = 0x28

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USART1 = 37  ! USART1 Global Interrupt

end module stm32g431_device
