// STM32F103C8T6 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32F103C8T6
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 72000000 Hz

class STM32F103C8T6Device {
  static const String deviceName = "STM32F103C8T6";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M3";
  static const int bits = 32;
  static const int clockFrequency = 72000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // General Purpose Register 0
  static const int R1_ADDR = 0x04;  // General Purpose Register 1
  static const int R2_ADDR = 0x08;  // General Purpose Register 2
  static const int R3_ADDR = 0x0C;  // General Purpose Register 3
  static const int R4_ADDR = 0x10;  // General Purpose Register 4
  static const int R5_ADDR = 0x14;  // General Purpose Register 5
  static const int R6_ADDR = 0x18;  // General Purpose Register 6
  static const int R7_ADDR = 0x1C;  // General Purpose Register 7
  static const int R8_ADDR = 0x20;  // General Purpose Register 8
  static const int R9_ADDR = 0x24;  // General Purpose Register 9
  static const int R10_ADDR = 0x28;  // General Purpose Register 10
  static const int R11_ADDR = 0x2C;  // General Purpose Register 11
  static const int R12_ADDR = 0x30;  // General Purpose Register 12
  static const int SP_ADDR = 0x34;  // Stack Pointer
  static const int LR_ADDR = 0x38;  // Link Register
  static const int PC_ADDR = 0x3C;  // Program Counter
  static const int XPSR_ADDR = 0x40;  // Program Status Register

  // 内存段定义
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x0800FFFF;
  static const int FLASH_SIZE = 65536;  // Main Flash Memory (64KB)
  static const int SYSTEM_MEMORY_START = 0x1FFFF000;
  static const int SYSTEM_MEMORY_END = 0x1FFFF7FF;
  static const int SYSTEM_MEMORY_SIZE = 2048;  // System Memory (2KB)
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20004FFF;
  static const int SRAM_SIZE = 20480;  // SRAM (20KB)
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x40023FFF;
  static const int PERIPHERAL_SIZE = 143360;  // Peripheral Registers
  static const int CORTEX_M_START = 0xE0000000;
  static const int CORTEX_M_END = 0xE00FFFFF;
  static const int CORTEX_M_SIZE = 1048576;  // Core Peripheral Registers

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40021000;
  static const int RCC_CR_ADDR = 0x00;
  static const int RCC_CFGR_ADDR = 0x04;
  static const int RCC_APB2ENR_ADDR = 0x18;
  static const int RCC_APB1ENR_ADDR = 0x1C;
  // GPIO Port A
  static const int GPIOA_BASE = 0x40010800;
  static const int GPIOA_CRL_ADDR = 0x00;
  static const int GPIOA_CRH_ADDR = 0x04;
  static const int GPIOA_IDR_ADDR = 0x08;
  static const int GPIOA_ODR_ADDR = 0x0C;
  static const int GPIOA_BSRR_ADDR = 0x10;
  static const int GPIOA_BRR_ADDR = 0x14;
  static const int GPIOA_LCKR_ADDR = 0x18;
  // GPIO Port B
  static const int GPIOB_BASE = 0x40010C00;
  static const int GPIOB_CRL_ADDR = 0x00;
  static const int GPIOB_CRH_ADDR = 0x04;
  static const int GPIOB_IDR_ADDR = 0x08;
  static const int GPIOB_ODR_ADDR = 0x0C;
  static const int GPIOB_BSRR_ADDR = 0x10;
  static const int GPIOB_BRR_ADDR = 0x14;
  // GPIO Port C
  static const int GPIOC_BASE = 0x40011000;
  static const int GPIOC_CRL_ADDR = 0x00;
  static const int GPIOC_CRH_ADDR = 0x04;
  static const int GPIOC_IDR_ADDR = 0x08;
  static const int GPIOC_ODR_ADDR = 0x0C;
  static const int GPIOC_BSRR_ADDR = 0x10;
  // USART 1
  static const int USART1_BASE = 0x40013800;
  static const int USART1_SR_ADDR = 0x00;
  static const int USART1_DR_ADDR = 0x04;
  static const int USART1_BRR_ADDR = 0x08;
  static const int USART1_CR1_ADDR = 0x0C;
  static const int USART1_CR2_ADDR = 0x10;
  static const int USART1_CR3_ADDR = 0x14;
  static const int USART1_GTPR_ADDR = 0x18;
  // USART 2
  static const int USART2_BASE = 0x40004400;
  static const int USART2_SR_ADDR = 0x00;
  static const int USART2_DR_ADDR = 0x04;
  static const int USART2_BRR_ADDR = 0x08;
  static const int USART2_CR1_ADDR = 0x0C;
  // SPI 1
  static const int SPI1_BASE = 0x40013000;
  static const int SPI1_CR1_ADDR = 0x00;
  static const int SPI1_CR2_ADDR = 0x04;
  static const int SPI1_SR_ADDR = 0x08;
  static const int SPI1_DR_ADDR = 0x0C;
  // SPI 2
  static const int SPI2_BASE = 0x40003800;
  static const int SPI2_CR1_ADDR = 0x00;
  static const int SPI2_CR2_ADDR = 0x04;
  static const int SPI2_SR_ADDR = 0x08;
  static const int SPI2_DR_ADDR = 0x0C;
  // I2C 1
  static const int I2C1_BASE = 0x40005400;
  static const int I2C1_CR1_ADDR = 0x00;
  static const int I2C1_CR2_ADDR = 0x04;
  static const int I2C1_SR1_ADDR = 0x08;
  static const int I2C1_SR2_ADDR = 0x0C;
  static const int I2C1_DR_ADDR = 0x10;
  static const int I2C1_CCR_ADDR = 0x14;
  static const int I2C1_TRISE_ADDR = 0x18;
  // I2C 2
  static const int I2C2_BASE = 0x40005800;
  static const int I2C2_CR1_ADDR = 0x00;
  static const int I2C2_CR2_ADDR = 0x04;
  static const int I2C2_SR1_ADDR = 0x08;
  static const int I2C2_SR2_ADDR = 0x0C;
  static const int I2C2_DR_ADDR = 0x10;
  static const int I2C2_CCR_ADDR = 0x14;
  // Advanced Timer 1
  static const int TIM1_BASE = 0x40012C00;
  static const int TIM1_CR1_ADDR = 0x00;
  static const int TIM1_CR2_ADDR = 0x04;
  static const int TIM1_SMCR_ADDR = 0x08;
  static const int TIM1_DIER_ADDR = 0x0C;
  static const int TIM1_SR_ADDR = 0x10;
  static const int TIM1_EGR_ADDR = 0x14;
  static const int TIM1_CCMR1_ADDR = 0x18;
  static const int TIM1_CCMR2_ADDR = 0x1C;
  static const int TIM1_CCER_ADDR = 0x20;
  static const int TIM1_CNT_ADDR = 0x24;
  static const int TIM1_PSC_ADDR = 0x28;
  static const int TIM1_ARR_ADDR = 0x2C;
  static const int TIM1_RCR_ADDR = 0x30;
  static const int TIM1_CCR1_ADDR = 0x34;
  static const int TIM1_CCR2_ADDR = 0x38;
  static const int TIM1_CCR3_ADDR = 0x3C;
  static const int TIM1_CCR4_ADDR = 0x40;
  static const int TIM1_BDTR_ADDR = 0x44;
  // General Purpose Timer 2
  static const int TIM2_BASE = 0x40000400;
  static const int TIM2_CR1_ADDR = 0x00;
  static const int TIM2_CNT_ADDR = 0x24;
  static const int TIM2_PSC_ADDR = 0x28;
  static const int TIM2_ARR_ADDR = 0x2C;
  static const int TIM2_CCR1_ADDR = 0x34;
  static const int TIM2_CCR2_ADDR = 0x38;
  static const int TIM2_CCR3_ADDR = 0x3C;
  static const int TIM2_CCR4_ADDR = 0x40;
  // General Purpose Timer 3
  static const int TIM3_BASE = 0x40000400;
  static const int TIM3_CR1_ADDR = 0x00;
  static const int TIM3_CNT_ADDR = 0x24;
  static const int TIM3_ARR_ADDR = 0x2C;
  static const int TIM3_CCR1_ADDR = 0x34;
  static const int TIM3_CCR2_ADDR = 0x38;
  static const int TIM3_CCR3_ADDR = 0x3C;
  static const int TIM3_CCR4_ADDR = 0x40;
  // General Purpose Timer 4
  static const int TIM4_BASE = 0x40000800;
  static const int TIM4_CR1_ADDR = 0x00;
  static const int TIM4_CNT_ADDR = 0x24;
  static const int TIM4_ARR_ADDR = 0x2C;
  static const int TIM4_CCR1_ADDR = 0x34;
  static const int TIM4_CCR2_ADDR = 0x38;
  static const int TIM4_CCR3_ADDR = 0x3C;
  static const int TIM4_CCR4_ADDR = 0x40;
  // ADC 1
  static const int ADC1_BASE = 0x40012400;
  static const int ADC1_SR_ADDR = 0x00;
  static const int ADC1_CR1_ADDR = 0x04;
  static const int ADC1_CR2_ADDR = 0x08;
  static const int ADC1_SMPR1_ADDR = 0x0C;
  static const int ADC1_SMPR2_ADDR = 0x10;
  static const int ADC1_JOFR1_ADDR = 0x14;
  static const int ADC1_JOFR2_ADDR = 0x18;
  static const int ADC1_JOFR3_ADDR = 0x1C;
  static const int ADC1_JOFR4_ADDR = 0x20;
  static const int ADC1_HTR_ADDR = 0x24;
  static const int ADC1_LTR_ADDR = 0x28;
  static const int ADC1_SQRT1_ADDR = 0x2C;
  static const int ADC1_SQRT2_ADDR = 0x30;
  static const int ADC1_SQRT3_ADDR = 0x34;
  static const int ADC1_JSQR_ADDR = 0x38;
  static const int ADC1_JDR1_ADDR = 0x3C;
  static const int ADC1_JDR2_ADDR = 0x40;
  static const int ADC1_JDR3_ADDR = 0x44;
  static const int ADC1_JDR4_ADDR = 0x48;
  static const int ADC1_DR_ADDR = 0x4C;
  // DMA Controller 1
  static const int DMA1_BASE = 0x40020000;
  static const int DMA1_ISR_ADDR = 0x00;
  static const int DMA1_IFCR_ADDR = 0x04;
  static const int DMA1_CCR1_ADDR = 0x08;
  static const int DMA1_CNDTR1_ADDR = 0x0C;
  static const int DMA1_CPAR1_ADDR = 0x10;
  static const int DMA1_CMAR1_ADDR = 0x14;
  static const int DMA1_CCR2_ADDR = 0x1C;
  static const int DMA1_CNDTR2_ADDR = 0x20;
  static const int DMA1_CPAR2_ADDR = 0x24;
  static const int DMA1_CMAR2_ADDR = 0x28;
  // Power Control
  static const int PWR_BASE = 0x40007000;
  static const int PWR_CR_ADDR = 0x00;
  static const int PWR_CSR_ADDR = 0x04;
  // Backup Registers
  static const int BKP_BASE = 0x40006C00;
  static const int BKP_DR1_ADDR = 0x04;
  static const int BKP_DR2_ADDR = 0x08;
  static const int BKP_CSR_ADDR = 0x2C;
  // Window Watchdog
  static const int WWDG_BASE = 0x40002C00;
  static const int WWDG_CR_ADDR = 0x00;
  static const int WWDG_CFR_ADDR = 0x04;
  static const int WWDG_SR_ADDR = 0x08;
  // Independent Watchdog
  static const int IWDG_BASE = 0x40003000;
  static const int IWDG_KR_ADDR = 0x00;
  static const int IWDG_PR_ADDR = 0x04;
  static const int IWDG_RLR_ADDR = 0x08;
  // External Interrupt/Event Controller
  static const int EXTI_BASE = 0x40010400;
  static const int EXTI_IMR_ADDR = 0x00;
  static const int EXTI_EMR_ADDR = 0x04;
  static const int EXTI_RTSR_ADDR = 0x08;
  static const int EXTI_FTSR_ADDR = 0x0C;
  static const int EXTI_SWIER_ADDR = 0x10;
  static const int EXTI_PR_ADDR = 0x14;
  // Alternate Function IO
  static const int AFIO_BASE = 0x40010000;
  static const int AFIO_EVCR_ADDR = 0x00;
  static const int AFIO_MAPR_ADDR = 0x04;
  static const int AFIO_EXTICR1_ADDR = 0x08;
  static const int AFIO_EXTICR2_ADDR = 0x0C;
  static const int AFIO_EXTICR3_ADDR = 0x10;
  static const int AFIO_MAPR2_ADDR = 0x1C;

  // 中断向量定义
  static const int INT_WWDG = 0;  // Window Watchdog Interrupt
  static const int INT_PVD = 1;  // PVD through EXTI Line detection
  static const int INT_TAMPER = 2;  // Tamper Interrupt
  static const int INT_RTC = 3;  // RTC Global Interrupt
  static const int INT_FLASH = 4;  // FLASH Global Interrupt
  static const int INT_RCC = 5;  // RCC Global Interrupt
  static const int INT_EXTI0 = 6;  // EXTI Line 0 Interrupt
  static const int INT_EXTI1 = 7;  // EXTI Line 1 Interrupt
  static const int INT_EXTI2 = 8;  // EXTI Line 2 Interrupt
  static const int INT_EXTI3 = 9;  // EXTI Line 3 Interrupt
  static const int INT_EXTI4 = 10;  // EXTI Line 4 Interrupt
  static const int INT_DMA1_CHANNEL1 = 11;  // DMA1 Channel 1 Interrupt
  static const int INT_DMA1_CHANNEL2 = 12;  // DMA1 Channel 2 Interrupt
  static const int INT_DMA1_CHANNEL3 = 13;  // DMA1 Channel 3 Interrupt
  static const int INT_DMA1_CHANNEL4 = 14;  // DMA1 Channel 4 Interrupt
  static const int INT_DMA1_CHANNEL5 = 15;  // DMA1 Channel 5 Interrupt
  static const int INT_DMA1_CHANNEL6 = 16;  // DMA1 Channel 6 Interrupt
  static const int INT_DMA1_CHANNEL7 = 17;  // DMA1 Channel 7 Interrupt
  static const int INT_ADC1_2 = 18;  // ADC1 and ADC2 Global Interrupt
  static const int INT_USB_HP_CAN_TX = 19;  // USB HP/CAN TX Interrupts
  static const int INT_USB_LP_CAN_RX0 = 20;  // USB LP/CAN RX0 Interrupt
  static const int INT_CAN_RX1 = 21;  // CAN RX1 Interrupt
  static const int INT_CAN_SCE = 22;  // CAN SCE Interrupt
  static const int INT_EXTI9_5 = 23;  // EXTI Line 9..5 Interrupt
  static const int INT_TIM1_BRK = 25;  // TIM1 Break Interrupt
  static const int INT_TIM1_UP = 26;  // TIM1 Update Interrupt
  static const int INT_TIM1_TRG_COM = 27;  // TIM1 Trigger and Commutation
  static const int INT_TIM1_CC = 28;  // TIM1 Capture Compare Interrupt
  static const int INT_TIM2 = 29;  // TIM2 Global Interrupt
  static const int INT_TIM3 = 30;  // TIM3 Global Interrupt
  static const int INT_TIM4 = 31;  // TIM4 Global Interrupt
  static const int INT_I2C1_EV = 32;  // I2C1 Event Interrupt
  static const int INT_I2C1_ER = 33;  // I2C1 Error Interrupt
  static const int INT_I2C2_EV = 34;  // I2C2 Event Interrupt
  static const int INT_I2C2_ER = 35;  // I2C2 Error Interrupt
  static const int INT_SPI1 = 35;  // SPI1 Global Interrupt
  static const int INT_SPI2 = 36;  // SPI2 Global Interrupt
  static const int INT_USART1 = 37;  // USART1 Global Interrupt
  static const int INT_USART2 = 38;  // USART2 Global Interrupt
  static const int INT_USART3 = 39;  // USART3 Global Interrupt
  static const int INT_EXTI15_10 = 40;  // EXTI Line 15..10 Interrupt
  static const int INT_RTCALARM = 41;  // RTC Alarm through EXTI
  static const int INT_USBWAKEUP = 42;  // USB Wakeup from suspend

  // 引脚定义
  static const int PIN_VBAT = 1;  // Battery Supply
  static const int PIN_PC13 = 2;  // GPIO Port C Pin 13
  static const int PIN_PC14 = 3;  // GPIO Port C Pin 14
  static const int PIN_PC15 = 4;  // GPIO Port C Pin 15
  static const int PIN_PD0 = 5;  // GPIO Port D Pin 0
  static const int PIN_PD1 = 6;  // GPIO Port D Pin 1
  static const int PIN_NRST = 7;  // Reset
  static const int PIN_VSSA = 8;  // Analog Ground
  static const int PIN_VDDA = 9;  // Analog Supply
  static const int PIN_PA0 = 10;  // GPIO Port A Pin 0 / ADC1_IN0
  static const int PIN_PA1 = 11;  // GPIO Port A Pin 1 / ADC1_IN1
  static const int PIN_PA2 = 12;  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
  static const int PIN_PA3 = 13;  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
  static const int PIN_PA4 = 14;  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
  static const int PIN_PA5 = 15;  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
  static const int PIN_PA6 = 16;  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
  static const int PIN_PA7 = 17;  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
  static const int PIN_PB0 = 18;  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
  static const int PIN_PB1 = 19;  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
  static const int PIN_PB2 = 20;  // GPIO Port B Pin 2
  static const int PIN_PB10 = 21;  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
  static const int PIN_PB11 = 22;  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
  static const int PIN_VSS = 23;  // Ground
  static const int PIN_VDD = 24;  // Digital Supply
  static const int PIN_PB12 = 25;  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
  static const int PIN_PB13 = 26;  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
  static const int PIN_PB14 = 27;  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
  static const int PIN_PB15 = 28;  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
  static const int PIN_PA8 = 29;  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
  static const int PIN_PA9 = 30;  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
  static const int PIN_PA10 = 31;  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
  static const int PIN_PA11 = 32;  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
  static const int PIN_PA12 = 33;  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
  static const int PIN_PA13 = 34;  // JTMS/SWDIO
  static const int PIN_PA14 = 37;  // JTCK/SWCLK
  static const int PIN_PA15 = 38;  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
  static const int PIN_PB3 = 39;  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
  static const int PIN_PB4 = 40;  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
  static const int PIN_PB5 = 41;  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
  static const int PIN_PB6 = 42;  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
  static const int PIN_PB7 = 43;  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
  static const int PIN_BOOT0 = 44;  // Boot Selection
  static const int PIN_PB8 = 45;  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
  static const int PIN_PB9 = 46;  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
  static const int PIN_VSS = 47;  // Ground
  static const int PIN_VDD = 48;  // Digital Supply

}
