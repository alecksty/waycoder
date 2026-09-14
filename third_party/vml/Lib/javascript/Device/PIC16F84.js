/**
 * PIC16F84 寄存器定义
 * 生成自: Microchip Technology/PIC16/PIC16F84
 * 版本: 
 */
export const pic16f84 = {
  // CPU: PIC16, 0位, 0 Hz

  // 外设定义
  // 8-bit timer/counter with prescaler
  Timer0_BASE: ,
  Timer0_TMR0: 0x00000001,
  // 16-bit timer/counter with prescaler
  Timer1_BASE: ,
  Timer1_TMR1L: 0x0000000E,
  Timer1_TMR1H: 0x0000000F,
  Timer1_T1CON: 0x00000010,
  Timer1_T1CON_TMR1ON: 0,  // Timer1 On
  Timer1_T1CON_TMR1CS: 1,  // Timer1 Clock Source
  Timer1_T1CON_T1SYNC: 2,  // Timer1 External Clock Input Synchronization
  Timer1_T1CON_T1OSCEN: 3,  // Timer1 Oscillator Enable
  Timer1_T1CON_T1CKPS0: 4,  // Timer1 Input Clock Prescale Select bit 0
  Timer1_T1CON_T1CKPS1: 5,  // Timer1 Input Clock Prescale Select bit 1
  // Watchdog Timer
  Watchdog_BASE: ,
  Watchdog_WDTCON: 0x00000007,
  Watchdog_WDTCON_SWDTEN: 0,  // Software Watchdog Timer Enable
  // 64-byte EEPROM data memory
  EEPROM_BASE: ,
  EEPROM_EEDATA: 0x00000008,
  EEPROM_EEADR: 0x00000009,
  EEPROM_EECON1: 0x00000088,
  EEPROM_EECON2: 0x00000089,
  // General Purpose I/O
  GPIO_BASE: ,
  GPIO_PORTA: 0x00000005,
  GPIO_PORTB: 0x00000006,
  GPIO_TRISA: 0x00000085,
  GPIO_TRISB: 0x00000086,

  // 中断向量
  IRQ_INT: 4,  // External interrupt on RB0/INT pin
  IRQ_TMR0: 4,  // Timer0 overflow interrupt
  IRQ_PORTB: 4,  // PORTB change interrupt (RB4-RB7)
  IRQ_EEPROM: 4,  // EEPROM write complete interrupt

  init: function() {
    // 硬件初始化
  }
};
