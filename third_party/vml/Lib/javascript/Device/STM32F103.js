/**
 * STM32F103C8T6 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32F103C8T6
 * 版本: 1.0
 */
export const stm32f103c8t6 = {
  // CPU: ARM-Cortex-M3, 32位, 72000000 Hz

  // 寄存器定义
  // General Purpose Register 0
  R0: 0x00,
  // General Purpose Register 1
  R1: 0x04,
  // General Purpose Register 2
  R2: 0x08,
  // General Purpose Register 3
  R3: 0x0C,
  // General Purpose Register 4
  R4: 0x10,
  // General Purpose Register 5
  R5: 0x14,
  // General Purpose Register 6
  R6: 0x18,
  // General Purpose Register 7
  R7: 0x1C,
  // General Purpose Register 8
  R8: 0x20,
  // General Purpose Register 9
  R9: 0x24,
  // General Purpose Register 10
  R10: 0x28,
  // General Purpose Register 11
  R11: 0x2C,
  // General Purpose Register 12
  R12: 0x30,
  // Stack Pointer
  SP: 0x34,
  // Link Register
  LR: 0x38,
  // Program Counter
  PC: 0x3C,
  // Program Status Register
  xPSR: 0x40,

  // 内存段
  // Main Flash Memory (64KB)
  flash_START: 0x08000000,
  flash_END: 0x0800FFFF,
  flash_SIZE: 65536,
  // System Memory (2KB)
  system_memory_START: 0x1FFFF000,
  system_memory_END: 0x1FFFF7FF,
  system_memory_SIZE: 2048,
  // SRAM (20KB)
  sram_START: 0x20000000,
  sram_END: 0x20004FFF,
  sram_SIZE: 20480,
  // Peripheral Registers
  peripheral_START: 0x40000000,
  peripheral_END: 0x40023FFF,
  peripheral_SIZE: 143360,
  // Core Peripheral Registers
  cortex_m_START: 0xE0000000,
  cortex_m_END: 0xE00FFFFF,
  cortex_m_SIZE: 1048576,

  // 外设定义
  // Reset and Clock Control
  RCC_BASE: 0x40021000,
  RCC_CR: 0x40021000,
  RCC_CFGR: 0x40021004,
  RCC_APB2ENR: 0x40021018,
  RCC_APB1ENR: 0x4002101C,
  // GPIO Port A
  GPIOA_BASE: 0x40010800,
  GPIOA_CRL: 0x40010800,
  GPIOA_CRH: 0x40010804,
  GPIOA_IDR: 0x40010808,
  GPIOA_ODR: 0x4001080C,
  GPIOA_BSRR: 0x40010810,
  GPIOA_BRR: 0x40010814,
  GPIOA_LCKR: 0x40010818,
  // GPIO Port B
  GPIOB_BASE: 0x40010C00,
  GPIOB_CRL: 0x40010C00,
  GPIOB_CRH: 0x40010C04,
  GPIOB_IDR: 0x40010C08,
  GPIOB_ODR: 0x40010C0C,
  GPIOB_BSRR: 0x40010C10,
  GPIOB_BRR: 0x40010C14,
  // GPIO Port C
  GPIOC_BASE: 0x40011000,
  GPIOC_CRL: 0x40011000,
  GPIOC_CRH: 0x40011004,
  GPIOC_IDR: 0x40011008,
  GPIOC_ODR: 0x4001100C,
  GPIOC_BSRR: 0x40011010,
  // USART 1
  USART1_BASE: 0x40013800,
  USART1_SR: 0x40013800,
  USART1_DR: 0x40013804,
  USART1_BRR: 0x40013808,
  USART1_CR1: 0x4001380C,
  USART1_CR2: 0x40013810,
  USART1_CR3: 0x40013814,
  USART1_GTPR: 0x40013818,
  // USART 2
  USART2_BASE: 0x40004400,
  USART2_SR: 0x40004400,
  USART2_DR: 0x40004404,
  USART2_BRR: 0x40004408,
  USART2_CR1: 0x4000440C,
  // SPI 1
  SPI1_BASE: 0x40013000,
  SPI1_CR1: 0x40013000,
  SPI1_CR2: 0x40013004,
  SPI1_SR: 0x40013008,
  SPI1_DR: 0x4001300C,
  // SPI 2
  SPI2_BASE: 0x40003800,
  SPI2_CR1: 0x40003800,
  SPI2_CR2: 0x40003804,
  SPI2_SR: 0x40003808,
  SPI2_DR: 0x4000380C,
  // I2C 1
  I2C1_BASE: 0x40005400,
  I2C1_CR1: 0x40005400,
  I2C1_CR2: 0x40005404,
  I2C1_SR1: 0x40005408,
  I2C1_SR2: 0x4000540C,
  I2C1_DR: 0x40005410,
  I2C1_CCR: 0x40005414,
  I2C1_TRISE: 0x40005418,
  // I2C 2
  I2C2_BASE: 0x40005800,
  I2C2_CR1: 0x40005800,
  I2C2_CR2: 0x40005804,
  I2C2_SR1: 0x40005808,
  I2C2_SR2: 0x4000580C,
  I2C2_DR: 0x40005810,
  I2C2_CCR: 0x40005814,
  // Advanced Timer 1
  TIM1_BASE: 0x40012C00,
  TIM1_CR1: 0x40012C00,
  TIM1_CR2: 0x40012C04,
  TIM1_SMCR: 0x40012C08,
  TIM1_DIER: 0x40012C0C,
  TIM1_SR: 0x40012C10,
  TIM1_EGR: 0x40012C14,
  TIM1_CCMR1: 0x40012C18,
  TIM1_CCMR2: 0x40012C1C,
  TIM1_CCER: 0x40012C20,
  TIM1_CNT: 0x40012C24,
  TIM1_PSC: 0x40012C28,
  TIM1_ARR: 0x40012C2C,
  TIM1_RCR: 0x40012C30,
  TIM1_CCR1: 0x40012C34,
  TIM1_CCR2: 0x40012C38,
  TIM1_CCR3: 0x40012C3C,
  TIM1_CCR4: 0x40012C40,
  TIM1_BDTR: 0x40012C44,
  // General Purpose Timer 2
  TIM2_BASE: 0x40000400,
  TIM2_CR1: 0x40000400,
  TIM2_CNT: 0x40000424,
  TIM2_PSC: 0x40000428,
  TIM2_ARR: 0x4000042C,
  TIM2_CCR1: 0x40000434,
  TIM2_CCR2: 0x40000438,
  TIM2_CCR3: 0x4000043C,
  TIM2_CCR4: 0x40000440,
  // General Purpose Timer 3
  TIM3_BASE: 0x40000400,
  TIM3_CR1: 0x40000400,
  TIM3_CNT: 0x40000424,
  TIM3_ARR: 0x4000042C,
  TIM3_CCR1: 0x40000434,
  TIM3_CCR2: 0x40000438,
  TIM3_CCR3: 0x4000043C,
  TIM3_CCR4: 0x40000440,
  // General Purpose Timer 4
  TIM4_BASE: 0x40000800,
  TIM4_CR1: 0x40000800,
  TIM4_CNT: 0x40000824,
  TIM4_ARR: 0x4000082C,
  TIM4_CCR1: 0x40000834,
  TIM4_CCR2: 0x40000838,
  TIM4_CCR3: 0x4000083C,
  TIM4_CCR4: 0x40000840,
  // ADC 1
  ADC1_BASE: 0x40012400,
  ADC1_SR: 0x40012400,
  ADC1_CR1: 0x40012404,
  ADC1_CR2: 0x40012408,
  ADC1_SMPR1: 0x4001240C,
  ADC1_SMPR2: 0x40012410,
  ADC1_JOFR1: 0x40012414,
  ADC1_JOFR2: 0x40012418,
  ADC1_JOFR3: 0x4001241C,
  ADC1_JOFR4: 0x40012420,
  ADC1_HTR: 0x40012424,
  ADC1_LTR: 0x40012428,
  ADC1_SQRT1: 0x4001242C,
  ADC1_SQRT2: 0x40012430,
  ADC1_SQRT3: 0x40012434,
  ADC1_JSQR: 0x40012438,
  ADC1_JDR1: 0x4001243C,
  ADC1_JDR2: 0x40012440,
  ADC1_JDR3: 0x40012444,
  ADC1_JDR4: 0x40012448,
  ADC1_DR: 0x4001244C,
  // DMA Controller 1
  DMA1_BASE: 0x40020000,
  DMA1_ISR: 0x40020000,
  DMA1_IFCR: 0x40020004,
  DMA1_CCR1: 0x40020008,
  DMA1_CNDTR1: 0x4002000C,
  DMA1_CPAR1: 0x40020010,
  DMA1_CMAR1: 0x40020014,
  DMA1_CCR2: 0x4002001C,
  DMA1_CNDTR2: 0x40020020,
  DMA1_CPAR2: 0x40020024,
  DMA1_CMAR2: 0x40020028,
  // Power Control
  PWR_BASE: 0x40007000,
  PWR_CR: 0x40007000,
  PWR_CSR: 0x40007004,
  // Backup Registers
  BKP_BASE: 0x40006C00,
  BKP_DR1: 0x40006C04,
  BKP_DR2: 0x40006C08,
  BKP_CSR: 0x40006C2C,
  // Window Watchdog
  WWDG_BASE: 0x40002C00,
  WWDG_CR: 0x40002C00,
  WWDG_CFR: 0x40002C04,
  WWDG_SR: 0x40002C08,
  // Independent Watchdog
  IWDG_BASE: 0x40003000,
  IWDG_KR: 0x40003000,
  IWDG_PR: 0x40003004,
  IWDG_RLR: 0x40003008,
  // External Interrupt/Event Controller
  EXTI_BASE: 0x40010400,
  EXTI_IMR: 0x40010400,
  EXTI_EMR: 0x40010404,
  EXTI_RTSR: 0x40010408,
  EXTI_FTSR: 0x4001040C,
  EXTI_SWIER: 0x40010410,
  EXTI_PR: 0x40010414,
  // Alternate Function IO
  AFIO_BASE: 0x40010000,
  AFIO_EVCR: 0x40010000,
  AFIO_MAPR: 0x40010004,
  AFIO_EXTICR1: 0x40010008,
  AFIO_EXTICR2: 0x4001000C,
  AFIO_EXTICR3: 0x40010010,
  AFIO_MAPR2: 0x4001001C,

  // 中断向量
  IRQ_WWDG: 0,  // Window Watchdog Interrupt
  IRQ_PVD: 1,  // PVD through EXTI Line detection
  IRQ_TAMPER: 2,  // Tamper Interrupt
  IRQ_RTC: 3,  // RTC Global Interrupt
  IRQ_FLASH: 4,  // FLASH Global Interrupt
  IRQ_RCC: 5,  // RCC Global Interrupt
  IRQ_EXTI0: 6,  // EXTI Line 0 Interrupt
  IRQ_EXTI1: 7,  // EXTI Line 1 Interrupt
  IRQ_EXTI2: 8,  // EXTI Line 2 Interrupt
  IRQ_EXTI3: 9,  // EXTI Line 3 Interrupt
  IRQ_EXTI4: 10,  // EXTI Line 4 Interrupt
  IRQ_DMA1_Channel1: 11,  // DMA1 Channel 1 Interrupt
  IRQ_DMA1_Channel2: 12,  // DMA1 Channel 2 Interrupt
  IRQ_DMA1_Channel3: 13,  // DMA1 Channel 3 Interrupt
  IRQ_DMA1_Channel4: 14,  // DMA1 Channel 4 Interrupt
  IRQ_DMA1_Channel5: 15,  // DMA1 Channel 5 Interrupt
  IRQ_DMA1_Channel6: 16,  // DMA1 Channel 6 Interrupt
  IRQ_DMA1_Channel7: 17,  // DMA1 Channel 7 Interrupt
  IRQ_ADC1_2: 18,  // ADC1 and ADC2 Global Interrupt
  IRQ_USB_HP_CAN_TX: 19,  // USB HP/CAN TX Interrupts
  IRQ_USB_LP_CAN_RX0: 20,  // USB LP/CAN RX0 Interrupt
  IRQ_CAN_RX1: 21,  // CAN RX1 Interrupt
  IRQ_CAN_SCE: 22,  // CAN SCE Interrupt
  IRQ_EXTI9_5: 23,  // EXTI Line 9..5 Interrupt
  IRQ_TIM1_BRK: 25,  // TIM1 Break Interrupt
  IRQ_TIM1_UP: 26,  // TIM1 Update Interrupt
  IRQ_TIM1_TRG_COM: 27,  // TIM1 Trigger and Commutation
  IRQ_TIM1_CC: 28,  // TIM1 Capture Compare Interrupt
  IRQ_TIM2: 29,  // TIM2 Global Interrupt
  IRQ_TIM3: 30,  // TIM3 Global Interrupt
  IRQ_TIM4: 31,  // TIM4 Global Interrupt
  IRQ_I2C1_EV: 32,  // I2C1 Event Interrupt
  IRQ_I2C1_ER: 33,  // I2C1 Error Interrupt
  IRQ_I2C2_EV: 34,  // I2C2 Event Interrupt
  IRQ_I2C2_ER: 35,  // I2C2 Error Interrupt
  IRQ_SPI1: 35,  // SPI1 Global Interrupt
  IRQ_SPI2: 36,  // SPI2 Global Interrupt
  IRQ_USART1: 37,  // USART1 Global Interrupt
  IRQ_USART2: 38,  // USART2 Global Interrupt
  IRQ_USART3: 39,  // USART3 Global Interrupt
  IRQ_EXTI15_10: 40,  // EXTI Line 15..10 Interrupt
  IRQ_RTCAlarm: 41,  // RTC Alarm through EXTI
  IRQ_USBWakeup: 42,  // USB Wakeup from suspend

  // 引脚定义
  PIN_VBAT: 1,  // Battery Supply
  PIN_PC13: 2,  // GPIO Port C Pin 13
  PIN_PC14: 3,  // GPIO Port C Pin 14
  PIN_PC15: 4,  // GPIO Port C Pin 15
  PIN_PD0: 5,  // GPIO Port D Pin 0
  PIN_PD1: 6,  // GPIO Port D Pin 1
  PIN_NRST: 7,  // Reset
  PIN_VSSA: 8,  // Analog Ground
  PIN_VDDA: 9,  // Analog Supply
  PIN_PA0: 10,  // GPIO Port A Pin 0 / ADC1_IN0
  PIN_PA1: 11,  // GPIO Port A Pin 1 / ADC1_IN1
  PIN_PA2: 12,  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
  PIN_PA3: 13,  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
  PIN_PA4: 14,  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
  PIN_PA5: 15,  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
  PIN_PA6: 16,  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
  PIN_PA7: 17,  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
  PIN_PB0: 18,  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
  PIN_PB1: 19,  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
  PIN_PB2: 20,  // GPIO Port B Pin 2
  PIN_PB10: 21,  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
  PIN_PB11: 22,  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
  PIN_VSS: 23,  // Ground
  PIN_VDD: 24,  // Digital Supply
  PIN_PB12: 25,  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
  PIN_PB13: 26,  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
  PIN_PB14: 27,  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
  PIN_PB15: 28,  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
  PIN_PA8: 29,  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
  PIN_PA9: 30,  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
  PIN_PA10: 31,  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
  PIN_PA11: 32,  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
  PIN_PA12: 33,  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
  PIN_PA13: 34,  // JTMS/SWDIO
  PIN_PA14: 37,  // JTCK/SWCLK
  PIN_PA15: 38,  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
  PIN_PB3: 39,  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
  PIN_PB4: 40,  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
  PIN_PB5: 41,  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
  PIN_PB6: 42,  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
  PIN_PB7: 43,  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
  PIN_BOOT0: 44,  // Boot Selection
  PIN_PB8: 45,  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
  PIN_PB9: 46,  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
  PIN_VSS: 47,  // Ground
  PIN_VDD: 48,  // Digital Supply

  init: function() {
    // 硬件初始化
  }
};
