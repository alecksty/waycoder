(*
 * STM32F103C8T6梯形图定义
 * 生成自: STMicroelectronics/STM32/STM32F103C8T6
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
 * CPU架构: ARM-Cortex-M3
 * 位宽: 32位
 * 时钟频率: 72000000 Hz
 *)

PROGRAM STM32F103C8T6
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (* General Purpose Register 0 *)
    R1 AT %M0x04 : DWORD; (* General Purpose Register 1 *)
    R2 AT %M0x08 : DWORD; (* General Purpose Register 2 *)
    R3 AT %M0x0C : DWORD; (* General Purpose Register 3 *)
    R4 AT %M0x10 : DWORD; (* General Purpose Register 4 *)
    R5 AT %M0x14 : DWORD; (* General Purpose Register 5 *)
    R6 AT %M0x18 : DWORD; (* General Purpose Register 6 *)
    R7 AT %M0x1C : DWORD; (* General Purpose Register 7 *)
    R8 AT %M0x20 : DWORD; (* General Purpose Register 8 *)
    R9 AT %M0x24 : DWORD; (* General Purpose Register 9 *)
    R10 AT %M0x28 : DWORD; (* General Purpose Register 10 *)
    R11 AT %M0x2C : DWORD; (* General Purpose Register 11 *)
    R12 AT %M0x30 : DWORD; (* General Purpose Register 12 *)
    SP AT %M0x34 : DWORD; (* Stack Pointer *)
    LR AT %M0x38 : DWORD; (* Link Register *)
    PC AT %M0x3C : DWORD; (* Program Counter *)
    XPSR AT %M0x40 : DWORD; (* Program Status Register *)

    (* 外设变量 *)
    (* Reset and Clock Control *)
    RCC_BASE AT %MW0x40021000 : WORD;
    RCC_CR AT %MW0x00 : DWORD;
    RCC_CFGR AT %MW0x04 : DWORD;
    RCC_APB2ENR AT %MW0x18 : DWORD;
    RCC_APB1ENR AT %MW0x1C : DWORD;
    (* GPIO Port A *)
    GPIOA_BASE AT %MW0x40010800 : WORD;
    GPIOA_CRL AT %MW0x00 : DWORD;
    GPIOA_CRH AT %MW0x04 : DWORD;
    GPIOA_IDR AT %MW0x08 : DWORD;
    GPIOA_ODR AT %MW0x0C : DWORD;
    GPIOA_BSRR AT %MW0x10 : DWORD;
    GPIOA_BRR AT %MW0x14 : DWORD;
    GPIOA_LCKR AT %MW0x18 : DWORD;
    (* GPIO Port B *)
    GPIOB_BASE AT %MW0x40010C00 : WORD;
    GPIOB_CRL AT %MW0x00 : DWORD;
    GPIOB_CRH AT %MW0x04 : DWORD;
    GPIOB_IDR AT %MW0x08 : DWORD;
    GPIOB_ODR AT %MW0x0C : DWORD;
    GPIOB_BSRR AT %MW0x10 : DWORD;
    GPIOB_BRR AT %MW0x14 : DWORD;
    (* GPIO Port C *)
    GPIOC_BASE AT %MW0x40011000 : WORD;
    GPIOC_CRL AT %MW0x00 : DWORD;
    GPIOC_CRH AT %MW0x04 : DWORD;
    GPIOC_IDR AT %MW0x08 : DWORD;
    GPIOC_ODR AT %MW0x0C : DWORD;
    GPIOC_BSRR AT %MW0x10 : DWORD;
    (* USART 1 *)
    USART1_BASE AT %MW0x40013800 : WORD;
    USART1_SR AT %MW0x00 : DWORD;
    USART1_DR AT %MW0x04 : DWORD;
    USART1_BRR AT %MW0x08 : DWORD;
    USART1_CR1 AT %MW0x0C : DWORD;
    USART1_CR2 AT %MW0x10 : DWORD;
    USART1_CR3 AT %MW0x14 : DWORD;
    USART1_GTPR AT %MW0x18 : DWORD;
    (* USART 2 *)
    USART2_BASE AT %MW0x40004400 : WORD;
    USART2_SR AT %MW0x00 : DWORD;
    USART2_DR AT %MW0x04 : DWORD;
    USART2_BRR AT %MW0x08 : DWORD;
    USART2_CR1 AT %MW0x0C : DWORD;
    (* SPI 1 *)
    SPI1_BASE AT %MW0x40013000 : WORD;
    SPI1_CR1 AT %MW0x00 : DWORD;
    SPI1_CR2 AT %MW0x04 : DWORD;
    SPI1_SR AT %MW0x08 : DWORD;
    SPI1_DR AT %MW0x0C : DWORD;
    (* SPI 2 *)
    SPI2_BASE AT %MW0x40003800 : WORD;
    SPI2_CR1 AT %MW0x00 : DWORD;
    SPI2_CR2 AT %MW0x04 : DWORD;
    SPI2_SR AT %MW0x08 : DWORD;
    SPI2_DR AT %MW0x0C : DWORD;
    (* I2C 1 *)
    I2C1_BASE AT %MW0x40005400 : WORD;
    I2C1_CR1 AT %MW0x00 : DWORD;
    I2C1_CR2 AT %MW0x04 : DWORD;
    I2C1_SR1 AT %MW0x08 : DWORD;
    I2C1_SR2 AT %MW0x0C : DWORD;
    I2C1_DR AT %MW0x10 : DWORD;
    I2C1_CCR AT %MW0x14 : DWORD;
    I2C1_TRISE AT %MW0x18 : DWORD;
    (* I2C 2 *)
    I2C2_BASE AT %MW0x40005800 : WORD;
    I2C2_CR1 AT %MW0x00 : DWORD;
    I2C2_CR2 AT %MW0x04 : DWORD;
    I2C2_SR1 AT %MW0x08 : DWORD;
    I2C2_SR2 AT %MW0x0C : DWORD;
    I2C2_DR AT %MW0x10 : DWORD;
    I2C2_CCR AT %MW0x14 : DWORD;
    (* Advanced Timer 1 *)
    TIM1_BASE AT %MW0x40012C00 : WORD;
    TIM1_CR1 AT %MW0x00 : DWORD;
    TIM1_CR2 AT %MW0x04 : DWORD;
    TIM1_SMCR AT %MW0x08 : DWORD;
    TIM1_DIER AT %MW0x0C : DWORD;
    TIM1_SR AT %MW0x10 : DWORD;
    TIM1_EGR AT %MW0x14 : DWORD;
    TIM1_CCMR1 AT %MW0x18 : DWORD;
    TIM1_CCMR2 AT %MW0x1C : DWORD;
    TIM1_CCER AT %MW0x20 : DWORD;
    TIM1_CNT AT %MW0x24 : DWORD;
    TIM1_PSC AT %MW0x28 : DWORD;
    TIM1_ARR AT %MW0x2C : DWORD;
    TIM1_RCR AT %MW0x30 : DWORD;
    TIM1_CCR1 AT %MW0x34 : DWORD;
    TIM1_CCR2 AT %MW0x38 : DWORD;
    TIM1_CCR3 AT %MW0x3C : DWORD;
    TIM1_CCR4 AT %MW0x40 : DWORD;
    TIM1_BDTR AT %MW0x44 : DWORD;
    (* General Purpose Timer 2 *)
    TIM2_BASE AT %MW0x40000400 : WORD;
    TIM2_CR1 AT %MW0x00 : DWORD;
    TIM2_CNT AT %MW0x24 : DWORD;
    TIM2_PSC AT %MW0x28 : DWORD;
    TIM2_ARR AT %MW0x2C : DWORD;
    TIM2_CCR1 AT %MW0x34 : DWORD;
    TIM2_CCR2 AT %MW0x38 : DWORD;
    TIM2_CCR3 AT %MW0x3C : DWORD;
    TIM2_CCR4 AT %MW0x40 : DWORD;
    (* General Purpose Timer 3 *)
    TIM3_BASE AT %MW0x40000400 : WORD;
    TIM3_CR1 AT %MW0x00 : DWORD;
    TIM3_CNT AT %MW0x24 : DWORD;
    TIM3_ARR AT %MW0x2C : DWORD;
    TIM3_CCR1 AT %MW0x34 : DWORD;
    TIM3_CCR2 AT %MW0x38 : DWORD;
    TIM3_CCR3 AT %MW0x3C : DWORD;
    TIM3_CCR4 AT %MW0x40 : DWORD;
    (* General Purpose Timer 4 *)
    TIM4_BASE AT %MW0x40000800 : WORD;
    TIM4_CR1 AT %MW0x00 : DWORD;
    TIM4_CNT AT %MW0x24 : DWORD;
    TIM4_ARR AT %MW0x2C : DWORD;
    TIM4_CCR1 AT %MW0x34 : DWORD;
    TIM4_CCR2 AT %MW0x38 : DWORD;
    TIM4_CCR3 AT %MW0x3C : DWORD;
    TIM4_CCR4 AT %MW0x40 : DWORD;
    (* ADC 1 *)
    ADC1_BASE AT %MW0x40012400 : WORD;
    ADC1_SR AT %MW0x00 : DWORD;
    ADC1_CR1 AT %MW0x04 : DWORD;
    ADC1_CR2 AT %MW0x08 : DWORD;
    ADC1_SMPR1 AT %MW0x0C : DWORD;
    ADC1_SMPR2 AT %MW0x10 : DWORD;
    ADC1_JOFR1 AT %MW0x14 : DWORD;
    ADC1_JOFR2 AT %MW0x18 : DWORD;
    ADC1_JOFR3 AT %MW0x1C : DWORD;
    ADC1_JOFR4 AT %MW0x20 : DWORD;
    ADC1_HTR AT %MW0x24 : DWORD;
    ADC1_LTR AT %MW0x28 : DWORD;
    ADC1_SQRT1 AT %MW0x2C : DWORD;
    ADC1_SQRT2 AT %MW0x30 : DWORD;
    ADC1_SQRT3 AT %MW0x34 : DWORD;
    ADC1_JSQR AT %MW0x38 : DWORD;
    ADC1_JDR1 AT %MW0x3C : DWORD;
    ADC1_JDR2 AT %MW0x40 : DWORD;
    ADC1_JDR3 AT %MW0x44 : DWORD;
    ADC1_JDR4 AT %MW0x48 : DWORD;
    ADC1_DR AT %MW0x4C : DWORD;
    (* DMA Controller 1 *)
    DMA1_BASE AT %MW0x40020000 : WORD;
    DMA1_ISR AT %MW0x00 : DWORD;
    DMA1_IFCR AT %MW0x04 : DWORD;
    DMA1_CCR1 AT %MW0x08 : DWORD;
    DMA1_CNDTR1 AT %MW0x0C : DWORD;
    DMA1_CPAR1 AT %MW0x10 : DWORD;
    DMA1_CMAR1 AT %MW0x14 : DWORD;
    DMA1_CCR2 AT %MW0x1C : DWORD;
    DMA1_CNDTR2 AT %MW0x20 : DWORD;
    DMA1_CPAR2 AT %MW0x24 : DWORD;
    DMA1_CMAR2 AT %MW0x28 : DWORD;
    (* Power Control *)
    PWR_BASE AT %MW0x40007000 : WORD;
    PWR_CR AT %MW0x00 : DWORD;
    PWR_CSR AT %MW0x04 : DWORD;
    (* Backup Registers *)
    BKP_BASE AT %MW0x40006C00 : WORD;
    BKP_DR1 AT %MW0x04 : DWORD;
    BKP_DR2 AT %MW0x08 : DWORD;
    BKP_CSR AT %MW0x2C : DWORD;
    (* Window Watchdog *)
    WWDG_BASE AT %MW0x40002C00 : WORD;
    WWDG_CR AT %MW0x00 : DWORD;
    WWDG_CFR AT %MW0x04 : DWORD;
    WWDG_SR AT %MW0x08 : DWORD;
    (* Independent Watchdog *)
    IWDG_BASE AT %MW0x40003000 : WORD;
    IWDG_KR AT %MW0x00 : DWORD;
    IWDG_PR AT %MW0x04 : DWORD;
    IWDG_RLR AT %MW0x08 : DWORD;
    (* External Interrupt/Event Controller *)
    EXTI_BASE AT %MW0x40010400 : WORD;
    EXTI_IMR AT %MW0x00 : DWORD;
    EXTI_EMR AT %MW0x04 : DWORD;
    EXTI_RTSR AT %MW0x08 : DWORD;
    EXTI_FTSR AT %MW0x0C : DWORD;
    EXTI_SWIER AT %MW0x10 : DWORD;
    EXTI_PR AT %MW0x14 : DWORD;
    (* Alternate Function IO *)
    AFIO_BASE AT %MW0x40010000 : WORD;
    AFIO_EVCR AT %MW0x00 : DWORD;
    AFIO_MAPR AT %MW0x04 : DWORD;
    AFIO_EXTICR1 AT %MW0x08 : DWORD;
    AFIO_EXTICR2 AT %MW0x0C : DWORD;
    AFIO_EXTICR3 AT %MW0x10 : DWORD;
    AFIO_MAPR2 AT %MW0x1C : DWORD;

    (* 中断变量 *)
    INT_WWDG : BOOL; (* Window Watchdog Interrupt *)
    INT_PVD : BOOL; (* PVD through EXTI Line detection *)
    INT_TAMPER : BOOL; (* Tamper Interrupt *)
    INT_RTC : BOOL; (* RTC Global Interrupt *)
    INT_FLASH : BOOL; (* FLASH Global Interrupt *)
    INT_RCC : BOOL; (* RCC Global Interrupt *)
    INT_EXTI0 : BOOL; (* EXTI Line 0 Interrupt *)
    INT_EXTI1 : BOOL; (* EXTI Line 1 Interrupt *)
    INT_EXTI2 : BOOL; (* EXTI Line 2 Interrupt *)
    INT_EXTI3 : BOOL; (* EXTI Line 3 Interrupt *)
    INT_EXTI4 : BOOL; (* EXTI Line 4 Interrupt *)
    INT_DMA1_CHANNEL1 : BOOL; (* DMA1 Channel 1 Interrupt *)
    INT_DMA1_CHANNEL2 : BOOL; (* DMA1 Channel 2 Interrupt *)
    INT_DMA1_CHANNEL3 : BOOL; (* DMA1 Channel 3 Interrupt *)
    INT_DMA1_CHANNEL4 : BOOL; (* DMA1 Channel 4 Interrupt *)
    INT_DMA1_CHANNEL5 : BOOL; (* DMA1 Channel 5 Interrupt *)
    INT_DMA1_CHANNEL6 : BOOL; (* DMA1 Channel 6 Interrupt *)
    INT_DMA1_CHANNEL7 : BOOL; (* DMA1 Channel 7 Interrupt *)
    INT_ADC1_2 : BOOL; (* ADC1 and ADC2 Global Interrupt *)
    INT_USB_HP_CAN_TX : BOOL; (* USB HP/CAN TX Interrupts *)
    INT_USB_LP_CAN_RX0 : BOOL; (* USB LP/CAN RX0 Interrupt *)
    INT_CAN_RX1 : BOOL; (* CAN RX1 Interrupt *)
    INT_CAN_SCE : BOOL; (* CAN SCE Interrupt *)
    INT_EXTI9_5 : BOOL; (* EXTI Line 9..5 Interrupt *)
    INT_TIM1_BRK : BOOL; (* TIM1 Break Interrupt *)
    INT_TIM1_UP : BOOL; (* TIM1 Update Interrupt *)
    INT_TIM1_TRG_COM : BOOL; (* TIM1 Trigger and Commutation *)
    INT_TIM1_CC : BOOL; (* TIM1 Capture Compare Interrupt *)
    INT_TIM2 : BOOL; (* TIM2 Global Interrupt *)
    INT_TIM3 : BOOL; (* TIM3 Global Interrupt *)
    INT_TIM4 : BOOL; (* TIM4 Global Interrupt *)
    INT_I2C1_EV : BOOL; (* I2C1 Event Interrupt *)
    INT_I2C1_ER : BOOL; (* I2C1 Error Interrupt *)
    INT_I2C2_EV : BOOL; (* I2C2 Event Interrupt *)
    INT_I2C2_ER : BOOL; (* I2C2 Error Interrupt *)
    INT_SPI1 : BOOL; (* SPI1 Global Interrupt *)
    INT_SPI2 : BOOL; (* SPI2 Global Interrupt *)
    INT_USART1 : BOOL; (* USART1 Global Interrupt *)
    INT_USART2 : BOOL; (* USART2 Global Interrupt *)
    INT_USART3 : BOOL; (* USART3 Global Interrupt *)
    INT_EXTI15_10 : BOOL; (* EXTI Line 15..10 Interrupt *)
    INT_RTCALARM : BOOL; (* RTC Alarm through EXTI *)
    INT_USBWAKEUP : BOOL; (* USB Wakeup from suspend *)

END_VAR

BEGIN
    (* STM32F103C8T6初始化梯形图 *)
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
    --|INT_RTC|--[中断处理]--
    --|INT_FLASH|--[中断处理]--
    --|INT_RCC|--[中断处理]--
    --|INT_EXTI0|--[中断处理]--
    --|INT_EXTI1|--[中断处理]--
    --|INT_EXTI2|--[中断处理]--
    --|INT_EXTI3|--[中断处理]--
    --|INT_EXTI4|--[中断处理]--
    --|INT_DMA1_CHANNEL1|--[中断处理]--
    --|INT_DMA1_CHANNEL2|--[中断处理]--
    --|INT_DMA1_CHANNEL3|--[中断处理]--
    --|INT_DMA1_CHANNEL4|--[中断处理]--
    --|INT_DMA1_CHANNEL5|--[中断处理]--
    --|INT_DMA1_CHANNEL6|--[中断处理]--
    --|INT_DMA1_CHANNEL7|--[中断处理]--
    --|INT_ADC1_2|--[中断处理]--
    --|INT_USB_HP_CAN_TX|--[中断处理]--
    --|INT_USB_LP_CAN_RX0|--[中断处理]--
    --|INT_CAN_RX1|--[中断处理]--
    --|INT_CAN_SCE|--[中断处理]--
    --|INT_EXTI9_5|--[中断处理]--
    --|INT_TIM1_BRK|--[中断处理]--
    --|INT_TIM1_UP|--[中断处理]--
    --|INT_TIM1_TRG_COM|--[中断处理]--
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
    --|INT_RTCALARM|--[中断处理]--
    --|INT_USBWAKEUP|--[中断处理]--

END_PROGRAM
