package vml.device.amssciosense.ccs811;

/**
 * CCS811 寄存器定义
 * 生成自: AMS/ScioSense/Sensor/CCS811
 * 版本: 1.0
 */
public final class CCS811 {
    private CCS811() {} // 工具类
    // CPU架构: Sensor, 16位, 400000 Hz

    // 外设定义
    // CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
    public static final int CCS811_BASE = (int)0x5A;
    public static final int CCS811_STATUS = (int)0x0000005A;
    public static final int CCS811_MEAS_MODE = (int)0x0000005B;
    public static final int CCS811_ALG_RESULT = (int)0x0000005C;
    public static final int CCS811_ECO2 = (int)0x0000005C;
    public static final int CCS811_TVOC = (int)0x0000005E;
    public static final int CCS811_RAW_DATA = (int)0x00000060;
    public static final int CCS811_BASELINE = (int)0x00000065;
    public static final int CCS811_HW_ID = (int)0x0000007A;
    public static final int CCS811_ERROR_ID = (int)0x0000013A;
    public static final int CCS811_APP_START = (int)0x0000014E;
    public static final int CCS811_SW_RESET = (int)0x00000159;

    // 中断向量定义
    public static final int IRQ_INT = 0;  // Data ready / interrupt pin

    // 引脚定义
    public static final int PIN_WAKE = 1;  // Wake pin (active low)

    public static native void ccs811_init();
}
