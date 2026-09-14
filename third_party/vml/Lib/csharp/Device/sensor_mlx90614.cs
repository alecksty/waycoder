using System;

namespace VML.Device.Melexis.MLX90614
{
    /// <summary>
    /// MLX90614 寄存器定义
    /// 生成自: Melexis/Sensor/MLX90614
    /// 版本: 1.0
    /// </summary>
    public static class MLX90614
    {
        // CPU架构: Sensor, 17位, 100000 Hz

        // 内存段定义
        // Internal EEPROM (calibration data)
        public const int EEPROM_START = 0x00;
        public const int EEPROM_END = 0x1F;
        public const int EEPROM_SIZE = 32;

        // 外设定义
        // MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
        public const int MLX90614_BASE = 0x5A;
        public static unsafe ushort* MLX90614_T_AMBIENT => (ushort*)0x00000060;
        public static unsafe ushort* MLX90614_T_OBJECT1 => (ushort*)0x00000061;
        public static unsafe ushort* MLX90614_T_OBJECT2 => (ushort*)0x00000062;
        public static unsafe ushort* MLX90614_RAW_IR1 => (ushort*)0x0000005E;
        public static unsafe ushort* MLX90614_RAW_IR2 => (ushort*)0x0000005F;
        public static unsafe ushort* MLX90614_EMISSIVITY => (ushort*)0x0000005E;

        public static void mlx90614_init()
        {
            // 硬件初始化代码
        }
    }
}
