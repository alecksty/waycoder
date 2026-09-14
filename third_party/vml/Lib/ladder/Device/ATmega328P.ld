(*
 * ATmega328P梯形图定义
 * 生成自: Atmel/AVR/ATmega328P
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
 * CPU架构: AVR
 * 位宽: 8位
 * 时钟频率: 16000000 Hz
 *)

PROGRAM ATmega328P
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : BYTE; (* General Purpose Register 0 *)
    R1 AT %M0x01 : BYTE; (* General Purpose Register 1 *)
    R2 AT %M0x02 : BYTE; (* General Purpose Register 2 *)
    R3 AT %M0x03 : BYTE; (* General Purpose Register 3 *)
    R4 AT %M0x04 : BYTE; (* General Purpose Register 4 *)
    R5 AT %M0x05 : BYTE; (* General Purpose Register 5 *)
    R6 AT %M0x06 : BYTE; (* General Purpose Register 6 *)
    R7 AT %M0x07 : BYTE; (* General Purpose Register 7 *)
    R8 AT %M0x08 : BYTE; (* General Purpose Register 8 *)
    R9 AT %M0x09 : BYTE; (* General Purpose Register 9 *)
    R10 AT %M0x0A : BYTE; (* General Purpose Register 10 *)
    R11 AT %M0x0B : BYTE; (* General Purpose Register 11 *)
    R12 AT %M0x0C : BYTE; (* General Purpose Register 12 *)
    R13 AT %M0x0D : BYTE; (* General Purpose Register 13 *)
    R14 AT %M0x0E : BYTE; (* General Purpose Register 14 *)
    R15 AT %M0x0F : BYTE; (* General Purpose Register 15 *)
    R16 AT %M0x10 : BYTE; (* General Purpose Register 16 *)
    R17 AT %M0x11 : BYTE; (* General Purpose Register 17 *)
    R18 AT %M0x12 : BYTE; (* General Purpose Register 18 *)
    R19 AT %M0x13 : BYTE; (* General Purpose Register 19 *)
    R20 AT %M0x14 : BYTE; (* General Purpose Register 20 *)
    R21 AT %M0x15 : BYTE; (* General Purpose Register 21 *)
    R22 AT %M0x16 : BYTE; (* General Purpose Register 22 *)
    R23 AT %M0x17 : BYTE; (* General Purpose Register 23 *)
    R24 AT %M0x18 : BYTE; (* General Purpose Register 24 *)
    R25 AT %M0x19 : BYTE; (* General Purpose Register 25 *)
    R26 AT %M0x1A : BYTE; (* General Purpose Register 26 (XL) *)
    R27 AT %M0x1B : BYTE; (* General Purpose Register 27 (XH) *)
    R28 AT %M0x1C : BYTE; (* General Purpose Register 28 (YL) *)
    R29 AT %M0x1D : BYTE; (* General Purpose Register 29 (YH) *)
    R30 AT %M0x1E : BYTE; (* General Purpose Register 30 (ZL) *)
    R31 AT %M0x1F : BYTE; (* General Purpose Register 31 (ZH) *)
    SPL AT %M0x5D : BYTE; (* Stack Pointer Low *)
    SPH AT %M0x5E : BYTE; (* Stack Pointer High *)
    SREG AT %M0x5F : BYTE; (* Status Register *)
    SREG_C AT %M0x5F.0 : BOOL; (* Carry Flag *)
    SREG_Z AT %M0x5F.1 : BOOL; (* Zero Flag *)
    SREG_N AT %M0x5F.2 : BOOL; (* Negative Flag *)
    SREG_V AT %M0x5F.3 : BOOL; (* Two's Complement Overflow Flag *)
    SREG_S AT %M0x5F.4 : BOOL; (* Sign Flag (N ⊕ V) *)
    SREG_H AT %M0x5F.5 : BOOL; (* Half Carry Flag *)
    SREG_T AT %M0x5F.6 : BOOL; (* Transfer Bit *)
    SREG_I AT %M0x5F.7 : BOOL; (* Global Interrupt Enable *)

    (* 外设变量 *)
    (* Port B Data Register *)
    PORTB_BASE AT %MW0x23 : WORD;
    PORTB_PORTB AT %MW0x25 : BYTE;
    PORTB_DDRB AT %MW0x24 : BYTE;
    PORTB_PINB AT %MW0x23 : BYTE;
    (* Port C Data Register *)
    PORTC_BASE AT %MW0x26 : WORD;
    PORTC_PORTC AT %MW0x28 : BYTE;
    PORTC_DDRC AT %MW0x27 : BYTE;
    PORTC_PINC AT %MW0x26 : BYTE;
    (* Port D Data Register *)
    PORTD_BASE AT %MW0x29 : WORD;
    PORTD_PORTD AT %MW0x2B : BYTE;
    PORTD_DDRD AT %MW0x2A : BYTE;
    PORTD_PIND AT %MW0x29 : BYTE;
    (* 8-bit Timer/Counter0 *)
    TIMER0_BASE AT %MW0x44 : WORD;
    TIMER0_TCCR0A AT %MW0x44 : BYTE;
    TIMER0_TCCR0A_WGM00 AT %MW0x44.0 : BOOL; (* Waveform Generation Mode *)
    TIMER0_TCCR0A_WGM01 AT %MW0x44.1 : BOOL; (* Waveform Generation Mode *)
    TIMER0_TCCR0A_COM0B0 AT %MW0x44.4 : BOOL; (* Compare Output Mode for Channel B *)
    TIMER0_TCCR0A_COM0B1 AT %MW0x44.5 : BOOL; (* Compare Output Mode for Channel B *)
    TIMER0_TCCR0A_COM0A0 AT %MW0x44.6 : BOOL; (* Compare Output Mode for Channel A *)
    TIMER0_TCCR0A_COM0A1 AT %MW0x44.7 : BOOL; (* Compare Output Mode for Channel A *)
    TIMER0_TCCR0B AT %MW0x45 : BYTE;
    TIMER0_TCCR0B_CS00 AT %MW0x45.0 : BOOL; (* Clock Select *)
    TIMER0_TCCR0B_CS01 AT %MW0x45.1 : BOOL; (* Clock Select *)
    TIMER0_TCCR0B_CS02 AT %MW0x45.2 : BOOL; (* Clock Select *)
    TIMER0_TCCR0B_WGM02 AT %MW0x45.3 : BOOL; (* Waveform Generation Mode *)
    TIMER0_TCCR0B_FOC0B AT %MW0x45.6 : BOOL; (* Force Output Compare B *)
    TIMER0_TCCR0B_FOC0A AT %MW0x45.7 : BOOL; (* Force Output Compare A *)
    TIMER0_TCNT0 AT %MW0x46 : BYTE;
    TIMER0_OCR0A AT %MW0x47 : BYTE;
    TIMER0_OCR0B AT %MW0x48 : BYTE;
    TIMER0_TIMSK0 AT %MW0x6E : BYTE;
    TIMER0_TIMSK0_TOIE0 AT %MW0x6E.0 : BOOL; (* Timer/Counter0 Overflow Interrupt Enable *)
    TIMER0_TIMSK0_OCIE0A AT %MW0x6E.1 : BOOL; (* Timer/Counter0 Output Compare A Match Interrupt Enable *)
    TIMER0_TIMSK0_OCIE0B AT %MW0x6E.2 : BOOL; (* Timer/Counter0 Output Compare B Match Interrupt Enable *)
    TIMER0_TIFR0 AT %MW0x35 : BYTE;
    TIMER0_TIFR0_TOV0 AT %MW0x35.0 : BOOL; (* Timer/Counter0 Overflow Flag *)
    TIMER0_TIFR0_OCF0A AT %MW0x35.1 : BOOL; (* Output Compare Flag 0A *)
    TIMER0_TIFR0_OCF0B AT %MW0x35.2 : BOOL; (* Output Compare Flag 0B *)
    (* Universal Synchronous/Asynchronous Receiver/Transmitter *)
    USART0_BASE AT %MW0xC0 : WORD;
    USART0_UDR0 AT %MW0xC6 : BYTE;
    USART0_UCSR0A AT %MW0xC0 : BYTE;
    USART0_UCSR0A_MPCM0 AT %MW0xC0.0 : BOOL; (* Multi-processor Communication Mode *)
    USART0_UCSR0A_U2X0 AT %MW0xC0.1 : BOOL; (* Double the USART Transmission Speed *)
    USART0_UCSR0A_UPE0 AT %MW0xC0.2 : BOOL; (* Parity Error *)
    USART0_UCSR0A_DOR0 AT %MW0xC0.3 : BOOL; (* Data OverRun *)
    USART0_UCSR0A_FE0 AT %MW0xC0.4 : BOOL; (* Frame Error *)
    USART0_UCSR0A_UDRE0 AT %MW0xC0.5 : BOOL; (* USART Data Register Empty *)
    USART0_UCSR0A_TXC0 AT %MW0xC0.6 : BOOL; (* USART Transmit Complete *)
    USART0_UCSR0A_RXC0 AT %MW0xC0.7 : BOOL; (* USART Receive Complete *)
    USART0_UCSR0B AT %MW0xC1 : BYTE;
    USART0_UCSR0B_TXB80 AT %MW0xC1.0 : BOOL; (* Transmit Data Bit 8 *)
    USART0_UCSR0B_RXB80 AT %MW0xC1.1 : BOOL; (* Receive Data Bit 8 *)
    USART0_UCSR0B_UCSZ02 AT %MW0xC1.2 : BOOL; (* Character Size *)
    USART0_UCSR0B_TXEN0 AT %MW0xC1.3 : BOOL; (* Transmitter Enable *)
    USART0_UCSR0B_RXEN0 AT %MW0xC1.4 : BOOL; (* Receiver Enable *)
    USART0_UCSR0B_UDRIE0 AT %MW0xC1.5 : BOOL; (* USART Data Register Empty Interrupt Enable *)
    USART0_UCSR0B_TXCIE0 AT %MW0xC1.6 : BOOL; (* TX Complete Interrupt Enable *)
    USART0_UCSR0B_RXCIE0 AT %MW0xC1.7 : BOOL; (* RX Complete Interrupt Enable *)
    USART0_UCSR0C AT %MW0xC2 : BYTE;
    USART0_UCSR0C_UCPOL0 AT %MW0xC2.0 : BOOL; (* Clock Polarity *)
    USART0_UCSR0C_UCSZ00 AT %MW0xC2.1 : BOOL; (* Character Size *)
    USART0_UCSR0C_UCSZ01 AT %MW0xC2.2 : BOOL; (* Character Size *)
    USART0_UCSR0C_USBS0 AT %MW0xC2.3 : BOOL; (* Stop Bit Select *)
    USART0_UCSR0C_UPM00 AT %MW0xC2.4 : BOOL; (* Parity Mode *)
    USART0_UCSR0C_UPM01 AT %MW0xC2.5 : BOOL; (* Parity Mode *)
    USART0_UCSR0C_UMSEL00 AT %MW0xC2.6 : BOOL; (* USART Mode Select *)
    USART0_UCSR0C_UMSEL01 AT %MW0xC2.7 : BOOL; (* USART Mode Select *)
    USART0_UBRR0 AT %MW0xC4 : WORD;
    (* Analog-to-Digital Converter *)
    ADC_BASE AT %MW0x78 : WORD;
    ADC_ADMUX AT %MW0x7C : BYTE;
    ADC_ADMUX_MUX0 AT %MW0x7C.0 : BOOL; (* Analog Channel Selection *)
    ADC_ADMUX_MUX1 AT %MW0x7C.1 : BOOL; (* Analog Channel Selection *)
    ADC_ADMUX_MUX2 AT %MW0x7C.2 : BOOL; (* Analog Channel Selection *)
    ADC_ADMUX_MUX3 AT %MW0x7C.3 : BOOL; (* Analog Channel Selection *)
    ADC_ADMUX_ADLAR AT %MW0x7C.5 : BOOL; (* ADC Left Adjust Result *)
    ADC_ADMUX_REFS0 AT %MW0x7C.6 : BOOL; (* Reference Selection *)
    ADC_ADMUX_REFS1 AT %MW0x7C.7 : BOOL; (* Reference Selection *)
    ADC_ADCSRA AT %MW0x7A : BYTE;
    ADC_ADCSRA_ADPS0 AT %MW0x7A.0 : BOOL; (* ADC Prescaler Select *)
    ADC_ADCSRA_ADPS1 AT %MW0x7A.1 : BOOL; (* ADC Prescaler Select *)
    ADC_ADCSRA_ADPS2 AT %MW0x7A.2 : BOOL; (* ADC Prescaler Select *)
    ADC_ADCSRA_ADIE AT %MW0x7A.3 : BOOL; (* ADC Interrupt Enable *)
    ADC_ADCSRA_ADIF AT %MW0x7A.4 : BOOL; (* ADC Interrupt Flag *)
    ADC_ADCSRA_ADATE AT %MW0x7A.5 : BOOL; (* ADC Auto Trigger Enable *)
    ADC_ADCSRA_ADSC AT %MW0x7A.6 : BOOL; (* ADC Start Conversion *)
    ADC_ADCSRA_ADEN AT %MW0x7A.7 : BOOL; (* ADC Enable *)
    ADC_ADCH AT %MW0x79 : BYTE;
    ADC_ADCL AT %MW0x78 : BYTE;

    (* 中断变量 *)
    INT_INT0 : BOOL; (* External Interrupt Request 0 *)
    INT_INT1 : BOOL; (* External Interrupt Request 1 *)
    INT_PCINT0 : BOOL; (* Pin Change Interrupt Request 0 *)
    INT_PCINT1 : BOOL; (* Pin Change Interrupt Request 1 *)
    INT_PCINT2 : BOOL; (* Pin Change Interrupt Request 2 *)
    INT_WDT : BOOL; (* Watchdog Time-out Interrupt *)
    INT_TIMER2_COMPA : BOOL; (* Timer/Counter2 Compare Match A *)
    INT_TIMER2_COMPB : BOOL; (* Timer/Counter2 Compare Match B *)
    INT_TIMER2_OVF : BOOL; (* Timer/Counter2 Overflow *)
    INT_TIMER1_CAPT : BOOL; (* Timer/Counter1 Capture Event *)
    INT_TIMER1_COMPA : BOOL; (* Timer/Counter1 Compare Match A *)
    INT_TIMER1_COMPB : BOOL; (* Timer/Counter1 Compare Match B *)
    INT_TIMER1_OVF : BOOL; (* Timer/Counter1 Overflow *)
    INT_TIMER0_COMPA : BOOL; (* Timer/Counter0 Compare Match A *)
    INT_TIMER0_COMPB : BOOL; (* Timer/Counter0 Compare Match B *)
    INT_TIMER0_OVF : BOOL; (* Timer/Counter0 Overflow *)
    INT_SPI_STC : BOOL; (* SPI Serial Transfer Complete *)
    INT_USART_RX : BOOL; (* USART Rx Complete *)
    INT_USART_UDRE : BOOL; (* USART Data Register Empty *)
    INT_USART_TX : BOOL; (* USART Tx Complete *)
    INT_ADC : BOOL; (* ADC Conversion Complete *)
    INT_EE_READY : BOOL; (* EEPROM Ready *)
    INT_ANALOG_COMP : BOOL; (* Analog Comparator *)
    INT_TWI : BOOL; (* Two-wire Serial Interface *)
    INT_SPM_READY : BOOL; (* Store Program Memory Ready *)

END_VAR

BEGIN
    (* ATmega328P初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_INT0|--[中断处理]--
    --|INT_INT1|--[中断处理]--
    --|INT_PCINT0|--[中断处理]--
    --|INT_PCINT1|--[中断处理]--
    --|INT_PCINT2|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_TIMER2_COMPA|--[中断处理]--
    --|INT_TIMER2_COMPB|--[中断处理]--
    --|INT_TIMER2_OVF|--[中断处理]--
    --|INT_TIMER1_CAPT|--[中断处理]--
    --|INT_TIMER1_COMPA|--[中断处理]--
    --|INT_TIMER1_COMPB|--[中断处理]--
    --|INT_TIMER1_OVF|--[中断处理]--
    --|INT_TIMER0_COMPA|--[中断处理]--
    --|INT_TIMER0_COMPB|--[中断处理]--
    --|INT_TIMER0_OVF|--[中断处理]--
    --|INT_SPI_STC|--[中断处理]--
    --|INT_USART_RX|--[中断处理]--
    --|INT_USART_UDRE|--[中断处理]--
    --|INT_USART_TX|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_EE_READY|--[中断处理]--
    --|INT_ANALOG_COMP|--[中断处理]--
    --|INT_TWI|--[中断处理]--
    --|INT_SPM_READY|--[中断处理]--

END_PROGRAM
