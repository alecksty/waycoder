package vml.device.atmel.atmega328p;

/**
 * ATmega328P 寄存器定义
 * 生成自: Atmel/AVR/ATmega328P
 * 版本: 1.0
 */
public final class ATmega328P {
    private ATmega328P() {} // 工具类
    // CPU架构: AVR, 8位, 16000000 Hz

    // 寄存器定义
    // General Purpose Register 0
    public static final int R0_ADDR = (int)0x00;

    // General Purpose Register 1
    public static final int R1_ADDR = (int)0x01;

    // General Purpose Register 2
    public static final int R2_ADDR = (int)0x02;

    // General Purpose Register 3
    public static final int R3_ADDR = (int)0x03;

    // General Purpose Register 4
    public static final int R4_ADDR = (int)0x04;

    // General Purpose Register 5
    public static final int R5_ADDR = (int)0x05;

    // General Purpose Register 6
    public static final int R6_ADDR = (int)0x06;

    // General Purpose Register 7
    public static final int R7_ADDR = (int)0x07;

    // General Purpose Register 8
    public static final int R8_ADDR = (int)0x08;

    // General Purpose Register 9
    public static final int R9_ADDR = (int)0x09;

    // General Purpose Register 10
    public static final int R10_ADDR = (int)0x0A;

    // General Purpose Register 11
    public static final int R11_ADDR = (int)0x0B;

    // General Purpose Register 12
    public static final int R12_ADDR = (int)0x0C;

    // General Purpose Register 13
    public static final int R13_ADDR = (int)0x0D;

    // General Purpose Register 14
    public static final int R14_ADDR = (int)0x0E;

    // General Purpose Register 15
    public static final int R15_ADDR = (int)0x0F;

    // General Purpose Register 16
    public static final int R16_ADDR = (int)0x10;

    // General Purpose Register 17
    public static final int R17_ADDR = (int)0x11;

    // General Purpose Register 18
    public static final int R18_ADDR = (int)0x12;

    // General Purpose Register 19
    public static final int R19_ADDR = (int)0x13;

    // General Purpose Register 20
    public static final int R20_ADDR = (int)0x14;

    // General Purpose Register 21
    public static final int R21_ADDR = (int)0x15;

    // General Purpose Register 22
    public static final int R22_ADDR = (int)0x16;

    // General Purpose Register 23
    public static final int R23_ADDR = (int)0x17;

    // General Purpose Register 24
    public static final int R24_ADDR = (int)0x18;

    // General Purpose Register 25
    public static final int R25_ADDR = (int)0x19;

    // General Purpose Register 26 (XL)
    public static final int R26_ADDR = (int)0x1A;

    // General Purpose Register 27 (XH)
    public static final int R27_ADDR = (int)0x1B;

    // General Purpose Register 28 (YL)
    public static final int R28_ADDR = (int)0x1C;

    // General Purpose Register 29 (YH)
    public static final int R29_ADDR = (int)0x1D;

    // General Purpose Register 30 (ZL)
    public static final int R30_ADDR = (int)0x1E;

    // General Purpose Register 31 (ZH)
    public static final int R31_ADDR = (int)0x1F;

    // Stack Pointer Low
    public static final int SPL_ADDR = (int)0x5D;

    // Stack Pointer High
    public static final int SPH_ADDR = (int)0x5E;

    // Status Register
    public static final int SREG_ADDR = (int)0x5F;
    public static final int SREG_C = 0;  // Carry Flag
    public static final int SREG_Z = 1;  // Zero Flag
    public static final int SREG_N = 2;  // Negative Flag
    public static final int SREG_V = 3;  // Two's Complement Overflow Flag
    public static final int SREG_S = 4;  // Sign Flag (N ⊕ V)
    public static final int SREG_H = 5;  // Half Carry Flag
    public static final int SREG_T = 6;  // Transfer Bit
    public static final int SREG_I = 7;  // Global Interrupt Enable

    // 内存段定义
    // Program Flash Memory
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x7FFF;
    public static final int FLASH_SIZE = 32768;

    // Static RAM
    public static final int SRAM_START = (int)0x0100;
    public static final int SRAM_END = (int)0x08FF;
    public static final int SRAM_SIZE = 2048;

    // EEPROM
    public static final int EEPROM_START = (int)0x0000;
    public static final int EEPROM_END = (int)0x03FF;
    public static final int EEPROM_SIZE = 1024;

    // I/O Registers
    public static final int IO_START = (int)0x00;
    public static final int IO_END = (int)0x3F;
    public static final int IO_SIZE = 64;

    // Extended I/O Registers
    public static final int EXTIO_START = (int)0x40;
    public static final int EXTIO_END = (int)0xFF;
    public static final int EXTIO_SIZE = 192;

    // 外设定义
    // Port B Data Register
    public static final int PORTB_BASE = (int)0x23;
    public static final int PORTB_PORTB = (int)0x00000048;
    public static final int PORTB_DDRB = (int)0x00000047;
    public static final int PORTB_PINB = (int)0x00000046;
    public static final int PORTB_PB0 = 0;  // Port B, bit 0
    public static final int PORTB_PB1 = 1;  // Port B, bit 1
    public static final int PORTB_PB2 = 2;  // Port B, bit 2
    public static final int PORTB_PB3 = 3;  // Port B, bit 3
    public static final int PORTB_PB4 = 4;  // Port B, bit 4
    public static final int PORTB_PB5 = 5;  // Port B, bit 5
    public static final int PORTB_PB6 = 6;  // Port B, bit 6
    public static final int PORTB_PB7 = 7;  // Port B, bit 7

    // Port C Data Register
    public static final int PORTC_BASE = (int)0x26;
    public static final int PORTC_PORTC = (int)0x0000004E;
    public static final int PORTC_DDRC = (int)0x0000004D;
    public static final int PORTC_PINC = (int)0x0000004C;
    public static final int PORTC_PC0 = 0;  // Port C, bit 0
    public static final int PORTC_PC1 = 1;  // Port C, bit 1
    public static final int PORTC_PC2 = 2;  // Port C, bit 2
    public static final int PORTC_PC3 = 3;  // Port C, bit 3
    public static final int PORTC_PC4 = 4;  // Port C, bit 4
    public static final int PORTC_PC5 = 5;  // Port C, bit 5
    public static final int PORTC_PC6 = 6;  // Port C, bit 6

    // Port D Data Register
    public static final int PORTD_BASE = (int)0x29;
    public static final int PORTD_PORTD = (int)0x00000054;
    public static final int PORTD_DDRD = (int)0x00000053;
    public static final int PORTD_PIND = (int)0x00000052;
    public static final int PORTD_PD0 = 0;  // Port D, bit 0
    public static final int PORTD_PD1 = 1;  // Port D, bit 1
    public static final int PORTD_PD2 = 2;  // Port D, bit 2
    public static final int PORTD_PD3 = 3;  // Port D, bit 3
    public static final int PORTD_PD4 = 4;  // Port D, bit 4
    public static final int PORTD_PD5 = 5;  // Port D, bit 5
    public static final int PORTD_PD6 = 6;  // Port D, bit 6
    public static final int PORTD_PD7 = 7;  // Port D, bit 7

    // 8-bit Timer/Counter0
    public static final int TIMER0_BASE = (int)0x44;
    public static final int TIMER0_TCCR0A = (int)0x00000088;
    public static final int TIMER0_TCCR0A_WGM00 = 0;  // Waveform Generation Mode
    public static final int TIMER0_TCCR0A_WGM01 = 1;  // Waveform Generation Mode
    public static final int TIMER0_TCCR0A_COM0B0 = 4;  // Compare Output Mode for Channel B
    public static final int TIMER0_TCCR0A_COM0B1 = 5;  // Compare Output Mode for Channel B
    public static final int TIMER0_TCCR0A_COM0A0 = 6;  // Compare Output Mode for Channel A
    public static final int TIMER0_TCCR0A_COM0A1 = 7;  // Compare Output Mode for Channel A
    public static final int TIMER0_TCCR0B = (int)0x00000089;
    public static final int TIMER0_TCCR0B_CS00 = 0;  // Clock Select
    public static final int TIMER0_TCCR0B_CS01 = 1;  // Clock Select
    public static final int TIMER0_TCCR0B_CS02 = 2;  // Clock Select
    public static final int TIMER0_TCCR0B_WGM02 = 3;  // Waveform Generation Mode
    public static final int TIMER0_TCCR0B_FOC0B = 6;  // Force Output Compare B
    public static final int TIMER0_TCCR0B_FOC0A = 7;  // Force Output Compare A
    public static final int TIMER0_TCNT0 = (int)0x0000008A;
    public static final int TIMER0_OCR0A = (int)0x0000008B;
    public static final int TIMER0_OCR0B = (int)0x0000008C;
    public static final int TIMER0_TIMSK0 = (int)0x000000B2;
    public static final int TIMER0_TIMSK0_TOIE0 = 0;  // Timer/Counter0 Overflow Interrupt Enable
    public static final int TIMER0_TIMSK0_OCIE0A = 1;  // Timer/Counter0 Output Compare A Match Interrupt Enable
    public static final int TIMER0_TIMSK0_OCIE0B = 2;  // Timer/Counter0 Output Compare B Match Interrupt Enable
    public static final int TIMER0_TIFR0 = (int)0x00000079;
    public static final int TIMER0_TIFR0_TOV0 = 0;  // Timer/Counter0 Overflow Flag
    public static final int TIMER0_TIFR0_OCF0A = 1;  // Output Compare Flag 0A
    public static final int TIMER0_TIFR0_OCF0B = 2;  // Output Compare Flag 0B

    // Universal Synchronous/Asynchronous Receiver/Transmitter
    public static final int USART0_BASE = (int)0xC0;
    public static final int USART0_UDR0 = (int)0x00000186;
    public static final int USART0_UCSR0A = (int)0x00000180;
    public static final int USART0_UCSR0A_MPCM0 = 0;  // Multi-processor Communication Mode
    public static final int USART0_UCSR0A_U2X0 = 1;  // Double the USART Transmission Speed
    public static final int USART0_UCSR0A_UPE0 = 2;  // Parity Error
    public static final int USART0_UCSR0A_DOR0 = 3;  // Data OverRun
    public static final int USART0_UCSR0A_FE0 = 4;  // Frame Error
    public static final int USART0_UCSR0A_UDRE0 = 5;  // USART Data Register Empty
    public static final int USART0_UCSR0A_TXC0 = 6;  // USART Transmit Complete
    public static final int USART0_UCSR0A_RXC0 = 7;  // USART Receive Complete
    public static final int USART0_UCSR0B = (int)0x00000181;
    public static final int USART0_UCSR0B_TXB80 = 0;  // Transmit Data Bit 8
    public static final int USART0_UCSR0B_RXB80 = 1;  // Receive Data Bit 8
    public static final int USART0_UCSR0B_UCSZ02 = 2;  // Character Size
    public static final int USART0_UCSR0B_TXEN0 = 3;  // Transmitter Enable
    public static final int USART0_UCSR0B_RXEN0 = 4;  // Receiver Enable
    public static final int USART0_UCSR0B_UDRIE0 = 5;  // USART Data Register Empty Interrupt Enable
    public static final int USART0_UCSR0B_TXCIE0 = 6;  // TX Complete Interrupt Enable
    public static final int USART0_UCSR0B_RXCIE0 = 7;  // RX Complete Interrupt Enable
    public static final int USART0_UCSR0C = (int)0x00000182;
    public static final int USART0_UCSR0C_UCPOL0 = 0;  // Clock Polarity
    public static final int USART0_UCSR0C_UCSZ00 = 1;  // Character Size
    public static final int USART0_UCSR0C_UCSZ01 = 2;  // Character Size
    public static final int USART0_UCSR0C_USBS0 = 3;  // Stop Bit Select
    public static final int USART0_UCSR0C_UPM00 = 4;  // Parity Mode
    public static final int USART0_UCSR0C_UPM01 = 5;  // Parity Mode
    public static final int USART0_UCSR0C_UMSEL00 = 6;  // USART Mode Select
    public static final int USART0_UCSR0C_UMSEL01 = 7;  // USART Mode Select
    public static final int USART0_UBRR0 = (int)0x00000184;

    // Analog-to-Digital Converter
    public static final int ADC_BASE = (int)0x78;
    public static final int ADC_ADMUX = (int)0x000000F4;
    public static final int ADC_ADMUX_MUX0 = 0;  // Analog Channel Selection
    public static final int ADC_ADMUX_MUX1 = 1;  // Analog Channel Selection
    public static final int ADC_ADMUX_MUX2 = 2;  // Analog Channel Selection
    public static final int ADC_ADMUX_MUX3 = 3;  // Analog Channel Selection
    public static final int ADC_ADMUX_ADLAR = 5;  // ADC Left Adjust Result
    public static final int ADC_ADMUX_REFS0 = 6;  // Reference Selection
    public static final int ADC_ADMUX_REFS1 = 7;  // Reference Selection
    public static final int ADC_ADCSRA = (int)0x000000F2;
    public static final int ADC_ADCSRA_ADPS0 = 0;  // ADC Prescaler Select
    public static final int ADC_ADCSRA_ADPS1 = 1;  // ADC Prescaler Select
    public static final int ADC_ADCSRA_ADPS2 = 2;  // ADC Prescaler Select
    public static final int ADC_ADCSRA_ADIE = 3;  // ADC Interrupt Enable
    public static final int ADC_ADCSRA_ADIF = 4;  // ADC Interrupt Flag
    public static final int ADC_ADCSRA_ADATE = 5;  // ADC Auto Trigger Enable
    public static final int ADC_ADCSRA_ADSC = 6;  // ADC Start Conversion
    public static final int ADC_ADCSRA_ADEN = 7;  // ADC Enable
    public static final int ADC_ADCH = (int)0x000000F1;
    public static final int ADC_ADCL = (int)0x000000F0;

    // 中断向量定义
    public static final int IRQ_INT0 = 1;  // External Interrupt Request 0
    public static final int IRQ_INT1 = 2;  // External Interrupt Request 1
    public static final int IRQ_PCINT0 = 3;  // Pin Change Interrupt Request 0
    public static final int IRQ_PCINT1 = 4;  // Pin Change Interrupt Request 1
    public static final int IRQ_PCINT2 = 5;  // Pin Change Interrupt Request 2
    public static final int IRQ_WDT = 6;  // Watchdog Time-out Interrupt
    public static final int IRQ_TIMER2_COMPA = 7;  // Timer/Counter2 Compare Match A
    public static final int IRQ_TIMER2_COMPB = 8;  // Timer/Counter2 Compare Match B
    public static final int IRQ_TIMER2_OVF = 9;  // Timer/Counter2 Overflow
    public static final int IRQ_TIMER1_CAPT = 10;  // Timer/Counter1 Capture Event
    public static final int IRQ_TIMER1_COMPA = 11;  // Timer/Counter1 Compare Match A
    public static final int IRQ_TIMER1_COMPB = 12;  // Timer/Counter1 Compare Match B
    public static final int IRQ_TIMER1_OVF = 13;  // Timer/Counter1 Overflow
    public static final int IRQ_TIMER0_COMPA = 14;  // Timer/Counter0 Compare Match A
    public static final int IRQ_TIMER0_COMPB = 15;  // Timer/Counter0 Compare Match B
    public static final int IRQ_TIMER0_OVF = 16;  // Timer/Counter0 Overflow
    public static final int IRQ_SPI_STC = 17;  // SPI Serial Transfer Complete
    public static final int IRQ_USART_RX = 18;  // USART Rx Complete
    public static final int IRQ_USART_UDRE = 19;  // USART Data Register Empty
    public static final int IRQ_USART_TX = 20;  // USART Tx Complete
    public static final int IRQ_ADC = 21;  // ADC Conversion Complete
    public static final int IRQ_EE_READY = 22;  // EEPROM Ready
    public static final int IRQ_ANALOG_COMP = 23;  // Analog Comparator
    public static final int IRQ_TWI = 24;  // Two-wire Serial Interface
    public static final int IRQ_SPM_READY = 25;  // Store Program Memory Ready

    // 引脚定义
    public static final int PIN_PC6 = 1;  // Reset Pin
    public static final int PIN_PD0 = 2;  // Digital I/O, RX (USART)
    public static final int PIN_PD1 = 3;  // Digital I/O, TX (USART)
    public static final int PIN_PD2 = 4;  // Digital I/O, INT0
    public static final int PIN_PD3 = 5;  // Digital I/O, INT1, OC2B
    public static final int PIN_PD4 = 6;  // Digital I/O, T0, XCK
    public static final int PIN_VCC = 7;  // Supply Voltage
    public static final int PIN_GND = 8;  // Ground
    public static final int PIN_PB6 = 9;  // Digital I/O, XTAL1
    public static final int PIN_PB7 = 10;  // Digital I/O, XTAL2
    public static final int PIN_PD5 = 11;  // Digital I/O, T1, OC0B
    public static final int PIN_PD6 = 12;  // Digital I/O, AIN0, OC0A
    public static final int PIN_PD7 = 13;  // Digital I/O, AIN1
    public static final int PIN_PB0 = 14;  // Digital I/O, ICP1, CLKO
    public static final int PIN_PB1 = 15;  // Digital I/O, OC1A
    public static final int PIN_PB2 = 16;  // Digital I/O, SS, OC1B
    public static final int PIN_PB3 = 17;  // Digital I/O, MOSI, OC2A
    public static final int PIN_PB4 = 18;  // Digital I/O, MISO
    public static final int PIN_PB5 = 19;  // Digital I/O, SCK
    public static final int PIN_AVCC = 20;  // Supply Voltage for ADC
    public static final int PIN_AREF = 21;  // Analog Reference
    public static final int PIN_GND = 22;  // Ground
    public static final int PIN_PC0 = 23;  // Digital I/O, ADC0
    public static final int PIN_PC1 = 24;  // Digital I/O, ADC1
    public static final int PIN_PC2 = 25;  // Digital I/O, ADC2
    public static final int PIN_PC3 = 26;  // Digital I/O, ADC3
    public static final int PIN_PC4 = 27;  // Digital I/O, ADC4, SDA
    public static final int PIN_PC5 = 28;  // Digital I/O, ADC5, SCL

    public static native void atmega328p_init();
}
