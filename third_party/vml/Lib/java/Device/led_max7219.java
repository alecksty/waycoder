package vml.device.maxim.max7219;

/**
 * MAX7219 寄存器定义
 * 生成自: Maxim/LED/MAX7219
 * 版本: 1.0
 */
public final class MAX7219 {
    private MAX7219() {} // 工具类
    // CPU架构: LED, 8位, 10000000 Hz

    // 外设定义
    // MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
    public static final int MAX7219_BASE = (int)0x00;
    public static final int MAX7219_DIGIT0 = (int)0x00000001;
    public static final int MAX7219_DIGIT1 = (int)0x00000002;
    public static final int MAX7219_DIGIT2 = (int)0x00000003;
    public static final int MAX7219_DIGIT3 = (int)0x00000004;
    public static final int MAX7219_DIGIT4 = (int)0x00000005;
    public static final int MAX7219_DIGIT5 = (int)0x00000006;
    public static final int MAX7219_DIGIT6 = (int)0x00000007;
    public static final int MAX7219_DIGIT7 = (int)0x00000008;
    public static final int MAX7219_DECODE = (int)0x00000009;
    public static final int MAX7219_INTENSITY = (int)0x0000000A;
    public static final int MAX7219_SCAN_LIMIT = (int)0x0000000B;
    public static final int MAX7219_SHUTDOWN = (int)0x0000000C;
    public static final int MAX7219_TEST = (int)0x0000000F;

    public static native void max7219_init();
}
