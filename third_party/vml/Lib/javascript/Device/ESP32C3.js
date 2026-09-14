/**
 * ESP32-C3 寄存器定义
 * 生成自: Espressif/ESP32-C/ESP32-C3
 * 版本: 1.0
 */
export const esp32_c3 = {
  // CPU: RISC-V, 32位, 160000000 Hz

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
  // Flash via Cache
  flash_START: 0x42000000,
  flash_END: 0x427FFFFF,
  flash_SIZE: 8388608,
  // Internal SRAM
  sram_START: 0x3FC80000,
  sram_END: 0x3FCE3FFF,
  sram_SIZE: 409600,
  peripheral_START: 0x60000000,
  peripheral_END: 0x600FFFFF,
  peripheral_SIZE: 1048576,

  // 外设定义
  // General Purpose I/O
  GPIO_BASE: 0x60004000,
  GPIO_OUT: 0x60004004,
  GPIO_OUT_W1TS: 0x60004008,
  GPIO_OUT_W1TC: 0x6000400C,
  GPIO_IN: 0x60004010,
  GPIO_ENABLE: 0x60004020,
  GPIO_ENABLE_W1TS: 0x60004024,
  GPIO_ENABLE_W1TC: 0x60004028,
  // I/O MUX
  IO_MUX_BASE: 0x60009000,
  IO_MUX_GPIO0: 0x60009000,
  IO_MUX_GPIO1: 0x60009004,
  IO_MUX_GPIO2: 0x60009008,
  IO_MUX_GPIO3: 0x6000900C,
  // RTC Control
  RTC_CNTL_BASE: 0x60008000,
  RTC_CNTL_OPTIONS0: 0x60008000,
  RTC_CNTL_CLK_CONF: 0x60008030,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_MachineSoftware: 3,  // 
  IRQ_MachineTimer: 7,  // 
  IRQ_MachineExternal: 11,  // 

  init: function() {
    // 硬件初始化
  }
};
