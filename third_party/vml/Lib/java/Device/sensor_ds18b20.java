package vml.device.maximdallas.ds18b20;

/**
 * DS18B20 寄存器定义
 * 生成自: Maxim/Dallas/Sensor/DS18B20
 * 版本: 1.0
 */
public final class DS18B20 {
    private DS18B20() {} // 工具类
    // CPU架构: Sensor, 8位, 100000 Hz

    // 内存段定义
    // Scratchpad memory (9 bytes)
    public static final int SCRATCHPAD_START = (int)0x00;
    public static final int SCRATCHPAD_END = (int)0x08;
    public static final int SCRATCHPAD_SIZE = 9;

    // EEPROM (TH, TL, config bytes)
    public static final int EEPROM_START = (int)0x00;
    public static final int EEPROM_END = (int)0x02;
    public static final int EEPROM_SIZE = 3;

    // 外设定义
    // DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
    public static final int DS18B20_BASE = (int)0x00;
    public static final int DS18B20_TEMP_LSB = (int)0x00000000;
    public static final int DS18B20_TEMP_MSB = (int)0x00000001;
    public static final int DS18B20_TH_REG = (int)0x00000002;
    public static final int DS18B20_TL_REG = (int)0x00000003;
    public static final int DS18B20_CONFIG = (int)0x00000004;
    public static final int DS18B20_CONFIG_R0 = 5;  // Resolution select bit 0
    public static final int DS18B20_CONFIG_R1 = 6;  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
    public static final int DS18B20_COUNT_REMAIN = (int)0x00000006;
    public static final int DS18B20_COUNT_PER_C = (int)0x00000007;
    public static final int DS18B20_CRC = (int)0x00000008;

    public static native void ds18b20_init();
}
