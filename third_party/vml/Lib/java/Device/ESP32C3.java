package vml.device.espressif.esp32_c3;

/**
 * ESP32-C3 寄存器定义
 * 生成自: Espressif/ESP32-C/ESP32-C3
 * 版本: 1.0
 */
public final class ESP32_C3 {
    private ESP32_C3() {} // 工具类
    // CPU架构: RISC-V, 32位, 160000000 Hz

    // 寄存器定义
    // Return Address
    public static final int X1_ADDR = (int)0x04;

    // Stack Pointer (SP)
    public static final int X2_ADDR = (int)0x08;

    // Global Pointer (GP)
    public static final int X3_ADDR = (int)0x0C;

    // Frame Pointer (FP)
    public static final int X8_ADDR = (int)0x20;

    // Function Argument (A0)
    public static final int X10_ADDR = (int)0x28;

    // Function Argument (A1)
    public static final int X11_ADDR = (int)0x2C;

    // Program Counter
    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    // Flash via Cache
    public static final int FLASH_START = (int)0x42000000;
    public static final int FLASH_END = (int)0x427FFFFF;
    public static final int FLASH_SIZE = 8388608;

    // Internal SRAM
    public static final int SRAM_START = (int)0x3FC80000;
    public static final int SRAM_END = (int)0x3FCE3FFF;
    public static final int SRAM_SIZE = 409600;

    public static final int PERIPHERAL_START = (int)0x60000000;
    public static final int PERIPHERAL_END = (int)0x600FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // 外设定义
    // General Purpose I/O
    public static final int GPIO_BASE = (int)0x60004000;
    public static final int GPIO_OUT = (int)0x60004004;
    public static final int GPIO_OUT_W1TS = (int)0x60004008;
    public static final int GPIO_OUT_W1TC = (int)0x6000400C;
    public static final int GPIO_IN = (int)0x60004010;
    public static final int GPIO_ENABLE = (int)0x60004020;
    public static final int GPIO_ENABLE_W1TS = (int)0x60004024;
    public static final int GPIO_ENABLE_W1TC = (int)0x60004028;

    // I/O MUX
    public static final int IO_MUX_BASE = (int)0x60009000;
    public static final int IO_MUX_GPIO0 = (int)0x60009000;
    public static final int IO_MUX_GPIO1 = (int)0x60009004;
    public static final int IO_MUX_GPIO2 = (int)0x60009008;
    public static final int IO_MUX_GPIO3 = (int)0x6000900C;

    // RTC Control
    public static final int RTC_CNTL_BASE = (int)0x60008000;
    public static final int RTC_CNTL_OPTIONS0 = (int)0x60008000;
    public static final int RTC_CNTL_CLK_CONF = (int)0x60008030;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_MACHINESOFTWARE = 3;  // 
    public static final int IRQ_MACHINETIMER = 7;  // 
    public static final int IRQ_MACHINEEXTERNAL = 11;  // 

    public static native void esp32_c3_init();
}
