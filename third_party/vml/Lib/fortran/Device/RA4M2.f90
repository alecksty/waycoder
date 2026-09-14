! RA4M2 设备定义 - Fortran 模块
! 生成自: Renesas/RA/RA4M2
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
! CPU架构: ARM-Cortex-M4
! 位宽: 32位
! 时钟频率: 100000000 Hz

module ra4m2_device
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
  integer, parameter :: SRAM_START = 0x1FFE0000
  integer, parameter :: SRAM_END = 0x1FFE7FFF
  integer, parameter :: SRAM_SIZE = 32768  ! SRAM0
  integer, parameter :: SRAM1_START = 0x20000000
  integer, parameter :: SRAM1_END = 0x20017FFF
  integer, parameter :: SRAM1_SIZE = 98304  ! SRAM1
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x400FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 

  ! 外设定义
  ! Module Stop Control
  integer, parameter :: MSTP_BASE = 0x40020000
  integer, parameter :: MSTP_MSTPCR_A_ADDR = 0x20
  integer, parameter :: MSTP_MSTPCR_A_MSTP41_BIT = 9  ! GPIO A stop
  integer, parameter :: MSTP_MSTPCR_A_MSTP42_BIT = 10  ! GPIO B stop
  integer, parameter :: MSTP_MSTPCR_B_ADDR = 0x24
  integer, parameter :: MSTP_MSTPCR_C_ADDR = 0x28
  integer, parameter :: MSTP_MSTPCR_D_ADDR = 0x2C
  ! Interrupt Controller Unit
  integer, parameter :: ICU_BASE = 0x40030000
  integer, parameter :: ICU_IRQCR0_ADDR = 0x600
  integer, parameter :: ICU_IRQCR1_ADDR = 0x602
  ! General Purpose I/O Port A
  integer, parameter :: GPIOA_BASE = 0x40040000
  integer, parameter :: GPIOA_PDR_ADDR = 0x00
  integer, parameter :: GPIOA_PODR_ADDR = 0x04
  integer, parameter :: GPIOA_PIDR_ADDR = 0x08
  integer, parameter :: GPIOA_PMR_ADDR = 0x10
  integer, parameter :: GPIOA_PCR_ADDR = 0x18
  ! General Purpose I/O Port B
  integer, parameter :: GPIOB_BASE = 0x40040020
  integer, parameter :: GPIOB_PDR_ADDR = 0x00
  integer, parameter :: GPIOB_PODR_ADDR = 0x04
  integer, parameter :: GPIOB_PIDR_ADDR = 0x08
  integer, parameter :: GPIOB_PMR_ADDR = 0x10
  ! SCI UART 0
  integer, parameter :: SCIUART0_BASE = 0x40070000
  integer, parameter :: SCIUART0_SCR_ADDR = 0x00
  integer, parameter :: SCIUART0_BRR_ADDR = 0x04
  integer, parameter :: SCIUART0_TDR_ADDR = 0x08
  integer, parameter :: SCIUART0_RDR_ADDR = 0x0C
  integer, parameter :: SCIUART0_SSR_ADDR = 0x10

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_SCIUART0_RXI = 24  ! SCI UART0 Receive Interrupt
  integer, parameter :: INT_SCIUART0_TXI = 25  ! SCI UART0 Transmit Interrupt

end module ra4m2_device
