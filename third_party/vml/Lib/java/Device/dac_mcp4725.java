package vml.device.microchip.mcp4725;

/**
 * MCP4725 寄存器定义
 * 生成自: Microchip/DAC/MCP4725
 * 版本: 1.0
 */
public final class MCP4725 {
    private MCP4725() {} // 工具类
    // CPU架构: DAC, 12位, 400000 Hz

    // 内存段定义
    // Power-on default DAC value
    public static final int EEPROM_START = (int)0x00;
    public static final int EEPROM_END = (int)0x01;
    public static final int EEPROM_SIZE = 2;

    // 外设定义
    // MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
    public static final int MCP4725_BASE = (int)0x60;
    public static final int MCP4725_DAC_VALUE = (int)0x00000060;
    public static final int MCP4725_DAC_VALUE_PD = 12;  // Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
    public static final int MCP4725_WRITE_EEPROM = (int)0x000000C0;

    public static native void mcp4725_init();
}
