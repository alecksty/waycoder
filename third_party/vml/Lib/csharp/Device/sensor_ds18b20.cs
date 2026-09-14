using System;

namespace VML.Device.Maxim/Dallas.DS18B20
{
    /// <summary>
    /// DS18B20 寄存器定义
    /// 生成自: Maxim/Dallas/Sensor/DS18B20
    /// 版本: 1.0
    /// </summary>
    public static class DS18B20
    {
        // CPU架构: Sensor, 8位, 100000 Hz

        // 内存段定义
        // Scratchpad memory (9 bytes)
        public const int SCRATCHPAD_START = 0x00;
        public const int SCRATCHPAD_END = 0x08;
        public const int SCRATCHPAD_SIZE = 9;

        // EEPROM (TH, TL, config bytes)
        public const int EEPROM_START = 0x00;
        public const int EEPROM_END = 0x02;
        public const int EEPROM_SIZE = 3;

        // 外设定义
        // DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
        public const int DS18B20_BASE = 0x00;
        public static unsafe byte* DS18B20_TEMP_LSB => (byte*)0x00000000;
        public static unsafe byte* DS18B20_TEMP_MSB => (byte*)0x00000001;
        public static unsafe byte* DS18B20_TH_REG => (byte*)0x00000002;
        public static unsafe byte* DS18B20_TL_REG => (byte*)0x00000003;
        public static unsafe byte* DS18B20_CONFIG => (byte*)0x00000004;
        public const int DS18B20_CONFIG_R0 = 5;  // Resolution select bit 0
        public const int DS18B20_CONFIG_R1 = 6;  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
        public static unsafe byte* DS18B20_COUNT_REMAIN => (byte*)0x00000006;
        public static unsafe byte* DS18B20_COUNT_PER_C => (byte*)0x00000007;
        public static unsafe byte* DS18B20_CRC => (byte*)0x00000008;

        public static void ds18b20_init()
        {
            // 硬件初始化代码
        }
    }
}
