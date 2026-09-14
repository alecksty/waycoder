! TMS320F280049 设备定义 - Fortran 模块
! 生成自: Texas Instruments/C2000/TMS320F280049
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
! CPU架构: C28x-DSP
! 位宽: 32位
! 时钟频率: 100000000 Hz

module tms320f280049_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: AL_ADDR = 0x00  ! Accumulator Low
  integer, parameter :: AH_ADDR = 0x02  ! Accumulator High
  integer, parameter :: PH_ADDR = 0x04  ! Product High
  integer, parameter :: PL_ADDR = 0x06  ! Product Low
  integer, parameter :: TREG_ADDR = 0x08  ! Temporary Register
  integer, parameter :: AR0_ADDR = 0x0A  ! 
  integer, parameter :: AR1_ADDR = 0x0C  ! 
  integer, parameter :: ST0_ADDR = 0x20  ! Status 0
  integer, parameter :: ST1_ADDR = 0x22  ! Status 1
  integer, parameter :: PC_ADDR = 0x24  ! Program Counter
  integer, parameter :: SP_ADDR = 0x26  ! Stack Pointer

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x080000
  integer, parameter :: FLASH_END = 0x0BFFFF
  integer, parameter :: FLASH_SIZE = 262144  ! 
  integer, parameter :: SRAM_LS_START = 0x008000
  integer, parameter :: SRAM_LS_END = 0x00BFFF
  integer, parameter :: SRAM_LS_SIZE = 16384  ! Local Shared RAM
  integer, parameter :: SRAM_GS_START = 0x00C000
  integer, parameter :: SRAM_GS_END = 0x01FFFF
  integer, parameter :: SRAM_GS_SIZE = 81920  ! Global Shared RAM
  integer, parameter :: PERIPHERAL_START = 0x400000
  integer, parameter :: PERIPHERAL_END = 0x40FFFF
  integer, parameter :: PERIPHERAL_SIZE = 65536  ! 

  ! 外设定义
  ! PLL Clock Control
  integer, parameter :: PLL_BASE = 0x5C10
  integer, parameter :: PLL_SYSPLLCTL1_ADDR = 0x00
  integer, parameter :: PLL_SYSPLLCTL2_ADDR = 0x02
  integer, parameter :: PLL_CLKSRCCTL1_ADDR = 0x04
  integer, parameter :: PLL_CLKSRCCTL2_ADDR = 0x06
  ! GPIO Control Registers
  integer, parameter :: GPIO_CTRL_BASE = 0x7C00
  integer, parameter :: GPIO_CTRL_GPACTRL_ADDR = 0x00
  integer, parameter :: GPIO_CTRL_GPAQSEL1_ADDR = 0x02
  integer, parameter :: GPIO_CTRL_GPAQSEL2_ADDR = 0x04
  integer, parameter :: GPIO_CTRL_GPAMUX1_ADDR = 0x06
  integer, parameter :: GPIO_CTRL_GPAMUX2_ADDR = 0x08
  integer, parameter :: GPIO_CTRL_GPADIR_ADDR = 0x0A
  integer, parameter :: GPIO_CTRL_GPAPUD_ADDR = 0x0C
  ! GPIO Data Registers
  integer, parameter :: GPIO_DATA_BASE = 0x7F00
  integer, parameter :: GPIO_DATA_GPADAT_ADDR = 0x00
  integer, parameter :: GPIO_DATA_GPASET_ADDR = 0x02
  integer, parameter :: GPIO_DATA_GPACLEAR_ADDR = 0x04
  integer, parameter :: GPIO_DATA_GPATOGGLE_ADDR = 0x06
  integer, parameter :: GPIO_DATA_GPBDAT_ADDR = 0x08
  integer, parameter :: GPIO_DATA_GPBSET_ADDR = 0x0A
  integer, parameter :: GPIO_DATA_GPBCLEAR_ADDR = 0x0C
  integer, parameter :: GPIO_DATA_GPBTOGGLE_ADDR = 0x0E
  ! GPIO B Control
  integer, parameter :: GPIO_B_CTRL_BASE = 0x7C20
  integer, parameter :: GPIO_B_CTRL_GPBMUX1_ADDR = 0x00
  integer, parameter :: GPIO_B_CTRL_GPBMUX2_ADDR = 0x02
  integer, parameter :: GPIO_B_CTRL_GPBDIR_ADDR = 0x04
  integer, parameter :: GPIO_B_CTRL_GPBPUD_ADDR = 0x06
  ! SCI-A UART
  integer, parameter :: SCI_A_BASE = 0x7320
  integer, parameter :: SCI_A_SCICCR_ADDR = 0x00
  integer, parameter :: SCI_A_SCICTL1_ADDR = 0x02
  integer, parameter :: SCI_A_SCIBAUD_ADDR = 0x04
  integer, parameter :: SCI_A_SCIRXBUF_ADDR = 0x0A
  integer, parameter :: SCI_A_SCITXBUF_ADDR = 0x0C

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_SCIA_RX = 8  ! SCI-A Receive Interrupt
  integer, parameter :: INT_SCIA_TX = 9  ! SCI-A Transmit Interrupt

end module tms320f280049_device
