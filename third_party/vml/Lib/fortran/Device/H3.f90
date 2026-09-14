! Allwinner H3 设备定义 - Fortran 模块
! 生成自: Allwinner/H-Series/Allwinner H3
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
! CPU架构: ARM-Cortex-A7
! 位宽: 32位
! 时钟频率: 1200000000 Hz

module allwinner h3_device
  implicit none

  ! 外设定义
  ! UART 0 (debug console)
  integer, parameter :: UART0_BASE = 0x01C28000
  integer, parameter :: UART0_RBR_ADDR = 0x00
  integer, parameter :: UART0_THR_ADDR = 0x00
  integer, parameter :: UART0_IER_ADDR = 0x04
  integer, parameter :: UART0_IIR_ADDR = 0x08
  integer, parameter :: UART0_FCR_ADDR = 0x08
  integer, parameter :: UART0_LCR_ADDR = 0x0C
  integer, parameter :: UART0_MCR_ADDR = 0x10
  integer, parameter :: UART0_LSR_ADDR = 0x14
  integer, parameter :: UART0_MSR_ADDR = 0x18
  integer, parameter :: UART0_DLL_ADDR = 0x00
  integer, parameter :: UART0_DLH_ADDR = 0x04
  ! UART 1
  integer, parameter :: UART1_BASE = 0x01C28400
  integer, parameter :: UART1_RBR_ADDR = 0x00
  integer, parameter :: UART1_THR_ADDR = 0x00
  integer, parameter :: UART1_LSR_ADDR = 0x14
  ! GPIO 控制器
  integer, parameter :: GPIO_BASE = 0x01C20800
  integer, parameter :: GPIO_PA_CFG0_ADDR = 0x00
  integer, parameter :: GPIO_PA_CFG1_ADDR = 0x04
  integer, parameter :: GPIO_PA_DAT_ADDR = 0x10
  integer, parameter :: GPIO_PA_DRV0_ADDR = 0x14
  integer, parameter :: GPIO_PA_PUL0_ADDR = 0x1C
  integer, parameter :: GPIO_PB_CFG0_ADDR = 0x24
  integer, parameter :: GPIO_PB_DAT_ADDR = 0x34
  integer, parameter :: GPIO_PC_CFG0_ADDR = 0x48
  integer, parameter :: GPIO_PC_DAT_ADDR = 0x58
  ! AVS 定时器
  integer, parameter :: TIMER_BASE = 0x01C20C00
  integer, parameter :: TIMER_CNT0_ADDR = 0x00
  integer, parameter :: TIMER_CNT1_ADDR = 0x04
  integer, parameter :: TIMER_CTRL_ADDR = 0x08
  integer, parameter :: TIMER_INTV_ADDR = 0x0C
  ! 时钟控制单元
  integer, parameter :: CCU_BASE = 0x01C20000
  integer, parameter :: CCU_PLL1_CFG_ADDR = 0x000
  integer, parameter :: CCU_PLL3_CFG_ADDR = 0x010
  integer, parameter :: CCU_CPU_AXI_CFG_ADDR = 0x050
  integer, parameter :: CCU_AHB1_APB1_CFG_ADDR = 0x054
  integer, parameter :: CCU_APB2_CFG_ADDR = 0x058
  integer, parameter :: CCU_BUS_GATE0_ADDR = 0x060
  integer, parameter :: CCU_BUS_GATE1_ADDR = 0x064
  integer, parameter :: CCU_BUS_GATE2_ADDR = 0x068

end module allwinner h3_device
