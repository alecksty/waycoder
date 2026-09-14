package vml.device.tinxp._74hc595;

/**
 * 74HC595 寄存器定义
 * 生成自: TI/NXP/GPIO/74HC595
 * 版本: 1.0
 */
public final class 74HC595 {
    private 74HC595() {} // 工具类
    // CPU架构: GPIO, 8位, 10000000 Hz

    // 外设定义
    // 74HC595 8-bit Shift Register (2V-6V, DIP-16)
    public static final int _74HC595_BASE = (int)0x00;
    public static final int _74HC595_DATA = (int)0x00000000;
    public static final int _74HC595_LATCH = (int)0x00000001;
    public static final int _74HC595_CHAIN_COUNT = (int)0x00000002;
    public static final int _74HC595_OE = (int)0x00000003;
    public static final int _74HC595_CLEAR = (int)0x00000004;

    public static native void _74hc595_init();
}
