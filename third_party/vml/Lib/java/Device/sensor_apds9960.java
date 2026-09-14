package vml.device.broadcomavago.apds9960;

/**
 * APDS9960 寄存器定义
 * 生成自: Broadcom/Avago/Sensor/APDS9960
 * 版本: 1.0
 */
public final class APDS9960 {
    private APDS9960() {} // 工具类
    // CPU架构: Sensor, 8位, 400000 Hz

    // 外设定义
    // APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
    public static final int APDS9960_BASE = (int)0x39;
    public static final int APDS9960_ENABLE = (int)0x000000B9;
    public static final int APDS9960_GESTURE = (int)0x00000135;
    public static final int APDS9960_PROXIMITY = (int)0x000000D5;
    public static final int APDS9960_AMBIENT = (int)0x000000CF;
    public static final int APDS9960_RED = (int)0x000000D1;
    public static final int APDS9960_GREEN = (int)0x000000D3;
    public static final int APDS9960_BLUE = (int)0x000000D5;
    public static final int APDS9960_GESTURE_FIFO = (int)0x00000135;
    public static final int APDS9960_GESTURE_COUNT = (int)0x00000136;

    // 中断向量定义
    public static final int IRQ_INT = 0;  // Gesture/Proximity/Light interrupt

    public static native void apds9960_init();
}
