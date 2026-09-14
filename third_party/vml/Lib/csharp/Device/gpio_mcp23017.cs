using System;

namespace VML.Device.Microchip.MCP23017
{
    /// <summary>
    /// MCP23017 寄存器定义
    /// 生成自: Microchip/GPIO/MCP23017
    /// 版本: 1.0
    /// </summary>
    public static class MCP23017
    {
        // CPU架构: GPIO, 16位, 400000 Hz

        // 外设定义
        // MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
        public const int MCP23017_BASE = 0x20;
        public static unsafe byte* MCP23017_IODIRA => (byte*)0x00000020;
        public static unsafe byte* MCP23017_IODIRB => (byte*)0x00000021;
        public static unsafe byte* MCP23017_GPIOA => (byte*)0x00000032;
        public static unsafe byte* MCP23017_GPIOB => (byte*)0x00000033;
        public static unsafe byte* MCP23017_GPINTENA => (byte*)0x00000024;
        public static unsafe byte* MCP23017_GPINTENB => (byte*)0x00000025;
        public static unsafe byte* MCP23017_INTCONA => (byte*)0x00000028;
        public static unsafe byte* MCP23017_IOCON => (byte*)0x0000002A;
        public static unsafe byte* MCP23017_GPPUA => (byte*)0x0000002C;
        public static unsafe byte* MCP23017_GPPUB => (byte*)0x0000002D;

        // 中断向量定义
        public const int IRQ_INTA = 0;  // Port A interrupt
        public const int IRQ_INTB = 1;  // Port B interrupt

        public static void mcp23017_init()
        {
            // 硬件初始化代码
        }
    }
}
