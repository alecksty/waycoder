! GD32F103 设备定义 - Fortran 模块
! 生成自: GigaDevice/GD32/GD32F103
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
! CPU架构: ARM-Cortex-M3
! 位宽: 32位
! 时钟频率: 108000000 Hz

module gd32f103_device
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
  integer, parameter :: SRAM_END = 0x20004FFF
  integer, parameter :: SRAM_SIZE = 20480  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4003FFFF
  integer, parameter :: PERIPHERAL_SIZE = 262144  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40021000
  integer, parameter :: RCC_CTLR_ADDR = 0x00
  integer, parameter :: RCC_CFGR0_ADDR = 0x04
  integer, parameter :: RCC_APB2PCENR_ADDR = 0x18
  integer, parameter :: RCC_APB2PCENR_IOPAEN_BIT = 2  ! GPIOA clock enable
  integer, parameter :: RCC_APB2PCENR_IOPBEN_BIT = 3  ! GPIOB clock enable
  integer, parameter :: RCC_APB2PCENR_IOPCEN_BIT = 4  ! GPIOC clock enable
  integer, parameter :: RCC_APB2PCENR_USART0EN_BIT = 14  ! USART0 clock enable
  integer, parameter :: RCC_APB1PCENR_ADDR = 0x1C
  integer, parameter :: RCC_APB1PCENR_USART1EN_BIT = 17  ! USART1 clock enable
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x40010800
  integer, parameter :: GPIOA_CTL0_ADDR = 0x00
  integer, parameter :: GPIOA_CTL1_ADDR = 0x04
  integer, parameter :: GPIOA_ISTAT_ADDR = 0x08
  integer, parameter :: GPIOA_OCTL_ADDR = 0x0C
  integer, parameter :: GPIOA_BOP_ADDR = 0x10
  integer, parameter :: GPIOA_BC_ADDR = 0x14
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x40010C00
  integer, parameter :: GPIOB_CTL0_ADDR = 0x00
  integer, parameter :: GPIOB_CTL1_ADDR = 0x04
  integer, parameter :: GPIOB_ISTAT_ADDR = 0x08
  integer, parameter :: GPIOB_OCTL_ADDR = 0x0C
  integer, parameter :: GPIOB_BOP_ADDR = 0x10
  integer, parameter :: GPIOB_BC_ADDR = 0x14
  ! General Purpose I/O Port C
  integer, parameter :: GPIOC_BASE = 0x40011000
  integer, parameter :: GPIOC_CTL0_ADDR = 0x00
  integer, parameter :: GPIOC_CTL1_ADDR = 0x04
  integer, parameter :: GPIOC_ISTAT_ADDR = 0x08
  integer, parameter :: GPIOC_OCTL_ADDR = 0x0C
  integer, parameter :: GPIOC_BOP_ADDR = 0x10
  integer, parameter :: GPIOC_BC_ADDR = 0x14
  ! USART0
  integer, parameter :: USART0_BASE = 0x40013800
  integer, parameter :: USART0_STATR_ADDR = 0x00
  integer, parameter :: USART0_DATAR_ADDR = 0x04
  integer, parameter :: USART0_BRR_ADDR = 0x08
  integer, parameter :: USART0_CTLR1_ADDR = 0x0C
  ! USART1
  integer, parameter :: USART1_BASE = 0x40004400
  integer, parameter :: USART1_STATR_ADDR = 0x00
  integer, parameter :: USART1_DATAR_ADDR = 0x04
  integer, parameter :: USART1_BRR_ADDR = 0x08
  integer, parameter :: USART1_CTLR1_ADDR = 0x0C

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USART0 = 25  ! USART0 Global Interrupt
  integer, parameter :: INT_USART1 = 37  ! USART1 Global Interrupt

end module gd32f103_device
