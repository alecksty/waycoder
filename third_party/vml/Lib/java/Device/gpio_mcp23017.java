package vml.device.microchip.mcp23017;

/**
 * MCP23017 寄存器定义
 * 生成自: Microchip/GPIO/MCP23017
 * 版本: 1.0
 */
public final class MCP23017 {
    private MCP23017() {} // 工具类
    // CPU架构: GPIO, 16位, 400000 Hz

    // 外设定义
    // MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
    public static final int MCP23017_BASE = (int)0x20;
    public static final int MCP23017_IODIRA = (int)0x00000020;
    public static final int MCP23017_IODIRB = (int)0x00000021;
    public static final int MCP23017_GPIOA = (int)0x00000032;
    public static final int MCP23017_GPIOB = (int)0x00000033;
    public static final int MCP23017_GPINTENA = (int)0x00000024;
    public static final int MCP23017_GPINTENB = (int)0x00000025;
    public static final int MCP23017_INTCONA = (int)0x00000028;
    public static final int MCP23017_IOCON = (int)0x0000002A;
    public static final int MCP23017_GPPUA = (int)0x0000002C;
    public static final int MCP23017_GPPUB = (int)0x0000002D;

    // 中断向量定义
    public static final int IRQ_INTA = 0;  // Port A interrupt
    public static final int IRQ_INTB = 1;  // Port B interrupt

    public static native void mcp23017_init();
}
