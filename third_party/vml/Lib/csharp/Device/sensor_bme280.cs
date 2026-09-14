using System;

namespace VML.Device.Bosch.BME280
{
    /// <summary>
    /// BME280 寄存器定义
    /// 生成自: Bosch/Sensor/BME280
    /// 版本: 1.0
    /// </summary>
    public static class BME280
    {
        // CPU架构: Sensor, 8位, 400000 Hz

        // 外设定义
        // BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
        public const int BME280_BASE = 0x76;
        public static unsafe byte* BME280_CHIP_ID => (byte*)0x00000146;
        public static unsafe byte* BME280_RESET => (byte*)0x00000156;
        public static unsafe byte* BME280_CTRL_HUM => (byte*)0x00000168;
        public static unsafe byte* BME280_STATUS => (byte*)0x00000169;
        public static unsafe byte* BME280_CTRL_MEAS => (byte*)0x0000016A;
        public static unsafe byte* BME280_CONFIG => (byte*)0x0000016B;
        public static unsafe uint* BME280_PRESS => (uint*)0x0000016D;
        public static unsafe uint* BME280_TEMP => (uint*)0x00000170;
        public static unsafe ushort* BME280_HUM => (ushort*)0x00000173;

        public static void bme280_init()
        {
            // 硬件初始化代码
        }
    }
}
