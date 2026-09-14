package vml.device.stmicroelectronics.vl53l0x;

/**
 * VL53L0X 寄存器定义
 * 生成自: STMicroelectronics/Sensor/VL53L0X
 * 版本: 1.0
 */
public final class VL53L0X {
    private VL53L0X() {} // 工具类
    // CPU架构: Sensor, 16位, 400000 Hz

    // 外设定义
    // VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
    public static final int VL53L0X_BASE = (int)0x29;
    public static final int VL53L0X_DISTANCE = (int)0x00000029;
    public static final int VL53L0X_SIGNAL_RATE = (int)0x0000002B;
    public static final int VL53L0X_AMBIENT_RATE = (int)0x0000002D;
    public static final int VL53L0X_SPAD_COUNT = (int)0x0000002F;
    public static final int VL53L0X_RANGE_STATUS = (int)0x00000031;
    public static final int VL53L0X_TIMING_BUDGET = (int)0x00000032;
    public static final int VL53L0X_INTER_MEAS = (int)0x00000036;
    public static final int VL53L0X_MODE = (int)0x00000037;

    // 引脚定义
    public static final int PIN_XSHUT = 1;  // Shutdown pin (active low)
    public static final int PIN_INT = 2;  // Interrupt (open-drain)

    public static native void vl53l0x_init();
}
