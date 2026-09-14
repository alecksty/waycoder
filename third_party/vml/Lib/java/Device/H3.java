package vml.device.allwinner.allwinner_h3;

/**
 * Allwinner H3 寄存器定义
 * 生成自: Allwinner/H-Series/Allwinner H3
 * 版本: 1.0
 */
public final class Allwinner H3 {
    private Allwinner H3() {} // 工具类
    // CPU架构: ARM-Cortex-A7, 32位, 1200000000 Hz

    // 外设定义
    // UART 0 (debug console)
    public static final int UART0_BASE = (int)0x01C28000;
    public static final int UART0_RBR = (int)0x01C28000;
    public static final int UART0_THR = (int)0x01C28000;
    public static final int UART0_IER = (int)0x01C28004;
    public static final int UART0_IIR = (int)0x01C28008;
    public static final int UART0_FCR = (int)0x01C28008;
    public static final int UART0_LCR = (int)0x01C2800C;
    public static final int UART0_MCR = (int)0x01C28010;
    public static final int UART0_LSR = (int)0x01C28014;
    public static final int UART0_MSR = (int)0x01C28018;
    public static final int UART0_DLL = (int)0x01C28000;
    public static final int UART0_DLH = (int)0x01C28004;

    // UART 1
    public static final int UART1_BASE = (int)0x01C28400;
    public static final int UART1_RBR = (int)0x01C28400;
    public static final int UART1_THR = (int)0x01C28400;
    public static final int UART1_LSR = (int)0x01C28414;

    // GPIO 控制器
    public static final int GPIO_BASE = (int)0x01C20800;
    public static final int GPIO_PA_CFG0 = (int)0x01C20800;
    public static final int GPIO_PA_CFG1 = (int)0x01C20804;
    public static final int GPIO_PA_DAT = (int)0x01C20810;
    public static final int GPIO_PA_DRV0 = (int)0x01C20814;
    public static final int GPIO_PA_PUL0 = (int)0x01C2081C;
    public static final int GPIO_PB_CFG0 = (int)0x01C20824;
    public static final int GPIO_PB_DAT = (int)0x01C20834;
    public static final int GPIO_PC_CFG0 = (int)0x01C20848;
    public static final int GPIO_PC_DAT = (int)0x01C20858;

    // AVS 定时器
    public static final int TIMER_BASE = (int)0x01C20C00;
    public static final int TIMER_CNT0 = (int)0x01C20C00;
    public static final int TIMER_CNT1 = (int)0x01C20C04;
    public static final int TIMER_CTRL = (int)0x01C20C08;
    public static final int TIMER_INTV = (int)0x01C20C0C;

    // 时钟控制单元
    public static final int CCU_BASE = (int)0x01C20000;
    public static final int CCU_PLL1_CFG = (int)0x01C20000;
    public static final int CCU_PLL3_CFG = (int)0x01C20010;
    public static final int CCU_CPU_AXI_CFG = (int)0x01C20050;
    public static final int CCU_AHB1_APB1_CFG = (int)0x01C20054;
    public static final int CCU_APB2_CFG = (int)0x01C20058;
    public static final int CCU_BUS_GATE0 = (int)0x01C20060;
    public static final int CCU_BUS_GATE1 = (int)0x01C20064;
    public static final int CCU_BUS_GATE2 = (int)0x01C20068;

    public static native void allwinner_h3_init();
}
