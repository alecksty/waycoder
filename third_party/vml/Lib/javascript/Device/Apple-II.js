/**
 * Apple-II 寄存器定义
 * 生成自: Apple Computer/Apple II/Apple-II
 * 版本: 1.0
 */
export const apple_ii = {
  // CPU: MOS 6502, 8位, 1023000 Hz

  // 寄存器定义
  // Accumulator
  A: 0,
  // Index Register X
  X: 0,
  // Index Register Y
  Y: 0,
  // Stack Pointer
  SP: 0,
  // Program Counter
  PC: 0,
  // Status Register
  P: 0,

  // 外设定义
  // Apple II keyboard
  Keyboard_BASE: ,
  Keyboard_KBD: 0x0000C000,
  Keyboard_KBDSTRB: 0x0000C010,
  // Built-in speaker
  Speaker_BASE: ,
  Speaker_SPKR: 0x0000C030,
  // Cassette tape interface
  Cassette_BASE: ,
  Cassette_TAPEIN: 0x0000C060,
  Cassette_TAPEOUT: 0x0000C020,
  // Game controller port
  GamePort_BASE: ,
  GamePort_PADDLE0: 0x0000C064,
  GamePort_PADDLE1: 0x0000C065,
  GamePort_PADDLE2: 0x0000C066,
  GamePort_PADDLE3: 0x0000C067,
  GamePort_BUTTON0: 0x0000C061,
  GamePort_BUTTON1: 0x0000C062,
  // Disk II controller
  DiskController_BASE: ,
  DiskController_DISKUNIT: 0x0000C0E0,
  DiskController_DISKCMD: 0x0000C0E8,
  DiskController_DISKSTAT: 0x0000C0E9,
  DiskController_DISKDATA: 0x0000C0EA,

  // 中断向量
  IRQ_NMI: 65526,  // Non-maskable interrupt
  IRQ_RESET: 65528,  // Reset vector
  IRQ_IRQ: 65530,  // Interrupt request
  IRQ_BRK: 65532,  // Break instruction

  init: function() {
    // 硬件初始化
  }
};
