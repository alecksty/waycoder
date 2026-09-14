package vml.device.bosch.bme280;

/**
 * BME280 寄存器定义
 * 生成自: Bosch/Sensor/BME280
 * 版本: 1.0
 */
public final class BME280 {
    private BME280() {} // 工具类
    // CPU架构: Sensor, 8位, 400000 Hz

    // 外设定义
    // BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
    public static final int BME280_BASE = (int)0x76;
    public static final int BME280_CHIP_ID = (int)0x00000146;
    public static final int BME280_RESET = (int)0x00000156;
    public static final int BME280_CTRL_HUM = (int)0x00000168;
    public static final int BME280_STATUS = (int)0x00000169;
    public static final int BME280_CTRL_MEAS = (int)0x0000016A;
    public static final int BME280_CONFIG = (int)0x0000016B;
    public static final int BME280_PRESS = (int)0x0000016D;
    public static final int BME280_TEMP = (int)0x00000170;
    public static final int BME280_HUM = (int)0x00000173;

    public static native void bme280_init();
}
