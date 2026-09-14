package vml.device.melexis.mlx90614;

/**
 * MLX90614 寄存器定义
 * 生成自: Melexis/Sensor/MLX90614
 * 版本: 1.0
 */
public final class MLX90614 {
    private MLX90614() {} // 工具类
    // CPU架构: Sensor, 17位, 100000 Hz

    // 内存段定义
    // Internal EEPROM (calibration data)
    public static final int EEPROM_START = (int)0x00;
    public static final int EEPROM_END = (int)0x1F;
    public static final int EEPROM_SIZE = 32;

    // 外设定义
    // MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
    public static final int MLX90614_BASE = (int)0x5A;
    public static final int MLX90614_T_AMBIENT = (int)0x00000060;
    public static final int MLX90614_T_OBJECT1 = (int)0x00000061;
    public static final int MLX90614_T_OBJECT2 = (int)0x00000062;
    public static final int MLX90614_RAW_IR1 = (int)0x0000005E;
    public static final int MLX90614_RAW_IR2 = (int)0x0000005F;
    public static final int MLX90614_EMISSIVITY = (int)0x0000005E;

    public static native void mlx90614_init();
}
