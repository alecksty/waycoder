package vml.device.microchip.mcp4921;

/**
 * MCP4921 寄存器定义
 * 生成自: Microchip/DAC/MCP4921
 * 版本: 1.0
 */
public final class MCP4921 {
    private MCP4921() {} // 工具类
    // CPU架构: DAC, 12位, 20000000 Hz

    // 外设定义
    // MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
    public static final int MCP4921_BASE = (int)0x00;
    public static final int MCP4921_DAC_VALUE = (int)0x00000000;
    public static final int MCP4921_DAC_VALUE_BUF = 14;  // VREF buffer (0=unbuffered, 1=buffered)
    public static final int MCP4921_DAC_VALUE_GA = 13;  // Gain (0=2x, 1=1x)
    public static final int MCP4921_DAC_VALUE_SHDN = 12;  // Shutdown (0=shutdown, 1=active)
    public static final int MCP4921_VREF = (int)0x00000002;

    public static native void mcp4921_init();
}
