using System;

namespace VML.Device.Cypress(Infineon).CY8C5888LTI_LP097
{
    /// <summary>
    /// CY8C5888LTI-LP097 寄存器定义
    /// 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
    /// 版本: 1.0
    /// </summary>
    public static class CY8C5888LTI_LP097
    {
        // CPU架构: ARM-Cortex-M3, 32位, 80000000 Hz

        // 外设定义
        // SCB UART (可编程)
        public const int UART_BASE = 0x40050000;
        public static unsafe uint* UART_CTRL => (uint*)0x40050000;
        public static unsafe uint* UART_STATUS => (uint*)0x40050004;
        public static unsafe uint* UART_TX_DATA => (uint*)0x40050008;
        public static unsafe uint* UART_RX_DATA => (uint*)0x4005000C;

        // SCB I2C
        public const int I2C_BASE = 0x40051000;
        public static unsafe uint* I2C_CTRL => (uint*)0x40051000;
        public static unsafe uint* I2C_STATUS => (uint*)0x40051004;
        public static unsafe uint* I2C_TX_DATA => (uint*)0x40051008;
        public static unsafe uint* I2C_RX_DATA => (uint*)0x4005100C;

        // TCPWM 定时器
        public const int TIMER_BASE = 0x40060000;
        public static unsafe uint* TIMER_CTRL => (uint*)0x40060000;
        public static unsafe uint* TIMER_STATUS => (uint*)0x40060004;
        public static unsafe uint* TIMER_CNT => (uint*)0x40060008;
        public static unsafe uint* TIMER_PERIOD => (uint*)0x4006000C;
        public static unsafe uint* TIMER_CC => (uint*)0x40060010;

        // DelSig ADC 20-bit
        public const int ADC_BASE = 0x40100000;
        public static unsafe uint* ADC_CTRL => (uint*)0x40100000;
        public static unsafe uint* ADC_STATUS => (uint*)0x40100004;
        public static unsafe uint* ADC_DATA => (uint*)0x40100008;
        public static unsafe uint* ADC_CLOCK => (uint*)0x40100010;

        // GPIO 端口
        public const int GPIO_BASE = 0x40040000;
        public static unsafe uint* GPIO_DR => (uint*)0x40040000;
        public static unsafe uint* GPIO_PS => (uint*)0x40040004;
        public static unsafe uint* GPIO_IE => (uint*)0x40040008;
        public static unsafe uint* GPIO_DM => (uint*)0x4004000C;

        // USB 控制器
        public const int USB_BASE = 0x40080000;
        public static unsafe uint* USB_CR0 => (uint*)0x40080000;
        public static unsafe uint* USB_CR1 => (uint*)0x40080004;
        public static unsafe uint* USB_STAT => (uint*)0x40080008;

        public static void cy8c5888lti_lp097_init()
        {
            // 硬件初始化代码
        }
    }
}
