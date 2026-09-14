/**
 * PIC32MX170F256B 寄存器定义
 * 生成自: Microchip/PIC32/PIC32MX170F256B
 * 版本: 1.0
 */
export const pic32mx170f256b = {
  // CPU: MIPS32-M4K, 32位, 50000000 Hz

  // 寄存器定义
  // Hard-wired zero
  $0: 0x00,
  // AT
  $1: 0x04,
  // V0
  $2: 0x08,
  // V1
  $3: 0x0C,
  // A0
  $4: 0x10,
  // A1
  $5: 0x14,
  // Stack Pointer (SP)
  $29: 0x74,
  // Return Address (RA)
  $31: 0x7C,
  // Program Counter
  PC: 0x80,

  // 内存段
  // Program Flash
  flash_START: 0x9D000000,
  flash_END: 0x9D03FFFF,
  flash_SIZE: 262144,
  sram_START: 0xA0000000,
  sram_END: 0xA000FFFF,
  sram_SIZE: 65536,
  peripheral_START: 0xBF800000,
  peripheral_END: 0xBF8FFFFF,
  peripheral_SIZE: 1048576,
  // Boot Flash
  bootflash_START: 0xBFC00000,
  bootflash_END: 0xBFC02FFF,
  bootflash_SIZE: 12288,

  // 外设定义
  // General Purpose I/O Port A
  PORTA_BASE: 0xBF886000,
  PORTA_TRISA: 0xBF886000,
  PORTA_PORTA: 0xBF886010,
  PORTA_LATA: 0xBF886020,
  PORTA_ODCA: 0xBF886030,
  // General Purpose I/O Port B
  PORTB_BASE: 0xBF886100,
  PORTB_TRISB: 0xBF886100,
  PORTB_PORTB: 0xBF886110,
  PORTB_LATB: 0xBF886120,
  PORTB_ODCB: 0xBF886130,
  // UART1
  UART1_BASE: 0xBF822000,
  UART1_UXMODE: 0xBF822000,
  UART1_UXSTA: 0xBF822004,
  UART1_UXTXREG: 0xBF822008,
  UART1_UXRXREG: 0xBF82200C,
  UART1_UXBRG: 0xBF822010,

  // 中断向量
  IRQ_Reset: 0,  // 
  IRQ_UART1: 8,  // UART1 Interrupt

  init: function() {
    // 硬件初始化
  }
};
