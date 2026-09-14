using System;

namespace VML.Device.Generic.24C02
{
    /// <summary>
    /// 24C02 寄存器定义
    /// 生成自: Generic/Memory/24C02
    /// 版本: 1.0
    /// </summary>
    public static class 24C02
    {
        // CPU架构: Memory, 8位, 400000 Hz

        // 内存段定义
        // EEPROM main memory array (256 bytes, 8-byte page write)
        public const int EEPROM_START = 0x00;
        public const int EEPROM_END = 0xFF;
        public const int EEPROM_SIZE = 256;

        // 外设定义
        // 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
        public const int _24C02_BASE = 0x50;
        public static unsafe byte* _24C02_STATUS => (byte*)0x0000014F;
        public const int _24C02_STATUS_BUSY = 0;  // 1=Write in progress
        public static unsafe byte* _24C02_PAGE_SIZE => (byte*)0x0000014E;
        public static unsafe ushort* _24C02_SIZE => (ushort*)0x0000014D;

        public static void _24c02_init()
        {
            // 硬件初始化代码
        }
    }
}
