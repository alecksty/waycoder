! STM32F303CCT6 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32F303CCT6
! 版本: 1.0
! 日期: 2026-04-29
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
! CPU架构: ARM-Cortex-M4F
! 位宽: 32位
! 时钟频率: 72000000 Hz

module stm32f303cct6_device
  implicit none

  ! 外设定义
  ! USART 1
  integer, parameter :: USART1_BASE = 0x40013800
  integer, parameter :: USART1_SR_ADDR = 0x00
  integer, parameter :: USART1_DR_ADDR = 0x04
  integer, parameter :: USART1_BRR_ADDR = 0x08
  integer, parameter :: USART1_CR1_ADDR = 0x0C
  integer, parameter :: USART1_CR2_ADDR = 0x10
  integer, parameter :: USART1_CR3_ADDR = 0x14
  ! USART 2
  integer, parameter :: USART2_BASE = 0x40004400
  integer, parameter :: USART2_SR_ADDR = 0x00
  integer, parameter :: USART2_DR_ADDR = 0x04
  integer, parameter :: USART2_BRR_ADDR = 0x08
  integer, parameter :: USART2_CR1_ADDR = 0x0C
  ! USART 3
  integer, parameter :: USART3_BASE = 0x40004800
  integer, parameter :: USART3_SR_ADDR = 0x00
  integer, parameter :: USART3_DR_ADDR = 0x04
  integer, parameter :: USART3_BRR_ADDR = 0x08
  integer, parameter :: USART3_CR1_ADDR = 0x0C
  ! GPIO Port A
  integer, parameter :: GPIOA_BASE = 0x48000000
  integer, parameter :: GPIOA_MODER_ADDR = 0x00
  integer, parameter :: GPIOA_OTYPER_ADDR = 0x04
  integer, parameter :: GPIOA_OSPEEDR_ADDR = 0x08
  integer, parameter :: GPIOA_PUPDR_ADDR = 0x0C
  integer, parameter :: GPIOA_IDR_ADDR = 0x10
  integer, parameter :: GPIOA_ODR_ADDR = 0x14
  integer, parameter :: GPIOA_BSRR_ADDR = 0x18
  integer, parameter :: GPIOA_AFRL_ADDR = 0x20
  integer, parameter :: GPIOA_AFRH_ADDR = 0x24
  ! 高级定时器 1
  integer, parameter :: TIM1_BASE = 0x40012C00
  integer, parameter :: TIM1_CR1_ADDR = 0x00
  integer, parameter :: TIM1_CNT_ADDR = 0x24
  integer, parameter :: TIM1_PSC_ADDR = 0x28
  integer, parameter :: TIM1_ARR_ADDR = 0x2C
  integer, parameter :: TIM1_CCR1_ADDR = 0x34
  ! ADC 1
  integer, parameter :: ADC1_BASE = 0x50000000
  integer, parameter :: ADC1_SR_ADDR = 0x00
  integer, parameter :: ADC1_CR_ADDR = 0x08
  integer, parameter :: ADC1_CFGR_ADDR = 0x0C
  integer, parameter :: ADC1_SMPR1_ADDR = 0x14
  integer, parameter :: ADC1_DR_ADDR = 0x40

end module stm32f303cct6_device
