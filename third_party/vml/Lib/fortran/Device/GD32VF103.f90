! GD32VF103 设备定义 - Fortran 模块
! 生成自: GigaDevice/GD32/GD32VF103
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
! CPU架构: RISC-V
! 位宽: 32位
! 时钟频率: 108000000 Hz

module gd32vf103_device
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
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x0801FFFF
  integer, parameter :: FLASH_SIZE = 131072  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20007FFF
  integer, parameter :: SRAM_SIZE = 32768  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4003FFFF
  integer, parameter :: PERIPHERAL_SIZE = 262144  ! 

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCU_BASE = 0x40021000
  integer, parameter :: RCU_CTL_ADDR = 0x00
  integer, parameter :: RCU_CFG0_ADDR = 0x04
  integer, parameter :: RCU_CFG1_ADDR = 0x08
  integer, parameter :: RCU_APB2EN_ADDR = 0x18
  integer, parameter :: RCU_APB2EN_PAEN_BIT = 2  ! GPIOA enable
  integer, parameter :: RCU_APB2EN_PBEN_BIT = 3  ! GPIOB enable
  integer, parameter :: RCU_APB2EN_PCEN_BIT = 4  ! GPIOC enable
  integer, parameter :: RCU_APB2EN_USART0EN_BIT = 14  ! USART0 enable
  integer, parameter :: RCU_APB1EN_ADDR = 0x1C
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

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_MACHINESOFTWARE = 3  ! 
  integer, parameter :: INT_MACHINETIMER = 7  ! 
  integer, parameter :: INT_MACHINEEXTERNAL = 11  ! 
  integer, parameter :: INT_USART0 = 25  ! USART0 Global Interrupt

end module gd32vf103_device
