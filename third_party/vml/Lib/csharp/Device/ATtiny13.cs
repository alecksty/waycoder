using System;

namespace VML.Device.Atmel.ATtiny13
{
    /// <summary>
    /// ATtiny13 寄存器定义
    /// 生成自: Atmel/AVR/ATtiny13
    /// 版本: 1.0
    /// </summary>
    public static class ATtiny13
    {
        // CPU架构: AVR, 8位, 20000000 Hz

        // 寄存器定义
        public const int R0_ADDR = 0x00;
        public static unsafe byte* R0 => (byte*)0x00;

        public const int R1_ADDR = 0x01;
        public static unsafe byte* R1 => (byte*)0x01;

        public const int R2_ADDR = 0x02;
        public static unsafe byte* R2 => (byte*)0x02;

        public const int R16_ADDR = 0x10;
        public static unsafe byte* R16 => (byte*)0x10;

        public const int R17_ADDR = 0x11;
        public static unsafe byte* R17 => (byte*)0x11;

        // XL
        public const int R26_ADDR = 0x1A;
        public static unsafe byte* R26 => (byte*)0x1A;

        // XH
        public const int R27_ADDR = 0x1B;
        public static unsafe byte* R27 => (byte*)0x1B;

        // YL
        public const int R28_ADDR = 0x1C;
        public static unsafe byte* R28 => (byte*)0x1C;

        // YH
        public const int R29_ADDR = 0x1D;
        public static unsafe byte* R29 => (byte*)0x1D;

        // ZL
        public const int R30_ADDR = 0x1E;
        public static unsafe byte* R30 => (byte*)0x1E;

        // ZH
        public const int R31_ADDR = 0x1F;
        public static unsafe byte* R31 => (byte*)0x1F;

        // Stack Pointer Low
        public const int SPL_ADDR = 0x5D;
        public static unsafe byte* SPL => (byte*)0x5D;

        // Stack Pointer High
        public const int SPH_ADDR = 0x5E;
        public static unsafe byte* SPH => (byte*)0x5E;

        // Status Register
        public const int SREG_ADDR = 0x5F;
        public static unsafe byte* SREG => (byte*)0x5F;

        // 内存段定义
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x03FF;
        public const int FLASH_SIZE = 1024;

        public const int SRAM_START = 0x0060;
        public const int SRAM_END = 0x009F;
        public const int SRAM_SIZE = 64;

        public const int EEPROM_START = 0x0000;
        public const int EEPROM_END = 0x003F;
        public const int EEPROM_SIZE = 64;

        public const int IO_START = 0x00;
        public const int IO_END = 0x1F;
        public const int IO_SIZE = 32;

        public const int EXTIO_START = 0x20;
        public const int EXTIO_END = 0x5F;
        public const int EXTIO_SIZE = 64;

        // 外设定义
        // Port B (only port)
        public const int PORTB_BASE = 0x18;
        public static unsafe byte* PORTB_DDRB => (byte*)0x0000002F;
        public static unsafe byte* PORTB_PORTB => (byte*)0x00000030;
        public static unsafe byte* PORTB_PINB => (byte*)0x00000031;
        public const int PORTB_PB0 = 0;  // Port B bit 0
        public const int PORTB_PB1 = 1;  // Port B bit 1
        public const int PORTB_PB2 = 2;  // Port B bit 2
        public const int PORTB_PB3 = 3;  // Port B bit 3
        public const int PORTB_PB4 = 4;  // Port B bit 4
        public const int PORTB_PB5 = 5;  // Port B bit 5

        // 8-bit Timer/Counter0
        public const int TIMER0_BASE = 0x33;
        public static unsafe byte* TIMER0_TCCR0A => (byte*)0x00000066;
        public static unsafe byte* TIMER0_TCCR0B => (byte*)0x00000066;
        public static unsafe byte* TIMER0_TCNT0 => (byte*)0x00000065;
        public static unsafe byte* TIMER0_OCR0A => (byte*)0x00000069;
        public static unsafe byte* TIMER0_OCR0B => (byte*)0x00000068;
        public static unsafe byte* TIMER0_TIMSK0 => (byte*)0x0000006C;
        public static unsafe byte* TIMER0_TIFR0 => (byte*)0x0000006B;

        // Analog-to-Digital
        public const int ADC_BASE = 0x04;
        public static unsafe byte* ADC_ADMUX => (byte*)0x0000000B;
        public static unsafe byte* ADC_ADCSRA => (byte*)0x0000000A;
        public static unsafe byte* ADC_ADCL => (byte*)0x00000008;
        public static unsafe byte* ADC_ADCH => (byte*)0x00000009;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_INT0 = 2;  // External Interrupt 0
        public const int IRQ_PCINT0 = 3;  // Pin Change Interrupt
        public const int IRQ_TIM0_OVF = 4;  // Timer0 Overflow
        public const int IRQ_TIM0_COMPA = 5;  // Timer0 Compare A
        public const int IRQ_WDT = 6;  // Watchdog Timeout
        public const int IRQ_ADC = 7;  // ADC Conversion Complete

        public static void attiny13_init()
        {
            // 硬件初始化代码
        }
    }
}
