! BL618 设备定义 - Fortran 模块
! 生成自: Bouffalo Lab/BL6/BL618
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
! CPU架构: RISC-V
! 位宽: 32位
! 时钟频率: 320000000 Hz

module bl618_device
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
  integer, parameter :: FLASH_START = 0x20000000
  integer, parameter :: FLASH_END = 0x203FFFFF
  integer, parameter :: FLASH_SIZE = 4194304  ! 
  integer, parameter :: SRAM_HPSYS_START = 0x22000000
  integer, parameter :: SRAM_HPSYS_END = 0x22003FFF
  integer, parameter :: SRAM_HPSYS_SIZE = 16384  ! 
  integer, parameter :: SRAM_DTCM_START = 0x22010000
  integer, parameter :: SRAM_DTCM_END = 0x22017FFF
  integer, parameter :: SRAM_DTCM_SIZE = 32768  ! DTCM
  integer, parameter :: SRAM_SYS_START = 0x22020000
  integer, parameter :: SRAM_SYS_END = 0x2208FFFF
  integer, parameter :: SRAM_SYS_SIZE = 458752  ! 
  integer, parameter :: PERIPHERAL_START = 0x30000000
  integer, parameter :: PERIPHERAL_END = 0x300FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 

  ! 外设定义
  ! Global Control (Clock and Reset)
  integer, parameter :: GLB_BASE = 0x30000000
  integer, parameter :: GLB_GLB_CLK_EN_ADDR = 0x10
  integer, parameter :: GLB_GLB_CLK_EN_GPIO_CLK_EN_BIT = 6  ! GPIO clock enable
  integer, parameter :: GLB_GLB_CLK_EN_UART0_CLK_EN_BIT = 12  ! UART0 clock enable
  integer, parameter :: GLB_GLB_SYS_CLK_CTRL_ADDR = 0x14
  integer, parameter :: GLB_GLB_PLL_CTRL_ADDR = 0x1C
  ! GPIO Port A
  integer, parameter :: GPIO_P0_BASE = 0x30007000
  integer, parameter :: GPIO_P0_GPIO_CFG0_ADDR = 0x00
  integer, parameter :: GPIO_P0_GPIO_CFG1_ADDR = 0x04
  integer, parameter :: GPIO_P0_GPIO_OE_ADDR = 0x08
  integer, parameter :: GPIO_P0_GPIO_OUT_ADDR = 0x0C
  integer, parameter :: GPIO_P0_GPIO_IN_ADDR = 0x10
  integer, parameter :: GPIO_P0_GPIO_SET_ADDR = 0x14
  integer, parameter :: GPIO_P0_GPIO_CLR_ADDR = 0x18
  integer, parameter :: GPIO_P0_GPIO_TOG_ADDR = 0x1C
  ! GPIO Port B
  integer, parameter :: GPIO_P1_BASE = 0x30007200
  integer, parameter :: GPIO_P1_GPIO_CFG0_ADDR = 0x00
  integer, parameter :: GPIO_P1_GPIO_CFG1_ADDR = 0x04
  integer, parameter :: GPIO_P1_GPIO_OE_ADDR = 0x08
  integer, parameter :: GPIO_P1_GPIO_OUT_ADDR = 0x0C
  integer, parameter :: GPIO_P1_GPIO_IN_ADDR = 0x10
  integer, parameter :: GPIO_P1_GPIO_SET_ADDR = 0x14
  integer, parameter :: GPIO_P1_GPIO_CLR_ADDR = 0x18
  integer, parameter :: GPIO_P1_GPIO_TOG_ADDR = 0x1C
  ! UART 0
  integer, parameter :: UART0_BASE = 0x30002000
  integer, parameter :: UART0_UART_CR_ADDR = 0x00
  integer, parameter :: UART0_UART_BRR_ADDR = 0x04
  integer, parameter :: UART0_UART_TDR_ADDR = 0x08
  integer, parameter :: UART0_UART_RDR_ADDR = 0x0C
  integer, parameter :: UART0_UART_SR_ADDR = 0x10

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! 
  integer, parameter :: INT_MACHINESOFTWARE = 3  ! 
  integer, parameter :: INT_MACHINETIMER = 7  ! 
  integer, parameter :: INT_MACHINEEXTERNAL = 11  ! 
  integer, parameter :: INT_UART0 = 20  ! UART0 Interrupt

end module bl618_device
