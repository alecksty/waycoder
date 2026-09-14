using System;

namespace VML.Device.Allwinner.Allwinner H3
{
    /// <summary>
    /// Allwinner H3 寄存器定义
    /// 生成自: Allwinner/H-Series/Allwinner H3
    /// 版本: 1.0
    /// </summary>
    public static class Allwinner H3
    {
        // CPU架构: ARM-Cortex-A7, 32位, 1200000000 Hz

        // 外设定义
        // UART 0 (debug console)
        public const int UART0_BASE = 0x01C28000;
        public static unsafe uint* UART0_RBR => (uint*)0x01C28000;
        public static unsafe uint* UART0_THR => (uint*)0x01C28000;
        public static unsafe uint* UART0_IER => (uint*)0x01C28004;
        public static unsafe uint* UART0_IIR => (uint*)0x01C28008;
        public static unsafe uint* UART0_FCR => (uint*)0x01C28008;
        public static unsafe uint* UART0_LCR => (uint*)0x01C2800C;
        public static unsafe uint* UART0_MCR => (uint*)0x01C28010;
        public static unsafe uint* UART0_LSR => (uint*)0x01C28014;
        public static unsafe uint* UART0_MSR => (uint*)0x01C28018;
        public static unsafe uint* UART0_DLL => (uint*)0x01C28000;
        public static unsafe uint* UART0_DLH => (uint*)0x01C28004;

        // UART 1
        public const int UART1_BASE = 0x01C28400;
        public static unsafe uint* UART1_RBR => (uint*)0x01C28400;
        public static unsafe uint* UART1_THR => (uint*)0x01C28400;
        public static unsafe uint* UART1_LSR => (uint*)0x01C28414;

        // GPIO 控制器
        public const int GPIO_BASE = 0x01C20800;
        public static unsafe uint* GPIO_PA_CFG0 => (uint*)0x01C20800;
        public static unsafe uint* GPIO_PA_CFG1 => (uint*)0x01C20804;
        public static unsafe uint* GPIO_PA_DAT => (uint*)0x01C20810;
        public static unsafe uint* GPIO_PA_DRV0 => (uint*)0x01C20814;
        public static unsafe uint* GPIO_PA_PUL0 => (uint*)0x01C2081C;
        public static unsafe uint* GPIO_PB_CFG0 => (uint*)0x01C20824;
        public static unsafe uint* GPIO_PB_DAT => (uint*)0x01C20834;
        public static unsafe uint* GPIO_PC_CFG0 => (uint*)0x01C20848;
        public static unsafe uint* GPIO_PC_DAT => (uint*)0x01C20858;

        // AVS 定时器
        public const int TIMER_BASE = 0x01C20C00;
        public static unsafe uint* TIMER_CNT0 => (uint*)0x01C20C00;
        public static unsafe uint* TIMER_CNT1 => (uint*)0x01C20C04;
        public static unsafe uint* TIMER_CTRL => (uint*)0x01C20C08;
        public static unsafe uint* TIMER_INTV => (uint*)0x01C20C0C;

        // 时钟控制单元
        public const int CCU_BASE = 0x01C20000;
        public static unsafe uint* CCU_PLL1_CFG => (uint*)0x01C20000;
        public static unsafe uint* CCU_PLL3_CFG => (uint*)0x01C20010;
        public static unsafe uint* CCU_CPU_AXI_CFG => (uint*)0x01C20050;
        public static unsafe uint* CCU_AHB1_APB1_CFG => (uint*)0x01C20054;
        public static unsafe uint* CCU_APB2_CFG => (uint*)0x01C20058;
        public static unsafe uint* CCU_BUS_GATE0 => (uint*)0x01C20060;
        public static unsafe uint* CCU_BUS_GATE1 => (uint*)0x01C20064;
        public static unsafe uint* CCU_BUS_GATE2 => (uint*)0x01C20068;

        public static void allwinner_h3_init()
        {
            // 硬件初始化代码
        }
    }
}
