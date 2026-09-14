using System;

namespace VML.Device.Aosong.DHT11
{
    /// <summary>
    /// DHT11 寄存器定义
    /// 生成自: Aosong/Sensor/DHT11
    /// 版本: 1.0
    /// </summary>
    public static class DHT11
    {
        // CPU架构: Sensor, 8位, 500000 Hz

        // 内存段定义
        // DIP-4/SMD-4
        public const int PACKAGE_START = 0x00;
        public const int PACKAGE_END = 0x00;
        public const int PACKAGE_SIZE = 4;

        // 外设定义
        // DHT11 1-Wire Sensor (3.0V-5.5V)
        public const int DHT11_BASE = 0x00;
        public static unsafe byte* DHT11_HUMIDITY_INT => (byte*)0x00000000;
        public static unsafe byte* DHT11_HUMIDITY_DEC => (byte*)0x00000001;
        public static unsafe byte* DHT11_TEMP_INT => (byte*)0x00000002;
        public static unsafe byte* DHT11_TEMP_DEC => (byte*)0x00000003;
        public static unsafe byte* DHT11_CHECKSUM => (byte*)0x00000004;

        public static void dht11_init()
        {
            // 硬件初始化代码
        }
    }
}
