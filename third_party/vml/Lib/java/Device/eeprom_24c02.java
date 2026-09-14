package vml.device.generic._24c02;

/**
 * 24C02 寄存器定义
 * 生成自: Generic/Memory/24C02
 * 版本: 1.0
 */
public final class 24C02 {
    private 24C02() {} // 工具类
    // CPU架构: Memory, 8位, 400000 Hz

    // 内存段定义
    // EEPROM main memory array (256 bytes, 8-byte page write)
    public static final int EEPROM_START = (int)0x00;
    public static final int EEPROM_END = (int)0xFF;
    public static final int EEPROM_SIZE = 256;

    // 外设定义
    // 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
    public static final int _24C02_BASE = (int)0x50;
    public static final int _24C02_STATUS = (int)0x0000014F;
    public static final int _24C02_STATUS_BUSY = 0;  // 1=Write in progress
    public static final int _24C02_PAGE_SIZE = (int)0x0000014E;
    public static final int _24C02_SIZE = (int)0x0000014D;

    public static native void _24c02_init();
}
