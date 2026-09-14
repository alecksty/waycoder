/**
 * BL618 寄存器定义
 * 生成自: Bouffalo Lab/BL6/BL618
 * 版本: 1.0
 */
export const bl618 = {
  // CPU: RISC-V, 32位, 320000000 Hz

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
  flash_START: 0x20000000,
  flash_END: 0x203FFFFF,
  flash_SIZE: 4194304,
  sram_hpsys_START: 0x22000000,
  sram_hpsys_END: 0x22003FFF,
  sram_hpsys_SIZE: 16384,
  // DTCM
  sram_dtcm_START: 0x22010000,
  sram_dtcm_END: 0x22017FFF,
  sram_dtcm_SIZE: 32768,
  sram_sys_START: 0x22020000,
  sram_sys_END: 0x2208FFFF,
  sram_sys_SIZE: 458752,
  peripheral_START: 0x30000000,
  peripheral_END: 0x300FFFFF,
  peripheral_SIZE: 1048576,

  // 外设定义
  // Global Control (Clock and Reset)
  GLB_BASE: 0x30000000,
  GLB_GLB_CLK_EN: 0x30000010,
  GLB_GLB_CLK_EN_GPIO_CLK_EN: 6,  // GPIO clock enable
  GLB_GLB_CLK_EN_UART0_CLK_EN: 12,  // UART0 clock enable
  GLB_GLB_SYS_CLK_CTRL: 0x30000014,
  GLB_GLB_PLL_CTRL: 0x3000001C,
  // GPIO Port A
  GPIO_P0_BASE: 0x30007000,
  GPIO_P0_GPIO_CFG0: 0x30007000,
  GPIO_P0_GPIO_CFG1: 0x30007004,
  GPIO_P0_GPIO_OE: 0x30007008,
  GPIO_P0_GPIO_OUT: 0x3000700C,
  GPIO_P0_GPIO_IN: 0x30007010,
  GPIO_P0_GPIO_SET: 0x30007014,
  GPIO_P0_GPIO_CLR: 0x30007018,
  GPIO_P0_GPIO_TOG: 0x3000701C,
  // GPIO Port B
  GPIO_P1_BASE: 0x30007200,
  GPIO_P1_GPIO_CFG0: 0x30007200,
  GPIO_P1_GPIO_CFG1: 0x30007204,
  GPIO_P1_GPIO_OE: 0x30007208,
  GPIO_P1_GPIO_OUT: 0x3000720C,
  GPIO_P1_GPIO_IN: 0x30007210,
  GPIO_P1_GPIO_SET: 0x30007214,
  GPIO_P1_GPIO_CLR: 0x30007218,
  GPIO_P1_GPIO_TOG: 0x3000721C,
  // UART 0
  UART0_BASE: 0x30002000,
  UART0_UART_CR: 0x30002000,
  UART0_UART_BRR: 0x30002004,
  UART0_UART_TDR: 0x30002008,
  UART0_UART_RDR: 0x3000200C,
  UART0_UART_SR: 0x30002010,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_MachineSoftware: 3,  // 
  IRQ_MachineTimer: 7,  // 
  IRQ_MachineExternal: 11,  // 
  IRQ_UART0: 20,  // UART0 Interrupt

  init: function() {
    // 硬件初始化
  }
};
