using System;

namespace VML.Device.Microchip.MCP4921
{
    /// <summary>
    /// MCP4921 寄存器定义
    /// 生成自: Microchip/DAC/MCP4921
    /// 版本: 1.0
    /// </summary>
    public static class MCP4921
    {
        // CPU架构: DAC, 12位, 20000000 Hz

        // 外设定义
        // MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
        public const int MCP4921_BASE = 0x00;
        public static unsafe ushort* MCP4921_DAC_VALUE => (ushort*)0x00000000;
        public const int MCP4921_DAC_VALUE_BUF = 14;  // VREF buffer (0=unbuffered, 1=buffered)
        public const int MCP4921_DAC_VALUE_GA = 13;  // Gain (0=2x, 1=1x)
        public const int MCP4921_DAC_VALUE_SHDN = 12;  // Shutdown (0=shutdown, 1=active)
        public static unsafe ushort* MCP4921_VREF => (ushort*)0x00000002;

        public static void mcp4921_init()
        {
            // 硬件初始化代码
        }
    }
}
