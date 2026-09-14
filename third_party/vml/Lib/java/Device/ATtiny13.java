package vml.device.atmel.attiny13;

/**
 * ATtiny13 寄存器定义
 * 生成自: Atmel/AVR/ATtiny13
 * 版本: 1.0
 */
public final class ATtiny13 {
    private ATtiny13() {} // 工具类
    // CPU架构: AVR, 8位, 20000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x01;

    public static final int R2_ADDR = (int)0x02;

    public static final int R16_ADDR = (int)0x10;

    public static final int R17_ADDR = (int)0x11;

    // XL
    public static final int R26_ADDR = (int)0x1A;

    // XH
    public static final int R27_ADDR = (int)0x1B;

    // YL
    public static final int R28_ADDR = (int)0x1C;

    // YH
    public static final int R29_ADDR = (int)0x1D;

    // ZL
    public static final int R30_ADDR = (int)0x1E;

    // ZH
    public static final int R31_ADDR = (int)0x1F;

    // Stack Pointer Low
    public static final int SPL_ADDR = (int)0x5D;

    // Stack Pointer High
    public static final int SPH_ADDR = (int)0x5E;

    // Status Register
    public static final int SREG_ADDR = (int)0x5F;

    // 内存段定义
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x03FF;
    public static final int FLASH_SIZE = 1024;

    public static final int SRAM_START = (int)0x0060;
    public static final int SRAM_END = (int)0x009F;
    public static final int SRAM_SIZE = 64;

    public static final int EEPROM_START = (int)0x0000;
    public static final int EEPROM_END = (int)0x003F;
    public static final int EEPROM_SIZE = 64;

    public static final int IO_START = (int)0x00;
    public static final int IO_END = (int)0x1F;
    public static final int IO_SIZE = 32;

    public static final int EXTIO_START = (int)0x20;
    public static final int EXTIO_END = (int)0x5F;
    public static final int EXTIO_SIZE = 64;

    // 外设定义
    // Port B (only port)
    public static final int PORTB_BASE = (int)0x18;
    public static final int PORTB_DDRB = (int)0x0000002F;
    public static final int PORTB_PORTB = (int)0x00000030;
    public static final int PORTB_PINB = (int)0x00000031;
    public static final int PORTB_PB0 = 0;  // Port B bit 0
    public static final int PORTB_PB1 = 1;  // Port B bit 1
    public static final int PORTB_PB2 = 2;  // Port B bit 2
    public static final int PORTB_PB3 = 3;  // Port B bit 3
    public static final int PORTB_PB4 = 4;  // Port B bit 4
    public static final int PORTB_PB5 = 5;  // Port B bit 5

    // 8-bit Timer/Counter0
    public static final int TIMER0_BASE = (int)0x33;
    public static final int TIMER0_TCCR0A = (int)0x00000066;
    public static final int TIMER0_TCCR0B = (int)0x00000066;
    public static final int TIMER0_TCNT0 = (int)0x00000065;
    public static final int TIMER0_OCR0A = (int)0x00000069;
    public static final int TIMER0_OCR0B = (int)0x00000068;
    public static final int TIMER0_TIMSK0 = (int)0x0000006C;
    public static final int TIMER0_TIFR0 = (int)0x0000006B;

    // Analog-to-Digital
    public static final int ADC_BASE = (int)0x04;
    public static final int ADC_ADMUX = (int)0x0000000B;
    public static final int ADC_ADCSRA = (int)0x0000000A;
    public static final int ADC_ADCL = (int)0x00000008;
    public static final int ADC_ADCH = (int)0x00000009;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_INT0 = 2;  // External Interrupt 0
    public static final int IRQ_PCINT0 = 3;  // Pin Change Interrupt
    public static final int IRQ_TIM0_OVF = 4;  // Timer0 Overflow
    public static final int IRQ_TIM0_COMPA = 5;  // Timer0 Compare A
    public static final int IRQ_WDT = 6;  // Watchdog Timeout
    public static final int IRQ_ADC = 7;  // ADC Conversion Complete

    public static native void attiny13_init();
}
