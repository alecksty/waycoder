! STM32F411 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32F411
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4 MCU with 512KB Flash, 128KB RAM, 100MHz
! CPU架构: ARM-Cortex-M4
! 位宽: 32位
! 时钟频率: 100000000 Hz

module stm32f411_device
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
  integer, parameter :: FLASH_END = 0x0807FFFF
  integer, parameter :: FLASH_SIZE = 524288  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x2001FFFF
  integer, parameter :: SRAM_SIZE = 131072  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x400FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40023800
  integer, parameter :: RCC_CR_ADDR = 0x00
  integer, parameter :: RCC_PLLCFGR_ADDR = 0x04
  integer, parameter :: RCC_CFGR_ADDR = 0x08
  integer, parameter :: RCC_AHB1ENR_ADDR = 0x30
  integer, parameter :: RCC_AHB1ENR_GPIOAEN_BIT = 0  ! GPIOA clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOBEN_BIT = 1  ! GPIOB clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOCEN_BIT = 2  ! GPIOC clock enable
  integer, parameter :: RCC_APB1ENR_ADDR = 0x40
  integer, parameter :: RCC_APB2ENR_ADDR = 0x44
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x40020000
  integer, parameter :: GPIOA_MODER_ADDR = 0x00
  integer, parameter :: GPIOA_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOA_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOA_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOA_IDR_ADDR = 0x10
  integer, parameter :: GPIOA_ODR_ADDR = 0x14
  integer, parameter :: GPIOA_BSRR_ADDR = 0x18
  integer, parameter :: GPIOA_BRR_ADDR = 0x28
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x40020400
  integer, parameter :: GPIOB_MODER_ADDR = 0x00
  integer, parameter :: GPIOB_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOB_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOB_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOB_IDR_ADDR = 0x10
  integer, parameter :: GPIOB_ODR_ADDR = 0x14
  integer, parameter :: GPIOB_BSRR_ADDR = 0x18
  integer, parameter :: GPIOB_BRR_ADDR = 0x28
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x40020800
  integer, parameter :: GPIOC_MODER_ADDR = 0x00
  integer, parameter :: GPIOC_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOC_IDR_ADDR = 0x10
  integer, parameter :: GPIOC_ODR_ADDR = 0x14
  integer, parameter :: GPIOC_BSRR_ADDR = 0x18
  ! Universal Synchronous/Asynchronous Receiver/Transmitter 1
  integer, parameter :: USART1_BASE = 0x40011000
  integer, parameter :: USART1_SR_ADDR = 0x00
  integer, parameter :: USART1_DR_ADDR = 0x04
  integer, parameter :: USART1_BRR_ADDR = 0x08
  integer, parameter :: USART1_CR1_ADDR = 0x0C

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USART1 = 37  ! USART1 Global Interrupt

end module stm32f411_device
