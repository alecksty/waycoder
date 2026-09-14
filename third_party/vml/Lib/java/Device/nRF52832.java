package vml.device.nordic.nrf52832;

/**
 * nRF52832 寄存器定义
 * 生成自: Nordic/nRF52/nRF52832
 * 版本: 1.0
 */
public final class nRF52832 {
    private nRF52832() {} // 工具类
    // CPU架构: ARM-Cortex-M4F, 32位, 64000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x04;

    public static final int R2_ADDR = (int)0x08;

    public static final int R3_ADDR = (int)0x0C;

    public static final int SP_ADDR = (int)0x34;

    public static final int LR_ADDR = (int)0x38;

    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    public static final int FLASH_START = (int)0x00000000;
    public static final int FLASH_END = (int)0x0007FFFF;
    public static final int FLASH_SIZE = 524288;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x2000FFFF;
    public static final int SRAM_SIZE = 65536;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x400FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // Factory Information Configuration Registers
    public static final int FICR_START = (int)0x10000000;
    public static final int FICR_END = (int)0x10000FFF;
    public static final int FICR_SIZE = 4096;

    // 外设定义
    // General Purpose I/O Port 0
    public static final int GPIO_P0_BASE = (int)0x50000000;
    public static final int GPIO_P0_OUT = (int)0x50000504;
    public static final int GPIO_P0_OUTSET = (int)0x50000508;
    public static final int GPIO_P0_OUTCLR = (int)0x5000050C;
    public static final int GPIO_P0_IN = (int)0x50000510;
    public static final int GPIO_P0_DIR = (int)0x50000514;
    public static final int GPIO_P0_DIRSET = (int)0x50000518;
    public static final int GPIO_P0_DIRCLR = (int)0x5000051C;

    // Power Control
    public static final int POWER_BASE = (int)0x40000000;
    public static final int POWER_DCDCEN = (int)0x400001C4;
    public static final int POWER_RAMSTATUS = (int)0x40000268;

    // Clock Control
    public static final int CLOCK_BASE = (int)0x40000000;
    public static final int CLOCK_HFCLKSTART = (int)0x40000108;
    public static final int CLOCK_HFCLKSTARTED = (int)0x40000208;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 

    public static native void nrf52832_init();
}
