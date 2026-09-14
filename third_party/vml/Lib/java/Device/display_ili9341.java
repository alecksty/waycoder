package vml.device.ilitek.ili9341;

/**
 * ILI9341 寄存器定义
 * 生成自: Ilitek/Display/ILI9341
 * 版本: 1.0
 */
public final class ILI9341 {
    private ILI9341() {} // 工具类
    // CPU架构: Display, 18位, 20000000 Hz

    // 内存段定义
    // Graphics RAM (240x320x18bit)
    public static final int GRAM_START = (int)0x00;
    public static final int GRAM_END = (int)0xBCFF;
    public static final int GRAM_SIZE = 156672;

    // 外设定义
    // ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
    public static final int ILI9341_BASE = (int)0x00;
    public static final int ILI9341_CMD = (int)0x00000000;
    public static final int ILI9341_DATA = (int)0x00000001;
    public static final int ILI9341_COL_START = (int)0x0000002A;
    public static final int ILI9341_PAGE_START = (int)0x0000002B;
    public static final int ILI9341_WRITE_RAM = (int)0x0000002C;
    public static final int ILI9341_MADCTL = (int)0x00000036;
    public static final int ILI9341_PIXFMT = (int)0x0000003A;
    public static final int ILI9341_FRMCTL = (int)0x000000B1;
    public static final int ILI9341_GAMMA = (int)0x00000026;
    public static final int ILI9341_SLEEP_OUT = (int)0x00000011;
    public static final int ILI9341_DISP_ON = (int)0x00000029;

    public static native void ili9341_init();
}
