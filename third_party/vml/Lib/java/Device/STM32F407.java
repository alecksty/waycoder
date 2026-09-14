package vml.device.stmicroelectronics.stm32f407vgt6;

/**
 * STM32F407VGT6 寄存器定义
 * 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
 * 版本: 1.0
 */
public final class STM32F407VGT6 {
    private STM32F407VGT6() {} // 工具类
    // CPU架构: ARM-Cortex-M4, 32位, 16000000 Hz

    // 寄存器定义
    // General Purpose Register 0
    public static final int R0_ADDR = (int)0x00000000;

    // General Purpose Register 1
    public static final int R1_ADDR = (int)0x00000004;

    // General Purpose Register 2
    public static final int R2_ADDR = (int)0x00000008;

    // General Purpose Register 3
    public static final int R3_ADDR = (int)0x0000000C;

    // General Purpose Register 4
    public static final int R4_ADDR = (int)0x00000010;

    // General Purpose Register 5
    public static final int R5_ADDR = (int)0x00000014;

    // General Purpose Register 6
    public static final int R6_ADDR = (int)0x00000018;

    // General Purpose Register 7
    public static final int R7_ADDR = (int)0x0000001C;

    // General Purpose Register 8
    public static final int R8_ADDR = (int)0x00000020;

    // General Purpose Register 9
    public static final int R9_ADDR = (int)0x00000024;

    // General Purpose Register 10
    public static final int R10_ADDR = (int)0x00000028;

    // General Purpose Register 11
    public static final int R11_ADDR = (int)0x0000002C;

    // General Purpose Register 12
    public static final int R12_ADDR = (int)0x00000030;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x00000034;

    // Link Register
    public static final int LR_ADDR = (int)0x00000038;

    // Program Counter
    public static final int PC_ADDR = (int)0x0000003C;

    // Program Status Register
    public static final int PSR_ADDR = (int)0x00000040;
    public static final int PSR_N = 31;  // Negative Flag
    public static final int PSR_Z = 30;  // Zero Flag
    public static final int PSR_C = 29;  // Carry Flag
    public static final int PSR_V = 28;  // Overflow Flag
    public static final int PSR_Q = 27;  // SAT Flag
    public static final int PSR_ICI = 0;  // ICI/IT
    public static final int PSR_GE = 0;  // Greater than or Equal
    public static final int PSR_IT = 0;  // IT status
    public static final int PSR_Q = 9;  // APSR
    public static final int PSR_IPSR = 0;  // IPSR

    // Priority Mask Register
    public static final int PRIMASK_ADDR = (int)0xE0000E20;

    // Base Priority Register
    public static final int BASEPRI_ADDR = (int)0xE0000E24;

    // Fault Mask Register
    public static final int FAULTMASK_ADDR = (int)0xE0000E28;

    // Control Register
    public static final int CONTROL_ADDR = (int)0xE0000E2C;

    // FPU Status Control
    public static final int FPSCR_ADDR = (int)E0000EF34;

    // FP Register 0
    public static final int S0_ADDR = (int)0xE0000EF00;

    // FP Register 1
    public static final int S1_ADDR = (int)0xE0000EF04;

    // FP Register 2
    public static final int S2_ADDR = (int)0xE0000EF08;

    // FP Register 3
    public static final int S3_ADDR = (int)0xE0000EF0C;

    // FP Register 4
    public static final int S4_ADDR = (int)0xE0000EF10;

    // FP Register 5
    public static final int S5_ADDR = (int)0xE0000EF14;

    // FP Register 6
    public static final int S6_ADDR = (int)0xE0000EF18;

    // FP Register 7
    public static final int S7_ADDR = (int)0xE0000EF1C;

    // FP Register 8
    public static final int S8_ADDR = (int)0xE0000EF20;

    // FP Register 9
    public static final int S9_ADDR = (int)0xE0000EF24;

    // FP Register 10
    public static final int S10_ADDR = (int)0xE0000EF28;

    // FP Register 11
    public static final int S11_ADDR = (int)0xE0000EF2C;

    // FP Register 12
    public static final int S12_ADDR = (int)0xE0000EF30;

    // FP Register 13
    public static final int S13_ADDR = (int)0xE0000EF34;

    // FP Register 14
    public static final int S14_ADDR = (int)0xE0000EF38;

    // FP Register 15
    public static final int S15_ADDR = (int)0xE0000EF3C;

    // FP Register 16
    public static final int S16_ADDR = (int)0xE0000EF40;

    // FP Register 17
    public static final int S17_ADDR = (int)0xE0000EF44;

    // FP Register 18
    public static final int S18_ADDR = (int)0xE0000EF48;

    // FP Register 19
    public static final int S19_ADDR = (int)0xE0000EF4C;

    // FP Register 20
    public static final int S20_ADDR = (int)0xE0000EF50;

    // FP Register 21
    public static final int S21_ADDR = (int)0xE0000EF54;

    // FP Register 22
    public static final int S22_ADDR = (int)0xE0000EF58;

    // FP Register 23
    public static final int S23_ADDR = (int)0xE0000EF5C;

    // FP Register 24
    public static final int S24_ADDR = (int)0xE0000EF60;

    // FP Register 25
    public static final int S25_ADDR = (int)0xE0000EF64;

    // FP Register 26
    public static final int S26_ADDR = (int)0xE0000EF68;

    // FP Register 27
    public static final int S27_ADDR = (int)0xE0000EF6C;

    // FP Register 28
    public static final int S28_ADDR = (int)0xE0000EF70;

    // FP Register 29
    public static final int S29_ADDR = (int)0xE0000EF74;

    // FP Register 30
    public static final int S30_ADDR = (int)0xE0000EF78;

    // FP Register 31
    public static final int S31_ADDR = (int)0xE0000EF7C;

    // 内存段定义
    // Main Flash (1MB)
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x080FFFFF;
    public static final int FLASH_SIZE = 1048576;

    // System Flash (32KB)
    public static final int SYSTEM_START = (int)0x1FFF0000;
    public static final int SYSTEM_END = (int)0x1FFF7FFF;
    public static final int SYSTEM_SIZE = 32768;

    // Option Bytes (16KB)
    public static final int OPTION_START = (int)0x1FFF8000;
    public static final int OPTION_END = (int)0x1FFFC000;
    public static final int OPTION_SIZE = 16384;

    // SRAM1 (128KB)
    public static final int SRAM1_START = (int)0x20000000;
    public static final int SRAM1_END = (int)0x2001FFFF;
    public static final int SRAM1_SIZE = 131072;

    // SRAM2 (64KB)
    public static final int SRAM2_START = (int)0x20020000;
    public static final int SRAM2_END = (int)0x2002FFFF;
    public static final int SRAM2_SIZE = 65536;

    // CCM RAM (64KB)
    public static final int CCM_START = (int)0x20030000;
    public static final int CCM_END = (int)0x2003FFFF;
    public static final int CCM_SIZE = 65536;

    // Peripheral Registers
    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x40023FFF;
    public static final int PERIPHERAL_SIZE = 147456;

    // FMC Registers
    public static final int FMC_START = (int)0x40020000;
    public static final int FMC_END = (int)0x40023FFF;
    public static final int FMC_SIZE = 16384;

    // FSMC Registers
    public static final int FSMC_START = (int)0x40000000;
    public static final int FSMC_END = (int)0x400003FF;
    public static final int FSMC_SIZE = 1024;

    // GPIOA Registers
    public static final int GPIOA_START = (int)0x40020000;
    public static final int GPIOA_END = (int)0x400203FF;
    public static final int GPIOA_SIZE = 1024;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40023800;
    public static final int RCC_CR = (int)0x40023800;
    public static final int RCC_PLLCFGR = (int)0x40023804;
    public static final int RCC_CFGR = (int)0x40023808;
    public static final int RCC_CIR = (int)0x4002380C;
    public static final int RCC_APB2RSTR = (int)0x40023810;
    public static final int RCC_APB1RSTR = (int)0x40023814;
    public static final int RCC_AHB1ENR = (int)0x40023830;
    public static final int RCC_AHB2ENR = (int)0x40023834;
    public static final int RCC_AHB3ENR = (int)0x40023838;
    public static final int RCC_APB2ENR = (int)0x40023840;
    public static final int RCC_APB1ENR = (int)0x40023844;
    public static final int RCC_BDCR = (int)0x40023850;
    public static final int RCC_CSR = (int)0x40023854;
    public static final int RCC_AHB1RSTR = (int)0x40023820;
    public static final int RCC_AHB2RSTR = (int)0x40023824;
    public static final int RCC_AHB3RSTR = (int)0x40023828;
    public static final int RCC_CFGR2 = (int)0x40023818;
    public static final int RCC_CFGR3 = (int)0x4002381C;
    public static final int RCC_PLL2CFGR = (int)0x40023860;
    public static final int RCC_PLL2DIV = (int)0x40023864;
    public static final int RCC_PLL3CFGR = (int)0x40023868;
    public static final int RCC_PLL3DIV = (int)0x4002386C;

    // Flash Interface
    public static final int FLASH_BASE = (int)0x40023C00;
    public static final int FLASH_ACR = (int)0x40023C00;
    public static final int FLASH_KEYR = (int)0x40023C04;
    public static final int FLASH_OPTKEYR = (int)0x40023C08;
    public static final int FLASH_SR = (int)0x40023C0C;
    public static final int FLASH_CR = (int)0x40023C10;
    public static final int FLASH_OPTCR = (int)0x40023C14;
    public static final int FLASH_OPTCR1 = (int)0x40023C18;

    // Power Control
    public static final int PWR_BASE = (int)0x40007000;
    public static final int PWR_CR = (int)0x40007000;
    public static final int PWR_CSR = (int)0x40007004;

    // DMA1 Controller
    public static final int DMA1_BASE = (int)0x40026000;
    public static final int DMA1_LIFCR = (int)0x40026000;
    public static final int DMA1_HIFCR = (int)0x40026004;
    public static final int DMA1_LISR = (int)0x40026008;
    public static final int DMA1_HISR = (int)0x4002600C;
    public static final int DMA1_S0CR = (int)0x40026010;
    public static final int DMA1_S0NDTR = (int)0x40026014;
    public static final int DMA1_S0PAR = (int)0x40026018;
    public static final int DMA1_S0M0AR = (int)0x4002601C;
    public static final int DMA1_S0M1AR = (int)0x40026020;
    public static final int DMA1_S0FCR = (int)0x40026024;
    public static final int DMA1_S1CR = (int)0x40026028;
    public static final int DMA1_S1NDTR = (int)0x4002602C;
    public static final int DMA1_S2CR = (int)0x40026040;
    public static final int DMA1_S3CR = (int)0x40026058;
    public static final int DMA1_S4CR = (int)0x40026070;
    public static final int DMA1_S5CR = (int)0x40026088;
    public static final int DMA1_S6CR = (int)0x400260A0;
    public static final int DMA1_S7CR = (int)0x400260B8;

    // DMA2 Controller
    public static final int DMA2_BASE = (int)0x40026400;
    public static final int DMA2_LIFCR = (int)0x40026400;
    public static final int DMA2_HIFCR = (int)0x40026404;
    public static final int DMA2_LISR = (int)0x40026408;
    public static final int DMA2_HISR = (int)0x4002640C;
    public static final int DMA2_S0CR = (int)0x40026410;
    public static final int DMA2_S1CR = (int)0x40026428;
    public static final int DMA2_S2CR = (int)0x40026440;
    public static final int DMA2_S3CR = (int)0x40026458;
    public static final int DMA2_S4CR = (int)0x40026470;
    public static final int DMA2_S5CR = (int)0x40026488;
    public static final int DMA2_S6CR = (int)0x400264A0;
    public static final int DMA2_S7CR = (int)0x400264B8;

    // USART1
    public static final int USART1_BASE = (int)0x40011000;
    public static final int USART1_SR = (int)0x40011000;
    public static final int USART1_DR = (int)0x40011004;
    public static final int USART1_BRR = (int)0x40011008;
    public static final int USART1_CR1 = (int)0x4001100C;
    public static final int USART1_CR2 = (int)0x40011010;
    public static final int USART1_CR3 = (int)0x40011014;
    public static final int USART1_GTPR = (int)0x40011018;

    // USART2
    public static final int USART2_BASE = (int)0x40004400;
    public static final int USART2_SR = (int)0x40004400;
    public static final int USART2_DR = (int)0x40004404;
    public static final int USART2_BRR = (int)0x40004408;
    public static final int USART2_CR1 = (int)0x4000440C;
    public static final int USART2_CR2 = (int)0x40004410;
    public static final int USART2_CR3 = (int)0x40004414;

    // USART3
    public static final int USART3_BASE = (int)0x40004800;
    public static final int USART3_SR = (int)0x40004800;
    public static final int USART3_DR = (int)0x40004804;
    public static final int USART3_BRR = (int)0x40004808;
    public static final int USART3_CR1 = (int)0x4000480C;
    public static final int USART3_CR2 = (int)0x40004810;
    public static final int USART3_CR3 = (int)0x40004814;

    // SPI1
    public static final int SPI1_BASE = (int)0x40013000;
    public static final int SPI1_CR1 = (int)0x40013000;
    public static final int SPI1_CR2 = (int)0x40013004;
    public static final int SPI1_SR = (int)0x40013008;
    public static final int SPI1_DR = (int)0x4001300C;
    public static final int SPI1_CRCPR = (int)0x40013010;
    public static final int SPI1_RXCRCR = (int)0x40013014;
    public static final int SPI1_TXCRCR = (int)0x40013018;
    public static final int SPI1_I2SCFGR = (int)0x4001301C;

    // SPI2
    public static final int SPI2_BASE = (int)0x40003800;
    public static final int SPI2_CR1 = (int)0x40003800;
    public static final int SPI2_CR2 = (int)0x40003804;
    public static final int SPI2_SR = (int)0x40003808;
    public static final int SPI2_DR = (int)0x4000380C;
    public static final int SPI2_CRCPR = (int)0x40003810;
    public static final int SPI2_RXCRCR = (int)0x40003814;

    // SPI3
    public static final int SPI3_BASE = (int)0x40003C00;
    public static final int SPI3_CR1 = (int)0x40003C00;
    public static final int SPI3_CR2 = (int)0x40003C04;
    public static final int SPI3_SR = (int)0x40003C08;
    public static final int SPI3_DR = (int)0x40003C0C;

    // I2C1
    public static final int I2C1_BASE = (int)0x40005400;
    public static final int I2C1_CR1 = (int)0x40005400;
    public static final int I2C1_CR2 = (int)0x40005404;
    public static final int I2C1_OAR1 = (int)0x40005408;
    public static final int I2C1_OAR2 = (int)0x4000540C;
    public static final int I2C1_DR = (int)0x40005410;
    public static final int I2C1_SR1 = (int)0x40005414;
    public static final int I2C1_SR2 = (int)0x40005418;
    public static final int I2C1_CCR = (int)0x4000541C;
    public static final int I2C1_TRISE = (int)0x40005420;
    public static final int I2C1_FLTR = (int)0x40005424;

    // I2C2
    public static final int I2C2_BASE = (int)0x40005800;
    public static final int I2C2_CR1 = (int)0x40005800;
    public static final int I2C2_CR2 = (int)0x40005804;
    public static final int I2C2_OAR1 = (int)0x40005808;
    public static final int I2C2_OAR2 = (int)0x4000580C;
    public static final int I2C2_DR = (int)0x40005810;
    public static final int I2C2_SR1 = (int)0x40005814;
    public static final int I2C2_SR2 = (int)0x40005818;
    public static final int I2C2_CCR = (int)0x4000581C;
    public static final int I2C2_TRISE = (int)0x40005820;

    // I2C3
    public static final int I2C3_BASE = (int)0x40005C00;
    public static final int I2C3_CR1 = (int)0x40005C00;
    public static final int I2C3_CR2 = (int)0x40005C04;
    public static final int I2C3_OAR1 = (int)0x40005C08;
    public static final int I2C3_DR = (int)0x40005C10;
    public static final int I2C3_SR1 = (int)0x40005C14;
    public static final int I2C3_SR2 = (int)0x40005C18;
    public static final int I2C3_CCR = (int)0x40005C1C;

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
    public static final int TIM1_DCR = (int)0x40012C48;
    public static final int TIM1_DMAR = (int)0x40012C4C;

    // General Purpose Timer 2
    public static final int TIM2_BASE = (int)0x40000000;
    public static final int TIM2_CR1 = (int)0x40000000;
    public static final int TIM2_CR2 = (int)0x40000004;
    public static final int TIM2_SMCR = (int)0x40000008;
    public static final int TIM2_DIER = (int)0x4000000C;
    public static final int TIM2_SR = (int)0x40000010;
    public static final int TIM2_EGR = (int)0x40000014;
    public static final int TIM2_CCMR1 = (int)0x40000018;
    public static final int TIM2_CCMR2 = (int)0x4000001C;
    public static final int TIM2_CCER = (int)0x40000020;
    public static final int TIM2_CNT = (int)0x40000024;
    public static final int TIM2_PSC = (int)0x40000028;
    public static final int TIM2_ARR = (int)0x4000002C;
    public static final int TIM2_CCR1 = (int)0x40000034;
    public static final int TIM2_CCR2 = (int)0x40000038;
    public static final int TIM2_CCR3 = (int)0x4000003C;
    public static final int TIM2_CCR4 = (int)0x40000040;

    // General Purpose Timer 3
    public static final int TIM3_BASE = (int)0x40000400;
    public static final int TIM3_CR1 = (int)0x40000400;
    public static final int TIM3_SR = (int)0x40000410;
    public static final int TIM3_CCMR1 = (int)0x40000418;
    public static final int TIM3_CCER = (int)0x40000420;
    public static final int TIM3_CNT = (int)0x40000424;
    public static final int TIM3_PSC = (int)0x40000428;
    public static final int TIM3_ARR = (int)0x4000042C;
    public static final int TIM3_CCR1 = (int)0x40000434;
    public static final int TIM3_CCR2 = (int)0x40000438;
    public static final int TIM3_CCR3 = (int)0x4000043C;
    public static final int TIM3_CCR4 = (int)0x40000440;

    // General Purpose Timer 4
    public static final int TIM4_BASE = (int)0x40000800;
    public static final int TIM4_CR1 = (int)0x40000800;
    public static final int TIM4_SR = (int)0x40000810;
    public static final int TIM4_CCMR1 = (int)0x40000818;
    public static final int TIM4_CCER = (int)0x40000820;
    public static final int TIM4_CNT = (int)0x40000824;
    public static final int TIM4_PSC = (int)0x40000828;
    public static final int TIM4_ARR = (int)0x4000082C;
    public static final int TIM4_CCR1 = (int)0x40000834;
    public static final int TIM4_CCR2 = (int)0x40000838;
    public static final int TIM4_CCR3 = (int)0x4000083C;
    public static final int TIM4_CCR4 = (int)0x40000840;

    // General Purpose Timer 5
    public static final int TIM5_BASE = (int)0x40000C00;
    public static final int TIM5_CR1 = (int)0x40000C00;
    public static final int TIM5_SR = (int)0x40000C10;
    public static final int TIM5_CNT = (int)0x40000C24;
    public static final int TIM5_PSC = (int)0x40000C28;
    public static final int TIM5_ARR = (int)0x40000C2C;
    public static final int TIM5_CCR1 = (int)0x40000C34;
    public static final int TIM5_CCR2 = (int)0x40000C38;
    public static final int TIM5_CCR3 = (int)0x40000C3C;
    public static final int TIM5_CCR4 = (int)0x40000C40;

    // General Purpose Timer 9
    public static final int TIM9_BASE = (int)0x40014C00;
    public static final int TIM9_CR1 = (int)0x40014C00;
    public static final int TIM9_SMCR = (int)0x40014C08;
    public static final int TIM9_DIER = (int)0x40014C0C;
    public static final int TIM9_SR = (int)0x40014C10;
    public static final int TIM9_EGR = (int)0x40014C14;
    public static final int TIM9_CCMR1 = (int)0x40014C18;
    public static final int TIM9_CCER = (int)0x40014C20;
    public static final int TIM9_CNT = (int)0x40014C24;
    public static final int TIM9_PSC = (int)0x40014C28;
    public static final int TIM9_ARR = (int)0x40014C2C;
    public static final int TIM9_CCR1 = (int)0x40014C34;
    public static final int TIM9_CCR2 = (int)0x40014C38;

    // General Purpose Timer 10
    public static final int TIM10_BASE = (int)0x40015000;
    public static final int TIM10_CR1 = (int)0x40015000;
    public static final int TIM10_DIER = (int)0x4001500C;
    public static final int TIM10_SR = (int)0x40015010;
    public static final int TIM10_EGR = (int)0x40015014;
    public static final int TIM10_CCMR1 = (int)0x40015018;
    public static final int TIM10_CCER = (int)0x40015020;
    public static final int TIM10_CNT = (int)0x40015024;
    public static final int TIM10_PSC = (int)0x40015028;
    public static final int TIM10_ARR = (int)0x4001502C;
    public static final int TIM10_CCR1 = (int)0x40015034;

    // ADC1
    public static final int ADC1_BASE = (int)0x40012000;
    public static final int ADC1_SR = (int)0x40012000;
    public static final int ADC1_CR1 = (int)0x40012004;
    public static final int ADC1_CR2 = (int)0x40012008;
    public static final int ADC1_SMPR1 = (int)0x4001200C;
    public static final int ADC1_SMPR2 = (int)0x40012010;
    public static final int ADC1_JOFR1 = (int)0x40012014;
    public static final int ADC1_JOFR2 = (int)0x40012018;
    public static final int ADC1_JOFR3 = (int)0x4001201C;
    public static final int ADC1_JOFR4 = (int)0x40012020;
    public static final int ADC1_HTR = (int)0x40012024;
    public static final int ADC1_LTR = (int)0x40012028;
    public static final int ADC1_SQR1 = (int)0x4001202C;
    public static final int ADC1_SQR2 = (int)0x40012030;
    public static final int ADC1_SQR3 = (int)0x40012034;
    public static final int ADC1_JSQR = (int)0x4001203C;
    public static final int ADC1_JDR1 = (int)0x40012040;
    public static final int ADC1_JDR2 = (int)0x40012044;
    public static final int ADC1_JDR3 = (int)0x40012048;
    public static final int ADC1_JDR4 = (int)0x4001204C;
    public static final int ADC1_DR = (int)0x40012050;

    // ADC2
    public static final int ADC2_BASE = (int)0x40012100;
    public static final int ADC2_SR = (int)0x40012100;
    public static final int ADC2_CR1 = (int)0x40012104;
    public static final int ADC2_CR2 = (int)0x40012108;
    public static final int ADC2_SMPR1 = (int)0x4001210C;
    public static final int ADC2_SMPR2 = (int)0x40012110;
    public static final int ADC2_SQR1 = (int)0x4001212C;
    public static final int ADC2_DR = (int)0x40012150;

    // ADC3
    public static final int ADC3_BASE = (int)0x40012200;
    public static final int ADC3_SR = (int)0x40012200;
    public static final int ADC3_CR1 = (int)0x40012204;
    public static final int ADC3_CR2 = (int)0x40012208;
    public static final int ADC3_SMPR1 = (int)0x4001220C;
    public static final int ADC3_SMPR2 = (int)0x40012210;
    public static final int ADC3_SQR1 = (int)0x4001222C;
    public static final int ADC3_DR = (int)0x40012250;

    // System Configuration Controller
    public static final int SYSCFG_BASE = (int)0x40013800;
    public static final int SYSCFG_CFGR = (int)0x40013800;
    public static final int SYSCFG_EXTICR1 = (int)0x40013808;
    public static final int SYSCFG_EXTICR2 = (int)0x4001380C;
    public static final int SYSCFG_EXTICR3 = (int)0x40013810;
    public static final int SYSCFG_EXTICR4 = (int)0x40013814;
    public static final int SYSCFG_CBR = (int)0x4001381C;

    // External Interrupt/Event Controller
    public static final int EXTI_BASE = (int)0x40013C00;
    public static final int EXTI_IMR = (int)0x40013C00;
    public static final int EXTI_EMR = (int)0x40013C04;
    public static final int EXTI_RTSR = (int)0x40013C08;
    public static final int EXTI_FTSR = (int)0x40013C0C;
    public static final int EXTI_SWIER = (int)0x40013C10;
    public static final int EXTI_PR = (int)0x40013C14;

    // Random Number Generator
    public static final int RNG_BASE = (int)0x50060800;
    public static final int RNG_CR = (int)0x50060800;
    public static final int RNG_SR = (int)0x50060804;
    public static final int RNG_DR = (int)0x50060808;

    // CRYP Accelerator
    public static final int CRYP_BASE = (int)0x50060000;
    public static final int CRYP_CR = (int)0x50060000;
    public static final int CRYP_SR = (int)0x50060004;
    public static final int CRYP_DIN = (int)0x50060008;
    public static final int CRYP_DOUT = (int)0x5006000C;
    public static final int CRYP_DMACR = (int)0x50060010;
    public static final int CRYP_IMSCR = (int)0x50060014;
    public static final int CRYP_RISR = (int)0x50060018;
    public static final int CRYP_MISR = (int)0x5006001C;
    public static final int CRYP_K0LR = (int)0x50060020;
    public static final int CRYP_K0RR = (int)0x50060024;
    public static final int CRYP_K1LR = (int)0x50060028;
    public static final int CRYP_K1RR = (int)0x5006002C;
    public static final int CRYP_K2LR = (int)0x50060030;
    public static final int CRYP_K2RR = (int)0x50060034;
    public static final int CRYP_K3LR = (int)0x50060038;
    public static final int CRYP_K3RR = (int)0x5006003C;
    public static final int CRYP_IV0LR = (int)0x50060040;
    public static final int CRYP_IV0RR = (int)0x50060044;
    public static final int CRYP_IV1LR = (int)0x50060048;
    public static final int CRYP_IV1RR = (int)0x5006004C;

    // HASH Accelerator
    public static final int HASH_BASE = (int)0x50060400;
    public static final int HASH_CR = (int)0x50060400;
    public static final int HASH_DIN = (int)0x50060404;
    public static final int HASH_DINSTAT = (int)0x50060408;
    public static final int HASH_HR = (int)0x5006040C;
    public static final int HASH_IMR = (int)0x50060420;
    public static final int HASH_SR = (int)0x50060424;

    // Digital Camera Interface
    public static final int DCMI_BASE = (int)0x50050000;
    public static final int DCMI_CR = (int)0x50050000;
    public static final int DCMI_SR = (int)0x50050004;
    public static final int DCMI_RISR = (int)0x50050008;
    public static final int DCMI_IER = (int)0x5005000C;
    public static final int DCMI_MISR = (int)0x50050010;
    public static final int DCMI_ICR = (int)0x50050014;
    public static final int DCMI_MFISH = (int)0x5005001C;
    public static final int DCMI_CWSTRT = (int)0x50050020;
    public static final int DCMI_CWSIZE = (int)0x50050024;
    public static final int DCMI_DR = (int)0x50050028;
    public static final int DCMI_OR = (int)0x5005002C;

    // USB OTG High Speed
    public static final int USB_OTG_HS_BASE = (int)0x40040000;
    public static final int USB_OTG_HS_GOTGCTL = (int)0x40040000;
    public static final int USB_OTG_HS_GOTGINT = (int)0x40040004;
    public static final int USB_OTG_HS_GINTMSK = (int)0x40040008;
    public static final int USB_OTG_HS_GRSTCTL = (int)0x4004000C;
    public static final int USB_OTG_HS_GINTSTS = (int)0x40040010;
    public static final int USB_OTG_HS_GRXSTSR = (int)0x40040014;
    public static final int USB_OTG_HS_GRXFSIZ = (int)0x40040024;
    public static final int USB_OTG_HS_HNPTXFSIZ = (int)0x40040028;
    public static final int USB_OTG_HS_HNPTXSTS = (int)0x4004002C;
    public static final int USB_OTG_HS_GCCFG = (int)0x40040038;
    public static final int USB_OTG_HS_CID = (int)0x4004003C;
    public static final int USB_OTG_HS_HPTXFSIZ = (int)0x40040100;
    public static final int USB_OTG_HS_DIEPTXF = (int)0x40040200;

    // Ethernet
    public static final int ETH_BASE = (int)0x40028000;
    public static final int ETH_MACCR = (int)0x40028000;
    public static final int ETH_MACFFR = (int)0x40028004;
    public static final int ETH_MACHTHR = (int)0x40028008;
    public static final int ETH_MACHTLR = (int)0x4002800C;
    public static final int ETH_MACMIIAR = (int)0x40028010;
    public static final int ETH_MACMIIDR = (int)0x40028014;
    public static final int ETH_MACCR = (int)0x40028018;
    public static final int ETH_MACVLANTR = (int)0x4002801C;
    public static final int ETH_MACRWUFFR = (int)0x40028028;
    public static final int ETH_MACPMTCSR = (int)0x4002802C;
    public static final int ETH_MACSR = (int)0x40028030;
    public static final int ETH_MACIMR = (int)0x40028034;
    public static final int ETH_MACA0HR = (int)0x40028040;
    public static final int ETH_MACA0LR = (int)0x40028044;
    public static final int ETH_MACA1HR = (int)0x40028048;
    public static final int ETH_MACA1LR = (int)0x4002804C;
    public static final int ETH_MMCCR = (int)0x40028100;
    public static final int ETH_MMCRIR = (int)0x40028104;
    public static final int ETH_MMCTIR = (int)0x40028108;
    public static final int ETH_MMCRIMR = (int)0x4002810C;
    public static final int ETH_MMCTIMR = (int)0x40028110;
    public static final int ETH_MMCTGBSCCR = (int)0x40028114;
    public static final int ETH_MMCRGUFCCR = (int)0x40028118;
    public static final int ETH_PTPTSCR = (int)0x40028700;
    public static final int ETH_PTPSSIR = (int)0x40028704;
    public static final int ETH_PTPTSHR = (int)0x40028708;
    public static final int ETH_PTPTSLR = (int)0x4002870C;
    public static final int ETH_PTPTSHUR = (int)0x40028710;
    public static final int ETH_PTPTSLUR = (int)0x40028714;
    public static final int ETH_PTPTSAR = (int)0x40028718;
    public static final int ETH_PTPTTHR = (int)0x4002871C;
    public static final int ETH_PTPTTLR = (int)0x40028720;
    public static final int ETH_PTPTSR = (int)0x40028728;
    public static final int ETH_DMABMR = (int)0x40029000;
    public static final int ETH_DMASR = (int)0x40029004;
    public static final int ETH_DMAOMR = (int)0x40029008;
    public static final int ETH_DMAIER = (int)0x4002900C;
    public static final int ETH_DMAMFBOCR = (int)0x40029010;
    public static final int ETH_DMACHTDR = (int)0x40029014;
    public static final int ETH_DMACHRDR = (int)0x40029018;
    public static final int ETH_DMACHTBAR = (int)0x4002901C;
    public static final int ETH_DMACHRBAR = (int)0x40029020;

    // 中断向量定义
    public static final int IRQ_WWDG = 0;  // Window WatchDog interrupt
    public static final int IRQ_PVD = 1;  // PVD through EXTI line detection interrupt
    public static final int IRQ_TAMPER = 2;  // Tamper interrupt
    public static final int IRQ_RTC_WKUP = 3;  // RTC Wakeup interrupt
    public static final int IRQ_FLASH = 4;  // FLASH global interrupt
    public static final int IRQ_RCC = 5;  // RCC global interrupt
    public static final int IRQ_EXTI0 = 6;  // EXTI Line0 interrupt
    public static final int IRQ_EXTI1 = 7;  // EXTI Line1 interrupt
    public static final int IRQ_EXTI2 = 8;  // EXTI Line2 interrupt
    public static final int IRQ_EXTI3 = 9;  // EXTI Line3 interrupt
    public static final int IRQ_EXTI4 = 10;  // EXTI Line4 interrupt
    public static final int IRQ_DMA1_STREAM0 = 11;  // DMA1 Stream0 global interrupt
    public static final int IRQ_DMA1_STREAM1 = 12;  // DMA1 Stream1 global interrupt
    public static final int IRQ_DMA1_STREAM2 = 13;  // DMA1 Stream2 global interrupt
    public static final int IRQ_DMA1_STREAM3 = 14;  // DMA1 Stream3 global interrupt
    public static final int IRQ_DMA1_STREAM4 = 15;  // DMA1 Stream4 global interrupt
    public static final int IRQ_DMA1_STREAM5 = 16;  // DMA1 Stream5 global interrupt
    public static final int IRQ_DMA1_STREAM6 = 17;  // DMA1 Stream6 global interrupt
    public static final int IRQ_ADC = 18;  // ADC1 global interrupt
    public static final int IRQ_CAN1_TX = 19;  // CAN1 TX interrupts
    public static final int IRQ_CAN1_RX0 = 20;  // CAN1 RX0 interrupts
    public static final int IRQ_CAN1_RX1 = 21;  // CAN1 RX1 interrupt
    public static final int IRQ_CAN1_SCE = 22;  // CAN1 SCE interrupt
    public static final int IRQ_EXTI9_5 = 23;  // EXTI Line[9:5] interrupt
    public static final int IRQ_TIM1_BRK_TIM9 = 24;  // TIM1 Break interrupt and TIM9 global interrupt
    public static final int IRQ_TIM1_UP_TIM10 = 25;  // TIM1 Update interrupt and TIM10 global interrupt
    public static final int IRQ_TIM1_TRG_COM_TIM11 = 26;  // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
    public static final int IRQ_TIM1_CC = 27;  // TIM1 Capture Compare interrupt
    public static final int IRQ_TIM2 = 28;  // TIM2 global interrupt
    public static final int IRQ_TIM3 = 29;  // TIM3 global interrupt
    public static final int IRQ_TIM4 = 30;  // TIM4 global interrupt
    public static final int IRQ_I2C1_EV = 31;  // I2C1 Event interrupt
    public static final int IRQ_I2C1_ER = 32;  // I2C1 Error interrupt
    public static final int IRQ_I2C2_EV = 33;  // I2C2 Event interrupt
    public static final int IRQ_I2C2_ER = 34;  // I2C2 Error interrupt
    public static final int IRQ_SPI1 = 35;  // SPI1 global interrupt
    public static final int IRQ_SPI2 = 36;  // SPI2 global interrupt
    public static final int IRQ_USART1 = 37;  // USART1 global interrupt
    public static final int IRQ_USART2 = 38;  // USART2 global interrupt
    public static final int IRQ_USART3 = 39;  // USART3 global interrupt
    public static final int IRQ_EXTI15_10 = 40;  // EXTI Line[15:10] interrupts
    public static final int IRQ_RTC_ALARM = 41;  // RTC Alarm (A and B) through EXTI Line interrupt
    public static final int IRQ_OTG_FS_WKUP = 42;  // USB OTG FS Wakeup through EXTI interrupt
    public static final int IRQ_TIM8_BRK_TIM12 = 43;  // TIM8 Break interrupt and TIM12 global interrupt
    public static final int IRQ_TIM8_UP_TIM13 = 44;  // TIM8 Update interrupt and TIM13 global interrupt
    public static final int IRQ_TIM8_TRG_COM_TIM14 = 45;  // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
    public static final int IRQ_TIM8_CC = 46;  // TIM8 Capture Compare interrupt
    public static final int IRQ_SPI3 = 47;  // SPI3 global interrupt
    public static final int IRQ_UART4 = 48;  // UART4 global interrupt
    public static final int IRQ_UART5 = 49;  // UART5 global interrupt
    public static final int IRQ_TIM6 = 50;  // TIM6 global interrupt
    public static final int IRQ_TIM7 = 51;  // TIM7 global interrupt
    public static final int IRQ_DMA2_STREAM0 = 52;  // DMA2 Stream0 global interrupt
    public static final int IRQ_DMA2_STREAM1 = 53;  // DMA2 Stream1 global interrupt
    public static final int IRQ_DMA2_STREAM2 = 54;  // DMA2 Stream2 global interrupt
    public static final int IRQ_DMA2_STREAM3 = 55;  // DMA2 Stream3 global interrupt
    public static final int IRQ_DMA2_STREAM4 = 56;  // DMA2 Stream4 global interrupt
    public static final int IRQ_ETH = 57;  // Ethernet global interrupt
    public static final int IRQ_ETH_WKUP = 58;  // Ethernet Wakeup through EXTI interrupt
    public static final int IRQ_CAN2_TX = 59;  // CAN2 TX interrupts
    public static final int IRQ_CAN2_RX0 = 60;  // CAN2 RX0 interrupts
    public static final int IRQ_CAN2_RX1 = 61;  // CAN2 RX1 interrupt
    public static final int IRQ_CAN2_SCE = 62;  // CAN2 SCE interrupt
    public static final int IRQ_NA = 63;  // Not Available
    public static final int IRQ_OTG_FS = 64;  // USB OTG FS global interrupt
    public static final int IRQ_DCMI = 65;  // DCMI global interrupt
    public static final int IRQ_CRYP = 66;  // CRYP global interrupt
    public static final int IRQ_HASH_RNG = 67;  // HASH and RNG global interrupt
    public static final int IRQ_FPU = 68;  // FPU global interrupt

    // 引脚定义
    public static final int PIN_PE2 = 1;  // Tristate - any function
    public static final int PIN_PE3 = 2;  // Tristate - any function
    public static final int PIN_PE4 = 3;  // Tristate - any function
    public static final int PIN_PE5 = 4;  // Tristate - any function
    public static final int PIN_PE6 = 5;  // Tristate - any function
    public static final int PIN_VCAP1 = 6;  // 1.2V voltage supply
    public static final int PIN_VBAT = 7;  // Battery voltage supply
    public static final int PIN_PC13 = 8;  // TAMPER-RTC
    public static final int PIN_PC14 = 9;  // OSC32_IN
    public static final int PIN_PC15 = 10;  // OSC32_OUT
    public static final int PIN_PH0 = 11;  // OSC_IN
    public static final int PIN_PH1 = 12;  // OSC_OUT
    public static final int PIN_NRST = 13;  // External reset
    public static final int PIN_VSSA = 14;  // Analog ground
    public static final int PIN_VDDA = 15;  // Analog supply
    public static final int PIN_PA0 = 16;  // WKUP/USART_CTS
    public static final int PIN_PA1 = 17;  // USART_RTS
    public static final int PIN_PA2 = 18;  // USART_TX
    public static final int PIN_PA3 = 19;  // USART_RX
    public static final int PIN_PA4 = 20;  // DAC1_OUT
    public static final int PIN_PA5 = 21;  // DAC2_OUT
    public static final int PIN_PA6 = 22;  // SPI1_MISO
    public static final int PIN_PA7 = 23;  // SPI1_MOSI
    public static final int PIN_PA8 = 24;  // MCO1
    public static final int PIN_PA9 = 25;  // USART1_TX
    public static final int PIN_PA10 = 26;  // USART1_RX
    public static final int PIN_PA11 = 27;  // USB_DM
    public static final int PIN_PA12 = 28;  // USB_DP
    public static final int PIN_PA13 = 29;  // JTMS/SWDIO
    public static final int PIN_VCAP2 = 30;  // 1.2V voltage supply
    public static final int PIN_PA14 = 31;  // JTCK/SWCLK
    public static final int PIN_PA15 = 32;  // JTDI
    public static final int PIN_PB0 = 33;  // SPI1_CS/TIM3_CH3
    public static final int PIN_PB1 = 34;  // TIM3_CH4
    public static final int PIN_PB2 = 35;  // BOOT1
    public static final int PIN_PB3 = 36;  // JTDO/TRACESWO
    public static final int PIN_PB4 = 37;  // NJTRST
    public static final int PIN_PB5 = 38;  // CAN2_RX/TIM3_CH2
    public static final int PIN_PB6 = 39;  // USART1_TX
    public static final int PIN_PB7 = 40;  // USART1_RX
    public static final int PIN_PB8 = 41;  // I2C1_SCL/TIM4_CH1
    public static final int PIN_PB9 = 42;  // I2C1_SDA/TIM4_CH2
    public static final int PIN_PB10 = 43;  // I2C2_SCL/USART3_TX
    public static final int PIN_PB11 = 44;  // I2C2_SDA/USART3_RX
    public static final int PIN_PB12 = 45;  // SPI2_CS/I2C2_SMBA
    public static final int PIN_PB13 = 46;  // SPI2_SCK
    public static final int PIN_PB14 = 47;  // SPI2_MISO
    public static final int PIN_PB15 = 48;  // SPI2_MOSI
    public static final int PIN_PD0 = 49;  // FSMC_D2
    public static final int PIN_PD1 = 50;  // FSMC_D3
    public static final int PIN_PD2 = 51;  // TIM3_ETR/UART4_RX
    public static final int PIN_PD3 = 52;  // FSMC_CLK/UART4_CTS
    public static final int PIN_PD4 = 53;  // FSMC_NOE
    public static final int PIN_PD5 = 54;  // FSMC_NWE
    public static final int PIN_PD6 = 55;  // FSMC_NWAIT
    public static final int PIN_PD7 = 56;  // FSMC_NE1/FSMC_NCE2
    public static final int PIN_PD8 = 57;  // FSMC_D13/USART3_TX
    public static final int PIN_PD9 = 58;  // FSMC_D14/USART3_RX
    public static final int PIN_PD10 = 59;  // FSMC_D15/USART3_CK
    public static final int PIN_PD11 = 60;  // FSMC_A16/USART3_CTS
    public static final int PIN_PD12 = 61;  // FSMC_A17/TIM4_CH1
    public static final int PIN_PD13 = 62;  // FSMC_A18/TIM4_CH2
    public static final int PIN_PD14 = 63;  // FSMC_D0/TIM4_CH3
    public static final int PIN_PD15 = 64;  // FSMC_D1/TIM4_CH4
    public static final int PIN_PG0 = 65;  // FSMC_A10/TIM4_ETR
    public static final int PIN_PG1 = 66;  // FSMC_A11
    public static final int PIN_PE0 = 67;  // FSMC_NBL0/TIM4_CH3
    public static final int PIN_PE1 = 68;  // FSMC_NBL1/TIM4_CH4
    public static final int PIN_PE3 = 69;  // FSMC_A19
    public static final int PIN_PE4 = 70;  // FSMC_A20
    public static final int PIN_PE5 = 71;  // FSMC_A21
    public static final int PIN_PE6 = 72;  // FSMC_A22/TIM4_CH1
    public static final int PIN_PE7 = 73;  // FSMC_D4/USART6_RX
    public static final int PIN_PE8 = 74;  // FSMC_D5/USART6_TX
    public static final int PIN_PE9 = 75;  // FSMC_D6/TIM4_CH1
    public static final int PIN_PE10 = 76;  // FSMC_D7/USART6_RTS
    public static final int PIN_PE11 = 77;  // FSMC_D8
    public static final int PIN_PE12 = 78;  // FSMC_D9
    public static final int PIN_PE13 = 79;  // FSMC_D10
    public static final int PIN_PE14 = 80;  // FSMC_D11
    public static final int PIN_PE15 = 81;  // FSMC_D12
    public static final int PIN_PB10 = 82;  // I2C2_SCL/USART3_TX
    public static final int PIN_PB11 = 83;  // I2C2_SDA/USART3_RX
    public static final int PIN_PB12 = 84;  // SPI2_CS/I2C2_SMBA
    public static final int PIN_PB13 = 85;  // SPI2_SCK
    public static final int PIN_PB14 = 86;  // SPI2_MISO
    public static final int PIN_PB15 = 87;  // SPI2_MOSI
    public static final int PIN_PD8 = 88;  // FSMC_D13/USART3_TX
    public static final int PIN_PD9 = 89;  // FSMC_D14/USART3_RX
    public static final int PIN_PD10 = 90;  // FSMC_D15/USART3_CK
    public static final int PIN_PD11 = 91;  // FSMC_A16/USART3_CTS
    public static final int PIN_PD12 = 92;  // FSMC_A17/TIM4_CH1
    public static final int PIN_PD13 = 93;  // FSMC_A18/TIM4_CH2
    public static final int PIN_PD14 = 94;  // FSMC_D0/TIM4_CH3
    public static final int PIN_PD15 = 95;  // FSMC_D1/TIM4_CH4
    public static final int PIN_PG2 = 96;  // FSMC_A12
    public static final int PIN_PG3 = 97;  // FSMC_A13
    public static final int PIN_PG4 = 98;  // FSMC_A14
    public static final int PIN_PG5 = 99;  // FSMC_A15
    public static final int PIN_PG6 = 100;  // FSMC_INT2
    public static final int PIN_PG7 = 101;  // FSMC_INT3
    public static final int PIN_PG8 = 102;  // FSMC_INT4
    public static final int PIN_PG9 = 103;  // FSMC_NE2/FSMC_NCE3
    public static final int PIN_PG10 = 104;  // FSMC_NCE4_1
    public static final int PIN_PG11 = 105;  // FSMC_NCE4_2
    public static final int PIN_PG12 = 106;  // FSMC_NCE4_3
    public static final int PIN_PG13 = 107;  // FSMC_A24
    public static final int PIN_PG14 = 108;  // FSMC_A25
    public static final int PIN_PG15 = 109;  // FSMC_INT2
    public static final int PIN_VSS = 110;  // Ground
    public static final int PIN_VDD = 111;  // 3.3V Supply

    public static native void stm32f407vgt6_init();
}
