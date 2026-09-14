unit stm32f407vgt6;

interface

// STM32F407VGT6寄存器定义
// 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: High-performance ARM Cortex-M4 with FPU, 168MHz, 1MB Flash, 192KB SRAM

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 16000000 Hz

const

  // 寄存器定义
  // General Purpose Register 0
  R0 = 0x00000000;

  // General Purpose Register 1
  R1 = 0x00000004;

  // General Purpose Register 2
  R2 = 0x00000008;

  // General Purpose Register 3
  R3 = 0x0000000C;

  // General Purpose Register 4
  R4 = 0x00000010;

  // General Purpose Register 5
  R5 = 0x00000014;

  // General Purpose Register 6
  R6 = 0x00000018;

  // General Purpose Register 7
  R7 = 0x0000001C;

  // General Purpose Register 8
  R8 = 0x00000020;

  // General Purpose Register 9
  R9 = 0x00000024;

  // General Purpose Register 10
  R10 = 0x00000028;

  // General Purpose Register 11
  R11 = 0x0000002C;

  // General Purpose Register 12
  R12 = 0x00000030;

  // Stack Pointer
  SP = 0x00000034;

  // Link Register
  LR = 0x00000038;

  // Program Counter
  PC = 0x0000003C;

  // Program Status Register
  PSR = 0x00000040;
  PSR_N = 31;  // Negative Flag
  PSR_Z = 30;  // Zero Flag
  PSR_C = 29;  // Carry Flag
  PSR_V = 28;  // Overflow Flag
  PSR_Q = 27;  // SAT Flag
  PSR_ICI = 0;  // ICI/IT
  PSR_GE = 0;  // Greater than or Equal
  PSR_IT = 0;  // IT status
  PSR_Q = 9;  // APSR
  PSR_IPSR = 0;  // IPSR

  // Priority Mask Register
  PRIMASK = 0xE0000E20;

  // Base Priority Register
  BASEPRI = 0xE0000E24;

  // Fault Mask Register
  FAULTMASK = 0xE0000E28;

  // Control Register
  CONTROL = 0xE0000E2C;

  // FPU Status Control
  FPSCR = E0000EF34;

  // FP Register 0
  S0 = 0xE0000EF00;

  // FP Register 1
  S1 = 0xE0000EF04;

  // FP Register 2
  S2 = 0xE0000EF08;

  // FP Register 3
  S3 = 0xE0000EF0C;

  // FP Register 4
  S4 = 0xE0000EF10;

  // FP Register 5
  S5 = 0xE0000EF14;

  // FP Register 6
  S6 = 0xE0000EF18;

  // FP Register 7
  S7 = 0xE0000EF1C;

  // FP Register 8
  S8 = 0xE0000EF20;

  // FP Register 9
  S9 = 0xE0000EF24;

  // FP Register 10
  S10 = 0xE0000EF28;

  // FP Register 11
  S11 = 0xE0000EF2C;

  // FP Register 12
  S12 = 0xE0000EF30;

  // FP Register 13
  S13 = 0xE0000EF34;

  // FP Register 14
  S14 = 0xE0000EF38;

  // FP Register 15
  S15 = 0xE0000EF3C;

  // FP Register 16
  S16 = 0xE0000EF40;

  // FP Register 17
  S17 = 0xE0000EF44;

  // FP Register 18
  S18 = 0xE0000EF48;

  // FP Register 19
  S19 = 0xE0000EF4C;

  // FP Register 20
  S20 = 0xE0000EF50;

  // FP Register 21
  S21 = 0xE0000EF54;

  // FP Register 22
  S22 = 0xE0000EF58;

  // FP Register 23
  S23 = 0xE0000EF5C;

  // FP Register 24
  S24 = 0xE0000EF60;

  // FP Register 25
  S25 = 0xE0000EF64;

  // FP Register 26
  S26 = 0xE0000EF68;

  // FP Register 27
  S27 = 0xE0000EF6C;

  // FP Register 28
  S28 = 0xE0000EF70;

  // FP Register 29
  S29 = 0xE0000EF74;

  // FP Register 30
  S30 = 0xE0000EF78;

  // FP Register 31
  S31 = 0xE0000EF7C;

  // 内存段定义
  // Main Flash (1MB)
  FLASH_START = 0x08000000;
  FLASH_END = 0x080FFFFF;
  FLASH_SIZE = 1048576;

  // System Flash (32KB)
  SYSTEM_START = 0x1FFF0000;
  SYSTEM_END = 0x1FFF7FFF;
  SYSTEM_SIZE = 32768;

  // Option Bytes (16KB)
  OPTION_START = 0x1FFF8000;
  OPTION_END = 0x1FFFC000;
  OPTION_SIZE = 16384;

  // SRAM1 (128KB)
  SRAM1_START = 0x20000000;
  SRAM1_END = 0x2001FFFF;
  SRAM1_SIZE = 131072;

  // SRAM2 (64KB)
  SRAM2_START = 0x20020000;
  SRAM2_END = 0x2002FFFF;
  SRAM2_SIZE = 65536;

  // CCM RAM (64KB)
  CCM_START = 0x20030000;
  CCM_END = 0x2003FFFF;
  CCM_SIZE = 65536;

  // Peripheral Registers
  PERIPHERAL_START = 0x40000000;
  PERIPHERAL_END = 0x40023FFF;
  PERIPHERAL_SIZE = 147456;

  // FMC Registers
  FMC_START = 0x40020000;
  FMC_END = 0x40023FFF;
  FMC_SIZE = 16384;

  // FSMC Registers
  FSMC_START = 0x40000000;
  FSMC_END = 0x400003FF;
  FSMC_SIZE = 1024;

  // GPIOA Registers
  GPIOA_START = 0x40020000;
  GPIOA_END = 0x400203FF;
  GPIOA_SIZE = 1024;

  // 外设定义
  // Reset and Clock Control
  RCC_BASE = 0x40023800;
  RCC_CR = 0x0000;
  RCC_PLLCFGR = 0x0004;
  RCC_CFGR = 0x0008;
  RCC_CIR = 0x000C;
  RCC_APB2RSTR = 0x0010;
  RCC_APB1RSTR = 0x0014;
  RCC_AHB1ENR = 0x0030;
  RCC_AHB2ENR = 0x0034;
  RCC_AHB3ENR = 0x0038;
  RCC_APB2ENR = 0x0040;
  RCC_APB1ENR = 0x0044;
  RCC_BDCR = 0x0050;
  RCC_CSR = 0x0054;
  RCC_AHB1RSTR = 0x0020;
  RCC_AHB2RSTR = 0x0024;
  RCC_AHB3RSTR = 0x0028;
  RCC_CFGR2 = 0x0018;
  RCC_CFGR3 = 0x001C;
  RCC_PLL2CFGR = 0x0060;
  RCC_PLL2DIV = 0x0064;
  RCC_PLL3CFGR = 0x0068;
  RCC_PLL3DIV = 0x006C;

  // Flash Interface
  FLASH_BASE = 0x40023C00;
  FLASH_ACR = 0x0000;
  FLASH_KEYR = 0x0004;
  FLASH_OPTKEYR = 0x0008;
  FLASH_SR = 0x000C;
  FLASH_CR = 0x0010;
  FLASH_OPTCR = 0x0014;
  FLASH_OPTCR1 = 0x0018;

  // Power Control
  PWR_BASE = 0x40007000;
  PWR_CR = 0x0000;
  PWR_CSR = 0x0004;

  // DMA1 Controller
  DMA1_BASE = 0x40026000;
  DMA1_LIFCR = 0x0000;
  DMA1_HIFCR = 0x0004;
  DMA1_LISR = 0x0008;
  DMA1_HISR = 0x000C;
  DMA1_S0CR = 0x0010;
  DMA1_S0NDTR = 0x0014;
  DMA1_S0PAR = 0x0018;
  DMA1_S0M0AR = 0x001C;
  DMA1_S0M1AR = 0x0020;
  DMA1_S0FCR = 0x0024;
  DMA1_S1CR = 0x0028;
  DMA1_S1NDTR = 0x002C;
  DMA1_S2CR = 0x0040;
  DMA1_S3CR = 0x0058;
  DMA1_S4CR = 0x0070;
  DMA1_S5CR = 0x0088;
  DMA1_S6CR = 0x00A0;
  DMA1_S7CR = 0x00B8;

  // DMA2 Controller
  DMA2_BASE = 0x40026400;
  DMA2_LIFCR = 0x0000;
  DMA2_HIFCR = 0x0004;
  DMA2_LISR = 0x0008;
  DMA2_HISR = 0x000C;
  DMA2_S0CR = 0x0010;
  DMA2_S1CR = 0x0028;
  DMA2_S2CR = 0x0040;
  DMA2_S3CR = 0x0058;
  DMA2_S4CR = 0x0070;
  DMA2_S5CR = 0x0088;
  DMA2_S6CR = 0x00A0;
  DMA2_S7CR = 0x00B8;

  // USART1
  USART1_BASE = 0x40011000;
  USART1_SR = 0x0000;
  USART1_DR = 0x0004;
  USART1_BRR = 0x0008;
  USART1_CR1 = 0x000C;
  USART1_CR2 = 0x0010;
  USART1_CR3 = 0x0014;
  USART1_GTPR = 0x0018;

  // USART2
  USART2_BASE = 0x40004400;
  USART2_SR = 0x0000;
  USART2_DR = 0x0004;
  USART2_BRR = 0x0008;
  USART2_CR1 = 0x000C;
  USART2_CR2 = 0x0010;
  USART2_CR3 = 0x0014;

  // USART3
  USART3_BASE = 0x40004800;
  USART3_SR = 0x0000;
  USART3_DR = 0x0004;
  USART3_BRR = 0x0008;
  USART3_CR1 = 0x000C;
  USART3_CR2 = 0x0010;
  USART3_CR3 = 0x0014;

  // SPI1
  SPI1_BASE = 0x40013000;
  SPI1_CR1 = 0x0000;
  SPI1_CR2 = 0x0004;
  SPI1_SR = 0x0008;
  SPI1_DR = 0x000C;
  SPI1_CRCPR = 0x0010;
  SPI1_RXCRCR = 0x0014;
  SPI1_TXCRCR = 0x0018;
  SPI1_I2SCFGR = 0x001C;

  // SPI2
  SPI2_BASE = 0x40003800;
  SPI2_CR1 = 0x0000;
  SPI2_CR2 = 0x0004;
  SPI2_SR = 0x0008;
  SPI2_DR = 0x000C;
  SPI2_CRCPR = 0x0010;
  SPI2_RXCRCR = 0x0014;

  // SPI3
  SPI3_BASE = 0x40003C00;
  SPI3_CR1 = 0x0000;
  SPI3_CR2 = 0x0004;
  SPI3_SR = 0x0008;
  SPI3_DR = 0x000C;

  // I2C1
  I2C1_BASE = 0x40005400;
  I2C1_CR1 = 0x0000;
  I2C1_CR2 = 0x0004;
  I2C1_OAR1 = 0x0008;
  I2C1_OAR2 = 0x000C;
  I2C1_DR = 0x0010;
  I2C1_SR1 = 0x0014;
  I2C1_SR2 = 0x0018;
  I2C1_CCR = 0x001C;
  I2C1_TRISE = 0x0020;
  I2C1_FLTR = 0x0024;

  // I2C2
  I2C2_BASE = 0x40005800;
  I2C2_CR1 = 0x0000;
  I2C2_CR2 = 0x0004;
  I2C2_OAR1 = 0x0008;
  I2C2_OAR2 = 0x000C;
  I2C2_DR = 0x0010;
  I2C2_SR1 = 0x0014;
  I2C2_SR2 = 0x0018;
  I2C2_CCR = 0x001C;
  I2C2_TRISE = 0x0020;

  // I2C3
  I2C3_BASE = 0x40005C00;
  I2C3_CR1 = 0x0000;
  I2C3_CR2 = 0x0004;
  I2C3_OAR1 = 0x0008;
  I2C3_DR = 0x0010;
  I2C3_SR1 = 0x0014;
  I2C3_SR2 = 0x0018;
  I2C3_CCR = 0x001C;

  // Advanced Timer 1
  TIM1_BASE = 0x40012C00;
  TIM1_CR1 = 0x0000;
  TIM1_CR2 = 0x0004;
  TIM1_SMCR = 0x0008;
  TIM1_DIER = 0x000C;
  TIM1_SR = 0x0010;
  TIM1_EGR = 0x0014;
  TIM1_CCMR1 = 0x0018;
  TIM1_CCMR2 = 0x001C;
  TIM1_CCER = 0x0020;
  TIM1_CNT = 0x0024;
  TIM1_PSC = 0x0028;
  TIM1_ARR = 0x002C;
  TIM1_RCR = 0x0030;
  TIM1_CCR1 = 0x0034;
  TIM1_CCR2 = 0x0038;
  TIM1_CCR3 = 0x003C;
  TIM1_CCR4 = 0x0040;
  TIM1_BDTR = 0x0044;
  TIM1_DCR = 0x0048;
  TIM1_DMAR = 0x004C;

  // General Purpose Timer 2
  TIM2_BASE = 0x40000000;
  TIM2_CR1 = 0x0000;
  TIM2_CR2 = 0x0004;
  TIM2_SMCR = 0x0008;
  TIM2_DIER = 0x000C;
  TIM2_SR = 0x0010;
  TIM2_EGR = 0x0014;
  TIM2_CCMR1 = 0x0018;
  TIM2_CCMR2 = 0x001C;
  TIM2_CCER = 0x0020;
  TIM2_CNT = 0x0024;
  TIM2_PSC = 0x0028;
  TIM2_ARR = 0x002C;
  TIM2_CCR1 = 0x0034;
  TIM2_CCR2 = 0x0038;
  TIM2_CCR3 = 0x003C;
  TIM2_CCR4 = 0x0040;

  // General Purpose Timer 3
  TIM3_BASE = 0x40000400;
  TIM3_CR1 = 0x0000;
  TIM3_SR = 0x0010;
  TIM3_CCMR1 = 0x0018;
  TIM3_CCER = 0x0020;
  TIM3_CNT = 0x0024;
  TIM3_PSC = 0x0028;
  TIM3_ARR = 0x002C;
  TIM3_CCR1 = 0x0034;
  TIM3_CCR2 = 0x0038;
  TIM3_CCR3 = 0x003C;
  TIM3_CCR4 = 0x0040;

  // General Purpose Timer 4
  TIM4_BASE = 0x40000800;
  TIM4_CR1 = 0x0000;
  TIM4_SR = 0x0010;
  TIM4_CCMR1 = 0x0018;
  TIM4_CCER = 0x0020;
  TIM4_CNT = 0x0024;
  TIM4_PSC = 0x0028;
  TIM4_ARR = 0x002C;
  TIM4_CCR1 = 0x0034;
  TIM4_CCR2 = 0x0038;
  TIM4_CCR3 = 0x003C;
  TIM4_CCR4 = 0x0040;

  // General Purpose Timer 5
  TIM5_BASE = 0x40000C00;
  TIM5_CR1 = 0x0000;
  TIM5_SR = 0x0010;
  TIM5_CNT = 0x0024;
  TIM5_PSC = 0x0028;
  TIM5_ARR = 0x002C;
  TIM5_CCR1 = 0x0034;
  TIM5_CCR2 = 0x0038;
  TIM5_CCR3 = 0x003C;
  TIM5_CCR4 = 0x0040;

  // General Purpose Timer 9
  TIM9_BASE = 0x40014C00;
  TIM9_CR1 = 0x0000;
  TIM9_SMCR = 0x0008;
  TIM9_DIER = 0x000C;
  TIM9_SR = 0x0010;
  TIM9_EGR = 0x0014;
  TIM9_CCMR1 = 0x0018;
  TIM9_CCER = 0x0020;
  TIM9_CNT = 0x0024;
  TIM9_PSC = 0x0028;
  TIM9_ARR = 0x002C;
  TIM9_CCR1 = 0x0034;
  TIM9_CCR2 = 0x0038;

  // General Purpose Timer 10
  TIM10_BASE = 0x40015000;
  TIM10_CR1 = 0x0000;
  TIM10_DIER = 0x000C;
  TIM10_SR = 0x0010;
  TIM10_EGR = 0x0014;
  TIM10_CCMR1 = 0x0018;
  TIM10_CCER = 0x0020;
  TIM10_CNT = 0x0024;
  TIM10_PSC = 0x0028;
  TIM10_ARR = 0x002C;
  TIM10_CCR1 = 0x0034;

  // ADC1
  ADC1_BASE = 0x40012000;
  ADC1_SR = 0x0000;
  ADC1_CR1 = 0x0004;
  ADC1_CR2 = 0x0008;
  ADC1_SMPR1 = 0x000C;
  ADC1_SMPR2 = 0x0010;
  ADC1_JOFR1 = 0x0014;
  ADC1_JOFR2 = 0x0018;
  ADC1_JOFR3 = 0x001C;
  ADC1_JOFR4 = 0x0020;
  ADC1_HTR = 0x0024;
  ADC1_LTR = 0x0028;
  ADC1_SQR1 = 0x002C;
  ADC1_SQR2 = 0x0030;
  ADC1_SQR3 = 0x0034;
  ADC1_JSQR = 0x003C;
  ADC1_JDR1 = 0x0040;
  ADC1_JDR2 = 0x0044;
  ADC1_JDR3 = 0x0048;
  ADC1_JDR4 = 0x004C;
  ADC1_DR = 0x0050;

  // ADC2
  ADC2_BASE = 0x40012100;
  ADC2_SR = 0x0000;
  ADC2_CR1 = 0x0004;
  ADC2_CR2 = 0x0008;
  ADC2_SMPR1 = 0x000C;
  ADC2_SMPR2 = 0x0010;
  ADC2_SQR1 = 0x002C;
  ADC2_DR = 0x0050;

  // ADC3
  ADC3_BASE = 0x40012200;
  ADC3_SR = 0x0000;
  ADC3_CR1 = 0x0004;
  ADC3_CR2 = 0x0008;
  ADC3_SMPR1 = 0x000C;
  ADC3_SMPR2 = 0x0010;
  ADC3_SQR1 = 0x002C;
  ADC3_DR = 0x0050;

  // System Configuration Controller
  SYSCFG_BASE = 0x40013800;
  SYSCFG_CFGR = 0x0000;
  SYSCFG_EXTICR1 = 0x0008;
  SYSCFG_EXTICR2 = 0x000C;
  SYSCFG_EXTICR3 = 0x0010;
  SYSCFG_EXTICR4 = 0x0014;
  SYSCFG_CBR = 0x001C;

  // External Interrupt/Event Controller
  EXTI_BASE = 0x40013C00;
  EXTI_IMR = 0x0000;
  EXTI_EMR = 0x0004;
  EXTI_RTSR = 0x0008;
  EXTI_FTSR = 0x000C;
  EXTI_SWIER = 0x0010;
  EXTI_PR = 0x0014;

  // Random Number Generator
  RNG_BASE = 0x50060800;
  RNG_CR = 0x0000;
  RNG_SR = 0x0004;
  RNG_DR = 0x0008;

  // CRYP Accelerator
  CRYP_BASE = 0x50060000;
  CRYP_CR = 0x0000;
  CRYP_SR = 0x0004;
  CRYP_DIN = 0x0008;
  CRYP_DOUT = 0x000C;
  CRYP_DMACR = 0x0010;
  CRYP_IMSCR = 0x0014;
  CRYP_RISR = 0x0018;
  CRYP_MISR = 0x001C;
  CRYP_K0LR = 0x0020;
  CRYP_K0RR = 0x0024;
  CRYP_K1LR = 0x0028;
  CRYP_K1RR = 0x002C;
  CRYP_K2LR = 0x0030;
  CRYP_K2RR = 0x0034;
  CRYP_K3LR = 0x0038;
  CRYP_K3RR = 0x003C;
  CRYP_IV0LR = 0x0040;
  CRYP_IV0RR = 0x0044;
  CRYP_IV1LR = 0x0048;
  CRYP_IV1RR = 0x004C;

  // HASH Accelerator
  HASH_BASE = 0x50060400;
  HASH_CR = 0x0000;
  HASH_DIN = 0x0004;
  HASH_DINSTAT = 0x0008;
  HASH_HR = 0x000C;
  HASH_IMR = 0x0020;
  HASH_SR = 0x0024;

  // Digital Camera Interface
  DCMI_BASE = 0x50050000;
  DCMI_CR = 0x0000;
  DCMI_SR = 0x0004;
  DCMI_RISR = 0x0008;
  DCMI_IER = 0x000C;
  DCMI_MISR = 0x0010;
  DCMI_ICR = 0x0014;
  DCMI_MFISH = 0x001C;
  DCMI_CWSTRT = 0x0020;
  DCMI_CWSIZE = 0x0024;
  DCMI_DR = 0x0028;
  DCMI_OR = 0x002C;

  // USB OTG High Speed
  USB_OTG_HS_BASE = 0x40040000;
  USB_OTG_HS_GOTGCTL = 0x0000;
  USB_OTG_HS_GOTGINT = 0x0004;
  USB_OTG_HS_GINTMSK = 0x0008;
  USB_OTG_HS_GRSTCTL = 0x000C;
  USB_OTG_HS_GINTSTS = 0x0010;
  USB_OTG_HS_GRXSTSR = 0x0014;
  USB_OTG_HS_GRXFSIZ = 0x0024;
  USB_OTG_HS_HNPTXFSIZ = 0x0028;
  USB_OTG_HS_HNPTXSTS = 0x002C;
  USB_OTG_HS_GCCFG = 0x0038;
  USB_OTG_HS_CID = 0x003C;
  USB_OTG_HS_HPTXFSIZ = 0x0100;
  USB_OTG_HS_DIEPTXF = 0x0200;

  // Ethernet
  ETH_BASE = 0x40028000;
  ETH_MACCR = 0x0000;
  ETH_MACFFR = 0x0004;
  ETH_MACHTHR = 0x0008;
  ETH_MACHTLR = 0x000C;
  ETH_MACMIIAR = 0x0010;
  ETH_MACMIIDR = 0x0014;
  ETH_MACCR = 0x0018;
  ETH_MACVLANTR = 0x001C;
  ETH_MACRWUFFR = 0x0028;
  ETH_MACPMTCSR = 0x002C;
  ETH_MACSR = 0x0030;
  ETH_MACIMR = 0x0034;
  ETH_MACA0HR = 0x0040;
  ETH_MACA0LR = 0x0044;
  ETH_MACA1HR = 0x0048;
  ETH_MACA1LR = 0x004C;
  ETH_MMCCR = 0x0100;
  ETH_MMCRIR = 0x0104;
  ETH_MMCTIR = 0x0108;
  ETH_MMCRIMR = 0x010C;
  ETH_MMCTIMR = 0x0110;
  ETH_MMCTGBSCCR = 0x0114;
  ETH_MMCRGUFCCR = 0x0118;
  ETH_PTPTSCR = 0x0700;
  ETH_PTPSSIR = 0x0704;
  ETH_PTPTSHR = 0x0708;
  ETH_PTPTSLR = 0x070C;
  ETH_PTPTSHUR = 0x0710;
  ETH_PTPTSLUR = 0x0714;
  ETH_PTPTSAR = 0x0718;
  ETH_PTPTTHR = 0x071C;
  ETH_PTPTTLR = 0x0720;
  ETH_PTPTSR = 0x0728;
  ETH_DMABMR = 0x1000;
  ETH_DMASR = 0x1004;
  ETH_DMAOMR = 0x1008;
  ETH_DMAIER = 0x100C;
  ETH_DMAMFBOCR = 0x1010;
  ETH_DMACHTDR = 0x1014;
  ETH_DMACHRDR = 0x1018;
  ETH_DMACHTBAR = 0x101C;
  ETH_DMACHRBAR = 0x1020;

  // 中断向量定义
  WWDG_VECTOR = 0;  // Window WatchDog interrupt
  PVD_VECTOR = 1;  // PVD through EXTI line detection interrupt
  TAMPER_VECTOR = 2;  // Tamper interrupt
  RTC_WKUP_VECTOR = 3;  // RTC Wakeup interrupt
  FLASH_VECTOR = 4;  // FLASH global interrupt
  RCC_VECTOR = 5;  // RCC global interrupt
  EXTI0_VECTOR = 6;  // EXTI Line0 interrupt
  EXTI1_VECTOR = 7;  // EXTI Line1 interrupt
  EXTI2_VECTOR = 8;  // EXTI Line2 interrupt
  EXTI3_VECTOR = 9;  // EXTI Line3 interrupt
  EXTI4_VECTOR = 10;  // EXTI Line4 interrupt
  DMA1_STREAM0_VECTOR = 11;  // DMA1 Stream0 global interrupt
  DMA1_STREAM1_VECTOR = 12;  // DMA1 Stream1 global interrupt
  DMA1_STREAM2_VECTOR = 13;  // DMA1 Stream2 global interrupt
  DMA1_STREAM3_VECTOR = 14;  // DMA1 Stream3 global interrupt
  DMA1_STREAM4_VECTOR = 15;  // DMA1 Stream4 global interrupt
  DMA1_STREAM5_VECTOR = 16;  // DMA1 Stream5 global interrupt
  DMA1_STREAM6_VECTOR = 17;  // DMA1 Stream6 global interrupt
  ADC_VECTOR = 18;  // ADC1 global interrupt
  CAN1_TX_VECTOR = 19;  // CAN1 TX interrupts
  CAN1_RX0_VECTOR = 20;  // CAN1 RX0 interrupts
  CAN1_RX1_VECTOR = 21;  // CAN1 RX1 interrupt
  CAN1_SCE_VECTOR = 22;  // CAN1 SCE interrupt
  EXTI9_5_VECTOR = 23;  // EXTI Line[9:5] interrupt
  TIM1_BRK_TIM9_VECTOR = 24;  // TIM1 Break interrupt and TIM9 global interrupt
  TIM1_UP_TIM10_VECTOR = 25;  // TIM1 Update interrupt and TIM10 global interrupt
  TIM1_TRG_COM_TIM11_VECTOR = 26;  // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
  TIM1_CC_VECTOR = 27;  // TIM1 Capture Compare interrupt
  TIM2_VECTOR = 28;  // TIM2 global interrupt
  TIM3_VECTOR = 29;  // TIM3 global interrupt
  TIM4_VECTOR = 30;  // TIM4 global interrupt
  I2C1_EV_VECTOR = 31;  // I2C1 Event interrupt
  I2C1_ER_VECTOR = 32;  // I2C1 Error interrupt
  I2C2_EV_VECTOR = 33;  // I2C2 Event interrupt
  I2C2_ER_VECTOR = 34;  // I2C2 Error interrupt
  SPI1_VECTOR = 35;  // SPI1 global interrupt
  SPI2_VECTOR = 36;  // SPI2 global interrupt
  USART1_VECTOR = 37;  // USART1 global interrupt
  USART2_VECTOR = 38;  // USART2 global interrupt
  USART3_VECTOR = 39;  // USART3 global interrupt
  EXTI15_10_VECTOR = 40;  // EXTI Line[15:10] interrupts
  RTC_ALARM_VECTOR = 41;  // RTC Alarm (A and B) through EXTI Line interrupt
  OTG_FS_WKUP_VECTOR = 42;  // USB OTG FS Wakeup through EXTI interrupt
  TIM8_BRK_TIM12_VECTOR = 43;  // TIM8 Break interrupt and TIM12 global interrupt
  TIM8_UP_TIM13_VECTOR = 44;  // TIM8 Update interrupt and TIM13 global interrupt
  TIM8_TRG_COM_TIM14_VECTOR = 45;  // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
  TIM8_CC_VECTOR = 46;  // TIM8 Capture Compare interrupt
  SPI3_VECTOR = 47;  // SPI3 global interrupt
  UART4_VECTOR = 48;  // UART4 global interrupt
  UART5_VECTOR = 49;  // UART5 global interrupt
  TIM6_VECTOR = 50;  // TIM6 global interrupt
  TIM7_VECTOR = 51;  // TIM7 global interrupt
  DMA2_STREAM0_VECTOR = 52;  // DMA2 Stream0 global interrupt
  DMA2_STREAM1_VECTOR = 53;  // DMA2 Stream1 global interrupt
  DMA2_STREAM2_VECTOR = 54;  // DMA2 Stream2 global interrupt
  DMA2_STREAM3_VECTOR = 55;  // DMA2 Stream3 global interrupt
  DMA2_STREAM4_VECTOR = 56;  // DMA2 Stream4 global interrupt
  ETH_VECTOR = 57;  // Ethernet global interrupt
  ETH_WKUP_VECTOR = 58;  // Ethernet Wakeup through EXTI interrupt
  CAN2_TX_VECTOR = 59;  // CAN2 TX interrupts
  CAN2_RX0_VECTOR = 60;  // CAN2 RX0 interrupts
  CAN2_RX1_VECTOR = 61;  // CAN2 RX1 interrupt
  CAN2_SCE_VECTOR = 62;  // CAN2 SCE interrupt
  NA_VECTOR = 63;  // Not Available
  OTG_FS_VECTOR = 64;  // USB OTG FS global interrupt
  DCMI_VECTOR = 65;  // DCMI global interrupt
  CRYP_VECTOR = 66;  // CRYP global interrupt
  HASH_RNG_VECTOR = 67;  // HASH and RNG global interrupt
  FPU_VECTOR = 68;  // FPU global interrupt

  // 引脚定义
  PIN_PE2 = 1;  // Tristate - any function
  PIN_PE3 = 2;  // Tristate - any function
  PIN_PE4 = 3;  // Tristate - any function
  PIN_PE5 = 4;  // Tristate - any function
  PIN_PE6 = 5;  // Tristate - any function
  PIN_VCAP1 = 6;  // 1.2V voltage supply
  PIN_VBAT = 7;  // Battery voltage supply
  PIN_PC13 = 8;  // TAMPER-RTC
  PIN_PC14 = 9;  // OSC32_IN
  PIN_PC15 = 10;  // OSC32_OUT
  PIN_PH0 = 11;  // OSC_IN
  PIN_PH1 = 12;  // OSC_OUT
  PIN_NRST = 13;  // External reset
  PIN_VSSA = 14;  // Analog ground
  PIN_VDDA = 15;  // Analog supply
  PIN_PA0 = 16;  // WKUP/USART_CTS
  PIN_PA1 = 17;  // USART_RTS
  PIN_PA2 = 18;  // USART_TX
  PIN_PA3 = 19;  // USART_RX
  PIN_PA4 = 20;  // DAC1_OUT
  PIN_PA5 = 21;  // DAC2_OUT
  PIN_PA6 = 22;  // SPI1_MISO
  PIN_PA7 = 23;  // SPI1_MOSI
  PIN_PA8 = 24;  // MCO1
  PIN_PA9 = 25;  // USART1_TX
  PIN_PA10 = 26;  // USART1_RX
  PIN_PA11 = 27;  // USB_DM
  PIN_PA12 = 28;  // USB_DP
  PIN_PA13 = 29;  // JTMS/SWDIO
  PIN_VCAP2 = 30;  // 1.2V voltage supply
  PIN_PA14 = 31;  // JTCK/SWCLK
  PIN_PA15 = 32;  // JTDI
  PIN_PB0 = 33;  // SPI1_CS/TIM3_CH3
  PIN_PB1 = 34;  // TIM3_CH4
  PIN_PB2 = 35;  // BOOT1
  PIN_PB3 = 36;  // JTDO/TRACESWO
  PIN_PB4 = 37;  // NJTRST
  PIN_PB5 = 38;  // CAN2_RX/TIM3_CH2
  PIN_PB6 = 39;  // USART1_TX
  PIN_PB7 = 40;  // USART1_RX
  PIN_PB8 = 41;  // I2C1_SCL/TIM4_CH1
  PIN_PB9 = 42;  // I2C1_SDA/TIM4_CH2
  PIN_PB10 = 43;  // I2C2_SCL/USART3_TX
  PIN_PB11 = 44;  // I2C2_SDA/USART3_RX
  PIN_PB12 = 45;  // SPI2_CS/I2C2_SMBA
  PIN_PB13 = 46;  // SPI2_SCK
  PIN_PB14 = 47;  // SPI2_MISO
  PIN_PB15 = 48;  // SPI2_MOSI
  PIN_PD0 = 49;  // FSMC_D2
  PIN_PD1 = 50;  // FSMC_D3
  PIN_PD2 = 51;  // TIM3_ETR/UART4_RX
  PIN_PD3 = 52;  // FSMC_CLK/UART4_CTS
  PIN_PD4 = 53;  // FSMC_NOE
  PIN_PD5 = 54;  // FSMC_NWE
  PIN_PD6 = 55;  // FSMC_NWAIT
  PIN_PD7 = 56;  // FSMC_NE1/FSMC_NCE2
  PIN_PD8 = 57;  // FSMC_D13/USART3_TX
  PIN_PD9 = 58;  // FSMC_D14/USART3_RX
  PIN_PD10 = 59;  // FSMC_D15/USART3_CK
  PIN_PD11 = 60;  // FSMC_A16/USART3_CTS
  PIN_PD12 = 61;  // FSMC_A17/TIM4_CH1
  PIN_PD13 = 62;  // FSMC_A18/TIM4_CH2
  PIN_PD14 = 63;  // FSMC_D0/TIM4_CH3
  PIN_PD15 = 64;  // FSMC_D1/TIM4_CH4
  PIN_PG0 = 65;  // FSMC_A10/TIM4_ETR
  PIN_PG1 = 66;  // FSMC_A11
  PIN_PE0 = 67;  // FSMC_NBL0/TIM4_CH3
  PIN_PE1 = 68;  // FSMC_NBL1/TIM4_CH4
  PIN_PE3 = 69;  // FSMC_A19
  PIN_PE4 = 70;  // FSMC_A20
  PIN_PE5 = 71;  // FSMC_A21
  PIN_PE6 = 72;  // FSMC_A22/TIM4_CH1
  PIN_PE7 = 73;  // FSMC_D4/USART6_RX
  PIN_PE8 = 74;  // FSMC_D5/USART6_TX
  PIN_PE9 = 75;  // FSMC_D6/TIM4_CH1
  PIN_PE10 = 76;  // FSMC_D7/USART6_RTS
  PIN_PE11 = 77;  // FSMC_D8
  PIN_PE12 = 78;  // FSMC_D9
  PIN_PE13 = 79;  // FSMC_D10
  PIN_PE14 = 80;  // FSMC_D11
  PIN_PE15 = 81;  // FSMC_D12
  PIN_PB10 = 82;  // I2C2_SCL/USART3_TX
  PIN_PB11 = 83;  // I2C2_SDA/USART3_RX
  PIN_PB12 = 84;  // SPI2_CS/I2C2_SMBA
  PIN_PB13 = 85;  // SPI2_SCK
  PIN_PB14 = 86;  // SPI2_MISO
  PIN_PB15 = 87;  // SPI2_MOSI
  PIN_PD8 = 88;  // FSMC_D13/USART3_TX
  PIN_PD9 = 89;  // FSMC_D14/USART3_RX
  PIN_PD10 = 90;  // FSMC_D15/USART3_CK
  PIN_PD11 = 91;  // FSMC_A16/USART3_CTS
  PIN_PD12 = 92;  // FSMC_A17/TIM4_CH1
  PIN_PD13 = 93;  // FSMC_A18/TIM4_CH2
  PIN_PD14 = 94;  // FSMC_D0/TIM4_CH3
  PIN_PD15 = 95;  // FSMC_D1/TIM4_CH4
  PIN_PG2 = 96;  // FSMC_A12
  PIN_PG3 = 97;  // FSMC_A13
  PIN_PG4 = 98;  // FSMC_A14
  PIN_PG5 = 99;  // FSMC_A15
  PIN_PG6 = 100;  // FSMC_INT2
  PIN_PG7 = 101;  // FSMC_INT3
  PIN_PG8 = 102;  // FSMC_INT4
  PIN_PG9 = 103;  // FSMC_NE2/FSMC_NCE3
  PIN_PG10 = 104;  // FSMC_NCE4_1
  PIN_PG11 = 105;  // FSMC_NCE4_2
  PIN_PG12 = 106;  // FSMC_NCE4_3
  PIN_PG13 = 107;  // FSMC_A24
  PIN_PG14 = 108;  // FSMC_A25
  PIN_PG15 = 109;  // FSMC_INT2
  PIN_VSS = 110;  // Ground
  PIN_VDD = 111;  // 3.3V Supply

type
  TSTM32F407VGT6 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure stm32f407vgt6_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure stm32f407vgt6_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
