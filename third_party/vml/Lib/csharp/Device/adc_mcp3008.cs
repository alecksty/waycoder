using System;

namespace VML.Device.Microchip.MCP3008
{
    /// <summary>
    /// MCP3008 寄存器定义
    /// 生成自: Microchip/ADC/MCP3008
    /// 版本: 1.0
    /// </summary>
    public static class MCP3008
    {
        // CPU架构: ADC, 10位, 1350000 Hz

        // 外设定义
        // MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)
        public const int MCP3008_BASE = 0x00;
        public static unsafe ushort* MCP3008_CH0 => (ushort*)0x00000000;
        public static unsafe ushort* MCP3008_CH1 => (ushort*)0x00000001;
        public static unsafe ushort* MCP3008_CH2 => (ushort*)0x00000002;
        public static unsafe ushort* MCP3008_CH3 => (ushort*)0x00000003;
        public static unsafe ushort* MCP3008_CH4 => (ushort*)0x00000004;
        public static unsafe ushort* MCP3008_CH5 => (ushort*)0x00000005;
        public static unsafe ushort* MCP3008_CH6 => (ushort*)0x00000006;
        public static unsafe ushort* MCP3008_CH7 => (ushort*)0x00000007;
        public static unsafe ushort* MCP3008_DIFF_01 => (ushort*)0x00000008;
        public static unsafe ushort* MCP3008_DIFF_23 => (ushort*)0x00000009;

        public static void mcp3008_init()
        {
            // 硬件初始化代码
        }
    }
}
