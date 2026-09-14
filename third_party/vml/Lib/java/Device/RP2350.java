package vml.device.raspberry.rp2350;

/**
 * RP2350 寄存器定义
 * 生成自: Raspberry/RP2/RP2350
 * 版本: 1.0
 */
public final class RP2350 {
    private RP2350() {} // 工具类
    // CPU架构: ARM-Cortex-M33, 32位, 150000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x04;

    public static final int R2_ADDR = (int)0x08;

    public static final int R3_ADDR = (int)0x0C;

    public static final int R4_ADDR = (int)0x10;

    public static final int R5_ADDR = (int)0x14;

    public static final int SP_ADDR = (int)0x34;

    public static final int LR_ADDR = (int)0x38;

    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    // XIP Flash
    public static final int FLASH_START = (int)0x10000000;
    public static final int FLASH_END = (int)0x107FFFFF;
    public static final int FLASH_SIZE = 8388608;

    // Total SRAM
    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20081FFF;
    public static final int SRAM_SIZE = 532480;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x5000FFFF;
    public static final int PERIPHERAL_SIZE = 16777216;

    // 外设定义
    // Single-Cycle I/O (GPIO)
    public static final int SIO_BASE = (int)0xD0000000;
    public static final int SIO_GPIO_IN = (int)0xD0000004;
    public static final int SIO_GPIO_OUT = (int)0xD0000010;
    public static final int SIO_GPIO_OUT_SET = (int)0xD0000014;
    public static final int SIO_GPIO_OUT_CLR = (int)0xD0000018;
    public static final int SIO_GPIO_OUT_XOR = (int)0xD000001C;
    public static final int SIO_GPIO_OE = (int)0xD0000020;
    public static final int SIO_GPIO_OE_SET = (int)0xD0000024;
    public static final int SIO_GPIO_OE_CLR = (int)0xD0000028;

    // IO Bank 0 (GPIO control)
    public static final int IO_BANK0_BASE = (int)0x40028000;
    public static final int IO_BANK0_GPIO0_STATUS = (int)0x40028000;
    public static final int IO_BANK0_GPIO0_CTRL = (int)0x40028004;
    public static final int IO_BANK0_GPIO1_STATUS = (int)0x40028008;
    public static final int IO_BANK0_GPIO1_CTRL = (int)0x4002800C;

    // Pad controls for GPIO 0-29
    public static final int PADS_BANK0_BASE = (int)0x4002C000;
    public static final int PADS_BANK0_GPIO0 = (int)0x4002C000;
    public static final int PADS_BANK0_GPIO1 = (int)0x4002C004;

    // Reset Controller
    public static final int RESETS_BASE = (int)0x4000C000;
    public static final int RESETS_RESET = (int)0x4000C000;
    public static final int RESETS_RESET_DONE = (int)0x4000C008;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 

    public static native void rp2350_init();
}
