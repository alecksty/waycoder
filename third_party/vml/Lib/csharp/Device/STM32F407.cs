using System;

namespace VML.Device.STMicroelectronics.STM32F407VGT6
{
    /// <summary>
    /// STM32F407VGT6 寄存器定义
    /// 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
    /// 版本: 1.0
    /// </summary>
    public static class STM32F407VGT6
    {
        // CPU架构: ARM-Cortex-M4, 32位, 16000000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00000000;
        public static unsafe uint* R0 => (uint*)0x00000000;

        // General Purpose Register 1
        public const int R1_ADDR = 0x00000004;
        public static unsafe uint* R1 => (uint*)0x00000004;

        // General Purpose Register 2
        public const int R2_ADDR = 0x00000008;
        public static unsafe uint* R2 => (uint*)0x00000008;

        // General Purpose Register 3
        public const int R3_ADDR = 0x0000000C;
        public static unsafe uint* R3 => (uint*)0x0000000C;

        // General Purpose Register 4
        public const int R4_ADDR = 0x00000010;
        public static unsafe uint* R4 => (uint*)0x00000010;

        // General Purpose Register 5
        public const int R5_ADDR = 0x00000014;
        public static unsafe uint* R5 => (uint*)0x00000014;

        // General Purpose Register 6
        public const int R6_ADDR = 0x00000018;
        public static unsafe uint* R6 => (uint*)0x00000018;

        // General Purpose Register 7
        public const int R7_ADDR = 0x0000001C;
        public static unsafe uint* R7 => (uint*)0x0000001C;

        // General Purpose Register 8
        public const int R8_ADDR = 0x00000020;
        public static unsafe uint* R8 => (uint*)0x00000020;

        // General Purpose Register 9
        public const int R9_ADDR = 0x00000024;
        public static unsafe uint* R9 => (uint*)0x00000024;

        // General Purpose Register 10
        public const int R10_ADDR = 0x00000028;
        public static unsafe uint* R10 => (uint*)0x00000028;

        // General Purpose Register 11
        public const int R11_ADDR = 0x0000002C;
        public static unsafe uint* R11 => (uint*)0x0000002C;

        // General Purpose Register 12
        public const int R12_ADDR = 0x00000030;
        public static unsafe uint* R12 => (uint*)0x00000030;

        // Stack Pointer
        public const int SP_ADDR = 0x00000034;
        public static unsafe uint* SP => (uint*)0x00000034;

        // Link Register
        public const int LR_ADDR = 0x00000038;
        public static unsafe uint* LR => (uint*)0x00000038;

        // Program Counter
        public const int PC_ADDR = 0x0000003C;
        public static unsafe uint* PC => (uint*)0x0000003C;

        // Program Status Register
        public const int PSR_ADDR = 0x00000040;
        public static unsafe uint* PSR => (uint*)0x00000040;
        public const int PSR_N = 31;  // Negative Flag
        public const int PSR_Z = 30;  // Zero Flag
        public const int PSR_C = 29;  // Carry Flag
        public const int PSR_V = 28;  // Overflow Flag
        public const int PSR_Q = 27;  // SAT Flag
        public const int PSR_ICI = 0;  // ICI/IT
        public const int PSR_GE = 0;  // Greater than or Equal
        public const int PSR_IT = 0;  // IT status
        public const int PSR_Q = 9;  // APSR
        public const int PSR_IPSR = 0;  // IPSR

        // Priority Mask Register
        public const int PRIMASK_ADDR = 0xE0000E20;
        public static unsafe uint* PRIMASK => (uint*)0xE0000E20;

        // Base Priority Register
        public const int BASEPRI_ADDR = 0xE0000E24;
        public static unsafe uint* BASEPRI => (uint*)0xE0000E24;

        // Fault Mask Register
        public const int FAULTMASK_ADDR = 0xE0000E28;
        public static unsafe uint* FAULTMASK => (uint*)0xE0000E28;

        // Control Register
        public const int CONTROL_ADDR = 0xE0000E2C;
        public static unsafe uint* CONTROL => (uint*)0xE0000E2C;

        // FPU Status Control
        public const int FPSCR_ADDR = E0000EF34;
        public static unsafe uint* FPSCR => (uint*)E0000EF34;

        // FP Register 0
        public const int S0_ADDR = 0xE0000EF00;
        public static unsafe uint* S0 => (uint*)0xE0000EF00;

        // FP Register 1
        public const int S1_ADDR = 0xE0000EF04;
        public static unsafe uint* S1 => (uint*)0xE0000EF04;

        // FP Register 2
        public const int S2_ADDR = 0xE0000EF08;
        public static unsafe uint* S2 => (uint*)0xE0000EF08;

        // FP Register 3
        public const int S3_ADDR = 0xE0000EF0C;
        public static unsafe uint* S3 => (uint*)0xE0000EF0C;

        // FP Register 4
        public const int S4_ADDR = 0xE0000EF10;
        public static unsafe uint* S4 => (uint*)0xE0000EF10;

        // FP Register 5
        public const int S5_ADDR = 0xE0000EF14;
        public static unsafe uint* S5 => (uint*)0xE0000EF14;

        // FP Register 6
        public const int S6_ADDR = 0xE0000EF18;
        public static unsafe uint* S6 => (uint*)0xE0000EF18;

        // FP Register 7
        public const int S7_ADDR = 0xE0000EF1C;
        public static unsafe uint* S7 => (uint*)0xE0000EF1C;

        // FP Register 8
        public const int S8_ADDR = 0xE0000EF20;
        public static unsafe uint* S8 => (uint*)0xE0000EF20;

        // FP Register 9
        public const int S9_ADDR = 0xE0000EF24;
        public static unsafe uint* S9 => (uint*)0xE0000EF24;

        // FP Register 10
        public const int S10_ADDR = 0xE0000EF28;
        public static unsafe uint* S10 => (uint*)0xE0000EF28;

        // FP Register 11
        public const int S11_ADDR = 0xE0000EF2C;
        public static unsafe uint* S11 => (uint*)0xE0000EF2C;

        // FP Register 12
        public const int S12_ADDR = 0xE0000EF30;
        public static unsafe uint* S12 => (uint*)0xE0000EF30;

        // FP Register 13
        public const int S13_ADDR = 0xE0000EF34;
        public static unsafe uint* S13 => (uint*)0xE0000EF34;

        // FP Register 14
        public const int S14_ADDR = 0xE0000EF38;
        public static unsafe uint* S14 => (uint*)0xE0000EF38;

        // FP Register 15
        public const int S15_ADDR = 0xE0000EF3C;
        public static unsafe uint* S15 => (uint*)0xE0000EF3C;

        // FP Register 16
        public const int S16_ADDR = 0xE0000EF40;
        public static unsafe uint* S16 => (uint*)0xE0000EF40;

        // FP Register 17
        public const int S17_ADDR = 0xE0000EF44;
        public static unsafe uint* S17 => (uint*)0xE0000EF44;

        // FP Register 18
        public const int S18_ADDR = 0xE0000EF48;
        public static unsafe uint* S18 => (uint*)0xE0000EF48;

        // FP Register 19
        public const int S19_ADDR = 0xE0000EF4C;
        public static unsafe uint* S19 => (uint*)0xE0000EF4C;

        // FP Register 20
        public const int S20_ADDR = 0xE0000EF50;
        public static unsafe uint* S20 => (uint*)0xE0000EF50;

        // FP Register 21
        public const int S21_ADDR = 0xE0000EF54;
        public static unsafe uint* S21 => (uint*)0xE0000EF54;

        // FP Register 22
        public const int S22_ADDR = 0xE0000EF58;
        public static unsafe uint* S22 => (uint*)0xE0000EF58;

        // FP Register 23
        public const int S23_ADDR = 0xE0000EF5C;
        public static unsafe uint* S23 => (uint*)0xE0000EF5C;

        // FP Register 24
        public const int S24_ADDR = 0xE0000EF60;
        public static unsafe uint* S24 => (uint*)0xE0000EF60;

        // FP Register 25
        public const int S25_ADDR = 0xE0000EF64;
        public static unsafe uint* S25 => (uint*)0xE0000EF64;

        // FP Register 26
        public const int S26_ADDR = 0xE0000EF68;
        public static unsafe uint* S26 => (uint*)0xE0000EF68;

        // FP Register 27
        public const int S27_ADDR = 0xE0000EF6C;
        public static unsafe uint* S27 => (uint*)0xE0000EF6C;

        // FP Register 28
        public const int S28_ADDR = 0xE0000EF70;
        public static unsafe uint* S28 => (uint*)0xE0000EF70;

        // FP Register 29
        public const int S29_ADDR = 0xE0000EF74;
        public static unsafe uint* S29 => (uint*)0xE0000EF74;

        // FP Register 30
        public const int S30_ADDR = 0xE0000EF78;
        public static unsafe uint* S30 => (uint*)0xE0000EF78;

        // FP Register 31
        public const int S31_ADDR = 0xE0000EF7C;
        public static unsafe uint* S31 => (uint*)0xE0000EF7C;

        // 内存段定义
        // Main Flash (1MB)
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x080FFFFF;
        public const int FLASH_SIZE = 1048576;

        // System Flash (32KB)
        public const int SYSTEM_START = 0x1FFF0000;
        public const int SYSTEM_END = 0x1FFF7FFF;
        public const int SYSTEM_SIZE = 32768;

        // Option Bytes (16KB)
        public const int OPTION_START = 0x1FFF8000;
        public const int OPTION_END = 0x1FFFC000;
        public const int OPTION_SIZE = 16384;

        // SRAM1 (128KB)
        public const int SRAM1_START = 0x20000000;
        public const int SRAM1_END = 0x2001FFFF;
        public const int SRAM1_SIZE = 131072;

        // SRAM2 (64KB)
        public const int SRAM2_START = 0x20020000;
        public const int SRAM2_END = 0x2002FFFF;
        public const int SRAM2_SIZE = 65536;

        // CCM RAM (64KB)
        public const int CCM_START = 0x20030000;
        public const int CCM_END = 0x2003FFFF;
        public const int CCM_SIZE = 65536;

        // Peripheral Registers
        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x40023FFF;
        public const int PERIPHERAL_SIZE = 147456;

        // FMC Registers
        public const int FMC_START = 0x40020000;
        public const int FMC_END = 0x40023FFF;
        public const int FMC_SIZE = 16384;

        // FSMC Registers
        public const int FSMC_START = 0x40000000;
        public const int FSMC_END = 0x400003FF;
        public const int FSMC_SIZE = 1024;

        // GPIOA Registers
        public const int GPIOA_START = 0x40020000;
        public const int GPIOA_END = 0x400203FF;
        public const int GPIOA_SIZE = 1024;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40023800;
        public static unsafe uint* RCC_CR => (uint*)0x40023800;
        public static unsafe uint* RCC_PLLCFGR => (uint*)0x40023804;
        public static unsafe uint* RCC_CFGR => (uint*)0x40023808;
        public static unsafe uint* RCC_CIR => (uint*)0x4002380C;
        public static unsafe uint* RCC_APB2RSTR => (uint*)0x40023810;
        public static unsafe uint* RCC_APB1RSTR => (uint*)0x40023814;
        public static unsafe uint* RCC_AHB1ENR => (uint*)0x40023830;
        public static unsafe uint* RCC_AHB2ENR => (uint*)0x40023834;
        public static unsafe uint* RCC_AHB3ENR => (uint*)0x40023838;
        public static unsafe uint* RCC_APB2ENR => (uint*)0x40023840;
        public static unsafe uint* RCC_APB1ENR => (uint*)0x40023844;
        public static unsafe uint* RCC_BDCR => (uint*)0x40023850;
        public static unsafe uint* RCC_CSR => (uint*)0x40023854;
        public static unsafe uint* RCC_AHB1RSTR => (uint*)0x40023820;
        public static unsafe uint* RCC_AHB2RSTR => (uint*)0x40023824;
        public static unsafe uint* RCC_AHB3RSTR => (uint*)0x40023828;
        public static unsafe uint* RCC_CFGR2 => (uint*)0x40023818;
        public static unsafe uint* RCC_CFGR3 => (uint*)0x4002381C;
        public static unsafe uint* RCC_PLL2CFGR => (uint*)0x40023860;
        public static unsafe uint* RCC_PLL2DIV => (uint*)0x40023864;
        public static unsafe uint* RCC_PLL3CFGR => (uint*)0x40023868;
        public static unsafe uint* RCC_PLL3DIV => (uint*)0x4002386C;

        // Flash Interface
        public const int FLASH_BASE = 0x40023C00;
        public static unsafe uint* FLASH_ACR => (uint*)0x40023C00;
        public static unsafe uint* FLASH_KEYR => (uint*)0x40023C04;
        public static unsafe uint* FLASH_OPTKEYR => (uint*)0x40023C08;
        public static unsafe uint* FLASH_SR => (uint*)0x40023C0C;
        public static unsafe uint* FLASH_CR => (uint*)0x40023C10;
        public static unsafe uint* FLASH_OPTCR => (uint*)0x40023C14;
        public static unsafe uint* FLASH_OPTCR1 => (uint*)0x40023C18;

        // Power Control
        public const int PWR_BASE = 0x40007000;
        public static unsafe uint* PWR_CR => (uint*)0x40007000;
        public static unsafe uint* PWR_CSR => (uint*)0x40007004;

        // DMA1 Controller
        public const int DMA1_BASE = 0x40026000;
        public static unsafe uint* DMA1_LIFCR => (uint*)0x40026000;
        public static unsafe uint* DMA1_HIFCR => (uint*)0x40026004;
        public static unsafe uint* DMA1_LISR => (uint*)0x40026008;
        public static unsafe uint* DMA1_HISR => (uint*)0x4002600C;
        public static unsafe uint* DMA1_S0CR => (uint*)0x40026010;
        public static unsafe uint* DMA1_S0NDTR => (uint*)0x40026014;
        public static unsafe uint* DMA1_S0PAR => (uint*)0x40026018;
        public static unsafe uint* DMA1_S0M0AR => (uint*)0x4002601C;
        public static unsafe uint* DMA1_S0M1AR => (uint*)0x40026020;
        public static unsafe uint* DMA1_S0FCR => (uint*)0x40026024;
        public static unsafe uint* DMA1_S1CR => (uint*)0x40026028;
        public static unsafe uint* DMA1_S1NDTR => (uint*)0x4002602C;
        public static unsafe uint* DMA1_S2CR => (uint*)0x40026040;
        public static unsafe uint* DMA1_S3CR => (uint*)0x40026058;
        public static unsafe uint* DMA1_S4CR => (uint*)0x40026070;
        public static unsafe uint* DMA1_S5CR => (uint*)0x40026088;
        public static unsafe uint* DMA1_S6CR => (uint*)0x400260A0;
        public static unsafe uint* DMA1_S7CR => (uint*)0x400260B8;

        // DMA2 Controller
        public const int DMA2_BASE = 0x40026400;
        public static unsafe uint* DMA2_LIFCR => (uint*)0x40026400;
        public static unsafe uint* DMA2_HIFCR => (uint*)0x40026404;
        public static unsafe uint* DMA2_LISR => (uint*)0x40026408;
        public static unsafe uint* DMA2_HISR => (uint*)0x4002640C;
        public static unsafe uint* DMA2_S0CR => (uint*)0x40026410;
        public static unsafe uint* DMA2_S1CR => (uint*)0x40026428;
        public static unsafe uint* DMA2_S2CR => (uint*)0x40026440;
        public static unsafe uint* DMA2_S3CR => (uint*)0x40026458;
        public static unsafe uint* DMA2_S4CR => (uint*)0x40026470;
        public static unsafe uint* DMA2_S5CR => (uint*)0x40026488;
        public static unsafe uint* DMA2_S6CR => (uint*)0x400264A0;
        public static unsafe uint* DMA2_S7CR => (uint*)0x400264B8;

        // USART1
        public const int USART1_BASE = 0x40011000;
        public static unsafe uint* USART1_SR => (uint*)0x40011000;
        public static unsafe uint* USART1_DR => (uint*)0x40011004;
        public static unsafe uint* USART1_BRR => (uint*)0x40011008;
        public static unsafe uint* USART1_CR1 => (uint*)0x4001100C;
        public static unsafe uint* USART1_CR2 => (uint*)0x40011010;
        public static unsafe uint* USART1_CR3 => (uint*)0x40011014;
        public static unsafe uint* USART1_GTPR => (uint*)0x40011018;

        // USART2
        public const int USART2_BASE = 0x40004400;
        public static unsafe uint* USART2_SR => (uint*)0x40004400;
        public static unsafe uint* USART2_DR => (uint*)0x40004404;
        public static unsafe uint* USART2_BRR => (uint*)0x40004408;
        public static unsafe uint* USART2_CR1 => (uint*)0x4000440C;
        public static unsafe uint* USART2_CR2 => (uint*)0x40004410;
        public static unsafe uint* USART2_CR3 => (uint*)0x40004414;

        // USART3
        public const int USART3_BASE = 0x40004800;
        public static unsafe uint* USART3_SR => (uint*)0x40004800;
        public static unsafe uint* USART3_DR => (uint*)0x40004804;
        public static unsafe uint* USART3_BRR => (uint*)0x40004808;
        public static unsafe uint* USART3_CR1 => (uint*)0x4000480C;
        public static unsafe uint* USART3_CR2 => (uint*)0x40004810;
        public static unsafe uint* USART3_CR3 => (uint*)0x40004814;

        // SPI1
        public const int SPI1_BASE = 0x40013000;
        public static unsafe uint* SPI1_CR1 => (uint*)0x40013000;
        public static unsafe uint* SPI1_CR2 => (uint*)0x40013004;
        public static unsafe uint* SPI1_SR => (uint*)0x40013008;
        public static unsafe uint* SPI1_DR => (uint*)0x4001300C;
        public static unsafe uint* SPI1_CRCPR => (uint*)0x40013010;
        public static unsafe uint* SPI1_RXCRCR => (uint*)0x40013014;
        public static unsafe uint* SPI1_TXCRCR => (uint*)0x40013018;
        public static unsafe uint* SPI1_I2SCFGR => (uint*)0x4001301C;

        // SPI2
        public const int SPI2_BASE = 0x40003800;
        public static unsafe uint* SPI2_CR1 => (uint*)0x40003800;
        public static unsafe uint* SPI2_CR2 => (uint*)0x40003804;
        public static unsafe uint* SPI2_SR => (uint*)0x40003808;
        public static unsafe uint* SPI2_DR => (uint*)0x4000380C;
        public static unsafe uint* SPI2_CRCPR => (uint*)0x40003810;
        public static unsafe uint* SPI2_RXCRCR => (uint*)0x40003814;

        // SPI3
        public const int SPI3_BASE = 0x40003C00;
        public static unsafe uint* SPI3_CR1 => (uint*)0x40003C00;
        public static unsafe uint* SPI3_CR2 => (uint*)0x40003C04;
        public static unsafe uint* SPI3_SR => (uint*)0x40003C08;
        public static unsafe uint* SPI3_DR => (uint*)0x40003C0C;

        // I2C1
        public const int I2C1_BASE = 0x40005400;
        public static unsafe uint* I2C1_CR1 => (uint*)0x40005400;
        public static unsafe uint* I2C1_CR2 => (uint*)0x40005404;
        public static unsafe uint* I2C1_OAR1 => (uint*)0x40005408;
        public static unsafe uint* I2C1_OAR2 => (uint*)0x4000540C;
        public static unsafe uint* I2C1_DR => (uint*)0x40005410;
        public static unsafe uint* I2C1_SR1 => (uint*)0x40005414;
        public static unsafe uint* I2C1_SR2 => (uint*)0x40005418;
        public static unsafe uint* I2C1_CCR => (uint*)0x4000541C;
        public static unsafe uint* I2C1_TRISE => (uint*)0x40005420;
        public static unsafe uint* I2C1_FLTR => (uint*)0x40005424;

        // I2C2
        public const int I2C2_BASE = 0x40005800;
        public static unsafe uint* I2C2_CR1 => (uint*)0x40005800;
        public static unsafe uint* I2C2_CR2 => (uint*)0x40005804;
        public static unsafe uint* I2C2_OAR1 => (uint*)0x40005808;
        public static unsafe uint* I2C2_OAR2 => (uint*)0x4000580C;
        public static unsafe uint* I2C2_DR => (uint*)0x40005810;
        public static unsafe uint* I2C2_SR1 => (uint*)0x40005814;
        public static unsafe uint* I2C2_SR2 => (uint*)0x40005818;
        public static unsafe uint* I2C2_CCR => (uint*)0x4000581C;
        public static unsafe uint* I2C2_TRISE => (uint*)0x40005820;

        // I2C3
        public const int I2C3_BASE = 0x40005C00;
        public static unsafe uint* I2C3_CR1 => (uint*)0x40005C00;
        public static unsafe uint* I2C3_CR2 => (uint*)0x40005C04;
        public static unsafe uint* I2C3_OAR1 => (uint*)0x40005C08;
        public static unsafe uint* I2C3_DR => (uint*)0x40005C10;
        public static unsafe uint* I2C3_SR1 => (uint*)0x40005C14;
        public static unsafe uint* I2C3_SR2 => (uint*)0x40005C18;
        public static unsafe uint* I2C3_CCR => (uint*)0x40005C1C;

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
        public static unsafe uint* TIM1_DCR => (uint*)0x40012C48;
        public static unsafe uint* TIM1_DMAR => (uint*)0x40012C4C;

        // General Purpose Timer 2
        public const int TIM2_BASE = 0x40000000;
        public static unsafe uint* TIM2_CR1 => (uint*)0x40000000;
        public static unsafe uint* TIM2_CR2 => (uint*)0x40000004;
        public static unsafe uint* TIM2_SMCR => (uint*)0x40000008;
        public static unsafe uint* TIM2_DIER => (uint*)0x4000000C;
        public static unsafe uint* TIM2_SR => (uint*)0x40000010;
        public static unsafe uint* TIM2_EGR => (uint*)0x40000014;
        public static unsafe uint* TIM2_CCMR1 => (uint*)0x40000018;
        public static unsafe uint* TIM2_CCMR2 => (uint*)0x4000001C;
        public static unsafe uint* TIM2_CCER => (uint*)0x40000020;
        public static unsafe uint* TIM2_CNT => (uint*)0x40000024;
        public static unsafe uint* TIM2_PSC => (uint*)0x40000028;
        public static unsafe uint* TIM2_ARR => (uint*)0x4000002C;
        public static unsafe uint* TIM2_CCR1 => (uint*)0x40000034;
        public static unsafe uint* TIM2_CCR2 => (uint*)0x40000038;
        public static unsafe uint* TIM2_CCR3 => (uint*)0x4000003C;
        public static unsafe uint* TIM2_CCR4 => (uint*)0x40000040;

        // General Purpose Timer 3
        public const int TIM3_BASE = 0x40000400;
        public static unsafe uint* TIM3_CR1 => (uint*)0x40000400;
        public static unsafe uint* TIM3_SR => (uint*)0x40000410;
        public static unsafe uint* TIM3_CCMR1 => (uint*)0x40000418;
        public static unsafe uint* TIM3_CCER => (uint*)0x40000420;
        public static unsafe uint* TIM3_CNT => (uint*)0x40000424;
        public static unsafe uint* TIM3_PSC => (uint*)0x40000428;
        public static unsafe uint* TIM3_ARR => (uint*)0x4000042C;
        public static unsafe uint* TIM3_CCR1 => (uint*)0x40000434;
        public static unsafe uint* TIM3_CCR2 => (uint*)0x40000438;
        public static unsafe uint* TIM3_CCR3 => (uint*)0x4000043C;
        public static unsafe uint* TIM3_CCR4 => (uint*)0x40000440;

        // General Purpose Timer 4
        public const int TIM4_BASE = 0x40000800;
        public static unsafe uint* TIM4_CR1 => (uint*)0x40000800;
        public static unsafe uint* TIM4_SR => (uint*)0x40000810;
        public static unsafe uint* TIM4_CCMR1 => (uint*)0x40000818;
        public static unsafe uint* TIM4_CCER => (uint*)0x40000820;
        public static unsafe uint* TIM4_CNT => (uint*)0x40000824;
        public static unsafe uint* TIM4_PSC => (uint*)0x40000828;
        public static unsafe uint* TIM4_ARR => (uint*)0x4000082C;
        public static unsafe uint* TIM4_CCR1 => (uint*)0x40000834;
        public static unsafe uint* TIM4_CCR2 => (uint*)0x40000838;
        public static unsafe uint* TIM4_CCR3 => (uint*)0x4000083C;
        public static unsafe uint* TIM4_CCR4 => (uint*)0x40000840;

        // General Purpose Timer 5
        public const int TIM5_BASE = 0x40000C00;
        public static unsafe uint* TIM5_CR1 => (uint*)0x40000C00;
        public static unsafe uint* TIM5_SR => (uint*)0x40000C10;
        public static unsafe uint* TIM5_CNT => (uint*)0x40000C24;
        public static unsafe uint* TIM5_PSC => (uint*)0x40000C28;
        public static unsafe uint* TIM5_ARR => (uint*)0x40000C2C;
        public static unsafe uint* TIM5_CCR1 => (uint*)0x40000C34;
        public static unsafe uint* TIM5_CCR2 => (uint*)0x40000C38;
        public static unsafe uint* TIM5_CCR3 => (uint*)0x40000C3C;
        public static unsafe uint* TIM5_CCR4 => (uint*)0x40000C40;

        // General Purpose Timer 9
        public const int TIM9_BASE = 0x40014C00;
        public static unsafe uint* TIM9_CR1 => (uint*)0x40014C00;
        public static unsafe uint* TIM9_SMCR => (uint*)0x40014C08;
        public static unsafe uint* TIM9_DIER => (uint*)0x40014C0C;
        public static unsafe uint* TIM9_SR => (uint*)0x40014C10;
        public static unsafe uint* TIM9_EGR => (uint*)0x40014C14;
        public static unsafe uint* TIM9_CCMR1 => (uint*)0x40014C18;
        public static unsafe uint* TIM9_CCER => (uint*)0x40014C20;
        public static unsafe uint* TIM9_CNT => (uint*)0x40014C24;
        public static unsafe uint* TIM9_PSC => (uint*)0x40014C28;
        public static unsafe uint* TIM9_ARR => (uint*)0x40014C2C;
        public static unsafe uint* TIM9_CCR1 => (uint*)0x40014C34;
        public static unsafe uint* TIM9_CCR2 => (uint*)0x40014C38;

        // General Purpose Timer 10
        public const int TIM10_BASE = 0x40015000;
        public static unsafe uint* TIM10_CR1 => (uint*)0x40015000;
        public static unsafe uint* TIM10_DIER => (uint*)0x4001500C;
        public static unsafe uint* TIM10_SR => (uint*)0x40015010;
        public static unsafe uint* TIM10_EGR => (uint*)0x40015014;
        public static unsafe uint* TIM10_CCMR1 => (uint*)0x40015018;
        public static unsafe uint* TIM10_CCER => (uint*)0x40015020;
        public static unsafe uint* TIM10_CNT => (uint*)0x40015024;
        public static unsafe uint* TIM10_PSC => (uint*)0x40015028;
        public static unsafe uint* TIM10_ARR => (uint*)0x4001502C;
        public static unsafe uint* TIM10_CCR1 => (uint*)0x40015034;

        // ADC1
        public const int ADC1_BASE = 0x40012000;
        public static unsafe uint* ADC1_SR => (uint*)0x40012000;
        public static unsafe uint* ADC1_CR1 => (uint*)0x40012004;
        public static unsafe uint* ADC1_CR2 => (uint*)0x40012008;
        public static unsafe uint* ADC1_SMPR1 => (uint*)0x4001200C;
        public static unsafe uint* ADC1_SMPR2 => (uint*)0x40012010;
        public static unsafe uint* ADC1_JOFR1 => (uint*)0x40012014;
        public static unsafe uint* ADC1_JOFR2 => (uint*)0x40012018;
        public static unsafe uint* ADC1_JOFR3 => (uint*)0x4001201C;
        public static unsafe uint* ADC1_JOFR4 => (uint*)0x40012020;
        public static unsafe uint* ADC1_HTR => (uint*)0x40012024;
        public static unsafe uint* ADC1_LTR => (uint*)0x40012028;
        public static unsafe uint* ADC1_SQR1 => (uint*)0x4001202C;
        public static unsafe uint* ADC1_SQR2 => (uint*)0x40012030;
        public static unsafe uint* ADC1_SQR3 => (uint*)0x40012034;
        public static unsafe uint* ADC1_JSQR => (uint*)0x4001203C;
        public static unsafe uint* ADC1_JDR1 => (uint*)0x40012040;
        public static unsafe uint* ADC1_JDR2 => (uint*)0x40012044;
        public static unsafe uint* ADC1_JDR3 => (uint*)0x40012048;
        public static unsafe uint* ADC1_JDR4 => (uint*)0x4001204C;
        public static unsafe uint* ADC1_DR => (uint*)0x40012050;

        // ADC2
        public const int ADC2_BASE = 0x40012100;
        public static unsafe uint* ADC2_SR => (uint*)0x40012100;
        public static unsafe uint* ADC2_CR1 => (uint*)0x40012104;
        public static unsafe uint* ADC2_CR2 => (uint*)0x40012108;
        public static unsafe uint* ADC2_SMPR1 => (uint*)0x4001210C;
        public static unsafe uint* ADC2_SMPR2 => (uint*)0x40012110;
        public static unsafe uint* ADC2_SQR1 => (uint*)0x4001212C;
        public static unsafe uint* ADC2_DR => (uint*)0x40012150;

        // ADC3
        public const int ADC3_BASE = 0x40012200;
        public static unsafe uint* ADC3_SR => (uint*)0x40012200;
        public static unsafe uint* ADC3_CR1 => (uint*)0x40012204;
        public static unsafe uint* ADC3_CR2 => (uint*)0x40012208;
        public static unsafe uint* ADC3_SMPR1 => (uint*)0x4001220C;
        public static unsafe uint* ADC3_SMPR2 => (uint*)0x40012210;
        public static unsafe uint* ADC3_SQR1 => (uint*)0x4001222C;
        public static unsafe uint* ADC3_DR => (uint*)0x40012250;

        // System Configuration Controller
        public const int SYSCFG_BASE = 0x40013800;
        public static unsafe uint* SYSCFG_CFGR => (uint*)0x40013800;
        public static unsafe uint* SYSCFG_EXTICR1 => (uint*)0x40013808;
        public static unsafe uint* SYSCFG_EXTICR2 => (uint*)0x4001380C;
        public static unsafe uint* SYSCFG_EXTICR3 => (uint*)0x40013810;
        public static unsafe uint* SYSCFG_EXTICR4 => (uint*)0x40013814;
        public static unsafe uint* SYSCFG_CBR => (uint*)0x4001381C;

        // External Interrupt/Event Controller
        public const int EXTI_BASE = 0x40013C00;
        public static unsafe uint* EXTI_IMR => (uint*)0x40013C00;
        public static unsafe uint* EXTI_EMR => (uint*)0x40013C04;
        public static unsafe uint* EXTI_RTSR => (uint*)0x40013C08;
        public static unsafe uint* EXTI_FTSR => (uint*)0x40013C0C;
        public static unsafe uint* EXTI_SWIER => (uint*)0x40013C10;
        public static unsafe uint* EXTI_PR => (uint*)0x40013C14;

        // Random Number Generator
        public const int RNG_BASE = 0x50060800;
        public static unsafe uint* RNG_CR => (uint*)0x50060800;
        public static unsafe uint* RNG_SR => (uint*)0x50060804;
        public static unsafe uint* RNG_DR => (uint*)0x50060808;

        // CRYP Accelerator
        public const int CRYP_BASE = 0x50060000;
        public static unsafe uint* CRYP_CR => (uint*)0x50060000;
        public static unsafe uint* CRYP_SR => (uint*)0x50060004;
        public static unsafe uint* CRYP_DIN => (uint*)0x50060008;
        public static unsafe uint* CRYP_DOUT => (uint*)0x5006000C;
        public static unsafe uint* CRYP_DMACR => (uint*)0x50060010;
        public static unsafe uint* CRYP_IMSCR => (uint*)0x50060014;
        public static unsafe uint* CRYP_RISR => (uint*)0x50060018;
        public static unsafe uint* CRYP_MISR => (uint*)0x5006001C;
        public static unsafe uint* CRYP_K0LR => (uint*)0x50060020;
        public static unsafe uint* CRYP_K0RR => (uint*)0x50060024;
        public static unsafe uint* CRYP_K1LR => (uint*)0x50060028;
        public static unsafe uint* CRYP_K1RR => (uint*)0x5006002C;
        public static unsafe uint* CRYP_K2LR => (uint*)0x50060030;
        public static unsafe uint* CRYP_K2RR => (uint*)0x50060034;
        public static unsafe uint* CRYP_K3LR => (uint*)0x50060038;
        public static unsafe uint* CRYP_K3RR => (uint*)0x5006003C;
        public static unsafe uint* CRYP_IV0LR => (uint*)0x50060040;
        public static unsafe uint* CRYP_IV0RR => (uint*)0x50060044;
        public static unsafe uint* CRYP_IV1LR => (uint*)0x50060048;
        public static unsafe uint* CRYP_IV1RR => (uint*)0x5006004C;

        // HASH Accelerator
        public const int HASH_BASE = 0x50060400;
        public static unsafe uint* HASH_CR => (uint*)0x50060400;
        public static unsafe uint* HASH_DIN => (uint*)0x50060404;
        public static unsafe uint* HASH_DINSTAT => (uint*)0x50060408;
        public static unsafe uint* HASH_HR => (uint*)0x5006040C;
        public static unsafe uint* HASH_IMR => (uint*)0x50060420;
        public static unsafe uint* HASH_SR => (uint*)0x50060424;

        // Digital Camera Interface
        public const int DCMI_BASE = 0x50050000;
        public static unsafe uint* DCMI_CR => (uint*)0x50050000;
        public static unsafe uint* DCMI_SR => (uint*)0x50050004;
        public static unsafe uint* DCMI_RISR => (uint*)0x50050008;
        public static unsafe uint* DCMI_IER => (uint*)0x5005000C;
        public static unsafe uint* DCMI_MISR => (uint*)0x50050010;
        public static unsafe uint* DCMI_ICR => (uint*)0x50050014;
        public static unsafe uint* DCMI_MFISH => (uint*)0x5005001C;
        public static unsafe uint* DCMI_CWSTRT => (uint*)0x50050020;
        public static unsafe uint* DCMI_CWSIZE => (uint*)0x50050024;
        public static unsafe uint* DCMI_DR => (uint*)0x50050028;
        public static unsafe uint* DCMI_OR => (uint*)0x5005002C;

        // USB OTG High Speed
        public const int USB_OTG_HS_BASE = 0x40040000;
        public static unsafe uint* USB_OTG_HS_GOTGCTL => (uint*)0x40040000;
        public static unsafe uint* USB_OTG_HS_GOTGINT => (uint*)0x40040004;
        public static unsafe uint* USB_OTG_HS_GINTMSK => (uint*)0x40040008;
        public static unsafe uint* USB_OTG_HS_GRSTCTL => (uint*)0x4004000C;
        public static unsafe uint* USB_OTG_HS_GINTSTS => (uint*)0x40040010;
        public static unsafe uint* USB_OTG_HS_GRXSTSR => (uint*)0x40040014;
        public static unsafe uint* USB_OTG_HS_GRXFSIZ => (uint*)0x40040024;
        public static unsafe uint* USB_OTG_HS_HNPTXFSIZ => (uint*)0x40040028;
        public static unsafe uint* USB_OTG_HS_HNPTXSTS => (uint*)0x4004002C;
        public static unsafe uint* USB_OTG_HS_GCCFG => (uint*)0x40040038;
        public static unsafe uint* USB_OTG_HS_CID => (uint*)0x4004003C;
        public static unsafe uint* USB_OTG_HS_HPTXFSIZ => (uint*)0x40040100;
        public static unsafe uint* USB_OTG_HS_DIEPTXF => (uint*)0x40040200;

        // Ethernet
        public const int ETH_BASE = 0x40028000;
        public static unsafe uint* ETH_MACCR => (uint*)0x40028000;
        public static unsafe uint* ETH_MACFFR => (uint*)0x40028004;
        public static unsafe uint* ETH_MACHTHR => (uint*)0x40028008;
        public static unsafe uint* ETH_MACHTLR => (uint*)0x4002800C;
        public static unsafe uint* ETH_MACMIIAR => (uint*)0x40028010;
        public static unsafe uint* ETH_MACMIIDR => (uint*)0x40028014;
        public static unsafe uint* ETH_MACCR => (uint*)0x40028018;
        public static unsafe uint* ETH_MACVLANTR => (uint*)0x4002801C;
        public static unsafe uint* ETH_MACRWUFFR => (uint*)0x40028028;
        public static unsafe uint* ETH_MACPMTCSR => (uint*)0x4002802C;
        public static unsafe uint* ETH_MACSR => (uint*)0x40028030;
        public static unsafe uint* ETH_MACIMR => (uint*)0x40028034;
        public static unsafe uint* ETH_MACA0HR => (uint*)0x40028040;
        public static unsafe uint* ETH_MACA0LR => (uint*)0x40028044;
        public static unsafe uint* ETH_MACA1HR => (uint*)0x40028048;
        public static unsafe uint* ETH_MACA1LR => (uint*)0x4002804C;
        public static unsafe uint* ETH_MMCCR => (uint*)0x40028100;
        public static unsafe uint* ETH_MMCRIR => (uint*)0x40028104;
        public static unsafe uint* ETH_MMCTIR => (uint*)0x40028108;
        public static unsafe uint* ETH_MMCRIMR => (uint*)0x4002810C;
        public static unsafe uint* ETH_MMCTIMR => (uint*)0x40028110;
        public static unsafe uint* ETH_MMCTGBSCCR => (uint*)0x40028114;
        public static unsafe uint* ETH_MMCRGUFCCR => (uint*)0x40028118;
        public static unsafe uint* ETH_PTPTSCR => (uint*)0x40028700;
        public static unsafe uint* ETH_PTPSSIR => (uint*)0x40028704;
        public static unsafe uint* ETH_PTPTSHR => (uint*)0x40028708;
        public static unsafe uint* ETH_PTPTSLR => (uint*)0x4002870C;
        public static unsafe uint* ETH_PTPTSHUR => (uint*)0x40028710;
        public static unsafe uint* ETH_PTPTSLUR => (uint*)0x40028714;
        public static unsafe uint* ETH_PTPTSAR => (uint*)0x40028718;
        public static unsafe uint* ETH_PTPTTHR => (uint*)0x4002871C;
        public static unsafe uint* ETH_PTPTTLR => (uint*)0x40028720;
        public static unsafe uint* ETH_PTPTSR => (uint*)0x40028728;
        public static unsafe uint* ETH_DMABMR => (uint*)0x40029000;
        public static unsafe uint* ETH_DMASR => (uint*)0x40029004;
        public static unsafe uint* ETH_DMAOMR => (uint*)0x40029008;
        public static unsafe uint* ETH_DMAIER => (uint*)0x4002900C;
        public static unsafe uint* ETH_DMAMFBOCR => (uint*)0x40029010;
        public static unsafe uint* ETH_DMACHTDR => (uint*)0x40029014;
        public static unsafe uint* ETH_DMACHRDR => (uint*)0x40029018;
        public static unsafe uint* ETH_DMACHTBAR => (uint*)0x4002901C;
        public static unsafe uint* ETH_DMACHRBAR => (uint*)0x40029020;

        // 中断向量定义
        public const int IRQ_WWDG = 0;  // Window WatchDog interrupt
        public const int IRQ_PVD = 1;  // PVD through EXTI line detection interrupt
        public const int IRQ_TAMPER = 2;  // Tamper interrupt
        public const int IRQ_RTC_WKUP = 3;  // RTC Wakeup interrupt
        public const int IRQ_FLASH = 4;  // FLASH global interrupt
        public const int IRQ_RCC = 5;  // RCC global interrupt
        public const int IRQ_EXTI0 = 6;  // EXTI Line0 interrupt
        public const int IRQ_EXTI1 = 7;  // EXTI Line1 interrupt
        public const int IRQ_EXTI2 = 8;  // EXTI Line2 interrupt
        public const int IRQ_EXTI3 = 9;  // EXTI Line3 interrupt
        public const int IRQ_EXTI4 = 10;  // EXTI Line4 interrupt
        public const int IRQ_DMA1_STREAM0 = 11;  // DMA1 Stream0 global interrupt
        public const int IRQ_DMA1_STREAM1 = 12;  // DMA1 Stream1 global interrupt
        public const int IRQ_DMA1_STREAM2 = 13;  // DMA1 Stream2 global interrupt
        public const int IRQ_DMA1_STREAM3 = 14;  // DMA1 Stream3 global interrupt
        public const int IRQ_DMA1_STREAM4 = 15;  // DMA1 Stream4 global interrupt
        public const int IRQ_DMA1_STREAM5 = 16;  // DMA1 Stream5 global interrupt
        public const int IRQ_DMA1_STREAM6 = 17;  // DMA1 Stream6 global interrupt
        public const int IRQ_ADC = 18;  // ADC1 global interrupt
        public const int IRQ_CAN1_TX = 19;  // CAN1 TX interrupts
        public const int IRQ_CAN1_RX0 = 20;  // CAN1 RX0 interrupts
        public const int IRQ_CAN1_RX1 = 21;  // CAN1 RX1 interrupt
        public const int IRQ_CAN1_SCE = 22;  // CAN1 SCE interrupt
        public const int IRQ_EXTI9_5 = 23;  // EXTI Line[9:5] interrupt
        public const int IRQ_TIM1_BRK_TIM9 = 24;  // TIM1 Break interrupt and TIM9 global interrupt
        public const int IRQ_TIM1_UP_TIM10 = 25;  // TIM1 Update interrupt and TIM10 global interrupt
        public const int IRQ_TIM1_TRG_COM_TIM11 = 26;  // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
        public const int IRQ_TIM1_CC = 27;  // TIM1 Capture Compare interrupt
        public const int IRQ_TIM2 = 28;  // TIM2 global interrupt
        public const int IRQ_TIM3 = 29;  // TIM3 global interrupt
        public const int IRQ_TIM4 = 30;  // TIM4 global interrupt
        public const int IRQ_I2C1_EV = 31;  // I2C1 Event interrupt
        public const int IRQ_I2C1_ER = 32;  // I2C1 Error interrupt
        public const int IRQ_I2C2_EV = 33;  // I2C2 Event interrupt
        public const int IRQ_I2C2_ER = 34;  // I2C2 Error interrupt
        public const int IRQ_SPI1 = 35;  // SPI1 global interrupt
        public const int IRQ_SPI2 = 36;  // SPI2 global interrupt
        public const int IRQ_USART1 = 37;  // USART1 global interrupt
        public const int IRQ_USART2 = 38;  // USART2 global interrupt
        public const int IRQ_USART3 = 39;  // USART3 global interrupt
        public const int IRQ_EXTI15_10 = 40;  // EXTI Line[15:10] interrupts
        public const int IRQ_RTC_ALARM = 41;  // RTC Alarm (A and B) through EXTI Line interrupt
        public const int IRQ_OTG_FS_WKUP = 42;  // USB OTG FS Wakeup through EXTI interrupt
        public const int IRQ_TIM8_BRK_TIM12 = 43;  // TIM8 Break interrupt and TIM12 global interrupt
        public const int IRQ_TIM8_UP_TIM13 = 44;  // TIM8 Update interrupt and TIM13 global interrupt
        public const int IRQ_TIM8_TRG_COM_TIM14 = 45;  // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
        public const int IRQ_TIM8_CC = 46;  // TIM8 Capture Compare interrupt
        public const int IRQ_SPI3 = 47;  // SPI3 global interrupt
        public const int IRQ_UART4 = 48;  // UART4 global interrupt
        public const int IRQ_UART5 = 49;  // UART5 global interrupt
        public const int IRQ_TIM6 = 50;  // TIM6 global interrupt
        public const int IRQ_TIM7 = 51;  // TIM7 global interrupt
        public const int IRQ_DMA2_STREAM0 = 52;  // DMA2 Stream0 global interrupt
        public const int IRQ_DMA2_STREAM1 = 53;  // DMA2 Stream1 global interrupt
        public const int IRQ_DMA2_STREAM2 = 54;  // DMA2 Stream2 global interrupt
        public const int IRQ_DMA2_STREAM3 = 55;  // DMA2 Stream3 global interrupt
        public const int IRQ_DMA2_STREAM4 = 56;  // DMA2 Stream4 global interrupt
        public const int IRQ_ETH = 57;  // Ethernet global interrupt
        public const int IRQ_ETH_WKUP = 58;  // Ethernet Wakeup through EXTI interrupt
        public const int IRQ_CAN2_TX = 59;  // CAN2 TX interrupts
        public const int IRQ_CAN2_RX0 = 60;  // CAN2 RX0 interrupts
        public const int IRQ_CAN2_RX1 = 61;  // CAN2 RX1 interrupt
        public const int IRQ_CAN2_SCE = 62;  // CAN2 SCE interrupt
        public const int IRQ_NA = 63;  // Not Available
        public const int IRQ_OTG_FS = 64;  // USB OTG FS global interrupt
        public const int IRQ_DCMI = 65;  // DCMI global interrupt
        public const int IRQ_CRYP = 66;  // CRYP global interrupt
        public const int IRQ_HASH_RNG = 67;  // HASH and RNG global interrupt
        public const int IRQ_FPU = 68;  // FPU global interrupt

        // 引脚定义
        public const int PIN_PE2 = 1;  // Tristate - any function
        public const int PIN_PE3 = 2;  // Tristate - any function
        public const int PIN_PE4 = 3;  // Tristate - any function
        public const int PIN_PE5 = 4;  // Tristate - any function
        public const int PIN_PE6 = 5;  // Tristate - any function
        public const int PIN_VCAP1 = 6;  // 1.2V voltage supply
        public const int PIN_VBAT = 7;  // Battery voltage supply
        public const int PIN_PC13 = 8;  // TAMPER-RTC
        public const int PIN_PC14 = 9;  // OSC32_IN
        public const int PIN_PC15 = 10;  // OSC32_OUT
        public const int PIN_PH0 = 11;  // OSC_IN
        public const int PIN_PH1 = 12;  // OSC_OUT
        public const int PIN_NRST = 13;  // External reset
        public const int PIN_VSSA = 14;  // Analog ground
        public const int PIN_VDDA = 15;  // Analog supply
        public const int PIN_PA0 = 16;  // WKUP/USART_CTS
        public const int PIN_PA1 = 17;  // USART_RTS
        public const int PIN_PA2 = 18;  // USART_TX
        public const int PIN_PA3 = 19;  // USART_RX
        public const int PIN_PA4 = 20;  // DAC1_OUT
        public const int PIN_PA5 = 21;  // DAC2_OUT
        public const int PIN_PA6 = 22;  // SPI1_MISO
        public const int PIN_PA7 = 23;  // SPI1_MOSI
        public const int PIN_PA8 = 24;  // MCO1
        public const int PIN_PA9 = 25;  // USART1_TX
        public const int PIN_PA10 = 26;  // USART1_RX
        public const int PIN_PA11 = 27;  // USB_DM
        public const int PIN_PA12 = 28;  // USB_DP
        public const int PIN_PA13 = 29;  // JTMS/SWDIO
        public const int PIN_VCAP2 = 30;  // 1.2V voltage supply
        public const int PIN_PA14 = 31;  // JTCK/SWCLK
        public const int PIN_PA15 = 32;  // JTDI
        public const int PIN_PB0 = 33;  // SPI1_CS/TIM3_CH3
        public const int PIN_PB1 = 34;  // TIM3_CH4
        public const int PIN_PB2 = 35;  // BOOT1
        public const int PIN_PB3 = 36;  // JTDO/TRACESWO
        public const int PIN_PB4 = 37;  // NJTRST
        public const int PIN_PB5 = 38;  // CAN2_RX/TIM3_CH2
        public const int PIN_PB6 = 39;  // USART1_TX
        public const int PIN_PB7 = 40;  // USART1_RX
        public const int PIN_PB8 = 41;  // I2C1_SCL/TIM4_CH1
        public const int PIN_PB9 = 42;  // I2C1_SDA/TIM4_CH2
        public const int PIN_PB10 = 43;  // I2C2_SCL/USART3_TX
        public const int PIN_PB11 = 44;  // I2C2_SDA/USART3_RX
        public const int PIN_PB12 = 45;  // SPI2_CS/I2C2_SMBA
        public const int PIN_PB13 = 46;  // SPI2_SCK
        public const int PIN_PB14 = 47;  // SPI2_MISO
        public const int PIN_PB15 = 48;  // SPI2_MOSI
        public const int PIN_PD0 = 49;  // FSMC_D2
        public const int PIN_PD1 = 50;  // FSMC_D3
        public const int PIN_PD2 = 51;  // TIM3_ETR/UART4_RX
        public const int PIN_PD3 = 52;  // FSMC_CLK/UART4_CTS
        public const int PIN_PD4 = 53;  // FSMC_NOE
        public const int PIN_PD5 = 54;  // FSMC_NWE
        public const int PIN_PD6 = 55;  // FSMC_NWAIT
        public const int PIN_PD7 = 56;  // FSMC_NE1/FSMC_NCE2
        public const int PIN_PD8 = 57;  // FSMC_D13/USART3_TX
        public const int PIN_PD9 = 58;  // FSMC_D14/USART3_RX
        public const int PIN_PD10 = 59;  // FSMC_D15/USART3_CK
        public const int PIN_PD11 = 60;  // FSMC_A16/USART3_CTS
        public const int PIN_PD12 = 61;  // FSMC_A17/TIM4_CH1
        public const int PIN_PD13 = 62;  // FSMC_A18/TIM4_CH2
        public const int PIN_PD14 = 63;  // FSMC_D0/TIM4_CH3
        public const int PIN_PD15 = 64;  // FSMC_D1/TIM4_CH4
        public const int PIN_PG0 = 65;  // FSMC_A10/TIM4_ETR
        public const int PIN_PG1 = 66;  // FSMC_A11
        public const int PIN_PE0 = 67;  // FSMC_NBL0/TIM4_CH3
        public const int PIN_PE1 = 68;  // FSMC_NBL1/TIM4_CH4
        public const int PIN_PE3 = 69;  // FSMC_A19
        public const int PIN_PE4 = 70;  // FSMC_A20
        public const int PIN_PE5 = 71;  // FSMC_A21
        public const int PIN_PE6 = 72;  // FSMC_A22/TIM4_CH1
        public const int PIN_PE7 = 73;  // FSMC_D4/USART6_RX
        public const int PIN_PE8 = 74;  // FSMC_D5/USART6_TX
        public const int PIN_PE9 = 75;  // FSMC_D6/TIM4_CH1
        public const int PIN_PE10 = 76;  // FSMC_D7/USART6_RTS
        public const int PIN_PE11 = 77;  // FSMC_D8
        public const int PIN_PE12 = 78;  // FSMC_D9
        public const int PIN_PE13 = 79;  // FSMC_D10
        public const int PIN_PE14 = 80;  // FSMC_D11
        public const int PIN_PE15 = 81;  // FSMC_D12
        public const int PIN_PB10 = 82;  // I2C2_SCL/USART3_TX
        public const int PIN_PB11 = 83;  // I2C2_SDA/USART3_RX
        public const int PIN_PB12 = 84;  // SPI2_CS/I2C2_SMBA
        public const int PIN_PB13 = 85;  // SPI2_SCK
        public const int PIN_PB14 = 86;  // SPI2_MISO
        public const int PIN_PB15 = 87;  // SPI2_MOSI
        public const int PIN_PD8 = 88;  // FSMC_D13/USART3_TX
        public const int PIN_PD9 = 89;  // FSMC_D14/USART3_RX
        public const int PIN_PD10 = 90;  // FSMC_D15/USART3_CK
        public const int PIN_PD11 = 91;  // FSMC_A16/USART3_CTS
        public const int PIN_PD12 = 92;  // FSMC_A17/TIM4_CH1
        public const int PIN_PD13 = 93;  // FSMC_A18/TIM4_CH2
        public const int PIN_PD14 = 94;  // FSMC_D0/TIM4_CH3
        public const int PIN_PD15 = 95;  // FSMC_D1/TIM4_CH4
        public const int PIN_PG2 = 96;  // FSMC_A12
        public const int PIN_PG3 = 97;  // FSMC_A13
        public const int PIN_PG4 = 98;  // FSMC_A14
        public const int PIN_PG5 = 99;  // FSMC_A15
        public const int PIN_PG6 = 100;  // FSMC_INT2
        public const int PIN_PG7 = 101;  // FSMC_INT3
        public const int PIN_PG8 = 102;  // FSMC_INT4
        public const int PIN_PG9 = 103;  // FSMC_NE2/FSMC_NCE3
        public const int PIN_PG10 = 104;  // FSMC_NCE4_1
        public const int PIN_PG11 = 105;  // FSMC_NCE4_2
        public const int PIN_PG12 = 106;  // FSMC_NCE4_3
        public const int PIN_PG13 = 107;  // FSMC_A24
        public const int PIN_PG14 = 108;  // FSMC_A25
        public const int PIN_PG15 = 109;  // FSMC_INT2
        public const int PIN_VSS = 110;  // Ground
        public const int PIN_VDD = 111;  // 3.3V Supply

        public static void stm32f407vgt6_init()
        {
            // 硬件初始化代码
        }
    }
}
