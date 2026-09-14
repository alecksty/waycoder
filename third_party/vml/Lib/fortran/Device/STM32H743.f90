! STM32H743 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32H743
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M7 MCU with 2MB Flash, 1MB RAM, 400MHz
! CPU架构: ARM-Cortex-M7
! 位宽: 32位
! 时钟频率: 400000000 Hz

module stm32h743_device
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
  integer, parameter :: FLASH_END = 0x081FFFFF
  integer, parameter :: FLASH_SIZE = 2097152  ! 
  integer, parameter :: DTCM_START = 0x20000000
  integer, parameter :: DTCM_END = 0x2001FFFF
  integer, parameter :: DTCM_SIZE = 131072  ! DTCM RAM
  integer, parameter :: ITCM_START = 0x00000000
  integer, parameter :: ITCM_END = 0x0000FFFF
  integer, parameter :: ITCM_SIZE = 65536  ! ITCM RAM
  integer, parameter :: SRAM_AXI_START = 0x24000000
  integer, parameter :: SRAM_AXI_END = 0x2407FFFF
  integer, parameter :: SRAM_AXI_SIZE = 524288  ! AXI SRAM
  integer, parameter :: SRAM_SRAM_START = 0x30000000
  integer, parameter :: SRAM_SRAM_END = 0x3003FFFF
  integer, parameter :: SRAM_SRAM_SIZE = 262144  ! SRAM1-3
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4FFFFFFF
  integer, parameter :: PERIPHERAL_SIZE = 268435456  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x58024400
  integer, parameter :: RCC_CR_ADDR = 0x00
  integer, parameter :: RCC_CFGR_ADDR = 0x04
  integer, parameter :: RCC_PLL1CFGR_ADDR = 0x0C
  integer, parameter :: RCC_AHB1ENR_ADDR = 0x30
  integer, parameter :: RCC_AHB1ENR_GPIOAEN_BIT = 0  ! GPIOA clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOBEN_BIT = 1  ! GPIOB clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOCEN_BIT = 2  ! GPIOC clock enable
  integer, parameter :: RCC_AHB1ENR_GPIODEN_BIT = 3  ! GPIOD clock enable
  integer, parameter :: RCC_AHB1ENR_GPIOEEN_BIT = 4  ! GPIOE clock enable
  integer, parameter :: RCC_AHB1ENR_DMA1EN_BIT = 21  ! DMA1 clock enable
  integer, parameter :: RCC_AHB1ENR_DMA2EN_BIT = 22  ! DMA2 clock enable
  integer, parameter :: RCC_AHB2ENR_ADDR = 0x34
  integer, parameter :: RCC_AHB4ENR_ADDR = 0x3C
  integer, parameter :: RCC_APB1LENR_ADDR = 0x50
  integer, parameter :: RCC_APB2ENR_ADDR = 0x58
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x58020000
  integer, parameter :: GPIOA_MODER_ADDR = 0x00
  integer, parameter :: GPIOA_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOA_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOA_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOA_IDR_ADDR = 0x10
  integer, parameter :: GPIOA_ODR_ADDR = 0x14
  integer, parameter :: GPIOA_BSRR_ADDR = 0x18
  integer, parameter :: GPIOA_BRR_ADDR = 0x28
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x58020400
  integer, parameter :: GPIOB_MODER_ADDR = 0x00
  integer, parameter :: GPIOB_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOB_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOB_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOB_IDR_ADDR = 0x10
  integer, parameter :: GPIOB_ODR_ADDR = 0x14
  integer, parameter :: GPIOB_BSRR_ADDR = 0x18
  integer, parameter :: GPIOB_BRR_ADDR = 0x28
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x58020800
  integer, parameter :: GPIOC_MODER_ADDR = 0x00
  integer, parameter :: GPIOC_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOC_IDR_ADDR = 0x10
  integer, parameter :: GPIOC_ODR_ADDR = 0x14
  integer, parameter :: GPIOC_BSRR_ADDR = 0x18
  ! General Purpose I/O Port D
  integer, parameter :: GPIOD_BASE = 0x58020C00
  integer, parameter :: GPIOD_MODER_ADDR = 0x00
  integer, parameter :: GPIOD_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOD_IDR_ADDR = 0x10
  integer, parameter :: GPIOD_ODR_ADDR = 0x14
  integer, parameter :: GPIOD_BSRR_ADDR = 0x18
  ! General Purpose I/O Port E
  integer, parameter :: GPIOE_BASE = 0x58021000
  integer, parameter :: GPIOE_MODER_ADDR = 0x00
  integer, parameter :: GPIOE_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOE_IDR_ADDR = 0x10
  integer, parameter :: GPIOE_ODR_ADDR = 0x14
  integer, parameter :: GPIOE_BSRR_ADDR = 0x18
  ! USART1
  integer, parameter :: USART1_BASE = 0x40011000
  integer, parameter :: USART1_CR1_ADDR = 0x00
  integer, parameter :: USART1_BRR_ADDR = 0x0C
  integer, parameter :: USART1_RDR_ADDR = 0x24
  integer, parameter :: USART1_TDR_ADDR = 0x28

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_SYSTICK = 15  ! 
  integer, parameter :: INT_USART1 = 56  ! USART1 Global Interrupt

end module stm32h743_device
