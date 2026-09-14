/**
 * Acorn-Archimedes-A310 寄存器定义
 * 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
 * 版本: 1.0
 */
export const acorn_archimedes_a310 = {
  // CPU: ARM250, 32位, 26000000 Hz

  // 寄存器定义
  // General Purpose Register 0
  R0: 0x00,
  // General Purpose Register 1
  R1: 0x04,
  // General Purpose Register 2
  R2: 0x08,
  // General Purpose Register 3
  R3: 0x0C,
  // General Purpose Register 4
  R4: 0x10,
  // General Purpose Register 5
  R5: 0x14,
  // General Purpose Register 6
  R6: 0x18,
  // General Purpose Register 7
  R7: 0x1C,
  // General Purpose Register 8
  R8: 0x20,
  // General Purpose Register 9
  R9: 0x24,
  // General Purpose Register 10
  R10: 0x28,
  // General Purpose Register 11 (fp)
  R11: 0x2C,
  // General Purpose Register 12
  R12: 0x30,
  // Stack Pointer (R13)
  SP: 0x34,
  // Link Register (R14)
  LR: 0x38,
  // Program Counter (R15)
  PC: 0x3C,
  // Processor Status Register
  PSR: 0x40,
  PSR_MODE: 0,  // Mode bits (0-4)
  PSR_T: 5,  // Thumb state
  PSR_F: 6,  // FIQ disable
  PSR_I: 7,  // IRQ disable
  PSR_V: 28,  // Overflow
  PSR_C: 29,  // Carry
  PSR_Z: 30,  // Zero
  PSR_N: 31,  // Negative

  // 内存段
  // RISC OS ROM (512KB)
  rom_START: 0x00000000,
  rom_END: 0x0007FFFF,
  rom_SIZE: 524288,
  // Main RAM (up to 4MB)
  ram_START: 0x00080000,
  ram_END: 0x003FFFFF,
  ram_SIZE: 3932160,
  // Video RAM (4MB, VIDC)
  vram_START: 0x00400000,
  vram_END: 0x007FFFFF,
  vram_SIZE: 4194304,
  // I/O controller (IOC)
  io_START: 0x03000000,
  io_END: 0x0301FFFF,
  io_SIZE: 131072,
  // Memory Controller (MEMC)
  memc_START: 0x03200000,
  memc_END: 0x0320FFFF,
  memc_SIZE: 4096,
  // Video Controller (VIDC)
  vidc_START: 0x03400000,
  vidc_END: 0x0340FFFF,
  vidc_SIZE: 4096,
  // I/O and Memory DMA
  iomd_START: 0x03300000,
  iomd_END: 0x0330FFFF,
  iomd_SIZE: 4096,

  // 外设定义
  // I/O Controller (IOC) - Interrupt/Keyboard/RTC
  IOC_BASE: 0x03000000,
  IOC_IOC_TIMER1: 0x06000000,
  IOC_IOC_TIMER2: 0x06000004,
  IOC_IOC_IOSEL: 0x06000008,
  IOC_IOC_IRQST: 0x0600000C,
  IOC_IOC_IRQLATCH: 0x06000010,
  IOC_IOC_FIQST: 0x06000014,
  IOC_IOC_FIQEN: 0x06000018,
  IOC_IOC_IRQEN: 0x0600001C,
  IOC_IOC_KBDDATA: 0x06000020,
  IOC_IOC_KBDCR: 0x06000024,
  IOC_IOC_RTCDR: 0x06000028,
  IOC_IOC_RTCCR: 0x0600002C,
  IOC_IOC_PRST: 0x06000030,
  IOC_IOC_PORTA: 0x06000034,
  IOC_IOC_PORTB: 0x06000038,
  IOC_IOC_PORTC: 0x0600003C,
  // Memory Controller (MEMC1)
  MEMC_BASE: 0x03200000,
  MEMC_MEMC_PT: 0x06400000,
  MEMC_MEMC_CTRL: 0x06400004,
  MEMC_MEMC_DRAM: 0x06400008,
  MEMC_MEMC_ERR: 0x0640000C,
  // Video Controller - VIDC1
  VIDC_BASE: 0x03400000,
  VIDC_VIDC_PALETTE: 0x06800000,
  VIDC_VIDC_STARTL: 0x06800004,
  VIDC_VIDC_STARTH: 0x06800008,
  VIDC_VIDC_CONFIG: 0x0680000C,
  VIDC_VIDC_HDISP: 0x06800010,
  VIDC_VIDC_VDISP: 0x06800014,
  VIDC_VIDC_HSYNC: 0x06800018,
  VIDC_VIDC_VSYNC: 0x0680001C,
  VIDC_VIDC_BORDER: 0x06800020,
  VIDC_VIDC_CURSOR: 0x06800024,
  VIDC_VIDC_SOUND: 0x06800028,
  // Intel 82710 Floppy Disk Controller
  FDC_BASE: 0x03010000,
  FDC_FDC_STATUS: 0x06020000,
  FDC_FDC_COMMAND: 0x06020000,
  FDC_FDC_TRACK: 0x06020004,
  FDC_FDC_SECTOR: 0x06020008,
  FDC_FDC_DATA: 0x0602000C,
  // Serial Port (via IOC)
  SERIAL_BASE: 0x03010010,
  SERIAL_SERIAL_TX: 0x06020020,
  SERIAL_SERIAL_RX: 0x06020024,
  SERIAL_SERIAL_CTRL: 0x06020028,

  // 中断向量
  IRQ_RESET: 0,  // Reset
  IRQ_UND: 1,  // Undefined instruction
  IRQ_SWI: 2,  // Software Interrupt (SWI/SVC)
  IRQ_PABORT: 3,  // Prefetch Abort
  IRQ_DABORT: 4,  // Data Abort
  IRQ_ADDRESS: 5,  // Address Exception
  IRQ_IRQ: 6,  // IRQ interrupt (IOC)
  IRQ_FIQ: 7,  // FIQ interrupt (VIDC)

  // 引脚定义
  PIN_VCC: 1,  // +5V Power
  PIN_GND: 2,  // Ground
  PIN_CLK: 3,  // ARM clock (26MHz)
  PIN_nRESET: 4,  // Reset (active low)
  PIN_nMREQ: 5,  // Memory Request (active low)
  PIN_nIORQ: 6,  // I/O Request (active low)
  PIN_nRW: 7,  // Read/Write (0=write, 1=read)
  PIN_MAS0: 8,  // Master address bit 0
  PIN_MAS1: 9,  // Master address bit 1
  PIN_MAS2: 10,  // Master address bit 2
  PIN_LOCK: 11,  // Bus lock
  PIN_nMREQ: 12,  // Memory request (active low)
  PIN_nWAIT: 13,  // Wait state (active low)
  PIN_nIRQLINE: 14,  // IRQ line (active low)
  PIN_nFIRQLINE: 15,  // FIQ line (active low)
  PIN_A1-A25: 16,  // Address Bus (26-bit)
  PIN_D0-D31: 17,  // Data Bus (32-bit)

  init: function() {
    // 硬件初始化
  }
};
