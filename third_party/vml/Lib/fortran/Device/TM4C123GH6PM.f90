! TM4C123GH6PM 设备定义 - Fortran 模块
! 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
! CPU架构: ARM-Cortex-M4F
! 位宽: 32位
! 时钟频率: 80000000 Hz

module tm4c123gh6pm_device
  implicit none

  ! 外设定义
  ! UART 0
  integer, parameter :: UART0_BASE = 0x4000C000
  integer, parameter :: UART0_DR_ADDR = 0x000
  integer, parameter :: UART0_FR_ADDR = 0x018
  integer, parameter :: UART0_IBRD_ADDR = 0x024
  integer, parameter :: UART0_FBRD_ADDR = 0x028
  integer, parameter :: UART0_LCRH_ADDR = 0x02C
  integer, parameter :: UART0_CTL_ADDR = 0x030
  integer, parameter :: UART0_IM_ADDR = 0x038
  integer, parameter :: UART0_RIS_ADDR = 0x03C
  integer, parameter :: UART0_ICR_ADDR = 0x044
  ! UART 1
  integer, parameter :: UART1_BASE = 0x4000D000
  integer, parameter :: UART1_DR_ADDR = 0x000
  integer, parameter :: UART1_FR_ADDR = 0x018
  integer, parameter :: UART1_IBRD_ADDR = 0x024
  integer, parameter :: UART1_FBRD_ADDR = 0x028
  integer, parameter :: UART1_LCRH_ADDR = 0x02C
  integer, parameter :: UART1_CTL_ADDR = 0x030
  ! GPIO Port A
  integer, parameter :: GPIOA_BASE = 0x40004000
  integer, parameter :: GPIOA_DATA_ADDR = 0x3FC
  integer, parameter :: GPIOA_DIR_ADDR = 0x400
  integer, parameter :: GPIOA_IS_ADDR = 0x404
  integer, parameter :: GPIOA_IBE_ADDR = 0x408
  integer, parameter :: GPIOA_IEV_ADDR = 0x40C
  integer, parameter :: GPIOA_IM_ADDR = 0x410
  integer, parameter :: GPIOA_RIS_ADDR = 0x414
  integer, parameter :: GPIOA_MIS_ADDR = 0x418
  integer, parameter :: GPIOA_ICR_ADDR = 0x41C
  integer, parameter :: GPIOA_AFSEL_ADDR = 0x420
  integer, parameter :: GPIOA_DEN_ADDR = 0x51C
  ! 16/32-bit Timer 0
  integer, parameter :: TIMER0_BASE = 0x40030000
  integer, parameter :: TIMER0_CFG_ADDR = 0x000
  integer, parameter :: TIMER0_TAMR_ADDR = 0x004
  integer, parameter :: TIMER0_CTL_ADDR = 0x00C
  integer, parameter :: TIMER0_ILR_ADDR = 0x028
  integer, parameter :: TIMER0_V_ADDR = 0x038
  integer, parameter :: TIMER0_ICR_ADDR = 0x024
  ! ADC 0
  integer, parameter :: ADC0_BASE = 0x40038000
  integer, parameter :: ADC0_ACTSS_ADDR = 0x000
  integer, parameter :: ADC0_EMUX_ADDR = 0x014
  integer, parameter :: ADC0_SSMUX0_ADDR = 0x040
  integer, parameter :: ADC0_SSFIFO0_ADDR = 0x048
  integer, parameter :: ADC0_PROC_ADDR = 0x030

end module tm4c123gh6pm_device
