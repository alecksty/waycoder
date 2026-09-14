package vml.device.stti.uln2003;

/**
 * ULN2003 寄存器定义
 * 生成自: ST/TI/Motor/ULN2003
 * 版本: 1.0
 */
public final class ULN2003 {
    private ULN2003() {} // 工具类
    // CPU架构: Motor, 8位, 0 Hz

    // 外设定义
    // ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
    public static final int ULN2003_BASE = (int)0x00;
    public static final int ULN2003_STEPPER = (int)0x00000000;
    public static final int ULN2003_STEP_MODE = (int)0x00000001;
    public static final int ULN2003_STEPS = (int)0x00000002;
    public static final int ULN2003_DELAY_MS = (int)0x00000004;
    public static final int ULN2003_POSITION = (int)0x00000005;
    public static final int ULN2003_DIRECTION = (int)0x00000007;

    public static native void uln2003_init();
}
