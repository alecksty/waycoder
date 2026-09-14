package vml.device.atmel.atmega2560;

/**
 * ATmega2560 寄存器定义
 * 生成自: Atmel/AVR/ATmega2560
 * 版本: 1.0
 */
public final class ATmega2560 {
    private ATmega2560() {} // 工具类
    // CPU架构: AVR, 8位, 16000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x01;

    public static final int R2_ADDR = (int)0x02;

    public static final int SPL_ADDR = (int)0x5D;

    public static final int SPH_ADDR = (int)0x5E;

    public static final int SREG_ADDR = (int)0x5F;

    // 内存段定义
    public static final int FLASH_START = (int)0x0000;
    public static final int FLASH_END = (int)0x3FFFF;
    public static final int FLASH_SIZE = 262144;

    public static final int SRAM_START = (int)0x0200;
    public static final int SRAM_END = (int)0x21FF;
    public static final int SRAM_SIZE = 8192;

    public static final int EEPROM_START = (int)0x0000;
    public static final int EEPROM_END = (int)0x0FFF;
    public static final int EEPROM_SIZE = 4096;

    public static final int IO_START = (int)0x00;
    public static final int IO_END = (int)0x3F;
    public static final int IO_SIZE = 64;

    public static final int EXTIO_START = (int)0x40;
    public static final int EXTIO_END = (int)0xFF;
    public static final int EXTIO_SIZE = 192;

    // 外设定义
    // Port A
    public static final int PORTA_BASE = (int)0x22;
    public static final int PORTA_DDRA = (int)0x00000043;
    public static final int PORTA_PORTA = (int)0x00000044;
    public static final int PORTA_PINA = (int)0x00000042;

    // Port B
    public static final int PORTB_BASE = (int)0x25;
    public static final int PORTB_DDRB = (int)0x00000049;
    public static final int PORTB_PORTB = (int)0x0000004A;
    public static final int PORTB_PINB = (int)0x00000048;

    // Port C
    public static final int PORTC_BASE = (int)0x28;
    public static final int PORTC_DDRC = (int)0x0000004F;
    public static final int PORTC_PORTC = (int)0x00000050;
    public static final int PORTC_PINC = (int)0x0000004E;

    // Port D
    public static final int PORTD_BASE = (int)0x2B;
    public static final int PORTD_DDRD = (int)0x00000055;
    public static final int PORTD_PORTD = (int)0x00000056;
    public static final int PORTD_PIND = (int)0x00000054;

    // Port E
    public static final int PORTE_BASE = (int)0x2E;
    public static final int PORTE_DDRE = (int)0x0000005B;
    public static final int PORTE_PORTE = (int)0x0000005C;
    public static final int PORTE_PINE = (int)0x0000005A;

    // Port F
    public static final int PORTF_BASE = (int)0x31;
    public static final int PORTF_DDRF = (int)0x00000061;
    public static final int PORTF_PORTF = (int)0x00000062;
    public static final int PORTF_PINF = (int)0x00000060;

    // Port G
    public static final int PORTG_BASE = (int)0x34;
    public static final int PORTG_DDRG = (int)0x00000067;
    public static final int PORTG_PORTG = (int)0x00000068;
    public static final int PORTG_PING = (int)0x00000066;

    // USART 0
    public static final int USART0_BASE = (int)0xC0;
    public static final int USART0_UDR0 = (int)0x00000186;
    public static final int USART0_UCSR0A = (int)0x00000180;
    public static final int USART0_UCSR0B = (int)0x00000181;
    public static final int USART0_UCSR0C = (int)0x00000182;
    public static final int USART0_UBRR0L = (int)0x00000184;
    public static final int USART0_UBRR0H = (int)0x00000185;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_INT0 = 2;  // 
    public static final int IRQ_INT1 = 3;  // 
    public static final int IRQ_INT2 = 4;  // 
    public static final int IRQ_INT3 = 5;  // 
    public static final int IRQ_INT4 = 6;  // 
    public static final int IRQ_INT5 = 7;  // 
    public static final int IRQ_INT6 = 8;  // 
    public static final int IRQ_INT7 = 9;  // 
    public static final int IRQ_PCINT0 = 10;  // 
    public static final int IRQ_PCINT1 = 11;  // 
    public static final int IRQ_PCINT2 = 12;  // 
    public static final int IRQ_WDT = 13;  // 
    public static final int IRQ_TIM2_COMPA = 14;  // 
    public static final int IRQ_TIM2_COMPB = 15;  // 
    public static final int IRQ_TIM2_OVF = 16;  // 
    public static final int IRQ_TIM1_CAPT = 17;  // 
    public static final int IRQ_TIM1_COMPA = 18;  // 
    public static final int IRQ_TIM1_COMPB = 19;  // 
    public static final int IRQ_TIM1_OVF = 20;  // 
    public static final int IRQ_TIM0_COMPA = 21;  // 
    public static final int IRQ_TIM0_COMPB = 22;  // 
    public static final int IRQ_TIM0_OVF = 23;  // 
    public static final int IRQ_SPI_STC = 24;  // 
    public static final int IRQ_USART0_RX = 25;  // 
    public static final int IRQ_USART0_UDRE = 26;  // 
    public static final int IRQ_USART0_TX = 27;  // 

    public static native void atmega2560_init();
}
