using System;

namespace VML.Device.Maxim.MAX7219
{
    /// <summary>
    /// MAX7219 寄存器定义
    /// 生成自: Maxim/LED/MAX7219
    /// 版本: 1.0
    /// </summary>
    public static class MAX7219
    {
        // CPU架构: LED, 8位, 10000000 Hz

        // 外设定义
        // MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
        public const int MAX7219_BASE = 0x00;
        public static unsafe byte* MAX7219_DIGIT0 => (byte*)0x00000001;
        public static unsafe byte* MAX7219_DIGIT1 => (byte*)0x00000002;
        public static unsafe byte* MAX7219_DIGIT2 => (byte*)0x00000003;
        public static unsafe byte* MAX7219_DIGIT3 => (byte*)0x00000004;
        public static unsafe byte* MAX7219_DIGIT4 => (byte*)0x00000005;
        public static unsafe byte* MAX7219_DIGIT5 => (byte*)0x00000006;
        public static unsafe byte* MAX7219_DIGIT6 => (byte*)0x00000007;
        public static unsafe byte* MAX7219_DIGIT7 => (byte*)0x00000008;
        public static unsafe byte* MAX7219_DECODE => (byte*)0x00000009;
        public static unsafe byte* MAX7219_INTENSITY => (byte*)0x0000000A;
        public static unsafe byte* MAX7219_SCAN_LIMIT => (byte*)0x0000000B;
        public static unsafe byte* MAX7219_SHUTDOWN => (byte*)0x0000000C;
        public static unsafe byte* MAX7219_TEST => (byte*)0x0000000F;

        public static void max7219_init()
        {
            // 硬件初始化代码
        }
    }
}
