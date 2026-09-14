using System;

namespace VML.Device.Bosch.BMP280
{
    /// <summary>
    /// BMP280 寄存器定义
    /// 生成自: Bosch/Sensor/BMP280
    /// 版本: 1.0
    /// </summary>
    public static class BMP280
    {
        // CPU架构: Sensor, 8位, 3400000 Hz

        // 内存段定义
        // LGA-8 (2.0x2.5x0.95mm)
        public const int PACKAGE_START = 0x00;
        public const int PACKAGE_END = 0x00;
        public const int PACKAGE_SIZE = 8;

        // 外设定义
        // BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
        public const int BMP280_BASE = 0x76;
        public static unsafe byte* BMP280_TEMP_XLSB => (byte*)0x00000172;
        public static unsafe byte* BMP280_TEMP_LSB => (byte*)0x00000171;
        public static unsafe byte* BMP280_TEMP_MSB => (byte*)0x00000170;
        public static unsafe byte* BMP280_PRESS_XLSB => (byte*)0x0000016F;
        public static unsafe byte* BMP280_PRESS_LSB => (byte*)0x0000016E;
        public static unsafe byte* BMP280_PRESS_MSB => (byte*)0x0000016D;
        public static unsafe byte* BMP280_CONFIG => (byte*)0x0000016B;
        public const int BMP280_CONFIG_T_SB = 5;  // Standby time in normal mode
        public const int BMP280_CONFIG_FILTER = 2;  // Filter coefficient
        public const int BMP280_CONFIG_SPI3W_EN = 0;  // Enable 3-wire SPI
        public static unsafe byte* BMP280_CTRL_MEAS => (byte*)0x0000016A;
        public const int BMP280_CTRL_MEAS_MODE = 0;  // 0=sleep, 1/2=forced, 3=normal
        public const int BMP280_CTRL_MEAS_OSRS_P = 2;  // Pressure oversampling
        public const int BMP280_CTRL_MEAS_OSRS_T = 5;  // Temperature oversampling
        public static unsafe byte* BMP280_STATUS => (byte*)0x00000169;
        public const int BMP280_STATUS_IM_UPDATE = 0;  // 1=Image register update in progress
        public const int BMP280_STATUS_MEASURING = 3;  // 1=Conversion is running
        public static unsafe byte* BMP280_CHIP_ID => (byte*)0x00000146;
        public static unsafe byte* BMP280_RESET => (byte*)0x00000156;

        public static void bmp280_init()
        {
            // 硬件初始化代码
        }
    }
}
