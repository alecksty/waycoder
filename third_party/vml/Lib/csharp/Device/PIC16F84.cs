using System;

namespace VML.Device.MicrochipTechnology.PIC16F84
{
    /// <summary>
    /// PIC16F84 寄存器定义
    /// 生成自: Microchip Technology/PIC16/PIC16F84
    /// 版本: 
    /// </summary>
    public static class PIC16F84
    {
        // CPU架构: PIC16, 0位, 0 Hz

        // 外设定义
        // 8-bit timer/counter with prescaler
        public const int TIMER0_BASE = ;
        public static unsafe ulong* TIMER0_TMR0 => (ulong*)0x00000001;

        // 16-bit timer/counter with prescaler
        public const int TIMER1_BASE = ;
        public static unsafe ulong* TIMER1_TMR1L => (ulong*)0x0000000E;
        public static unsafe ulong* TIMER1_TMR1H => (ulong*)0x0000000F;
        public static unsafe ulong* TIMER1_T1CON => (ulong*)0x00000010;
        public const int TIMER1_T1CON_TMR1ON = 0;  // Timer1 On
        public const int TIMER1_T1CON_TMR1CS = 1;  // Timer1 Clock Source
        public const int TIMER1_T1CON_T1SYNC = 2;  // Timer1 External Clock Input Synchronization
        public const int TIMER1_T1CON_T1OSCEN = 3;  // Timer1 Oscillator Enable
        public const int TIMER1_T1CON_T1CKPS0 = 4;  // Timer1 Input Clock Prescale Select bit 0
        public const int TIMER1_T1CON_T1CKPS1 = 5;  // Timer1 Input Clock Prescale Select bit 1

        // Watchdog Timer
        public const int WATCHDOG_BASE = ;
        public static unsafe ulong* WATCHDOG_WDTCON => (ulong*)0x00000007;
        public const int WATCHDOG_WDTCON_SWDTEN = 0;  // Software Watchdog Timer Enable

        // 64-byte EEPROM data memory
        public const int EEPROM_BASE = ;
        public static unsafe ulong* EEPROM_EEDATA => (ulong*)0x00000008;
        public static unsafe ulong* EEPROM_EEADR => (ulong*)0x00000009;
        public static unsafe ulong* EEPROM_EECON1 => (ulong*)0x00000088;
        public static unsafe ulong* EEPROM_EECON2 => (ulong*)0x00000089;

        // General Purpose I/O
        public const int GPIO_BASE = ;
        public static unsafe ulong* GPIO_PORTA => (ulong*)0x00000005;
        public static unsafe ulong* GPIO_PORTB => (ulong*)0x00000006;
        public static unsafe ulong* GPIO_TRISA => (ulong*)0x00000085;
        public static unsafe ulong* GPIO_TRISB => (ulong*)0x00000086;

        // 中断向量定义
        public const int IRQ_INT = 4;  // External interrupt on RB0/INT pin
        public const int IRQ_TMR0 = 4;  // Timer0 overflow interrupt
        public const int IRQ_PORTB = 4;  // PORTB change interrupt (RB4-RB7)
        public const int IRQ_EEPROM = 4;  // EEPROM write complete interrupt

        public static void pic16f84_init()
        {
            // 硬件初始化代码
        }
    }
}
