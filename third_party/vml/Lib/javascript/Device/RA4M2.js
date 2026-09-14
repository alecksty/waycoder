/**
 * RA4M2 寄存器定义
 * 生成自: Renesas/RA/RA4M2
 * 版本: 1.0
 */
export const ra4m2 = {
  // CPU: ARM-Cortex-M4, 32位, 100000000 Hz

  // 寄存器定义
  R0: 0x00,
  R1: 0x04,
  R2: 0x08,
  R3: 0x0C,
  R4: 0x10,
  R5: 0x14,
  SP: 0x34,
  LR: 0x38,
  PC: 0x3C,

  // 内存段
  flash_START: 0x00000000,
  flash_END: 0x0003FFFF,
  flash_SIZE: 262144,
  // SRAM0
  sram_START: 0x1FFE0000,
  sram_END: 0x1FFE7FFF,
  sram_SIZE: 32768,
  // SRAM1
  sram1_START: 0x20000000,
  sram1_END: 0x20017FFF,
  sram1_SIZE: 98304,
  peripheral_START: 0x40000000,
  peripheral_END: 0x400FFFFF,
  peripheral_SIZE: 1048576,

  // 外设定义
  // Module Stop Control
  MSTP_BASE: 0x40020000,
  MSTP_MSTPCR_A: 0x40020020,
  MSTP_MSTPCR_A_MSTP41: 9,  // GPIO A stop
  MSTP_MSTPCR_A_MSTP42: 10,  // GPIO B stop
  MSTP_MSTPCR_B: 0x40020024,
  MSTP_MSTPCR_C: 0x40020028,
  MSTP_MSTPCR_D: 0x4002002C,
  // Interrupt Controller Unit
  ICU_BASE: 0x40030000,
  ICU_IRQCR0: 0x40030600,
  ICU_IRQCR1: 0x40030602,
  // General Purpose I/O Port A
  GPIOA_BASE: 0x40040000,
  GPIOA_PDR: 0x40040000,
  GPIOA_PODR: 0x40040004,
  GPIOA_PIDR: 0x40040008,
  GPIOA_PMR: 0x40040010,
  GPIOA_PCR: 0x40040018,
  // General Purpose I/O Port B
  GPIOB_BASE: 0x40040020,
  GPIOB_PDR: 0x40040020,
  GPIOB_PODR: 0x40040024,
  GPIOB_PIDR: 0x40040028,
  GPIOB_PMR: 0x40040030,
  // SCI UART 0
  SCIUART0_BASE: 0x40070000,
  SCIUART0_SCR: 0x40070000,
  SCIUART0_BRR: 0x40070004,
  SCIUART0_TDR: 0x40070008,
  SCIUART0_RDR: 0x4007000C,
  SCIUART0_SSR: 0x40070010,

  // 中断向量
  IRQ_Reset: 0,  // 
  IRQ_SVCall: 11,  // 
  IRQ_SCIUART0_RXI: 24,  // SCI UART0 Receive Interrupt
  IRQ_SCIUART0_TXI: 25,  // SCI UART0 Transmit Interrupt

  init: function() {
    // 硬件初始化
  }
};
