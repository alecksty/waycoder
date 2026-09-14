package vml.device.stmicroelectronics.l298n;

/**
 * L298N 寄存器定义
 * 生成自: STMicroelectronics/Motor/L298N
 * 版本: 1.0
 */
public final class L298N {
    private L298N() {} // 工具类
    // CPU架构: Motor, 8位, 0 Hz

    // 外设定义
    // L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
    public static final int L298N_BASE = (int)0x00;
    public static final int L298N_MOTOR_A = (int)0x00000000;
    public static final int L298N_MOTOR_A_IN1 = 0;  // Motor A Input 1
    public static final int L298N_MOTOR_A_IN2 = 1;  // Motor A Input 2
    public static final int L298N_MOTOR_A_ENA = 2;  // Motor A Enable/PWM
    public static final int L298N_MOTOR_B = (int)0x00000001;
    public static final int L298N_MOTOR_B_IN3 = 0;  // Motor B Input 3
    public static final int L298N_MOTOR_B_IN4 = 1;  // Motor B Input 4
    public static final int L298N_MOTOR_B_ENB = 2;  // Motor B Enable/PWM
    public static final int L298N_SPEED_A = (int)0x00000002;
    public static final int L298N_SPEED_B = (int)0x00000003;
    public static final int L298N_STATUS = (int)0x00000004;

    public static native void l298n_init();
}
