using System;

namespace VML.Device.TowerPro.SG90
{
    /// <summary>
    /// SG90 寄存器定义
    /// 生成自: Tower Pro/Motor/SG90
    /// 版本: 1.0
    /// </summary>
    public static class SG90
    {
        // CPU架构: Motor, 8位, 0 Hz

        // 外设定义
        // SG90 Micro Servo (500-2500us pulse, 50Hz)
        public const int SG90_BASE = 0x00;
        public static unsafe byte* SG90_ANGLE => (byte*)0x00000000;
        public static unsafe ushort* SG90_PULSE_MIN => (ushort*)0x00000001;
        public static unsafe ushort* SG90_PULSE_MAX => (ushort*)0x00000003;
        public static unsafe byte* SG90_CURRENT_ANGLE => (byte*)0x00000005;
        public static unsafe byte* SG90_SPEED => (byte*)0x00000006;

        public static void sg90_init()
        {
            // 硬件初始化代码
        }
    }
}
