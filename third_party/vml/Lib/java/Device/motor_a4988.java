package vml.device.allegro.a4988;

/**
 * A4988 寄存器定义
 * 生成自: Allegro/Motor/A4988
 * 版本: 1.0
 */
public final class A4988 {
    private A4988() {} // 工具类
    // CPU架构: Motor, 8位, 0 Hz

    // 外设定义
    // A4988 Stepper Motor Driver (3.3V/5V logic)
    public static final int A4988_BASE = (int)0x00;
    public static final int A4988_CTRL = (int)0x00000000;
    public static final int A4988_CTRL_STEP = 0;  // Step pulse (rising edge)
    public static final int A4988_CTRL_DIR = 1;  // Direction (0=CW, 1=CCW)
    public static final int A4988_CTRL_ENABLE = 2;  // Enable (active low)
    public static final int A4988_CTRL_SLEEP = 3;  // Sleep mode (active low)
    public static final int A4988_CTRL_RESET = 4;  // Reset (active low)
    public static final int A4988_MICROSTEP = (int)0x00000001;
    public static final int A4988_MICROSTEP_MS1 = 0;  // Microstep select 1
    public static final int A4988_MICROSTEP_MS2 = 1;  // Microstep select 2
    public static final int A4988_MICROSTEP_MS3 = 2;  // Microstep select 3
    public static final int A4988_STEPS = (int)0x00000002;
    public static final int A4988_DELAY_US = (int)0x00000006;

    public static native void a4988_init();
}
