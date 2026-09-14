# PIC16F84 设备定义 - Ruby 模块
# 生成自: Microchip Technology/PIC16/PIC16F84
# 版本: 
# 日期: 
# 作者: 
# 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
# CPU架构: PIC16
# 位宽: 0位
# 时钟频率: 0 Hz

module PIC16F84

  # 外设定义
  # 8-bit timer/counter with prescaler
  TIMER0_BASE = 
  TIMER0_TMR0_ADDR = 0x01
  # 16-bit timer/counter with prescaler
  TIMER1_BASE = 
  TIMER1_TMR1L_ADDR = 0x0E
  TIMER1_TMR1H_ADDR = 0x0F
  TIMER1_T1CON_ADDR = 0x10
  TIMER1_T1CON_TMR1ON_BIT = 0  # Timer1 On
  TIMER1_T1CON_TMR1CS_BIT = 1  # Timer1 Clock Source
  TIMER1_T1CON_T1SYNC_BIT = 2  # Timer1 External Clock Input Synchronization
  TIMER1_T1CON_T1OSCEN_BIT = 3  # Timer1 Oscillator Enable
  TIMER1_T1CON_T1CKPS0_BIT = 4  # Timer1 Input Clock Prescale Select bit 0
  TIMER1_T1CON_T1CKPS1_BIT = 5  # Timer1 Input Clock Prescale Select bit 1
  # Watchdog Timer
  WATCHDOG_BASE = 
  WATCHDOG_WDTCON_ADDR = 0x07
  WATCHDOG_WDTCON_SWDTEN_BIT = 0  # Software Watchdog Timer Enable
  # 64-byte EEPROM data memory
  EEPROM_BASE = 
  EEPROM_EEDATA_ADDR = 0x08
  EEPROM_EEADR_ADDR = 0x09
  EEPROM_EECON1_ADDR = 0x88
  EEPROM_EECON2_ADDR = 0x89
  # General Purpose I/O
  GPIO_BASE = 
  GPIO_PORTA_ADDR = 0x05
  GPIO_PORTB_ADDR = 0x06
  GPIO_TRISA_ADDR = 0x85
  GPIO_TRISB_ADDR = 0x86

  # 中断向量定义
  INT_INT = 4  # External interrupt on RB0/INT pin
  INT_TMR0 = 4  # Timer0 overflow interrupt
  INT_PORTB = 4  # PORTB change interrupt (RB4-RB7)
  INT_EEPROM = 4  # EEPROM write complete interrupt

end
