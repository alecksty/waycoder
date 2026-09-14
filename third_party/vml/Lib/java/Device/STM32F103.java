package vml.device.stmicroelectronics.stm32f103c8t6;

/**
 * STM32F103C8T6 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32F103C8T6
 * 版本: 1.0
 */
public final class STM32F103C8T6 {
    private STM32F103C8T6() {} // 工具类
    // CPU架构: ARM-Cortex-M3, 32位, 72000000 Hz

    // 寄存器定义
    // General Purpose Register 0
    public static final int R0_ADDR = (int)0x00;

    // General Purpose Register 1
    public static final int R1_ADDR = (int)0x04;

    // General Purpose Register 2
    public static final int R2_ADDR = (int)0x08;

    // General Purpose Register 3
    public static final int R3_ADDR = (int)0x0C;

    // General Purpose Register 4
    public static final int R4_ADDR = (int)0x10;

    // General Purpose Register 5
    public static final int R5_ADDR = (int)0x14;

    // General Purpose Register 6
    public static final int R6_ADDR = (int)0x18;

    // General Purpose Register 7
    public static final int R7_ADDR = (int)0x1C;

    // General Purpose Register 8
    public static final int R8_ADDR = (int)0x20;

    // General Purpose Register 9
    public static final int R9_ADDR = (int)0x24;

    // General Purpose Register 10
    public static final int R10_ADDR = (int)0x28;

    // General Purpose Register 11
    public static final int R11_ADDR = (int)0x2C;

    // General Purpose Register 12
    public static final int R12_ADDR = (int)0x30;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x34;

    // Link Register
    public static final int LR_ADDR = (int)0x38;

    // Program Counter
    public static final int PC_ADDR = (int)0x3C;

    // Program Status Register
    public static final int XPSR_ADDR = (int)0x40;

    // 内存段定义
    // Main Flash Memory (64KB)
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x0800FFFF;
    public static final int FLASH_SIZE = 65536;

    // System Memory (2KB)
    public static final int SYSTEM_MEMORY_START = (int)0x1FFFF000;
    public static final int SYSTEM_MEMORY_END = (int)0x1FFFF7FF;
    public static final int SYSTEM_MEMORY_SIZE = 2048;

    // SRAM (20KB)
    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20004FFF;
    public static final int SRAM_SIZE = 20480;

    // Peripheral Registers
    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x40023FFF;
    public static final int PERIPHERAL_SIZE = 143360;

    // Core Peripheral Registers
    public static final int CORTEX_M_START = (int)0xE0000000;
    public static final int CORTEX_M_END = (int)0xE00FFFFF;
    public static final int CORTEX_M_SIZE = 1048576;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40021000;
    public static final int RCC_CR = (int)0x40021000;
    public static final int RCC_CFGR = (int)0x40021004;
    public static final int RCC_APB2ENR = (int)0x40021018;
    public static final int RCC_APB1ENR = (int)0x4002101C;

    // GPIO Port A
    public static final int GPIOA_BASE = (int)0x40010800;
    public static final int GPIOA_CRL = (int)0x40010800;
    public static final int GPIOA_CRH = (int)0x40010804;
    public static final int GPIOA_IDR = (int)0x40010808;
    public static final int GPIOA_ODR = (int)0x4001080C;
    public static final int GPIOA_BSRR = (int)0x40010810;
    public static final int GPIOA_BRR = (int)0x40010814;
    public static final int GPIOA_LCKR = (int)0x40010818;

    // GPIO Port B
    public static final int GPIOB_BASE = (int)0x40010C00;
    public static final int GPIOB_CRL = (int)0x40010C00;
    public static final int GPIOB_CRH = (int)0x40010C04;
    public static final int GPIOB_IDR = (int)0x40010C08;
    public static final int GPIOB_ODR = (int)0x40010C0C;
    public static final int GPIOB_BSRR = (int)0x40010C10;
    public static final int GPIOB_BRR = (int)0x40010C14;

    // GPIO Port C
    public static final int GPIOC_BASE = (int)0x40011000;
    public static final int GPIOC_CRL = (int)0x40011000;
    public static final int GPIOC_CRH = (int)0x40011004;
    public static final int GPIOC_IDR = (int)0x40011008;
    public static final int GPIOC_ODR = (int)0x4001100C;
    public static final int GPIOC_BSRR = (int)0x40011010;

    // USART 1
    public static final int USART1_BASE = (int)0x40013800;
    public static final int USART1_SR = (int)0x40013800;
    public static final int USART1_DR = (int)0x40013804;
    public static final int USART1_BRR = (int)0x40013808;
    public static final int USART1_CR1 = (int)0x4001380C;
    public static final int USART1_CR2 = (int)0x40013810;
    public static final int USART1_CR3 = (int)0x40013814;
    public static final int USART1_GTPR = (int)0x40013818;

    // USART 2
    public static final int USART2_BASE = (int)0x40004400;
    public static final int USART2_SR = (int)0x40004400;
    public static final int USART2_DR = (int)0x40004404;
    public static final int USART2_BRR = (int)0x40004408;
    public static final int USART2_CR1 = (int)0x4000440C;

    // SPI 1
    public static final int SPI1_BASE = (int)0x40013000;
    public static final int SPI1_CR1 = (int)0x40013000;
    public static final int SPI1_CR2 = (int)0x40013004;
    public static final int SPI1_SR = (int)0x40013008;
    public static final int SPI1_DR = (int)0x4001300C;

    // SPI 2
    public static final int SPI2_BASE = (int)0x40003800;
    public static final int SPI2_CR1 = (int)0x40003800;
    public static final int SPI2_CR2 = (int)0x40003804;
    public static final int SPI2_SR = (int)0x40003808;
    public static final int SPI2_DR = (int)0x4000380C;

    // I2C 1
    public static final int I2C1_BASE = (int)0x40005400;
    public static final int I2C1_CR1 = (int)0x40005400;
    public static final int I2C1_CR2 = (int)0x40005404;
    public static final int I2C1_SR1 = (int)0x40005408;
    public static final int I2C1_SR2 = (int)0x4000540C;
    public static final int I2C1_DR = (int)0x40005410;
    public static final int I2C1_CCR = (int)0x40005414;
    public static final int I2C1_TRISE = (int)0x40005418;

    // I2C 2
    public static final int I2C2_BASE = (int)0x40005800;
    public static final int I2C2_CR1 = (int)0x40005800;
    public static final int I2C2_CR2 = (int)0x40005804;
    public static final int I2C2_SR1 = (int)0x40005808;
    public static final int I2C2_SR2 = (int)0x4000580C;
    public static final int I2C2_DR = (int)0x40005810;
    public static final int I2C2_CCR = (int)0x40005814;

    // Advanced Timer 1
    public static final int TIM1_BASE = (int)0x40012C00;
    public static final int TIM1_CR1 = (int)0x40012C00;
    public static final int TIM1_CR2 = (int)0x40012C04;
    public static final int TIM1_SMCR = (int)0x40012C08;
    public static final int TIM1_DIER = (int)0x40012C0C;
    public static final int TIM1_SR = (int)0x40012C10;
    public static final int TIM1_EGR = (int)0x40012C14;
    public static final int TIM1_CCMR1 = (int)0x40012C18;
    public static final int TIM1_CCMR2 = (int)0x40012C1C;
    public static final int TIM1_CCER = (int)0x40012C20;
    public static final int TIM1_CNT = (int)0x40012C24;
    public static final int TIM1_PSC = (int)0x40012C28;
    public static final int TIM1_ARR = (int)0x40012C2C;
    public static final int TIM1_RCR = (int)0x40012C30;
    public static final int TIM1_CCR1 = (int)0x40012C34;
    public static final int TIM1_CCR2 = (int)0x40012C38;
    public static final int TIM1_CCR3 = (int)0x40012C3C;
    public static final int TIM1_CCR4 = (int)0x40012C40;
    public static final int TIM1_BDTR = (int)0x40012C44;

    // General Purpose Timer 2
    public static final int TIM2_BASE = (int)0x40000400;
    public static final int TIM2_CR1 = (int)0x40000400;
    public static final int TIM2_CNT = (int)0x40000424;
    public static final int TIM2_PSC = (int)0x40000428;
    public static final int TIM2_ARR = (int)0x4000042C;
    public static final int TIM2_CCR1 = (int)0x40000434;
    public static final int TIM2_CCR2 = (int)0x40000438;
    public static final int TIM2_CCR3 = (int)0x4000043C;
    public static final int TIM2_CCR4 = (int)0x40000440;

    // General Purpose Timer 3
    public static final int TIM3_BASE = (int)0x40000400;
    public static final int TIM3_CR1 = (int)0x40000400;
    public static final int TIM3_CNT = (int)0x40000424;
    public static final int TIM3_ARR = (int)0x4000042C;
    public static final int TIM3_CCR1 = (int)0x40000434;
    public static final int TIM3_CCR2 = (int)0x40000438;
    public static final int TIM3_CCR3 = (int)0x4000043C;
    public static final int TIM3_CCR4 = (int)0x40000440;

    // General Purpose Timer 4
    public static final int TIM4_BASE = (int)0x40000800;
    public static final int TIM4_CR1 = (int)0x40000800;
    public static final int TIM4_CNT = (int)0x40000824;
    public static final int TIM4_ARR = (int)0x4000082C;
    public static final int TIM4_CCR1 = (int)0x40000834;
    public static final int TIM4_CCR2 = (int)0x40000838;
    public static final int TIM4_CCR3 = (int)0x4000083C;
    public static final int TIM4_CCR4 = (int)0x40000840;

    // ADC 1
    public static final int ADC1_BASE = (int)0x40012400;
    public static final int ADC1_SR = (int)0x40012400;
    public static final int ADC1_CR1 = (int)0x40012404;
    public static final int ADC1_CR2 = (int)0x40012408;
    public static final int ADC1_SMPR1 = (int)0x4001240C;
    public static final int ADC1_SMPR2 = (int)0x40012410;
    public static final int ADC1_JOFR1 = (int)0x40012414;
    public static final int ADC1_JOFR2 = (int)0x40012418;
    public static final int ADC1_JOFR3 = (int)0x4001241C;
    public static final int ADC1_JOFR4 = (int)0x40012420;
    public static final int ADC1_HTR = (int)0x40012424;
    public static final int ADC1_LTR = (int)0x40012428;
    public static final int ADC1_SQRT1 = (int)0x4001242C;
    public static final int ADC1_SQRT2 = (int)0x40012430;
    public static final int ADC1_SQRT3 = (int)0x40012434;
    public static final int ADC1_JSQR = (int)0x40012438;
    public static final int ADC1_JDR1 = (int)0x4001243C;
    public static final int ADC1_JDR2 = (int)0x40012440;
    public static final int ADC1_JDR3 = (int)0x40012444;
    public static final int ADC1_JDR4 = (int)0x40012448;
    public static final int ADC1_DR = (int)0x4001244C;

    // DMA Controller 1
    public static final int DMA1_BASE = (int)0x40020000;
    public static final int DMA1_ISR = (int)0x40020000;
    public static final int DMA1_IFCR = (int)0x40020004;
    public static final int DMA1_CCR1 = (int)0x40020008;
    public static final int DMA1_CNDTR1 = (int)0x4002000C;
    public static final int DMA1_CPAR1 = (int)0x40020010;
    public static final int DMA1_CMAR1 = (int)0x40020014;
    public static final int DMA1_CCR2 = (int)0x4002001C;
    public static final int DMA1_CNDTR2 = (int)0x40020020;
    public static final int DMA1_CPAR2 = (int)0x40020024;
    public static final int DMA1_CMAR2 = (int)0x40020028;

    // Power Control
    public static final int PWR_BASE = (int)0x40007000;
    public static final int PWR_CR = (int)0x40007000;
    public static final int PWR_CSR = (int)0x40007004;

    // Backup Registers
    public static final int BKP_BASE = (int)0x40006C00;
    public static final int BKP_DR1 = (int)0x40006C04;
    public static final int BKP_DR2 = (int)0x40006C08;
    public static final int BKP_CSR = (int)0x40006C2C;

    // Window Watchdog
    public static final int WWDG_BASE = (int)0x40002C00;
    public static final int WWDG_CR = (int)0x40002C00;
    public static final int WWDG_CFR = (int)0x40002C04;
    public static final int WWDG_SR = (int)0x40002C08;

    // Independent Watchdog
    public static final int IWDG_BASE = (int)0x40003000;
    public static final int IWDG_KR = (int)0x40003000;
    public static final int IWDG_PR = (int)0x40003004;
    public static final int IWDG_RLR = (int)0x40003008;

    // External Interrupt/Event Controller
    public static final int EXTI_BASE = (int)0x40010400;
    public static final int EXTI_IMR = (int)0x40010400;
    public static final int EXTI_EMR = (int)0x40010404;
    public static final int EXTI_RTSR = (int)0x40010408;
    public static final int EXTI_FTSR = (int)0x4001040C;
    public static final int EXTI_SWIER = (int)0x40010410;
    public static final int EXTI_PR = (int)0x40010414;

    // Alternate Function IO
    public static final int AFIO_BASE = (int)0x40010000;
    public static final int AFIO_EVCR = (int)0x40010000;
    public static final int AFIO_MAPR = (int)0x40010004;
    public static final int AFIO_EXTICR1 = (int)0x40010008;
    public static final int AFIO_EXTICR2 = (int)0x4001000C;
    public static final int AFIO_EXTICR3 = (int)0x40010010;
    public static final int AFIO_MAPR2 = (int)0x4001001C;

    // 中断向量定义
    public static final int IRQ_WWDG = 0;  // Window Watchdog Interrupt
    public static final int IRQ_PVD = 1;  // PVD through EXTI Line detection
    public static final int IRQ_TAMPER = 2;  // Tamper Interrupt
    public static final int IRQ_RTC = 3;  // RTC Global Interrupt
    public static final int IRQ_FLASH = 4;  // FLASH Global Interrupt
    public static final int IRQ_RCC = 5;  // RCC Global Interrupt
    public static final int IRQ_EXTI0 = 6;  // EXTI Line 0 Interrupt
    public static final int IRQ_EXTI1 = 7;  // EXTI Line 1 Interrupt
    public static final int IRQ_EXTI2 = 8;  // EXTI Line 2 Interrupt
    public static final int IRQ_EXTI3 = 9;  // EXTI Line 3 Interrupt
    public static final int IRQ_EXTI4 = 10;  // EXTI Line 4 Interrupt
    public static final int IRQ_DMA1_CHANNEL1 = 11;  // DMA1 Channel 1 Interrupt
    public static final int IRQ_DMA1_CHANNEL2 = 12;  // DMA1 Channel 2 Interrupt
    public static final int IRQ_DMA1_CHANNEL3 = 13;  // DMA1 Channel 3 Interrupt
    public static final int IRQ_DMA1_CHANNEL4 = 14;  // DMA1 Channel 4 Interrupt
    public static final int IRQ_DMA1_CHANNEL5 = 15;  // DMA1 Channel 5 Interrupt
    public static final int IRQ_DMA1_CHANNEL6 = 16;  // DMA1 Channel 6 Interrupt
    public static final int IRQ_DMA1_CHANNEL7 = 17;  // DMA1 Channel 7 Interrupt
    public static final int IRQ_ADC1_2 = 18;  // ADC1 and ADC2 Global Interrupt
    public static final int IRQ_USB_HP_CAN_TX = 19;  // USB HP/CAN TX Interrupts
    public static final int IRQ_USB_LP_CAN_RX0 = 20;  // USB LP/CAN RX0 Interrupt
    public static final int IRQ_CAN_RX1 = 21;  // CAN RX1 Interrupt
    public static final int IRQ_CAN_SCE = 22;  // CAN SCE Interrupt
    public static final int IRQ_EXTI9_5 = 23;  // EXTI Line 9..5 Interrupt
    public static final int IRQ_TIM1_BRK = 25;  // TIM1 Break Interrupt
    public static final int IRQ_TIM1_UP = 26;  // TIM1 Update Interrupt
    public static final int IRQ_TIM1_TRG_COM = 27;  // TIM1 Trigger and Commutation
    public static final int IRQ_TIM1_CC = 28;  // TIM1 Capture Compare Interrupt
    public static final int IRQ_TIM2 = 29;  // TIM2 Global Interrupt
    public static final int IRQ_TIM3 = 30;  // TIM3 Global Interrupt
    public static final int IRQ_TIM4 = 31;  // TIM4 Global Interrupt
    public static final int IRQ_I2C1_EV = 32;  // I2C1 Event Interrupt
    public static final int IRQ_I2C1_ER = 33;  // I2C1 Error Interrupt
    public static final int IRQ_I2C2_EV = 34;  // I2C2 Event Interrupt
    public static final int IRQ_I2C2_ER = 35;  // I2C2 Error Interrupt
    public static final int IRQ_SPI1 = 35;  // SPI1 Global Interrupt
    public static final int IRQ_SPI2 = 36;  // SPI2 Global Interrupt
    public static final int IRQ_USART1 = 37;  // USART1 Global Interrupt
    public static final int IRQ_USART2 = 38;  // USART2 Global Interrupt
    public static final int IRQ_USART3 = 39;  // USART3 Global Interrupt
    public static final int IRQ_EXTI15_10 = 40;  // EXTI Line 15..10 Interrupt
    public static final int IRQ_RTCALARM = 41;  // RTC Alarm through EXTI
    public static final int IRQ_USBWAKEUP = 42;  // USB Wakeup from suspend

    // 引脚定义
    public static final int PIN_VBAT = 1;  // Battery Supply
    public static final int PIN_PC13 = 2;  // GPIO Port C Pin 13
    public static final int PIN_PC14 = 3;  // GPIO Port C Pin 14
    public static final int PIN_PC15 = 4;  // GPIO Port C Pin 15
    public static final int PIN_PD0 = 5;  // GPIO Port D Pin 0
    public static final int PIN_PD1 = 6;  // GPIO Port D Pin 1
    public static final int PIN_NRST = 7;  // Reset
    public static final int PIN_VSSA = 8;  // Analog Ground
    public static final int PIN_VDDA = 9;  // Analog Supply
    public static final int PIN_PA0 = 10;  // GPIO Port A Pin 0 / ADC1_IN0
    public static final int PIN_PA1 = 11;  // GPIO Port A Pin 1 / ADC1_IN1
    public static final int PIN_PA2 = 12;  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
    public static final int PIN_PA3 = 13;  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
    public static final int PIN_PA4 = 14;  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
    public static final int PIN_PA5 = 15;  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
    public static final int PIN_PA6 = 16;  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
    public static final int PIN_PA7 = 17;  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
    public static final int PIN_PB0 = 18;  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
    public static final int PIN_PB1 = 19;  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
    public static final int PIN_PB2 = 20;  // GPIO Port B Pin 2
    public static final int PIN_PB10 = 21;  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
    public static final int PIN_PB11 = 22;  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
    public static final int PIN_VSS = 23;  // Ground
    public static final int PIN_VDD = 24;  // Digital Supply
    public static final int PIN_PB12 = 25;  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
    public static final int PIN_PB13 = 26;  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
    public static final int PIN_PB14 = 27;  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
    public static final int PIN_PB15 = 28;  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
    public static final int PIN_PA8 = 29;  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
    public static final int PIN_PA9 = 30;  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
    public static final int PIN_PA10 = 31;  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
    public static final int PIN_PA11 = 32;  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
    public static final int PIN_PA12 = 33;  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
    public static final int PIN_PA13 = 34;  // JTMS/SWDIO
    public static final int PIN_PA14 = 37;  // JTCK/SWCLK
    public static final int PIN_PA15 = 38;  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
    public static final int PIN_PB3 = 39;  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
    public static final int PIN_PB4 = 40;  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
    public static final int PIN_PB5 = 41;  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
    public static final int PIN_PB6 = 42;  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
    public static final int PIN_PB7 = 43;  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
    public static final int PIN_BOOT0 = 44;  // Boot Selection
    public static final int PIN_PB8 = 45;  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
    public static final int PIN_PB9 = 46;  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
    public static final int PIN_VSS = 47;  // Ground
    public static final int PIN_VDD = 48;  // Digital Supply

    public static native void stm32f103c8t6_init();
}
