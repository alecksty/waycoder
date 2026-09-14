using System;

namespace VML.Device.STMicroelectronics.STM32F303CCT6
{
    /// <summary>
    /// STM32F303CCT6 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32F303CCT6
    /// 版本: 1.0
    /// </summary>
    public static class STM32F303CCT6
    {
        // CPU架构: ARM-Cortex-M4F, 32位, 72000000 Hz

        // 外设定义
        // USART 1
        public const int USART1_BASE = 0x40013800;
        public static unsafe uint* USART1_SR => (uint*)0x40013800;
        public static unsafe uint* USART1_DR => (uint*)0x40013804;
        public static unsafe uint* USART1_BRR => (uint*)0x40013808;
        public static unsafe uint* USART1_CR1 => (uint*)0x4001380C;
        public static unsafe uint* USART1_CR2 => (uint*)0x40013810;
        public static unsafe uint* USART1_CR3 => (uint*)0x40013814;

        // USART 2
        public const int USART2_BASE = 0x40004400;
        public static unsafe uint* USART2_SR => (uint*)0x40004400;
        public static unsafe uint* USART2_DR => (uint*)0x40004404;
        public static unsafe uint* USART2_BRR => (uint*)0x40004408;
        public static unsafe uint* USART2_CR1 => (uint*)0x4000440C;

        // USART 3
        public const int USART3_BASE = 0x40004800;
        public static unsafe uint* USART3_SR => (uint*)0x40004800;
        public static unsafe uint* USART3_DR => (uint*)0x40004804;
        public static unsafe uint* USART3_BRR => (uint*)0x40004808;
        public static unsafe uint* USART3_CR1 => (uint*)0x4000480C;

        // GPIO Port A
        public const int GPIOA_BASE = 0x48000000;
        public static unsafe uint* GPIOA_MODER => (uint*)0x48000000;
        public static unsafe uint* GPIOA_OTYPER => (uint*)0x48000004;
        public static unsafe uint* GPIOA_OSPEEDR => (uint*)0x48000008;
        public static unsafe uint* GPIOA_PUPDR => (uint*)0x4800000C;
        public static unsafe uint* GPIOA_IDR => (uint*)0x48000010;
        public static unsafe uint* GPIOA_ODR => (uint*)0x48000014;
        public static unsafe uint* GPIOA_BSRR => (uint*)0x48000018;
        public static unsafe uint* GPIOA_AFRL => (uint*)0x48000020;
        public static unsafe uint* GPIOA_AFRH => (uint*)0x48000024;

        // 高级定时器 1
        public const int TIM1_BASE = 0x40012C00;
        public static unsafe uint* TIM1_CR1 => (uint*)0x40012C00;
        public static unsafe uint* TIM1_CNT => (uint*)0x40012C24;
        public static unsafe uint* TIM1_PSC => (uint*)0x40012C28;
        public static unsafe uint* TIM1_ARR => (uint*)0x40012C2C;
        public static unsafe uint* TIM1_CCR1 => (uint*)0x40012C34;

        // ADC 1
        public const int ADC1_BASE = 0x50000000;
        public static unsafe uint* ADC1_SR => (uint*)0x50000000;
        public static unsafe uint* ADC1_CR => (uint*)0x50000008;
        public static unsafe uint* ADC1_CFGR => (uint*)0x5000000C;
        public static unsafe uint* ADC1_SMPR1 => (uint*)0x50000014;
        public static unsafe uint* ADC1_DR => (uint*)0x50000040;

        public static void stm32f303cct6_init()
        {
            // 硬件初始化代码
        }
    }
}
