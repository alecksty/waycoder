/**
 * nRF52832 寄存器定义
 * 生成自: Nordic/nRF52/nRF52832
 * 版本: 1.0
 */
export const nrf52832 = {
  // CPU: ARM-Cortex-M4F, 32位, 64000000 Hz

  // 寄存器定义
  R0: 0x00,
  R1: 0x04,
  R2: 0x08,
  R3: 0x0C,
  SP: 0x34,
  LR: 0x38,
  PC: 0x3C,

  // 内存段
  flash_START: 0x00000000,
  flash_END: 0x0007FFFF,
  flash_SIZE: 524288,
  sram_START: 0x20000000,
  sram_END: 0x2000FFFF,
  sram_SIZE: 65536,
  peripheral_START: 0x40000000,
  peripheral_END: 0x400FFFFF,
  peripheral_SIZE: 1048576,
  // Factory Information Configuration Registers
  ficr_START: 0x10000000,
  ficr_END: 0x10000FFF,
  ficr_SIZE: 4096,

  // 外设定义
  // General Purpose I/O Port 0
  GPIO_P0_BASE: 0x50000000,
  GPIO_P0_OUT: 0x50000504,
  GPIO_P0_OUTSET: 0x50000508,
  GPIO_P0_OUTCLR: 0x5000050C,
  GPIO_P0_IN: 0x50000510,
  GPIO_P0_DIR: 0x50000514,
  GPIO_P0_DIRSET: 0x50000518,
  GPIO_P0_DIRCLR: 0x5000051C,
  // Power Control
  POWER_BASE: 0x40000000,
  POWER_DCDCEN: 0x400001C4,
  POWER_RAMSTATUS: 0x40000268,
  // Clock Control
  CLOCK_BASE: 0x40000000,
  CLOCK_HFCLKSTART: 0x40000108,
  CLOCK_HFCLKSTARTED: 0x40000208,

  // 中断向量
  IRQ_Reset: 0,  // 
  IRQ_SVCall: 11,  // 

  init: function() {
    // 硬件初始化
  }
};
