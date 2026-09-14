// NeoGeo-68000 设备定义 - Dart 库
// 生成自: SNK/M68K/NeoGeo-68000
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 12000000 Hz

class NeoGeo_68000Device {
  static const String deviceName = "NeoGeo-68000";
  static const String manufacturer = "SNK";
  static const String family = "M68K";
  static const String version = "1.0";
  static const String architecture = "MC68000";
  static const int bits = 32;
  static const int clockFrequency = 12000000;

  // 寄存器地址定义
  static const int D0_ADDR = 0x00;  // Data Register 0
  static const int D1_ADDR = 0x04;  // Data Register 1
  static const int D2_ADDR = 0x08;  // Data Register 2
  static const int D3_ADDR = 0x0C;  // Data Register 3
  static const int D4_ADDR = 0x10;  // Data Register 4
  static const int D5_ADDR = 0x14;  // Data Register 5
  static const int D6_ADDR = 0x18;  // Data Register 6
  static const int D7_ADDR = 0x1C;  // Data Register 7
  static const int A0_ADDR = 0x20;  // Address Register 0
  static const int A1_ADDR = 0x24;  // Address Register 1
  static const int A2_ADDR = 0x28;  // Address Register 2
  static const int A3_ADDR = 0x2C;  // Address Register 3
  static const int A4_ADDR = 0x30;  // Address Register 4
  static const int A5_ADDR = 0x34;  // Address Register 5
  static const int A6_ADDR = 0x38;  // Address Register 6
  static const int A7_ADDR = 0x3C;  // User Stack Pointer (USP)
  static const int SP_ADDR = 0x3C;  // Supervisor Stack Pointer (SSP)
  static const int PC_ADDR = 0x40;  // Program Counter
  static const int SR_ADDR = 0x44;  // Status Register
  static const int SR_C_BIT = 0;  // Carry
  static const int SR_V_BIT = 1;  // Overflow
  static const int SR_Z_BIT = 2;  // Zero
  static const int SR_N_BIT = 3;  // Negative
  static const int SR_X_BIT = 4;  // Extend
  static const int SR_I0_BIT = 8;  // Interrupt Mask 0
  static const int SR_I1_BIT = 9;  // Interrupt Mask 1
  static const int SR_I2_BIT = 10;  // Interrupt Mask 2
  static const int SR_M_BIT = 11;  // Master/Interrupt
  static const int SR_S_BIT = 13;  // Supervisor/User
  static const int SR_T0_BIT = 14;  // Trace Mode 0
  static const int SR_T1_BIT = 15;  // Trace Mode 1

  // 内存段定义
  static const int WORK_RAM_START = 0x100000;
  static const int WORK_RAM_END = 0x10FFFF;
  static const int WORK_RAM_SIZE = 65536;  // Work RAM (64KB)
  static const int BACKUP_RAM_START = 0x200000;
  static const int BACKUP_RAM_END = 0x20FFFF;
  static const int BACKUP_RAM_SIZE = 65536;  // Backup SRAM (battery-backed, 64KB)
  static const int FIX_ROM_START = 0x000000;
  static const int FIX_ROM_END = 0x07FFFF;
  static const int FIX_ROM_SIZE = 524288;  // Fix Layer ROM (512KB)
  static const int SPR_ROM_START = 0x400000;
  static const int SPR_ROM_END = 0x4FFFFF;
  static const int SPR_ROM_SIZE = 1048576;  // Sprite ROM (up to 1MB)
  static const int AUDIO_ROM_START = 0x800000;
  static const int AUDIO_ROM_END = 0x80FFFF;
  static const int AUDIO_ROM_SIZE = 65536;  // Audio ROM (up to 64KB)
  static const int CART_ROM_START = 0xC00000;
  static const int CART_ROM_END = 0xC7FFFF;
  static const int CART_ROM_SIZE = 524288;  // Cartridge ROM (up to 512KB, expandable)
  static const int IO_AREA_START = 0x300000;
  static const int IO_AREA_END = 0x3FFFFF;
  static const int IO_AREA_SIZE = 1048576;  // I/O Area (VDP, YM2610, Z80 port, etc.)
  static const int Z80_RAM_START = 0x10000;
  static const int Z80_RAM_END = 0x107FF;
  static const int Z80_RAM_SIZE = 2048;  // Z80 Work RAM (2KB)

  // 外设定义
  // Z80 Audio Coprocessor @ 4MHz
  static const int Z80_BASE = 0x300000;
  static const int Z80_Z80_A_ADDR = 0x00;
  static const int Z80_Z80_F_ADDR = 0x01;
  static const int Z80_Z80_B_ADDR = 0x02;
  static const int Z80_Z80_C_ADDR = 0x03;
  static const int Z80_Z80_D_ADDR = 0x04;
  static const int Z80_Z80_E_ADDR = 0x05;
  static const int Z80_Z80_H_ADDR = 0x06;
  static const int Z80_Z80_L_ADDR = 0x07;
  static const int Z80_Z80_AF_ADDR = 0x08;
  static const int Z80_Z80_BC_ADDR = 0x0A;
  static const int Z80_Z80_DE_ADDR = 0x0C;
  static const int Z80_Z80_HL_ADDR = 0x0E;
  static const int Z80_Z80_IX_ADDR = 0x10;
  static const int Z80_Z80_IY_ADDR = 0x12;
  static const int Z80_Z80_SP_ADDR = 0x14;
  static const int Z80_Z80_PC_ADDR = 0x16;
  static const int Z80_Z80_I_ADDR = 0x18;
  static const int Z80_Z80_R_ADDR = 0x19;
  static const int Z80_Z80_IM_ADDR = 0x1A;
  static const int Z80_Z80_BUSREQ_ADDR = 0x1E;
  static const int Z80_Z80_RESET_ADDR = 0x1F;
  // Yamaha YM2610 FM + ADPCM Audio Generator
  static const int YM2610_BASE = 0x300000;
  static const int YM2610_YM_ADDR_A0_ADDR = 0x00;
  static const int YM2610_YM_DATA_A0_ADDR = 0x01;
  static const int YM2610_YM_ADDR_A1_ADDR = 0x02;
  static const int YM2610_YM_DATA_A1_ADDR = 0x03;
  static const int YM2610_YM_ADDR_B0_ADDR = 0x04;
  static const int YM2610_YM_DATA_B0_ADDR = 0x05;
  static const int YM2610_YM_TEST_ADDR = 0x08;
  static const int YM2610_YM_FM_CH0_FREQ_L_ADDR = 0xA0;
  static const int YM2610_YM_FM_CH0_FREQ_H_ADDR = 0xA4;
  static const int YM2610_YM_FM_CH1_FREQ_L_ADDR = 0xA1;
  static const int YM2610_YM_FM_CH1_FREQ_H_ADDR = 0xA5;
  static const int YM2610_YM_FM_CH2_FREQ_L_ADDR = 0xA2;
  static const int YM2610_YM_FM_CH2_FREQ_H_ADDR = 0xA6;
  static const int YM2610_YM_FM_CH3_FREQ_L_ADDR = 0xA3;
  static const int YM2610_YM_FM_CH3_FREQ_H_ADDR = 0xA7;
  static const int YM2610_YM_FM_KEY_ON_ADDR = 0x28;
  static const int YM2610_YM_FM_CH0_ALG_ADDR = 0xB0;
  static const int YM2610_YM_FM_CH1_ALG_ADDR = 0xB1;
  static const int YM2610_YM_FM_CH2_ALG_ADDR = 0xB2;
  static const int YM2610_YM_FM_CH3_ALG_ADDR = 0xB3;
  static const int YM2610_YM_FM_TIMER_H_ADDR = 0x24;
  static const int YM2610_YM_FM_TIMER_L_ADDR = 0x25;
  static const int YM2610_YM_FM_TIMER_CTRL_ADDR = 0x27;
  static const int YM2610_YM_FM_TIMER_CTRL_TIMER_A_START_BIT = 0;  // Timer A Start
  static const int YM2610_YM_FM_TIMER_CTRL_TIMER_B_START_BIT = 1;  // Timer B Start
  static const int YM2610_YM_FM_TIMER_CTRL_LOAD_A_BIT = 2;  // Load Timer A
  static const int YM2610_YM_FM_TIMER_CTRL_LOAD_B_BIT = 3;  // Load Timer B
  static const int YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A_BIT = 4;  // Timer A IRQ Enable
  static const int YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B_BIT = 5;  // Timer B IRQ Enable
  static const int YM2610_YM_FM_TIMER_CTRL_CSM_MODE_BIT = 7;  // CSM Mode (auto Key-On after timer A)
  static const int YM2610_YM_FM_CH0_DETUNE_ADDR = 0x30;
  static const int YM2610_YM_FM_CH0_MUL_ADDR = 0x30;
  static const int YM2610_YM_FM_CH0_TL_ADDR = 0x40;
  static const int YM2610_YM_FM_CH0_KS_AR_ADDR = 0x50;
  static const int YM2610_YM_FM_CH0_AM_DR_ADDR = 0x60;
  static const int YM2610_YM_FM_CH0_SR_ADDR = 0x70;
  static const int YM2610_YM_FM_CH0_RR_SL_ADDR = 0x80;
  static const int YM2610_YM_FM_CH0_SSG_ADDR = 0x90;
  static const int YM2610_YM_SSG_CHA_FREQ_L_ADDR = 0x00;
  static const int YM2610_YM_SSG_CHA_FREQ_H_ADDR = 0x01;
  static const int YM2610_YM_SSG_CHB_FREQ_L_ADDR = 0x02;
  static const int YM2610_YM_SSG_CHB_FREQ_H_ADDR = 0x03;
  static const int YM2610_YM_SSG_CHC_FREQ_L_ADDR = 0x04;
  static const int YM2610_YM_SSG_CHC_FREQ_H_ADDR = 0x05;
  static const int YM2610_YM_SSG_CHA_VOL_ADDR = 0x08;
  static const int YM2610_YM_SSG_CHB_VOL_ADDR = 0x09;
  static const int YM2610_YM_SSG_CHC_VOL_ADDR = 0x0A;
  static const int YM2610_YM_SSG_MIXER_ADDR = 0x07;
  static const int YM2610_YM_SSG_ENV_FREQ_L_ADDR = 0x0B;
  static const int YM2610_YM_SSG_ENV_FREQ_H_ADDR = 0x0C;
  static const int YM2610_YM_SSG_ENV_SHAPE_ADDR = 0x0D;
  static const int YM2610_YM_SSG_IO_A_ADDR = 0x0E;
  static const int YM2610_YM_SSG_IO_B_ADDR = 0x0F;
  static const int YM2610_YM_ADPCM_STATUS_ADDR = 0x10;
  static const int YM2610_YM_ADPCM_START_ADDR = 0x11;
  static const int YM2610_YM_ADPCM_END_ADDR = 0x12;
  static const int YM2610_YM_ADPCM_VOL_L_ADDR = 0x13;
  static const int YM2610_YM_ADPCM_VOL_R_ADDR = 0x14;
  static const int YM2610_YM_DELTA_N_L_ADDR = 0x15;
  static const int YM2610_YM_DELTA_N_H_ADDR = 0x16;
  static const int YM2610_YM_ADPCM_B_START_ADDR = 0x18;
  static const int YM2610_YM_ADPCM_B_END_ADDR = 0x19;
  static const int YM2610_YM_ADPCM_B_VOL_ADDR = 0x1A;
  static const int YM2610_YM_ADPCM_B_CTRL_ADDR = 0x1B;
  // Neo Geo VDP (Video Display Processor)
  static const int YGV628_BASE = 0x3C0000;
  static const int YGV628_VRAM_ADDR_L_ADDR = 0x00;
  static const int YGV628_VRAM_ADDR_H_ADDR = 0x01;
  static const int YGV628_VRAM_DATA_ADDR = 0x02;
  static const int YGV628_VRAM_READ_ADDR = 0x03;
  static const int YGV628_CRAM_ADDR_ADDR = 0x04;
  static const int YGV628_CRAM_DATA_ADDR = 0x05;
  static const int YGV628_VDP_STATUS_ADDR = 0x06;
  static const int YGV628_VDP_STATUS_VBLANK_BIT = 0;  // V-Blank Flag
  static const int YGV628_VDP_STATUS_FIELD_BIT = 1;  // Field (0=even, 1=odd for interlace)
  static const int YGV628_VDP_STATUS_ODD_FIELD_BIT = 1;  // Odd Field Flag
  static const int YGV628_VDP_STATUS_DMA_BUSY_BIT = 2;  // DMA Busy
  static const int YGV628_VDP_STATUS_SPRITE_OVERFLOW_BIT = 3;  // Sprite Overflow (more than 16 per line)
  static const int YGV628_VDP_STATUS_SPRITE_COLLISION_BIT = 4;  // Sprite Collision
  static const int YGV628_VDP_CTRL_ADDR = 0x07;
  static const int YGV628_VDP_CTRL_VRAM_INC_BIT = 0;  // VRAM Auto-Increment (0=+1, 1=+2)
  static const int YGV628_VDP_CTRL_ROW_SCROLL_BIT = 1;  // Row Scroll Mode
  static const int YGV628_VDP_CTRL_COL_SCROLL_BIT = 2;  // Column Scroll Mode
  static const int YGV628_VDP_CTRL_FIX_DISP_BIT = 3;  // Fix Layer Display
  static const int YGV628_VDP_CTRL_SPR_DISP_BIT = 4;  // Sprite Layer Display
  static const int YGV628_VDP_CTRL_SCROLL2_DISP_BIT = 5;  // Scroll Layer 2 Display
  static const int YGV628_VDP_CTRL_SCROLL1_DISP_BIT = 6;  // Scroll Layer 1 Display
  static const int YGV628_VDP_CTRL_DMA_ENABLE_BIT = 7;  // DMA Enable
  static const int YGV628_SCROLL1_BASE_ADDR = 0x08;
  static const int YGV628_SCROLL2_BASE_ADDR = 0x0A;
  static const int YGV628_SPR_BASE_ADDR = 0x0C;
  static const int YGV628_SPR_COUNT_ADDR = 0x0E;
  static const int YGV628_WINDOW_X_ADDR = 0x10;
  static const int YGV628_WINDOW_Y_ADDR = 0x11;
  static const int YGV628_WINDOW_W_ADDR = 0x12;
  static const int YGV628_WINDOW_H_ADDR = 0x13;
  static const int YGV628_LINE_SCROLL_L_ADDR = 0x14;
  static const int YGV628_LINE_SCROLL_H_ADDR = 0x15;
  static const int YGV628_RASTER_COMP_ADDR = 0x16;
  static const int YGV628_H_TIMING_ADDR = 0x18;
  static const int YGV628_V_TIMING_ADDR = 0x19;
  static const int YGV628_DMA_SRC_L_ADDR = 0x1A;
  static const int YGV628_DMA_SRC_H_ADDR = 0x1B;
  static const int YGV628_DMA_SRC_B_ADDR = 0x1C;
  static const int YGV628_DMA_DEST_L_ADDR = 0x1D;
  static const int YGV628_DMA_DEST_H_ADDR = 0x1E;
  static const int YGV628_DMA_COUNT_ADDR = 0x1F;
  // Neo Geo System Driver / Controller
  static const int NEODRIVER_BASE = 0x310000;
  static const int NEODRIVER_PDI0_ADDR = 0x00;
  static const int NEODRIVER_PDI0_UP_BIT = 0;  // Up (0=pressed)
  static const int NEODRIVER_PDI0_DOWN_BIT = 1;  // Down (0=pressed)
  static const int NEODRIVER_PDI0_LEFT_BIT = 2;  // Left (0=pressed)
  static const int NEODRIVER_PDI0_RIGHT_BIT = 3;  // Right (0=pressed)
  static const int NEODRIVER_PDI0_A_BIT = 4;  // A Button (0=pressed)
  static const int NEODRIVER_PDI0_B_BIT = 5;  // B Button (0=pressed)
  static const int NEODRIVER_PDI0_C_BIT = 6;  // C Button (0=pressed)
  static const int NEODRIVER_PDI0_D_BIT = 7;  // D Button (0=pressed)
  static const int NEODRIVER_PDI1_ADDR = 0x01;
  static const int NEODRIVER_PDI2_ADDR = 0x02;
  static const int NEODRIVER_PDI3_ADDR = 0x03;
  static const int NEODRIVER_PDO0_ADDR = 0x04;
  static const int NEODRIVER_PDO1_ADDR = 0x05;
  static const int NEODRIVER_PDO2_ADDR = 0x06;
  static const int NEODRIVER_PDO3_ADDR = 0x07;
  static const int NEODRIVER_DIPSEL1_ADDR = 0x08;
  static const int NEODRIVER_DIPSEL1_COIN_SELECT_BIT = 0;  // Coin Select (0=common, 1=1 coin 1 credit)
  static const int NEODRIVER_DIPSEL1_FREE_PLAY_BIT = 1;  // Free Play
  static const int NEODRIVER_DIPSEL1_DEMO_SOUND_BIT = 2;  // Demo Sound
  static const int NEODRIVER_DIPSEL1_CHIP_MODE_BIT = 3;  // Chip Mode (0=AES, 1=MVS)
  static const int NEODRIVER_DIPSEL1_CONTROLLER_TYPE_BIT = 4;  // Controller Type (0=standard, 1=keyboard)
  static const int NEODRIVER_DIPSEL2_ADDR = 0x09;
  static const int NEODRIVER_DIPSEL3_ADDR = 0x0A;
  static const int NEODRIVER_DIPSEL4_ADDR = 0x0B;
  static const int NEODRIVER_SYSCTRL_ADDR = 0x0C;
  static const int NEODRIVER_SYSCTRL_RTSEL_BIT = 0;  // Real Time Switch Select
  static const int NEODRIVER_SYSCTRL_RESERVED0_BIT = 1;  // Reserved
  static const int NEODRIVER_SYSCTRL_SCC_BIT = 2;  // System Clock Control
  static const int NEODRIVER_SYSCTRL_PHEN_BIT = 3;  // PHEN (bus timing)
  static const int NEODRIVER_SYSCTRL_PCK2_BIT = 4;  // PCK2 (bus timing)
  static const int NEODRIVER_SYSCTRL_PCK1_BIT = 5;  // PCK1 (bus timing)
  static const int NEODRIVER_SYSCTRL_CKDIV2_BIT = 6;  // Clock Divide by 2
  static const int NEODRIVER_SYSCTRL_FEFIX_BIT = 7;  // FE Fix
  static const int NEODRIVER_IRQMASK_ADDR = 0x0D;
  static const int NEODRIVER_IRQMASK_VBLANK_MASK_BIT = 0;  // V-Blank Interrupt Mask
  static const int NEODRIVER_IRQMASK_HBLANK_MASK_BIT = 1;  // H-Blank Interrupt Mask
  static const int NEODRIVER_IRQMASK_VECTOR_IN_MASK_BIT = 2;  // Vector In (from Z80) Mask
  static const int NEODRIVER_IRQMASK_SYSTEM_IN_MASK_BIT = 3;  // System Input (JAMMA) Mask
  static const int NEODRIVER_IRQFLAG_ADDR = 0x0E;
  static const int NEODRIVER_SECAM_MODE_ADDR = 0x0F;
  // Controller Port 1
  static const int CONTROLLER1_BASE = 0x310000;
  static const int CONTROLLER1_PDI0_ADDR = 0x00;
  // Controller Port 2
  static const int CONTROLLER2_BASE = 0x310001;
  static const int CONTROLLER2_PDI1_ADDR = 0x00;
  // Memory Card Interface
  static const int MEMORY_CARD_BASE = 0x320000;
  static const int MEMORY_CARD_CARD_DATA_ADDR = 0x00;
  static const int MEMORY_CARD_CARD_STATUS_ADDR = 0x01;
  static const int MEMORY_CARD_CARD_STATUS_INSERTED_BIT = 0;  // Card Inserted (0=yes)
  static const int MEMORY_CARD_CARD_STATUS_WRITE_PROTECT_BIT = 1;  // Write Protected (0=yes)
  static const int MEMORY_CARD_CARD_STATUS_READY_BIT = 2;  // Ready for I/O
  static const int MEMORY_CARD_CARD_CTRL_ADDR = 0x02;
  // Cartridge Bank Switching
  static const int CART_BANK_BASE = 0x2FFFF0;
  static const int CART_BANK_BANK_REG_ADDR = 0x00;

  // 中断向量定义
  static const int INT_RESET_SP = 1;  // Reset Initial Stack Pointer
  static const int INT_RESET_PC = 2;  // Reset Initial PC
  static const int INT_BUS_ERROR = 3;  // Bus Error
  static const int INT_ADDRESS_ERROR = 4;  // Address Error
  static const int INT_ILLEGAL_INSTR = 5;  // Illegal Instruction
  static const int INT_ZERO_DIVIDE = 6;  // Zero Divide
  static const int INT_CHK_EXCEPTION = 7;  // CHK Exception
  static const int INT_TRAPV = 8;  // TRAPV Exception
  static const int INT_PRIVILEGE = 9;  // Privilege Violation
  static const int INT_TRACE = 10;  // Trace
  static const int INT_LINE_A = 11;  // Line 1010 Emulator
  static const int INT_LINE_F = 12;  // Line 1111 Emulator
  static const int INT_IRQ1 = 24;  // H-Blank / VDP Interrupt (raster)
  static const int INT_IRQ2 = 25;  // V-Blank / Frame End Interrupt
  static const int INT_IRQ3 = 26;  // System Controller / Z80 Vector In
  static const int INT_IRQ4 = 27;  // JAMMA / System Input
  static const int INT_IRQ5 = 28;  // Z80 Interrupt Request
  static const int INT_TRAP0 = 32;  // TRAP #0 (system call)
  static const int INT_TRAP1 = 33;  // TRAP #1

  // 引脚定义
  static const int PIN_VCC = 1;  // Power Supply (5V)
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // System Clock (12MHz for 68K)
  static const int PIN_RESET = 4;  // Reset (active low)
  static const int PIN_HALT = 5;  // Halt (stops CPU)
  static const int PIN_NMI = 6;  // Non-Maskable Interrupt
  static const int PIN_IPL0 = 7;  // Interrupt Priority Level 0
  static const int PIN_IPL1 = 8;  // Interrupt Priority Level 1
  static const int PIN_IPL2 = 9;  // Interrupt Priority Level 2
  static const int PIN_DTACK = 10;  // Data Acknowledge (active low)
  static const int PIN_BERR = 11;  // Bus Error (active low)
  static const int PIN_BR = 12;  // Bus Request (active low)
  static const int PIN_BG = 13;  // Bus Grant (active low)
  static const int PIN_A0 = 14;  // Address Bus Bit 0
  static const int PIN_A1 = 15;  // Address Bus Bit 1
  static const int PIN_A2 = 16;  // Address Bus Bit 2
  static const int PIN_A3 = 17;  // Address Bus Bit 3
  static const int PIN_A4 = 18;  // Address Bus Bit 4
  static const int PIN_A5 = 19;  // Address Bus Bit 5
  static const int PIN_A6 = 20;  // Address Bus Bit 6
  static const int PIN_A7 = 21;  // Address Bus Bit 7
  static const int PIN_A8 = 22;  // Address Bus Bit 8
  static const int PIN_A9 = 23;  // Address Bus Bit 9
  static const int PIN_A10 = 24;  // Address Bus Bit 10
  static const int PIN_A11 = 25;  // Address Bus Bit 11
  static const int PIN_A12 = 26;  // Address Bus Bit 12
  static const int PIN_A13 = 27;  // Address Bus Bit 13
  static const int PIN_A14 = 28;  // Address Bus Bit 14
  static const int PIN_A15 = 29;  // Address Bus Bit 15
  static const int PIN_A16 = 30;  // Address Bus Bit 16
  static const int PIN_A17 = 31;  // Address Bus Bit 17
  static const int PIN_A18 = 32;  // Address Bus Bit 18
  static const int PIN_A19 = 33;  // Address Bus Bit 19
  static const int PIN_A20 = 34;  // Address Bus Bit 20
  static const int PIN_A21 = 35;  // Address Bus Bit 21
  static const int PIN_A22 = 36;  // Address Bus Bit 22
  static const int PIN_A23 = 37;  // Address Bus Bit 23
  static const int PIN_D0 = 38;  // Data Bus Bit 0
  static const int PIN_D1 = 39;  // Data Bus Bit 1
  static const int PIN_D2 = 40;  // Data Bus Bit 2
  static const int PIN_D3 = 41;  // Data Bus Bit 3
  static const int PIN_D4 = 42;  // Data Bus Bit 4
  static const int PIN_D5 = 43;  // Data Bus Bit 5
  static const int PIN_D6 = 44;  // Data Bus Bit 6
  static const int PIN_D7 = 45;  // Data Bus Bit 7
  static const int PIN_D8 = 46;  // Data Bus Bit 8
  static const int PIN_D9 = 47;  // Data Bus Bit 9
  static const int PIN_D10 = 48;  // Data Bus Bit 10
  static const int PIN_D11 = 49;  // Data Bus Bit 11
  static const int PIN_D12 = 50;  // Data Bus Bit 12
  static const int PIN_D13 = 51;  // Data Bus Bit 13
  static const int PIN_D14 = 52;  // Data Bus Bit 14
  static const int PIN_D15 = 53;  // Data Bus Bit 15
  static const int PIN_AS = 54;  // Address Strobe (active low)
  static const int PIN_UDS = 55;  // Upper Data Strobe (active low)
  static const int PIN_LDS = 56;  // Lower Data Strobe (active low)
  static const int PIN_R_W = 57;  // Read/Write (1=Read, 0=Write)
  static const int PIN_FC0 = 58;  // Function Code 0
  static const int PIN_FC1 = 59;  // Function Code 1
  static const int PIN_FC2 = 60;  // Function Code 2
  static const int PIN_E = 61;  // E Clock (Enable, for Z80 sync)
  static const int PIN_VPA = 62;  // Valid Peripheral Address (for Z80 I/O)
  static const int PIN_VM = 63;  // Valid Memory (for Z80 memory access)
  static const int PIN_BKGR = 64;  // Background Audio Mix (analog output)
  static const int PIN_AUDIO_OUT = 65;  // Main Audio Output (Left)
  static const int PIN_AUDIO_R = 66;  // Audio Right Channel
  static const int PIN_VIDEO_R = 67;  // Video Output Red
  static const int PIN_VIDEO_G = 68;  // Video Output Green
  static const int PIN_VIDEO_B = 69;  // Video Output Blue
  static const int PIN_SYNC = 70;  // Video Sync

}
