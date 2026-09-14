using System;

namespace VML.Device.SolomonSystech.SSD1306
{
    /// <summary>
    /// SSD1306 寄存器定义
    /// 生成自: Solomon Systech/Display/SSD1306
    /// 版本: 1.0
    /// </summary>
    public static class SSD1306
    {
        // CPU架构: Display, 8位, 400000 Hz

        // 内存段定义
        // Graphic Display Data RAM (128x64 = 1024 bytes)
        public const int GDDRAM_START = 0x00;
        public const int GDDRAM_END = 0x3FF;
        public const int GDDRAM_SIZE = 1024;

        // 外设定义
        // SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
        public const int SSD1306_BASE = 0x3C;
        public static unsafe byte* SSD1306_CMD => (byte*)0x0000003C;
        public static unsafe byte* SSD1306_DATA => (byte*)0x0000007C;
        public static unsafe byte* SSD1306_DISPLAY_OFF => (byte*)0x000000EA;
        public static unsafe byte* SSD1306_DISPLAY_ON => (byte*)0x000000EB;
        public static unsafe byte* SSD1306_CONTRAST => (byte*)0x000000BD;
        public static unsafe byte* SSD1306_SEG_REMAP => (byte*)0x000000DD;
        public static unsafe byte* SSD1306_COM_SCAN => (byte*)0x00000104;
        public static unsafe byte* SSD1306_ADDR_MODE => (byte*)0x0000005C;
        public static unsafe byte* SSD1306_COL_START => (byte*)0x0000005D;
        public static unsafe byte* SSD1306_PAGE_START => (byte*)0x0000005E;

        public static void ssd1306_init()
        {
            // 硬件初始化代码
        }
    }
}
