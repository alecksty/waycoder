using System;

namespace VML.Device.STMicroelectronics.STM32F103C8T6
{
    /// <summary>
    /// STM32F103C8T6 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32F103C8T6
    /// 版本: 1.0
    /// </summary>
    public static class STM32F103C8T6
    {
        // CPU架构: ARM-Cortex-M3, 32位, 72000000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        // General Purpose Register 1
        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        // General Purpose Register 2
        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        // General Purpose Register 3
        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        // General Purpose Register 4
        public const int R4_ADDR = 0x10;
        public static unsafe uint* R4 => (uint*)0x10;

        // General Purpose Register 5
        public const int R5_ADDR = 0x14;
        public static unsafe uint* R5 => (uint*)0x14;

        // General Purpose Register 6
        public const int R6_ADDR = 0x18;
        public static unsafe uint* R6 => (uint*)0x18;

        // General Purpose Register 7
        public const int R7_ADDR = 0x1C;
        public static unsafe uint* R7 => (uint*)0x1C;

        // General Purpose Register 8
        public const int R8_ADDR = 0x20;
        public static unsafe uint* R8 => (uint*)0x20;

        // General Purpose Register 9
        public const int R9_ADDR = 0x24;
        public static unsafe uint* R9 => (uint*)0x24;

        // General Purpose Register 10
        public const int R10_ADDR = 0x28;
        public static unsafe uint* R10 => (uint*)0x28;

        // General Purpose Register 11
        public const int R11_ADDR = 0x2C;
        public static unsafe uint* R11 => (uint*)0x2C;

        // General Purpose Register 12
        public const int R12_ADDR = 0x30;
        public static unsafe uint* R12 => (uint*)0x30;

        // Stack Pointer
        public const int SP_ADDR = 0x34;
        public static unsafe uint* SP => (uint*)0x34;

        // Link Register
        public const int LR_ADDR = 0x38;
        public static unsafe uint* LR => (uint*)0x38;

        // Program Counter
        public const int PC_ADDR = 0x3C;
        public static unsafe uint* PC => (uint*)0x3C;

        // Program Status Register
        public const int XPSR_ADDR = 0x40;
        public static unsafe uint* xPSR => (uint*)0x40;

        // 内存段定义
        // Main Flash Memory (64KB)
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x0800FFFF;
        public const int FLASH_SIZE = 65536;

        // System Memory (2KB)
        public const int SYSTEM_MEMORY_START = 0x1FFFF000;
        public const int SYSTEM_MEMORY_END = 0x1FFFF7FF;
        public const int SYSTEM_MEMORY_SIZE = 2048;

        // SRAM (20KB)
        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20004FFF;
        public const int SRAM_SIZE = 20480;

        // Peripheral Registers
        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x40023FFF;
        public const int PERIPHERAL_SIZE = 143360;

        // Core Peripheral Registers
        public const int CORTEX_M_START = 0xE0000000;
        public const int CORTEX_M_END = 0xE00FFFFF;
        public const int CORTEX_M_SIZE = 1048576;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40021000;
        public static unsafe uint* RCC_CR => (uint*)0x40021000;
        public static unsafe uint* RCC_CFGR => (uint*)0x40021004;
        public static unsafe uint* RCC_APB2ENR => (uint*)0x40021018;
        public static unsafe uint* RCC_APB1ENR => (uint*)0x4002101C;

        // GPIO Port A
        public const int GPIOA_BASE = 0x40010800;
        public static unsafe uint* GPIOA_CRL => (uint*)0x40010800;
        public static unsafe uint* GPIOA_CRH => (uint*)0x40010804;
        public static unsafe uint* GPIOA_IDR => (uint*)0x40010808;
        public static unsafe uint* GPIOA_ODR => (uint*)0x4001080C;
        public static unsafe uint* GPIOA_BSRR => (uint*)0x40010810;
        public static unsafe uint* GPIOA_BRR => (uint*)0x40010814;
        public static unsafe uint* GPIOA_LCKR => (uint*)0x40010818;

        // GPIO Port B
        public const int GPIOB_BASE = 0x40010C00;
        public static unsafe uint* GPIOB_CRL => (uint*)0x40010C00;
        public static unsafe uint* GPIOB_CRH => (uint*)0x40010C04;
        public static unsafe uint* GPIOB_IDR => (uint*)0x40010C08;
        public static unsafe uint* GPIOB_ODR => (uint*)0x40010C0C;
        public static unsafe uint* GPIOB_BSRR => (uint*)0x40010C10;
        public static unsafe uint* GPIOB_BRR => (uint*)0x40010C14;

        // GPIO Port C
        public const int GPIOC_BASE = 0x40011000;
        public static unsafe uint* GPIOC_CRL => (uint*)0x40011000;
        public static unsafe uint* GPIOC_CRH => (uint*)0x40011004;
        public static unsafe uint* GPIOC_IDR => (uint*)0x40011008;
        public static unsafe uint* GPIOC_ODR => (uint*)0x4001100C;
        public static unsafe uint* GPIOC_BSRR => (uint*)0x40011010;

        // USART 1
        public const int USART1_BASE = 0x40013800;
        public static unsafe uint* USART1_SR => (uint*)0x40013800;
        public static unsafe uint* USART1_DR => (uint*)0x40013804;
        public static unsafe uint* USART1_BRR => (uint*)0x40013808;
        public static unsafe uint* USART1_CR1 => (uint*)0x4001380C;
        public static unsafe uint* USART1_CR2 => (uint*)0x40013810;
        public static unsafe uint* USART1_CR3 => (uint*)0x40013814;
        public static unsafe uint* USART1_GTPR => (uint*)0x40013818;

        // USART 2
        public const int USART2_BASE = 0x40004400;
        public static unsafe uint* USART2_SR => (uint*)0x40004400;
        public static unsafe uint* USART2_DR => (uint*)0x40004404;
        public static unsafe uint* USART2_BRR => (uint*)0x40004408;
        public static unsafe uint* USART2_CR1 => (uint*)0x4000440C;

        // SPI 1
        public const int SPI1_BASE = 0x40013000;
        public static unsafe uint* SPI1_CR1 => (uint*)0x40013000;
        public static unsafe uint* SPI1_CR2 => (uint*)0x40013004;
        public static unsafe uint* SPI1_SR => (uint*)0x40013008;
        public static unsafe uint* SPI1_DR => (uint*)0x4001300C;

        // SPI 2
        public const int SPI2_BASE = 0x40003800;
        public static unsafe uint* SPI2_CR1 => (uint*)0x40003800;
        public static unsafe uint* SPI2_CR2 => (uint*)0x40003804;
        public static unsafe uint* SPI2_SR => (uint*)0x40003808;
        public static unsafe uint* SPI2_DR => (uint*)0x4000380C;

        // I2C 1
        public const int I2C1_BASE = 0x40005400;
        public static unsafe uint* I2C1_CR1 => (uint*)0x40005400;
        public static unsafe uint* I2C1_CR2 => (uint*)0x40005404;
        public static unsafe uint* I2C1_SR1 => (uint*)0x40005408;
        public static unsafe uint* I2C1_SR2 => (uint*)0x4000540C;
        public static unsafe uint* I2C1_DR => (uint*)0x40005410;
        public static unsafe uint* I2C1_CCR => (uint*)0x40005414;
        public static unsafe uint* I2C1_TRISE => (uint*)0x40005418;

        // I2C 2
        public const int I2C2_BASE = 0x40005800;
        public static unsafe uint* I2C2_CR1 => (uint*)0x40005800;
        public static unsafe uint* I2C2_CR2 => (uint*)0x40005804;
        public static unsafe uint* I2C2_SR1 => (uint*)0x40005808;
        public static unsafe uint* I2C2_SR2 => (uint*)0x4000580C;
        public static unsafe uint* I2C2_DR => (uint*)0x40005810;
        public static unsafe uint* I2C2_CCR => (uint*)0x40005814;

        // Advanced Timer 1
        public const int TIM1_BASE = 0x40012C00;
        public static unsafe uint* TIM1_CR1 => (uint*)0x40012C00;
        public static unsafe uint* TIM1_CR2 => (uint*)0x40012C04;
        public static unsafe uint* TIM1_SMCR => (uint*)0x40012C08;
        public static unsafe uint* TIM1_DIER => (uint*)0x40012C0C;
        public static unsafe uint* TIM1_SR => (uint*)0x40012C10;
        public static unsafe uint* TIM1_EGR => (uint*)0x40012C14;
        public static unsafe uint* TIM1_CCMR1 => (uint*)0x40012C18;
        public static unsafe uint* TIM1_CCMR2 => (uint*)0x40012C1C;
        public static unsafe uint* TIM1_CCER => (uint*)0x40012C20;
        public static unsafe uint* TIM1_CNT => (uint*)0x40012C24;
        public static unsafe uint* TIM1_PSC => (uint*)0x40012C28;
        public static unsafe uint* TIM1_ARR => (uint*)0x40012C2C;
        public static unsafe uint* TIM1_RCR => (uint*)0x40012C30;
        public static unsafe uint* TIM1_CCR1 => (uint*)0x40012C34;
        public static unsafe uint* TIM1_CCR2 => (uint*)0x40012C38;
        public static unsafe uint* TIM1_CCR3 => (uint*)0x40012C3C;
        public static unsafe uint* TIM1_CCR4 => (uint*)0x40012C40;
        public static unsafe uint* TIM1_BDTR => (uint*)0x40012C44;

        // General Purpose Timer 2
        public const int TIM2_BASE = 0x40000400;
        public static unsafe uint* TIM2_CR1 => (uint*)0x40000400;
        public static unsafe uint* TIM2_CNT => (uint*)0x40000424;
        public static unsafe uint* TIM2_PSC => (uint*)0x40000428;
        public static unsafe uint* TIM2_ARR => (uint*)0x4000042C;
        public static unsafe uint* TIM2_CCR1 => (uint*)0x40000434;
        public static unsafe uint* TIM2_CCR2 => (uint*)0x40000438;
        public static unsafe uint* TIM2_CCR3 => (uint*)0x4000043C;
        public static unsafe uint* TIM2_CCR4 => (uint*)0x40000440;

        // General Purpose Timer 3
        public const int TIM3_BASE = 0x40000400;
        public static unsafe uint* TIM3_CR1 => (uint*)0x40000400;
        public static unsafe uint* TIM3_CNT => (uint*)0x40000424;
        public static unsafe uint* TIM3_ARR => (uint*)0x4000042C;
        public static unsafe uint* TIM3_CCR1 => (uint*)0x40000434;
        public static unsafe uint* TIM3_CCR2 => (uint*)0x40000438;
        public static unsafe uint* TIM3_CCR3 => (uint*)0x4000043C;
        public static unsafe uint* TIM3_CCR4 => (uint*)0x40000440;

        // General Purpose Timer 4
        public const int TIM4_BASE = 0x40000800;
        public static unsafe uint* TIM4_CR1 => (uint*)0x40000800;
        public static unsafe uint* TIM4_CNT => (uint*)0x40000824;
        public static unsafe uint* TIM4_ARR => (uint*)0x4000082C;
        public static unsafe uint* TIM4_CCR1 => (uint*)0x40000834;
        public static unsafe uint* TIM4_CCR2 => (uint*)0x40000838;
        public static unsafe uint* TIM4_CCR3 => (uint*)0x4000083C;
        public static unsafe uint* TIM4_CCR4 => (uint*)0x40000840;

        // ADC 1
        public const int ADC1_BASE = 0x40012400;
        public static unsafe uint* ADC1_SR => (uint*)0x40012400;
        public static unsafe uint* ADC1_CR1 => (uint*)0x40012404;
        public static unsafe uint* ADC1_CR2 => (uint*)0x40012408;
        public static unsafe uint* ADC1_SMPR1 => (uint*)0x4001240C;
        public static unsafe uint* ADC1_SMPR2 => (uint*)0x40012410;
        public static unsafe uint* ADC1_JOFR1 => (uint*)0x40012414;
        public static unsafe uint* ADC1_JOFR2 => (uint*)0x40012418;
        public static unsafe uint* ADC1_JOFR3 => (uint*)0x4001241C;
        public static unsafe uint* ADC1_JOFR4 => (uint*)0x40012420;
        public static unsafe uint* ADC1_HTR => (uint*)0x40012424;
        public static unsafe uint* ADC1_LTR => (uint*)0x40012428;
        public static unsafe uint* ADC1_SQRT1 => (uint*)0x4001242C;
        public static unsafe uint* ADC1_SQRT2 => (uint*)0x40012430;
        public static unsafe uint* ADC1_SQRT3 => (uint*)0x40012434;
        public static unsafe uint* ADC1_JSQR => (uint*)0x40012438;
        public static unsafe uint* ADC1_JDR1 => (uint*)0x4001243C;
        public static unsafe uint* ADC1_JDR2 => (uint*)0x40012440;
        public static unsafe uint* ADC1_JDR3 => (uint*)0x40012444;
        public static unsafe uint* ADC1_JDR4 => (uint*)0x40012448;
        public static unsafe uint* ADC1_DR => (uint*)0x4001244C;

        // DMA Controller 1
        public const int DMA1_BASE = 0x40020000;
        public static unsafe uint* DMA1_ISR => (uint*)0x40020000;
        public static unsafe uint* DMA1_IFCR => (uint*)0x40020004;
        public static unsafe uint* DMA1_CCR1 => (uint*)0x40020008;
        public static unsafe uint* DMA1_CNDTR1 => (uint*)0x4002000C;
        public static unsafe uint* DMA1_CPAR1 => (uint*)0x40020010;
        public static unsafe uint* DMA1_CMAR1 => (uint*)0x40020014;
        public static unsafe uint* DMA1_CCR2 => (uint*)0x4002001C;
        public static unsafe uint* DMA1_CNDTR2 => (uint*)0x40020020;
        public static unsafe uint* DMA1_CPAR2 => (uint*)0x40020024;
        public static unsafe uint* DMA1_CMAR2 => (uint*)0x40020028;

        // Power Control
        public const int PWR_BASE = 0x40007000;
        public static unsafe uint* PWR_CR => (uint*)0x40007000;
        public static unsafe uint* PWR_CSR => (uint*)0x40007004;

        // Backup Registers
        public const int BKP_BASE = 0x40006C00;
        public static unsafe uint* BKP_DR1 => (uint*)0x40006C04;
        public static unsafe uint* BKP_DR2 => (uint*)0x40006C08;
        public static unsafe uint* BKP_CSR => (uint*)0x40006C2C;

        // Window Watchdog
        public const int WWDG_BASE = 0x40002C00;
        public static unsafe uint* WWDG_CR => (uint*)0x40002C00;
        public static unsafe uint* WWDG_CFR => (uint*)0x40002C04;
        public static unsafe uint* WWDG_SR => (uint*)0x40002C08;

        // Independent Watchdog
        public const int IWDG_BASE = 0x40003000;
        public static unsafe uint* IWDG_KR => (uint*)0x40003000;
        public static unsafe uint* IWDG_PR => (uint*)0x40003004;
        public static unsafe uint* IWDG_RLR => (uint*)0x40003008;

        // External Interrupt/Event Controller
        public const int EXTI_BASE = 0x40010400;
        public static unsafe uint* EXTI_IMR => (uint*)0x40010400;
        public static unsafe uint* EXTI_EMR => (uint*)0x40010404;
        public static unsafe uint* EXTI_RTSR => (uint*)0x40010408;
        public static unsafe uint* EXTI_FTSR => (uint*)0x4001040C;
        public static unsafe uint* EXTI_SWIER => (uint*)0x40010410;
        public static unsafe uint* EXTI_PR => (uint*)0x40010414;

        // Alternate Function IO
        public const int AFIO_BASE = 0x40010000;
        public static unsafe uint* AFIO_EVCR => (uint*)0x40010000;
        public static unsafe uint* AFIO_MAPR => (uint*)0x40010004;
        public static unsafe uint* AFIO_EXTICR1 => (uint*)0x40010008;
        public static unsafe uint* AFIO_EXTICR2 => (uint*)0x4001000C;
        public static unsafe uint* AFIO_EXTICR3 => (uint*)0x40010010;
        public static unsafe uint* AFIO_MAPR2 => (uint*)0x4001001C;

        // 中断向量定义
        public const int IRQ_WWDG = 0;  // Window Watchdog Interrupt
        public const int IRQ_PVD = 1;  // PVD through EXTI Line detection
        public const int IRQ_TAMPER = 2;  // Tamper Interrupt
        public const int IRQ_RTC = 3;  // RTC Global Interrupt
        public const int IRQ_FLASH = 4;  // FLASH Global Interrupt
        public const int IRQ_RCC = 5;  // RCC Global Interrupt
        public const int IRQ_EXTI0 = 6;  // EXTI Line 0 Interrupt
        public const int IRQ_EXTI1 = 7;  // EXTI Line 1 Interrupt
        public const int IRQ_EXTI2 = 8;  // EXTI Line 2 Interrupt
        public const int IRQ_EXTI3 = 9;  // EXTI Line 3 Interrupt
        public const int IRQ_EXTI4 = 10;  // EXTI Line 4 Interrupt
        public const int IRQ_DMA1_CHANNEL1 = 11;  // DMA1 Channel 1 Interrupt
        public const int IRQ_DMA1_CHANNEL2 = 12;  // DMA1 Channel 2 Interrupt
        public const int IRQ_DMA1_CHANNEL3 = 13;  // DMA1 Channel 3 Interrupt
        public const int IRQ_DMA1_CHANNEL4 = 14;  // DMA1 Channel 4 Interrupt
        public const int IRQ_DMA1_CHANNEL5 = 15;  // DMA1 Channel 5 Interrupt
        public const int IRQ_DMA1_CHANNEL6 = 16;  // DMA1 Channel 6 Interrupt
        public const int IRQ_DMA1_CHANNEL7 = 17;  // DMA1 Channel 7 Interrupt
        public const int IRQ_ADC1_2 = 18;  // ADC1 and ADC2 Global Interrupt
        public const int IRQ_USB_HP_CAN_TX = 19;  // USB HP/CAN TX Interrupts
        public const int IRQ_USB_LP_CAN_RX0 = 20;  // USB LP/CAN RX0 Interrupt
        public const int IRQ_CAN_RX1 = 21;  // CAN RX1 Interrupt
        public const int IRQ_CAN_SCE = 22;  // CAN SCE Interrupt
        public const int IRQ_EXTI9_5 = 23;  // EXTI Line 9..5 Interrupt
        public const int IRQ_TIM1_BRK = 25;  // TIM1 Break Interrupt
        public const int IRQ_TIM1_UP = 26;  // TIM1 Update Interrupt
        public const int IRQ_TIM1_TRG_COM = 27;  // TIM1 Trigger and Commutation
        public const int IRQ_TIM1_CC = 28;  // TIM1 Capture Compare Interrupt
        public const int IRQ_TIM2 = 29;  // TIM2 Global Interrupt
        public const int IRQ_TIM3 = 30;  // TIM3 Global Interrupt
        public const int IRQ_TIM4 = 31;  // TIM4 Global Interrupt
        public const int IRQ_I2C1_EV = 32;  // I2C1 Event Interrupt
        public const int IRQ_I2C1_ER = 33;  // I2C1 Error Interrupt
        public const int IRQ_I2C2_EV = 34;  // I2C2 Event Interrupt
        public const int IRQ_I2C2_ER = 35;  // I2C2 Error Interrupt
        public const int IRQ_SPI1 = 35;  // SPI1 Global Interrupt
        public const int IRQ_SPI2 = 36;  // SPI2 Global Interrupt
        public const int IRQ_USART1 = 37;  // USART1 Global Interrupt
        public const int IRQ_USART2 = 38;  // USART2 Global Interrupt
        public const int IRQ_USART3 = 39;  // USART3 Global Interrupt
        public const int IRQ_EXTI15_10 = 40;  // EXTI Line 15..10 Interrupt
        public const int IRQ_RTCALARM = 41;  // RTC Alarm through EXTI
        public const int IRQ_USBWAKEUP = 42;  // USB Wakeup from suspend

        // 引脚定义
        public const int PIN_VBAT = 1;  // Battery Supply
        public const int PIN_PC13 = 2;  // GPIO Port C Pin 13
        public const int PIN_PC14 = 3;  // GPIO Port C Pin 14
        public const int PIN_PC15 = 4;  // GPIO Port C Pin 15
        public const int PIN_PD0 = 5;  // GPIO Port D Pin 0
        public const int PIN_PD1 = 6;  // GPIO Port D Pin 1
        public const int PIN_NRST = 7;  // Reset
        public const int PIN_VSSA = 8;  // Analog Ground
        public const int PIN_VDDA = 9;  // Analog Supply
        public const int PIN_PA0 = 10;  // GPIO Port A Pin 0 / ADC1_IN0
        public const int PIN_PA1 = 11;  // GPIO Port A Pin 1 / ADC1_IN1
        public const int PIN_PA2 = 12;  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
        public const int PIN_PA3 = 13;  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
        public const int PIN_PA4 = 14;  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
        public const int PIN_PA5 = 15;  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
        public const int PIN_PA6 = 16;  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
        public const int PIN_PA7 = 17;  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
        public const int PIN_PB0 = 18;  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
        public const int PIN_PB1 = 19;  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
        public const int PIN_PB2 = 20;  // GPIO Port B Pin 2
        public const int PIN_PB10 = 21;  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
        public const int PIN_PB11 = 22;  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
        public const int PIN_VSS = 23;  // Ground
        public const int PIN_VDD = 24;  // Digital Supply
        public const int PIN_PB12 = 25;  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
        public const int PIN_PB13 = 26;  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
        public const int PIN_PB14 = 27;  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
        public const int PIN_PB15 = 28;  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
        public const int PIN_PA8 = 29;  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
        public const int PIN_PA9 = 30;  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
        public const int PIN_PA10 = 31;  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
        public const int PIN_PA11 = 32;  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
        public const int PIN_PA12 = 33;  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
        public const int PIN_PA13 = 34;  // JTMS/SWDIO
        public const int PIN_PA14 = 37;  // JTCK/SWCLK
        public const int PIN_PA15 = 38;  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
        public const int PIN_PB3 = 39;  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
        public const int PIN_PB4 = 40;  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
        public const int PIN_PB5 = 41;  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
        public const int PIN_PB6 = 42;  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
        public const int PIN_PB7 = 43;  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
        public const int PIN_BOOT0 = 44;  // Boot Selection
        public const int PIN_PB8 = 45;  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
        public const int PIN_PB9 = 46;  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
        public const int PIN_VSS = 47;  // Ground
        public const int PIN_VDD = 48;  // Digital Supply

        public static void stm32f103c8t6_init()
        {
            // 硬件初始化代码
        }
    }
}
