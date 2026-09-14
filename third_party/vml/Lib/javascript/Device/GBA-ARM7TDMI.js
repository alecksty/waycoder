/**
 * ARM7TDMI 寄存器定义
 * 生成自: ARM/ARM7/ARM7TDMI
 * 版本: 1.0
 */
export const arm7tdmi = {
  // CPU: ARM7TDMI, 32位, 16780000 Hz

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
  // General Purpose Register 9 / SB
  R9: 0x24,
  // General Purpose Register 10 / SL
  R10: 0x28,
  // Frame Pointer / FP
  R11: 0x2C,
  // Intra-Procedure-call Scratch Register / IP
  R12: 0x30,
  // Stack Pointer / SP
  R13: 0x34,
  // Link Register / LR
  R14: 0x38,
  // Program Counter / PC
  R15: 0x3C,
  // Current Program Status Register
  CPSR: 0x40,
  CPSR_MODE: 0,  // Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
  CPSR_T: 5,  // Thumb State Bit (1=Thumb mode)
  CPSR_F: 6,  // FIQ Disable
  CPSR_I: 7,  // IRQ Disable
  CPSR_A: 8,  // Imprecise Data Abort Disable
  CPSR_E: 9,  // Endianness (0=Little)
  CPSR_GE: 0,  // Greater-than-or-Equal flags
  CPSR_N: 31,  // Negative
  CPSR_Z: 30,  // Zero
  CPSR_C: 29,  // Carry
  CPSR_V: 28,  // Overflow
  // Saved PSR (Supervisor Mode)
  SPSR_SVC: 0x44,
  // Saved PSR (Abort Mode)
  SPSR_ABT: 0x48,
  // Saved PSR (IRQ Mode)
  SPSR_IRQ: 0x4C,
  // Saved PSR (FIQ Mode)
  SPSR_FIQ: 0x50,

  // 内存段
  // Internal Work RAM (32KB, 2-cycle access)
  iwram_START: 0x03000000,
  iwram_END: 0x03007FFF,
  iwram_SIZE: 32768,
  // Internal Work RAM Fast (high-speed region)
  iwram_fast_START: 0x03008000,
  iwram_fast_END: 0x03FFFFFF,
  iwram_fast_SIZE: 32752,
  // Video RAM (96KB + 64KB OBJ VRAM)
  vram_START: 0x06000000,
  vram_END: 0x06017FFF,
  vram_SIZE: 98304,
  // BG Palette RAM (256 colors x 2 bytes)
  palette_START: 0x05000200,
  palette_END: 0x050003FF,
  palette_SIZE: 512,
  // Object Palette RAM
  obj_palette_START: 0x05000400,
  obj_palette_END: 0x050005FF,
  obj_palette_SIZE: 512,
  // Object Attribute Memory (OAM, 128 sprites)
  oam_START: 0x07000000,
  oam_END: 0x070003FF,
  oam_SIZE: 1024,
  // Cartridge ROM (max 32MB)
  rom_START: 0x08000000,
  rom_END: 0x09FFFFFF,
  rom_SIZE: 33554432,
  // Cartridge SRAM / Flash
  cart_ram_START: 0x0E000000,
  cart_ram_END: 0x0E00FFFF,
  cart_ram_SIZE: 65536,
  // GBA BIOS (16KB)
  bios_START: 0x00000000,
  bios_END: 0x00003FFF,
  bios_SIZE: 16384,
  // I/O Registers (MMIO)
  io_regs_START: 0x04000000,
  io_regs_END: 0x04FFFFFF,
  io_regs_SIZE: 16777216,

  // 外设定义
  // LCD Controller
  LCD_BASE: 0x04000000,
  LCD_DISPCNT: 0x08000000,
  LCD_DISPCNT_BG_MODE: 0,  // BG Mode (0-6)
  LCD_DISPCNT_GB_WINDOW: 5,  // Game Boy Window Enable
  LCD_DISPCNT_WIN0_ENABLE: 13,  // Window 0 Enable
  LCD_DISPCNT_WIN1_ENABLE: 14,  // Window 1 Enable
  LCD_DISPCNT_OBJ_WIN: 15,  // Object Window Enable
  LCD_DISPCNT_BG0_ENABLE: 8,  // BG0 Enable
  LCD_DISPCNT_BG1_ENABLE: 9,  // BG1 Enable
  LCD_DISPCNT_BG2_ENABLE: 10,  // BG2 Enable
  LCD_DISPCNT_BG3_ENABLE: 11,  // BG3 Enable
  LCD_DISPCNT_OBJ_ENABLE: 12,  // Object/Sprite Enable
  LCD_GREEN_SWAP: 0x08000002,
  LCD_DISPSTAT: 0x08000004,
  LCD_DISPSTAT_V_COUNT: 0,  // Vertical Line Counter
  LCD_DISPSTAT_VBLANK_FLAG: 0,  // V-Blank Flag (read-only)
  LCD_DISPSTAT_HBLANK_FLAG: 1,  // H-Blank Flag (read-only)
  LCD_DISPSTAT_V_COUNT_FLAG: 2,  // V-Count Flag (LY==LYC)
  LCD_DISPSTAT_VBLANK_IRQ: 3,  // V-Blank IRQ Enable
  LCD_DISPSTAT_HBLANK_IRQ: 4,  // H-Blank IRQ Enable
  LCD_DISPSTAT_VCOUNT_IRQ: 5,  // V-Count IRQ Enable
  LCD_VCOUNT: 0x08000006,
  LCD_BG0CNT: 0x08000008,
  LCD_BG1CNT: 0x0800000A,
  LCD_BG2CNT: 0x0800000C,
  LCD_BG3CNT: 0x0800000E,
  LCD_BG0HOFS: 0x08000010,
  LCD_BG0VOFS: 0x08000012,
  LCD_BG1HOFS: 0x08000014,
  LCD_BG1VOFS: 0x08000016,
  LCD_BG2HOFS: 0x08000018,
  LCD_BG2VOFS: 0x0800001A,
  LCD_BG3HOFS: 0x0800001C,
  LCD_BG3VOFS: 0x0800001E,
  LCD_BG2PA: 0x08000020,
  LCD_BG2PB: 0x08000022,
  LCD_BG2PC: 0x08000024,
  LCD_BG2PD: 0x08000026,
  LCD_BG2X: 0x08000028,
  LCD_BG2Y: 0x0800002C,
  LCD_BG3PA: 0x08000030,
  LCD_BG3PB: 0x08000032,
  LCD_BG3PC: 0x08000034,
  LCD_BG3PD: 0x08000036,
  LCD_BG3X: 0x08000038,
  LCD_BG3Y: 0x0800003C,
  LCD_WIN0H: 0x08000040,
  LCD_WIN1H: 0x08000042,
  LCD_WIN0V: 0x08000044,
  LCD_WIN1V: 0x08000046,
  LCD_WININ: 0x08000048,
  LCD_WINOUT: 0x08000049,
  LCD_MOSAIC: 0x0800004C,
  LCD_BLDCNT: 0x08000050,
  LCD_BLDCNT_BG1ST: 0,  // BG1 1st Target
  LCD_BLDCNT_BG2ST: 1,  // BG2 1st Target
  LCD_BLDCNT_BG3ST: 2,  // BG3 1st Target
  LCD_BLDCNT_OBJST: 3,  // Object 1st Target
  LCD_BLDCNT_BDST: 4,  // Backdrop 1st Target
  LCD_BLDCNT_BLEND_MODE: 0,  // Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
  LCD_BLDCNT_BG1ST2: 8,  // BG1 2nd Target
  LCD_BLDCNT_BG2ST2: 9,  // BG2 2nd Target
  LCD_BLDCNT_BG3ST2: 10,  // BG3 2nd Target
  LCD_BLDCNT_OBJST2: 11,  // Object 2nd Target
  LCD_BLDCNT_BDST2: 12,  // Backdrop 2nd Target
  LCD_BLDALPHA: 0x08000052,
  LCD_BLDY: 0x08000054,
  // Direct Memory Access Controller
  DMA_BASE: 0x040000B0,
  DMA_DMA0SAD: 0x08000160,
  DMA_DMA0DAD: 0x08000164,
  DMA_DMA0CNT_L: 0x08000168,
  DMA_DMA0CNT_H: 0x0800016A,
  DMA_DMA0CNT_H_TRANSFER_COUNT: 0,  // Number of Transfers
  DMA_DMA0CNT_H_DEST_ADD_MODE: 0,  // Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
  DMA_DMA0CNT_H_SRC_ADD_MODE: 0,  // Source Address Control (0=fix, 1=inc, 2=dec)
  DMA_DMA0CNT_H_REPEAT: 18,  // Repeat (for 16-bit repeat mode)
  DMA_DMA0CNT_H_WORD_SIZE: 20,  // Word Size (0=16-bit, 1=32-bit)
  DMA_DMA0CNT_H_DRQ: 27,  // DRQ Trigger (DMA from external source)
  DMA_DMA0CNT_H_TIMING: 0,  // Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
  DMA_DMA0CNT_H_ENABLE: 31,  // DMA Enable
  DMA_DMA1SAD: 0x0800016C,
  DMA_DMA1DAD: 0x08000170,
  DMA_DMA1CNT_L: 0x08000174,
  DMA_DMA1CNT_H: 0x08000176,
  DMA_DMA2SAD: 0x08000178,
  DMA_DMA2DAD: 0x0800017C,
  DMA_DMA2CNT_L: 0x08000180,
  DMA_DMA2CNT_H: 0x08000182,
  DMA_DMA3SAD: 0x08000184,
  DMA_DMA3DAD: 0x08000188,
  DMA_DMA3CNT_L: 0x0800018C,
  DMA_DMA3CNT_H: 0x0800018E,
  // Timer Units (4 timers)
  TIMER_BASE: 0x04000100,
  TIMER_TM0CNT_L: 0x08000200,
  TIMER_TM0CNT_H: 0x08000202,
  TIMER_TM0CNT_H_PRESCALER: 0,  // Prescaler (0=1, 1=64, 2=256, 3=1024)
  TIMER_TM0CNT_H_COUNT_UP: 2,  // Count Up (cascade mode)
  TIMER_TM0CNT_H_IRQ_ENABLE: 6,  // Timer IRQ Enable
  TIMER_TM0CNT_H_ENABLE: 7,  // Timer Enable
  TIMER_TM1CNT_L: 0x08000204,
  TIMER_TM1CNT_H: 0x08000206,
  TIMER_TM2CNT_L: 0x08000208,
  TIMER_TM2CNT_H: 0x0800020A,
  TIMER_TM3CNT_L: 0x0800020C,
  TIMER_TM3CNT_H: 0x0800020E,
  // Serial I/O (JOY BUS / Link Cable)
  SIO_BASE: 0x04000120,
  SIO_SIOCNT: 0x08000240,
  SIO_SIOCNT_CLOCK_SEL: 0,  // Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
  SIO_SIOCNT_SO_ENABLE: 3,  // SO Output Enable
  SIO_SIOCNT_RECV_ENABLE: 5,  // Receive Enable
  SIO_SIOCNT_SEND_ENABLE: 6,  // Send Enable
  SIO_SIOCNT_START_BIT: 7,  // Start Transfer
  SIO_SIODATA8: 0x0800024A,
  SIO_JOYCNT: 0x08000250,
  SIO_JOYSTAT: 0x08000254,
  SIO_JOY_RECV: 0x08000270,
  SIO_JOY_TRANS: 0x08000274,
  // Key Input
  KEYINPUT_BASE: 0x04000130,
  KEYINPUT_KEYINPUT: 0x08000260,
  KEYINPUT_KEYINPUT_A: 0,  // A Button (0=Pressed)
  KEYINPUT_KEYINPUT_B: 1,  // B Button (0=Pressed)
  KEYINPUT_KEYINPUT_SELECT: 2,  // Select Button (0=Pressed)
  KEYINPUT_KEYINPUT_START: 3,  // Start Button (0=Pressed)
  KEYINPUT_KEYINPUT_RIGHT: 4,  // D-Pad Right (0=Pressed)
  KEYINPUT_KEYINPUT_LEFT: 5,  // D-Pad Left (0=Pressed)
  KEYINPUT_KEYINPUT_UP: 6,  // D-Pad Up (0=Pressed)
  KEYINPUT_KEYINPUT_DOWN: 7,  // D-Pad Down (0=Pressed)
  KEYINPUT_KEYINPUT_R: 8,  // R Shoulder Button (0=Pressed)
  KEYINPUT_KEYINPUT_L: 9,  // L Shoulder Button (0=Pressed)
  KEYINPUT_KEYCNT: 0x08000262,
  KEYINPUT_KEYCNT_KEY_MASK: 0,  // Key Interrupt Enable Mask
  KEYINPUT_KEYCNT_IRQ_ENABLE: 14,  // Key Interrupt Enable
  // Interrupt Control
  INTERRUPT_BASE: 0x04000200,
  INTERRUPT_IME: 0x08000408,
  INTERRUPT_IE: 0x08000410,
  INTERRUPT_IE_VBLANK: 0,  // V-Blank Interrupt Enable
  INTERRUPT_IE_HBLANK: 1,  // H-Blank Interrupt Enable
  INTERRUPT_IE_VCOUNT: 2,  // V-Count Match Interrupt Enable
  INTERRUPT_IE_TIMER0: 3,  // Timer 0 Interrupt Enable
  INTERRUPT_IE_TIMER1: 4,  // Timer 1 Interrupt Enable
  INTERRUPT_IE_TIMER2: 5,  // Timer 2 Interrupt Enable
  INTERRUPT_IE_TIMER3: 6,  // Timer 3 Interrupt Enable
  INTERRUPT_IE_SIO: 7,  // Serial I/O Interrupt Enable
  INTERRUPT_IE_DMA0: 8,  // DMA 0 Interrupt Enable
  INTERRUPT_IE_DMA1: 9,  // DMA 1 Interrupt Enable
  INTERRUPT_IE_DMA2: 10,  // DMA 2 Interrupt Enable
  INTERRUPT_IE_DMA3: 11,  // DMA 3 Interrupt Enable
  INTERRUPT_IE_KEYPAD: 12,  // Keypad Interrupt Enable
  INTERRUPT_IE_CART: 13,  // Game Pak Interrupt Enable
  INTERRUPT_IF: 0x08000414,
  // Waitstate Control
  WAITCNT_BASE: 0x04000204,
  WAITCNT_WAITCNT: 0x08000408,
  WAITCNT_WAITCNT_PHI_OD: 0,  // PHI Terminal Output (0=Disable)
  WAITCNT_WAITCNT_SRAM_WS: 0,  // SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
  WAITCNT_WAITCNT_WS0_N: 0,  // Wait State 0 (ROM/SRAM 1st access)
  WAITCNT_WAITCNT_WS0_S: 5,  // Wait State 0 (ROM/SRAM 2nd access)
  WAITCNT_WAITCNT_WS1_N: 0,  // Wait State 1 (ROM 2nd access)
  WAITCNT_WAITCNT_WS1_S: 8,  // Wait State 1 (ROM 2nd access short)
  WAITCNT_WAITCNT_WS2_N: 0,  // Wait State 2 (ROM 3rd access)
  WAITCNT_WAITCNT_WS2_S: 11,  // Wait State 2 (ROM 3rd access short)
  WAITCNT_WAITCNT_PREFE: 12,  // Prefetch Enable (GBA SP only)

  // 中断向量
  IRQ_VBLANK: 0,  // V-Blank Interrupt
  IRQ_HBLANK: 1,  // H-Blank Interrupt
  IRQ_VCOUNT: 2,  // V-Count Match Interrupt
  IRQ_TIMER0: 3,  // Timer 0 Overflow Interrupt
  IRQ_TIMER1: 4,  // Timer 1 Overflow Interrupt
  IRQ_TIMER2: 5,  // Timer 2 Overflow Interrupt
  IRQ_TIMER3: 6,  // Timer 3 Overflow Interrupt
  IRQ_SIO: 7,  // Serial I/O Interrupt
  IRQ_DMA0: 8,  // DMA 0 Complete Interrupt
  IRQ_DMA1: 9,  // DMA 1 Complete Interrupt
  IRQ_DMA2: 10,  // DMA 2 Complete Interrupt
  IRQ_DMA3: 11,  // DMA 3 Complete Interrupt
  IRQ_KEYPAD: 12,  // Keypad Interrupt
  IRQ_CART: 13,  // Game Pak Interrupt

  // 引脚定义
  PIN_VSS: 1,  // Ground
  PIN_VDD: 2,  // Power Supply
  PIN_CLK: 3,  // System Clock Input (16.78MHz)
  PIN_RESET: 4,  // Reset Signal
  PIN_NMI: 5,  // Non-Maskable Interrupt
  PIN_IRQ: 6,  // Interrupt Request
  PIN_AB0: 7,  // Address Bus Bit 0
  PIN_AB1: 8,  // Address Bus Bit 1
  PIN_AB2: 9,  // Address Bus Bit 2
  PIN_AB3: 10,  // Address Bus Bit 3
  PIN_AB4: 11,  // Address Bus Bit 4
  PIN_AB5: 12,  // Address Bus Bit 5
  PIN_AB6: 13,  // Address Bus Bit 6
  PIN_AB7: 14,  // Address Bus Bit 7
  PIN_AB8: 15,  // Address Bus Bit 8
  PIN_AB9: 16,  // Address Bus Bit 9
  PIN_AB10: 17,  // Address Bus Bit 10
  PIN_AB11: 18,  // Address Bus Bit 11
  PIN_AB12: 19,  // Address Bus Bit 12
  PIN_AB13: 20,  // Address Bus Bit 13
  PIN_AB14: 21,  // Address Bus Bit 14
  PIN_AB15: 22,  // Address Bus Bit 15
  PIN_AB16: 23,  // Address Bus Bit 16
  PIN_AB17: 24,  // Address Bus Bit 17
  PIN_AB18: 25,  // Address Bus Bit 18
  PIN_AB19: 26,  // Address Bus Bit 19
  PIN_AB20: 27,  // Address Bus Bit 20
  PIN_AB21: 28,  // Address Bus Bit 21
  PIN_AB22: 29,  // Address Bus Bit 22
  PIN_AB23: 30,  // Address Bus Bit 23
  PIN_AB24: 31,  // Address Bus Bit 24
  PIN_AB25: 32,  // Address Bus Bit 25
  PIN_AB26: 33,  // Address Bus Bit 26
  PIN_AB27: 34,  // Address Bus Bit 27
  PIN_AB28: 35,  // Address Bus Bit 28
  PIN_AB29: 36,  // Address Bus Bit 29
  PIN_AB30: 37,  // Address Bus Bit 30
  PIN_AB31: 38,  // Address Bus Bit 31
  PIN_DB0: 39,  // Data Bus Bit 0
  PIN_DB1: 40,  // Data Bus Bit 1
  PIN_DB2: 41,  // Data Bus Bit 2
  PIN_DB3: 42,  // Data Bus Bit 3
  PIN_DB4: 43,  // Data Bus Bit 4
  PIN_DB5: 44,  // Data Bus Bit 5
  PIN_DB6: 45,  // Data Bus Bit 6
  PIN_DB7: 46,  // Data Bus Bit 7
  PIN_DB8: 47,  // Data Bus Bit 8
  PIN_DB9: 48,  // Data Bus Bit 9
  PIN_DB10: 49,  // Data Bus Bit 10
  PIN_DB11: 50,  // Data Bus Bit 11
  PIN_DB12: 51,  // Data Bus Bit 12
  PIN_DB13: 52,  // Data Bus Bit 13
  PIN_DB14: 53,  // Data Bus Bit 14
  PIN_DB15: 54,  // Data Bus Bit 15
  PIN_DB16: 55,  // Data Bus Bit 16
  PIN_DB17: 56,  // Data Bus Bit 17
  PIN_DB18: 57,  // Data Bus Bit 18
  PIN_DB19: 58,  // Data Bus Bit 19
  PIN_DB20: 59,  // Data Bus Bit 20
  PIN_DB21: 60,  // Data Bus Bit 21
  PIN_DB22: 61,  // Data Bus Bit 22
  PIN_DB23: 62,  // Data Bus Bit 23
  PIN_DB24: 63,  // Data Bus Bit 24
  PIN_DB25: 64,  // Data Bus Bit 25
  PIN_DB26: 65,  // Data Bus Bit 26
  PIN_DB27: 66,  // Data Bus Bit 27
  PIN_DB28: 67,  // Data Bus Bit 28
  PIN_DB29: 68,  // Data Bus Bit 29
  PIN_DB30: 69,  // Data Bus Bit 30
  PIN_DB31: 70,  // Data Bus Bit 31
  PIN_NCS0: 71,  // Chip Select 0 (ROM)
  PIN_NCS1: 72,  // Chip Select 1 (RAM)
  PIN_NWR: 73,  // Write Enable (active low)
  PIN_NRD: 74,  // Read Enable (active low)
  PIN_ADV: 75,  // Address Valid (for external DMA)
  PIN_BE0: 76,  // Byte Enable 0
  PIN_BE1: 77,  // Byte Enable 1
  PIN_BREQ: 78,  // Bus Request (from external master)
  PIN_BACK: 79,  // Bus Acknowledge
  PIN_EKO: 80,  // Serial Data Out (Link Cable)
  PIN_EKI: 81,  // Serial Data In (Link Cable)
  PIN_SOUND: 82,  // Stereo Audio Output (L+R)

  init: function() {
    // 硬件初始化
  }
};
