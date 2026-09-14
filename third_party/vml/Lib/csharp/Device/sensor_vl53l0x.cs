using System;

namespace VML.Device.STMicroelectronics.VL53L0X
{
    /// <summary>
    /// VL53L0X 寄存器定义
    /// 生成自: STMicroelectronics/Sensor/VL53L0X
    /// 版本: 1.0
    /// </summary>
    public static class VL53L0X
    {
        // CPU架构: Sensor, 16位, 400000 Hz

        // 外设定义
        // VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
        public const int VL53L0X_BASE = 0x29;
        public static unsafe ushort* VL53L0X_DISTANCE => (ushort*)0x00000029;
        public static unsafe ushort* VL53L0X_SIGNAL_RATE => (ushort*)0x0000002B;
        public static unsafe ushort* VL53L0X_AMBIENT_RATE => (ushort*)0x0000002D;
        public static unsafe ushort* VL53L0X_SPAD_COUNT => (ushort*)0x0000002F;
        public static unsafe byte* VL53L0X_RANGE_STATUS => (byte*)0x00000031;
        public static unsafe uint* VL53L0X_TIMING_BUDGET => (uint*)0x00000032;
        public static unsafe uint* VL53L0X_INTER_MEAS => (uint*)0x00000036;
        public static unsafe byte* VL53L0X_MODE => (byte*)0x00000037;

        // 引脚定义
        public const int PIN_XSHUT = 1;  // Shutdown pin (active low)
        public const int PIN_INT = 2;  // Interrupt (open-drain)

        public static void vl53l0x_init()
        {
            // 硬件初始化代码
        }
    }
}
