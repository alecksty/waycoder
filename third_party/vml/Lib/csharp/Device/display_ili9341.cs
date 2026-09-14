using System;

namespace VML.Device.Ilitek.ILI9341
{
    /// <summary>
    /// ILI9341 寄存器定义
    /// 生成自: Ilitek/Display/ILI9341
    /// 版本: 1.0
    /// </summary>
    public static class ILI9341
    {
        // CPU架构: Display, 18位, 20000000 Hz

        // 内存段定义
        // Graphics RAM (240x320x18bit)
        public const int GRAM_START = 0x00;
        public const int GRAM_END = 0xBCFF;
        public const int GRAM_SIZE = 156672;

        // 外设定义
        // ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
        public const int ILI9341_BASE = 0x00;
        public static unsafe byte* ILI9341_CMD => (byte*)0x00000000;
        public static unsafe byte* ILI9341_DATA => (byte*)0x00000001;
        public static unsafe ushort* ILI9341_COL_START => (ushort*)0x0000002A;
        public static unsafe ushort* ILI9341_PAGE_START => (ushort*)0x0000002B;
        public static unsafe ushort* ILI9341_WRITE_RAM => (ushort*)0x0000002C;
        public static unsafe byte* ILI9341_MADCTL => (byte*)0x00000036;
        public static unsafe byte* ILI9341_PIXFMT => (byte*)0x0000003A;
        public static unsafe ushort* ILI9341_FRMCTL => (ushort*)0x000000B1;
        public static unsafe byte* ILI9341_GAMMA => (byte*)0x00000026;
        public static unsafe uint* ILI9341_SLEEP_OUT => (uint*)0x00000011;
        public static unsafe uint* ILI9341_DISP_ON => (uint*)0x00000029;

        public static void ili9341_init()
        {
            // 硬件初始化代码
        }
    }
}
