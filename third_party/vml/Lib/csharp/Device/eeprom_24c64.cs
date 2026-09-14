using System;

namespace VML.Device.Generic.24C64
{
    /// <summary>
    /// 24C64 寄存器定义
    /// 生成自: Generic/Memory/24C64
    /// 版本: 1.0
    /// </summary>
    public static class 24C64
    {
        // CPU架构: Memory, 8位, 400000 Hz

        // 内存段定义
        // EEPROM main memory array (8KB, 32-byte page write)
        public const int EEPROM_START = 0x00;
        public const int EEPROM_END = 0x1FFF;
        public const int EEPROM_SIZE = 8192;

        // 外设定义
        // 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
        public const int _24C64_BASE = 0x50;
        public static unsafe byte* _24C64_ADDR_H => (byte*)0x00000050;
        public static unsafe byte* _24C64_ADDR_L => (byte*)0x00000051;
        public static unsafe byte* _24C64_DATA => (byte*)0x00000052;
        public static unsafe byte* _24C64_PAGE_SIZE => (byte*)0x0000014E;
        public static unsafe ushort* _24C64_SIZE => (ushort*)0x0000014D;

        public static void _24c64_init()
        {
            // 硬件初始化代码
        }
    }
}
