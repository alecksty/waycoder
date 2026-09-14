package vml.device.sitronix.st7735;

/**
 * ST7735 寄存器定义
 * 生成自: Sitronix/Display/ST7735
 * 版本: 1.0
 */
public final class ST7735 {
    private ST7735() {} // 工具类
    // CPU架构: Display, 16位, 16000000 Hz

    // 内存段定义
    // Graphics RAM (128x160x16bit)
    public static final int GRAM_START = (int)0x00;
    public static final int GRAM_END = (int)0x4FFF;
    public static final int GRAM_SIZE = 20480;

    // 外设定义
    // ST7735 128x160 TFT (SPI, 3.3V-5V)
    public static final int ST7735_BASE = (int)0x00;
    public static final int ST7735_CMD = (int)0x00000000;
    public static final int ST7735_DATA = (int)0x00000001;
    public static final int ST7735_COL_START = (int)0x0000002A;
    public static final int ST7735_ROW_START = (int)0x0000002B;
    public static final int ST7735_WRITE_RAM = (int)0x0000002C;
    public static final int ST7735_MADCTL = (int)0x00000036;
    public static final int ST7735_COLMOD = (int)0x0000003A;
    public static final int ST7735_INVON = (int)0x00000021;
    public static final int ST7735_SLEEP_OUT = (int)0x00000011;
    public static final int ST7735_DISP_ON = (int)0x00000029;

    public static native void st7735_init();
}
