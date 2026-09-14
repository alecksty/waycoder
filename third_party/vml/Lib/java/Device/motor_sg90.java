package vml.device.towerpro.sg90;

/**
 * SG90 寄存器定义
 * 生成自: Tower Pro/Motor/SG90
 * 版本: 1.0
 */
public final class SG90 {
    private SG90() {} // 工具类
    // CPU架构: Motor, 8位, 0 Hz

    // 外设定义
    // SG90 Micro Servo (500-2500us pulse, 50Hz)
    public static final int SG90_BASE = (int)0x00;
    public static final int SG90_ANGLE = (int)0x00000000;
    public static final int SG90_PULSE_MIN = (int)0x00000001;
    public static final int SG90_PULSE_MAX = (int)0x00000003;
    public static final int SG90_CURRENT_ANGLE = (int)0x00000005;
    public static final int SG90_SPEED = (int)0x00000006;

    public static native void sg90_init();
}
