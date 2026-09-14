using System;

namespace VML.Device.ST/TI.ULN2003
{
    /// <summary>
    /// ULN2003 寄存器定义
    /// 生成自: ST/TI/Motor/ULN2003
    /// 版本: 1.0
    /// </summary>
    public static class ULN2003
    {
        // CPU架构: Motor, 8位, 0 Hz

        // 外设定义
        // ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
        public const int ULN2003_BASE = 0x00;
        public static unsafe byte* ULN2003_STEPPER => (byte*)0x00000000;
        public static unsafe byte* ULN2003_STEP_MODE => (byte*)0x00000001;
        public static unsafe ushort* ULN2003_STEPS => (ushort*)0x00000002;
        public static unsafe byte* ULN2003_DELAY_MS => (byte*)0x00000004;
        public static unsafe ushort* ULN2003_POSITION => (ushort*)0x00000005;
        public static unsafe byte* ULN2003_DIRECTION => (byte*)0x00000007;

        public static void uln2003_init()
        {
            // 硬件初始化代码
        }
    }
}
