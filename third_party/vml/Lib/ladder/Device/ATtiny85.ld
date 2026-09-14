(*
 * ATtiny85梯形图定义
 * 生成自: Microchip/AVR/ATtiny85
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
 * CPU架构: AVR
 * 位宽: 8位
 * 时钟频率: 1000000 Hz
 *)

PROGRAM ATtiny85
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
    X AT %M0x1A : WORD; (* Register pair X (R27:R26) *)
    Y AT %M0x1C : WORD; (* Register pair Y (R29:R28) *)
    Z AT %M0x1E : WORD; (* Register pair Z (R31:R30) *)
    SP AT %M0x3D : WORD; (* Stack Pointer *)
    SREG AT %M0x3F : BYTE; (* Status Register *)
    SREG_C AT %M0x3F.0 : BOOL; (* Carry Flag *)
    SREG_Z AT %M0x3F.1 : BOOL; (* Zero Flag *)
    SREG_N AT %M0x3F.2 : BOOL; (* Negative Flag *)
    SREG_V AT %M0x3F.3 : BOOL; (* Two's Complement Overflow Flag *)
    SREG_S AT %M0x3F.4 : BOOL; (* Sign Flag (N xor V) *)
    SREG_H AT %M0x3F.5 : BOOL; (* Half Carry Flag *)
    SREG_T AT %M0x3F.6 : BOOL; (* Transfer Bit *)
    SREG_I AT %M0x3F.7 : BOOL; (* Global Interrupt Enable *)

    (* 外设变量 *)
    (* Port A *)
    PORTA_BASE AT %MW0x20 : WORD;
    PORTA_PINA AT %MW0x20 : BYTE;
    PORTA_DDRA AT %MW0x21 : BYTE;
    PORTA_PORTA AT %MW0x22 : BYTE;
    (* Port B *)
    PORTB_BASE AT %MW0x18 : WORD;
    PORTB_PINB AT %MW0x16 : BYTE;
    PORTB_DDRB AT %MW0x17 : BYTE;
    PORTB_PORTB AT %MW0x18 : BYTE;
    (* Timer/Counter0 *)
    TIPO_BASE AT %MW0x20 : WORD;
    TIPO_TCCR0A AT %MW0x20 : BYTE;
    TIPO_TCCR0A_WGM00 AT %MW0x20.0 : BOOL; (* Waveform Generation Mode *)
    TIPO_TCCR0A_WGM01 AT %MW0x20.1 : BOOL; (* Waveform Generation Mode *)
    TIPO_TCCR0A_COM0B0 AT %MW0x20.4 : BOOL; (* Compare Output Mode B *)
    TIPO_TCCR0A_COM0B1 AT %MW0x20.5 : BOOL; (* Compare Output Mode B *)
    TIPO_TCCR0A_COM0A0 AT %MW0x20.6 : BOOL; (* Compare Output Mode A *)
    TIPO_TCCR0A_COM0A1 AT %MW0x20.7 : BOOL; (* Compare Output Mode A *)
    TIPO_TCCR0B AT %MW0x21 : BYTE;
    TIPO_TCCR0B_CS00 AT %MW0x21.0 : BOOL; (* Clock Select *)
    TIPO_TCCR0B_CS01 AT %MW0x21.1 : BOOL; (* Clock Select *)
    TIPO_TCCR0B_CS02 AT %MW0x21.2 : BOOL; (* Clock Select *)
    TIPO_TCCR0B_WGM02 AT %MW0x21.3 : BOOL; (* Waveform Generation Mode *)
    TIPO_TCCR0B_FOC0B AT %MW0x21.6 : BOOL; (* Force Output Compare B *)
    TIPO_TCCR0B_FOC0A AT %MW0x21.7 : BOOL; (* Force Output Compare A *)
    TIPO_TCNT0 AT %MW0x22 : BYTE;
    TIPO_OCR0A AT %MW0x23 : BYTE;
    TIPO_OCR0B AT %MW0x24 : BYTE;
    TIPO_TIMSK AT %MW0x39 : BYTE;
    TIPO_TIMSK_TOIE0 AT %MW0x39.0 : BOOL; (* Timer/Counter0 Overflow Interrupt Enable *)
    TIPO_TIMSK_OCIE0A AT %MW0x39.1 : BOOL; (* Output Compare A Match Interrupt Enable *)
    TIPO_TIMSK_OCIE0B AT %MW0x39.2 : BOOL; (* Output Compare B Match Interrupt Enable *)
    TIPO_TIFR AT %MW0x38 : BYTE;
    TIPO_TIFR_TOV0 AT %MW0x38.0 : BOOL; (* Timer/Counter0 Overflow Flag *)
    TIPO_TIFR_OCF0A AT %MW0x38.1 : BOOL; (* Output Compare A Flag *)
    TIPO_TIFR_OCF0B AT %MW0x38.2 : BOOL; (* Output Compare B Flag *)
    (* Timer/Counter1 *)
    TMR1_BASE AT %MW0x28 : WORD;
    TMR1_TCCR1A AT %MW0x28 : BYTE;
    TMR1_TCCR1A_PCM1 AT %MW0x28.0 : BOOL; (* PWM Mode *)
    TMR1_TCCR1A_COM1A AT %MW0x28.0 : BOOL; (* Compare Output Mode A *)
    TMR1_TCCR1A_COM1B AT %MW0x28.0 : BOOL; (* Compare Output Mode B *)
    TMR1_TCCR1A_WG13 AT %MW0x28.1 : BOOL; (* Waveform Generation Mode *)
    TMR1_TCCR1A_WG10 AT %MW0x28.0 : BOOL; (* Waveform Generation Mode *)
    TMR1_TCCR1B AT %MW0x29 : BYTE;
    TMR1_TCCR1B_CTC1 AT %MW0x29.7 : BOOL; (* Clear Timer on Compare *)
    TMR1_TCCR1B_WGM13 AT %MW0x29.4 : BOOL; (* Waveform Generation Mode *)
    TMR1_TCCR1B_WGM12 AT %MW0x29.3 : BOOL; (* Waveform Generation Mode *)
    TMR1_TCCR1B_CS1 AT %MW0x29.0 : BOOL; (* Clock Select *)
    TMR1_TCNT1 AT %MW0x2A : WORD;
    TMR1_OCR1A AT %MW0x2C : WORD;
    TMR1_OCR1B AT %MW0x2E : WORD;
    TMR1_OCR1C AT %MW0x30 : WORD;
    TMR1_TIMSK1 AT %MW0x33 : BYTE;
    TMR1_TIFR1 AT %MW0x32 : BYTE;
    (* ADC Multiplexer *)
    ADMUX_BASE AT %MW0x12 : WORD;
    ADMUX_ADMUX AT %MW0x12 : BYTE;
    ADMUX_ADMUX_MUX AT %MW0x12.0 : BOOL; (* Analog Channel Selection *)
    ADMUX_ADMUX_ADLAR AT %MW0x12.5 : BOOL; (* ADC Left Adjust Result *)
    ADMUX_ADMUX_REFS AT %MW0x12.0 : BOOL; (* Reference Selection *)
    ADMUX_ADCSRA AT %MW0x13 : BYTE;
    ADMUX_ADCSRA_ADPS AT %MW0x13.0 : BOOL; (* ADC Prescaler Select *)
    ADMUX_ADCSRA_ADIE AT %MW0x13.3 : BOOL; (* ADC Interrupt Enable *)
    ADMUX_ADCSRA_ADIF AT %MW0x13.4 : BOOL; (* ADC Interrupt Flag *)
    ADMUX_ADCSRA_ADATE AT %MW0x13.5 : BOOL; (* ADC Auto Trigger Enable *)
    ADMUX_ADCSRA_ADSC AT %MW0x13.6 : BOOL; (* ADC Start Conversion *)
    ADMUX_ADCSRA_ADEN AT %MW0x13.7 : BOOL; (* ADC Enable *)
    ADMUX_ADCH AT %MW0x14 : BYTE;
    ADMUX_ADCL AT %MW0x15 : BYTE;
    (* Universal Serial Interface *)
    USI_BASE AT %MW0x18 : WORD;
    USI_USIDR AT %MW0x18 : BYTE;
    USI_USISR AT %MW0x19 : BYTE;
    USI_USISR_USICNT AT %MW0x19.0 : BOOL; (* Counter *)
    USI_USISR_USIDC AT %MW0x19.4 : BOOL; (* Data Register *)
    USI_USISR_USIPF AT %MW0x19.5 : BOOL; (* Stop Cond Flag *)
    USI_USISR_USIOV AT %MW0x19.6 : BOOL; (* Overflow Flag *)
    USI_USISR_USISIF AT %MW0x19.7 : BOOL; (* Start Cond Interrupt Flag *)
    USI_USICR AT %MW0x1A : BYTE;
    USI_USICR_USICS AT %MW0x1A.0 : BOOL; (* Clock Source Select *)
    USI_USICR_USISCL AT %MW0x1A.2 : BOOL; (* SCL strobe *)
    USI_USICR_USIOW AT %MW0x1A.3 : BOOL; (* SDA output override *)
    USI_USICR_USIOE AT %MW0x1A.4 : BOOL; (* Output Enable *)
    USI_USICR_USISRE AT %MW0x1A.5 : BOOL; (* Start Recognition Enable *)
    USI_USICR_USIORE AT %MW0x1A.6 : BOOL; (* Stop Recognition Enable *)
    USI_USICR_USIGIE AT %MW0x1A.7 : BOOL; (* Global Interrupt Enable *)
    USI_USIPORT AT %MW0x1B : BYTE;
    (* MCU Control *)
    MCUCR_BASE AT %MW0x35 : WORD;
    MCUCR_MCUCR AT %MW0x35 : BYTE;
    MCUCR_MCUCR_ISC AT %MW0x35.0 : BOOL; (* Interrupt Sense Control *)
    MCUCR_MCUCR_SE AT %MW0x35.4 : BOOL; (* Sleep Enable *)
    MCUCR_MCUCR_SM AT %MW0x35.0 : BOOL; (* Sleep Mode *)
    MCUCR_MCUCSR AT %MW0x36 : BYTE;
    MCUCR_MCUCSR_PORF AT %MW0x36.0 : BOOL; (* Power-on Reset Flag *)
    MCUCR_MCUCSR_EXTRF AT %MW0x36.1 : BOOL; (* External Reset Flag *)
    MCUCR_MCUCSR_WDRF AT %MW0x36.2 : BOOL; (* Watchdog Reset Flag *)
    MCUCR_MCUCSR_BORF AT %MW0x36.4 : BOOL; (* Brown-out Reset Flag *)
    (* Watchdog Timer *)
    WDTCR_BASE AT %MW0x21 : WORD;
    WDTCR_WDTCR AT %MW0x21 : BYTE;
    WDTCR_WDTCR_WDP AT %MW0x21.0 : BOOL; (* Watchdog Prescaler *)
    WDTCR_WDTCR_WDE AT %MW0x21.3 : BOOL; (* Watchdog Enable *)
    WDTCR_WDTCR_WDIE AT %MW0x21.4 : BOOL; (* Watchdog Interrupt Enable *)
    (* EEPROM *)
    EEPR_BASE AT %MW0x1C : WORD;
    EEPR_EEAR AT %MW0x1E : BYTE;
    EEPR_EEDR AT %MW0x1D : BYTE;
    EEPR_EECR AT %MW0x1F : BYTE;
    EEPR_EECR_EEPM AT %MW0x1F.0 : BOOL; (* EEPROM Programming Mode *)
    EEPR_EECR_EERIE AT %MW0x1F.3 : BOOL; (* EEPROM Ready Interrupt Enable *)
    EEPR_EECR_EEWE AT %MW0x1F.2 : BOOL; (* EEPROM Write Enable *)
    EEPR_EECR_EEMWE AT %MW0x1F.1 : BOOL; (* EEPROM Master Write Enable *)
    EEPR_EECR_EERE AT %MW0x1F.0 : BOOL; (* EEPROM Read Enable *)
    (* External Interrupt *)
    GIMSK_BASE AT %MW0x3B : WORD;
    GIMSK_GIMSK AT %MW0x3B : BYTE;
    GIMSK_GIMSK_INT0 AT %MW0x3B.0 : BOOL; (* External Interrupt Request 0 Enable *)
    GIMSK_GIMSK_PCIE AT %MW0x3B.1 : BOOL; (* Pin Change Interrupt Enable *)
    GIMSK_GIFR AT %MW0x3C : BYTE;
    GIMSK_GIFR_INTF0 AT %MW0x3C.0 : BOOL; (* External Interrupt Flag 0 *)
    GIMSK_GIFR_PCIF AT %MW0x3C.1 : BOOL; (* Pin Change Interrupt Flag *)
    (* Pin Change Mask *)
    PCMSK_BASE AT %MW0x15 : WORD;
    PCMSK_PCMSK AT %MW0x15 : BYTE;
    (* Store Program Memory *)
    SPMCSR_BASE AT %MW0x37 : WORD;
    SPMCSR_SPMCSR AT %MW0x37 : BYTE;
    SPMCSR_SPMCSR_SPMCR AT %MW0x37.0 : BOOL; (* SPM Mode *)
    SPMCSR_SPMCSR_PGERS AT %MW0x37.1 : BOOL; (* Page Erase *)
    SPMCSR_SPMCSR_PGWRT AT %MW0x37.2 : BOOL; (* Page Write *)
    SPMCSR_SPMCSR_BLBSET AT %MW0x37.3 : BOOL; (* Boot Lock Bits Set *)
    SPMCSR_SPMCSR_RWWSRE AT %MW0x37.4 : BOOL; (* Read-While-Read Strobe Enable *)
    SPMCSR_SPMCSR_SIGRD AT %MW0x37.5 : BOOL; (* Signature Row Read *)
    SPMCSR_SPMCSR_SPMEN AT %MW0x37.7 : BOOL; (* SPM Enable *)

    (* 中断变量 *)
    INT_RESET : BOOL; (* External Reset, Power-on Reset, Brown-out Reset *)
    INT_INT0 : BOOL; (* External Interrupt Request 0 *)
    INT_PCINT0 : BOOL; (* Pin Change *)
    INT_WDT : BOOL; (* Watchdog Timeout *)
    INT_TIM1_COMPA : BOOL; (* Timer/Counter1 Compare Match A *)
    INT_TIM1_OVF : BOOL; (* Timer/Counter1 Overflow *)
    INT_TIM0_COMPA : BOOL; (* Timer/Counter0 Compare Match A *)
    INT_TIM0_OVF : BOOL; (* Timer/Counter0 Overflow *)
    INT_SPI_STC : BOOL; (* SPI Serial Transfer Complete *)
    INT_ADC : BOOL; (* ADC Conversion Complete *)
    INT_USI_START : BOOL; (* USI Start Condition *)
    INT_USI_OVF : BOOL; (* USI Overflow *)
    INT_EE_READY : BOOL; (* EEPROM Ready *)

END_VAR

BEGIN
    (* ATtiny85初始化梯形图 *)
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
    --|INT_RESET|--[中断处理]--
    --|INT_INT0|--[中断处理]--
    --|INT_PCINT0|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_TIM1_COMPA|--[中断处理]--
    --|INT_TIM1_OVF|--[中断处理]--
    --|INT_TIM0_COMPA|--[中断处理]--
    --|INT_TIM0_OVF|--[中断处理]--
    --|INT_SPI_STC|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_USI_START|--[中断处理]--
    --|INT_USI_OVF|--[中断处理]--
    --|INT_EE_READY|--[中断处理]--

END_PROGRAM
