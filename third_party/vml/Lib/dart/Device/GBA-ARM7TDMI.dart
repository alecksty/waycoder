// ARM7TDMI 设备定义 - Dart 库
// 生成自: ARM/ARM7/ARM7TDMI
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Game Boy Advance main processor - ARM7TDMI @ 16.78MHz with 32-bit ARM + 16-bit Thumb instruction sets
// CPU架构: ARM7TDMI
// 位宽: 32位
// 时钟频率: 16780000 Hz

class ARM7TDMIDevice {
  static const String deviceName = "ARM7TDMI";
  static const String manufacturer = "ARM";
  static const String family = "ARM7";
  static const String version = "1.0";
  static const String architecture = "ARM7TDMI";
  static const int bits = 32;
  static const int clockFrequency = 16780000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // General Purpose Register 0
  static const int R1_ADDR = 0x04;  // General Purpose Register 1
  static const int R2_ADDR = 0x08;  // General Purpose Register 2
  static const int R3_ADDR = 0x0C;  // General Purpose Register 3
  static const int R4_ADDR = 0x10;  // General Purpose Register 4
  static const int R5_ADDR = 0x14;  // General Purpose Register 5
  static const int R6_ADDR = 0x18;  // General Purpose Register 6
  static const int R7_ADDR = 0x1C;  // General Purpose Register 7
  static const int R8_ADDR = 0x20;  // General Purpose Register 8
  static const int R9_ADDR = 0x24;  // General Purpose Register 9 / SB
  static const int R10_ADDR = 0x28;  // General Purpose Register 10 / SL
  static const int R11_ADDR = 0x2C;  // Frame Pointer / FP
  static const int R12_ADDR = 0x30;  // Intra-Procedure-call Scratch Register / IP
  static const int R13_ADDR = 0x34;  // Stack Pointer / SP
  static const int R14_ADDR = 0x38;  // Link Register / LR
  static const int R15_ADDR = 0x3C;  // Program Counter / PC
  static const int CPSR_ADDR = 0x40;  // Current Program Status Register
  static const int CPSR_MODE_BIT = 0;  // Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
  static const int CPSR_T_BIT = 5;  // Thumb State Bit (1=Thumb mode)
  static const int CPSR_F_BIT = 6;  // FIQ Disable
  static const int CPSR_I_BIT = 7;  // IRQ Disable
  static const int CPSR_A_BIT = 8;  // Imprecise Data Abort Disable
  static const int CPSR_E_BIT = 9;  // Endianness (0=Little)
  static const int CPSR_GE_BIT = 0;  // Greater-than-or-Equal flags
  static const int CPSR_N_BIT = 31;  // Negative
  static const int CPSR_Z_BIT = 30;  // Zero
  static const int CPSR_C_BIT = 29;  // Carry
  static const int CPSR_V_BIT = 28;  // Overflow
  static const int SPSR_SVC_ADDR = 0x44;  // Saved PSR (Supervisor Mode)
  static const int SPSR_ABT_ADDR = 0x48;  // Saved PSR (Abort Mode)
  static const int SPSR_IRQ_ADDR = 0x4C;  // Saved PSR (IRQ Mode)
  static const int SPSR_FIQ_ADDR = 0x50;  // Saved PSR (FIQ Mode)

  // 内存段定义
  static const int IWRAM_START = 0x03000000;
  static const int IWRAM_END = 0x03007FFF;
  static const int IWRAM_SIZE = 32768;  // Internal Work RAM (32KB, 2-cycle access)
  static const int IWRAM_FAST_START = 0x03008000;
  static const int IWRAM_FAST_END = 0x03FFFFFF;
  static const int IWRAM_FAST_SIZE = 32752;  // Internal Work RAM Fast (high-speed region)
  static const int VRAM_START = 0x06000000;
  static const int VRAM_END = 0x06017FFF;
  static const int VRAM_SIZE = 98304;  // Video RAM (96KB + 64KB OBJ VRAM)
  static const int PALETTE_START = 0x05000200;
  static const int PALETTE_END = 0x050003FF;
  static const int PALETTE_SIZE = 512;  // BG Palette RAM (256 colors x 2 bytes)
  static const int OBJ_PALETTE_START = 0x05000400;
  static const int OBJ_PALETTE_END = 0x050005FF;
  static const int OBJ_PALETTE_SIZE = 512;  // Object Palette RAM
  static const int OAM_START = 0x07000000;
  static const int OAM_END = 0x070003FF;
  static const int OAM_SIZE = 1024;  // Object Attribute Memory (OAM, 128 sprites)
  static const int ROM_START = 0x08000000;
  static const int ROM_END = 0x09FFFFFF;
  static const int ROM_SIZE = 33554432;  // Cartridge ROM (max 32MB)
  static const int CART_RAM_START = 0x0E000000;
  static const int CART_RAM_END = 0x0E00FFFF;
  static const int CART_RAM_SIZE = 65536;  // Cartridge SRAM / Flash
  static const int BIOS_START = 0x00000000;
  static const int BIOS_END = 0x00003FFF;
  static const int BIOS_SIZE = 16384;  // GBA BIOS (16KB)
  static const int IO_REGS_START = 0x04000000;
  static const int IO_REGS_END = 0x04FFFFFF;
  static const int IO_REGS_SIZE = 16777216;  // I/O Registers (MMIO)

  // 外设定义
  // LCD Controller
  static const int LCD_BASE = 0x04000000;
  static const int LCD_DISPCNT_ADDR = 0x04000000;
  static const int LCD_DISPCNT_BG_MODE_BIT = 0;  // BG Mode (0-6)
  static const int LCD_DISPCNT_GB_WINDOW_BIT = 5;  // Game Boy Window Enable
  static const int LCD_DISPCNT_WIN0_ENABLE_BIT = 13;  // Window 0 Enable
  static const int LCD_DISPCNT_WIN1_ENABLE_BIT = 14;  // Window 1 Enable
  static const int LCD_DISPCNT_OBJ_WIN_BIT = 15;  // Object Window Enable
  static const int LCD_DISPCNT_BG0_ENABLE_BIT = 8;  // BG0 Enable
  static const int LCD_DISPCNT_BG1_ENABLE_BIT = 9;  // BG1 Enable
  static const int LCD_DISPCNT_BG2_ENABLE_BIT = 10;  // BG2 Enable
  static const int LCD_DISPCNT_BG3_ENABLE_BIT = 11;  // BG3 Enable
  static const int LCD_DISPCNT_OBJ_ENABLE_BIT = 12;  // Object/Sprite Enable
  static const int LCD_GREEN_SWAP_ADDR = 0x04000002;
  static const int LCD_DISPSTAT_ADDR = 0x04000004;
  static const int LCD_DISPSTAT_V_COUNT_BIT = 0;  // Vertical Line Counter
  static const int LCD_DISPSTAT_VBLANK_FLAG_BIT = 0;  // V-Blank Flag (read-only)
  static const int LCD_DISPSTAT_HBLANK_FLAG_BIT = 1;  // H-Blank Flag (read-only)
  static const int LCD_DISPSTAT_V_COUNT_FLAG_BIT = 2;  // V-Count Flag (LY==LYC)
  static const int LCD_DISPSTAT_VBLANK_IRQ_BIT = 3;  // V-Blank IRQ Enable
  static const int LCD_DISPSTAT_HBLANK_IRQ_BIT = 4;  // H-Blank IRQ Enable
  static const int LCD_DISPSTAT_VCOUNT_IRQ_BIT = 5;  // V-Count IRQ Enable
  static const int LCD_VCOUNT_ADDR = 0x04000006;
  static const int LCD_BG0CNT_ADDR = 0x04000008;
  static const int LCD_BG1CNT_ADDR = 0x0400000A;
  static const int LCD_BG2CNT_ADDR = 0x0400000C;
  static const int LCD_BG3CNT_ADDR = 0x0400000E;
  static const int LCD_BG0HOFS_ADDR = 0x04000010;
  static const int LCD_BG0VOFS_ADDR = 0x04000012;
  static const int LCD_BG1HOFS_ADDR = 0x04000014;
  static const int LCD_BG1VOFS_ADDR = 0x04000016;
  static const int LCD_BG2HOFS_ADDR = 0x04000018;
  static const int LCD_BG2VOFS_ADDR = 0x0400001A;
  static const int LCD_BG3HOFS_ADDR = 0x0400001C;
  static const int LCD_BG3VOFS_ADDR = 0x0400001E;
  static const int LCD_BG2PA_ADDR = 0x04000020;
  static const int LCD_BG2PB_ADDR = 0x04000022;
  static const int LCD_BG2PC_ADDR = 0x04000024;
  static const int LCD_BG2PD_ADDR = 0x04000026;
  static const int LCD_BG2X_ADDR = 0x04000028;
  static const int LCD_BG2Y_ADDR = 0x0400002C;
  static const int LCD_BG3PA_ADDR = 0x04000030;
  static const int LCD_BG3PB_ADDR = 0x04000032;
  static const int LCD_BG3PC_ADDR = 0x04000034;
  static const int LCD_BG3PD_ADDR = 0x04000036;
  static const int LCD_BG3X_ADDR = 0x04000038;
  static const int LCD_BG3Y_ADDR = 0x0400003C;
  static const int LCD_WIN0H_ADDR = 0x04000040;
  static const int LCD_WIN1H_ADDR = 0x04000042;
  static const int LCD_WIN0V_ADDR = 0x04000044;
  static const int LCD_WIN1V_ADDR = 0x04000046;
  static const int LCD_WININ_ADDR = 0x04000048;
  static const int LCD_WINOUT_ADDR = 0x04000049;
  static const int LCD_MOSAIC_ADDR = 0x0400004C;
  static const int LCD_BLDCNT_ADDR = 0x04000050;
  static const int LCD_BLDCNT_BG1ST_BIT = 0;  // BG1 1st Target
  static const int LCD_BLDCNT_BG2ST_BIT = 1;  // BG2 1st Target
  static const int LCD_BLDCNT_BG3ST_BIT = 2;  // BG3 1st Target
  static const int LCD_BLDCNT_OBJST_BIT = 3;  // Object 1st Target
  static const int LCD_BLDCNT_BDST_BIT = 4;  // Backdrop 1st Target
  static const int LCD_BLDCNT_BLEND_MODE_BIT = 0;  // Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
  static const int LCD_BLDCNT_BG1ST2_BIT = 8;  // BG1 2nd Target
  static const int LCD_BLDCNT_BG2ST2_BIT = 9;  // BG2 2nd Target
  static const int LCD_BLDCNT_BG3ST2_BIT = 10;  // BG3 2nd Target
  static const int LCD_BLDCNT_OBJST2_BIT = 11;  // Object 2nd Target
  static const int LCD_BLDCNT_BDST2_BIT = 12;  // Backdrop 2nd Target
  static const int LCD_BLDALPHA_ADDR = 0x04000052;
  static const int LCD_BLDY_ADDR = 0x04000054;
  // Direct Memory Access Controller
  static const int DMA_BASE = 0x040000B0;
  static const int DMA_DMA0SAD_ADDR = 0x040000B0;
  static const int DMA_DMA0DAD_ADDR = 0x040000B4;
  static const int DMA_DMA0CNT_L_ADDR = 0x040000B8;
  static const int DMA_DMA0CNT_H_ADDR = 0x040000BA;
  static const int DMA_DMA0CNT_H_TRANSFER_COUNT_BIT = 0;  // Number of Transfers
  static const int DMA_DMA0CNT_H_DEST_ADD_MODE_BIT = 0;  // Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
  static const int DMA_DMA0CNT_H_SRC_ADD_MODE_BIT = 0;  // Source Address Control (0=fix, 1=inc, 2=dec)
  static const int DMA_DMA0CNT_H_REPEAT_BIT = 18;  // Repeat (for 16-bit repeat mode)
  static const int DMA_DMA0CNT_H_WORD_SIZE_BIT = 20;  // Word Size (0=16-bit, 1=32-bit)
  static const int DMA_DMA0CNT_H_DRQ_BIT = 27;  // DRQ Trigger (DMA from external source)
  static const int DMA_DMA0CNT_H_TIMING_BIT = 0;  // Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
  static const int DMA_DMA0CNT_H_ENABLE_BIT = 31;  // DMA Enable
  static const int DMA_DMA1SAD_ADDR = 0x040000BC;
  static const int DMA_DMA1DAD_ADDR = 0x040000C0;
  static const int DMA_DMA1CNT_L_ADDR = 0x040000C4;
  static const int DMA_DMA1CNT_H_ADDR = 0x040000C6;
  static const int DMA_DMA2SAD_ADDR = 0x040000C8;
  static const int DMA_DMA2DAD_ADDR = 0x040000CC;
  static const int DMA_DMA2CNT_L_ADDR = 0x040000D0;
  static const int DMA_DMA2CNT_H_ADDR = 0x040000D2;
  static const int DMA_DMA3SAD_ADDR = 0x040000D4;
  static const int DMA_DMA3DAD_ADDR = 0x040000D8;
  static const int DMA_DMA3CNT_L_ADDR = 0x040000DC;
  static const int DMA_DMA3CNT_H_ADDR = 0x040000DE;
  // Timer Units (4 timers)
  static const int TIMER_BASE = 0x04000100;
  static const int TIMER_TM0CNT_L_ADDR = 0x04000100;
  static const int TIMER_TM0CNT_H_ADDR = 0x04000102;
  static const int TIMER_TM0CNT_H_PRESCALER_BIT = 0;  // Prescaler (0=1, 1=64, 2=256, 3=1024)
  static const int TIMER_TM0CNT_H_COUNT_UP_BIT = 2;  // Count Up (cascade mode)
  static const int TIMER_TM0CNT_H_IRQ_ENABLE_BIT = 6;  // Timer IRQ Enable
  static const int TIMER_TM0CNT_H_ENABLE_BIT = 7;  // Timer Enable
  static const int TIMER_TM1CNT_L_ADDR = 0x04000104;
  static const int TIMER_TM1CNT_H_ADDR = 0x04000106;
  static const int TIMER_TM2CNT_L_ADDR = 0x04000108;
  static const int TIMER_TM2CNT_H_ADDR = 0x0400010A;
  static const int TIMER_TM3CNT_L_ADDR = 0x0400010C;
  static const int TIMER_TM3CNT_H_ADDR = 0x0400010E;
  // Serial I/O (JOY BUS / Link Cable)
  static const int SIO_BASE = 0x04000120;
  static const int SIO_SIOCNT_ADDR = 0x04000120;
  static const int SIO_SIOCNT_CLOCK_SEL_BIT = 0;  // Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
  static const int SIO_SIOCNT_SO_ENABLE_BIT = 3;  // SO Output Enable
  static const int SIO_SIOCNT_RECV_ENABLE_BIT = 5;  // Receive Enable
  static const int SIO_SIOCNT_SEND_ENABLE_BIT = 6;  // Send Enable
  static const int SIO_SIOCNT_START_BIT_BIT = 7;  // Start Transfer
  static const int SIO_SIODATA8_ADDR = 0x0400012A;
  static const int SIO_JOYCNT_ADDR = 0x04000130;
  static const int SIO_JOYSTAT_ADDR = 0x04000134;
  static const int SIO_JOY_RECV_ADDR = 0x04000150;
  static const int SIO_JOY_TRANS_ADDR = 0x04000154;
  // Key Input
  static const int KEYINPUT_BASE = 0x04000130;
  static const int KEYINPUT_KEYINPUT_ADDR = 0x04000130;
  static const int KEYINPUT_KEYINPUT_A_BIT = 0;  // A Button (0=Pressed)
  static const int KEYINPUT_KEYINPUT_B_BIT = 1;  // B Button (0=Pressed)
  static const int KEYINPUT_KEYINPUT_SELECT_BIT = 2;  // Select Button (0=Pressed)
  static const int KEYINPUT_KEYINPUT_START_BIT = 3;  // Start Button (0=Pressed)
  static const int KEYINPUT_KEYINPUT_RIGHT_BIT = 4;  // D-Pad Right (0=Pressed)
  static const int KEYINPUT_KEYINPUT_LEFT_BIT = 5;  // D-Pad Left (0=Pressed)
  static const int KEYINPUT_KEYINPUT_UP_BIT = 6;  // D-Pad Up (0=Pressed)
  static const int KEYINPUT_KEYINPUT_DOWN_BIT = 7;  // D-Pad Down (0=Pressed)
  static const int KEYINPUT_KEYINPUT_R_BIT = 8;  // R Shoulder Button (0=Pressed)
  static const int KEYINPUT_KEYINPUT_L_BIT = 9;  // L Shoulder Button (0=Pressed)
  static const int KEYINPUT_KEYCNT_ADDR = 0x04000132;
  static const int KEYINPUT_KEYCNT_KEY_MASK_BIT = 0;  // Key Interrupt Enable Mask
  static const int KEYINPUT_KEYCNT_IRQ_ENABLE_BIT = 14;  // Key Interrupt Enable
  // Interrupt Control
  static const int INTERRUPT_BASE = 0x04000200;
  static const int INTERRUPT_IME_ADDR = 0x04000208;
  static const int INTERRUPT_IE_ADDR = 0x04000210;
  static const int INTERRUPT_IE_VBLANK_BIT = 0;  // V-Blank Interrupt Enable
  static const int INTERRUPT_IE_HBLANK_BIT = 1;  // H-Blank Interrupt Enable
  static const int INTERRUPT_IE_VCOUNT_BIT = 2;  // V-Count Match Interrupt Enable
  static const int INTERRUPT_IE_TIMER0_BIT = 3;  // Timer 0 Interrupt Enable
  static const int INTERRUPT_IE_TIMER1_BIT = 4;  // Timer 1 Interrupt Enable
  static const int INTERRUPT_IE_TIMER2_BIT = 5;  // Timer 2 Interrupt Enable
  static const int INTERRUPT_IE_TIMER3_BIT = 6;  // Timer 3 Interrupt Enable
  static const int INTERRUPT_IE_SIO_BIT = 7;  // Serial I/O Interrupt Enable
  static const int INTERRUPT_IE_DMA0_BIT = 8;  // DMA 0 Interrupt Enable
  static const int INTERRUPT_IE_DMA1_BIT = 9;  // DMA 1 Interrupt Enable
  static const int INTERRUPT_IE_DMA2_BIT = 10;  // DMA 2 Interrupt Enable
  static const int INTERRUPT_IE_DMA3_BIT = 11;  // DMA 3 Interrupt Enable
  static const int INTERRUPT_IE_KEYPAD_BIT = 12;  // Keypad Interrupt Enable
  static const int INTERRUPT_IE_CART_BIT = 13;  // Game Pak Interrupt Enable
  static const int INTERRUPT_IF_ADDR = 0x04000214;
  // Waitstate Control
  static const int WAITCNT_BASE = 0x04000204;
  static const int WAITCNT_WAITCNT_ADDR = 0x04000204;
  static const int WAITCNT_WAITCNT_PHI_OD_BIT = 0;  // PHI Terminal Output (0=Disable)
  static const int WAITCNT_WAITCNT_SRAM_WS_BIT = 0;  // SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
  static const int WAITCNT_WAITCNT_WS0_N_BIT = 0;  // Wait State 0 (ROM/SRAM 1st access)
  static const int WAITCNT_WAITCNT_WS0_S_BIT = 5;  // Wait State 0 (ROM/SRAM 2nd access)
  static const int WAITCNT_WAITCNT_WS1_N_BIT = 0;  // Wait State 1 (ROM 2nd access)
  static const int WAITCNT_WAITCNT_WS1_S_BIT = 8;  // Wait State 1 (ROM 2nd access short)
  static const int WAITCNT_WAITCNT_WS2_N_BIT = 0;  // Wait State 2 (ROM 3rd access)
  static const int WAITCNT_WAITCNT_WS2_S_BIT = 11;  // Wait State 2 (ROM 3rd access short)
  static const int WAITCNT_WAITCNT_PREFE_BIT = 12;  // Prefetch Enable (GBA SP only)

  // 中断向量定义
  static const int INT_VBLANK = 0;  // V-Blank Interrupt
  static const int INT_HBLANK = 1;  // H-Blank Interrupt
  static const int INT_VCOUNT = 2;  // V-Count Match Interrupt
  static const int INT_TIMER0 = 3;  // Timer 0 Overflow Interrupt
  static const int INT_TIMER1 = 4;  // Timer 1 Overflow Interrupt
  static const int INT_TIMER2 = 5;  // Timer 2 Overflow Interrupt
  static const int INT_TIMER3 = 6;  // Timer 3 Overflow Interrupt
  static const int INT_SIO = 7;  // Serial I/O Interrupt
  static const int INT_DMA0 = 8;  // DMA 0 Complete Interrupt
  static const int INT_DMA1 = 9;  // DMA 1 Complete Interrupt
  static const int INT_DMA2 = 10;  // DMA 2 Complete Interrupt
  static const int INT_DMA3 = 11;  // DMA 3 Complete Interrupt
  static const int INT_KEYPAD = 12;  // Keypad Interrupt
  static const int INT_CART = 13;  // Game Pak Interrupt

  // 引脚定义
  static const int PIN_VSS = 1;  // Ground
  static const int PIN_VDD = 2;  // Power Supply
  static const int PIN_CLK = 3;  // System Clock Input (16.78MHz)
  static const int PIN_RESET = 4;  // Reset Signal
  static const int PIN_NMI = 5;  // Non-Maskable Interrupt
  static const int PIN_IRQ = 6;  // Interrupt Request
  static const int PIN_AB0 = 7;  // Address Bus Bit 0
  static const int PIN_AB1 = 8;  // Address Bus Bit 1
  static const int PIN_AB2 = 9;  // Address Bus Bit 2
  static const int PIN_AB3 = 10;  // Address Bus Bit 3
  static const int PIN_AB4 = 11;  // Address Bus Bit 4
  static const int PIN_AB5 = 12;  // Address Bus Bit 5
  static const int PIN_AB6 = 13;  // Address Bus Bit 6
  static const int PIN_AB7 = 14;  // Address Bus Bit 7
  static const int PIN_AB8 = 15;  // Address Bus Bit 8
  static const int PIN_AB9 = 16;  // Address Bus Bit 9
  static const int PIN_AB10 = 17;  // Address Bus Bit 10
  static const int PIN_AB11 = 18;  // Address Bus Bit 11
  static const int PIN_AB12 = 19;  // Address Bus Bit 12
  static const int PIN_AB13 = 20;  // Address Bus Bit 13
  static const int PIN_AB14 = 21;  // Address Bus Bit 14
  static const int PIN_AB15 = 22;  // Address Bus Bit 15
  static const int PIN_AB16 = 23;  // Address Bus Bit 16
  static const int PIN_AB17 = 24;  // Address Bus Bit 17
  static const int PIN_AB18 = 25;  // Address Bus Bit 18
  static const int PIN_AB19 = 26;  // Address Bus Bit 19
  static const int PIN_AB20 = 27;  // Address Bus Bit 20
  static const int PIN_AB21 = 28;  // Address Bus Bit 21
  static const int PIN_AB22 = 29;  // Address Bus Bit 22
  static const int PIN_AB23 = 30;  // Address Bus Bit 23
  static const int PIN_AB24 = 31;  // Address Bus Bit 24
  static const int PIN_AB25 = 32;  // Address Bus Bit 25
  static const int PIN_AB26 = 33;  // Address Bus Bit 26
  static const int PIN_AB27 = 34;  // Address Bus Bit 27
  static const int PIN_AB28 = 35;  // Address Bus Bit 28
  static const int PIN_AB29 = 36;  // Address Bus Bit 29
  static const int PIN_AB30 = 37;  // Address Bus Bit 30
  static const int PIN_AB31 = 38;  // Address Bus Bit 31
  static const int PIN_DB0 = 39;  // Data Bus Bit 0
  static const int PIN_DB1 = 40;  // Data Bus Bit 1
  static const int PIN_DB2 = 41;  // Data Bus Bit 2
  static const int PIN_DB3 = 42;  // Data Bus Bit 3
  static const int PIN_DB4 = 43;  // Data Bus Bit 4
  static const int PIN_DB5 = 44;  // Data Bus Bit 5
  static const int PIN_DB6 = 45;  // Data Bus Bit 6
  static const int PIN_DB7 = 46;  // Data Bus Bit 7
  static const int PIN_DB8 = 47;  // Data Bus Bit 8
  static const int PIN_DB9 = 48;  // Data Bus Bit 9
  static const int PIN_DB10 = 49;  // Data Bus Bit 10
  static const int PIN_DB11 = 50;  // Data Bus Bit 11
  static const int PIN_DB12 = 51;  // Data Bus Bit 12
  static const int PIN_DB13 = 52;  // Data Bus Bit 13
  static const int PIN_DB14 = 53;  // Data Bus Bit 14
  static const int PIN_DB15 = 54;  // Data Bus Bit 15
  static const int PIN_DB16 = 55;  // Data Bus Bit 16
  static const int PIN_DB17 = 56;  // Data Bus Bit 17
  static const int PIN_DB18 = 57;  // Data Bus Bit 18
  static const int PIN_DB19 = 58;  // Data Bus Bit 19
  static const int PIN_DB20 = 59;  // Data Bus Bit 20
  static const int PIN_DB21 = 60;  // Data Bus Bit 21
  static const int PIN_DB22 = 61;  // Data Bus Bit 22
  static const int PIN_DB23 = 62;  // Data Bus Bit 23
  static const int PIN_DB24 = 63;  // Data Bus Bit 24
  static const int PIN_DB25 = 64;  // Data Bus Bit 25
  static const int PIN_DB26 = 65;  // Data Bus Bit 26
  static const int PIN_DB27 = 66;  // Data Bus Bit 27
  static const int PIN_DB28 = 67;  // Data Bus Bit 28
  static const int PIN_DB29 = 68;  // Data Bus Bit 29
  static const int PIN_DB30 = 69;  // Data Bus Bit 30
  static const int PIN_DB31 = 70;  // Data Bus Bit 31
  static const int PIN_NCS0 = 71;  // Chip Select 0 (ROM)
  static const int PIN_NCS1 = 72;  // Chip Select 1 (RAM)
  static const int PIN_NWR = 73;  // Write Enable (active low)
  static const int PIN_NRD = 74;  // Read Enable (active low)
  static const int PIN_ADV = 75;  // Address Valid (for external DMA)
  static const int PIN_BE0 = 76;  // Byte Enable 0
  static const int PIN_BE1 = 77;  // Byte Enable 1
  static const int PIN_BREQ = 78;  // Bus Request (from external master)
  static const int PIN_BACK = 79;  // Bus Acknowledge
  static const int PIN_EKO = 80;  // Serial Data Out (Link Cable)
  static const int PIN_EKI = 81;  // Serial Data In (Link Cable)
  static const int PIN_SOUND = 82;  // Stereo Audio Output (L+R)

}
