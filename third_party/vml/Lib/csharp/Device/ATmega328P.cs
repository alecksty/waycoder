using System;

namespace VML.Device.Atmel.ATmega328P
{
    /// <summary>
    /// ATmega328P 寄存器定义
    /// 生成自: Atmel/AVR/ATmega328P
    /// 版本: 1.0
    /// </summary>
    public static class ATmega328P
    {
        // CPU架构: AVR, 8位, 16000000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00;
        public static unsafe byte* R0 => (byte*)0x00;

        // General Purpose Register 1
        public const int R1_ADDR = 0x01;
        public static unsafe byte* R1 => (byte*)0x01;

        // General Purpose Register 2
        public const int R2_ADDR = 0x02;
        public static unsafe byte* R2 => (byte*)0x02;

        // General Purpose Register 3
        public const int R3_ADDR = 0x03;
        public static unsafe byte* R3 => (byte*)0x03;

        // General Purpose Register 4
        public const int R4_ADDR = 0x04;
        public static unsafe byte* R4 => (byte*)0x04;

        // General Purpose Register 5
        public const int R5_ADDR = 0x05;
        public static unsafe byte* R5 => (byte*)0x05;

        // General Purpose Register 6
        public const int R6_ADDR = 0x06;
        public static unsafe byte* R6 => (byte*)0x06;

        // General Purpose Register 7
        public const int R7_ADDR = 0x07;
        public static unsafe byte* R7 => (byte*)0x07;

        // General Purpose Register 8
        public const int R8_ADDR = 0x08;
        public static unsafe byte* R8 => (byte*)0x08;

        // General Purpose Register 9
        public const int R9_ADDR = 0x09;
        public static unsafe byte* R9 => (byte*)0x09;

        // General Purpose Register 10
        public const int R10_ADDR = 0x0A;
        public static unsafe byte* R10 => (byte*)0x0A;

        // General Purpose Register 11
        public const int R11_ADDR = 0x0B;
        public static unsafe byte* R11 => (byte*)0x0B;

        // General Purpose Register 12
        public const int R12_ADDR = 0x0C;
        public static unsafe byte* R12 => (byte*)0x0C;

        // General Purpose Register 13
        public const int R13_ADDR = 0x0D;
        public static unsafe byte* R13 => (byte*)0x0D;

        // General Purpose Register 14
        public const int R14_ADDR = 0x0E;
        public static unsafe byte* R14 => (byte*)0x0E;

        // General Purpose Register 15
        public const int R15_ADDR = 0x0F;
        public static unsafe byte* R15 => (byte*)0x0F;

        // General Purpose Register 16
        public const int R16_ADDR = 0x10;
        public static unsafe byte* R16 => (byte*)0x10;

        // General Purpose Register 17
        public const int R17_ADDR = 0x11;
        public static unsafe byte* R17 => (byte*)0x11;

        // General Purpose Register 18
        public const int R18_ADDR = 0x12;
        public static unsafe byte* R18 => (byte*)0x12;

        // General Purpose Register 19
        public const int R19_ADDR = 0x13;
        public static unsafe byte* R19 => (byte*)0x13;

        // General Purpose Register 20
        public const int R20_ADDR = 0x14;
        public static unsafe byte* R20 => (byte*)0x14;

        // General Purpose Register 21
        public const int R21_ADDR = 0x15;
        public static unsafe byte* R21 => (byte*)0x15;

        // General Purpose Register 22
        public const int R22_ADDR = 0x16;
        public static unsafe byte* R22 => (byte*)0x16;

        // General Purpose Register 23
        public const int R23_ADDR = 0x17;
        public static unsafe byte* R23 => (byte*)0x17;

        // General Purpose Register 24
        public const int R24_ADDR = 0x18;
        public static unsafe byte* R24 => (byte*)0x18;

        // General Purpose Register 25
        public const int R25_ADDR = 0x19;
        public static unsafe byte* R25 => (byte*)0x19;

        // General Purpose Register 26 (XL)
        public const int R26_ADDR = 0x1A;
        public static unsafe byte* R26 => (byte*)0x1A;

        // General Purpose Register 27 (XH)
        public const int R27_ADDR = 0x1B;
        public static unsafe byte* R27 => (byte*)0x1B;

        // General Purpose Register 28 (YL)
        public const int R28_ADDR = 0x1C;
        public static unsafe byte* R28 => (byte*)0x1C;

        // General Purpose Register 29 (YH)
        public const int R29_ADDR = 0x1D;
        public static unsafe byte* R29 => (byte*)0x1D;

        // General Purpose Register 30 (ZL)
        public const int R30_ADDR = 0x1E;
        public static unsafe byte* R30 => (byte*)0x1E;

        // General Purpose Register 31 (ZH)
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
        public const int SREG_C = 0;  // Carry Flag
        public const int SREG_Z = 1;  // Zero Flag
        public const int SREG_N = 2;  // Negative Flag
        public const int SREG_V = 3;  // Two's Complement Overflow Flag
        public const int SREG_S = 4;  // Sign Flag (N ⊕ V)
        public const int SREG_H = 5;  // Half Carry Flag
        public const int SREG_T = 6;  // Transfer Bit
        public const int SREG_I = 7;  // Global Interrupt Enable

        // 内存段定义
        // Program Flash Memory
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x7FFF;
        public const int FLASH_SIZE = 32768;

        // Static RAM
        public const int SRAM_START = 0x0100;
        public const int SRAM_END = 0x08FF;
        public const int SRAM_SIZE = 2048;

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
        // Port B Data Register
        public const int PORTB_BASE = 0x23;
        public static unsafe byte* PORTB_PORTB => (byte*)0x00000048;
        public static unsafe byte* PORTB_DDRB => (byte*)0x00000047;
        public static unsafe byte* PORTB_PINB => (byte*)0x00000046;
        public const int PORTB_PB0 = 0;  // Port B, bit 0
        public const int PORTB_PB1 = 1;  // Port B, bit 1
        public const int PORTB_PB2 = 2;  // Port B, bit 2
        public const int PORTB_PB3 = 3;  // Port B, bit 3
        public const int PORTB_PB4 = 4;  // Port B, bit 4
        public const int PORTB_PB5 = 5;  // Port B, bit 5
        public const int PORTB_PB6 = 6;  // Port B, bit 6
        public const int PORTB_PB7 = 7;  // Port B, bit 7

        // Port C Data Register
        public const int PORTC_BASE = 0x26;
        public static unsafe byte* PORTC_PORTC => (byte*)0x0000004E;
        public static unsafe byte* PORTC_DDRC => (byte*)0x0000004D;
        public static unsafe byte* PORTC_PINC => (byte*)0x0000004C;
        public const int PORTC_PC0 = 0;  // Port C, bit 0
        public const int PORTC_PC1 = 1;  // Port C, bit 1
        public const int PORTC_PC2 = 2;  // Port C, bit 2
        public const int PORTC_PC3 = 3;  // Port C, bit 3
        public const int PORTC_PC4 = 4;  // Port C, bit 4
        public const int PORTC_PC5 = 5;  // Port C, bit 5
        public const int PORTC_PC6 = 6;  // Port C, bit 6

        // Port D Data Register
        public const int PORTD_BASE = 0x29;
        public static unsafe byte* PORTD_PORTD => (byte*)0x00000054;
        public static unsafe byte* PORTD_DDRD => (byte*)0x00000053;
        public static unsafe byte* PORTD_PIND => (byte*)0x00000052;
        public const int PORTD_PD0 = 0;  // Port D, bit 0
        public const int PORTD_PD1 = 1;  // Port D, bit 1
        public const int PORTD_PD2 = 2;  // Port D, bit 2
        public const int PORTD_PD3 = 3;  // Port D, bit 3
        public const int PORTD_PD4 = 4;  // Port D, bit 4
        public const int PORTD_PD5 = 5;  // Port D, bit 5
        public const int PORTD_PD6 = 6;  // Port D, bit 6
        public const int PORTD_PD7 = 7;  // Port D, bit 7

        // 8-bit Timer/Counter0
        public const int TIMER0_BASE = 0x44;
        public static unsafe byte* TIMER0_TCCR0A => (byte*)0x00000088;
        public const int TIMER0_TCCR0A_WGM00 = 0;  // Waveform Generation Mode
        public const int TIMER0_TCCR0A_WGM01 = 1;  // Waveform Generation Mode
        public const int TIMER0_TCCR0A_COM0B0 = 4;  // Compare Output Mode for Channel B
        public const int TIMER0_TCCR0A_COM0B1 = 5;  // Compare Output Mode for Channel B
        public const int TIMER0_TCCR0A_COM0A0 = 6;  // Compare Output Mode for Channel A
        public const int TIMER0_TCCR0A_COM0A1 = 7;  // Compare Output Mode for Channel A
        public static unsafe byte* TIMER0_TCCR0B => (byte*)0x00000089;
        public const int TIMER0_TCCR0B_CS00 = 0;  // Clock Select
        public const int TIMER0_TCCR0B_CS01 = 1;  // Clock Select
        public const int TIMER0_TCCR0B_CS02 = 2;  // Clock Select
        public const int TIMER0_TCCR0B_WGM02 = 3;  // Waveform Generation Mode
        public const int TIMER0_TCCR0B_FOC0B = 6;  // Force Output Compare B
        public const int TIMER0_TCCR0B_FOC0A = 7;  // Force Output Compare A
        public static unsafe byte* TIMER0_TCNT0 => (byte*)0x0000008A;
        public static unsafe byte* TIMER0_OCR0A => (byte*)0x0000008B;
        public static unsafe byte* TIMER0_OCR0B => (byte*)0x0000008C;
        public static unsafe byte* TIMER0_TIMSK0 => (byte*)0x000000B2;
        public const int TIMER0_TIMSK0_TOIE0 = 0;  // Timer/Counter0 Overflow Interrupt Enable
        public const int TIMER0_TIMSK0_OCIE0A = 1;  // Timer/Counter0 Output Compare A Match Interrupt Enable
        public const int TIMER0_TIMSK0_OCIE0B = 2;  // Timer/Counter0 Output Compare B Match Interrupt Enable
        public static unsafe byte* TIMER0_TIFR0 => (byte*)0x00000079;
        public const int TIMER0_TIFR0_TOV0 = 0;  // Timer/Counter0 Overflow Flag
        public const int TIMER0_TIFR0_OCF0A = 1;  // Output Compare Flag 0A
        public const int TIMER0_TIFR0_OCF0B = 2;  // Output Compare Flag 0B

        // Universal Synchronous/Asynchronous Receiver/Transmitter
        public const int USART0_BASE = 0xC0;
        public static unsafe byte* USART0_UDR0 => (byte*)0x00000186;
        public static unsafe byte* USART0_UCSR0A => (byte*)0x00000180;
        public const int USART0_UCSR0A_MPCM0 = 0;  // Multi-processor Communication Mode
        public const int USART0_UCSR0A_U2X0 = 1;  // Double the USART Transmission Speed
        public const int USART0_UCSR0A_UPE0 = 2;  // Parity Error
        public const int USART0_UCSR0A_DOR0 = 3;  // Data OverRun
        public const int USART0_UCSR0A_FE0 = 4;  // Frame Error
        public const int USART0_UCSR0A_UDRE0 = 5;  // USART Data Register Empty
        public const int USART0_UCSR0A_TXC0 = 6;  // USART Transmit Complete
        public const int USART0_UCSR0A_RXC0 = 7;  // USART Receive Complete
        public static unsafe byte* USART0_UCSR0B => (byte*)0x00000181;
        public const int USART0_UCSR0B_TXB80 = 0;  // Transmit Data Bit 8
        public const int USART0_UCSR0B_RXB80 = 1;  // Receive Data Bit 8
        public const int USART0_UCSR0B_UCSZ02 = 2;  // Character Size
        public const int USART0_UCSR0B_TXEN0 = 3;  // Transmitter Enable
        public const int USART0_UCSR0B_RXEN0 = 4;  // Receiver Enable
        public const int USART0_UCSR0B_UDRIE0 = 5;  // USART Data Register Empty Interrupt Enable
        public const int USART0_UCSR0B_TXCIE0 = 6;  // TX Complete Interrupt Enable
        public const int USART0_UCSR0B_RXCIE0 = 7;  // RX Complete Interrupt Enable
        public static unsafe byte* USART0_UCSR0C => (byte*)0x00000182;
        public const int USART0_UCSR0C_UCPOL0 = 0;  // Clock Polarity
        public const int USART0_UCSR0C_UCSZ00 = 1;  // Character Size
        public const int USART0_UCSR0C_UCSZ01 = 2;  // Character Size
        public const int USART0_UCSR0C_USBS0 = 3;  // Stop Bit Select
        public const int USART0_UCSR0C_UPM00 = 4;  // Parity Mode
        public const int USART0_UCSR0C_UPM01 = 5;  // Parity Mode
        public const int USART0_UCSR0C_UMSEL00 = 6;  // USART Mode Select
        public const int USART0_UCSR0C_UMSEL01 = 7;  // USART Mode Select
        public static unsafe ushort* USART0_UBRR0 => (ushort*)0x00000184;

        // Analog-to-Digital Converter
        public const int ADC_BASE = 0x78;
        public static unsafe byte* ADC_ADMUX => (byte*)0x000000F4;
        public const int ADC_ADMUX_MUX0 = 0;  // Analog Channel Selection
        public const int ADC_ADMUX_MUX1 = 1;  // Analog Channel Selection
        public const int ADC_ADMUX_MUX2 = 2;  // Analog Channel Selection
        public const int ADC_ADMUX_MUX3 = 3;  // Analog Channel Selection
        public const int ADC_ADMUX_ADLAR = 5;  // ADC Left Adjust Result
        public const int ADC_ADMUX_REFS0 = 6;  // Reference Selection
        public const int ADC_ADMUX_REFS1 = 7;  // Reference Selection
        public static unsafe byte* ADC_ADCSRA => (byte*)0x000000F2;
        public const int ADC_ADCSRA_ADPS0 = 0;  // ADC Prescaler Select
        public const int ADC_ADCSRA_ADPS1 = 1;  // ADC Prescaler Select
        public const int ADC_ADCSRA_ADPS2 = 2;  // ADC Prescaler Select
        public const int ADC_ADCSRA_ADIE = 3;  // ADC Interrupt Enable
        public const int ADC_ADCSRA_ADIF = 4;  // ADC Interrupt Flag
        public const int ADC_ADCSRA_ADATE = 5;  // ADC Auto Trigger Enable
        public const int ADC_ADCSRA_ADSC = 6;  // ADC Start Conversion
        public const int ADC_ADCSRA_ADEN = 7;  // ADC Enable
        public static unsafe byte* ADC_ADCH => (byte*)0x000000F1;
        public static unsafe byte* ADC_ADCL => (byte*)0x000000F0;

        // 中断向量定义
        public const int IRQ_INT0 = 1;  // External Interrupt Request 0
        public const int IRQ_INT1 = 2;  // External Interrupt Request 1
        public const int IRQ_PCINT0 = 3;  // Pin Change Interrupt Request 0
        public const int IRQ_PCINT1 = 4;  // Pin Change Interrupt Request 1
        public const int IRQ_PCINT2 = 5;  // Pin Change Interrupt Request 2
        public const int IRQ_WDT = 6;  // Watchdog Time-out Interrupt
        public const int IRQ_TIMER2_COMPA = 7;  // Timer/Counter2 Compare Match A
        public const int IRQ_TIMER2_COMPB = 8;  // Timer/Counter2 Compare Match B
        public const int IRQ_TIMER2_OVF = 9;  // Timer/Counter2 Overflow
        public const int IRQ_TIMER1_CAPT = 10;  // Timer/Counter1 Capture Event
        public const int IRQ_TIMER1_COMPA = 11;  // Timer/Counter1 Compare Match A
        public const int IRQ_TIMER1_COMPB = 12;  // Timer/Counter1 Compare Match B
        public const int IRQ_TIMER1_OVF = 13;  // Timer/Counter1 Overflow
        public const int IRQ_TIMER0_COMPA = 14;  // Timer/Counter0 Compare Match A
        public const int IRQ_TIMER0_COMPB = 15;  // Timer/Counter0 Compare Match B
        public const int IRQ_TIMER0_OVF = 16;  // Timer/Counter0 Overflow
        public const int IRQ_SPI_STC = 17;  // SPI Serial Transfer Complete
        public const int IRQ_USART_RX = 18;  // USART Rx Complete
        public const int IRQ_USART_UDRE = 19;  // USART Data Register Empty
        public const int IRQ_USART_TX = 20;  // USART Tx Complete
        public const int IRQ_ADC = 21;  // ADC Conversion Complete
        public const int IRQ_EE_READY = 22;  // EEPROM Ready
        public const int IRQ_ANALOG_COMP = 23;  // Analog Comparator
        public const int IRQ_TWI = 24;  // Two-wire Serial Interface
        public const int IRQ_SPM_READY = 25;  // Store Program Memory Ready

        // 引脚定义
        public const int PIN_PC6 = 1;  // Reset Pin
        public const int PIN_PD0 = 2;  // Digital I/O, RX (USART)
        public const int PIN_PD1 = 3;  // Digital I/O, TX (USART)
        public const int PIN_PD2 = 4;  // Digital I/O, INT0
        public const int PIN_PD3 = 5;  // Digital I/O, INT1, OC2B
        public const int PIN_PD4 = 6;  // Digital I/O, T0, XCK
        public const int PIN_VCC = 7;  // Supply Voltage
        public const int PIN_GND = 8;  // Ground
        public const int PIN_PB6 = 9;  // Digital I/O, XTAL1
        public const int PIN_PB7 = 10;  // Digital I/O, XTAL2
        public const int PIN_PD5 = 11;  // Digital I/O, T1, OC0B
        public const int PIN_PD6 = 12;  // Digital I/O, AIN0, OC0A
        public const int PIN_PD7 = 13;  // Digital I/O, AIN1
        public const int PIN_PB0 = 14;  // Digital I/O, ICP1, CLKO
        public const int PIN_PB1 = 15;  // Digital I/O, OC1A
        public const int PIN_PB2 = 16;  // Digital I/O, SS, OC1B
        public const int PIN_PB3 = 17;  // Digital I/O, MOSI, OC2A
        public const int PIN_PB4 = 18;  // Digital I/O, MISO
        public const int PIN_PB5 = 19;  // Digital I/O, SCK
        public const int PIN_AVCC = 20;  // Supply Voltage for ADC
        public const int PIN_AREF = 21;  // Analog Reference
        public const int PIN_GND = 22;  // Ground
        public const int PIN_PC0 = 23;  // Digital I/O, ADC0
        public const int PIN_PC1 = 24;  // Digital I/O, ADC1
        public const int PIN_PC2 = 25;  // Digital I/O, ADC2
        public const int PIN_PC3 = 26;  // Digital I/O, ADC3
        public const int PIN_PC4 = 27;  // Digital I/O, ADC4, SDA
        public const int PIN_PC5 = 28;  // Digital I/O, ADC5, SCL

        public static void atmega328p_init()
        {
            // 硬件初始化代码
        }
    }
}
