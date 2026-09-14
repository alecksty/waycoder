/**
 * 8051 寄存器定义
 * 生成自: Intel/MCS-51/8051
 * 版本: 1.0
 */
export const _8051 = {
  // CPU: MCS-51, 8位, 11059200 Hz

  // 寄存器定义
  // Accumulator
  ACC: 0xE0,
  // B Register
  B: 0xF0,
  // Program Status Word
  PSW: 0xD0,
  PSW_P: 0,  // Parity Flag
  PSW_OV: 2,  // Overflow Flag
  PSW_RS0: 3,  // Register Bank Select 0
  PSW_RS1: 4,  // Register Bank Select 1
  PSW_F0: 5,  // Flag 0
  PSW_AC: 6,  // Auxiliary Carry Flag
  PSW_CY: 7,  // Carry Flag
  // Stack Pointer
  SP: 0x81,
  // Data Pointer (DPL/DPH)
  DPTR: 0x82,

  // 内存段
  // Program Memory
  CODE_START: 0x0000,
  CODE_END: 0x0FFF,
  CODE_SIZE: 4096,
  // Internal Data Memory
  IDATA_START: 0x00,
  IDATA_END: 0x7F,
  IDATA_SIZE: 128,
  // Special Function Registers
  SFR_START: 0x80,
  SFR_END: 0xFF,
  SFR_SIZE: 128,
  // External Data Memory
  XDATA_START: 0x0000,
  XDATA_END: 0xFFFF,
  XDATA_SIZE: 65536,

  // 外设定义
  // Port 0
  PORT0_BASE: 0x80,
  PORT0_P0: 0x00000100,
  // Port 1
  PORT1_BASE: 0x90,
  PORT1_P1: 0x00000120,
  // Port 2
  PORT2_BASE: 0xA0,
  PORT2_P2: 0x00000140,
  // Port 3
  PORT3_BASE: 0xB0,
  PORT3_P3: 0x00000160,
  // Timer/Counter 0
  TIMER0_BASE: 0x8A,
  TIMER0_TH0: 0x00000116,
  TIMER0_TL0: 0x00000114,
  TIMER0_TMOD: 0x00000113,
  TIMER0_TMOD_M0_0: 0,  // Timer 0 Mode bit 0
  TIMER0_TMOD_M1_0: 1,  // Timer 0 Mode bit 1
  TIMER0_TMOD_C_T0: 2,  // Timer 0 Counter/Timer Select
  TIMER0_TMOD_GATE0: 3,  // Timer 0 Gate Control
  TIMER0_TCON: 0x00000112,
  TIMER0_TCON_TR0: 4,  // Timer 0 Run Control
  TIMER0_TCON_TF0: 5,  // Timer 0 Overflow Flag
  // Serial Port
  UART_BASE: 0x98,
  UART_SBUF: 0x00000131,
  UART_SCON: 0x00000130,
  UART_SCON_RI: 0,  // Receive Interrupt Flag
  UART_SCON_TI: 1,  // Transmit Interrupt Flag
  UART_SCON_REN: 4,  // Receive Enable
  UART_SCON_SM0: 6,  // Serial Mode bit 0
  UART_SCON_SM1: 7,  // Serial Mode bit 1

  // 中断向量
  IRQ_RESET: 0,  // Reset Vector
  IRQ_INT0: 1,  // External Interrupt 0
  IRQ_TIMER0: 2,  // Timer 0 Interrupt
  IRQ_INT1: 3,  // External Interrupt 1
  IRQ_TIMER1: 4,  // Timer 1 Interrupt
  IRQ_UART: 5,  // Serial Port Interrupt

  // 引脚定义
  PIN_P1.0: 1,  // Port 1, bit 0
  PIN_P1.1: 2,  // Port 1, bit 1
  PIN_P1.2: 3,  // Port 1, bit 2
  PIN_P1.3: 4,  // Port 1, bit 3
  PIN_P1.4: 5,  // Port 1, bit 4
  PIN_P1.5: 6,  // Port 1, bit 5
  PIN_P1.6: 7,  // Port 1, bit 6
  PIN_P1.7: 8,  // Port 1, bit 7
  PIN_RST: 9,  // Reset Pin
  PIN_RX: 10,  // Serial Receive (P3.0)
  PIN_TX: 11,  // Serial Transmit (P3.1)
  PIN_INT0: 12,  // External Interrupt 0 (P3.2)
  PIN_INT1: 13,  // External Interrupt 1 (P3.3)
  PIN_T0: 14,  // Timer 0 Input (P3.4)
  PIN_T1: 15,  // Timer 1 Input (P3.5)
  PIN_WR: 16,  // External Memory Write Strobe (P3.6)
  PIN_RD: 17,  // External Memory Read Strobe (P3.7)
  PIN_XTAL1: 18,  // Crystal Oscillator Input
  PIN_XTAL2: 19,  // Crystal Oscillator Output
  PIN_VCC: 20,  // Power Supply (+5V)
  PIN_GND: 21,  // Ground

  init: function() {
    // 硬件初始化
  }
};
