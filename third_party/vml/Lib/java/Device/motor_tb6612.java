package vml.device.toshiba.tb6612;

/**
 * TB6612 寄存器定义
 * 生成自: Toshiba/Motor/TB6612
 * 版本: 1.0
 */
public final class TB6612 {
    private TB6612() {} // 工具类
    // CPU架构: Motor, 8位, 100000 Hz

    // 外设定义
    // TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
    public static final int TB6612_BASE = (int)0x00;
    public static final int TB6612_MOTOR_A = (int)0x00000000;
    public static final int TB6612_MOTOR_A_AIN1 = 0;  // Motor A input 1
    public static final int TB6612_MOTOR_A_AIN2 = 1;  // Motor A input 2
    public static final int TB6612_MOTOR_A_PWMA = 2;  // Motor A PWM enable
    public static final int TB6612_MOTOR_B = (int)0x00000001;
    public static final int TB6612_MOTOR_B_BIN1 = 0;  // Motor B input 1
    public static final int TB6612_MOTOR_B_BIN2 = 1;  // Motor B input 2
    public static final int TB6612_MOTOR_B_PWMB = 2;  // Motor B PWM enable
    public static final int TB6612_SPEED_A = (int)0x00000002;
    public static final int TB6612_SPEED_B = (int)0x00000004;
    public static final int TB6612_STBY = (int)0x00000006;

    public static native void tb6612_init();
}
