using System;

namespace VML.Device.Atmel.ATmega32U4
{
    /// <summary>
    /// ATmega32U4 寄存器定义
    /// 生成自: Atmel/AVR/ATmega32U4
    /// 版本: 1.0
    /// </summary>
    public static class ATmega32U4
    {
        // CPU架构: AVR, 8位, 16000000 Hz

        // 寄存器定义
        public const int R0_ADDR = 0x00;
        public static unsafe byte* R0 => (byte*)0x00;

        public const int R1_ADDR = 0x01;
        public static unsafe byte* R1 => (byte*)0x01;

        public const int R2_ADDR = 0x02;
        public static unsafe byte* R2 => (byte*)0x02;

        public const int R3_ADDR = 0x03;
        public static unsafe byte* R3 => (byte*)0x03;

        public const int R4_ADDR = 0x04;
        public static unsafe byte* R4 => (byte*)0x04;

        public const int R5_ADDR = 0x05;
        public static unsafe byte* R5 => (byte*)0x05;

        public const int R6_ADDR = 0x06;
        public static unsafe byte* R6 => (byte*)0x06;

        public const int R7_ADDR = 0x07;
        public static unsafe byte* R7 => (byte*)0x07;

        public const int R8_ADDR = 0x08;
        public static unsafe byte* R8 => (byte*)0x08;

        public const int R9_ADDR = 0x09;
        public static unsafe byte* R9 => (byte*)0x09;

        public const int R10_ADDR = 0x0A;
        public static unsafe byte* R10 => (byte*)0x0A;

        public const int R11_ADDR = 0x0B;
        public static unsafe byte* R11 => (byte*)0x0B;

        public const int R12_ADDR = 0x0C;
        public static unsafe byte* R12 => (byte*)0x0C;

        public const int R13_ADDR = 0x0D;
        public static unsafe byte* R13 => (byte*)0x0D;

        public const int R14_ADDR = 0x0E;
        public static unsafe byte* R14 => (byte*)0x0E;

        public const int R15_ADDR = 0x0F;
        public static unsafe byte* R15 => (byte*)0x0F;

        public const int R16_ADDR = 0x10;
        public static unsafe byte* R16 => (byte*)0x10;

        public const int R17_ADDR = 0x11;
        public static unsafe byte* R17 => (byte*)0x11;

        public const int R18_ADDR = 0x12;
        public static unsafe byte* R18 => (byte*)0x12;

        public const int R19_ADDR = 0x13;
        public static unsafe byte* R19 => (byte*)0x13;

        public const int R20_ADDR = 0x14;
        public static unsafe byte* R20 => (byte*)0x14;

        public const int R21_ADDR = 0x15;
        public static unsafe byte* R21 => (byte*)0x15;

        public const int R22_ADDR = 0x16;
        public static unsafe byte* R22 => (byte*)0x16;

        public const int R23_ADDR = 0x17;
        public static unsafe byte* R23 => (byte*)0x17;

        public const int R24_ADDR = 0x18;
        public static unsafe byte* R24 => (byte*)0x18;

        public const int R25_ADDR = 0x19;
        public static unsafe byte* R25 => (byte*)0x19;

        public const int R26_ADDR = 0x1A;
        public static unsafe byte* R26 => (byte*)0x1A;

        public const int R27_ADDR = 0x1B;
        public static unsafe byte* R27 => (byte*)0x1B;

        public const int R28_ADDR = 0x1C;
        public static unsafe byte* R28 => (byte*)0x1C;

        public const int R29_ADDR = 0x1D;
        public static unsafe byte* R29 => (byte*)0x1D;

        public const int R30_ADDR = 0x1E;
        public static unsafe byte* R30 => (byte*)0x1E;

        public const int R31_ADDR = 0x1F;
        public static unsafe byte* R31 => (byte*)0x1F;

        public const int SPL_ADDR = 0x5D;
        public static unsafe byte* SPL => (byte*)0x5D;

        public const int SPH_ADDR = 0x5E;
        public static unsafe byte* SPH => (byte*)0x5E;

        public const int SREG_ADDR = 0x5F;
        public static unsafe byte* SREG => (byte*)0x5F;

        // 内存段定义
        // Program Flash Memory
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x7FFF;
        public const int FLASH_SIZE = 32768;

        // Static RAM
        public const int SRAM_START = 0x0100;
        public const int SRAM_END = 0x0AFF;
        public const int SRAM_SIZE = 2560;

        // EEPROM
        public const int EEPROM_START = 0x0000;
        public const int EEPROM_END = 0x03FF;
        public const int EEPROM_SIZE = 1024;

        // I/O Registers
        public const int IO_START = 0x00;
        public const int IO_END = 0x3F;
        public const int IO_SIZE = 64;

        // Extended I/O Registers
        public const int EXTIO_START = 0x40;
        public const int EXTIO_END = 0xFF;
        public const int EXTIO_SIZE = 192;

        // 外设定义
        // Port B
        public const int PORTB_BASE = 0x23;
        public static unsafe byte* PORTB_PORTB => (byte*)0x00000048;
        public static unsafe byte* PORTB_DDRB => (byte*)0x00000047;
        public static unsafe byte* PORTB_PINB => (byte*)0x00000046;

        // Port C
        public const int PORTC_BASE = 0x26;
        public static unsafe byte* PORTC_PORTC => (byte*)0x0000004E;
        public static unsafe byte* PORTC_DDRC => (byte*)0x0000004D;
        public static unsafe byte* PORTC_PINC => (byte*)0x0000004C;

        // Port D
        public const int PORTD_BASE = 0x29;
        public static unsafe byte* PORTD_PORTD => (byte*)0x00000054;
        public static unsafe byte* PORTD_DDRD => (byte*)0x00000053;
        public static unsafe byte* PORTD_PIND => (byte*)0x00000052;

        // Port E
        public const int PORTE_BASE = 0x2C;
        public static unsafe byte* PORTE_PORTE => (byte*)0x0000005A;
        public static unsafe byte* PORTE_DDRE => (byte*)0x00000059;
        public static unsafe byte* PORTE_PINE => (byte*)0x00000058;

        // USART1
        public const int UART1_BASE = 0xC8;
        public static unsafe byte* UART1_UDR1 => (byte*)0x00000196;
        public static unsafe byte* UART1_UCSR1A => (byte*)0x00000190;
        public static unsafe byte* UART1_UCSR1B => (byte*)0x00000191;
        public static unsafe byte* UART1_UCSR1C => (byte*)0x00000192;
        public static unsafe ushort* UART1_UBRR1 => (ushort*)0x00000194;

        // USB Controller
        public const int USB_BASE = 0xD0;
        public static unsafe byte* USB_UDCON => (byte*)0x000001A0;
        public static unsafe byte* USB_UDIEN => (byte*)0x000001A1;
        public static unsafe byte* USB_UDINT => (byte*)0x000001A2;

        // 中断向量定义
        public const int IRQ_INT0 = 1;  // External Interrupt 0
        public const int IRQ_INT1 = 2;  // External Interrupt 1
        public const int IRQ_INT2 = 3;  // External Interrupt 2
        public const int IRQ_INT3 = 4;  // External Interrupt 3
        public const int IRQ_INT4 = 5;  // External Interrupt 4
        public const int IRQ_INT5 = 6;  // External Interrupt 5
        public const int IRQ_INT6 = 7;  // External Interrupt 6
        public const int IRQ_PCINT0 = 8;  // Pin Change Interrupt 0
        public const int IRQ_USB_GENERAL = 9;  // USB General
        public const int IRQ_USB_ENDPOINT = 10;  // USB Endpoint
        public const int IRQ_WDT = 11;  // Watchdog Timeout
        public const int IRQ_TIMER1_CAPT = 12;  // Timer1 Capture
        public const int IRQ_TIMER1_COMPA = 13;  // Timer1 Compare A
        public const int IRQ_TIMER1_COMPB = 14;  // Timer1 Compare B
        public const int IRQ_TIMER1_OVF = 15;  // Timer1 Overflow
        public const int IRQ_TIMER0_COMPA = 16;  // Timer0 Compare A
        public const int IRQ_TIMER0_COMPB = 17;  // Timer0 Compare B
        public const int IRQ_TIMER0_OVF = 18;  // Timer0 Overflow
        public const int IRQ_SPI_STC = 19;  // SPI Transfer Complete
        public const int IRQ_UART1_RX = 20;  // UART1 Receive
        public const int IRQ_UART1_UDRE = 21;  // UART1 Data Register Empty
        public const int IRQ_UART1_TX = 22;  // UART1 Transmit
        public const int IRQ_ADC = 23;  // ADC Conversion Complete

        public static void atmega32u4_init()
        {
            // 硬件初始化代码
        }
    }
}
