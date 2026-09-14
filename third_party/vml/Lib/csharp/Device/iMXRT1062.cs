using System;

namespace VML.Device.NXP.i.MX RT1062
{
    /// <summary>
    /// i.MX RT1062 寄存器定义
    /// 生成自: NXP/i.MX RT/i.MX RT1062
    /// 版本: 1.0
    /// </summary>
    public static class i.MX RT1062
    {
        // CPU架构: ARM-Cortex-M7, 32位, 528000000 Hz

        // 外设定义
        // LPUART 1
        public const int UART1_BASE = 0x40184000;
        public static unsafe uint* UART1_VERID => (uint*)0x40184000;
        public static unsafe uint* UART1_CTRL => (uint*)0x40184010;
        public static unsafe uint* UART1_STAT => (uint*)0x40184014;
        public static unsafe uint* UART1_DATA => (uint*)0x4018401C;
        public static unsafe uint* UART1_BAUD => (uint*)0x40184024;

        // LPUART 2
        public const int UART2_BASE = 0x40188000;
        public static unsafe uint* UART2_CTRL => (uint*)0x40188010;
        public static unsafe uint* UART2_STAT => (uint*)0x40188014;
        public static unsafe uint* UART2_DATA => (uint*)0x4018801C;
        public static unsafe uint* UART2_BAUD => (uint*)0x40188024;

        // GPIO 1
        public const int GPIO1_BASE = 0x401B8000;
        public static unsafe uint* GPIO1_DR => (uint*)0x401B8000;
        public static unsafe uint* GPIO1_GDIR => (uint*)0x401B8004;
        public static unsafe uint* GPIO1_PSR => (uint*)0x401B8008;
        public static unsafe uint* GPIO1_ICR1 => (uint*)0x401B800C;
        public static unsafe uint* GPIO1_ICR2 => (uint*)0x401B8010;
        public static unsafe uint* GPIO1_IMR => (uint*)0x401B8014;
        public static unsafe uint* GPIO1_ISR => (uint*)0x401B8018;
        public static unsafe uint* GPIO1_EDGE_SEL => (uint*)0x401B801C;

        // GPT 定时器 1
        public const int GPT1_BASE = 0x401EC000;
        public static unsafe uint* GPT1_CR => (uint*)0x401EC000;
        public static unsafe uint* GPT1_PR => (uint*)0x401EC004;
        public static unsafe uint* GPT1_SR => (uint*)0x401EC008;
        public static unsafe uint* GPT1_IR => (uint*)0x401EC00C;
        public static unsafe uint* GPT1_OCR1 => (uint*)0x401EC010;
        public static unsafe uint* GPT1_CNT => (uint*)0x401EC024;

        // USB OTG 1
        public const int USB1_BASE = 0x402E0000;
        public static unsafe uint* USB1_ID => (uint*)0x402E0000;
        public static unsafe uint* USB1_OTGSC => (uint*)0x402E000C;
        public static unsafe uint* USB1_USBCMD => (uint*)0x402E0100;
        public static unsafe uint* USB1_PORTSC1 => (uint*)0x402E0184;

        public static void i_mx_rt1062_init()
        {
            // 硬件初始化代码
        }
    }
}
