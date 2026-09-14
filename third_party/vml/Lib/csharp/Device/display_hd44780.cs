using System;

namespace VML.Device.Hitachi.HD44780
{
    /// <summary>
    /// HD44780 寄存器定义
    /// 生成自: Hitachi/Display/HD44780
    /// 版本: 1.0
    /// </summary>
    public static class HD44780
    {
        // CPU架构: Display, 8位, 0 Hz

        // 内存段定义
        // Display Data RAM (80 bytes, 2 lines)
        public const int DDRAM_START = 0x00;
        public const int DDRAM_END = 0x4F;
        public const int DDRAM_SIZE = 80;

        // Character Generator RAM (8 custom chars x 8 bytes)
        public const int CGRAM_START = 0x00;
        public const int CGRAM_END = 0x3F;
        public const int CGRAM_SIZE = 64;

        // 外设定义
        // HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
        public const int HD44780_BASE = 0x27;
        public static unsafe byte* HD44780_CMD => (byte*)0x00000027;
        public static unsafe byte* HD44780_DATA => (byte*)0x00000028;
        public static unsafe byte* HD44780_CTRL_RS => (byte*)0x00000027;
        public static unsafe byte* HD44780_CTRL_RW => (byte*)0x00000028;
        public static unsafe byte* HD44780_CTRL_EN => (byte*)0x00000029;
        public static unsafe byte* HD44780_CTRL_BL => (byte*)0x0000002A;
        public static unsafe byte* HD44780_ADDR_DDRAM => (byte*)0x000000A7;
        public static unsafe byte* HD44780_ADDR_CGRAM => (byte*)0x00000067;

        public static void hd44780_init()
        {
            // 硬件初始化代码
        }
    }
}
