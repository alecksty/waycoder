(*
 * STM32F407VGT6梯形图定义
 * 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: High-performance ARM Cortex-M4 with FPU, 168MHz, 1MB Flash, 192KB SRAM
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 16000000 Hz
 *)

PROGRAM STM32F407VGT6
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00000000 : DWORD; (* General Purpose Register 0 *)
    R1 AT %M0x00000004 : DWORD; (* General Purpose Register 1 *)
    R2 AT %M0x00000008 : DWORD; (* General Purpose Register 2 *)
    R3 AT %M0x0000000C : DWORD; (* General Purpose Register 3 *)
    R4 AT %M0x00000010 : DWORD; (* General Purpose Register 4 *)
    R5 AT %M0x00000014 : DWORD; (* General Purpose Register 5 *)
    R6 AT %M0x00000018 : DWORD; (* General Purpose Register 6 *)
    R7 AT %M0x0000001C : DWORD; (* General Purpose Register 7 *)
    R8 AT %M0x00000020 : DWORD; (* General Purpose Register 8 *)
    R9 AT %M0x00000024 : DWORD; (* General Purpose Register 9 *)
    R10 AT %M0x00000028 : DWORD; (* General Purpose Register 10 *)
    R11 AT %M0x0000002C : DWORD; (* General Purpose Register 11 *)
    R12 AT %M0x00000030 : DWORD; (* General Purpose Register 12 *)
    SP AT %M0x00000034 : DWORD; (* Stack Pointer *)
    LR AT %M0x00000038 : DWORD; (* Link Register *)
    PC AT %M0x0000003C : DWORD; (* Program Counter *)
    PSR AT %M0x00000040 : DWORD; (* Program Status Register *)
    PSR_N AT %M0x00000040.31 : BOOL; (* Negative Flag *)
    PSR_Z AT %M0x00000040.30 : BOOL; (* Zero Flag *)
    PSR_C AT %M0x00000040.29 : BOOL; (* Carry Flag *)
    PSR_V AT %M0x00000040.28 : BOOL; (* Overflow Flag *)
    PSR_Q AT %M0x00000040.27 : BOOL; (* SAT Flag *)
    PSR_ICI AT %M0x00000040.0 : BOOL; (* ICI/IT *)
    PSR_GE AT %M0x00000040.0 : BOOL; (* Greater than or Equal *)
    PSR_IT AT %M0x00000040.0 : BOOL; (* IT status *)
    PSR_Q AT %M0x00000040.9 : BOOL; (* APSR *)
    PSR_IPSR AT %M0x00000040.0 : BOOL; (* IPSR *)
    PRIMASK AT %M0xE0000E20 : DWORD; (* Priority Mask Register *)
    BASEPRI AT %M0xE0000E24 : DWORD; (* Base Priority Register *)
    FAULTMASK AT %M0xE0000E28 : DWORD; (* Fault Mask Register *)
    CONTROL AT %M0xE0000E2C : DWORD; (* Control Register *)
    FPSCR AT %ME0000EF34 : DWORD; (* FPU Status Control *)
    S0 AT %M0xE0000EF00 : DWORD; (* FP Register 0 *)
    S1 AT %M0xE0000EF04 : DWORD; (* FP Register 1 *)
    S2 AT %M0xE0000EF08 : DWORD; (* FP Register 2 *)
    S3 AT %M0xE0000EF0C : DWORD; (* FP Register 3 *)
    S4 AT %M0xE0000EF10 : DWORD; (* FP Register 4 *)
    S5 AT %M0xE0000EF14 : DWORD; (* FP Register 5 *)
    S6 AT %M0xE0000EF18 : DWORD; (* FP Register 6 *)
    S7 AT %M0xE0000EF1C : DWORD; (* FP Register 7 *)
    S8 AT %M0xE0000EF20 : DWORD; (* FP Register 8 *)
    S9 AT %M0xE0000EF24 : DWORD; (* FP Register 9 *)
    S10 AT %M0xE0000EF28 : DWORD; (* FP Register 10 *)
    S11 AT %M0xE0000EF2C : DWORD; (* FP Register 11 *)
    S12 AT %M0xE0000EF30 : DWORD; (* FP Register 12 *)
    S13 AT %M0xE0000EF34 : DWORD; (* FP Register 13 *)
    S14 AT %M0xE0000EF38 : DWORD; (* FP Register 14 *)
    S15 AT %M0xE0000EF3C : DWORD; (* FP Register 15 *)
    S16 AT %M0xE0000EF40 : DWORD; (* FP Register 16 *)
    S17 AT %M0xE0000EF44 : DWORD; (* FP Register 17 *)
    S18 AT %M0xE0000EF48 : DWORD; (* FP Register 18 *)
    S19 AT %M0xE0000EF4C : DWORD; (* FP Register 19 *)
    S20 AT %M0xE0000EF50 : DWORD; (* FP Register 20 *)
    S21 AT %M0xE0000EF54 : DWORD; (* FP Register 21 *)
    S22 AT %M0xE0000EF58 : DWORD; (* FP Register 22 *)
    S23 AT %M0xE0000EF5C : DWORD; (* FP Register 23 *)
    S24 AT %M0xE0000EF60 : DWORD; (* FP Register 24 *)
    S25 AT %M0xE0000EF64 : DWORD; (* FP Register 25 *)
    S26 AT %M0xE0000EF68 : DWORD; (* FP Register 26 *)
    S27 AT %M0xE0000EF6C : DWORD; (* FP Register 27 *)
    S28 AT %M0xE0000EF70 : DWORD; (* FP Register 28 *)
    S29 AT %M0xE0000EF74 : DWORD; (* FP Register 29 *)
    S30 AT %M0xE0000EF78 : DWORD; (* FP Register 30 *)
    S31 AT %M0xE0000EF7C : DWORD; (* FP Register 31 *)

    (* 外设变量 *)
    (* Reset and Clock Control *)
    RCC_BASE AT %MW0x40023800 : WORD;
    RCC_CR AT %MW0x0000 : DWORD;
    RCC_PLLCFGR AT %MW0x0004 : DWORD;
    RCC_CFGR AT %MW0x0008 : DWORD;
    RCC_CIR AT %MW0x000C : DWORD;
    RCC_APB2RSTR AT %MW0x0010 : DWORD;
    RCC_APB1RSTR AT %MW0x0014 : DWORD;
    RCC_AHB1ENR AT %MW0x0030 : DWORD;
    RCC_AHB2ENR AT %MW0x0034 : DWORD;
    RCC_AHB3ENR AT %MW0x0038 : DWORD;
    RCC_APB2ENR AT %MW0x0040 : DWORD;
    RCC_APB1ENR AT %MW0x0044 : DWORD;
    RCC_BDCR AT %MW0x0050 : DWORD;
    RCC_CSR AT %MW0x0054 : DWORD;
    RCC_AHB1RSTR AT %MW0x0020 : DWORD;
    RCC_AHB2RSTR AT %MW0x0024 : DWORD;
    RCC_AHB3RSTR AT %MW0x0028 : DWORD;
    RCC_CFGR2 AT %MW0x0018 : DWORD;
    RCC_CFGR3 AT %MW0x001C : DWORD;
    RCC_PLL2CFGR AT %MW0x0060 : DWORD;
    RCC_PLL2DIV AT %MW0x0064 : DWORD;
    RCC_PLL3CFGR AT %MW0x0068 : DWORD;
    RCC_PLL3DIV AT %MW0x006C : DWORD;
    (* Flash Interface *)
    FLASH_BASE AT %MW0x40023C00 : WORD;
    FLASH_ACR AT %MW0x0000 : DWORD;
    FLASH_KEYR AT %MW0x0004 : DWORD;
    FLASH_OPTKEYR AT %MW0x0008 : DWORD;
    FLASH_SR AT %MW0x000C : DWORD;
    FLASH_CR AT %MW0x0010 : DWORD;
    FLASH_OPTCR AT %MW0x0014 : DWORD;
    FLASH_OPTCR1 AT %MW0x0018 : DWORD;
    (* Power Control *)
    PWR_BASE AT %MW0x40007000 : WORD;
    PWR_CR AT %MW0x0000 : DWORD;
    PWR_CSR AT %MW0x0004 : DWORD;
    (* DMA1 Controller *)
    DMA1_BASE AT %MW0x40026000 : WORD;
    DMA1_LIFCR AT %MW0x0000 : DWORD;
    DMA1_HIFCR AT %MW0x0004 : DWORD;
    DMA1_LISR AT %MW0x0008 : DWORD;
    DMA1_HISR AT %MW0x000C : DWORD;
    DMA1_S0CR AT %MW0x0010 : DWORD;
    DMA1_S0NDTR AT %MW0x0014 : DWORD;
    DMA1_S0PAR AT %MW0x0018 : DWORD;
    DMA1_S0M0AR AT %MW0x001C : DWORD;
    DMA1_S0M1AR AT %MW0x0020 : DWORD;
    DMA1_S0FCR AT %MW0x0024 : DWORD;
    DMA1_S1CR AT %MW0x0028 : DWORD;
    DMA1_S1NDTR AT %MW0x002C : DWORD;
    DMA1_S2CR AT %MW0x0040 : DWORD;
    DMA1_S3CR AT %MW0x0058 : DWORD;
    DMA1_S4CR AT %MW0x0070 : DWORD;
    DMA1_S5CR AT %MW0x0088 : DWORD;
    DMA1_S6CR AT %MW0x00A0 : DWORD;
    DMA1_S7CR AT %MW0x00B8 : DWORD;
    (* DMA2 Controller *)
    DMA2_BASE AT %MW0x40026400 : WORD;
    DMA2_LIFCR AT %MW0x0000 : DWORD;
    DMA2_HIFCR AT %MW0x0004 : DWORD;
    DMA2_LISR AT %MW0x0008 : DWORD;
    DMA2_HISR AT %MW0x000C : DWORD;
    DMA2_S0CR AT %MW0x0010 : DWORD;
    DMA2_S1CR AT %MW0x0028 : DWORD;
    DMA2_S2CR AT %MW0x0040 : DWORD;
    DMA2_S3CR AT %MW0x0058 : DWORD;
    DMA2_S4CR AT %MW0x0070 : DWORD;
    DMA2_S5CR AT %MW0x0088 : DWORD;
    DMA2_S6CR AT %MW0x00A0 : DWORD;
    DMA2_S7CR AT %MW0x00B8 : DWORD;
    (* USART1 *)
    USART1_BASE AT %MW0x40011000 : WORD;
    USART1_SR AT %MW0x0000 : DWORD;
    USART1_DR AT %MW0x0004 : DWORD;
    USART1_BRR AT %MW0x0008 : DWORD;
    USART1_CR1 AT %MW0x000C : DWORD;
    USART1_CR2 AT %MW0x0010 : DWORD;
    USART1_CR3 AT %MW0x0014 : DWORD;
    USART1_GTPR AT %MW0x0018 : DWORD;
    (* USART2 *)
    USART2_BASE AT %MW0x40004400 : WORD;
    USART2_SR AT %MW0x0000 : DWORD;
    USART2_DR AT %MW0x0004 : DWORD;
    USART2_BRR AT %MW0x0008 : DWORD;
    USART2_CR1 AT %MW0x000C : DWORD;
    USART2_CR2 AT %MW0x0010 : DWORD;
    USART2_CR3 AT %MW0x0014 : DWORD;
    (* USART3 *)
    USART3_BASE AT %MW0x40004800 : WORD;
    USART3_SR AT %MW0x0000 : DWORD;
    USART3_DR AT %MW0x0004 : DWORD;
    USART3_BRR AT %MW0x0008 : DWORD;
    USART3_CR1 AT %MW0x000C : DWORD;
    USART3_CR2 AT %MW0x0010 : DWORD;
    USART3_CR3 AT %MW0x0014 : DWORD;
    (* SPI1 *)
    SPI1_BASE AT %MW0x40013000 : WORD;
    SPI1_CR1 AT %MW0x0000 : DWORD;
    SPI1_CR2 AT %MW0x0004 : DWORD;
    SPI1_SR AT %MW0x0008 : DWORD;
    SPI1_DR AT %MW0x000C : DWORD;
    SPI1_CRCPR AT %MW0x0010 : DWORD;
    SPI1_RXCRCR AT %MW0x0014 : DWORD;
    SPI1_TXCRCR AT %MW0x0018 : DWORD;
    SPI1_I2SCFGR AT %MW0x001C : DWORD;
    (* SPI2 *)
    SPI2_BASE AT %MW0x40003800 : WORD;
    SPI2_CR1 AT %MW0x0000 : DWORD;
    SPI2_CR2 AT %MW0x0004 : DWORD;
    SPI2_SR AT %MW0x0008 : DWORD;
    SPI2_DR AT %MW0x000C : DWORD;
    SPI2_CRCPR AT %MW0x0010 : DWORD;
    SPI2_RXCRCR AT %MW0x0014 : DWORD;
    (* SPI3 *)
    SPI3_BASE AT %MW0x40003C00 : WORD;
    SPI3_CR1 AT %MW0x0000 : DWORD;
    SPI3_CR2 AT %MW0x0004 : DWORD;
    SPI3_SR AT %MW0x0008 : DWORD;
    SPI3_DR AT %MW0x000C : DWORD;
    (* I2C1 *)
    I2C1_BASE AT %MW0x40005400 : WORD;
    I2C1_CR1 AT %MW0x0000 : DWORD;
    I2C1_CR2 AT %MW0x0004 : DWORD;
    I2C1_OAR1 AT %MW0x0008 : DWORD;
    I2C1_OAR2 AT %MW0x000C : DWORD;
    I2C1_DR AT %MW0x0010 : DWORD;
    I2C1_SR1 AT %MW0x0014 : DWORD;
    I2C1_SR2 AT %MW0x0018 : DWORD;
    I2C1_CCR AT %MW0x001C : DWORD;
    I2C1_TRISE AT %MW0x0020 : DWORD;
    I2C1_FLTR AT %MW0x0024 : DWORD;
    (* I2C2 *)
    I2C2_BASE AT %MW0x40005800 : WORD;
    I2C2_CR1 AT %MW0x0000 : DWORD;
    I2C2_CR2 AT %MW0x0004 : DWORD;
    I2C2_OAR1 AT %MW0x0008 : DWORD;
    I2C2_OAR2 AT %MW0x000C : DWORD;
    I2C2_DR AT %MW0x0010 : DWORD;
    I2C2_SR1 AT %MW0x0014 : DWORD;
    I2C2_SR2 AT %MW0x0018 : DWORD;
    I2C2_CCR AT %MW0x001C : DWORD;
    I2C2_TRISE AT %MW0x0020 : DWORD;
    (* I2C3 *)
    I2C3_BASE AT %MW0x40005C00 : WORD;
    I2C3_CR1 AT %MW0x0000 : DWORD;
    I2C3_CR2 AT %MW0x0004 : DWORD;
    I2C3_OAR1 AT %MW0x0008 : DWORD;
    I2C3_DR AT %MW0x0010 : DWORD;
    I2C3_SR1 AT %MW0x0014 : DWORD;
    I2C3_SR2 AT %MW0x0018 : DWORD;
    I2C3_CCR AT %MW0x001C : DWORD;
    (* Advanced Timer 1 *)
    TIM1_BASE AT %MW0x40012C00 : WORD;
    TIM1_CR1 AT %MW0x0000 : DWORD;
    TIM1_CR2 AT %MW0x0004 : DWORD;
    TIM1_SMCR AT %MW0x0008 : DWORD;
    TIM1_DIER AT %MW0x000C : DWORD;
    TIM1_SR AT %MW0x0010 : DWORD;
    TIM1_EGR AT %MW0x0014 : DWORD;
    TIM1_CCMR1 AT %MW0x0018 : DWORD;
    TIM1_CCMR2 AT %MW0x001C : DWORD;
    TIM1_CCER AT %MW0x0020 : DWORD;
    TIM1_CNT AT %MW0x0024 : DWORD;
    TIM1_PSC AT %MW0x0028 : DWORD;
    TIM1_ARR AT %MW0x002C : DWORD;
    TIM1_RCR AT %MW0x0030 : DWORD;
    TIM1_CCR1 AT %MW0x0034 : DWORD;
    TIM1_CCR2 AT %MW0x0038 : DWORD;
    TIM1_CCR3 AT %MW0x003C : DWORD;
    TIM1_CCR4 AT %MW0x0040 : DWORD;
    TIM1_BDTR AT %MW0x0044 : DWORD;
    TIM1_DCR AT %MW0x0048 : DWORD;
    TIM1_DMAR AT %MW0x004C : DWORD;
    (* General Purpose Timer 2 *)
    TIM2_BASE AT %MW0x40000000 : WORD;
    TIM2_CR1 AT %MW0x0000 : DWORD;
    TIM2_CR2 AT %MW0x0004 : DWORD;
    TIM2_SMCR AT %MW0x0008 : DWORD;
    TIM2_DIER AT %MW0x000C : DWORD;
    TIM2_SR AT %MW0x0010 : DWORD;
    TIM2_EGR AT %MW0x0014 : DWORD;
    TIM2_CCMR1 AT %MW0x0018 : DWORD;
    TIM2_CCMR2 AT %MW0x001C : DWORD;
    TIM2_CCER AT %MW0x0020 : DWORD;
    TIM2_CNT AT %MW0x0024 : DWORD;
    TIM2_PSC AT %MW0x0028 : DWORD;
    TIM2_ARR AT %MW0x002C : DWORD;
    TIM2_CCR1 AT %MW0x0034 : DWORD;
    TIM2_CCR2 AT %MW0x0038 : DWORD;
    TIM2_CCR3 AT %MW0x003C : DWORD;
    TIM2_CCR4 AT %MW0x0040 : DWORD;
    (* General Purpose Timer 3 *)
    TIM3_BASE AT %MW0x40000400 : WORD;
    TIM3_CR1 AT %MW0x0000 : DWORD;
    TIM3_SR AT %MW0x0010 : DWORD;
    TIM3_CCMR1 AT %MW0x0018 : DWORD;
    TIM3_CCER AT %MW0x0020 : DWORD;
    TIM3_CNT AT %MW0x0024 : DWORD;
    TIM3_PSC AT %MW0x0028 : DWORD;
    TIM3_ARR AT %MW0x002C : DWORD;
    TIM3_CCR1 AT %MW0x0034 : DWORD;
    TIM3_CCR2 AT %MW0x0038 : DWORD;
    TIM3_CCR3 AT %MW0x003C : DWORD;
    TIM3_CCR4 AT %MW0x0040 : DWORD;
    (* General Purpose Timer 4 *)
    TIM4_BASE AT %MW0x40000800 : WORD;
    TIM4_CR1 AT %MW0x0000 : DWORD;
    TIM4_SR AT %MW0x0010 : DWORD;
    TIM4_CCMR1 AT %MW0x0018 : DWORD;
    TIM4_CCER AT %MW0x0020 : DWORD;
    TIM4_CNT AT %MW0x0024 : DWORD;
    TIM4_PSC AT %MW0x0028 : DWORD;
    TIM4_ARR AT %MW0x002C : DWORD;
    TIM4_CCR1 AT %MW0x0034 : DWORD;
    TIM4_CCR2 AT %MW0x0038 : DWORD;
    TIM4_CCR3 AT %MW0x003C : DWORD;
    TIM4_CCR4 AT %MW0x0040 : DWORD;
    (* General Purpose Timer 5 *)
    TIM5_BASE AT %MW0x40000C00 : WORD;
    TIM5_CR1 AT %MW0x0000 : DWORD;
    TIM5_SR AT %MW0x0010 : DWORD;
    TIM5_CNT AT %MW0x0024 : DWORD;
    TIM5_PSC AT %MW0x0028 : DWORD;
    TIM5_ARR AT %MW0x002C : DWORD;
    TIM5_CCR1 AT %MW0x0034 : DWORD;
    TIM5_CCR2 AT %MW0x0038 : DWORD;
    TIM5_CCR3 AT %MW0x003C : DWORD;
    TIM5_CCR4 AT %MW0x0040 : DWORD;
    (* General Purpose Timer 9 *)
    TIM9_BASE AT %MW0x40014C00 : WORD;
    TIM9_CR1 AT %MW0x0000 : DWORD;
    TIM9_SMCR AT %MW0x0008 : DWORD;
    TIM9_DIER AT %MW0x000C : DWORD;
    TIM9_SR AT %MW0x0010 : DWORD;
    TIM9_EGR AT %MW0x0014 : DWORD;
    TIM9_CCMR1 AT %MW0x0018 : DWORD;
    TIM9_CCER AT %MW0x0020 : DWORD;
    TIM9_CNT AT %MW0x0024 : DWORD;
    TIM9_PSC AT %MW0x0028 : DWORD;
    TIM9_ARR AT %MW0x002C : DWORD;
    TIM9_CCR1 AT %MW0x0034 : DWORD;
    TIM9_CCR2 AT %MW0x0038 : DWORD;
    (* General Purpose Timer 10 *)
    TIM10_BASE AT %MW0x40015000 : WORD;
    TIM10_CR1 AT %MW0x0000 : DWORD;
    TIM10_DIER AT %MW0x000C : DWORD;
    TIM10_SR AT %MW0x0010 : DWORD;
    TIM10_EGR AT %MW0x0014 : DWORD;
    TIM10_CCMR1 AT %MW0x0018 : DWORD;
    TIM10_CCER AT %MW0x0020 : DWORD;
    TIM10_CNT AT %MW0x0024 : DWORD;
    TIM10_PSC AT %MW0x0028 : DWORD;
    TIM10_ARR AT %MW0x002C : DWORD;
    TIM10_CCR1 AT %MW0x0034 : DWORD;
    (* ADC1 *)
    ADC1_BASE AT %MW0x40012000 : WORD;
    ADC1_SR AT %MW0x0000 : DWORD;
    ADC1_CR1 AT %MW0x0004 : DWORD;
    ADC1_CR2 AT %MW0x0008 : DWORD;
    ADC1_SMPR1 AT %MW0x000C : DWORD;
    ADC1_SMPR2 AT %MW0x0010 : DWORD;
    ADC1_JOFR1 AT %MW0x0014 : DWORD;
    ADC1_JOFR2 AT %MW0x0018 : DWORD;
    ADC1_JOFR3 AT %MW0x001C : DWORD;
    ADC1_JOFR4 AT %MW0x0020 : DWORD;
    ADC1_HTR AT %MW0x0024 : DWORD;
    ADC1_LTR AT %MW0x0028 : DWORD;
    ADC1_SQR1 AT %MW0x002C : DWORD;
    ADC1_SQR2 AT %MW0x0030 : DWORD;
    ADC1_SQR3 AT %MW0x0034 : DWORD;
    ADC1_JSQR AT %MW0x003C : DWORD;
    ADC1_JDR1 AT %MW0x0040 : DWORD;
    ADC1_JDR2 AT %MW0x0044 : DWORD;
    ADC1_JDR3 AT %MW0x0048 : DWORD;
    ADC1_JDR4 AT %MW0x004C : DWORD;
    ADC1_DR AT %MW0x0050 : DWORD;
    (* ADC2 *)
    ADC2_BASE AT %MW0x40012100 : WORD;
    ADC2_SR AT %MW0x0000 : DWORD;
    ADC2_CR1 AT %MW0x0004 : DWORD;
    ADC2_CR2 AT %MW0x0008 : DWORD;
    ADC2_SMPR1 AT %MW0x000C : DWORD;
    ADC2_SMPR2 AT %MW0x0010 : DWORD;
    ADC2_SQR1 AT %MW0x002C : DWORD;
    ADC2_DR AT %MW0x0050 : DWORD;
    (* ADC3 *)
    ADC3_BASE AT %MW0x40012200 : WORD;
    ADC3_SR AT %MW0x0000 : DWORD;
    ADC3_CR1 AT %MW0x0004 : DWORD;
    ADC3_CR2 AT %MW0x0008 : DWORD;
    ADC3_SMPR1 AT %MW0x000C : DWORD;
    ADC3_SMPR2 AT %MW0x0010 : DWORD;
    ADC3_SQR1 AT %MW0x002C : DWORD;
    ADC3_DR AT %MW0x0050 : DWORD;
    (* System Configuration Controller *)
    SYSCFG_BASE AT %MW0x40013800 : WORD;
    SYSCFG_CFGR AT %MW0x0000 : DWORD;
    SYSCFG_EXTICR1 AT %MW0x0008 : DWORD;
    SYSCFG_EXTICR2 AT %MW0x000C : DWORD;
    SYSCFG_EXTICR3 AT %MW0x0010 : DWORD;
    SYSCFG_EXTICR4 AT %MW0x0014 : DWORD;
    SYSCFG_CBR AT %MW0x001C : DWORD;
    (* External Interrupt/Event Controller *)
    EXTI_BASE AT %MW0x40013C00 : WORD;
    EXTI_IMR AT %MW0x0000 : DWORD;
    EXTI_EMR AT %MW0x0004 : DWORD;
    EXTI_RTSR AT %MW0x0008 : DWORD;
    EXTI_FTSR AT %MW0x000C : DWORD;
    EXTI_SWIER AT %MW0x0010 : DWORD;
    EXTI_PR AT %MW0x0014 : DWORD;
    (* Random Number Generator *)
    RNG_BASE AT %MW0x50060800 : WORD;
    RNG_CR AT %MW0x0000 : DWORD;
    RNG_SR AT %MW0x0004 : DWORD;
    RNG_DR AT %MW0x0008 : DWORD;
    (* CRYP Accelerator *)
    CRYP_BASE AT %MW0x50060000 : WORD;
    CRYP_CR AT %MW0x0000 : DWORD;
    CRYP_SR AT %MW0x0004 : DWORD;
    CRYP_DIN AT %MW0x0008 : DWORD;
    CRYP_DOUT AT %MW0x000C : DWORD;
    CRYP_DMACR AT %MW0x0010 : DWORD;
    CRYP_IMSCR AT %MW0x0014 : DWORD;
    CRYP_RISR AT %MW0x0018 : DWORD;
    CRYP_MISR AT %MW0x001C : DWORD;
    CRYP_K0LR AT %MW0x0020 : DWORD;
    CRYP_K0RR AT %MW0x0024 : DWORD;
    CRYP_K1LR AT %MW0x0028 : DWORD;
    CRYP_K1RR AT %MW0x002C : DWORD;
    CRYP_K2LR AT %MW0x0030 : DWORD;
    CRYP_K2RR AT %MW0x0034 : DWORD;
    CRYP_K3LR AT %MW0x0038 : DWORD;
    CRYP_K3RR AT %MW0x003C : DWORD;
    CRYP_IV0LR AT %MW0x0040 : DWORD;
    CRYP_IV0RR AT %MW0x0044 : DWORD;
    CRYP_IV1LR AT %MW0x0048 : DWORD;
    CRYP_IV1RR AT %MW0x004C : DWORD;
    (* HASH Accelerator *)
    HASH_BASE AT %MW0x50060400 : WORD;
    HASH_CR AT %MW0x0000 : DWORD;
    HASH_DIN AT %MW0x0004 : DWORD;
    HASH_DINSTAT AT %MW0x0008 : DWORD;
    HASH_HR AT %MW0x000C : DWORD;
    HASH_IMR AT %MW0x0020 : DWORD;
    HASH_SR AT %MW0x0024 : DWORD;
    (* Digital Camera Interface *)
    DCMI_BASE AT %MW0x50050000 : WORD;
    DCMI_CR AT %MW0x0000 : DWORD;
    DCMI_SR AT %MW0x0004 : DWORD;
    DCMI_RISR AT %MW0x0008 : DWORD;
    DCMI_IER AT %MW0x000C : DWORD;
    DCMI_MISR AT %MW0x0010 : DWORD;
    DCMI_ICR AT %MW0x0014 : DWORD;
    DCMI_MFISH AT %MW0x001C : DWORD;
    DCMI_CWSTRT AT %MW0x0020 : DWORD;
    DCMI_CWSIZE AT %MW0x0024 : DWORD;
    DCMI_DR AT %MW0x0028 : DWORD;
    DCMI_OR AT %MW0x002C : DWORD;
    (* USB OTG High Speed *)
    USB_OTG_HS_BASE AT %MW0x40040000 : WORD;
    USB_OTG_HS_GOTGCTL AT %MW0x0000 : DWORD;
    USB_OTG_HS_GOTGINT AT %MW0x0004 : DWORD;
    USB_OTG_HS_GINTMSK AT %MW0x0008 : DWORD;
    USB_OTG_HS_GRSTCTL AT %MW0x000C : DWORD;
    USB_OTG_HS_GINTSTS AT %MW0x0010 : DWORD;
    USB_OTG_HS_GRXSTSR AT %MW0x0014 : DWORD;
    USB_OTG_HS_GRXFSIZ AT %MW0x0024 : DWORD;
    USB_OTG_HS_HNPTXFSIZ AT %MW0x0028 : DWORD;
    USB_OTG_HS_HNPTXSTS AT %MW0x002C : DWORD;
    USB_OTG_HS_GCCFG AT %MW0x0038 : DWORD;
    USB_OTG_HS_CID AT %MW0x003C : DWORD;
    USB_OTG_HS_HPTXFSIZ AT %MW0x0100 : DWORD;
    USB_OTG_HS_DIEPTXF AT %MW0x0200 : DWORD;
    (* Ethernet *)
    ETH_BASE AT %MW0x40028000 : WORD;
    ETH_MACCR AT %MW0x0000 : DWORD;
    ETH_MACFFR AT %MW0x0004 : DWORD;
    ETH_MACHTHR AT %MW0x0008 : DWORD;
    ETH_MACHTLR AT %MW0x000C : DWORD;
    ETH_MACMIIAR AT %MW0x0010 : DWORD;
    ETH_MACMIIDR AT %MW0x0014 : DWORD;
    ETH_MACCR AT %MW0x0018 : DWORD;
    ETH_MACVLANTR AT %MW0x001C : DWORD;
    ETH_MACRWUFFR AT %MW0x0028 : DWORD;
    ETH_MACPMTCSR AT %MW0x002C : DWORD;
    ETH_MACSR AT %MW0x0030 : DWORD;
    ETH_MACIMR AT %MW0x0034 : DWORD;
    ETH_MACA0HR AT %MW0x0040 : DWORD;
    ETH_MACA0LR AT %MW0x0044 : DWORD;
    ETH_MACA1HR AT %MW0x0048 : DWORD;
    ETH_MACA1LR AT %MW0x004C : DWORD;
    ETH_MMCCR AT %MW0x0100 : DWORD;
    ETH_MMCRIR AT %MW0x0104 : DWORD;
    ETH_MMCTIR AT %MW0x0108 : DWORD;
    ETH_MMCRIMR AT %MW0x010C : DWORD;
    ETH_MMCTIMR AT %MW0x0110 : DWORD;
    ETH_MMCTGBSCCR AT %MW0x0114 : DWORD;
    ETH_MMCRGUFCCR AT %MW0x0118 : DWORD;
    ETH_PTPTSCR AT %MW0x0700 : DWORD;
    ETH_PTPSSIR AT %MW0x0704 : DWORD;
    ETH_PTPTSHR AT %MW0x0708 : DWORD;
    ETH_PTPTSLR AT %MW0x070C : DWORD;
    ETH_PTPTSHUR AT %MW0x0710 : DWORD;
    ETH_PTPTSLUR AT %MW0x0714 : DWORD;
    ETH_PTPTSAR AT %MW0x0718 : DWORD;
    ETH_PTPTTHR AT %MW0x071C : DWORD;
    ETH_PTPTTLR AT %MW0x0720 : DWORD;
    ETH_PTPTSR AT %MW0x0728 : DWORD;
    ETH_DMABMR AT %MW0x1000 : DWORD;
    ETH_DMASR AT %MW0x1004 : DWORD;
    ETH_DMAOMR AT %MW0x1008 : DWORD;
    ETH_DMAIER AT %MW0x100C : DWORD;
    ETH_DMAMFBOCR AT %MW0x1010 : DWORD;
    ETH_DMACHTDR AT %MW0x1014 : DWORD;
    ETH_DMACHRDR AT %MW0x1018 : DWORD;
    ETH_DMACHTBAR AT %MW0x101C : DWORD;
    ETH_DMACHRBAR AT %MW0x1020 : DWORD;

    (* 中断变量 *)
    INT_WWDG : BOOL; (* Window WatchDog interrupt *)
    INT_PVD : BOOL; (* PVD through EXTI line detection interrupt *)
    INT_TAMPER : BOOL; (* Tamper interrupt *)
    INT_RTC_WKUP : BOOL; (* RTC Wakeup interrupt *)
    INT_FLASH : BOOL; (* FLASH global interrupt *)
    INT_RCC : BOOL; (* RCC global interrupt *)
    INT_EXTI0 : BOOL; (* EXTI Line0 interrupt *)
    INT_EXTI1 : BOOL; (* EXTI Line1 interrupt *)
    INT_EXTI2 : BOOL; (* EXTI Line2 interrupt *)
    INT_EXTI3 : BOOL; (* EXTI Line3 interrupt *)
    INT_EXTI4 : BOOL; (* EXTI Line4 interrupt *)
    INT_DMA1_STREAM0 : BOOL; (* DMA1 Stream0 global interrupt *)
    INT_DMA1_STREAM1 : BOOL; (* DMA1 Stream1 global interrupt *)
    INT_DMA1_STREAM2 : BOOL; (* DMA1 Stream2 global interrupt *)
    INT_DMA1_STREAM3 : BOOL; (* DMA1 Stream3 global interrupt *)
    INT_DMA1_STREAM4 : BOOL; (* DMA1 Stream4 global interrupt *)
    INT_DMA1_STREAM5 : BOOL; (* DMA1 Stream5 global interrupt *)
    INT_DMA1_STREAM6 : BOOL; (* DMA1 Stream6 global interrupt *)
    INT_ADC : BOOL; (* ADC1 global interrupt *)
    INT_CAN1_TX : BOOL; (* CAN1 TX interrupts *)
    INT_CAN1_RX0 : BOOL; (* CAN1 RX0 interrupts *)
    INT_CAN1_RX1 : BOOL; (* CAN1 RX1 interrupt *)
    INT_CAN1_SCE : BOOL; (* CAN1 SCE interrupt *)
    INT_EXTI9_5 : BOOL; (* EXTI Line[9:5] interrupt *)
    INT_TIM1_BRK_TIM9 : BOOL; (* TIM1 Break interrupt and TIM9 global interrupt *)
    INT_TIM1_UP_TIM10 : BOOL; (* TIM1 Update interrupt and TIM10 global interrupt *)
    INT_TIM1_TRG_COM_TIM11 : BOOL; (* TIM1 Trigger and commutation interrupt and TIM11 global interrupt *)
    INT_TIM1_CC : BOOL; (* TIM1 Capture Compare interrupt *)
    INT_TIM2 : BOOL; (* TIM2 global interrupt *)
    INT_TIM3 : BOOL; (* TIM3 global interrupt *)
    INT_TIM4 : BOOL; (* TIM4 global interrupt *)
    INT_I2C1_EV : BOOL; (* I2C1 Event interrupt *)
    INT_I2C1_ER : BOOL; (* I2C1 Error interrupt *)
    INT_I2C2_EV : BOOL; (* I2C2 Event interrupt *)
    INT_I2C2_ER : BOOL; (* I2C2 Error interrupt *)
    INT_SPI1 : BOOL; (* SPI1 global interrupt *)
    INT_SPI2 : BOOL; (* SPI2 global interrupt *)
    INT_USART1 : BOOL; (* USART1 global interrupt *)
    INT_USART2 : BOOL; (* USART2 global interrupt *)
    INT_USART3 : BOOL; (* USART3 global interrupt *)
    INT_EXTI15_10 : BOOL; (* EXTI Line[15:10] interrupts *)
    INT_RTC_ALARM : BOOL; (* RTC Alarm (A and B) through EXTI Line interrupt *)
    INT_OTG_FS_WKUP : BOOL; (* USB OTG FS Wakeup through EXTI interrupt *)
    INT_TIM8_BRK_TIM12 : BOOL; (* TIM8 Break interrupt and TIM12 global interrupt *)
    INT_TIM8_UP_TIM13 : BOOL; (* TIM8 Update interrupt and TIM13 global interrupt *)
    INT_TIM8_TRG_COM_TIM14 : BOOL; (* TIM8 Trigger and commutation interrupt and TIM14 global interrupt *)
    INT_TIM8_CC : BOOL; (* TIM8 Capture Compare interrupt *)
    INT_SPI3 : BOOL; (* SPI3 global interrupt *)
    INT_UART4 : BOOL; (* UART4 global interrupt *)
    INT_UART5 : BOOL; (* UART5 global interrupt *)
    INT_TIM6 : BOOL; (* TIM6 global interrupt *)
    INT_TIM7 : BOOL; (* TIM7 global interrupt *)
    INT_DMA2_STREAM0 : BOOL; (* DMA2 Stream0 global interrupt *)
    INT_DMA2_STREAM1 : BOOL; (* DMA2 Stream1 global interrupt *)
    INT_DMA2_STREAM2 : BOOL; (* DMA2 Stream2 global interrupt *)
    INT_DMA2_STREAM3 : BOOL; (* DMA2 Stream3 global interrupt *)
    INT_DMA2_STREAM4 : BOOL; (* DMA2 Stream4 global interrupt *)
    INT_ETH : BOOL; (* Ethernet global interrupt *)
    INT_ETH_WKUP : BOOL; (* Ethernet Wakeup through EXTI interrupt *)
    INT_CAN2_TX : BOOL; (* CAN2 TX interrupts *)
    INT_CAN2_RX0 : BOOL; (* CAN2 RX0 interrupts *)
    INT_CAN2_RX1 : BOOL; (* CAN2 RX1 interrupt *)
    INT_CAN2_SCE : BOOL; (* CAN2 SCE interrupt *)
    INT_NA : BOOL; (* Not Available *)
    INT_OTG_FS : BOOL; (* USB OTG FS global interrupt *)
    INT_DCMI : BOOL; (* DCMI global interrupt *)
    INT_CRYP : BOOL; (* CRYP global interrupt *)
    INT_HASH_RNG : BOOL; (* HASH and RNG global interrupt *)
    INT_FPU : BOOL; (* FPU global interrupt *)

END_VAR

BEGIN
    (* STM32F407VGT6初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_WWDG|--[中断处理]--
    --|INT_PVD|--[中断处理]--
    --|INT_TAMPER|--[中断处理]--
    --|INT_RTC_WKUP|--[中断处理]--
    --|INT_FLASH|--[中断处理]--
    --|INT_RCC|--[中断处理]--
    --|INT_EXTI0|--[中断处理]--
    --|INT_EXTI1|--[中断处理]--
    --|INT_EXTI2|--[中断处理]--
    --|INT_EXTI3|--[中断处理]--
    --|INT_EXTI4|--[中断处理]--
    --|INT_DMA1_STREAM0|--[中断处理]--
    --|INT_DMA1_STREAM1|--[中断处理]--
    --|INT_DMA1_STREAM2|--[中断处理]--
    --|INT_DMA1_STREAM3|--[中断处理]--
    --|INT_DMA1_STREAM4|--[中断处理]--
    --|INT_DMA1_STREAM5|--[中断处理]--
    --|INT_DMA1_STREAM6|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_CAN1_TX|--[中断处理]--
    --|INT_CAN1_RX0|--[中断处理]--
    --|INT_CAN1_RX1|--[中断处理]--
    --|INT_CAN1_SCE|--[中断处理]--
    --|INT_EXTI9_5|--[中断处理]--
    --|INT_TIM1_BRK_TIM9|--[中断处理]--
    --|INT_TIM1_UP_TIM10|--[中断处理]--
    --|INT_TIM1_TRG_COM_TIM11|--[中断处理]--
    --|INT_TIM1_CC|--[中断处理]--
    --|INT_TIM2|--[中断处理]--
    --|INT_TIM3|--[中断处理]--
    --|INT_TIM4|--[中断处理]--
    --|INT_I2C1_EV|--[中断处理]--
    --|INT_I2C1_ER|--[中断处理]--
    --|INT_I2C2_EV|--[中断处理]--
    --|INT_I2C2_ER|--[中断处理]--
    --|INT_SPI1|--[中断处理]--
    --|INT_SPI2|--[中断处理]--
    --|INT_USART1|--[中断处理]--
    --|INT_USART2|--[中断处理]--
    --|INT_USART3|--[中断处理]--
    --|INT_EXTI15_10|--[中断处理]--
    --|INT_RTC_ALARM|--[中断处理]--
    --|INT_OTG_FS_WKUP|--[中断处理]--
    --|INT_TIM8_BRK_TIM12|--[中断处理]--
    --|INT_TIM8_UP_TIM13|--[中断处理]--
    --|INT_TIM8_TRG_COM_TIM14|--[中断处理]--
    --|INT_TIM8_CC|--[中断处理]--
    --|INT_SPI3|--[中断处理]--
    --|INT_UART4|--[中断处理]--
    --|INT_UART5|--[中断处理]--
    --|INT_TIM6|--[中断处理]--
    --|INT_TIM7|--[中断处理]--
    --|INT_DMA2_STREAM0|--[中断处理]--
    --|INT_DMA2_STREAM1|--[中断处理]--
    --|INT_DMA2_STREAM2|--[中断处理]--
    --|INT_DMA2_STREAM3|--[中断处理]--
    --|INT_DMA2_STREAM4|--[中断处理]--
    --|INT_ETH|--[中断处理]--
    --|INT_ETH_WKUP|--[中断处理]--
    --|INT_CAN2_TX|--[中断处理]--
    --|INT_CAN2_RX0|--[中断处理]--
    --|INT_CAN2_RX1|--[中断处理]--
    --|INT_CAN2_SCE|--[中断处理]--
    --|INT_NA|--[中断处理]--
    --|INT_OTG_FS|--[中断处理]--
    --|INT_DCMI|--[中断处理]--
    --|INT_CRYP|--[中断处理]--
    --|INT_HASH_RNG|--[中断处理]--
    --|INT_FPU|--[中断处理]--

END_PROGRAM
