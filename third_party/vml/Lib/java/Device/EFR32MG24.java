package vml.device.siliconlabs.efr32mg24;

/**
 * EFR32MG24 寄存器定义
 * 生成自: Silicon Labs/EFR32/EFR32MG24
 * 版本: 1.0
 */
public final class EFR32MG24 {
    private EFR32MG24() {} // 工具类
    // CPU架构: ARM-Cortex-M33, 32位, 78000000 Hz

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
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x0817FFFF;
    public static final int FLASH_SIZE = 1572864;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x2003FFFF;
    public static final int SRAM_SIZE = 262144;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4007FFFF;
    public static final int PERIPHERAL_SIZE = 524288;

    // 外设定义
    // Clock Management Unit
    public static final int CMU_BASE = (int)0x40080000;
    public static final int CMU_CTRL = (int)0x40080000;
    public static final int CMU_HFCORECLKCFG = (int)0x40080008;
    public static final int CMU_HFPERCLKEN0 = (int)0x40080010;
    public static final int CMU_HFPERCLKEN0_GPIOEN = 4;  // GPIO clock enable
    public static final int CMU_HFPERCLKEN0_USART0EN = 12;  // USART0 clock enable
    public static final int CMU_HFPERCLKEN0_USART1EN = 13;  // USART1 clock enable
    public static final int CMU_LFBCLKEN0 = (int)0x40080020;

    // GPIO Controller
    public static final int GPIO_BASE = (int)0x40088000;
    public static final int GPIO_PORT_A_CTRL = (int)0x40088000;
    public static final int GPIO_PORT_B_CTRL = (int)0x40088004;
    public static final int GPIO_PORT_C_CTRL = (int)0x40088008;
    public static final int GPIO_PORT_D_CTRL = (int)0x4008800C;
    public static final int GPIO_MODEL = (int)0x40088010;
    public static final int GPIO_MODEH = (int)0x40088014;
    public static final int GPIO_DOUT = (int)0x4008801C;
    public static final int GPIO_DOUTSET = (int)0x40088020;
    public static final int GPIO_DOUTCLR = (int)0x40088024;
    public static final int GPIO_DOUTTGL = (int)0x40088028;
    public static final int GPIO_DIN = (int)0x4008802C;

    // GPIO Port A extended
    public static final int GPIO_PA_BASE = (int)0x40088400;
    public static final int GPIO_PA_PA_CFG = (int)0x40088400;
    public static final int GPIO_PA_PA_PINOUT = (int)0x40088404;

    // GPIO Port B extended
    public static final int GPIO_PB_BASE = (int)0x40088800;
    public static final int GPIO_PB_PB_CFG = (int)0x40088800;

    // USART 0
    public static final int USART0_BASE = (int)0x40060000;
    public static final int USART0_CTRL = (int)0x40060000;
    public static final int USART0_CMD = (int)0x40060004;
    public static final int USART0_STATUS = (int)0x40060008;
    public static final int USART0_RXDATA = (int)0x4006000C;
    public static final int USART0_TXDATA = (int)0x40060010;
    public static final int USART0_CLKDIV = (int)0x40060014;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_USART0_RX = 12;  // USART0 Receive Interrupt
    public static final int IRQ_USART0_TX = 13;  // USART0 Transmit Interrupt

    public static native void efr32mg24_init();
}
