package vml.device.invensensetdk.mpu6050;

/**
 * MPU6050 寄存器定义
 * 生成自: InvenSense/TDK/Sensor/MPU6050
 * 版本: 1.0
 */
public final class MPU6050 {
    private MPU6050() {} // 工具类
    // CPU架构: Sensor, 8位, 400000 Hz

    // 内存段定义
    // QFN-24 (4x4x0.9mm)
    public static final int PACKAGE_START = (int)0x00;
    public static final int PACKAGE_END = (int)0x00;
    public static final int PACKAGE_SIZE = 24;

    // 外设定义
    // MPU6050 IMU (0x68/0x69, 2.375V-3.46V)
    public static final int MPU6050_BASE = (int)0x68;
    public static final int MPU6050_SMPLRT_DIV = (int)0x00000081;
    public static final int MPU6050_CONFIG = (int)0x00000082;
    public static final int MPU6050_CONFIG_DLPF_CFG = 0;  // Digital low-pass filter configuration
    public static final int MPU6050_GYRO_CONFIG = (int)0x00000083;
    public static final int MPU6050_GYRO_CONFIG_FS_SEL = 3;  // Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s
    public static final int MPU6050_ACCEL_CONFIG = (int)0x00000084;
    public static final int MPU6050_ACCEL_CONFIG_AFS_SEL = 3;  // Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g
    public static final int MPU6050_ACCEL_XOUT_H = (int)0x000000A3;
    public static final int MPU6050_ACCEL_XOUT_L = (int)0x000000A4;
    public static final int MPU6050_ACCEL_YOUT_H = (int)0x000000A5;
    public static final int MPU6050_ACCEL_YOUT_L = (int)0x000000A6;
    public static final int MPU6050_ACCEL_ZOUT_H = (int)0x000000A7;
    public static final int MPU6050_ACCEL_ZOUT_L = (int)0x000000A8;
    public static final int MPU6050_TEMP_OUT_H = (int)0x000000A9;
    public static final int MPU6050_TEMP_OUT_L = (int)0x000000AA;
    public static final int MPU6050_GYRO_XOUT_H = (int)0x000000AB;
    public static final int MPU6050_GYRO_XOUT_L = (int)0x000000AC;
    public static final int MPU6050_GYRO_YOUT_H = (int)0x000000AD;
    public static final int MPU6050_GYRO_YOUT_L = (int)0x000000AE;
    public static final int MPU6050_GYRO_ZOUT_H = (int)0x000000AF;
    public static final int MPU6050_GYRO_ZOUT_L = (int)0x000000B0;
    public static final int MPU6050_PWR_MGMT_1 = (int)0x000000D3;
    public static final int MPU6050_PWR_MGMT_1_DEVICE_RESET = 7;  // 1=Reset all internal registers
    public static final int MPU6050_PWR_MGMT_1_SLEEP = 6;  // 1=Sleep mode
    public static final int MPU6050_PWR_MGMT_1_CYCLE = 5;  // 1=Cycle mode
    public static final int MPU6050_PWR_MGMT_1_CLKSEL = 0;  // Clock source select
    public static final int MPU6050_WHO_AM_I = (int)0x000000DD;

    public static native void mpu6050_init();
}
