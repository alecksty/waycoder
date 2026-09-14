using System;

namespace VML.Device.Broadcom/Avago.APDS9960
{
    /// <summary>
    /// APDS9960 寄存器定义
    /// 生成自: Broadcom/Avago/Sensor/APDS9960
    /// 版本: 1.0
    /// </summary>
    public static class APDS9960
    {
        // CPU架构: Sensor, 8位, 400000 Hz

        // 外设定义
        // APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
        public const int APDS9960_BASE = 0x39;
        public static unsafe byte* APDS9960_ENABLE => (byte*)0x000000B9;
        public static unsafe byte* APDS9960_GESTURE => (byte*)0x00000135;
        public static unsafe byte* APDS9960_PROXIMITY => (byte*)0x000000D5;
        public static unsafe ushort* APDS9960_AMBIENT => (ushort*)0x000000CF;
        public static unsafe ushort* APDS9960_RED => (ushort*)0x000000D1;
        public static unsafe ushort* APDS9960_GREEN => (ushort*)0x000000D3;
        public static unsafe ushort* APDS9960_BLUE => (ushort*)0x000000D5;
        public static unsafe uint* APDS9960_GESTURE_FIFO => (uint*)0x00000135;
        public static unsafe byte* APDS9960_GESTURE_COUNT => (byte*)0x00000136;

        // 中断向量定义
        public const int IRQ_INT = 0;  // Gesture/Proximity/Light interrupt

        public static void apds9960_init()
        {
            // 硬件初始化代码
        }
    }
}
