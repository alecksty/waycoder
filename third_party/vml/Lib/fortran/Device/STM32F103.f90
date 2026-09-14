! STM32F103C8T6 设备定义 - Fortran 模块
! 生成自: STMicroelectronics/STM32/STM32F103C8T6
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
! CPU架构: ARM-Cortex-M3
! 位宽: 32位
! 时钟频率: 72000000 Hz

module stm32f103c8t6_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! General Purpose Register 0
  integer, parameter :: R1_ADDR = 0x04  ! General Purpose Register 1
  integer, parameter :: R2_ADDR = 0x08  ! General Purpose Register 2
  integer, parameter :: R3_ADDR = 0x0C  ! General Purpose Register 3
  integer, parameter :: R4_ADDR = 0x10  ! General Purpose Register 4
  integer, parameter :: R5_ADDR = 0x14  ! General Purpose Register 5
  integer, parameter :: R6_ADDR = 0x18  ! General Purpose Register 6
  integer, parameter :: R7_ADDR = 0x1C  ! General Purpose Register 7
  integer, parameter :: R8_ADDR = 0x20  ! General Purpose Register 8
  integer, parameter :: R9_ADDR = 0x24  ! General Purpose Register 9
  integer, parameter :: R10_ADDR = 0x28  ! General Purpose Register 10
  integer, parameter :: R11_ADDR = 0x2C  ! General Purpose Register 11
  integer, parameter :: R12_ADDR = 0x30  ! General Purpose Register 12
  integer, parameter :: SP_ADDR = 0x34  ! Stack Pointer
  integer, parameter :: LR_ADDR = 0x38  ! Link Register
  integer, parameter :: PC_ADDR = 0x3C  ! Program Counter
  integer, parameter :: XPSR_ADDR = 0x40  ! Program Status Register

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x0800FFFF
  integer, parameter :: FLASH_SIZE = 65536  ! Main Flash Memory (64KB)
  integer, parameter :: SYSTEM_MEMORY_START = 0x1FFFF000
  integer, parameter :: SYSTEM_MEMORY_END = 0x1FFFF7FF
  integer, parameter :: SYSTEM_MEMORY_SIZE = 2048  ! System Memory (2KB)
  integer, parameter :: SRAM_START = 0x20000000
  integer, parameter :: SRAM_END = 0x20004FFF
  integer, parameter :: SRAM_SIZE = 20480  ! SRAM (20KB)
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x40023FFF
  integer, parameter :: PERIPHERAL_SIZE = 143360  ! Peripheral Registers
  integer, parameter :: CORTEX_M_START = 0xE0000000
  integer, parameter :: CORTEX_M_END = 0xE00FFFFF
  integer, parameter :: CORTEX_M_SIZE = 1048576  ! Core Peripheral Registers

  ! 外设定义
  ! Reset and Clock Control
  integer, parameter :: RCC_BASE = 0x40021000
  integer, parameter :: RCC_CR_ADDR = 0x00
  integer, parameter :: RCC_CFGR_ADDR = 0x04
  integer, parameter :: RCC_APB2ENR_ADDR = 0x18
  integer, parameter :: RCC_APB1ENR_ADDR = 0x1C
  ! GPIO Port A
  integer, parameter :: GPIOA_BASE = 0x40010800
  integer, parameter :: GPIOA_CRL_ADDR = 0x00
  integer, parameter :: GPIOA_CRH_ADDR = 0x04
  integer, parameter :: GPIOA_IDR_ADDR = 0x08
  integer, parameter :: GPIOA_ODR_ADDR = 0x0C
  integer, parameter :: GPIOA_BSRR_ADDR = 0x10
  integer, parameter :: GPIOA_BRR_ADDR = 0x14
  integer, parameter :: GPIOA_LCKR_ADDR = 0x18
  ! GPIO Port B
  integer, parameter :: GPIOB_BASE = 0x40010C00
  integer, parameter :: GPIOB_CRL_ADDR = 0x00
  integer, parameter :: GPIOB_CRH_ADDR = 0x04
  integer, parameter :: GPIOB_IDR_ADDR = 0x08
  integer, parameter :: GPIOB_ODR_ADDR = 0x0C
  integer, parameter :: GPIOB_BSRR_ADDR = 0x10
  integer, parameter :: GPIOB_BRR_ADDR = 0x14
  ! GPIO Port C
  integer, parameter :: GPIOC_BASE = 0x40011000
  integer, parameter :: GPIOC_CRL_ADDR = 0x00
  integer, parameter :: GPIOC_CRH_ADDR = 0x04
  integer, parameter :: GPIOC_IDR_ADDR = 0x08
  integer, parameter :: GPIOC_ODR_ADDR = 0x0C
  integer, parameter :: GPIOC_BSRR_ADDR = 0x10
  ! USART 1
  integer, parameter :: USART1_BASE = 0x40013800
  integer, parameter :: USART1_SR_ADDR = 0x00
  integer, parameter :: USART1_DR_ADDR = 0x04
  integer, parameter :: USART1_BRR_ADDR = 0x08
  integer, parameter :: USART1_CR1_ADDR = 0x0C
  integer, parameter :: USART1_CR2_ADDR = 0x10
  integer, parameter :: USART1_CR3_ADDR = 0x14
  integer, parameter :: USART1_GTPR_ADDR = 0x18
  ! USART 2
  integer, parameter :: USART2_BASE = 0x40004400
  integer, parameter :: USART2_SR_ADDR = 0x00
  integer, parameter :: USART2_DR_ADDR = 0x04
  integer, parameter :: USART2_BRR_ADDR = 0x08
  integer, parameter :: USART2_CR1_ADDR = 0x0C
  ! SPI 1
  integer, parameter :: SPI1_BASE = 0x40013000
  integer, parameter :: SPI1_CR1_ADDR = 0x00
  integer, parameter :: SPI1_CR2_ADDR = 0x04
  integer, parameter :: SPI1_SR_ADDR = 0x08
  integer, parameter :: SPI1_DR_ADDR = 0x0C
  ! SPI 2
  integer, parameter :: SPI2_BASE = 0x40003800
  integer, parameter :: SPI2_CR1_ADDR = 0x00
  integer, parameter :: SPI2_CR2_ADDR = 0x04
  integer, parameter :: SPI2_SR_ADDR = 0x08
  integer, parameter :: SPI2_DR_ADDR = 0x0C
  ! I2C 1
  integer, parameter :: I2C1_BASE = 0x40005400
  integer, parameter :: I2C1_CR1_ADDR = 0x00
  integer, parameter :: I2C1_CR2_ADDR = 0x04
  integer, parameter :: I2C1_SR1_ADDR = 0x08
  integer, parameter :: I2C1_SR2_ADDR = 0x0C
  integer, parameter :: I2C1_DR_ADDR = 0x10
  integer, parameter :: I2C1_CCR_ADDR = 0x14
  integer, parameter :: I2C1_TRISE_ADDR = 0x18
  ! I2C 2
  integer, parameter :: I2C2_BASE = 0x40005800
  integer, parameter :: I2C2_CR1_ADDR = 0x00
  integer, parameter :: I2C2_CR2_ADDR = 0x04
  integer, parameter :: I2C2_SR1_ADDR = 0x08
  integer, parameter :: I2C2_SR2_ADDR = 0x0C
  integer, parameter :: I2C2_DR_ADDR = 0x10
  integer, parameter :: I2C2_CCR_ADDR = 0x14
  ! Advanced Timer 1
  integer, parameter :: TIM1_BASE = 0x40012C00
  integer, parameter :: TIM1_CR1_ADDR = 0x00
  integer, parameter :: TIM1_CR2_ADDR = 0x04
  integer, parameter :: TIM1_SMCR_ADDR = 0x08
  integer, parameter :: TIM1_DIER_ADDR = 0x0C
  integer, parameter :: TIM1_SR_ADDR = 0x10
  integer, parameter :: TIM1_EGR_ADDR = 0x14
  integer, parameter :: TIM1_CCMR1_ADDR = 0x18
  integer, parameter :: TIM1_CCMR2_ADDR = 0x1C
  integer, parameter :: TIM1_CCER_ADDR = 0x20
  integer, parameter :: TIM1_CNT_ADDR = 0x24
  integer, parameter :: TIM1_PSC_ADDR = 0x28
  integer, parameter :: TIM1_ARR_ADDR = 0x2C
  integer, parameter :: TIM1_RCR_ADDR = 0x30
  integer, parameter :: TIM1_CCR1_ADDR = 0x34
  integer, parameter :: TIM1_CCR2_ADDR = 0x38
  integer, parameter :: TIM1_CCR3_ADDR = 0x3C
  integer, parameter :: TIM1_CCR4_ADDR = 0x40
  integer, parameter :: TIM1_BDTR_ADDR = 0x44
  ! General Purpose Timer 2
  integer, parameter :: TIM2_BASE = 0x40000400
  integer, parameter :: TIM2_CR1_ADDR = 0x00
  integer, parameter :: TIM2_CNT_ADDR = 0x24
  integer, parameter :: TIM2_PSC_ADDR = 0x28
  integer, parameter :: TIM2_ARR_ADDR = 0x2C
  integer, parameter :: TIM2_CCR1_ADDR = 0x34
  integer, parameter :: TIM2_CCR2_ADDR = 0x38
  integer, parameter :: TIM2_CCR3_ADDR = 0x3C
  integer, parameter :: TIM2_CCR4_ADDR = 0x40
  ! General Purpose Timer 3
  integer, parameter :: TIM3_BASE = 0x40000400
  integer, parameter :: TIM3_CR1_ADDR = 0x00
  integer, parameter :: TIM3_CNT_ADDR = 0x24
  integer, parameter :: TIM3_ARR_ADDR = 0x2C
  integer, parameter :: TIM3_CCR1_ADDR = 0x34
  integer, parameter :: TIM3_CCR2_ADDR = 0x38
  integer, parameter :: TIM3_CCR3_ADDR = 0x3C
  integer, parameter :: TIM3_CCR4_ADDR = 0x40
  ! General Purpose Timer 4
  integer, parameter :: TIM4_BASE = 0x40000800
  integer, parameter :: TIM4_CR1_ADDR = 0x00
  integer, parameter :: TIM4_CNT_ADDR = 0x24
  integer, parameter :: TIM4_ARR_ADDR = 0x2C
  integer, parameter :: TIM4_CCR1_ADDR = 0x34
  integer, parameter :: TIM4_CCR2_ADDR = 0x38
  integer, parameter :: TIM4_CCR3_ADDR = 0x3C
  integer, parameter :: TIM4_CCR4_ADDR = 0x40
  ! ADC 1
  integer, parameter :: ADC1_BASE = 0x40012400
  integer, parameter :: ADC1_SR_ADDR = 0x00
  integer, parameter :: ADC1_CR1_ADDR = 0x04
  integer, parameter :: ADC1_CR2_ADDR = 0x08
  integer, parameter :: ADC1_SMPR1_ADDR = 0x0C
  integer, parameter :: ADC1_SMPR2_ADDR = 0x10
  integer, parameter :: ADC1_JOFR1_ADDR = 0x14
  integer, parameter :: ADC1_JOFR2_ADDR = 0x18
  integer, parameter :: ADC1_JOFR3_ADDR = 0x1C
  integer, parameter :: ADC1_JOFR4_ADDR = 0x20
  integer, parameter :: ADC1_HTR_ADDR = 0x24
  integer, parameter :: ADC1_LTR_ADDR = 0x28
  integer, parameter :: ADC1_SQRT1_ADDR = 0x2C
  integer, parameter :: ADC1_SQRT2_ADDR = 0x30
  integer, parameter :: ADC1_SQRT3_ADDR = 0x34
  integer, parameter :: ADC1_JSQR_ADDR = 0x38
  integer, parameter :: ADC1_JDR1_ADDR = 0x3C
  integer, parameter :: ADC1_JDR2_ADDR = 0x40
  integer, parameter :: ADC1_JDR3_ADDR = 0x44
  integer, parameter :: ADC1_JDR4_ADDR = 0x48
  integer, parameter :: ADC1_DR_ADDR = 0x4C
  ! DMA Controller 1
  integer, parameter :: DMA1_BASE = 0x40020000
  integer, parameter :: DMA1_ISR_ADDR = 0x00
  integer, parameter :: DMA1_IFCR_ADDR = 0x04
  integer, parameter :: DMA1_CCR1_ADDR = 0x08
  integer, parameter :: DMA1_CNDTR1_ADDR = 0x0C
  integer, parameter :: DMA1_CPAR1_ADDR = 0x10
  integer, parameter :: DMA1_CMAR1_ADDR = 0x14
  integer, parameter :: DMA1_CCR2_ADDR = 0x1C
  integer, parameter :: DMA1_CNDTR2_ADDR = 0x20
  integer, parameter :: DMA1_CPAR2_ADDR = 0x24
  integer, parameter :: DMA1_CMAR2_ADDR = 0x28
  ! Power Control
  integer, parameter :: PWR_BASE = 0x40007000
  integer, parameter :: PWR_CR_ADDR = 0x00
  integer, parameter :: PWR_CSR_ADDR = 0x04
  ! Backup Registers
  integer, parameter :: BKP_BASE = 0x40006C00
  integer, parameter :: BKP_DR1_ADDR = 0x04
  integer, parameter :: BKP_DR2_ADDR = 0x08
  integer, parameter :: BKP_CSR_ADDR = 0x2C
  ! Window Watchdog
  integer, parameter :: WWDG_BASE = 0x40002C00
  integer, parameter :: WWDG_CR_ADDR = 0x00
  integer, parameter :: WWDG_CFR_ADDR = 0x04
  integer, parameter :: WWDG_SR_ADDR = 0x08
  ! Independent Watchdog
  integer, parameter :: IWDG_BASE = 0x40003000
  integer, parameter :: IWDG_KR_ADDR = 0x00
  integer, parameter :: IWDG_PR_ADDR = 0x04
  integer, parameter :: IWDG_RLR_ADDR = 0x08
  ! External Interrupt/Event Controller
  integer, parameter :: EXTI_BASE = 0x40010400
  integer, parameter :: EXTI_IMR_ADDR = 0x00
  integer, parameter :: EXTI_EMR_ADDR = 0x04
  integer, parameter :: EXTI_RTSR_ADDR = 0x08
  integer, parameter :: EXTI_FTSR_ADDR = 0x0C
  integer, parameter :: EXTI_SWIER_ADDR = 0x10
  integer, parameter :: EXTI_PR_ADDR = 0x14
  ! Alternate Function IO
  integer, parameter :: AFIO_BASE = 0x40010000
  integer, parameter :: AFIO_EVCR_ADDR = 0x00
  integer, parameter :: AFIO_MAPR_ADDR = 0x04
  integer, parameter :: AFIO_EXTICR1_ADDR = 0x08
  integer, parameter :: AFIO_EXTICR2_ADDR = 0x0C
  integer, parameter :: AFIO_EXTICR3_ADDR = 0x10
  integer, parameter :: AFIO_MAPR2_ADDR = 0x1C

  ! 中断向量定义
  integer, parameter :: INT_WWDG = 0  ! Window Watchdog Interrupt
  integer, parameter :: INT_PVD = 1  ! PVD through EXTI Line detection
  integer, parameter :: INT_TAMPER = 2  ! Tamper Interrupt
  integer, parameter :: INT_RTC = 3  ! RTC Global Interrupt
  integer, parameter :: INT_FLASH = 4  ! FLASH Global Interrupt
  integer, parameter :: INT_RCC = 5  ! RCC Global Interrupt
  integer, parameter :: INT_EXTI0 = 6  ! EXTI Line 0 Interrupt
  integer, parameter :: INT_EXTI1 = 7  ! EXTI Line 1 Interrupt
  integer, parameter :: INT_EXTI2 = 8  ! EXTI Line 2 Interrupt
  integer, parameter :: INT_EXTI3 = 9  ! EXTI Line 3 Interrupt
  integer, parameter :: INT_EXTI4 = 10  ! EXTI Line 4 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL1 = 11  ! DMA1 Channel 1 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL2 = 12  ! DMA1 Channel 2 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL3 = 13  ! DMA1 Channel 3 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL4 = 14  ! DMA1 Channel 4 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL5 = 15  ! DMA1 Channel 5 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL6 = 16  ! DMA1 Channel 6 Interrupt
  integer, parameter :: INT_DMA1_CHANNEL7 = 17  ! DMA1 Channel 7 Interrupt
  integer, parameter :: INT_ADC1_2 = 18  ! ADC1 and ADC2 Global Interrupt
  integer, parameter :: INT_USB_HP_CAN_TX = 19  ! USB HP/CAN TX Interrupts
  integer, parameter :: INT_USB_LP_CAN_RX0 = 20  ! USB LP/CAN RX0 Interrupt
  integer, parameter :: INT_CAN_RX1 = 21  ! CAN RX1 Interrupt
  integer, parameter :: INT_CAN_SCE = 22  ! CAN SCE Interrupt
  integer, parameter :: INT_EXTI9_5 = 23  ! EXTI Line 9..5 Interrupt
  integer, parameter :: INT_TIM1_BRK = 25  ! TIM1 Break Interrupt
  integer, parameter :: INT_TIM1_UP = 26  ! TIM1 Update Interrupt
  integer, parameter :: INT_TIM1_TRG_COM = 27  ! TIM1 Trigger and Commutation
  integer, parameter :: INT_TIM1_CC = 28  ! TIM1 Capture Compare Interrupt
  integer, parameter :: INT_TIM2 = 29  ! TIM2 Global Interrupt
  integer, parameter :: INT_TIM3 = 30  ! TIM3 Global Interrupt
  integer, parameter :: INT_TIM4 = 31  ! TIM4 Global Interrupt
  integer, parameter :: INT_I2C1_EV = 32  ! I2C1 Event Interrupt
  integer, parameter :: INT_I2C1_ER = 33  ! I2C1 Error Interrupt
  integer, parameter :: INT_I2C2_EV = 34  ! I2C2 Event Interrupt
  integer, parameter :: INT_I2C2_ER = 35  ! I2C2 Error Interrupt
  integer, parameter :: INT_SPI1 = 35  ! SPI1 Global Interrupt
  integer, parameter :: INT_SPI2 = 36  ! SPI2 Global Interrupt
  integer, parameter :: INT_USART1 = 37  ! USART1 Global Interrupt
  integer, parameter :: INT_USART2 = 38  ! USART2 Global Interrupt
  integer, parameter :: INT_USART3 = 39  ! USART3 Global Interrupt
  integer, parameter :: INT_EXTI15_10 = 40  ! EXTI Line 15..10 Interrupt
  integer, parameter :: INT_RTCALARM = 41  ! RTC Alarm through EXTI
  integer, parameter :: INT_USBWAKEUP = 42  ! USB Wakeup from suspend

  ! 引脚定义
  integer, parameter :: PIN_VBAT = 1  ! Battery Supply
  integer, parameter :: PIN_PC13 = 2  ! GPIO Port C Pin 13
  integer, parameter :: PIN_PC14 = 3  ! GPIO Port C Pin 14
  integer, parameter :: PIN_PC15 = 4  ! GPIO Port C Pin 15
  integer, parameter :: PIN_PD0 = 5  ! GPIO Port D Pin 0
  integer, parameter :: PIN_PD1 = 6  ! GPIO Port D Pin 1
  integer, parameter :: PIN_NRST = 7  ! Reset
  integer, parameter :: PIN_VSSA = 8  ! Analog Ground
  integer, parameter :: PIN_VDDA = 9  ! Analog Supply
  integer, parameter :: PIN_PA0 = 10  ! GPIO Port A Pin 0 / ADC1_IN0
  integer, parameter :: PIN_PA1 = 11  ! GPIO Port A Pin 1 / ADC1_IN1
  integer, parameter :: PIN_PA2 = 12  ! GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
  integer, parameter :: PIN_PA3 = 13  ! GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
  integer, parameter :: PIN_PA4 = 14  ! GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
  integer, parameter :: PIN_PA5 = 15  ! GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
  integer, parameter :: PIN_PA6 = 16  ! GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
  integer, parameter :: PIN_PA7 = 17  ! GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
  integer, parameter :: PIN_PB0 = 18  ! GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
  integer, parameter :: PIN_PB1 = 19  ! GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
  integer, parameter :: PIN_PB2 = 20  ! GPIO Port B Pin 2
  integer, parameter :: PIN_PB10 = 21  ! GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
  integer, parameter :: PIN_PB11 = 22  ! GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
  integer, parameter :: PIN_VSS = 23  ! Ground
  integer, parameter :: PIN_VDD = 24  ! Digital Supply
  integer, parameter :: PIN_PB12 = 25  ! GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
  integer, parameter :: PIN_PB13 = 26  ! GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
  integer, parameter :: PIN_PB14 = 27  ! GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
  integer, parameter :: PIN_PB15 = 28  ! GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
  integer, parameter :: PIN_PA8 = 29  ! GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
  integer, parameter :: PIN_PA9 = 30  ! GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
  integer, parameter :: PIN_PA10 = 31  ! GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
  integer, parameter :: PIN_PA11 = 32  ! GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
  integer, parameter :: PIN_PA12 = 33  ! GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
  integer, parameter :: PIN_PA13 = 34  ! JTMS/SWDIO
  integer, parameter :: PIN_PA14 = 37  ! JTCK/SWCLK
  integer, parameter :: PIN_PA15 = 38  ! GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
  integer, parameter :: PIN_PB3 = 39  ! GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
  integer, parameter :: PIN_PB4 = 40  ! GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
  integer, parameter :: PIN_PB5 = 41  ! GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
  integer, parameter :: PIN_PB6 = 42  ! GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
  integer, parameter :: PIN_PB7 = 43  ! GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
  integer, parameter :: PIN_BOOT0 = 44  ! Boot Selection
  integer, parameter :: PIN_PB8 = 45  ! GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
  integer, parameter :: PIN_PB9 = 46  ! GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
  integer, parameter :: PIN_VSS = 47  ! Ground
  integer, parameter :: PIN_VDD = 48  ! Digital Supply

end module stm32f103c8t6_device
