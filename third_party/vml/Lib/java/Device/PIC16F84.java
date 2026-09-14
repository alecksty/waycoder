package vml.device.microchiptechnology.pic16f84;

/**
 * PIC16F84 寄存器定义
 * 生成自: Microchip Technology/PIC16/PIC16F84
 * 版本: 
 */
public final class PIC16F84 {
    private PIC16F84() {} // 工具类
    // CPU架构: PIC16, 0位, 0 Hz

    // 外设定义
    // 8-bit timer/counter with prescaler
    public static final int TIMER0_BASE = (int);
    public static final int TIMER0_TMR0 = (int)0x00000001;

    // 16-bit timer/counter with prescaler
    public static final int TIMER1_BASE = (int);
    public static final int TIMER1_TMR1L = (int)0x0000000E;
    public static final int TIMER1_TMR1H = (int)0x0000000F;
    public static final int TIMER1_T1CON = (int)0x00000010;
    public static final int TIMER1_T1CON_TMR1ON = 0;  // Timer1 On
    public static final int TIMER1_T1CON_TMR1CS = 1;  // Timer1 Clock Source
    public static final int TIMER1_T1CON_T1SYNC = 2;  // Timer1 External Clock Input Synchronization
    public static final int TIMER1_T1CON_T1OSCEN = 3;  // Timer1 Oscillator Enable
    public static final int TIMER1_T1CON_T1CKPS0 = 4;  // Timer1 Input Clock Prescale Select bit 0
    public static final int TIMER1_T1CON_T1CKPS1 = 5;  // Timer1 Input Clock Prescale Select bit 1

    // Watchdog Timer
    public static final int WATCHDOG_BASE = (int);
    public static final int WATCHDOG_WDTCON = (int)0x00000007;
    public static final int WATCHDOG_WDTCON_SWDTEN = 0;  // Software Watchdog Timer Enable

    // 64-byte EEPROM data memory
    public static final int EEPROM_BASE = (int);
    public static final int EEPROM_EEDATA = (int)0x00000008;
    public static final int EEPROM_EEADR = (int)0x00000009;
    public static final int EEPROM_EECON1 = (int)0x00000088;
    public static final int EEPROM_EECON2 = (int)0x00000089;

    // General Purpose I/O
    public static final int GPIO_BASE = (int);
    public static final int GPIO_PORTA = (int)0x00000005;
    public static final int GPIO_PORTB = (int)0x00000006;
    public static final int GPIO_TRISA = (int)0x00000085;
    public static final int GPIO_TRISB = (int)0x00000086;

    // 中断向量定义
    public static final int IRQ_INT = 4;  // External interrupt on RB0/INT pin
    public static final int IRQ_TMR0 = 4;  // Timer0 overflow interrupt
    public static final int IRQ_PORTB = 4;  // PORTB change interrupt (RB4-RB7)
    public static final int IRQ_EEPROM = 4;  // EEPROM write complete interrupt

    public static native void pic16f84_init();
}
