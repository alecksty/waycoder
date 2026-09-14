package vml.device.microchip.attiny85;

/**
 * ATtiny85 寄存器定义
 * 生成自: Microchip/AVR/ATtiny85
 * 版本: 1.0
 */
public final class ATtiny85 {
    private ATtiny85() {} // 工具类
    // CPU架构: AVR, 8位, 1000000 Hz

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

    // Register pair X (R27:R26)
    public static final int X_ADDR = (int)0x1A;

    // Register pair Y (R29:R28)
    public static final int Y_ADDR = (int)0x1C;

    // Register pair Z (R31:R30)
    public static final int Z_ADDR = (int)0x1E;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x3D;

    // Status Register
    public static final int SREG_ADDR = (int)0x3F;
    public static final int SREG_C = 0;  // Carry Flag
    public static final int SREG_Z = 1;  // Zero Flag
    public static final int SREG_N = 2;  // Negative Flag
    public static final int SREG_V = 3;  // Two's Complement Overflow Flag
    public static final int SREG_S = 4;  // Sign Flag (N xor V)
    public static final int SREG_H = 5;  // Half Carry Flag
    public static final int SREG_T = 6;  // Transfer Bit
    public static final int SREG_I = 7;  // Global Interrupt Enable

    // 内存段定义
    // Program Flash (8KB)
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x1FFF;
    public static final int FLASH_SIZE = 8192;

    // Internal SRAM (512B)
    public static final int SRAM_START = (int)0x0060;
    public static final int SRAM_END = (int)0x025F;
    public static final int SRAM_SIZE = 512;

    // EEPROM (512B)
    public static final int EEPROM_START = (int)0x0000;
    public static final int EEPROM_END = (int)0x01FF;
    public static final int EEPROM_SIZE = 512;

    // I/O Registers
    public static final int IO_START = (int)0x00;
    public static final int IO_END = (int)0x3F;
    public static final int IO_SIZE = 64;

    // 外设定义
    // Port A
    public static final int PORTA_BASE = (int)0x20;
    public static final int PORTA_PINA = (int)0x00000040;
    public static final int PORTA_DDRA = (int)0x00000041;
    public static final int PORTA_PORTA = (int)0x00000042;

    // Port B
    public static final int PORTB_BASE = (int)0x18;
    public static final int PORTB_PINB = (int)0x0000002E;
    public static final int PORTB_DDRB = (int)0x0000002F;
    public static final int PORTB_PORTB = (int)0x00000030;

    // Timer/Counter0
    public static final int TIPO_BASE = (int)0x20;
    public static final int TIPO_TCCR0A = (int)0x00000040;
    public static final int TIPO_TCCR0A_WGM00 = 0;  // Waveform Generation Mode
    public static final int TIPO_TCCR0A_WGM01 = 1;  // Waveform Generation Mode
    public static final int TIPO_TCCR0A_COM0B0 = 4;  // Compare Output Mode B
    public static final int TIPO_TCCR0A_COM0B1 = 5;  // Compare Output Mode B
    public static final int TIPO_TCCR0A_COM0A0 = 6;  // Compare Output Mode A
    public static final int TIPO_TCCR0A_COM0A1 = 7;  // Compare Output Mode A
    public static final int TIPO_TCCR0B = (int)0x00000041;
    public static final int TIPO_TCCR0B_CS00 = 0;  // Clock Select
    public static final int TIPO_TCCR0B_CS01 = 1;  // Clock Select
    public static final int TIPO_TCCR0B_CS02 = 2;  // Clock Select
    public static final int TIPO_TCCR0B_WGM02 = 3;  // Waveform Generation Mode
    public static final int TIPO_TCCR0B_FOC0B = 6;  // Force Output Compare B
    public static final int TIPO_TCCR0B_FOC0A = 7;  // Force Output Compare A
    public static final int TIPO_TCNT0 = (int)0x00000042;
    public static final int TIPO_OCR0A = (int)0x00000043;
    public static final int TIPO_OCR0B = (int)0x00000044;
    public static final int TIPO_TIMSK = (int)0x00000059;
    public static final int TIPO_TIMSK_TOIE0 = 0;  // Timer/Counter0 Overflow Interrupt Enable
    public static final int TIPO_TIMSK_OCIE0A = 1;  // Output Compare A Match Interrupt Enable
    public static final int TIPO_TIMSK_OCIE0B = 2;  // Output Compare B Match Interrupt Enable
    public static final int TIPO_TIFR = (int)0x00000058;
    public static final int TIPO_TIFR_TOV0 = 0;  // Timer/Counter0 Overflow Flag
    public static final int TIPO_TIFR_OCF0A = 1;  // Output Compare A Flag
    public static final int TIPO_TIFR_OCF0B = 2;  // Output Compare B Flag

    // Timer/Counter1
    public static final int TMR1_BASE = (int)0x28;
    public static final int TMR1_TCCR1A = (int)0x00000050;
    public static final int TMR1_TCCR1A_PCM1 = 0;  // PWM Mode
    public static final int TMR1_TCCR1A_COM1A = 0;  // Compare Output Mode A
    public static final int TMR1_TCCR1A_COM1B = 0;  // Compare Output Mode B
    public static final int TMR1_TCCR1A_WG13 = 1;  // Waveform Generation Mode
    public static final int TMR1_TCCR1A_WG10 = 0;  // Waveform Generation Mode
    public static final int TMR1_TCCR1B = (int)0x00000051;
    public static final int TMR1_TCCR1B_CTC1 = 7;  // Clear Timer on Compare
    public static final int TMR1_TCCR1B_WGM13 = 4;  // Waveform Generation Mode
    public static final int TMR1_TCCR1B_WGM12 = 3;  // Waveform Generation Mode
    public static final int TMR1_TCCR1B_CS1 = 0;  // Clock Select
    public static final int TMR1_TCNT1 = (int)0x00000052;
    public static final int TMR1_OCR1A = (int)0x00000054;
    public static final int TMR1_OCR1B = (int)0x00000056;
    public static final int TMR1_OCR1C = (int)0x00000058;
    public static final int TMR1_TIMSK1 = (int)0x0000005B;
    public static final int TMR1_TIFR1 = (int)0x0000005A;

    // ADC Multiplexer
    public static final int ADMUX_BASE = (int)0x12;
    public static final int ADMUX_ADMUX = (int)0x00000024;
    public static final int ADMUX_ADMUX_MUX = 0;  // Analog Channel Selection
    public static final int ADMUX_ADMUX_ADLAR = 5;  // ADC Left Adjust Result
    public static final int ADMUX_ADMUX_REFS = 0;  // Reference Selection
    public static final int ADMUX_ADCSRA = (int)0x00000025;
    public static final int ADMUX_ADCSRA_ADPS = 0;  // ADC Prescaler Select
    public static final int ADMUX_ADCSRA_ADIE = 3;  // ADC Interrupt Enable
    public static final int ADMUX_ADCSRA_ADIF = 4;  // ADC Interrupt Flag
    public static final int ADMUX_ADCSRA_ADATE = 5;  // ADC Auto Trigger Enable
    public static final int ADMUX_ADCSRA_ADSC = 6;  // ADC Start Conversion
    public static final int ADMUX_ADCSRA_ADEN = 7;  // ADC Enable
    public static final int ADMUX_ADCH = (int)0x00000026;
    public static final int ADMUX_ADCL = (int)0x00000027;

    // Universal Serial Interface
    public static final int USI_BASE = (int)0x18;
    public static final int USI_USIDR = (int)0x00000030;
    public static final int USI_USISR = (int)0x00000031;
    public static final int USI_USISR_USICNT = 0;  // Counter
    public static final int USI_USISR_USIDC = 4;  // Data Register
    public static final int USI_USISR_USIPF = 5;  // Stop Cond Flag
    public static final int USI_USISR_USIOV = 6;  // Overflow Flag
    public static final int USI_USISR_USISIF = 7;  // Start Cond Interrupt Flag
    public static final int USI_USICR = (int)0x00000032;
    public static final int USI_USICR_USICS = 0;  // Clock Source Select
    public static final int USI_USICR_USISCL = 2;  // SCL strobe
    public static final int USI_USICR_USIOW = 3;  // SDA output override
    public static final int USI_USICR_USIOE = 4;  // Output Enable
    public static final int USI_USICR_USISRE = 5;  // Start Recognition Enable
    public static final int USI_USICR_USIORE = 6;  // Stop Recognition Enable
    public static final int USI_USICR_USIGIE = 7;  // Global Interrupt Enable
    public static final int USI_USIPORT = (int)0x00000033;

    // MCU Control
    public static final int MCUCR_BASE = (int)0x35;
    public static final int MCUCR_MCUCR = (int)0x0000006A;
    public static final int MCUCR_MCUCR_ISC = 0;  // Interrupt Sense Control
    public static final int MCUCR_MCUCR_SE = 4;  // Sleep Enable
    public static final int MCUCR_MCUCR_SM = 0;  // Sleep Mode
    public static final int MCUCR_MCUCSR = (int)0x0000006B;
    public static final int MCUCR_MCUCSR_PORF = 0;  // Power-on Reset Flag
    public static final int MCUCR_MCUCSR_EXTRF = 1;  // External Reset Flag
    public static final int MCUCR_MCUCSR_WDRF = 2;  // Watchdog Reset Flag
    public static final int MCUCR_MCUCSR_BORF = 4;  // Brown-out Reset Flag

    // Watchdog Timer
    public static final int WDTCR_BASE = (int)0x21;
    public static final int WDTCR_WDTCR = (int)0x00000042;
    public static final int WDTCR_WDTCR_WDP = 0;  // Watchdog Prescaler
    public static final int WDTCR_WDTCR_WDE = 3;  // Watchdog Enable
    public static final int WDTCR_WDTCR_WDIE = 4;  // Watchdog Interrupt Enable

    // EEPROM
    public static final int EEPR_BASE = (int)0x1C;
    public static final int EEPR_EEAR = (int)0x0000003A;
    public static final int EEPR_EEDR = (int)0x00000039;
    public static final int EEPR_EECR = (int)0x0000003B;
    public static final int EEPR_EECR_EEPM = 0;  // EEPROM Programming Mode
    public static final int EEPR_EECR_EERIE = 3;  // EEPROM Ready Interrupt Enable
    public static final int EEPR_EECR_EEWE = 2;  // EEPROM Write Enable
    public static final int EEPR_EECR_EEMWE = 1;  // EEPROM Master Write Enable
    public static final int EEPR_EECR_EERE = 0;  // EEPROM Read Enable

    // External Interrupt
    public static final int GIMSK_BASE = (int)0x3B;
    public static final int GIMSK_GIMSK = (int)0x00000076;
    public static final int GIMSK_GIMSK_INT0 = 0;  // External Interrupt Request 0 Enable
    public static final int GIMSK_GIMSK_PCIE = 1;  // Pin Change Interrupt Enable
    public static final int GIMSK_GIFR = (int)0x00000077;
    public static final int GIMSK_GIFR_INTF0 = 0;  // External Interrupt Flag 0
    public static final int GIMSK_GIFR_PCIF = 1;  // Pin Change Interrupt Flag

    // Pin Change Mask
    public static final int PCMSK_BASE = (int)0x15;
    public static final int PCMSK_PCMSK = (int)0x0000002A;

    // Store Program Memory
    public static final int SPMCSR_BASE = (int)0x37;
    public static final int SPMCSR_SPMCSR = (int)0x0000006E;
    public static final int SPMCSR_SPMCSR_SPMCR = 0;  // SPM Mode
    public static final int SPMCSR_SPMCSR_PGERS = 1;  // Page Erase
    public static final int SPMCSR_SPMCSR_PGWRT = 2;  // Page Write
    public static final int SPMCSR_SPMCSR_BLBSET = 3;  // Boot Lock Bits Set
    public static final int SPMCSR_SPMCSR_RWWSRE = 4;  // Read-While-Read Strobe Enable
    public static final int SPMCSR_SPMCSR_SIGRD = 5;  // Signature Row Read
    public static final int SPMCSR_SPMCSR_SPMEN = 7;  // SPM Enable

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // External Reset, Power-on Reset, Brown-out Reset
    public static final int IRQ_INT0 = 1;  // External Interrupt Request 0
    public static final int IRQ_PCINT0 = 2;  // Pin Change
    public static final int IRQ_WDT = 3;  // Watchdog Timeout
    public static final int IRQ_TIM1_COMPA = 4;  // Timer/Counter1 Compare Match A
    public static final int IRQ_TIM1_OVF = 5;  // Timer/Counter1 Overflow
    public static final int IRQ_TIM0_COMPA = 6;  // Timer/Counter0 Compare Match A
    public static final int IRQ_TIM0_OVF = 7;  // Timer/Counter0 Overflow
    public static final int IRQ_SPI_STC = 8;  // SPI Serial Transfer Complete
    public static final int IRQ_ADC = 9;  // ADC Conversion Complete
    public static final int IRQ_USI_START = 10;  // USI Start Condition
    public static final int IRQ_USI_OVF = 11;  // USI Overflow
    public static final int IRQ_EE_READY = 12;  // EEPROM Ready

    // 引脚定义
    public static final int PIN_PB5 = 1;  // RESET - ADC0 - dW
    public static final int PIN_PB3 = 2;  // XTAL1 - CLKI - ADC3
    public static final int PIN_PB4 = 3;  // XTAL2 - ADC2
    public static final int PIN_PB0 = 4;  // MOSI - AI - ADC0 - T0 - INT0
    public static final int PIN_PB1 = 5;  // MISO - AI - ADC1 - OC1A - INT1
    public static final int PIN_PB2 = 6;  // SCK - AI - ADC3 - OC1B
    public static final int PIN_VCC = 7;  // Supply Voltage
    public static final int PIN_GND = 8;  // Ground

    public static native void attiny85_init();
}
