using System;

namespace VML.Device.NXP/TI.PCF8574
{
    /// <summary>
    /// PCF8574 寄存器定义
    /// 生成自: NXP/TI/GPIO/PCF8574
    /// 版本: 1.0
    /// </summary>
    public static class PCF8574
    {
        // CPU架构: GPIO, 8位, 100000 Hz

        // 外设定义
        // PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
        public const int PCF8574_BASE = 0x20;
        public static unsafe byte* PCF8574_INPUT => (byte*)0x00000020;
        public const int PCF8574_INPUT_P0 = 0;  // Pin P0
        public const int PCF8574_INPUT_P1 = 1;  // Pin P1
        public const int PCF8574_INPUT_P2 = 2;  // Pin P2
        public const int PCF8574_INPUT_P3 = 3;  // Pin P3
        public const int PCF8574_INPUT_P4 = 4;  // Pin P4
        public const int PCF8574_INPUT_P5 = 5;  // Pin P5
        public const int PCF8574_INPUT_P6 = 6;  // Pin P6
        public const int PCF8574_INPUT_P7 = 7;  // Pin P7
        public static unsafe byte* PCF8574_OUTPUT => (byte*)0x00000021;
        public static unsafe byte* PCF8574_POLARITY => (byte*)0x00000022;

        // 中断向量定义
        public const int IRQ_INT = 0;  // Pin change interrupt (open-drain, active low)

        public static void pcf8574_init()
        {
            // 硬件初始化代码
        }
    }
}
