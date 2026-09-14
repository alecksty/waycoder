--[[
  STM32F103C8T6设备定义 - Lua模块
  生成自: STMicroelectronics/STM32/STM32F103C8T6
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
  CPU架构: ARM-Cortex-M3
  位宽: 32位
  时钟频率: 72000000 Hz
]]

local STM32F103C8T6 = {}

-- 设备信息
STM32F103C8T6.DEVICE_NAME = "STM32F103C8T6"
STM32F103C8T6.MANUFACTURER = "STMicroelectronics"
STM32F103C8T6.FAMILY = "STM32"
STM32F103C8T6.VERSION = "1.0"
STM32F103C8T6.ARCHITECTURE = "ARM-Cortex-M3"
STM32F103C8T6.BITS = 32
STM32F103C8T6.CLOCK_FREQUENCY = 72000000

-- 寄存器地址定义
STM32F103C8T6.R0_ADDR = 0x00  -- General Purpose Register 0
STM32F103C8T6.R1_ADDR = 0x04  -- General Purpose Register 1
STM32F103C8T6.R2_ADDR = 0x08  -- General Purpose Register 2
STM32F103C8T6.R3_ADDR = 0x0C  -- General Purpose Register 3
STM32F103C8T6.R4_ADDR = 0x10  -- General Purpose Register 4
STM32F103C8T6.R5_ADDR = 0x14  -- General Purpose Register 5
STM32F103C8T6.R6_ADDR = 0x18  -- General Purpose Register 6
STM32F103C8T6.R7_ADDR = 0x1C  -- General Purpose Register 7
STM32F103C8T6.R8_ADDR = 0x20  -- General Purpose Register 8
STM32F103C8T6.R9_ADDR = 0x24  -- General Purpose Register 9
STM32F103C8T6.R10_ADDR = 0x28  -- General Purpose Register 10
STM32F103C8T6.R11_ADDR = 0x2C  -- General Purpose Register 11
STM32F103C8T6.R12_ADDR = 0x30  -- General Purpose Register 12
STM32F103C8T6.SP_ADDR = 0x34  -- Stack Pointer
STM32F103C8T6.LR_ADDR = 0x38  -- Link Register
STM32F103C8T6.PC_ADDR = 0x3C  -- Program Counter
STM32F103C8T6.XPSR_ADDR = 0x40  -- Program Status Register

-- 内存段定义
STM32F103C8T6.FLASH_START = 0x08000000
STM32F103C8T6.FLASH_END = 0x0800FFFF
STM32F103C8T6.FLASH_SIZE = 65536  -- Main Flash Memory (64KB)
STM32F103C8T6.SYSTEM_MEMORY_START = 0x1FFFF000
STM32F103C8T6.SYSTEM_MEMORY_END = 0x1FFFF7FF
STM32F103C8T6.SYSTEM_MEMORY_SIZE = 2048  -- System Memory (2KB)
STM32F103C8T6.SRAM_START = 0x20000000
STM32F103C8T6.SRAM_END = 0x20004FFF
STM32F103C8T6.SRAM_SIZE = 20480  -- SRAM (20KB)
STM32F103C8T6.PERIPHERAL_START = 0x40000000
STM32F103C8T6.PERIPHERAL_END = 0x40023FFF
STM32F103C8T6.PERIPHERAL_SIZE = 143360  -- Peripheral Registers
STM32F103C8T6.CORTEX_M_START = 0xE0000000
STM32F103C8T6.CORTEX_M_END = 0xE00FFFFF
STM32F103C8T6.CORTEX_M_SIZE = 1048576  -- Core Peripheral Registers

-- 外设定义
-- Reset and Clock Control
STM32F103C8T6.RCC_BASE = 0x40021000
STM32F103C8T6.RCC_CR_ADDR = 0x00
STM32F103C8T6.RCC_CFGR_ADDR = 0x04
STM32F103C8T6.RCC_APB2ENR_ADDR = 0x18
STM32F103C8T6.RCC_APB1ENR_ADDR = 0x1C
-- GPIO Port A
STM32F103C8T6.GPIOA_BASE = 0x40010800
STM32F103C8T6.GPIOA_CRL_ADDR = 0x00
STM32F103C8T6.GPIOA_CRH_ADDR = 0x04
STM32F103C8T6.GPIOA_IDR_ADDR = 0x08
STM32F103C8T6.GPIOA_ODR_ADDR = 0x0C
STM32F103C8T6.GPIOA_BSRR_ADDR = 0x10
STM32F103C8T6.GPIOA_BRR_ADDR = 0x14
STM32F103C8T6.GPIOA_LCKR_ADDR = 0x18
-- GPIO Port B
STM32F103C8T6.GPIOB_BASE = 0x40010C00
STM32F103C8T6.GPIOB_CRL_ADDR = 0x00
STM32F103C8T6.GPIOB_CRH_ADDR = 0x04
STM32F103C8T6.GPIOB_IDR_ADDR = 0x08
STM32F103C8T6.GPIOB_ODR_ADDR = 0x0C
STM32F103C8T6.GPIOB_BSRR_ADDR = 0x10
STM32F103C8T6.GPIOB_BRR_ADDR = 0x14
-- GPIO Port C
STM32F103C8T6.GPIOC_BASE = 0x40011000
STM32F103C8T6.GPIOC_CRL_ADDR = 0x00
STM32F103C8T6.GPIOC_CRH_ADDR = 0x04
STM32F103C8T6.GPIOC_IDR_ADDR = 0x08
STM32F103C8T6.GPIOC_ODR_ADDR = 0x0C
STM32F103C8T6.GPIOC_BSRR_ADDR = 0x10
-- USART 1
STM32F103C8T6.USART1_BASE = 0x40013800
STM32F103C8T6.USART1_SR_ADDR = 0x00
STM32F103C8T6.USART1_DR_ADDR = 0x04
STM32F103C8T6.USART1_BRR_ADDR = 0x08
STM32F103C8T6.USART1_CR1_ADDR = 0x0C
STM32F103C8T6.USART1_CR2_ADDR = 0x10
STM32F103C8T6.USART1_CR3_ADDR = 0x14
STM32F103C8T6.USART1_GTPR_ADDR = 0x18
-- USART 2
STM32F103C8T6.USART2_BASE = 0x40004400
STM32F103C8T6.USART2_SR_ADDR = 0x00
STM32F103C8T6.USART2_DR_ADDR = 0x04
STM32F103C8T6.USART2_BRR_ADDR = 0x08
STM32F103C8T6.USART2_CR1_ADDR = 0x0C
-- SPI 1
STM32F103C8T6.SPI1_BASE = 0x40013000
STM32F103C8T6.SPI1_CR1_ADDR = 0x00
STM32F103C8T6.SPI1_CR2_ADDR = 0x04
STM32F103C8T6.SPI1_SR_ADDR = 0x08
STM32F103C8T6.SPI1_DR_ADDR = 0x0C
-- SPI 2
STM32F103C8T6.SPI2_BASE = 0x40003800
STM32F103C8T6.SPI2_CR1_ADDR = 0x00
STM32F103C8T6.SPI2_CR2_ADDR = 0x04
STM32F103C8T6.SPI2_SR_ADDR = 0x08
STM32F103C8T6.SPI2_DR_ADDR = 0x0C
-- I2C 1
STM32F103C8T6.I2C1_BASE = 0x40005400
STM32F103C8T6.I2C1_CR1_ADDR = 0x00
STM32F103C8T6.I2C1_CR2_ADDR = 0x04
STM32F103C8T6.I2C1_SR1_ADDR = 0x08
STM32F103C8T6.I2C1_SR2_ADDR = 0x0C
STM32F103C8T6.I2C1_DR_ADDR = 0x10
STM32F103C8T6.I2C1_CCR_ADDR = 0x14
STM32F103C8T6.I2C1_TRISE_ADDR = 0x18
-- I2C 2
STM32F103C8T6.I2C2_BASE = 0x40005800
STM32F103C8T6.I2C2_CR1_ADDR = 0x00
STM32F103C8T6.I2C2_CR2_ADDR = 0x04
STM32F103C8T6.I2C2_SR1_ADDR = 0x08
STM32F103C8T6.I2C2_SR2_ADDR = 0x0C
STM32F103C8T6.I2C2_DR_ADDR = 0x10
STM32F103C8T6.I2C2_CCR_ADDR = 0x14
-- Advanced Timer 1
STM32F103C8T6.TIM1_BASE = 0x40012C00
STM32F103C8T6.TIM1_CR1_ADDR = 0x00
STM32F103C8T6.TIM1_CR2_ADDR = 0x04
STM32F103C8T6.TIM1_SMCR_ADDR = 0x08
STM32F103C8T6.TIM1_DIER_ADDR = 0x0C
STM32F103C8T6.TIM1_SR_ADDR = 0x10
STM32F103C8T6.TIM1_EGR_ADDR = 0x14
STM32F103C8T6.TIM1_CCMR1_ADDR = 0x18
STM32F103C8T6.TIM1_CCMR2_ADDR = 0x1C
STM32F103C8T6.TIM1_CCER_ADDR = 0x20
STM32F103C8T6.TIM1_CNT_ADDR = 0x24
STM32F103C8T6.TIM1_PSC_ADDR = 0x28
STM32F103C8T6.TIM1_ARR_ADDR = 0x2C
STM32F103C8T6.TIM1_RCR_ADDR = 0x30
STM32F103C8T6.TIM1_CCR1_ADDR = 0x34
STM32F103C8T6.TIM1_CCR2_ADDR = 0x38
STM32F103C8T6.TIM1_CCR3_ADDR = 0x3C
STM32F103C8T6.TIM1_CCR4_ADDR = 0x40
STM32F103C8T6.TIM1_BDTR_ADDR = 0x44
-- General Purpose Timer 2
STM32F103C8T6.TIM2_BASE = 0x40000400
STM32F103C8T6.TIM2_CR1_ADDR = 0x00
STM32F103C8T6.TIM2_CNT_ADDR = 0x24
STM32F103C8T6.TIM2_PSC_ADDR = 0x28
STM32F103C8T6.TIM2_ARR_ADDR = 0x2C
STM32F103C8T6.TIM2_CCR1_ADDR = 0x34
STM32F103C8T6.TIM2_CCR2_ADDR = 0x38
STM32F103C8T6.TIM2_CCR3_ADDR = 0x3C
STM32F103C8T6.TIM2_CCR4_ADDR = 0x40
-- General Purpose Timer 3
STM32F103C8T6.TIM3_BASE = 0x40000400
STM32F103C8T6.TIM3_CR1_ADDR = 0x00
STM32F103C8T6.TIM3_CNT_ADDR = 0x24
STM32F103C8T6.TIM3_ARR_ADDR = 0x2C
STM32F103C8T6.TIM3_CCR1_ADDR = 0x34
STM32F103C8T6.TIM3_CCR2_ADDR = 0x38
STM32F103C8T6.TIM3_CCR3_ADDR = 0x3C
STM32F103C8T6.TIM3_CCR4_ADDR = 0x40
-- General Purpose Timer 4
STM32F103C8T6.TIM4_BASE = 0x40000800
STM32F103C8T6.TIM4_CR1_ADDR = 0x00
STM32F103C8T6.TIM4_CNT_ADDR = 0x24
STM32F103C8T6.TIM4_ARR_ADDR = 0x2C
STM32F103C8T6.TIM4_CCR1_ADDR = 0x34
STM32F103C8T6.TIM4_CCR2_ADDR = 0x38
STM32F103C8T6.TIM4_CCR3_ADDR = 0x3C
STM32F103C8T6.TIM4_CCR4_ADDR = 0x40
-- ADC 1
STM32F103C8T6.ADC1_BASE = 0x40012400
STM32F103C8T6.ADC1_SR_ADDR = 0x00
STM32F103C8T6.ADC1_CR1_ADDR = 0x04
STM32F103C8T6.ADC1_CR2_ADDR = 0x08
STM32F103C8T6.ADC1_SMPR1_ADDR = 0x0C
STM32F103C8T6.ADC1_SMPR2_ADDR = 0x10
STM32F103C8T6.ADC1_JOFR1_ADDR = 0x14
STM32F103C8T6.ADC1_JOFR2_ADDR = 0x18
STM32F103C8T6.ADC1_JOFR3_ADDR = 0x1C
STM32F103C8T6.ADC1_JOFR4_ADDR = 0x20
STM32F103C8T6.ADC1_HTR_ADDR = 0x24
STM32F103C8T6.ADC1_LTR_ADDR = 0x28
STM32F103C8T6.ADC1_SQRT1_ADDR = 0x2C
STM32F103C8T6.ADC1_SQRT2_ADDR = 0x30
STM32F103C8T6.ADC1_SQRT3_ADDR = 0x34
STM32F103C8T6.ADC1_JSQR_ADDR = 0x38
STM32F103C8T6.ADC1_JDR1_ADDR = 0x3C
STM32F103C8T6.ADC1_JDR2_ADDR = 0x40
STM32F103C8T6.ADC1_JDR3_ADDR = 0x44
STM32F103C8T6.ADC1_JDR4_ADDR = 0x48
STM32F103C8T6.ADC1_DR_ADDR = 0x4C
-- DMA Controller 1
STM32F103C8T6.DMA1_BASE = 0x40020000
STM32F103C8T6.DMA1_ISR_ADDR = 0x00
STM32F103C8T6.DMA1_IFCR_ADDR = 0x04
STM32F103C8T6.DMA1_CCR1_ADDR = 0x08
STM32F103C8T6.DMA1_CNDTR1_ADDR = 0x0C
STM32F103C8T6.DMA1_CPAR1_ADDR = 0x10
STM32F103C8T6.DMA1_CMAR1_ADDR = 0x14
STM32F103C8T6.DMA1_CCR2_ADDR = 0x1C
STM32F103C8T6.DMA1_CNDTR2_ADDR = 0x20
STM32F103C8T6.DMA1_CPAR2_ADDR = 0x24
STM32F103C8T6.DMA1_CMAR2_ADDR = 0x28
-- Power Control
STM32F103C8T6.PWR_BASE = 0x40007000
STM32F103C8T6.PWR_CR_ADDR = 0x00
STM32F103C8T6.PWR_CSR_ADDR = 0x04
-- Backup Registers
STM32F103C8T6.BKP_BASE = 0x40006C00
STM32F103C8T6.BKP_DR1_ADDR = 0x04
STM32F103C8T6.BKP_DR2_ADDR = 0x08
STM32F103C8T6.BKP_CSR_ADDR = 0x2C
-- Window Watchdog
STM32F103C8T6.WWDG_BASE = 0x40002C00
STM32F103C8T6.WWDG_CR_ADDR = 0x00
STM32F103C8T6.WWDG_CFR_ADDR = 0x04
STM32F103C8T6.WWDG_SR_ADDR = 0x08
-- Independent Watchdog
STM32F103C8T6.IWDG_BASE = 0x40003000
STM32F103C8T6.IWDG_KR_ADDR = 0x00
STM32F103C8T6.IWDG_PR_ADDR = 0x04
STM32F103C8T6.IWDG_RLR_ADDR = 0x08
-- External Interrupt/Event Controller
STM32F103C8T6.EXTI_BASE = 0x40010400
STM32F103C8T6.EXTI_IMR_ADDR = 0x00
STM32F103C8T6.EXTI_EMR_ADDR = 0x04
STM32F103C8T6.EXTI_RTSR_ADDR = 0x08
STM32F103C8T6.EXTI_FTSR_ADDR = 0x0C
STM32F103C8T6.EXTI_SWIER_ADDR = 0x10
STM32F103C8T6.EXTI_PR_ADDR = 0x14
-- Alternate Function IO
STM32F103C8T6.AFIO_BASE = 0x40010000
STM32F103C8T6.AFIO_EVCR_ADDR = 0x00
STM32F103C8T6.AFIO_MAPR_ADDR = 0x04
STM32F103C8T6.AFIO_EXTICR1_ADDR = 0x08
STM32F103C8T6.AFIO_EXTICR2_ADDR = 0x0C
STM32F103C8T6.AFIO_EXTICR3_ADDR = 0x10
STM32F103C8T6.AFIO_MAPR2_ADDR = 0x1C

-- 中断向量定义
STM32F103C8T6.INT_WWDG = 0  -- Window Watchdog Interrupt
STM32F103C8T6.INT_PVD = 1  -- PVD through EXTI Line detection
STM32F103C8T6.INT_TAMPER = 2  -- Tamper Interrupt
STM32F103C8T6.INT_RTC = 3  -- RTC Global Interrupt
STM32F103C8T6.INT_FLASH = 4  -- FLASH Global Interrupt
STM32F103C8T6.INT_RCC = 5  -- RCC Global Interrupt
STM32F103C8T6.INT_EXTI0 = 6  -- EXTI Line 0 Interrupt
STM32F103C8T6.INT_EXTI1 = 7  -- EXTI Line 1 Interrupt
STM32F103C8T6.INT_EXTI2 = 8  -- EXTI Line 2 Interrupt
STM32F103C8T6.INT_EXTI3 = 9  -- EXTI Line 3 Interrupt
STM32F103C8T6.INT_EXTI4 = 10  -- EXTI Line 4 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL1 = 11  -- DMA1 Channel 1 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL2 = 12  -- DMA1 Channel 2 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL3 = 13  -- DMA1 Channel 3 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL4 = 14  -- DMA1 Channel 4 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL5 = 15  -- DMA1 Channel 5 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL6 = 16  -- DMA1 Channel 6 Interrupt
STM32F103C8T6.INT_DMA1_CHANNEL7 = 17  -- DMA1 Channel 7 Interrupt
STM32F103C8T6.INT_ADC1_2 = 18  -- ADC1 and ADC2 Global Interrupt
STM32F103C8T6.INT_USB_HP_CAN_TX = 19  -- USB HP/CAN TX Interrupts
STM32F103C8T6.INT_USB_LP_CAN_RX0 = 20  -- USB LP/CAN RX0 Interrupt
STM32F103C8T6.INT_CAN_RX1 = 21  -- CAN RX1 Interrupt
STM32F103C8T6.INT_CAN_SCE = 22  -- CAN SCE Interrupt
STM32F103C8T6.INT_EXTI9_5 = 23  -- EXTI Line 9..5 Interrupt
STM32F103C8T6.INT_TIM1_BRK = 25  -- TIM1 Break Interrupt
STM32F103C8T6.INT_TIM1_UP = 26  -- TIM1 Update Interrupt
STM32F103C8T6.INT_TIM1_TRG_COM = 27  -- TIM1 Trigger and Commutation
STM32F103C8T6.INT_TIM1_CC = 28  -- TIM1 Capture Compare Interrupt
STM32F103C8T6.INT_TIM2 = 29  -- TIM2 Global Interrupt
STM32F103C8T6.INT_TIM3 = 30  -- TIM3 Global Interrupt
STM32F103C8T6.INT_TIM4 = 31  -- TIM4 Global Interrupt
STM32F103C8T6.INT_I2C1_EV = 32  -- I2C1 Event Interrupt
STM32F103C8T6.INT_I2C1_ER = 33  -- I2C1 Error Interrupt
STM32F103C8T6.INT_I2C2_EV = 34  -- I2C2 Event Interrupt
STM32F103C8T6.INT_I2C2_ER = 35  -- I2C2 Error Interrupt
STM32F103C8T6.INT_SPI1 = 35  -- SPI1 Global Interrupt
STM32F103C8T6.INT_SPI2 = 36  -- SPI2 Global Interrupt
STM32F103C8T6.INT_USART1 = 37  -- USART1 Global Interrupt
STM32F103C8T6.INT_USART2 = 38  -- USART2 Global Interrupt
STM32F103C8T6.INT_USART3 = 39  -- USART3 Global Interrupt
STM32F103C8T6.INT_EXTI15_10 = 40  -- EXTI Line 15..10 Interrupt
STM32F103C8T6.INT_RTCALARM = 41  -- RTC Alarm through EXTI
STM32F103C8T6.INT_USBWAKEUP = 42  -- USB Wakeup from suspend

-- 引脚定义
STM32F103C8T6.PIN_VBAT = 1  -- Battery Supply
STM32F103C8T6.PIN_PC13 = 2  -- GPIO Port C Pin 13
STM32F103C8T6.PIN_PC14 = 3  -- GPIO Port C Pin 14
STM32F103C8T6.PIN_PC15 = 4  -- GPIO Port C Pin 15
STM32F103C8T6.PIN_PD0 = 5  -- GPIO Port D Pin 0
STM32F103C8T6.PIN_PD1 = 6  -- GPIO Port D Pin 1
STM32F103C8T6.PIN_NRST = 7  -- Reset
STM32F103C8T6.PIN_VSSA = 8  -- Analog Ground
STM32F103C8T6.PIN_VDDA = 9  -- Analog Supply
STM32F103C8T6.PIN_PA0 = 10  -- GPIO Port A Pin 0 / ADC1_IN0
STM32F103C8T6.PIN_PA1 = 11  -- GPIO Port A Pin 1 / ADC1_IN1
STM32F103C8T6.PIN_PA2 = 12  -- GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
STM32F103C8T6.PIN_PA3 = 13  -- GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
STM32F103C8T6.PIN_PA4 = 14  -- GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
STM32F103C8T6.PIN_PA5 = 15  -- GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
STM32F103C8T6.PIN_PA6 = 16  -- GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
STM32F103C8T6.PIN_PA7 = 17  -- GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
STM32F103C8T6.PIN_PB0 = 18  -- GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
STM32F103C8T6.PIN_PB1 = 19  -- GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
STM32F103C8T6.PIN_PB2 = 20  -- GPIO Port B Pin 2
STM32F103C8T6.PIN_PB10 = 21  -- GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
STM32F103C8T6.PIN_PB11 = 22  -- GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
STM32F103C8T6.PIN_VSS = 23  -- Ground
STM32F103C8T6.PIN_VDD = 24  -- Digital Supply
STM32F103C8T6.PIN_PB12 = 25  -- GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
STM32F103C8T6.PIN_PB13 = 26  -- GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
STM32F103C8T6.PIN_PB14 = 27  -- GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
STM32F103C8T6.PIN_PB15 = 28  -- GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
STM32F103C8T6.PIN_PA8 = 29  -- GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
STM32F103C8T6.PIN_PA9 = 30  -- GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
STM32F103C8T6.PIN_PA10 = 31  -- GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
STM32F103C8T6.PIN_PA11 = 32  -- GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
STM32F103C8T6.PIN_PA12 = 33  -- GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
STM32F103C8T6.PIN_PA13 = 34  -- JTMS/SWDIO
STM32F103C8T6.PIN_PA14 = 37  -- JTCK/SWCLK
STM32F103C8T6.PIN_PA15 = 38  -- GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
STM32F103C8T6.PIN_PB3 = 39  -- GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
STM32F103C8T6.PIN_PB4 = 40  -- GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
STM32F103C8T6.PIN_PB5 = 41  -- GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
STM32F103C8T6.PIN_PB6 = 42  -- GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
STM32F103C8T6.PIN_PB7 = 43  -- GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
STM32F103C8T6.PIN_BOOT0 = 44  -- Boot Selection
STM32F103C8T6.PIN_PB8 = 45  -- GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
STM32F103C8T6.PIN_PB9 = 46  -- GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
STM32F103C8T6.PIN_VSS = 47  -- Ground
STM32F103C8T6.PIN_VDD = 48  -- Digital Supply

-- 设备类
function STM32F103C8T6.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "General Purpose Register 1",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "General Purpose Register 9",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "General Purpose Register 10",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 11",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "General Purpose Register 12",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Link Register",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["xPSR"] = {
            address = 0x40,
            size = 4,
            access = "rw",
            description = "Program Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["RCC"] = {
            base = 0x40021000,
            type = "clock",
            description = "Reset and Clock Control",
            registers = {}
        }
        
        local p = self.peripherals["RCC"]
        p.registers["CR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["APB2ENR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["APB1ENR"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x40010800,
            type = "gpio",
            description = "GPIO Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIOA"]
        p.registers["CRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CRH"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["LCKR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOB"] = {
            base = 0x40010C00,
            type = "gpio",
            description = "GPIO Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIOB"]
        p.registers["CRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CRH"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOC"] = {
            base = 0x40011000,
            type = "gpio",
            description = "GPIO Port C",
            registers = {}
        }
        
        local p = self.peripherals["GPIOC"]
        p.registers["CRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CRH"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        self.peripherals["USART1"] = {
            base = 0x40013800,
            type = "uart",
            description = "USART 1",
            registers = {}
        }
        
        local p = self.peripherals["USART1"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CR3"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["GTPR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["USART2"] = {
            base = 0x40004400,
            type = "uart",
            description = "USART 2",
            registers = {}
        }
        
        local p = self.peripherals["USART2"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["SPI1"] = {
            base = 0x40013000,
            type = "spi",
            description = "SPI 1",
            registers = {}
        }
        
        local p = self.peripherals["SPI1"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["SPI2"] = {
            base = 0x40003800,
            type = "spi",
            description = "SPI 2",
            registers = {}
        }
        
        local p = self.peripherals["SPI2"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["I2C1"] = {
            base = 0x40005400,
            type = "i2c",
            description = "I2C 1",
            registers = {}
        }
        
        local p = self.peripherals["I2C1"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SR1"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SR2"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["TRISE"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["I2C2"] = {
            base = 0x40005800,
            type = "i2c",
            description = "I2C 2",
            registers = {}
        }
        
        local p = self.peripherals["I2C2"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SR1"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SR2"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["TIM1"] = {
            base = 0x40012C00,
            type = "timer",
            description = "Advanced Timer 1",
            registers = {}
        }
        
        local p = self.peripherals["TIM1"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SMCR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["DIER"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["EGR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["CCMR1"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["CCMR2"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["CCER"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["PSC"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["ARR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["RCR"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["CCR3"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["CCR4"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        p.registers["BDTR"] = {
            address = 0x44,
            size = 4,
            value = 0
        }
        self.peripherals["TIM2"] = {
            base = 0x40000400,
            type = "timer",
            description = "General Purpose Timer 2",
            registers = {}
        }
        
        local p = self.peripherals["TIM2"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["PSC"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["ARR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["CCR3"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["CCR4"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        self.peripherals["TIM3"] = {
            base = 0x40000400,
            type = "timer",
            description = "General Purpose Timer 3",
            registers = {}
        }
        
        local p = self.peripherals["TIM3"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["ARR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["CCR3"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["CCR4"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        self.peripherals["TIM4"] = {
            base = 0x40000800,
            type = "timer",
            description = "General Purpose Timer 4",
            registers = {}
        }
        
        local p = self.peripherals["TIM4"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["ARR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["CCR3"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["CCR4"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        self.peripherals["ADC1"] = {
            base = 0x40012400,
            type = "adc",
            description = "ADC 1",
            registers = {}
        }
        
        local p = self.peripherals["ADC1"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SMPR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["SMPR2"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["JOFR1"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["JOFR2"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["JOFR3"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["JOFR4"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["HTR"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["LTR"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["SQRT1"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["SQRT2"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        p.registers["SQRT3"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["JSQR"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["JDR1"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["JDR2"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        p.registers["JDR3"] = {
            address = 0x44,
            size = 4,
            value = 0
        }
        p.registers["JDR4"] = {
            address = 0x48,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x4C,
            size = 4,
            value = 0
        }
        self.peripherals["DMA1"] = {
            base = 0x40020000,
            type = "dma",
            description = "DMA Controller 1",
            registers = {}
        }
        
        local p = self.peripherals["DMA1"]
        p.registers["ISR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["IFCR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CNDTR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["CPAR1"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CMAR1"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["CNDTR2"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["CPAR2"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["CMAR2"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        self.peripherals["PWR"] = {
            base = 0x40007000,
            type = "power",
            description = "Power Control",
            registers = {}
        }
        
        local p = self.peripherals["PWR"]
        p.registers["CR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CSR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        self.peripherals["BKP"] = {
            base = 0x40006C00,
            type = "backup",
            description = "Backup Registers",
            registers = {}
        }
        
        local p = self.peripherals["BKP"]
        p.registers["DR1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["DR2"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CSR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        self.peripherals["WWDG"] = {
            base = 0x40002C00,
            type = "watchdog",
            description = "Window Watchdog",
            registers = {}
        }
        
        local p = self.peripherals["WWDG"]
        p.registers["CR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        self.peripherals["IWDG"] = {
            base = 0x40003000,
            type = "watchdog",
            description = "Independent Watchdog",
            registers = {}
        }
        
        local p = self.peripherals["IWDG"]
        p.registers["KR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["RLR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        self.peripherals["EXTI"] = {
            base = 0x40010400,
            type = "exti",
            description = "External Interrupt/Event Controller",
            registers = {}
        }
        
        local p = self.peripherals["EXTI"]
        p.registers["IMR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["EMR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["RTSR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["FTSR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["SWIER"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["AFIO"] = {
            base = 0x40010000,
            type = "gpio",
            description = "Alternate Function IO",
            registers = {}
        }
        
        local p = self.peripherals["AFIO"]
        p.registers["EVCR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["MAPR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["EXTICR1"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["EXTICR2"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["EXTICR3"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["MAPR2"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = STM32F103C8T6.DEVICE_NAME,
            manufacturer = STM32F103C8T6.MANUFACTURER,
            family = STM32F103C8T6.FAMILY,
            version = STM32F103C8T6.VERSION,
            architecture = STM32F103C8T6.ARCHITECTURE,
            bits = STM32F103C8T6.BITS,
            clock_frequency = STM32F103C8T6.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("STM32F103C8T6(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function STM32F103C8T6.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function STM32F103C8T6.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function STM32F103C8T6.print_device_info(device)
    device = device or STM32F103C8T6.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function STM32F103C8T6.print_registers(device)
    device = device or STM32F103C8T6.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            STM32F103C8T6.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function STM32F103C8T6.example()
    print("=== STM32F103C8T6设备示例 ===")
    
    -- 创建设备实例
    local device = STM32F103C8T6.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    STM32F103C8T6.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. STM32F103C8T6.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. STM32F103C8T6.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    STM32F103C8T6.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("STM32F103C8T6.lua$") then
    STM32F103C8T6.example()
end

return STM32F103C8T6
