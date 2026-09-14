package vml.device.generic._24c64;

/**
 * 24C64 寄存器定义
 * 生成自: Generic/Memory/24C64
 * 版本: 1.0
 */
public final class 24C64 {
    private 24C64() {} // 工具类
    // CPU架构: Memory, 8位, 400000 Hz

    // 内存段定义
    // EEPROM main memory array (8KB, 32-byte page write)
    public static final int EEPROM_START = (int)0x00;
    public static final int EEPROM_END = (int)0x1FFF;
    public static final int EEPROM_SIZE = 8192;

    // 外设定义
    // 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
    public static final int _24C64_BASE = (int)0x50;
    public static final int _24C64_ADDR_H = (int)0x00000050;
    public static final int _24C64_ADDR_L = (int)0x00000051;
    public static final int _24C64_DATA = (int)0x00000052;
    public static final int _24C64_PAGE_SIZE = (int)0x0000014E;
    public static final int _24C64_SIZE = (int)0x0000014D;

    public static native void _24c64_init();
}
