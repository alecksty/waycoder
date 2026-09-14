using System;

namespace VML.Device.Worldsemi.WS2812B
{
    /// <summary>
    /// WS2812B 寄存器定义
    /// 生成自: Worldsemi/LED/WS2812B
    /// 版本: 1.0
    /// </summary>
    public static class WS2812B
    {
        // CPU架构: LED, 24位, 800000 Hz

        // 内存段定义
        // Frame buffer (up to 256 LEDs × 3 bytes)
        public const int LED_FB_START = 0x00;
        public const int LED_FB_END = 0xFF;
        public const int LED_FB_SIZE = 256;

        // 外设定义
        // WS2812B RGB LED Strip (5V, 60mA/led)
        public const int WS2812B_BASE = 0x00;
        public static unsafe ushort* WS2812B_LED_COUNT => (ushort*)0x00000000;
        public static unsafe uint* WS2812B_LED_DATA => (uint*)0x00000002;
        public static unsafe byte* WS2812B_BRIGHTNESS => (byte*)0x00000005;
        public static unsafe byte* WS2812B_SHOW => (byte*)0x00000006;
        public static unsafe byte* WS2812B_CLEAR => (byte*)0x00000007;

        public static void ws2812b_init()
        {
            // 硬件初始化代码
        }
    }
}
