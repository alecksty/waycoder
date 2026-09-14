package vml.device.microchip.mcp3008;

/**
 * MCP3008 寄存器定义
 * 生成自: Microchip/ADC/MCP3008
 * 版本: 1.0
 */
public final class MCP3008 {
    private MCP3008() {} // 工具类
    // CPU架构: ADC, 10位, 1350000 Hz

    // 外设定义
    // MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)
    public static final int MCP3008_BASE = (int)0x00;
    public static final int MCP3008_CH0 = (int)0x00000000;
    public static final int MCP3008_CH1 = (int)0x00000001;
    public static final int MCP3008_CH2 = (int)0x00000002;
    public static final int MCP3008_CH3 = (int)0x00000003;
    public static final int MCP3008_CH4 = (int)0x00000004;
    public static final int MCP3008_CH5 = (int)0x00000005;
    public static final int MCP3008_CH6 = (int)0x00000006;
    public static final int MCP3008_CH7 = (int)0x00000007;
    public static final int MCP3008_DIFF_01 = (int)0x00000008;
    public static final int MCP3008_DIFF_23 = (int)0x00000009;

    public static native void mcp3008_init();
}
