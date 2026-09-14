using System;

namespace VML.Device.Atmel.ATmega2560
{
    /// <summary>
    /// ATmega2560 寄存器定义
    /// 生成自: Atmel/AVR/ATmega2560
    /// 版本: 1.0
    /// </summary>
    public static class ATmega2560
    {
        // CPU架构: AVR, 8位, 16000000 Hz

        // 寄存器定义
        public const int R0_ADDR = 0x00;
        public static unsafe byte* R0 => (byte*)0x00;

        public const int R1_ADDR = 0x01;
        public static unsafe byte* R1 => (byte*)0x01;

        public const int R2_ADDR = 0x02;
        public static unsafe byte* R2 => (byte*)0x02;

        public const int SPL_ADDR = 0x5D;
        public static unsafe byte* SPL => (byte*)0x5D;

        public const int SPH_ADDR = 0x5E;
        public static unsafe byte* SPH => (byte*)0x5E;

        public const int SREG_ADDR = 0x5F;
        public static unsafe byte* SREG => (byte*)0x5F;

        // 内存段定义
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x3FFFF;
        public const int FLASH_SIZE = 262144;

        public const int SRAM_START = 0x0200;
        public const int SRAM_END = 0x21FF;
        public const int SRAM_SIZE = 8192;

        public const int EEPROM_START = 0x0000;
        public const int EEPROM_END = 0x0FFF;
        public const int EEPROM_SIZE = 4096;

        public const int IO_START = 0x00;
        public const int IO_END = 0x3F;
        public const int IO_SIZE = 64;

        public const int EXTIO_START = 0x40;
        public const int EXTIO_END = 0xFF;
        public const int EXTIO_SIZE = 192;

        // 外设定义
        // Port A
        public const int PORTA_BASE = 0x22;
        public static unsafe byte* PORTA_DDRA => (byte*)0x00000043;
        public static unsafe byte* PORTA_PORTA => (byte*)0x00000044;
        public static unsafe byte* PORTA_PINA => (byte*)0x00000042;

        // Port B
        public const int PORTB_BASE = 0x25;
        public static unsafe byte* PORTB_DDRB => (byte*)0x00000049;
        public static unsafe byte* PORTB_PORTB => (byte*)0x0000004A;
        public static unsafe byte* PORTB_PINB => (byte*)0x00000048;

        // Port C
        public const int PORTC_BASE = 0x28;
        public static unsafe byte* PORTC_DDRC => (byte*)0x0000004F;
        public static unsafe byte* PORTC_PORTC => (byte*)0x00000050;
        public static unsafe byte* PORTC_PINC => (byte*)0x0000004E;

        // Port D
        public const int PORTD_BASE = 0x2B;
        public static unsafe byte* PORTD_DDRD => (byte*)0x00000055;
        public static unsafe byte* PORTD_PORTD => (byte*)0x00000056;
        public static unsafe byte* PORTD_PIND => (byte*)0x00000054;

        // Port E
        public const int PORTE_BASE = 0x2E;
        public static unsafe byte* PORTE_DDRE => (byte*)0x0000005B;
        public static unsafe byte* PORTE_PORTE => (byte*)0x0000005C;
        public static unsafe byte* PORTE_PINE => (byte*)0x0000005A;

        // Port F
        public const int PORTF_BASE = 0x31;
        public static unsafe byte* PORTF_DDRF => (byte*)0x00000061;
        public static unsafe byte* PORTF_PORTF => (byte*)0x00000062;
        public static unsafe byte* PORTF_PINF => (byte*)0x00000060;

        // Port G
        public const int PORTG_BASE = 0x34;
        public static unsafe byte* PORTG_DDRG => (byte*)0x00000067;
        public static unsafe byte* PORTG_PORTG => (byte*)0x00000068;
        public static unsafe byte* PORTG_PING => (byte*)0x00000066;

        // USART 0
        public const int USART0_BASE = 0xC0;
        public static unsafe byte* USART0_UDR0 => (byte*)0x00000186;
        public static unsafe byte* USART0_UCSR0A => (byte*)0x00000180;
        public static unsafe byte* USART0_UCSR0B => (byte*)0x00000181;
        public static unsafe byte* USART0_UCSR0C => (byte*)0x00000182;
        public static unsafe byte* USART0_UBRR0L => (byte*)0x00000184;
        public static unsafe byte* USART0_UBRR0H => (byte*)0x00000185;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_INT0 = 2;  // 
        public const int IRQ_INT1 = 3;  // 
        public const int IRQ_INT2 = 4;  // 
        public const int IRQ_INT3 = 5;  // 
        public const int IRQ_INT4 = 6;  // 
        public const int IRQ_INT5 = 7;  // 
        public const int IRQ_INT6 = 8;  // 
        public const int IRQ_INT7 = 9;  // 
        public const int IRQ_PCINT0 = 10;  // 
        public const int IRQ_PCINT1 = 11;  // 
        public const int IRQ_PCINT2 = 12;  // 
        public const int IRQ_WDT = 13;  // 
        public const int IRQ_TIM2_COMPA = 14;  // 
        public const int IRQ_TIM2_COMPB = 15;  // 
        public const int IRQ_TIM2_OVF = 16;  // 
        public const int IRQ_TIM1_CAPT = 17;  // 
        public const int IRQ_TIM1_COMPA = 18;  // 
        public const int IRQ_TIM1_COMPB = 19;  // 
        public const int IRQ_TIM1_OVF = 20;  // 
        public const int IRQ_TIM0_COMPA = 21;  // 
        public const int IRQ_TIM0_COMPB = 22;  // 
        public const int IRQ_TIM0_OVF = 23;  // 
        public const int IRQ_SPI_STC = 24;  // 
        public const int IRQ_USART0_RX = 25;  // 
        public const int IRQ_USART0_UDRE = 26;  // 
        public const int IRQ_USART0_TX = 27;  // 

        public static void atmega2560_init()
        {
            // 硬件初始化代码
        }
    }
}
