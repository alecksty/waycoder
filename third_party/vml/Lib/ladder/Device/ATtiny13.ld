(*
 * ATtiny13梯形图定义
 * 生成自: Atmel/AVR/ATtiny13
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
 * CPU架构: AVR
 * 位宽: 8位
 * 时钟频率: 20000000 Hz
 *)

PROGRAM ATtiny13
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : BYTE; (*  *)
    R1 AT %M0x01 : BYTE; (*  *)
    R2 AT %M0x02 : BYTE; (*  *)
    R16 AT %M0x10 : BYTE; (*  *)
    R17 AT %M0x11 : BYTE; (*  *)
    R26 AT %M0x1A : BYTE; (* XL *)
    R27 AT %M0x1B : BYTE; (* XH *)
    R28 AT %M0x1C : BYTE; (* YL *)
    R29 AT %M0x1D : BYTE; (* YH *)
    R30 AT %M0x1E : BYTE; (* ZL *)
    R31 AT %M0x1F : BYTE; (* ZH *)
    SPL AT %M0x5D : BYTE; (* Stack Pointer Low *)
    SPH AT %M0x5E : BYTE; (* Stack Pointer High *)
    SREG AT %M0x5F : BYTE; (* Status Register *)

    (* 外设变量 *)
    (* Port B (only port) *)
    PORTB_BASE AT %MW0x18 : WORD;
    PORTB_DDRB AT %MW0x17 : BYTE;
    PORTB_PORTB AT %MW0x18 : BYTE;
    PORTB_PINB AT %MW0x19 : BYTE;
    (* 8-bit Timer/Counter0 *)
    TIMER0_BASE AT %MW0x33 : WORD;
    TIMER0_TCCR0A AT %MW0x33 : BYTE;
    TIMER0_TCCR0B AT %MW0x33 : BYTE;
    TIMER0_TCNT0 AT %MW0x32 : BYTE;
    TIMER0_OCR0A AT %MW0x36 : BYTE;
    TIMER0_OCR0B AT %MW0x35 : BYTE;
    TIMER0_TIMSK0 AT %MW0x39 : BYTE;
    TIMER0_TIFR0 AT %MW0x38 : BYTE;
    (* Analog-to-Digital *)
    ADC_BASE AT %MW0x04 : WORD;
    ADC_ADMUX AT %MW0x07 : BYTE;
    ADC_ADCSRA AT %MW0x06 : BYTE;
    ADC_ADCL AT %MW0x04 : BYTE;
    ADC_ADCH AT %MW0x05 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_INT0 : BOOL; (* External Interrupt 0 *)
    INT_PCINT0 : BOOL; (* Pin Change Interrupt *)
    INT_TIM0_OVF : BOOL; (* Timer0 Overflow *)
    INT_TIM0_COMPA : BOOL; (* Timer0 Compare A *)
    INT_WDT : BOOL; (* Watchdog Timeout *)
    INT_ADC : BOOL; (* ADC Conversion Complete *)

END_VAR

BEGIN
    (* ATtiny13初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R16>0|--(监控灯)--
    --|R17>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_INT0|--[中断处理]--
    --|INT_PCINT0|--[中断处理]--
    --|INT_TIM0_OVF|--[中断处理]--
    --|INT_TIM0_COMPA|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_ADC|--[中断处理]--

END_PROGRAM
