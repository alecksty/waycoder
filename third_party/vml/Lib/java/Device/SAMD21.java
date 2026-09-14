package vml.device.atmelmicrochip.samd21;

/**
 * SAMD21 寄存器定义
 * 生成自: Atmel (Microchip)/SAM D/SAMD21
 * 版本: 
 */
public final class SAMD21 {
    private SAMD21() {} // 工具类
    // CPU架构: ARM Cortex-M0+, 0位, 0 Hz

    // 外设定义
    // Power Manager
    public static final int PM_BASE = (int);
    public static final int PM_PM_CTRL = (int)0x40000400;

    // System Controller
    public static final int SYSCTRL_BASE = (int);
    public static final int SYSCTRL_SYSCTRL_INTENCLR = (int)0x40000800;

    // Generic Clock Generator
    public static final int GCLK_BASE = (int);
    public static final int GCLK_GCLK_CTRL = (int)0x40000C00;

    // Watchdog Timer
    public static final int WDT_BASE = (int);
    public static final int WDT_WDT_CTRL = (int)0x40001000;

    // Real-Time Clock
    public static final int RTC_BASE = (int);
    public static final int RTC_RTC_CTRL = (int)0x40001400;

    // External Interrupt Controller
    public static final int EIC_BASE = (int);
    public static final int EIC_EIC_CTRL = (int)0x40001800;

    // Serial Communication Interface 0
    public static final int SERCOM0_BASE = (int);
    public static final int SERCOM0_SERCOM0_I2CM_CTRLA = (int)0x42000800;

    // Analog-to-Digital Converter
    public static final int ADC_BASE = (int);
    public static final int ADC_ADC_CTRLA = (int)0x42002000;

    // Digital-to-Analog Converter
    public static final int DAC_BASE = (int);
    public static final int DAC_DAC_CTRLA = (int)0x42002400;

    // General Purpose I/O
    public static final int PORT_BASE = (int);
    public static final int PORT_PORT_DIR = (int)0x41004400;

    // Timer/Counter 0
    public static final int TC0_BASE = (int);
    public static final int TC0_TC0_CTRLA = (int)0x42002800;

    // USB Device Controller
    public static final int USB_BASE = (int);
    public static final int USB_USB_CTRLA = (int)0x41005000;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Reset vector
    public static final int IRQ_NONMASKABLEINT = 1;  // Non-maskable interrupt
    public static final int IRQ_HARDFAULT = 2;  // Hard fault
    public static final int IRQ_SVCALL = 3;  // Supervisor call
    public static final int IRQ_PENDSV = 4;  // Pendable service call
    public static final int IRQ_SYSTICK = 5;  // System tick timer
    public static final int IRQ_PM = 6;  // Power Manager
    public static final int IRQ_SYSCTRL = 7;  // System Controller
    public static final int IRQ_WDT = 8;  // Watchdog Timer
    public static final int IRQ_RTC = 9;  // Real-Time Clock
    public static final int IRQ_EIC = 10;  // External Interrupt Controller
    public static final int IRQ_NVMCTRL = 11;  // Non-Volatile Memory Controller
    public static final int IRQ_DMAC = 12;  // Direct Memory Access Controller
    public static final int IRQ_USB = 13;  // USB Device Controller
    public static final int IRQ_EVSYS = 14;  // Event System
    public static final int IRQ_SERCOM0 = 15;  // Serial Communication Interface 0
    public static final int IRQ_SERCOM1 = 16;  // Serial Communication Interface 1
    public static final int IRQ_SERCOM2 = 17;  // Serial Communication Interface 2
    public static final int IRQ_SERCOM3 = 18;  // Serial Communication Interface 3
    public static final int IRQ_SERCOM4 = 19;  // Serial Communication Interface 4
    public static final int IRQ_SERCOM5 = 20;  // Serial Communication Interface 5
    public static final int IRQ_TCC0 = 21;  // Timer/Counter for Control 0
    public static final int IRQ_TCC1 = 22;  // Timer/Counter for Control 1
    public static final int IRQ_TCC2 = 23;  // Timer/Counter for Control 2
    public static final int IRQ_TC3 = 24;  // Timer/Counter 3
    public static final int IRQ_TC4 = 25;  // Timer/Counter 4
    public static final int IRQ_TC5 = 26;  // Timer/Counter 5
    public static final int IRQ_TC6 = 27;  // Timer/Counter 6
    public static final int IRQ_TC7 = 28;  // Timer/Counter 7
    public static final int IRQ_ADC = 29;  // Analog-to-Digital Converter
    public static final int IRQ_AC = 30;  // Analog Comparator
    public static final int IRQ_DAC = 31;  // Digital-to-Analog Converter
    public static final int IRQ_PTC = 32;  // Peripheral Touch Controller
    public static final int IRQ_I2S = 33;  // Inter-IC Sound Interface

    public static native void samd21_init();
}
