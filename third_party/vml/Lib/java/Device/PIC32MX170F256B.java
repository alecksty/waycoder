package vml.device.microchip.pic32mx170f256b;

/**
 * PIC32MX170F256B 寄存器定义
 * 生成自: Microchip/PIC32/PIC32MX170F256B
 * 版本: 1.0
 */
public final class PIC32MX170F256B {
    private PIC32MX170F256B() {} // 工具类
    // CPU架构: MIPS32-M4K, 32位, 50000000 Hz

    // 寄存器定义
    // Hard-wired zero
    public static final int _0_ADDR = (int)0x00;

    // AT
    public static final int _1_ADDR = (int)0x04;

    // V0
    public static final int _2_ADDR = (int)0x08;

    // V1
    public static final int _3_ADDR = (int)0x0C;

    // A0
    public static final int _4_ADDR = (int)0x10;

    // A1
    public static final int _5_ADDR = (int)0x14;

    // Stack Pointer (SP)
    public static final int _29_ADDR = (int)0x74;

    // Return Address (RA)
    public static final int _31_ADDR = (int)0x7C;

    // Program Counter
    public static final int PC_ADDR = (int)0x80;

    // 内存段定义
    // Program Flash
    public static final int FLASH_START = (int)0x9D000000;
    public static final int FLASH_END = (int)0x9D03FFFF;
    public static final int FLASH_SIZE = 262144;

    public static final int SRAM_START = (int)0xA0000000;
    public static final int SRAM_END = (int)0xA000FFFF;
    public static final int SRAM_SIZE = 65536;

    public static final int PERIPHERAL_START = (int)0xBF800000;
    public static final int PERIPHERAL_END = (int)0xBF8FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // Boot Flash
    public static final int BOOTFLASH_START = (int)0xBFC00000;
    public static final int BOOTFLASH_END = (int)0xBFC02FFF;
    public static final int BOOTFLASH_SIZE = 12288;

    // 外设定义
    // General Purpose I/O Port A
    public static final int PORTA_BASE = (int)0xBF886000;
    public static final int PORTA_TRISA = (int)0xBF886000;
    public static final int PORTA_PORTA = (int)0xBF886010;
    public static final int PORTA_LATA = (int)0xBF886020;
    public static final int PORTA_ODCA = (int)0xBF886030;

    // General Purpose I/O Port B
    public static final int PORTB_BASE = (int)0xBF886100;
    public static final int PORTB_TRISB = (int)0xBF886100;
    public static final int PORTB_PORTB = (int)0xBF886110;
    public static final int PORTB_LATB = (int)0xBF886120;
    public static final int PORTB_ODCB = (int)0xBF886130;

    // UART1
    public static final int UART1_BASE = (int)0xBF822000;
    public static final int UART1_UXMODE = (int)0xBF822000;
    public static final int UART1_UXSTA = (int)0xBF822004;
    public static final int UART1_UXTXREG = (int)0xBF822008;
    public static final int UART1_UXRXREG = (int)0xBF82200C;
    public static final int UART1_UXBRG = (int)0xBF822010;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_UART1 = 8;  // UART1 Interrupt

    public static native void pic32mx170f256b_init();
}
