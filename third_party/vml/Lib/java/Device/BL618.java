package vml.device.bouffalolab.bl618;

/**
 * BL618 寄存器定义
 * 生成自: Bouffalo Lab/BL6/BL618
 * 版本: 1.0
 */
public final class BL618 {
    private BL618() {} // 工具类
    // CPU架构: RISC-V, 32位, 320000000 Hz

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
    public static final int FLASH_START = (int)0x20000000;
    public static final int FLASH_END = (int)0x203FFFFF;
    public static final int FLASH_SIZE = 4194304;

    public static final int SRAM_HPSYS_START = (int)0x22000000;
    public static final int SRAM_HPSYS_END = (int)0x22003FFF;
    public static final int SRAM_HPSYS_SIZE = 16384;

    // DTCM
    public static final int SRAM_DTCM_START = (int)0x22010000;
    public static final int SRAM_DTCM_END = (int)0x22017FFF;
    public static final int SRAM_DTCM_SIZE = 32768;

    public static final int SRAM_SYS_START = (int)0x22020000;
    public static final int SRAM_SYS_END = (int)0x2208FFFF;
    public static final int SRAM_SYS_SIZE = 458752;

    public static final int PERIPHERAL_START = (int)0x30000000;
    public static final int PERIPHERAL_END = (int)0x300FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // 外设定义
    // Global Control (Clock and Reset)
    public static final int GLB_BASE = (int)0x30000000;
    public static final int GLB_GLB_CLK_EN = (int)0x30000010;
    public static final int GLB_GLB_CLK_EN_GPIO_CLK_EN = 6;  // GPIO clock enable
    public static final int GLB_GLB_CLK_EN_UART0_CLK_EN = 12;  // UART0 clock enable
    public static final int GLB_GLB_SYS_CLK_CTRL = (int)0x30000014;
    public static final int GLB_GLB_PLL_CTRL = (int)0x3000001C;

    // GPIO Port A
    public static final int GPIO_P0_BASE = (int)0x30007000;
    public static final int GPIO_P0_GPIO_CFG0 = (int)0x30007000;
    public static final int GPIO_P0_GPIO_CFG1 = (int)0x30007004;
    public static final int GPIO_P0_GPIO_OE = (int)0x30007008;
    public static final int GPIO_P0_GPIO_OUT = (int)0x3000700C;
    public static final int GPIO_P0_GPIO_IN = (int)0x30007010;
    public static final int GPIO_P0_GPIO_SET = (int)0x30007014;
    public static final int GPIO_P0_GPIO_CLR = (int)0x30007018;
    public static final int GPIO_P0_GPIO_TOG = (int)0x3000701C;

    // GPIO Port B
    public static final int GPIO_P1_BASE = (int)0x30007200;
    public static final int GPIO_P1_GPIO_CFG0 = (int)0x30007200;
    public static final int GPIO_P1_GPIO_CFG1 = (int)0x30007204;
    public static final int GPIO_P1_GPIO_OE = (int)0x30007208;
    public static final int GPIO_P1_GPIO_OUT = (int)0x3000720C;
    public static final int GPIO_P1_GPIO_IN = (int)0x30007210;
    public static final int GPIO_P1_GPIO_SET = (int)0x30007214;
    public static final int GPIO_P1_GPIO_CLR = (int)0x30007218;
    public static final int GPIO_P1_GPIO_TOG = (int)0x3000721C;

    // UART 0
    public static final int UART0_BASE = (int)0x30002000;
    public static final int UART0_UART_CR = (int)0x30002000;
    public static final int UART0_UART_BRR = (int)0x30002004;
    public static final int UART0_UART_TDR = (int)0x30002008;
    public static final int UART0_UART_RDR = (int)0x3000200C;
    public static final int UART0_UART_SR = (int)0x30002010;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_MACHINESOFTWARE = 3;  // 
    public static final int IRQ_MACHINETIMER = 7;  // 
    public static final int IRQ_MACHINEEXTERNAL = 11;  // 
    public static final int IRQ_UART0 = 20;  // UART0 Interrupt

    public static native void bl618_init();
}
