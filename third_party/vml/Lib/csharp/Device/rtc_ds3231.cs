using System;

namespace VML.Device.Maxim/Dallas.DS3231
{
    /// <summary>
    /// DS3231 寄存器定义
    /// 生成自: Maxim/Dallas/RTC/DS3231
    /// 版本: 1.0
    /// </summary>
    public static class DS3231
    {
        // CPU架构: RTC, 8位, 400000 Hz

        // 内存段定义
        // AT24C32 EEPROM (32Kbit)
        public const int EEPROM_START = 0x14;
        public const int EEPROM_END = 0xFF;
        public const int EEPROM_SIZE = 236;

        // 外设定义
        // DS3231 Precision RTC (0x68, 3.3V-5.5V)
        public const int DS3231_BASE = 0x68;
        public static unsafe byte* DS3231_SEC => (byte*)0x00000068;
        public static unsafe byte* DS3231_MIN => (byte*)0x00000069;
        public static unsafe byte* DS3231_HOUR => (byte*)0x0000006A;
        public static unsafe byte* DS3231_DAY => (byte*)0x0000006B;
        public static unsafe byte* DS3231_DATE => (byte*)0x0000006C;
        public static unsafe byte* DS3231_MONTH_CENT => (byte*)0x0000006D;
        public static unsafe byte* DS3231_YEAR => (byte*)0x0000006E;
        public static unsafe byte* DS3231_ALARM1_SEC => (byte*)0x0000006F;
        public static unsafe byte* DS3231_ALARM1_MIN => (byte*)0x00000070;
        public static unsafe byte* DS3231_ALARM1_HOUR => (byte*)0x00000071;
        public static unsafe byte* DS3231_ALARM2_MIN => (byte*)0x00000073;
        public static unsafe byte* DS3231_ALARM2_HOUR => (byte*)0x00000074;
        public static unsafe byte* DS3231_CTRL => (byte*)0x00000076;
        public static unsafe byte* DS3231_CTRL_STATUS => (byte*)0x00000077;
        public static unsafe byte* DS3231_TEMP_MSB => (byte*)0x00000079;
        public static unsafe byte* DS3231_TEMP_LSB => (byte*)0x0000007A;

        public static void ds3231_init()
        {
            // 硬件初始化代码
        }
    }
}
