package vml.device.rohm.bh1750;

/**
 * BH1750 寄存器定义
 * 生成自: ROHM/Sensor/BH1750
 * 版本: 1.0
 */
public final class BH1750 {
    private BH1750() {} // 工具类
    // CPU架构: Sensor, 16位, 400000 Hz

    // 外设定义
    // BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
    public static final int BH1750_BASE = (int)0x23;
    public static final int BH1750_LUX = (int)0x00000023;
    public static final int BH1750_MODE = (int)0x00000024;
    public static final int BH1750_MODE_CONT_H = 0;  // Continuous High Res (1lx, 120ms)
    public static final int BH1750_MODE_CONT_H2 = 1;  // Continuous High Res 2 (0.5lx, 120ms)
    public static final int BH1750_MODE_CONT_L = 2;  // Continuous Low Res (4lx, 16ms)
    public static final int BH1750_MODE_ONCE_H = 3;  // One-time High Res (1lx, 120ms)
    public static final int BH1750_MODE_ONCE_H2 = 4;  // One-time High Res 2 (0.5lx, 120ms)
    public static final int BH1750_MODE_ONCE_L = 5;  // One-time Low Res (4lx, 16ms)
    public static final int BH1750_CMD_POWER_ON = (int)0x00000024;
    public static final int BH1750_CMD_POWER_OFF = (int)0x00000023;
    public static final int BH1750_CMD_RESET = (int)0x0000002A;

    public static native void bh1750_init();
}
