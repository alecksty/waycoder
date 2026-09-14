package vml.device.hitachi.hd44780;

/**
 * HD44780 寄存器定义
 * 生成自: Hitachi/Display/HD44780
 * 版本: 1.0
 */
public final class HD44780 {
    private HD44780() {} // 工具类
    // CPU架构: Display, 8位, 0 Hz

    // 内存段定义
    // Display Data RAM (80 bytes, 2 lines)
    public static final int DDRAM_START = (int)0x00;
    public static final int DDRAM_END = (int)0x4F;
    public static final int DDRAM_SIZE = 80;

    // Character Generator RAM (8 custom chars x 8 bytes)
    public static final int CGRAM_START = (int)0x00;
    public static final int CGRAM_END = (int)0x3F;
    public static final int CGRAM_SIZE = 64;

    // 外设定义
    // HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
    public static final int HD44780_BASE = (int)0x27;
    public static final int HD44780_CMD = (int)0x00000027;
    public static final int HD44780_DATA = (int)0x00000028;
    public static final int HD44780_CTRL_RS = (int)0x00000027;
    public static final int HD44780_CTRL_RW = (int)0x00000028;
    public static final int HD44780_CTRL_EN = (int)0x00000029;
    public static final int HD44780_CTRL_BL = (int)0x0000002A;
    public static final int HD44780_ADDR_DDRAM = (int)0x000000A7;
    public static final int HD44780_ADDR_CGRAM = (int)0x00000067;

    public static native void hd44780_init();
}
