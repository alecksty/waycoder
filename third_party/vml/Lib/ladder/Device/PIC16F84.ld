(*
 * PIC16F84梯形图定义
 * 生成自: Microchip Technology/PIC16/PIC16F84
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
 * CPU架构: PIC16
 * 位宽: 0位
 * 时钟频率: 0 Hz
 *)

PROGRAM PIC16F84
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* 8-bit timer/counter with prescaler *)
    TIMER0_BASE AT %MW : WORD;
    TIMER0_TMR0 AT %MW0x01 : LWORD;
    (* 16-bit timer/counter with prescaler *)
    TIMER1_BASE AT %MW : WORD;
    TIMER1_TMR1L AT %MW0x0E : LWORD;
    TIMER1_TMR1H AT %MW0x0F : LWORD;
    TIMER1_T1CON AT %MW0x10 : LWORD;
    TIMER1_T1CON_TMR1ON AT %MW0x10.0 : BOOL; (* Timer1 On *)
    TIMER1_T1CON_TMR1CS AT %MW0x10.1 : BOOL; (* Timer1 Clock Source *)
    TIMER1_T1CON_T1SYNC AT %MW0x10.2 : BOOL; (* Timer1 External Clock Input Synchronization *)
    TIMER1_T1CON_T1OSCEN AT %MW0x10.3 : BOOL; (* Timer1 Oscillator Enable *)
    TIMER1_T1CON_T1CKPS0 AT %MW0x10.4 : BOOL; (* Timer1 Input Clock Prescale Select bit 0 *)
    TIMER1_T1CON_T1CKPS1 AT %MW0x10.5 : BOOL; (* Timer1 Input Clock Prescale Select bit 1 *)
    (* Watchdog Timer *)
    WATCHDOG_BASE AT %MW : WORD;
    WATCHDOG_WDTCON AT %MW0x07 : LWORD;
    WATCHDOG_WDTCON_SWDTEN AT %MW0x07.0 : BOOL; (* Software Watchdog Timer Enable *)
    (* 64-byte EEPROM data memory *)
    EEPROM_BASE AT %MW : WORD;
    EEPROM_EEDATA AT %MW0x08 : LWORD;
    EEPROM_EEADR AT %MW0x09 : LWORD;
    EEPROM_EECON1 AT %MW0x88 : LWORD;
    EEPROM_EECON2 AT %MW0x89 : LWORD;
    (* General Purpose I/O *)
    GPIO_BASE AT %MW : WORD;
    GPIO_PORTA AT %MW0x05 : LWORD;
    GPIO_PORTB AT %MW0x06 : LWORD;
    GPIO_TRISA AT %MW0x85 : LWORD;
    GPIO_TRISB AT %MW0x86 : LWORD;

    (* 中断变量 *)
    INT_INT : BOOL; (* External interrupt on RB0/INT pin *)
    INT_TMR0 : BOOL; (* Timer0 overflow interrupt *)
    INT_PORTB : BOOL; (* PORTB change interrupt (RB4-RB7) *)
    INT_EEPROM : BOOL; (* EEPROM write complete interrupt *)

END_VAR

BEGIN
    (* PIC16F84初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_INT|--[中断处理]--
    --|INT_TMR0|--[中断处理]--
    --|INT_PORTB|--[中断处理]--
    --|INT_EEPROM|--[中断处理]--

END_PROGRAM
