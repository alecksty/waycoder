using System;

namespace VML.Device.TexasInstruments.MSP432P401R
{
    /// <summary>
    /// MSP432P401R 寄存器定义
    /// 生成自: Texas Instruments/MSP432/MSP432P401R
    /// 版本: 1.0
    /// </summary>
    public static class MSP432P401R
    {
        // CPU架构: ARM-Cortex-M4F, 32位, 48000000 Hz

        // 外设定义
        // eUSCI_A0 UART
        public const int UART0_BASE = 0x40001000;
        public static unsafe ushort* UART0_CTLW0 => (ushort*)0x40001000;
        public static unsafe ushort* UART0_BRW => (ushort*)0x40001006;
        public static unsafe ushort* UART0_UCA0TXBUF => (ushort*)0x40001008;
        public static unsafe ushort* UART0_UCA0RXBUF => (ushort*)0x4000100A;
        public static unsafe ushort* UART0_IFG => (ushort*)0x4000100C;
        public static unsafe ushort* UART0_IE => (ushort*)0x4000100E;

        // eUSCI_A1 UART
        public const int UART1_BASE = 0x40002000;
        public static unsafe ushort* UART1_CTLW0 => (ushort*)0x40002000;
        public static unsafe ushort* UART1_BRW => (ushort*)0x40002006;
        public static unsafe ushort* UART1_TXBUF => (ushort*)0x40002008;
        public static unsafe ushort* UART1_RXBUF => (ushort*)0x4000200A;
        public static unsafe ushort* UART1_IFG => (ushort*)0x4000200C;
        public static unsafe ushort* UART1_IE => (ushort*)0x4000200E;

        // Timer_A0 16bit
        public const int TIMER0_BASE = 0x40003000;
        public static unsafe ushort* TIMER0_CTL => (ushort*)0x40003000;
        public static unsafe ushort* TIMER0_R => (ushort*)0x40003010;
        public static unsafe ushort* TIMER0_CCR0 => (ushort*)0x40003012;
        public static unsafe ushort* TIMER0_CCR1 => (ushort*)0x40003014;
        public static unsafe ushort* TIMER0_CCR2 => (ushort*)0x40003016;
        public static unsafe ushort* TIMER0_EX0 => (ushort*)0x40003020;

        // ADC14 14-bit
        public const int ADC14_BASE = 0x40006000;
        public static unsafe ushort* ADC14_CTL0 => (ushort*)0x40006000;
        public static unsafe ushort* ADC14_CTL1 => (ushort*)0x40006002;
        public static unsafe ushort* ADC14_LO => (ushort*)0x40006004;
        public static unsafe ushort* ADC14_HI => (ushort*)0x40006006;
        public static unsafe ushort* ADC14_MCTL0 => (ushort*)0x40006008;
        public static unsafe ushort* ADC14_MEM0 => (ushort*)0x40006020;

        public static void msp432p401r_init()
        {
            // 硬件初始化代码
        }
    }
}
