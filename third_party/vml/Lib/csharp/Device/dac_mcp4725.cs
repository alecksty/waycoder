using System;

namespace VML.Device.Microchip.MCP4725
{
    /// <summary>
    /// MCP4725 寄存器定义
    /// 生成自: Microchip/DAC/MCP4725
    /// 版本: 1.0
    /// </summary>
    public static class MCP4725
    {
        // CPU架构: DAC, 12位, 400000 Hz

        // 内存段定义
        // Power-on default DAC value
        public const int EEPROM_START = 0x00;
        public const int EEPROM_END = 0x01;
        public const int EEPROM_SIZE = 2;

        // 外设定义
        // MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
        public const int MCP4725_BASE = 0x60;
        public static unsafe ushort* MCP4725_DAC_VALUE => (ushort*)0x00000060;
        public const int MCP4725_DAC_VALUE_PD = 12;  // Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
        public static unsafe ushort* MCP4725_WRITE_EEPROM => (ushort*)0x000000C0;

        public static void mcp4725_init()
        {
            // 硬件初始化代码
        }
    }
}
