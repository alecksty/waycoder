/**
 * GD32VF103 寄存器定义
 * 生成自: GigaDevice/GD32/GD32VF103
 * 版本: 1.0
 */
export const gd32vf103 = {
  // CPU: RISC-V, 32位, 108000000 Hz

  // 寄存器定义
  // Return Address
  x1: 0x04,
  // Stack Pointer (SP)
  x2: 0x08,
  // Global Pointer (GP)
  x3: 0x0C,
  // Frame Pointer (FP)
  x8: 0x20,
  // Function Argument (A0)
  x10: 0x28,
  // Function Argument (A1)
  x11: 0x2C,
  // Program Counter
  pc: 0x3C,

  // 内存段
  flash_START: 0x08000000,
  flash_END: 0x0801FFFF,
  flash_SIZE: 131072,
  sram_START: 0x20000000,
  sram_END: 0x20007FFF,
  sram_SIZE: 32768,
  peripheral_START: 0x40000000,
  peripheral_END: 0x4003FFFF,
  peripheral_SIZE: 262144,

  // 外设定义
  // Reset and Clock Control
  RCU_BASE: 0x40021000,
  RCU_CTL: 0x40021000,
  RCU_CFG0: 0x40021004,
  RCU_CFG1: 0x40021008,
  RCU_APB2EN: 0x40021018,
  RCU_APB2EN_PAEN: 2,  // GPIOA enable
  RCU_APB2EN_PBEN: 3,  // GPIOB enable
  RCU_APB2EN_PCEN: 4,  // GPIOC enable
  RCU_APB2EN_USART0EN: 14,  // USART0 enable
  RCU_APB1EN: 0x4002101C,
  // General Purpose I/O Port A
  GPIOA_BASE: 0x40010800,
  GPIOA_CTL0: 0x40010800,
  GPIOA_CTL1: 0x40010804,
  GPIOA_ISTAT: 0x40010808,
  GPIOA_OCTL: 0x4001080C,
  GPIOA_BOP: 0x40010810,
  GPIOA_BC: 0x40010814,
  // General Purpose I/O Port B
  GPIOB_BASE: 0x40010C00,
  GPIOB_CTL0: 0x40010C00,
  GPIOB_CTL1: 0x40010C04,
  GPIOB_ISTAT: 0x40010C08,
  GPIOB_OCTL: 0x40010C0C,
  GPIOB_BOP: 0x40010C10,
  GPIOB_BC: 0x40010C14,
  // General Purpose I/O Port C
  GPIOC_BASE: 0x40011000,
  GPIOC_CTL0: 0x40011000,
  GPIOC_CTL1: 0x40011004,
  GPIOC_ISTAT: 0x40011008,
  GPIOC_OCTL: 0x4001100C,
  GPIOC_BOP: 0x40011010,
  GPIOC_BC: 0x40011014,
  // USART0
  USART0_BASE: 0x40013800,
  USART0_STATR: 0x40013800,
  USART0_DATAR: 0x40013804,
  USART0_BRR: 0x40013808,
  USART0_CTLR1: 0x4001380C,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_MachineSoftware: 3,  // 
  IRQ_MachineTimer: 7,  // 
  IRQ_MachineExternal: 11,  // 
  IRQ_USART0: 25,  // USART0 Global Interrupt

  init: function() {
    // 硬件初始化
  }
};
