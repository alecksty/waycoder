using System;

namespace VML.Device.TexasInstruments.TM4C123GH6PM
{
    /// <summary>
    /// TM4C123GH6PM 寄存器定义
    /// 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
    /// 版本: 1.0
    /// </summary>
    public static class TM4C123GH6PM
    {
        // CPU架构: ARM-Cortex-M4F, 32位, 80000000 Hz

        // 外设定义
        // UART 0
        public const int UART0_BASE = 0x4000C000;
        public static unsafe uint* UART0_DR => (uint*)0x4000C000;
        public static unsafe uint* UART0_FR => (uint*)0x4000C018;
        public static unsafe uint* UART0_IBRD => (uint*)0x4000C024;
        public static unsafe uint* UART0_FBRD => (uint*)0x4000C028;
        public static unsafe uint* UART0_LCRH => (uint*)0x4000C02C;
        public static unsafe uint* UART0_CTL => (uint*)0x4000C030;
        public static unsafe uint* UART0_IM => (uint*)0x4000C038;
        public static unsafe uint* UART0_RIS => (uint*)0x4000C03C;
        public static unsafe uint* UART0_ICR => (uint*)0x4000C044;

        // UART 1
        public const int UART1_BASE = 0x4000D000;
        public static unsafe uint* UART1_DR => (uint*)0x4000D000;
        public static unsafe uint* UART1_FR => (uint*)0x4000D018;
        public static unsafe uint* UART1_IBRD => (uint*)0x4000D024;
        public static unsafe uint* UART1_FBRD => (uint*)0x4000D028;
        public static unsafe uint* UART1_LCRH => (uint*)0x4000D02C;
        public static unsafe uint* UART1_CTL => (uint*)0x4000D030;

        // GPIO Port A
        public const int GPIOA_BASE = 0x40004000;
        public static unsafe uint* GPIOA_DATA => (uint*)0x400043FC;
        public static unsafe uint* GPIOA_DIR => (uint*)0x40004400;
        public static unsafe uint* GPIOA_IS => (uint*)0x40004404;
        public static unsafe uint* GPIOA_IBE => (uint*)0x40004408;
        public static unsafe uint* GPIOA_IEV => (uint*)0x4000440C;
        public static unsafe uint* GPIOA_IM => (uint*)0x40004410;
        public static unsafe uint* GPIOA_RIS => (uint*)0x40004414;
        public static unsafe uint* GPIOA_MIS => (uint*)0x40004418;
        public static unsafe uint* GPIOA_ICR => (uint*)0x4000441C;
        public static unsafe uint* GPIOA_AFSEL => (uint*)0x40004420;
        public static unsafe uint* GPIOA_DEN => (uint*)0x4000451C;

        // 16/32-bit Timer 0
        public const int TIMER0_BASE = 0x40030000;
        public static unsafe uint* TIMER0_CFG => (uint*)0x40030000;
        public static unsafe uint* TIMER0_TAMR => (uint*)0x40030004;
        public static unsafe uint* TIMER0_CTL => (uint*)0x4003000C;
        public static unsafe uint* TIMER0_ILR => (uint*)0x40030028;
        public static unsafe uint* TIMER0_V => (uint*)0x40030038;
        public static unsafe uint* TIMER0_ICR => (uint*)0x40030024;

        // ADC 0
        public const int ADC0_BASE = 0x40038000;
        public static unsafe uint* ADC0_ACTSS => (uint*)0x40038000;
        public static unsafe uint* ADC0_EMUX => (uint*)0x40038014;
        public static unsafe uint* ADC0_SSMUX0 => (uint*)0x40038040;
        public static unsafe uint* ADC0_SSFIFO0 => (uint*)0x40038048;
        public static unsafe uint* ADC0_PROC => (uint*)0x40038030;

        public static void tm4c123gh6pm_init()
        {
            // 硬件初始化代码
        }
    }
}
