/**
 * TMS320F280049 寄存器定义
 * 生成自: Texas Instruments/C2000/TMS320F280049
 * 版本: 1.0
 */
export const tms320f280049 = {
  // CPU: C28x-DSP, 32位, 100000000 Hz

  // 寄存器定义
  // Accumulator Low
  AL: 0x00,
  // Accumulator High
  AH: 0x02,
  // Product High
  PH: 0x04,
  // Product Low
  PL: 0x06,
  // Temporary Register
  TREG: 0x08,
  AR0: 0x0A,
  AR1: 0x0C,
  // Status 0
  ST0: 0x20,
  // Status 1
  ST1: 0x22,
  // Program Counter
  PC: 0x24,
  // Stack Pointer
  SP: 0x26,

  // 内存段
  flash_START: 0x080000,
  flash_END: 0x0BFFFF,
  flash_SIZE: 262144,
  // Local Shared RAM
  sram_ls_START: 0x008000,
  sram_ls_END: 0x00BFFF,
  sram_ls_SIZE: 16384,
  // Global Shared RAM
  sram_gs_START: 0x00C000,
  sram_gs_END: 0x01FFFF,
  sram_gs_SIZE: 81920,
  peripheral_START: 0x400000,
  peripheral_END: 0x40FFFF,
  peripheral_SIZE: 65536,

  // 外设定义
  // PLL Clock Control
  PLL_BASE: 0x5C10,
  PLL_SYSPLLCTL1: 0x00005C10,
  PLL_SYSPLLCTL2: 0x00005C12,
  PLL_CLKSRCCTL1: 0x00005C14,
  PLL_CLKSRCCTL2: 0x00005C16,
  // GPIO Control Registers
  GPIO_CTRL_BASE: 0x7C00,
  GPIO_CTRL_GPACTRL: 0x00007C00,
  GPIO_CTRL_GPAQSEL1: 0x00007C02,
  GPIO_CTRL_GPAQSEL2: 0x00007C04,
  GPIO_CTRL_GPAMUX1: 0x00007C06,
  GPIO_CTRL_GPAMUX2: 0x00007C08,
  GPIO_CTRL_GPADIR: 0x00007C0A,
  GPIO_CTRL_GPAPUD: 0x00007C0C,
  // GPIO Data Registers
  GPIO_DATA_BASE: 0x7F00,
  GPIO_DATA_GPADAT: 0x00007F00,
  GPIO_DATA_GPASET: 0x00007F02,
  GPIO_DATA_GPACLEAR: 0x00007F04,
  GPIO_DATA_GPATOGGLE: 0x00007F06,
  GPIO_DATA_GPBDAT: 0x00007F08,
  GPIO_DATA_GPBSET: 0x00007F0A,
  GPIO_DATA_GPBCLEAR: 0x00007F0C,
  GPIO_DATA_GPBTOGGLE: 0x00007F0E,
  // GPIO B Control
  GPIO_B_CTRL_BASE: 0x7C20,
  GPIO_B_CTRL_GPBMUX1: 0x00007C20,
  GPIO_B_CTRL_GPBMUX2: 0x00007C22,
  GPIO_B_CTRL_GPBDIR: 0x00007C24,
  GPIO_B_CTRL_GPBPUD: 0x00007C26,
  // SCI-A UART
  SCI_A_BASE: 0x7320,
  SCI_A_SCICCR: 0x00007320,
  SCI_A_SCICTL1: 0x00007322,
  SCI_A_SCIBAUD: 0x00007324,
  SCI_A_SCIRXBUF: 0x0000732A,
  SCI_A_SCITXBUF: 0x0000732C,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_SCIA_RX: 8,  // SCI-A Receive Interrupt
  IRQ_SCIA_TX: 9,  // SCI-A Transmit Interrupt

  init: function() {
    // 硬件初始化
  }
};
