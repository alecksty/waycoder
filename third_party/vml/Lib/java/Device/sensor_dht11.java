package vml.device.aosong.dht11;

/**
 * DHT11 寄存器定义
 * 生成自: Aosong/Sensor/DHT11
 * 版本: 1.0
 */
public final class DHT11 {
    private DHT11() {} // 工具类
    // CPU架构: Sensor, 8位, 500000 Hz

    // 内存段定义
    // DIP-4/SMD-4
    public static final int PACKAGE_START = (int)0x00;
    public static final int PACKAGE_END = (int)0x00;
    public static final int PACKAGE_SIZE = 4;

    // 外设定义
    // DHT11 1-Wire Sensor (3.0V-5.5V)
    public static final int DHT11_BASE = (int)0x00;
    public static final int DHT11_HUMIDITY_INT = (int)0x00000000;
    public static final int DHT11_HUMIDITY_DEC = (int)0x00000001;
    public static final int DHT11_TEMP_INT = (int)0x00000002;
    public static final int DHT11_TEMP_DEC = (int)0x00000003;
    public static final int DHT11_CHECKSUM = (int)0x00000004;

    public static native void dht11_init();
}
