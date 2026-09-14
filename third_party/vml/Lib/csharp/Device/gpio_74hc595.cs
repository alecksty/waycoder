using System;

namespace VML.Device.TI/NXP.74HC595
{
    /// <summary>
    /// 74HC595 寄存器定义
    /// 生成自: TI/NXP/GPIO/74HC595
    /// 版本: 1.0
    /// </summary>
    public static class 74HC595
    {
        // CPU架构: GPIO, 8位, 10000000 Hz

        // 外设定义
        // 74HC595 8-bit Shift Register (2V-6V, DIP-16)
        public const int _74HC595_BASE = 0x00;
        public static unsafe byte* _74HC595_DATA => (byte*)0x00000000;
        public static unsafe byte* _74HC595_LATCH => (byte*)0x00000001;
        public static unsafe byte* _74HC595_CHAIN_COUNT => (byte*)0x00000002;
        public static unsafe byte* _74HC595_OE => (byte*)0x00000003;
        public static unsafe byte* _74HC595_CLEAR => (byte*)0x00000004;

        public static void _74hc595_init()
        {
            // 硬件初始化代码
        }
    }
}
