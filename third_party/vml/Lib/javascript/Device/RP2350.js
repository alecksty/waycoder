/**
 * RP2350 寄存器定义
 * 生成自: Raspberry/RP2/RP2350
 * 版本: 1.0
 */
export const rp2350 = {
  // CPU: ARM-Cortex-M33, 32位, 150000000 Hz

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
  // XIP Flash
  flash_START: 0x10000000,
  flash_END: 0x107FFFFF,
  flash_SIZE: 8388608,
  // Total SRAM
  sram_START: 0x20000000,
  sram_END: 0x20081FFF,
  sram_SIZE: 532480,
  peripheral_START: 0x40000000,
  peripheral_END: 0x5000FFFF,
  peripheral_SIZE: 16777216,

  // 外设定义
  // Single-Cycle I/O (GPIO)
  SIO_BASE: 0xD0000000,
  SIO_GPIO_IN: 0xD0000004,
  SIO_GPIO_OUT: 0xD0000010,
  SIO_GPIO_OUT_SET: 0xD0000014,
  SIO_GPIO_OUT_CLR: 0xD0000018,
  SIO_GPIO_OUT_XOR: 0xD000001C,
  SIO_GPIO_OE: 0xD0000020,
  SIO_GPIO_OE_SET: 0xD0000024,
  SIO_GPIO_OE_CLR: 0xD0000028,
  // IO Bank 0 (GPIO control)
  IO_BANK0_BASE: 0x40028000,
  IO_BANK0_GPIO0_STATUS: 0x40028000,
  IO_BANK0_GPIO0_CTRL: 0x40028004,
  IO_BANK0_GPIO1_STATUS: 0x40028008,
  IO_BANK0_GPIO1_CTRL: 0x4002800C,
  // Pad controls for GPIO 0-29
  PADS_BANK0_BASE: 0x4002C000,
  PADS_BANK0_GPIO0: 0x4002C000,
  PADS_BANK0_GPIO1: 0x4002C004,
  // Reset Controller
  RESETS_BASE: 0x4000C000,
  RESETS_RESET: 0x4000C000,
  RESETS_RESET_DONE: 0x4000C008,

  // 中断向量
  IRQ_Reset: 0,  // 
  IRQ_SVCall: 11,  // 

  init: function() {
    // 硬件初始化
  }
};
