using System;

namespace VML.Device.Allegro.A4988
{
    /// <summary>
    /// A4988 寄存器定义
    /// 生成自: Allegro/Motor/A4988
    /// 版本: 1.0
    /// </summary>
    public static class A4988
    {
        // CPU架构: Motor, 8位, 0 Hz

        // 外设定义
        // A4988 Stepper Motor Driver (3.3V/5V logic)
        public const int A4988_BASE = 0x00;
        public static unsafe byte* A4988_CTRL => (byte*)0x00000000;
        public const int A4988_CTRL_STEP = 0;  // Step pulse (rising edge)
        public const int A4988_CTRL_DIR = 1;  // Direction (0=CW, 1=CCW)
        public const int A4988_CTRL_ENABLE = 2;  // Enable (active low)
        public const int A4988_CTRL_SLEEP = 3;  // Sleep mode (active low)
        public const int A4988_CTRL_RESET = 4;  // Reset (active low)
        public static unsafe byte* A4988_MICROSTEP => (byte*)0x00000001;
        public const int A4988_MICROSTEP_MS1 = 0;  // Microstep select 1
        public const int A4988_MICROSTEP_MS2 = 1;  // Microstep select 2
        public const int A4988_MICROSTEP_MS3 = 2;  // Microstep select 3
        public static unsafe uint* A4988_STEPS => (uint*)0x00000002;
        public static unsafe ushort* A4988_DELAY_US => (ushort*)0x00000006;

        public static void a4988_init()
        {
            // 硬件初始化代码
        }
    }
}
