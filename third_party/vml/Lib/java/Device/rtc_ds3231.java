package vml.device.maximdallas.ds3231;

/**
 * DS3231 寄存器定义
 * 生成自: Maxim/Dallas/RTC/DS3231
 * 版本: 1.0
 */
public final class DS3231 {
    private DS3231() {} // 工具类
    // CPU架构: RTC, 8位, 400000 Hz

    // 内存段定义
    // AT24C32 EEPROM (32Kbit)
    public static final int EEPROM_START = (int)0x14;
    public static final int EEPROM_END = (int)0xFF;
    public static final int EEPROM_SIZE = 236;

    // 外设定义
    // DS3231 Precision RTC (0x68, 3.3V-5.5V)
    public static final int DS3231_BASE = (int)0x68;
    public static final int DS3231_SEC = (int)0x00000068;
    public static final int DS3231_MIN = (int)0x00000069;
    public static final int DS3231_HOUR = (int)0x0000006A;
    public static final int DS3231_DAY = (int)0x0000006B;
    public static final int DS3231_DATE = (int)0x0000006C;
    public static final int DS3231_MONTH_CENT = (int)0x0000006D;
    public static final int DS3231_YEAR = (int)0x0000006E;
    public static final int DS3231_ALARM1_SEC = (int)0x0000006F;
    public static final int DS3231_ALARM1_MIN = (int)0x00000070;
    public static final int DS3231_ALARM1_HOUR = (int)0x00000071;
    public static final int DS3231_ALARM2_MIN = (int)0x00000073;
    public static final int DS3231_ALARM2_HOUR = (int)0x00000074;
    public static final int DS3231_CTRL = (int)0x00000076;
    public static final int DS3231_CTRL_STATUS = (int)0x00000077;
    public static final int DS3231_TEMP_MSB = (int)0x00000079;
    public static final int DS3231_TEMP_LSB = (int)0x0000007A;

    public static native void ds3231_init();
}
