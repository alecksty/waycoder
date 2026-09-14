package vml.device.bosch.bmp280;

/**
 * BMP280 寄存器定义
 * 生成自: Bosch/Sensor/BMP280
 * 版本: 1.0
 */
public final class BMP280 {
    private BMP280() {} // 工具类
    // CPU架构: Sensor, 8位, 3400000 Hz

    // 内存段定义
    // LGA-8 (2.0x2.5x0.95mm)
    public static final int PACKAGE_START = (int)0x00;
    public static final int PACKAGE_END = (int)0x00;
    public static final int PACKAGE_SIZE = 8;

    // 外设定义
    // BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
    public static final int BMP280_BASE = (int)0x76;
    public static final int BMP280_TEMP_XLSB = (int)0x00000172;
    public static final int BMP280_TEMP_LSB = (int)0x00000171;
    public static final int BMP280_TEMP_MSB = (int)0x00000170;
    public static final int BMP280_PRESS_XLSB = (int)0x0000016F;
    public static final int BMP280_PRESS_LSB = (int)0x0000016E;
    public static final int BMP280_PRESS_MSB = (int)0x0000016D;
    public static final int BMP280_CONFIG = (int)0x0000016B;
    public static final int BMP280_CONFIG_T_SB = 5;  // Standby time in normal mode
    public static final int BMP280_CONFIG_FILTER = 2;  // Filter coefficient
    public static final int BMP280_CONFIG_SPI3W_EN = 0;  // Enable 3-wire SPI
    public static final int BMP280_CTRL_MEAS = (int)0x0000016A;
    public static final int BMP280_CTRL_MEAS_MODE = 0;  // 0=sleep, 1/2=forced, 3=normal
    public static final int BMP280_CTRL_MEAS_OSRS_P = 2;  // Pressure oversampling
    public static final int BMP280_CTRL_MEAS_OSRS_T = 5;  // Temperature oversampling
    public static final int BMP280_STATUS = (int)0x00000169;
    public static final int BMP280_STATUS_IM_UPDATE = 0;  // 1=Image register update in progress
    public static final int BMP280_STATUS_MEASURING = 3;  // 1=Conversion is running
    public static final int BMP280_CHIP_ID = (int)0x00000146;
    public static final int BMP280_RESET = (int)0x00000156;

    public static native void bmp280_init();
}
