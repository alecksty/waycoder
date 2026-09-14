package vml.device.solomonsystech.ssd1306;

/**
 * SSD1306 寄存器定义
 * 生成自: Solomon Systech/Display/SSD1306
 * 版本: 1.0
 */
public final class SSD1306 {
    private SSD1306() {} // 工具类
    // CPU架构: Display, 8位, 400000 Hz

    // 内存段定义
    // Graphic Display Data RAM (128x64 = 1024 bytes)
    public static final int GDDRAM_START = (int)0x00;
    public static final int GDDRAM_END = (int)0x3FF;
    public static final int GDDRAM_SIZE = 1024;

    // 外设定义
    // SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
    public static final int SSD1306_BASE = (int)0x3C;
    public static final int SSD1306_CMD = (int)0x0000003C;
    public static final int SSD1306_DATA = (int)0x0000007C;
    public static final int SSD1306_DISPLAY_OFF = (int)0x000000EA;
    public static final int SSD1306_DISPLAY_ON = (int)0x000000EB;
    public static final int SSD1306_CONTRAST = (int)0x000000BD;
    public static final int SSD1306_SEG_REMAP = (int)0x000000DD;
    public static final int SSD1306_COM_SCAN = (int)0x00000104;
    public static final int SSD1306_ADDR_MODE = (int)0x0000005C;
    public static final int SSD1306_COL_START = (int)0x0000005D;
    public static final int SSD1306_PAGE_START = (int)0x0000005E;

    public static native void ssd1306_init();
}
