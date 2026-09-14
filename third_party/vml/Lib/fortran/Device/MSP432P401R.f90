! MSP432P401R 设备定义 - Fortran 模块
! 生成自: Texas Instruments/MSP432/MSP432P401R
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 64KB SRAM, 48MHz, FPU
! CPU架构: ARM-Cortex-M4F
! 位宽: 32位
! 时钟频率: 48000000 Hz

module msp432p401r_device
  implicit none

  ! 外设定义
  ! eUSCI_A0 UART
  integer, parameter :: UART0_BASE = 0x40001000
  integer, parameter :: UART0_CTLW0_ADDR = 0x00
  integer, parameter :: UART0_BRW_ADDR = 0x06
  integer, parameter :: UART0_UCA0TXBUF_ADDR = 0x08
  integer, parameter :: UART0_UCA0RXBUF_ADDR = 0x0A
  integer, parameter :: UART0_IFG_ADDR = 0x0C
  integer, parameter :: UART0_IE_ADDR = 0x0E
  ! eUSCI_A1 UART
  integer, parameter :: UART1_BASE = 0x40002000
  integer, parameter :: UART1_CTLW0_ADDR = 0x00
  integer, parameter :: UART1_BRW_ADDR = 0x06
  integer, parameter :: UART1_TXBUF_ADDR = 0x08
  integer, parameter :: UART1_RXBUF_ADDR = 0x0A
  integer, parameter :: UART1_IFG_ADDR = 0x0C
  integer, parameter :: UART1_IE_ADDR = 0x0E
  ! Timer_A0 16bit
  integer, parameter :: TIMER0_BASE = 0x40003000
  integer, parameter :: TIMER0_CTL_ADDR = 0x00
  integer, parameter :: TIMER0_R_ADDR = 0x10
  integer, parameter :: TIMER0_CCR0_ADDR = 0x12
  integer, parameter :: TIMER0_CCR1_ADDR = 0x14
  integer, parameter :: TIMER0_CCR2_ADDR = 0x16
  integer, parameter :: TIMER0_EX0_ADDR = 0x20
  ! ADC14 14-bit
  integer, parameter :: ADC14_BASE = 0x40006000
  integer, parameter :: ADC14_CTL0_ADDR = 0x00
  integer, parameter :: ADC14_CTL1_ADDR = 0x02
  integer, parameter :: ADC14_LO_ADDR = 0x04
  integer, parameter :: ADC14_HI_ADDR = 0x06
  integer, parameter :: ADC14_MCTL0_ADDR = 0x08
  integer, parameter :: ADC14_MEM0_ADDR = 0x20

end module msp432p401r_device
