// PIC16F84 设备定义 - Dart 库
// 生成自: Microchip Technology/PIC16/PIC16F84
// 版本: 
// 日期: 
// 作者: 
// 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
// CPU架构: PIC16
// 位宽: 0位
// 时钟频率: 0 Hz

class PIC16F84Device {
  static const String deviceName = "PIC16F84";
  static const String manufacturer = "Microchip Technology";
  static const String family = "PIC16";
  static const String version = "";
  static const String architecture = "PIC16";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // 8-bit timer/counter with prescaler
  static const int TIMER0_BASE = ;
  static const int TIMER0_TMR0_ADDR = 0x01;
  // 16-bit timer/counter with prescaler
  static const int TIMER1_BASE = ;
  static const int TIMER1_TMR1L_ADDR = 0x0E;
  static const int TIMER1_TMR1H_ADDR = 0x0F;
  static const int TIMER1_T1CON_ADDR = 0x10;
  static const int TIMER1_T1CON_TMR1ON_BIT = 0;  // Timer1 On
  static const int TIMER1_T1CON_TMR1CS_BIT = 1;  // Timer1 Clock Source
  static const int TIMER1_T1CON_T1SYNC_BIT = 2;  // Timer1 External Clock Input Synchronization
  static const int TIMER1_T1CON_T1OSCEN_BIT = 3;  // Timer1 Oscillator Enable
  static const int TIMER1_T1CON_T1CKPS0_BIT = 4;  // Timer1 Input Clock Prescale Select bit 0
  static const int TIMER1_T1CON_T1CKPS1_BIT = 5;  // Timer1 Input Clock Prescale Select bit 1
  // Watchdog Timer
  static const int WATCHDOG_BASE = ;
  static const int WATCHDOG_WDTCON_ADDR = 0x07;
  static const int WATCHDOG_WDTCON_SWDTEN_BIT = 0;  // Software Watchdog Timer Enable
  // 64-byte EEPROM data memory
  static const int EEPROM_BASE = ;
  static const int EEPROM_EEDATA_ADDR = 0x08;
  static const int EEPROM_EEADR_ADDR = 0x09;
  static const int EEPROM_EECON1_ADDR = 0x88;
  static const int EEPROM_EECON2_ADDR = 0x89;
  // General Purpose I/O
  static const int GPIO_BASE = ;
  static const int GPIO_PORTA_ADDR = 0x05;
  static const int GPIO_PORTB_ADDR = 0x06;
  static const int GPIO_TRISA_ADDR = 0x85;
  static const int GPIO_TRISB_ADDR = 0x86;

  // 中断向量定义
  static const int INT_INT = 4;  // External interrupt on RB0/INT pin
  static const int INT_TMR0 = 4;  // Timer0 overflow interrupt
  static const int INT_PORTB = 4;  // PORTB change interrupt (RB4-RB7)
  static const int INT_EEPROM = 4;  // EEPROM write complete interrupt

}
