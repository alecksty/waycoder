using System;

namespace VML.Device.Sitronix.ST7735
{
    /// <summary>
    /// ST7735 寄存器定义
    /// 生成自: Sitronix/Display/ST7735
    /// 版本: 1.0
    /// </summary>
    public static class ST7735
    {
        // CPU架构: Display, 16位, 16000000 Hz

        // 内存段定义
        // Graphics RAM (128x160x16bit)
        public const int GRAM_START = 0x00;
        public const int GRAM_END = 0x4FFF;
        public const int GRAM_SIZE = 20480;

        // 外设定义
        // ST7735 128x160 TFT (SPI, 3.3V-5V)
        public const int ST7735_BASE = 0x00;
        public static unsafe byte* ST7735_CMD => (byte*)0x00000000;
        public static unsafe byte* ST7735_DATA => (byte*)0x00000001;
        public static unsafe ushort* ST7735_COL_START => (ushort*)0x0000002A;
        public static unsafe ushort* ST7735_ROW_START => (ushort*)0x0000002B;
        public static unsafe ushort* ST7735_WRITE_RAM => (ushort*)0x0000002C;
        public static unsafe byte* ST7735_MADCTL => (byte*)0x00000036;
        public static unsafe byte* ST7735_COLMOD => (byte*)0x0000003A;
        public static unsafe uint* ST7735_INVON => (uint*)0x00000021;
        public static unsafe uint* ST7735_SLEEP_OUT => (uint*)0x00000011;
        public static unsafe uint* ST7735_DISP_ON => (uint*)0x00000029;

        public static void st7735_init()
        {
            // 硬件初始化代码
        }
    }
}
