using System;

namespace VML.Device.Maxim/Dallas.DS1307
{
    /// <summary>
    /// DS1307 寄存器定义
    /// 生成自: Maxim/Dallas/RTC/DS1307
    /// 版本: 1.0
    /// </summary>
    public static class DS1307
    {
        // CPU架构: RTC, 8位, 100000 Hz

        // 内存段定义
        // Non-volatile RAM (56 bytes)
        public const int NVRAM_START = 0x08;
        public const int NVRAM_END = 0x3F;
        public const int NVRAM_SIZE = 56;

        // 外设定义
        // DS1307 RTC (0x68, 5V, DIP-8)
        public const int DS1307_BASE = 0x68;
        public static unsafe byte* DS1307_SEC => (byte*)0x00000068;
        public static unsafe byte* DS1307_MIN => (byte*)0x00000069;
        public static unsafe byte* DS1307_HOUR => (byte*)0x0000006A;
        public static unsafe byte* DS1307_DAY => (byte*)0x0000006B;
        public static unsafe byte* DS1307_DATE => (byte*)0x0000006C;
        public static unsafe byte* DS1307_MONTH => (byte*)0x0000006D;
        public static unsafe byte* DS1307_YEAR => (byte*)0x0000006E;
        public static unsafe byte* DS1307_CTRL => (byte*)0x0000006F;

        public static void ds1307_init()
        {
            // 硬件初始化代码
        }
    }
}
