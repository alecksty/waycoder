! EFR32MG24 设备定义 - Fortran 模块
! 生成自: Silicon Labs/EFR32/EFR32MG24
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
! CPU架构: ARM-Cortex-M33
! 位宽: 32位
! 时钟频率: 78000000 Hz

module efr32mg24_device
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
  integer, parameter :: FLASH_END = 0x0817FFFF
  integer, parameter :: FLASH_SIZE = 1572864  ! 
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x2003FFFF
  integer, parameter :: SRAM_SIZE = 262144  ! 
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4007FFFF
  integer, parameter :: PERIPHERAL_SIZE = 524288  ! 

  ! 外设定义
  ! Clock Management Unit
  integer, parameter :: CMU_BASE = 0x40080000
  integer, parameter :: CMU_CTRL_ADDR = 0x00
  integer, parameter :: CMU_HFCORECLKCFG_ADDR = 0x08
  integer, parameter :: CMU_HFPERCLKEN0_ADDR = 0x10
  integer, parameter :: CMU_HFPERCLKEN0_GPIOEN_BIT = 4  ! GPIO clock enable
  integer, parameter :: CMU_HFPERCLKEN0_USART0EN_BIT = 12  ! USART0 clock enable
  integer, parameter :: CMU_HFPERCLKEN0_USART1EN_BIT = 13  ! USART1 clock enable
  integer, parameter :: CMU_LFBCLKEN0_ADDR = 0x20
  ! GPIO Controller
  integer, parameter :: GPIO_BASE = 0x40088000
  integer, parameter :: GPIO_PORT_A_CTRL_ADDR = 0x00
  integer, parameter :: GPIO_PORT_B_CTRL_ADDR = 0x04
  integer, parameter :: GPIO_PORT_C_CTRL_ADDR = 0x08
  integer, parameter :: GPIO_PORT_D_CTRL_ADDR = 0x0C
  integer, parameter :: GPIO_MODEL_ADDR = 0x10
  integer, parameter :: GPIO_MODEH_ADDR = 0x14
  integer, parameter :: GPIO_DOUT_ADDR = 0x1C
  integer, parameter :: GPIO_DOUTSET_ADDR = 0x20
  integer, parameter :: GPIO_DOUTCLR_ADDR = 0x24
  integer, parameter :: GPIO_DOUTTGL_ADDR = 0x28
  integer, parameter :: GPIO_DIN_ADDR = 0x2C
  ! GPIO Port A extended
  integer, parameter :: GPIO_PA_BASE = 0x40088400
  integer, parameter :: GPIO_PA_PA_CFG_ADDR = 0x00
  integer, parameter :: GPIO_PA_PA_PINOUT_ADDR = 0x04
  ! GPIO Port B extended
  integer, parameter :: GPIO_PB_BASE = 0x40088800
  integer, parameter :: GPIO_PB_PB_CFG_ADDR = 0x00
  ! USART 0
  integer, parameter :: USART0_BASE = 0x40060000
  integer, parameter :: USART0_CTRL_ADDR = 0x00
  integer, parameter :: USART0_CMD_ADDR = 0x04
  integer, parameter :: USART0_STATUS_ADDR = 0x08
  integer, parameter :: USART0_RXDATA_ADDR = 0x0C
  integer, parameter :: USART0_TXDATA_ADDR = 0x10
  integer, parameter :: USART0_CLKDIV_ADDR = 0x14

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USART0_RX = 12  ! USART0 Receive Interrupt
  integer, parameter :: INT_USART0_TX = 13  ! USART0 Transmit Interrupt

end module efr32mg24_device
