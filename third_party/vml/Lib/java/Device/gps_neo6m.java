package vml.device.ublox.neo6m;

/**
 * NEO6M 寄存器定义
 * 生成自: u-blox/GPS/NEO6M
 * 版本: 1.0
 */
public final class NEO6M {
    private NEO6M() {} // 工具类
    // CPU架构: GPS, 8位, 9600 Hz

    // 外设定义
    // NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
    public static final int NEO6M_BASE = (int)0x00;
    public static final int NEO6M_LATITUDE = (int)0x00000000;
    public static final int NEO6M_LONGITUDE = (int)0x00000004;
    public static final int NEO6M_ALTITUDE = (int)0x00000008;
    public static final int NEO6M_SPEED = (int)0x0000000C;
    public static final int NEO6M_HEADING = (int)0x0000000E;
    public static final int NEO6M_SATELLITES = (int)0x00000010;
    public static final int NEO6M_HDOP = (int)0x00000011;
    public static final int NEO6M_FIX_TYPE = (int)0x00000013;
    public static final int NEO6M_DATE = (int)0x00000014;
    public static final int NEO6M_TIME = (int)0x00000018;
    public static final int NEO6M_VALID = (int)0x0000001C;

    public static native void neo6m_init();
}
