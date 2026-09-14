/**
 * CH32V003 寄存器定义
 * 生成自: WCH/CH32V0/CH32V003
 * 版本: 1.0
 */
export const ch32v003 = {
  // CPU: RISC-V, 32位, 48000000 Hz

  // 寄存器定义
  // Return Address
  x1: 0x04,
  // Stack Pointer (SP)
  x2: 0x08,
  // Global Pointer (GP)
  x3: 0x0C,
  // Program Counter
  pc: 0x3C,

  // 内存段
  flash_START: 0x08000000,
  flash_END: 0x08003FFF,
  flash_SIZE: 16384,
  sram_START: 0x20000000,
  sram_END: 0x200007FF,
  sram_SIZE: 2048,
  peripheral_START: 0x40000000,
  peripheral_END: 0x40003FFF,
  peripheral_SIZE: 16384,

  // 外设定义
  // Reset and Clock Control
  RCC_BASE: 0x40021000,
  RCC_CTLR: 0x40021000,
  RCC_CFGR0: 0x40021004,
  RCC_APB2PCENR: 0x40021018,
  RCC_APB2PCENR_IOPAEN: 2,  // GPIOA clock enable
  RCC_APB2PCENR_IOPCEN: 4,  // GPIOC clock enable
  RCC_APB2PCENR_IOPDEN: 5,  // GPIOD clock enable
  // General Purpose I/O Port A
  GPIOA_BASE: 0x40010800,
  GPIOA_CFGLR: 0x40010800,
  GPIOA_CFGHR: 0x40010804,
  GPIOA_INDR: 0x40010808,
  GPIOA_OUTDR: 0x4001080C,
  GPIOA_BSHR: 0x40010810,
  GPIOA_BCR: 0x40010814,
  // General Purpose I/O Port C
  GPIOC_BASE: 0x40011000,
  GPIOC_CFGLR: 0x40011000,
  GPIOC_CFGHR: 0x40011004,
  GPIOC_INDR: 0x40011008,
  GPIOC_OUTDR: 0x4001100C,
  GPIOC_BSHR: 0x40011010,
  GPIOC_BCR: 0x40011014,
  // General Purpose I/O Port D
  GPIOD_BASE: 0x40011400,
  GPIOD_CFGLR: 0x40011400,
  GPIOD_CFGHR: 0x40011404,
  GPIOD_INDR: 0x40011408,
  GPIOD_OUTDR: 0x4001140C,
  GPIOD_BSHR: 0x40011410,
  GPIOD_BCR: 0x40011414,
  // USART1
  USART1_BASE: 0x40013800,
  USART1_STATR: 0x40013800,
  USART1_DATAR: 0x40013804,
  USART1_BRR: 0x40013808,
  USART1_CTLR1: 0x4001380C,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_MachineSoftware: 3,  // 
  IRQ_MachineTimer: 7,  // 
  IRQ_MachineExternal: 11,  // 
  IRQ_USART1: 25,  // USART1 Global Interrupt

  init: function() {
    // 硬件初始化
  }
};
