// ATmega328P 设备定义 - Dart 库
// 生成自: Atmel/AVR/ATmega328P
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

class ATmega328PDevice {
  static const String deviceName = "ATmega328P";
  static const String manufacturer = "Atmel";
  static const String family = "AVR";
  static const String version = "1.0";
  static const String architecture = "AVR";
  static const int bits = 8;
  static const int clockFrequency = 16000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // General Purpose Register 0
  static const int R1_ADDR = 0x01;  // General Purpose Register 1
  static const int R2_ADDR = 0x02;  // General Purpose Register 2
  static const int R3_ADDR = 0x03;  // General Purpose Register 3
  static const int R4_ADDR = 0x04;  // General Purpose Register 4
  static const int R5_ADDR = 0x05;  // General Purpose Register 5
  static const int R6_ADDR = 0x06;  // General Purpose Register 6
  static const int R7_ADDR = 0x07;  // General Purpose Register 7
  static const int R8_ADDR = 0x08;  // General Purpose Register 8
  static const int R9_ADDR = 0x09;  // General Purpose Register 9
  static const int R10_ADDR = 0x0A;  // General Purpose Register 10
  static const int R11_ADDR = 0x0B;  // General Purpose Register 11
  static const int R12_ADDR = 0x0C;  // General Purpose Register 12
  static const int R13_ADDR = 0x0D;  // General Purpose Register 13
  static const int R14_ADDR = 0x0E;  // General Purpose Register 14
  static const int R15_ADDR = 0x0F;  // General Purpose Register 15
  static const int R16_ADDR = 0x10;  // General Purpose Register 16
  static const int R17_ADDR = 0x11;  // General Purpose Register 17
  static const int R18_ADDR = 0x12;  // General Purpose Register 18
  static const int R19_ADDR = 0x13;  // General Purpose Register 19
  static const int R20_ADDR = 0x14;  // General Purpose Register 20
  static const int R21_ADDR = 0x15;  // General Purpose Register 21
  static const int R22_ADDR = 0x16;  // General Purpose Register 22
  static const int R23_ADDR = 0x17;  // General Purpose Register 23
  static const int R24_ADDR = 0x18;  // General Purpose Register 24
  static const int R25_ADDR = 0x19;  // General Purpose Register 25
  static const int R26_ADDR = 0x1A;  // General Purpose Register 26 (XL)
  static const int R27_ADDR = 0x1B;  // General Purpose Register 27 (XH)
  static const int R28_ADDR = 0x1C;  // General Purpose Register 28 (YL)
  static const int R29_ADDR = 0x1D;  // General Purpose Register 29 (YH)
  static const int R30_ADDR = 0x1E;  // General Purpose Register 30 (ZL)
  static const int R31_ADDR = 0x1F;  // General Purpose Register 31 (ZH)
  static const int SPL_ADDR = 0x5D;  // Stack Pointer Low
  static const int SPH_ADDR = 0x5E;  // Stack Pointer High
  static const int SREG_ADDR = 0x5F;  // Status Register
  static const int SREG_C_BIT = 0;  // Carry Flag
  static const int SREG_Z_BIT = 1;  // Zero Flag
  static const int SREG_N_BIT = 2;  // Negative Flag
  static const int SREG_V_BIT = 3;  // Two's Complement Overflow Flag
  static const int SREG_S_BIT = 4;  // Sign Flag (N ⊕ V)
  static const int SREG_H_BIT = 5;  // Half Carry Flag
  static const int SREG_T_BIT = 6;  // Transfer Bit
  static const int SREG_I_BIT = 7;  // Global Interrupt Enable

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x7FFF;
  static const int FLASH_SIZE = 32768;  // Program Flash Memory
  static const int SRAM_START = 0x0100;
  static const int SRAM_END = 0x08FF;
  static const int SRAM_SIZE = 2048;  // Static RAM
  static const int EEPROM_START = 0x0000;
  static const int EEPROM_END = 0x03FF;
  static const int EEPROM_SIZE = 1024;  // EEPROM
  static const int IO_START = 0x00;
  static const int IO_END = 0x3F;
  static const int IO_SIZE = 64;  // I/O Registers
  static const int EXTIO_START = 0x40;
  static const int EXTIO_END = 0xFF;
  static const int EXTIO_SIZE = 192;  // Extended I/O Registers

  // 外设定义
  // Port B Data Register
  static const int PORTB_BASE = 0x23;
  static const int PORTB_PORTB_ADDR = 0x25;
  static const int PORTB_DDRB_ADDR = 0x24;
  static const int PORTB_PINB_ADDR = 0x23;
  // Port C Data Register
  static const int PORTC_BASE = 0x26;
  static const int PORTC_PORTC_ADDR = 0x28;
  static const int PORTC_DDRC_ADDR = 0x27;
  static const int PORTC_PINC_ADDR = 0x26;
  // Port D Data Register
  static const int PORTD_BASE = 0x29;
  static const int PORTD_PORTD_ADDR = 0x2B;
  static const int PORTD_DDRD_ADDR = 0x2A;
  static const int PORTD_PIND_ADDR = 0x29;
  // 8-bit Timer/Counter0
  static const int TIMER0_BASE = 0x44;
  static const int TIMER0_TCCR0A_ADDR = 0x44;
  static const int TIMER0_TCCR0A_WGM00_BIT = 0;  // Waveform Generation Mode
  static const int TIMER0_TCCR0A_WGM01_BIT = 1;  // Waveform Generation Mode
  static const int TIMER0_TCCR0A_COM0B0_BIT = 4;  // Compare Output Mode for Channel B
  static const int TIMER0_TCCR0A_COM0B1_BIT = 5;  // Compare Output Mode for Channel B
  static const int TIMER0_TCCR0A_COM0A0_BIT = 6;  // Compare Output Mode for Channel A
  static const int TIMER0_TCCR0A_COM0A1_BIT = 7;  // Compare Output Mode for Channel A
  static const int TIMER0_TCCR0B_ADDR = 0x45;
  static const int TIMER0_TCCR0B_CS00_BIT = 0;  // Clock Select
  static const int TIMER0_TCCR0B_CS01_BIT = 1;  // Clock Select
  static const int TIMER0_TCCR0B_CS02_BIT = 2;  // Clock Select
  static const int TIMER0_TCCR0B_WGM02_BIT = 3;  // Waveform Generation Mode
  static const int TIMER0_TCCR0B_FOC0B_BIT = 6;  // Force Output Compare B
  static const int TIMER0_TCCR0B_FOC0A_BIT = 7;  // Force Output Compare A
  static const int TIMER0_TCNT0_ADDR = 0x46;
  static const int TIMER0_OCR0A_ADDR = 0x47;
  static const int TIMER0_OCR0B_ADDR = 0x48;
  static const int TIMER0_TIMSK0_ADDR = 0x6E;
  static const int TIMER0_TIMSK0_TOIE0_BIT = 0;  // Timer/Counter0 Overflow Interrupt Enable
  static const int TIMER0_TIMSK0_OCIE0A_BIT = 1;  // Timer/Counter0 Output Compare A Match Interrupt Enable
  static const int TIMER0_TIMSK0_OCIE0B_BIT = 2;  // Timer/Counter0 Output Compare B Match Interrupt Enable
  static const int TIMER0_TIFR0_ADDR = 0x35;
  static const int TIMER0_TIFR0_TOV0_BIT = 0;  // Timer/Counter0 Overflow Flag
  static const int TIMER0_TIFR0_OCF0A_BIT = 1;  // Output Compare Flag 0A
  static const int TIMER0_TIFR0_OCF0B_BIT = 2;  // Output Compare Flag 0B
  // Universal Synchronous/Asynchronous Receiver/Transmitter
  static const int USART0_BASE = 0xC0;
  static const int USART0_UDR0_ADDR = 0xC6;
  static const int USART0_UCSR0A_ADDR = 0xC0;
  static const int USART0_UCSR0A_MPCM0_BIT = 0;  // Multi-processor Communication Mode
  static const int USART0_UCSR0A_U2X0_BIT = 1;  // Double the USART Transmission Speed
  static const int USART0_UCSR0A_UPE0_BIT = 2;  // Parity Error
  static const int USART0_UCSR0A_DOR0_BIT = 3;  // Data OverRun
  static const int USART0_UCSR0A_FE0_BIT = 4;  // Frame Error
  static const int USART0_UCSR0A_UDRE0_BIT = 5;  // USART Data Register Empty
  static const int USART0_UCSR0A_TXC0_BIT = 6;  // USART Transmit Complete
  static const int USART0_UCSR0A_RXC0_BIT = 7;  // USART Receive Complete
  static const int USART0_UCSR0B_ADDR = 0xC1;
  static const int USART0_UCSR0B_TXB80_BIT = 0;  // Transmit Data Bit 8
  static const int USART0_UCSR0B_RXB80_BIT = 1;  // Receive Data Bit 8
  static const int USART0_UCSR0B_UCSZ02_BIT = 2;  // Character Size
  static const int USART0_UCSR0B_TXEN0_BIT = 3;  // Transmitter Enable
  static const int USART0_UCSR0B_RXEN0_BIT = 4;  // Receiver Enable
  static const int USART0_UCSR0B_UDRIE0_BIT = 5;  // USART Data Register Empty Interrupt Enable
  static const int USART0_UCSR0B_TXCIE0_BIT = 6;  // TX Complete Interrupt Enable
  static const int USART0_UCSR0B_RXCIE0_BIT = 7;  // RX Complete Interrupt Enable
  static const int USART0_UCSR0C_ADDR = 0xC2;
  static const int USART0_UCSR0C_UCPOL0_BIT = 0;  // Clock Polarity
  static const int USART0_UCSR0C_UCSZ00_BIT = 1;  // Character Size
  static const int USART0_UCSR0C_UCSZ01_BIT = 2;  // Character Size
  static const int USART0_UCSR0C_USBS0_BIT = 3;  // Stop Bit Select
  static const int USART0_UCSR0C_UPM00_BIT = 4;  // Parity Mode
  static const int USART0_UCSR0C_UPM01_BIT = 5;  // Parity Mode
  static const int USART0_UCSR0C_UMSEL00_BIT = 6;  // USART Mode Select
  static const int USART0_UCSR0C_UMSEL01_BIT = 7;  // USART Mode Select
  static const int USART0_UBRR0_ADDR = 0xC4;
  // Analog-to-Digital Converter
  static const int ADC_BASE = 0x78;
  static const int ADC_ADMUX_ADDR = 0x7C;
  static const int ADC_ADMUX_MUX0_BIT = 0;  // Analog Channel Selection
  static const int ADC_ADMUX_MUX1_BIT = 1;  // Analog Channel Selection
  static const int ADC_ADMUX_MUX2_BIT = 2;  // Analog Channel Selection
  static const int ADC_ADMUX_MUX3_BIT = 3;  // Analog Channel Selection
  static const int ADC_ADMUX_ADLAR_BIT = 5;  // ADC Left Adjust Result
  static const int ADC_ADMUX_REFS0_BIT = 6;  // Reference Selection
  static const int ADC_ADMUX_REFS1_BIT = 7;  // Reference Selection
  static const int ADC_ADCSRA_ADDR = 0x7A;
  static const int ADC_ADCSRA_ADPS0_BIT = 0;  // ADC Prescaler Select
  static const int ADC_ADCSRA_ADPS1_BIT = 1;  // ADC Prescaler Select
  static const int ADC_ADCSRA_ADPS2_BIT = 2;  // ADC Prescaler Select
  static const int ADC_ADCSRA_ADIE_BIT = 3;  // ADC Interrupt Enable
  static const int ADC_ADCSRA_ADIF_BIT = 4;  // ADC Interrupt Flag
  static const int ADC_ADCSRA_ADATE_BIT = 5;  // ADC Auto Trigger Enable
  static const int ADC_ADCSRA_ADSC_BIT = 6;  // ADC Start Conversion
  static const int ADC_ADCSRA_ADEN_BIT = 7;  // ADC Enable
  static const int ADC_ADCH_ADDR = 0x79;
  static const int ADC_ADCL_ADDR = 0x78;

  // 中断向量定义
  static const int INT_INT0 = 1;  // External Interrupt Request 0
  static const int INT_INT1 = 2;  // External Interrupt Request 1
  static const int INT_PCINT0 = 3;  // Pin Change Interrupt Request 0
  static const int INT_PCINT1 = 4;  // Pin Change Interrupt Request 1
  static const int INT_PCINT2 = 5;  // Pin Change Interrupt Request 2
  static const int INT_WDT = 6;  // Watchdog Time-out Interrupt
  static const int INT_TIMER2_COMPA = 7;  // Timer/Counter2 Compare Match A
  static const int INT_TIMER2_COMPB = 8;  // Timer/Counter2 Compare Match B
  static const int INT_TIMER2_OVF = 9;  // Timer/Counter2 Overflow
  static const int INT_TIMER1_CAPT = 10;  // Timer/Counter1 Capture Event
  static const int INT_TIMER1_COMPA = 11;  // Timer/Counter1 Compare Match A
  static const int INT_TIMER1_COMPB = 12;  // Timer/Counter1 Compare Match B
  static const int INT_TIMER1_OVF = 13;  // Timer/Counter1 Overflow
  static const int INT_TIMER0_COMPA = 14;  // Timer/Counter0 Compare Match A
  static const int INT_TIMER0_COMPB = 15;  // Timer/Counter0 Compare Match B
  static const int INT_TIMER0_OVF = 16;  // Timer/Counter0 Overflow
  static const int INT_SPI_STC = 17;  // SPI Serial Transfer Complete
  static const int INT_USART_RX = 18;  // USART Rx Complete
  static const int INT_USART_UDRE = 19;  // USART Data Register Empty
  static const int INT_USART_TX = 20;  // USART Tx Complete
  static const int INT_ADC = 21;  // ADC Conversion Complete
  static const int INT_EE_READY = 22;  // EEPROM Ready
  static const int INT_ANALOG_COMP = 23;  // Analog Comparator
  static const int INT_TWI = 24;  // Two-wire Serial Interface
  static const int INT_SPM_READY = 25;  // Store Program Memory Ready

  // 引脚定义
  static const int PIN_PC6 = 1;  // Reset Pin
  static const int PIN_PD0 = 2;  // Digital I/O, RX (USART)
  static const int PIN_PD1 = 3;  // Digital I/O, TX (USART)
  static const int PIN_PD2 = 4;  // Digital I/O, INT0
  static const int PIN_PD3 = 5;  // Digital I/O, INT1, OC2B
  static const int PIN_PD4 = 6;  // Digital I/O, T0, XCK
  static const int PIN_VCC = 7;  // Supply Voltage
  static const int PIN_GND = 8;  // Ground
  static const int PIN_PB6 = 9;  // Digital I/O, XTAL1
  static const int PIN_PB7 = 10;  // Digital I/O, XTAL2
  static const int PIN_PD5 = 11;  // Digital I/O, T1, OC0B
  static const int PIN_PD6 = 12;  // Digital I/O, AIN0, OC0A
  static const int PIN_PD7 = 13;  // Digital I/O, AIN1
  static const int PIN_PB0 = 14;  // Digital I/O, ICP1, CLKO
  static const int PIN_PB1 = 15;  // Digital I/O, OC1A
  static const int PIN_PB2 = 16;  // Digital I/O, SS, OC1B
  static const int PIN_PB3 = 17;  // Digital I/O, MOSI, OC2A
  static const int PIN_PB4 = 18;  // Digital I/O, MISO
  static const int PIN_PB5 = 19;  // Digital I/O, SCK
  static const int PIN_AVCC = 20;  // Supply Voltage for ADC
  static const int PIN_AREF = 21;  // Analog Reference
  static const int PIN_GND = 22;  // Ground
  static const int PIN_PC0 = 23;  // Digital I/O, ADC0
  static const int PIN_PC1 = 24;  // Digital I/O, ADC1
  static const int PIN_PC2 = 25;  // Digital I/O, ADC2
  static const int PIN_PC3 = 26;  // Digital I/O, ADC3
  static const int PIN_PC4 = 27;  // Digital I/O, ADC4, SDA
  static const int PIN_PC5 = 28;  // Digital I/O, ADC5, SCL

}
