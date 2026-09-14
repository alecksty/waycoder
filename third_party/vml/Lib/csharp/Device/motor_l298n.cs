using System;

namespace VML.Device.STMicroelectronics.L298N
{
    /// <summary>
    /// L298N 寄存器定义
    /// 生成自: STMicroelectronics/Motor/L298N
    /// 版本: 1.0
    /// </summary>
    public static class L298N
    {
        // CPU架构: Motor, 8位, 0 Hz

        // 外设定义
        // L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
        public const int L298N_BASE = 0x00;
        public static unsafe byte* L298N_MOTOR_A => (byte*)0x00000000;
        public const int L298N_MOTOR_A_IN1 = 0;  // Motor A Input 1
        public const int L298N_MOTOR_A_IN2 = 1;  // Motor A Input 2
        public const int L298N_MOTOR_A_ENA = 2;  // Motor A Enable/PWM
        public static unsafe byte* L298N_MOTOR_B => (byte*)0x00000001;
        public const int L298N_MOTOR_B_IN3 = 0;  // Motor B Input 3
        public const int L298N_MOTOR_B_IN4 = 1;  // Motor B Input 4
        public const int L298N_MOTOR_B_ENB = 2;  // Motor B Enable/PWM
        public static unsafe byte* L298N_SPEED_A => (byte*)0x00000002;
        public static unsafe byte* L298N_SPEED_B => (byte*)0x00000003;
        public static unsafe byte* L298N_STATUS => (byte*)0x00000004;

        public static void l298n_init()
        {
            // 硬件初始化代码
        }
    }
}
