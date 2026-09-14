using System;

namespace VML.Device.Microchip.ATtiny85
{
    /// <summary>
    /// ATtiny85 寄存器定义
    /// 生成自: Microchip/AVR/ATtiny85
    /// 版本: 1.0
    /// </summary>
    public static class ATtiny85
    {
        // CPU架构: AVR, 8位, 1000000 Hz

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

        // Register pair X (R27:R26)
        public const int X_ADDR = 0x1A;
        public static unsafe ushort* X => (ushort*)0x1A;

        // Register pair Y (R29:R28)
        public const int Y_ADDR = 0x1C;
        public static unsafe ushort* Y => (ushort*)0x1C;

        // Register pair Z (R31:R30)
        public const int Z_ADDR = 0x1E;
        public static unsafe ushort* Z => (ushort*)0x1E;

        // Stack Pointer
        public const int SP_ADDR = 0x3D;
        public static unsafe ushort* SP => (ushort*)0x3D;

        // Status Register
        public const int SREG_ADDR = 0x3F;
        public static unsafe byte* SREG => (byte*)0x3F;
        public const int SREG_C = 0;  // Carry Flag
        public const int SREG_Z = 1;  // Zero Flag
        public const int SREG_N = 2;  // Negative Flag
        public const int SREG_V = 3;  // Two's Complement Overflow Flag
        public const int SREG_S = 4;  // Sign Flag (N xor V)
        public const int SREG_H = 5;  // Half Carry Flag
        public const int SREG_T = 6;  // Transfer Bit
        public const int SREG_I = 7;  // Global Interrupt Enable

        // 内存段定义
        // Program Flash (8KB)
        public const int FLASH_START = 0x0000;
        public const int FLASH_END = 0x1FFF;
        public const int FLASH_SIZE = 8192;

        // Internal SRAM (512B)
        public const int SRAM_START = 0x0060;
        public const int SRAM_END = 0x025F;
        public const int SRAM_SIZE = 512;

        // EEPROM (512B)
        public const int EEPROM_START = 0x0000;
        public const int EEPROM_END = 0x01FF;
        public const int EEPROM_SIZE = 512;

        // I/O Registers
        public const int IO_START = 0x00;
        public const int IO_END = 0x3F;
        public const int IO_SIZE = 64;

        // 外设定义
        // Port A
        public const int PORTA_BASE = 0x20;
        public static unsafe byte* PORTA_PINA => (byte*)0x00000040;
        public static unsafe byte* PORTA_DDRA => (byte*)0x00000041;
        public static unsafe byte* PORTA_PORTA => (byte*)0x00000042;

        // Port B
        public const int PORTB_BASE = 0x18;
        public static unsafe byte* PORTB_PINB => (byte*)0x0000002E;
        public static unsafe byte* PORTB_DDRB => (byte*)0x0000002F;
        public static unsafe byte* PORTB_PORTB => (byte*)0x00000030;

        // Timer/Counter0
        public const int TIPO_BASE = 0x20;
        public static unsafe byte* TIPO_TCCR0A => (byte*)0x00000040;
        public const int TIPO_TCCR0A_WGM00 = 0;  // Waveform Generation Mode
        public const int TIPO_TCCR0A_WGM01 = 1;  // Waveform Generation Mode
        public const int TIPO_TCCR0A_COM0B0 = 4;  // Compare Output Mode B
        public const int TIPO_TCCR0A_COM0B1 = 5;  // Compare Output Mode B
        public const int TIPO_TCCR0A_COM0A0 = 6;  // Compare Output Mode A
        public const int TIPO_TCCR0A_COM0A1 = 7;  // Compare Output Mode A
        public static unsafe byte* TIPO_TCCR0B => (byte*)0x00000041;
        public const int TIPO_TCCR0B_CS00 = 0;  // Clock Select
        public const int TIPO_TCCR0B_CS01 = 1;  // Clock Select
        public const int TIPO_TCCR0B_CS02 = 2;  // Clock Select
        public const int TIPO_TCCR0B_WGM02 = 3;  // Waveform Generation Mode
        public const int TIPO_TCCR0B_FOC0B = 6;  // Force Output Compare B
        public const int TIPO_TCCR0B_FOC0A = 7;  // Force Output Compare A
        public static unsafe byte* TIPO_TCNT0 => (byte*)0x00000042;
        public static unsafe byte* TIPO_OCR0A => (byte*)0x00000043;
        public static unsafe byte* TIPO_OCR0B => (byte*)0x00000044;
        public static unsafe byte* TIPO_TIMSK => (byte*)0x00000059;
        public const int TIPO_TIMSK_TOIE0 = 0;  // Timer/Counter0 Overflow Interrupt Enable
        public const int TIPO_TIMSK_OCIE0A = 1;  // Output Compare A Match Interrupt Enable
        public const int TIPO_TIMSK_OCIE0B = 2;  // Output Compare B Match Interrupt Enable
        public static unsafe byte* TIPO_TIFR => (byte*)0x00000058;
        public const int TIPO_TIFR_TOV0 = 0;  // Timer/Counter0 Overflow Flag
        public const int TIPO_TIFR_OCF0A = 1;  // Output Compare A Flag
        public const int TIPO_TIFR_OCF0B = 2;  // Output Compare B Flag

        // Timer/Counter1
        public const int TMR1_BASE = 0x28;
        public static unsafe byte* TMR1_TCCR1A => (byte*)0x00000050;
        public const int TMR1_TCCR1A_PCM1 = 0;  // PWM Mode
        public const int TMR1_TCCR1A_COM1A = 0;  // Compare Output Mode A
        public const int TMR1_TCCR1A_COM1B = 0;  // Compare Output Mode B
        public const int TMR1_TCCR1A_WG13 = 1;  // Waveform Generation Mode
        public const int TMR1_TCCR1A_WG10 = 0;  // Waveform Generation Mode
        public static unsafe byte* TMR1_TCCR1B => (byte*)0x00000051;
        public const int TMR1_TCCR1B_CTC1 = 7;  // Clear Timer on Compare
        public const int TMR1_TCCR1B_WGM13 = 4;  // Waveform Generation Mode
        public const int TMR1_TCCR1B_WGM12 = 3;  // Waveform Generation Mode
        public const int TMR1_TCCR1B_CS1 = 0;  // Clock Select
        public static unsafe ushort* TMR1_TCNT1 => (ushort*)0x00000052;
        public static unsafe ushort* TMR1_OCR1A => (ushort*)0x00000054;
        public static unsafe ushort* TMR1_OCR1B => (ushort*)0x00000056;
        public static unsafe ushort* TMR1_OCR1C => (ushort*)0x00000058;
        public static unsafe byte* TMR1_TIMSK1 => (byte*)0x0000005B;
        public static unsafe byte* TMR1_TIFR1 => (byte*)0x0000005A;

        // ADC Multiplexer
        public const int ADMUX_BASE = 0x12;
        public static unsafe byte* ADMUX_ADMUX => (byte*)0x00000024;
        public const int ADMUX_ADMUX_MUX = 0;  // Analog Channel Selection
        public const int ADMUX_ADMUX_ADLAR = 5;  // ADC Left Adjust Result
        public const int ADMUX_ADMUX_REFS = 0;  // Reference Selection
        public static unsafe byte* ADMUX_ADCSRA => (byte*)0x00000025;
        public const int ADMUX_ADCSRA_ADPS = 0;  // ADC Prescaler Select
        public const int ADMUX_ADCSRA_ADIE = 3;  // ADC Interrupt Enable
        public const int ADMUX_ADCSRA_ADIF = 4;  // ADC Interrupt Flag
        public const int ADMUX_ADCSRA_ADATE = 5;  // ADC Auto Trigger Enable
        public const int ADMUX_ADCSRA_ADSC = 6;  // ADC Start Conversion
        public const int ADMUX_ADCSRA_ADEN = 7;  // ADC Enable
        public static unsafe byte* ADMUX_ADCH => (byte*)0x00000026;
        public static unsafe byte* ADMUX_ADCL => (byte*)0x00000027;

        // Universal Serial Interface
        public const int USI_BASE = 0x18;
        public static unsafe byte* USI_USIDR => (byte*)0x00000030;
        public static unsafe byte* USI_USISR => (byte*)0x00000031;
        public const int USI_USISR_USICNT = 0;  // Counter
        public const int USI_USISR_USIDC = 4;  // Data Register
        public const int USI_USISR_USIPF = 5;  // Stop Cond Flag
        public const int USI_USISR_USIOV = 6;  // Overflow Flag
        public const int USI_USISR_USISIF = 7;  // Start Cond Interrupt Flag
        public static unsafe byte* USI_USICR => (byte*)0x00000032;
        public const int USI_USICR_USICS = 0;  // Clock Source Select
        public const int USI_USICR_USISCL = 2;  // SCL strobe
        public const int USI_USICR_USIOW = 3;  // SDA output override
        public const int USI_USICR_USIOE = 4;  // Output Enable
        public const int USI_USICR_USISRE = 5;  // Start Recognition Enable
        public const int USI_USICR_USIORE = 6;  // Stop Recognition Enable
        public const int USI_USICR_USIGIE = 7;  // Global Interrupt Enable
        public static unsafe byte* USI_USIPORT => (byte*)0x00000033;

        // MCU Control
        public const int MCUCR_BASE = 0x35;
        public static unsafe byte* MCUCR_MCUCR => (byte*)0x0000006A;
        public const int MCUCR_MCUCR_ISC = 0;  // Interrupt Sense Control
        public const int MCUCR_MCUCR_SE = 4;  // Sleep Enable
        public const int MCUCR_MCUCR_SM = 0;  // Sleep Mode
        public static unsafe byte* MCUCR_MCUCSR => (byte*)0x0000006B;
        public const int MCUCR_MCUCSR_PORF = 0;  // Power-on Reset Flag
        public const int MCUCR_MCUCSR_EXTRF = 1;  // External Reset Flag
        public const int MCUCR_MCUCSR_WDRF = 2;  // Watchdog Reset Flag
        public const int MCUCR_MCUCSR_BORF = 4;  // Brown-out Reset Flag

        // Watchdog Timer
        public const int WDTCR_BASE = 0x21;
        public static unsafe byte* WDTCR_WDTCR => (byte*)0x00000042;
        public const int WDTCR_WDTCR_WDP = 0;  // Watchdog Prescaler
        public const int WDTCR_WDTCR_WDE = 3;  // Watchdog Enable
        public const int WDTCR_WDTCR_WDIE = 4;  // Watchdog Interrupt Enable

        // EEPROM
        public const int EEPR_BASE = 0x1C;
        public static unsafe byte* EEPR_EEAR => (byte*)0x0000003A;
        public static unsafe byte* EEPR_EEDR => (byte*)0x00000039;
        public static unsafe byte* EEPR_EECR => (byte*)0x0000003B;
        public const int EEPR_EECR_EEPM = 0;  // EEPROM Programming Mode
        public const int EEPR_EECR_EERIE = 3;  // EEPROM Ready Interrupt Enable
        public const int EEPR_EECR_EEWE = 2;  // EEPROM Write Enable
        public const int EEPR_EECR_EEMWE = 1;  // EEPROM Master Write Enable
        public const int EEPR_EECR_EERE = 0;  // EEPROM Read Enable

        // External Interrupt
        public const int GIMSK_BASE = 0x3B;
        public static unsafe byte* GIMSK_GIMSK => (byte*)0x00000076;
        public const int GIMSK_GIMSK_INT0 = 0;  // External Interrupt Request 0 Enable
        public const int GIMSK_GIMSK_PCIE = 1;  // Pin Change Interrupt Enable
        public static unsafe byte* GIMSK_GIFR => (byte*)0x00000077;
        public const int GIMSK_GIFR_INTF0 = 0;  // External Interrupt Flag 0
        public const int GIMSK_GIFR_PCIF = 1;  // Pin Change Interrupt Flag

        // Pin Change Mask
        public const int PCMSK_BASE = 0x15;
        public static unsafe byte* PCMSK_PCMSK => (byte*)0x0000002A;

        // Store Program Memory
        public const int SPMCSR_BASE = 0x37;
        public static unsafe byte* SPMCSR_SPMCSR => (byte*)0x0000006E;
        public const int SPMCSR_SPMCSR_SPMCR = 0;  // SPM Mode
        public const int SPMCSR_SPMCSR_PGERS = 1;  // Page Erase
        public const int SPMCSR_SPMCSR_PGWRT = 2;  // Page Write
        public const int SPMCSR_SPMCSR_BLBSET = 3;  // Boot Lock Bits Set
        public const int SPMCSR_SPMCSR_RWWSRE = 4;  // Read-While-Read Strobe Enable
        public const int SPMCSR_SPMCSR_SIGRD = 5;  // Signature Row Read
        public const int SPMCSR_SPMCSR_SPMEN = 7;  // SPM Enable

        // 中断向量定义
        public const int IRQ_RESET = 0;  // External Reset, Power-on Reset, Brown-out Reset
        public const int IRQ_INT0 = 1;  // External Interrupt Request 0
        public const int IRQ_PCINT0 = 2;  // Pin Change
        public const int IRQ_WDT = 3;  // Watchdog Timeout
        public const int IRQ_TIM1_COMPA = 4;  // Timer/Counter1 Compare Match A
        public const int IRQ_TIM1_OVF = 5;  // Timer/Counter1 Overflow
        public const int IRQ_TIM0_COMPA = 6;  // Timer/Counter0 Compare Match A
        public const int IRQ_TIM0_OVF = 7;  // Timer/Counter0 Overflow
        public const int IRQ_SPI_STC = 8;  // SPI Serial Transfer Complete
        public const int IRQ_ADC = 9;  // ADC Conversion Complete
        public const int IRQ_USI_START = 10;  // USI Start Condition
        public const int IRQ_USI_OVF = 11;  // USI Overflow
        public const int IRQ_EE_READY = 12;  // EEPROM Ready

        // 引脚定义
        public const int PIN_PB5 = 1;  // RESET - ADC0 - dW
        public const int PIN_PB3 = 2;  // XTAL1 - CLKI - ADC3
        public const int PIN_PB4 = 3;  // XTAL2 - ADC2
        public const int PIN_PB0 = 4;  // MOSI - AI - ADC0 - T0 - INT0
        public const int PIN_PB1 = 5;  // MISO - AI - ADC1 - OC1A - INT1
        public const int PIN_PB2 = 6;  // SCK - AI - ADC3 - OC1B
        public const int PIN_VCC = 7;  // Supply Voltage
        public const int PIN_GND = 8;  // Ground

        public static void attiny85_init()
        {
            // 硬件初始化代码
        }
    }
}
