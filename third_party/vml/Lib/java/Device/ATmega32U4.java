package vml.device.atmel.atmega32u4;

/**
 * ATmega32U4 寄存器定义
 * 生成自: Atmel/AVR/ATmega32U4
 * 版本: 1.0
 */
public final class ATmega32U4 {
    private ATmega32U4() {} // 工具类
    // CPU架构: AVR, 8位, 16000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x01;

    public static final int R2_ADDR = (int)0x02;

    public static final int R3_ADDR = (int)0x03;

    public static final int R4_ADDR = (int)0x04;

    public static final int R5_ADDR = (int)0x05;

    public static final int R6_ADDR = (int)0x06;

    public static final int R7_ADDR = (int)0x07;

    public static final int R8_ADDR = (int)0x08;

    public static final int R9_ADDR = (int)0x09;

    public static final int R10_ADDR = (int)0x0A;

    public static final int R11_ADDR = (int)0x0B;

    public static final int R12_ADDR = (int)0x0C;

    public static final int R13_ADDR = (int)0x0D;

    public static final int R14_ADDR = (int)0x0E;

    public static final int R15_ADDR = (int)0x0F;

    public static final int R16_ADDR = (int)0x10;

    public static final int R17_ADDR = (int)0x11;

    public static final int R18_ADDR = (int)0x12;

    public static final int R19_ADDR = (int)0x13;

    public static final int R20_ADDR = (int)0x14;

    public static final int R21_ADDR = (int)0x15;

    public static final int R22_ADDR = (int)0x16;

    public static final int R23_ADDR = (int)0x17;

    public static final int R24_ADDR = (int)0x18;

    public static final int R25_ADDR = (int)0x19;

    public static final int R26_ADDR = (int)0x1A;

    public static final int R27_ADDR = (int)0x1B;

    public static final int R28_ADDR = (int)0x1C;

    public static final int R29_ADDR = (int)0x1D;

    public static final int R30_ADDR = (int)0x1E;

    public static final int R31_ADDR = (int)0x1F;

    public static final int SPL_ADDR = (int)0x5D;

    public static final int SPH_ADDR = (int)0x5E;

    public static final int SREG_ADDR = (int)0x5F;

    // 内存段定义
    // Program Flash Memory
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x7FFF;
    public static final int FLASH_SIZE = 32768;

    // Static RAM
    public static final int SRAM_START = (int)0x0100;
    public static final int SRAM_END = (int)0x0AFF;
    public static final int SRAM_SIZE = 2560;

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
    // Port B
    public static final int PORTB_BASE = (int)0x23;
    public static final int PORTB_PORTB = (int)0x00000048;
    public static final int PORTB_DDRB = (int)0x00000047;
    public static final int PORTB_PINB = (int)0x00000046;

    // Port C
    public static final int PORTC_BASE = (int)0x26;
    public static final int PORTC_PORTC = (int)0x0000004E;
    public static final int PORTC_DDRC = (int)0x0000004D;
    public static final int PORTC_PINC = (int)0x0000004C;

    // Port D
    public static final int PORTD_BASE = (int)0x29;
    public static final int PORTD_PORTD = (int)0x00000054;
    public static final int PORTD_DDRD = (int)0x00000053;
    public static final int PORTD_PIND = (int)0x00000052;

    // Port E
    public static final int PORTE_BASE = (int)0x2C;
    public static final int PORTE_PORTE = (int)0x0000005A;
    public static final int PORTE_DDRE = (int)0x00000059;
    public static final int PORTE_PINE = (int)0x00000058;

    // USART1
    public static final int UART1_BASE = (int)0xC8;
    public static final int UART1_UDR1 = (int)0x00000196;
    public static final int UART1_UCSR1A = (int)0x00000190;
    public static final int UART1_UCSR1B = (int)0x00000191;
    public static final int UART1_UCSR1C = (int)0x00000192;
    public static final int UART1_UBRR1 = (int)0x00000194;

    // USB Controller
    public static final int USB_BASE = (int)0xD0;
    public static final int USB_UDCON = (int)0x000001A0;
    public static final int USB_UDIEN = (int)0x000001A1;
    public static final int USB_UDINT = (int)0x000001A2;

    // 中断向量定义
    public static final int IRQ_INT0 = 1;  // External Interrupt 0
    public static final int IRQ_INT1 = 2;  // External Interrupt 1
    public static final int IRQ_INT2 = 3;  // External Interrupt 2
    public static final int IRQ_INT3 = 4;  // External Interrupt 3
    public static final int IRQ_INT4 = 5;  // External Interrupt 4
    public static final int IRQ_INT5 = 6;  // External Interrupt 5
    public static final int IRQ_INT6 = 7;  // External Interrupt 6
    public static final int IRQ_PCINT0 = 8;  // Pin Change Interrupt 0
    public static final int IRQ_USB_GENERAL = 9;  // USB General
    public static final int IRQ_USB_ENDPOINT = 10;  // USB Endpoint
    public static final int IRQ_WDT = 11;  // Watchdog Timeout
    public static final int IRQ_TIMER1_CAPT = 12;  // Timer1 Capture
    public static final int IRQ_TIMER1_COMPA = 13;  // Timer1 Compare A
    public static final int IRQ_TIMER1_COMPB = 14;  // Timer1 Compare B
    public static final int IRQ_TIMER1_OVF = 15;  // Timer1 Overflow
    public static final int IRQ_TIMER0_COMPA = 16;  // Timer0 Compare A
    public static final int IRQ_TIMER0_COMPB = 17;  // Timer0 Compare B
    public static final int IRQ_TIMER0_OVF = 18;  // Timer0 Overflow
    public static final int IRQ_SPI_STC = 19;  // SPI Transfer Complete
    public static final int IRQ_UART1_RX = 20;  // UART1 Receive
    public static final int IRQ_UART1_UDRE = 21;  // UART1 Data Register Empty
    public static final int IRQ_UART1_TX = 22;  // UART1 Transmit
    public static final int IRQ_ADC = 23;  // ADC Conversion Complete

    public static native void atmega32u4_init();
}
