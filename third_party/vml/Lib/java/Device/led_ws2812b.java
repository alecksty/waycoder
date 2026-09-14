package vml.device.worldsemi.ws2812b;

/**
 * WS2812B 寄存器定义
 * 生成自: Worldsemi/LED/WS2812B
 * 版本: 1.0
 */
public final class WS2812B {
    private WS2812B() {} // 工具类
    // CPU架构: LED, 24位, 800000 Hz

    // 内存段定义
    // Frame buffer (up to 256 LEDs × 3 bytes)
    public static final int LED_FB_START = (int)0x00;
    public static final int LED_FB_END = (int)0xFF;
    public static final int LED_FB_SIZE = 256;

    // 外设定义
    // WS2812B RGB LED Strip (5V, 60mA/led)
    public static final int WS2812B_BASE = (int)0x00;
    public static final int WS2812B_LED_COUNT = (int)0x00000000;
    public static final int WS2812B_LED_DATA = (int)0x00000002;
    public static final int WS2812B_BRIGHTNESS = (int)0x00000005;
    public static final int WS2812B_SHOW = (int)0x00000006;
    public static final int WS2812B_CLEAR = (int)0x00000007;

    public static native void ws2812b_init();
}
