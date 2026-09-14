package vml.device.maximdallas.ds1307;

/**
 * DS1307 寄存器定义
 * 生成自: Maxim/Dallas/RTC/DS1307
 * 版本: 1.0
 */
public final class DS1307 {
    private DS1307() {} // 工具类
    // CPU架构: RTC, 8位, 100000 Hz

    // 内存段定义
    // Non-volatile RAM (56 bytes)
    public static final int NVRAM_START = (int)0x08;
    public static final int NVRAM_END = (int)0x3F;
    public static final int NVRAM_SIZE = 56;

    // 外设定义
    // DS1307 RTC (0x68, 5V, DIP-8)
    public static final int DS1307_BASE = (int)0x68;
    public static final int DS1307_SEC = (int)0x00000068;
    public static final int DS1307_MIN = (int)0x00000069;
    public static final int DS1307_HOUR = (int)0x0000006A;
    public static final int DS1307_DAY = (int)0x0000006B;
    public static final int DS1307_DATE = (int)0x0000006C;
    public static final int DS1307_MONTH = (int)0x0000006D;
    public static final int DS1307_YEAR = (int)0x0000006E;
    public static final int DS1307_CTRL = (int)0x0000006F;

    public static native void ds1307_init();
}
