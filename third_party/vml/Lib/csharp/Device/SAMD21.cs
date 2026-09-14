using System;

namespace VML.Device.Atmel(Microchip).SAMD21
{
    /// <summary>
    /// SAMD21 寄存器定义
    /// 生成自: Atmel (Microchip)/SAM D/SAMD21
    /// 版本: 
    /// </summary>
    public static class SAMD21
    {
        // CPU架构: ARM Cortex-M0+, 0位, 0 Hz

        // 外设定义
        // Power Manager
        public const int PM_BASE = ;
        public static unsafe ulong* PM_PM_CTRL => (ulong*)0x40000400;

        // System Controller
        public const int SYSCTRL_BASE = ;
        public static unsafe uint* SYSCTRL_SYSCTRL_INTENCLR => (uint*)0x40000800;

        // Generic Clock Generator
        public const int GCLK_BASE = ;
        public static unsafe ulong* GCLK_GCLK_CTRL => (ulong*)0x40000C00;

        // Watchdog Timer
        public const int WDT_BASE = ;
        public static unsafe ulong* WDT_WDT_CTRL => (ulong*)0x40001000;

        // Real-Time Clock
        public const int RTC_BASE = ;
        public static unsafe uint* RTC_RTC_CTRL => (uint*)0x40001400;

        // External Interrupt Controller
        public const int EIC_BASE = ;
        public static unsafe ulong* EIC_EIC_CTRL => (ulong*)0x40001800;

        // Serial Communication Interface 0
        public const int SERCOM0_BASE = ;
        public static unsafe uint* SERCOM0_SERCOM0_I2CM_CTRLA => (uint*)0x42000800;

        // Analog-to-Digital Converter
        public const int ADC_BASE = ;
        public static unsafe ulong* ADC_ADC_CTRLA => (ulong*)0x42002000;

        // Digital-to-Analog Converter
        public const int DAC_BASE = ;
        public static unsafe ulong* DAC_DAC_CTRLA => (ulong*)0x42002400;

        // General Purpose I/O
        public const int PORT_BASE = ;
        public static unsafe uint* PORT_PORT_DIR => (uint*)0x41004400;

        // Timer/Counter 0
        public const int TC0_BASE = ;
        public static unsafe uint* TC0_TC0_CTRLA => (uint*)0x42002800;

        // USB Device Controller
        public const int USB_BASE = ;
        public static unsafe ulong* USB_USB_CTRLA => (ulong*)0x41005000;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Reset vector
        public const int IRQ_NONMASKABLEINT = 1;  // Non-maskable interrupt
        public const int IRQ_HARDFAULT = 2;  // Hard fault
        public const int IRQ_SVCALL = 3;  // Supervisor call
        public const int IRQ_PENDSV = 4;  // Pendable service call
        public const int IRQ_SYSTICK = 5;  // System tick timer
        public const int IRQ_PM = 6;  // Power Manager
        public const int IRQ_SYSCTRL = 7;  // System Controller
        public const int IRQ_WDT = 8;  // Watchdog Timer
        public const int IRQ_RTC = 9;  // Real-Time Clock
        public const int IRQ_EIC = 10;  // External Interrupt Controller
        public const int IRQ_NVMCTRL = 11;  // Non-Volatile Memory Controller
        public const int IRQ_DMAC = 12;  // Direct Memory Access Controller
        public const int IRQ_USB = 13;  // USB Device Controller
        public const int IRQ_EVSYS = 14;  // Event System
        public const int IRQ_SERCOM0 = 15;  // Serial Communication Interface 0
        public const int IRQ_SERCOM1 = 16;  // Serial Communication Interface 1
        public const int IRQ_SERCOM2 = 17;  // Serial Communication Interface 2
        public const int IRQ_SERCOM3 = 18;  // Serial Communication Interface 3
        public const int IRQ_SERCOM4 = 19;  // Serial Communication Interface 4
        public const int IRQ_SERCOM5 = 20;  // Serial Communication Interface 5
        public const int IRQ_TCC0 = 21;  // Timer/Counter for Control 0
        public const int IRQ_TCC1 = 22;  // Timer/Counter for Control 1
        public const int IRQ_TCC2 = 23;  // Timer/Counter for Control 2
        public const int IRQ_TC3 = 24;  // Timer/Counter 3
        public const int IRQ_TC4 = 25;  // Timer/Counter 4
        public const int IRQ_TC5 = 26;  // Timer/Counter 5
        public const int IRQ_TC6 = 27;  // Timer/Counter 6
        public const int IRQ_TC7 = 28;  // Timer/Counter 7
        public const int IRQ_ADC = 29;  // Analog-to-Digital Converter
        public const int IRQ_AC = 30;  // Analog Comparator
        public const int IRQ_DAC = 31;  // Digital-to-Analog Converter
        public const int IRQ_PTC = 32;  // Peripheral Touch Controller
        public const int IRQ_I2S = 33;  // Inter-IC Sound Interface

        public static void samd21_init()
        {
            // 硬件初始化代码
        }
    }
}
